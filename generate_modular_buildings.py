"""
Procedural Modular & Interactive Houses and Buildings Generator for Blender 5.2
Creates:
 1. Complete Modular Kit Palette (Floors, Walls, Doors, Windows, Stairs, Roofs, Balconies, HVAC)
 2. Assembled 2-Story Modular Residential House with Walkable Interiors, Stairs, Balcony & Interactive Doors
 3. Assembled 3-Story Modular Urban Commercial/Residential Building with Shopfront, Double Doors, Walkable Floors & Rooftop
 4. Interactive Elements with precise Hinge/Pivot placement, Custom Properties, and Animation Keyframes
 5. Procedural PBR Textures (Bricks, Stucco, Concrete, Wood Parquet, Marble, Terracotta Roof, Glass, Metals, Lighting)
 6. Studio Lighting, Ground Environment, and High-Resolution Showcase Camera & Render
 7. Exports models & .blend directly to Unity project structure
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix, Euler

# -----------------------------------------------------------------------------
# Configuration & Paths
# -----------------------------------------------------------------------------
PROJECT_DIR = r"E:\Backup-C\Projetos\testejogo"
BLEND_FILE_NAME = "Modular_Buildings_Interactive_Pack.blend"
BLEND_FILE_PATH = os.path.join(PROJECT_DIR, BLEND_FILE_NAME)
ASSETS_MODULAR_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "ModularBuildings")
ASSETS_BLEND_PATH = os.path.join(ASSETS_MODULAR_DIR, BLEND_FILE_NAME)
RENDER_OUTPUT_PATH = os.path.join(PROJECT_DIR, "modular_buildings_showcase.png")

os.makedirs(ASSETS_MODULAR_DIR, exist_ok=True)

# -----------------------------------------------------------------------------
# Scene Cleanup & Initialization
# -----------------------------------------------------------------------------
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = 'METERS'
    
    # Configure Color Management for rich architectural rendering
    scene.view_settings.view_transform = 'AgX' if 'AgX' in bpy.types.ColorManagedViewSettings.bl_rna.properties['view_transform'].enum_items else 'Filmic'
    scene.view_settings.look = 'Medium High Contrast'
    
    # World background
    world = bpy.data.worlds.new("Architectural_Sky")
    scene.world = world
    world.use_nodes = True
    wnodes = world.node_tree.nodes
    wnodes.clear()
    w_bg = wnodes.new('ShaderNodeBackground')
    w_bg.inputs['Color'].default_value = (0.75, 0.85, 0.95, 1.0)
    w_bg.inputs['Strength'].default_value = 0.85
    w_out = wnodes.new('ShaderNodeOutputWorld')
    world.node_tree.links.new(w_bg.outputs['Background'], w_out.inputs['Surface'])

# -----------------------------------------------------------------------------
# Collections Management
# -----------------------------------------------------------------------------
def get_or_create_collection(name, parent=None):
    col = bpy.data.collections.get(name)
    if not col:
        col = bpy.data.collections.new(name)
        if parent:
            parent.children.link(col)
        else:
            bpy.context.scene.collection.children.link(col)
    return col

# -----------------------------------------------------------------------------
# Material System (Procedural PBR Shaders)
# -----------------------------------------------------------------------------
MATERIALS = {}

def create_pbr_materials():
    global MATERIALS
    
    # Helper to set Principled BSDF inputs safely across Blender versions
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

    # 1. Brick Material (Procedural Bricks + Mortar + Bump)
    mat_brick = bpy.data.materials.new(name="Mat_Brick_Red")
    mat_brick.use_nodes = True
    nodes = mat_brick.node_tree.nodes
    links = mat_brick.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, roughness=0.72)
    
    tex_coord = nodes.new('ShaderNodeTexCoord')
    mapping = nodes.new('ShaderNodeMapping')
    mapping.inputs['Scale'].default_value = (4.0, 4.0, 4.0)
    
    tex_brick = nodes.new('ShaderNodeTexBrick')
    tex_brick.inputs['Color1'].default_value = (0.42, 0.14, 0.09, 1.0)
    tex_brick.inputs['Color2'].default_value = (0.56, 0.20, 0.13, 1.0)
    tex_brick.inputs['Mortar'].default_value = (0.76, 0.74, 0.71, 1.0)
    tex_brick.inputs['Scale'].default_value = 1.0
    tex_brick.inputs['Mortar Size'].default_value = 0.02
    tex_brick.inputs['Mortar Smooth'].default_value = 0.1
    tex_brick.inputs['Row Height'].default_value = 0.25
    tex_brick.inputs['Brick Width'].default_value = 0.5
    
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.4
    bump.inputs['Distance'].default_value = 0.02
    
    links.new(tex_coord.outputs['UV'], mapping.inputs['Vector'])
    links.new(mapping.outputs['Vector'], tex_brick.inputs['Vector'])
    links.new(tex_brick.outputs['Color'], bsdf.inputs['Base Color'])
    links.new(tex_brick.outputs['Fac'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    MATERIALS["Brick_Red"] = mat_brick

    # 2. Stucco Wall (Warm Beige Architectural Stucco)
    mat_stucco = bpy.data.materials.new(name="Mat_Wall_Stucco_Beige")
    mat_stucco.use_nodes = True
    nodes = mat_stucco.node_tree.nodes
    links = mat_stucco.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.94, 0.91, 0.86, 1.0), roughness=0.78)
    
    tex_coord = nodes.new('ShaderNodeTexCoord')
    noise = nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 60.0
    noise.inputs['Detail'].default_value = 4.0
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.08
    bump.inputs['Distance'].default_value = 0.01
    
    links.new(tex_coord.outputs['UV'], noise.inputs['Vector'])
    links.new(noise.outputs['Fac'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    MATERIALS["Wall_Stucco"] = mat_stucco

    # 3. Modern Concrete / Slate Wall (Contemporary Urban)
    mat_concrete_wall = bpy.data.materials.new(name="Mat_Wall_Modern_Concrete")
    mat_concrete_wall.use_nodes = True
    nodes = mat_concrete_wall.node_tree.nodes
    links = mat_concrete_wall.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.28, 0.30, 0.34, 1.0), roughness=0.52, metallic=0.08)
    MATERIALS["Wall_Concrete"] = mat_concrete_wall

    # 4. Terracotta Roof Tiles
    mat_roof = bpy.data.materials.new(name="Mat_Roof_Terracotta")
    mat_roof.use_nodes = True
    nodes = mat_roof.node_tree.nodes
    links = mat_roof.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.65, 0.22, 0.12, 1.0), roughness=0.68)
    
    tex_coord = nodes.new('ShaderNodeTexCoord')
    wave = nodes.new('ShaderNodeTexWave')
    wave.wave_type = 'BANDS'
    wave.inputs['Scale'].default_value = 8.0
    wave.inputs['Distortion'].default_value = 0.2
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.4
    bump.inputs['Distance'].default_value = 0.03
    
    links.new(tex_coord.outputs['UV'], wave.inputs['Vector'])
    links.new(wave.outputs['Color'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    MATERIALS["Roof_Terracotta"] = mat_roof

    # 5. Wood Parquet / Flooring (Honey Oak)
    mat_parquet = bpy.data.materials.new(name="Mat_Floor_WoodParquet")
    mat_parquet.use_nodes = True
    nodes = mat_parquet.node_tree.nodes
    links = mat_parquet.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.58, 0.36, 0.19, 1.0), roughness=0.28)
    
    tex_coord = nodes.new('ShaderNodeTexCoord')
    wave = nodes.new('ShaderNodeTexWave')
    wave.inputs['Scale'].default_value = 14.0
    wave.inputs['Distortion'].default_value = 1.2
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.12
    bump.inputs['Distance'].default_value = 0.01
    
    links.new(tex_coord.outputs['Object'], wave.inputs['Vector'])
    links.new(wave.outputs['Fac'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    MATERIALS["Floor_Parquet"] = mat_parquet

    # 6. Polished Marble / Commercial Tile
    mat_marble = bpy.data.materials.new(name="Mat_Floor_MarbleTiles")
    mat_marble.use_nodes = True
    nodes = mat_marble.node_tree.nodes
    links = mat_marble.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.88, 0.90, 0.92, 1.0), roughness=0.15)
    
    tex_coord = nodes.new('ShaderNodeTexCoord')
    tex_brick = nodes.new('ShaderNodeTexBrick')
    tex_brick.inputs['Color1'].default_value = (0.86, 0.88, 0.90, 1.0)
    tex_brick.inputs['Color2'].default_value = (0.76, 0.78, 0.82, 1.0)
    tex_brick.inputs['Mortar'].default_value = (0.35, 0.38, 0.42, 1.0)
    tex_brick.inputs['Scale'].default_value = 2.5
    tex_brick.inputs['Mortar Size'].default_value = 0.008
    tex_brick.inputs['Row Height'].default_value = 0.5
    
    links.new(tex_coord.outputs['Object'], tex_brick.inputs['Vector'])
    links.new(tex_brick.outputs['Color'], bsdf.inputs['Base Color'])
    MATERIALS["Floor_Marble"] = mat_marble

    # 7. Architectural Window Glass
    mat_glass = bpy.data.materials.new(name="Mat_Window_Glass")
    mat_glass.use_nodes = True
    nodes = mat_glass.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.82, 0.94, 0.98, 1.0), roughness=0.03, transmission=0.92, alpha=0.32, ior=1.52)
    mat_glass.blend_method = 'BLEND' if hasattr(mat_glass, 'blend_method') else None
    MATERIALS["Window_Glass"] = mat_glass

    # 8. Anthracite Metal (Frames, Railings, Trims)
    mat_metal = bpy.data.materials.new(name="Mat_Metal_Anthracite")
    mat_metal.use_nodes = True
    nodes = mat_metal.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.14, 0.15, 0.17, 1.0), metallic=0.92, roughness=0.28)
    MATERIALS["Metal_Anthracite"] = mat_metal

    # 9. Rich Walnut Wood (Residential Front Door)
    mat_door_wood = bpy.data.materials.new(name="Mat_Wood_FrontDoor")
    mat_door_wood.use_nodes = True
    nodes = mat_door_wood.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.32, 0.17, 0.10, 1.0), roughness=0.36)
    MATERIALS["Wood_FrontDoor"] = mat_door_wood

    # 10. Brass Hardware (Door Handles, Fixtures)
    mat_brass = bpy.data.materials.new(name="Mat_Brass_Gold")
    mat_brass.use_nodes = True
    nodes = mat_brass.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.95, 0.78, 0.36, 1.0), metallic=0.95, roughness=0.22)
    MATERIALS["Brass_Gold"] = mat_brass

    # 11. Concrete Foundation / Sidewalk Pavement
    mat_concrete = bpy.data.materials.new(name="Mat_Concrete_Foundation")
    mat_concrete.use_nodes = True
    nodes = mat_concrete.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.52, 0.54, 0.56, 1.0), roughness=0.82)
    MATERIALS["Concrete_Foundation"] = mat_concrete

    # 12. Warm Interior Ceiling Glow (Lighting Fixtures)
    mat_glow = bpy.data.materials.new(name="Mat_Interior_Ceiling_Glow")
    mat_glow.use_nodes = True
    nodes = mat_glow.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(1.0, 0.96, 0.88, 1.0), emission_color=(1.0, 0.92, 0.78, 1.0), emission_strength=4.0)
    MATERIALS["Interior_Glow"] = mat_glow

    # 13. Commercial Shopfront Header / Signage
    mat_sign = bpy.data.materials.new(name="Mat_Commercial_StoreSign")
    mat_sign.use_nodes = True
    nodes = mat_sign.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.08, 0.16, 0.24, 1.0), metallic=0.4, roughness=0.3)
    MATERIALS["Commercial_Sign"] = mat_sign

    # 14. White Drywall / Ceiling Interior
    mat_drywall = bpy.data.materials.new(name="Mat_Interior_Drywall")
    mat_drywall.use_nodes = True
    nodes = mat_drywall.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.96, 0.96, 0.95, 1.0), roughness=0.88)
    MATERIALS["Interior_Drywall"] = mat_drywall

    # 15. Flat Roof Waterproofing / Gravel
    mat_gravel = bpy.data.materials.new(name="Mat_Roof_Gravel")
    mat_gravel.use_nodes = True
    nodes = mat_gravel.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    set_bsdf(bsdf, base_color=(0.35, 0.36, 0.38, 1.0), roughness=0.92)
    MATERIALS["Roof_Gravel"] = mat_gravel

# -----------------------------------------------------------------------------
# Mesh Utility Helpers
# -----------------------------------------------------------------------------
def assign_mat(obj, mat_name):
    mat = MATERIALS.get(mat_name)
    if mat:
        if len(obj.data.materials) == 0:
            obj.data.materials.append(mat)
        else:
            obj.data.materials[0] = mat

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
    mesh = bpy.data.meshes.new(obj_name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    
    obj = bpy.data.objects.new(obj_name, mesh)
    if mats:
        for m_name in mats:
            if m_name in MATERIALS:
                obj.data.materials.append(MATERIALS[m_name])
    
    if collection:
        collection.objects.link(obj)
    else:
        bpy.context.scene.collection.objects.link(obj)
    return obj

# -----------------------------------------------------------------------------
# Modular Kit Piece Builders (Standard Grid: 4.0m W x 4.0m D x 3.2m H)
# -----------------------------------------------------------------------------

def build_foundation_slab(name="Mod_Foundation_4x4", collection=None):
    bm = bmesh.new()
    # 4m x 4m foundation, 0.25m thick
    add_box(bm, -2.0, 2.0, -2.0, 2.0, -0.25, 0.0, mat_index=0)
    return finish_mesh(name, bm, mats=["Concrete_Foundation"], collection=collection)

def build_floor_slab(name="Mod_Floor_Wood_4x4", top_mat="Floor_Parquet", collection=None):
    bm = bmesh.new()
    # Top finished surface
    add_box(bm, -2.0, 2.0, -2.0, 2.0, -0.05, 0.0, mat_index=0)
    # Underside ceiling (white drywall)
    add_box(bm, -2.0, 2.0, -2.0, 2.0, -0.20, -0.05, mat_index=1)
    return finish_mesh(name, bm, mats=[top_mat, "Interior_Drywall"], collection=collection)

def build_floor_stairwell(name="Mod_Floor_Stairwell_4x4", collection=None):
    bm = bmesh.new()
    # Floor slab with a 1.3m x 3.2m cutout for stairs
    # Cutout from x = -0.65 to 0.65, y = -1.6 to 1.6
    # Left wing
    add_box(bm, -2.0, -0.65, -2.0, 2.0, -0.20, 0.0, mat_index=0)
    # Right wing
    add_box(bm, 0.65, 2.0, -2.0, 2.0, -0.20, 0.0, mat_index=0)
    # Back connector
    add_box(bm, -0.65, 0.65, 1.6, 2.0, -0.20, 0.0, mat_index=0)
    # Front connector
    add_box(bm, -0.65, 0.65, -2.0, -1.6, -0.20, 0.0, mat_index=0)
    return finish_mesh(name, bm, mats=["Floor_Parquet", "Interior_Drywall"], collection=collection)

def build_wall_solid(name="Mod_Wall_Solid_4x3", ext_mat="Brick_Red", collection=None):
    bm = bmesh.new()
    # 4.0m wide, 3.2m high, 0.25m thick
    # Exterior half
    add_box(bm, -2.0, 2.0, -0.125, 0.0, 0.0, 3.2, mat_index=0)
    # Interior half
    add_box(bm, -2.0, 2.0, 0.0, 0.125, 0.0, 3.2, mat_index=1)
    # Interior baseboard
    add_box(bm, -2.0, 2.0, 0.125, 0.145, 0.0, 0.12, mat_index=2)
    return finish_mesh(name, bm, mats=[ext_mat, "Interior_Drywall", "Wood_FrontDoor"], collection=collection)

def build_wall_doorway(name="Mod_Wall_Doorway_4x3", ext_mat="Brick_Red", collection=None):
    bm = bmesh.new()
    # Door opening: 1.1m wide (-0.55 to 0.55), 2.2m high (0 to 2.2)
    # Left wall section
    add_box(bm, -2.0, -0.55, -0.125, 0.0, 0.0, 3.2, mat_index=0) # Ext
    add_box(bm, -2.0, -0.55, 0.0, 0.125, 0.0, 3.2, mat_index=1)  # Int
    # Right wall section
    add_box(bm, 0.55, 2.0, -0.125, 0.0, 0.0, 3.2, mat_index=0)   # Ext
    add_box(bm, 0.55, 2.0, 0.0, 0.125, 0.0, 3.2, mat_index=1)    # Int
    # Header above door (2.2m to 3.2m)
    add_box(bm, -0.55, 0.55, -0.125, 0.0, 2.2, 3.2, mat_index=0) # Ext
    add_box(bm, -0.55, 0.55, 0.0, 0.125, 2.2, 3.2, mat_index=1)  # Int
    # Door frame trim (jambs and lintel)
    add_box(bm, -0.55, -0.48, -0.14, 0.14, 0.0, 2.22, mat_index=2)
    add_box(bm, 0.48, 0.55, -0.14, 0.14, 0.0, 2.22, mat_index=2)
    add_box(bm, -0.55, 0.55, -0.14, 0.14, 2.15, 2.22, mat_index=2)
    # Door threshold/sill
    add_box(bm, -0.55, 0.55, -0.15, 0.15, 0.0, 0.03, mat_index=2)
    return finish_mesh(name, bm, mats=[ext_mat, "Interior_Drywall", "Metal_Anthracite"], collection=collection)

def build_wall_window_double(name="Mod_Wall_Window_Double_4x3", ext_mat="Brick_Red", collection=None):
    bm = bmesh.new()
    # Window opening: 1.8m wide (-0.9 to 0.9), sill at 0.9m, top at 2.4m (height 1.5m)
    # Left wall section
    add_box(bm, -2.0, -0.9, -0.125, 0.0, 0.0, 3.2, mat_index=0)
    add_box(bm, -2.0, -0.9, 0.0, 0.125, 0.0, 3.2, mat_index=1)
    # Right wall section
    add_box(bm, 0.9, 2.0, -0.125, 0.0, 0.0, 3.2, mat_index=0)
    add_box(bm, 0.9, 2.0, 0.0, 0.125, 0.0, 3.2, mat_index=1)
    # Bottom wall (0 to 0.9)
    add_box(bm, -0.9, 0.9, -0.125, 0.0, 0.0, 0.9, mat_index=0)
    add_box(bm, -0.9, 0.9, 0.0, 0.125, 0.0, 0.9, mat_index=1)
    # Top wall (2.4 to 3.2)
    add_box(bm, -0.9, 0.9, -0.125, 0.0, 2.4, 3.2, mat_index=0)
    add_box(bm, -0.9, 0.9, 0.0, 0.125, 2.4, 3.2, mat_index=1)
    
    # Exterior Sill (stone/concrete)
    add_box(bm, -0.95, 0.95, -0.18, -0.10, 0.86, 0.92, mat_index=2)
    # Outer frame
    add_box(bm, -0.9, -0.84, -0.06, 0.06, 0.9, 2.4, mat_index=3)
    add_box(bm, 0.84, 0.9, -0.06, 0.06, 0.9, 2.4, mat_index=3)
    add_box(bm, -0.9, 0.9, -0.06, 0.06, 2.34, 2.4, mat_index=3)
    add_box(bm, -0.9, 0.9, -0.06, 0.06, 0.9, 0.96, mat_index=3)
    # Central divider (mullion)
    add_box(bm, -0.03, 0.03, -0.05, 0.05, 0.9, 2.4, mat_index=3)
    # Glass panes
    add_box(bm, -0.84, -0.03, -0.01, 0.01, 0.96, 2.34, mat_index=4)
    add_box(bm, 0.03, 0.84, -0.01, 0.01, 0.96, 2.34, mat_index=4)
    
    return finish_mesh(name, bm, mats=[ext_mat, "Interior_Drywall", "Concrete_Foundation", "Metal_Anthracite", "Window_Glass"], collection=collection)

def build_wall_panoramic(name="Mod_Wall_Window_Panoramic_4x3", ext_mat="Wall_Concrete", collection=None):
    bm = bmesh.new()
    # Wide modern architectural window (2.8m wide, from -1.4 to 1.4, sill at 0.7m, top at 2.6m)
    add_box(bm, -2.0, -1.4, -0.125, 0.125, 0.0, 3.2, mat_index=0)
    add_box(bm, 1.4, 2.0, -0.125, 0.125, 0.0, 3.2, mat_index=0)
    add_box(bm, -1.4, 1.4, -0.125, 0.125, 0.0, 0.7, mat_index=0)
    add_box(bm, -1.4, 1.4, -0.125, 0.125, 2.6, 3.2, mat_index=0)
    
    # Modern metal frame
    add_box(bm, -1.4, 1.4, -0.06, 0.06, 0.7, 0.76, mat_index=1)
    add_box(bm, -1.4, 1.4, -0.06, 0.06, 2.54, 2.6, mat_index=1)
    add_box(bm, -1.4, -1.34, -0.06, 0.06, 0.7, 2.6, mat_index=1)
    add_box(bm, 1.34, 1.4, -0.06, 0.06, 0.7, 2.6, mat_index=1)
    # Vertical mullions
    add_box(bm, -0.48, -0.44, -0.05, 0.05, 0.76, 2.54, mat_index=1)
    add_box(bm, 0.44, 0.48, -0.05, 0.05, 0.76, 2.54, mat_index=1)
    # Glass panes
    add_box(bm, -1.34, 1.34, -0.01, 0.01, 0.76, 2.54, mat_index=2)
    return finish_mesh(name, bm, mats=[ext_mat, "Metal_Anthracite", "Window_Glass"], collection=collection)

def build_wall_shopfront(name="Mod_Wall_ShopFront_4x3", collection=None):
    bm = bmesh.new()
    # Commercial ground floor full-height glass shopfront
    # Two side structural pillars
    add_box(bm, -2.0, -1.7, -0.15, 0.15, 0.0, 3.2, mat_index=0) # Concrete pillar
    add_box(bm, 1.7, 2.0, -0.15, 0.15, 0.0, 3.2, mat_index=0)  # Concrete pillar
    # Header fascia (signage band at top: 2.5m to 3.2m)
    add_box(bm, -1.7, 1.7, -0.16, 0.16, 2.5, 3.2, mat_index=1)  # Signage
    # Kickplate at bottom (0.0 to 0.25m)
    add_box(bm, -1.7, 1.7, -0.08, 0.08, 0.0, 0.25, mat_index=2) # Metal
    # Central entrance doorway frame (1.8m wide, from -0.9 to 0.9, height 2.25m)
    # Left display window frame & glass (from -1.7 to -0.9)
    add_box(bm, -1.7, -0.9, -0.06, 0.06, 2.25, 2.5, mat_index=2) # Transom header
    add_box(bm, -0.95, -0.90, -0.06, 0.06, 0.25, 2.5, mat_index=2) # Door jamb
    add_box(bm, -1.68, -0.95, -0.01, 0.01, 0.25, 2.25, mat_index=3) # Left Glass
    # Right display window frame & glass (from 0.9 to 1.7)
    add_box(bm, 0.9, 1.7, -0.06, 0.06, 2.25, 2.5, mat_index=2)
    add_box(bm, 0.90, 0.95, -0.06, 0.06, 0.25, 2.5, mat_index=2)
    add_box(bm, 0.95, 1.68, -0.01, 0.01, 0.25, 2.25, mat_index=3) # Right Glass
    # Transom glass above doors (from -0.9 to 0.9, 2.25 to 2.5)
    add_box(bm, -0.9, 0.9, -0.06, 0.06, 2.22, 2.28, mat_index=2)
    add_box(bm, -0.88, 0.88, -0.01, 0.01, 2.28, 2.48, mat_index=3)
    return finish_mesh(name, bm, mats=["Wall_Concrete", "Commercial_Sign", "Metal_Anthracite", "Window_Glass"], collection=collection)

def build_staircase(name="Mod_Staircase_Straight_4x3", collection=None):
    bm = bmesh.new()
    # 16 steps: total height 3.2m (rise = 0.20m per step), depth 3.2m (run = 0.20m per step), width 1.2m
    num_steps = 16
    step_w = 1.2
    step_run = 0.20
    step_rise = 0.20
    
    for i in range(num_steps):
        z_bot = 0.0
        z_top = (i + 1) * step_rise
        y_front = -1.6 + i * step_run
        y_back = -1.6 + (i + 1) * step_run
        # Step block
        add_box(bm, -step_w/2, step_w/2, y_front, y_back, z_bot, z_top, mat_index=0)
        # Step nose / tread highlight
        add_box(bm, -step_w/2 - 0.02, step_w/2 + 0.02, y_front - 0.02, y_back, z_top - 0.03, z_top, mat_index=1)
    
    # Handrail / Balustrade on right side
    # Posts
    for p in [0, 5, 10, 15]:
        py = -1.6 + p * step_run + 0.1
        pz = (p + 1) * step_rise
        add_box(bm, step_w/2 - 0.04, step_w/2, py - 0.02, py + 0.02, pz, pz + 0.9, mat_index=2)
    # Sloped handrail bar
    add_box(bm, step_w/2 - 0.05, step_w/2 + 0.01, -1.6, -1.6 + 16 * step_run, 0.9, 0.95 + 16 * step_rise, mat_index=1)
    return finish_mesh(name, bm, mats=["Floor_Parquet", "Wood_FrontDoor", "Metal_Anthracite"], collection=collection)

def build_roof_gable(name="Mod_Roof_Gable_4x4", collection=None):
    bm = bmesh.new()
    # Sloped roof (width 4.4m with overhang, depth 4.4m, apex height 1.8m above base)
    w = 2.2
    d = 2.2
    h = 1.8
    # Left slope
    verts_l = [
        bm.verts.new((-w, -d, 0.0)),
        bm.verts.new((0.0, -d, h)),
        bm.verts.new((0.0, d, h)),
        bm.verts.new((-w, d, 0.0)),
        bm.verts.new((-w, -d, 0.12)),
        bm.verts.new((0.0, -d, h + 0.12)),
        bm.verts.new((0.0, d, h + 0.12)),
        bm.verts.new((-w, d, 0.12)),
    ]
    # Right slope
    verts_r = [
        bm.verts.new((0.0, -d, h)),
        bm.verts.new((w, -d, 0.0)),
        bm.verts.new((w, d, 0.0)),
        bm.verts.new((0.0, d, h)),
        bm.verts.new((0.0, -d, h + 0.12)),
        bm.verts.new((w, -d, 0.12)),
        bm.verts.new((w, d, 0.12)),
        bm.verts.new((0.0, d, h + 0.12)),
    ]
    # Faces for left slope
    f_l = [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (3, 2, 6, 7), (0, 3, 7, 4), (1, 5, 6, 2)]
    for f in f_l:
        face = bm.faces.new([verts_l[i] for i in f])
        face.material_index = 0
    # Faces for right slope
    f_r = [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (3, 2, 6, 7), (0, 3, 7, 4), (1, 5, 6, 2)]
    for f in f_r:
        face = bm.faces.new([verts_r[i] for i in f])
        face.material_index = 0
        
    # Ridge cap at apex
    add_box(bm, -0.15, 0.15, -d, d, h + 0.08, h + 0.18, mat_index=0)
    # Triangular gable fascia ends
    # Front triangle
    f_v = [bm.verts.new((-2.0, -2.0, 0.0)), bm.verts.new((2.0, -2.0, 0.0)), bm.verts.new((0.0, -2.0, h))]
    bm.faces.new(f_v).material_index = 1
    # Back triangle
    b_v = [bm.verts.new((2.0, 2.0, 0.0)), bm.verts.new((-2.0, 2.0, 0.0)), bm.verts.new((0.0, 2.0, h))]
    bm.faces.new(b_v).material_index = 1

    return finish_mesh(name, bm, mats=["Roof_Terracotta", "Wall_Stucco"], collection=collection)

def build_roof_flat_urban(name="Mod_Roof_Flat_4x4", collection=None):
    bm = bmesh.new()
    # Flat roof slab (4.0m x 4.0m, depth 0.2m)
    add_box(bm, -2.0, 2.0, -2.0, 2.0, -0.2, 0.0, mat_index=0)
    # Perimeter parapet walls (0.8m high, 0.2m thick)
    add_box(bm, -2.0, 2.0, -2.0, -1.8, 0.0, 0.8, mat_index=1) # Front
    add_box(bm, -2.0, 2.0, 1.8, 2.0, 0.0, 0.8, mat_index=1)  # Back
    add_box(bm, -2.0, -1.8, -1.8, 1.8, 0.0, 0.8, mat_index=1) # Left
    add_box(bm, 1.8, 2.0, -1.8, 1.8, 0.0, 0.8, mat_index=1)  # Right
    # Parapet stone coping cap
    add_box(bm, -2.05, 2.05, -2.05, -1.75, 0.8, 0.88, mat_index=2)
    add_box(bm, -2.05, 2.05, 1.75, 2.05, 0.8, 0.88, mat_index=2)
    add_box(bm, -2.05, -1.75, -1.75, 1.75, 0.8, 0.88, mat_index=2)
    add_box(bm, 1.75, 2.05, -1.75, 1.75, 0.8, 0.88, mat_index=2)
    return finish_mesh(name, bm, mats=["Roof_Gravel", "Wall_Concrete", "Concrete_Foundation"], collection=collection)

def build_hvac_unit(name="Mod_Roof_HVAC_Chiller", collection=None):
    bm = bmesh.new()
    # Industrial rooftop air conditioning unit (1.6m x 1.2m x 1.0m)
    add_box(bm, -0.8, 0.8, -0.6, 0.6, 0.0, 1.0, mat_index=0)
    # Support mounting rails
    add_box(bm, -0.75, 0.75, -0.55, -0.45, -0.1, 0.0, mat_index=1)
    add_box(bm, -0.75, 0.75, 0.45, 0.55, -0.1, 0.0, mat_index=1)
    # Exhaust fan grilles on top
    add_box(bm, -0.55, -0.05, -0.4, 0.1, 1.0, 1.08, mat_index=1)
    add_box(bm, 0.05, 0.55, -0.4, 0.1, 1.0, 1.08, mat_index=1)
    # Side ventilation louvers
    add_box(bm, -0.82, -0.80, -0.5, 0.5, 0.2, 0.8, mat_index=1)
    add_box(bm, 0.80, 0.82, -0.5, 0.5, 0.2, 0.8, mat_index=1)
    return finish_mesh(name, bm, mats=["Metal_Anthracite", "Concrete_Foundation"], collection=collection)

def build_balcony_railing(name="Mod_Balcony_GlassRailing_4m", collection=None):
    bm = bmesh.new()
    # 4.0m railing with dark metal posts and tempered safety glass
    # Bottom anchor channel
    add_box(bm, -2.0, 2.0, -0.04, 0.04, 0.0, 0.08, mat_index=0)
    # Top handrail
    add_box(bm, -2.0, 2.0, -0.04, 0.04, 0.95, 1.02, mat_index=0)
    # Vertical posts
    for x in [-2.0, -1.0, 0.0, 1.0, 2.0]:
        add_box(bm, x - 0.03, x + 0.03, -0.03, 0.03, 0.0, 1.0, mat_index=0)
    # Glass infill panels
    for x_c in [-1.5, -0.5, 0.5, 1.5]:
        add_box(bm, x_c - 0.45, x_c + 0.45, -0.01, 0.01, 0.10, 0.92, mat_index=1)
    return finish_mesh(name, bm, mats=["Metal_Anthracite", "Window_Glass"], collection=collection)

def build_corner_pillar(name="Mod_Pillar_Corner_3m", collection=None):
    bm = bmesh.new()
    # 0.4m x 0.4m structural pillar, 3.2m tall
    add_box(bm, -0.2, 0.2, -0.2, 0.2, 0.0, 3.2, mat_index=0)
    # Base plinth and capital trim
    add_box(bm, -0.25, 0.25, -0.25, 0.25, 0.0, 0.2, mat_index=1)
    add_box(bm, -0.25, 0.25, -0.25, 0.25, 3.0, 3.2, mat_index=1)
    return finish_mesh(name, bm, mats=["Wall_Concrete", "Metal_Anthracite"], collection=collection)

def build_chimney(name="Mod_Chimney_Brick", collection=None):
    bm = bmesh.new()
    # Brick chimney shaft (0.6m x 0.6m, 2.2m tall)
    add_box(bm, -0.3, 0.3, -0.3, 0.3, 0.0, 2.0, mat_index=0)
    # Overhanging concrete cap
    add_box(bm, -0.38, 0.38, -0.38, 0.38, 2.0, 2.12, mat_index=1)
    # Ceramic flue pot
    add_box(bm, -0.15, 0.15, -0.15, 0.15, 2.12, 2.45, mat_index=2)
    return finish_mesh(name, bm, mats=["Brick_Red", "Concrete_Foundation", "Roof_Terracotta"], collection=collection)

# -----------------------------------------------------------------------------
# Interactive Elements Builders (Careful Pivot Placement at Hinge!)
# -----------------------------------------------------------------------------

def build_interactive_door_residential(name="Mod_Interactive_Door_Residential", collection=None):
    """
    Wooden residential front door with brass handle.
    IMPORTANT: The object origin (pivot) is placed at (0, 0, 0) - EXACTLY on the hinge!
    Door spans from x = 0 to x = 0.96m, y = -0.025 to 0.025m, z = 0 to 2.12m.
    Rotating door.rotation_euler.z opens the door realistically!
    """
    bm = bmesh.new()
    w = 0.96
    h = 2.12
    t = 0.045
    
    # Door main wooden leaf
    add_box(bm, 0.0, w, -t/2, t/2, 0.0, h, mat_index=0)
    
    # Decorative classical recessed panels (Top and Bottom panels)
    # Lower recessed panels
    add_box(bm, 0.12, 0.44, -t/2 - 0.008, t/2 + 0.008, 0.15, 0.85, mat_index=0)
    add_box(bm, 0.52, 0.84, -t/2 - 0.008, t/2 + 0.008, 0.15, 0.85, mat_index=0)
    # Upper recessed panels
    add_box(bm, 0.12, 0.44, -t/2 - 0.008, t/2 + 0.008, 1.05, 1.95, mat_index=0)
    add_box(bm, 0.52, 0.84, -t/2 - 0.008, t/2 + 0.008, 1.05, 1.95, mat_index=0)
    
    # Brass door handle (Lever handle + escutcheon plate) placed at x = 0.84, z = 1.02
    hx = 0.84
    hz = 1.02
    # Front escutcheon plate
    add_box(bm, hx - 0.03, hx + 0.03, t/2, t/2 + 0.015, hz - 0.10, hz + 0.08, mat_index=1)
    # Front handle lever
    add_box(bm, hx - 0.12, hx + 0.02, t/2 + 0.015, t/2 + 0.05, hz, hz + 0.03, mat_index=1)
    # Back escutcheon plate
    add_box(bm, hx - 0.03, hx + 0.03, -t/2 - 0.015, -t/2, hz - 0.10, hz + 0.08, mat_index=1)
    # Back handle lever
    add_box(bm, hx - 0.12, hx + 0.02, -t/2 - 0.05, -t/2 - 0.015, hz, hz + 0.03, mat_index=1)
    
    # Brass Hinges (at x = 0, z = 0.3 and z = 1.8)
    for hz_h in [0.30, 1.80]:
        add_box(bm, -0.015, 0.015, -0.02, 0.02, hz_h - 0.06, hz_h + 0.06, mat_index=1)
        
    obj = finish_mesh(name, bm, mats=["Wood_FrontDoor", "Brass_Gold"], collection=collection)
    
    # Custom properties for game interaction
    obj["is_interactive"] = True
    obj["interact_type"] = "HingeDoor"
    obj["open_angle_deg"] = 85.0
    obj["interact_prompt"] = "[E] Abrir / Fechar Porta"
    obj["state"] = "Closed"
    
    # Add Animation Action for demonstration
    anim_data = obj.animation_data_create()
    action = bpy.data.actions.new(name=f"{name}_OpenClose")
    obj.animation_data.action = action
    
    obj.rotation_euler = (0, 0, 0)
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=1)
    obj.rotation_euler = (0, 0, math.radians(85.0))
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=35)
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=65)
    obj.rotation_euler = (0, 0, 0)
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=95)
    
    return obj

def build_interactive_door_commercial(name="Mod_Interactive_Door_Commercial_L", hinge_side="left", collection=None):
    """
    Modern tempered glass & dark metal commercial entrance door.
    Hinged on the outer side for push/pull opening.
    """
    bm = bmesh.new()
    w = 0.86
    h = 2.20
    t = 0.05
    
    # If hinge_side == 'left', spans from 0 to w; if 'right', spans from -w to 0
    x_min = 0.0 if hinge_side == "left" else -w
    x_max = w if hinge_side == "left" else 0.0
    
    # Metal perimeter frame
    add_box(bm, x_min, x_max, -t/2, t/2, 0.0, 0.08, mat_index=0) # Bottom rail
    add_box(bm, x_min, x_max, -t/2, t/2, h - 0.08, h, mat_index=0) # Top rail
    add_box(bm, x_min, x_min + 0.07, -t/2, t/2, 0.08, h - 0.08, mat_index=0) # Left stile
    add_box(bm, x_max - 0.07, x_max, -t/2, t/2, 0.08, h - 0.08, mat_index=0) # Right stile
    # Large tempered glass pane
    add_box(bm, x_min + 0.07, x_max - 0.07, -0.008, 0.008, 0.08, h - 0.08, mat_index=1)
    
    # Modern stainless steel vertical pull handle bar
    hx = (x_max - 0.12) if hinge_side == "left" else (x_min + 0.12)
    for y_sign in [-1, 1]:
        y_pos = y_sign * (t/2 + 0.04)
        add_box(bm, hx - 0.02, hx + 0.02, y_pos - 0.015, y_pos + 0.015, 0.7, 1.4, mat_index=2)
        # Handle standoffs
        add_box(bm, hx - 0.015, hx + 0.015, min(y_sign*t/2, y_pos), max(y_sign*t/2, y_pos), 0.75, 0.78, mat_index=2)
        add_box(bm, hx - 0.015, hx + 0.015, min(y_sign*t/2, y_pos), max(y_sign*t/2, y_pos), 1.32, 1.35, mat_index=2)
        
    obj = finish_mesh(name, bm, mats=["Metal_Anthracite", "Window_Glass", "Brass_Gold"], collection=collection)
    
    open_angle = 80.0 if hinge_side == "left" else -80.0
    obj["is_interactive"] = True
    obj["interact_type"] = "CommercialDoor"
    obj["open_angle_deg"] = open_angle
    obj["interact_prompt"] = "[E] Empurrar / Fechar Porta"
    obj["state"] = "Closed"
    
    anim_data = obj.animation_data_create()
    action = bpy.data.actions.new(name=f"{name}_OpenClose")
    obj.animation_data.action = action
    
    obj.rotation_euler = (0, 0, 0)
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=1)
    obj.rotation_euler = (0, 0, math.radians(open_angle))
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=35)
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=65)
    obj.rotation_euler = (0, 0, 0)
    obj.keyframe_insert(data_path="rotation_euler", index=2, frame=95)
    
    return obj

def build_interactive_window_sash(name="Mod_Interactive_Window_Sash", collection=None):
    """
    Openable window sash leaf with hinge at (0, 0, 0).
    Spans from x = 0 to 0.78m, z = 0 to 1.36m.
    """
    bm = bmesh.new()
    w = 0.78
    h = 1.36
    t = 0.04
    
    # Sash perimeter frame
    add_box(bm, 0.0, w, -t/2, t/2, 0.0, 0.06, mat_index=0)
    add_box(bm, 0.0, w, -t/2, t/2, h - 0.06, h, mat_index=0)
    add_box(bm, 0.0, 0.06, -t/2, t/2, 0.06, h - 0.06, mat_index=0)
    add_box(bm, w - 0.06, w, -t/2, t/2, 0.06, h - 0.06, mat_index=0)
    # Glass pane
    add_box(bm, 0.06, w - 0.06, -0.006, 0.006, 0.06, h - 0.06, mat_index=1)
    # Window latch handle at x = w - 0.05, z = h/2
    add_box(bm, w - 0.06, w - 0.02, t/2, t/2 + 0.04, h/2 - 0.04, h/2 + 0.04, mat_index=2)
    
    obj = finish_mesh(name, bm, mats=["Metal_Anthracite", "Window_Glass", "Brass_Gold"], collection=collection)
    obj["is_interactive"] = True
    obj["interact_type"] = "HingeWindow"
    obj["open_angle_deg"] = -65.0
    obj["interact_prompt"] = "[E] Abrir / Fechar Janela"
    return obj

# -----------------------------------------------------------------------------
# Modular Assembly 1: Casa Modular Residencial (2 Andares)
# -----------------------------------------------------------------------------
def assemble_residential_house(base_loc=Vector((0, 0, 0))):
    house_col = get_or_create_collection("Casa_Modular_Residencial_2Andares")
    
    # Root empty for whole house
    root = bpy.data.objects.new("Casa_Residencial_Root", None)
    root.location = base_loc
    house_col.objects.link(root)
    
    # 1. Foundation (8m x 8m, from x=-4 to 4, y=-4 to 4)
    bm = bmesh.new()
    add_box(bm, -4.2, 4.2, -4.2, 4.2, -0.3, 0.0, mat_index=0)
    # Front porch platform & steps
    add_box(bm, -2.5, 0.5, -5.4, -4.2, -0.15, 0.0, mat_index=0)
    add_box(bm, -2.7, 0.7, -6.0, -5.4, -0.3, -0.15, mat_index=0)
    foundation = finish_mesh("House_Foundation", bm, mats=["Concrete_Foundation"], collection=house_col)
    foundation.parent = root
    
    # 2. Ground Floor Interior Wood Parquet
    bm = bmesh.new()
    add_box(bm, -3.85, 3.85, -3.85, 3.85, -0.02, 0.0, mat_index=0)
    floor_gnd = finish_mesh("House_Ground_Parquet", bm, mats=["Floor_Parquet"], collection=house_col)
    floor_gnd.parent = root
    
    # 3. Ground Floor Walls (Z = 0.0 to 3.2)
    # Front Wall (Y = -4.0, from X = -4.0 to 4.0):
    # Left side (Entrance doorway): Door opening from X = -1.6 to -0.6 (width 1.0m, height 2.2m)
    bm = bmesh.new()
    # Left of door
    add_box(bm, -4.0, -1.6, -4.125, -3.875, 0.0, 3.2, mat_index=0)
    # Right of door (up to center x=0)
    add_box(bm, -0.6, 0.0, -4.125, -3.875, 0.0, 3.2, mat_index=0)
    # Lintel above door
    add_box(bm, -1.6, -0.6, -4.125, -3.875, 2.2, 3.2, mat_index=0)
    # Door frame jambs
    add_box(bm, -1.62, -1.58, -4.14, -3.86, 0.0, 2.22, mat_index=1)
    add_box(bm, -0.62, -0.58, -4.14, -3.86, 0.0, 2.22, mat_index=1)
    add_box(bm, -1.62, -0.58, -4.14, -3.86, 2.18, 2.22, mat_index=1)
    
    # Right side: Living room double window (from X = 0.8 to 2.8, sill 0.9m to 2.3m)
    add_box(bm, 0.0, 0.8, -4.125, -3.875, 0.0, 3.2, mat_index=0)
    add_box(bm, 2.8, 4.0, -4.125, -3.875, 0.0, 3.2, mat_index=0)
    add_box(bm, 0.8, 2.8, -4.125, -3.875, 0.0, 0.9, mat_index=0)
    add_box(bm, 0.8, 2.8, -4.125, -3.875, 2.3, 3.2, mat_index=0)
    # Window frame & glass
    add_box(bm, 0.8, 2.8, -4.04, -3.96, 0.9, 0.96, mat_index=1)
    add_box(bm, 0.8, 2.8, -4.04, -3.96, 2.24, 2.30, mat_index=1)
    add_box(bm, 0.8, 0.86, -4.04, -3.96, 0.9, 2.3, mat_index=1)
    add_box(bm, 2.74, 2.8, -4.04, -3.96, 0.9, 2.3, mat_index=1)
    add_box(bm, 1.77, 1.83, -4.04, -3.96, 0.9, 2.3, mat_index=1)
    add_box(bm, 0.86, 2.74, -4.01, -3.99, 0.96, 2.24, mat_index=2) # Glass
    wall_front_gnd = finish_mesh("House_Wall_Front_Gnd", bm, mats=["Brick_Red", "Metal_Anthracite", "Window_Glass"], collection=house_col)
    wall_front_gnd.parent = root
    
    # Interactive Front Door (Pivoting at hinge X = -1.6, Y = -4.0, Z = 0.0)
    door_front = build_interactive_door_residential("House_Interactive_FrontDoor", collection=house_col)
    door_front.location = Vector((-1.58, -4.0, 0.0))
    door_front.parent = root
    # Slightly open (25 degrees) to immediately show interactivity in 3D viewport!
    door_front.rotation_euler.z = math.radians(25.0)
    
    # Back Wall (Y = 4.0, from X = -4.0 to 4.0) with large dining window
    bm = bmesh.new()
    add_box(bm, -4.0, -1.5, 3.875, 4.125, 0.0, 3.2, mat_index=0)
    add_box(bm, 1.5, 4.0, 3.875, 4.125, 0.0, 3.2, mat_index=0)
    add_box(bm, -1.5, 1.5, 3.875, 4.125, 0.0, 0.9, mat_index=0)
    add_box(bm, -1.5, 1.5, 3.875, 4.125, 2.4, 3.2, mat_index=0)
    add_box(bm, -1.5, 1.5, 3.96, 4.04, 0.9, 2.4, mat_index=1)
    add_box(bm, -1.45, 1.45, 3.99, 4.01, 0.95, 2.35, mat_index=2)
    wall_back_gnd = finish_mesh("House_Wall_Back_Gnd", bm, mats=["Brick_Red", "Metal_Anthracite", "Window_Glass"], collection=house_col)
    wall_back_gnd.parent = root
    
    # Left & Right Solid Brick Walls (X = -4.0 and X = 4.0)
    bm = bmesh.new()
    add_box(bm, -4.125, -3.875, -4.0, 4.0, 0.0, 3.2, mat_index=0)
    add_box(bm, 3.875, 4.125, -4.0, 4.0, 0.0, 3.2, mat_index=0)
    walls_side_gnd = finish_mesh("House_Walls_Side_Gnd", bm, mats=["Brick_Red"], collection=house_col)
    walls_side_gnd.parent = root
    
    # Interior Walkable Staircase (Rising from Y = -1.0 to 2.2, at X = -3.2 to -2.0, Z = 0 to 3.2)
    staircase = build_staircase("House_Interior_Stairs", collection=house_col)
    staircase.location = Vector((-2.6, 0.6, 0.0))
    staircase.parent = root
    
    # Interior Ceiling Light (Glowing fixture)
    bm = bmesh.new()
    add_box(bm, 1.5, 2.1, -1.5, -0.9, 3.05, 3.12, mat_index=0)
    light_fixture = finish_mesh("House_Interior_Lamp_Gnd", bm, mats=["Interior_Glow"], collection=house_col)
    light_fixture.parent = root
    
    # 4. Second Floor Slab with Stair Cutout (Z = 3.2)
    # Cutout around X = -3.5 to -1.7, Y = -1.2 to 2.4 so player can walk up!
    bm = bmesh.new()
    # Main floor area
    add_box(bm, -1.7, 4.0, -4.0, 4.0, 3.0, 3.2, mat_index=0)
    # Areas around cutout
    add_box(bm, -4.0, -1.7, -4.0, -1.2, 3.0, 3.2, mat_index=0)
    add_box(bm, -4.0, -1.7, 2.4, 4.0, 3.0, 3.2, mat_index=0)
    # Stairwell guardrail on 2nd floor
    add_box(bm, -1.75, -1.65, -1.2, 2.4, 3.2, 4.1, mat_index=1)
    add_box(bm, -4.0, -1.65, -1.25, -1.15, 3.2, 4.1, mat_index=1)
    floor_second = finish_mesh("House_Floor_Second", bm, mats=["Floor_Parquet", "Metal_Anthracite"], collection=house_col)
    floor_second.parent = root
    
    # 5. Second Floor Walls (Z = 3.2 to 6.4) - Elegant Warm Stucco Exterior
    # Front Wall features a Walkout Balcony on right side (X = 0.5 to 3.8, Y = -4.0 to -2.2)
    bm = bmesh.new()
    # Left bedroom wall with double window (X = -4.0 to 0.0)
    add_box(bm, -4.0, -2.5, -4.125, -3.875, 3.2, 6.4, mat_index=0)
    add_box(bm, -0.8, 0.0, -4.125, -3.875, 3.2, 6.4, mat_index=0)
    add_box(bm, -2.5, -0.8, -4.125, -3.875, 3.2, 4.1, mat_index=0)
    add_box(bm, -2.5, -0.8, -4.125, -3.875, 5.5, 6.4, mat_index=0)
    add_box(bm, -2.5, -0.8, -4.04, -3.96, 4.1, 5.5, mat_index=1) # Frame
    add_box(bm, -2.45, -0.85, -4.01, -3.99, 4.15, 5.45, mat_index=2) # Glass
    
    # Recessed Balcony Wall (at Y = -2.2, from X = 0.0 to 4.0)
    # Doorway to balcony from X = 1.0 to 2.0, height 3.2 to 5.4 (2.2m door)
    add_box(bm, 0.0, 1.0, -2.325, -2.075, 3.2, 6.4, mat_index=0)
    add_box(bm, 2.0, 4.0, -2.325, -2.075, 3.2, 6.4, mat_index=0)
    add_box(bm, 1.0, 2.0, -2.325, -2.075, 5.4, 6.4, mat_index=0)
    # Balcony door frame
    add_box(bm, 0.98, 2.02, -2.25, -2.15, 3.2, 5.42, mat_index=1)
    
    # Balcony sidewalls
    add_box(bm, -0.125, 0.125, -4.0, -2.2, 3.2, 6.4, mat_index=0)
    add_box(bm, 3.875, 4.125, -4.0, -2.2, 3.2, 6.4, mat_index=0)
    
    # Back and Side 2nd Floor Walls
    add_box(bm, -4.125, -3.875, -4.0, 4.0, 3.2, 6.4, mat_index=0) # Left
    add_box(bm, 3.875, 4.125, -2.2, 4.0, 3.2, 6.4, mat_index=0)  # Right
    add_box(bm, -4.0, 4.0, 3.875, 4.125, 3.2, 6.4, mat_index=0)    # Back
    
    wall_second = finish_mesh("House_Walls_Second", bm, mats=["Wall_Stucco", "Metal_Anthracite", "Window_Glass"], collection=house_col)
    wall_second.parent = root
    
    # Interactive Balcony Door (Hinged at X = 1.0, Y = -2.2, Z = 3.2)
    door_balcony = build_interactive_door_residential("House_Interactive_BalconyDoor", collection=house_col)
    door_balcony.location = Vector((1.02, -2.2, 3.2))
    door_balcony.parent = root
    door_balcony.rotation_euler.z = math.radians(45.0) # Partially open
    
    # Balcony Tempered Glass Railing (at Y = -4.0, from X = 0.0 to 4.0)
    bm = bmesh.new()
    add_box(bm, 0.0, 4.0, -4.04, -3.96, 3.2, 3.28, mat_index=0) # Base rail
    add_box(bm, 0.0, 4.0, -4.04, -3.96, 4.15, 4.22, mat_index=0) # Handrail
    for bx in [0.0, 1.33, 2.66, 4.0]:
        add_box(bm, bx - 0.03, bx + 0.03, -4.03, -3.97, 3.2, 4.2, mat_index=0)
    add_box(bm, 0.05, 3.95, -4.01, -3.99, 3.3, 4.12, mat_index=1) # Glass
    balcony_railing = finish_mesh("House_Balcony_Railing", bm, mats=["Metal_Anthracite", "Window_Glass"], collection=house_col)
    balcony_railing.parent = root
    
    # 6. Residential Roof (Z = 6.4 to 8.6) - Mediterranean Terracotta Sloped Gable
    roof_gable = build_roof_gable("House_Roof_Terracotta", collection=house_col)
    roof_gable.location = Vector((0.0, 0.0, 6.4))
    roof_gable.scale = Vector((2.05, 2.05, 1.25))
    roof_gable.parent = root
    
    # Brick Chimney
    chimney = build_chimney("House_Chimney", collection=house_col)
    chimney.location = Vector((-2.2, 1.8, 6.8))
    chimney.parent = root
    
    return root

# -----------------------------------------------------------------------------
# Modular Assembly 2: Prédio Modular Urbano Comercial & Residencial (3 Andares)
# -----------------------------------------------------------------------------
def assemble_commercial_building(base_loc=Vector((14.0, 0.0, 0.0))):
    bld_col = get_or_create_collection("Predio_Modular_Urbano_3Andares")
    
    root = bpy.data.objects.new("Predio_Urbano_Root", None)
    root.location = base_loc
    bld_col.objects.link(root)
    
    # 1. Foundation Slab (8.4m x 8.4m) + Sidewalk Curb
    bm = bmesh.new()
    add_box(bm, -4.2, 4.2, -4.2, 4.2, -0.25, 0.0, mat_index=0)
    # Sidewalk extension in front
    add_box(bm, -4.6, 4.6, -6.2, -4.2, -0.25, 0.0, mat_index=1)
    foundation = finish_mesh("Building_Foundation", bm, mats=["Concrete_Foundation", "Wall_Modern_Concrete" if "Wall_Modern_Concrete" in MATERIALS else "Concrete_Foundation"], collection=bld_col)
    foundation.parent = root
    
    # 2. Ground Floor (Comercial / Shopfront): Floor-to-Ceiling Glass & Polished Marble
    # Floor Interior
    bm = bmesh.new()
    add_box(bm, -3.9, 3.9, -3.9, 3.9, -0.02, 0.0, mat_index=0)
    floor_gnd = finish_mesh("Building_Gnd_MarbleFloor", bm, mats=["Floor_Marble"], collection=bld_col)
    floor_gnd.parent = root
    
    # Ground Floor Walls (Z = 0.0 to 3.2):
    # Front Facade (Y = -4.0):
    # Two large concrete corner columns (at X = -4.0 and X = 4.0)
    # Large storefront glass windows (left X = -3.6 to -1.1, right X = 1.1 to 3.6)
    # Central entrance doorway (X = -1.1 to 1.1) with double interactive glass doors!
    bm = bmesh.new()
    # Left corner pillar
    add_box(bm, -4.0, -3.5, -4.2, -3.8, 0.0, 3.2, mat_index=0)
    # Right corner pillar
    add_box(bm, 3.5, 4.0, -4.2, -3.8, 0.0, 3.2, mat_index=0)
    # Header Signage Band (Z = 2.5 to 3.2, full width)
    add_box(bm, -4.0, 4.0, -4.22, -3.85, 2.5, 3.2, mat_index=1)
    
    # Left display window (X = -3.5 to -1.0)
    add_box(bm, -3.5, -1.0, -4.1, -3.95, 0.0, 0.25, mat_index=2) # Kickplate
    add_box(bm, -3.5, -1.0, -4.06, -3.98, 0.25, 2.5, mat_index=2) # Frame
    add_box(bm, -3.45, -1.05, -4.01, -3.99, 0.3, 2.45, mat_index=3) # Glass
    
    # Right display window (X = 1.0 to 3.5)
    add_box(bm, 1.0, 3.5, -4.1, -3.95, 0.0, 0.25, mat_index=2)
    add_box(bm, 1.0, 3.5, -4.06, -3.98, 0.25, 2.5, mat_index=2)
    add_box(bm, 1.05, 3.45, -4.01, -3.99, 0.3, 2.45, mat_index=3)
    
    # Central door frame lintel and transom (Z = 2.2 to 2.5)
    add_box(bm, -1.0, 1.0, -4.08, -3.96, 2.18, 2.24, mat_index=2) # Door frame lintel
    add_box(bm, -0.98, 0.98, -4.01, -3.99, 2.24, 2.48, mat_index=3) # Transom glass
    # Side jambs for doors
    add_box(bm, -1.02, -0.98, -4.1, -3.94, 0.0, 2.24, mat_index=2)
    add_box(bm, 0.98, 1.02, -4.1, -3.94, 0.0, 2.24, mat_index=2)
    
    # Solid side and rear walls on ground floor
    add_box(bm, -4.15, -3.85, -3.8, 4.0, 0.0, 3.2, mat_index=0) # Left wall
    add_box(bm, 3.85, 4.15, -3.8, 4.0, 0.0, 3.2, mat_index=0)  # Right wall
    add_box(bm, -4.0, 4.0, 3.85, 4.15, 0.0, 3.2, mat_index=0)    # Back wall
    
    facade_gnd = finish_mesh("Building_Facade_Ground", bm, mats=["Wall_Concrete", "Commercial_Sign", "Metal_Anthracite", "Window_Glass"], collection=bld_col)
    facade_gnd.parent = root
    
    # Double Interactive Commercial Glass Doors:
    # Left Door (Hinged at X = -0.98, Y = -4.0, swinging inwards/outwards)
    door_comm_l = build_interactive_door_commercial("Building_Door_Commercial_Left", hinge_side="left", collection=bld_col)
    door_comm_l.location = Vector((-0.98, -4.0, 0.0))
    door_comm_l.parent = root
    door_comm_l.rotation_euler.z = math.radians(40.0) # Welcoming open posture!
    
    # Right Door (Hinged at X = 0.98, Y = -4.0)
    door_comm_r = build_interactive_door_commercial("Building_Door_Commercial_Right", hinge_side="right", collection=bld_col)
    door_comm_r.location = Vector((0.98, -4.0, 0.0))
    door_comm_r.parent = root
    door_comm_r.rotation_euler.z = math.radians(-40.0) # Matching open posture!
    
    # Ground Floor Interior Walkable Staircase (X = 2.0 to 3.2, Y = 0.0 to 3.2)
    stair_gnd = build_staircase("Building_Staircase_Floor1_to_2", collection=bld_col)
    stair_gnd.location = Vector((2.6, 1.6, 0.0))
    stair_gnd.parent = root
    
    # 3. Second Floor (Residencial / Escritórios - Z = 3.2 to 6.4)
    # Floor slab with stair cutout
    bm = bmesh.new()
    add_box(bm, -4.0, 1.8, -4.0, 4.0, 3.0, 3.2, mat_index=0)
    add_box(bm, 1.8, 4.0, -4.0, 0.0, 3.0, 3.2, mat_index=0) # Leaves stairwell open (Y=0.0 to 3.2)
    add_box(bm, 1.8, 4.0, 3.2, 4.0, 3.0, 3.2, mat_index=0)
    floor_fl2 = finish_mesh("Building_Floor_Level2", bm, mats=["Floor_Marble"], collection=bld_col)
    floor_fl2.parent = root
    
    # Second Floor Facade: Modern Slate Panels + Two Large Panoramic Window Modules
    bm = bmesh.new()
    # Front Wall (Y = -4.0): Two large office/apartment windows
    add_box(bm, -4.0, -3.4, -4.125, -3.875, 3.2, 6.4, mat_index=0)
    add_box(bm, -0.6, 0.6, -4.125, -3.875, 3.2, 6.4, mat_index=0)
    add_box(bm, 3.4, 4.0, -4.125, -3.875, 3.2, 6.4, mat_index=0)
    # Window spandrels (below sill 0.8m, above top 2.6m)
    add_box(bm, -3.4, -0.6, -4.125, -3.875, 3.2, 4.0, mat_index=0)
    add_box(bm, -3.4, -0.6, -4.125, -3.875, 5.8, 6.4, mat_index=0)
    add_box(bm, 0.6, 3.4, -4.125, -3.875, 3.2, 4.0, mat_index=0)
    add_box(bm, 0.6, 3.4, -4.125, -3.875, 5.8, 6.4, mat_index=0)
    # Window frames & glass
    add_box(bm, -3.4, -0.6, -4.05, -3.95, 4.0, 5.8, mat_index=1)
    add_box(bm, -3.35, -0.65, -4.01, -3.99, 4.05, 5.75, mat_index=2)
    add_box(bm, 0.6, 3.4, -4.05, -3.95, 4.0, 5.8, mat_index=1)
    add_box(bm, 0.65, 3.35, -4.01, -3.99, 4.05, 5.75, mat_index=2)
    
    # Side and back walls
    add_box(bm, -4.125, -3.875, -4.0, 4.0, 3.2, 6.4, mat_index=0)
    add_box(bm, 3.875, 4.125, -4.0, 4.0, 3.2, 6.4, mat_index=0)
    add_box(bm, -4.0, 4.0, 3.875, 4.125, 3.2, 6.4, mat_index=0)
    
    facade_fl2 = finish_mesh("Building_Facade_Level2", bm, mats=["Wall_Concrete", "Metal_Anthracite", "Window_Glass"], collection=bld_col)
    facade_fl2.parent = root
    
    # Staircase continuing from Floor 2 to Floor 3
    stair_fl2 = build_staircase("Building_Staircase_Floor2_to_3", collection=bld_col)
    stair_fl2.location = Vector((2.6, 1.6, 3.2))
    stair_fl2.parent = root
    
    # 4. Third Floor (Penthouse / Executive - Z = 6.4 to 9.6)
    # Floor slab with stair cutout
    bm = bmesh.new()
    add_box(bm, -4.0, 1.8, -4.0, 4.0, 6.2, 6.4, mat_index=0)
    add_box(bm, 1.8, 4.0, -4.0, 0.0, 6.2, 6.4, mat_index=0)
    add_box(bm, 1.8, 4.0, 3.2, 4.0, 6.2, 6.4, mat_index=0)
    floor_fl3 = finish_mesh("Building_Floor_Level3", bm, mats=["Floor_Marble"], collection=bld_col)
    floor_fl3.parent = root
    
    # Third floor facade with corner balcony on left side (X = -4.0 to -1.5, Y = -4.0 to -2.0)
    bm = bmesh.new()
    # Recessed wall for balcony (Y = -2.0, from X = -4.0 to -1.5)
    add_box(bm, -4.0, -1.5, -2.125, -1.875, 6.4, 9.6, mat_index=0)
    # Balcony door frame (X = -3.2 to -2.2)
    add_box(bm, -3.22, -2.18, -2.05, -1.95, 6.4, 8.6, mat_index=1)
    
    # Front wall right portion (X = -1.5 to 4.0, Y = -4.0) with large window
    add_box(bm, -1.5, 4.0, -4.125, -3.875, 6.4, 7.2, mat_index=0)
    add_box(bm, -1.5, 4.0, -4.125, -3.875, 9.0, 9.6, mat_index=0)
    add_box(bm, -1.5, -0.8, -4.125, -3.875, 7.2, 9.0, mat_index=0)
    add_box(bm, 3.2, 4.0, -4.125, -3.875, 7.2, 9.0, mat_index=0)
    add_box(bm, -0.8, 3.2, -4.05, -3.95, 7.2, 9.0, mat_index=1)
    add_box(bm, -0.75, 3.15, -4.01, -3.99, 7.25, 8.95, mat_index=2)
    
    # Outer side and back walls
    add_box(bm, -4.125, -3.875, -2.0, 4.0, 6.4, 9.6, mat_index=0) # Left side
    add_box(bm, 3.875, 4.125, -4.0, 4.0, 6.4, 9.6, mat_index=0)  # Right side
    add_box(bm, -4.0, 4.0, 3.875, 4.125, 6.4, 9.6, mat_index=0)    # Back wall
    
    facade_fl3 = finish_mesh("Building_Facade_Level3", bm, mats=["Wall_Concrete", "Metal_Anthracite", "Window_Glass"], collection=bld_col)
    facade_fl3.parent = root
    
    # Balcony Railing (Level 3)
    bm = bmesh.new()
    add_box(bm, -4.0, -1.5, -4.04, -3.96, 6.4, 7.4, mat_index=0)
    add_box(bm, -3.95, -1.55, -4.01, -3.99, 6.5, 7.3, mat_index=1)
    add_box(bm, -1.54, -1.46, -4.0, -2.0, 6.4, 7.4, mat_index=0)
    railing_fl3 = finish_mesh("Building_Balcony_Railing_Level3", bm, mats=["Metal_Anthracite", "Window_Glass"], collection=bld_col)
    railing_fl3.parent = root
    
    # Interactive Door to Penthouse Balcony
    door_pent = build_interactive_door_residential("Building_Door_Penthouse_Balcony", collection=bld_col)
    door_pent.location = Vector((-3.2, -2.0, 6.4))
    door_pent.parent = root
    door_pent.rotation_euler.z = math.radians(50.0)
    
    # 5. Rooftop (Z = 9.6): Flat Roof with Parapets, Stair Doghouse, HVAC Chillers & Antenna
    roof_flat = build_roof_flat_urban("Building_Roof_Flat", collection=bld_col)
    roof_flat.location = Vector((0.0, 0.0, 9.6))
    roof_flat.scale = Vector((2.02, 2.02, 1.0))
    roof_flat.parent = root
    
    # Rooftop Stair Doghouse (Access enclosure at X = 1.5 to 3.5, Y = 0.5 to 3.5, Z = 9.6 to 12.0)
    bm = bmesh.new()
    add_box(bm, 1.5, 3.5, 0.5, 3.5, 9.6, 12.0, mat_index=0)
    # Doghouse door frame on front (Y = 0.5, X = 1.8 to 2.8)
    add_box(bm, 1.78, 2.82, 0.44, 0.54, 9.6, 11.7, mat_index=1)
    doghouse = finish_mesh("Building_Rooftop_Doghouse", bm, mats=["Wall_Concrete", "Metal_Anthracite"], collection=bld_col)
    doghouse.parent = root
    
    # Interactive Rooftop Service Door
    door_roof = build_interactive_door_commercial("Building_Door_Rooftop_Access", hinge_side="left", collection=bld_col)
    door_roof.location = Vector((1.8, 0.48, 9.6))
    door_roof.parent = root
    door_roof.rotation_euler.z = math.radians(65.0)
    
    # Industrial Rooftop HVAC Chillers
    hvac1 = build_hvac_unit("Building_HVAC_Unit_01", collection=bld_col)
    hvac1.location = Vector((-2.0, 1.5, 9.6))
    hvac1.parent = root
    
    hvac2 = build_hvac_unit("Building_HVAC_Unit_02", collection=bld_col)
    hvac2.location = Vector((-2.0, -1.5, 9.6))
    hvac2.parent = root
    
    # Rooftop Communications Mast / Antenna
    bm = bmesh.new()
    # Base mount
    add_box(bm, -0.3, 0.3, -0.3, 0.3, 9.6, 9.9, mat_index=0)
    # Vertical mast tower
    add_box(bm, -0.06, 0.06, -0.06, 0.06, 9.9, 14.5, mat_index=0)
    # Crossbars
    add_box(bm, -0.5, 0.5, -0.03, 0.03, 13.0, 13.1, mat_index=0)
    add_box(bm, -0.3, 0.3, -0.03, 0.03, 13.8, 13.9, mat_index=0)
    # Red obstacle light at tip
    add_box(bm, -0.08, 0.08, -0.08, 0.08, 14.5, 14.7, mat_index=1)
    antenna = finish_mesh("Building_Roof_Antenna", bm, mats=["Metal_Anthracite", "Interior_Glow"], collection=bld_col)
    antenna.parent = root
    
    return root

# -----------------------------------------------------------------------------
# Modular Kit Showcase Palette (Organized Grid of Individual Snap Pieces)
# -----------------------------------------------------------------------------
def assemble_modular_kit_palette(start_loc=Vector((-12.0, -10.0, 0.0))):
    kit_col = get_or_create_collection("Kit_Pecas_Modulares_Individuais")
    
    root = bpy.data.objects.new("Modular_Kit_Palette_Root", None)
    root.location = start_loc
    kit_col.objects.link(root)
    
    # Grid spacing: 5.5m in X, 5.5m in Y
    # Row 0 (Y = 0): Foundations & Floors
    p_fnd = build_foundation_slab("Kit_Mod_Foundation_4x4", collection=kit_col)
    p_fnd.location = Vector((0.0, 0.0, 0.0))
    p_fnd.parent = root
    
    p_wood = build_floor_slab("Kit_Mod_Floor_Wood_4x4", top_mat="Floor_Parquet", collection=kit_col)
    p_wood.location = Vector((5.5, 0.0, 0.0))
    p_wood.parent = root
    
    p_tile = build_floor_slab("Kit_Mod_Floor_Tile_4x4", top_mat="Floor_Marble", collection=kit_col)
    p_tile.location = Vector((11.0, 0.0, 0.0))
    p_tile.parent = root
    
    p_stair_fl = build_floor_stairwell("Kit_Mod_Floor_StairCutout_4x4", collection=kit_col)
    p_stair_fl.location = Vector((16.5, 0.0, 0.0))
    p_stair_fl.parent = root
    
    # Row 1 (Y = -5.5): Exterior Walls
    p_w_solid = build_wall_solid("Kit_Mod_Wall_Solid_4x3", ext_mat="Brick_Red", collection=kit_col)
    p_w_solid.location = Vector((0.0, -5.5, 0.0))
    p_w_solid.parent = root
    
    p_w_door = build_wall_doorway("Kit_Mod_Wall_Doorway_4x3", ext_mat="Brick_Red", collection=kit_col)
    p_w_door.location = Vector((5.5, -5.5, 0.0))
    p_w_door.parent = root
    
    p_w_win = build_wall_window_double("Kit_Mod_Wall_Window_Double_4x3", ext_mat="Brick_Red", collection=kit_col)
    p_w_win.location = Vector((11.0, -5.5, 0.0))
    p_w_win.parent = root
    
    p_w_pano = build_wall_panoramic("Kit_Mod_Wall_Window_Panoramic_4x3", ext_mat="Wall_Concrete", collection=kit_col)
    p_w_pano.location = Vector((16.5, -5.5, 0.0))
    p_w_pano.parent = root
    
    p_w_shop = build_wall_shopfront("Kit_Mod_Wall_ShopFront_4x3", collection=kit_col)
    p_w_shop.location = Vector((22.0, -5.5, 0.0))
    p_w_shop.parent = root
    
    # Row 2 (Y = -11.0): Stairs, Interactive Doors, Railings & Roofs
    p_stairs = build_staircase("Kit_Mod_Staircase_Straight_4x3", collection=kit_col)
    p_stairs.location = Vector((0.0, -11.0, 0.0))
    p_stairs.parent = root
    
    p_door_res = build_interactive_door_residential("Kit_Mod_Interactive_Door_Residential", collection=kit_col)
    p_door_res.location = Vector((5.0, -11.0, 0.0))
    p_door_res.parent = root
    
    p_door_com = build_interactive_door_commercial("Kit_Mod_Interactive_Door_Commercial", hinge_side="left", collection=kit_col)
    p_door_com.location = Vector((8.0, -11.0, 0.0))
    p_door_com.parent = root
    
    p_win_sash = build_interactive_window_sash("Kit_Mod_Interactive_Window_Sash", collection=kit_col)
    p_win_sash.location = Vector((11.0, -11.0, 0.0))
    p_win_sash.parent = root
    
    p_railing = build_balcony_railing("Kit_Mod_Balcony_GlassRailing_4m", collection=kit_col)
    p_railing.location = Vector((16.5, -11.0, 0.0))
    p_railing.parent = root
    
    p_pillar = build_corner_pillar("Kit_Mod_Pillar_Corner_3m", collection=kit_col)
    p_pillar.location = Vector((22.0, -11.0, 0.0))
    p_pillar.parent = root
    
    # Row 3 (Y = -17.5): Roof Modules & Rooftop Props
    p_roof_gable = build_roof_gable("Kit_Mod_Roof_Gable_4x4", collection=kit_col)
    p_roof_gable.location = Vector((0.0, -17.5, 0.0))
    p_roof_gable.parent = root
    
    p_roof_flat = build_roof_flat_urban("Kit_Mod_Roof_Flat_4x4", collection=kit_col)
    p_roof_flat.location = Vector((7.0, -17.5, 0.0))
    p_roof_flat.parent = root
    
    p_hvac = build_hvac_unit("Kit_Mod_Roof_HVAC_Chiller", collection=kit_col)
    p_hvac.location = Vector((14.0, -17.5, 0.0))
    p_hvac.parent = root
    
    p_chim = build_chimney("Kit_Mod_Chimney_Brick", collection=kit_col)
    p_chim.location = Vector((18.5, -17.5, 0.0))
    p_chim.parent = root
    
    return root

# -----------------------------------------------------------------------------
# Studio Lighting, Ground Environment, and Camera Setup
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

def setup_environment_and_camera():
    studio_col = get_or_create_collection("Studio_Setup")
    
    # Ground plane (Paved asphalt / plaza ground)
    bm = bmesh.new()
    add_box(bm, -40.0, 50.0, -50.0, 25.0, -0.4, -0.25, mat_index=0)
    # Front street road strip
    add_box(bm, -40.0, 50.0, -8.0, -4.5, -0.25, -0.23, mat_index=1)
    ground = finish_mesh("Studio_Ground_Plaza", bm, mats=["Concrete_Foundation", "Wall_Concrete"], collection=studio_col)
    
    # Sun Light (Warm afternoon golden light, casting crisp architectural shadows)
    sun_data = bpy.data.lights.new(name="Sun_KeyLight", type='SUN')
    sun_data.energy = 4.8
    sun_data.color = (1.0, 0.95, 0.88)
    sun_data.angle = math.radians(1.5)
    sun_obj = bpy.data.objects.new("Sun_KeyLight", sun_data)
    sun_obj.location = Vector((20.0, -25.0, 30.0))
    sun_obj.rotation_euler = (math.radians(52.0), math.radians(15.0), math.radians(-38.0))
    studio_col.objects.link(sun_obj)
    
    # Ambient Fill Light (Soft cool sky bounce)
    fill_data = bpy.data.lights.new(name="Light_Sky_Fill", type='SUN')
    fill_data.energy = 1.3
    fill_data.color = (0.75, 0.85, 1.0)
    fill_obj = bpy.data.objects.new("Light_Sky_Fill", fill_data)
    fill_obj.location = Vector((-20.0, 20.0, 20.0))
    fill_obj.rotation_euler = (math.radians(45.0), math.radians(-20.0), math.radians(130.0))
    studio_col.objects.link(fill_obj)
    
    # Warm Interior Ground Floor Lights to highlight the open doors & windows
    for loc, name in [
        (Vector((-0.5, -2.5, 1.8)), "PointLight_House_LivingRoom"),
        (Vector((14.0, -2.0, 1.8)), "PointLight_Building_ShopLobby"),
    ]:
        p_data = bpy.data.lights.new(name=name, type='POINT')
        p_data.energy = 380.0
        p_data.color = (1.0, 0.88, 0.72)
        p_data.shadow_soft_size = 0.5
        p_obj = bpy.data.objects.new(name, p_data)
        p_obj.location = loc
        studio_col.objects.link(p_obj)
    
    # Showcase Camera
    cam_data = bpy.data.cameras.new("Showcase_Camera")
    cam_data.lens = 26 # Ultra-wide architectural lens
    cam_data.clip_start = 0.1
    cam_data.clip_end = 300.0
    cam_obj = bpy.data.objects.new("Showcase_Camera", cam_data)
    
    # Position camera to capture both the 2-Story House, 3-Story Building, and Modular Kit
    cam_obj.location = Vector((6.5, -48.0, 20.0))
    cam_obj.rotation_euler = (math.radians(64.0), 0.0, math.radians(-1.0))
    studio_col.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    
    return cam_obj

# -----------------------------------------------------------------------------
# Render Settings & Image Output
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
    
    # Eevee / viewport render settings
    if hasattr(scene, 'eevee'):
        if hasattr(scene.eevee, 'use_gtao'):
            scene.eevee.use_gtao = True
        if hasattr(scene.eevee, 'use_bloom'):
            scene.eevee.use_bloom = True
        if hasattr(scene.eevee, 'use_ssr'):
            scene.eevee.use_ssr = True
            
    print(f"Rendering showcase image to: {output_path}...")
    bpy.ops.render.render(write_still=True)
    print("Render completed successfully!")

# -----------------------------------------------------------------------------
# FBX Export Utilities
# -----------------------------------------------------------------------------
def export_modular_fbx_assets():
    print("Exporting modular pieces and buildings to Unity Assets...")
    
    # 1. Export Assembled House
    house_root = bpy.data.objects.get("Casa_Residencial_Root")
    if house_root:
        bpy.ops.object.select_all(action='DESELECT')
        def select_children(o):
            o.select_set(True)
            for c in o.children:
                select_children(c)
        select_children(house_root)
        bpy.context.view_layer.objects.active = house_root
        fbx_path = os.path.join(ASSETS_MODULAR_DIR, "Casa_Modular_Residencial_Complete.fbx")
        try:
            bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=True, apply_unit_scale=True, axis_forward='-Z', axis_up='Y')
            print(f"Exported: {fbx_path}")
        except Exception as e:
            print(f"Export warning: {e}")
            
    # 2. Export Assembled Building
    bld_root = bpy.data.objects.get("Predio_Urbano_Root")
    if bld_root:
        bpy.ops.object.select_all(action='DESELECT')
        def select_children(o):
            o.select_set(True)
            for c in o.children:
                select_children(c)
        select_children(bld_root)
        bpy.context.view_layer.objects.active = bld_root
        fbx_path = os.path.join(ASSETS_MODULAR_DIR, "Predio_Modular_Urbano_Complete.fbx")
        try:
            bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=True, apply_unit_scale=True, axis_forward='-Z', axis_up='Y')
            print(f"Exported: {fbx_path}")
        except Exception as e:
            print(f"Export warning: {e}")

    # 3. Export Individual Modular Kit Elements (Centered at Origin)
    kit_col = bpy.data.collections.get("Kit_Pecas_Modulares_Individuais")
    if kit_col:
        for obj in kit_col.objects:
            if obj.type == 'MESH':
                clean_name = obj.name.replace("Kit_", "")
                fbx_path = os.path.join(ASSETS_MODULAR_DIR, f"{clean_name}.fbx")
                
                # Temporarily unparent and center to 0,0,0
                orig_loc = obj.location.copy()
                orig_parent = obj.parent
                orig_rot = obj.rotation_euler.copy()
                
                obj.parent = None
                obj.location = Vector((0, 0, 0))
                if "Door" in obj.name or "Window" in obj.name:
                    obj.rotation_euler = (0, 0, 0) # Neutral closed position for prefab export
                
                bpy.ops.object.select_all(action='DESELECT')
                obj.select_set(True)
                bpy.context.view_layer.objects.active = obj
                try:
                    bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=True, apply_unit_scale=True, axis_forward='-Z', axis_up='Y')
                    print(f"Exported Modular Part: {clean_name}.fbx")
                except Exception as e:
                    print(f"Export warning for {obj.name}: {e}")
                
                # Restore original placement in showcase palette
                obj.location = orig_loc
                obj.rotation_euler = orig_rot
                obj.parent = orig_parent

# -----------------------------------------------------------------------------
# Main Execution Pipeline
# -----------------------------------------------------------------------------
def main():
    print("===================================================================")
    print("Generating Modular & Interactive Houses and Buildings for Blender...")
    print("===================================================================")
    
    reset_scene()
    create_pbr_materials()
    
    # 1. Assemble 2-Story Modular Residential House
    print("Building Casa Modular Residencial (2 Andares)...")
    house = assemble_residential_house(base_loc=Vector((0.0, 0.0, 0.0)))
    
    # 2. Assemble 3-Story Modular Urban Building
    print("Building Prédio Modular Urbano Comercial (3 Andares)...")
    building = assemble_commercial_building(base_loc=Vector((14.0, 0.0, 0.0)))
    
    # 3. Assemble Modular Kit Palette
    print("Building Kit de Peças Modulares Individuais...")
    palette = assemble_modular_kit_palette(start_loc=Vector((-12.0, -10.0, 0.0)))
    
    # 4. Setup Lighting, Ground, and Camera
    print("Setting up Architectural Studio Lighting and Camera...")
    setup_environment_and_camera()
    
    # 5. Generate Architectural UV coordinates
    auto_unwrap_all_meshes()
    
    # 6. Export Modular Assets to Unity FBX
    export_modular_fbx_assets()
    
    # 7. Save Master .blend File
    print(f"Saving Blender master file: {BLEND_FILE_PATH}...")
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_FILE_PATH)
    
    # Also save a copy inside Unity Assets/_Project/Models/ModularBuildings
    print(f"Saving copy to Unity Assets: {ASSETS_BLEND_PATH}...")
    bpy.ops.wm.save_as_mainfile(filepath=ASSETS_BLEND_PATH)
    
    # 8. Render Showcase Image
    render_showcase_image(RENDER_OUTPUT_PATH)
    
    print("===================================================================")
    print("ALL MODULAR BUILDINGS & INTERACTIVE PACK GENERATED SUCCESSFULLY!")
    print("===================================================================")

if __name__ == "__main__":
    main()
