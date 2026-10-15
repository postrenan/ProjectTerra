"""
Procedural 3D World Traffic Signs & Special Distance Signs Generator for Blender 5.2.
Creates 26 distinct, high-fidelity road signs spanning international regional standards
(Vienna Convention, MUTCD Americas, Mercosul / Brazil, Asian / Japan & UK Commonwealth)
plus special signs:
 - Multi-city highway distance confirmation boards (Paris/Lyon, NY/DC, SP/Campinas)
 - Monumental overhead highway lattice truss gantry with dual lane boards & exit tab
 - Cantilever semi-gantry exit arm
 - Roadside concrete milestone marker (Marco Quilometrico)
 - Highway milepost paddle marker
 - Electronic LED Variable Message Sign (VMS / PMV) with amber matrix display & flashers
 - Monumental city gateway welcome sign with stone masonry plinth & floodlights

Exports all individual models as FBX into Unity project structure and saves master .blend.
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix, Euler

# -----------------------------------------------------------------------------
# Paths & Config
# -----------------------------------------------------------------------------
PROJECT_DIR = r"E:\Backup-C\Projetos\testejogo"
BLEND_FILE_NAME = "World_Traffic_Signs_Collection.blend"
BLEND_FILE_PATH = os.path.join(PROJECT_DIR, BLEND_FILE_NAME)

ASSETS_SIGNS_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "Structures", "TrafficSigns")
ASSETS_BLEND_PATH = os.path.join(ASSETS_SIGNS_DIR, BLEND_FILE_NAME)
TEXTURES_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Textures", "TrafficSigns")
RENDER_OUTPUT_PATH = os.path.join(PROJECT_DIR, "traffic_signs_showcase.png")

os.makedirs(ASSETS_SIGNS_DIR, exist_ok=True)

# -----------------------------------------------------------------------------
# Scene Reset
# -----------------------------------------------------------------------------
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = 'METERS'
    
    # View transform
    if 'AgX' in bpy.types.ColorManagedViewSettings.bl_rna.properties['view_transform'].enum_items:
        scene.view_settings.view_transform = 'AgX'
    else:
        scene.view_settings.view_transform = 'Filmic'
    scene.view_settings.look = 'Medium High Contrast'
    
    # World lighting
    world = bpy.data.worlds.new("World_Sky")
    scene.world = world
    world.use_nodes = True
    wnodes = world.node_tree.nodes
    wnodes.clear()
    w_bg = wnodes.new('ShaderNodeBackground')
    w_bg.inputs['Color'].default_value = (0.75, 0.82, 0.90, 1.0)
    w_bg.inputs['Strength'].default_value = 0.9
    w_out = wnodes.new('ShaderNodeOutputWorld')
    world.node_tree.links.new(w_bg.outputs['Background'], w_out.inputs['Surface'])

# -----------------------------------------------------------------------------
# Material Management
# -----------------------------------------------------------------------------
def get_or_create_mat(name, base_color=(0.8, 0.8, 0.8, 1.0), metallic=0.0, roughness=0.5,
                      emission_color=None, emission_strength=0.0):
    mat = bpy.data.materials.get(name)
    if not mat:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = base_color
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if emission_color and "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = emission_color
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat

def create_textured_mat(name, texture_filename, roughness=0.35, metallic=0.0, is_emissive=False, emission_strength=2.0):
    mat = bpy.data.materials.get(name)
    if not mat:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    
    tex_path = os.path.join(TEXTURES_DIR, texture_filename)
    if os.path.exists(tex_path):
        img = bpy.data.images.load(filepath=tex_path, check_existing=True)
        tex_node = nodes.new('ShaderNodeTexImage')
        tex_node.image = img
        tex_node.location = (-300, 300)
        
        # Link to Base Color
        links.new(tex_node.outputs['Color'], bsdf.inputs['Base Color'])
        if "Alpha" in bsdf.inputs:
            links.new(tex_node.outputs['Alpha'], bsdf.inputs['Alpha'])
            
        if is_emissive and "Emission Color" in bsdf.inputs:
            links.new(tex_node.outputs['Color'], bsdf.inputs['Emission Color'])
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission_strength
                
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = metallic
    if "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = roughness
        
    return mat

def setup_materials():
    mats = {
        # Hardware & structural
        "steel_galvanized": get_or_create_mat("Mat_Steel_Galvanized", (0.75, 0.77, 0.80, 1.0), 0.95, 0.28),
        "steel_dark_post": get_or_create_mat("Mat_Steel_Dark_Unistrut", (0.28, 0.30, 0.32, 1.0), 0.85, 0.42),
        "steel_black_trim": get_or_create_mat("Mat_Steel_Black_Trim", (0.12, 0.12, 0.13, 1.0), 0.7, 0.35),
        "aluminum_back": get_or_create_mat("Mat_Sign_Aluminum_Back", (0.72, 0.74, 0.76, 1.0), 0.90, 0.30),
        "concrete_base": get_or_create_mat("Mat_Concrete_Footing", (0.58, 0.58, 0.60, 1.0), 0.0, 0.88),
        "bolts_hardware": get_or_create_mat("Mat_Bolts_Hardware", (0.86, 0.87, 0.90, 1.0), 0.98, 0.15),
        "zebra_wrap": get_or_create_mat("Mat_Zebra_Warning", (0.95, 0.75, 0.05, 1.0), 0.1, 0.4),
        # Special structures
        "stone_masonry": get_or_create_mat("Mat_Stone_Masonry", (0.44, 0.41, 0.38, 1.0), 0.0, 0.85),
        "wood_trim": get_or_create_mat("Mat_Wood_Trim", (0.35, 0.22, 0.12, 1.0), 0.0, 0.45),
        "led_housing": get_or_create_mat("Mat_LED_Housing", (0.08, 0.08, 0.09, 1.0), 0.3, 0.45),
        "amber_strobe": get_or_create_mat("Mat_Amber_Strobe", (1.0, 0.65, 0.05, 1.0), 0.0, 0.1,
                                          emission_color=(1.0, 0.65, 0.05, 1.0), emission_strength=10.0),
        "showcase_pedestal": get_or_create_mat("Mat_Showcase_Pedestal", (0.10, 0.10, 0.12, 1.0), 0.2, 0.5),
        "showcase_road": get_or_create_mat("Mat_Showcase_Road", (0.15, 0.15, 0.16, 1.0), 0.0, 0.82),
        "showcase_curb": get_or_create_mat("Mat_Showcase_Curb", (0.85, 0.85, 0.85, 1.0), 0.0, 0.6),
        "text_gold": get_or_create_mat("Mat_Text_Gold", (0.96, 0.82, 0.26, 1.0), 0.88, 0.18),
        
        # Textured sign faces
        "tex_eu_stop": create_textured_mat("Mat_Sign_EU_Stop", "tex_sign_eu_stop.png"),
        "tex_eu_yield": create_textured_mat("Mat_Sign_EU_Yield", "tex_sign_eu_yield.png"),
        "tex_eu_curve": create_textured_mat("Mat_Sign_EU_Curve", "tex_sign_eu_curve.png"),
        "tex_eu_speed80": create_textured_mat("Mat_Sign_EU_Speed80", "tex_sign_eu_speed80.png"),
        "tex_eu_no_entry": create_textured_mat("Mat_Sign_EU_NoEntry", "tex_sign_eu_no_entry.png"),
        "tex_eu_roundabout": create_textured_mat("Mat_Sign_EU_Roundabout", "tex_sign_eu_roundabout.png"),
        "tex_eu_motorway": create_textured_mat("Mat_Sign_EU_Motorway", "tex_sign_eu_motorway.png"),
        "tex_us_curve": create_textured_mat("Mat_Sign_US_Curve", "tex_sign_us_curve.png"),
        "tex_us_speed65": create_textured_mat("Mat_Sign_US_Speed65", "tex_sign_us_speed65.png"),
        "tex_us_oneway": create_textured_mat("Mat_Sign_US_OneWay", "tex_sign_us_oneway.png"),
        "tex_us_interstate": create_textured_mat("Mat_Sign_US_Interstate", "tex_sign_us_interstate.png"),
        "tex_br_lombada": create_textured_mat("Mat_Sign_BR_Lombada", "tex_sign_br_lombada.png"),
        "tex_br_pare": create_textured_mat("Mat_Sign_BR_Pare", "tex_sign_br_pare.png"),
        "tex_br_shield": create_textured_mat("Mat_Sign_BR_Shield", "tex_sign_br_shield.png"),
        "tex_jp_stop": create_textured_mat("Mat_Sign_JP_Stop", "tex_sign_jp_stop.png"),
        "tex_uk_speedlimit": create_textured_mat("Mat_Sign_UK_SpeedLimit", "tex_sign_uk_speedlimit.png"),
        "tex_au_kangaroo": create_textured_mat("Mat_Sign_AU_Kangaroo", "tex_sign_au_kangaroo.png"),
        "tex_dist_paris": create_textured_mat("Mat_Sign_Dist_Paris", "tex_sign_dist_paris.png"),
        "tex_dist_newyork": create_textured_mat("Mat_Sign_Dist_NewYork", "tex_sign_dist_newyork.png"),
        "tex_dist_saopaulo": create_textured_mat("Mat_Sign_Dist_SaoPaulo", "tex_sign_dist_saopaulo.png"),
        "tex_gantry_left": create_textured_mat("Mat_Sign_Gantry_Left", "tex_sign_gantry_left.png"),
        "tex_gantry_right": create_textured_mat("Mat_Sign_Gantry_Right", "tex_sign_gantry_right.png"),
        "tex_gantry_tab": create_textured_mat("Mat_Sign_Gantry_Tab", "tex_sign_gantry_tab.png"),
        "tex_cantilever": create_textured_mat("Mat_Sign_Cantilever", "tex_sign_cantilever.png"),
        "tex_milestone": create_textured_mat("Mat_Sign_Milestone", "tex_sign_milestone.png"),
        "tex_paddle": create_textured_mat("Mat_Sign_Paddle", "tex_sign_paddle.png"),
        "tex_vms_led": create_textured_mat("Mat_Sign_VMS_LED", "tex_sign_vms_led.png", is_emissive=True, emission_strength=4.5),
        "tex_city_welcome": create_textured_mat("Mat_Sign_City_Welcome", "tex_sign_city_welcome.png"),
    }
    return mats

# -----------------------------------------------------------------------------
# Mesh Construction Helpers (BMesh)
# -----------------------------------------------------------------------------
class SignBuilder:
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.materials = []
        self.mat_map = {}

    def get_mat_idx(self, mat):
        if mat not in self.mat_map:
            self.mat_map[mat] = len(self.materials)
            self.materials.append(mat)
        return self.mat_map[mat]

    def add_cylinder(self, r, h, segments=16, center=Vector((0, 0, 0)), axis='Z', mat=None):
        mat_idx = self.get_mat_idx(mat) if mat else 0
        verts_bot = []
        verts_top = []
        for i in range(segments):
            ang = 2 * math.pi * i / segments
            c = math.cos(ang) * r
            s = math.sin(ang) * r
            if axis == 'Z':
                vb = self.bm.verts.new(center + Vector((c, s, -h / 2)))
                vt = self.bm.verts.new(center + Vector((c, s, h / 2)))
            elif axis == 'Y':
                vb = self.bm.verts.new(center + Vector((c, -h / 2, s)))
                vt = self.bm.verts.new(center + Vector((c, h / 2, s)))
            else: # 'X'
                vb = self.bm.verts.new(center + Vector((-h / 2, c, s)))
                vt = self.bm.verts.new(center + Vector((h / 2, c, s)))
            verts_bot.append(vb)
            verts_top.append(vt)

        # Side faces
        for i in range(segments):
            next_i = (i + 1) % segments
            f = self.bm.faces.new([verts_bot[i], verts_bot[next_i], verts_top[next_i], verts_top[i]])
            f.material_index = mat_idx

        # Caps
        fb = self.bm.faces.new(list(reversed(verts_bot)))
        fb.material_index = mat_idx
        ft = self.bm.faces.new(verts_top)
        ft.material_index = mat_idx

    def add_box(self, size, center=Vector((0, 0, 0)), mat=None):
        mat_idx = self.get_mat_idx(mat) if mat else 0
        sx, sy, sz = size.x / 2, size.y / 2, size.z / 2
        cx, cy, cz = center.x, center.y, center.z
        v = [
            self.bm.verts.new((cx - sx, cy - sy, cz - sz)),
            self.bm.verts.new((cx + sx, cy - sy, cz - sz)),
            self.bm.verts.new((cx + sx, cy + sy, cz - sz)),
            self.bm.verts.new((cx - sx, cy + sy, cz - sz)),
            self.bm.verts.new((cx - sx, cy - sy, cz + sz)),
            self.bm.verts.new((cx + sx, cy - sy, cz + sz)),
            self.bm.verts.new((cx + sx, cy + sy, cz + sz)),
            self.bm.verts.new((cx - sx, cy + sy, cz + sz)),
        ]
        faces = [
            [v[0], v[1], v[2], v[3]], # Bottom -Z
            [v[4], v[7], v[6], v[5]], # Top +Z
            [v[0], v[4], v[5], v[1]], # Front -Y
            [v[2], v[6], v[7], v[3]], # Back +Y
            [v[0], v[3], v[7], v[4]], # Left -X
            [v[1], v[5], v[6], v[2]], # Right +X
        ]
        for fv in faces:
            f = self.bm.faces.new(fv)
            f.material_index = mat_idx

    def add_i_beam(self, w, d, h, tf, tw, center=Vector((0, 0, 0)), mat=None):
        # I-beam / H-beam along Z axis
        # Flanges along X, web along Y
        cz = center.z
        # Bottom flange
        self.add_box(Vector((w, tf, h)), center=center + Vector((0, -d/2 + tf/2, 0)), mat=mat)
        # Top flange
        self.add_box(Vector((w, tf, h)), center=center + Vector((0, d/2 - tf/2, 0)), mat=mat)
        # Web
        self.add_box(Vector((tw, d - 2*tf, h)), center=center, mat=mat)

    def add_textured_planar_sign(self, pts_2d, thickness=0.004, center=Vector((0, 0, 0)),
                                 face_mat=None, back_mat=None, edge_mat=None):
        """
        pts_2d: list of (x, z) relative coordinates defining the polygon perimeter.
        Extrudes along Y axis from -thickness to 0 (so front face is at Y=0).
        Maps UV of front face perfectly to [0, 1].
        """
        face_idx = self.get_mat_idx(face_mat)
        back_idx = self.get_mat_idx(back_mat if back_mat else face_mat)
        edge_idx = self.get_mat_idx(edge_mat if edge_mat else back_mat)

        uv_layer = self.bm.loops.layers.uv.verify()

        # Find bounds for UV mapping
        min_x = min(p[0] for p in pts_2d)
        max_x = max(p[0] for p in pts_2d)
        min_z = min(p[1] for p in pts_2d)
        max_z = max(p[1] for p in pts_2d)
        dx = max(max_x - min_x, 1e-5)
        dz = max(max_z - min_z, 1e-5)

        verts_front = []
        verts_back = []
        for x, z in pts_2d:
            vf = self.bm.verts.new(center + Vector((x, 0.0, z)))
            vb = self.bm.verts.new(center + Vector((x, thickness, z)))
            verts_front.append(vf)
            verts_back.append(vb)

        # Front Face (normal points towards -Y, which is the viewer in front of the sign)
        # To face towards -Y, loop must be clockwise in XZ plane
        front_face = self.bm.faces.new(list(reversed(verts_front)))
        front_face.material_index = face_idx
        for loop in front_face.loops:
            vx = loop.vert.co.x - center.x
            vz = loop.vert.co.z - center.z
            u = (vx - min_x) / dx
            v = (vz - min_z) / dz
            loop[uv_layer].uv = Vector((u, v))

        # Back Face (normal points towards +Y)
        back_face = self.bm.faces.new(verts_back)
        back_face.material_index = back_idx
        for loop in back_face.loops:
            loop[uv_layer].uv = Vector((0.5, 0.5))

        # Side / Rim Faces
        n = len(pts_2d)
        for i in range(n):
            next_i = (i + 1) % n
            # Quad between front and back
            side_f = self.bm.faces.new([verts_front[i], verts_front[next_i], verts_back[next_i], verts_back[i]])
            side_f.material_index = edge_idx
            for loop in side_f.loops:
                loop[uv_layer].uv = Vector((0.1, 0.1))

    def build_object(self):
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.context.collection.objects.link(obj)
        for m in self.materials:
            obj.data.materials.append(m)
        return obj

# -----------------------------------------------------------------------------
# Common Hardware Assembly Functions
# -----------------------------------------------------------------------------
def add_standard_pole(builder, height=2.4, r=0.038, has_footing=True, mats=None):
    # Galvanized round pole
    builder.add_cylinder(r, height, segments=16, center=Vector((0, 0.05, height / 2)), axis='Z', mat=mats["steel_galvanized"])
    # Cap on top of pole
    builder.add_cylinder(r * 1.1, 0.03, segments=16, center=Vector((0, 0.05, height)), axis='Z', mat=mats["steel_black_trim"])
    # Concrete base footing at ground
    if has_footing:
        builder.add_box(Vector((0.32, 0.32, 0.15)), center=Vector((0, 0.05, 0.075)), mat=mats["concrete_base"])
        # Steel mounting base plate & bolts
        builder.add_box(Vector((0.20, 0.20, 0.015)), center=Vector((0, 0.05, 0.155)), mat=mats["steel_galvanized"])
        for bx in [-0.07, 0.07]:
            for by in [-0.07, 0.07]:
                builder.add_cylinder(0.012, 0.04, segments=8, center=Vector((bx, 0.05 + by, 0.17)), axis='Z', mat=mats["bolts_hardware"])

def add_unistrut_post(builder, height=2.3, size=0.05, mats=None):
    # Perforated square telespar post
    builder.add_box(Vector((size, size, height)), center=Vector((0, 0.05, height / 2)), mat=mats["steel_dark_post"])
    # Square base sleeve
    builder.add_box(Vector((size * 1.3, size * 1.3, 0.45)), center=Vector((0, 0.05, 0.225)), mat=mats["steel_galvanized"])
    # Concrete footing
    builder.add_box(Vector((0.30, 0.30, 0.15)), center=Vector((0, 0.05, 0.075)), mat=mats["concrete_base"])
    # Fastener bolts
    builder.add_cylinder(0.01, 0.08, segments=8, center=Vector((0, 0.05, 0.35)), axis='Y', mat=mats["bolts_hardware"])

def add_rear_mounting_brackets(builder, center_z, span_z=0.35, mats=None):
    # Two horizontal mounting channels across back of sign plate
    for z_off in [-span_z / 2, span_z / 2]:
        cz = center_z + z_off
        # Aluminum channel
        builder.add_box(Vector((0.40, 0.025, 0.035)), center=Vector((0, 0.02, cz)), mat=mats["aluminum_back"])
        # U-clamp hugging pole
        builder.add_cylinder(0.042, 0.02, segments=12, center=Vector((0, 0.05, cz)), axis='Z', mat=mats["bolts_hardware"])
        # Clamp hex nuts
        builder.add_cylinder(0.012, 0.03, segments=6, center=Vector((0.05, 0.07, cz)), axis='Y', mat=mats["bolts_hardware"])
        builder.add_cylinder(0.012, 0.03, segments=6, center=Vector((-0.05, 0.07, cz)), axis='Y', mat=mats["bolts_hardware"])

# -----------------------------------------------------------------------------
# Polygon Generators
# -----------------------------------------------------------------------------
def make_circle_pts(radius, segments=32):
    pts = []
    for i in range(segments):
        ang = 2 * math.pi * i / segments
        pts.append((math.cos(ang) * radius, math.sin(ang) * radius))
    return pts

def make_octagon_pts(radius):
    pts = []
    for i in range(8):
        ang = math.radians(22.5 + i * 45)
        pts.append((math.cos(ang) * radius, math.sin(ang) * radius))
    return pts

def make_triangle_pts(side, inverted=False):
    h = side * (math.sqrt(3) / 2)
    rc = h * (2 / 3)
    rin = h * (1 / 3)
    if not inverted:
        return [(0, rc), (side / 2, -rin), (-side / 2, -rin)]
    else:
        return [(-side / 2, rin), (side / 2, rin), (0, -rc)]

def make_diamond_pts(size):
    s = size / 2.0
    return [(0, s), (s, 0), (0, -s), (-s, 0)]

def make_rect_pts(w, h):
    hw, hh = w / 2, h / 2
    return [(-hw, hh), (hw, hh), (hw, -hh), (-hw, -hh)]

def make_shield_pts(w, h):
    hw, hh = w / 2, h / 2
    return [
        (0, hh * 0.9), (hw * 0.4, hh), (hw, hh * 0.75),
        (hw * 0.85, -hh * 0.4), (0, -hh), (-hw * 0.85, -hh * 0.4),
        (-hw, hh * 0.75), (-hw * 0.4, hh)
    ]

# =============================================================================
# SIGN BUILDERS
# =============================================================================

# 1. European / International Signs
def build_sign_eu_stop(mats):
    b = SignBuilder("Sign_EU_Stop_Octagon")
    add_standard_pole(b, height=2.45, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.35, mats=mats)
    pts = make_octagon_pts(0.38)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_eu_stop"], back_mat=mats["aluminum_back"])
    return b.build_object()

def build_sign_eu_yield(mats):
    b = SignBuilder("Sign_EU_Yield_Triangle")
    add_standard_pole(b, height=2.45, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.30, mats=mats)
    pts = make_triangle_pts(0.85, inverted=True)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_eu_yield"], back_mat=mats["aluminum_back"])
    return b.build_object()

def build_sign_eu_curve(mats):
    b = SignBuilder("Sign_EU_Warning_Curve_Triangle")
    add_standard_pole(b, height=2.45, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.30, mats=mats)
    pts = make_triangle_pts(0.85, inverted=False)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_eu_curve"], back_mat=mats["aluminum_back"])
    return b.build_object()

def build_sign_eu_speed80(mats):
    b = SignBuilder("Sign_EU_Speed_80_Circle")
    add_standard_pole(b, height=2.45, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.32, mats=mats)
    pts = make_circle_pts(0.35, segments=32)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_eu_speed80"], back_mat=mats["aluminum_back"])
    return b.build_object()

def build_sign_eu_no_entry(mats):
    b = SignBuilder("Sign_EU_No_Entry_Circle")
    add_standard_pole(b, height=2.45, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.32, mats=mats)
    pts = make_circle_pts(0.35, segments=32)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_eu_no_entry"], back_mat=mats["aluminum_back"])
    return b.build_object()

def build_sign_eu_roundabout(mats):
    b = SignBuilder("Sign_EU_Mandatory_Roundabout")
    add_standard_pole(b, height=2.45, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.32, mats=mats)
    pts = make_circle_pts(0.35, segments=32)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_eu_roundabout"], back_mat=mats["aluminum_back"])
    return b.build_object()

def build_sign_eu_motorway(mats):
    b = SignBuilder("Sign_EU_Motorway_Entry")
    add_standard_pole(b, height=2.55, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.45, mats=mats)
    pts = make_rect_pts(0.70, 0.95)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_eu_motorway"], back_mat=mats["aluminum_back"])
    return b.build_object()

# 2. Americas & Mercosul Signs
def build_sign_us_curve(mats):
    b = SignBuilder("Sign_US_Warning_Curve_Diamond")
    add_unistrut_post(b, height=2.45, mats=mats)
    pts = make_diamond_pts(0.75)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_us_curve"], back_mat=mats["aluminum_back"])
    # Front bolt heads
    b.add_cylinder(0.012, 0.01, segments=8, center=Vector((0, -0.006, 2.30)), axis='Y', mat=mats["bolts_hardware"])
    b.add_cylinder(0.012, 0.01, segments=8, center=Vector((0, -0.006, 2.00)), axis='Y', mat=mats["bolts_hardware"])
    return b.build_object()

def build_sign_us_speed65(mats):
    b = SignBuilder("Sign_US_Speed_Limit_65")
    add_unistrut_post(b, height=2.45, mats=mats)
    pts = make_rect_pts(0.60, 0.80)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_us_speed65"], back_mat=mats["aluminum_back"])
    b.add_cylinder(0.012, 0.01, segments=8, center=Vector((0, -0.006, 2.38)), axis='Y', mat=mats["bolts_hardware"])
    b.add_cylinder(0.012, 0.01, segments=8, center=Vector((0, -0.006, 1.92)), axis='Y', mat=mats["bolts_hardware"])
    return b.build_object()

def build_sign_us_oneway(mats):
    b = SignBuilder("Sign_US_One_Way_Arrow")
    add_unistrut_post(b, height=2.30, mats=mats)
    pts = make_rect_pts(0.90, 0.30)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_us_oneway"], back_mat=mats["aluminum_back"])
    b.add_cylinder(0.012, 0.01, segments=8, center=Vector((0, -0.006, 2.15)), axis='Y', mat=mats["bolts_hardware"])
    return b.build_object()

def build_sign_us_interstate(mats):
    b = SignBuilder("Sign_US_Interstate_Shield")
    add_standard_pole(b, height=2.45, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.30, mats=mats)
    pts = make_shield_pts(0.70, 0.70)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_us_interstate"], back_mat=mats["aluminum_back"])
    return b.build_object()

def build_sign_br_lombada(mats):
    b = SignBuilder("Sign_BR_Warning_Lombada_Diamond")
    add_standard_pole(b, height=2.45, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.35, mats=mats)
    pts = make_diamond_pts(0.75)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_br_lombada"], back_mat=mats["aluminum_back"])
    return b.build_object()

def build_sign_br_pare(mats):
    b = SignBuilder("Sign_BR_Regulatory_Pare")
    add_standard_pole(b, height=2.45, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.35, mats=mats)
    pts = make_octagon_pts(0.38)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_br_pare"], back_mat=mats["aluminum_back"])
    return b.build_object()

def build_sign_br_shield(mats):
    b = SignBuilder("Sign_BR_Highway_Shield")
    add_standard_pole(b, height=2.45, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.30, mats=mats)
    pts = make_shield_pts(0.65, 0.75)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_br_shield"], back_mat=mats["aluminum_back"])
    return b.build_object()

# 3. Asian & Commonwealth Signs
def build_sign_jp_stop(mats):
    b = SignBuilder("Sign_JP_Stop_Inverted_Triangle")
    add_standard_pole(b, height=2.45, mats=mats)
    # Yellow and black warning sleeve wrap at the bottom of pole (0.15m to 0.90m)
    b.add_cylinder(0.045, 0.75, segments=16, center=Vector((0, 0.05, 0.525)), axis='Z', mat=mats["zebra_wrap"])
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.30, mats=mats)
    pts = make_triangle_pts(0.85, inverted=True)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_jp_stop"], back_mat=mats["aluminum_back"])
    return b.build_object()

def build_sign_uk_speedlimit(mats):
    b = SignBuilder("Sign_UK_National_Speed_Limit")
    add_standard_pole(b, height=2.45, mats=mats)
    add_rear_mounting_brackets(b, center_z=2.15, span_z=0.32, mats=mats)
    pts = make_circle_pts(0.35, segments=32)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_uk_speedlimit"], back_mat=mats["aluminum_back"])
    return b.build_object()

def build_sign_au_kangaroo(mats):
    b = SignBuilder("Sign_AU_Wildlife_Kangaroo")
    add_unistrut_post(b, height=2.45, mats=mats)
    pts = make_diamond_pts(0.75)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, 0, 2.15)),
                               face_mat=mats["tex_au_kangaroo"], back_mat=mats["aluminum_back"])
    return b.build_object()

# =============================================================================
# 4. SPECIAL DISTANCE & CITY MILEAGE SIGNS (Placas Especiais de Distância)
# =============================================================================

def build_large_multi_city_distance_board(name, tex_mat, mats, post_type="i_beam"):
    b = SignBuilder(name)
    w, h, depth = 2.60, 1.70, 0.05
    board_center_z = 2.65 # clearance ~1.8m above ground
    
    # Signboard main box with extruded lip
    b.add_box(Vector((w, depth, h)), center=Vector((0, 0.025, board_center_z)), mat=mats["aluminum_back"])
    # Front face
    pts = make_rect_pts(w, h)
    b.add_textured_planar_sign(pts, thickness=0.005, center=Vector((0, 0, board_center_z)),
                               face_mat=tex_mat, back_mat=mats["aluminum_back"])
    # Frame perimeter trim
    frame_th = 0.03
    b.add_box(Vector((w + 2*frame_th, frame_th, frame_th)), center=Vector((0, 0.015, board_center_z + h/2 + frame_th/2)), mat=mats["steel_galvanized"])
    b.add_box(Vector((w + 2*frame_th, frame_th, frame_th)), center=Vector((0, 0.015, board_center_z - h/2 - frame_th/2)), mat=mats["steel_galvanized"])
    b.add_box(Vector((frame_th, frame_th, h)), center=Vector((-w/2 - frame_th/2, 0.015, board_center_z)), mat=mats["steel_galvanized"])
    b.add_box(Vector((frame_th, frame_th, h)), center=Vector((w/2 + frame_th/2, 0.015, board_center_z)), mat=mats["steel_galvanized"])
    
    # 3 Rear horizontal reinforcing C-channel beams
    for rz in [board_center_z - h*0.35, board_center_z, board_center_z + h*0.35]:
        b.add_box(Vector((w * 0.95, 0.05, 0.08)), center=Vector((0, 0.075, rz)), mat=mats["steel_galvanized"])
        
    # Dual heavy steel posts spaced 1.7m apart
    post_x = 0.85
    post_h = 3.60
    post_y = 0.16
    for px in [-post_x, post_x]:
        if post_type == "i_beam":
            b.add_i_beam(w=0.14, d=0.14, h=post_h, tf=0.014, tw=0.010,
                         center=Vector((px, post_y, post_h / 2)), mat=mats["steel_galvanized"])
        else: # square tube
            b.add_box(Vector((0.12, 0.12, post_h)), center=Vector((px, post_y, post_h / 2)), mat=mats["steel_galvanized"])
            
        # Concrete base footing (0.5m x 0.5m x 0.35m)
        b.add_box(Vector((0.55, 0.55, 0.35)), center=Vector((px, post_y, 0.175)), mat=mats["concrete_base"])
        # Base plate & 4 anchor bolts
        b.add_box(Vector((0.32, 0.32, 0.025)), center=Vector((px, post_y, 0.362)), mat=mats["steel_galvanized"])
        for bx in [-0.11, 0.11]:
            for by in [-0.11, 0.11]:
                b.add_cylinder(0.016, 0.06, segments=8, center=Vector((px + bx, post_y + by, 0.38)), axis='Z', mat=mats["bolts_hardware"])
                
        # Mounting clamp brackets connecting I-beams to sign channels
        for rz in [board_center_z - h*0.35, board_center_z, board_center_z + h*0.35]:
            b.add_box(Vector((0.18, 0.06, 0.10)), center=Vector((px, 0.10, rz)), mat=mats["bolts_hardware"])
            
    return b.build_object()

def build_sign_special_distance_paris(mats):
    return build_large_multi_city_distance_board("Sign_Special_Distance_Paris_Lyon", mats["tex_dist_paris"], mats, post_type="i_beam")

def build_sign_special_distance_newyork(mats):
    return build_large_multi_city_distance_board("Sign_Special_Distance_NewYork_DC", mats["tex_dist_newyork"], mats, post_type="i_beam")

def build_sign_special_distance_saopaulo(mats):
    return build_large_multi_city_distance_board("Sign_Special_Distance_SaoPaulo_Campinas", mats["tex_dist_saopaulo"], mats, post_type="square")

def build_sign_special_overhead_gantry(mats):
    b = SignBuilder("Sign_Special_Overhead_Highway_Gantry")
    span = 9.80
    height = 6.60
    chord_r = 0.05
    lattice_r = 0.025
    
    # 2 Support Towers (Left at -span/2, Right at +span/2)
    for col_x in [-span / 2, span / 2]:
        # 4 upright vertical tubular chords forming a 0.7m x 0.7m column
        for dx in [-0.35, 0.35]:
            for dy in [0.0, 0.70]:
                b.add_cylinder(chord_r, height, segments=12,
                               center=Vector((col_x + dx, dy, height / 2)), axis='Z', mat=mats["steel_galvanized"])
        # Cross lacing on towers every 0.8m
        for step_z in range(1, int(height / 0.8) + 1):
            cz = step_z * 0.8
            # horizontal struts
            b.add_box(Vector((0.70, 0.04, 0.04)), center=Vector((col_x, 0.0, cz)), mat=mats["steel_galvanized"])
            b.add_box(Vector((0.70, 0.04, 0.04)), center=Vector((col_x, 0.70, cz)), mat=mats["steel_galvanized"])
            b.add_box(Vector((0.04, 0.70, 0.04)), center=Vector((col_x - 0.35, 0.35, cz)), mat=mats["steel_galvanized"])
            b.add_box(Vector((0.04, 0.70, 0.04)), center=Vector((col_x + 0.35, 0.35, cz)), mat=mats["steel_galvanized"])
            
        # Heavy concrete foundations (1.2m x 1.2m x 0.5m)
        b.add_box(Vector((1.20, 1.20, 0.50)), center=Vector((col_x, 0.35, 0.25)), mat=mats["concrete_base"])
        b.add_box(Vector((0.95, 0.95, 0.03)), center=Vector((col_x, 0.35, 0.515)), mat=mats["steel_galvanized"])
        for ax in [-0.35, 0.35]:
            for ay in [-0.35, 0.35]:
                b.add_cylinder(0.02, 0.08, segments=8, center=Vector((col_x + ax, 0.35 + ay, 0.55)), axis='Z', mat=mats["bolts_hardware"])

    # Horizontal Box Truss spanning across the road (at Z = 5.2m to 6.4m, span = span)
    truss_z = 5.80
    truss_h = 1.20
    truss_d = 0.70
    for dz in [-truss_h / 2, truss_h / 2]:
        for dy in [0.0, truss_d]:
            b.add_cylinder(chord_r, span, segments=12,
                           center=Vector((0, dy, truss_z + dz)), axis='X', mat=mats["steel_galvanized"])
                           
    # Diagonal zigzag trusses across the span
    num_bays = 10
    bay_w = span / num_bays
    for i in range(num_bays):
        bx = -span / 2 + i * bay_w + bay_w / 2
        # Front vertical & diagonal struts
        b.add_box(Vector((0.04, 0.04, truss_h)), center=Vector((bx - bay_w/2, 0.0, truss_z)), mat=mats["steel_galvanized"])
        # Diagonal tube
        diag_len = math.sqrt(bay_w**2 + truss_h**2)
        # simplified box representation
        b.add_box(Vector((0.04, 0.04, diag_len * 0.95)), center=Vector((bx, 0.0, truss_z)), mat=mats["steel_galvanized"])
        
    # Maintenance Catwalk Walkway with Handrails (on the backside, Y = truss_d + 0.3m)
    catwalk_y = truss_d + 0.40
    catwalk_z = truss_z - truss_h / 2
    # Catwalk grating floor
    b.add_box(Vector((span * 0.96, 0.70, 0.04)), center=Vector((0, catwalk_y, catwalk_z + 0.02)), mat=mats["steel_dark_post"])
    # Handrails (height 1.1m)
    for hz in [0.55, 1.10]:
        b.add_cylinder(0.025, span * 0.96, segments=8, center=Vector((0, catwalk_y + 0.35, catwalk_z + hz)), axis='X', mat=mats["steel_galvanized"])
    # Vertical railing posts
    for i in range(num_bays + 1):
        rx = -span / 2 + i * bay_w
        b.add_cylinder(0.025, 1.10, segments=8, center=Vector((rx, catwalk_y + 0.35, catwalk_z + 0.55)), axis='Z', mat=mats["steel_galvanized"])

    # 2 Suspended Highway Signs on the Gantry
    # Sign A (Left - Downtown Exit 1km): width 2.2m, height 1.4m
    sign_a_x = -2.20
    sign_a_z = 5.20
    pts_a = make_rect_pts(2.20, 1.40)
    b.add_box(Vector((2.20, 0.05, 1.40)), center=Vector((sign_a_x, -0.05, sign_a_z)), mat=mats["aluminum_back"])
    b.add_textured_planar_sign(pts_a, thickness=0.005, center=Vector((sign_a_x, -0.08, sign_a_z)),
                               face_mat=mats["tex_gantry_left"], back_mat=mats["aluminum_back"])
    # Hangers connecting Sign A to truss
    for hx in [-0.8, 0.8]:
        b.add_box(Vector((0.06, 0.06, 1.80)), center=Vector((sign_a_x + hx, -0.03, sign_a_z + 0.4)), mat=mats["steel_galvanized"])
        
    # Exit Tab Banner mounted on top of Sign A
    tab_z = sign_a_z + 1.40 / 2 + 0.22
    pts_tab = make_rect_pts(1.30, 0.40)
    b.add_box(Vector((1.30, 0.04, 0.40)), center=Vector((sign_a_x, -0.05, tab_z)), mat=mats["aluminum_back"])
    b.add_textured_planar_sign(pts_tab, thickness=0.005, center=Vector((sign_a_x, -0.08, tab_z)),
                               face_mat=mats["tex_gantry_tab"], back_mat=mats["aluminum_back"])

    # Sign B (Right - Airport 12km): width 2.2m, height 1.4m
    sign_b_x = 2.20
    sign_b_z = 5.20
    pts_b = make_rect_pts(2.20, 1.40)
    b.add_box(Vector((2.20, 0.05, 1.40)), center=Vector((sign_b_x, -0.05, sign_b_z)), mat=mats["aluminum_back"])
    b.add_textured_planar_sign(pts_b, thickness=0.005, center=Vector((sign_b_x, -0.08, sign_b_z)),
                               face_mat=mats["tex_gantry_right"], back_mat=mats["aluminum_back"])
    for hx in [-0.8, 0.8]:
        b.add_box(Vector((0.06, 0.06, 1.80)), center=Vector((sign_b_x + hx, -0.03, sign_b_z + 0.4)), mat=mats["steel_galvanized"])

    # Overhead Luminaire Brackets illuminating the signs
    for sx in [sign_a_x, sign_b_x]:
        for lx in [-0.6, 0.6]:
            # Extruded arm extending forward
            b.add_box(Vector((0.04, 0.60, 0.04)), center=Vector((sx + lx, -0.35, sign_a_z + 0.8)), mat=mats["steel_black_trim"])
            # Lamp fixture
            b.add_box(Vector((0.25, 0.15, 0.10)), center=Vector((sx + lx, -0.65, sign_a_z + 0.75)), mat=mats["steel_black_trim"])

    return b.build_object()

def build_sign_special_cantilever(mats):
    b = SignBuilder("Sign_Special_Cantilever_Exit")
    # Mast on right side (X = 2.20m), arm extends to the left over the road
    mast_x = 2.20
    mast_h = 6.20
    arm_len = 4.40
    arm_z = 5.40
    
    # Vertical tubular mast
    b.add_cylinder(0.14, mast_h, segments=16, center=Vector((mast_x, 0.20, mast_h / 2)), axis='Z', mat=mats["steel_galvanized"])
    # Heavy base footing & gusset plates
    b.add_box(Vector((1.0, 1.0, 0.40)), center=Vector((mast_x, 0.20, 0.20)), mat=mats["concrete_base"])
    b.add_box(Vector((0.75, 0.75, 0.03)), center=Vector((mast_x, 0.20, 0.415)), mat=mats["steel_galvanized"])
    for gx in [-0.25, 0.25]:
        b.add_box(Vector((0.03, 0.40, 0.45)), center=Vector((mast_x + gx, 0.20, 0.65)), mat=mats["steel_galvanized"])
        
    # Horizontal cantilever arm extending to left
    b.add_cylinder(0.11, arm_len, segments=16, center=Vector((mast_x - arm_len / 2, 0.20, arm_z)), axis='X', mat=mats["steel_galvanized"])
    # Diagonal brace strut
    b.add_box(Vector((1.40, 0.08, 0.08)), center=Vector((mast_x - 0.70, 0.20, arm_z - 0.70)), mat=mats["steel_galvanized"])
    
    # Suspended directional exit sign: width 2.2m, height 1.2m
    sign_x = mast_x - 2.20
    pts = make_rect_pts(2.20, 1.20)
    b.add_box(Vector((2.20, 0.05, 1.20)), center=Vector((sign_x, 0.0, arm_z - 0.85)), mat=mats["aluminum_back"])
    b.add_textured_planar_sign(pts, thickness=0.005, center=Vector((sign_x, -0.03, arm_z - 0.85)),
                               face_mat=mats["tex_cantilever"], back_mat=mats["aluminum_back"])
    # Vertical hangers
    for hx in [-0.7, 0.7]:
        b.add_box(Vector((0.06, 0.06, 1.20)), center=Vector((sign_x + hx, 0.10, arm_z - 0.5)), mat=mats["steel_galvanized"])
        
    return b.build_object()

def build_sign_special_milestone(mats):
    b = SignBuilder("Sign_Special_Kilometre_Milestone")
    # Roadside concrete milestone marker (Marco Quilométrico)
    w, h, depth = 0.46, 0.85, 0.32
    # Base concrete block
    b.add_box(Vector((w, depth, h * 0.75)), center=Vector((0, 0, h * 0.75 / 2)), mat=mats["concrete_base"])
    # Domed cylinder top cap
    b.add_cylinder(w / 2, depth, segments=24, center=Vector((0, 0, h * 0.75)), axis='Y', mat=mats["concrete_base"])
    # Front plaque with KM 250 & BR-101
    pts = make_rect_pts(w * 0.90, h * 0.85)
    b.add_textured_planar_sign(pts, thickness=0.004, center=Vector((0, -depth / 2 - 0.002, h * 0.45)),
                               face_mat=mats["tex_milestone"], back_mat=mats["concrete_base"])
    # Corner chamfers / pedestal base slab
    b.add_box(Vector((w * 1.25, depth * 1.25, 0.12)), center=Vector((0, 0, 0.06)), mat=mats["concrete_base"])
    return b.build_object()

def build_sign_special_paddle(mats):
    b = SignBuilder("Sign_Special_Paddle_Mileage_Marker")
    # Milepost blade marker on green U-channel post
    b.add_box(Vector((0.04, 0.02, 1.40)), center=Vector((0, 0.02, 0.70)), mat=mats["steel_dark_post"])
    pts = make_rect_pts(0.20, 0.60)
    b.add_textured_planar_sign(pts, thickness=0.003, center=Vector((0, 0, 1.05)),
                               face_mat=mats["tex_paddle"], back_mat=mats["aluminum_back"])
    # White glass-bead reflector discs
    b.add_cylinder(0.02, 0.01, segments=12, center=Vector((0, -0.005, 1.38)), axis='Y', mat=mats["amber_strobe"])
    return b.build_object()

def build_sign_special_vms_led(mats):
    b = SignBuilder("Sign_Special_LED_Variable_Message")
    # Industrial electronic VMS display cabinet
    w, h, depth = 2.80, 1.10, 0.38
    center_z = 3.20
    
    # Outer weatherproof enclosure
    b.add_box(Vector((w, depth, h)), center=Vector((0, depth / 2, center_z)), mat=mats["led_housing"])
    # Slanted top sun hood / visor
    b.add_box(Vector((w + 0.10, 0.25, 0.04)), center=Vector((0, -0.08, center_z + h / 2 + 0.02)), mat=mats["led_housing"])
    
    # Recessed LED screen panel (positioned flush on front face at Y = -0.005)
    pts = make_rect_pts(w * 0.94, h * 0.88)
    b.add_textured_planar_sign(pts, thickness=0.005, center=Vector((0, -0.005, center_z)),
                               face_mat=mats["tex_vms_led"], back_mat=mats["led_housing"])
                               
    # Dual flashing amber LED beacons on top
    for lx in [-w * 0.40, w * 0.40]:
        bz = center_z + h / 2 + 0.12
        b.add_cylinder(0.07, 0.14, segments=16, center=Vector((lx, depth / 2, bz)), axis='Z', mat=mats["amber_strobe"])
        b.add_cylinder(0.085, 0.03, segments=16, center=Vector((lx, depth / 2, bz + 0.08)), axis='Z', mat=mats["led_housing"])

    # Heavy tubular steel mast & foundation
    mast_r = 0.12
    b.add_cylinder(mast_r, center_z - h / 2, segments=16,
                   center=Vector((0, depth / 2, (center_z - h / 2) / 2)), axis='Z', mat=mats["steel_galvanized"])
    b.add_box(Vector((0.80, 0.80, 0.30)), center=Vector((0, depth / 2, 0.15)), mat=mats["concrete_base"])
    
    # Controller cabinet at base
    b.add_box(Vector((0.60, 0.40, 1.00)), center=Vector((0.65, depth / 2, 0.80)), mat=mats["steel_dark_post"])
    return b.build_object()

def build_sign_special_city_welcome(mats):
    b = SignBuilder("Sign_Special_City_Welcome_Monument")
    # Stone brick masonry foundation plinth
    b.add_box(Vector((3.40, 0.80, 0.55)), center=Vector((0, 0.30, 0.275)), mat=mats["stone_masonry"])
    
    # Dual stone masonry side pillars
    for px in [-1.50, 1.50]:
        b.add_box(Vector((0.55, 0.70, 2.30)), center=Vector((px, 0.30, 1.40)), mat=mats["stone_masonry"])
        # Pillar decorative stone cap
        b.add_box(Vector((0.68, 0.82, 0.15)), center=Vector((px, 0.30, 2.625)), mat=mats["concrete_base"])
        
    # Architectural welcome signboard between pillars
    w, h = 2.20, 1.30
    sign_z = 1.65
    b.add_box(Vector((w, 0.10, h)), center=Vector((0, 0.25, sign_z)), mat=mats["wood_trim"])
    pts = make_rect_pts(w, h)
    b.add_textured_planar_sign(pts, thickness=0.005, center=Vector((0, 0.19, sign_z)),
                               face_mat=mats["tex_city_welcome"], back_mat=mats["wood_trim"])
                               
    # Timber header beam spanning across top
    b.add_box(Vector((w * 1.15, 0.18, 0.18)), center=Vector((0, 0.25, sign_z + h / 2 + 0.10)), mat=mats["wood_trim"])
    
    # Dual ground spotlights angled toward the monument
    for lx in [-0.8, 0.8]:
        b.add_cylinder(0.08, 0.15, segments=12, center=Vector((lx, -0.40, 0.15)), axis='Y', mat=mats["steel_black_trim"])
        b.add_cylinder(0.06, 0.02, segments=12, center=Vector((lx, -0.32, 0.15)), axis='Y', mat=mats["amber_strobe"])
        
    return b.build_object()

# -----------------------------------------------------------------------------
# 3D Golden Label Generator
# -----------------------------------------------------------------------------
def create_3d_label(text, location, scale=0.25, mat=None):
    curve_data = bpy.data.curves.new(name=f"Label_{text[:8]}", type='FONT')
    curve_data.body = text
    curve_data.extrude = 0.02
    curve_data.bevel_depth = 0.003
    curve_data.align_x = 'CENTER'
    curve_data.align_y = 'CENTER'
    
    obj = bpy.data.objects.new(name=f"Text_{text[:8]}", object_data=curve_data)
    obj.location = location
    obj.scale = (scale, scale, scale)
    obj.rotation_euler = (math.radians(90), 0, 0)
    bpy.context.collection.objects.link(obj)
    if mat:
        obj.data.materials.append(mat)
    return obj

# -----------------------------------------------------------------------------
# Showroom Scene Setup
# -----------------------------------------------------------------------------
def setup_showroom_scene(signs_dict, mats):
    # Groups:
    # Row 1 (Front): Standard European & American Regulatory/Warning Signs
    # Row 2 (Middle): Multi-City Highway Distance Boards (Paris, New York, São Paulo) & Milestone & Paddle
    # Row 3 (Back): Giant Overhead Gantry, Cantilever Arm, Dynamic LED VMS & City Monument
    
    row1_items = [
        signs_dict["Sign_EU_Stop_Octagon"],
        signs_dict["Sign_EU_Yield_Triangle"],
        signs_dict["Sign_EU_Warning_Curve_Triangle"],
        signs_dict["Sign_EU_Speed_80_Circle"],
        signs_dict["Sign_EU_No_Entry_Circle"],
        signs_dict["Sign_EU_Mandatory_Roundabout"],
        signs_dict["Sign_EU_Motorway_Entry"],
        signs_dict["Sign_US_Warning_Curve_Diamond"],
        signs_dict["Sign_US_Speed_Limit_65"],
        signs_dict["Sign_US_One_Way_Arrow"],
        signs_dict["Sign_US_Interstate_Shield"],
        signs_dict["Sign_BR_Warning_Lombada_Diamond"],
        signs_dict["Sign_BR_Regulatory_Pare"],
        signs_dict["Sign_BR_Highway_Shield"],
        signs_dict["Sign_JP_Stop_Inverted_Triangle"],
        signs_dict["Sign_UK_National_Speed_Limit"],
        signs_dict["Sign_AU_Wildlife_Kangaroo"],
    ]
    
    # Position Row 1 (Y = -4.0m) - Roadway Signs & Ground Markers
    # Low-profile ground markers placed on the front flanks:
    signs_dict["Sign_Special_Kilometre_Milestone"].location = Vector((-11.0, -4.0, 0))
    signs_dict["Sign_Special_Paddle_Mileage_Marker"].location = Vector((11.0, -4.0, 0))

    r1_spacing = 1.25
    start_x_r1 = -(len(row1_items) - 1) * r1_spacing / 2.0
    for idx, obj in enumerate(row1_items):
        obj.location = Vector((start_x_r1 + idx * r1_spacing, -4.0, 0))
        
    # Position Row 2 (Y = 0.6m) - Multi-City Highway Distance Confirmation Boards
    signs_dict["Sign_Special_Distance_Paris_Lyon"].location = Vector((-5.5, 0.6, 0))
    signs_dict["Sign_Special_Distance_NewYork_DC"].location = Vector((0.0, 0.6, 0))
    signs_dict["Sign_Special_Distance_SaoPaulo_Campinas"].location = Vector((5.5, 0.6, 0))
        
    # Position Row 3 (Y = 6.0m to 7.5m) - Overhead Gantry, Cantilever, Dynamic LED VMS & Welcome Monument
    signs_dict["Sign_Special_Cantilever_Exit"].location = Vector((-8.8, 6.2, 0))
    signs_dict["Sign_Special_Overhead_Highway_Gantry"].location = Vector((0.0, 7.5, 0))
    signs_dict["Sign_Special_LED_Variable_Message"].location = Vector((7.2, 5.6, 0))
    signs_dict["Sign_Special_City_Welcome_Monument"].location = Vector((12.2, 6.0, 0))
    
    # Showroom Ground / Road Platform
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 2.0, -0.20))
    road = bpy.context.active_object
    road.name = "Showroom_Asphalt_Plaza"
    road.scale = Vector((38.0, 26.0, 0.40))
    road.data.materials.append(mats["showcase_road"])
    
    # Curb trim along front
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -8.8, 0.05))
    curb = bpy.context.active_object
    curb.name = "Showroom_Curb"
    curb.scale = Vector((38.0, 0.35, 0.15))
    curb.data.materials.append(mats["showcase_curb"])

    # 3D Category Title Labels
    create_3d_label("WORLD TRAFFIC SIGNS & DISTANCE SYSTEM", Vector((0, -7.5, 0.1)), scale=0.45, mat=mats["text_gold"])
    create_3d_label("INTERNATIONAL REGULATORY & WARNING SIGNS", Vector((0, -5.8, 0.05)), scale=0.25, mat=mats["text_gold"])
    create_3d_label("HIGHWAY DESTINATION & MILEAGE BOARDS (PARIS * NEW YORK * SAO PAULO)", Vector((0, -1.6, 0.05)), scale=0.28, mat=mats["text_gold"])
    create_3d_label("OVERHEAD TRUSS GANTRY * CANTILEVER ARM * DYNAMIC LED VMS * MONUMENTS", Vector((0, 3.8, 0.05)), scale=0.30, mat=mats["text_gold"])

    # Studio Lighting Setup
    # Key Light (Sun / Strong Directional)
    bpy.ops.object.light_add(type='SUN', location=(14.0, -20.0, 24.0))
    sun = bpy.context.active_object
    sun.name = "Key_Sun_Light"
    sun.data.energy = 4.2
    sun.data.color = (1.0, 0.98, 0.94)
    sun.rotation_euler = (math.radians(48), math.radians(14), math.radians(-32))
    
    # Fill Light (Cool sky soft)
    bpy.ops.object.light_add(type='AREA', location=(-16.0, -14.0, 16.0))
    fill = bpy.context.active_object
    fill.name = "Fill_Area_Light"
    fill.data.energy = 900.0
    fill.data.size = 16.0
    fill.data.color = (0.85, 0.92, 1.0)
    fill.rotation_euler = (math.radians(55), math.radians(-25), math.radians(20))
    
    # Rim / Backlight
    bpy.ops.object.light_add(type='AREA', location=(0.0, 18.0, 15.0))
    rim = bpy.context.active_object
    rim.name = "Rim_Backlight"
    rim.data.energy = 1300.0
    rim.data.size = 20.0
    rim.data.color = (1.0, 0.95, 0.9)
    rim.rotation_euler = (math.radians(-45), 0, 0)

    # Showcase Camera
    bpy.ops.object.camera_add(location=(0.0, -21.8, 9.6))
    cam = bpy.context.active_object
    cam.name = "Showcase_Camera"
    cam.data.lens = 25 # Wide focal length for epic collection view
    cam.rotation_euler = (math.radians(70), 0, 0)
    bpy.context.scene.camera = cam

# -----------------------------------------------------------------------------
# Main Execution & FBX Export
# -----------------------------------------------------------------------------
def export_individual_fbx(obj, original_loc):
    # Center object to (0,0,0) for clean Unity prefab pivot
    obj.location = Vector((0, 0, 0))
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    
    fbx_name = f"{obj.name}.fbx"
    fbx_path = os.path.join(ASSETS_SIGNS_DIR, fbx_name)
    try:
        bpy.ops.export_scene.fbx(
            filepath=fbx_path,
            use_selection=True,
            apply_unit_scale=True,
            axis_forward='-Z',
            axis_up='Y'
        )
        print(f"Exported FBX: {fbx_name}")
    except Exception as e:
        print(f"FBX export note ({obj.name}): {e}")
        
    # Restore showroom position
    obj.location = original_loc

def main():
    print("=== STARTING WORLD TRAFFIC SIGNS GENERATION FOR BLENDER 5.2 ===")
    reset_scene()
    mats = setup_materials()
    
    signs = {}
    print("Building European Signs...")
    signs["Sign_EU_Stop_Octagon"] = build_sign_eu_stop(mats)
    signs["Sign_EU_Yield_Triangle"] = build_sign_eu_yield(mats)
    signs["Sign_EU_Warning_Curve_Triangle"] = build_sign_eu_curve(mats)
    signs["Sign_EU_Speed_80_Circle"] = build_sign_eu_speed80(mats)
    signs["Sign_EU_No_Entry_Circle"] = build_sign_eu_no_entry(mats)
    signs["Sign_EU_Mandatory_Roundabout"] = build_sign_eu_roundabout(mats)
    signs["Sign_EU_Motorway_Entry"] = build_sign_eu_motorway(mats)
    
    print("Building Americas & Mercosul Signs...")
    signs["Sign_US_Warning_Curve_Diamond"] = build_sign_us_curve(mats)
    signs["Sign_US_Speed_Limit_65"] = build_sign_us_speed65(mats)
    signs["Sign_US_One_Way_Arrow"] = build_sign_us_oneway(mats)
    signs["Sign_US_Interstate_Shield"] = build_sign_us_interstate(mats)
    signs["Sign_BR_Warning_Lombada_Diamond"] = build_sign_br_lombada(mats)
    signs["Sign_BR_Regulatory_Pare"] = build_sign_br_pare(mats)
    signs["Sign_BR_Highway_Shield"] = build_sign_br_shield(mats)
    
    print("Building Asian & Commonwealth Signs...")
    signs["Sign_JP_Stop_Inverted_Triangle"] = build_sign_jp_stop(mats)
    signs["Sign_UK_National_Speed_Limit"] = build_sign_uk_speedlimit(mats)
    signs["Sign_AU_Wildlife_Kangaroo"] = build_sign_au_kangaroo(mats)
    
    print("Building Special Highway Distance & City Mileage Boards...")
    signs["Sign_Special_Distance_Paris_Lyon"] = build_sign_special_distance_paris(mats)
    signs["Sign_Special_Distance_NewYork_DC"] = build_sign_special_distance_newyork(mats)
    signs["Sign_Special_Distance_SaoPaulo_Campinas"] = build_sign_special_distance_saopaulo(mats)
    
    print("Building Overhead Gantry, Cantilever, VMS & Monuments...")
    signs["Sign_Special_Overhead_Highway_Gantry"] = build_sign_special_overhead_gantry(mats)
    signs["Sign_Special_Cantilever_Exit"] = build_sign_special_cantilever(mats)
    signs["Sign_Special_Kilometre_Milestone"] = build_sign_special_milestone(mats)
    signs["Sign_Special_Paddle_Mileage_Marker"] = build_sign_special_paddle(mats)
    signs["Sign_Special_LED_Variable_Message"] = build_sign_special_vms_led(mats)
    signs["Sign_Special_City_Welcome_Monument"] = build_sign_special_city_welcome(mats)
    
    print("Setting up Showroom Exhibition Scene...")
    setup_showroom_scene(signs, mats)
    
    # Export individual FBX models for Unity
    print("Exporting individual FBX models to Unity Assets...")
    for name, obj in signs.items():
        export_individual_fbx(obj, obj.location.copy())
        
    # Save .blend master files
    print(f"Saving Master .blend to {BLEND_FILE_PATH}...")
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_FILE_PATH)
    bpy.ops.wm.save_as_mainfile(filepath=ASSETS_BLEND_PATH)
    print(f"Saved Assets .blend to {ASSETS_BLEND_PATH}")
    
    # Render Showcase image
    print(f"Rendering showcase image to {RENDER_OUTPUT_PATH}...")
    scene = bpy.context.scene
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = RENDER_OUTPUT_PATH
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100
    
    for engine in ['BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'BLENDER_WORKBENCH', 'CYCLES']:
        try:
            scene.render.engine = engine
            break
        except Exception:
            pass
            
    bpy.ops.render.render(write_still=True)
    print("=== WORLD TRAFFIC SIGNS GENERATED AND RENDERED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
