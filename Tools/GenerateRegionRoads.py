import os
import json
import struct

from geo_common import (
    STREAMING_DIR, CACHE_DIR, load_regions, load_id_map, latlon_to_xy, iter_lines,
)

# Classes de via (byte):
CLASS_MAJOR = 0       # Major Highway / Beltway / Bypass
CLASS_SECONDARY = 1   # Secondary Highway
CLASS_EXPRESSWAY = 2  # expressway == 1
CLASS_RAILROAD = 3    # ferrovia
CLASS_CONNECTOR = 4   # conector procedural (hibrido) ligando cidades

MAX_PTS = 48          # decimacao: teto de pontos por polilinha
MAJOR_ROAD_TYPES = {"Major Highway", "Beltway", "Bypass"}


def classify_road(props):
    """Retorna a classe da via, ou None se nao for rodovia principal/expressa."""
    if str(props.get("expressway")) == "1":
        return CLASS_EXPRESSWAY
    t = props.get("type")
    if t in MAJOR_ROAD_TYPES:
        return CLASS_MAJOR
    if t == "Secondary Highway":
        return CLASS_SECONDARY
    return None


def decimate(points):
    """Reduz a polilinha preservando extremos, com teto de MAX_PTS."""
    n = len(points)
    if n <= MAX_PTS:
        return points
    step = (n - 1) / (MAX_PTS - 1)
    out = [points[min(n - 1, int(round(i * step)))] for i in range(MAX_PTS)]
    return out


def split_line_by_region(line, id_map):
    """Quebra uma polilinha (lista [lon,lat]) em trechos contidos numa unica regiao.
    Retorna lista de (regionId, [(lat,lon), ...])."""
    runs = []
    current = None
    cur_rid = 0
    H, W = id_map.shape
    for lon, lat in line:
        x, y = latlon_to_xy(lon, lat)
        rid = int(id_map[y, x])
        if rid <= 0:
            if current is not None and len(current) >= 2:
                runs.append((cur_rid, current))
            current, cur_rid = None, 0
            continue
        if current is None:
            current, cur_rid = [(lat, lon)], rid
        elif rid == cur_rid:
            current.append((lat, lon))
        else:
            current.append((lat, lon))  # ponto de fronteira compartilhado
            if len(current) >= 2:
                runs.append((cur_rid, current))
            current, cur_rid = [(lat, lon)], rid
    if current is not None and len(current) >= 2:
        runs.append((cur_rid, current))
    return runs


def ingest(features, id_map, classify, fixed_class, by_region, counters):
    for feat in features:
        props = feat.get("properties", {})
        cls = fixed_class if fixed_class is not None else classify(props)
        if cls is None:
            continue
        for line in iter_lines(feat.get("geometry")):
            if len(line) < 2:
                continue
            for rid, pts in split_line_by_region(line, id_map):
                pts = decimate(pts)
                if len(pts) < 2:
                    continue
                by_region.setdefault(rid, []).append((cls, pts))
                counters[cls] = counters.get(cls, 0) + 1


def add_city_connectors(by_region, cities_by_region, counters=None):
    """Hibrido A+C: onde a malha real for esparsa (<2 trechos), liga as cidades
    principais em estrela a partir da maior, garantindo conectividade economica."""
    added = 0
    for rid, cities in cities_by_region.items():
        real = sum(1 for cls, _ in by_region.get(rid, []) if cls != CLASS_CONNECTOR)
        if real >= 2 or len(cities) < 2:
            continue
        hub = cities[0]
        for c in cities[1:6]:
            seg = [(hub["lat"], hub["lon"]), (c["lat"], c["lon"])]
            by_region.setdefault(rid, []).append((CLASS_CONNECTOR, seg))
            added += 1
    if counters is not None:
        counters[CLASS_CONNECTOR] = counters.get(CLASS_CONNECTOR, 0) + added
    print(f"[HYBRID] {added} conectores procedurais adicionados em regioes de dado esparso.")


def load_cities_for_connectors():
    path = os.path.join(STREAMING_DIR, "regions_cities.json")
    if not os.path.exists(path):
        print("[WARN] regions_cities.json ausente - rode GenerateRegionCities.py antes para os conectores.")
        return {}
    data = json.load(open(path, encoding="utf-8"))["regions"]
    return {r["regionId"]: r["cities"] for r in data}


def save_bin(by_region):
    """PTRO v1: header + tabela de indice (seek por regiao) + blocos de dados."""
    region_ids = sorted(by_region.keys())
    blocks = {}
    for rid in region_ids:
        b = bytearray()
        for cls, pts in by_region[rid]:
            b.extend(struct.pack("<BH", cls, len(pts)))
            for lat, lon in pts:
                b.extend(struct.pack("<ff", lat, lon))
        blocks[rid] = b

    header = bytearray()
    header.extend(b"PTRO")
    header.extend(struct.pack("<HI", 1, len(region_ids)))
    index_entry = struct.Struct("<iqH")  # regionId, offset(int64), polyCount(uint16)
    index_size = index_entry.size * len(region_ids)
    data_start = len(header) + index_size

    index = bytearray()
    data = bytearray()
    for rid in region_ids:
        offset = data_start + len(data)
        index.extend(index_entry.pack(rid, offset, len(by_region[rid])))
        data.extend(blocks[rid])

    path = os.path.join(STREAMING_DIR, "regions_roads.bin")
    with open(path, "wb") as f:
        f.write(header)
        f.write(index)
        f.write(data)
    print(f"[OUT] BIN  {path} ({len(header) + len(index) + len(data)} bytes)")


def build_roads():
    print("=== PROJECT TERRA: GERADOR DE RODOVIAS E FERROVIAS ===")
    id_map = load_id_map()
    print(f"[INFO] raster {id_map.shape} carregado.")

    roads = json.load(open(os.path.join(CACHE_DIR, "ne_10m_roads.geojson"), encoding="utf-8"))["features"]
    rails = json.load(open(os.path.join(CACHE_DIR, "ne_10m_railroads.geojson"), encoding="utf-8"))["features"]
    print(f"[INFO] {len(roads)} rodovias e {len(rails)} ferrovias no dataset.")

    by_region = {}
    counters = {}
    print("  -> Recortando rodovias principais por regiao...")
    ingest(roads, id_map, classify_road, None, by_region, counters)
    print("  -> Recortando ferrovias por regiao...")
    ingest(rails, id_map, None, CLASS_RAILROAD, by_region, counters)

    add_city_connectors(by_region, load_cities_for_connectors(), counters)

    save_bin(by_region)

    labels = {0: "Rodovia principal", 1: "Rodovia secundaria", 2: "Via expressa", 3: "Ferrovia", 4: "Conector"}
    total = sum(len(v) for v in by_region.values())
    print(f"\n=== VERIFICACAO ===")
    print(f"Regioes com malha: {len(by_region)} | Total de trechos: {total}")
    for cls in sorted(labels):
        print(f"  {labels[cls]}: {counters.get(cls, 0)} trechos")


if __name__ == "__main__":
    build_roads()
