"""
Realistic Heavy Vehicle Tires & Wheels Generator for Blender 5.2
Faithfully replicates the user reference images:
 - Image 1 & 3: Curved chevron swamp / agricultural / bog traction tire with shoulder cleats
 - Image 2: Commercial heavy truck tire with multi-rib tread, groove inspection stripe, and detailed sidewalls
 - Dual standard wheels, asymmetric dual wheels (1 narrow + 1 normal), and dual swamp wheels
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector, Euler, Matrix

PROJECT_DIR = r"E:\Backup-C\Projetos\testejogo"
BLEND_FILE_NAME = "Realistic_Wheels_Collection.blend"
BLEND_FILE_PATH = os.path.join(PROJECT_DIR, BLEND_FILE_NAME)
ASSETS_MODELS_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "Vehicles", "RealisticWheelsPack")
ASSETS_BLEND_PATH = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "Vehicles", BLEND_FILE_NAME)
RENDER_OUTPUT_PATH = os.path.join(PROJECT_DIR, "realistic_wheels_showcase.png")

os.makedirs(ASSETS_MODELS_DIR, exist_ok=True)

# -----------------------------------------------------------------------------
# High-Fidelity PBR Shader Setup
# -----------------------------------------------------------------------------
def get_or_create_mat(name, base_color, metallic=0.0, roughness=0.5, specular=0.5):
    mat = bpy.data.materials.get(name)
    if not mat:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = base_color
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = specular
    return mat

def setup_realistic_materials():
    return {
        # Photorealistic tire rubber: soft satin sheen on sidewalls, matte on treads
        "rubber_sidewall": get_or_create_mat("Mat_Real_Rubber_Sidewall", (0.055, 0.055, 0.058, 1.0), metallic=0.0, roughness=0.42, specular=0.6),
        "rubber_tread": get_or_create_mat("Mat_Real_Rubber_Tread", (0.042, 0.042, 0.044, 1.0), metallic=0.0, roughness=0.68, specular=0.4),
        "rubber_groove": get_or_create_mat("Mat_Real_Rubber_Groove", (0.025, 0.025, 0.027, 1.0), metallic=0.0, roughness=0.85, specular=0.3),
        "stripe_red": get_or_create_mat("Mat_Real_Stripe_Red", (0.85, 0.08, 0.06, 1.0), metallic=0.1, roughness=0.35, specular=0.5),
        "rim_silver": get_or_create_mat("Mat_Real_Rim_Silver", (0.80, 0.82, 0.84, 1.0), metallic=0.94, roughness=0.22, specular=0.9),
        "rim_dark": get_or_create_mat("Mat_Real_Rim_Dark", (0.13, 0.14, 0.15, 1.0), metallic=0.88, roughness=0.28, specular=0.8),
        "rim_agri_yellow": get_or_create_mat("Mat_Real_Rim_AgriYellow", (0.94, 0.65, 0.04, 1.0), metallic=0.2, roughness=0.32, specular=0.6),
        "hub_iron": get_or_create_mat("Mat_Real_Hub_Iron", (0.20, 0.21, 0.22, 1.0), metallic=0.85, roughness=0.45, specular=0.5),
        "bolts": get_or_create_mat("Mat_Real_Bolts", (0.85, 0.86, 0.90, 1.0), metallic=0.98, roughness=0.14, specular=1.0),
        "chrome_cap": get_or_create_mat("Mat_Real_Chrome_Cap", (0.95, 0.95, 0.96, 1.0), metallic=1.0, roughness=0.04, specular=1.0),
        "brake_drum": get_or_create_mat("Mat_Real_Brake_Drum", (0.35, 0.36, 0.38, 1.0), metallic=0.88, roughness=0.42, specular=0.5),
        "pedestal": get_or_create_mat("Mat_Real_Pedestal", (0.08, 0.08, 0.09, 1.0), metallic=0.35, roughness=0.45),
        "pedestal_top": get_or_create_mat("Mat_Real_Pedestal_Top", (0.16, 0.16, 0.18, 1.0), metallic=0.55, roughness=0.3),
        "text_gold": get_or_create_mat("Mat_Real_Text_Gold", (0.95, 0.82, 0.28, 1.0), metallic=0.85, roughness=0.2)
    }

# -----------------------------------------------------------------------------
# Procedural BMesh Modeling Builder
# -----------------------------------------------------------------------------
class RealMeshBuilder:
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

    def add_cylinder(self, radius, depth, x_center=0.0, verts=36, mat=None):
        m_idx = self.get_mat_idx(mat) if mat else 0
        x0 = x_center - depth / 2.0
        x1 = x_center + depth / 2.0
        v_b, v_f = [], []
        for i in range(verts):
            th = 2 * math.pi * i / verts
            y = radius * math.cos(th)
            z = radius * math.sin(th)
            v_b.append(self.bm.verts.new((x0, y, z)))
            v_f.append(self.bm.verts.new((x1, y, z)))
        for i in range(verts):
            ni = (i + 1) % verts
            f = self.bm.faces.new((v_b[i], v_f[i], v_f[ni], v_b[ni]))
            f.material_index = m_idx
        f_front = self.bm.faces.new(reversed(v_f))
        f_front.material_index = m_idx
        f_back = self.bm.faces.new(v_b)
        f_back.material_index = m_idx

    def add_tube(self, outer_r, inner_r, depth, x_center=0.0, verts=36, mat=None):
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

    def add_revolved_profile(self, profile, x_offset=0.0, verts=48, mat=None):
        """Revolves a cross section (dx, r, mat) around the X axis with high resolution."""
        rings = []
        ring_mats = []
        for item in profile:
            if len(item) == 3:
                dx, r, m = item
            else:
                dx, r = item
                m = mat
            ring_mats.append(self.get_mat_idx(m) if m else 0)
            ring = []
            x = x_offset + dx
            for i in range(verts):
                th = 2 * math.pi * i / verts
                ring.append(self.bm.verts.new((x, r * math.cos(th), r * math.sin(th))))
            rings.append(ring)

        for r_idx in range(len(rings) - 1):
            m_idx = ring_mats[r_idx]
            for i in range(verts):
                ni = (i + 1) % verts
                f = self.bm.faces.new((rings[r_idx][i], rings[r_idx][ni], rings[r_idx + 1][ni], rings[r_idx + 1][i]))
                f.material_index = m_idx

        # Close bead bridge
        for i in range(verts):
            ni = (i + 1) % verts
            f = self.bm.faces.new((rings[-1][i], rings[-1][ni], rings[0][ni], rings[0][i]))
            f.material_index = ring_mats[-1]

    def add_organic_chevron_lug(self, x_center, th_center, is_right_side=True, length=0.18, height=0.048, sweep_rad=0.65, mat=None):
        """
        Creates a realistic curved chevron traction lug matching Reference Images 1 & 3:
        Tapered nose near center, swept back along the crown, rounded shoulder wrap.
        """
        m_idx = self.get_mat_idx(mat) if mat else 0
        steps = 8
        cross_sections = []
        sign_x = 1.0 if is_right_side else -1.0
        
        # Center radius of tire crown
        r_crown = 0.54
        r_shoulder = 0.49
        
        for s in range(steps + 1):
            t = s / steps
            # X coordinate across tread half: starts near center, extends past shoulder
            dx = (0.012 + t * length) * sign_x
            x = x_center + dx
            
            # Swept curve backwards along the circumference
            # In Images 1 & 3, the lug bends backward progressively
            curve_angle = (t ** 1.35) * sweep_rad
            th = th_center + curve_angle
            
            # Radius decreases toward shoulder
            r_base = r_crown - (t ** 2) * (r_crown - r_shoulder)
            
            # Lug height above surface
            # Starts tapered at center, reaches maximum at middle, slightly slopes at edge
            h_factor = math.sin(t * math.pi * 0.85 + 0.15)
            h = height * max(0.2, h_factor)
            
            # Trapezoidal cross-section width
            w_base = 0.042 * (0.85 + 0.3 * (1.0 - abs(t - 0.5) * 1.5))
            w_top = w_base * 0.58  # 45-degree draft angle for mold release
            
            # 4 vertices for this slice
            v_slice = []
            pts = [
                (r_base, -w_base / 2.0),
                (r_base, w_base / 2.0),
                (r_base + h, w_top / 2.0),
                (r_base + h, -w_top / 2.0)
            ]
            for rad, circ in pts:
                d_th = th + (circ / rad)
                y = rad * math.cos(d_th)
                z = rad * math.sin(d_th)
                v_slice.append(self.bm.verts.new((x, y, z)))
            cross_sections.append(v_slice)
            
        for s in range(steps):
            s1 = cross_sections[s]
            s2 = cross_sections[s + 1]
            for i in range(4):
                ni = (i + 1) % 4
                f = self.bm.faces.new((s1[i], s2[i], s2[ni], s1[ni]))
                f.material_index = m_idx
                
        # Front nose cap and shoulder tail cap
        f_cap1 = self.bm.faces.new(reversed(cross_sections[0]))
        f_cap1.material_index = m_idx
        f_cap2 = self.bm.faces.new(cross_sections[-1])
        f_cap2.material_index = m_idx

    def add_shoulder_cleat(self, x_center, th_center, is_right_side=True, mat=None):
        """Creates the secondary triangular cleat between chevron lugs on the shoulder (Images 1 & 3)."""
        m_idx = self.get_mat_idx(mat) if mat else 0
        sign_x = 1.0 if is_right_side else -1.0
        x0 = x_center + (0.13 * sign_x)
        x1 = x_center + (0.175 * sign_x)
        r0 = 0.49
        h = 0.025
        w = 0.028
        
        pts = [
            (x0, r0, -w/2), (x0, r0, w/2), (x0, r0 + h*0.5, 0),
            (x1, r0 - 0.02, -w/3), (x1, r0 - 0.02, w/3), (x1, r0 - 0.02 + h, 0)
        ]
        v_list = []
        for x, rad, circ in pts:
            d_th = th_center + (circ / rad)
            y = rad * math.cos(d_th)
            z = rad * math.sin(d_th)
            v_list.append(self.bm.verts.new((x, y, z)))
            
        faces_idx = [
            (0, 1, 2), # center triangle
            (3, 5, 4), # outer triangle
            (0, 3, 4, 1), # bottom
            (1, 4, 5, 2), # right side
            (2, 5, 3, 0)  # left side
        ]
        for fi in faces_idx:
            if len(fi) == 3:
                f = self.bm.faces.new((v_list[fi[0]], v_list[fi[1]], v_list[fi[2]]))
            else:
                f = self.bm.faces.new((v_list[fi[0]], v_list[fi[1]], v_list[fi[2]], v_list[fi[3]]))
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

    def add_realistic_truck_rim(self, x_center, outer_r=0.29, inner_r=0.25, width=0.24, is_outward=False, mat_rim=None, mats=None):
        """Creates a stamped commercial truck rim with authentic bead lip, dish curvature, and 8 ventilation oval holes."""
        # 1. Outer rim lip & bead seat
        self.add_tube(outer_r=outer_r + 0.012, inner_r=outer_r - 0.005, depth=0.025, x_center=x_center + (width/2.0 - 0.015), verts=36, mat=mat_rim)
        self.add_tube(outer_r=outer_r, inner_r=inner_r, depth=width, x_center=x_center, verts=36, mat=mat_rim)
        
        # Dish offset: rear dual dishes curve inward; front steer dishes curve outward
        dish_x = x_center + (0.05 if is_outward else -0.05)
        self.add_cylinder(radius=inner_r + 0.005, depth=0.035, x_center=dish_x, verts=36, mat=mat_rim)
        
        # 8 ventilation oval hand holes
        for i in range(8):
            a = 2 * math.pi * i / 8 + 0.2
            y = 0.20 * math.cos(a)
            z = 0.20 * math.sin(a)
            self.add_cylinder(radius=0.022, depth=0.045, x_center=dish_x, verts=16, mat=mats["hub_iron"])

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
# Tire Profile Generators
# -----------------------------------------------------------------------------

def build_swamp_carcass_profile(width=0.42, r_rim=0.28, r_crown=0.535, mats=None):
    """
    Creates an authentic rounded agricultural/swamp carcass profile
    matching Images 1 & 3: bead ring, rounded bulbous sidewalls, and crowned tread.
    """
    m_side = mats["rubber_sidewall"]
    m_tread = mats["rubber_tread"]
    w2 = width / 2.0
    
    # (dx, r, material)
    profile = [
        # Bead seat near rim
        (-w2 * 0.72, r_rim, m_side),
        (-w2 * 0.75, r_rim + 0.025, m_side),
        (-w2 * 0.78, r_rim + 0.05, m_side),
        # Bulbous lower sidewall with embossed bead ring
        (-w2 * 0.95, r_rim + 0.12, m_side),
        (-w2 * 1.02, r_rim + 0.18, m_side),
        (-w2 * 0.98, r_rim + 0.23, m_side),
        # Upper sidewall transition
        (-w2 * 0.88, r_crown - 0.05, m_tread),
        (-w2 * 0.65, r_crown - 0.02, m_tread),
        # Crown
        (-w2 * 0.35, r_crown - 0.005, m_tread),
        (0.0, r_crown, m_tread),
        (w2 * 0.35, r_crown - 0.005, m_tread),
        (w2 * 0.65, r_crown - 0.02, m_tread),
        (w2 * 0.88, r_crown - 0.05, m_tread),
        # Right sidewall
        (w2 * 0.98, r_rim + 0.23, m_side),
        (w2 * 1.02, r_rim + 0.18, m_side),
        (w2 * 0.95, r_rim + 0.12, m_side),
        (w2 * 0.78, r_rim + 0.05, m_side),
        (w2 * 0.75, r_rim + 0.025, m_side),
        (w2 * 0.72, r_rim, m_side)
    ]
    return profile


def build_commercial_truck_profile(width=0.28, r_rim=0.29, r_crown=0.525, mats=None):
    """
    Creates an authentic commercial truck tire profile matching Image 2 (Milemax D / Speedmax):
    4 deep rain grooves, red manufacturing line, and detailed sidewalls with embossed ribs.
    """
    m_side = mats["rubber_sidewall"]
    m_tread = mats["rubber_tread"]
    m_groove = mats["rubber_groove"]
    m_red = mats["stripe_red"]
    
    w2 = width / 2.0
    r_g = r_crown - 0.016  # groove bottom radius
    
    # Build detailed cross section with grooves and shoulder steps
    profile = [
        # Left bead seat
        (-w2 * 0.85, r_rim, m_side),
        (-w2 * 0.88, r_rim + 0.02, m_side),
        (-w2 * 0.96, r_rim + 0.08, m_side),
        # Embossed outer rim protection rib (Image 2)
        (-w2 * 1.04, r_rim + 0.14, m_side),
        (-w2 * 1.02, r_rim + 0.17, m_side),
        # Brand plaque flat area
        (-w2 * 0.98, r_rim + 0.20, m_side),
        (-w2 * 0.94, r_crown - 0.03, m_tread),
        # Left Shoulder block with step
        (-w2 * 0.85, r_crown - 0.008, m_tread),
        (-w2 * 0.75, r_crown, m_tread),
        (-w2 * 0.58, r_crown, m_tread),
        
        # Groove 1 (outer left)
        (-w2 * 0.56, r_g, m_groove),
        (-w2 * 0.50, r_g, m_groove),
        
        # Rib 2 (intermediate left with RED line)
        (-w2 * 0.48, r_crown, m_tread),
        (-w2 * 0.32, r_crown, m_tread),
        # Red inspection line in second channel
        (-w2 * 0.30, r_g, m_red),
        (-w2 * 0.24, r_g, m_red),
        
        # Center Rib
        (-w2 * 0.22, r_crown, m_tread),
        (0.0, r_crown + 0.002, m_tread),
        (w2 * 0.22, r_crown, m_tread),
        
        # Groove 3 (intermediate right)
        (w2 * 0.24, r_g, m_groove),
        (w2 * 0.30, r_g, m_groove),
        
        # Rib 4 (intermediate right)
        (w2 * 0.32, r_crown, m_tread),
        (w2 * 0.48, r_crown, m_tread),
        
        # Groove 4 (outer right)
        (w2 * 0.50, r_g, m_groove),
        (w2 * 0.56, r_g, m_groove),
        
        # Right Shoulder block with step
        (w2 * 0.58, r_crown, m_tread),
        (w2 * 0.75, r_crown, m_tread),
        (w2 * 0.85, r_crown - 0.008, m_tread),
        (w2 * 0.94, r_crown - 0.03, m_tread),
        # Right sidewall
        (w2 * 0.98, r_rim + 0.20, m_side),
        (w2 * 1.02, r_rim + 0.17, m_side),
        (w2 * 1.04, r_rim + 0.14, m_side),
        (w2 * 0.96, r_rim + 0.08, m_side),
        (w2 * 0.88, r_rim + 0.02, m_side),
        (w2 * 0.85, r_rim, m_side)
    ]
    return profile


def build_narrow_truck_profile(width=0.15, r_rim=0.29, r_crown=0.525, mats=None):
    """
    Narrow Commercial Truck Tire profile designed for asymmetric dual setups (1 fino + 1 normal).
    Has 2 deep rain grooves and authentic narrow proportions.
    """
    m_side = mats["rubber_sidewall"]
    m_tread = mats["rubber_tread"]
    m_groove = mats["rubber_groove"]
    w2 = width / 2.0
    r_g = r_crown - 0.016
    
    profile = [
        (-w2 * 0.85, r_rim, m_side),
        (-w2 * 0.92, r_rim + 0.04, m_side),
        (-w2 * 1.02, r_rim + 0.12, m_side),
        (-w2 * 0.92, r_crown - 0.025, m_tread),
        (-w2 * 0.65, r_crown, m_tread),
        # Groove 1
        (-w2 * 0.45, r_g, m_groove),
        (-w2 * 0.25, r_g, m_groove),
        # Center Rib
        (-w2 * 0.20, r_crown, m_tread),
        (0.0, r_crown + 0.002, m_tread),
        (w2 * 0.20, r_crown, m_tread),
        # Groove 2
        (w2 * 0.25, r_g, m_groove),
        (w2 * 0.45, r_g, m_groove),
        # Right Rib
        (w2 * 0.65, r_crown, m_tread),
        (w2 * 0.92, r_crown - 0.025, m_tread),
        (w2 * 1.02, r_rim + 0.12, m_side),
        (w2 * 0.92, r_rim + 0.04, m_side),
        (w2 * 0.85, r_rim, m_side)
    ]
    return profile

# -----------------------------------------------------------------------------
# Complete Assembled Wheels Builders
# -----------------------------------------------------------------------------

def build_swamp_traction_wheel_pro(mats, name="Realistic_Wheel_Swamp_Pro"):
    """
    Faithful recreation of Reference Images 1 & 3:
    Curved chevron swept lugs, secondary shoulder cleats, and heavy agricultural yellow steel rim.
    """
    b = RealMeshBuilder(name)
    prof = build_swamp_carcass_profile(width=0.42, r_rim=0.28, r_crown=0.535, mats=mats)
    b.add_revolved_profile(prof, x_offset=0.0, verts=48)
    
    # 18 Pairs of organic curved chevron lugs (Images 1 & 3)
    lug_pairs = 18
    for i in range(lug_pairs):
        th = 2 * math.pi * i / lug_pairs
        # Left swept lug
        b.add_organic_chevron_lug(x_center=0.0, th_center=th, is_right_side=False, length=0.18, height=0.048, sweep_rad=0.65, mat=mats["rubber_tread"])
        # Right swept lug (staggered by half phase)
        th_r = th + (math.pi / lug_pairs)
        b.add_organic_chevron_lug(x_center=0.0, th_center=th_r, is_right_side=True, length=0.18, height=0.048, sweep_rad=0.65, mat=mats["rubber_tread"])
        
        # Shoulder cleats between lugs (Images 1 & 3)
        b.add_shoulder_cleat(x_center=0.0, th_center=th + 0.16, is_right_side=False, mat=mats["rubber_tread"])
        b.add_shoulder_cleat(x_center=0.0, th_center=th_r + 0.16, is_right_side=True, mat=mats["rubber_tread"])
        
    # Heavy agricultural pressed steel wheel in CAT/John Deere Yellow
    b.add_tube(outer_r=0.28, inner_r=0.24, depth=0.38, x_center=0.0, verts=36, mat=mats["rim_agri_yellow"])
    b.add_cylinder(radius=0.245, depth=0.05, x_center=0.06, verts=36, mat=mats["rim_agri_yellow"])
    b.add_cylinder(radius=0.11, depth=0.10, x_center=0.11, verts=24, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.055, depth=0.035, x_center=0.16, verts=24, mat=mats["chrome_cap"])
    b.add_bolts_circle(8, 0.17, x_pos=0.085, bolt_r=0.015, depth=0.03, mat=mats["bolts"])
    
    return b.finish_object()


def build_swamp_tire_solo_pro(mats, name="Realistic_Tire_Swamp_Solo"):
    """Standalone tire matching Images 1 & 3 exactly (without rim, as shown in the reference)."""
    b = RealMeshBuilder(name)
    prof = build_swamp_carcass_profile(width=0.42, r_rim=0.28, r_crown=0.535, mats=mats)
    b.add_revolved_profile(prof, x_offset=0.0, verts=48)
    
    lug_pairs = 18
    for i in range(lug_pairs):
        th = 2 * math.pi * i / lug_pairs
        b.add_organic_chevron_lug(x_center=0.0, th_center=th, is_right_side=False, length=0.18, height=0.048, sweep_rad=0.65, mat=mats["rubber_tread"])
        th_r = th + (math.pi / lug_pairs)
        b.add_organic_chevron_lug(x_center=0.0, th_center=th_r, is_right_side=True, length=0.18, height=0.048, sweep_rad=0.65, mat=mats["rubber_tread"])
        b.add_shoulder_cleat(x_center=0.0, th_center=th + 0.16, is_right_side=False, mat=mats["rubber_tread"])
        b.add_shoulder_cleat(x_center=0.0, th_center=th_r + 0.16, is_right_side=True, mat=mats["rubber_tread"])
        
    return b.finish_object()


def build_truck_tire_solo_pro(mats, name="Realistic_Tire_Truck_Solo"):
    """Standalone commercial truck tire matching Image 2 exactly (without rim, as shown in the reference)."""
    b = RealMeshBuilder(name)
    prof = build_commercial_truck_profile(width=0.28, r_rim=0.29, r_crown=0.525, mats=mats)
    b.add_revolved_profile(prof, x_offset=0.0, verts=48)
    
    # 28 Micro-sipes across shoulder blocks for authentic Milemax look
    sipe_count = 28
    for i in range(sipe_count):
        th = 2 * math.pi * i / sipe_count
        y = 0.526 * math.cos(th)
        z = 0.526 * math.sin(th)
        # Sipe groove cutouts on shoulder
        b.add_cylinder(radius=0.005, depth=0.035, x_center=-0.11, verts=8, mat=mats["rubber_groove"])
        b.add_cylinder(radius=0.005, depth=0.035, x_center=0.11, verts=8, mat=mats["rubber_groove"])
        
    return b.finish_object()


def build_truck_dual_standard_pro(mats, name="Realistic_Wheel_Dual_Standard_Pro"):
    """
    Standard Commercial Dual Wheels with Milemax-style tires (Rodado Duplo Padrão).
    Two tires matching Image 2 mounted back-to-back on a heavy dual axle with spacer.
    """
    b = RealMeshBuilder(name)
    inner_x = -0.165
    outer_x = 0.165
    
    # Inner and Outer Tire Carcasses
    prof = build_commercial_truck_profile(width=0.27, r_rim=0.29, r_crown=0.525, mats=mats)
    b.add_revolved_profile(prof, x_offset=inner_x, verts=48)
    b.add_revolved_profile(prof, x_offset=outer_x, verts=48)
    
    # Stamped steel dual rims (both curve inward)
    b.add_realistic_truck_rim(x_center=inner_x, width=0.22, is_outward=False, mat_rim=mats["rim_silver"], mats=mats)
    b.add_realistic_truck_rim(x_center=outer_x, width=0.22, is_outward=False, mat_rim=mats["rim_silver"], mats=mats)
    
    # Heavy spacer between wheels
    b.add_tube(outer_r=0.26, inner_r=0.22, depth=0.11, x_center=0.0, verts=36, mat=mats["hub_iron"])
    
    # Hub & 10 Studs
    b.add_cylinder(radius=0.10, depth=0.14, x_center=0.20, verts=24, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.055, depth=0.04, x_center=0.28, verts=24, mat=mats["chrome_cap"])
    b.add_bolts_circle(10, 0.165, x_pos=0.10, bolt_r=0.013, depth=0.045, mat=mats["bolts"])
    
    # Inside cooling brake drum
    b.add_cylinder(radius=0.24, depth=0.12, x_center=-0.32, verts=36, mat=mats["brake_drum"])
    b.add_tube(outer_r=0.248, inner_r=0.240, depth=0.015, x_center=-0.30, verts=36, mat=mats["hub_iron"])
    
    return b.finish_object()


def build_truck_dual_asymmetric_pro(mats, name="Realistic_Wheel_Dual_Asymmetric_Pro"):
    """
    Asymmetric Dual Wheels (Rodado Duplo Misto: 1 Fino + 1 Normal).
    Specifically requested: 1 narrow tire (15cm) + 1 normal tire (27cm) with realistic Milemax styling.
    """
    b = RealMeshBuilder(name)
    inner_x = -0.155
    outer_x = 0.145
    
    # 1. Inner Normal Tire (W = 0.27m)
    norm_prof = build_commercial_truck_profile(width=0.27, r_rim=0.29, r_crown=0.525, mats=mats)
    b.add_revolved_profile(norm_prof, x_offset=inner_x, verts=48)
    b.add_realistic_truck_rim(x_center=inner_x, width=0.22, is_outward=False, mat_rim=mats["rim_dark"], mats=mats)
    
    # 2. Outer Narrow Tire (W = 0.15m)
    narrow_prof = build_narrow_truck_profile(width=0.15, r_rim=0.29, r_crown=0.525, mats=mats)
    b.add_revolved_profile(narrow_prof, x_offset=outer_x, verts=48)
    b.add_realistic_truck_rim(x_center=outer_x, width=0.13, is_outward=False, mat_rim=mats["rim_silver"], mats=mats)
    
    # 3. Offset Dual Spacer Hub
    b.add_tube(outer_r=0.26, inner_r=0.22, depth=0.13, x_center=0.0, verts=36, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.10, depth=0.14, x_center=0.18, verts=24, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.055, depth=0.04, x_center=0.26, verts=24, mat=mats["chrome_cap"])
    b.add_bolts_circle(10, 0.165, x_pos=0.09, bolt_r=0.013, depth=0.045, mat=mats["bolts"])
    
    # Inside brake drum
    b.add_cylinder(radius=0.24, depth=0.12, x_center=-0.32, verts=36, mat=mats["brake_drum"])
    
    return b.finish_object()


def build_swamp_dual_pro(mats, name="Realistic_Wheel_Dual_Swamp_Pro"):
    """
    Dual Swamp Traction Wheels (Rodado Duplo de Banhado / Arrozal).
    Two deep-chevron swamp tires side-by-side on wide dual agricultural rims.
    """
    b = RealMeshBuilder(name)
    inner_x = -0.24
    outer_x = 0.24
    
    prof = build_swamp_carcass_profile(width=0.40, r_rim=0.28, r_crown=0.535, mats=mats)
    b.add_revolved_profile(prof, x_offset=inner_x, verts=48)
    b.add_revolved_profile(prof, x_offset=outer_x, verts=48)
    
    lug_pairs = 16
    for t_x in [inner_x, outer_x]:
        for i in range(lug_pairs):
            th = 2 * math.pi * i / lug_pairs
            b.add_organic_chevron_lug(x_center=t_x, th_center=th, is_right_side=False, length=0.17, height=0.048, sweep_rad=0.65, mat=mats["rubber_tread"])
            th_r = th + (math.pi / lug_pairs)
            b.add_organic_chevron_lug(x_center=t_x, th_center=th_r, is_right_side=True, length=0.17, height=0.048, sweep_rad=0.65, mat=mats["rubber_tread"])
            b.add_shoulder_cleat(x_center=t_x, th_center=th + 0.18, is_right_side=False, mat=mats["rubber_tread"])
            b.add_shoulder_cleat(x_center=t_x, th_center=th_r + 0.18, is_right_side=True, mat=mats["rubber_tread"])
            
    # Dual wide agricultural yellow rims
    b.add_tube(outer_r=0.28, inner_r=0.24, depth=0.34, x_center=inner_x, verts=36, mat=mats["rim_agri_yellow"])
    b.add_tube(outer_r=0.28, inner_r=0.24, depth=0.34, x_center=outer_x, verts=36, mat=mats["rim_agri_yellow"])
    b.add_tube(outer_r=0.26, inner_r=0.21, depth=0.16, x_center=0.0, verts=36, mat=mats["hub_iron"])
    
    # Planetary hub
    b.add_cylinder(radius=0.13, depth=0.14, x_center=0.28, verts=24, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.07, depth=0.04, x_center=0.36, verts=24, mat=mats["chrome_cap"])
    b.add_bolts_circle(10, 0.18, x_pos=0.24, bolt_r=0.015, depth=0.035, mat=mats["bolts"])
    
    return b.finish_object()


def build_truck_front_steer_pro(mats, name="Realistic_Wheel_Truck_FrontSteer_Pro"):
    """Single front steer heavy truck wheel with outward convex rim dish and Milemax tire."""
    b = RealMeshBuilder(name)
    prof = build_commercial_truck_profile(width=0.28, r_rim=0.29, r_crown=0.525, mats=mats)
    b.add_revolved_profile(prof, x_offset=0.0, verts=48)
    
    # Outward convex dish rim
    b.add_realistic_truck_rim(x_center=0.0, width=0.24, is_outward=True, mat_rim=mats["rim_silver"], mats=mats)
    
    b.add_cylinder(radius=0.085, depth=0.08, x_center=0.12, verts=24, mat=mats["hub_iron"])
    b.add_cylinder(radius=0.045, depth=0.03, x_center=0.17, verts=24, mat=mats["chrome_cap"])
    b.add_bolts_circle(10, 0.165, x_pos=0.08, bolt_r=0.013, depth=0.04, mat=mats["bolts"])
    b.add_cylinder(radius=0.23, depth=0.10, x_center=-0.14, verts=36, mat=mats["brake_drum"])
    
    return b.finish_object()

# -----------------------------------------------------------------------------
# Showroom Setup & Studio Staging
# -----------------------------------------------------------------------------
def setup_realistic_showroom(wheel_configs, mats):
    """
    Arranges the realistic models in 2 tiered showcase rows with studio lighting.
    Row 1 (Front, Z=0.0, Y=-1.8):
      - 01. Swamp Tire (Solo - Ref Image 1 & 3)
      - 02. Truck Tire (Solo - Ref Image 2)
      - 03. Truck Dual Standard (Duplo)
      - 04. Truck Dual Asymmetric (1 Fino + 1 Normal)
    Row 2 (Back Elevated Z=0.45, Y=1.8):
      - 05. Swamp Traction Wheel (Roda de Banhado c/ Aro)
      - 06. Swamp Dual Wheel (Duplo de Banhado)
      - 07. Truck Front Steer (Direcional Pesado)
    """
    ped_radius = 0.88
    ped_height = 0.22
    spacing_x = 2.45
    
    front_row = wheel_configs[:4]
    back_row = wheel_configs[4:]
    
    rows = [
        (front_row, -1.8, 0.0),
        (back_row, 1.8, 0.45)
    ]
    
    for row_items, row_y, base_z in rows:
        num_items = len(row_items)
        for idx, (wheel, label_name) in enumerate(row_items):
            x = (idx - (num_items - 1) / 2.0) * spacing_x
            y = row_y
            
            dim_z = wheel.dimensions.z
            z_pos = base_z + ped_height + (dim_z / 2.0)
            wheel.location = Vector((x, y, z_pos))
            # Angle towards camera showing 3/4 face and treads
            wheel.rotation_euler = Euler((0, 0, math.radians(-38)), 'XYZ')
            
            # Pedestal
            bpy.ops.mesh.primitive_cylinder_add(radius=ped_radius, depth=ped_height + base_z, location=(x, y, (ped_height + base_z)/2.0))
            ped = bpy.context.active_object
            ped.name = f"Pedestal_{wheel.name}"
            ped.data.materials.append(mats["pedestal"])
            
            bpy.ops.mesh.primitive_cylinder_add(radius=ped_radius - 0.05, depth=0.02, location=(x, y, base_z + ped_height + 0.01))
            ped_top = bpy.context.active_object
            ped_top.name = f"PedestalTop_{wheel.name}"
            ped_top.data.materials.append(mats["pedestal_top"])
            
            # 3D Text Label
            bpy.ops.object.text_add(location=(x - 0.85, y - 0.95, base_z + 0.02), rotation=(math.radians(90), 0, 0))
            txt = bpy.context.active_object
            txt.name = f"Label_{wheel.name}"
            txt.data.body = label_name
            txt.data.size = 0.105
            txt.data.extrude = 0.018
            txt.data.materials.append(mats["text_gold"])

    # Studio Floor & Backdrop
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0))
    floor = bpy.context.active_object
    floor.name = "Showroom_Floor"
    floor_mat = get_or_create_mat("Mat_ShowroomFloor", (0.05, 0.05, 0.06, 1.0), metallic=0.3, roughness=0.22)
    floor.data.materials.append(floor_mat)

    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 12, 12), rotation=(math.radians(90), 0, 0))
    wall = bpy.context.active_object
    wall.name = "Showroom_Wall"
    wall_mat = get_or_create_mat("Mat_ShowroomWall", (0.07, 0.08, 0.09, 1.0), metallic=0.1, roughness=0.5)
    wall.data.materials.append(wall_mat)

    # Studio 3-Point Lighting tuned for tire rubber specular highlights
    bpy.ops.object.light_add(type='SUN', location=(10, -14, 18), rotation=(math.radians(45), math.radians(24), math.radians(-32)))
    sun = bpy.context.active_object
    sun.name = "Key_Sun"
    sun.data.energy = 5.5
    sun.data.color = (1.0, 0.98, 0.94)

    bpy.ops.object.light_add(type='AREA', location=(-12, -10, 10), rotation=(math.radians(52), math.radians(-35), math.radians(20)))
    fill = bpy.context.active_object
    fill.name = "Fill_Light"
    fill.data.energy = 900.0
    fill.data.size = 16.0
    fill.data.color = (0.75, 0.88, 1.0)

    bpy.ops.object.light_add(type='AREA', location=(0, 12, 9), rotation=(math.radians(-62), 0, 0))
    rim = bpy.context.active_object
    rim.name = "Rim_Light"
    rim.data.energy = 1500.0
    rim.data.size = 20.0
    rim.data.color = (0.9, 0.95, 1.0)

    # Front Soft Bounce Light
    bpy.ops.object.light_add(type='AREA', location=(0, -8, 2), rotation=(math.radians(30), 0, 0))
    bounce = bpy.context.active_object
    bounce.name = "Front_Bounce"
    bounce.data.energy = 400.0
    bounce.data.size = 12.0
    bounce.data.color = (0.9, 0.92, 0.95)

    # Camera
    bpy.ops.object.camera_add(location=(0, -11.5, 5.2), rotation=(math.radians(68), 0, 0))
    cam = bpy.context.active_object
    cam.name = "Showroom_Camera"
    cam.data.lens = 38
    bpy.context.scene.camera = cam

# -----------------------------------------------------------------------------
# Main Execution
# -----------------------------------------------------------------------------
def main():
    print("=== STARTING REALISTIC VEHICLE TIRES & WHEELS GENERATION ===")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mats = setup_realistic_materials()

    print("Building Realistic Reference Models...")
    # 1. Swamp Tire Solo (Reference Images 1 & 3)
    t_swamp_solo = build_swamp_tire_solo_pro(mats)
    # 2. Commercial Truck Tire Solo (Reference Image 2)
    t_truck_solo = build_truck_tire_solo_pro(mats)
    # 3. Dual Standard Commercial Truck (Rodado Duplo Padrão)
    w_dual_std = build_truck_dual_standard_pro(mats)
    # 4. Dual Asymmetric Commercial Truck (1 Fino + 1 Normal)
    w_dual_asym = build_truck_dual_asymmetric_pro(mats)
    # 5. Swamp Traction Wheel Assembled (Pneu de Banhado com Aro)
    w_swamp_wheel = build_swamp_traction_wheel_pro(mats)
    # 6. Swamp Dual Traction Wheel (Rodado Duplo de Banhado)
    w_swamp_dual = build_swamp_dual_pro(mats)
    # 7. Commercial Front Steer Wheel (Direcional Pesado)
    w_truck_steer = build_truck_front_steer_pro(mats)

    all_models = [
        (t_swamp_solo, "01. Pneu Banhado (Ref. Img 1/3)"),
        (t_truck_solo, "02. Pneu Pesado (Ref. Img 2)"),
        (w_dual_std, "03. Rodado Duplo Padrao"),
        (w_dual_asym, "04. Duplo Misto (1 Fino + 1 Normal)"),
        (w_swamp_wheel, "05. Roda Banhado c/ Aro"),
        (w_swamp_dual, "06. Rodado Duplo Banhado"),
        (w_truck_steer, "07. Dianteira Direcional")
    ]

    print("Exporting Realistic Models to FBX...")
    for model, _ in all_models:
        bpy.ops.object.select_all(action='DESELECT')
        model.select_set(True)
        bpy.context.view_layer.objects.active = model
        fbx_path = os.path.join(ASSETS_MODELS_DIR, f"{model.name}.fbx")
        try:
            bpy.ops.export_scene.fbx(
                filepath=fbx_path,
                use_selection=True,
                apply_unit_scale=True,
                axis_forward='-Z',
                axis_up='Y'
            )
            print(f"Exported Realistic FBX: {fbx_path}")
        except Exception as e:
            print(f"FBX error: {e}")

    print("Setting up Realistic Showroom Stage...")
    setup_realistic_showroom(all_models, mats)

    # Save Blender file
    print(f"Saving Blender file to {BLEND_FILE_PATH}...")
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
    print("=== REALISTIC TIRES & WHEELS COMPLETED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
