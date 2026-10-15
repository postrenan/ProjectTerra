"""
Gerador de RELEVO REAL por regiao a partir do Copernicus GLO-30 DEM
(modelo digital de superficie global de 30 m, AWS Open Data, livre).

Substitui o heightmap procedural (BatchGeographicDataProcessor.py) por elevacao
real: para cada regiao le APENAS a janela da bounding-box no COG via HTTP range
requests (/vsicurl) e grava no MESMO formato binario "HMAP" que o runtime ja
consome (GeographicDataLoader / RegionalSandboxManager), agora com:

  * alturas reais normalizadas (16-bit),
  * minElevation / maxElevation / elevationRange reais (metros),
  * seaLevelNormalized real,
  * waterPercent e isCoastal derivados do ESA WorldCover (landcover_<id>.raw),
  * nome/pais/dimensoes vindos do banco de regioes.

Os heightmaps reais sao gravados com version=2 no cabecalho, o que permite
--skip-existing pular o que ja foi convertido (os procedurais tem version=1).

Uso:
    python Tools/GenerateRegionElevation.py --id 1
    python Tools/GenerateRegionElevation.py --ids 1,2,3
    python Tools/GenerateRegionElevation.py --all --workers 8 --skip-existing

Dependencias: rasterio, certifi, numpy
"""

import os
import sys
import math
import time
import struct
import argparse
import threading
from concurrent.futures import ThreadPoolExecutor

try:
    import certifi
    os.environ.setdefault("CURL_CA_BUNDLE", certifi.where())
    os.environ.setdefault("SSL_CERT_FILE", certifi.where())
except Exception:
    pass

os.environ.setdefault("AWS_NO_SIGN_REQUEST", "YES")
os.environ.setdefault("GDAL_HTTP_UNSAFESSL", "NO")
os.environ.setdefault("CPL_VSIL_CURL_ALLOWED_EXTENSIONS", ".tif")
os.environ.setdefault("GDAL_DISABLE_READDIR_ON_OPEN", "EMPTY_DIR")
os.environ.setdefault("VSI_CACHE", "TRUE")

try:
    import numpy as np
    import rasterio
    from rasterio.windows import from_bounds
    from rasterio.enums import Resampling
except ImportError as e:
    print("[ERRO] Faltam dependencias. Instale com:\n    pip install rasterio certifi numpy\n"
          f"Detalhe: {e}")
    sys.exit(1)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import geo_common  # noqa: E402

COP_HOST = "https://copernicus-dem-30m.s3.amazonaws.com"
RESOLUTION = 129  # mesmo grid padrao do Unity Terrain usado pelo runtime
OUTPUT_DIR = os.path.join(geo_common.STREAMING_DIR, "GeographicData")
HMAP_VERSION_REAL = 2

_print_lock = threading.Lock()
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass


def log(*a):
    with _print_lock:
        try:
            print(*a)
        except UnicodeEncodeError:
            print(" ".join(str(x).encode("ascii", "replace").decode("ascii") for x in a))
        sys.stdout.flush()


def cop_tile_url(lat_floor, lon_floor):
    ns = "N" if lat_floor >= 0 else "S"
    ew = "E" if lon_floor >= 0 else "W"
    name = f"Copernicus_DSM_COG_10_{ns}{abs(lat_floor):02d}_00_{ew}{abs(lon_floor):03d}_00_DEM"
    return f"/vsicurl/{COP_HOST}/{name}/{name}.tif"


def covering_tiles(min_lon, min_lat, max_lon, max_lat):
    """Cantos SW inteiros (lat, lon) dos tiles 1deg x 1deg que cobrem a bbox."""
    out = []
    lat = int(math.floor(min_lat))
    while lat <= int(math.floor(max_lat - 1e-9)):
        lon = int(math.floor(min_lon))
        while lon <= int(math.floor(max_lon - 1e-9)):
            out.append((lat, lon))
            lon += 1
        lat += 1
    return out


def read_region_dem(min_lon, min_lat, max_lon, max_lat, res):
    """DEM da bbox num grid res x res (float32 metros, norte no topo).
    Celulas fora de qualquer tile (oceano) ficam 0 (nivel do mar)."""
    out = np.zeros((res, res), dtype=np.float32)
    filled = np.zeros((res, res), dtype=bool)
    any_tile = False

    for (lat_f, lon_f) in covering_tiles(min_lon, min_lat, max_lon, max_lat):
        url = cop_tile_url(lat_f, lon_f)
        try:
            with rasterio.open(url) as ds:
                ix0 = max(min_lon, lon_f); iy0 = max(min_lat, lat_f)
                ix1 = min(max_lon, lon_f + 1); iy1 = min(max_lat, lat_f + 1)
                if ix1 <= ix0 or iy1 <= iy0:
                    continue
                win = from_bounds(ix0, iy0, ix1, iy1, ds.transform)
                gx0 = int(round((ix0 - min_lon) / (max_lon - min_lon) * res))
                gx1 = int(round((ix1 - min_lon) / (max_lon - min_lon) * res))
                gy0 = int(round((max_lat - iy1) / (max_lat - min_lat) * res))
                gy1 = int(round((max_lat - iy0) / (max_lat - min_lat) * res))
                gx0, gx1 = max(0, gx0), min(res, gx1)
                gy0, gy1 = max(0, gy0), min(res, gy1)
                sw, sh = gx1 - gx0, gy1 - gy0
                if sw <= 0 or sh <= 0:
                    continue
                sub = ds.read(1, window=win, out_shape=(sh, sw),
                              resampling=Resampling.bilinear).astype(np.float32)
                nd = ds.nodata
                if nd is not None:
                    sub = np.where(sub == nd, 0.0, sub)
                out[gy0:gy1, gx0:gx1] = sub
                filled[gy0:gy1, gx0:gx1] = True
                any_tile = True
        except Exception as ex:
            msg = str(ex)
            if "404" not in msg and "does not exist" not in msg.lower():
                log(f"      [aviso] tile {os.path.basename(url)[:40]}: {msg[:70]}")
            continue
    return out, filled, any_tile


# ---- leitura do ESA WorldCover ja gerado (para agua/costa) ----

def read_landcover_water(region_id):
    """Retorna (waterPercent 0..100, temDado) a partir de landcover_<id>.raw."""
    path = os.path.join(OUTPUT_DIR, f"landcover_{region_id}.raw")
    if not os.path.exists(path):
        return None, False
    try:
        b = open(path, "rb").read()
        if b[:4] != b"LCOV":
            return None, False
        header_size = struct.unpack_from("<H", b, 6)[0]
        res = struct.unpack_from("<H", b, 8)[0]
        data = np.frombuffer(b[header_size:header_size + res * res], dtype=np.uint8)
        if data.size == 0:
            return None, False
        water = float(np.mean(data == 80)) * 100.0  # classe 80 = agua
        return water, True
    except Exception:
        return None, False


# ---- escrita HMAP (mesmo layout do BatchGeographicDataProcessor, version=2) ----

def pack_str(s):
    b = (s or "").encode("utf-8")
    return struct.pack("<H", len(b)) + b


def write_hmap(region, dem, res):
    rid = region["id"]
    min_h = float(np.min(dem)); max_h = float(np.max(dem))
    range_h = max(1.0, max_h - min_h)

    water_pct, has_wc = read_landcover_water(rid)
    if water_pct is None:
        water_pct = float(region.get("waterPercent", 20))
    is_coastal = (min_h <= 2.0 and water_pct >= 10.0) or water_pct >= 40.0
    has_rivers = (not is_coastal) and (1.0 <= water_pct < 40.0)
    sea_level_norm = max(0.0, min(1.0, (0.0 - min_h) / range_h)) if min_h < 0 else 0.0

    # Normaliza alturas reais -> 16-bit.
    norm = np.clip((dem - min_h) / range_h, 0.0, 1.0)
    vals = (norm * 65535.0 + 0.5).astype(np.uint16)

    fixed = struct.pack("<4sHHHi ffff ?? h iii",
                        b"HMAP",
                        HMAP_VERSION_REAL,
                        0,                       # placeholder headerSize
                        res,
                        int(rid),
                        round(min_h, 1), round(max_h, 1), round(range_h, 1),
                        round(sea_level_norm, 4),
                        bool(is_coastal), bool(has_rivers),
                        int(round(water_pct)),
                        int(region.get("realWidthMeters", 0)),
                        int(region.get("realLengthMeters", 0)),
                        int(region.get("realAreaKm2", 0)))
    header = bytearray(fixed + pack_str(region.get("name", "")) + pack_str(region.get("country", "")))
    struct.pack_into("<H", header, 6, len(header))

    path = os.path.join(OUTPUT_DIR, f"heightmap_{rid}.raw")
    with open(path, "wb") as f:
        f.write(header)
        f.write(vals.tobytes(order="C"))
    return min_h, max_h, water_pct, is_coastal, has_wc


def already_real(region_id):
    path = os.path.join(OUTPUT_DIR, f"heightmap_{region_id}.raw")
    if not os.path.exists(path):
        return False
    try:
        with open(path, "rb") as f:
            head = f.read(6)
        return head[:4] == b"HMAP" and struct.unpack_from("<H", head, 4)[0] >= HMAP_VERSION_REAL
    except Exception:
        return False


def process_region(region, res, skip_existing=False):
    rid = region["id"]
    if skip_existing and already_real(rid):
        return True
    try:
        min_lon = float(region["minLon"]); max_lon = float(region["maxLon"])
        min_lat = float(region["minLat"]); max_lat = float(region["maxLat"])
    except (KeyError, ValueError):
        log(f"[skip] regiao {rid}: bbox invalida."); return False
    if max_lon <= min_lon or max_lat <= min_lat:
        log(f"[skip] regiao {rid}: bbox degenerada."); return False

    t0 = time.time()
    try:
        dem, filled, any_tile = read_region_dem(min_lon, min_lat, max_lon, max_lat, res)
    except Exception as ex:
        log(f"[ERRO] regiao {rid}: {str(ex)[:100]}"); return False
    if not any_tile:
        log(f"[skip] regiao {rid} ({region.get('name', '?')}): sem tile Copernicus (oceano).")
        return False

    min_h, max_h, water, coastal, has_wc = write_hmap(region, dem, res)
    log(f"[ok] {rid:>4} {region.get('name', '?')[:20]:20} "
        f"{min_h:5.0f}..{max_h:5.0f} m | agua {water:4.1f}%{'(WC)' if has_wc else '(db)'} "
        f"{'costa' if coastal else 'interior':8} {time.time() - t0:4.1f}s")
    return True


def main():
    ap = argparse.ArgumentParser(description="Relevo real por regiao (Copernicus GLO-30 DEM).")
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--id", type=int)
    g.add_argument("--ids", type=str)
    g.add_argument("--all", action="store_true")
    ap.add_argument("--res", type=int, default=RESOLUTION)
    ap.add_argument("--workers", type=int, default=8)
    ap.add_argument("--skip-existing", action="store_true",
                    help="Pula regioes que ja tem heightmap real (version>=2).")
    args = ap.parse_args()

    os.makedirs(OUTPUT_DIR, exist_ok=True)
    regions = {r["id"]: r for r in geo_common.load_regions()}
    if args.all:
        ids = sorted(regions.keys())
    elif args.ids:
        ids = [int(x) for x in args.ids.split(",") if x.strip()]
    else:
        ids = [args.id]

    log("=" * 60)
    log("[ProjectTerra] Relevo Real (Copernicus GLO-30 DEM 30m)")
    log(f"[INFO] Regioes: {len(ids)} | grid {args.res}x{args.res} | workers {args.workers}")
    log("=" * 60)

    jobs = [regions[i] for i in ids if i in regions]
    for i in ids:
        if i not in regions:
            log(f"[skip] regiao {i}: inexistente.")

    t_start = time.time()
    done = {"n": 0, "ok": 0}
    total = len(jobs)

    def run(region):
        try:
            ok = process_region(region, args.res, args.skip_existing)
        except Exception as ex:
            log(f"[ERRO] regiao {region.get('id')}: {str(ex)[:100]}"); ok = False
        with _print_lock:
            done["n"] += 1
            done["ok"] += 1 if ok else 0
            show = done["n"] % 100 == 0 or done["n"] == total
        if show:
            el = time.time() - t_start
            rate = done["n"] / el if el > 0 else 0
            eta = (total - done["n"]) / rate if rate > 0 else 0
            log(f"    --- progresso {done['n']}/{total} ({done['ok']} ok) "
                f"| {rate:.1f} reg/s | ETA {eta/60:.1f} min ---")
        return ok

    if args.workers > 1 and total > 1:
        with ThreadPoolExecutor(max_workers=args.workers) as ex:
            list(ex.map(run, jobs))
    else:
        for r in jobs:
            run(r)

    log("-" * 60)
    log(f"[FIM] {done['ok']}/{total} heightmaps reais em {(time.time() - t_start)/60:.1f} min")


if __name__ == "__main__":
    main()
