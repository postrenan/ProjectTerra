import os
import urllib.request

# Datasets vetoriais do Natural Earth 10m para a malha de transporte e cidades.
# Rodovias principais, ferrovias e localidades povoadas (com POP_MAX para ranqueamento econômico).
CACHE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Temp", "GeoCache"))

URLS = {
    "ne_10m_roads": "https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_10m_roads.geojson",
    "ne_10m_railroads": "https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_10m_railroads.geojson",
    "ne_10m_populated_places": "https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_10m_populated_places.geojson",
    "ne_10m_rivers_lake_centerlines": "https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_10m_rivers_lake_centerlines.geojson",
}


def download_file(name, url):
    os.makedirs(CACHE_DIR, exist_ok=True)
    dest = os.path.join(CACHE_DIR, f"{name}.geojson")
    if os.path.exists(dest) and os.path.getsize(dest) > 10000:
        print(f"[CACHE] {name}.geojson ja existe ({os.path.getsize(dest)} bytes)")
        return dest
    print(f"[DOWNLOAD] {name} <- {url}")
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=120) as resp:
        content = resp.read()
    with open(dest, "wb") as f:
        f.write(content)
    print(f"[DONE] {dest} ({len(content)} bytes)")
    return dest


def main():
    print("=== NATURAL EARTH 10M: ESTRADAS, FERROVIAS E CIDADES ===")
    for name, url in URLS.items():
        download_file(name, url)
    print("=== DOWNLOADS CONCLUIDOS ===")


if __name__ == "__main__":
    main()
