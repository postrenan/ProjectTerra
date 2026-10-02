import os
import json
import urllib.request
import math
import numpy as np
from PIL import Image, ImageDraw

CACHE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Temp", "GeoCache"))
OUTPUT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Textures", "Earth"))
STREAMING_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "StreamingAssets"))

os.makedirs(CACHE_DIR, exist_ok=True)
os.makedirs(OUTPUT_DIR, exist_ok=True)
os.makedirs(STREAMING_DIR, exist_ok=True)

URLS = {
    'ne_10m_admin_0_boundary_lines_land': 'https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_10m_admin_0_boundary_lines_land.geojson',
    'ne_10m_admin_1_states_provinces_lines': 'https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_10m_admin_1_states_provinces_lines.geojson',
    'countries_poly': 'https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_50m_admin_0_countries.geojson',
    'ne_10m_admin_1_states_provinces': 'https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_10m_admin_1_states_provinces.geojson'
}

def download_file(name, url):
    dest = os.path.join(CACHE_DIR, f"{name}.geojson")
    if os.path.exists(dest) and os.path.getsize(dest) > 1000:
        print(f"[CACHE] {name}.geojson already downloaded ({os.path.getsize(dest)} bytes)")
        return dest
    print(f"[DOWNLOAD] Downloading {name} from {url}...")
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    with urllib.request.urlopen(req, timeout=60) as resp:
        content = resp.read()
        with open(dest, 'wb') as f:
            f.write(content)
    print(f"[DONE] Saved {dest} ({len(content)} bytes)")
    return dest

def latlon_to_xy(lon, lat, width, height):
    x = (lon + 180.0) / 360.0 * (width - 1)
    y = (90.0 - lat) / 180.0 * (height - 1)
    return x, y

def render_lines(features, draw, width, height, color, line_width):
    line_count = 0
    for feature in features:
        if not feature:
            continue
        geom = feature.get('geometry')
        if not geom:
            continue
        gtype = geom.get('type')
        coords = geom.get('coordinates', [])

        if gtype == 'LineString':
            lines = [coords]
        elif gtype == 'MultiLineString':
            lines = coords
        else:
            continue

        for line in lines:
            pts = []
            for i in range(len(line) - 1):
                p1 = line[i]
                p2 = line[i + 1]
                # Avoid wrap lines across antimeridian
                if abs(p1[0] - p2[0]) > 180.0:
                    if len(pts) > 1:
                        draw.line(pts, fill=color, width=line_width)
                        line_count += 1
                    pts = []
                    continue

                x1, y1 = latlon_to_xy(p1[0], p1[1], width, height)
                x2, y2 = latlon_to_xy(p2[0], p2[1], width, height)
                if not pts:
                    pts.append((x1, y1))
                pts.append((x2, y2))

            if len(pts) > 1:
                draw.line(pts, fill=color, width=line_width)
                line_count += 1
    return line_count

def get_stats(name, country, lat, lon):
    seed = (int(abs(lat * 100)) * 73 + int(abs(lon * 100)) * 37 + len(name) * 19) % 100
    if abs(lat) > 60:
        forest = 15 + (seed % 25)
        minerals = 30 + ((seed * 3) % 55)
        arable = 2 + (seed % 15)
        water = 40 + (seed % 40)
    elif abs(lat) < 23.5:
        forest = 35 + (seed % 55)
        minerals = 20 + ((seed * 7) % 60)
        arable = 30 + (seed % 50)
        water = 25 + ((seed * 5) % 65)
    else:
        forest = 20 + (seed % 60)
        minerals = 25 + ((seed * 11) % 60)
        arable = 35 + (seed % 55)
        water = 20 + ((seed * 3) % 60)
    return {
        'forestPercent': max(5, min(95, forest)),
        'mineralsPercent': max(10, min(90, minerals)),
        'arablePercent': max(5, min(90, arable)),
        'waterPercent': max(10, min(95, water))
    }

def get_poly_bbox_area(coords, gtype, map_w, map_h):
    polys = [coords] if gtype == 'Polygon' else (coords if gtype == 'MultiPolygon' else [])
    min_x, max_x = map_w, 0
    min_y, max_y = map_h, 0
    for poly in polys:
        if not poly or len(poly[0]) < 3:
            continue
        for pt in poly[0]:
            x, y = latlon_to_xy(pt[0], pt[1], map_w, map_h)
            if x < min_x: min_x = x
            if x > max_x: max_x = x
            if y < min_y: min_y = y
            if y > max_y: max_y = y
    if max_x < min_x:
        return 0
    return (max_x - min_x) * (max_y - min_y)

def generate_regions_db_and_id_map(states_path, countries_path):
    print("[DB] Generating 4K regions database and raster ID map...")

    with open(countries_path, 'r', encoding='utf-8') as f:
        c_feats = json.load(f)['features']
    with open(states_path, 'r', encoding='utf-8') as f:
        s_feats = json.load(f)['features']

    regions = []

    # 1. Countries (Base Layer)
    for feat in c_feats:
        p = feat.get('properties', {})
        name = p.get('NAME') or p.get('ADMIN') or 'País'
        country = name
        clat = p.get('LABEL_Y') or 0.0
        clon = p.get('LABEL_X') or 0.0
        stats = get_stats(name, country, clat, clon)
        regions.append({
            'id': len(regions) + 1,
            'name': name,
            'country': country,
            'type': 'País Soberano',
            'centerLat': round(float(clat), 4),
            'centerLon': round(float(clon), 4),
            'forestPercent': stats['forestPercent'],
            'mineralsPercent': stats['mineralsPercent'],
            'arablePercent': stats['arablePercent'],
            'waterPercent': stats['waterPercent']
        })

    # 2. States / Provinces (Detailed Layer - 4,596 features)
    state_offset = len(c_feats)
    state_items = []
    MAP_W = 4096
    MAP_H = 2048

    for i, feat in enumerate(s_feats):
        p = feat.get('properties', {})
        admin = p.get('admin') or p.get('geonunit') or p.get('sov_a3') or 'País'
        name = p.get('name') or p.get('name_en') or (f"{admin} (Território)") or 'Território'
        stype = p.get('type_en') or p.get('type') or 'Estado'
        clat = p.get('latitude') or 0.0
        clon = p.get('longitude') or 0.0
        stats = get_stats(name, admin, clat, clon)

        cid = state_offset + i + 1
        regions.append({
            'id': cid,
            'name': name,
            'country': admin,
            'type': stype,
            'centerLat': round(float(clat), 4),
            'centerLon': round(float(clon), 4),
            'forestPercent': stats['forestPercent'],
            'mineralsPercent': stats['mineralsPercent'],
            'arablePercent': stats['arablePercent'],
            'waterPercent': stats['waterPercent']
        })

        # Calculate bounding box area for Z-ordering (large polygons drawn first, small enclaves drawn on top)
        geom = feat.get('geometry') or {}
        gtype = geom.get('type')
        coords = geom.get('coordinates', [])
        area = get_poly_bbox_area(coords, gtype, MAP_W, MAP_H)
        state_items.append((area, cid, geom))

    print(f"[DB] Total regions cataloged: {len(regions)}")

    # Raster ID Map (4096 x 2048, 16-bit ushort)
    img = Image.new('I', (MAP_W, MAP_H), 0)
    draw = ImageDraw.Draw(img)

    print("  -> Rasterizing base country layer...")
    for i, feat in enumerate(c_feats):
        cid = i + 1
        geom = feat.get('geometry') or {}
        coords = geom.get('coordinates', [])
        polys = [coords] if geom.get('type') == 'Polygon' else (coords if geom.get('type') == 'MultiPolygon' else [])
        for poly in polys:
            if not poly or len(poly[0]) < 3: continue
            pts = [latlon_to_xy(pt[0], pt[1], MAP_W, MAP_H) for pt in poly[0]]
            draw.polygon(pts, fill=cid)

    print("  -> Rasterizing 4,596 state/province polygons (sorted by area descending)...")
    # Sort descending by bounding box area: largest states drawn first, smallest enclaves/islands drawn last on top
    state_items.sort(key=lambda x: x[0], reverse=True)
    for area, cid, geom in state_items:
        coords = geom.get('coordinates', [])
        polys = [coords] if geom.get('type') == 'Polygon' else (coords if geom.get('type') == 'MultiPolygon' else [])
        for poly in polys:
            if not poly or len(poly[0]) < 3: continue
            pts = [latlon_to_xy(pt[0], pt[1], MAP_W, MAP_H) for pt in poly[0]]
            draw.polygon(pts, fill=cid)

    print("  -> Stamping center points for micro-islands and small territories...")
    for r in regions:
        cx, cy = latlon_to_xy(r['centerLon'], r['centerLat'], MAP_W, MAP_H)
        if 0 <= cx < MAP_W and 0 <= cy < MAP_H:
            # 3x3 footprint ensures micro-islands are selectable and render with crisp highlight
            draw.rectangle([cx-1, cy-1, cx+1, cy+1], fill=r['id'])

    # Convert Image to NumPy array for ultra-fast I/O and lossless encoding
    print("  -> Converting to raw binary and lossless texture...")
    id_array = np.array(img, dtype=np.uint16)

    # 1. Export 4096x2048x2 bytes raw binary (16 MB)
    bin_path = os.path.join(STREAMING_DIR, "region_id_map.bin")
    id_array.tofile(bin_path)
    print(f"[MAP] Saved region_id_map.bin ({os.path.getsize(bin_path)} bytes)")

    # 2. Export 4096x2048 lossless PNG texture for Unity Shader
    # Encoding: R = ID & 0xFF, G = (ID >> 8) & 0xFF, B = 0
    rgb_array = np.zeros((MAP_H, MAP_W, 3), dtype=np.uint8)
    rgb_array[:, :, 0] = (id_array & 0xFF).astype(np.uint8)
    rgb_array[:, :, 1] = ((id_array >> 8) & 0xFF).astype(np.uint8)
    png_img = Image.fromarray(rgb_array, mode='RGB')
    png_path = os.path.join(OUTPUT_DIR, "region_id_map.png")
    png_img.save(png_path, "PNG", optimize=False)
    print(f"[MAP] Saved region_id_map.png ({os.path.getsize(png_path)} bytes)")

    # 3. Export Binary database (PTRD)
    db_bin_path = os.path.join(STREAMING_DIR, "regions_database.bin")
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
    with open(db_bin_path, "wb") as f:
        f.write(out)
    print(f"[DB] Saved {len(regions)} regions into {db_bin_path} ({len(out)} bytes)")

def main():
    print("=== NATURAL EARTH 10M 4K BORDERS & REGION DATABASE BUILDER ===")
    c_lines_path = download_file('ne_10m_admin_0_boundary_lines_land', URLS['ne_10m_admin_0_boundary_lines_land'])
    s_lines_path = download_file('ne_10m_admin_1_states_provinces_lines', URLS['ne_10m_admin_1_states_provinces_lines'])
    c_poly_path = download_file('countries_poly', URLS['countries_poly'])
    s_poly_path = download_file('ne_10m_admin_1_states_provinces', URLS['ne_10m_admin_1_states_provinces'])

    WIDTH = 4096
    HEIGHT = 2048
    print(f"[TEXTURE] Rendering {WIDTH}x{HEIGHT} political borders texture...")

    img = Image.new('RGBA', (WIDTH, HEIGHT), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    print("  -> Loading 10m lines datasets...")
    with open(s_lines_path, 'r', encoding='utf-8') as f:
        s_lines_features = json.load(f)['features']
    with open(c_lines_path, 'r', encoding='utf-8') as f:
        c_lines_features = json.load(f)['features']

    print("  -> Drawing high-precision state & province borders (45,000+ segments)...")
    sc = render_lines(s_lines_features, draw, WIDTH, HEIGHT, color=(160, 225, 255, 175), line_width=1)
    print(f"     Rendered {sc} state border segments.")

    print("  -> Drawing high-precision national borders...")
    cc = render_lines(c_lines_features, draw, WIDTH, HEIGHT, color=(255, 235, 140, 240), line_width=2)
    print(f"     Rendered {cc} national border segments.")

    borders_png_path = os.path.join(OUTPUT_DIR, "4k_earth_borders.png")
    img.save(borders_png_path, "PNG", optimize=True)
    print(f"[TEXTURE] Saved borders overlay to {borders_png_path} ({os.path.getsize(borders_png_path)} bytes)")

    generate_regions_db_and_id_map(s_poly_path, c_poly_path)
    print("=== ALL ASSETS GENERATED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
