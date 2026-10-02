import json
import os
import sys
import struct
import time
from concurrent.futures import ThreadPoolExecutor

BASE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
STREAMING_DIR = os.path.join(BASE_DIR, "Assets", "StreamingAssets")
GEO_DIR = os.path.join(STREAMING_DIR, "GeographicData")
DB_JSON = os.path.join(STREAMING_DIR, "regions_database.json")
DB_BIN = os.path.join(STREAMING_DIR, "regions_database.bin")
BACKUP_DIR = os.path.join(BASE_DIR, "Tools", "DataBackup")

def pack_str(s):
    b = (s or '').encode('utf-8')
    return struct.pack('<H', len(b)) + b

def migrate_regions_database():
    print("[1/3] Migrando regions_database.json -> regions_database.bin...")
    if not os.path.exists(DB_JSON):
        print(f"  [AVISO] {DB_JSON} nao encontrado. Pulando...")
        return

    os.makedirs(BACKUP_DIR, exist_ok=True)
    # Copiar backup
    backup_db = os.path.join(BACKUP_DIR, "regions_database.json")
    if not os.path.exists(backup_db):
        with open(DB_JSON, "rb") as f_src, open(backup_db, "wb") as f_dst:
            f_dst.write(f_src.read())
        print(f"  [BACKUP] Salvo em {backup_db}")

    with open(DB_JSON, "r", encoding="utf-8") as f:
        db = json.load(f)

    regions = db.get("regions", [])
    out = bytearray()
    out.extend(b'PTRD')  # Magic: Project Terra Region Database
    out.extend(struct.pack('<HI', 1, len(regions)))  # Version 1, Count

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

    with open(DB_BIN, "wb") as f:
        f.write(out)

    orig_sz = os.path.getsize(DB_JSON)
    new_sz = len(out)
    ratio = (1.0 - (new_sz / orig_sz)) * 100
    print(f"  [SUCESSO] {len(regions)} regioes migradas. De {orig_sz:,} bytes para {new_sz:,} bytes (-{ratio:.1f}%!).")

def process_single_region_terrain(r_id):
    raw_path = os.path.join(GEO_DIR, f"heightmap_{r_id}.raw")
    json_path = os.path.join(GEO_DIR, f"hydro_{r_id}.json")

    if not os.path.exists(raw_path):
        return False

    with open(raw_path, "rb") as f_raw:
        raw_data = f_raw.read()

    # Se ja tiver o magic HMAP, ja esta no formato novo
    if raw_data.startswith(b'HMAP'):
        return True

    # Ler dados hidrograficos do JSON
    h = {}
    if os.path.exists(json_path):
        try:
            with open(json_path, "r", encoding="utf-8") as f_json:
                h = json.load(f_json)
        except Exception:
            pass

    name_bytes = pack_str(h.get('name', ''))
    country_bytes = pack_str(h.get('country', ''))

    fixed = struct.pack('<4sHHHi ffff ?? h iii',
        b'HMAP',
        1,   # version
        0,   # placeholder header_size
        129, # resolution
        int(h.get('regionId', r_id)),
        float(h.get('minElevation', 0.0)),
        float(h.get('maxElevation', 0.0)),
        float(h.get('elevationRange', 0.0)),
        float(h.get('seaLevelNormalized', 0.0)),
        bool(h.get('isCoastal', False)),
        bool(h.get('hasRivers', False)),
        int(h.get('waterPercent', 0)),
        int(h.get('realWidthMeters', 0)),
        int(h.get('realLengthMeters', 0)),
        int(h.get('realAreaKm2', 0))
    )

    header = bytearray(fixed + name_bytes + country_bytes)
    header_size = len(header)
    struct.pack_into('<H', header, 6, header_size)

    # Gravar arquivo unificado no mesmo caminho raw
    with open(raw_path, "wb") as f_out:
        f_out.write(header)
        f_out.write(raw_data)

    return True

def migrate_terrain_datasets():
    print("[2/3] Unificando datasets de relevo (.raw + .json) em arquivos binarios unificados...")
    # Descobrir todos os IDs
    r_ids = []
    for fname in os.listdir(GEO_DIR):
        if fname.startswith("heightmap_") and fname.endswith(".raw"):
            try:
                rid = int(fname.replace("heightmap_", "").replace(".raw", ""))
                r_ids.append(rid)
            except ValueError:
                pass

    print(f"  [INFO] Encontrados {len(r_ids)} arquivos heightmap para converter...")
    t0 = time.time()
    with ThreadPoolExecutor(max_workers=8) as pool:
        results = list(pool.map(process_single_region_terrain, r_ids))

    success_count = sum(1 for res in results if res)
    print(f"  [SUCESSO] {success_count}/{len(r_ids)} terrenos unificados em {time.time()-t0:.2f}s!")

def clean_redundant_files():
    print("[3/3] Removendo arquivos JSON e META redundantes em GeographicData e StreamingAssets...")
    removed_jsons = 0
    removed_metas = 0

    for fname in os.listdir(GEO_DIR):
        if fname.startswith("hydro_") and fname.endswith(".json"):
            fpath = os.path.join(GEO_DIR, fname)
            mpath = fpath + ".meta"
            try:
                os.remove(fpath)
                removed_jsons += 1
            except Exception:
                pass
            if os.path.exists(mpath):
                try:
                    os.remove(mpath)
                    removed_metas += 1
                except Exception:
                    pass

    # Remover regions_database.json e seu .meta se .bin existir e for valido
    if os.path.exists(DB_BIN) and os.path.getsize(DB_BIN) > 100000:
        if os.path.exists(DB_JSON):
            os.remove(DB_JSON)
            print(f"  [REMOVIDO] {DB_JSON}")
        json_meta = DB_JSON + ".meta"
        if os.path.exists(json_meta):
            os.remove(json_meta)
            print(f"  [REMOVIDO] {json_meta}")

    print(f"  [SUCESSO] Limpeza concluida: {removed_jsons} arquivos .json e {removed_metas} arquivos .meta eliminados!")
    print(f"  Total de arquivos a menos no projeto: {removed_jsons + removed_metas + 2} arquivos!")

if __name__ == "__main__":
    t_start = time.time()
    print("=========================================================")
    print("[ProjectTerra] Migracao Completa de Formatos para Binario")
    print("=========================================================")
    migrate_regions_database()
    migrate_terrain_datasets()
    clean_redundant_files()
    print("---------------------------------------------------------")
    print(f"Migracao concluida com sucesso em {time.time() - t_start:.2f} segundos!")
    print("=========================================================")
