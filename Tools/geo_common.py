import os
import json
import numpy as np

STREAMING_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Assets", "StreamingAssets"))
CACHE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Temp", "GeoCache"))
BACKUP_DB_PATH = os.path.abspath(os.path.join(os.path.dirname(__file__), "DataBackup", "regions_database.json"))

MAP_W = 4096
MAP_H = 2048


def load_regions():
    """Catalogo autoritativo das 4.838 regioes (id, bbox, centro)."""
    with open(BACKUP_DB_PATH, "r", encoding="utf-8") as f:
        return json.load(f)["regions"]


def load_id_map():
    """Raster 4096x2048 uint16: cada pixel lat/lon -> id da regiao (0 = oceano)."""
    path = os.path.join(STREAMING_DIR, "region_id_map.bin")
    return np.fromfile(path, dtype=np.uint16).reshape((MAP_H, MAP_W))


def latlon_to_xy(lon, lat):
    x = int(round((lon + 180.0) / 360.0 * (MAP_W - 1)))
    y = int(round((90.0 - lat) / 180.0 * (MAP_H - 1)))
    return max(0, min(MAP_W - 1, x)), max(0, min(MAP_H - 1, y))


def assign_region(id_map, lon, lat, search_radius=0):
    """Resolve a regiao de um ponto. Com search_radius > 0 procura o pixel
    de terra mais proximo (util para cidades/portos costeiros caindo no mar)."""
    x, y = latlon_to_xy(lon, lat)
    rid = int(id_map[y, x])
    if rid > 0 or search_radius <= 0:
        return rid
    for r in range(1, search_radius + 1):
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                ny = max(0, min(MAP_H - 1, y + dy))
                nx = max(0, min(MAP_W - 1, x + dx))
                v = int(id_map[ny, nx])
                if v > 0:
                    return v
    return 0


def build_bbox_lookup(regions):
    """id -> (minLat, maxLat, minLon, maxLon) para projecao no runtime."""
    out = {}
    for r in regions:
        out[r["id"]] = (
            float(r.get("minLat", 0.0)), float(r.get("maxLat", 0.0)),
            float(r.get("minLon", 0.0)), float(r.get("maxLon", 0.0)),
        )
    return out


def iter_lines(geom):
    """Normaliza LineString / MultiLineString em listas de pontos [lon,lat]."""
    if not geom:
        return []
    t = geom.get("type")
    if t == "LineString":
        return [geom.get("coordinates", [])]
    if t == "MultiLineString":
        return geom.get("coordinates", [])
    return []
