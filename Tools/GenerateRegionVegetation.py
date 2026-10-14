"""
Gerador da camada de VEGETACAO/COBERTURA DO SOLO por regiao a partir de dados
reais de satelite (ESA WorldCover 10m 2021 v200, licenca CC BY 4.0).

Para cada regiao, le APENAS a janela correspondente a bounding-box no COG
(Cloud-Optimized GeoTIFF) hospedado no AWS S3 Open Data, via HTTP range
requests (/vsicurl) — baixa alguns MB em vez do tile inteiro de ~156 MB.
O resultado e reamostrado (nearest, preservando as classes) para um grid
quadrado e gravado em:

    Assets/StreamingAssets/GeographicData/landcover_<id>.raw

Formato binario "LCOV" (little-endian):
    4s  magic        = b'LCOV'
    H   version      = 1
    H   headerSize   (preenchido no fim)
    H   resolution   (res; grid res x res)
    i   regionId
    H   year         = 2021
    H   srcLen + src (UTF-8, ex.: "ESA_WorldCover_10m_2021_v200")
    ... res*res bytes uint8: codigo de classe WorldCover por celula.

Grid gravado em orientacao NORTE-no-topo (linha 0 = maxLat), igual ao raster
original. O runtime (VegetationSpawner) faz o flip de Z ao plantar.

Classes WorldCover:
    10 Tree cover | 20 Shrubland | 30 Grassland | 40 Cropland | 50 Built-up
    60 Bare/sparse | 70 Snow/ice | 80 Water | 90 Herb. wetland
    95 Mangroves | 100 Moss/lichen | 0 NoData

Uso:
    python Tools/GenerateRegionVegetation.py --id 1
    python Tools/GenerateRegionVegetation.py --ids 1,2,3 --res 128
    python Tools/GenerateRegionVegetation.py --all        # todas as 4838 (pesado)

Dependencias: rasterio, certifi, numpy  ->  pip install rasterio certifi numpy
"""

import os
import sys
import math
import time
import struct
import argparse
import threading
from concurrent.futures import ThreadPoolExecutor, as_completed

# --- SSL / GDAL remoto: aponta o GDAL para o bundle de certificados do certifi
# (o store padrao do Windows costuma falhar para o S3 da ESA). ---
try:
    import certifi
    os.environ.setdefault("CURL_CA_BUNDLE", certifi.where())
    os.environ.setdefault("SSL_CERT_FILE", certifi.where())
except Exception:
    pass

# Otimizacoes de leitura remota de COG.
os.environ.setdefault("GDAL_HTTP_UNSAFESSL", "NO")
os.environ.setdefault("CPL_VSIL_CURL_ALLOWED_EXTENSIONS", ".tif")
os.environ.setdefault("GDAL_DISABLE_READDIR_ON_OPEN", "EMPTY_DIR")
os.environ.setdefault("VSI_CACHE", "TRUE")
os.environ.setdefault("GDAL_HTTP_MAX_RETRY", "3")
os.environ.setdefault("GDAL_HTTP_RETRY_DELAY", "1")

try:
    import numpy as np
    import rasterio
    from rasterio.windows import from_bounds
    from rasterio.enums import Resampling
except ImportError as e:
    print("[ERRO] Faltam dependencias. Instale com:\n"
          "    pip install rasterio certifi numpy\n"
          f"Detalhe: {e}")
    sys.exit(1)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import geo_common  # noqa: E402

# ESA WorldCover 2021 v200 — COGs de 3deg x 3deg no AWS Open Data.
WC_BASE = ("https://esa-worldcover.s3.eu-central-1.amazonaws.com/"
           "v200/2021/map/ESA_WorldCover_10m_2021_v200_{tile}_Map.tif")
WC_SOURCE_TAG = "ESA_WorldCover_10m_2021_v200"
TILE_STEP = 3  # graus por tile

OUTPUT_DIR = os.path.join(geo_common.STREAMING_DIR, "GeographicData")

_print_lock = threading.Lock()

# Console do Windows (cp1252) nao encoda nomes com Latin Extended (ex.: 'ŏ').
# Forca UTF-8 com replace para que um nome "exotico" nunca derrube um worker.
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass


def log(*args):
    with _print_lock:
        try:
            print(*args)
        except UnicodeEncodeError:
            print(" ".join(str(a).encode("ascii", "replace").decode("ascii") for a in args))
        sys.stdout.flush()


def tile_name(lat_floor, lon_floor):
    """SW-corner -> nome do tile WorldCover, ex. (-21, 27) -> 'S21E027'."""
    ns = "N" if lat_floor >= 0 else "S"
    ew = "E" if lon_floor >= 0 else "W"
    return f"{ns}{abs(lat_floor):02d}{ew}{abs(lon_floor):03d}"


def covering_tiles(min_lon, min_lat, max_lon, max_lat):
    """Conjunto de cantos SW (lat_floor, lon_floor) dos tiles que cobrem a bbox."""
    lat0 = int(math.floor(min_lat / TILE_STEP)) * TILE_STEP
    lat1 = int(math.floor((max_lat - 1e-9) / TILE_STEP)) * TILE_STEP
    lon0 = int(math.floor(min_lon / TILE_STEP)) * TILE_STEP
    lon1 = int(math.floor((max_lon - 1e-9) / TILE_STEP)) * TILE_STEP
    tiles = []
    lat = lat0
    while lat <= lat1:
        lon = lon0
        while lon <= lon1:
            tiles.append((lat, lon))
            lon += TILE_STEP
        lat += TILE_STEP
    return tiles


def read_region_landcover(min_lon, min_lat, max_lon, max_lat, res):
    """Le a cobertura do solo da bbox num grid res x res (uint8, norte no topo).
    Celulas sem cobertura (fora de qualquer tile/oceano) ficam 0 (NoData)."""
    out = np.zeros((res, res), dtype=np.uint8)
    tiles = covering_tiles(min_lon, min_lat, max_lon, max_lat)
    if not tiles:
        return out, []

    used = []
    for (lat_f, lon_f) in tiles:
        name = tile_name(lat_f, lon_f)
        url = "/vsicurl/" + WC_BASE.format(tile=name)
        try:
            with rasterio.open(url) as ds:
                # Interseccao da bbox da regiao com a bbox do tile.
                t_min_lon, t_min_lat = lon_f, lat_f
                t_max_lon, t_max_lat = lon_f + TILE_STEP, lat_f + TILE_STEP
                ix0 = max(min_lon, t_min_lon)
                iy0 = max(min_lat, t_min_lat)
                ix1 = min(max_lon, t_max_lon)
                iy1 = min(max_lat, t_max_lat)
                if ix1 <= ix0 or iy1 <= iy0:
                    continue

                win = from_bounds(ix0, iy0, ix1, iy1, ds.transform)
                # Fracao do grid de saida coberta por esta interseccao.
                gx0 = int(round((ix0 - min_lon) / (max_lon - min_lon) * res))
                gx1 = int(round((ix1 - min_lon) / (max_lon - min_lon) * res))
                # Y invertido: norte (maxLat) = linha 0.
                gy0 = int(round((max_lat - iy1) / (max_lat - min_lat) * res))
                gy1 = int(round((max_lat - iy0) / (max_lat - min_lat) * res))
                gx0, gx1 = max(0, gx0), min(res, gx1)
                gy0, gy1 = max(0, gy0), min(res, gy1)
                sub_w, sub_h = gx1 - gx0, gy1 - gy0
                if sub_w <= 0 or sub_h <= 0:
                    continue

                sub = ds.read(1, window=win, out_shape=(sub_h, sub_w),
                              resampling=Resampling.nearest)
                out[gy0:gy1, gx0:gx1] = sub.astype(np.uint8)
                used.append(name)
        except Exception as ex:
            log(f"      [aviso] tile {name} indisponivel: {str(ex)[:80]}")
            continue
    return out, used


def pack_str(s):
    b = (s or "").encode("utf-8")
    return struct.pack("<H", len(b)) + b


def write_landcover_raw(region_id, grid, year=2021):
    res = grid.shape[0]
    fixed = struct.pack("<4sHHHiH",
                        b"LCOV",
                        1,          # version
                        0,          # placeholder headerSize
                        res,
                        int(region_id),
                        int(year))
    header = bytearray(fixed + pack_str(WC_SOURCE_TAG))
    struct.pack_into("<H", header, 6, len(header))  # headerSize no offset 6

    path = os.path.join(OUTPUT_DIR, f"landcover_{region_id}.raw")
    with open(path, "wb") as f:
        f.write(header)
        f.write(grid.astype(np.uint8).tobytes(order="C"))
    return path


CLASS_NAMES = {10: "Tree", 20: "Shrub", 30: "Grass", 40: "Crop", 50: "Built",
               60: "Bare", 70: "Snow", 80: "Water", 90: "Wetland",
               95: "Mangrove", 100: "Moss", 0: "NoData"}


def summarize(grid):
    vals, cnt = np.unique(grid, return_counts=True)
    tot = cnt.sum() or 1
    parts = sorted(zip(vals.tolist(), cnt.tolist()), key=lambda z: -z[1])
    return ", ".join(f"{CLASS_NAMES.get(v, v)} {100 * c / tot:.0f}%"
                     for v, c in parts[:5])


def process_region(region, res, skip_existing=False):
    rid = region["id"]
    if skip_existing and os.path.exists(os.path.join(OUTPUT_DIR, f"landcover_{rid}.raw")):
        return True
    try:
        min_lon = float(region["minLon"]); max_lon = float(region["maxLon"])
        min_lat = float(region["minLat"]); max_lat = float(region["maxLat"])
    except (KeyError, ValueError):
        log(f"[skip] regiao {rid}: sem bounding-box valida.")
        return False
    if max_lon <= min_lon or max_lat <= min_lat:
        log(f"[skip] regiao {rid}: bbox degenerada.")
        return False

    t0 = time.time()
    try:
        grid, used = read_region_landcover(min_lon, min_lat, max_lon, max_lat, res)
    except Exception as ex:
        log(f"[ERRO] regiao {rid} ({region.get('name', '?')}): {str(ex)[:100]}")
        return False
    if not used:
        log(f"[skip] regiao {rid} ({region.get('name', '?')}): "
            f"nenhum tile WorldCover cobre a bbox (provavel oceano).")
        return False
    path = write_landcover_raw(rid, grid)
    log(f"[ok] {rid:>4} {region.get('name', '?')[:22]:22} "
        f"tiles={len(used):2} {res}x{res} {time.time() - t0:4.1f}s | {summarize(grid)}")
    return True


def main():
    ap = argparse.ArgumentParser(description="Gera landcover_<id>.raw via ESA WorldCover.")
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--id", type=int, help="ID de uma regiao.")
    g.add_argument("--ids", type=str, help="IDs separados por virgula: 1,2,3")
    g.add_argument("--all", action="store_true", help="Todas as regioes (pesado).")
    ap.add_argument("--res", type=int, default=128, help="Resolucao do grid (padrao 128).")
    ap.add_argument("--workers", type=int, default=8, help="Leituras paralelas (padrao 8).")
    ap.add_argument("--skip-existing", action="store_true",
                    help="Pula regioes que ja tem landcover_<id>.raw (retomar lote).")
    args = ap.parse_args()

    os.makedirs(OUTPUT_DIR, exist_ok=True)
    regions = {r["id"]: r for r in geo_common.load_regions()}

    if args.all:
        target_ids = sorted(regions.keys())
    elif args.ids:
        target_ids = [int(x) for x in args.ids.split(",") if x.strip()]
    else:
        target_ids = [args.id]

    log("=" * 60)
    log("[ProjectTerra] Camada de Vegetacao (ESA WorldCover 10m 2021)")
    log(f"[INFO] Regioes: {len(target_ids)} | grid {args.res}x{args.res} | "
        f"workers {args.workers} | destino {os.path.relpath(OUTPUT_DIR)}")
    log("=" * 60)

    jobs = [regions[rid] for rid in target_ids if rid in regions]
    for rid in target_ids:
        if rid not in regions:
            log(f"[skip] regiao {rid}: inexistente no banco.")

    t_start = time.time()
    done = {"ok": 0, "n": 0}
    total = len(jobs)

    def run(region):
        try:
            res_ok = process_region(region, args.res, args.skip_existing)
        except Exception as ex:
            log(f"[ERRO] regiao {region.get('id')}: {str(ex)[:100]}")
            res_ok = False
        with _print_lock:
            done["n"] += 1
            if res_ok:
                done["ok"] += 1
            show = done["n"] % 100 == 0 or done["n"] == total
        if show:
            el = time.time() - t_start
            rate = done["n"] / el if el > 0 else 0
            eta = (total - done["n"]) / rate if rate > 0 else 0
            log(f"    --- progresso {done['n']}/{total} ({done['ok']} gravadas) "
                f"| {rate:.1f} reg/s | ETA {eta/60:.1f} min ---")
        return res_ok

    if args.workers > 1 and total > 1:
        with ThreadPoolExecutor(max_workers=args.workers) as ex:
            list(ex.map(run, jobs))
    else:
        for r in jobs:
            run(r)

    log("-" * 60)
    log(f"[FIM] {done['ok']}/{total} regioes gravadas em {os.path.relpath(OUTPUT_DIR)} "
        f"({(time.time() - t_start)/60:.1f} min)")


if __name__ == "__main__":
    main()
