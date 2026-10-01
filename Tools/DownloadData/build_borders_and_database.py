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
    # Equirectangular projection
    x = (lon + 180.0) / 360.0 * width
    y = (90.0 - lat) / 180.0 * height
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
                # Check for anti-meridian wrapping
                if abs(p1[0] - p2[0]) > 180.0:
                    continue
                x1, y1 = latlon_to_xy(p1[0], p1[1], width, height)
                x2, y2 = latlon_to_xy(p2[0], p2[1], width, height)
                draw.line([(x1, y1), (x2, y2)], fill=color, width=line_width)

def generate_regions_db(states_path, countries_path):
    print("[DB] Generating regional database...")
    regions = []

    # Hash helper for pseudo-random yet consistent regional stats
    def get_stats(name, country, lat, lon):
        h = abs(hash(name + country)) % 1000000
        # Latitude influences climate and vegetation
        abs_lat = abs(lat)
        base_forest = max(5, int(80 - abs_lat * 1.1 + (h % 30)))
        base_forest = min(92, max(3, base_forest))

        base_minerals = (h * 7) % 75 + 10
        base_arable = max(5, int(70 - abs_lat * 0.7 + ((h // 10) % 35)))
        base_arable = min(88, max(5, base_arable))

        base_water = ((h * 13) % 70) + 15

        # Special region adjustments (e.g. deserts vs tropical)
        if abs_lat > 65: # Polar
            base_forest = max(2, base_forest // 4)
            base_arable = max(1, base_arable // 6)
            base_minerals = min(90, base_minerals + 20)
        elif 18 < abs_lat < 32 and (lon < 60 and lon > -20): # Sahara / Middle East
            base_forest = max(1, base_forest // 6)
            base_arable = max(2, base_arable // 4)
            base_minerals = min(95, base_minerals + 25)

        return {
            'forestPercent': base_forest,
            'mineralsPercent': base_minerals,
            'arablePercent': base_arable,
            'waterPercent': base_water
        }

    with open(states_path, 'r', encoding='utf-8') as f:
        states_data = json.load(f)

    for feat in states_data.get('features', []):
        props = feat.get('properties', {})
        name = props.get('name') or props.get('name_en') or props.get('admin') or 'Região'
        country = props.get('admin') or props.get('sov_a3') or ''
        region_type = props.get('type_en') or props.get('type') or 'Estado/Província'

        geom = feat.get('geometry', {})
        gtype = geom.get('type')
        coords = geom.get('coordinates', [])

        # Calculate bounding box and centroid
        min_lon, min_lat = 180, 90
        max_lon, max_lat = -180, -90
        sum_lon, sum_lat, pt_count = 0, 0, 0

        def traverse(pts):
            nonlocal min_lon, min_lat, max_lon, max_lat, sum_lon, sum_lat, pt_count
            for p in pts:
                if isinstance(p[0], (int, float)):
                    lon, lat = p[0], p[1]
                    if lon < min_lon: min_lon = lon
                    if lon > max_lon: max_lon = lon
                    if lat < min_lat: min_lat = lat
                    if lat > max_lat: max_lat = lat
                    sum_lon += lon
                    sum_lat += lat
                    pt_count += 1
                else:
                    traverse(p)

        traverse(coords)

        if pt_count > 0:
            center_lon = sum_lon / pt_count
            center_lat = sum_lat / pt_count
        else:
            center_lon = props.get('longitude', 0)
            center_lat = props.get('latitude', 0)

        stats = get_stats(name, country, center_lat, center_lon)

        regions.append({
            'name': name,
            'country': country,
            'type': region_type,
            'centerLat': round(center_lat, 4),
            'centerLon': round(center_lon, 4),
            'minLat': round(min_lat, 4),
            'maxLat': round(max_lat, 4),
            'minLon': round(min_lon, 4),
            'maxLon': round(max_lon, 4),
            'forestPercent': stats['forestPercent'],
            'mineralsPercent': stats['mineralsPercent'],
            'arablePercent': stats['arablePercent'],
            'waterPercent': stats['waterPercent']
        })

    # Also add countries from countries_poly
    with open(countries_path, 'r', encoding='utf-8') as f:
        c_data = json.load(f)

    for feat in c_data.get('features', []):
        props = feat.get('properties', {})
        name = props.get('NAME') or props.get('ADMIN') or 'País'
        country = name
        center_lat = props.get('LABEL_Y') or 0
        center_lon = props.get('LABEL_X') or 0

        stats = get_stats(name, country, center_lat, center_lon)
        regions.append({
            'name': name,
            'country': country,
            'type': 'País',
            'centerLat': round(center_lat, 4),
            'centerLon': round(center_lon, 4),
            'minLat': -85,
            'maxLat': 85,
            'minLon': -180,
            'maxLon': 180,
            'forestPercent': stats['forestPercent'],
            'mineralsPercent': stats['mineralsPercent'],
            'arablePercent': stats['arablePercent'],
            'waterPercent': stats['waterPercent']
        })

    out_file = os.path.join(STREAMING_DIR, "regions_database.json")
    with open(out_file, 'w', encoding='utf-8') as f:
        json.dump({'regions': regions}, f, ensure_ascii=False, indent=2)

    print(f"[DB] Saved {len(regions)} regions into {out_file}")

def main():
    print("=== NATURAL EARTH BORDERS & REGION DATABASE BUILDER ===")
    c_lines = download_file('countries_lines', URLS['countries_lines'])
    s_lines = download_file('states_lines', URLS['states_lines'])
    c_poly = download_file('countries_poly', URLS['countries_poly'])
    s_poly = download_file('states_poly', URLS['states_poly'])

    WIDTH = 4096
    HEIGHT = 2048
    print(f"[TEXTURE] Rendering {WIDTH}x{HEIGHT} political borders texture...")

    # Create transparent RGBA image
    img = Image.new('RGBA', (WIDTH, HEIGHT), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. State / Province boundaries: Subtle cyan/white (1px)
    print("  -> Drawing state & province borders...")
    render_lines(s_lines, draw, WIDTH, HEIGHT, color=(160, 225, 255, 175), line_width=1)

    # 2. Country boundaries: Crisp bright amber/gold (2px)
    print("  -> Drawing national country borders...")
    render_lines(c_lines, draw, WIDTH, HEIGHT, color=(255, 235, 140, 240), line_width=2)

    borders_png_path = os.path.join(OUTPUT_DIR, "4k_earth_borders.png")
    img.save(borders_png_path, "PNG", optimize=True)
    print(f"[TEXTURE] Saved borders overlay to {borders_png_path} ({os.path.getsize(borders_png_path)} bytes)")

    generate_regions_db(s_poly, c_poly)
    print("=== ALL ASSETS GENERATED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
