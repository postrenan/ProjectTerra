import os
import sys
import urllib.request
import time

BASE_URL = "https://www.solarsystemscope.com/textures/download/"
OUTPUT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Textures", "Earth"))

FILES_TO_DOWNLOAD = [
    # 2K fast preview / fallback
    "2k_earth_daymap.jpg",
    "2k_earth_specular_map.tif",
    "2k_earth_normal_map.tif",
    # 8K full high-fidelity
    "8k_earth_daymap.jpg",
    "8k_earth_specular_map.tif",
    "8k_earth_normal_map.tif"
]

def download_file(filename):
    target_path = os.path.join(OUTPUT_DIR, filename)
    if os.path.exists(target_path) and os.path.getsize(target_path) > 10000:
        print(f"[SKIP] {filename} already exists ({os.path.getsize(target_path)} bytes).")
        return

    url = BASE_URL + filename
    print(f"[DOWNLOADING] {filename} from {url}...")
    headers = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'}
    req = urllib.request.Request(url, headers=headers)
    
    start_time = time.time()
    with urllib.request.urlopen(req, timeout=30) as resp:
        total_size = int(resp.headers.get('Content-Length', 0))
        downloaded = 0
        chunk_size = 64 * 1024
        
        with open(target_path, 'wb') as f:
            while True:
                chunk = resp.read(chunk_size)
                if not chunk:
                    break
                f.write(chunk)
                downloaded += len(chunk)
                if total_size > 0:
                    percent = (downloaded / total_size) * 100
                    mb = downloaded / (1024 * 1024)
                    sys.stdout.write(f"\r  -> {percent:5.1f}% ({mb:5.2f} MB)")
                    sys.stdout.flush()
        
    duration = time.time() - start_time
    total_mb = downloaded / (1024 * 1024)
    print(f"\n[DONE] {filename} saved ({total_mb:.2f} MB in {duration:.1f}s)")

def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    print(f"Target Directory: {OUTPUT_DIR}")
    for f in FILES_TO_DOWNLOAD:
        download_file(f)
    print("\nAll datasets downloaded successfully!")

if __name__ == "__main__":
    main()
