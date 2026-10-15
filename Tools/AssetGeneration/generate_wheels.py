"""
Procedural 3D Wheels Collection Generator for Blender 5.2
Creates 12 distinct, high-fidelity vehicle wheels:
 1. Sport Racing / Supercar
 2. Off-Road / Mud-Terrain 4x4
 3. Heavy Truck / Commercial Semi
 4. Agricultural Tractor Rear Wheel
 5. Classic Muscle Car / Hot Rod
 6. Formula 1 / Track Slick
 7. Cyber / Sci-Fi Futuristic
 8. Military Armored 8x8 Tactical
 9. Sport Motorcycle / Superbike
10. Vintage Spoke Bicycle / Retro
11. Heavy Construction Loader / Mining
12. Aircraft Landing Gear

Engineered with clean bmesh geometry, multi-material slots,
showroom pedestals, 3D golden labels, studio 3-point lighting,
and exports both .blend and individual .fbx models for Unity.
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix, Euler

# -----------------------------------------------------------------------------
# Paths
# -----------------------------------------------------------------------------
PROJECT_DIR = r"E:\Backup-C\Projetos\testejogo"
BLEND_FILE_NAME = "Vehicle_Wheels_Collection.blend"
BLEND_FILE_PATH = os.path.join(PROJECT_DIR, BLEND_FILE_NAME)
ASSETS_MODELS_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "Vehicles", "WheelsPack")
ASSETS_BLEND_PATH = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "Vehicles", BLEND_FILE_NAME)
RENDER_OUTPUT_PATH = os.path.join(PROJECT_DIR, "wheels_showcase_render.png")

os.makedirs(ASSETS_MODELS_DIR, exist_ok=True)

# -----------------------------------------------------------------------------
# Materials
# -----------------------------------------------------------------------------
def get_or_create_mat(name, base_color, metallic=0.0, roughness=0.5, emission_color=None, emission_strength=0.0):
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

def setup_all_materials():
    return {
        "tire_dark": get_or_create_mat("Mat_Tire_Dark", (0.05, 0.05, 0.05, 1.0), 0.0, 0.75),
        "tire_rough": get_or_create_mat("Mat_Tire_Rough", (0.04, 0.04, 0.04, 1.0), 0.0, 0.88),
        "tire_vintage": get_or_create_mat("Mat_Tire_Vintage", (0.06, 0.06, 0.06, 1.0), 0.0, 0.65),
        "tire_gumwall": get_or_create_mat("Mat_Gumwall", (0.72, 0.54, 0.32, 1.0), 0.0, 0.6),
        "rim_alloy": get_or_create_mat("Mat_Rim_Alloy", (0.80, 0.82, 0.84, 1.0), 0.95, 0.20),
        "rim_dark": get_or_create_mat("Mat_Rim_DarkGunmetal", (0.12, 0.13, 0.15, 1.0), 0.88, 0.25),
        "rim_chrome": get_or_create_mat("Mat_Rim_Chrome", (0.95, 0.95, 0.96, 1.0), 1.0, 0.04),
        "rim_gold": get_or_create_mat("Mat_Rim_Gold", (0.90, 0.70, 0.16, 1.0), 0.92, 0.18),
        "rim_yellow": get_or_create_mat("Mat_Rim_YellowInd", (0.95, 0.65, 0.04, 1.0), 0.15, 0.35),
        "rim_military": get_or_create_mat("Mat_Rim_MilitaryGreen", (0.18, 0.22, 0.14, 1.0), 0.1, 0.65),
        "rim_aircraft": get_or_create_mat("Mat_Rim_AircraftWhite", (0.88, 0.88, 0.90, 1.0), 0.4, 0.28),
        "brake_steel": get_or_create_mat("Mat_Brake_Steel", (0.65, 0.67, 0.70, 1.0), 0.95, 0.30),
        "caliper_red": get_or_create_mat("Mat_Caliper_Red", (0.88, 0.03, 0.03, 1.0), 0.2, 0.22),
        "caliper_gold": get_or_create_mat("Mat_Caliper_Gold", (0.92, 0.74, 0.14, 1.0), 0.85, 0.22),
        "cyber_glow": get_or_create_mat("Mat_Cyber_CyanGlow", (0.0, 0.9, 1.0, 1.0), 0.0, 0.1, emission_color=(0.0, 0.9, 1.0, 1.0), emission_strength=10.0),
        "stripe_red": get_or_create_mat("Mat_Stripe_Red", (0.92, 0.05, 0.05, 1.0), 0.1, 0.3),
        "beadlock_orange": get_or_create_mat("Mat_Beadlock_Orange", (0.96, 0.38, 0.02, 1.0), 0.85, 0.28),
        "bolts": get_or_create_mat("Mat_Bolts", (0.85, 0.86, 0.90, 1.0), 0.98, 0.12),
        "pedestal": get_or_create_mat("Mat_Pedestal", (0.08, 0.08, 0.09, 1.0), 0.3, 0.45),
        "pedestal_top": get_or_create_mat("Mat_Pedestal_Top", (0.16, 0.16, 0.18, 1.0), 0.5, 0.3),
        "text_gold": get_or_create_mat("Mat_Text_Gold", (0.96, 0.82, 0.28, 1.0), 0.85, 0.18)
    }

# -----------------------------------------------------------------------------
# BMesh Procedural Modeling Engine
# All coordinates: X is wheel axis, +X is OUTER rim face, -X is INNER face
# Y is horizontal, Z is vertical. Circle is in YZ plane.
# -----------------------------------------------------------------------------
class WheelMeshBuilder:
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.materials = []
        self.mat_map = {}

    def get_mat_idx(self, mat):
        if mat not in self.mat_map:
            idx = len(self.materials)
            self.materials.append(mat)
            self.mat_map[mat] = idx
            return idx
        return self.mat_map[mat]

    def add_cylinder(self, radius, depth, x_center=0.0, verts=32, mat=None):
        m_idx = self.get_mat_idx(mat) if mat else 0
        x0 = x_center - depth / 2.0
        x1 = x_center + depth / 2.0
        
        v_back = []
        v_front = []
        for i in range(verts):
            th = 2 * math.pi * i / verts
            y = radius * math.cos(th)
            z = radius * math.sin(th)
            v_back.append(self.bm.verts.new((x0, y, z)))
            v_front.append(self.bm.verts.new((x1, y, z)))
            
        # Side quads
        for i in range(verts):
            ni = (i + 1) % verts
            f = self.bm.faces.new((v_back[i], v_front[i], v_front[ni], v_back[ni]))
            f.material_index = m_idx
            
        # Caps
        f_front = self.bm.faces.new(reversed(v_front))
        f_front.material_index = m_idx
        f_back = self.bm.faces.new(v_back)
        f_back.material_index = m_idx

    def add_tube(self, outer_r, inner_r, depth, x_center=0.0, verts=32, mat=None):
        m_idx = self.get_mat_idx(mat) if mat else 0
        x0 = x_center - depth / 2.0
        x1 = x_center + depth / 2.0
        
        vo_b, vo_f = [], []
        vi_b, vi_f = [], []
        for i in range(verts):
            th = 2 * math.pi * i / verts
            yo = outer_r * math.cos(th)
            zo = outer_r * math.sin(th)
            yi = inner_r * math.cos(th)
            zi = inner_r * math.sin(th)
            vo_b.append(self.bm.verts.new((x0, yo, zo)))
            vo_f.append(self.bm.verts.new((x1, yo, zo)))
            vi_b.append(self.bm.verts.new((x0, yi, zi)))
            vi_f.append(self.bm.verts.new((x1, yi, zi)))
            
        for i in range(verts):
            ni = (i + 1) % verts
            # Outer surface
            f1 = self.bm.faces.new((vo_b[i], vo_f[i], vo_f[ni], vo_b[ni]))
            f1.material_index = m_idx
            # Inner surface
            f2 = self.bm.faces.new((vi_f[i], vi_b[i], vi_b[ni], vi_f[ni]))
            f2.material_index = m_idx
            # Front cap (+X)
            f3 = self.bm.faces.new((vo_f[i], vi_f[i], vi_f[ni], vo_f[ni]))
            f3.material_index = m_idx
            # Back cap (-X)
            f4 = self.bm.faces.new((vi_b[i], vo_b[i], vo_b[ni], vi_b[ni]))
            f4.material_index = m_idx

    def add_beveled_tire(self, outer_r, inner_r, width, shoulder_r=0.04, verts=36, mat=None):
        m_idx = self.get_mat_idx(mat) if mat else 0
        w2 = width / 2.0
        
        # Profile points: (x, r)
        profile = [
            (-w2, inner_r),
            (-w2, inner_r + (outer_r - inner_r)*0.35),
            (-w2 + shoulder_r, outer_r),
            (w2 - shoulder_r, outer_r),
            (w2, inner_r + (outer_r - inner_r)*0.35),
            (w2, inner_r)
        ]
        
        rings = []
        for x, r in profile:
            ring = []
            for i in range(verts):
                th = 2 * math.pi * i / verts
                ring.append(self.bm.verts.new((x, r * math.cos(th), r * math.sin(th))))
            rings.append(ring)
            
        for r_idx in range(len(rings) - 1):
            for i in range(verts):
                ni = (i + 1) % verts
                v1 = rings[r_idx][i]
                v2 = rings[r_idx][ni]
                v3 = rings[r_idx + 1][ni]
                v4 = rings[r_idx + 1][i]
                f = self.bm.faces.new((v1, v2, v3, v4))
                f.material_index = m_idx
                
        # Inner rim rim-bed cap connecting ring 0 and ring -1
        for i in range(verts):
            ni = (i + 1) % verts
            f = self.bm.faces.new((rings[-1][i], rings[-1][ni], rings[0][ni], rings[0][i]))
            f.material_index = m_idx

    def add_box(self, dims, loc=(0,0,0), rot_x=0.0, mat=None):
        """Creates a box rotated around the X axis by rot_x, located at loc."""
        m_idx = self.get_mat_idx(mat) if mat else 0
        dx, dy, dz = dims[0]/2, dims[1]/2, dims[2]/2
        
        # 8 local corners
        corners = [
            (-dx, -dy, -dz), (-dx, dy, -dz), (-dx, dy, dz), (-dx, -dy, dz),
            (dx, -dy, -dz), (dx, dy, -dz), (dx, dy, dz), (dx, -dy, dz)
        ]
        
        cos_a = math.cos(rot_x)
        sin_a = math.sin(rot_x)
        
        v_objs = []
        for cx, cy, cz in corners:
            # Rotate in YZ plane around X
            ry = cy * cos_a - cz * sin_a
            rz = cy * sin_a + cz * cos_a
            v_objs.append(self.bm.verts.new((cx + loc[0], ry + loc[1], rz + loc[2])))
            
        faces_idx = [
            (0, 1, 2, 3), # -X
            (4, 7, 6, 5), # +X
            (0, 4, 5, 1), # -Z
            (1, 5, 6, 2), # +Y
            (2, 6, 7, 3), # +Z
            (3, 7, 4, 0)  # -Y
        ]
        
        for fi in faces_idx:
            f = self.bm.faces.new((v_objs[fi[0]], v_objs[fi[1]], v_objs[fi[2]], v_objs[fi[3]]))
            f.material_index = m_idx

    def add_hex_bolt(self, radius, depth, loc=(0,0,0), rot_x=0.0, mat=None):
        m_idx = self.get_mat_idx(mat) if mat else 0
        x0 = loc[0]
        x1 = loc[0] + depth
        
        v_b, v_f = [], []
        for i in range(6):
            th = rot_x + 2 * math.pi * i / 6
            y = loc[1] + radius * math.cos(th)
            z = loc[2] + radius * math.sin(th)
            v_b.append(self.bm.verts.new((x0, y, z)))
            v_f.append(self.bm.verts.new((x1, y, z)))
            
        for i in range(6):
            ni = (i + 1) % 6
            f = self.bm.faces.new((v_b[i], v_f[i], v_f[ni], v_b[ni]))
            f.material_index = m_idx
        f_cap = self.bm.faces.new(reversed(v_f))
        f_cap.material_index = m_idx

    def add_bolts_circle(self, count, radius, x_pos, bolt_r=0.012, depth=0.02, mat=None):
        for i in range(count):
            th = 2 * math.pi * i / count
            y = radius * math.cos(th)
            z = radius * math.sin(th)
            self.add_hex_bolt(bolt_r, depth, loc=(x_pos, y, z), rot_x=th, mat=mat)

    def finish_object(self):
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=0.0001)
        mesh = bpy.data.meshes.new(self.name + "_Mesh")
        self.bm.to_mesh(mesh)
        self.bm.free()
        
        obj = bpy.data.objects.new(self.name, mesh)
        for m in self.materials:
            obj.data.materials.append(m)
            
        bpy.context.collection.objects.link(obj)
        
        # Smooth shading
        for poly in mesh.polygons:
            poly.use_smooth = True
            
        return obj

# -----------------------------------------------------------------------------
# Builders for Each Wheel Type
# -----------------------------------------------------------------------------

def build_01_sport_racing(mats):
    b = WheelMeshBuilder("Wheel_01_Sport_Racing")
    # Low-profile performance tire
    b.add_beveled_tire(outer_r=0.38, inner_r=0.27, width=0.25, shoulder_r=0.03, verts=36, mat=mats["tire_dark"])
    # Rim barrel
    b.add_tube(outer_r=0.27, inner_r=0.24, depth=0.24, x_center=0.0, verts=36, mat=mats["rim_dark"])
    # Outer polished lip
    b.add_tube(outer_r=0.272, inner_r=0.255, depth=0.025, x_center=0.115, verts=36, mat=mats["rim_alloy"])
    # Hub center
    b.add_cylinder(radius=0.07, depth=0.05, x_center=0.09, verts=24, mat=mats["rim_dark"])
    b.add_cylinder(radius=0.035, depth=0.02, x_center=0.125, verts=24, mat=mats["rim_alloy"])
    # 5 Split V-spokes
    for i in range(5):
        base_a = 2 * math.pi * i / 5
        for offset in [-0.08, 0.08]:
            a = base_a + offset
            y = 0.16 * math.cos(a)
            z = 0.16 * math.sin(a)
            b.add_box(dims=(0.025, 0.032, 0.17), loc=(0.095, y, z), rot_x=a, mat=mats["rim_dark"])
    # 5 Lug nuts
    b.add_bolts_circle(5, 0.048, x_pos=0.115, bolt_r=0.007, depth=0.015, mat=mats["bolts"])
    # Brake Rotor & Caliper behind rim
    b.add_cylinder(radius=0.22, depth=0.012, x_center=0.02, verts=32, mat=mats["brake_steel"])
    b.add_box(dims=(0.08, 0.08, 0.12), loc=(0.025, 0.0, 0.17), rot_x=0.0, mat=mats["caliper_red"])
    return b.finish_object()


def build_02_offroad_4x4(mats):
    b = WheelMeshBuilder("Wheel_02_Offroad_4x4")
    # Massive mud tire
    b.add_beveled_tire(outer_r=0.46, inner_r=0.24, width=0.32, shoulder_r=0.05, verts=36, mat=mats["tire_rough"])
    # Mud lugs along tread
    lug_count = 20
    for i in range(lug_count):
        th = 2 * math.pi * i / lug_count
        y = 0.462 * math.cos(th)
        z = 0.462 * math.sin(th)
        x_shift = 0.05 if i % 2 == 0 else -0.05
        b.add_box(dims=(0.16, 0.045, 0.028), loc=(x_shift, y, z), rot_x=th, mat=mats["tire_rough"])
    # Rim barrel
    b.add_tube(outer_r=0.24, inner_r=0.21, depth=0.30, x_center=0.0, verts=32, mat=mats["rim_dark"])
    # Beadlock ring in anodized orange
    b.add_tube(outer_r=0.255, inner_r=0.225, depth=0.025, x_center=0.15, verts=32, mat=mats["beadlock_orange"])
    # 16 Beadlock perimeter bolts
    b.add_bolts_circle(16, 0.24, x_pos=0.163, bolt_r=0.007, depth=0.012, mat=mats["bolts"])
    # Heavy 6-spoke center dish
    b.add_cylinder(radius=0.085, depth=0.06, x_center=0.08, verts=24, mat=mats["rim_dark"])
    for i in range(6):
        a = 2 * math.pi * i / 6
        y = 0.15 * math.cos(a)
        z = 0.15 * math.sin(a)
        b.add_box(dims=(0.04, 0.05, 0.13), loc=(0.09, y, z), rot_x=a, mat=mats["rim_dark"])
    b.add_bolts_circle(6, 0.055, x_pos=0.115, bolt_r=0.009, depth=0.015, mat=mats["bolts"])
    return b.finish_object()


def build_03_heavy_truck(mats):
    b = WheelMeshBuilder("Wheel_03_Heavy_Truck")
    # Commercial highway tire
    b.add_beveled_tire(outer_r=0.52, inner_r=0.29, width=0.30, shoulder_r=0.04, verts=36, mat=mats["tire_rough"])
    # 3 Circumferential tread channels
    for x_off in [-0.07, 0.0, 0.07]:
        b.add_tube(outer_r=0.523, inner_r=0.518, depth=0.025, x_center=x_off, verts=36, mat=mats["tire_dark"])
    # Deep dish concave steel rim
    b.add_tube(outer_r=0.29, inner_r=0.25, depth=0.28, x_center=0.0, verts=36, mat=mats["rim_alloy"])
    b.add_cylinder(radius=0.255, depth=0.04, x_center=-0.02, verts=36, mat=mats["rim_alloy"])
    # Large axle grease hub
    b.add_cylinder(radius=0.09, depth=0.10, x_center=0.08, verts=24, mat=mats["rim_dark"])
    b.add_cylinder(radius=0.05, depth=0.03, x_center=0.14, verts=24, mat=mats["rim_chrome"])
    # 10 Heavy wheel studs & nuts
    b.add_bolts_circle(10, 0.16, x_pos=0.015, bolt_r=0.013, depth=0.035, mat=mats["bolts"])
    # 6 Oval hand holes around dish
    for i in range(6):
        a = 2 * math.pi * i / 6 + 0.3
        y = 0.21 * math.cos(a)
        z = 0.21 * math.sin(a)
        b.add_cylinder(radius=0.022, depth=0.06, x_center=0.0, verts=16, mat=mats["rim_dark"])
    return b.finish_object()


def build_04_agricultural_tractor(mats):
    b = WheelMeshBuilder("Wheel_04_Agricultural_Tractor")
    # Huge rear tractor tire
    b.add_beveled_tire(outer_r=0.74, inner_r=0.38, width=0.42, shoulder_r=0.06, verts=36, mat=mats["tire_rough"])
    # Chevron / Herringbone V-lugs (16 pairs angled at 45 deg)
    chevron_count = 16
    for i in range(chevron_count):
        th = 2 * math.pi * i / chevron_count
        y = 0.745 * math.cos(th)
        z = 0.745 * math.sin(th)
        # Left angled lug
        b.add_box(dims=(0.18, 0.06, 0.055), loc=(-0.10, y, z), rot_x=th, mat=mats["tire_rough"])
        # Right angled lug staggered
        th_r = th + (math.pi / chevron_count)
        y_r = 0.745 * math.cos(th_r)
        z_r = 0.745 * math.sin(th_r)
        b.add_box(dims=(0.18, 0.06, 0.055), loc=(0.10, y_r, z_r), rot_x=th_r, mat=mats["tire_rough"])
    # Industrial yellow agricultural steel wheel
    b.add_tube(outer_r=0.38, inner_r=0.34, depth=0.38, x_center=0.0, verts=36, mat=mats["rim_yellow"])
    b.add_cylinder(radius=0.345, depth=0.06, x_center=0.04, verts=36, mat=mats["rim_yellow"])
    b.add_cylinder(radius=0.14, depth=0.14, x_center=0.10, verts=24, mat=mats["rim_dark"])
    b.add_bolts_circle(8, 0.22, x_pos=0.075, bolt_r=0.016, depth=0.03, mat=mats["bolts"])
    return b.finish_object()


def build_05_classic_muscle(mats):
    b = WheelMeshBuilder("Wheel_05_Classic_Muscle")
    # Vintage muscle car tire
    b.add_beveled_tire(outer_r=0.36, inner_r=0.20, width=0.28, shoulder_r=0.04, verts=36, mat=mats["tire_vintage"])
    # White sidewall stripe on outer face
    b.add_tube(outer_r=0.28, inner_r=0.25, depth=0.005, x_center=0.142, verts=36, mat=mats["rim_aircraft"])
    # Polished chrome rim
    b.add_tube(outer_r=0.20, inner_r=0.18, depth=0.26, x_center=0.0, verts=36, mat=mats["rim_chrome"])
    b.add_tube(outer_r=0.205, inner_r=0.175, depth=0.04, x_center=0.12, verts=36, mat=mats["rim_chrome"])
    # 5-Spoke Torq-Thrust pattern
    b.add_cylinder(radius=0.06, depth=0.05, x_center=0.07, verts=24, mat=mats["rim_dark"])
    for i in range(5):
        a = 2 * math.pi * i / 5
        y = 0.12 * math.cos(a)
        z = 0.12 * math.sin(a)
        b.add_box(dims=(0.04, 0.045, 0.12), loc=(0.07, y, z), rot_x=a, mat=mats["rim_chrome"])
    # Tri-Bar Spinner center cap
    b.add_cylinder(radius=0.035, depth=0.04, x_center=0.11, verts=24, mat=mats["rim_chrome"])
    for i in range(3):
        a = 2 * math.pi * i / 3
        y = 0.04 * math.cos(a)
        z = 0.04 * math.sin(a)
        b.add_box(dims=(0.02, 0.02, 0.06), loc=(0.12, y, z), rot_x=a, mat=mats["rim_chrome"])
    return b.finish_object()


def build_06_formula_slick(mats):
    b = WheelMeshBuilder("Wheel_06_Formula_Slick")
    # Ultra wide track slick tire
    b.add_beveled_tire(outer_r=0.35, inner_r=0.18, width=0.38, shoulder_r=0.025, verts=36, mat=mats["tire_dark"])
    # Red sidewall soft-compound stripe
    b.add_tube(outer_r=0.28, inner_r=0.26, depth=0.005, x_center=0.192, verts=36, mat=mats["stripe_red"])
    # Aerodynamic turbofan cover
    b.add_cylinder(radius=0.178, depth=0.02, x_center=0.17, verts=36, mat=mats["rim_dark"])
    # 12 Perimeter cooling fins
    for i in range(12):
        a = 2 * math.pi * i / 12
        y = 0.14 * math.cos(a)
        z = 0.14 * math.sin(a)
        b.add_box(dims=(0.015, 0.012, 0.04), loc=(0.185, y, z), rot_x=a, mat=mats["rim_alloy"])
    # Single center-lock nut (F1 pitstop hex)
    b.add_cylinder(radius=0.04, depth=0.035, x_center=0.195, verts=6, mat=mats["beadlock_orange"])
    b.add_cylinder(radius=0.015, depth=0.02, x_center=0.215, verts=12, mat=mats["bolts"])
    return b.finish_object()


def build_07_cyber_futuristic(mats):
    b = WheelMeshBuilder("Wheel_07_Cyber_Futuristic")
    # Geometric cyber tire
    b.add_beveled_tire(outer_r=0.38, inner_r=0.25, width=0.26, shoulder_r=0.04, verts=36, mat=mats["tire_dark"])
    # Segmented cybernetic tread armor plates
    for i in range(16):
        a = 2 * math.pi * i / 16
        y = 0.382 * math.cos(a)
        z = 0.382 * math.sin(a)
        b.add_box(dims=(0.24, 0.04, 0.016), loc=(0.0, y, z), rot_x=a, mat=mats["rim_dark"])
    # Glowing neon cyan energy ring
    b.add_tube(outer_r=0.248, inner_r=0.228, depth=0.04, x_center=0.08, verts=36, mat=mats["cyber_glow"])
    # Outer dark titanium structure
    b.add_tube(outer_r=0.25, inner_r=0.21, depth=0.22, x_center=0.0, verts=36, mat=mats["rim_dark"])
    # 3 Cyber spokes
    for i in range(3):
        a = 2 * math.pi * i / 3
        y = 0.12 * math.cos(a)
        z = 0.12 * math.sin(a)
        b.add_box(dims=(0.05, 0.045, 0.18), loc=(0.08, y, z), rot_x=a, mat=mats["rim_alloy"])
    # Floating core with neon accent
    b.add_cylinder(radius=0.05, depth=0.06, x_center=0.08, verts=8, mat=mats["rim_dark"])
    b.add_cylinder(radius=0.025, depth=0.07, x_center=0.08, verts=16, mat=mats["cyber_glow"])
    return b.finish_object()


def build_08_military_armored(mats):
    b = WheelMeshBuilder("Wheel_08_Military_Armored")
    # Run-flat heavy combat tire
    b.add_beveled_tire(outer_r=0.48, inner_r=0.25, width=0.34, shoulder_r=0.05, verts=36, mat=mats["tire_rough"])
    # Tactical directional cross tread blocks
    for i in range(18):
        a = 2 * math.pi * i / 18
        y = 0.482 * math.cos(a)
        z = 0.482 * math.sin(a)
        b.add_box(dims=(0.28, 0.05, 0.02), loc=(0.0, y, z), rot_x=a, mat=mats["tire_rough"])
    # Olive drab tactical armored rim
    b.add_tube(outer_r=0.25, inner_r=0.21, depth=0.32, x_center=0.0, verts=36, mat=mats["rim_military"])
    b.add_cylinder(radius=0.22, depth=0.04, x_center=0.10, verts=36, mat=mats["rim_military"])
    # Central Tire Inflation System (CTIS) protective armored cap
    b.add_cylinder(radius=0.09, depth=0.08, x_center=0.14, verts=24, mat=mats["rim_military"])
    b.add_cylinder(radius=0.06, depth=0.03, x_center=0.185, verts=8, mat=mats["rim_dark"])
    # 12 Armored perimeter hex bolts
    b.add_bolts_circle(12, 0.18, x_pos=0.125, bolt_r=0.011, depth=0.02, mat=mats["bolts"])
    return b.finish_object()


def build_09_motorcycle_sport(mats):
    b = WheelMeshBuilder("Wheel_09_Motorcycle_Sport")
    # Rounded crown sportbike tire
    b.add_beveled_tire(outer_r=0.32, inner_r=0.22, width=0.15, shoulder_r=0.04, verts=36, mat=mats["tire_dark"])
    # Forged racing gold rim
    b.add_tube(outer_r=0.22, inner_r=0.20, depth=0.14, x_center=0.0, verts=36, mat=mats["rim_gold"])
    b.add_cylinder(radius=0.045, depth=0.15, x_center=0.0, verts=24, mat=mats["rim_gold"])
    # Lightweight 3-spoke split Y pattern
    for i in range(3):
        a = 2 * math.pi * i / 3
        y = 0.12 * math.cos(a)
        z = 0.12 * math.sin(a)
        b.add_box(dims=(0.02, 0.03, 0.16), loc=(0.0, y, z), rot_x=a, mat=mats["rim_gold"])
    # Dual floating wave brake rotors & gold calipers on both sides
    for side_x in [-0.09, 0.09]:
        b.add_tube(outer_r=0.18, inner_r=0.09, depth=0.008, x_center=side_x, verts=32, mat=mats["brake_steel"])
        b.add_box(dims=(0.03, 0.05, 0.08), loc=(side_x, 0.0, 0.15), rot_x=0.0, mat=mats["caliper_gold"])
    return b.finish_object()


def build_10_bicycle_retro(mats):
    b = WheelMeshBuilder("Wheel_10_Bicycle_Retro")
    # Slender tire with vintage gumwall sidewalls
    b.add_beveled_tire(outer_r=0.34, inner_r=0.30, width=0.04, shoulder_r=0.01, verts=36, mat=mats["tire_gumwall"])
    b.add_tube(outer_r=0.342, inner_r=0.338, depth=0.02, x_center=0.0, verts=36, mat=mats["tire_vintage"])
    # Slender chrome rim
    b.add_tube(outer_r=0.30, inner_r=0.285, depth=0.03, x_center=0.0, verts=36, mat=mats["rim_chrome"])
    # Small hub & axle nuts
    b.add_cylinder(radius=0.025, depth=0.08, x_center=0.0, verts=24, mat=mats["rim_chrome"])
    b.add_cylinder(radius=0.012, depth=0.02, x_center=-0.05, verts=6, mat=mats["bolts"])
    b.add_cylinder(radius=0.012, depth=0.02, x_center=0.05, verts=6, mat=mats["bolts"])
    # 24 Intersecting wire spokes
    spoke_count = 24
    for i in range(spoke_count):
        a = 2 * math.pi * i / spoke_count
        y = 0.15 * math.cos(a)
        z = 0.15 * math.sin(a)
        side = 0.015 if i % 2 == 0 else -0.015
        b.add_box(dims=(0.003, 0.003, 0.28), loc=(side, y, z), rot_x=a + 0.1, mat=mats["rim_chrome"])
    return b.finish_object()


def build_11_construction_loader(mats):
    b = WheelMeshBuilder("Wheel_11_Construction_Loader")
    # Giant earthmover loader tire
    b.add_beveled_tire(outer_r=0.68, inner_r=0.32, width=0.46, shoulder_r=0.07, verts=36, mat=mats["tire_rough"])
    # 16 Deep square rock lugs
    for i in range(16):
        a = 2 * math.pi * i / 16
        y = 0.685 * math.cos(a)
        z = 0.685 * math.sin(a)
        b.add_box(dims=(0.38, 0.09, 0.06), loc=(0.0, y, z), rot_x=a, mat=mats["tire_rough"])
    # Safety yellow heavy duty plate rim
    b.add_tube(outer_r=0.32, inner_r=0.28, depth=0.44, x_center=0.0, verts=36, mat=mats["rim_yellow"])
    b.add_cylinder(radius=0.285, depth=0.06, x_center=0.10, verts=36, mat=mats["rim_yellow"])
    # Planetary reduction final drive hub
    b.add_cylinder(radius=0.14, depth=0.12, x_center=0.16, verts=24, mat=mats["rim_yellow"])
    b.add_cylinder(radius=0.08, depth=0.04, x_center=0.23, verts=24, mat=mats["rim_dark"])
    b.add_bolts_circle(12, 0.20, x_pos=0.135, bolt_r=0.016, depth=0.03, mat=mats["bolts"])
    return b.finish_object()


def build_12_airplane_landing(mats):
    b = WheelMeshBuilder("Wheel_12_Airplane_Landing")
    # Aviation high pressure tire
    b.add_beveled_tire(outer_r=0.36, inner_r=0.18, width=0.22, shoulder_r=0.035, verts=36, mat=mats["tire_dark"])
    # Twin circumferential hydroplaning relief grooves
    for x_off in [-0.035, 0.035]:
        b.add_tube(outer_r=0.362, inner_r=0.358, depth=0.012, x_center=x_off, verts=36, mat=mats["tire_rough"])
    # Split aircraft magnesium wheel in Aircraft White
    b.add_tube(outer_r=0.18, inner_r=0.15, depth=0.20, x_center=0.0, verts=36, mat=mats["rim_aircraft"])
    b.add_cylinder(radius=0.155, depth=0.04, x_center=0.06, verts=36, mat=mats["rim_aircraft"])
    b.add_cylinder(radius=0.05, depth=0.08, x_center=0.09, verts=24, mat=mats["rim_dark"])
    # 8 Rim tie-bolts
    b.add_bolts_circle(8, 0.11, x_pos=0.085, bolt_r=0.009, depth=0.02, mat=mats["bolts"])
    # 6 Thermal relief cooling holes
    for i in range(6):
        a = 2 * math.pi * i / 6 + 0.26
        y = 0.08 * math.cos(a)
        z = 0.08 * math.sin(a)
        b.add_cylinder(radius=0.015, depth=0.05, x_center=0.06, verts=16, mat=mats["brake_steel"])
    return b.finish_object()

# -----------------------------------------------------------------------------
# Showroom Setup: 2-Row Pedestals, Golden Labels, Studio Lights, Camera
# -----------------------------------------------------------------------------
def setup_showroom_scene(wheels_front, wheels_back, mats):
    spacing_x = 1.85
    
    # Pedestal parameters
    ped_radius = 0.75
    ped_height = 0.22
    
    def place_row(wheels, row_y, label_start_idx):
        for col_idx, (wheel, name_label) in enumerate(wheels):
            x = (col_idx - 2.5) * spacing_x
            y = row_y
            
            # Position wheel so it rests flush on the pedestal
            # After rotation, its lowest point should touch ped_height
            # In our builder, center is (0,0,0) and radius in YZ is R.
            # When rotated around Z, the lowest Z point is -R.
            # So z_center = ped_height + R
            dim_z = wheel.dimensions.z
            z_pos = ped_height + (dim_z / 2.0)
            
            wheel.location = Vector((x, y, z_pos))
            # 3/4 beauty angle: rotate around Z by -65 deg so outer face (+X) faces camera (-Y)
            wheel.rotation_euler = Euler((0, 0, math.radians(-65)), 'XYZ')
            
            # Pedestal
            bpy.ops.mesh.primitive_cylinder_add(radius=ped_radius, depth=ped_height, location=(x, y, ped_height/2.0))
            ped = bpy.context.active_object
            ped.name = f"Pedestal_{wheel.name}"
            ped.data.materials.append(mats["pedestal"])
            
            bpy.ops.mesh.primitive_cylinder_add(radius=ped_radius - 0.04, depth=0.02, location=(x, y, ped_height + 0.01))
            ped_top = bpy.context.active_object
            ped_top.name = f"PedestalTop_{wheel.name}"
            ped_top.data.materials.append(mats["pedestal_top"])
            
            # 3D Text Label
            bpy.ops.object.text_add(location=(x - 0.65, y - 0.82, 0.02), rotation=(math.radians(90), 0, 0))
            txt = bpy.context.active_object
            txt.name = f"Label_{wheel.name}"
            txt.data.body = name_label
            txt.data.size = 0.10
            txt.data.extrude = 0.015
            txt.data.materials.append(mats["text_gold"])

    # Front row: Lower / sleeker vehicles
    place_row(wheels_front, row_y=-1.5, label_start_idx=1)
    
    # Back row: Taller / heavy / industrial vehicles
    place_row(wheels_back, row_y=1.5, label_start_idx=7)

    # Studio Floor & Cyclorama Backdrop
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0))
    floor = bpy.context.active_object
    floor.name = "Showroom_Floor"
    floor_mat = get_or_create_mat("Mat_ShowroomFloor", (0.05, 0.05, 0.06, 1.0), metallic=0.25, roughness=0.25)
    floor.data.materials.append(floor_mat)

    # Curved seamless backdrop wall
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 10, 10), rotation=(math.radians(90), 0, 0))
    wall = bpy.context.active_object
    wall.name = "Showroom_Wall"
    wall_mat = get_or_create_mat("Mat_ShowroomWall", (0.07, 0.08, 0.09, 1.0), metallic=0.1, roughness=0.5)
    wall.data.materials.append(wall_mat)

    # Studio Lighting: Key, Fill, Rim
    # Key Light
    bpy.ops.object.light_add(type='SUN', location=(8, -12, 16), rotation=(math.radians(45), math.radians(22), math.radians(-32)))
    sun = bpy.context.active_object
    sun.name = "Key_Sun"
    sun.data.energy = 5.2
    sun.data.color = (1.0, 0.97, 0.92)

    # Fill Light (Cool Soft)
    bpy.ops.object.light_add(type='AREA', location=(-10, -8, 9), rotation=(math.radians(52), math.radians(-35), math.radians(20)))
    fill = bpy.context.active_object
    fill.name = "Fill_Light"
    fill.data.energy = 800.0
    fill.data.size = 14.0
    fill.data.color = (0.75, 0.88, 1.0)

    # Rim Backlight
    bpy.ops.object.light_add(type='AREA', location=(0, 10, 8), rotation=(math.radians(-62), 0, 0))
    rim = bpy.context.active_object
    rim.name = "Rim_Light"
    rim.data.energy = 1200.0
    rim.data.size = 18.0
    rim.data.color = (0.9, 0.95, 1.0)

    # Camera perfectly framing both rows without edge clipping
    bpy.ops.object.camera_add(location=(0, -12.8, 5.6), rotation=(math.radians(70), 0, 0))
    cam = bpy.context.active_object
    cam.name = "Showroom_Camera"
    cam.data.lens = 38
    bpy.context.scene.camera = cam

# -----------------------------------------------------------------------------
# Main Execution
# -----------------------------------------------------------------------------
def main():
    print("=== STARTING REFINED PROCEDURAL WHEELS GENERATION ===")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mats = setup_all_materials()

    print("Building all 12 specialized wheels...")
    w1 = build_01_sport_racing(mats)
    w2 = build_02_offroad_4x4(mats)
    w3 = build_03_heavy_truck(mats)
    w4 = build_04_agricultural_tractor(mats)
    w5 = build_05_classic_muscle(mats)
    w6 = build_06_formula_slick(mats)
    w7 = build_07_cyber_futuristic(mats)
    w8 = build_08_military_armored(mats)
    w9 = build_09_motorcycle_sport(mats)
    w10 = build_10_bicycle_retro(mats)
    w11 = build_11_construction_loader(mats)
    w12 = build_12_airplane_landing(mats)

    all_wheels = [w1, w2, w3, w4, w5, w6, w7, w8, w9, w10, w11, w12]

    # Export individual FBX models for Unity / game engine
    print("Exporting individual FBX models...")
    for w in all_wheels:
        bpy.ops.object.select_all(action='DESELECT')
        w.select_set(True)
        bpy.context.view_layer.objects.active = w
        
        fbx_path = os.path.join(ASSETS_MODELS_DIR, f"{w.name}.fbx")
        try:
            bpy.ops.export_scene.fbx(
                filepath=fbx_path,
                use_selection=True,
                apply_unit_scale=True,
                axis_forward='-Z',
                axis_up='Y'
            )
            print(f"Exported FBX: {fbx_path}")
        except Exception as e:
            print(f"FBX Export note: {e}")

    # Front row: sleek & street vehicles
    front_row = [
        (w1, "01. Sport Racing"),
        (w5, "02. Muscle Classic"),
        (w6, "03. Formula Slick"),
        (w9, "04. Sport Moto"),
        (w10, "05. Retro Bicycle"),
        (w12, "06. Aircraft Landing")
    ]
    # Back row: tall & heavy industrial vehicles
    back_row = [
        (w2, "07. Off-Road 4x4"),
        (w3, "08. Heavy Truck"),
        (w4, "09. Tractor Agri"),
        (w7, "10. Cyber Sci-Fi"),
        (w8, "11. Military Armored"),
        (w11, "12. Heavy Loader")
    ]

    print("Setting up showroom environment...")
    setup_showroom_scene(front_row, back_row, mats)

    # Save .blend files
    print(f"Saving project .blend to {BLEND_FILE_PATH}...")
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_FILE_PATH)
    bpy.ops.wm.save_as_mainfile(filepath=ASSETS_BLEND_PATH)
    print(f"Saved Assets .blend to {ASSETS_BLEND_PATH}")

    # Render showroom image
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
    print("=== ALL WHEELS GENERATED AND RENDERED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
