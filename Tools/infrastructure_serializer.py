import os
import json
import struct

def save_infrastructure_json(infrastructure_list, json_out_path):
    with open(json_out_path, "w", encoding="utf-8") as f:
        json.dump({"regions": infrastructure_list}, f, indent=2, ensure_ascii=False)
    print(f"[OUT] Saved JSON to {json_out_path} ({os.path.getsize(json_out_path)} bytes)")

def save_infrastructure_bin(infrastructure_list, bin_out_path):
    out = bytearray()
    out.extend(b'PTIN')
    out.extend(struct.pack('<HI', 1, len(infrastructure_list)))

    def pack_str(s):
        b = (s or '').encode('utf-8')
        return struct.pack('<H', len(b)) + b

    for item in infrastructure_list:
        out.extend(struct.pack('<i', item['regionId']))
        out.extend(struct.pack('<10B',
            1 if item['hasCoastline'] else 0,
            1 if item['hasRivers'] else 0,
            1 if item['hasLakes'] else 0,
            1 if item['hasPort'] else 0,
            1 if item['hasAirport'] else 0,
            1 if item['hasRoadNetwork'] else 0,
            1 if item['isAgricultureViable'] else 0,
            1 if item['isFishingViable'] else 0,
            1 if item['isAviationViable'] else 0,
            1 if item['isTruckingViable'] else 0
        ))
        out.extend(pack_str(item['waterBodyName']))
        out.extend(pack_str(item['portName']))
        out.extend(pack_str(item['airportName']))
        out.extend(pack_str(item['environmentCategory']))
        out.extend(pack_str(item['agricultureReason']))
        out.extend(pack_str(item['fishingReason']))
        out.extend(pack_str(item['aviationReason']))
        out.extend(pack_str(item['truckingReason']))

    with open(bin_out_path, "wb") as f:
        f.write(out)
    print(f"[OUT] Saved Binary to {bin_out_path} ({os.path.getsize(bin_out_path)} bytes)")
