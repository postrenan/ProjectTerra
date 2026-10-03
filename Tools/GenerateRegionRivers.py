import os
import json
import struct

from geo_common import STREAMING_DIR, CACHE_DIR, load_id_map, iter_lines
from GenerateRegionRoads import split_line_by_region, decimate


def river_width(scalerank):
    """Largura visual do rio em metros a partir do scalerank (0 = grande, 10 = pequeno)."""
    try:
        sr = float(scalerank)
    except (TypeError, ValueError):
        sr = 8.0
    return max(10.0, 90.0 - sr * 8.0)


def build_rivers():
    print("=== PROJECT TERRA: GERADOR DE RIOS ===")
    id_map = load_id_map()
    feats = json.load(open(os.path.join(CACHE_DIR, "ne_10m_rivers_lake_centerlines.geojson"), encoding="utf-8"))["features"]
    print(f"[INFO] {len(feats)} rios no dataset.")

    by_region = {}  # rid -> list of (width, [(lat,lon),...])
    for feat in feats:
        w = river_width(feat.get("properties", {}).get("scalerank"))
        for line in iter_lines(feat.get("geometry")):
            if len(line) < 2:
                continue
            for rid, pts in split_line_by_region(line, id_map):
                pts = decimate(pts)
                if len(pts) < 2:
                    continue
                by_region.setdefault(rid, []).append((w, pts))

    save_bin(by_region)

    total = sum(len(v) for v in by_region.values())
    print(f"\n=== VERIFICACAO ===")
    print(f"Regioes com rios: {len(by_region)} | Total de trechos: {total}")
    if 1980 in by_region:
        print(f"SP(1980): {len(by_region[1980])} trechos de rio")


def save_bin(by_region):
    """PTRV v1: header + indice (seek por regiao) + blocos {width, ptCount, pts(lat,lon)}."""
    region_ids = sorted(by_region.keys())
    blocks = {}
    for rid in region_ids:
        b = bytearray()
        for w, pts in by_region[rid]:
            b.extend(struct.pack("<fH", w, len(pts)))
            for lat, lon in pts:
                b.extend(struct.pack("<ff", lat, lon))
        blocks[rid] = b

    header = bytearray()
    header.extend(b"PTRV")
    header.extend(struct.pack("<HI", 1, len(region_ids)))
    index_entry = struct.Struct("<iqH")
    data_start = len(header) + index_entry.size * len(region_ids)

    index = bytearray()
    data = bytearray()
    for rid in region_ids:
        offset = data_start + len(data)
        index.extend(index_entry.pack(rid, offset, len(by_region[rid])))
        data.extend(blocks[rid])

    path = os.path.join(STREAMING_DIR, "regions_rivers.bin")
    with open(path, "wb") as f:
        f.write(header)
        f.write(index)
        f.write(data)
    print(f"[OUT] BIN  {path} ({len(header) + len(index) + len(data)} bytes)")


if __name__ == "__main__":
    build_rivers()
