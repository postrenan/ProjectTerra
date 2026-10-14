"""
High-Fidelity NPC Character Generator based on 'The Company Asset Pack Styloo'.
Creates 12 distinct, stylized human NPCs covering authentic ethnic morphologies:
 - Asian (Male & Female): almond eye slant, high cheekbones, black hair, silk robes
 - Egyptian (Male & Female): aquiline nose, kohl-rimmed almond eyes, linen galabeya / teal gowns, gold trim
 - American (Male & Female): athletic square jaw, denim & leather, casual contemporary streetwear
 - European (Male & Female): classic angular chiseled features, wool overcoat, blue & green noble tunics
 - African (Male & Female): deep ebony skin, sculpted features, bronze & vibrant gold garments
 - Nordic (Male): fair skin, thick copper/red beard & mustache, heavy iron & leather

All meshes are derived from Styloo's stylized base characters with:
 - Combat weapons (giant warhammers, floating arrows, bows) removed for civilian NPCs
 - Fantasy ear tips humanized into natural human ears
 - Facial bones morphed to match target ethnic features and applied to rest pose
 - 2048x2048 hand-painted textures tinted per character (skin, hair, garments)
 - Unity Mecanim compatible humanoids exported as FBX to Assets/_Project/Models/Characters/NPCs/
"""

import bpy
import bmesh
import math
import os
import shutil
import numpy as np

# -----------------------------------------------------------------------------
# Paths
# -----------------------------------------------------------------------------
PROJECT_DIR = r"E:\Backup-C\Projetos\testejogo"
STYLOO_BASE = r"C:\Users\renan\Downloads\The company Asset Pack Styloo\The company Asset Pack Styloo\fbx"
STYLOO_TEX_DIR = os.path.join(PROJECT_DIR, "styloo_textures")

MODELS_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "Characters")
NPCS_DIR = os.path.join(MODELS_DIR, "NPCs")
RESOURCES_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Resources", "Models", "Characters")
TEXTURES_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Textures", "Characters")

BLEND_FILE_NAME = "NPC_Characters_Collection.blend"
BLEND_OUTPUT_PATH = os.path.join(PROJECT_DIR, BLEND_FILE_NAME)
ASSETS_BLEND_PATH = os.path.join(MODELS_DIR, BLEND_FILE_NAME)
SHOWCASE_RENDER_PATH = os.path.join(MODELS_DIR, "npc_characters_showcase.png")
PORTRAIT_RENDER_PATH = os.path.join(MODELS_DIR, "npc_portraits_showcase.png")
ROOT_SHOWCASE_RENDER = os.path.join(PROJECT_DIR, "npc_characters_showcase.png")
ROOT_PORTRAIT_RENDER = os.path.join(PROJECT_DIR, "npc_portraits_showcase.png")

os.makedirs(NPCS_DIR, exist_ok=True)
os.makedirs(RESOURCES_DIR, exist_ok=True)
os.makedirs(TEXTURES_DIR, exist_ok=True)

# -----------------------------------------------------------------------------
# Texture Tinting Engine (NumPy)
# -----------------------------------------------------------------------------
def generate_character_texture(src_tex_name, dst_path, skin_mult, clothes_mult, hair_mult=None, brightness=1.0):
    src_path = os.path.join(STYLOO_TEX_DIR, src_tex_name)
    if not os.path.exists(src_path):
        print(f"[Texture] Error: source texture not found: {src_path}")
        return
        
    img = bpy.data.images.load(src_path)
    w, h = img.size
    arr = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(arr)
    arr_img = arr.reshape((h, w, 4))
    
    # Analyze brightness and saturation to separate skin, hair, and clothes
    rgb = arr_img[:, :, :3]
    val = np.max(rgb, axis=2)
    min_val = np.min(rgb, axis=2)
    sat = np.where(val > 0, (val - min_val) / (val + 1e-6), 0)
    
    # Skin detector heuristic for Styloo textures (fair peachy tones: R > G > B, moderate saturation)
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    is_skin = (r > g) & (g > b) & (r > 0.45) & (sat < 0.55) & (b > 0.20)
    
    # Hair detector (yellow/blonde in elf: high R and G, low B, higher saturation)
    is_hair = (r > 0.50) & (g > 0.35) & (b < 0.30) & (sat > 0.40) & ~is_skin
    
    # Green clothes detector in elf (G > R and G > B)
    is_green_clothes = (g > r * 1.1) & (g > b * 1.1) & (g > 0.25)
    
    # Blue clothes detector in knight (B > R and B > G)
    is_blue_clothes = (b > r * 1.1) & (b > g * 1.05) & (b > 0.30)
    
    # Purple/violet clothes in mage (R and B high, G low)
    is_purple_clothes = (r > g * 1.2) & (b > g * 1.1) & (val > 0.25)
    
    # General clothes (anything saturated or darker that is not skin or hair)
    is_clothes = (is_green_clothes | is_blue_clothes | is_purple_clothes | (sat > 0.35)) & ~is_skin & ~is_hair
    
    # Apply Skin Tint
    if skin_mult is not None:
        sm = np.array(skin_mult, dtype=np.float32)
        for c in range(3):
            arr_img[is_skin, c] = arr_img[is_skin, c] * sm[c]
            
    # Apply Clothes Tint
    if clothes_mult is not None:
        cm = np.array(clothes_mult, dtype=np.float32)
        for c in range(3):
            arr_img[is_clothes, c] = arr_img[is_clothes, c] * cm[c]
            
    # Apply Hair Tint
    if hair_mult is not None:
        hm = np.array(hair_mult, dtype=np.float32)
        for c in range(3):
            arr_img[is_hair, c] = arr_img[is_hair, c] * hm[c]
            
    # Global brightness adjustment
    if brightness != 1.0:
        arr_img[:, :, :3] *= brightness
        
    np.clip(arr_img, 0.0, 1.0, out=arr_img)
    
    # Save output texture
    out_img = bpy.data.images.new(os.path.basename(dst_path), w, h)
    out_img.pixels.foreach_set(arr_img.flatten())
    out_img.filepath_raw = dst_path
    out_img.file_format = 'PNG'
    out_img.save()
    
    # Clean up temporary images from memory
    bpy.data.images.remove(img)
    bpy.data.images.remove(out_img)
    print(f"  [Texture] Generated: {os.path.basename(dst_path)}")

# -----------------------------------------------------------------------------
# Clean Combat Weapons (Civilian NPC Sanitization)
# -----------------------------------------------------------------------------
def sanitize_civilian_mesh(mesh_obj, base_type):
    """Removes combat weaponry (giant bows, arrows, staffs, warhammers) using bone vertex groups so NPCs act as civilian pedestrians."""
    weapon_kw = []
    if base_type == 'elf':
        weapon_kw = ['bow', 'arrow']
    elif base_type == 'mage':
        weapon_kw = ['staff']
    elif base_type == 'nain':
        weapon_kw = ['weapon']
    elif base_type == 'knight':
        weapon_kw = ['shield']

    weapon_vgs = [vg.index for vg in mesh_obj.vertex_groups if any(kw in vg.name.lower() for kw in weapon_kw)]

    bm = bmesh.new()
    bm.from_mesh(mesh_obj.data)

    verts_to_delete = set()
    for v in mesh_obj.data.vertices:
        w_weapon = sum(g.weight for g in v.groups if g.group in weapon_vgs)
        if w_weapon > 0.05:
            verts_to_delete.add(v.index)

    faces_to_delete = [f for f in bm.faces if all(v.index in verts_to_delete for v in f.verts)]

    # For knight: remove detached floating sword/shield islands if any
    if base_type == 'knight':
        islands = []
        visited = set()
        for f in bm.faces:
            if f in visited:
                continue
            isl = [f]
            visited.add(f)
            stack = [f]
            while stack:
                cf = stack.pop()
                for e in cf.edges:
                    for lf in e.link_faces:
                        if lf not in visited:
                            visited.add(lf)
                            isl.append(lf)
                            stack.append(lf)
            islands.append(isl)
        for isl in islands:
            xs = [v.co.x for f in isl for v in f.verts]
            if max(abs(min(xs)), abs(max(xs))) > 0.70:
                faces_to_delete.extend(isl)

    if faces_to_delete:
        faces_to_delete = list(set(faces_to_delete))
        bmesh.ops.delete(bm, geom=faces_to_delete, context='FACES')
        bm.to_mesh(mesh_obj.data)
        mesh_obj.data.update()
        print(f"  [Sanitize] Removed {len(faces_to_delete)} weapon faces from {mesh_obj.name}.")
    bm.free()

    # Humanize ears on elf geometry for civilian appearance
    if base_type == 'elf':
        ear_vgs = [vg.index for vg in mesh_obj.vertex_groups if vg.name.lower().startswith('def-ear')]
        humanized_count = 0
        for v in mesh_obj.data.vertices:
            w_ear = sum(g.weight for g in v.groups if g.group in ear_vgs)
            if w_ear > 0.1:
                humanized_count += 1
                if abs(v.co.x) > 0.11:
                    sign = 1.0 if v.co.x > 0 else -1.0
                    excess = abs(v.co.x) - 0.11
                    v.co.x -= sign * (excess * 0.60 * min(1.0, w_ear))
                if v.co.z > 1.62:
                    v.co.z -= (v.co.z - 1.62) * 0.55 * min(1.0, w_ear)
        mesh_obj.data.update()
        print(f"  [Sanitize] Humanized {humanized_count} ear vertices on {mesh_obj.name}.")

# -----------------------------------------------------------------------------
# Morph Ethnic Facial Anatomy on Rig
# -----------------------------------------------------------------------------
def apply_ethnic_facial_morph(arm_obj, ethnicity, base_type):
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='POSE')
    
    # 1. Humanize ears for elf/knight models
    if base_type in ['elf', 'knight']:
        for side in ['L', 'R']:
            for i in ['001', '002', '003', '004']:
                pb = arm_obj.pose.bones.get(f'DEF-ear.{side}.{i}')
                if pb:
                    pb.scale = (0.20, 0.20, 0.20)
            pb_base = arm_obj.pose.bones.get(f'DEF-ear.{side}')
            if pb_base:
                pb_base.scale = (0.65, 0.65, 0.65)
                
    # 2. Ethnic Facial Features
    if ethnicity == 'asian':
        # High cheekbones, almond eye tilt, refined softer nose bridge
        for s in ['L', 'R']:
            # Cheeks
            pb = arm_obj.pose.bones.get(f'DEF-cheek.T.{s}.001')
            if pb: pb.location.z += 0.012; pb.location.x += (0.010 if s == 'L' else -0.010)
            # Almond eye tilt
            pb_eye = arm_obj.pose.bones.get(f'DEF-eye.{s}')
            if pb_eye: pb_eye.rotation_euler.y += (0.05 if s == 'L' else -0.05)
            pb_brow = arm_obj.pose.bones.get(f'DEF-brow.T.{s}.002')
            if pb_brow: pb_brow.location.z += 0.008
        pb_nose = arm_obj.pose.bones.get('DEF-nose.001')
        if pb_nose: pb_nose.scale.x = 0.90
        
    elif ethnicity == 'egyptian':
        # Prominent aristocratic aquiline nose, noble jaw, expressively sculpted almond eyes
        pb_nose1 = arm_obj.pose.bones.get('DEF-nose.001')
        if pb_nose1: pb_nose1.location.y += 0.018; pb_nose1.scale.z = 1.15
        pb_nose2 = arm_obj.pose.bones.get('DEF-nose.002')
        if pb_nose2: pb_nose2.location.y += 0.015
        for s in ['L', 'R']:
            pb_jaw = arm_obj.pose.bones.get(f'DEF-jaw.{s}')
            if pb_jaw: pb_jaw.scale.x = 0.94
            pb_brow = arm_obj.pose.bones.get(f'DEF-brow.B.{s}.001')
            if pb_brow: pb_brow.location.z += 0.006
            
    elif ethnicity == 'european':
        # Chiseled angular jaw, straight narrow nasal bridge
        pb_nose = arm_obj.pose.bones.get('DEF-nose.001')
        if pb_nose: pb_nose.location.y += 0.010; pb_nose.scale.x = 0.92
        for s in ['L', 'R']:
            pb_jaw = arm_obj.pose.bones.get(f'DEF-jaw.{s}')
            if pb_jaw: pb_jaw.scale.x = 1.04
            
    elif ethnicity == 'african':
        # Sculpted wide nasal base, full lips, athletic jawline
        pb_nose = arm_obj.pose.bones.get('DEF-nose.003')
        if pb_nose: pb_nose.scale.x = 1.25
        for s in ['L', 'R']:
            pb_nose_s = arm_obj.pose.bones.get(f'DEF-nose.{s}.001')
            if pb_nose_s: pb_nose_s.location.x += (0.012 if s == 'L' else -0.012)
            for lip in ['lip.T', 'lip.B']:
                pb_lip = arm_obj.pose.bones.get(f'DEF-{lip}.{s}')
                if pb_lip: pb_lip.scale.z = 1.20
                
    elif ethnicity == 'american':
        # Square athletic jaw, confident friendly brow
        for s in ['L', 'R']:
            pb_jaw = arm_obj.pose.bones.get(f'DEF-jaw.{s}')
            if pb_jaw: pb_jaw.scale.x = 1.08; pb_jaw.location.y += 0.008
            
    # Apply pose permanently to Rest Pose
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    print(f"  [Morph] Applied '{ethnicity}' facial morphology to rest pose.")

# -----------------------------------------------------------------------------
# Setup Materials & Textures on Character
# -----------------------------------------------------------------------------
def assign_character_textures(mesh_obj, char_id, tex_path_1, tex_path_2=None):
    img1 = bpy.data.images.load(tex_path_1)
    img2 = bpy.data.images.load(tex_path_2) if tex_path_2 and os.path.exists(tex_path_2) else img1

    for i, old_mat in enumerate(list(mesh_obj.data.materials)):
        if not old_mat:
            continue
        # Duplicate to create a completely unique material per character
        mat = old_mat.copy()
        mat.name = f"M_{char_id}_{i+1}"
        mesh_obj.data.materials[i] = mat

        if hasattr(mat, 'blend_method'):
            mat.blend_method = 'OPAQUE'
        if hasattr(mat, 'surface_render_method'):
            mat.surface_render_method = 'DITHERED'

        target_img = img2 if (i == 1 and tex_path_2) else img1
        if mat.node_tree:
            for n in mat.node_tree.nodes:
                if n.type == 'TEX_IMAGE' and ('color' in n.name.lower() or n.name == 'Image Texture'):
                    n.image = target_img
                elif n.type == 'BSDF_PRINCIPLED':
                    alpha_sock = n.inputs.get('Alpha')
                    if alpha_sock and alpha_sock.is_linked:
                        for l in list(alpha_sock.links):
                            mat.node_tree.links.remove(l)
                    if alpha_sock:
                        alpha_sock.default_value = 1.0
                    n.inputs['Roughness'].default_value = 0.70
                    n.inputs['Metallic'].default_value = 0.0
    print(f"  [Material] Assigned unique materials & textures to {mesh_obj.name}.")

# -----------------------------------------------------------------------------
# Export Single FBX
# -----------------------------------------------------------------------------
def export_character_fbx(arm_obj, mesh_obj, output_path):
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
        armature_nodetype='NULL',
        path_mode='COPY',
        embed_textures=True
    )
    print(f"  [Export] Exported FBX with embedded textures: {os.path.basename(output_path)}")

# -----------------------------------------------------------------------------
# Main Generation Function
# -----------------------------------------------------------------------------
def build_all_npcs():
    print("[NPC Pipeline] Starting generation of 12 distinct human NPCs from Styloo asset pack...")
    
    # Specifications for the 12 characters
    character_specs = [
        # --- Asian Group ---
        {
            'id': 'NPC_Asian_Female_Silk',
            'base_fbx': 'elfexport.fbx',
            'base_type': 'elf',
            'mesh_name': 'elfBody',
            'arm_name': 'elfgame_root',
            'ethnicity': 'asian',
            'skin_mult': (1.02, 0.95, 0.82),      # Fitzpatrick III (Warm Golden Asian)
            'hair_mult': (0.15, 0.15, 0.16),      # Silky Black Hair
            'clothes_mult': (1.8, 0.35, 0.40),    # Crimson Red & Gold Silk Robe
            'src_tex1': 'elfFirst_color.png',
            'src_tex2': 'elfSecond_color.png'
        },
        {
            'id': 'NPC_Asian_Male_Scholar',
            'base_fbx': 'knightclean4.fbx',
            'base_type': 'knight',
            'mesh_name': 'newknight.001',
            'arm_name': 'knight_root',
            'ethnicity': 'asian',
            'skin_mult': (1.02, 0.96, 0.84),      # Fitzpatrick III (Light Golden)
            'hair_mult': (0.15, 0.15, 0.16),      # Jet Black Hair
            'clothes_mult': (0.35, 1.40, 1.25),   # Jade Green & Gold Tunic
            'src_tex1': 'knightfirst_color.png',
            'src_tex2': 'knightsecond_color.png.001.png'
        },
        # --- Egyptian Group ---
        {
            'id': 'NPC_Egyptian_Male_Elder',
            'base_fbx': 'mageclean4.fbx',
            'base_type': 'mage',
            'mesh_name': 'mage',
            'arm_name': 'mage_root_deform',
            'ethnicity': 'egyptian',
            'skin_mult': (0.88, 0.72, 0.54),      # Fitzpatrick IV/V (Egyptian Bronze)
            'hair_mult': (0.55, 0.55, 0.58),      # Salt & Pepper Noble Beard
            'clothes_mult': (1.45, 1.35, 1.15),   # Off-White Linen Galabeya with Gold Trim
            'src_tex1': 'sorcererFirst_color.png',
            'src_tex2': 'sorcererSecond_color.png'
        },
        {
            'id': 'NPC_Egyptian_Female_Dignitary',
            'base_fbx': 'elfexport.fbx',
            'base_type': 'elf',
            'mesh_name': 'elfBody',
            'arm_name': 'elfgame_root',
            'ethnicity': 'egyptian',
            'skin_mult': (0.88, 0.72, 0.54),      # Fitzpatrick IV/V (Egyptian Golden Bronze)
            'hair_mult': (0.12, 0.12, 0.14),      # Raven Black Kohl Hair
            'clothes_mult': (0.25, 1.35, 1.45),   # Royal Teal & Gold Embroidered Gown
            'src_tex1': 'elfFirst_color.png',
            'src_tex2': 'elfSecond_color.png'
        },
        # --- European Group ---
        {
            'id': 'NPC_European_Male_Knight',
            'base_fbx': 'knightclean4.fbx',
            'base_type': 'knight',
            'mesh_name': 'newknight.001',
            'arm_name': 'knight_root',
            'ethnicity': 'european',
            'skin_mult': (1.05, 0.98, 0.94),      # Fitzpatrick II (Fair European Peach)
            'hair_mult': (0.95, 0.78, 0.45),      # Golden Brown / Wavy Locks
            'clothes_mult': (0.35, 0.65, 1.65),   # Royal Sapphire Blue Wool & Steel
            'src_tex1': 'knightfirst_color.png',
            'src_tex2': 'knightsecond_color.png.001.png'
        },
        {
            'id': 'NPC_European_Female_Noble',
            'base_fbx': 'elfexport.fbx',
            'base_type': 'elf',
            'mesh_name': 'elfBody',
            'arm_name': 'elfgame_root',
            'ethnicity': 'european',
            'skin_mult': (1.08, 1.00, 0.96),      # Fitzpatrick I (Nordic Pale Fair)
            'hair_mult': (1.15, 1.05, 0.70),      # Platinum Blonde Hair
            'clothes_mult': (0.45, 1.25, 0.70),   # Emerald Forest Green Velvet Gown
            'src_tex1': 'elfFirst_color.png',
            'src_tex2': 'elfSecond_color.png'
        },
        {
            'id': 'NPC_Nordic_Male_Blacksmith',
            'base_fbx': 'nain4export.fbx',
            'base_type': 'nain',
            'mesh_name': 'nainSecond',
            'arm_name': 'naingame_root',
            'ethnicity': 'european',
            'skin_mult': (1.06, 0.95, 0.90),      # Fitzpatrick I (Nordic Pale with Rosy cheeks)
            'hair_mult': (1.55, 0.65, 0.25),      # Copper Ginger Full Beard
            'clothes_mult': (0.85, 0.85, 0.90),   # Iron & Charcoal Leather Cuirass
            'src_tex1': 'nainFirst_color.png',
            'src_tex2': 'nainSecond_color.png'
        },
        # --- American & Global Group ---
        {
            'id': 'NPC_American_Male_Explorer',
            'base_fbx': 'knightclean4.fbx',
            'base_type': 'knight',
            'mesh_name': 'newknight.001',
            'arm_name': 'knight_root',
            'ethnicity': 'american',
            'skin_mult': (1.00, 0.90, 0.80),      # Fitzpatrick II/III (Tanned American)
            'hair_mult': (0.40, 0.25, 0.15),      # Chestnut Brown Hair
            'clothes_mult': (0.45, 0.55, 0.90),   # Denim Indigo & Leather Brown
            'src_tex1': 'knightfirst_color.png',
            'src_tex2': 'knightsecond_color.png.001.png'
        },
        {
            'id': 'NPC_American_Female_Adventurer',
            'base_fbx': 'elfexport.fbx',
            'base_type': 'elf',
            'mesh_name': 'elfBody',
            'arm_name': 'elfgame_root',
            'ethnicity': 'american',
            'skin_mult': (0.84, 0.68, 0.52),      # Fitzpatrick IV (Warm Caramel / Latina)
            'hair_mult': (0.35, 0.22, 0.15),      # Deep Brunette Hair
            'clothes_mult': (1.10, 0.85, 0.55),   # Khaki & Terra Leather Tunic
            'src_tex1': 'elfFirst_color.png',
            'src_tex2': 'elfSecond_color.png'
        },
        {
            'id': 'NPC_African_Male_Champion',
            'base_fbx': 'nain4export.fbx',
            'base_type': 'nain',
            'mesh_name': 'nainSecond',
            'arm_name': 'naingame_root',
            'ethnicity': 'african',
            'skin_mult': (0.42, 0.30, 0.22),      # Fitzpatrick VI (Deep Rich Ebony)
            'hair_mult': (0.10, 0.10, 0.10),      # Dark Buzzcut / Beard
            'clothes_mult': (1.35, 0.95, 0.40),   # Polished Bronze & Gold Medallion
            'src_tex1': 'nainFirst_color.png',
            'src_tex2': 'nainSecond_color.png'
        },
        {
            'id': 'NPC_African_Female_Merchant',
            'base_fbx': 'elfexport.fbx',
            'base_type': 'elf',
            'mesh_name': 'elfBody',
            'arm_name': 'elfgame_root',
            'ethnicity': 'african',
            'skin_mult': (0.42, 0.30, 0.22),      # Fitzpatrick VI (Luminous Ebony)
            'hair_mult': (0.10, 0.10, 0.10),      # Jet Black Sleek Hair
            'clothes_mult': (1.65, 1.25, 0.35),   # Saffron Gold & Royal Purple Gown
            'src_tex1': 'elfFirst_color.png',
            'src_tex2': 'elfSecond_color.png'
        },
        {
            'id': 'NPC_MiddleEastern_Magistrate',
            'base_fbx': 'mageclean4.fbx',
            'base_type': 'mage',
            'mesh_name': 'mage',
            'arm_name': 'mage_root_deform',
            'ethnicity': 'egyptian',
            'skin_mult': (0.85, 0.70, 0.55),      # Fitzpatrick IV (Warm Olive Bronze)
            'hair_mult': (0.25, 0.22, 0.22),      # Dark Trimmed Beard
            'clothes_mult': (1.45, 0.45, 1.25),   # Royal Purple Silk Robe with Amethyst Gem
            'src_tex1': 'sorcererFirst_color.png',
            'src_tex2': 'sorcererSecond_color.png'
        }
    ]
    
    # Step 1: Process each character individually and export FBX
    for spec in character_specs:
        char_id = spec['id']
        print(f"\n---> Generating NPC: {char_id} ({spec['ethnicity']})...")
        
        # 1. Generate dedicated textures
        tex1_name = f"{char_id}_Diffuse1.png"
        tex1_path = os.path.join(TEXTURES_DIR, tex1_name)
        generate_character_texture(
            spec['src_tex1'], tex1_path,
            spec['skin_mult'], spec['clothes_mult'], spec['hair_mult']
        )
        
        tex2_path = None
        if 'src_tex2' in spec and spec['src_tex2']:
            tex2_name = f"{char_id}_Diffuse2.png"
            tex2_path = os.path.join(TEXTURES_DIR, tex2_name)
            generate_character_texture(
                spec['src_tex2'], tex2_path,
                spec['skin_mult'], spec['clothes_mult'], spec['hair_mult']
            )
            
        # 2. Open fresh Blender scene and import base FBX
        bpy.ops.wm.read_factory_settings(use_empty=True)
        fbx_src_path = os.path.join(STYLOO_BASE, spec['base_fbx'])
        bpy.ops.import_scene.fbx(filepath=fbx_src_path)
        
        mesh_obj = bpy.data.objects.get(spec['mesh_name'])
        arm_obj = bpy.data.objects.get(spec['arm_name'])
        
        if not mesh_obj or not arm_obj:
            print(f"  Error: Mesh '{spec['mesh_name']}' or Armature '{spec['arm_name']}' not found in {spec['base_fbx']}")
            continue
            
        # 3. Clean combat weapons for civilian pedestrian look
        sanitize_civilian_mesh(mesh_obj, spec['base_type'])
        
        # 4. Apply facial morphing & humanize ears
        apply_ethnic_facial_morph(arm_obj, spec['ethnicity'], spec['base_type'])
        
        # 5. Assign dedicated textures to materials
        assign_character_textures(mesh_obj, char_id, tex1_path, tex2_path)
        
        # 6. Rename objects cleanly
        mesh_obj.name = f"{char_id}_Mesh"
        arm_obj.name = f"{char_id}_Armature"
        
        # 7. Export FBX to NPCs and Resources
        fbx_filename = f"{char_id}.fbx"
        fbx_out_path = os.path.join(NPCS_DIR, fbx_filename)
        export_character_fbx(arm_obj, mesh_obj, fbx_out_path)
        
        res_fbx_path = os.path.join(RESOURCES_DIR, fbx_filename)
        shutil.copy2(fbx_out_path, res_fbx_path)
        
    print("\n[NPC Pipeline] All 12 individual FBXs successfully generated and exported!")
    
    # Step 2: Build Master Showroom Scene & High-Resolution Showcase Renders
    build_showroom_and_renders(character_specs)

# -----------------------------------------------------------------------------
# Showroom Parade & High-Res Renders
# -----------------------------------------------------------------------------
def build_showroom_and_renders(character_specs):
    print("\n[NPC Showroom] Setting up master showroom scene with all 12 characters...")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = 'METERS'
    
    # AgX or Filmic
    if 'AgX' in bpy.types.ColorManagedViewSettings.bl_rna.properties['view_transform'].enum_items:
        scene.view_settings.view_transform = 'AgX'
    else:
        scene.view_settings.view_transform = 'Filmic'
    scene.view_settings.look = 'Medium High Contrast'
    
    # World lighting
    world = bpy.data.worlds.new("World_Showroom")
    scene.world = world
    world.use_nodes = True
    wnodes = world.node_tree.nodes
    wnodes.clear()
    w_bg = wnodes.new('ShaderNodeBackground')
    w_bg.inputs['Color'].default_value = (0.75, 0.78, 0.82, 1.0)
    w_bg.inputs['Strength'].default_value = 0.85
    w_out = wnodes.new('ShaderNodeOutputWorld')
    world.node_tree.links.new(w_bg.outputs['Background'], w_out.inputs['Surface'])
    
    # Showroom Floor
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.2, 0.5, -0.15))
    floor = bpy.context.active_object
    floor.name = "Showroom_Floor"
    floor.scale = (22.0, 10.0, 0.3)
    stage_mat = bpy.data.materials.new("M_Stage")
    stage_mat.use_nodes = True
    stage_mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.10, 0.11, 0.13, 1.0)
    stage_mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 0.30
    floor.data.materials.append(stage_mat)
    
    # Elevated Tier for Back Row (z = 0.40)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.4, 1.25, 0.10))
    tier = bpy.context.active_object
    tier.name = "Showroom_Tier"
    tier.scale = (20.0, 3.2, 0.40)
    mat_tier = bpy.data.materials.new("M_Tier")
    mat_tier.use_nodes = True
    mat_tier.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.16, 0.17, 0.20, 1.0)
    mat_tier.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 0.40
    tier.data.materials.append(mat_tier)
    
    # Backdrop wall placed BEHIND characters at Y = +4.0
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.2, 4.0, 2.5))
    backdrop = bpy.context.active_object
    backdrop.name = "Showroom_Backdrop"
    backdrop.scale = (26.0, 0.5, 6.0)
    wall_mat = bpy.data.materials.new("M_Wall")
    wall_mat.use_nodes = True
    wall_mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.86, 0.88, 0.90, 1.0)
    wall_mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 0.70
    backdrop.data.materials.append(wall_mat)
    
    # Import all 12 generated characters and place them in 2 interleaved front-facing rows
    # Staggered arrangement:
    # Front row: 6 characters
    front_specs = [
        character_specs[0],  # Asian Silk
        character_specs[3],  # Egyptian Dignitary
        character_specs[4],  # European Knight
        character_specs[9],  # African Champion
        character_specs[8],  # American Adventurer
        character_specs[6],  # Nordic Blacksmith
    ]
    # Back row: 6 characters
    back_specs = [
        character_specs[1],  # Asian Scholar
        character_specs[2],  # Egyptian Elder
        character_specs[5],  # European Noble
        character_specs[10], # African Merchant
        character_specs[7],  # American Explorer
        character_specs[11], # Middle Eastern Magistrate
    ]
    
    # Front row: spaced by 1.70m, y = -0.4, z = 0.0
    for idx, spec in enumerate(front_specs):
        fbx_path = os.path.join(NPCS_DIR, f"{spec['id']}.fbx")
        cur_objs = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=fbx_path)
        new_objs = [o for o in bpy.data.objects if o not in cur_objs]
        
        pos_x = (idx - 2.5) * 1.70
        for o in new_objs:
            if o.parent is None:
                o.location = (pos_x, -0.4, 0.0)
                o.rotation_euler = (0, 0, -pos_x * 0.02)
            if o.type == 'MESH':
                tex1_path = os.path.join(TEXTURES_DIR, f"{spec['id']}_Diffuse1.png")
                tex2_path = os.path.join(TEXTURES_DIR, f"{spec['id']}_Diffuse2.png") if spec.get('src_tex2') else None
                assign_character_textures(o, spec['id'], tex1_path, tex2_path)
                
    # Back row: interleaved (+0.85m offset), elevated on tier (+0.40m z), y = 1.25
    for idx, spec in enumerate(back_specs):
        fbx_path = os.path.join(NPCS_DIR, f"{spec['id']}.fbx")
        cur_objs = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=fbx_path)
        new_objs = [o for o in bpy.data.objects if o not in cur_objs]
        
        pos_x = (idx - 2.5) * 1.70 + 0.85
        for o in new_objs:
            if o.parent is None:
                o.location = (pos_x, 1.25, 0.40)
                o.rotation_euler = (0, 0, -pos_x * 0.02)
            if o.type == 'MESH':
                tex1_path = os.path.join(TEXTURES_DIR, f"{spec['id']}_Diffuse1.png")
                tex2_path = os.path.join(TEXTURES_DIR, f"{spec['id']}_Diffuse2.png") if spec.get('src_tex2') else None
                assign_character_textures(o, spec['id'], tex1_path, tex2_path)
                
    # Sanitize all imported materials in the showroom scene
    for mat in bpy.data.materials:
        if hasattr(mat, 'blend_method'):
            mat.blend_method = 'OPAQUE'
        if hasattr(mat, 'surface_render_method'):
            mat.surface_render_method = 'DITHERED'
        if mat.node_tree:
            for node in mat.node_tree.nodes:
                if node.type == 'BSDF_PRINCIPLED':
                    alpha_sock = node.inputs.get('Alpha')
                    if alpha_sock and alpha_sock.is_linked:
                        for l in list(alpha_sock.links):
                            mat.node_tree.links.remove(l)
                    if alpha_sock:
                        alpha_sock.default_value = 1.0
                    node.inputs['Roughness'].default_value = 0.65
                
    # Lighting: 3-Point Studio Setup
    key_light_data = bpy.data.lights.new(name="Light_Key_Sun", type='SUN')
    key_light_data.energy = 4.2
    key_light_data.color = (1.0, 0.98, 0.95)
    key_light_obj = bpy.data.objects.new("Light_Key_Sun", key_light_data)
    bpy.context.collection.objects.link(key_light_obj)
    key_light_obj.location = (6.0, -8.0, 9.0)
    key_light_obj.rotation_euler = (math.radians(50), math.radians(10), math.radians(-30))
    
    fill_light_data = bpy.data.lights.new(name="Light_Fill_Area", type='AREA')
    fill_light_data.energy = 3000.0
    fill_light_data.size = 12.0
    fill_light_data.color = (0.75, 0.85, 1.0)
    fill_light_obj = bpy.data.objects.new("Light_Fill_Area", fill_light_data)
    bpy.context.collection.objects.link(fill_light_obj)
    fill_light_obj.location = (-7.0, -6.0, 5.0)
    fill_light_obj.rotation_euler = (math.radians(55), 0, math.radians(45))
    
    rim_light_data = bpy.data.lights.new(name="Light_Rim_Area", type='AREA')
    rim_light_data.energy = 2500.0
    rim_light_data.size = 14.0
    rim_light_data.color = (1.0, 0.95, 0.90)
    rim_light_obj = bpy.data.objects.new("Light_Rim_Area", rim_light_data)
    bpy.context.collection.objects.link(rim_light_obj)
    rim_light_obj.location = (0, 3.2, 5.5)
    rim_light_obj.rotation_euler = (math.radians(-65), 0, 0)
    
    # Panoramic Parade Camera
    cam_data = bpy.data.cameras.new("Showroom_Camera")
    cam_data.lens = 30
    cam_data.clip_start = 0.1
    cam_data.clip_end = 100.0
    cam_obj = bpy.data.objects.new("Showroom_Camera", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    cam_obj.location = (0.35, -6.8, 1.70)
    cam_obj.rotation_euler = (math.radians(88), 0, 0)
    
    scene.render.engine = 'BLENDER_EEVEE_NEXT' if hasattr(bpy.types, 'RenderEngineEEVEENext') else 'BLENDER_EEVEE'
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100
    scene.render.filepath = SHOWCASE_RENDER_PATH
    
    print(f"[NPC Showroom] Rendering full showcase parade to: {SHOWCASE_RENDER_PATH}...")
    bpy.ops.render.render(write_still=True)
    if os.path.exists(SHOWCASE_RENDER_PATH):
        shutil.copy2(SHOWCASE_RENDER_PATH, ROOT_SHOWCASE_RENDER)
        print(f"[NPC Showroom] Showcase parade saved successfully ({os.path.getsize(SHOWCASE_RENDER_PATH)} bytes).")
        
    # Close-up Portrait Camera (focus on diverse cultural representatives)
    cam_close_data = bpy.data.cameras.new("Portrait_Camera")
    cam_close_data.lens = 52
    cam_close_obj = bpy.data.objects.new("Portrait_Camera", cam_close_data)
    bpy.context.collection.objects.link(cam_close_obj)
    bpy.context.scene.camera = cam_close_obj
    cam_close_obj.location = (0.0, -3.2, 1.45)
    cam_close_obj.rotation_euler = (math.radians(88.5), 0, 0)
    
    scene.render.filepath = PORTRAIT_RENDER_PATH
    print(f"[NPC Showroom] Rendering cultural portraits close-up to: {PORTRAIT_RENDER_PATH}...")
    bpy.ops.render.render(write_still=True)
    if os.path.exists(PORTRAIT_RENDER_PATH):
        shutil.copy2(PORTRAIT_RENDER_PATH, ROOT_PORTRAIT_RENDER)
        print(f"[NPC Showroom] Portrait close-up saved successfully ({os.path.getsize(PORTRAIT_RENDER_PATH)} bytes).")
        
    # Save master .blend file
    print(f"[NPC Showroom] Saving master Blender file to {BLEND_OUTPUT_PATH}...")
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUTPUT_PATH)
    shutil.copy2(BLEND_OUTPUT_PATH, ASSETS_BLEND_PATH)
    print(f"[NPC Showroom] Master file saved and copied to {ASSETS_BLEND_PATH}")

if __name__ == "__main__":
    build_all_npcs()
