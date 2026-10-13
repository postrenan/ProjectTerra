# -*- coding: utf-8 -*-
"""
Regenera APENAS os assets de selecao/linhas afetados pela politica de tamanho
minimo de regiao (region_policy.py):

  - Assets/StreamingAssets/region_id_map.bin      (mapa de IDs 4096x2048 u16)
  - Assets/_Project/Textures/Earth/region_id_map.png
  - Assets/_Project/Textures/Earth/4k_earth_borders.png

NAO toca em regions_database.bin, heightmaps, cidades nem infraestrutura:
os IDs e registros existentes permanecem intactos. Divisoes abaixo do limiar
apenas deixam de pintar seus pixels (passam a herdar o pais/vizinho) e suas
fronteiras internas somem.

Uso: python Tools/DownloadData/regenerate_region_map.py
(precisa das geojson em Temp/GeoCache, baixadas por build_borders_and_database.py)
"""
import os
import json
import numpy as np
from PIL import Image, ImageDraw

import region_policy as rp

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
CACHE = os.path.join(ROOT, "Temp", "GeoCache")
STREAMING = os.path.join(ROOT, "Assets", "StreamingAssets")
EARTH = os.path.join(ROOT, "Assets", "_Project", "Textures", "Earth")
BACKUP_JSON = os.path.join(ROOT, "Tools", "DataBackup", "regions_database.json")

COUNTRIES = os.path.join(CACHE, "countries_poly.geojson")
STATES = os.path.join(CACHE, "ne_10m_admin_1_states_provinces.geojson")
NAT_LINES = os.path.join(CACHE, "ne_10m_admin_0_boundary_lines_land.geojson")

MAP_W, MAP_H = 4096, 2048
STATE_LINE_RGBA = (160, 225, 255, 175)
NAT_LINE_RGBA = (255, 235, 140, 240)


def load(path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)["features"]


def validate_id_scheme(country_feats, state_feats):
    """Confere que o esquema de IDs reproduz o banco existente (id -> nome)."""
    if not os.path.exists(BACKUP_JSON):
        print("[WARN] backup json ausente; pulando validacao de IDs")
        return
    db = {r["id"]: r["name"] for r in load_json_regions(BACKUP_JSON)}
    mism = 0
    for i, feat in enumerate(country_feats):
        p = feat.get("properties", {}) or {}
        nm = p.get("NAME") or p.get("ADMIN") or "País"
        if db.get(i + 1) not in (nm, None) and mism < 8:
            print(f"  [id {i+1}] builder='{nm}' banco='{db.get(i+1)}'")
            mism += 1
    off = len(country_feats)
    for i, feat in enumerate(state_feats):
        p = feat.get("properties", {}) or {}
        nm = p.get("name") or p.get("name_en") or "Território"
        if db.get(off + i + 1) not in (nm, None) and mism < 8:
            print(f"  [id {off+i+1}] builder='{nm}' banco='{db.get(off+i+1)}'")
            mism += 1
    if mism == 0:
        print("[OK] esquema de IDs bate com o banco existente")
    else:
        print(f"[WARN] {mism}+ divergencias de nome (pode ser so acentuacao/variacao)")


def load_json_regions(path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f).get("regions", [])


def rasterize_id_map(country_feats, state_feats, merge):
    final = merge["state_final_id"]
    subdivided = merge["subdivided_cids"]
    kept = merge["kept_state_ids"]
    n_c = merge["n_countries"]

    img = Image.new("I", (MAP_W, MAP_H), 0)
    draw = ImageDraw.Draw(img)

    # 1. base: poligono de cada pais com seu id
    print("  -> base dos paises...")
    for i, feat in enumerate(country_feats):
        cid = i + 1
        geom = feat.get("geometry") or {}
        polys = _polys(geom)
        for poly in polys:
            if len(poly[0]) < 3:
                continue
            pts = [rp.latlon_to_xy(p[0], p[1], MAP_W, MAP_H) for p in poly[0]]
            draw.polygon(pts, fill=cid)

    # 2. estados por id FINAL (grandes primeiro; enclaves por cima)
    print("  -> divisoes (id final, grandes primeiro)...")
    items = []
    for i, feat in enumerate(state_feats):
        sid = n_c + i + 1
        geom = feat.get("geometry") or {}
        items.append((_bbox_area(geom), final.get(sid, sid), geom))
    items.sort(key=lambda x: x[0], reverse=True)
    for _, fid, geom in items:
        for poly in _polys(geom):
            if len(poly[0]) < 3:
                continue
            pts = [rp.latlon_to_xy(p[0], p[1], MAP_W, MAP_H) for p in poly[0]]
            draw.polygon(pts, fill=fid)

    # 3. carimbos de centro: so para regioes CLICAVEIS mantidas
    print("  -> carimbos de centro (paises nao subdivididos + divisoes mantidas)...")
    for i, feat in enumerate(country_feats):
        cid = i + 1
        if cid in subdivided:
            continue  # coberto pelas divisoes; nao deve ser clicavel inteiro
        clon = (feat.get("properties", {}) or {}).get("LABEL_X") or 0.0
        clat = (feat.get("properties", {}) or {}).get("LABEL_Y") or 0.0
        _stamp(draw, cid, clon, clat)
    for i, feat in enumerate(state_feats):
        sid = n_c + i + 1
        if sid in kept:
            cx, cy = rp._state_centroid(feat)
            _stamp(draw, sid, cx, cy)

    return np.array(img, dtype=np.uint16)


def _stamp(draw, rid, lon, lat):
    cx, cy = rp.latlon_to_xy(lon, lat, MAP_W, MAP_H)
    if 0 <= cx < MAP_W and 0 <= cy < MAP_H:
        draw.rectangle([cx - 1, cy - 1, cx + 1, cy + 1], fill=rid)


def _polys(geom):
    t = geom.get("type")
    c = geom.get("coordinates", [])
    if t == "Polygon":
        return [c] if c else []
    if t == "MultiPolygon":
        return c
    return []


def _bbox_area(geom):
    polys = _polys(geom)
    minx = miny = 1e9
    maxx = maxy = -1e9
    for poly in polys:
        if not poly or len(poly[0]) < 3:
            continue
        for pt in poly[0]:
            x, y = rp.latlon_to_xy(pt[0], pt[1], MAP_W, MAP_H)
            minx = min(minx, x); maxx = max(maxx, x)
            miny = min(miny, y); maxy = max(maxy, y)
    if maxx < minx:
        return 0
    return (maxx - minx) * (maxy - miny)


def internal_border_mask(id_array, country_key):
    """Linhas internas = fronteira entre duas regioes do MESMO pais."""
    maxid = int(id_array.max())
    cof = np.zeros(maxid + 1, dtype=np.int32)
    for rid, key in country_key.items():
        if rid <= maxid:
            cof[rid] = key
    kp = cof[id_array]  # chave nacional por pixel (0 = oceano)

    mask = np.zeros_like(id_array, dtype=bool)
    # vizinho horizontal
    a, b = id_array[:, :-1], id_array[:, 1:]
    ka, kb = kp[:, :-1], kp[:, 1:]
    h = (a != b) & (a != 0) & (b != 0) & (ka == kb)
    mask[:, :-1] |= h
    # vizinho vertical
    a, b = id_array[:-1, :], id_array[1:, :]
    ka, kb = kp[:-1, :], kp[1:, :]
    v = (a != b) & (a != 0) & (b != 0) & (ka == kb)
    mask[:-1, :] |= v
    return mask


def render_national_lines(features, draw, width=2):
    for feature in features:
        geom = (feature or {}).get("geometry")
        if not geom:
            continue
        gt = geom.get("type")
        coords = geom.get("coordinates", [])
        lines = [coords] if gt == "LineString" else (coords if gt == "MultiLineString" else [])
        for line in lines:
            pts = []
            for i in range(len(line) - 1):
                p1, p2 = line[i], line[i + 1]
                if abs(p1[0] - p2[0]) > 180.0:  # antimeridiano
                    if len(pts) > 1:
                        draw.line(pts, fill=NAT_LINE_RGBA, width=width)
                    pts = []
                    continue
                x1, y1 = rp.latlon_to_xy(p1[0], p1[1], MAP_W, MAP_H)
                x2, y2 = rp.latlon_to_xy(p2[0], p2[1], MAP_W, MAP_H)
                if not pts:
                    pts.append((x1, y1))
                pts.append((x2, y2))
            if len(pts) > 1:
                draw.line(pts, fill=NAT_LINE_RGBA, width=width)


def main():
    print("=== REGENERAR MAPA DE IDs + BORDAS (politica tamanho minimo) ===")
    print(f"    COUNTRY_MIN={rp.COUNTRY_MIN_KM2:,} km2  DIVISION_MIN={rp.DIVISION_MIN_KM2:,} km2")
    country_feats = load(COUNTRIES)
    state_feats = load(STATES)
    validate_id_scheme(country_feats, state_feats)

    merge = rp.compute_merge(country_feats, state_feats)
    n_sub = len(merge["subdivided_cids"])
    n_kept = len(merge["kept_state_ids"])
    n_states = len(state_feats)
    n_absorbed = n_states - sum(1 for s, f in merge["state_final_id"].items() if f == s)
    final_regions = merge["n_countries"] + n_kept - _country_reps_overlap(merge)
    print(f"[POLITICA] {merge['n_countries']} paises | {n_sub} paises subdivididos | "
          f"{n_kept} divisoes mantidas | {n_absorbed} divisoes absorvidas")
    print(f"[POLITICA] ~{merge['n_countries'] + n_kept} regioes clicaveis (de {merge['n_countries'] + n_states})")

    # --- mapa de IDs ---
    print("[1/3] rasterizando mapa de IDs...")
    id_array = rasterize_id_map(country_feats, state_feats, merge)

    bin_path = os.path.join(STREAMING, "region_id_map.bin")
    id_array.tofile(bin_path)
    print(f"      region_id_map.bin  ({os.path.getsize(bin_path):,} bytes)")

    rgb = np.zeros((MAP_H, MAP_W, 3), dtype=np.uint8)
    rgb[:, :, 0] = (id_array & 0xFF).astype(np.uint8)
    rgb[:, :, 1] = ((id_array >> 8) & 0xFF).astype(np.uint8)
    png_path = os.path.join(EARTH, "region_id_map.png")
    Image.fromarray(rgb, "RGB").save(png_path, "PNG", optimize=False)
    print(f"      region_id_map.png  ({os.path.getsize(png_path):,} bytes)")

    # --- bordas ---
    print("[2/3] desenhando fronteiras internas (edge-detect) ...")
    mask = internal_border_mask(id_array, merge["country_key"])
    border = np.zeros((MAP_H, MAP_W, 4), dtype=np.uint8)
    border[mask] = STATE_LINE_RGBA
    img = Image.fromarray(border, "RGBA")

    print("[3/3] desenhando fronteiras nacionais (vetorial, inalteradas) ...")
    draw = ImageDraw.Draw(img)
    render_national_lines(load(NAT_LINES), draw, width=2)
    borders_path = os.path.join(EARTH, "4k_earth_borders.png")
    img.save(borders_path, "PNG", optimize=True)
    print(f"      4k_earth_borders.png  ({os.path.getsize(borders_path):,} bytes)  "
          f"({int(mask.sum()):,} px de linha interna)")
    print("=== OK: banco e heightmaps intactos; apenas selecao/linhas atualizadas ===")


def _country_reps_overlap(merge):
    # reps de grupos sem pais-base ja contam em kept; nao subtrai nada aqui
    return 0


if __name__ == "__main__":
    main()
