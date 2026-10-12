"""
Adiciona LAGOS (type byte = 5) ao dataset de marcos geográficos landmarks.bin.

Fonte: Natural Earth 10m physical lakes (ne_10m_lakes.geojson) — mesma origem já
usada pelo pipeline de regiões (build_borders_and_database.py). Leve (~5 MB).

O landmarks.bin é o dataset mundial lido por LandmarkManager.cs em runtime.
Formato (little-endian):
    magic  : 4 chars  "LMK1"
    count  : int32
    record : float lat, float lon, float height, byte type, uint16 nameLen, nameLen bytes (UTF-8)
    type   : 0=mountain 1=volcano 2=plateau 3=waterfall 4=river 5=lake

Para lagos, o campo `height` carrega o RAIO aproximado do lago em metros — usado
tanto para ordenar por proeminência quanto para dimensionar o corpo d'água na cena.

Idempotente: remove quaisquer lagos (type==5) já presentes antes de reanexar,
então rodar duas vezes não duplica.
"""
import os
import json
import math
import struct
import urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CACHE_DIR = os.path.join(ROOT, "Temp", "GeoCache")
STREAMING_DIR = os.path.join(ROOT, "Assets", "StreamingAssets")
LANDMARKS_BIN = os.path.join(STREAMING_DIR, "landmarks.bin")
LAKES_URL = "https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_10m_lakes.geojson"

LAKE_TYPE = 5
MIN_RADIUS_M = 400.0
MAX_RADIUS_M = 6000.0


def download_lakes():
    os.makedirs(CACHE_DIR, exist_ok=True)
    dest = os.path.join(CACHE_DIR, "ne_10m_lakes.geojson")
    if os.path.exists(dest) and os.path.getsize(dest) > 1000:
        print(f"[CACHE] {dest} ({os.path.getsize(dest)} bytes)")
        return dest
    print(f"[DOWNLOAD] {LAKES_URL}")
    req = urllib.request.Request(LAKES_URL, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=60) as r:
        open(dest, "wb").write(r.read())
    print(f"[DONE] {dest} ({os.path.getsize(dest)} bytes)")
    return dest


def rings_of(geom):
    """Retorna as listas de anéis externos (lon,lat) de Polygon/MultiPolygon."""
    t = geom.get("type")
    coords = geom.get("coordinates", [])
    if t == "Polygon":
        return [coords[0]] if coords else []
    if t == "MultiPolygon":
        return [poly[0] for poly in coords if poly]
    return []


def ring_centroid_and_area_km2(ring):
    """Centroide (lon,lat) e área aproximada em km² de um anel (shoelace em graus)."""
    n = len(ring)
    if n < 3:
        return None, 0.0
    a2 = 0.0  # 2*area (graus²)
    cx = 0.0
    cy = 0.0
    for i in range(n):
        x0, y0 = ring[i][0], ring[i][1]
        x1, y1 = ring[(i + 1) % n][0], ring[(i + 1) % n][1]
        cross = x0 * y1 - x1 * y0
        a2 += cross
        cx += (x0 + x1) * cross
        cy += (y0 + y1) * cross
    if abs(a2) < 1e-12:
        # degenera: usa média simples
        mx = sum(p[0] for p in ring) / n
        my = sum(p[1] for p in ring) / n
        return (mx, my), 0.0
    area_deg2 = abs(a2) / 2.0
    cx /= (3.0 * a2)
    cy /= (3.0 * a2)
    lat = cy
    km_per_deg = 111.32
    area_km2 = area_deg2 * (km_per_deg ** 2) * max(0.05, math.cos(math.radians(lat)))
    return (cx, cy), area_km2


def best_name(props):
    for k in ("name_pt", "name", "name_en"):
        v = props.get(k)
        if v:
            return str(v)
    return None


def collect_lakes(path):
    feats = json.load(open(path, encoding="utf-8"))["features"]
    lakes = []
    for f in feats:
        props = f.get("properties") or {}
        name = best_name(props)
        if not name:
            continue  # pula lagos sem nome (rótulo vazio polui a cena)
        geom = f.get("geometry") or {}
        # escolhe o maior anel (corpo principal do lago)
        best_ring = None
        best_area = -1.0
        best_c = None
        for ring in rings_of(geom):
            c, area = ring_centroid_and_area_km2(ring)
            if c and area > best_area:
                best_area, best_ring, best_c = area, ring, c
        if best_c is None:
            continue
        lon, lat = best_c
        radius_m = math.sqrt(max(best_area, 0.01) / math.pi) * 1000.0
        radius_m = max(MIN_RADIUS_M, min(MAX_RADIUS_M, radius_m))
        lakes.append((float(lat), float(lon), float(radius_m), name))
    # dedup por nome+posição aproximada
    seen = set()
    out = []
    for lat, lon, r, nm in lakes:
        key = (nm, round(lat, 2), round(lon, 2))
        if key in seen:
            continue
        seen.add(key)
        out.append((lat, lon, r, nm))
    return out


def read_landmarks():
    with open(LANDMARKS_BIN, "rb") as f:
        magic = f.read(4)
        if magic != b"LMK1":
            raise SystemExit(f"magic inválido em landmarks.bin: {magic!r}")
        count = struct.unpack("<i", f.read(4))[0]
        recs = []
        for _ in range(count):
            lat, lon, h = struct.unpack("<3f", f.read(12))
            tp = f.read(1)[0]
            nl = struct.unpack("<H", f.read(2))[0]
            nm = f.read(nl)
            recs.append((lat, lon, h, tp, nm))
    return recs


def write_landmarks(recs):
    out = bytearray()
    out += b"LMK1"
    out += struct.pack("<i", len(recs))
    for lat, lon, h, tp, nm in recs:
        out += struct.pack("<3f", lat, lon, h)
        out += struct.pack("<B", tp)
        out += struct.pack("<H", len(nm))
        out += nm
    with open(LANDMARKS_BIN, "wb") as f:
        f.write(out)


def main():
    print("=== ADICIONAR LAGOS AO landmarks.bin ===")
    lakes_path = download_lakes()
    lakes = collect_lakes(lakes_path)
    print(f"[LAKES] {len(lakes)} lagos nomeados extraídos.")

    recs = read_landmarks()
    before = len(recs)
    # idempotente: remove lagos já existentes (type==5)
    recs = [r for r in recs if r[3] != LAKE_TYPE]
    removed = before - len(recs)
    if removed:
        print(f"[CLEAN] Removidos {removed} lagos pré-existentes (reexecução).")

    for lat, lon, r, nm in lakes:
        recs.append((lat, lon, r, LAKE_TYPE, nm.encode("utf-8")))

    write_landmarks(recs)
    print(f"[DONE] landmarks.bin: {before} -> {len(recs)} marcos (+{len(lakes)} lagos). "
          f"{os.path.getsize(LANDMARKS_BIN)} bytes.")


if __name__ == "__main__":
    main()
