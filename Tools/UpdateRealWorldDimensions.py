import json
import os
import struct
import math
import time

DB_BIN_PATH = os.path.join("Assets", "StreamingAssets", "regions_database.bin")
DB_JSON_PATH = os.path.join("Assets", "StreamingAssets", "regions_database.json")
BIN_MAP_PATH = os.path.join("Assets", "StreamingAssets", "region_id_map.bin")
GEO_DIR = os.path.join("Assets", "StreamingAssets", "GeographicData")
W, H = 4096, 2048

def load_regions():
    if os.path.exists(DB_BIN_PATH):
        regions = []
        with open(DB_BIN_PATH, "rb") as f:
            magic = f.read(4)
            if magic != b'PTRD':
                raise ValueError("Magic invalido em regions_database.bin")
            version, count = struct.unpack("<HI", f.read(6))
            for _ in range(count):
                rid = struct.unpack("<i", f.read(4))[0]
                def r_str():
                    l = struct.unpack("<H", f.read(2))[0]
                    return f.read(l).decode('utf-8')
                name = r_str()
                country = r_str()
                rtype = r_str()
                cLat, cLon, minLat, maxLat, minLon, maxLon, fPct, mPct, aPct, wPct, rw, rl, ra = struct.unpack("<ffffffiiiiiii", f.read(52))
                regions.append({
                    "id": rid, "name": name, "country": country, "type": rtype,
                    "centerLat": cLat, "centerLon": cLon, "minLat": minLat, "maxLat": maxLat,
                    "minLon": minLon, "maxLon": maxLon, "forestPercent": fPct,
                    "mineralsPercent": mPct, "arablePercent": aPct, "waterPercent": wPct,
                    "realWidthMeters": rw, "realLengthMeters": rl, "realAreaKm2": ra
                })
        return regions
    elif os.path.exists(DB_JSON_PATH):
        with open(DB_JSON_PATH, "r", encoding="utf-8") as f:
            return json.load(f).get("regions", [])
    return []

def save_regions_binary(regions):
    out = bytearray()
    out.extend(b'PTRD')
    out.extend(struct.pack('<HI', 1, len(regions)))
    def pack_str(s):
        b = (s or '').encode('utf-8')
        return struct.pack('<H', len(b)) + b

    for r in regions:
        out.extend(struct.pack('<i', r.get('id', 0)))
        out.extend(pack_str(r.get('name', '')))
        out.extend(pack_str(r.get('country', '')))
        out.extend(pack_str(r.get('type', '')))
        out.extend(struct.pack('<ffffffiiiiiii',
            float(r.get('centerLat', 0.0)),
            float(r.get('centerLon', 0.0)),
            float(r.get('minLat', 0.0)),
            float(r.get('maxLat', 0.0)),
            float(r.get('minLon', 0.0)),
            float(r.get('maxLon', 0.0)),
            int(r.get('forestPercent', 0)),
            int(r.get('mineralsPercent', 0)),
            int(r.get('arablePercent', 0)),
            int(r.get('waterPercent', 0)),
            int(r.get('realWidthMeters', 0)),
            int(r.get('realLengthMeters', 0)),
            int(r.get('realAreaKm2', 0))
        ))

    with open(DB_BIN_PATH, "wb") as f:
        f.write(out)

def update_raw_header_dimensions(rid, width_m, length_m, area_km2):
    raw_path = os.path.join(GEO_DIR, f"heightmap_{rid}.raw")
    if not os.path.exists(raw_path):
        return
    with open(raw_path, "r+b") as f:
        magic = f.read(4)
        if magic == b'HMAP':
            # Offset dos campos realWidthMeters, realLengthMeters, realAreaKm2:
            # magic (4) + version (2) + header_size (2) + resolution (2) + regionId (4)
            # + minElev (4) + maxElev (4) + elevRange (4) + seaLevel (4) + isCoastal (1) + hasRivers (1) + waterPct (2) = 34 bytes
            f.seek(34)
            f.write(struct.pack("<iii", width_m, length_m, area_km2))

def main():
    print("=========================================================")
    print("[ProjectTerra] Calculo de Dimensoes Reais 1:1 dos Estados")
    print("=========================================================")

    t0 = time.time()
    regions = load_regions()
    if not regions:
        print("[ERRO] Nenhuma regiao encontrada no banco de dados!")
        return

    with open(BIN_MAP_PATH, "rb") as f:
        raw = f.read()

    bboxes = {}

    for py in range(H):
        lat = 90.0 - (py / float(H)) * 180.0
        row_offset = py * W * 2
        for px in range(W):
            idx = row_offset + px * 2
            rid = raw[idx] | (raw[idx+1] << 8)
            if rid == 0: continue
            lon = (px / float(W)) * 360.0 - 180.0
            
            if rid not in bboxes:
                bboxes[rid] = [lat, lat, lon, lon]
            else:
                b = bboxes[rid]
                if lat < b[0]: b[0] = lat
                if lat > b[1]: b[1] = lat
                if lon < b[2]: b[2] = lon
                if lon > b[3]: b[3] = lon

    print(f"[INFO] Bounding boxes reais calculadas em {time.time()-t0:.2f}s")

    updated_count = 0
    for r in regions:
        rid = r.get("id")
        c_lat = r.get("centerLat", 0.0)
        c_lon = r.get("centerLon", 0.0)

        if rid in bboxes:
            min_lat, max_lat, min_lon, max_lon = bboxes[rid]
        else:
            min_lat = c_lat - 0.15
            max_lat = c_lat + 0.15
            min_lon = c_lon - 0.15
            max_lon = c_lon + 0.15

        d_lat = max(0.04, abs(max_lat - min_lat))
        d_lon = max(0.04, abs(max_lon - min_lon))

        len_meters = max(8000.0, d_lat * 111132.0)
        width_meters = max(8000.0, d_lon * 111132.0 * math.cos(math.radians(max(-85.0, min(85.0, c_lat)))))
        area_km2 = (len_meters * width_meters) / 1000000.0

        r["minLat"] = round(min_lat, 4)
        r["maxLat"] = round(max_lat, 4)
        r["minLon"] = round(min_lon, 4)
        r["maxLon"] = round(max_lon, 4)
        r["realWidthMeters"] = int(round(width_meters))
        r["realLengthMeters"] = int(round(len_meters))
        r["realAreaKm2"] = int(round(area_km2))

        # Atualizar cabecalho binario do heightmap diretamente
        update_raw_header_dimensions(rid, r["realWidthMeters"], r["realLengthMeters"], r["realAreaKm2"])
        updated_count += 1

    save_regions_binary(regions)
    print(f"[SUCESSO] Total de {updated_count} regioes atualizadas em formato binario com dimensoes reais 1:1!")
    print("=========================================================")

if __name__ == "__main__":
    main()
