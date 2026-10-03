import os
import json
import struct

from geo_common import (
    STREAMING_DIR, CACHE_DIR, load_regions, load_id_map, assign_region,
)

# Top-N cidades por regiao (estado/provincia). Base economica de cada regiao.
TOP_N = 10

CAPITAL_CLASSES = {
    "Admin-0 capital", "Admin-0 capital alt", "Admin-1 capital",
    "Admin-1 region capital", "Admin-0 region capital",
}


def build_cities():
    print("=== PROJECT TERRA: GERADOR DE CIDADES (BASE ECONOMICA) ===")
    regions = load_regions()
    id_map = load_id_map()
    print(f"[INFO] {len(regions)} regioes, raster {id_map.shape} carregados.")

    places_path = os.path.join(CACHE_DIR, "ne_10m_populated_places.geojson")
    with open(places_path, "r", encoding="utf-8") as f:
        places = json.load(f)["features"]
    print(f"[INFO] {len(places)} localidades povoadas no dataset.")

    by_region = {}  # rid -> list of city dicts
    assigned = 0
    for feat in places:
        geom = feat.get("geometry")
        if not geom or geom.get("type") != "Point":
            continue
        lon, lat = geom["coordinates"][0], geom["coordinates"][1]
        rid = assign_region(id_map, lon, lat, search_radius=6)
        if rid <= 0:
            continue
        p = feat["properties"]
        name = p.get("NAME") or p.get("NAMEASCII") or "Cidade"
        pop = p.get("POP_MAX") or p.get("POP_MIN") or 0
        pop = max(0, int(pop))  # Natural Earth usa -99 como sentinela de ausencia
        fcla = p.get("FEATURECLA") or ""
        by_region.setdefault(rid, []).append({
            "name": name,
            "lat": round(float(lat), 5),
            "lon": round(float(lon), 5),
            "population": pop,
            "isCapital": 1 if fcla in CAPITAL_CLASSES else 0,
        })
        assigned += 1

    print(f"[INFO] {assigned} cidades atribuidas a {len(by_region)} regioes.")

    # Ordena por populacao desc e corta no TOP_N; calcula peso economico normalizado.
    out_regions = []
    for rid in sorted(by_region.keys()):
        cities = sorted(by_region[rid], key=lambda c: c["population"], reverse=True)[:TOP_N]
        max_pop = max((c["population"] for c in cities), default=0) or 1
        for rank, c in enumerate(cities):
            c["rank"] = rank
            # Peso economico 0.1..1.0 (raiz suaviza a dominancia da maior cidade).
            c["economicWeight"] = round(0.1 + 0.9 * ((c["population"] / max_pop) ** 0.5), 4)
        out_regions.append({"regionId": rid, "cities": cities})

    save_json(out_regions)
    save_bin(out_regions)
    verify(out_regions, regions)


def save_json(out_regions):
    path = os.path.join(STREAMING_DIR, "regions_cities.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"regions": out_regions}, f, ensure_ascii=False, indent=1)
    print(f"[OUT] JSON {path} ({os.path.getsize(path)} bytes)")


def save_bin(out_regions):
    """PTCY v1: header + por regiao {id, count, cidades{name,lat,lon,pop,rank,capital,weight}}."""
    out = bytearray()
    out.extend(b"PTCY")
    out.extend(struct.pack("<HI", 1, len(out_regions)))

    def pack_str(s):
        b = (s or "").encode("utf-8")
        return struct.pack("<H", len(b)) + b

    for reg in out_regions:
        out.extend(struct.pack("<i", reg["regionId"]))
        out.extend(struct.pack("<H", len(reg["cities"])))
        for c in reg["cities"]:
            out.extend(pack_str(c["name"]))
            out.extend(struct.pack("<ff", c["lat"], c["lon"]))
            out.extend(struct.pack("<i", c["population"]))
            out.extend(struct.pack("<B", c["rank"]))
            out.extend(struct.pack("<B", c["isCapital"]))
            out.extend(struct.pack("<f", c["economicWeight"]))

    path = os.path.join(STREAMING_DIR, "regions_cities.bin")
    with open(path, "wb") as f:
        f.write(out)
    print(f"[OUT] BIN  {path} ({len(out)} bytes)")


def verify(out_regions, regions):
    name_by_id = {r["id"]: (r["name"], r["country"]) for r in regions}
    total_cities = sum(len(r["cities"]) for r in out_regions)
    print(f"\n=== VERIFICACAO ===")
    print(f"Regioes com cidades: {len(out_regions)} | Total de cidades (top-{TOP_N}): {total_cities}")
    for rid in [r["regionId"] for r in out_regions[:0]]:
        pass
    # Amostra: maiores capitais
    sample_ids = []
    for r in out_regions:
        if r["cities"] and r["cities"][0]["population"] > 5_000_000:
            sample_ids.append(r["regionId"])
    for rid in sample_ids[:6]:
        reg = next(r for r in out_regions if r["regionId"] == rid)
        nm, co = name_by_id.get(rid, ("?", "?"))
        top = reg["cities"][0]
        print(f"  [{rid}] {nm}, {co}: {len(reg['cities'])} cidades | maior: {top['name']} ({top['population']:,})")


if __name__ == "__main__":
    build_cities()
