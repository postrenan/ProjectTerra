import os
import numpy as np

from infrastructure_features import (
    load_regions,
    detect_coastal_regions,
    load_airports,
    load_ports,
    load_rivers,
    load_lakes
)
from infrastructure_viability import evaluate_region_infrastructure
from infrastructure_serializer import save_infrastructure_json, save_infrastructure_bin

STREAMING_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Assets", "StreamingAssets"))
CACHE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Temp", "GeoCache"))
BACKUP_DB_PATH = os.path.abspath(os.path.join(os.path.dirname(__file__), "DataBackup", "regions_database.json"))

def verify_samples(infrastructure_list):
    print("\n=== VERIFICATION OF SAMPLE REGIONS ===")
    test_ids = [1668, 837, 251, 2053, 240, 264, 1537, 4598]
    for tid in test_ids:
        match = next((item for item in infrastructure_list if item["regionId"] == tid), None)
        if match:
            print(f"[{match['regionId']}] {match['regionName']} ({match['countryName']}) - {match['environmentCategory']}:")
            print(f"     [Agri]: {match['isAgricultureViable']} ({match['agricultureReason']})")
            print(f"     [Fish]: {match['isFishingViable']} ({match['fishingReason']})")
            print(f"     [Air]:  {match['isAviationViable']} | Port: {match['portName']} | Airp: {match['airportName']}")

def main():
    print("=== PROJECT TERRA: REGION INFRASTRUCTURE GENERATOR ===")
    regions = load_regions(BACKUP_DB_PATH)
    print(f"[INFO] Loaded {len(regions)} regions from database.")

    # 1. Load raster ID map (4096 x 2048)
    id_map_path = os.path.join(STREAMING_DIR, "region_id_map.bin")
    id_map = np.fromfile(id_map_path, dtype=np.uint16).reshape((2048, 4096))
    map_h, map_w = id_map.shape

    # 2. Extract coastal and GeoJSON features
    coastal_ids = detect_coastal_regions(id_map)
    airport_map = load_airports(CACHE_DIR, id_map, map_w, map_h)
    port_map = load_ports(CACHE_DIR, id_map, map_w, map_h)
    river_map = load_rivers(CACHE_DIR, id_map, map_w, map_h)
    lake_map = load_lakes(CACHE_DIR, id_map, map_w, map_h)

    print("  -> Evaluating geographic conditions and career viability...")
    infrastructure_list = [
        evaluate_region_infrastructure(r, coastal_ids, river_map, lake_map, port_map, airport_map)
        for r in regions
    ]

    # Output JSON and BIN
    json_out_path = os.path.join(STREAMING_DIR, "regions_infrastructure.json")
    save_infrastructure_json(infrastructure_list, json_out_path)

    bin_out_path = os.path.join(STREAMING_DIR, "regions_infrastructure.bin")
    save_infrastructure_bin(infrastructure_list, bin_out_path)

    verify_samples(infrastructure_list)

if __name__ == "__main__":
    main()
