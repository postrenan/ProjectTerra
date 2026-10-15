"""
Refino da cobertura vegetal de regioes ESPECIFICAS com dados do OpenStreetMap.

O ESA WorldCover (10m) ja da a base global, mas e generalizado: o OSM traz
limites de bosque/floresta mais precisos e, principalmente, as areas verdes
urbanas (parques, jardins) que o WorldCover marca como "construido" sem arvores.

Para cada regiao, consulta o Overpass por poligonos de vegetacao na bounding-box
e sobrepoe no grid de landcover_<id>.raw (so onde o OSM tem dado; o resto
permanece WorldCover). Nao muda o runtime — apenas melhora o dataset, que o
VegetationSpawner ja consome.

Mapeamento OSM -> classe WorldCover usada pelo jogo:
    natural=wood, landuse=forest            -> 10 (arvores, denso)
    natural=scrub                           -> 20 (arbusto)
    leisure=park|garden, landuse=grass|
      meadow|recreation_ground              -> 30 (campo/verde urbano)

Uso (poucas regioes por vez — o Overpass e limitado):
    python Tools/RefineVegetationOSM.py --ids 3980,120,109
    python Tools/RefineVegetationOSM.py --id 3980

Dependencias: apenas stdlib + certifi (numpy opcional).
"""

import os
import sys
import json
import time
import struct
import argparse
import ssl
import urllib.request
import urllib.parse

import numpy as np

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace", line_buffering=True)
except Exception:
    pass

try:
    import certifi
    _SSL = ssl.create_default_context(cafile=certifi.where())
except Exception:
    _SSL = ssl.create_default_context()

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import geo_common  # noqa: E402

OUTPUT_DIR = os.path.join(geo_common.STREAMING_DIR, "GeographicData")

OVERPASS_ENDPOINTS = [
    "https://overpass-api.de/api/interpreter",
    "https://overpass.kumi.systems/api/interpreter",
    "https://overpass.private.coffee/api/interpreter",
]

# Tag OSM -> classe. A ordem define a prioridade (floresta sobre parque).
TAG_TO_CLASS = [
    (("natural", "wood"), 10),
    (("landuse", "forest"), 10),
    (("natural", "scrub"), 20),
    (("leisure", "park"), 30),
    (("leisure", "garden"), 30),
    (("landuse", "recreation_ground"), 30),
    (("landuse", "meadow"), 30),
    (("landuse", "grass"), 30),
]
CLASS_PRIORITY = {10: 3, 20: 2, 30: 1}  # maior vence


def overpass_query(s, w, n, e):
    parts = []
    for (k, v), _cls in TAG_TO_CLASS:
        parts.append(f'way["{k}"="{v}"]({s},{w},{n},{e});')
        parts.append(f'relation["{k}"="{v}"]({s},{w},{n},{e});')
    return f'[out:json][timeout:50];({"".join(parts)});out geom;'


def fetch_overpass(s, w, n, e, retries=3):
    q = overpass_query(s, w, n, e)
    data = urllib.parse.urlencode({"data": q}).encode()
    for attempt in range(retries):
        for ep in OVERPASS_ENDPOINTS:
            try:
                req = urllib.request.Request(ep, data=data,
                                             headers={"User-Agent": "ProjectTerra-veg/1.0"})
                with urllib.request.urlopen(req, timeout=90, context=_SSL) as r:
                    return json.loads(r.read().decode("utf-8"))
            except Exception as ex:
                print(f"      .. {ep.split('//')[1].split('/')[0]} {type(ex).__name__} {str(ex)[:50]}")
                continue
        time.sleep(4 * (attempt + 1))
    return None


def class_for_tags(tags):
    best, best_pri = 0, 0
    for (k, v), cls in TAG_TO_CLASS:
        if tags.get(k) == v and CLASS_PRIORITY[cls] > best_pri:
            best, best_pri = cls, CLASS_PRIORITY[cls]
    return best


def polygons_from_elements(elements):
    """Lista de (cls, minlat, minlon, maxlat, maxlon, pts[(lon,lat)])."""
    polys = []

    def add_geom(geom, cls):
        if not geom or len(geom) < 3:
            return
        pts = [(p["lon"], p["lat"]) for p in geom]
        lons = [p[0] for p in pts]; lats = [p[1] for p in pts]
        polys.append((cls, min(lats), min(lons), max(lats), max(lons), pts))

    for el in elements:
        cls = class_for_tags(el.get("tags", {}))
        if cls == 0:
            continue
        if el.get("type") == "way":
            add_geom(el.get("geometry"), cls)
        elif el.get("type") == "relation":
            for m in el.get("members", []):
                if m.get("type") == "way" and m.get("role") in ("outer", "", None):
                    add_geom(m.get("geometry"), cls)
    return polys


def rasterize_poly(pts, lon_grid, lat_grid):
    """Mascara booleana (mesma shape dos grids) dos pontos dentro do poligono,
    pela regra even-odd, vetorizada em numpy."""
    xs = np.asarray([p[0] for p in pts], dtype=np.float64)
    ys = np.asarray([p[1] for p in pts], dtype=np.float64)
    inside = np.zeros(lon_grid.shape, dtype=bool)
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi, xj, yj = xs[i], ys[i], xs[j], ys[j]
        cond = ((yi > lat_grid) != (yj > lat_grid))
        dy = (yj - yi) or 1e-12
        xint = (xj - xi) * (lat_grid - yi) / dy + xi
        inside ^= cond & (lon_grid < xint)
        j = i
    return inside


def read_landcover(region_id):
    path = os.path.join(OUTPUT_DIR, f"landcover_{region_id}.raw")
    if not os.path.exists(path):
        return None, None, None
    b = bytearray(open(path, "rb").read())
    if b[:4] != b"LCOV":
        return None, None, None
    header_size = struct.unpack_from("<H", b, 6)[0]
    res = struct.unpack_from("<H", b, 8)[0]
    return b, header_size, res


def refine_region(region, pause):
    rid = region["id"]
    b, header_size, res = read_landcover(rid)
    if b is None:
        print(f"[skip] regiao {rid}: sem landcover base (rode GenerateRegionVegetation primeiro).")
        return False

    s, w = float(region["minLat"]), float(region["minLon"])
    n, e = float(region["maxLat"]), float(region["maxLon"])

    t0 = time.time()
    j = fetch_overpass(s, w, n, e)
    if j is None:
        print(f"[ERRO] regiao {rid} ({region.get('name','?')}): Overpass indisponivel.")
        return False
    polys = polygons_from_elements(j.get("elements", []))
    if not polys:
        print(f"[ok]  {rid:>4} {region.get('name','?')[:20]:20} 0 poligonos OSM — nada a refinar ({time.time()-t0:.1f}s)")
        time.sleep(pause)
        return False

    span_lat = (n - s) or 1e-9
    span_lon = (e - w) or 1e-9
    lons = w + (np.arange(res) + 0.5) / res * span_lon            # oeste->leste
    lats = n - (np.arange(res) + 0.5) / res * span_lat            # norte->sul (linha 0 = norte)
    lon_grid, lat_grid = np.meshgrid(lons, lats)                  # shape (res,res)

    base = np.frombuffer(bytes(b[header_size:header_size + res * res]), dtype=np.uint8).reshape(res, res).copy()
    out = base.copy()
    pri = np.zeros((res, res), dtype=np.uint8)

    # Processa em ordem crescente de prioridade; floresta (maior) sobrescreve por ultimo.
    for (cls, plo, pla, phi, pha, pts) in sorted(polys, key=lambda p: CLASS_PRIORITY[p[0]]):
        # Sub-janela do bbox do poligono (margem de 1 celula).
        gx0 = max(0, int((pla - w) / span_lon * res) - 1)
        gx1 = min(res, int((pha - w) / span_lon * res) + 2)
        gy0 = max(0, int((n - phi) / span_lat * res) - 1)
        gy1 = min(res, int((n - plo) / span_lat * res) + 2)
        if gx1 <= gx0 or gy1 <= gy0:
            continue
        sub_lon = lon_grid[gy0:gy1, gx0:gx1]
        sub_lat = lat_grid[gy0:gy1, gx0:gx1]
        mask = rasterize_poly(pts, sub_lon, sub_lat)
        if not mask.any():
            continue
        cp = CLASS_PRIORITY[cls]
        apply = mask & (pri[gy0:gy1, gx0:gx1] < cp)
        out[gy0:gy1, gx0:gx1][apply] = cls
        pri[gy0:gy1, gx0:gx1][apply] = cp

    changed = int(np.count_nonzero(out != base))
    b[header_size:header_size + res * res] = out.tobytes(order="C")
    with open(os.path.join(OUTPUT_DIR, f"landcover_{rid}.raw"), "wb") as f:
        f.write(b)

    print(f"[ok]  {rid:>4} {region.get('name','?')[:20]:20} {len(polys):4} poligonos OSM | "
          f"{changed} celulas refinadas ({time.time()-t0:.1f}s)")
    time.sleep(pause)
    return True


def main():
    ap = argparse.ArgumentParser(description="Refina landcover_<id>.raw com vegetacao do OpenStreetMap.")
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--id", type=int)
    g.add_argument("--ids", type=str)
    ap.add_argument("--pause", type=float, default=2.0,
                    help="Pausa entre regioes em s (gentileza com o Overpass).")
    args = ap.parse_args()

    regions = {r["id"]: r for r in geo_common.load_regions()}
    ids = [int(x) for x in args.ids.split(",")] if args.ids else [args.id]

    print("=" * 60)
    print("[ProjectTerra] Refino de Vegetacao via OpenStreetMap (Overpass)")
    print(f"[INFO] Regioes: {len(ids)}")
    print("=" * 60)

    ok = 0
    for i in ids:
        r = regions.get(i)
        if r is None:
            print(f"[skip] regiao {i}: inexistente.")
            continue
        if refine_region(r, args.pause):
            ok += 1
    print("-" * 60)
    print(f"[FIM] {ok}/{len(ids)} regioes refinadas com OSM.")


if __name__ == "__main__":
    main()
