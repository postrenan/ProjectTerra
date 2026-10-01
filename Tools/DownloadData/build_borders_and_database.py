import os
import json
import urllib.request
import math
from PIL import Image, ImageDraw

CACHE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Temp", "GeoCache"))
OUTPUT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Textures", "Earth"))
STREAMING_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "StreamingAssets"))

os.makedirs(CACHE_DIR, exist_ok=True)
os.makedirs(OUTPUT_DIR, exist_ok=True)
os.makedirs(STREAMING_DIR, exist_ok=True)

URLS = {
    'countries_lines': 'https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_50m_admin_0_boundary_lines_land.geojson',
    'states_lines': 'https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_50m_admin_1_states_provinces_lines.geojson',
    'countries_poly': 'https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_50m_admin_0_countries.geojson',
    'states_poly': 'https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_50m_admin_1_states_provinces.geojson'
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

def render_lines(geojson_path, draw, width, height, color, line_width):
    with open(geojson_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    for feature in data.get('features', []):
        geom = feature.get('geometry', {})
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
                    pts = []
                    continue

                x1, y1 = latlon_to_xy(p1[0], p1[1], width, height)
                x2, y2 = latlon_to_xy(p2[0], p2[1], width, height)
                if not pts:
                    pts.append((x1, y1))
                pts.append((x2, y2))

            if len(pts) > 1:
                draw.line(pts, fill=color, width=line_width)

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

def generate_regions_db_and_id_map(states_path, countries_path):
    print("[DB] Generating regions database and raster ID map...")

    with open(countries_path, 'r', encoding='utf-8') as f:
        c_feats = json.load(f)['features']
    with open(states_path, 'r', encoding='utf-8') as f:
        s_feats = json.load(f)['features']

    regions = []

    # 1. Countries
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

    # 2. States / Provinces
    for feat in s_feats:
        p = feat.get('properties', {})
        name = p.get('name') or p.get('name_en') or 'Estado'
        country = p.get('admin') or p.get('sov_a3') or 'País'
        stype = p.get('type_en') or p.get('type') or 'Estado'
        clat = p.get('latitude') or 0.0
        clon = p.get('longitude') or 0.0
        stats = get_stats(name, country, clat, clon)
        regions.append({
            'id': len(regions) + 1,
            'name': name,
            'country': country,
            'type': stype,
            'centerLat': round(float(clat), 4),
            'centerLon': round(float(clon), 4),
            'forestPercent': stats['forestPercent'],
            'mineralsPercent': stats['mineralsPercent'],
            'arablePercent': stats['arablePercent'],
            'waterPercent': stats['waterPercent']
        })

    # Raster ID Map (2048 x 1024, 16-bit ushort)
    MAP_W = 2048
    MAP_H = 1024
    img = Image.new('I', (MAP_W, MAP_H), 0)
    draw = ImageDraw.Draw(img)

    for i, feat in enumerate(c_feats):
        cid = i + 1
        geom = feat.get('geometry', {})
        coords = geom.get('coordinates', [])
        polys = [coords] if geom.get('type') == 'Polygon' else (coords if geom.get('type') == 'MultiPolygon' else [])
        for poly in polys:
            if not poly or len(poly[0]) < 3: continue
            pts = [latlon_to_xy(pt[0], pt[1], MAP_W, MAP_H) for pt in poly[0]]
            draw.polygon(pts, fill=cid)

    state_offset = len(c_feats)
    for i, feat in enumerate(s_feats):
        cid = state_offset + i + 1
        geom = feat.get('geometry', {})
        coords = geom.get('coordinates', [])
        polys = [coords] if geom.get('type') == 'Polygon' else (coords if geom.get('type') == 'MultiPolygon' else [])
        for poly in polys:
            if not poly or len(poly[0]) < 3: continue
            pts = [latlon_to_xy(pt[0], pt[1], MAP_W, MAP_H) for pt in poly[0]]
            draw.polygon(pts, fill=cid)

    # Ensure small islands / micro-nations are clickable
    for r in regions:
        cx, cy = latlon_to_xy(r['centerLon'], r['centerLat'], MAP_W, MAP_H)
        draw.rectangle([cx-1, cy-1, cx+1, cy+1], fill=r['id'])

    # Export 16-bit raw binary
    raw_data = bytearray(MAP_W * MAP_H * 2)
    for y in range(MAP_H):
        for x in range(MAP_W):
            val = img.getpixel((x, y))
            idx = (y * MAP_W + x) * 2
            raw_data[idx] = val & 0xFF
            raw_data[idx + 1] = (val >> 8) & 0xFF

    bin_path = os.path.join(STREAMING_DIR, "region_id_map.bin")
    with open(bin_path, "wb") as f:
        f.write(raw_data)
    print(f"[MAP] Saved region_id_map.bin ({os.path.getsize(bin_path)} bytes)")

    db_path = os.path.join(STREAMING_DIR, "regions_database.json")
    with open(db_path, "w", encoding='utf-8') as f:
        json.dump({'regions': regions}, f, ensure_ascii=False, indent=2)
    print(f"[DB] Saved {len(regions)} regions into {db_path} ({os.path.getsize(db_path)} bytes)")

def main():
    print("=== NATURAL EARTH BORDERS & REGION DATABASE BUILDER ===")
    c_lines = download_file('countries_lines', URLS['countries_lines'])
    s_lines = download_file('states_lines', URLS['states_lines'])
    c_poly = download_file('countries_poly', URLS['countries_poly'])
    s_poly = download_file('states_poly', URLS['states_poly'])

    WIDTH = 4096
    HEIGHT = 2048
    print(f"[TEXTURE] Rendering {WIDTH}x{HEIGHT} political borders texture...")

    img = Image.new('RGBA', (WIDTH, HEIGHT), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    print("  -> Drawing state & province borders...")
    render_lines(s_lines, draw, WIDTH, HEIGHT, color=(160, 225, 255, 175), line_width=1)

    print("  -> Drawing national country borders...")
    render_lines(c_lines, draw, WIDTH, HEIGHT, color=(255, 235, 140, 240), line_width=2)

    borders_png_path = os.path.join(OUTPUT_DIR, "4k_earth_borders.png")
    img.save(borders_png_path, "PNG", optimize=True)
    print(f"[TEXTURE] Saved borders overlay to {borders_png_path} ({os.path.getsize(borders_png_path)} bytes)")

    generate_regions_db_and_id_map(s_poly, c_poly)
    print("=== ALL ASSETS GENERATED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
