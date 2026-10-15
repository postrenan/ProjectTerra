"""
Gerador de Texturas PBR de Alta Definição e Realismo para Modelos Modulares de Fazenda e Cidade.
Gera mapas:
- BaseColor / Albedo (RGBA ou RGB)
- Normal Map (Tangente espacial, vetores normalizados)
- Roughness (Tons de cinza)
- Metallic (Tons de cinza)
- Ambient Occlusion (Oclusão de fendas e juntas)
"""

import os
import math
import numpy as np
from PIL import Image, ImageFilter

FARM_TEX_DIR = r"E:\Backup-C\Projetos\testejogo\Assets\_Project\Textures\ModularBuildings\Farm"
CITY_TEX_DIR = r"E:\Backup-C\Projetos\testejogo\Assets\_Project\Textures\ModularBuildings\City"

def height_to_normal(height_map, strength=2.0):
    """Calcula normal map espacial tangente a partir de height map usando filtros Sobel."""
    h, w = height_map.shape
    # Gradiente em X e Y usando numpy gradient
    gy, gx = np.gradient(height_map.astype(np.float32) / 255.0)
    gx = gx * strength
    gy = -gy * strength # DirectX / Unity padrão
    gz = np.ones_like(gx)
    
    norm = np.sqrt(gx**2 + gy**2 + gz**2)
    norm[norm == 0] = 1.0
    
    nx = (gx / norm * 0.5 + 0.5) * 255.0
    ny = (gy / norm * 0.5 + 0.5) * 255.0
    nz = (gz / norm * 0.5 + 0.5) * 255.0
    
    normal_rgb = np.stack([nx, ny, nz], axis=2).astype(np.uint8)
    return Image.fromarray(normal_rgb, mode='RGB')

def generate_barn_wood(size=2048):
    print("Gerando texturas de Madeira Rústica de Celeiro (Barn Wood)...")
    # Número de tábuas verticais
    num_planks = 16
    plank_w = size // num_planks
    
    # Grid de coordenadas
    y, x = np.mgrid[0:size, 0:size]
    
    # 1. Base planks e sulcos
    plank_id = x // plank_w
    plank_local_x = (x % plank_w).astype(np.float32) / plank_w
    
    # Sulco entre tábuas (groove)
    groove_mask = (plank_local_x < 0.04) | (plank_local_x > 0.96)
    
    # Variação de cor por tábua
    np.random.seed(42)
    plank_tint = np.random.uniform(0.75, 1.25, num_planks)
    tint_map = plank_tint[plank_id]
    
    # Veios de madeira (frequência alta vertical, variação lenta horizontal)
    noise_wood = np.sin(x * 0.2 + np.sin(y * 0.015) * 20.0) * 0.5 + 0.5
    grain_fine = (np.random.normal(0.5, 0.08, (size, size))).clip(0, 1)
    
    # Altura
    height = (noise_wood * 0.3 + grain_fine * 0.3 + 0.4) * 255.0
    height[groove_mask] *= 0.15
    height = height.clip(0, 255).astype(np.uint8)
    
    # Albedo: Marrom castanho envelhecido / celeiro rústico com intempéries
    base_r = 135 * tint_map * (0.8 + 0.2 * noise_wood) * grain_fine
    base_g = 88 * tint_map * (0.8 + 0.2 * noise_wood) * grain_fine
    base_b = 58 * tint_map * (0.8 + 0.2 * noise_wood) * grain_fine
    
    # Aplicar sulco escuro
    base_r[groove_mask] *= 0.25
    base_g[groove_mask] *= 0.25
    base_b[groove_mask] *= 0.25
    
    # Sujeira e desgastes nas pontas superior/inferior
    vignette = 1.0 - 0.2 * ((y.astype(np.float32)/size - 0.5)**4 * 16.0).clip(0, 1)
    base_r *= vignette
    base_g *= vignette
    base_b *= vignette
    
    albedo = np.stack([base_r.clip(0, 255), base_g.clip(0, 255), base_b.clip(0, 255)], axis=2).astype(np.uint8)
    img_albedo = Image.fromarray(albedo, mode='RGB')
    
    # Normal map
    img_normal = height_to_normal(height, strength=3.5)
    
    # Roughness: Madeira rústica é fosca (0.7 a 0.95), sulcos mais ásperos
    roughness = (0.75 + noise_wood * 0.15 + (1.0 - grain_fine) * 0.1).clip(0, 1) * 255.0
    roughness[groove_mask] = 245
    img_roughness = Image.fromarray(roughness.astype(np.uint8), mode='L')
    
    # Metallic: Dielétrico puro (0)
    img_metallic = Image.new('L', (size, size), 0)
    
    # AO
    ao = np.ones((size, size), dtype=np.uint8) * 255
    ao[groove_mask] = 60
    img_ao = Image.fromarray(ao, mode='L').filter(ImageFilter.GaussianBlur(radius=2))
    
    # Salvar
    img_albedo.save(os.path.join(FARM_TEX_DIR, "T_Barn_Wood_Albedo.png"))
    img_normal.save(os.path.join(FARM_TEX_DIR, "T_Barn_Wood_Normal.png"))
    img_roughness.save(os.path.join(FARM_TEX_DIR, "T_Barn_Wood_Roughness.png"))
    img_metallic.save(os.path.join(FARM_TEX_DIR, "T_Barn_Wood_Metallic.png"))
    img_ao.save(os.path.join(FARM_TEX_DIR, "T_Barn_Wood_AO.png"))
    print("Barn Wood salvo!")

def generate_corrugated_metal(size=2048):
    print("Gerando texturas de Chapa Metálica Ondulada / Telhado Galvanizado...")
    y, x = np.mgrid[0:size, 0:size]
    
    # Ondas senoidais com período de ~64 pixels
    period = 64.0
    wave = (np.sin(x * (2 * math.pi / period)) * 0.5 + 0.5)
    
    # Parafusos hexagonais/rebites distribuídos nas cristas a cada 256 pixels
    rivet_mask = np.zeros((size, size), dtype=bool)
    for ry in range(128, size, 384):
        for rx in range(32, size, int(period * 2)):
            dist = np.sqrt((y - ry)**2 + (x - rx)**2)
            rivet_mask |= (dist < 10)
            
    # Micro ruído de metal galvanizado (cristais de zinco / spangle)
    zinc_spangle = (np.sin(x * 0.3) * np.cos(y * 0.3) + np.sin(x * 0.7 + y * 0.5)) * 0.25 + 0.5
    grain = np.random.normal(0.5, 0.05, (size, size)).clip(0, 1)
    
    # Manchas sutis de oxidação / ferrugem alaranjada em partes das depressões
    rust_mask = ((wave < 0.25) & (np.random.uniform(0, 1, (size, size)) > 0.85))
    rust_mask = Image.fromarray((rust_mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(radius=6))
    rust_val = np.array(rust_mask).astype(np.float32) / 255.0
    
    # Height map
    height = (wave * 0.8 + zinc_spangle * 0.1 + grain * 0.1) * 220.0
    height[rivet_mask] += 35
    height = height.clip(0, 255).astype(np.uint8)
    
    # Albedo
    # Metal galvanizado: cinza claro azulado/neutro (180, 185, 190)
    base_r = (180 * zinc_spangle * grain) * (1.0 - rust_val) + (160 * rust_val)
    base_g = (186 * zinc_spangle * grain) * (1.0 - rust_val) + (85 * rust_val)
    base_b = (192 * zinc_spangle * grain) * (1.0 - rust_val) + (35 * rust_val)
    
    albedo = np.stack([base_r.clip(0, 255), base_g.clip(0, 255), base_b.clip(0, 255)], axis=2).astype(np.uint8)
    img_albedo = Image.fromarray(albedo, mode='RGB')
    
    # Normal map
    img_normal = height_to_normal(height, strength=4.0)
    
    # Metallic: Metal alto (~0.9), oxidação baixa (~0.1)
    metallic = (230 * (1.0 - rust_val)).clip(0, 255).astype(np.uint8)
    img_metallic = Image.fromarray(metallic, mode='L')
    
    # Roughness: Metal galvanizado semi-polido (0.35), ferrugem fosca (0.85)
    roughness = (90 * (1.0 - rust_val) + 215 * rust_val).clip(0, 255).astype(np.uint8)
    img_roughness = Image.fromarray(roughness, mode='L')
    
    # AO
    ao = (255 * (0.6 + 0.4 * wave)).clip(0, 255).astype(np.uint8)
    img_ao = Image.fromarray(ao, mode='L')
    
    img_albedo.save(os.path.join(FARM_TEX_DIR, "T_Corrugated_Metal_Albedo.png"))
    img_normal.save(os.path.join(FARM_TEX_DIR, "T_Corrugated_Metal_Normal.png"))
    img_roughness.save(os.path.join(FARM_TEX_DIR, "T_Corrugated_Metal_Roughness.png"))
    img_metallic.save(os.path.join(FARM_TEX_DIR, "T_Corrugated_Metal_Metallic.png"))
    img_ao.save(os.path.join(FARM_TEX_DIR, "T_Corrugated_Metal_AO.png"))
    print("Corrugated Metal salvo!")

def generate_farm_props_texture(size=2048):
    print("Gerando texturas PBR para Objetos de Fazenda (Feno, Barril, Paletes, Comedouros)...")
    # Atlas de 4 quadrantes para props rurais:
    # Top-Left: Palha / Feno dourado (Hay Bale)
    # Top-Right: Carvalho tratado de barril com aros de ferro
    # Bottom-Left: Madeira serrada de paletes e caixotes
    # Bottom-Right: Madeira escura/comedouro e detalhes de ferro forjado
    half = size // 2
    
    albedo = np.zeros((size, size, 3), dtype=np.uint8)
    height = np.zeros((size, size), dtype=np.uint8)
    roughness = np.zeros((size, size), dtype=np.uint8)
    metallic = np.zeros((size, size), dtype=np.uint8)
    
    # 1. Hay Bale (Top-Left)
    y1, x1 = np.mgrid[0:half, 0:half]
    strands = (np.sin(x1 * 0.8 + np.sin(y1 * 0.3) * 6.0) + np.sin(y1 * 1.2)) * 0.25 + 0.5
    grain_hay = np.random.normal(0.5, 0.1, (half, half)).clip(0, 1)
    hay_r = (215 * strands * grain_hay + 40).clip(0, 255)
    hay_g = (175 * strands * grain_hay + 30).clip(0, 255)
    hay_b = (70 * strands * grain_hay + 15).clip(0, 255)
    albedo[0:half, 0:half, 0] = hay_r
    albedo[0:half, 0:half, 1] = hay_g
    albedo[0:half, 0:half, 2] = hay_b
    height[0:half, 0:half] = ((strands * 0.6 + grain_hay * 0.4) * 255).astype(np.uint8)
    roughness[0:half, 0:half] = 245
    metallic[0:half, 0:half] = 0
    
    # 2. Barrel Oak & Iron Bands (Top-Right)
    y2, x2 = np.mgrid[0:half, 0:half]
    band_mask = ((y2 > 40) & (y2 < 90)) | ((y2 > 420) & (y2 < 470)) | ((y2 > 700) & (y2 < 750)) | ((y2 > 930) & (y2 < 980))
    oak_grain = (np.sin(x2 * 0.15) * 0.2 + np.random.normal(0.5, 0.05, (half, half))).clip(0, 1)
    
    oak_r = (145 * oak_grain).clip(0, 255)
    oak_g = (95 * oak_grain).clip(0, 255)
    oak_b = (55 * oak_grain).clip(0, 255)
    # Aros de ferro escuro
    oak_r[band_mask] = 45
    oak_g[band_mask] = 48
    oak_b[band_mask] = 52
    
    albedo[0:half, half:size, 0] = oak_r
    albedo[0:half, half:size, 1] = oak_g
    albedo[0:half, half:size, 2] = oak_b
    
    h_oak = (oak_grain * 200).astype(np.uint8)
    h_oak[band_mask] = 250
    height[0:half, half:size] = h_oak
    
    r_oak = np.ones((half, half), dtype=np.uint8) * 180
    r_oak[band_mask] = 90
    roughness[0:half, half:size] = r_oak
    
    m_oak = np.zeros((half, half), dtype=np.uint8)
    m_oak[band_mask] = 220
    metallic[0:half, half:size] = m_oak
    
    # 3. Pine Pallet Wood (Bottom-Left)
    y3, x3 = np.mgrid[0:half, 0:half]
    pine_grain = np.random.normal(0.5, 0.06, (half, half)).clip(0, 1)
    pine_r = (190 * pine_grain + 20).clip(0, 255)
    pine_g = (155 * pine_grain + 15).clip(0, 255)
    pine_b = (105 * pine_grain + 10).clip(0, 255)
    albedo[half:size, 0:half, 0] = pine_r
    albedo[half:size, 0:half, 1] = pine_g
    albedo[half:size, 0:half, 2] = pine_b
    height[half:size, 0:half] = (pine_grain * 220).astype(np.uint8)
    roughness[half:size, 0:half] = 200
    metallic[half:size, 0:half] = 0
    
    # 4. Galvanized / Dark Wood / Trough (Bottom-Right)
    y4, x4 = np.mgrid[0:half, 0:half]
    metal_dark = np.random.normal(0.5, 0.05, (half, half)).clip(0, 1)
    albedo[half:size, half:size, 0] = (80 * metal_dark).clip(0, 255)
    albedo[half:size, half:size, 1] = (85 * metal_dark).clip(0, 255)
    albedo[half:size, half:size, 2] = (90 * metal_dark).clip(0, 255)
    height[half:size, half:size] = (metal_dark * 180).astype(np.uint8)
    roughness[half:size, half:size] = 130
    metallic[half:size, half:size] = 200
    
    img_albedo = Image.fromarray(albedo, mode='RGB')
    img_normal = height_to_normal(height, strength=3.0)
    img_roughness = Image.fromarray(roughness, mode='L')
    img_metallic = Image.fromarray(metallic, mode='L')
    
    img_albedo.save(os.path.join(FARM_TEX_DIR, "T_Farm_Props_Albedo.png"))
    img_normal.save(os.path.join(FARM_TEX_DIR, "T_Farm_Props_Normal.png"))
    img_roughness.save(os.path.join(FARM_TEX_DIR, "T_Farm_Props_Roughness.png"))
    img_metallic.save(os.path.join(FARM_TEX_DIR, "T_Farm_Props_Metallic.png"))
    print("Farm Props salvo!")

def generate_city_brick(size=2048):
    print("Gerando texturas de Tijolo Urbano e Alvenaria (City Brick Facade)...")
    # Padrão amarração inglesa/estilo residencial urbano
    brick_h = 64
    brick_w = 128
    mortar = 8
    
    y, x = np.mgrid[0:size, 0:size]
    row = y // brick_h
    # Alternar offset a cada linha
    offset_x = (row % 2) * (brick_w // 2)
    col = (x + offset_x) // brick_w
    
    in_brick_y = (y % brick_h)
    in_brick_x = ((x + offset_x) % brick_w)
    
    is_mortar = (in_brick_y < mortar) | (in_brick_x < mortar)
    
    np.random.seed(101)
    brick_seeds = np.random.uniform(0.7, 1.3, (size // brick_h + 2, size // brick_w + 3))
    
    # Map tint
    tint = np.zeros((size, size), dtype=np.float32)
    for r in range(size // brick_h):
        off = (r % 2) * (brick_w // 2)
        for c in range(size // brick_w + 1):
            x_start = max(0, c * brick_w - off)
            x_end = min(size, (c + 1) * brick_w - off)
            y_start = r * brick_h
            y_end = min(size, (r + 1) * brick_h)
            if x_end > x_start:
                tint[y_start:y_end, x_start:x_end] = brick_seeds[r, c]
                
    noise_porosity = np.random.normal(0.5, 0.08, (size, size)).clip(0, 1)
    
    # Tijolos vermelhos e terracota
    base_r = (165 * tint * noise_porosity + 20).clip(0, 255)
    base_g = (70 * tint * noise_porosity + 10).clip(0, 255)
    base_b = (50 * tint * noise_porosity + 10).clip(0, 255)
    
    # Argamassa cinza clara
    mortar_color = (190 * noise_porosity + 10).clip(0, 255)
    base_r[is_mortar] = mortar_color[is_mortar]
    base_g[is_mortar] = mortar_color[is_mortar]
    base_b[is_mortar] = mortar_color[is_mortar]
    
    albedo = np.stack([base_r, base_g, base_b], axis=2).astype(np.uint8)
    img_albedo = Image.fromarray(albedo, mode='RGB')
    
    # Relevo
    height = (210 * noise_porosity).clip(0, 255).astype(np.uint8)
    height[is_mortar] = 60
    
    img_normal = height_to_normal(height, strength=4.0)
    
    roughness = (215 * noise_porosity + 30).clip(0, 255).astype(np.uint8)
    roughness[is_mortar] = 240
    img_roughness = Image.fromarray(roughness, mode='L')
    
    img_metallic = Image.new('L', (size, size), 0)
    
    ao = np.ones((size, size), dtype=np.uint8) * 255
    ao[is_mortar] = 80
    img_ao = Image.fromarray(ao, mode='L').filter(ImageFilter.GaussianBlur(radius=2))
    
    img_albedo.save(os.path.join(CITY_TEX_DIR, "T_City_Brick_Albedo.png"))
    img_normal.save(os.path.join(CITY_TEX_DIR, "T_City_Brick_Normal.png"))
    img_roughness.save(os.path.join(CITY_TEX_DIR, "T_City_Brick_Roughness.png"))
    img_metallic.save(os.path.join(CITY_TEX_DIR, "T_City_Brick_Metallic.png"))
    img_ao.save(os.path.join(CITY_TEX_DIR, "T_City_Brick_AO.png"))
    print("City Brick salvo!")

def generate_city_sidewalk(size=2048):
    print("Gerando texturas de Calçada Urbana e Pavimentação (City Sidewalk & Concrete)...")
    y, x = np.mgrid[0:size, 0:size]
    
    # Grid de placas de concreto (lajotas 512x512)
    tile_size = 512
    joint_width = 8
    
    is_joint = ((x % tile_size) < joint_width) | ((y % tile_size) < joint_width)
    
    # Micro agregados de brita/areia do concreto
    aggregate = np.random.normal(0.5, 0.05, (size, size)).clip(0, 1)
    large_variation = (np.sin(x * 0.005) * np.cos(y * 0.005) * 0.15 + 0.5)
    
    # Tons de cinza concreto
    base_val = (175 * aggregate * (0.8 + 0.4 * large_variation)).clip(0, 255)
    r = base_val
    g = (base_val * 0.98).clip(0, 255)
    b = (base_val * 0.95).clip(0, 255)
    
    r[is_joint] = 45
    g[is_joint] = 45
    b[is_joint] = 45
    
    albedo = np.stack([r, g, b], axis=2).astype(np.uint8)
    img_albedo = Image.fromarray(albedo, mode='RGB')
    
    height = (190 * aggregate).clip(0, 255).astype(np.uint8)
    height[is_joint] = 40
    img_normal = height_to_normal(height, strength=3.0)
    
    roughness = (195 * aggregate + 40).clip(0, 255).astype(np.uint8)
    roughness[is_joint] = 250
    img_roughness = Image.fromarray(roughness, mode='L')
    
    img_metallic = Image.new('L', (size, size), 0)
    
    ao = np.ones((size, size), dtype=np.uint8) * 255
    ao[is_joint] = 70
    img_ao = Image.fromarray(ao, mode='L').filter(ImageFilter.GaussianBlur(radius=2))
    
    img_albedo.save(os.path.join(CITY_TEX_DIR, "T_City_Sidewalk_Albedo.png"))
    img_normal.save(os.path.join(CITY_TEX_DIR, "T_City_Sidewalk_Normal.png"))
    img_roughness.save(os.path.join(CITY_TEX_DIR, "T_City_Sidewalk_Roughness.png"))
    img_metallic.save(os.path.join(CITY_TEX_DIR, "T_City_Sidewalk_Metallic.png"))
    img_ao.save(os.path.join(CITY_TEX_DIR, "T_City_Sidewalk_AO.png"))
    print("City Sidewalk salvo!")

def generate_city_facade_glass_metal(size=2048):
    print("Gerando texturas de Vitrine Comercial, Painéis de Alumínio e Vidro Moderno...")
    # Atlas com Vidro comercial com reflexos, perfis de alumínio anodizado antracite e granito polido
    half = size // 2
    
    albedo = np.zeros((size, size, 3), dtype=np.uint8)
    height = np.zeros((size, size), dtype=np.uint8)
    roughness = np.zeros((size, size), dtype=np.uint8)
    metallic = np.zeros((size, size), dtype=np.uint8)
    
    # 1. Glass (Top-Left): Vidro escuro reflexivo com leve tom ciano
    albedo[0:half, 0:half, 0] = 30
    albedo[0:half, 0:half, 1] = 42
    albedo[0:half, 0:half, 2] = 48
    height[0:half, 0:half] = 128
    roughness[0:half, 0:half] = 12 # Altíssimo brilho / espelhado
    metallic[0:half, 0:half] = 20
    
    # 2. Anodized Aluminum Frame (Top-Right): Grafite acetinado
    alum_grain = np.random.normal(0.5, 0.03, (half, half)).clip(0, 1)
    albedo[0:half, half:size, 0] = (50 * alum_grain).clip(0, 255)
    albedo[0:half, half:size, 1] = (52 * alum_grain).clip(0, 255)
    albedo[0:half, half:size, 2] = (55 * alum_grain).clip(0, 255)
    height[0:half, half:size] = (150 * alum_grain).astype(np.uint8)
    roughness[0:half, half:size] = 75 # Acetinado
    metallic[0:half, half:size] = 220 # Metálico alto
    
    # 3. Polished Granite Base (Bottom-Left): Granito preto com veios brancos sutis
    gran_noise = np.random.normal(0.3, 0.15, (half, half)).clip(0, 1)
    albedo[half:size, 0:half, 0] = (60 * gran_noise).clip(0, 255)
    albedo[half:size, 0:half, 1] = (62 * gran_noise).clip(0, 255)
    albedo[half:size, 0:half, 2] = (65 * gran_noise).clip(0, 255)
    height[half:size, 0:half] = 128
    roughness[half:size, 0:half] = 40 # Polido
    metallic[half:size, 0:half] = 0
    
    # 4. Roof Zinc & Parapet Flashings (Bottom-Right): Zinco com rebites
    zinc_grain = np.random.normal(0.5, 0.04, (half, half)).clip(0, 1)
    albedo[half:size, half:size, 0] = (140 * zinc_grain).clip(0, 255)
    albedo[half:size, half:size, 1] = (145 * zinc_grain).clip(0, 255)
    albedo[half:size, half:size, 2] = (150 * zinc_grain).clip(0, 255)
    height[half:size, half:size] = (160 * zinc_grain).astype(np.uint8)
    roughness[half:size, half:size] = 100
    metallic[half:size, half:size] = 210
    
    img_albedo = Image.fromarray(albedo, mode='RGB')
    img_normal = height_to_normal(height, strength=2.5)
    img_roughness = Image.fromarray(roughness, mode='L')
    img_metallic = Image.fromarray(metallic, mode='L')
    
    img_albedo.save(os.path.join(CITY_TEX_DIR, "T_City_Modern_Albedo.png"))
    img_normal.save(os.path.join(CITY_TEX_DIR, "T_City_Modern_Normal.png"))
    img_roughness.save(os.path.join(CITY_TEX_DIR, "T_City_Modern_Roughness.png"))
    img_metallic.save(os.path.join(CITY_TEX_DIR, "T_City_Modern_Metallic.png"))
    print("City Modern salvo!")

def generate_city_props_texture(size=2048):
    print("Gerando texturas PBR para Mobiliário Urbano (Poste, Hidrante, Lixeira, Banco, Ponto de Ônibus)...")
    half = size // 2
    
    albedo = np.zeros((size, size, 3), dtype=np.uint8)
    height = np.zeros((size, size), dtype=np.uint8)
    roughness = np.zeros((size, size), dtype=np.uint8)
    metallic = np.zeros((size, size), dtype=np.uint8)
    
    # 1. Fire Hydrant Red & Brass (Top-Left)
    y1, x1 = np.mgrid[0:half, 0:half]
    brass_mask = (y1 > 100) & (y1 < 220) & (x1 > 100) & (x1 < 400)
    grain_metal = np.random.normal(0.5, 0.04, (half, half)).clip(0, 1)
    
    # Vermelho segurança industrial com leve brilho
    r1 = (210 * grain_metal).clip(0, 255)
    g1 = (35 * grain_metal).clip(0, 255)
    b1 = (25 * grain_metal).clip(0, 255)
    # Detalhes em Latão
    r1[brass_mask] = 220
    g1[brass_mask] = 180
    b1[brass_mask] = 60
    
    albedo[0:half, 0:half, 0] = r1
    albedo[0:half, 0:half, 1] = g1
    albedo[0:half, 0:half, 2] = b1
    height[0:half, 0:half] = (160 * grain_metal).astype(np.uint8)
    height[0:half, 0:half][brass_mask] = 220
    
    rough1 = np.ones((half, half), dtype=np.uint8) * 85
    rough1[brass_mask] = 50
    roughness[0:half, 0:half] = rough1
    
    met1 = np.ones((half, half), dtype=np.uint8) * 160
    met1[brass_mask] = 230
    metallic[0:half, 0:half] = met1
    
    # 2. Modern Street Light & Bus Shelter Powder-coated Grey (Top-Right)
    coat_grain = np.random.normal(0.5, 0.02, (half, half)).clip(0, 1)
    albedo[0:half, half:size, 0] = (65 * coat_grain).clip(0, 255)
    albedo[0:half, half:size, 1] = (68 * coat_grain).clip(0, 255)
    albedo[0:half, half:size, 2] = (72 * coat_grain).clip(0, 255)
    height[0:half, half:size] = (130 * coat_grain).astype(np.uint8)
    roughness[0:half, half:size] = 95
    metallic[0:half, half:size] = 200
    
    # 3. Park Bench Wood Slats (Bottom-Left): Jacarandá/Teca envernizada
    y3, x3 = np.mgrid[0:half, 0:half]
    teak_grain = (np.sin(y3 * 0.1) * 0.15 + np.random.normal(0.5, 0.04, (half, half))).clip(0, 1)
    albedo[half:size, 0:half, 0] = (155 * teak_grain).clip(0, 255)
    albedo[half:size, 0:half, 1] = (75 * teak_grain).clip(0, 255)
    albedo[half:size, 0:half, 2] = (35 * teak_grain).clip(0, 255)
    height[half:size, 0:half] = (180 * teak_grain).astype(np.uint8)
    roughness[half:size, 0:half] = 110 # Verniz acetinado
    metallic[half:size, 0:half] = 0
    
    # 4. Bus Shelter Ad Poster & LED Panel (Bottom-Right)
    y4, x4 = np.mgrid[0:half, 0:half]
    # Poster com design moderno gradiente azul/laranja urbano
    poster_r = ((y4.astype(np.float32)/half) * 180 + 40).clip(0, 255)
    poster_g = (np.sin(x4 * 0.02) * 60 + 120).clip(0, 255)
    poster_b = ((1.0 - y4.astype(np.float32)/half) * 220 + 30).clip(0, 255)
    albedo[half:size, half:size, 0] = poster_r
    albedo[half:size, half:size, 1] = poster_g
    albedo[half:size, half:size, 2] = poster_b
    height[half:size, half:size] = 128
    roughness[half:size, half:size] = 60
    metallic[half:size, half:size] = 0
    
    img_albedo = Image.fromarray(albedo, mode='RGB')
    img_normal = height_to_normal(height, strength=2.8)
    img_roughness = Image.fromarray(roughness, mode='L')
    img_metallic = Image.fromarray(metallic, mode='L')
    
    img_albedo.save(os.path.join(CITY_TEX_DIR, "T_City_Props_Albedo.png"))
    img_normal.save(os.path.join(CITY_TEX_DIR, "T_City_Props_Normal.png"))
    img_roughness.save(os.path.join(CITY_TEX_DIR, "T_City_Props_Roughness.png"))
    img_metallic.save(os.path.join(CITY_TEX_DIR, "T_City_Props_Metallic.png"))
    print("City Props salvo!")

if __name__ == "__main__":
    generate_barn_wood()
    generate_corrugated_metal()
    generate_farm_props_texture()
    generate_city_brick()
    generate_city_sidewalk()
    generate_city_facade_glass_metal()
    generate_city_props_texture()
    print("=== TODAS AS TEXTURAS PBR GERADAS COM SUCESSO! ===")
