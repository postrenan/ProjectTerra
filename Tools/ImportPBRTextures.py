import os
import sys
import struct
import math
import uuid

PBR_BASE = os.path.join("Assets", "_Project", "Textures", "PBR")

CATEGORIES = {
    "Ground/Soil": {
        "color": (95, 68, 48),
        "detail_noise": "soil",
        "smoothness": 24   # ~0.09
    },
    "Ground/Grass": {
        "color": (62, 108, 45),
        "detail_noise": "grass",
        "smoothness": 32   # ~0.12
    },
    "Ground/Rock": {
        "color": (120, 115, 110),
        "detail_noise": "cliff",
        "smoothness": 28   # ~0.11
    },
    "Ground/Sand": {
        "color": (195, 175, 130),
        "detail_noise": "sand",
        "smoothness": 18   # ~0.07
    },
    "Ground/Gravel": {
        "color": (105, 102, 98),
        "detail_noise": "pebbles",
        "smoothness": 26   # ~0.10
    },
    "Infrastructure/Asphalt": {
        "color": (45, 45, 48),
        "detail_noise": "asphalt",
        "smoothness": 40   # ~0.15
    },
    "Infrastructure/Concrete": {
        "color": (160, 160, 162),
        "detail_noise": "concrete",
        "smoothness": 35   # ~0.14
    },
    "Infrastructure/Wood": {
        "color": (130, 95, 65),
        "detail_noise": "grain",
        "smoothness": 38   # ~0.15
    },
    "Infrastructure/Metal": {
        "color": (180, 185, 190),
        "detail_noise": "steel",
        "smoothness": 150  # ~0.59
    },
    "Ground/DryGrass": {
        "color": (155, 145, 68),
        "detail_noise": "grass",
        "smoothness": 28   # ~0.11
    },
    "Ground/Snow": {
        "color": (230, 235, 245),
        "detail_noise": "snow",
        "smoothness": 48   # ~0.19
    },
    "Vegetation/Foliage": {
        "color": (48, 125, 38),
        "detail_noise": "leaves",
        "smoothness": 35   # ~0.14
    }
}

def create_tga_image(width, height, get_pixel_func, is_rgba=True):
    """Gera uma imagem TGA não comprimida de 32-bit RGBA ou 24-bit RGB."""
    header = bytearray(18)
    header[2] = 2  # Uncompressed True-Color
    header[12] = width & 0xFF
    header[13] = (width >> 8) & 0xFF
    header[14] = height & 0xFF
    header[15] = (height >> 8) & 0xFF
    header[16] = 32 if is_rgba else 24
    header[17] = 0x20 if is_rgba else 0x20  # Top-to-bottom

    bytes_per_px = 4 if is_rgba else 3
    pixels = bytearray(width * height * bytes_per_px)
    idx = 0
    for y in range(height):
        for x in range(width):
            px = get_pixel_func(x, y, width, height)
            if is_rgba:
                r, g, b, a = px
                pixels[idx] = max(0, min(255, int(b)))
                pixels[idx + 1] = max(0, min(255, int(g)))
                pixels[idx + 2] = max(0, min(255, int(r)))
                pixels[idx + 3] = max(0, min(255, int(a)))
                idx += 4
            else:
                r, g, b = px
                pixels[idx] = max(0, min(255, int(b)))
                pixels[idx + 1] = max(0, min(255, int(g)))
                pixels[idx + 2] = max(0, min(255, int(r)))
                idx += 3

    return bytes(header) + bytes(pixels)

def get_or_create_guid(meta_path):
    if os.path.exists(meta_path):
        with open(meta_path, 'r', encoding='utf-8') as f:
            for line in f:
                if line.startswith('guid:'):
                    return line.split(':')[1].strip()
    return uuid.uuid4().hex

def write_texture_meta(meta_path, texture_type=0, srgb=1, max_size=2048):
    guid = get_or_create_guid(meta_path)
    meta_content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: {srgb}
    linearTexture: {0 if srgb else 1}
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {max_size}
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 16
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 0
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: {texture_type}
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, 'w', encoding='utf-8') as f:
        f.write(meta_content)

def generate_pbr_set(category_name, info, size=512):
    cat_dir = os.path.join(PBR_BASE, category_name)
    os.makedirs(cat_dir, exist_ok=True)
    
    base_r, base_g, base_b = info["color"]
    smoothness_val = info["smoothness"]

    # 1. Albedo (Cor Difusa PBR em RGB + Smoothness em Alpha)
    def albedo_func(x, y, w, h):
        nx = x / float(w)
        ny = y / float(h)
        n1 = math.sin(nx * 32.0 * math.pi) * math.cos(ny * 32.0 * math.pi)
        n2 = math.sin(nx * 128.0 * math.pi + 1.2) * math.cos(ny * 128.0 * math.pi + 0.8) * 0.5
        n3 = math.cos(nx * 256.0 * math.pi - ny * 192.0 * math.pi) * 0.25
        var = (n1 + n2 + n3) * 16.0
        smooth_var = (n1 + n2) * 4.0
        r = max(0, min(255, int(base_r + var)))
        g = max(0, min(255, int(base_g + var)))
        b = max(0, min(255, int(base_b + var)))
        a = max(5, min(250, int(smoothness_val + smooth_var)))
        return (r, g, b, a)

    albedo_bytes = create_tga_image(size, size, albedo_func, is_rgba=True)
    with open(os.path.join(cat_dir, "Albedo.tga"), "wb") as f:
        f.write(albedo_bytes)

    # 2. Normal Map (Tangente Normal PBR em espaço tangente)
    def normal_func(x, y, w, h):
        nx = x / float(w)
        ny = y / float(h)
        dx = math.cos(nx * 64.0 * math.pi) * 28.0 + math.cos(nx * 128.0 * math.pi) * 12.0
        dy = math.sin(ny * 64.0 * math.pi) * 28.0 + math.sin(ny * 128.0 * math.pi) * 12.0
        # Normalização aproximada para manter vetor unitário
        length = math.sqrt(dx*dx + dy*dy + 235.0*235.0)
        norm_x = int(128 + (dx / length) * 127.0)
        norm_y = int(128 + (dy / length) * 127.0)
        norm_z = int((235.0 / length) * 255.0)
        return (norm_x, norm_y, norm_z, 255)

    normal_bytes = create_tga_image(size, size, normal_func, is_rgba=True)
    with open(os.path.join(cat_dir, "Normal.tga"), "wb") as f:
        f.write(normal_bytes)

    # 3. Mask Map (R = Metallic, G = Ambient Occlusion, B = Detail Height, A = Smoothness)
    def mask_func(x, y, w, h):
        metallic = 210 if "Metal" in category_name else 0
        ao = 230
        height = 128
        return (metallic, ao, height, smoothness_val)

    mask_bytes = create_tga_image(size, size, mask_func, is_rgba=True)
    with open(os.path.join(cat_dir, "MaskMap.tga"), "wb") as f:
        f.write(mask_bytes)

    # Exportar também PNGs via PIL se disponível mantendo canal RGBA integral
    try:
        from PIL import Image
        for name in ["Albedo", "Normal", "MaskMap"]:
            tga_p = os.path.join(cat_dir, f"{name}.tga")
            png_p = os.path.join(cat_dir, f"{name}.png")
            img = Image.open(tga_p)
            img.save(png_p, "PNG")
    except Exception as e:
        print(f"PIL error for {category_name}: {e}")

    # Criar/atualizar metas com os tipos corretos
    write_texture_meta(os.path.join(cat_dir, "Albedo.png.meta"), texture_type=0, srgb=1, max_size=2048)
    write_texture_meta(os.path.join(cat_dir, "Albedo.tga.meta"), texture_type=0, srgb=1, max_size=2048)
    write_texture_meta(os.path.join(cat_dir, "Normal.png.meta"), texture_type=1, srgb=0, max_size=2048)
    write_texture_meta(os.path.join(cat_dir, "Normal.tga.meta"), texture_type=1, srgb=0, max_size=2048)
    write_texture_meta(os.path.join(cat_dir, "MaskMap.png.meta"), texture_type=0, srgb=0, max_size=2048)
    write_texture_meta(os.path.join(cat_dir, "MaskMap.tga.meta"), texture_type=0, srgb=0, max_size=2048)

def main():
    print("=========================================================")
    print("[ProjectTerra] Gerando Texturas PBR com Smoothness em Alpha e Normals Corretas...")
    print("=========================================================")
    os.makedirs(PBR_BASE, exist_ok=True)

    for cat_name, info in CATEGORIES.items():
        print(f"[PROCESSANDO] Gerando pacote PBR para {cat_name}...")
        generate_pbr_set(cat_name, info, size=512)

    print("---------------------------------------------------------")
    print(f"[SUCESSO] Todos os 12 conjuntos PBR gerados com canal Alpha para Built-in e HDRP!")
    print("=========================================================")

if __name__ == "__main__":
    main()
