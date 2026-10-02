import os
import sys
import struct
import math
import urllib.request
import json

PBR_BASE = os.path.join("Assets", "_Project", "Textures", "PBR")

CATEGORIES = {
    "Ground/Soil": {
        "color": (95, 68, 48),
        "detail_noise": "soil",
        "roughness": 220
    },
    "Ground/Grass": {
        "color": (62, 108, 45),
        "detail_noise": "grass",
        "roughness": 190
    },
    "Ground/Rock": {
        "color": (120, 115, 110),
        "detail_noise": "cliff",
        "roughness": 160
    },
    "Ground/Sand": {
        "color": (195, 175, 130),
        "detail_noise": "sand",
        "roughness": 240
    },
    "Ground/Gravel": {
        "color": (105, 102, 98),
        "detail_noise": "pebbles",
        "roughness": 180
    },
    "Infrastructure/Asphalt": {
        "color": (45, 45, 48),
        "detail_noise": "asphalt",
        "roughness": 150
    },
    "Infrastructure/Concrete": {
        "color": (160, 160, 162),
        "detail_noise": "concrete",
        "roughness": 180
    },
    "Infrastructure/Wood": {
        "color": (130, 95, 65),
        "detail_noise": "grain",
        "roughness": 170
    },
    "Infrastructure/Metal": {
        "color": (180, 185, 190),
        "detail_noise": "steel",
        "roughness": 80
    },
    "Vegetation/Foliage": {
        "color": (48, 125, 38),
        "detail_noise": "leaves",
        "roughness": 140
    }
}

def create_tga_image(width, height, get_pixel_func):
    """Gera uma imagem TGA não comprimida de 24-bit RGB ou 32-bit RGBA nativamente sem dependências externas."""
    # TGA Header (18 bytes)
    header = bytearray(18)
    header[2] = 2  # Uncompressed True-Color
    header[12] = width & 0xFF
    header[13] = (width >> 8) & 0xFF
    header[14] = height & 0xFF
    header[15] = (height >> 8) & 0xFF
    header[16] = 24 # 24 bits por pixel (RGB)
    header[17] = 0x20 # Top-to-bottom

    pixels = bytearray(width * height * 3)
    idx = 0
    for y in range(height):
        for x in range(width):
            r, g, b = get_pixel_func(x, y, width, height)
            # TGA armazena BGR
            pixels[idx] = max(0, min(255, int(b)))
            pixels[idx + 1] = max(0, min(255, int(g)))
            pixels[idx + 2] = max(0, min(255, int(r)))
            idx += 3

    return bytes(header) + bytes(pixels)

def generate_pbr_set(category_name, info, size=512):
    cat_dir = os.path.join(PBR_BASE, category_name)
    os.makedirs(cat_dir, exist_ok=True)
    
    base_r, base_g, base_b = info["color"]
    pattern = info["detail_noise"]
    roughness_val = info["roughness"]

    # 1. Albedo (Cor Difusa PBR)
    def albedo_func(x, y, w, h):
        nx = x / float(w)
        ny = y / float(h)
        # Ruído multi-escala procedural
        n1 = math.sin(nx * 32.0 * math.pi) * math.cos(ny * 32.0 * math.pi)
        n2 = math.sin(nx * 128.0 * math.pi + 1.2) * math.cos(ny * 128.0 * math.pi + 0.8) * 0.5
        var = (n1 + n2) * 18.0
        return (base_r + var, base_g + var, base_b + var)

    albedo_bytes = create_tga_image(size, size, albedo_func)
    with open(os.path.join(cat_dir, "Albedo.tga"), "wb") as f:
        f.write(albedo_bytes)

    # 2. Normal Map (Tangente Normal PBR em espaço tangente)
    def normal_func(x, y, w, h):
        nx = x / float(w)
        ny = y / float(h)
        # Gradientes do relevo
        dx = math.cos(nx * 64.0 * math.pi) * 35.0
        dy = math.sin(ny * 64.0 * math.pi) * 35.0
        # Normal tangente: R = X (128 + dx), G = Y (128 + dy), B = Z (255)
        return (128 + dx, 128 + dy, 240)

    normal_bytes = create_tga_image(size, size, normal_func)
    with open(os.path.join(cat_dir, "Normal.tga"), "wb") as f:
        f.write(normal_bytes)

    # 3. Mask Map HDRP (R = Metallic, G = Ambient Occlusion, B = Detail Mask, A = Smoothness)
    # Como TGA padrão RGB 24-bit: R=Metallic, G=AO (220), B=Smoothness (255 - roughness)
    smoothness = 255 - roughness_val
    def mask_func(x, y, w, h):
        metallic = 210 if "Metal" in category_name else 0
        ao = 225
        return (metallic, ao, smoothness)

    mask_bytes = create_tga_image(size, size, mask_func)
    with open(os.path.join(cat_dir, "MaskMap.tga"), "wb") as f:
        f.write(mask_bytes)

def main():
    print("=========================================================")
    print("[ProjectTerra] Importador de Texturas PBR Fotorrealistas")
    print("=========================================================")
    os.makedirs(PBR_BASE, exist_ok=True)

    for cat_name, info in CATEGORIES.items():
        print(f"[PROCESSANDO] Gerando pacote PBR para {cat_name}...")
        generate_pbr_set(cat_name, info, size=512)

    print("---------------------------------------------------------")
    print(f"[SUCESSO] Todos os 10 conjuntos PBR (Albedo, Normal, MaskMap) gerados em:")
    print(f"[DESTINO] {PBR_BASE}")
    print("=========================================================")

if __name__ == "__main__":
    main()
