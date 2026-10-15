"""
Script mestre executado via Blender 5.2 em background (-b -P)
Cria estruturas modulares e objetos realistas de Fazenda e Cidade.
Gera geometria precisa, UVs unwrapped, materiais PBR completos,
exporta modelos em FBX (Y-up para Unity) e salva coleções .blend e renders de demonstração.
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix, Euler

# Diretórios
PROJECT_ROOT = r"E:\Backup-C\Projetos\testejogo"
FARM_MODELS_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Models", "ModularBuildings", "Farm")
CITY_MODELS_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Models", "ModularBuildings", "City")
FARM_TEX_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Textures", "ModularBuildings", "Farm")
CITY_TEX_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Textures", "ModularBuildings", "City")

def clean_scene():
    """Remove todos os objetos, malhas, materiais e imagens da cena."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in bpy.data.meshes: bpy.data.meshes.remove(block, do_unlink=True)
    for block in bpy.data.materials: bpy.data.materials.remove(block, do_unlink=True)
    for block in bpy.data.textures: bpy.data.textures.remove(block, do_unlink=True)
    for block in bpy.data.images: bpy.data.images.remove(block, do_unlink=True)

# Funções primitivas robustas em bmesh
def add_box(bm, size=(1.0, 1.0, 1.0), loc=(0.0, 0.0, 0.0), rot=(0.0, 0.0, 0.0)):
    r = bmesh.ops.create_cube(bm, size=1.0)
    mat_rot = Euler(rot).to_matrix()
    sx, sy, sz = size
    lx, ly, lz = loc
    for v in r['verts']:
        scaled = Vector((v.co.x * sx, v.co.y * sy, v.co.z * sz))
        rotated = mat_rot @ scaled
        v.co = rotated + Vector((lx, ly, lz))
    return r['verts']

def add_cylinder(bm, radius=0.5, depth=1.0, segments=16, loc=(0.0, 0.0, 0.0), rot=(0.0, 0.0, 0.0), cap_ends=True):
    r = bmesh.ops.create_cone(bm, cap_ends=cap_ends, segments=segments, radius1=radius, radius2=radius, depth=depth)
    mat_rot = Euler(rot).to_matrix()
    lx, ly, lz = loc
    for v in r['verts']:
        rotated = mat_rot @ v.co
        v.co = rotated + Vector((lx, ly, lz))
    return r['verts']

def add_cone(bm, radius1=0.5, radius2=0.0, depth=1.0, segments=16, loc=(0.0, 0.0, 0.0), rot=(0.0, 0.0, 0.0), cap_ends=True):
    r = bmesh.ops.create_cone(bm, cap_ends=cap_ends, segments=segments, radius1=radius1, radius2=radius2, depth=depth)
    mat_rot = Euler(rot).to_matrix()
    lx, ly, lz = loc
    for v in r['verts']:
        rotated = mat_rot @ v.co
        v.co = rotated + Vector((lx, ly, lz))
    return r['verts']

def add_sphere(bm, radius=0.5, u_segments=16, v_segments=10, loc=(0.0, 0.0, 0.0)):
    r = bmesh.ops.create_uvsphere(bm, u_segments=u_segments, v_segments=v_segments, radius=radius)
    lx, ly, lz = loc
    for v in r['verts']:
        v.co += Vector((lx, ly, lz))
    return r['verts']

def create_pbr_material(mat_name, albedo_path, normal_path, roughness_path, metallic_path=None):
    """Cria material PBR com nós Principled BSDF."""
    mat = bpy.data.materials.new(name=mat_name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    
    node_out = nodes.new(type='ShaderNodeOutputMaterial')
    node_out.location = (400, 0)
    
    node_bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    node_bsdf.location = (100, 0)
    links.new(node_bsdf.outputs['BSDF'], node_out.inputs['Surface'])
    
    # Albedo
    if os.path.exists(albedo_path):
        img_alb = bpy.data.images.load(albedo_path)
        node_alb = nodes.new(type='ShaderNodeTexImage')
        node_alb.location = (-350, 200)
        node_alb.image = img_alb
        links.new(node_alb.outputs['Color'], node_bsdf.inputs['Base Color'])
        
    # Normal Map
    if os.path.exists(normal_path):
        img_nor = bpy.data.images.load(normal_path)
        img_nor.colorspace_settings.name = 'Non-Color'
        node_nor_tex = nodes.new(type='ShaderNodeTexImage')
        node_nor_tex.location = (-650, -250)
        node_nor_tex.image = img_nor
        
        node_nor_map = nodes.new(type='ShaderNodeNormalMap')
        node_nor_map.location = (-350, -250)
        node_nor_map.inputs['Strength'].default_value = 1.0
        
        links.new(node_nor_tex.outputs['Color'], node_nor_map.inputs['Color'])
        links.new(node_nor_map.outputs['Normal'], node_bsdf.inputs['Normal'])
        
    # Roughness
    if os.path.exists(roughness_path):
        img_rgh = bpy.data.images.load(roughness_path)
        img_rgh.colorspace_settings.name = 'Non-Color'
        node_rgh = nodes.new(type='ShaderNodeTexImage')
        node_rgh.location = (-350, -50)
        node_rgh.image = img_rgh
        links.new(node_rgh.outputs['Color'], node_bsdf.inputs['Roughness'])
    else:
        node_bsdf.inputs['Roughness'].default_value = 0.6
        
    # Metallic
    if metallic_path and os.path.exists(metallic_path):
        img_met = bpy.data.images.load(metallic_path)
        img_met.colorspace_settings.name = 'Non-Color'
        node_met = nodes.new(type='ShaderNodeTexImage')
        node_met.location = (-350, 50)
        node_met.image = img_met
        links.new(node_met.outputs['Color'], node_bsdf.inputs['Metallic'])
    else:
        node_bsdf.inputs['Metallic'].default_value = 0.0
        
    return mat

def finalize_object(obj, width=0.012):
    """Aplica rotação, escala, unwrapping Smart UV e chanfro suave."""
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    
    # UV Unwrap
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.01)
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # Smooth
    for p in obj.data.polygons:
        p.use_smooth = True
        
    # Bevel modifier
    if width > 0:
        mod = obj.modifiers.new(name="Bevel", type='BEVEL')
        mod.width = width
        mod.segments = 2
        mod.limit_method = 'ANGLE'
        mod.angle_limit = math.radians(35.0)

# =========================================================================
# CONSTRUTORES DE FAZENDA MODULAR
# =========================================================================

def build_farm_wall_wood():
    mesh = bpy.data.meshes.new("Farm_Wall_Wood_Planks_Mesh")
    obj = bpy.data.objects.new("Farm_Wall_Wood_Planks", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    # Painel central da parede
    add_box(bm, size=(3.8, 0.16, 3.2), loc=(0.0, 0.0, 1.8))
    # Pilares nas duas pontas (X = -1.9 e +1.9m)
    add_box(bm, size=(0.24, 0.24, 3.5), loc=(-1.9, 0.0, 1.75))
    add_box(bm, size=(0.24, 0.24, 3.5), loc=(1.9, 0.0, 1.75))
    # Viga superior
    add_box(bm, size=(4.0, 0.22, 0.2), loc=(0.0, 0.0, 3.4))
    # Rodapé de alvenaria/pedra
    add_box(bm, size=(4.0, 0.24, 0.2), loc=(0.0, 0.0, 0.1))
    # Travessa de reforço em cruz na face
    add_box(bm, size=(3.6, 0.06, 0.14), loc=(0.0, 0.10, 1.75))
    
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.012)
    return obj

def build_farm_wall_door():
    mesh = bpy.data.meshes.new("Farm_Wall_Door_Double_Mesh")
    obj = bpy.data.objects.new("Farm_Wall_Door_Double", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    # Parede esquerda e direita
    add_box(bm, size=(0.8, 0.20, 3.5), loc=(-1.6, 0.0, 1.75))
    add_box(bm, size=(0.8, 0.20, 3.5), loc=(1.6, 0.0, 1.75))
    # Verga superior
    add_box(bm, size=(2.4, 0.20, 0.6), loc=(0.0, 0.0, 3.2))
    # Trilho de ferro
    add_box(bm, size=(3.4, 0.06, 0.08), loc=(0.0, 0.15, 2.95))
    # Portão de correr esquerdo e direito
    add_box(bm, size=(1.25, 0.08, 2.85), loc=(-0.62, 0.13, 1.45))
    add_box(bm, size=(1.25, 0.08, 2.85), loc=(0.62, 0.13, 1.45))
    # Roldanas de aço
    for rx in [-0.9, -0.35, 0.35, 0.9]:
        add_cylinder(bm, radius=0.06, depth=0.04, segments=12, loc=(rx, 0.16, 2.95), rot=(math.pi/2, 0, 0))
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.012)
    return obj

def build_farm_wall_window():
    mesh = bpy.data.meshes.new("Farm_Wall_Window_Mesh")
    obj = bpy.data.objects.new("Farm_Wall_Window", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    # Painel inferior e superior
    add_box(bm, size=(4.0, 0.18, 1.2), loc=(0.0, 0.0, 0.6))
    add_box(bm, size=(4.0, 0.18, 0.9), loc=(0.0, 0.0, 3.05))
    # Painéis laterais da janela
    add_box(bm, size=(1.2, 0.18, 1.4), loc=(-1.4, 0.0, 1.9))
    add_box(bm, size=(1.2, 0.18, 1.4), loc=(1.4, 0.0, 1.9))
    # Peitoril espesso de madeira
    add_box(bm, size=(1.8, 0.28, 0.08), loc=(0.0, 0.0, 1.2))
    # Esquadria em cruz
    add_box(bm, size=(1.6, 0.06, 0.06), loc=(0.0, 0.0, 1.9))
    add_box(bm, size=(0.06, 0.06, 1.35), loc=(0.0, 0.0, 1.9))
    # Vidraça
    add_box(bm, size=(1.55, 0.02, 1.32), loc=(0.0, 0.0, 1.9))
    
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.012)
    return obj

def build_farm_roof_straight():
    mesh = bpy.data.meshes.new("Farm_Roof_Straight_Mesh")
    obj = bpy.data.objects.new("Farm_Roof_Straight", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    # Duas águas inclinadas 30 graus
    add_box(bm, size=(2.5, 4.0, 0.08), loc=(-1.12, 0.0, 0.72), rot=(0, math.radians(30), 0))
    add_box(bm, size=(2.5, 4.0, 0.08), loc=(1.12, 0.0, 0.72), rot=(0, math.radians(-30), 0))
    # Cumeeira
    add_box(bm, size=(0.35, 4.05, 0.12), loc=(0.0, 0.0, 1.4))
    # Vigas de suporte transversais
    for ty in [-1.5, 0.0, 1.5]:
        add_box(bm, size=(3.8, 0.14, 0.14), loc=(0.0, ty, 0.0))
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.01)
    return obj

def build_farm_roof_gable():
    mesh = bpy.data.meshes.new("Farm_Roof_Gable_End_Mesh")
    obj = bpy.data.objects.new("Farm_Roof_Gable_End", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    half_w = 2.0
    h = 1.35
    th = 0.18
    v1 = bm.verts.new((-half_w, -th/2, 0))
    v2 = bm.verts.new((half_w, -th/2, 0))
    v3 = bm.verts.new((0, -th/2, h))
    v4 = bm.verts.new((-half_w, th/2, 0))
    v5 = bm.verts.new((half_w, th/2, 0))
    v6 = bm.verts.new((0, th/2, h))
    bm.faces.new((v1, v2, v3))
    bm.faces.new((v4, v6, v5))
    bm.faces.new((v1, v3, v6, v4))
    bm.faces.new((v2, v5, v6, v3))
    bm.faces.new((v1, v4, v5, v2))
    
    # Respiro de sótão
    add_box(bm, size=(0.65, 0.24, 0.55), loc=(0.0, 0.0, 0.65))
    
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.01)
    return obj

def build_farm_silo():
    mesh = bpy.data.meshes.new("Farm_Silo_Modular_Mesh")
    obj = bpy.data.objects.new("Farm_Silo_Modular", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    # Corpo do Silo
    add_cylinder(bm, radius=1.7, depth=8.0, segments=32, loc=(0.0, 0.0, 4.0))
    # 7 anéis de reforço
    for i in range(1, 8):
        add_cylinder(bm, radius=1.74, depth=0.1, segments=32, loc=(0.0, 0.0, i * 1.0), cap_ends=False)
    # Cúpula cônica
    add_cone(bm, radius1=1.78, radius2=0.2, depth=1.4, segments=32, loc=(0.0, 0.0, 8.7))
    # Respiro superior
    add_cylinder(bm, radius=0.25, depth=0.4, segments=16, loc=(0.0, 0.0, 9.6))
    # Escada de inspeção
    for ly in [-0.22, 0.22]:
        add_box(bm, size=(0.04, 0.04, 8.0), loc=(1.82, ly, 4.0))
    for rz in range(1, 23):
        add_box(bm, size=(0.03, 0.44, 0.03), loc=(1.82, 0.0, rz * 0.35))
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.0)
    return obj

def build_farm_fence():
    mesh = bpy.data.meshes.new("Farm_Fence_Wood_Mesh")
    obj = bpy.data.objects.new("Farm_Fence_Wood", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    for mx in [-1.5, 1.5]:
        add_cylinder(bm, radius=0.09, depth=1.4, segments=12, loc=(mx, 0.0, 0.7))
    for hz in [0.35, 0.75, 1.15]:
        add_box(bm, size=(3.0, 0.05, 0.14), loc=(0.0, 0.08, hz))
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.01)
    return obj

def build_farm_fence_gate():
    mesh = bpy.data.meshes.new("Farm_Fence_Gate_Mesh")
    obj = bpy.data.objects.new("Farm_Fence_Gate", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    for mx in [-1.55, 1.55]:
        add_cylinder(bm, radius=0.12, depth=1.6, segments=14, loc=(mx, 0.0, 0.8))
    for px in [-1.4, 1.4]:
        add_box(bm, size=(0.09, 0.07, 1.25), loc=(px, 0.0, 0.75))
    for pz in [0.2, 0.55, 0.9, 1.3]:
        add_box(bm, size=(2.8, 0.05, 0.12), loc=(0.0, 0.0, pz))
    # Diagonal em Z
    add_box(bm, size=(3.0, 0.05, 0.12), loc=(0.0, 0.02, 0.75), rot=(0, math.radians(-24), 0))
    # Dobradiças
    for dz in [0.35, 1.15]:
        add_cylinder(bm, radius=0.03, depth=0.15, segments=10, loc=(-1.48, 0.0, dz))
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.01)
    return obj

def build_farm_prop_hay_bale():
    mesh = bpy.data.meshes.new("Farm_Prop_Hay_Bale_Mesh")
    obj = bpy.data.objects.new("Farm_Prop_Hay_Bale", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    add_box(bm, size=(1.05, 0.58, 0.46), loc=(0.0, 0.0, 0.23))
    # 2 Cordas
    for cx in [-0.25, 0.25]:
        add_box(bm, size=(0.03, 0.60, 0.48), loc=(cx, 0.0, 0.23))
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.03)
    return obj

def build_farm_prop_barrel():
    mesh = bpy.data.meshes.new("Farm_Prop_Wooden_Barrel_Mesh")
    obj = bpy.data.objects.new("Farm_Prop_Wooden_Barrel", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    # Corpo abaulado
    segments = 24
    height = 0.95
    rings = 10
    ring_verts = []
    for r_idx in range(rings + 1):
        t = r_idx / rings
        z = t * height
        radius = 0.35 + math.sin(t * math.pi) * 0.09
        curr = []
        for s in range(segments):
            angle = (s / segments) * 2 * math.pi
            curr.append(bm.verts.new((math.cos(angle) * radius, math.sin(angle) * radius, z)))
        ring_verts.append(curr)
        
    for r_idx in range(rings):
        r1 = ring_verts[r_idx]
        r2 = ring_verts[r_idx + 1]
        for s in range(segments):
            sn = (s + 1) % segments
            bm.faces.new((r1[s], r2[s], r2[sn], r1[sn]))
    bm.faces.new([ring_verts[0][s] for s in range(segments)])
    bm.faces.new([ring_verts[-1][s] for s in reversed(range(segments))])
    
    # 4 aros de ferro
    for az, ar in [(0.12, 0.39), (0.35, 0.445), (0.60, 0.445), (0.83, 0.39)]:
        add_cylinder(bm, radius=ar, depth=0.04, segments=24, loc=(0.0, 0.0, az), cap_ends=False)
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.0)
    return obj

def build_farm_prop_trough():
    mesh = bpy.data.meshes.new("Farm_Prop_Trough_Mesh")
    obj = bpy.data.objects.new("Farm_Prop_Trough", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    add_box(bm, size=(2.0, 0.6, 0.08), loc=(0.0, 0.0, 0.24))
    for ly in [-0.32, 0.32]:
        add_box(bm, size=(2.08, 0.07, 0.36), loc=(0.0, ly, 0.38))
    for cx in [-1.04, 1.04]:
        add_box(bm, size=(0.07, 0.68, 0.36), loc=(cx, 0.0, 0.38))
    for px in [-0.85, 0.85]:
        add_box(bm, size=(0.12, 0.74, 0.20), loc=(px, 0.0, 0.10))
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.015)
    return obj

def build_farm_prop_pallet():
    mesh = bpy.data.meshes.new("Farm_Prop_Pallet_Stack_Mesh")
    obj = bpy.data.objects.new("Farm_Prop_Pallet_Stack", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    for p_idx in range(2):
        bz = p_idx * 0.16
        for sy in [-0.36, 0.0, 0.36]:
            add_box(bm, size=(1.2, 0.08, 0.09), loc=(0.0, sy, bz + 0.045))
        for tx in [-0.5, -0.25, 0.0, 0.25, 0.5]:
            add_box(bm, size=(0.12, 0.8, 0.025), loc=(tx, 0.0, bz + 0.105))
            
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.008)
    return obj

# =========================================================================
# CONSTRUTORES DE CIDADE MODULAR
# =========================================================================

def build_city_sidewalk_straight():
    mesh = bpy.data.meshes.new("City_Sidewalk_Straight_4m_Mesh")
    obj = bpy.data.objects.new("City_Sidewalk_Straight_4m", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    # Calçada de pedestres
    add_box(bm, size=(4.0, 2.2, 0.20), loc=(0.0, 1.1, 0.10))
    # Meio-fio de granito
    add_box(bm, size=(4.0, 0.15, 0.20), loc=(0.0, 2.275, 0.10))
    # Sarjeta e asfalto
    add_box(bm, size=(4.0, 0.65, 0.05), loc=(0.0, 2.675, 0.025))
    
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.015)
    return obj

def build_city_sidewalk_corner():
    mesh = bpy.data.meshes.new("City_Sidewalk_Corner_4m_Mesh")
    obj = bpy.data.objects.new("City_Sidewalk_Corner_4m", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    add_box(bm, size=(4.0, 4.0, 0.20), loc=(0.0, 0.0, 0.10))
    add_box(bm, size=(4.0, 0.8, 0.05), loc=(0.0, 1.6, 0.025))
    
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.02)
    return obj

def build_city_wall_shopfront():
    mesh = bpy.data.meshes.new("City_Wall_Concrete_ShopFront_Mesh")
    obj = bpy.data.objects.new("City_Wall_Concrete_ShopFront", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    # Pilares laterais
    for px in [-1.8, 1.8]:
        add_box(bm, size=(0.4, 0.35, 4.0), loc=(px, 0.0, 2.0))
    # Viga superior
    add_box(bm, size=(3.2, 0.35, 0.8), loc=(0.0, 0.0, 3.6))
    # Rodapé granito
    add_box(bm, size=(3.2, 0.25, 0.25), loc=(0.0, 0.0, 0.125))
    # Marquise
    add_box(bm, size=(4.0, 0.8, 0.12), loc=(0.0, -0.4, 3.18))
    # Vitrine de vidro
    add_box(bm, size=(1.8, 0.02, 2.85), loc=(-0.7, 0.0, 1.68))
    # Moldura central
    add_box(bm, size=(0.06, 0.12, 2.9), loc=(0.2, 0.0, 1.7))
    # Porta comercial
    add_box(bm, size=(1.35, 0.04, 2.85), loc=(0.9, 0.0, 1.68))
    # Puxador tubular inox
    add_cylinder(bm, radius=0.02, depth=0.9, segments=12, loc=(0.35, -0.06, 1.35))
    
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.012)
    return obj

def build_city_wall_brick():
    mesh = bpy.data.meshes.new("City_Wall_Brick_Residential_Mesh")
    obj = bpy.data.objects.new("City_Wall_Brick_Residential", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    add_box(bm, size=(4.0, 0.30, 4.0), loc=(0.0, 0.0, 2.0))
    for jx in [-1.0, 1.0]:
        add_box(bm, size=(1.3, 0.36, 1.7), loc=(jx, 0.0, 1.9))
        add_box(bm, size=(1.45, 0.42, 0.12), loc=(jx, 0.0, 1.04))
        add_box(bm, size=(1.05, 0.04, 1.5), loc=(jx, 0.0, 1.9))
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.015)
    return obj

def build_city_wall_glass():
    mesh = bpy.data.meshes.new("City_Wall_Modern_Glass_Mesh")
    obj = bpy.data.objects.new("City_Wall_Modern_Glass", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    add_box(bm, size=(4.0, 0.06, 4.0), loc=(0.0, 0.0, 2.0))
    for mx in [-2.0, -1.0, 0.0, 1.0, 2.0]:
        add_box(bm, size=(0.08, 0.16, 4.0), loc=(mx, 0.0, 2.0))
    for hz in [1.33, 2.66]:
        add_box(bm, size=(4.0, 0.12, 0.06), loc=(0.0, 0.0, hz))
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.01)
    return obj

def build_city_roof_parapet():
    mesh = bpy.data.meshes.new("City_Roof_Parapet_4m_Mesh")
    obj = bpy.data.objects.new("City_Roof_Parapet_4m", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    add_box(bm, size=(4.0, 4.0, 0.25), loc=(0.0, 0.0, 0.125))
    add_box(bm, size=(4.0, 0.30, 0.80), loc=(0.0, -1.85, 0.65))
    add_box(bm, size=(4.05, 0.36, 0.06), loc=(0.0, -1.85, 1.08))
    
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.015)
    return obj

def build_city_prop_streetlight():
    mesh = bpy.data.meshes.new("City_Prop_StreetLight_Modern_Mesh")
    obj = bpy.data.objects.new("City_Prop_StreetLight_Modern", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    add_cylinder(bm, radius=0.25, depth=0.15, segments=16, loc=(0.0, 0.0, 0.075))
    add_cone(bm, radius1=0.14, radius2=0.07, depth=5.5, segments=16, loc=(0.0, 0.0, 2.85))
    add_box(bm, size=(0.08, 1.6, 0.08), loc=(0.0, 0.8, 5.9), rot=(math.radians(22), 0, 0))
    add_box(bm, size=(0.32, 0.75, 0.12), loc=(0.0, 1.65, 6.16))
    add_box(bm, size=(0.26, 0.65, 0.02), loc=(0.0, 1.65, 6.09))
    
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.0)
    return obj

def build_city_prop_bus_shelter():
    mesh = bpy.data.meshes.new("City_Prop_Bus_Shelter_Mesh")
    obj = bpy.data.objects.new("City_Prop_Bus_Shelter", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    for px in [-1.4, 0.4]:
        add_cylinder(bm, radius=0.06, depth=2.4, segments=16, loc=(px, 0.6, 1.2))
    add_box(bm, size=(3.4, 1.7, 0.08), loc=(0.0, 0.0, 2.42))
    add_box(bm, size=(1.8, 0.02, 2.0), loc=(-0.5, 0.6, 1.2))
    add_box(bm, size=(0.18, 1.2, 2.2), loc=(1.25, 0.0, 1.2))
    add_box(bm, size=(1.6, 0.4, 0.06), loc=(-0.5, 0.3, 0.48))
    
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.01)
    return obj

def build_city_prop_trash_bin():
    mesh = bpy.data.meshes.new("City_Prop_TrashBin_Duo_Mesh")
    obj = bpy.data.objects.new("City_Prop_TrashBin_Duo", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    for bx in [-0.32, 0.32]:
        add_cylinder(bm, radius=0.24, depth=0.85, segments=20, loc=(bx, 0.0, 0.45))
        add_cone(bm, radius1=0.26, radius2=0.15, depth=0.14, segments=20, loc=(bx, 0.0, 0.92))
    add_box(bm, size=(0.95, 0.12, 0.06), loc=(0.0, 0.0, 0.03))
    
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.0)
    return obj

def build_city_prop_park_bench():
    mesh = bpy.data.meshes.new("City_Prop_Park_Bench_Mesh")
    obj = bpy.data.objects.new("City_Prop_Park_Bench", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    for fx in [-0.75, 0.75]:
        add_box(bm, size=(0.06, 0.55, 0.44), loc=(fx, 0.0, 0.22))
        add_box(bm, size=(0.06, 0.06, 0.45), loc=(fx, 0.22, 0.66))
    for ry in [-0.20, -0.07, 0.06, 0.19]:
        add_box(bm, size=(1.8, 0.10, 0.035), loc=(0.0, ry, 0.45))
    for rz in [0.55, 0.68, 0.81]:
        add_box(bm, size=(1.8, 0.035, 0.10), loc=(0.0, 0.25, rz))
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.01)
    return obj

def build_city_prop_fire_hydrant():
    mesh = bpy.data.meshes.new("City_Prop_Fire_Hydrant_Mesh")
    obj = bpy.data.objects.new("City_Prop_Fire_Hydrant", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    add_cylinder(bm, radius=0.22, depth=0.08, segments=16, loc=(0.0, 0.0, 0.04))
    add_cylinder(bm, radius=0.15, depth=0.62, segments=20, loc=(0.0, 0.0, 0.38))
    add_sphere(bm, radius=0.16, loc=(0.0, 0.0, 0.69))
    add_cylinder(bm, radius=0.07, depth=0.12, segments=16, loc=(0.0, -0.18, 0.48), rot=(math.pi/2, 0, 0))
    for bx in [-0.18, 0.18]:
        add_cylinder(bm, radius=0.05, depth=0.10, segments=14, loc=(bx, 0.0, 0.48), rot=(0, math.pi/2, 0))
        
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.0)
    return obj

def build_city_prop_bollard():
    mesh = bpy.data.meshes.new("City_Prop_Bollard_Concrete_Mesh")
    obj = bpy.data.objects.new("City_Prop_Bollard_Concrete", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    
    add_cylinder(bm, radius=0.14, depth=0.85, segments=20, loc=(0.0, 0.0, 0.425))
    add_sphere(bm, radius=0.14, loc=(0.0, 0.0, 0.85))
    add_cylinder(bm, radius=0.145, depth=0.06, segments=20, loc=(0.0, 0.0, 0.72), cap_ends=False)
    
    bm.to_mesh(mesh)
    bm.free()
    finalize_object(obj, 0.0)
    return obj

# =========================================================================
# EXPORTAÇÃO E RENDERS SHOWCASE
# =========================================================================

def export_object_fbx(obj, filepath):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        mesh_smooth_type='FACE',
        bake_space_transform=True
    )
    print(f"Exportado FBX: {filepath}")

def setup_lighting_and_camera(cam_pos, cam_rot):
    light_data = bpy.data.lights.new(name="Sun", type='SUN')
    light_data.energy = 4.5
    light_data.angle = math.radians(3.5)
    light_data.color = (1.0, 0.96, 0.90)
    light_obj = bpy.data.objects.new(name="Sun", object_data=light_data)
    bpy.context.scene.collection.objects.link(light_obj)
    light_obj.rotation_euler = (math.radians(48), math.radians(18), math.radians(-38))
    
    sky_data = bpy.data.lights.new(name="SkyFill", type='SUN')
    sky_data.energy = 1.2
    sky_data.color = (0.75, 0.85, 1.0)
    sky_obj = bpy.data.objects.new(name="SkyFill", object_data=sky_data)
    bpy.context.scene.collection.objects.link(sky_obj)
    sky_obj.rotation_euler = (math.radians(75), math.radians(-45), math.radians(120))
    
    cam_data = bpy.data.cameras.new(name="Showcase_Camera")
    cam_data.lens = 40
    cam_obj = bpy.data.objects.new(name="Showcase_Camera", object_data=cam_data)
    bpy.context.scene.collection.objects.link(cam_obj)
    cam_obj.location = cam_pos
    cam_obj.rotation_euler = cam_rot
    bpy.context.scene.camera = cam_obj
    
    scene = bpy.context.scene
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100

def run_farm_suite():
    print("\n==========================================")
    print("CONSTRUINDO SUÍTE MODULAR DE FAZENDA")
    print("==========================================")
    clean_scene()
    
    mat_wood = create_pbr_material("M_Barn_Wood", 
        os.path.join(FARM_TEX_DIR, "T_Barn_Wood_Albedo.png"),
        os.path.join(FARM_TEX_DIR, "T_Barn_Wood_Normal.png"),
        os.path.join(FARM_TEX_DIR, "T_Barn_Wood_Roughness.png"),
        os.path.join(FARM_TEX_DIR, "T_Barn_Wood_Metallic.png"))
        
    mat_metal = create_pbr_material("M_Corrugated_Metal",
        os.path.join(FARM_TEX_DIR, "T_Corrugated_Metal_Albedo.png"),
        os.path.join(FARM_TEX_DIR, "T_Corrugated_Metal_Normal.png"),
        os.path.join(FARM_TEX_DIR, "T_Corrugated_Metal_Roughness.png"),
        os.path.join(FARM_TEX_DIR, "T_Corrugated_Metal_Metallic.png"))
        
    mat_props = create_pbr_material("M_Farm_Props",
        os.path.join(FARM_TEX_DIR, "T_Farm_Props_Albedo.png"),
        os.path.join(FARM_TEX_DIR, "T_Farm_Props_Normal.png"),
        os.path.join(FARM_TEX_DIR, "T_Farm_Props_Roughness.png"),
        os.path.join(FARM_TEX_DIR, "T_Farm_Props_Metallic.png"))
        
    w_wood = build_farm_wall_wood()
    w_wood.data.materials.append(mat_wood)
    
    w_door = build_farm_wall_door()
    w_door.data.materials.append(mat_wood)
    
    w_win = build_farm_wall_window()
    w_win.data.materials.append(mat_wood)
    
    r_straight = build_farm_roof_straight()
    r_straight.data.materials.append(mat_metal)
    
    r_gable = build_farm_roof_gable()
    r_gable.data.materials.append(mat_wood)
    
    silo = build_farm_silo()
    silo.data.materials.append(mat_metal)
    
    fence = build_farm_fence()
    fence.data.materials.append(mat_props)
    
    gate = build_farm_fence_gate()
    gate.data.materials.append(mat_props)
    
    hay = build_farm_prop_hay_bale()
    hay.data.materials.append(mat_props)
    
    barrel = build_farm_prop_barrel()
    barrel.data.materials.append(mat_props)
    
    trough = build_farm_prop_trough()
    trough.data.materials.append(mat_props)
    
    pallet = build_farm_prop_pallet()
    pallet.data.materials.append(mat_props)
    
    farm_items = [
        (w_wood, "Farm_Wall_Wood_Planks.fbx"),
        (w_door, "Farm_Wall_Door_Double.fbx"),
        (w_win, "Farm_Wall_Window.fbx"),
        (r_straight, "Farm_Roof_Straight.fbx"),
        (r_gable, "Farm_Roof_Gable_End.fbx"),
        (silo, "Farm_Silo_Modular.fbx"),
        (fence, "Farm_Fence_Wood.fbx"),
        (gate, "Farm_Fence_Gate.fbx"),
        (hay, "Farm_Prop_Hay_Bale.fbx"),
        (barrel, "Farm_Prop_Wooden_Barrel.fbx"),
        (trough, "Farm_Prop_Trough.fbx"),
        (pallet, "Farm_Prop_Pallet_Stack.fbx")
    ]
    for obj, filename in farm_items:
        export_object_fbx(obj, os.path.join(FARM_MODELS_DIR, filename))
        
    # Organizar para Showcase
    w_wood.location = (-6.0, 0, 0)
    w_door.location = (-2.0, 0, 0)
    w_win.location = (2.0, 0, 0)
    r_straight.location = (-2.0, 0, 3.5)
    r_gable.location = (-6.0, -2.0, 3.5)
    silo.location = (6.5, 1.5, 0)
    fence.location = (-5.5, -4.0, 0)
    gate.location = (-2.5, -4.0, 0)
    hay.location = (1.5, -2.5, 0)
    barrel.location = (3.2, -2.2, 0)
    trough.location = (1.8, -4.0, 0)
    pallet.location = (4.5, -2.5, 0)
    
    blend_path = os.path.join(FARM_MODELS_DIR, "Farm_Modular_Collection.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Blend de Fazenda salvo: {blend_path}")
    
    cam_pos = (-1.0, -12.5, 5.5)
    cam_rot = (math.radians(68), 0, math.radians(5))
    setup_lighting_and_camera(cam_pos, cam_rot)
    
    render_out = os.path.join(PROJECT_ROOT, "Farm_Modular_Showcase.png")
    bpy.context.scene.render.filepath = render_out
    bpy.ops.render.render(write_still=True)
    print(f"Render Showcase de Fazenda salvo: {render_out}")

def run_city_suite():
    print("\n==========================================")
    print("CONSTRUINDO SUÍTE MODULAR DE CIDADE")
    print("==========================================")
    clean_scene()
    
    mat_sidewalk = create_pbr_material("M_City_Sidewalk",
        os.path.join(CITY_TEX_DIR, "T_City_Sidewalk_Albedo.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Sidewalk_Normal.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Sidewalk_Roughness.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Sidewalk_Metallic.png"))
        
    mat_brick = create_pbr_material("M_City_Brick",
        os.path.join(CITY_TEX_DIR, "T_City_Brick_Albedo.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Brick_Normal.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Brick_Roughness.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Brick_Metallic.png"))
        
    mat_modern = create_pbr_material("M_City_Modern",
        os.path.join(CITY_TEX_DIR, "T_City_Modern_Albedo.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Modern_Normal.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Modern_Roughness.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Modern_Metallic.png"))
        
    mat_city_props = create_pbr_material("M_City_Props",
        os.path.join(CITY_TEX_DIR, "T_City_Props_Albedo.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Props_Normal.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Props_Roughness.png"),
        os.path.join(CITY_TEX_DIR, "T_City_Props_Metallic.png"))
        
    sw_straight = build_city_sidewalk_straight()
    sw_straight.data.materials.append(mat_sidewalk)
    
    sw_corner = build_city_sidewalk_corner()
    sw_corner.data.materials.append(mat_sidewalk)
    
    w_shop = build_city_wall_shopfront()
    w_shop.data.materials.append(mat_modern)
    
    w_brick = build_city_wall_brick()
    w_brick.data.materials.append(mat_brick)
    
    w_glass = build_city_wall_glass()
    w_glass.data.materials.append(mat_modern)
    
    r_parapet = build_city_roof_parapet()
    r_parapet.data.materials.append(mat_modern)
    
    prop_light = build_city_prop_streetlight()
    prop_light.data.materials.append(mat_city_props)
    
    prop_shelter = build_city_prop_bus_shelter()
    prop_shelter.data.materials.append(mat_city_props)
    
    prop_bin = build_city_prop_trash_bin()
    prop_bin.data.materials.append(mat_city_props)
    
    prop_bench = build_city_prop_park_bench()
    prop_bench.data.materials.append(mat_city_props)
    
    prop_hydrant = build_city_prop_fire_hydrant()
    prop_hydrant.data.materials.append(mat_city_props)
    
    prop_bollard = build_city_prop_bollard()
    prop_bollard.data.materials.append(mat_sidewalk)
    
    city_items = [
        (sw_straight, "City_Sidewalk_Straight_4m.fbx"),
        (sw_corner, "City_Sidewalk_Corner_4m.fbx"),
        (w_shop, "City_Wall_Concrete_ShopFront.fbx"),
        (w_brick, "City_Wall_Brick_Residential.fbx"),
        (w_glass, "City_Wall_Modern_Glass.fbx"),
        (r_parapet, "City_Roof_Parapet_4m.fbx"),
        (prop_light, "City_Prop_StreetLight_Modern.fbx"),
        (prop_shelter, "City_Prop_Bus_Shelter.fbx"),
        (prop_bin, "City_Prop_TrashBin_Duo.fbx"),
        (prop_bench, "City_Prop_Park_Bench.fbx"),
        (prop_hydrant, "City_Prop_Fire_Hydrant.fbx"),
        (prop_bollard, "City_Prop_Bollard_Concrete.fbx")
    ]
    for obj, filename in city_items:
        export_object_fbx(obj, os.path.join(CITY_MODELS_DIR, filename))
        
    sw_straight.location = (-2.0, 0, 0)
    sw_corner.location = (4.0, 0, 0)
    w_shop.location = (-2.0, -0.1, 0.2)
    w_brick.location = (2.0, -0.1, 0.2)
    w_glass.location = (-2.0, -0.1, 4.2)
    r_parapet.location = (2.0, 2.0, 4.2)
    prop_light.location = (-3.5, 1.8, 0.2)
    prop_shelter.location = (-0.5, 1.0, 0.2)
    prop_bin.location = (1.5, 1.8, 0.2)
    prop_bench.location = (3.0, 1.2, 0.2)
    prop_hydrant.location = (3.8, 2.0, 0.2)
    prop_bollard.location = (0.5, 2.0, 0.2)
    
    blend_path = os.path.join(CITY_MODELS_DIR, "City_Modular_Collection.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Blend de Cidade salvo: {blend_path}")
    
    cam_pos = (0.5, 8.5, 4.2)
    cam_rot = (math.radians(72), 0, math.radians(178))
    setup_lighting_and_camera(cam_pos, cam_rot)
    
    render_out = os.path.join(PROJECT_ROOT, "City_Modular_Showcase.png")
    bpy.context.scene.render.filepath = render_out
    bpy.ops.render.render(write_still=True)
    print(f"Render Showcase de Cidade salvo: {render_out}")

if __name__ == "__main__":
    run_farm_suite()
    run_city_suite()
    print("\n=== TODAS AS ESTRUTURAS, OBJETOS E RENDERS CONCLUÍDOS COM SUCESSO! ===")
