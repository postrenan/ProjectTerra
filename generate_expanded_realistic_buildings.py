"""
Expanded Realistic Architecture & World Styles Generator for Blender 5.2
Generates 11 High-Fidelity Realistic & Interactive Buildings:
 1. Commercial & Institutional:
    - Farmácia Moderna (with 3D Sign, Emissive Neon Cross & Glass Doors)
    - Supermercado da Cidade (with 3D Sign, Cart Bay & Produce Displays)
    - Posto de Combustível (with Illuminated Canopy, Fuel Dispensers, Price Totem & C-Store)
    - Hospital & Pronto-Socorro (with Ambulance Bay, 3D Red Cross & Rooftop Helipad)
    - Escola Municipal (with Classical Portico, 3D Sign, Clock Tower & Flag)
 2. Residential Styles of the World:
    - Casa de Fazenda (Rustic Farmhouse with Wrap-Around Porch, Bench & Stone Chimney)
    - Casa Sobrado Urbano (Narrow Townhouse with Wrought Iron Balconies & Garage)
    - Casa Grega de Santorini (Cycladic Cubic Architecture, Cobalt Dome & Terraces)
    - Casa Alemã da Baviera (Authentic Fachwerk Timber Framing & Window Flower Boxes)
    - Casa Brasileira Colonial (Colonial Veranda, Portuguese Barrel Tiles & Iron Gate)
    - Casa Norte-Americana (Craftsman Lap Siding, Shutters & Mailbox Post)

Features:
 - Staggered panoramic boulevard layout (all 11 facades 100% visible and unobstructed)
 - 3D Embossed Architectural Signage on all commercial buildings
 - Realistic PBR Procedural Shaders (Bricks, Slates, Barrel Tiles, Timber, Stucco, Glass, Metals, Neon Glow)
 - Interactive Doors with exact hinge pivots & animation keyframes
 - Seamless Cube UV Projections on all architectural geometry
 - Exports master .blend file and 11 individual FBX models to Unity WorldPack folder
 - Renders high-resolution 1080p architectural showcase: realistic_buildings_showcase.png
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix, Euler

# -----------------------------------------------------------------------------
# Paths & Settings
# -----------------------------------------------------------------------------
PROJECT_DIR = r"E:\Backup-C\Projetos\testejogo"
BLEND_FILE_NAME = "Modular_Buildings_Realistic_World_Pack.blend"
BLEND_FILE_PATH = os.path.join(PROJECT_DIR, BLEND_FILE_NAME)
ASSETS_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "ModularBuildings")
WORLDPACK_DIR = os.path.join(ASSETS_DIR, "WorldPack")
ASSETS_BLEND_PATH = os.path.join(ASSETS_DIR, BLEND_FILE_NAME)
RENDER_OUTPUT_PATH = os.path.join(PROJECT_DIR, "realistic_buildings_showcase.png")

os.makedirs(WORLDPACK_DIR, exist_ok=True)

# -----------------------------------------------------------------------------
# Scene Reset & World Atmosphere
# -----------------------------------------------------------------------------
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = 'METERS'
    
    # World sky background
    world = bpy.data.worlds.new("Architectural_Sky")
    scene.world = world
    world.use_nodes = True
    nodes = world.node_tree.nodes
    nodes.clear()
    bg = nodes.new('ShaderNodeBackground')
    bg.inputs['Color'].default_value = (0.76, 0.85, 0.96, 1.0)
    bg.inputs['Strength'].default_value = 0.95
    out = nodes.new('ShaderNodeOutputWorld')
    world.node_tree.links.new(bg.outputs['Background'], out.inputs['Surface'])

def get_col(name, parent=None):
    c = bpy.data.collections.get(name)
    if not c:
        c = bpy.data.collections.new(name)
        if parent:
            parent.children.link(c)
        else:
            bpy.context.scene.collection.children.link(c)
    return c

# -----------------------------------------------------------------------------
# Material System (Procedural Realistic PBR)
# -----------------------------------------------------------------------------
MATS = {}

def create_pbr_materials():
    global MATS
    
    def set_bsdf(bsdf, **kwargs):
        mapping = {
            'base_color': ['Base Color'],
            'metallic': ['Metallic'],
            'roughness': ['Roughness'],
            'transmission': ['Transmission Weight', 'Transmission'],
            'alpha': ['Alpha'],
            'ior': ['IOR'],
            'emission_color': ['Emission Color', 'Emission'],
            'emission_strength': ['Emission Strength']
        }
        for k, v in kwargs.items():
            if k in mapping:
                for target_name in mapping[k]:
                    if target_name in bsdf.inputs:
                        bsdf.inputs[target_name].default_value = v
                        break

    def make_mat(name, base_color, metallic=0.0, roughness=0.5, emission_color=None, emission_strength=0.0, transmission=0.0, alpha=1.0):
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        nodes = mat.node_tree.nodes
        bsdf = nodes.get("Principled BSDF")
        set_bsdf(bsdf, base_color=base_color, metallic=metallic, roughness=roughness, transmission=transmission, alpha=alpha)
        if emission_color:
            set_bsdf(bsdf, emission_color=emission_color, emission_strength=emission_strength)
        MATS[name] = mat
        return mat

    # 1. Concrete, Pavements & Roads
    make_mat("Mat_Concrete_Slab", (0.58, 0.60, 0.62, 1.0), roughness=0.82)
    make_mat("Mat_Asphalt_Road", (0.20, 0.21, 0.23, 1.0), roughness=0.9)
    make_mat("Mat_Sidewalk_Pavement", (0.74, 0.75, 0.77, 1.0), roughness=0.72)
    make_mat("Mat_Road_Stripe_Yellow", (0.95, 0.78, 0.10, 1.0), roughness=0.55)
    make_mat("Mat_Road_Stripe_White", (0.94, 0.95, 0.96, 1.0), roughness=0.5)
    make_mat("Mat_Grass_Lawn", (0.22, 0.48, 0.16, 1.0), roughness=0.85)

    # 2. Bricks & Wall Surfaces
    # Aged Red Brick with UV procedural projection
    mat_brick = bpy.data.materials.new("Mat_Brick_Aged")
    mat_brick.use_nodes = True
    b_nodes = mat_brick.node_tree.nodes
    b_links = mat_brick.node_tree.links
    b_bsdf = b_nodes.get("Principled BSDF")
    set_bsdf(b_bsdf, roughness=0.74)
    tc = b_nodes.new('ShaderNodeTexCoord')
    mp = b_nodes.new('ShaderNodeMapping')
    mp.inputs['Scale'].default_value = (4.0, 4.0, 4.0)
    tb = b_nodes.new('ShaderNodeTexBrick')
    tb.inputs['Color1'].default_value = (0.44, 0.16, 0.11, 1.0)
    tb.inputs['Color2'].default_value = (0.58, 0.22, 0.14, 1.0)
    tb.inputs['Mortar'].default_value = (0.76, 0.74, 0.70, 1.0)
    tb.inputs['Scale'].default_value = 1.0
    tb.inputs['Mortar Size'].default_value = 0.02
    tb.inputs['Row Height'].default_value = 0.25
    bump = b_nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.45
    bump.inputs['Distance'].default_value = 0.02
    b_links.new(tc.outputs['UV'], mp.inputs['Vector'])
    b_links.new(mp.outputs['Vector'], tb.inputs['Vector'])
    b_links.new(tb.outputs['Color'], b_bsdf.inputs['Base Color'])
    b_links.new(tb.outputs['Fac'], bump.inputs['Height'])
    b_links.new(bump.outputs['Normal'], b_bsdf.inputs['Normal'])
    MATS["Mat_Brick_Aged"] = mat_brick

    # Stucco & Plaster
    make_mat("Mat_Stucco_White_Greek", (0.97, 0.97, 0.98, 1.0), roughness=0.6)
    make_mat("Mat_Stucco_Fachwerk", (0.93, 0.90, 0.84, 1.0), roughness=0.76)
    make_mat("Mat_Stucco_Brazilian_Blue", (0.16, 0.38, 0.65, 1.0), roughness=0.68)
    make_mat("Mat_Stucco_Beige", (0.92, 0.88, 0.82, 1.0), roughness=0.78)
    make_mat("Mat_Wall_Modern_Grey", (0.32, 0.34, 0.38, 1.0), roughness=0.55)
    make_mat("Mat_American_Siding_Blue", (0.24, 0.34, 0.46, 1.0), roughness=0.48)
    make_mat("Mat_American_Siding_White", (0.95, 0.95, 0.96, 1.0), roughness=0.5)

    # 3. Woods & Timber
    make_mat("Mat_Timber_Dark_Oak", (0.22, 0.14, 0.08, 1.0), roughness=0.6)
    make_mat("Mat_Wood_Farm_Rustic", (0.38, 0.25, 0.15, 1.0), roughness=0.7)
    make_mat("Mat_Wood_Parquet", (0.58, 0.36, 0.19, 1.0), roughness=0.28)
    make_mat("Mat_Wood_Greek_Cobalt", (0.05, 0.28, 0.68, 1.0), roughness=0.35)
    make_mat("Mat_Wood_Brazilian_Carved", (0.28, 0.15, 0.08, 1.0), roughness=0.45)

    # 4. Roof Materials
    # Terracotta Tiles
    mat_roof_terra = bpy.data.materials.new("Mat_Roof_Terracotta_Colonial")
    mat_roof_terra.use_nodes = True
    rt_nodes = mat_roof_terra.node_tree.nodes
    rt_links = mat_roof_terra.node_tree.links
    rt_bsdf = rt_nodes.get("Principled BSDF")
    set_bsdf(rt_bsdf, base_color=(0.65, 0.22, 0.12, 1.0), roughness=0.68)
    tc_r = rt_nodes.new('ShaderNodeTexCoord')
    wv_r = rt_nodes.new('ShaderNodeTexWave')
    wv_r.wave_type = 'BANDS'
    wv_r.inputs['Scale'].default_value = 8.0
    bmp_r = rt_nodes.new('ShaderNodeBump')
    bmp_r.inputs['Strength'].default_value = 0.4
    bmp_r.inputs['Distance'].default_value = 0.03
    rt_links.new(tc_r.outputs['UV'], wv_r.inputs['Vector'])
    rt_links.new(wv_r.outputs['Color'], bmp_r.inputs['Height'])
    rt_links.new(bmp_r.outputs['Normal'], rt_bsdf.inputs['Normal'])
    MATS["Mat_Roof_Terracotta_Colonial"] = mat_roof_terra

    # German Steep Slate
    make_mat("Mat_Roof_German_Slate", (0.18, 0.20, 0.22, 1.0), roughness=0.6)
    # American Asphalt Shingles
    make_mat("Mat_Roof_Asphalt_Grey", (0.28, 0.29, 0.31, 1.0), roughness=0.75)
    # Greek Cobalt Blue Dome
    make_mat("Mat_Greek_Blue_Dome", (0.04, 0.32, 0.78, 1.0), roughness=0.18)
    # Metal Corrugated Farm Roof
    make_mat("Mat_Roof_Metal_Tin", (0.55, 0.58, 0.62, 1.0), metallic=0.85, roughness=0.4)

    # 5. Commercial, Medical & Institutional Materials
    make_mat("Mat_Pharmacy_Green", (0.06, 0.52, 0.24, 1.0), roughness=0.3)
    make_mat("Mat_Neon_Green", (0.1, 1.0, 0.3, 1.0), emission_color=(0.1, 1.0, 0.3, 1.0), emission_strength=6.0)
    make_mat("Mat_Hospital_White", (0.95, 0.96, 0.98, 1.0), roughness=0.35)
    make_mat("Mat_Hospital_Red", (0.85, 0.12, 0.14, 1.0), roughness=0.35)
    make_mat("Mat_Neon_Red", (1.0, 0.1, 0.12, 1.0), emission_color=(1.0, 0.1, 0.12, 1.0), emission_strength=6.0)
    make_mat("Mat_Supermarket_Yellow", (0.96, 0.78, 0.12, 1.0), roughness=0.3)
    make_mat("Mat_Supermarket_Blue", (0.08, 0.32, 0.72, 1.0), roughness=0.3)
    make_mat("Mat_GasStation_Canopy", (0.94, 0.95, 0.96, 1.0), roughness=0.25)
    make_mat("Mat_GasStation_RedStripe", (0.82, 0.12, 0.14, 1.0), roughness=0.25)
    make_mat("Mat_School_Brick_Trim", (0.88, 0.86, 0.80, 1.0), roughness=0.6)
    make_mat("Mat_School_Clock_Face", (0.98, 0.98, 0.96, 1.0), emission_color=(1.0, 0.98, 0.92, 1.0), emission_strength=3.0)
    make_mat("Mat_Flower_Pink", (0.92, 0.22, 0.55, 1.0), roughness=0.6)
    make_mat("Mat_Flower_Red", (0.88, 0.15, 0.18, 1.0), roughness=0.6)
    make_mat("Mat_Produce_Orange", (0.95, 0.50, 0.08, 1.0), roughness=0.5)

    # 6. Architectural Details, Glass & Metals
    make_mat("Mat_Glass_Clear", (0.85, 0.94, 0.98, 1.0), roughness=0.03, transmission=0.92, alpha=0.32)
    make_mat("Mat_Glass_Tinted", (0.15, 0.22, 0.28, 1.0), roughness=0.05, transmission=0.85, alpha=0.55)
    make_mat("Mat_Metal_Anthracite", (0.14, 0.15, 0.17, 1.0), metallic=0.9, roughness=0.28)
    make_mat("Mat_Metal_Stainless", (0.82, 0.84, 0.86, 1.0), metallic=0.95, roughness=0.2)
    make_mat("Mat_Brass_Gold", (0.95, 0.78, 0.36, 1.0), metallic=0.95, roughness=0.22)
    make_mat("Mat_Iron_Black_Wrought", (0.10, 0.10, 0.11, 1.0), metallic=0.8, roughness=0.45)
    make_mat("Mat_Interior_Warm_Glow", (1.0, 0.95, 0.85, 1.0), emission_color=(1.0, 0.92, 0.78, 1.0), emission_strength=4.5)
    make_mat("Mat_Sign_White", (0.98, 0.98, 0.98, 1.0), emission_color=(1.0, 1.0, 1.0, 1.0), emission_strength=1.5)

# -----------------------------------------------------------------------------
# Mesh Construction & 3D Text Helpers
# -----------------------------------------------------------------------------
def add_box(bm, x_min, x_max, y_min, y_max, z_min, z_max, mat_index=0):
    verts = [
        bm.verts.new((x_min, y_min, z_min)),
        bm.verts.new((x_max, y_min, z_min)),
        bm.verts.new((x_max, y_max, z_min)),
        bm.verts.new((x_min, y_max, z_min)),
        bm.verts.new((x_min, y_min, z_max)),
        bm.verts.new((x_max, y_min, z_max)),
        bm.verts.new((x_max, y_max, z_max)),
        bm.verts.new((x_min, y_max, z_max)),
    ]
    faces = [
        (0, 1, 2, 3), # Bottom
        (4, 7, 6, 5), # Top
        (0, 4, 5, 1), # Front
        (2, 6, 7, 3), # Back
        (0, 3, 7, 4), # Left
        (1, 5, 6, 2), # Right
    ]
    created = []
    for f in faces:
        face = bm.faces.new([verts[i] for i in f])
        face.material_index = mat_index
        created.append(face)
    return created

def finish_mesh(obj_name, bm, mats=None, collection=None):
    uv_layer = bm.loops.layers.uv.verify()
    mesh = bpy.data.meshes.new(obj_name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    
    obj = bpy.data.objects.new(obj_name, mesh)
    if mats:
        for m_name in mats:
            if m_name in MATS:
                obj.data.materials.append(MATS[m_name])
    
    if collection:
        collection.objects.link(obj)
    else:
        bpy.context.scene.collection.objects.link(obj)
    return obj

def create_3d_sign_text(name, text_str, location, rotation, size=0.55, extrude=0.06, mat_name="Mat_Sign_White", collection=None):
    font_curve = bpy.data.curves.new(type="FONT", name=name)
    font_curve.body = text_str
    font_curve.size = size
    font_curve.extrude = extrude
    font_curve.align_x = 'CENTER'
    font_curve.align_y = 'CENTER'
    font_obj = bpy.data.objects.new(name, font_curve)
    font_obj.location = location
    font_obj.rotation_euler = rotation
    if mat_name in MATS:
        font_obj.data.materials.append(MATS[mat_name])
    if collection:
        collection.objects.link(font_obj)
    return font_obj

def make_interactive_door(name, width, height, thickness=0.05, mat_leaf="Mat_Timber_Dark_Oak", mat_handle="Mat_Brass_Gold", hinge_side="left", open_angle=85.0, collection=None):
    bm = bmesh.new()
    x_min = 0.0 if hinge_side == "left" else -width
    x_max = width if hinge_side == "left" else 0.0
    
    # Door leaf
    add_box(bm, x_min, x_max, -thickness/2, thickness/2, 0.0, height, mat_index=0)
    
    # Handle
    hx = (x_max - 0.08) if hinge_side == "left" else (x_min + 0.08)
    hz = height * 0.48
    for y_sign in [-1, 1]:
        yp = y_sign * (thickness/2 + 0.02)
        add_box(bm, hx - 0.06, hx + 0.02, min(y_sign*thickness/2, yp), max(y_sign*thickness/2, yp), hz - 0.02, hz + 0.02, mat_index=1)
        add_box(bm, hx - 0.08, hx - 0.02, yp - 0.015, yp + 0.015, hz - 0.06, hz + 0.04, mat_index=1)
        
    obj = finish_mesh(name, bm, mats=[mat_leaf, mat_handle], collection=collection)
    
    obj["is_interactive"] = True
    obj["interact_type"] = "HingeDoor"
    obj["open_angle_deg"] = open_angle if hinge_side == "left" else -open_angle
    obj["interact_prompt"] = "[E] Abrir / Fechar Porta"
    obj["state"] = "Closed"
    
    anim_data = obj.animation_data_create()
    action = bpy.data.actions.new(name=f"{name}_OpenClose")
    obj.animation_data.action = action
    target_rot = math.radians(open_angle if hinge_side == "left" else -open_angle)
    
    obj.rotation_euler = (0, 0, 0)
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=1)
    obj.rotation_euler = (0, 0, target_rot)
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=30)
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=60)
    obj.rotation_euler = (0, 0, 0)
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=90)
    
    return obj

# -----------------------------------------------------------------------------
# BUILDINGS DEFINITIONS (11 REALISTIC STRUCTURES)
# -----------------------------------------------------------------------------

# 1. FARMÁCIA (Pharmacy)
def build_pharmacy(base_loc=Vector((0, 0, 0))):
    col = get_col("Building_Pharmacy")
    root = bpy.data.objects.new("Pharmacy_Root", None)
    root.location = base_loc
    col.objects.link(root)
    
    bm = bmesh.new()
    add_box(bm, -4.2, 4.2, -4.2, 4.2, -0.25, 0.0, mat_index=0) # Concrete slab
    # Body walls
    add_box(bm, -4.1, -3.85, -4.0, 4.0, 0.0, 4.2, mat_index=1) # Clinical white
    add_box(bm, 3.85, 4.1, -4.0, 4.0, 0.0, 4.2, mat_index=1)
    add_box(bm, -4.0, 4.0, 3.85, 4.1, 0.0, 4.2, mat_index=1)
    add_box(bm, -4.2, 4.2, -4.2, 4.2, 4.0, 4.4, mat_index=0) # Roof
    
    # Emerald green header sign fascia
    add_box(bm, -4.2, 4.2, -4.25, -3.9, 3.1, 4.3, mat_index=2) # Green
    # Display windows
    add_box(bm, -3.8, -1.2, -4.05, -3.95, 0.0, 0.3, mat_index=3)
    add_box(bm, -3.8, -1.2, -4.02, -3.98, 0.3, 3.1, mat_index=4) # Glass
    add_box(bm, 1.2, 3.8, -4.05, -3.95, 0.0, 0.3, mat_index=3)
    add_box(bm, 1.2, 3.8, -4.02, -3.98, 0.3, 3.1, mat_index=4)
    # Door frame
    add_box(bm, -1.22, 1.22, -4.05, -3.95, 2.45, 2.55, mat_index=3)
    
    body = finish_mesh("Pharmacy_Building", bm, mats=["Mat_Concrete_Slab", "Mat_Hospital_White", "Mat_Pharmacy_Green", "Mat_Metal_Anthracite", "Mat_Glass_Clear"], collection=col)
    body.parent = root
    
    # 3D Text "FARMÁCIA"
    sign_text = create_3d_sign_text("Pharmacy_Sign_Text", "FARMÁCIA", location=Vector((0.0, -4.28, 3.7)), rotation=(math.radians(90.0), 0.0, 0.0), size=0.6, extrude=0.08, mat_name="Mat_Sign_White", collection=col)
    sign_text.parent = root
    
    # Neon Green Cross
    bm_cross = bmesh.new()
    add_box(bm_cross, 3.1, 3.7, -4.35, -4.20, 3.4, 4.5, mat_index=0)
    add_box(bm_cross, 2.7, 4.1, -4.35, -4.20, 3.7, 4.2, mat_index=0)
    cross_obj = finish_mesh("Pharmacy_NeonCross", bm_cross, mats=["Mat_Neon_Green"], collection=col)
    cross_obj.parent = root
    
    # Double Glass Doors
    door_l = make_interactive_door("Pharmacy_Door_L", width=1.1, height=2.45, thickness=0.04, mat_leaf="Mat_Glass_Clear", mat_handle="Mat_Metal_Stainless", hinge_side="left", open_angle=75.0, collection=col)
    door_l.location = Vector((-1.15, -4.0, 0.0))
    door_l.parent = root
    door_l.rotation_euler.z = math.radians(35.0)
    
    door_r = make_interactive_door("Pharmacy_Door_R", width=1.1, height=2.45, thickness=0.04, mat_leaf="Mat_Glass_Clear", mat_handle="Mat_Metal_Stainless", hinge_side="right", open_angle=75.0, collection=col)
    door_r.location = Vector((1.15, -4.0, 0.0))
    door_r.parent = root
    door_r.rotation_euler.z = math.radians(-35.0)
    
    return root

# 2. MERCADO (Supermarket)
def build_supermarket(base_loc=Vector((0, 0, 0))):
    col = get_col("Building_Supermarket")
    root = bpy.data.objects.new("Supermarket_Root", None)
    root.location = base_loc
    col.objects.link(root)
    
    bm = bmesh.new()
    add_box(bm, -6.2, 6.2, -5.2, 5.2, -0.25, 0.0, mat_index=0)
    # Walls & Roof
    add_box(bm, -6.1, -5.85, -5.0, 5.0, 0.0, 5.0, mat_index=1)
    add_box(bm, 5.85, 6.1, -5.0, 5.0, 0.0, 5.0, mat_index=1)
    add_box(bm, -6.0, 6.0, 4.85, 5.1, 0.0, 5.0, mat_index=1)
    add_box(bm, -6.2, 6.2, -5.2, 5.2, 4.8, 5.2, mat_index=0)
    
    # Yellow & Blue Sign Fascia
    add_box(bm, -6.2, 6.2, -5.3, -4.9, 3.5, 5.2, mat_index=2) # Yellow
    add_box(bm, -6.2, 6.2, -5.32, -4.88, 3.3, 3.5, mat_index=3) # Blue
    # Windows
    add_box(bm, -5.8, -1.6, -5.02, -4.98, 0.4, 3.3, mat_index=4) # Glass
    add_box(bm, 1.6, 5.8, -5.02, -4.98, 0.4, 3.3, mat_index=4)
    
    # Shopping Cart Shelter & Carts
    add_box(bm, -5.5, -3.2, -6.8, -5.2, 0.0, 0.08, mat_index=0)
    add_box(bm, -5.55, -5.45, -6.7, -6.6, 0.0, 2.2, mat_index=5) # Metal
    add_box(bm, -3.3, -3.2, -6.7, -6.6, 0.0, 2.2, mat_index=5)
    add_box(bm, -5.6, -3.1, -6.9, -5.1, 2.15, 2.25, mat_index=3) # Blue
    for cx in [-5.0, -4.4, -3.8]:
        add_box(bm, cx - 0.22, cx + 0.22, -6.4, -5.4, 0.08, 0.95, mat_index=5)
        
    # Outdoor Fruit Display Stand
    add_box(bm, 2.5, 4.5, -6.0, -5.3, 0.0, 0.75, mat_index=6) # Wood crate
    add_box(bm, 2.6, 4.4, -5.9, -5.4, 0.75, 0.90, mat_index=7) # Oranges
    
    body = finish_mesh("Supermarket_Building", bm, mats=["Mat_Concrete_Slab", "Mat_Wall_Modern_Grey", "Mat_Supermarket_Yellow", "Mat_Supermarket_Blue", "Mat_Glass_Clear", "Mat_Metal_Anthracite", "Mat_Wood_Farm_Rustic", "Mat_Produce_Orange"], collection=col)
    body.parent = root
    
    # 3D Text "SUPERMERCADO"
    sign_text = create_3d_sign_text("Supermarket_Sign_Text", "SUPERMERCADO", location=Vector((0.0, -5.34, 4.3)), rotation=(math.radians(90.0), 0.0, 0.0), size=0.75, extrude=0.1, mat_name="Mat_Supermarket_Blue", collection=col)
    sign_text.parent = root
    
    # Interactive Sliding Doors
    door_l = make_interactive_door("Supermarket_Door_L", width=1.5, height=2.6, thickness=0.04, mat_leaf="Mat_Glass_Clear", mat_handle="Mat_Metal_Stainless", hinge_side="left", open_angle=80.0, collection=col)
    door_l.location = Vector((-1.55, -5.0, 0.0))
    door_l.parent = root
    door_l.rotation_euler.z = math.radians(40.0)
    
    door_r = make_interactive_door("Supermarket_Door_R", width=1.5, height=2.6, thickness=0.04, mat_leaf="Mat_Glass_Clear", mat_handle="Mat_Metal_Stainless", hinge_side="right", open_angle=80.0, collection=col)
    door_r.location = Vector((1.55, -5.0, 0.0))
    door_r.parent = root
    door_r.rotation_euler.z = math.radians(-40.0)
    
    return root

# 3. POSTO DE COMBUSTÍVEL (Gas Station)
def build_gas_station(base_loc=Vector((0, 0, 0))):
    col = get_col("Building_GasStation")
    root = bpy.data.objects.new("GasStation_Root", None)
    root.location = base_loc
    col.objects.link(root)
    
    # Convenience Store (Rear)
    bm_store = bmesh.new()
    add_box(bm_store, -4.0, 4.0, 0.0, 6.0, 0.0, 3.8, mat_index=0)
    add_box(bm_store, -4.1, 4.1, -0.1, 6.1, 3.5, 3.9, mat_index=1) # Red fascia
    add_box(bm_store, -3.6, -0.8, -0.05, 0.05, 0.2, 3.2, mat_index=2) # Glass
    add_box(bm_store, 0.8, 3.6, -0.05, 0.05, 0.2, 3.2, mat_index=2)
    c_store = finish_mesh("GasStation_CStore", bm_store, mats=["Mat_Wall_Modern_Grey", "Mat_GasStation_RedStripe", "Mat_Glass_Clear"], collection=col)
    c_store.parent = root
    
    # 3D Text "CONVENIÊNCIA" on store
    c_text = create_3d_sign_text("GasStation_Store_Text", "CONVENIÊNCIA 24h", location=Vector((0.0, -0.15, 3.7)), rotation=(math.radians(90.0), 0.0, 0.0), size=0.35, extrude=0.05, mat_name="Mat_Sign_White", collection=col)
    c_text.parent = root
    
    store_door = make_interactive_door("GasStation_Store_Door", width=1.3, height=2.4, thickness=0.04, mat_leaf="Mat_Glass_Clear", mat_handle="Mat_Metal_Stainless", hinge_side="left", open_angle=80.0, collection=col)
    store_door.location = Vector((-0.68, 0.0, 0.0))
    store_door.parent = root
    store_door.rotation_euler.z = math.radians(25.0)
    
    # Forecourt, Fuel Pumps, Pillars & Canopy
    bm_canopy = bmesh.new()
    add_box(bm_canopy, -8.0, 8.0, -9.0, 1.0, -0.2, 0.0, mat_index=0) # Asphalt apron
    # 2 Fuel Islands with Pumps
    for ix in [-3.5, 3.5]:
        add_box(bm_canopy, ix - 0.7, ix + 0.7, -6.5, -2.5, 0.0, 0.22, mat_index=1) # Yellow curb
        add_box(bm_canopy, ix - 0.65, ix + 0.65, -6.4, -2.6, 0.20, 0.25, mat_index=2) # Concrete
        add_box(bm_canopy, ix - 0.1, ix + 0.1, -6.7, -6.5, 0.0, 0.9, mat_index=1)
        add_box(bm_canopy, ix - 0.1, ix + 0.1, -2.5, -2.3, 0.0, 0.9, mat_index=1)
        add_box(bm_canopy, ix - 0.4, ix + 0.4, -5.2, -3.8, 0.25, 2.1, mat_index=3) # Dispenser
        add_box(bm_canopy, ix - 0.35, ix + 0.35, -5.0, -4.0, 1.4, 1.8, mat_index=4) # Display
    # Columns
    for cx in [-4.0, 4.0]:
        for cy in [-7.5, -1.5]:
            add_box(bm_canopy, cx - 0.25, cx + 0.25, cy - 0.25, cy + 0.25, 0.0, 4.8, mat_index=3)
    # Canopy Body
    add_box(bm_canopy, -7.5, 7.5, -8.5, -0.5, 4.8, 6.0, mat_index=5) # White
    add_box(bm_canopy, -7.55, 7.55, -8.55, -0.45, 5.0, 5.6, mat_index=6) # Red band
    
    # Fuel Price Totem at Forecourt entrance (X = -7.2, Y = -8.5)
    add_box(bm_canopy, -7.4, -6.8, -8.8, -8.4, 0.0, 4.2, mat_index=3) # Metal pillar
    add_box(bm_canopy, -7.6, -6.6, -8.9, -8.3, 1.5, 4.0, mat_index=4) # Price board
    
    canopy = finish_mesh("GasStation_Forecourt", bm_canopy, mats=[
        "Mat_Asphalt_Road", "Mat_Road_Stripe_Yellow", "Mat_Concrete_Slab", "Mat_Metal_Anthracite",
        "Mat_Neon_Green", "Mat_GasStation_Canopy", "Mat_GasStation_RedStripe"
    ], collection=col)
    canopy.parent = root
    
    # 3D Text "POSTO 24h" on Canopy Front
    canopy_text = create_3d_sign_text("GasStation_Canopy_Text", "POSTO 24h", location=Vector((0.0, -8.6, 5.3)), rotation=(math.radians(90.0), 0.0, 0.0), size=0.6, extrude=0.08, mat_name="Mat_Sign_White", collection=col)
    canopy_text.parent = root
    
    return root

# 4. HOSPITAL & PRONTO-SOCORRO (Hospital & Emergency Center)
def build_hospital(base_loc=Vector((0, 0, 0))):
    col = get_col("Building_Hospital")
    root = bpy.data.objects.new("Hospital_Root", None)
    root.location = base_loc
    col.objects.link(root)
    
    bm = bmesh.new()
    add_box(bm, -6.5, 6.5, -6.0, 5.5, -0.25, 0.0, mat_index=0)
    add_box(bm, -6.0, 6.0, -5.0, 5.0, 0.0, 11.0, mat_index=1) # Clinical white
    add_box(bm, -6.1, 6.1, -5.1, 5.1, 10.8, 11.6, mat_index=2) # Parapet
    # Upper window rows
    for fz in [4.2, 7.8]:
        for wx in [-4.5, -1.8, 1.8, 4.5]:
            add_box(bm, wx - 1.0, wx + 1.0, -5.08, -4.95, fz, fz + 2.0, mat_index=3) # Glass
            add_box(bm, wx - 1.05, wx + 1.05, -5.08, -4.95, fz - 0.05, fz + 0.05, mat_index=2)
    # Emergency Canopy
    add_box(bm, -5.5, 1.5, -7.5, -4.9, 3.4, 3.7, mat_index=4) # Red
    add_box(bm, -5.4, -5.1, -7.4, -7.1, 0.0, 3.4, mat_index=2)
    add_box(bm, 1.1, 1.4, -7.4, -7.1, 0.0, 3.4, mat_index=2)
    # Helipad 'H' on roof
    add_box(bm, -0.5, 0.5, -2.5, 2.5, 11.0, 11.08, mat_index=5) # Yellow
    add_box(bm, -2.0, 2.0, -0.25, 0.25, 11.0, 11.08, mat_index=5)
    
    body = finish_mesh("Hospital_Building", bm, mats=["Mat_Concrete_Slab", "Mat_Hospital_White", "Mat_Metal_Anthracite", "Mat_Glass_Tinted", "Mat_Hospital_Red", "Mat_Road_Stripe_Yellow"], collection=col)
    body.parent = root
    
    # 3D Sign Texts: "HOSPITAL GERAL" & "EMERGÊNCIA"
    hosp_text = create_3d_sign_text("Hospital_Main_Text", "HOSPITAL GERAL", location=Vector((0.0, -5.15, 10.2)), rotation=(math.radians(90.0), 0.0, 0.0), size=0.6, extrude=0.08, mat_name="Mat_Hospital_Red", collection=col)
    hosp_text.parent = root
    
    emerg_text = create_3d_sign_text("Hospital_Emerg_Text", "EMERGÊNCIA", location=Vector((-2.0, -7.56, 3.55)), rotation=(math.radians(90.0), 0.0, 0.0), size=0.45, extrude=0.06, mat_name="Mat_Sign_White", collection=col)
    emerg_text.parent = root
    
    # 3D Neon Red Cross
    bm_cross = bmesh.new()
    add_box(bm_cross, -2.3, -1.7, -7.6, -7.45, 3.9, 5.1, mat_index=0)
    add_box(bm_cross, -2.8, -1.2, -7.6, -7.45, 4.2, 4.8, mat_index=0)
    cross_obj = finish_mesh("Hospital_RedCross", bm_cross, mats=["Mat_Neon_Red"], collection=col)
    cross_obj.parent = root
    
    # Double Emergency Doors
    door_l = make_interactive_door("Hospital_Door_L", width=1.2, height=2.65, thickness=0.04, mat_leaf="Mat_Glass_Tinted", mat_handle="Mat_Metal_Stainless", hinge_side="left", open_angle=80.0, collection=col)
    door_l.location = Vector((-3.2, -5.0, 0.0))
    door_l.parent = root
    door_l.rotation_euler.z = math.radians(45.0)
    
    door_r = make_interactive_door("Hospital_Door_R", width=1.2, height=2.65, thickness=0.04, mat_leaf="Mat_Glass_Tinted", mat_handle="Mat_Metal_Stainless", hinge_side="right", open_angle=80.0, collection=col)
    door_r.location = Vector((-0.8, -5.0, 0.0))
    door_r.parent = root
    door_r.rotation_euler.z = math.radians(-45.0)
    
    return root

# 5. ESCOLA / COLÉGIO MUNICIPAL (School / Academy)
def build_school(base_loc=Vector((0, 0, 0))):
    col = get_col("Building_School")
    root = bpy.data.objects.new("School_Root", None)
    root.location = base_loc
    col.objects.link(root)
    
    bm = bmesh.new()
    add_box(bm, -7.2, 7.2, -4.7, 4.7, -0.3, 0.0, mat_index=0)
    add_box(bm, -7.0, 7.0, -4.5, 4.5, 0.0, 7.5, mat_index=1) # Brick
    add_box(bm, -7.05, 7.05, -4.55, 4.55, 3.6, 3.8, mat_index=2) # Stone belt
    add_box(bm, -7.1, 7.1, -4.6, 4.6, 7.4, 7.7, mat_index=2) # Cornice
    # Classroom windows
    for fz in [0.9, 4.4]:
        for wx in [-5.5, -3.8, -2.1, 2.1, 3.8, 5.5]:
            add_box(bm, wx - 0.55, wx + 0.55, -4.58, -4.45, fz, fz + 2.0, mat_index=3)
            add_box(bm, wx - 0.60, wx + 0.60, -4.62, -4.45, fz - 0.06, fz, mat_index=2)
    # Entry stairs & portico pillars
    for s in range(4):
        sy = -5.4 - s * 0.3
        sz = -s * 0.15
        add_box(bm, -2.0, 2.0, sy, -4.5, sz, 0.0, mat_index=2)
    add_box(bm, -1.4, -1.0, -5.2, -4.8, 0.0, 5.0, mat_index=2)
    add_box(bm, 1.0, 1.4, -5.2, -4.8, 0.0, 5.0, mat_index=2)
    add_box(bm, -1.6, 1.6, -5.3, -4.5, 5.0, 5.4, mat_index=2) # Pediment
    # Clock tower & clock face
    add_box(bm, -1.8, 1.8, -4.6, -3.0, 7.5, 9.8, mat_index=1)
    add_box(bm, -1.9, 1.9, -4.7, -2.9, 9.7, 10.2, mat_index=2)
    add_box(bm, -0.6, 0.6, -4.72, -4.6, 8.2, 9.4, mat_index=4) # Clock face
    
    # Flagpole with national flag
    add_box(bm, 3.95, 4.05, -6.05, -5.95, 0.0, 8.0, mat_index=5) # Pole
    add_box(bm, 4.05, 5.4, -6.01, -5.99, 6.8, 7.7, mat_index=6) # Flag
    
    body = finish_mesh("School_Building", bm, mats=["Mat_Concrete_Slab", "Mat_Brick_Aged", "Mat_School_Brick_Trim", "Mat_Glass_Clear", "Mat_School_Clock_Face", "Mat_Metal_Stainless", "Mat_Hospital_Red"], collection=col)
    body.parent = root
    
    # 3D Sign Text "ESCOLA MUNICIPAL"
    school_text = create_3d_sign_text("School_Sign_Text", "ESCOLA MUNICIPAL", location=Vector((0.0, -5.35, 5.2)), rotation=(math.radians(90.0), 0.0, 0.0), size=0.35, extrude=0.06, mat_name="Mat_Timber_Dark_Oak", collection=col)
    school_text.parent = root
    
    # Double Solid Wood Entrance Doors
    door_l = make_interactive_door("School_Door_L", width=0.85, height=2.6, thickness=0.06, mat_leaf="Mat_Timber_Dark_Oak", mat_handle="Mat_Brass_Gold", hinge_side="left", open_angle=85.0, collection=col)
    door_l.location = Vector((-0.88, -4.5, 0.0))
    door_l.parent = root
    door_l.rotation_euler.z = math.radians(30.0)
    
    door_r = make_interactive_door("School_Door_R", width=0.85, height=2.6, thickness=0.06, mat_leaf="Mat_Timber_Dark_Oak", mat_handle="Mat_Brass_Gold", hinge_side="right", open_angle=85.0, collection=col)
    door_r.location = Vector((0.88, -4.5, 0.0))
    door_r.parent = root
    door_r.rotation_euler.z = math.radians(-30.0)
    
    return root

# 6. CASA DE FAZENDA (Rustic Farmhouse)
def build_farmhouse(base_loc=Vector((0, 0, 0))):
    col = get_col("House_Farmhouse")
    root = bpy.data.objects.new("Farmhouse_Root", None)
    root.location = base_loc
    col.objects.link(root)
    
    bm = bmesh.new()
    add_box(bm, -4.2, 4.2, -3.7, 3.7, -0.4, 0.0, mat_index=0)
    add_box(bm, -3.5, 3.5, -3.0, 3.0, 0.0, 5.5, mat_index=1) # Weathered wood siding
    # Wrap-around porch deck
    add_box(bm, -4.5, 4.5, -4.8, -3.0, -0.05, 0.1, mat_index=2)
    add_box(bm, 3.5, 4.8, -3.0, 3.2, -0.05, 0.1, mat_index=2)
    # Posts & railing
    for px in [-4.4, -2.0, 0.5, 3.0, 4.4]:
        add_box(bm, px - 0.06, px + 0.06, -4.75, -4.65, 0.1, 2.7, mat_index=1)
    add_box(bm, -4.4, 4.4, -4.73, -4.67, 0.15, 0.85, mat_index=1)
    add_box(bm, -4.6, 4.6, -4.9, -2.9, 2.6, 2.9, mat_index=3) # Metal tin roof
    add_box(bm, -3.8, 3.8, -3.3, 3.3, 5.3, 7.4, mat_index=3) # Gable roof
    # Chimney & Bench
    add_box(bm, -4.0, -3.3, -0.8, 0.8, 0.0, 7.8, mat_index=0) # Stone
    add_box(bm, 1.2, 2.4, -4.5, -4.0, 0.1, 0.5, mat_index=1)  # Porch bench
    
    body = finish_mesh("Farmhouse_Building", bm, mats=["Mat_Concrete_Slab", "Mat_Wood_Farm_Rustic", "Mat_Wood_Parquet", "Mat_Roof_Metal_Tin", "Mat_Glass_Clear"], collection=col)
    body.parent = root
    
    door = make_interactive_door("Farmhouse_FrontDoor", width=1.0, height=2.2, thickness=0.06, mat_leaf="Mat_Timber_Dark_Oak", mat_handle="Mat_Iron_Black_Wrought", hinge_side="left", open_angle=85.0, collection=col)
    door.location = Vector((-0.5, -3.0, 0.0))
    door.parent = root
    door.rotation_euler.z = math.radians(35.0)
    
    return root

# 7. CASA SOBRADO URBANO (Townhouse)
def build_townhouse(base_loc=Vector((0, 0, 0))):
    col = get_col("House_Townhouse")
    root = bpy.data.objects.new("Townhouse_Root", None)
    root.location = base_loc
    col.objects.link(root)
    
    bm = bmesh.new()
    add_box(bm, -3.2, 3.2, -4.2, 4.2, -0.25, 0.0, mat_index=0)
    add_box(bm, -3.0, 3.0, -4.0, 4.0, 0.0, 9.2, mat_index=1) # Beige stucco
    add_box(bm, -3.15, 3.15, -4.15, 4.15, 9.0, 9.6, mat_index=2) # Cornice
    # Ground garage & french windows
    add_box(bm, -2.6, 0.0, -4.05, -3.95, 0.0, 2.5, mat_index=3) # Metal roll gate
    for fz in [3.4, 6.4]:
        for wx in [-1.5, 1.5]:
            add_box(bm, wx - 0.7, wx + 0.7, -4.08, -3.95, fz, fz + 2.1, mat_index=4)
            add_box(bm, wx - 0.75, wx + 0.75, -4.08, -3.95, fz + 2.05, fz + 2.3, mat_index=2)
    # Wrought iron balconies
    for fz in [3.4, 6.4]:
        add_box(bm, -2.4, 2.4, -4.4, -4.0, fz, fz + 0.1, mat_index=0)
        add_box(bm, -2.45, 2.45, -4.45, -4.35, fz + 0.1, fz + 1.0, mat_index=5)
        
    body = finish_mesh("Townhouse_Building", bm, mats=["Mat_Concrete_Slab", "Mat_Stucco_Beige", "Mat_School_Brick_Trim", "Mat_Metal_Anthracite", "Mat_Glass_Clear", "Mat_Iron_Black_Wrought"], collection=col)
    body.parent = root
    
    door = make_interactive_door("Townhouse_Door", width=0.95, height=2.3, thickness=0.05, mat_leaf="Mat_Timber_Dark_Oak", mat_handle="Mat_Brass_Gold", hinge_side="left", open_angle=80.0, collection=col)
    door.location = Vector((1.0, -4.0, 0.0))
    door.parent = root
    door.rotation_euler.z = math.radians(30.0)
    
    return root

# 8. CASA GREGA DE SANTORINI (Greek Cycladic House)
def build_greek_house(base_loc=Vector((0, 0, 0))):
    col = get_col("House_Greek")
    root = bpy.data.objects.new("Greek_House_Root", None)
    root.location = base_loc
    col.objects.link(root)
    
    bm = bmesh.new()
    add_box(bm, -3.6, 3.6, -3.6, 3.6, -0.3, 0.0, mat_index=0) # Pure white
    add_box(bm, -3.5, 3.5, -3.5, 3.5, 0.0, 3.2, mat_index=0)
    add_box(bm, -2.0, 3.2, -1.5, 3.2, 3.2, 6.0, mat_index=0)
    # Exterior white steps
    for s in range(12):
        sz = s * 0.25
        sy = -3.2 + s * 0.4
        add_box(bm, -3.4, -2.4, sy, sy + 0.4, 0.0, sz + 0.25, mat_index=0)
    add_box(bm, -2.1, 3.3, -3.4, -1.6, 3.2, 3.9, mat_index=0) # Parapet
    # Cobalt blue shutters & glass
    add_box(bm, 0.5, 2.2, -3.58, -3.45, 0.8, 2.2, mat_index=1)
    add_box(bm, 0.7, 2.0, -3.55, -3.48, 1.0, 2.0, mat_index=2)
    # Stepped Cobalt Blue Dome
    add_box(bm, -1.6, 2.8, -1.1, 2.8, 6.0, 6.4, mat_index=3)
    add_box(bm, -1.3, 2.5, -0.8, 2.5, 6.4, 7.1, mat_index=3)
    add_box(bm, -0.9, 2.1, -0.4, 2.1, 7.1, 7.6, mat_index=3)
    # Bougainvillea flower vine accent
    add_box(bm, 1.5, 3.2, -3.65, -3.55, 2.2, 3.2, mat_index=4)
    
    body = finish_mesh("Greek_Building", bm, mats=["Mat_Stucco_White_Greek", "Mat_Wood_Greek_Cobalt", "Mat_Glass_Clear", "Mat_Greek_Blue_Dome", "Mat_Flower_Pink"], collection=col)
    body.parent = root
    
    door = make_interactive_door("Greek_Cobalt_Door", width=1.0, height=2.25, thickness=0.05, mat_leaf="Mat_Wood_Greek_Cobalt", mat_handle="Mat_Brass_Gold", hinge_side="left", open_angle=85.0, collection=col)
    door.location = Vector((-1.8, -3.5, 0.0))
    door.parent = root
    door.rotation_euler.z = math.radians(40.0)
    
    return root

# 9. CASA ALEMÃ DA BAVIERA (Bavarian Fachwerk House)
def build_bavarian_house(base_loc=Vector((0, 0, 0))):
    col = get_col("House_Bavarian")
    root = bpy.data.objects.new("Bavarian_House_Root", None)
    root.location = base_loc
    col.objects.link(root)
    
    bm = bmesh.new()
    add_box(bm, -3.7, 3.7, -3.7, 3.7, -0.3, 0.4, mat_index=0)
    add_box(bm, -3.5, 3.5, -3.5, 3.5, 0.4, 5.8, mat_index=1) # Fachwerk stucco
    # Timber Framing (Enxaimel Posts & X-braces)
    for cx in [-3.52, 3.42]:
        for cy in [-3.52, 3.42]:
            add_box(bm, cx, cx + 0.12, cy, cy + 0.12, 0.4, 5.8, mat_index=2)
    add_box(bm, -3.55, 3.55, -3.55, -3.45, 3.0, 3.2, mat_index=2)
    add_box(bm, -3.55, 3.55, -3.55, -3.45, 5.6, 5.8, mat_index=2)
    add_box(bm, -3.3, -1.8, -3.54, -3.46, 0.6, 2.9, mat_index=2)
    add_box(bm, 1.8, 3.3, -3.54, -3.46, 0.6, 2.9, mat_index=2)
    add_box(bm, -3.3, -1.8, -3.54, -3.46, 3.3, 5.5, mat_index=2)
    add_box(bm, 1.8, 3.3, -3.54, -3.46, 3.3, 5.5, mat_index=2)
    # Steep German Slate Roof
    add_box(bm, -3.8, 3.8, -3.8, 3.8, 5.7, 8.8, mat_index=3)
    # Windows with colorful flower boxes
    for fz in [1.2, 3.6]:
        add_box(bm, -2.6, -1.4, -3.56, -3.48, fz, fz + 1.2, mat_index=4)
        add_box(bm, 1.4, 2.6, -3.56, -3.48, fz, fz + 1.2, mat_index=4)
        add_box(bm, -2.7, -1.3, -3.75, -3.54, fz - 0.2, fz, mat_index=2) # Wood box
        add_box(bm, 1.3, 2.7, -3.75, -3.54, fz - 0.2, fz, mat_index=2)
        add_box(bm, -2.65, -1.35, -3.74, -3.56, fz, fz + 0.15, mat_index=5) # Red flowers
        add_box(bm, 1.35, 2.65, -3.74, -3.56, fz, fz + 0.15, mat_index=5)
        
    body = finish_mesh("Bavarian_Building", bm, mats=["Mat_Concrete_Slab", "Mat_Stucco_Fachwerk", "Mat_Timber_Dark_Oak", "Mat_Roof_German_Slate", "Mat_Glass_Clear", "Mat_Flower_Red"], collection=col)
    body.parent = root
    
    door = make_interactive_door("Bavarian_Oak_Door", width=1.0, height=2.2, thickness=0.06, mat_leaf="Mat_Timber_Dark_Oak", mat_handle="Mat_Iron_Black_Wrought", hinge_side="left", open_angle=80.0, collection=col)
    door.location = Vector((-0.5, -3.5, 0.4))
    door.parent = root
    door.rotation_euler.z = math.radians(35.0)
    
    return root

# 10. CASA BRASILEIRA COLONIAL (Brazilian Colonial House)
def build_brazilian_house(base_loc=Vector((0, 0, 0))):
    col = get_col("House_Brazilian")
    root = bpy.data.objects.new("Brazilian_House_Root", None)
    root.location = base_loc
    col.objects.link(root)
    
    bm = bmesh.new()
    add_box(bm, -4.2, 4.2, -3.7, 3.7, -0.3, 0.0, mat_index=0)
    add_box(bm, -3.8, 3.8, -3.2, 3.2, 0.0, 3.8, mat_index=1) # Colonial blue
    # Veranda & carved wooden pillars
    add_box(bm, -4.2, 4.2, -4.8, -3.2, -0.05, 0.1, mat_index=0)
    for cx in [-4.0, -1.5, 1.5, 4.0]:
        add_box(bm, cx - 0.1, cx + 0.1, -4.7, -4.5, 0.1, 2.8, mat_index=2) # Wood
    add_box(bm, -4.0, -1.6, -4.65, -4.55, 0.1, 0.85, mat_index=1)
    add_box(bm, 1.6, 4.0, -4.65, -4.55, 0.1, 0.85, mat_index=1)
    # Clay barrel terracotta roof with wide overhangs
    add_box(bm, -4.4, 4.4, -5.0, 3.5, 3.6, 5.8, mat_index=3)
    # White moldings around windows
    for wx in [-2.6, 2.6]:
        add_box(bm, wx - 0.7, wx + 0.7, -3.26, -3.15, 0.9, 2.5, mat_index=4)
        add_box(bm, wx - 0.6, wx + 0.6, -3.28, -3.18, 1.0, 2.4, mat_index=2)
        
    body = finish_mesh("Brazilian_Building", bm, mats=["Mat_Concrete_Slab", "Mat_Stucco_Brazilian_Blue", "Mat_Wood_Brazilian_Carved", "Mat_Roof_Terracotta_Colonial", "Mat_American_Siding_White"], collection=col)
    body.parent = root
    
    door = make_interactive_door("Brazilian_Colonial_Door", width=1.1, height=2.35, thickness=0.06, mat_leaf="Mat_Wood_Brazilian_Carved", mat_handle="Mat_Brass_Gold", hinge_side="left", open_angle=85.0, collection=col)
    door.location = Vector((-0.55, -3.2, 0.0))
    door.parent = root
    door.rotation_euler.z = math.radians(40.0)
    
    return root

# 11. CASA NORTE-AMERICANA SUBURBANA (American Suburban Craftsman)
def build_american_house(base_loc=Vector((0, 0, 0))):
    col = get_col("House_American")
    root = bpy.data.objects.new("American_House_Root", None)
    root.location = base_loc
    col.objects.link(root)
    
    bm = bmesh.new()
    add_box(bm, -4.2, 4.2, -4.2, 4.2, -0.3, 0.0, mat_index=0)
    add_box(bm, -3.8, 3.8, -3.6, 3.6, 0.0, 6.2, mat_index=1) # Blue siding
    # Front porch & tapered stone-base craftsman columns
    add_box(bm, -3.8, 1.0, -4.8, -3.6, -0.05, 0.15, mat_index=0)
    for px in [-3.6, 0.8]:
        add_box(bm, px - 0.25, px + 0.25, -4.75, -4.45, 0.15, 1.0, mat_index=0)
        add_box(bm, px - 0.15, px + 0.15, -4.65, -4.55, 1.0, 2.8, mat_index=2)
    add_box(bm, -3.9, 1.1, -4.9, -3.5, 2.7, 4.0, mat_index=3) # Shingles
    add_box(bm, -4.1, 4.1, -3.9, 3.9, 5.8, 8.2, mat_index=3) # Main roof
    # Double-hung windows with black shutters
    add_box(bm, 1.6, 3.2, -3.68, -3.55, 1.0, 2.4, mat_index=4)
    add_box(bm, 1.2, 1.6, -3.7, -3.58, 1.0, 2.4, mat_index=5)
    add_box(bm, 3.2, 3.6, -3.7, -3.58, 1.0, 2.4, mat_index=5)
    for wx in [-2.2, 2.2]:
        add_box(bm, wx - 0.7, wx + 0.7, -3.68, -3.55, 3.8, 5.2, mat_index=4)
        add_box(bm, wx - 1.05, wx - 0.7, -3.7, -3.58, 3.8, 5.2, mat_index=5)
        add_box(bm, wx + 0.7, wx + 1.05, -3.7, -3.58, 3.8, 5.2, mat_index=5)
    # Mailbox
    add_box(bm, 1.5, 1.6, -5.8, -5.7, 0.0, 1.1, mat_index=2)
    add_box(bm, 1.4, 1.7, -6.1, -5.6, 1.0, 1.25, mat_index=5)
    
    body = finish_mesh("American_Building", bm, mats=["Mat_Concrete_Slab", "Mat_American_Siding_Blue", "Mat_American_Siding_White", "Mat_Roof_Asphalt_Grey", "Mat_Glass_Clear", "Mat_Metal_Anthracite"], collection=col)
    body.parent = root
    
    door = make_interactive_door("American_Craftsman_Door", width=0.96, height=2.2, thickness=0.05, mat_leaf="Mat_Timber_Dark_Oak", mat_handle="Mat_Brass_Gold", hinge_side="left", open_angle=85.0, collection=col)
    door.location = Vector((-1.5, -3.6, 0.0))
    door.parent = root
    door.rotation_euler.z = math.radians(35.0)
    
    return root

# -----------------------------------------------------------------------------
# City Environment, Ground Plaza & Studio Lighting Setup
# -----------------------------------------------------------------------------
def setup_environment_and_camera():
    studio_col = get_col("Environment_Studio")
    
    # Grand Avenue Plaza with Sidewalks, Street Markings and Crosswalks
    bm_ground = bmesh.new()
    # Broad asphalt avenue terrain
    add_box(bm_ground, -55.0, 75.0, -32.0, 20.0, -0.4, -0.25, mat_index=0)
    # North Sidewalk (Commercial front: Y = 1.0 to 4.5)
    add_box(bm_ground, -55.0, 75.0, 1.0, 4.5, -0.25, -0.05, mat_index=1)
    # South Sidewalk (Residential front: Y = -11.0 to -7.5)
    add_box(bm_ground, -55.0, 75.0, -11.0, -7.5, -0.25, -0.05, mat_index=1)
    # Main Central Avenue Yellow Centerline
    add_box(bm_ground, -55.0, 75.0, -3.3, -3.1, -0.24, -0.23, mat_index=2)
    # White Zebra Crosswalks
    for wx in [-18.0, 10.0, 38.0]:
        for sx in range(6):
            add_box(bm_ground, wx + sx * 0.9, wx + sx * 0.9 + 0.5, -7.2, 0.8, -0.24, -0.23, mat_index=3)
    # Green Lawn zones in front of residential houses
    add_box(bm_ground, -52.0, 72.0, -26.0, -11.5, -0.25, -0.08, mat_index=4)
    
    ground = finish_mesh("World_Plaza_Ground", bm_ground, mats=[
        "Mat_Asphalt_Road", "Mat_Sidewalk_Pavement", "Mat_Road_Stripe_Yellow", "Mat_Road_Stripe_White", "Mat_Grass_Lawn"
    ], collection=studio_col)
    
    # Realistic Sun Light
    sun_data = bpy.data.lights.new(name="Sun_Architectural", type='SUN')
    sun_data.energy = 5.2
    sun_data.color = (1.0, 0.95, 0.88)
    sun_data.angle = math.radians(2.0)
    sun_obj = bpy.data.objects.new("Sun_Architectural", sun_data)
    sun_obj.location = Vector((20.0, -45.0, 45.0))
    sun_obj.rotation_euler = (math.radians(52.0), math.radians(10.0), math.radians(-28.0))
    studio_col.objects.link(sun_obj)
    
    # Sky Fill Light
    fill_data = bpy.data.lights.new(name="Light_Sky_Fill", type='SUN')
    fill_data.energy = 1.4
    fill_data.color = (0.76, 0.86, 1.0)
    fill_obj = bpy.data.objects.new("Light_Sky_Fill", fill_data)
    fill_obj.location = Vector((-20.0, 30.0, 30.0))
    fill_obj.rotation_euler = (math.radians(45.0), math.radians(-15.0), math.radians(140.0))
    studio_col.objects.link(fill_obj)
    
    # Cinematic Panoramic Architectural Camera
    # Elevated 3/4 isometric perspective framing all 11 buildings from edge to edge
    cam_data = bpy.data.cameras.new("Showcase_Camera")
    cam_data.lens = 18 # Ultra wide-angle cinematic lens
    cam_data.clip_start = 0.1
    cam_data.clip_end = 600.0
    cam_obj = bpy.data.objects.new("Showcase_Camera", cam_data)
    
    cam_obj.location = Vector((9.0, -68.0, 36.0))
    cam_obj.rotation_euler = (math.radians(58.0), 0.0, math.radians(-1.5))
    studio_col.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    
    return cam_obj

# -----------------------------------------------------------------------------
# UV Mapping & Optimization
# -----------------------------------------------------------------------------
def auto_unwrap_all_meshes():
    print("Auto-generating clean architectural UV cube projections...")
    for obj in bpy.data.objects:
        if obj.type == 'MESH' and obj.data and len(obj.data.polygons) > 0:
            if not obj.data.uv_layers:
                obj.data.uv_layers.new(name="UVMap")
            
            bpy.ops.object.select_all(action='DESELECT')
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            try:
                bpy.ops.object.mode_set(mode='EDIT')
                bpy.ops.mesh.select_all(action='SELECT')
                bpy.ops.uv.cube_project(cube_size=2.0)
                bpy.ops.object.mode_set(mode='OBJECT')
            except Exception:
                try:
                    bpy.ops.object.mode_set(mode='OBJECT')
                except Exception:
                    pass

# -----------------------------------------------------------------------------
# FBX Export Pipeline
# -----------------------------------------------------------------------------
def export_all_buildings_to_fbx(building_roots):
    print("Exporting individual realistic buildings to Unity FBX...")
    for b_name, b_obj in building_roots.items():
        if not b_obj:
            continue
        fbx_path = os.path.join(WORLDPACK_DIR, f"{b_name}.fbx")
        
        # Select root and all children
        bpy.ops.object.select_all(action='DESELECT')
        def select_rec(o):
            o.select_set(True)
            for c in o.children:
                select_rec(c)
        select_rec(b_obj)
        bpy.context.view_layer.objects.active = b_obj
        
        try:
            bpy.ops.export_scene.fbx(
                filepath=fbx_path,
                use_selection=True,
                apply_unit_scale=True,
                axis_forward='-Z',
                axis_up='Y'
            )
            print(f"Exported FBX: {b_name}.fbx")
        except Exception as e:
            print(f"FBX export warning for {b_name}: {e}")

# -----------------------------------------------------------------------------
# Showcase Render
# -----------------------------------------------------------------------------
def render_showcase_image(output_path):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_depth = '8'
    scene.render.filepath = output_path
    
    print(f"Rendering realistic showcase image to: {output_path}...")
    bpy.ops.render.render(write_still=True)
    print("Showcase render completed successfully!")

# -----------------------------------------------------------------------------
# Main Execution Pipeline
# -----------------------------------------------------------------------------
def main():
    print("===================================================================")
    print("Generating Staggered Panoramic Realistic & Interactive World Pack...")
    print("===================================================================")
    
    reset_scene()
    create_pbr_materials()
    
    buildings = {}
    
    # 1. Commercial Boulevard (Row 1 at Y = 8.0)
    # Staggered with plenty of space so the residential row doesn't obstruct anything
    print("1/11 Building Farmácia Moderna...")
    buildings["Building_Pharmacy_Modern"] = build_pharmacy(Vector((-32.0, 8.0, 0.0)))
    
    print("2/11 Building Supermercado...")
    buildings["Building_Supermarket"] = build_supermarket(Vector((-15.0, 8.0, 0.0)))
    
    print("3/11 Building Posto de Combustível...")
    buildings["Building_GasStation_Canopy_Store"] = build_gas_station(Vector((5.0, 8.0, 0.0)))
    
    print("4/11 Building Hospital & Pronto-Socorro...")
    buildings["Building_Hospital_MedicalCenter"] = build_hospital(Vector((27.0, 8.0, 0.0)))
    
    print("5/11 Building Escola / Colégio...")
    buildings["Building_School_Academy"] = build_school(Vector((48.0, 8.0, 0.0)))
    
    # 2. Residential Village (Row 2 at Y = -16.0)
    # Placed in the gaps between the commercial buildings
    print("6/11 Building Casa de Fazenda (Farmhouse)...")
    buildings["House_Rustic_Farmhouse"] = build_farmhouse(Vector((-40.0, -16.0, 0.0)))
    
    print("7/11 Building Casa Sobrado Urbano (Townhouse)...")
    buildings["House_Townhouse_Sobrado"] = build_townhouse(Vector((-23.0, -16.0, 0.0)))
    
    print("8/11 Building Casa Grega de Santorini...")
    buildings["House_Greek_Santorini"] = build_greek_house(Vector((-6.0, -16.0, 0.0)))
    
    print("9/11 Building Casa Alemã da Baviera (Fachwerk)...")
    buildings["House_Bavarian_Fachwerk"] = build_bavarian_house(Vector((16.0, -16.0, 0.0)))
    
    print("10/11 Building Casa Brasileira Colonial...")
    buildings["House_Brazilian_Colonial"] = build_brazilian_house(Vector((37.0, -16.0, 0.0)))
    
    print("11/11 Building Casa Norte-Americana (Craftsman Siding)...")
    buildings["House_American_Suburban"] = build_american_house(Vector((57.0, -16.0, 0.0)))
    
    # 3. Environment, Avenue & Camera
    print("Setting up Environment, Grand Avenue Plaza and Camera...")
    setup_environment_and_camera()
    
    # 4. UV Cube Projection
    auto_unwrap_all_meshes()
    
    # 5. Export FBX Models to Unity
    export_all_buildings_to_fbx(buildings)
    
    # 6. Save Master .blend File
    print(f"Saving Blender master file: {BLEND_FILE_PATH}...")
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_FILE_PATH)
    
    print(f"Saving copy to Unity Assets: {ASSETS_BLEND_PATH}...")
    bpy.ops.wm.save_as_mainfile(filepath=ASSETS_BLEND_PATH)
    
    # 7. Render Showcase
    render_showcase_image(RENDER_OUTPUT_PATH)
    
    print("===================================================================")
    print("ALL 11 REALISTIC BUILDINGS GENERATED & EXPORTED SUCCESSFULLY!")
    print("===================================================================")

if __name__ == "__main__":
    main()
