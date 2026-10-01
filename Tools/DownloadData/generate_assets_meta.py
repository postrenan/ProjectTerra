import os
import uuid

def generate_guid():
    return uuid.uuid4().hex

def create_meta_if_missing(file_path, is_texture=False):
    meta_path = file_path + ".meta"
    if os.path.exists(meta_path):
        with open(meta_path, 'r', encoding='utf-8') as f:
            for line in f:
                if line.startswith("guid:"):
                    return line.split(":")[1].strip()
    
    guid = generate_guid()
    if is_texture:
        content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreservesCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 1
  streamingMipmapsPriority: 0
  vramBudgetMB: 512
  ignoreMasterTextureLimit: 0
  isPreProcessed: 0
  ignoreMipmapLimit: 0
  wrapU: 0
  wrapV: 1
  wrapW: 1
  filterMode: 2
  aniso: 16
  maxTextureSize: 8192
  textureCompression: 1
"""
    else:
        content = f"""fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, 'w', encoding='utf-8') as f:
        f.write(content)
    return guid

print("Generating metas...")
base_tex = r"Assets\_Project\Textures\Earth"
tex_guids = {}
for f in os.listdir(base_tex):
    if not f.endswith(".meta"):
        full = os.path.join(base_tex, f)
        guid = create_meta_if_missing(full, is_texture=True)
        tex_guids[f] = guid
        print(f"Texture {f} -> GUID {guid}")

shader_path = r"Assets\_Project\Shaders\EarthSurface.shader"
shader_guid = create_meta_if_missing(shader_path)
print(f"Shader -> GUID {shader_guid}")

# Create M_Earth.mat
mat_path = r"Assets\_Project\Materials\M_Earth.mat"
daymap_guid = tex_guids.get("8k_earth_daymap.jpg", tex_guids.get("2k_earth_daymap.jpg"))
watermask_guid = tex_guids.get("8k_earth_specular_map.tif", tex_guids.get("2k_earth_specular_map.tif"))
normal_guid = tex_guids.get("8k_earth_normal_map.tif", tex_guids.get("2k_earth_normal_map.tif"))

mat_content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: M_Earth
  m_Shader: {{fileID: 4800000, guid: {shader_guid}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BumpMap:
        m_Texture: {{fileID: 2800000, guid: {normal_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {daymap_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _WaterMask:
        m_Texture: {{fileID: 2800000, guid: {watermask_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _AtmosphereIntensity: 1.5
    - _AtmospherePower: 3.5
    - _BumpScale: 1.0
    - _LandSmoothness: 0.15
    - _OceanSmoothness: 0.92
    m_Colors:
    - _AtmosphereColor: {{r: 0.35, g: 0.65, b: 1.0, a: 1.0}}
    - _DayColor: {{r: 1.0, g: 1.0, b: 1.0, a: 1.0}}
    - _OceanColor: {{r: 0.05, g: 0.25, b: 0.6, a: 1.0}}
"""
with open(mat_path, 'w', encoding='utf-8') as f:
    f.write(mat_content)

mat_guid = create_meta_if_missing(mat_path)
print(f"Material M_Earth created -> GUID {mat_guid}")
