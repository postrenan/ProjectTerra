import os
import json
import numpy as np

def load_regions(backup_db_path):
    if os.path.exists(backup_db_path):
        with open(backup_db_path, "r", encoding="utf-8") as f:
            return json.load(f)["regions"]
    return []

def latlon_to_xy(lon, lat, map_w, map_h):
    x = int(round((lon + 180.0) / 360.0 * (map_w - 1)))
    y = int(round((90.0 - lat) / 180.0 * (map_h - 1)))
    return max(0, min(map_w - 1, x)), max(0, min(map_h - 1, y))

def detect_coastal_regions(id_map):
    print("  -> Analyzing ocean coastlines across 8M pixels...")
    is_land = (id_map > 0)
    is_ocean = (id_map == 0)
    coastal_mask = is_land & (
        np.pad(is_ocean[1:, :], ((0, 1), (0, 0)), constant_values=False) |
        np.pad(is_ocean[:-1, :], ((1, 0), (0, 0)), constant_values=False) |
        np.pad(is_ocean[:, 1:], ((0, 0), (0, 1)), constant_values=False) |
        np.pad(is_ocean[:, :-1], ((0, 0), (1, 0)), constant_values=False)
    )
    coastal_ids = set(int(x) for x in np.unique(id_map[coastal_mask]))
    print(f"     Identified {len(coastal_ids)} coastal regions touching global ocean/seas.")
    return coastal_ids

def load_airports(cache_dir, id_map, map_w, map_h):
    airports_path = os.path.join(cache_dir, "airports.geojson")
    with open(airports_path, "r", encoding="utf-8") as f:
        airports_data = json.load(f)["features"]
    
    airport_map = {}
    for feat in airports_data:
        geom = feat.get("geometry")
        if not geom or geom["type"] != "Point": continue
        lon, lat = geom["coordinates"]
        x, y = latlon_to_xy(lon, lat, map_w, map_h)
        rid = int(id_map[y, x])
        if rid == 0:
            for dy in range(-3, 4):
                for dx in range(-3, 4):
                    ny, nx = max(0, min(map_h - 1, y + dy)), max(0, min(map_w - 1, x + dx))
                    if id_map[ny, nx] > 0:
                        rid = int(id_map[ny, nx])
                        break
                if rid > 0: break
        if rid > 0:
            p = feat["properties"]
            aname = p.get("name") or p.get("NAME") or "Aeroporto"
            iata = p.get("iata_code") or p.get("gps_code") or ""
            label = f"{aname} ({iata})" if iata else aname
            if rid not in airport_map: airport_map[rid] = []
            airport_map[rid].append(label)
    return airport_map

def load_ports(cache_dir, id_map, map_w, map_h):
    ports_path = os.path.join(cache_dir, "ports.geojson")
    with open(ports_path, "r", encoding="utf-8") as f:
        ports_data = json.load(f)["features"]

    port_map = {}
    for feat in ports_data:
        geom = feat.get("geometry")
        if not geom or geom["type"] != "Point": continue
        lon, lat = geom["coordinates"]
        x, y = latlon_to_xy(lon, lat, map_w, map_h)
        rid = int(id_map[y, x])
        if rid == 0:
            for dy in range(-5, 6):
                for dx in range(-5, 6):
                    ny, nx = max(0, min(map_h - 1, y + dy)), max(0, min(map_w - 1, x + dx))
                    if id_map[ny, nx] > 0:
                        rid = int(id_map[ny, nx])
                        break
                if rid > 0: break
        if rid > 0:
            pname = feat["properties"].get("name") or "Porto Comercial"
            if rid not in port_map: port_map[rid] = []
            port_map[rid].append(pname)
    return port_map

def load_rivers(cache_dir, id_map, map_w, map_h):
    rivers_path = os.path.join(cache_dir, "rivers.geojson")
    with open(rivers_path, "r", encoding="utf-8") as f:
        rivers_data = json.load(f)["features"]

    river_map = {}
    for feat in rivers_data:
        rname = feat["properties"].get("name") or feat["properties"].get("name_en") or "Rio"
        geom = feat.get("geometry")
        if not geom: continue
        lines = [geom["coordinates"]] if geom["type"] == "LineString" else (geom["coordinates"] if geom["type"] == "MultiLineString" else [])
        for line in lines:
            for pt in line:
                x, y = latlon_to_xy(pt[0], pt[1], map_w, map_h)
                rid = int(id_map[y, x])
                if rid > 0:
                    if rid not in river_map: river_map[rid] = set()
                    river_map[rid].add(rname)
    return river_map

def load_lakes(cache_dir, id_map, map_w, map_h):
    lakes_path = os.path.join(cache_dir, "lakes.geojson")
    with open(lakes_path, "r", encoding="utf-8") as f:
        lakes_data = json.load(f)["features"]

    lake_map = {}
    for feat in lakes_data:
        lname = feat["properties"].get("name") or feat["properties"].get("name_en") or "Lago"
        geom = feat.get("geometry")
        if not geom: continue
        polys = [geom["coordinates"]] if geom["type"] == "Polygon" else (geom["coordinates"] if geom["type"] == "MultiPolygon" else [])
        for poly in polys:
            for ring in poly:
                for pt in ring:
                    x, y = latlon_to_xy(pt[0], pt[1], map_w, map_h)
                    rid = int(id_map[y, x])
                    if rid > 0:
                        if rid not in lake_map: lake_map[rid] = set()
                        lake_map[rid].add(lname)
    return lake_map
