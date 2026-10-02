import json
import os
import sys
import struct
import math
import time
from concurrent.futures import ThreadPoolExecutor

DB_PATH = os.path.join("Assets", "StreamingAssets", "regions_database.json")
OUTPUT_DIR = os.path.join("Assets", "StreamingAssets", "GeographicData")
RESOLUTION = 129  # Resolução padrão do Unity Terrain (2^n + 1)

# Principais cordilheiras mundiais e platôs para relevo fotorrealista (lat, lon, raio, altura máx)
MOUNTAIN_RANGES = [
    # Andes
    (-20.0, -68.0, 30.0, 5500.0),
    (-40.0, -71.0, 20.0, 3500.0),
    (0.0, -78.0, 15.0, 4800.0),
    # Rochosas / Cascades
    (45.0, -115.0, 25.0, 3800.0),
    (38.0, -106.0, 20.0, 4200.0),
    # Alpes
    (46.0, 10.0, 10.0, 4400.0),
    # Himalaia / Tibete
    (30.0, 85.0, 25.0, 7200.0),
    # Planalto Brasileiro / Serra do Mar / Mantiqueira
    (-22.0, -45.0, 15.0, 2400.0),
    (-15.0, -47.0, 18.0, 1400.0),
    # Rift Africano / Kilimanjaro / Etiópia
    (-3.0, 37.0, 12.0, 5200.0),
    (9.0, 39.0, 15.0, 3200.0),
    # Urais
    (60.0, 60.0, 20.0, 1800.0),
    # Grande Cordilheira Divisória (Austrália)
    (-33.0, 150.0, 15.0, 1800.0),
    # Montes Apalaches
    (37.0, -80.0, 15.0, 1900.0),
    # Escandinávia
    (64.0, 14.0, 15.0, 2100.0),
    # Cáucaso
    (42.5, 44.5, 8.0, 5000.0)
]

def get_continental_elevation(lat, lon):
    """Calcula a elevação base geodésica em metros para uma dada latitude e longitude."""
    base_elev = 180.0  # Média continental plana
    
    # Influência das grandes cadeias de montanhas reais
    for m_lat, m_lon, radius, max_h in MOUNTAIN_RANGES:
        d_lat = lat - m_lat
        d_lon = lon - m_lon
        dist = math.sqrt(d_lat * d_lat + d_lon * d_lon)
        if dist < radius:
            weight = math.cos((dist / radius) * (math.pi / 2.0))
            elev_boost = weight * max_h
            if elev_boost > base_elev:
                base_elev = elev_boost

    # Variações latitudinais naturais (tundra ártica, desertos, vales equatoriais)
    lat_rad = math.radians(lat)
    lon_rad = math.radians(lon)
    macro_noise = math.sin(lat_rad * 3.5) * math.cos(lon_rad * 4.2) * 120.0
    return max(15.0, base_elev + macro_noise)

def generate_region_geography(region):
    r_id = region.get("id", 0)
    lat = region.get("centerLat", 0.0)
    lon = region.get("centerLon", 0.0)
    water_pct = region.get("waterPercent", 20)
    minerals_pct = region.get("mineralsPercent", 20)
    arable_pct = region.get("arablePercent", 50)
    forest_pct = region.get("forestPercent", 30)

    # Elevação média e rugosidade
    mean_elev = get_continental_elevation(lat, lon)
    roughness = (minerals_pct / 100.0) * 1.6 + 0.4
    flatness = (arable_pct / 100.0) * 0.7

    is_coastal = water_pct >= 35 or r_id == 0

    heights = [[0.0 for _ in range(RESOLUTION)] for _ in range(RESOLUTION)]
    
    # Parâmetros de hidrografia
    river_points = []
    lakes = []

    # Determinar calha de rio principal (se tiver água interior)
    river_amplitude = 0.0
    river_center_x = RESOLUTION * 0.5
    river_width = 8.0
    if not is_coastal and water_pct > 15:
        river_amplitude = 12.0 + (water_pct * 0.2)
        # Caminho do rio em meandros naturais
        for y_idx in range(0, RESOLUTION, 8):
            py = y_idx / float(RESOLUTION - 1)
            px = 0.5 + 0.15 * math.sin(py * math.pi * 3.0) + 0.08 * math.cos(py * math.pi * 6.0)
            river_points.append({"x": round(px * 1000.0, 1), "z": round(py * 1000.0, 1)})
    
    # Se for litorânea, criar linha de costa
    coast_line_y = int(RESOLUTION * 0.32) if is_coastal else -1

    min_h = 99999.0
    max_h = -99999.0

    # Síntese topográfica multi-frequência (Harmônicas Perlin analíticas)
    for y in range(RESOLUTION):
        ny = y / float(RESOLUTION - 1)
        for x in range(RESOLUTION):
            nx = x / float(RESOLUTION - 1)

            # Relevo base montanhoso/ondulado
            h1 = math.sin((nx * 2.8 + lon * 0.1) * math.pi) * math.cos((ny * 2.5 + lat * 0.1) * math.pi)
            h2 = math.sin(nx * 7.2 * math.pi + 1.2) * math.cos(ny * 6.8 * math.pi + 0.8) * 0.4
            h3 = math.sin(nx * 18.5 * math.pi) * math.cos(ny * 16.2 * math.pi) * 0.15

            combined_noise = (h1 + h2 + h3) * roughness
            # Planícies agrícolas reduzem a amplitude local
            h_meters = mean_elev + (combined_noise * (280.0 * (1.0 - flatness * 0.6)))

            # Incisão do leito do rio
            if river_amplitude > 0.0:
                river_path_x = 0.5 + 0.15 * math.sin(ny * math.pi * 3.0) + 0.08 * math.cos(ny * math.pi * 6.0)
                dist_to_river = abs(nx - river_path_x) * RESOLUTION
                if dist_to_river < river_width:
                    factor = 1.0 - (dist_to_river / river_width)
                    valley_depression = (factor * factor) * river_amplitude
                    h_meters -= valley_depression

            # Litoral / Oceano para regiões com costa
            if is_coastal:
                if y < coast_line_y:
                    # Plataforma submarina
                    depth = (coast_line_y - y) / float(coast_line_y)
                    h_meters = -15.0 * depth
                elif y < coast_line_y + 6:
                    # Praia de transição suave
                    t_beach = (y - coast_line_y) / 6.0
                    h_meters = t_beach * 4.0

            heights[y][x] = h_meters
            if h_meters < min_h: min_h = h_meters
            if h_meters > max_h: max_h = h_meters

    # Normalizar para 16-bit unsigned integer (0 a 65535)
    range_h = max(1.0, max_h - min_h)
    raw_bytes = bytearray()
    
    for y in range(RESOLUTION):
        for x in range(RESOLUTION):
            norm = (heights[y][x] - min_h) / range_h
            val_16 = int(max(0, min(65535, round(norm * 65535))))
            raw_bytes.extend(struct.pack("<H", val_16))

    # Escrever arquivo binario unificado (Header HMAP + 16-bit Heightmap)
    raw_path = os.path.join(OUTPUT_DIR, f"heightmap_{r_id}.raw")

    def pack_str(s):
        b = (s or '').encode('utf-8')
        return struct.pack('<H', len(b)) + b

    name_bytes = pack_str(region.get("name", ""))
    country_bytes = pack_str(region.get("country", ""))

    fixed = struct.pack('<4sHHHi ffff ?? h iii',
        b'HMAP',
        1,   # version
        0,   # placeholder header_size
        129, # resolution
        r_id,
        float(round(min_h, 1)),
        float(round(max_h, 1)),
        float(round(range_h, 1)),
        float(round((-min_h) / range_h, 4) if min_h < 0 else 0.0),
        bool(is_coastal),
        bool(len(river_points) > 0),
        int(water_pct),
        int(region.get("realWidthMeters", 0)),
        int(region.get("realLengthMeters", 0)),
        int(region.get("realAreaKm2", 0))
    )
    header = bytearray(fixed + name_bytes + country_bytes)
    struct.pack_into('<H', header, 6, len(header))

    with open(raw_path, "wb") as f_raw:
        f_raw.write(header)
        f_raw.write(raw_bytes)

def load_regions_database():
    bin_path = os.path.join("Assets", "StreamingAssets", "regions_database.bin")
    json_path = os.path.join("Assets", "StreamingAssets", "regions_database.json")
    if os.path.exists(bin_path):
        regions = []
        with open(bin_path, "rb") as f:
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
    elif os.path.exists(json_path):
        with open(json_path, "r", encoding="utf-8") as f:
            return json.load(f).get("regions", [])
    return []

def main():
    start_time = time.time()
    print("=========================================================")
    print("[ProjectTerra] Processador de Relevo e Hidrografia Real")
    print("=========================================================")

    os.makedirs(OUTPUT_DIR, exist_ok=True)
    regions = load_regions_database()
    total_regions = len(regions)
    if total_regions == 0:
        print("[ERRO] Nenhuma regiao carregada do banco de dados binario/json.")
        sys.exit(1)

    print(f"[INFO] Total de regioes no banco: {total_regions}")
    print(f"[INFO] Resolucao do grid: {RESOLUTION}x{RESOLUTION} (16-bit Little-Endian RAW com Header HMAP)")
    print("[INFO] Iniciando processamento em lote com threads paralelas...")

    with ThreadPoolExecutor(max_workers=8) as executor:
        list(executor.map(generate_region_geography, regions))

    elapsed = time.time() - start_time
    print("---------------------------------------------------------")
    print(f"[SUCESSO] Concluido com sucesso em {elapsed:.2f} segundos!")
    print(f"[DESTINO] Pasta: {OUTPUT_DIR}")
    print(f"[STATUS] Gerados: {total_regions} arquivos binarios unificados (.raw com cabecalho)")
    print("=========================================================")

if __name__ == "__main__":
    main()
