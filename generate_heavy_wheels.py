"""
Realistic & Modular Heavy Vehicle Wheel Pack Generator for Blender 5.2
Creates specialized heavy vehicle wheels and modular parts:
 1. Standard Dual Heavy Truck Wheels (Rodado Duplo Padrão)
 2. Asymmetric Dual Wheels - 1 Narrow + 1 Normal (Rodado Duplo Misto / Banhado e Apoio)
 3. Heavy Off-Road / Mud Dual Wheels with mud-ejector clearance (Rodado Duplo Lama / Florestal)
 4. Swamp & Bog Flotation Balloon Wheel (Pneu Balão de Banhado / Flotação)
 5. Arctic / Marsh Ultra-Wide Low-Pressure Balloon (Pneu Balão Baixa Pressão para Lamaçal)
 6. Heavy Super-Single Wide-Base Hauler (Pneu Super Single Rodoviário)
 7. Heavy Front Steer Axle Wheel (Roda Dianteira Direcional Pesada)
 8. Forestry Swamp Traction Wheel with Deep Lugs (Pneu Garra Profunda Florestal / Banhado)

Also exports individual modular components:
 - Mod_Tire_Standard, Mod_Tire_Narrow, Mod_Tire_Offroad, Mod_Tire_Swamp_Balloon
 - Mod_Rim_Dual_Inward, Mod_Rim_Front_Outward, Mod_Rim_Flotation_Wide
 - Mod_Hub_Dual_Drive, Mod_Hub_Front_Steer, Mod_Brake_Drum_Heavy
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector, Euler

PROJECT_DIR = r"E:\Backup-C\Projetos\testejogo"
BLEND_FILE_NAME = "Heavy_Wheels_Collection.blend"
BLEND_FILE_PATH = os.path.join(PROJECT_DIR, BLEND_FILE_NAME)
ASSETS_MODELS_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "Vehicles", "HeavyWheelsPack")
ASSETS_BLEND_PATH = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "Vehicles", BLEND_FILE_NAME)
RENDER_OUTPUT_PATH = os.path.join(PROJECT_DIR, "heavy_wheels_showcase_render.png")

os.makedirs(ASSETS_MODELS_DIR, exist_ok=True)

# -----------------------------------------------------------------------------
# Material Management
# -----------------------------------------------------------------------------
def get_or_create_mat(name, base_color, metallic=0.0, roughness=0.5, bump_rough=0.0):
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
    return mat

def setup_heavy_materials():
    return {
        "rubber_tread": get_or_create_mat("Mat_Heavy_Rubber_Tread", (0.045, 0.045, 0.045, 1.0), 0.0, 0.85),
        "rubber_mud": get_or_create_mat("Mat_Heavy_Rubber_Mud", (0.04, 0.04, 0.04, 1.0), 0.0, 0.90),
        "rubber_sidewall": get_or_create_mat("Mat_Heavy_Rubber_Sidewall", (0.055, 0.055, 0.055, 1.0), 0.0, 0.72),
        "rim_silver": get_or_create_mat("Mat_Heavy_Rim_Silver", (0.76, 0.78, 0.80, 1.0), 0.92, 0.25),
        "rim_dark": get_or_create_mat("Mat_Heavy_Rim_Dark", (0.12, 0.13, 0.14, 1.0), 0.88, 0.32),
        "rim_yellow": get_or_create_mat("Mat_Heavy_Rim_Yellow", (0.92, 0.64, 0.06, 1.0), 0.15, 0.38),
        "rim_orange_forestry": get_or_create_mat("Mat_Heavy_Rim_Orange", (0.88, 0.35, 0.04, 1.0), 0.2, 0.40),
        "hub_iron": get_or_create_mat("Mat_Heavy_Hub_Iron", (0.18, 0.19, 0.20, 1.0), 0.85, 0.45),
        "brake_drum": get_or_create_mat("Mat_Heavy_Brake_Drum", (0.35, 0.36, 0.38, 1.0), 0.90, 0.40),
        "bolts": get_or_create_mat("Mat_Heavy_Bolts", (0.82, 0.84, 0.88, 1.0), 0.98, 0.15),
        "chrome_cap": get_or_create_mat("Mat_Heavy_Chrome_Cap", (0.95, 0.95, 0.96, 1.0), 1.0, 0.05),
        "pedestal": get_or_create_mat("Mat_Heavy_Pedestal", (0.08, 0.08, 0.09, 1.0), 0.35, 0.45),
        "pedestal_top": get_or_create_mat("Mat_Heavy_Pedestal_Top", (0.16, 0.16, 0.18, 1.0), 0.55, 0.3),
        "text_gold": get_or_create_mat("Mat_Heavy_Text_Gold", (0.95, 0.82, 0.28, 1.0), 0.85, 0.2)
    }

# -----------------------------------------------------------------------------
# BMesh Procedural Modeling Engine
# -----------------------------------------------------------------------------
class HeavyMeshBuilder:
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
        
        v_back, v_front = [], []
        for i in range(verts):
            th = 2 * math.pi * i / verts
            y = radius * math.cos(th)
            z = radius * math.sin(th)
            v_back.append(self.bm.verts.new((x0, y, z)))
            v_front.append(self.bm.verts.new((x1, y, z)))
            
        for i in range(verts):
            ni = (i + 1) % verts
            f = self.bm.faces.new((v_back[i], v_front[i], v_front[ni], v_back[ni]))
            f.material_index = m_idx
            
        f_front = self.bm.faces.new(reversed(v_front))
        f_front.material_index = m_idx
        f_back = self.bm.faces.new(v_back)
        f_back.material_index = m_idx

    def add_tube(self, outer_r, inner_r, depth, x_center=0.0, verts=32, mat=None):
        m_idx = self.get_mat_idx(mat) if mat else 0
        x0 = x_center - depth / 2.0
        x1 = x_center + depth / 2.0
        
        vo_b, vo_f, vi_b, vi_f = [], [], [], []
        for i in range(verts):
            th = 2 * math.pi * i / verts
            yo, zo = outer_r * math.cos(th), outer_r * math.sin(th)
            yi, zi = inner_r * math.cos(th), inner_r * math.sin(th)
            vo_b.append(self.bm.verts.new((x0, yo, zo)))
            vo_f.append(self.bm.verts.new((x1, yo, zo)))
            vi_b.append(self.bm.verts.new((x0, yi, zi)))
            vi_f.append(self.bm.verts.new((x1, yi, zi)))
            
        for i in range(verts):
            ni = (i + 1) % verts
            f1 = self.bm.faces.new((vo_b[i], vo_f[i], vo_f[ni], vo_b[ni]))
            f1.material_index = m_idx
            f2 = self.bm.faces.new((vi_f[i], vi_b[i], vi_b[ni], vi_f[ni]))
            f2.material_index = m_idx
            f3 = self.bm.faces.new((vo_f[i], vi_f[i], vi_f[ni], vo_f[ni]))
            f3.material_index = m_idx
            f4 = self.bm.faces.new((vi_b[i], vo_b[i], vo_b[ni], vi_b[ni]))
            f4.material_index = m_idx

    def add_tire_profile(self, profile, x_offset=0.0, verts=36, mat=None):
        """
        Creates a custom rotational tire profile:
        profile is a list of (dx, r) pairs relative to x_offset.
        """
        m_idx = self.get_mat_idx(mat) if mat else 0
        rings = []
        for dx, r in profile:
            ring = []
            x = x_offset + dx
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
                
        # Inner rim bed bridge
        for i in range(verts):
            ni = (i + 1) % verts
            f = self.bm.faces.new((rings[-1][i], rings[-1][ni], rings[0][ni], rings[0][i]))
            f.material_index = m_idx

    def add_box(self, dims, loc=(0,0,0), rot_x=0.0, mat=None):
        m_idx = self.get_mat_idx(mat) if mat else 0
        dx, dy, dz = dims[0]/2, dims[1]/2, dims[2]/2
        corners = [
            (-dx, -dy, -dz), (-dx, dy, -dz), (-dx, dy, dz), (-dx, -dy, dz),
            (dx, -dy, -dz), (dx, dy, -dz), (dx, dy, dz), (dx, -dy, dz)
        ]
        cos_a = math.cos(rot_x)
        sin_a = math.sin(rot_x)
        v_objs = []
        for cx, cy, cz in corners:
            ry = cy * cos_a - cz * sin_a
            rz = cy * sin_a + cz * cos_a
            v_objs.append(self.bm.verts.new((cx + loc[0], ry + loc[1], rz + loc[2])))
            
        faces_idx = [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]
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

    def add_commercial_dish_rim(self, x_center, outer_r=0.29, inner_r=0.25, width=0.22, is_outward=False, mat_rim=None, mats=None):
        """Creates a stamped commercial steel truck rim with ventilation hand holes and dish."""
        # Rim barrel
        self.add_tube(outer_r=outer_r, inner_r=inner_r, depth=width, x_center=x_center, verts=36, mat=mat_rim)
        # Dish offset: if outward (front steer), dish curves outward; if inward (rear dual), curves inward
        dish_x = x_center + (0.05 if is_outward else -0.05)
        self.add_cylinder(radius=inner_r + 0.005, depth=0.035, x_center=dish_x, verts=36, mat=mat_rim)
        
        # 8 ventilation oval/round holes around perimeter of dish
        for i in range(8):
            a = 2 * math.pi * i / 8 + 0.2
            y = 0.20 * math.cos(a)
            z = 0.20 * math.sin(a)
            self.add_cylinder(radius=0.02, depth=0.045, x_center=dish_x, verts=16, mat=mats["hub_iron"])

    def finish_object(self):
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=0.0001)
        mesh = bpy.data.meshes.new(self.name + "_Mesh")
        self.bm.to_mesh(mesh)
        self.bm.free()
        
        obj = bpy.data.objects.new(self.name, mesh)
        for m in self.materials:
            obj.data.materials.append(m)
        bpy.context.collection.objects.link(obj)
        for poly in mesh.polygons:
            poly.use_smooth = True
        return obj

# -----------------------------------------------------------------------------
# Wheel Definitions & Builders
# -----------------------------------------------------------------------------

def build_heavy_dual_standard(mats):
    """
    Standard Commercial Dual Tires (Rodado Duplo Padrão).
    Two identical heavy highway truck tires mounted on a dual hub with spacer.
    Total width ~0.56m, diameter 1.04m (radius 0.52m).
    """
    b = HeavyMeshBuilder("Wheel_Heavy_Dual_Standard")
    tire_w = 0.24
    inner_x = -0.16
    outer_x = 0.16
    
    # Profile for standard highway commercial tire
    std_profile = [
        (-tire_w/2, 0.29),
        (-tire_w/2, 0.38),
        (-tire_w/2 + 0.04, 0.52),
        (tire_w/2 - 0.04, 0.52),
        (tire_w/2, 0.38),
        (tire_w/2, 0.29)
    ]
    
    # 1. Inner and Outer Tires
    b.add_tire_profile(std_profile, x_offset=inner_x, verts=36, mat=mats["rubber_tread"])
    b.add_tire_profile(std_profile, x_offset=outer_x, verts=36, mat=mats["rubber_tread"])
    
    # Circumferential grooves on both tires
    for t_x in [inner_x, outer_x]:
        for g_off in [-0.06, 0.0, 0.06]:
            b.add_tube(outer_r=0.523, inner_r=0.518, depth=0.018, x_center=t_x + g_off, verts=36, mat=mats["rubber_mud"])
            
    # 2. Dual Rims: Inner rim and Outer rim (both dish inward towards center axle)
    b.add_commercial_dish_rim(x_center=inner_x, width=0.22, is_outward=False, mat_rim=mats["rim_silver"], mats=mats)
    b.add_commercial_dish_rim(x_center=outer_x, width=0.22, is_outward=False, mat_rim=mats["rim_silver"], mats=mats)
    
    # 3. Intermediate Spacer Ring between dual wheels
    b.add_tube(outer_r=0.26, inner_r=0.23, depth=0.10, x_center=0.0, verts=36, mat=mats["hub_iron"])
    
    # 4. Central Heavy Drive Axle Hub & Grease Cap
    b.add_cylinder(radius=0.10, depth=0.14, x_center=0.20, verts=24, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.055, depth=0.04, x_center=0.28, verts=24, mat=mats["chrome_cap"])
    
    # 10 Wheel Studs & Heavy Hex Nuts
    b.add_bolts_circle(10, 0.165, x_pos=0.10, bolt_r=0.013, depth=0.045, mat=mats["bolts"])
    
    # Heavy Cast-Iron Brake Drum on the inside chassis side (-X)
    b.add_cylinder(radius=0.24, depth=0.12, x_center=-0.32, verts=36, mat=mats["brake_drum"])
    # Brake drum cooling fins
    b.add_tube(outer_r=0.248, inner_r=0.240, depth=0.015, x_center=-0.30, verts=36, mat=mats["hub_iron"])
    b.add_tube(outer_r=0.248, inner_r=0.240, depth=0.015, x_center=-0.34, verts=36, mat=mats["hub_iron"])
    
    return b.finish_object()


def build_heavy_dual_asymmetric(mats):
    """
    Asymmetric Dual Wheels (Rodado Duplo Misto: 1 Fino + 1 Normal).
    Specifically requested: One narrow auxiliary/support tire alongside one normal wide drive tire.
    Used in severe mud/swamp hauling, multi-terrain support axles, and heavy logging setups.
    Inner = Normal heavy tire (width 0.25m, radius 0.52m).
    Outer = Narrow tire (width 0.14m, radius 0.52m) on offset spacer hub.
    """
    b = HeavyMeshBuilder("Wheel_Heavy_Dual_Asymmetric")
    
    # 1. Inner Normal Tire (W = 0.25m)
    inner_w = 0.25
    inner_x = -0.15
    inner_profile = [
        (-inner_w/2, 0.29),
        (-inner_w/2, 0.38),
        (-inner_w/2 + 0.04, 0.52),
        (inner_w/2 - 0.04, 0.52),
        (inner_w/2, 0.38),
        (inner_w/2, 0.29)
    ]
    b.add_tire_profile(inner_profile, x_offset=inner_x, verts=36, mat=mats["rubber_tread"])
    # 3 circumferential grooves on normal tire
    for g_off in [-0.06, 0.0, 0.06]:
        b.add_tube(outer_r=0.523, inner_r=0.518, depth=0.018, x_center=inner_x + g_off, verts=36, mat=mats["rubber_mud"])
    b.add_commercial_dish_rim(x_center=inner_x, width=0.22, is_outward=False, mat_rim=mats["rim_dark"], mats=mats)
    
    # 2. Outer Narrow Tire (W = 0.14m - Pneu Fino Auxiliar)
    narrow_w = 0.14
    outer_x = 0.15
    narrow_profile = [
        (-narrow_w/2, 0.29),
        (-narrow_w/2, 0.38),
        (-narrow_w/2 + 0.025, 0.52),
        (narrow_w/2 - 0.025, 0.52),
        (narrow_w/2, 0.38),
        (narrow_w/2, 0.29)
    ]
    b.add_tire_profile(narrow_profile, x_offset=outer_x, verts=36, mat=mats["rubber_tread"])
    # 1 central guide groove on narrow tire
    b.add_tube(outer_r=0.523, inner_r=0.518, depth=0.02, x_center=outer_x, verts=36, mat=mats["rubber_mud"])
    # Narrow steel rim
    b.add_commercial_dish_rim(x_center=outer_x, outer_r=0.29, inner_r=0.25, width=0.12, is_outward=False, mat_rim=mats["rim_silver"], mats=mats)
    
    # 3. Asymmetric Heavy Adapter Spacer Hub connecting the different rim widths
    b.add_tube(outer_r=0.26, inner_r=0.22, depth=0.12, x_center=0.0, verts=36, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.10, depth=0.14, x_center=0.18, verts=24, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.055, depth=0.035, x_center=0.26, verts=24, mat=mats["chrome_cap"])
    
    # 10 Heavy wheel studs with hex nuts
    b.add_bolts_circle(10, 0.165, x_pos=0.10, bolt_r=0.013, depth=0.045, mat=mats["bolts"])
    
    # Brake drum on the inside
    b.add_cylinder(radius=0.24, depth=0.12, x_center=-0.32, verts=36, mat=mats["brake_drum"])
    
    return b.finish_object()


def build_heavy_dual_offroad_mud(mats):
    """
    Heavy Off-Road / Mud Duals with Mud Ejector Clearance.
    Two aggressive all-terrain/mud cross-lug tires spaced for mud ejection.
    Used in 6x6 logging, sugarcane, and off-road mining trucks.
    """
    b = HeavyMeshBuilder("Wheel_Heavy_Dual_OffroadMud")
    tire_w = 0.24
    inner_x = -0.17
    outer_x = 0.17
    
    mud_profile = [
        (-tire_w/2, 0.29),
        (-tire_w/2, 0.39),
        (-tire_w/2 + 0.04, 0.53),
        (tire_w/2 - 0.04, 0.53),
        (tire_w/2, 0.39),
        (tire_w/2, 0.29)
    ]
    
    # 1. Base tires
    b.add_tire_profile(mud_profile, x_offset=inner_x, verts=36, mat=mats["rubber_mud"])
    b.add_tire_profile(mud_profile, x_offset=outer_x, verts=36, mat=mats["rubber_mud"])
    
    # 2. Aggressive cross-traction mud lugs on both tires
    lugs_count = 18
    for t_x in [inner_x, outer_x]:
        for i in range(lugs_count):
            th = 2 * math.pi * i / lugs_count
            y = 0.532 * math.cos(th)
            z = 0.532 * math.sin(th)
            # Staggered aggressive cleat
            x_shift = 0.04 if i % 2 == 0 else -0.04
            b.add_box(dims=(0.14, 0.05, 0.03), loc=(t_x + x_shift, y, z), rot_x=th, mat=mats["rubber_mud"])
            
    # 3. Off-Road Reinforced Rims (Dark industrial finish)
    b.add_commercial_dish_rim(x_center=inner_x, width=0.22, is_outward=False, mat_rim=mats["rim_dark"], mats=mats)
    b.add_commercial_dish_rim(x_center=outer_x, width=0.22, is_outward=False, mat_rim=mats["rim_dark"], mats=mats)
    
    # 4. Planetary gear reduction hub in center
    b.add_cylinder(radius=0.13, depth=0.12, x_center=0.20, verts=24, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.08, depth=0.04, x_center=0.27, verts=24, mat=mats["chrome_cap"])
    b.add_bolts_circle(10, 0.165, x_pos=0.10, bolt_r=0.013, depth=0.045, mat=mats["bolts"])
    b.add_bolts_circle(8, 0.10, x_pos=0.25, bolt_r=0.010, depth=0.03, mat=mats["bolts"])
    
    # Heavy brake drum
    b.add_cylinder(radius=0.24, depth=0.12, x_center=-0.33, verts=36, mat=mats["brake_drum"])
    
    return b.finish_object()


def build_heavy_swamp_flotation(mats):
    """
    Swamp & Bog Flotation Balloon Wheel (Pneu Balão de Banhado / Flotação).
    Requested specifically: "pneus de banhado e afins".
    Massive ultra-wide rounded flotation tire (width 0.52m, radius 0.58m).
    Equipped with curved paddle traction cleats that displace water and grip deep mud/swamp.
    Deep wide dish rim with planetary hub.
    """
    b = HeavyMeshBuilder("Wheel_Heavy_Swamp_Flotation")
    w = 0.52
    r_outer = 0.58
    r_rim = 0.28
    
    # Flotation balloon curved profile
    flotation_profile = [
        (-w/2, r_rim),
        (-w/2, r_rim + 0.10),
        (-w/2 + 0.08, r_outer - 0.04),
        (-w/4, r_outer),
        (0.0, r_outer + 0.01),
        (w/4, r_outer),
        (w/2 - 0.08, r_outer - 0.04),
        (w/2, r_rim + 0.10),
        (w/2, r_rim)
    ]
    b.add_tire_profile(flotation_profile, x_offset=0.0, verts=36, mat=mats["rubber_mud"])
    
    # Swamp paddle cleats (Garras de Pá para Lamaçal / Banhado)
    paddle_count = 16
    for i in range(paddle_count):
        th = 2 * math.pi * i / paddle_count
        y = (r_outer + 0.01) * math.cos(th)
        z = (r_outer + 0.01) * math.sin(th)
        # Wide curved paddle cleat across tire surface
        b.add_box(dims=(0.42, 0.065, 0.04), loc=(0.0, y, z), rot_x=th, mat=mats["rubber_mud"])
        
    # Super-wide flotation rim barrel
    b.add_tube(outer_r=r_rim, inner_r=r_rim - 0.04, depth=w - 0.04, x_center=0.0, verts=36, mat=mats["rim_yellow"])
    # Deep dish center mounting plate
    b.add_cylinder(radius=r_rim - 0.035, depth=0.06, x_center=0.12, verts=36, mat=mats["rim_yellow"])
    
    # Heavy planetary reduction hub for high torque swamp crawling
    b.add_cylinder(radius=0.14, depth=0.12, x_center=0.18, verts=24, mat=mats["rim_dark"])
    b.add_cylinder(radius=0.08, depth=0.04, x_center=0.25, verts=24, mat=mats["chrome_cap"])
    
    # 12 Heavy duty hub bolts
    b.add_bolts_circle(12, 0.19, x_pos=0.145, bolt_r=0.014, depth=0.035, mat=mats["bolts"])
    
    # Brake drum on inner side
    b.add_cylinder(radius=0.23, depth=0.10, x_center=-0.22, verts=36, mat=mats["brake_drum"])
    
    return b.finish_object()


def build_heavy_swamp_arctic_balloon(mats):
    """
    Arctic & Marsh Ultra-Wide Low-Pressure Balloon (Pneu Balão Baixa Pressão para Lamaçal / Pantanal).
    Similar to Trecol / Sherp / Marsh Buggy tires:
    Extremely bulbous spherical shape designed to float over swamps, deep peat bogs, and snow.
    Diameter 1.10m (radius 0.55m), width 0.60m!
    Shallow directional water ribs.
    """
    b = HeavyMeshBuilder("Wheel_Heavy_Swamp_Arctic_Balloon")
    w = 0.60
    r_outer = 0.55
    r_rim = 0.25
    
    # Bulbous balloon tire cross section
    steps = 10
    balloon_profile = []
    for s in range(steps + 1):
        frac = s / steps  # 0 to 1
        angle = math.pi * frac  # 0 to pi
        dx = (frac - 0.5) * w
        # Rounded sphere-like swelling
        r = r_rim + (r_outer - r_rim) * math.sin(angle)
        balloon_profile.append((dx, r))
        
    b.add_tire_profile(balloon_profile, x_offset=0.0, verts=36, mat=mats["rubber_tread"])
    
    # Directional amphibious / swamp ribs
    ribs_count = 14
    for i in range(ribs_count):
        th = 2 * math.pi * i / ribs_count
        y = r_outer * math.cos(th)
        z = r_outer * math.sin(th)
        b.add_box(dims=(0.48, 0.04, 0.025), loc=(0.0, y, z), rot_x=th, mat=mats["rubber_tread"])
        
    # Lightweight beadlock rim plate
    b.add_tube(outer_r=r_rim, inner_r=r_rim - 0.035, depth=w - 0.08, x_center=0.0, verts=36, mat=mats["rim_dark"])
    b.add_cylinder(radius=r_rim - 0.03, depth=0.05, x_center=0.15, verts=36, mat=mats["rim_silver"])
    
    # Central sealed amphibious CTIS hub (Central Tire Inflation System)
    b.add_cylinder(radius=0.11, depth=0.09, x_center=0.20, verts=24, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.06, depth=0.035, x_center=0.25, verts=12, mat=mats["chrome_cap"])
    b.add_bolts_circle(10, 0.16, x_pos=0.17, bolt_r=0.012, depth=0.03, mat=mats["bolts"])
    
    return b.finish_object()


def build_heavy_super_single_highway(mats):
    """
    Heavy Super-Single Wide-Base Hauler (Pneu Super Single Rodoviário).
    Replaces a traditional dual wheel assembly with one high-capacity wide-base tire.
    Width 0.44m, diameter 1.04m (radius 0.52m).
    Smooth fuel-saving multi-rib highway tread, polished forged alloy deep-dish rim.
    """
    b = HeavyMeshBuilder("Wheel_Heavy_Super_Single_Highway")
    w = 0.44
    r_outer = 0.52
    r_rim = 0.29
    
    ss_profile = [
        (-w/2, r_rim),
        (-w/2, r_rim + 0.10),
        (-w/2 + 0.04, r_outer),
        (w/2 - 0.04, r_outer),
        (w/2, r_rim + 0.10),
        (w/2, r_rim)
    ]
    b.add_tire_profile(ss_profile, x_offset=0.0, verts=36, mat=mats["rubber_tread"])
    
    # 5 Long-haul circumferential grooves
    for g_x in [-0.14, -0.07, 0.0, 0.07, 0.14]:
        b.add_tube(outer_r=r_outer + 0.003, inner_r=r_outer - 0.004, depth=0.018, x_center=g_x, verts=36, mat=mats["rubber_mud"])
        
    # Polished forged aluminum deep dish rim
    b.add_commercial_dish_rim(x_center=0.0, outer_r=r_rim, inner_r=r_rim - 0.04, width=w - 0.04, is_outward=False, mat_rim=mats["rim_silver"], mats=mats)
    
    # Axle hub
    b.add_cylinder(radius=0.10, depth=0.12, x_center=0.12, verts=24, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.055, depth=0.035, x_center=0.19, verts=24, mat=mats["chrome_cap"])
    b.add_bolts_circle(10, 0.165, x_pos=0.02, bolt_r=0.013, depth=0.045, mat=mats["bolts"])
    
    # Inside brake drum
    b.add_cylinder(radius=0.24, depth=0.12, x_center=-0.22, verts=36, mat=mats["brake_drum"])
    
    return b.finish_object()


def build_heavy_front_steer(mats):
    """
    Heavy Front Steer Axle Wheel (Roda Dianteira Direcional Pesada).
    Front steer wheels have OUTWARD-curving rims (convex dish), whereas rear wheels curve inward.
    Single heavy truck tire (width 0.28m, radius 0.52m) with steering tie-rod hub and disc/drum.
    """
    b = HeavyMeshBuilder("Wheel_Heavy_Front_Steer")
    w = 0.28
    r_outer = 0.52
    r_rim = 0.29
    
    steer_profile = [
        (-w/2, r_rim),
        (-w/2, r_rim + 0.09),
        (-w/2 + 0.04, r_outer),
        (w/2 - 0.04, r_outer),
        (w/2, r_rim + 0.09),
        (w/2, r_rim)
    ]
    b.add_tire_profile(steer_profile, x_offset=0.0, verts=36, mat=mats["rubber_tread"])
    
    # 4 Steer directional grooves
    for g_x in [-0.08, -0.025, 0.025, 0.08]:
        b.add_tube(outer_r=r_outer + 0.003, inner_r=r_outer - 0.003, depth=0.016, x_center=g_x, verts=36, mat=mats["rubber_mud"])
        
    # Outward curved convex steel rim (Curvatura Externa Direcional)
    b.add_commercial_dish_rim(x_center=0.0, outer_r=r_rim, inner_r=r_rim - 0.04, width=w - 0.04, is_outward=True, mat_rim=mats["rim_silver"], mats=mats)
    
    # Front steer spindle grease hub
    b.add_cylinder(radius=0.085, depth=0.08, x_center=0.12, verts=24, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.045, depth=0.03, x_center=0.17, verts=24, mat=mats["chrome_cap"])
    
    # 10 Front lug nuts
    b.add_bolts_circle(10, 0.165, x_pos=0.08, bolt_r=0.013, depth=0.04, mat=mats["bolts"])
    
    # Front brake assembly
    b.add_cylinder(radius=0.23, depth=0.10, x_center=-0.14, verts=36, mat=mats["brake_drum"])
    
    return b.finish_object()


def build_heavy_forestry_swamp_bogie(mats):
    """
    Heavy Forestry Swamp Traction Wheel with Deep Lugs (Pneu Garra Profunda Florestal / Banhado).
    Equipped with heavy forestry chevron lugs and reinforced rim rim-guard.
    Used in logging skidders, forwarders, and swamp transporters traversing thick mud, bogs, and tree debris.
    Width 0.46m, diameter 1.16m (radius 0.58m).
    """
    b = HeavyMeshBuilder("Wheel_Heavy_Forestry_Swamp_Bogie")
    w = 0.46
    r_outer = 0.58
    r_rim = 0.30
    
    forest_profile = [
        (-w/2, r_rim),
        (-w/2, r_rim + 0.12),
        (-w/2 + 0.05, r_outer),
        (w/2 - 0.05, r_outer),
        (w/2, r_rim + 0.12),
        (w/2, r_rim)
    ]
    b.add_tire_profile(forest_profile, x_offset=0.0, verts=36, mat=mats["rubber_mud"])
    
    # 16 Heavy forestry chevron traction blades (Garras de Banhado Florestal)
    lug_count = 16
    for i in range(lug_count):
        th = 2 * math.pi * i / lug_count
        y = (r_outer + 0.01) * math.cos(th)
        z = (r_outer + 0.01) * math.sin(th)
        # Angled lug left
        b.add_box(dims=(0.20, 0.07, 0.045), loc=(-0.10, y, z), rot_x=th, mat=mats["rubber_mud"])
        # Angled lug right (staggered)
        th_r = th + (math.pi / lug_count)
        y_r = (r_outer + 0.01) * math.cos(th_r)
        z_r = (r_outer + 0.01) * math.sin(th_r)
        b.add_box(dims=(0.20, 0.07, 0.045), loc=(0.10, y_r, z_r), rot_x=th_r, mat=mats["rubber_mud"])
        
    # Reinforced forestry steel rim in Safety Orange
    b.add_tube(outer_r=r_rim, inner_r=r_rim - 0.04, depth=w - 0.04, x_center=0.0, verts=36, mat=mats["rim_orange_forestry"])
    # Heavy protective outer flange rim-guard (proteção de bordo contra tocos e lama)
    b.add_tube(outer_r=r_rim + 0.02, inner_r=r_rim - 0.01, depth=0.035, x_center=w/2 - 0.02, verts=36, mat=mats["rim_orange_forestry"])
    
    # Heavy central dish & planetary hub
    b.add_cylinder(radius=r_rim - 0.035, depth=0.06, x_center=0.10, verts=36, mat=mats["rim_orange_forestry"])
    b.add_cylinder(radius=0.13, depth=0.12, x_center=0.15, verts=24, mat=mats["rim_dark"])
    b.add_cylinder(radius=0.07, depth=0.04, x_center=0.22, verts=24, mat=mats["chrome_cap"])
    
    b.add_bolts_circle(12, 0.19, x_pos=0.13, bolt_r=0.014, depth=0.035, mat=mats["bolts"])
    b.add_cylinder(radius=0.24, depth=0.10, x_center=-0.20, verts=36, mat=mats["brake_drum"])
    
    return b.finish_object()

# -----------------------------------------------------------------------------
# Modular Components Builders (Separate Models)
# -----------------------------------------------------------------------------
def build_modular_components(mats):
    """Generates independent modular components for custom in-game wheel assembly."""
    mods = []
    
    # 1. Mod_Tire_Standard
    b1 = HeavyMeshBuilder("Mod_Tire_Standard")
    w1, r_out1, r_in1 = 0.25, 0.52, 0.29
    prof1 = [(-w1/2, r_in1), (-w1/2, 0.38), (-w1/2 + 0.04, r_out1), (w1/2 - 0.04, r_out1), (w1/2, 0.38), (w1/2, r_in1)]
    b1.add_tire_profile(prof1, x_offset=0.0, verts=36, mat=mats["rubber_tread"])
    mods.append(b1.finish_object())
    
    # 2. Mod_Tire_Narrow
    b2 = HeavyMeshBuilder("Mod_Tire_Narrow")
    w2, r_out2, r_in2 = 0.14, 0.52, 0.29
    prof2 = [(-w2/2, r_in2), (-w2/2, 0.38), (-w2/2 + 0.025, r_out2), (w2/2 - 0.025, r_out2), (w2/2, 0.38), (w2/2, r_in2)]
    b2.add_tire_profile(prof2, x_offset=0.0, verts=36, mat=mats["rubber_tread"])
    mods.append(b2.finish_object())
    
    # 3. Mod_Tire_Swamp_Flotation
    b3 = HeavyMeshBuilder("Mod_Tire_Swamp_Flotation")
    w3, r_out3, r_in3 = 0.52, 0.58, 0.28
    prof3 = [(-w3/2, r_in3), (-w3/2, r_in3 + 0.10), (-w3/2 + 0.08, r_out3 - 0.04), (0.0, r_out3 + 0.01), (w3/2 - 0.08, r_out3 - 0.04), (w3/2, r_in3 + 0.10), (w3/2, r_in3)]
    b3.add_tire_profile(prof3, x_offset=0.0, verts=36, mat=mats["rubber_mud"])
    for i in range(16):
        th = 2 * math.pi * i / 16
        y = (r_out3 + 0.01) * math.cos(th)
        z = (r_out3 + 0.01) * math.sin(th)
        b3.add_box(dims=(0.42, 0.065, 0.04), loc=(0.0, y, z), rot_x=th, mat=mats["rubber_mud"])
    mods.append(b3.finish_object())
    
    # 4. Mod_Rim_Dual_Inward
    b4 = HeavyMeshBuilder("Mod_Rim_Dual_Inward")
    b4.add_commercial_dish_rim(x_center=0.0, outer_r=0.29, inner_r=0.25, width=0.22, is_outward=False, mat_rim=mats["rim_silver"], mats=mats)
    mods.append(b4.finish_object())
    
    # 5. Mod_Rim_Front_Outward
    b5 = HeavyMeshBuilder("Mod_Rim_Front_Outward")
    b5.add_commercial_dish_rim(x_center=0.0, outer_r=0.29, inner_r=0.25, width=0.24, is_outward=True, mat_rim=mats["rim_silver"], mats=mats)
    mods.append(b5.finish_object())
    
    # 6. Mod_Hub_Dual_Drive
    b6 = HeavyMeshBuilder("Mod_Hub_Dual_Drive")
    b6.add_tube(outer_r=0.26, inner_r=0.22, depth=0.14, x_center=0.0, verts=36, mat=mats["hub_iron"])
    b6.add_cylinder(radius=0.10, depth=0.14, x_center=0.16, verts=24, mat=mats["hub_iron"])
    b6.add_cylinder(radius=0.055, depth=0.04, x_center=0.24, verts=24, mat=mats["chrome_cap"])
    b6.add_bolts_circle(10, 0.165, x_pos=0.08, bolt_r=0.013, depth=0.045, mat=mats["bolts"])
    mods.append(b6.finish_object())
    
    # 7. Mod_Brake_Drum_Heavy
    b7 = HeavyMeshBuilder("Mod_Brake_Drum_Heavy")
    b7.add_cylinder(radius=0.24, depth=0.12, x_center=0.0, verts=36, mat=mats["brake_drum"])
    b7.add_tube(outer_r=0.248, inner_r=0.240, depth=0.015, x_center=0.02, verts=36, mat=mats["hub_iron"])
    b7.add_tube(outer_r=0.248, inner_r=0.240, depth=0.015, x_center=-0.02, verts=36, mat=mats["hub_iron"])
    mods.append(b7.finish_object())
    
    return mods

# -----------------------------------------------------------------------------
# Showroom Setup & Studio Staging
# -----------------------------------------------------------------------------
def setup_heavy_showroom(assembled_wheels, mats):
    """
    Arranges 8 complete assembled heavy wheels in 2 tiered rows of 4.
    Front Row: Z=0.0, Y=-1.7
    Back Row: Elevated Z=0.45, Y=1.7 (Stadium tiering so back row is fully visible)
    """
    ped_radius = 0.88
    ped_height = 0.22
    spacing_x = 2.45
    
    front_row = assembled_wheels[:4]
    back_row = assembled_wheels[4:]
    
    row_configs = [
        (front_row, -1.7, 0.0),
        (back_row, 1.7, 0.45)  # Elevated back row
    ]
    
    for row_wheels, row_y, base_z in row_configs:
        for idx, (wheel, label_name) in enumerate(row_wheels):
            x = (idx - 1.5) * spacing_x
            y = row_y
            
            dim_z = wheel.dimensions.z
            z_pos = base_z + ped_height + (dim_z / 2.0)
            wheel.location = Vector((x, y, z_pos))
            # 3/4 angle facing camera (-42 deg around Z) showing both rim face and dual tire depth
            wheel.rotation_euler = Euler((0, 0, math.radians(-42)), 'XYZ')
            
            # Pedestal Base
            bpy.ops.mesh.primitive_cylinder_add(radius=ped_radius, depth=ped_height + base_z, location=(x, y, (ped_height + base_z)/2.0))
            ped = bpy.context.active_object
            ped.name = f"Pedestal_{wheel.name}"
            ped.data.materials.append(mats["pedestal"])
            
            # Pedestal Metallic Rim Cap
            bpy.ops.mesh.primitive_cylinder_add(radius=ped_radius - 0.05, depth=0.02, location=(x, y, base_z + ped_height + 0.01))
            ped_top = bpy.context.active_object
            ped_top.name = f"PedestalTop_{wheel.name}"
            ped_top.data.materials.append(mats["pedestal_top"])
            
            # 3D Gold Text Nameplate
            bpy.ops.object.text_add(location=(x - 0.82, y - 0.95, base_z + 0.02), rotation=(math.radians(90), 0, 0))
            txt = bpy.context.active_object
            txt.name = f"Label_{wheel.name}"
            txt.data.body = label_name
            txt.data.size = 0.105
            txt.data.extrude = 0.018
            txt.data.materials.append(mats["text_gold"])

    # Studio Floor
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0))
    floor = bpy.context.active_object
    floor.name = "Showroom_Floor"
    floor_mat = get_or_create_mat("Mat_ShowroomFloor", (0.05, 0.05, 0.06, 1.0), metallic=0.3, roughness=0.22)
    floor.data.materials.append(floor_mat)

    # Curved Backdrop
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 12, 12), rotation=(math.radians(90), 0, 0))
    wall = bpy.context.active_object
    wall.name = "Showroom_Wall"
    wall_mat = get_or_create_mat("Mat_ShowroomWall", (0.07, 0.08, 0.09, 1.0), metallic=0.1, roughness=0.5)
    wall.data.materials.append(wall_mat)

    # Studio Lighting
    bpy.ops.object.light_add(type='SUN', location=(10, -14, 18), rotation=(math.radians(45), math.radians(24), math.radians(-32)))
    sun = bpy.context.active_object
    sun.name = "Key_Sun"
    sun.data.energy = 5.5
    sun.data.color = (1.0, 0.97, 0.92)

    bpy.ops.object.light_add(type='AREA', location=(-12, -10, 10), rotation=(math.radians(52), math.radians(-35), math.radians(20)))
    fill = bpy.context.active_object
    fill.name = "Fill_Light"
    fill.data.energy = 900.0
    fill.data.size = 16.0
    fill.data.color = (0.75, 0.88, 1.0)

    bpy.ops.object.light_add(type='AREA', location=(0, 12, 9), rotation=(math.radians(-62), 0, 0))
    rim = bpy.context.active_object
    rim.name = "Rim_Light"
    rim.data.energy = 1400.0
    rim.data.size = 20.0
    rim.data.color = (0.9, 0.95, 1.0)

    # Closer Camera perfectly framing the 8 wheels with maximum detail
    bpy.ops.object.camera_add(location=(0, -11.2, 5.2), rotation=(math.radians(68), 0, 0))
    cam = bpy.context.active_object
    cam.name = "Showroom_Camera"
    cam.data.lens = 38
    bpy.context.scene.camera = cam

# -----------------------------------------------------------------------------
# Main Execution
# -----------------------------------------------------------------------------
def main():
    print("=== STARTING HEAVY VEHICLE WHEELS GENERATION ===")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mats = setup_heavy_materials()

    print("Building 8 Complete Assembled Heavy Wheel Variants...")
    w1 = build_heavy_dual_standard(mats)
    w2 = build_heavy_dual_asymmetric(mats)
    w3 = build_heavy_dual_offroad_mud(mats)
    w4 = build_heavy_front_steer(mats)
    w5 = build_heavy_swamp_flotation(mats)
    w6 = build_heavy_swamp_arctic_balloon(mats)
    w7 = build_heavy_super_single_highway(mats)
    w8 = build_heavy_forestry_swamp_bogie(mats)

    assembled_configs = [
        (w1, "01. Dual Standard (Duplo)"),
        (w2, "02. Dual Asymmetric (1 Fino + 1 Normal)"),
        (w3, "03. Dual Off-Road Mud"),
        (w4, "04. Front Steer Heavy"),
        (w5, "05. Swamp Flotation (Banhado)"),
        (w6, "06. Arctic Balloon (Lamaçal)"),
        (w7, "07. Super Single Hauler"),
        (w8, "08. Forestry Swamp Bogie")
    ]

    print("Building Modular Components...")
    modular_parts = build_modular_components(mats)

    # Export all assembled wheels to FBX
    print("Exporting Assembled Wheels to FBX...")
    for w, _ in assembled_configs:
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
            print(f"Exported Assembled FBX: {fbx_path}")
        except Exception as e:
            print(f"FBX error: {e}")

    # Export modular components to FBX
    print("Exporting Modular Components to FBX...")
    for mod in modular_parts:
        bpy.ops.object.select_all(action='DESELECT')
        mod.select_set(True)
        bpy.context.view_layer.objects.active = mod
        fbx_path = os.path.join(ASSETS_MODELS_DIR, f"{mod.name}.fbx")
        try:
            bpy.ops.export_scene.fbx(
                filepath=fbx_path,
                use_selection=True,
                apply_unit_scale=True,
                axis_forward='-Z',
                axis_up='Y'
            )
            print(f"Exported Modular FBX: {fbx_path}")
        except Exception as e:
            print(f"FBX error: {e}")
        # Hide modular components from the showroom main camera stage
        mod.hide_viewport = True
        mod.hide_render = True

    print("Setting up Heavy Showroom Stage...")
    setup_heavy_showroom(assembled_configs, mats)

    # Save Blender project file
    print(f"Saving {BLEND_FILE_PATH}...")
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_FILE_PATH)
    bpy.ops.wm.save_as_mainfile(filepath=ASSETS_BLEND_PATH)
    print(f"Saved {ASSETS_BLEND_PATH}")

    # Render Preview
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
    print("=== HEAVY WHEELS GENERATION COMPLETED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
