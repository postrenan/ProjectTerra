"""
Procedural 3D NPC Characters Generator for Blender 5.2.
Generates an extensive, highly diverse collection of 12 distinct Humanoid NPCs
covering multiple ethnicities, facial characteristics, skin tones (Fitzpatrick I-VI),
and authentic traditional & contemporary garments:

Archetypes:
 1. NPC_Asian_Male_Techwear (East Asian: almond eyes, high cheekbones, urban techwear jacket, high-tops)
 2. NPC_Asian_Female_Silk (East Asian: delicate features, modern mandarin silk qipao, hair bun)
 3. NPC_Egyptian_Male_Galabeya (Egyptian/Arab: aquiline nose, almond eyes, neat beard, linen galabeya with gold trim, kufi cap)
 4. NPC_Egyptian_Female_Alexandria (Egyptian: pronounced almond eyes, flowing draped tunic, embroidered hem, silk wrap)
 5. NPC_American_Male_Streetwear (American Casual: athletic build, hoodie over tee, denim jeans, sneakers, baseball cap)
 6. NPC_American_Female_Athleisure (American Modern: crop bomber, high-waisted cargo joggers, runner trainers, high ponytail)
 7. NPC_European_Male_Overcoat (European Classic: prominent nasal bridge, tailored long wool overcoat, scarf, dress oxfords)
 8. NPC_European_Female_Trench (European Chic: belted camel trench coat, turtleneck, slim trousers, leather boots)
 9. NPC_African_Male_Urban (Afro-Descendant: deep ebony tone, sculpted features, varsity jacket, jogger jeans, fresh kicks)
 10. NPC_Latin_Female_Casual (Latin American: warm caramel skin, leather jacket, graphic tee, hiking boots, stylish locks)
 11. NPC_Nordic_Male_Winter (Northern European: fair skin, blond/ginger full beard, Nordic cable-knit sweater, beanie)
 12. NPC_MiddleEastern_Executive (Middle Eastern: warm olive/bronze skin, trimmed beard, tailored navy business suit, polished shoes)

Every character is fully rigged to a standard Unity-compatible Humanoid skeleton
(pelvis, spine_01/02/03, neck_01, Head, clavicles, arms, hands, thighs, calves, feet, balls)
with smooth vertex skin weighting, calibrated PBR materials, and individual FBX exports for Unity.
"""

import bpy
import bmesh
import math
import os
import shutil
from mathutils import Vector, Matrix, Euler

# -----------------------------------------------------------------------------
# Configuration & Paths
# -----------------------------------------------------------------------------
PROJECT_DIR = r"E:\Backup-C\Projetos\testejogo"
BLEND_FILE_NAME = "NPC_Characters_Collection.blend"
BLEND_OUTPUT_PATH = os.path.join(PROJECT_DIR, BLEND_FILE_NAME)

MODELS_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "Characters")
NPCS_DIR = os.path.join(MODELS_DIR, "NPCs")
RESOURCES_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Resources", "Models", "Characters")
ASSETS_BLEND_PATH = os.path.join(MODELS_DIR, BLEND_FILE_NAME)
SHOWCASE_RENDER_PATH = os.path.join(MODELS_DIR, "npc_characters_showcase.png")
ROOT_SHOWCASE_RENDER = os.path.join(PROJECT_DIR, "npc_characters_showcase.png")

os.makedirs(NPCS_DIR, exist_ok=True)
os.makedirs(RESOURCES_DIR, exist_ok=True)

print(f"[NPC Generator] Initializing high-fidelity procedural generator for Blender 5.2...")

# -----------------------------------------------------------------------------
# Scene Reset & Camera / Light Setup
# -----------------------------------------------------------------------------
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = 'METERS'
    
    # View transform & color management
    if 'AgX' in bpy.types.ColorManagedViewSettings.bl_rna.properties['view_transform'].enum_items:
        scene.view_settings.view_transform = 'AgX'
    else:
        scene.view_settings.view_transform = 'Filmic'
    scene.view_settings.look = 'Medium High Contrast'
    
    # World sky & ambient lighting
    world = bpy.data.worlds.new("World_NPCShowroom")
    scene.world = world
    world.use_nodes = True
    wnodes = world.node_tree.nodes
    wnodes.clear()
    
    w_bg = wnodes.new('ShaderNodeBackground')
    w_bg.inputs['Color'].default_value = (0.90, 0.92, 0.95, 1.0)
    w_bg.inputs['Strength'].default_value = 0.95
    w_out = wnodes.new('ShaderNodeOutputWorld')
    world.node_tree.links.new(w_bg.outputs['Background'], w_out.inputs['Surface'])

# -----------------------------------------------------------------------------
# PBR Material Creator with Real SSS & Melanin Calibration
# -----------------------------------------------------------------------------
def create_pbr_material(name, base_color, roughness=0.55, metallic=0.0, sss_weight=0.0, sss_color=None):
    mat = bpy.data.materials.get(name)
    if not mat:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
    
    nodes = mat.node_tree.nodes
    nodes.clear()
    
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Base Color'].default_value = base_color
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Metallic'].default_value = metallic
    
    # Blender 5.2 Principled BSDF Subsurface Scattering
    if sss_weight > 0 and 'Subsurface Weight' in bsdf.inputs:
        bsdf.inputs['Subsurface Weight'].default_value = sss_weight
        if sss_color is not None and 'Subsurface Radius' in bsdf.inputs:
            bsdf.inputs['Subsurface Radius'].default_value = sss_color[:3]
    
    output = nodes.new('ShaderNodeOutputMaterial')
    mat.node_tree.links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    return mat

def setup_all_materials():
    materials = {}
    
    # --- Human Skin Tones (Fitzpatrick Scale I - VI) ---
    # Type I: Nordic Pale
    materials['Skin_Nordic_Pale'] = create_pbr_material('M_Skin_Nordic_Pale', (0.96, 0.88, 0.82, 1.0), roughness=0.48, sss_weight=0.25, sss_color=(1.0, 0.45, 0.35))
    # Type II: Central European Fair
    materials['Skin_Euro_Fair'] = create_pbr_material('M_Skin_Euro_Fair', (0.92, 0.82, 0.74, 1.0), roughness=0.48, sss_weight=0.22, sss_color=(1.0, 0.42, 0.32))
    # Type III: East Asian Golden Warm
    materials['Skin_Asian_Golden'] = create_pbr_material('M_Skin_Asian_Golden', (0.90, 0.79, 0.69, 1.0), roughness=0.45, sss_weight=0.20, sss_color=(0.95, 0.45, 0.30))
    # Type IV: Mediterranean / Middle Eastern Olive
    materials['Skin_Olive_Med'] = create_pbr_material('M_Skin_Olive_Med', (0.82, 0.67, 0.54, 1.0), roughness=0.50, sss_weight=0.18, sss_color=(0.90, 0.40, 0.28))
    # Type IV/V: Egyptian Golden Bronze
    materials['Skin_Egyptian_Bronze'] = create_pbr_material('M_Skin_Egyptian_Bronze', (0.74, 0.56, 0.42, 1.0), roughness=0.50, sss_weight=0.18, sss_color=(0.85, 0.38, 0.25))
    # Type V: Latin American Caramel
    materials['Skin_Latin_Caramel'] = create_pbr_material('M_Skin_Latin_Caramel', (0.66, 0.48, 0.35, 1.0), roughness=0.50, sss_weight=0.17, sss_color=(0.80, 0.35, 0.22))
    # Type V+: Nubian / Deep Bronze Mocha
    materials['Skin_Deep_Mocha'] = create_pbr_material('M_Skin_Deep_Mocha', (0.46, 0.31, 0.22, 1.0), roughness=0.52, sss_weight=0.15, sss_color=(0.70, 0.30, 0.18))
    # Type VI: Sub-Saharan African Deep Ebony
    materials['Skin_African_Ebony'] = create_pbr_material('M_Skin_African_Ebony', (0.26, 0.17, 0.13, 1.0), roughness=0.52, sss_weight=0.14, sss_color=(0.60, 0.25, 0.15))

    # --- Eyes & Facial Details ---
    materials['Eye_DarkBrown'] = create_pbr_material('M_Eye_DarkBrown', (0.12, 0.08, 0.06, 1.0), roughness=0.10)
    materials['Eye_Black'] = create_pbr_material('M_Eye_Black', (0.02, 0.02, 0.02, 1.0), roughness=0.10)
    materials['Eye_OceanBlue'] = create_pbr_material('M_Eye_OceanBlue', (0.18, 0.45, 0.70, 1.0), roughness=0.10)
    materials['Eye_AmberGreen'] = create_pbr_material('M_Eye_AmberGreen', (0.28, 0.48, 0.22, 1.0), roughness=0.10)
    materials['Eye_Sclera'] = create_pbr_material('M_Eye_Sclera', (0.98, 0.98, 0.98, 1.0), roughness=0.15)
    materials['Lips_Natural'] = create_pbr_material('M_Lips_Natural', (0.85, 0.50, 0.50, 1.0), roughness=0.35)
    materials['Lips_Deep'] = create_pbr_material('M_Lips_Deep', (0.45, 0.24, 0.24, 1.0), roughness=0.35)

    # --- Hair & Facial Hair Colors ---
    materials['Hair_SilkyBlack'] = create_pbr_material('M_Hair_SilkyBlack', (0.04, 0.04, 0.04, 1.0), roughness=0.40)
    materials['Hair_DarkBrown'] = create_pbr_material('M_Hair_DarkBrown', (0.16, 0.10, 0.07, 1.0), roughness=0.50)
    materials['Hair_Chestnut'] = create_pbr_material('M_Hair_Chestnut', (0.32, 0.19, 0.11, 1.0), roughness=0.50)
    materials['Hair_Blonde'] = create_pbr_material('M_Hair_Blonde', (0.82, 0.70, 0.42, 1.0), roughness=0.45)
    materials['Hair_Ginger'] = create_pbr_material('M_Hair_Ginger', (0.72, 0.32, 0.14, 1.0), roughness=0.50)
    materials['Hair_SilverGray'] = create_pbr_material('M_Hair_SilverGray', (0.72, 0.74, 0.76, 1.0), roughness=0.45)

    # --- Garments & Textiles ---
    # Egyptian
    materials['Fab_EgyptianLinen'] = create_pbr_material('M_Fab_EgyptianLinen', (0.94, 0.91, 0.84, 1.0), roughness=0.75)
    materials['Fab_GoldTrim'] = create_pbr_material('M_Fab_GoldTrim', (0.92, 0.76, 0.24, 1.0), roughness=0.25, metallic=0.85)
    materials['Fab_EgyptianTeal'] = create_pbr_material('M_Fab_EgyptianTeal', (0.08, 0.40, 0.44, 1.0), roughness=0.60)
    materials['Fab_EgyptianTerracotta'] = create_pbr_material('M_Fab_EgyptianTerracotta', (0.70, 0.30, 0.18, 1.0), roughness=0.70)
    
    # Asian
    materials['Fab_AsianSilkRed'] = create_pbr_material('M_Fab_AsianSilkRed', (0.78, 0.12, 0.14, 1.0), roughness=0.30)
    materials['Fab_AsianSilkGold'] = create_pbr_material('M_Fab_AsianSilkGold', (0.88, 0.74, 0.24, 1.0), roughness=0.28, metallic=0.55)
    materials['Fab_TechwearBlack'] = create_pbr_material('M_Fab_TechwearBlack', (0.07, 0.07, 0.08, 1.0), roughness=0.38)
    materials['Fab_TechwearNeon'] = create_pbr_material('M_Fab_TechwearNeon', (0.12, 0.92, 0.82, 1.0), roughness=0.25)
    
    # American
    materials['Fab_DenimBlue'] = create_pbr_material('M_Fab_DenimBlue', (0.18, 0.30, 0.50, 1.0), roughness=0.75)
    materials['Fab_DenimDark'] = create_pbr_material('M_Fab_DenimDark', (0.10, 0.13, 0.20, 1.0), roughness=0.75)
    materials['Fab_CottonGrey'] = create_pbr_material('M_Fab_CottonGrey', (0.50, 0.51, 0.53, 1.0), roughness=0.70)
    materials['Fab_CottonWhite'] = create_pbr_material('M_Fab_CottonWhite', (0.96, 0.96, 0.96, 1.0), roughness=0.65)
    materials['Fab_BomberMaroon'] = create_pbr_material('M_Fab_BomberMaroon', (0.52, 0.10, 0.14, 1.0), roughness=0.40)
    materials['Fab_CapNavy'] = create_pbr_material('M_Fab_CapNavy', (0.08, 0.14, 0.30, 1.0), roughness=0.65)
    materials['Fab_AthleisureLilac'] = create_pbr_material('M_Fab_AthleisureLilac', (0.62, 0.50, 0.72, 1.0), roughness=0.50)
    
    # European
    materials['Fab_TrenchCamel'] = create_pbr_material('M_Fab_TrenchCamel', (0.74, 0.58, 0.40, 1.0), roughness=0.60)
    materials['Fab_WoolCharcoal'] = create_pbr_material('M_Fab_WoolCharcoal', (0.16, 0.16, 0.18, 1.0), roughness=0.80)
    materials['Fab_NavySuit'] = create_pbr_material('M_Fab_NavySuit', (0.10, 0.15, 0.28, 1.0), roughness=0.50)
    materials['Fab_ScarfBurgundy'] = create_pbr_material('M_Fab_ScarfBurgundy', (0.48, 0.10, 0.16, 1.0), roughness=0.75)
    materials['Fab_NordicSweater'] = create_pbr_material('M_Fab_NordicSweater', (0.86, 0.88, 0.90, 1.0), roughness=0.88)
    materials['Fab_NordicPattern'] = create_pbr_material('M_Fab_NordicPattern', (0.20, 0.34, 0.52, 1.0), roughness=0.88)

    # Leather & Footwear
    materials['Leather_DarkBrown'] = create_pbr_material('M_Leather_DarkBrown', (0.18, 0.11, 0.06, 1.0), roughness=0.38)
    materials['Leather_Black'] = create_pbr_material('M_Leather_Black', (0.06, 0.06, 0.06, 1.0), roughness=0.35)
    materials['Sneaker_White'] = create_pbr_material('M_Sneaker_White', (0.96, 0.96, 0.96, 1.0), roughness=0.40)
    materials['Sneaker_Accent'] = create_pbr_material('M_Sneaker_Accent', (0.90, 0.28, 0.16, 1.0), roughness=0.45)
    materials['Metal_Buckle'] = create_pbr_material('M_Metal_Buckle', (0.85, 0.85, 0.88, 1.0), roughness=0.20, metallic=0.92)
    
    return materials

# -----------------------------------------------------------------------------
# Humanoid Rig / Armature Generator (Unity Mecanim Compatible)
# -----------------------------------------------------------------------------
def create_humanoid_armature(name, height_scale=1.0):
    bpy.ops.object.select_all(action='DESELECT')
    arm_data = bpy.data.armatures.new(f"{name}_Armature")
    arm_obj = bpy.data.objects.new(f"{name}_Armature", arm_data)
    bpy.context.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    arm_obj.select_set(True)
    
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones
    
    def S(z): return z * height_scale
    
    # Root & Pelvis
    root = eb.new('root')
    root.head = (0, 0, 0)
    root.tail = (0, 0, S(0.1))
    
    pelvis = eb.new('pelvis')
    pelvis.head = (0, 0, S(0.95))
    pelvis.tail = (0, 0, S(1.05))
    pelvis.parent = root
    
    # Spine chain
    spine_01 = eb.new('spine_01')
    spine_01.head = (0, 0, S(1.05))
    spine_01.tail = (0, 0, S(1.18))
    spine_01.parent = pelvis
    
    spine_02 = eb.new('spine_02')
    spine_02.head = (0, 0, S(1.18))
    spine_02.tail = (0, 0, S(1.32))
    spine_02.parent = spine_01
    
    spine_03 = eb.new('spine_03')
    spine_03.head = (0, 0, S(1.32))
    spine_03.tail = (0, 0, S(1.48))
    spine_03.parent = spine_02
    
    # Neck & Head
    neck_01 = eb.new('neck_01')
    neck_01.head = (0, 0, S(1.48))
    neck_01.tail = (0, 0, S(1.57))
    neck_01.parent = spine_03
    
    head = eb.new('Head')
    head.head = (0, 0, S(1.57))
    head.tail = (0, 0, S(1.80))
    head.parent = neck_01
    
    # Left Arm
    clavicle_l = eb.new('clavicle_l')
    clavicle_l.head = (0.04, 0, S(1.46))
    clavicle_l.tail = (0.18, 0, S(1.46))
    clavicle_l.parent = spine_03
    
    upperarm_l = eb.new('upperarm_l')
    upperarm_l.head = (0.18, 0, S(1.46))
    upperarm_l.tail = (0.42, 0, S(1.46))
    upperarm_l.parent = clavicle_l
    
    lowerarm_l = eb.new('lowerarm_l')
    lowerarm_l.head = (0.42, 0, S(1.46))
    lowerarm_l.tail = (0.66, 0, S(1.46))
    lowerarm_l.parent = upperarm_l
    
    hand_l = eb.new('hand_l')
    hand_l.head = (0.66, 0, S(1.46))
    hand_l.tail = (0.78, 0, S(1.46))
    hand_l.parent = lowerarm_l
    
    # Right Arm
    clavicle_r = eb.new('clavicle_r')
    clavicle_r.head = (-0.04, 0, S(1.46))
    clavicle_r.tail = (-0.18, 0, S(1.46))
    clavicle_r.parent = spine_03
    
    upperarm_r = eb.new('upperarm_r')
    upperarm_r.head = (-0.18, 0, S(1.46))
    upperarm_r.tail = (-0.42, 0, S(1.46))
    upperarm_r.parent = clavicle_r
    
    lowerarm_r = eb.new('lowerarm_r')
    lowerarm_r.head = (-0.42, 0, S(1.46))
    lowerarm_r.tail = (-0.66, 0, S(1.46))
    lowerarm_r.parent = upperarm_r
    
    hand_r = eb.new('hand_r')
    hand_r.head = (-0.66, 0, S(1.46))
    hand_r.tail = (-0.78, 0, S(1.46))
    hand_r.parent = lowerarm_r
    
    # Left Leg
    thigh_l = eb.new('thigh_l')
    thigh_l.head = (0.10, 0, S(0.92))
    thigh_l.tail = (0.10, 0, S(0.50))
    thigh_l.parent = pelvis
    
    calf_l = eb.new('calf_l')
    calf_l.head = (0.10, 0, S(0.50))
    calf_l.tail = (0.10, 0, S(0.10))
    calf_l.parent = thigh_l
    
    foot_l = eb.new('foot_l')
    foot_l.head = (0.10, 0, S(0.10))
    foot_l.tail = (0.10, 0.14, S(0.02))
    foot_l.parent = calf_l
    
    ball_l = eb.new('ball_l')
    ball_l.head = (0.10, 0.14, S(0.02))
    ball_l.tail = (0.10, 0.20, S(0.01))
    ball_l.parent = foot_l
    
    # Right Leg
    thigh_r = eb.new('thigh_r')
    thigh_r.head = (-0.10, 0, S(0.92))
    thigh_r.tail = (-0.10, 0, S(0.50))
    thigh_r.parent = pelvis
    
    calf_r = eb.new('calf_r')
    calf_r.head = (-0.10, 0, S(0.50))
    calf_r.tail = (-0.10, 0, S(0.10))
    calf_r.parent = thigh_r
    
    foot_r = eb.new('foot_r')
    foot_r.head = (-0.10, 0, S(0.10))
    foot_r.tail = (-0.10, 0.14, S(0.02))
    foot_r.parent = calf_r
    
    ball_r = eb.new('ball_r')
    ball_r.head = (-0.10, 0.14, S(0.02))
    ball_r.tail = (-0.10, 0.20, S(0.01))
    ball_r.parent = foot_r
    
    bpy.ops.object.mode_set(mode='OBJECT')
    return arm_obj

# -----------------------------------------------------------------------------
# Smooth Skin Weighting Helper
# -----------------------------------------------------------------------------
def auto_skin_mesh_to_bones(mesh_obj, arm_obj, height_scale=1.0):
    bone_names = [b.name for b in arm_obj.data.bones]
    for bname in bone_names:
        if bname not in mesh_obj.vertex_groups:
            mesh_obj.vertex_groups.new(name=bname)
    
    mod = mesh_obj.modifiers.new(name="Armature", type='ARMATURE')
    mod.object = arm_obj
    mod.use_vertex_groups = True
    
    mesh = mesh_obj.data
    def S(z): return z * height_scale
    
    for v in mesh.vertices:
        x, y, z = v.co.x, v.co.y, v.co.z
        
        if z >= S(1.55):
            mesh_obj.vertex_groups['Head'].add([v.index], 1.0, 'REPLACE')
        elif z >= S(1.48):
            w_h = (z - S(1.48)) / S(0.07)
            mesh_obj.vertex_groups['Head'].add([v.index], w_h, 'REPLACE')
            mesh_obj.vertex_groups['neck_01'].add([v.index], 1.0 - w_h, 'REPLACE')
        elif z >= S(1.32):
            if abs(x) > 0.18 and z > S(1.35):
                if x > 0:
                    if x < 0.35:
                        mesh_obj.vertex_groups['upperarm_l'].add([v.index], 1.0, 'REPLACE')
                    elif x < 0.60:
                        mesh_obj.vertex_groups['lowerarm_l'].add([v.index], 1.0, 'REPLACE')
                    else:
                        mesh_obj.vertex_groups['hand_l'].add([v.index], 1.0, 'REPLACE')
                else:
                    if x > -0.35:
                        mesh_obj.vertex_groups['upperarm_r'].add([v.index], 1.0, 'REPLACE')
                    elif x > -0.60:
                        mesh_obj.vertex_groups['lowerarm_r'].add([v.index], 1.0, 'REPLACE')
                    else:
                        mesh_obj.vertex_groups['hand_r'].add([v.index], 1.0, 'REPLACE')
            else:
                mesh_obj.vertex_groups['spine_03'].add([v.index], 1.0, 'REPLACE')
        elif z >= S(1.18):
            mesh_obj.vertex_groups['spine_02'].add([v.index], 1.0, 'REPLACE')
        elif z >= S(1.00):
            mesh_obj.vertex_groups['spine_01'].add([v.index], 1.0, 'REPLACE')
        elif z >= S(0.88):
            mesh_obj.vertex_groups['pelvis'].add([v.index], 1.0, 'REPLACE')
        elif z >= S(0.50):
            if x >= 0:
                mesh_obj.vertex_groups['thigh_l'].add([v.index], 1.0, 'REPLACE')
            else:
                mesh_obj.vertex_groups['thigh_r'].add([v.index], 1.0, 'REPLACE')
        elif z >= S(0.10):
            if x >= 0:
                mesh_obj.vertex_groups['calf_l'].add([v.index], 1.0, 'REPLACE')
            else:
                mesh_obj.vertex_groups['calf_r'].add([v.index], 1.0, 'REPLACE')
        else:
            if x >= 0:
                mesh_obj.vertex_groups['foot_l'].add([v.index], 0.7, 'REPLACE')
                mesh_obj.vertex_groups['ball_l'].add([v.index], 0.3, 'REPLACE')
            else:
                mesh_obj.vertex_groups['foot_r'].add([v.index], 0.7, 'REPLACE')
                mesh_obj.vertex_groups['ball_r'].add([v.index], 0.3, 'REPLACE')

# -----------------------------------------------------------------------------
# Modular Geometry Construction Helpers
# -----------------------------------------------------------------------------
def bmesh_add_box(bm, center, size, mat_idx=0):
    half = Vector(size) * 0.5
    verts = [
        bm.verts.new(center + Vector((-half.x, -half.y, -half.z))),
        bm.verts.new(center + Vector(( half.x, -half.y, -half.z))),
        bm.verts.new(center + Vector(( half.x,  half.y, -half.z))),
        bm.verts.new(center + Vector((-half.x,  half.y, -half.z))),
        bm.verts.new(center + Vector((-half.x, -half.y,  half.z))),
        bm.verts.new(center + Vector(( half.x, -half.y,  half.z))),
        bm.verts.new(center + Vector(( half.x,  half.y,  half.z))),
        bm.verts.new(center + Vector((-half.x,  half.y,  half.z))),
    ]
    faces_indices = [
        (0, 1, 2, 3), (4, 7, 6, 5),
        (0, 4, 5, 1), (1, 5, 6, 2),
        (2, 6, 7, 3), (3, 7, 4, 0)
    ]
    for idxs in faces_indices:
        f = bm.faces.new([verts[i] for i in idxs])
        f.material_index = mat_idx

def bmesh_add_cylinder(bm, center, radius, height, segments=12, mat_idx=0, top_cap=True, bot_cap=True):
    half_h = height * 0.5
    top_verts = []
    bot_verts = []
    for i in range(segments):
        theta = 2.0 * math.pi * i / segments
        vx = math.cos(theta) * radius
        vy = math.sin(theta) * radius
        bot_verts.append(bm.verts.new(center + Vector((vx, vy, -half_h))))
        top_verts.append(bm.verts.new(center + Vector((vx, vy,  half_h))))
    
    for i in range(segments):
        ni = (i + 1) % segments
        f = bm.faces.new([bot_verts[i], bot_verts[ni], top_verts[ni], top_verts[i]])
        f.material_index = mat_idx
        
    if top_cap:
        f = bm.faces.new(list(reversed(top_verts)))
        f.material_index = mat_idx
    if bot_cap:
        f = bm.faces.new(bot_verts)
        f.material_index = mat_idx

def bmesh_add_sphere(bm, center, radius, seg_u=12, seg_v=8, mat_idx=0, scale=(1.0, 1.0, 1.0)):
    rings = []
    for v in range(seg_v + 1):
        phi = math.pi * v / seg_v
        ring = []
        for u in range(seg_u):
            theta = 2.0 * math.pi * u / seg_u
            x = math.sin(phi) * math.cos(theta) * radius * scale[0]
            y = math.sin(phi) * math.sin(theta) * radius * scale[1]
            z = math.cos(phi) * radius * scale[2]
            ring.append(bm.verts.new(center + Vector((x, y, z))))
        rings.append(ring)
        
    for v in range(seg_v):
        for u in range(seg_u):
            nu = (u + 1) % seg_u
            v0 = rings[v][u]
            v1 = rings[v][nu]
            v2 = rings[v+1][nu]
            v3 = rings[v+1][u]
            if v == 0:
                f = bm.faces.new([v0, v2, v3])
            elif v == seg_v - 1:
                f = bm.faces.new([v0, v1, v2])
            else:
                f = bm.faces.new([v0, v1, v2, v3])
            f.material_index = mat_idx

# -----------------------------------------------------------------------------
# Detailed Anatomical & Facial Feature Modeling
# -----------------------------------------------------------------------------
def build_character_head(bm, head_z, ethnicity='euro', gender='male',
                         skin_idx=0, eye_idx=1, sclera_idx=2, hair_idx=3,
                         features=None):
    """
    Constructs high-fidelity stylized head & face with clear ethnic facial anatomy:
    - Asian: High cheekbones, epicanthic eye slant, refined delicate nose bridge.
    - Egyptian: Aquiline/straight aristocratic nose bridge, kohl almond eyes, sculpted jaw.
    - American: Athletic square jawline, friendly brow, modern facial planes.
    - European: Narrow prominent nose bridge, deep-set brow ridge, angular chin.
    - African: Sculpted cheekbones, wider contoured nasal base, full lips.
    """
    features = features or {}
    
    # 1. Cranium / Skull
    cranium_center = Vector((0, -0.015, head_z + 0.08))
    bmesh_add_sphere(bm, cranium_center, 0.118, seg_u=16, seg_v=12, mat_idx=skin_idx, scale=(0.96, 1.14, 1.12))
    
    # 2. Jaw & Chin
    jaw_w = 0.084 if gender == 'male' else 0.072
    chin_depth = 0.086
    if ethnicity == 'asian':
        jaw_w = 0.086
        chin_depth = 0.076
    elif ethnicity == 'egyptian':
        jaw_w = 0.080
        chin_depth = 0.092
    elif ethnicity == 'african':
        jaw_w = 0.090
        chin_depth = 0.088
        
    chin_center = Vector((0, chin_depth * 0.5, head_z - 0.06))
    bmesh_add_sphere(bm, chin_center, 0.055, seg_u=12, seg_v=8, mat_idx=skin_idx, scale=(jaw_w / 0.08, 1.25, 0.95))
    
    # 3. Cheekbones (Zygomatic Prominence)
    cheek_spread = 0.078 if ethnicity == 'asian' else 0.070
    cheek_height = head_z + 0.022 if ethnicity == 'asian' else head_z + 0.012
    bmesh_add_sphere(bm, Vector(( cheek_spread, 0.048, cheek_height)), 0.040, seg_u=10, seg_v=8, mat_idx=skin_idx, scale=(1.2, 1.0, 0.9))
    bmesh_add_sphere(bm, Vector((-cheek_spread, 0.048, cheek_height)), 0.040, seg_u=10, seg_v=8, mat_idx=skin_idx, scale=(1.2, 1.0, 0.9))
    
    # 4. Eyes (Expressive Almond Outline + Iris + Pupil + Sclera + Eyelashes/Kohl)
    eye_z = head_z + 0.038
    eye_x = 0.042
    eye_y = 0.082
    
    eye_tilt = 0.006 if ethnicity in ['asian', 'egyptian'] else 0.000
    eye_scale_z = 0.55 if ethnicity == 'asian' else (0.78 if ethnicity == 'egyptian' else 0.70)
    eye_scale_x = 1.40 if ethnicity in ['asian', 'egyptian'] else 1.25
    
    # Left eye: Sclera + Iris/Pupil + Eyelid/Kohl
    bmesh_add_sphere(bm, Vector(( eye_x, eye_y, eye_z + eye_tilt)), 0.022, seg_u=10, seg_v=8, mat_idx=sclera_idx, scale=(eye_scale_x, 0.65, eye_scale_z))
    bmesh_add_sphere(bm, Vector(( eye_x, eye_y + 0.011, eye_z + eye_tilt)), 0.014, seg_u=8, seg_v=6, mat_idx=eye_idx, scale=(1.0, 0.45, eye_scale_z))
    # Eyeliner / Kohl lashline
    bmesh_add_box(bm, Vector(( eye_x, eye_y + 0.009, eye_z + eye_tilt + 0.013)), (0.046, 0.012, 0.006), mat_idx=hair_idx)
    
    # Right eye: Sclera + Iris/Pupil + Eyelid/Kohl
    bmesh_add_sphere(bm, Vector((-eye_x, eye_y, eye_z + eye_tilt)), 0.022, seg_u=10, seg_v=8, mat_idx=sclera_idx, scale=(eye_scale_x, 0.65, eye_scale_z))
    bmesh_add_sphere(bm, Vector((-eye_x, eye_y + 0.011, eye_z + eye_tilt)), 0.014, seg_u=8, seg_v=6, mat_idx=eye_idx, scale=(1.0, 0.45, eye_scale_z))
    bmesh_add_box(bm, Vector((-eye_x, eye_y + 0.009, eye_z + eye_tilt + 0.013)), (0.046, 0.012, 0.006), mat_idx=hair_idx)
    
    # 5. Eyebrows
    brow_arch = 0.008 if ethnicity in ['egyptian', 'euro'] else 0.002
    bmesh_add_box(bm, Vector(( eye_x, eye_y + 0.010, eye_z + 0.026 + brow_arch)), (0.046, 0.014, 0.009), mat_idx=hair_idx)
    bmesh_add_box(bm, Vector((-eye_x, eye_y + 0.010, eye_z + 0.026 + brow_arch)), (0.046, 0.014, 0.009), mat_idx=hair_idx)
    
    # 6. Sculpted Nose (Aquiline / Straight / Soft)
    nose_z = head_z + 0.012
    if ethnicity == 'egyptian':
        # Noble aquiline bridge: curved high bridge with refined tip and nostrils
        bmesh_add_box(bm, Vector((0, 0.088, nose_z + 0.016)), (0.015, 0.028, 0.038), mat_idx=skin_idx)
        bmesh_add_sphere(bm, Vector((0, 0.108, nose_z - 0.008)), 0.016, seg_u=8, seg_v=6, mat_idx=skin_idx, scale=(1.0, 1.4, 0.95))
        # Nostril wings
        bmesh_add_sphere(bm, Vector(( 0.016, 0.098, nose_z - 0.010)), 0.009, seg_u=6, seg_v=6, mat_idx=skin_idx)
        bmesh_add_sphere(bm, Vector((-0.016, 0.098, nose_z - 0.010)), 0.009, seg_u=6, seg_v=6, mat_idx=skin_idx)
    elif ethnicity == 'asian':
        # Refined soft bridge, delicate tip
        bmesh_add_box(bm, Vector((0, 0.082, nose_z + 0.012)), (0.013, 0.022, 0.032), mat_idx=skin_idx)
        bmesh_add_sphere(bm, Vector((0, 0.096, nose_z - 0.008)), 0.014, seg_u=8, seg_v=6, mat_idx=skin_idx, scale=(1.1, 1.2, 0.9))
        bmesh_add_sphere(bm, Vector(( 0.014, 0.090, nose_z - 0.010)), 0.008, seg_u=6, seg_v=6, mat_idx=skin_idx)
        bmesh_add_sphere(bm, Vector((-0.014, 0.090, nose_z - 0.010)), 0.008, seg_u=6, seg_v=6, mat_idx=skin_idx)
    elif ethnicity == 'african':
        # Sculpted wider base, smooth strong bridge
        bmesh_add_box(bm, Vector((0, 0.086, nose_z + 0.010)), (0.019, 0.025, 0.030), mat_idx=skin_idx)
        bmesh_add_sphere(bm, Vector((0, 0.104, nose_z - 0.008)), 0.020, seg_u=8, seg_v=6, mat_idx=skin_idx, scale=(1.4, 1.1, 0.9))
        bmesh_add_sphere(bm, Vector(( 0.020, 0.095, nose_z - 0.010)), 0.010, seg_u=6, seg_v=6, mat_idx=skin_idx)
        bmesh_add_sphere(bm, Vector((-0.020, 0.095, nose_z - 0.010)), 0.010, seg_u=6, seg_v=6, mat_idx=skin_idx)
    else: # Euro / American
        bmesh_add_box(bm, Vector((0, 0.086, nose_z + 0.016)), (0.014, 0.026, 0.038), mat_idx=skin_idx)
        bmesh_add_sphere(bm, Vector((0, 0.105, nose_z - 0.008)), 0.015, seg_u=8, seg_v=6, mat_idx=skin_idx, scale=(1.0, 1.3, 0.95))
        bmesh_add_sphere(bm, Vector(( 0.015, 0.095, nose_z - 0.010)), 0.008, seg_u=6, seg_v=6, mat_idx=skin_idx)
        bmesh_add_sphere(bm, Vector((-0.015, 0.095, nose_z - 0.010)), 0.008, seg_u=6, seg_v=6, mat_idx=skin_idx)
        
    # 7. Lips / Mouth (Full 3D Sculpted Cupid's Bow)
    lip_z = head_z - 0.032
    lip_th = 0.014 if ethnicity == 'african' else (0.008 if ethnicity == 'asian' else 0.010)
    lip_w = 0.048 if ethnicity == 'african' else 0.040
    # Upper lip
    bmesh_add_box(bm, Vector((0, 0.088, lip_z + 0.005)), (lip_w, 0.014, lip_th), mat_idx=skin_idx)
    # Lower lip
    bmesh_add_sphere(bm, Vector((0, 0.088, lip_z - 0.005)), 0.014, seg_u=8, seg_v=6, mat_idx=skin_idx, scale=(lip_w / 0.035, 0.8, 0.6))
    
    # 8. Ears
    ear_z = head_z + 0.028
    bmesh_add_sphere(bm, Vector(( 0.112, -0.01, ear_z)), 0.026, seg_u=8, seg_v=6, mat_idx=skin_idx, scale=(0.3, 0.8, 1.4))
    bmesh_add_sphere(bm, Vector((-0.112, -0.01, ear_z)), 0.026, seg_u=8, seg_v=6, mat_idx=skin_idx, scale=(0.3, 0.8, 1.4))
    
    # 9. Neck
    neck_center = Vector((0, -0.012, head_z - 0.12))
    bmesh_add_cylinder(bm, neck_center, 0.060 if gender == 'male' else 0.050, 0.14, segments=14, mat_idx=skin_idx)

# -----------------------------------------------------------------------------
# Hair & Headwear Sculpting
# -----------------------------------------------------------------------------
def build_hair_or_headwear(bm, head_z, style, hair_mat_idx, accessory_mat_idx=None, accent_mat_idx=None):
    if style == 'asian_techwear_hair':
        # Modern undercut / textured fade with sleek top and fringe
        bmesh_add_sphere(bm, Vector((0, -0.02, head_z + 0.10)), 0.125, seg_u=14, seg_v=10, mat_idx=hair_mat_idx, scale=(0.96, 1.15, 0.95))
        bmesh_add_box(bm, Vector((0, 0.045, head_z + 0.145)), (0.095, 0.08, 0.035), mat_idx=hair_mat_idx)
        bmesh_add_box(bm, Vector((0, 0.080, head_z + 0.115)), (0.085, 0.035, 0.045), mat_idx=hair_mat_idx)
        
    elif style == 'asian_female_bun':
        # Elegant sleek hair pulled back into traditional/modern high bun
        bmesh_add_sphere(bm, Vector((0, -0.02, head_z + 0.09)), 0.124, seg_u=14, seg_v=10, mat_idx=hair_mat_idx, scale=(0.95, 1.14, 0.96))
        # Sleek Top Bun
        bmesh_add_sphere(bm, Vector((0, -0.08, head_z + 0.15)), 0.058, seg_u=12, seg_v=10, mat_idx=hair_mat_idx)
        # Golden hairpin ornament
        if accent_mat_idx:
            bmesh_add_cylinder(bm, Vector((0, -0.08, head_z + 0.15)), 0.005, 0.18, segments=8, mat_idx=accent_mat_idx)
            bmesh_add_sphere(bm, Vector((0.09, -0.08, head_z + 0.15)), 0.014, seg_u=8, seg_v=6, mat_idx=accent_mat_idx)

    elif style == 'egyptian_kufi_cap':
        cap_mat = accessory_mat_idx if accessory_mat_idx else hair_mat_idx
        # Embroidered traditional Kufi / Taqiyah skullcap
        bmesh_add_cylinder(bm, Vector((0, -0.01, head_z + 0.11)), 0.120, 0.075, segments=18, mat_idx=cap_mat, top_cap=True)
        # Gold/embroidered trim along rim
        if accent_mat_idx:
            bmesh_add_cylinder(bm, Vector((0, -0.01, head_z + 0.075)), 0.124, 0.016, segments=18, mat_idx=accent_mat_idx)
            
    elif style == 'egyptian_female_wrap':
        # Draped Egyptian silk headscarf / contemporary elegant wrap (placed back to unveil face & eyes)
        wrap_mat = accessory_mat_idx if accessory_mat_idx else hair_mat_idx
        bmesh_add_sphere(bm, Vector((0, -0.045, head_z + 0.105)), 0.126, seg_u=14, seg_v=12, mat_idx=wrap_mat, scale=(0.96, 1.05, 1.08))
        # Shoulder drapes
        bmesh_add_box(bm, Vector(( 0.105, -0.02, head_z - 0.08)), (0.045, 0.10, 0.19), mat_idx=wrap_mat)
        bmesh_add_box(bm, Vector((-0.105, -0.02, head_z - 0.08)), (0.045, 0.10, 0.19), mat_idx=wrap_mat)
        if accent_mat_idx:
            bmesh_add_box(bm, Vector((0, 0.04, head_z + 0.12)), (0.13, 0.015, 0.018), mat_idx=accent_mat_idx)

    elif style == 'american_baseball_cap':
        cap_mat = accessory_mat_idx if accessory_mat_idx else hair_mat_idx
        # Crown
        bmesh_add_sphere(bm, Vector((0, -0.015, head_z + 0.105)), 0.126, seg_u=14, seg_v=10, mat_idx=cap_mat, scale=(0.96, 1.14, 0.86))
        # Visor / Brim
        bmesh_add_box(bm, Vector((0, 0.135, head_z + 0.085)), (0.135, 0.095, 0.010), mat_idx=cap_mat)
        # Button on top
        bmesh_add_sphere(bm, Vector((0, -0.01, head_z + 0.185)), 0.012, seg_u=8, seg_v=6, mat_idx=cap_mat)
        # Hair peeking underneath
        bmesh_add_box(bm, Vector((0, -0.085, head_z + 0.02)), (0.11, 0.045, 0.065), mat_idx=hair_mat_idx)

    elif style == 'american_ponytail':
        # Hair pulled back with bouncy high ponytail
        bmesh_add_sphere(bm, Vector((0, -0.015, head_z + 0.085)), 0.122, seg_u=14, seg_v=10, mat_idx=hair_mat_idx, scale=(0.95, 1.14, 0.95))
        # Ponytail knot
        bmesh_add_sphere(bm, Vector((0, -0.10, head_z + 0.12)), 0.038, seg_u=10, seg_v=8, mat_idx=hair_mat_idx)
        # Flowing ponytail
        bmesh_add_box(bm, Vector((0, -0.145, head_z + 0.04)), (0.05, 0.055, 0.17), mat_idx=hair_mat_idx)

    elif style == 'euro_classic_parted':
        # European classic gentleman side-part with neat volume
        bmesh_add_sphere(bm, Vector((0, -0.02, head_z + 0.085)), 0.124, seg_u=14, seg_v=10, mat_idx=hair_mat_idx, scale=(0.96, 1.15, 0.95))
        bmesh_add_box(bm, Vector((-0.035, 0.025, head_z + 0.145)), (0.085, 0.095, 0.030), mat_idx=hair_mat_idx)
        bmesh_add_box(bm, Vector(( 0.045, 0.025, head_z + 0.135)), (0.065, 0.085, 0.024), mat_idx=hair_mat_idx)

    elif style == 'euro_wavy_hair':
        # Flowing medium-length elegant wavy locks
        bmesh_add_sphere(bm, Vector((0, -0.02, head_z + 0.085)), 0.126, seg_u=14, seg_v=10, mat_idx=hair_mat_idx, scale=(0.97, 1.16, 0.98))
        bmesh_add_box(bm, Vector(( 0.095, 0.00, head_z - 0.02)), (0.045, 0.095, 0.19), mat_idx=hair_mat_idx)
        bmesh_add_box(bm, Vector((-0.095, 0.00, head_z - 0.02)), (0.045, 0.095, 0.19), mat_idx=hair_mat_idx)
        bmesh_add_box(bm, Vector(( 0.00, -0.095, head_z - 0.04)), (0.13, 0.055, 0.21), mat_idx=hair_mat_idx)

    elif style == 'nordic_beanie':
        beanie_mat = accessory_mat_idx if accessory_mat_idx else hair_mat_idx
        bmesh_add_sphere(bm, Vector((0, -0.015, head_z + 0.095)), 0.128, seg_u=14, seg_v=10, mat_idx=beanie_mat, scale=(0.98, 1.15, 0.95))
        bmesh_add_cylinder(bm, Vector((0, -0.01, head_z + 0.068)), 0.130, 0.038, segments=18, mat_idx=beanie_mat)
        if accent_mat_idx:
            bmesh_add_sphere(bm, Vector((0, -0.02, head_z + 0.21)), 0.035, seg_u=10, seg_v=8, mat_idx=accent_mat_idx)

    elif style == 'african_short_fade':
        # Crisp sculpted buzzcut / modern textured fade
        bmesh_add_sphere(bm, Vector((0, -0.015, head_z + 0.082)), 0.120, seg_u=16, seg_v=12, mat_idx=hair_mat_idx, scale=(0.95, 1.13, 0.95))

# -----------------------------------------------------------------------------
# Facial Hair Sculpting (Beards & Mustaches)
# -----------------------------------------------------------------------------
def build_facial_hair(bm, head_z, style, hair_mat_idx):
    if style == 'egyptian_neat_beard':
        # Sculpted royal/noble chin goatee & mustache with sharp contours
        bmesh_add_box(bm, Vector((0, 0.090, head_z - 0.020)), (0.046, 0.012, 0.012), mat_idx=hair_mat_idx)
        bmesh_add_box(bm, Vector((0, 0.080, head_z - 0.065)), (0.036, 0.026, 0.048), mat_idx=hair_mat_idx)
    elif style == 'nordic_full_beard':
        # Full thick explorer / Nordic beard and mustache
        bmesh_add_box(bm, Vector((0, 0.092, head_z - 0.020)), (0.056, 0.018, 0.016), mat_idx=hair_mat_idx)
        bmesh_add_sphere(bm, Vector((0, 0.076, head_z - 0.085)), 0.070, seg_u=12, seg_v=10, mat_idx=hair_mat_idx, scale=(1.15, 1.25, 1.25))
    elif style == 'trim_goatee':
        # Subtle neat designer stubble / goatee
        bmesh_add_box(bm, Vector((0, 0.086, head_z - 0.020)), (0.042, 0.009, 0.009), mat_idx=hair_mat_idx)
        bmesh_add_box(bm, Vector((0, 0.076, head_z - 0.060)), (0.030, 0.016, 0.028), mat_idx=hair_mat_idx)

# -----------------------------------------------------------------------------
# Base Humanoid Body Construction
# -----------------------------------------------------------------------------
def build_humanoid_body(bm, height_scale=1.0, gender='male',
                        skin_idx=0, top_idx=4, bot_idx=5, shoe_idx=6,
                        coat_idx=None, accent_idx=None, clothing_style='casual'):
    def S(z): return z * height_scale
    
    shoulder_w = 0.22 if gender == 'male' else 0.17
    chest_depth = 0.12 if gender == 'male' else 0.11
    waist_w = 0.16 if gender == 'male' else 0.13
    hip_w = 0.165 if gender == 'male' else 0.175
    
    # 1. Torso / Upper Garment (Chest, Ribcage, Abdomen)
    chest_z = S(1.35)
    chest_mat = coat_idx if coat_idx else top_idx
    bmesh_add_box(bm, Vector((0, 0, chest_z)), (shoulder_w * 1.8, chest_depth * 1.8, S(0.24)), mat_idx=chest_mat)
    
    # Abdomen / Waist
    abdomen_z = S(1.15)
    bmesh_add_box(bm, Vector((0, 0, abdomen_z)), (waist_w * 1.8, (chest_depth - 0.015) * 1.8, S(0.20)), mat_idx=top_idx)
    
    # 2. Pelvis / Hips / Lower Garment
    pelvis_z = S(0.95)
    bmesh_add_box(bm, Vector((0, 0, pelvis_z)), (hip_w * 1.85, (chest_depth - 0.01) * 1.8, S(0.18)), mat_idx=bot_idx)
    
    # Belt / Waistband detail
    if accent_idx:
        bmesh_add_box(bm, Vector((0, 0, S(1.04))), (waist_w * 1.88, (chest_depth - 0.01) * 1.88, 0.035), mat_idx=accent_idx)
        # Buckle
        bmesh_add_box(bm, Vector((0, chest_depth * 0.95, S(1.04))), (0.045, 0.012, 0.030), mat_idx=accent_idx)
        
    # 3. Arms & Hands
    arm_r = 0.046 if gender == 'male' else 0.038
    bmesh_add_cylinder(bm, Vector(( shoulder_w + 0.08, 0, S(1.35))), arm_r, S(0.24), segments=12, mat_idx=chest_mat)
    bmesh_add_cylinder(bm, Vector((-shoulder_w - 0.08, 0, S(1.35))), arm_r, S(0.24), segments=12, mat_idx=chest_mat)
    
    forearm_mat = skin_idx if clothing_style in ['tshirt', 'short_sleeve'] else chest_mat
    bmesh_add_cylinder(bm, Vector(( shoulder_w + 0.08, 0, S(1.12))), arm_r * 0.9, S(0.24), segments=12, mat_idx=forearm_mat)
    bmesh_add_cylinder(bm, Vector((-shoulder_w - 0.08, 0, S(1.12))), arm_r * 0.9, S(0.24), segments=12, mat_idx=forearm_mat)
    
    # Hands (Sculpted palms & thumbs)
    bmesh_add_box(bm, Vector(( shoulder_w + 0.08, 0.01, S(0.96))), (0.034, 0.068, 0.088), mat_idx=skin_idx)
    bmesh_add_box(bm, Vector((-shoulder_w - 0.08, 0.01, S(0.96))), (0.034, 0.068, 0.088), mat_idx=skin_idx)
    bmesh_add_box(bm, Vector(( shoulder_w + 0.065, 0.038, S(0.97))), (0.016, 0.026, 0.036), mat_idx=skin_idx)
    bmesh_add_box(bm, Vector((-shoulder_w - 0.065, 0.038, S(0.97))), (0.016, 0.026, 0.036), mat_idx=skin_idx)
    
    # 4. Legs (Thighs & Calves)
    leg_x = 0.105
    thigh_r = 0.068 if gender == 'male' else 0.062
    bmesh_add_cylinder(bm, Vector(( leg_x, 0, S(0.72))), thigh_r, S(0.38), segments=14, mat_idx=bot_idx)
    bmesh_add_cylinder(bm, Vector((-leg_x, 0, S(0.72))), thigh_r, S(0.38), segments=14, mat_idx=bot_idx)
    
    calf_r = 0.055 if gender == 'male' else 0.048
    bmesh_add_cylinder(bm, Vector(( leg_x, 0, S(0.34))), calf_r, S(0.40), segments=14, mat_idx=bot_idx)
    bmesh_add_cylinder(bm, Vector((-leg_x, 0, S(0.34))), calf_r, S(0.40), segments=14, mat_idx=bot_idx)
    
    # 5. Shoes / Boots / Sneakers with Soles
    shoe_len = 0.23
    shoe_w = 0.088
    shoe_h = 0.095
    # Left Shoe & Sole
    bmesh_add_box(bm, Vector(( leg_x, 0.045, S(0.065))), (shoe_w, shoe_len, shoe_h), mat_idx=shoe_idx)
    bmesh_add_box(bm, Vector(( leg_x, 0.045, S(0.015))), (shoe_w + 0.008, shoe_len + 0.008, 0.025), mat_idx=shoe_idx)
    # Right Shoe & Sole
    bmesh_add_box(bm, Vector((-leg_x, 0.045, S(0.065))), (shoe_w, shoe_len, shoe_h), mat_idx=shoe_idx)
    bmesh_add_box(bm, Vector((-leg_x, 0.045, S(0.015))), (shoe_w + 0.008, shoe_len + 0.008, 0.025), mat_idx=shoe_idx)

# -----------------------------------------------------------------------------
# Specific Garment Overlays (Long Coats, Galabeyas, Hoodies, etc.)
# -----------------------------------------------------------------------------
def build_garment_overlay(bm, height_scale, overlay_type, mat_idx, accent_idx=None):
    def S(z): return z * height_scale
    
    if overlay_type == 'galabeya_thobe':
        # Flowing traditional Egyptian / Arab ankle-length linen robe
        bmesh_add_cylinder(bm, Vector((0, 0, S(0.85))), 0.25, S(0.95), segments=18, mat_idx=mat_idx)
        bmesh_add_cylinder(bm, Vector((0, 0, S(0.32))), 0.29, S(0.45), segments=20, mat_idx=mat_idx)
        if accent_idx:
            # Embroidered golden collar & chest placket
            bmesh_add_box(bm, Vector((0, 0.125, S(1.35))), (0.085, 0.022, S(0.24)), mat_idx=accent_idx)
            bmesh_add_box(bm, Vector((0, 0.105, S(1.47))), (0.17, 0.022, 0.028), mat_idx=accent_idx)
            # Waist sash cord
            bmesh_add_cylinder(bm, Vector((0, 0, S(1.02))), 0.255, 0.038, segments=20, mat_idx=accent_idx)

    elif overlay_type == 'european_wool_overcoat':
        # Long double-breasted tailored wool overcoat down below knees
        bmesh_add_box(bm, Vector((0, -0.01, S(1.28))), (0.47, 0.27, S(0.38)), mat_idx=mat_idx)
        # Lapels
        bmesh_add_box(bm, Vector(( 0.085, 0.13, S(1.36))), (0.075, 0.022, 0.19), mat_idx=mat_idx)
        bmesh_add_box(bm, Vector((-0.085, 0.13, S(1.36))), (0.075, 0.022, 0.19), mat_idx=mat_idx)
        # Long coat skirts
        bmesh_add_box(bm, Vector((0, -0.01, S(0.75))), (0.49, 0.29, S(0.68)), mat_idx=mat_idx)
        # Scarf wrapped around neck
        if accent_idx:
            bmesh_add_cylinder(bm, Vector((0, 0, S(1.46))), 0.120, 0.070, segments=14, mat_idx=accent_idx)
            bmesh_add_box(bm, Vector((0.055, 0.115, S(1.35))), (0.065, 0.032, 0.19), mat_idx=accent_idx)

    elif overlay_type == 'european_belted_trench':
        # Belted classic camel trench coat
        bmesh_add_box(bm, Vector((0, -0.01, S(1.26))), (0.43, 0.26, S(0.38)), mat_idx=mat_idx)
        bmesh_add_box(bm, Vector((0, -0.01, S(0.76))), (0.46, 0.28, S(0.64)), mat_idx=mat_idx)
        bmesh_add_box(bm, Vector(( 0.19, 0, S(1.47))), (0.085, 0.042, 0.016), mat_idx=mat_idx)
        bmesh_add_box(bm, Vector((-0.19, 0, S(1.47))), (0.085, 0.042, 0.016), mat_idx=mat_idx)
        if accent_idx:
            bmesh_add_box(bm, Vector((0, -0.01, S(1.05))), (0.44, 0.27, 0.042), mat_idx=accent_idx)
            bmesh_add_box(bm, Vector((0, 0.135, S(1.05))), (0.055, 0.022, 0.042), mat_idx=accent_idx)

    elif overlay_type == 'hoodie_sweatshirt':
        # Relaxed casual American hoodie
        bmesh_add_box(bm, Vector((0, 0, S(1.30))), (0.45, 0.27, S(0.34)), mat_idx=mat_idx)
        # Kangaroo pocket
        bmesh_add_box(bm, Vector((0, 0.13, S(1.12))), (0.25, 0.032, 0.15), mat_idx=mat_idx)
        # Hood hanging behind neck
        bmesh_add_box(bm, Vector((0, -0.115, S(1.44))), (0.19, 0.11, 0.15), mat_idx=mat_idx)

    elif overlay_type == 'crop_windbreaker':
        # Cropped athleisure jacket
        bmesh_add_box(bm, Vector((0, 0, S(1.34))), (0.43, 0.26, S(0.24)), mat_idx=mat_idx)
        if accent_idx:
            bmesh_add_box(bm, Vector((0, 0.13, S(1.34))), (0.022, 0.016, S(0.24)), mat_idx=accent_idx)

    elif overlay_type == 'mandarin_jacket':
        # Traditional/Modern Chinese / East Asian jacket (Tangzhuang)
        bmesh_add_box(bm, Vector((0, 0, S(1.28))), (0.44, 0.26, S(0.40)), mat_idx=mat_idx)
        bmesh_add_cylinder(bm, Vector((0, 0, S(1.48))), 0.088, 0.042, segments=14, mat_idx=mat_idx)
        if accent_idx:
            for b_z in [1.15, 1.25, 1.35, 1.43]:
                bmesh_add_box(bm, Vector((0, 0.13, S(b_z))), (0.048, 0.016, 0.014), mat_idx=accent_idx)

    elif overlay_type == 'suit_jacket_executive':
        # Tailored single-breasted suit jacket
        bmesh_add_box(bm, Vector((0, 0, S(1.27))), (0.45, 0.26, S(0.42)), mat_idx=mat_idx)
        bmesh_add_box(bm, Vector(( 0.075, 0.125, S(1.35))), (0.065, 0.022, 0.19), mat_idx=mat_idx)
        bmesh_add_box(bm, Vector((-0.075, 0.125, S(1.35))), (0.065, 0.022, 0.19), mat_idx=mat_idx)
        if accent_idx:
            bmesh_add_box(bm, Vector((0, 0.115, S(1.38))), (0.055, 0.016, 0.15), mat_idx=accent_idx)
            bmesh_add_box(bm, Vector((0, 0.125, S(1.25))), (0.030, 0.014, 0.21), mat_idx=accent_idx)

# -----------------------------------------------------------------------------
# Complete Archetype Definition & Generation
# -----------------------------------------------------------------------------
def build_archetype_character(spec, materials):
    name = spec['name']
    print(f"  -> Building NPC: {name} ({spec['description']})...")
    
    height_scale = spec.get('height_scale', 1.0)
    arm_obj = create_humanoid_armature(name, height_scale)
    
    mesh_data = bpy.data.meshes.new(f"{name}_Mesh")
    mesh_obj = bpy.data.objects.new(name, mesh_data)
    bpy.context.collection.objects.link(mesh_obj)
    
    mat_list = spec['materials']
    mat_indices = {}
    for idx, mat_key in enumerate(mat_list):
        m = materials[mat_key]
        mesh_obj.data.materials.append(m)
        mat_indices[mat_key] = idx
        
    bm = bmesh.new()
    
    skin_idx = mat_indices[spec['skin_mat']]
    top_idx = mat_indices[spec['top_mat']]
    bot_idx = mat_indices[spec['bot_mat']]
    shoe_idx = mat_indices[spec['shoe_mat']]
    coat_idx = mat_indices.get(spec.get('coat_mat'))
    accent_idx = mat_indices.get(spec.get('accent_mat'))
    
    build_humanoid_body(
        bm, height_scale=height_scale, gender=spec.get('gender', 'male'),
        skin_idx=skin_idx, top_idx=top_idx, bot_idx=bot_idx, shoe_idx=shoe_idx,
        coat_idx=coat_idx, accent_idx=accent_idx, clothing_style=spec.get('clothing_style', 'casual')
    )
    
    head_z = 1.62 * height_scale
    eye_idx = mat_indices[spec['eye_mat']]
    sclera_idx = mat_indices['Eye_Sclera']
    hair_idx = mat_indices[spec['hair_mat']]
    
    build_character_head(
        bm, head_z, ethnicity=spec['ethnicity'], gender=spec.get('gender', 'male'),
        skin_idx=skin_idx, eye_idx=eye_idx, sclera_idx=sclera_idx, hair_idx=hair_idx
    )
    
    acc_idx = mat_indices.get(spec.get('accessory_mat'))
    build_hair_or_headwear(
        bm, head_z, spec['hair_style'], hair_idx,
        accessory_mat_idx=acc_idx, accent_mat_idx=accent_idx
    )
    
    if 'facial_hair_style' in spec and spec['facial_hair_style']:
        build_facial_hair(bm, head_z, spec['facial_hair_style'], hair_idx)
        
    if 'overlay_type' in spec and spec['overlay_type']:
        ov_mat = mat_indices[spec['overlay_mat']]
        ov_acc = mat_indices.get(spec.get('overlay_accent'))
        build_garment_overlay(bm, height_scale, spec['overlay_type'], ov_mat, accent_idx=ov_acc)
        
    bm.to_mesh(mesh_data)
    bm.free()
    
    for poly in mesh_data.polygons:
        poly.use_smooth = True
        
    mesh_obj.parent = arm_obj
    auto_skin_mesh_to_bones(mesh_obj, arm_obj, height_scale)
    
    return arm_obj, mesh_obj

# -----------------------------------------------------------------------------
# Export Models to FBX for Unity & Set Up Showroom Parade
# -----------------------------------------------------------------------------
def export_individual_fbx(arm_obj, mesh_obj, output_path):
    bpy.ops.object.select_all(action='DESELECT')
    arm_obj.select_set(True)
    mesh_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        check_existing=False,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'ARMATURE', 'MESH'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        armature_nodetype='NULL'
    )
    print(f"    Exported: {os.path.basename(output_path)}")

# -----------------------------------------------------------------------------
# Showroom Setup & High-Res Render (Two Front-Facing Rows)
# -----------------------------------------------------------------------------
def setup_showroom_and_render(characters):
    print("[NPC Generator] Setting up showroom parade and high-resolution lighting...")
    
    # Elegant 2-tier showroom pedestals
    # Front stage
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.9, -0.15))
    front_stage = bpy.context.active_object
    front_stage.name = "Showroom_Stage_Front"
    front_stage.scale = (12.0, 1.8, 0.3)
    stage_mat = create_pbr_material("M_Showroom_Stage", (0.10, 0.11, 0.13, 1.0), roughness=0.30)
    front_stage.data.materials.append(stage_mat)
    
    # Back elevated stage (+0.35m height)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -1.1, 0.05))
    back_stage = bpy.context.active_object
    back_stage.name = "Showroom_Stage_Back"
    back_stage.scale = (12.0, 2.0, 0.7)
    back_stage.data.materials.append(stage_mat)
    
    # Backdrop wall
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -2.8, 2.5))
    backdrop = bpy.context.active_object
    backdrop.name = "Showroom_Backdrop"
    backdrop.scale = (16.0, 0.4, 5.5)
    wall_mat = create_pbr_material("M_Showroom_Wall", (0.82, 0.85, 0.88, 1.0), roughness=0.60)
    backdrop.data.materials.append(wall_mat)
    
    # Arrange 12 characters: 6 in front row, 6 in back row (interleaved so everyone is fully visible)
    front_row = characters[0:6]
    back_row = characters[6:12]
    
    # Front row: x spaced by 1.40m, y = 0.9, z = 0.0
    for idx, (arm_obj, mesh_obj) in enumerate(front_row):
        t = (idx - 2.5) * 1.40
        arm_obj.location = (t, 0.9, 0.0)
        angle_z = -t * 0.04
        arm_obj.rotation_euler = (0, 0, angle_z)
        
    # Back row: interleaved (+0.70m offset), elevated on step (+0.40m z), y = -1.1
    for idx, (arm_obj, mesh_obj) in enumerate(back_row):
        t = (idx - 2.5) * 1.40 + 0.70
        arm_obj.location = (t, -1.1, 0.40)
        angle_z = -t * 0.04
        arm_obj.rotation_euler = (0, 0, angle_z)
        
    # Lighting: 3-Point Studio Setup
    # Key Sun Light (Warm Key)
    key_light_data = bpy.data.lights.new(name="Light_Key_Sun", type='SUN')
    key_light_data.energy = 4.0
    key_light_data.color = (1.0, 0.97, 0.92)
    key_light_obj = bpy.data.objects.new("Light_Key_Sun", key_light_data)
    bpy.context.collection.objects.link(key_light_obj)
    key_light_obj.location = (5.0, 9.0, 8.0)
    key_light_obj.rotation_euler = (math.radians(-50), math.radians(12), math.radians(145))
    
    # Fill Area Light (Soft Sky)
    fill_light_data = bpy.data.lights.new(name="Light_Fill_Area", type='AREA')
    fill_light_data.energy = 3800.0
    fill_light_data.size = 14.0
    fill_light_data.color = (0.78, 0.88, 1.0)
    fill_light_obj = bpy.data.objects.new("Light_Fill_Area", fill_light_data)
    bpy.context.collection.objects.link(fill_light_obj)
    fill_light_obj.location = (-6.0, 9.0, 5.0)
    fill_light_obj.rotation_euler = (math.radians(-52), 0, math.radians(-32))
    
    # Rim / Backlight
    rim_light_data = bpy.data.lights.new(name="Light_Rim_Area", type='AREA')
    rim_light_data.energy = 5500.0
    rim_light_data.size = 16.0
    rim_light_data.color = (1.0, 0.98, 0.90)
    rim_light_obj = bpy.data.objects.new("Light_Rim_Area", rim_light_data)
    bpy.context.collection.objects.link(rim_light_obj)
    rim_light_obj.location = (0, -3.5, 5.0)
    rim_light_obj.rotation_euler = (math.radians(65), 0, 0)
    
    # Panoramic Camera (Wide 32mm lens, captures all 12 characters comfortably)
    cam_data = bpy.data.cameras.new("Showroom_Camera")
    cam_data.lens = 32
    cam_data.clip_start = 0.1
    cam_data.clip_end = 100.0
    cam_obj = bpy.data.objects.new("Showroom_Camera", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    cam_obj.location = (0.35, 10.2, 1.90)
    cam_obj.rotation_euler = (math.radians(85), 0, math.radians(180))
    
    # Render Settings
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE_NEXT' if hasattr(bpy.types, 'RenderEngineEEVEENext') else 'BLENDER_EEVEE'
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100
    scene.render.filepath = SHOWCASE_RENDER_PATH
    
    print(f"[NPC Generator] Rendering full showroom parade to: {SHOWCASE_RENDER_PATH}...")
    bpy.ops.render.render(write_still=True)
    if os.path.exists(SHOWCASE_RENDER_PATH):
        shutil.copy2(SHOWCASE_RENDER_PATH, ROOT_SHOWCASE_RENDER)
        print(f"[NPC Generator] Showcase parade saved successfully ({os.path.getsize(SHOWCASE_RENDER_PATH)} bytes).")
        
    # Close-up Cultural Portrait Showcase Render
    PORTRAIT_RENDER_PATH = os.path.join(MODELS_DIR, "npc_portraits_showcase.png")
    ROOT_PORTRAIT_RENDER = os.path.join(PROJECT_DIR, "npc_portraits_showcase.png")
    
    cam_data_close = bpy.data.cameras.new("Portrait_Camera")
    cam_data_close.lens = 55 # 55mm portrait lens
    cam_close_obj = bpy.data.objects.new("Portrait_Camera", cam_data_close)
    bpy.context.collection.objects.link(cam_close_obj)
    bpy.context.scene.camera = cam_close_obj
    cam_close_obj.location = (0.7, 3.6, 1.48)
    cam_close_obj.rotation_euler = (math.radians(88), 0, math.radians(180))
    
    scene.render.filepath = PORTRAIT_RENDER_PATH
    print(f"[NPC Generator] Rendering portrait close-up to: {PORTRAIT_RENDER_PATH}...")
    bpy.ops.render.render(write_still=True)
    if os.path.exists(PORTRAIT_RENDER_PATH):
        shutil.copy2(PORTRAIT_RENDER_PATH, ROOT_PORTRAIT_RENDER)
        print(f"[NPC Generator] Portrait close-up saved ({os.path.getsize(PORTRAIT_RENDER_PATH)} bytes).")

# -----------------------------------------------------------------------------
# Main Execution
# -----------------------------------------------------------------------------
def main():
    reset_scene()
    materials = setup_all_materials()
    
    archetypes = [
        # Front Row: 1 to 6
        # 1. Asian Male Techwear
        {
            'name': 'NPC_Asian_Male_Techwear',
            'description': 'East Asian modern streetwear/techwear with almond eyes, high cheekbones, high-top kicks',
            'ethnicity': 'asian',
            'gender': 'male',
            'height_scale': 1.0,
            'skin_mat': 'Skin_Asian_Golden',
            'eye_mat': 'Eye_DarkBrown',
            'hair_mat': 'Hair_SilkyBlack',
            'hair_style': 'asian_techwear_hair',
            'facial_hair_style': None,
            'top_mat': 'Fab_TechwearBlack',
            'bot_mat': 'Fab_TechwearBlack',
            'shoe_mat': 'Sneaker_White',
            'accent_mat': 'Fab_TechwearNeon',
            'overlay_type': 'crop_windbreaker',
            'overlay_mat': 'Fab_TechwearBlack',
            'overlay_accent': 'Fab_TechwearNeon',
            'clothing_style': 'techwear',
            'materials': ['Skin_Asian_Golden', 'Eye_DarkBrown', 'Eye_Sclera', 'Hair_SilkyBlack',
                          'Fab_TechwearBlack', 'Sneaker_White', 'Fab_TechwearNeon']
        },
        # 2. Asian Female Silk Elegance
        {
            'name': 'NPC_Asian_Female_Silk',
            'description': 'East Asian female with delicate features, high hair bun with gold ornament, modern silk qipao',
            'ethnicity': 'asian',
            'gender': 'female',
            'height_scale': 0.95,
            'skin_mat': 'Skin_Asian_Golden',
            'eye_mat': 'Eye_Black',
            'hair_mat': 'Hair_SilkyBlack',
            'hair_style': 'asian_female_bun',
            'facial_hair_style': None,
            'top_mat': 'Fab_AsianSilkRed',
            'bot_mat': 'Fab_AsianSilkRed',
            'shoe_mat': 'Leather_Black',
            'accent_mat': 'Fab_AsianSilkGold',
            'overlay_type': 'mandarin_jacket',
            'overlay_mat': 'Fab_AsianSilkRed',
            'overlay_accent': 'Fab_AsianSilkGold',
            'clothing_style': 'formal',
            'materials': ['Skin_Asian_Golden', 'Eye_Black', 'Eye_Sclera', 'Hair_SilkyBlack',
                          'Fab_AsianSilkRed', 'Leather_Black', 'Fab_AsianSilkGold']
        },
        # 3. Egyptian Male Galabeya
        {
            'name': 'NPC_Egyptian_Male_Galabeya',
            'description': 'Egyptian male with aquiline nose, almond eyes, neat sculpted beard, linen galabeya with gold embroidery',
            'ethnicity': 'egyptian',
            'gender': 'male',
            'height_scale': 1.02,
            'skin_mat': 'Skin_Egyptian_Bronze',
            'eye_mat': 'Eye_AmberGreen',
            'hair_mat': 'Hair_DarkBrown',
            'hair_style': 'egyptian_kufi_cap',
            'accessory_mat': 'Fab_EgyptianLinen',
            'facial_hair_style': 'egyptian_neat_beard',
            'top_mat': 'Fab_EgyptianLinen',
            'bot_mat': 'Fab_EgyptianLinen',
            'shoe_mat': 'Leather_DarkBrown',
            'accent_mat': 'Fab_GoldTrim',
            'overlay_type': 'galabeya_thobe',
            'overlay_mat': 'Fab_EgyptianLinen',
            'overlay_accent': 'Fab_GoldTrim',
            'clothing_style': 'traditional',
            'materials': ['Skin_Egyptian_Bronze', 'Eye_AmberGreen', 'Eye_Sclera', 'Hair_DarkBrown',
                          'Fab_EgyptianLinen', 'Leather_DarkBrown', 'Fab_GoldTrim']
        },
        # 4. Egyptian Female Alexandria
        {
            'name': 'NPC_Egyptian_Female_Alexandria',
            'description': 'Egyptian female with prominent almond eyes, elegant silk headwrap, contemporary flowing tunic',
            'ethnicity': 'egyptian',
            'gender': 'female',
            'height_scale': 0.96,
            'skin_mat': 'Skin_Egyptian_Bronze',
            'eye_mat': 'Eye_DarkBrown',
            'hair_mat': 'Hair_SilkyBlack',
            'hair_style': 'egyptian_female_wrap',
            'accessory_mat': 'Fab_EgyptianTeal',
            'facial_hair_style': None,
            'top_mat': 'Fab_EgyptianLinen',
            'bot_mat': 'Fab_EgyptianTeal',
            'shoe_mat': 'Leather_DarkBrown',
            'accent_mat': 'Fab_GoldTrim',
            'overlay_type': None,
            'clothing_style': 'casual',
            'materials': ['Skin_Egyptian_Bronze', 'Eye_DarkBrown', 'Eye_Sclera', 'Hair_SilkyBlack',
                          'Fab_EgyptianLinen', 'Fab_EgyptianTeal', 'Leather_DarkBrown', 'Fab_GoldTrim']
        },
        # 5. American Male Streetwear
        {
            'name': 'NPC_American_Male_Streetwear',
            'description': 'American casual streetwear with athletic jawline, baseball cap, hoodie, denim jeans, sneakers',
            'ethnicity': 'american',
            'gender': 'male',
            'height_scale': 1.01,
            'skin_mat': 'Skin_Euro_Fair',
            'eye_mat': 'Eye_OceanBlue',
            'hair_mat': 'Hair_Chestnut',
            'hair_style': 'american_baseball_cap',
            'accessory_mat': 'Fab_CapNavy',
            'facial_hair_style': 'trim_goatee',
            'top_mat': 'Fab_CottonGrey',
            'bot_mat': 'Fab_DenimBlue',
            'shoe_mat': 'Sneaker_White',
            'accent_mat': 'Sneaker_Accent',
            'overlay_type': 'hoodie_sweatshirt',
            'overlay_mat': 'Fab_CottonGrey',
            'overlay_accent': 'Sneaker_Accent',
            'clothing_style': 'streetwear',
            'materials': ['Skin_Euro_Fair', 'Eye_OceanBlue', 'Eye_Sclera', 'Hair_Chestnut',
                          'Fab_CapNavy', 'Fab_CottonGrey', 'Fab_DenimBlue', 'Sneaker_White', 'Sneaker_Accent']
        },
        # 6. American Female Athleisure
        {
            'name': 'NPC_American_Female_Athleisure',
            'description': 'American athleisure with high ponytail, cropped windbreaker jacket, cargo trackpants, running shoes',
            'ethnicity': 'american',
            'gender': 'female',
            'height_scale': 0.97,
            'skin_mat': 'Skin_Olive_Med',
            'eye_mat': 'Eye_AmberGreen',
            'hair_mat': 'Hair_DarkBrown',
            'hair_style': 'american_ponytail',
            'facial_hair_style': None,
            'top_mat': 'Fab_AthleisureLilac',
            'bot_mat': 'Fab_TechwearBlack',
            'shoe_mat': 'Sneaker_White',
            'accent_mat': 'Sneaker_Accent',
            'overlay_type': 'crop_windbreaker',
            'overlay_mat': 'Fab_AthleisureLilac',
            'overlay_accent': 'Sneaker_Accent',
            'clothing_style': 'athleisure',
            'materials': ['Skin_Olive_Med', 'Eye_AmberGreen', 'Eye_Sclera', 'Hair_DarkBrown',
                          'Fab_AthleisureLilac', 'Fab_TechwearBlack', 'Sneaker_White', 'Sneaker_Accent']
        },
        
        # Back Row: 7 to 12 (Elevated on Step)
        # 7. European Male Classic Overcoat
        {
            'name': 'NPC_European_Male_Overcoat',
            'description': 'European classic gentleman with chiseled jaw, parted hair, charcoal wool overcoat, burgundy scarf',
            'ethnicity': 'euro',
            'gender': 'male',
            'height_scale': 1.03,
            'skin_mat': 'Skin_Nordic_Pale',
            'eye_mat': 'Eye_OceanBlue',
            'hair_mat': 'Hair_SilverGray',
            'hair_style': 'euro_classic_parted',
            'facial_hair_style': None,
            'top_mat': 'Fab_WoolCharcoal',
            'bot_mat': 'Fab_NavySuit',
            'shoe_mat': 'Leather_Black',
            'accent_mat': 'Fab_ScarfBurgundy',
            'overlay_type': 'european_wool_overcoat',
            'overlay_mat': 'Fab_WoolCharcoal',
            'overlay_accent': 'Fab_ScarfBurgundy',
            'clothing_style': 'formal',
            'materials': ['Skin_Nordic_Pale', 'Eye_OceanBlue', 'Eye_Sclera', 'Hair_SilverGray',
                          'Fab_WoolCharcoal', 'Fab_NavySuit', 'Leather_Black', 'Fab_ScarfBurgundy']
        },
        # 8. European Female Belted Trench
        {
            'name': 'NPC_European_Female_Trench',
            'description': 'European chic female with wavy locks, belted camel trench coat, turtleneck, leather ankle boots',
            'ethnicity': 'euro',
            'gender': 'female',
            'height_scale': 0.98,
            'skin_mat': 'Skin_Euro_Fair',
            'eye_mat': 'Eye_OceanBlue',
            'hair_mat': 'Hair_Blonde',
            'hair_style': 'euro_wavy_hair',
            'facial_hair_style': None,
            'top_mat': 'Fab_TrenchCamel',
            'bot_mat': 'Fab_DenimDark',
            'shoe_mat': 'Leather_DarkBrown',
            'accent_mat': 'Leather_Black',
            'overlay_type': 'european_belted_trench',
            'overlay_mat': 'Fab_TrenchCamel',
            'overlay_accent': 'Leather_Black',
            'clothing_style': 'casual',
            'materials': ['Skin_Euro_Fair', 'Eye_OceanBlue', 'Eye_Sclera', 'Hair_Blonde',
                          'Fab_TrenchCamel', 'Fab_DenimDark', 'Leather_DarkBrown', 'Leather_Black']
        },
        # 9. African American Male Urban
        {
            'name': 'NPC_African_Male_Urban',
            'description': 'Afro-descendant male with deep rich ebony skin, textured fade haircut, varsity bomber jacket, denim',
            'ethnicity': 'african',
            'gender': 'male',
            'height_scale': 1.04,
            'skin_mat': 'Skin_African_Ebony',
            'eye_mat': 'Eye_DarkBrown',
            'hair_mat': 'Hair_SilkyBlack',
            'hair_style': 'african_short_fade',
            'facial_hair_style': 'trim_goatee',
            'top_mat': 'Fab_BomberMaroon',
            'bot_mat': 'Fab_DenimDark',
            'shoe_mat': 'Sneaker_White',
            'accent_mat': 'Fab_CottonWhite',
            'overlay_type': 'crop_windbreaker',
            'overlay_mat': 'Fab_BomberMaroon',
            'overlay_accent': 'Fab_CottonWhite',
            'clothing_style': 'urban',
            'materials': ['Skin_African_Ebony', 'Eye_DarkBrown', 'Eye_Sclera', 'Hair_SilkyBlack',
                          'Fab_BomberMaroon', 'Fab_DenimDark', 'Sneaker_White', 'Fab_CottonWhite']
        },
        # 10. Latin American Female Casual
        {
            'name': 'NPC_Latin_Female_Casual',
            'description': 'Latin American female with warm golden caramel skin tone, dark wavy hair, leather jacket, boots',
            'ethnicity': 'american',
            'gender': 'female',
            'height_scale': 0.96,
            'skin_mat': 'Skin_Latin_Caramel',
            'eye_mat': 'Eye_DarkBrown',
            'hair_mat': 'Hair_DarkBrown',
            'hair_style': 'euro_wavy_hair',
            'facial_hair_style': None,
            'top_mat': 'Leather_Black',
            'bot_mat': 'Fab_DenimBlue',
            'shoe_mat': 'Leather_DarkBrown',
            'accent_mat': 'Metal_Buckle',
            'overlay_type': 'crop_windbreaker',
            'overlay_mat': 'Leather_Black',
            'overlay_accent': 'Metal_Buckle',
            'clothing_style': 'casual',
            'materials': ['Skin_Latin_Caramel', 'Eye_DarkBrown', 'Eye_Sclera', 'Hair_DarkBrown',
                          'Leather_Black', 'Fab_DenimBlue', 'Leather_DarkBrown', 'Metal_Buckle']
        },
        # 11. Nordic Male Winter Explorer
        {
            'name': 'NPC_Nordic_Male_Winter',
            'description': 'Northern European with fair skin, full ginger/blond beard, knitted Nordic sweater, winter beanie',
            'ethnicity': 'euro',
            'gender': 'male',
            'height_scale': 1.05,
            'skin_mat': 'Skin_Nordic_Pale',
            'eye_mat': 'Eye_OceanBlue',
            'hair_mat': 'Hair_Ginger',
            'hair_style': 'nordic_beanie',
            'accessory_mat': 'Fab_NavySuit',
            'facial_hair_style': 'nordic_full_beard',
            'top_mat': 'Fab_NordicSweater',
            'bot_mat': 'Fab_DenimDark',
            'shoe_mat': 'Leather_DarkBrown',
            'accent_mat': 'Fab_NordicPattern',
            'overlay_type': None,
            'clothing_style': 'winter',
            'materials': ['Skin_Nordic_Pale', 'Eye_OceanBlue', 'Eye_Sclera', 'Hair_Ginger',
                          'Fab_NordicSweater', 'Fab_DenimDark', 'Leather_DarkBrown', 'Fab_NavySuit', 'Fab_NordicPattern']
        },
        # 12. Middle Eastern Executive
        {
            'name': 'NPC_MiddleEastern_Executive',
            'description': 'Middle Eastern executive with olive-bronze complexion, trimmed beard, tailored navy suit, tie',
            'ethnicity': 'egyptian',
            'gender': 'male',
            'height_scale': 1.01,
            'skin_mat': 'Skin_Olive_Med',
            'eye_mat': 'Eye_DarkBrown',
            'hair_mat': 'Hair_SilkyBlack',
            'hair_style': 'euro_classic_parted',
            'facial_hair_style': 'trim_goatee',
            'top_mat': 'Fab_NavySuit',
            'bot_mat': 'Fab_NavySuit',
            'shoe_mat': 'Leather_Black',
            'accent_mat': 'Fab_ScarfBurgundy',
            'overlay_type': 'suit_jacket_executive',
            'overlay_mat': 'Fab_NavySuit',
            'overlay_accent': 'Fab_ScarfBurgundy',
            'clothing_style': 'business',
            'materials': ['Skin_Olive_Med', 'Eye_DarkBrown', 'Eye_Sclera', 'Hair_SilkyBlack',
                          'Fab_NavySuit', 'Leather_Black', 'Fab_ScarfBurgundy']
        }
    ]
    
    generated_characters = []
    
    for spec in archetypes:
        arm_obj, mesh_obj = build_archetype_character(spec, materials)
        generated_characters.append((arm_obj, mesh_obj))
        
        fbx_filename = f"{spec['name']}.fbx"
        fbx_path = os.path.join(NPCS_DIR, fbx_filename)
        export_individual_fbx(arm_obj, mesh_obj, fbx_path)
        
        res_fbx_path = os.path.join(RESOURCES_DIR, fbx_filename)
        shutil.copy2(fbx_path, res_fbx_path)
        
    setup_showroom_and_render(generated_characters)
    
    print(f"[NPC Generator] Saving master Blender file to {BLEND_OUTPUT_PATH}...")
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUTPUT_PATH)
    shutil.copy2(BLEND_OUTPUT_PATH, ASSETS_BLEND_PATH)
    print(f"[NPC Generator] Copied master file to {ASSETS_BLEND_PATH}")
    
    print("\n[NPC Generator] COMPLETE! Successfully generated 12 rigged Humanoid NPCs with diverse ethnicities and garments.")

if __name__ == "__main__":
    main()
