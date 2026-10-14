"""
Procedural High-Resolution Road Signs Texture Generator for Worldwide Traffic & Distance Signs.
Generates authentic retroreflective textures with crisp vector shapes, international symbols,
national road shields, and multi-city distance boards.
"""

import os
import math
from PIL import Image, ImageDraw, ImageFont

TEX_DIR = r"E:\Backup-C\Projetos\testejogo\Assets\_Project\Textures\TrafficSigns"
os.makedirs(TEX_DIR, exist_ok=True)

# Standard Fonts
FONT_PATH = "C:/Windows/Fonts/arialbd.ttf"
FONT_PATH_REG = "C:/Windows/Fonts/arial.ttf"

def get_font(size, bold=True):
    try:
        path = FONT_PATH if bold else FONT_PATH_REG
        return ImageFont.truetype(path, size)
    except Exception:
        return ImageFont.load_default()

def draw_octagon(draw, center, radius, fill, outline=None, width=1):
    cx, cy = center
    points = []
    for i in range(8):
        angle = math.radians(22.5 + i * 45)
        px = cx + radius * math.cos(angle)
        py = cy + radius * math.sin(angle)
        points.append((px, py))
    draw.polygon(points, fill=fill, outline=outline, width=width)
    return points

def draw_diamond(draw, center, size, fill, outline=None, width=1):
    cx, cy = center
    s = size / 2.0
    pts = [(cx, cy - s), (cx + s, cy), (cx, cy + s), (cx - s, cy)]
    draw.polygon(pts, fill=fill, outline=outline, width=width)
    return pts

def draw_equilateral_triangle(draw, center, side_length, fill, outline=None, width=1, inverted=False):
    cx, cy = center
    h = side_length * (math.sqrt(3) / 2)
    r_circum = h * (2 / 3)
    r_in = h * (1 / 3)
    if not inverted:
        top = (cx, cy - r_circum)
        bottom_left = (cx - side_length / 2, cy + r_in)
        bottom_right = (cx + side_length / 2, cy + r_in)
        pts = [top, bottom_right, bottom_left]
    else:
        bottom = (cx, cy + r_circum)
        top_left = (cx - side_length / 2, cy - r_in)
        top_right = (cx + side_length / 2, cy - r_in)
        pts = [top_left, top_right, bottom]
    draw.polygon(pts, fill=fill, outline=outline, width=width)
    return pts

# -----------------------------------------------------------------------------
# Color Palette (Standard International & MUTCD Colors)
# -----------------------------------------------------------------------------
COLOR_RED = (204, 25, 25, 255)
COLOR_WHITE = (248, 248, 248, 255)
COLOR_YELLOW = (255, 198, 0, 255)       # Warning Yellow / Amber
COLOR_BLACK = (20, 20, 22, 255)
COLOR_BLUE = (0, 75, 160, 255)          # Mandatory Blue
COLOR_MOTORWAY_BLUE = (15, 65, 140, 255)
COLOR_HWY_GREEN = (0, 110, 60, 255)     # Interstate & European Highway Green
COLOR_DARK_GREEN = (0, 85, 45, 255)
COLOR_LED_BG = (15, 16, 18, 255)
COLOR_LED_AMBER = (255, 175, 15, 255)
COLOR_LED_OFF = (35, 32, 20, 255)
COLOR_BRAZIL_BLUE = (0, 90, 170, 255)

# =============================================================================
# 1. European / Vienna Convention Signs
# =============================================================================

def gen_tex_eu_stop():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    center = (512, 512)
    # Red Octagon
    draw_octagon(d, center, 490, fill=COLOR_RED)
    # White inner border
    draw_octagon(d, center, 465, fill=None, outline=COLOR_WHITE, width=22)
    # Bold STOP
    font = get_font(260, bold=True)
    d.text(center, "STOP", fill=COLOR_WHITE, font=font, anchor="mm")
    img.save(os.path.join(TEX_DIR, "tex_sign_eu_stop.png"))

def gen_tex_eu_yield():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    center = (512, 470)
    # Outer red inverted triangle
    draw_equilateral_triangle(d, center, 980, fill=COLOR_RED, inverted=True)
    # Inner white inverted triangle
    draw_equilateral_triangle(d, center, 660, fill=COLOR_WHITE, inverted=True)
    img.save(os.path.join(TEX_DIR, "tex_sign_eu_yield.png"))

def gen_tex_eu_curve():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    center = (512, 550)
    # Red outer triangle
    draw_equilateral_triangle(d, center, 980, fill=COLOR_RED, inverted=False)
    # White inner triangle
    draw_equilateral_triangle(d, center, 660, fill=COLOR_WHITE, inverted=False)
    
    # Sharp Curve Arrow
    arrow_pts = [
        (460, 680), (510, 680), (510, 520), (600, 520), (600, 580),
        (690, 480), (600, 380), (600, 440), (460, 440)
    ]
    d.polygon(arrow_pts, fill=COLOR_BLACK)
    img.save(os.path.join(TEX_DIR, "tex_sign_eu_curve.png"))

def gen_tex_eu_speed80():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Outer Red Circle
    d.ellipse([(20, 20), (1004, 1004)], fill=COLOR_RED)
    # Inner White Circle
    d.ellipse([(140, 140), (884, 884)], fill=COLOR_WHITE)
    # Bold "80"
    font = get_font(380, bold=True)
    d.text((512, 512), "80", fill=COLOR_BLACK, font=font, anchor="mm")
    img.save(os.path.join(TEX_DIR, "tex_sign_eu_speed80.png"))

def gen_tex_eu_no_entry():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([(20, 20), (1004, 1004)], fill=COLOR_RED)
    # Horizontal White Bar
    d.rounded_rectangle([(180, 430), (844, 594)], radius=20, fill=COLOR_WHITE)
    img.save(os.path.join(TEX_DIR, "tex_sign_eu_no_entry.png"))

def gen_tex_eu_roundabout():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([(20, 20), (1004, 1004)], fill=COLOR_BLUE)
    d.ellipse([(35, 35), (989, 989)], outline=COLOR_WHITE, width=12)
    # Draw 3 circular curved arrows
    cx, cy = 512, 512
    ring_r = 230
    for ang_deg in [0, 120, 240]:
        ang = math.radians(ang_deg)
        # Arrow arc segment
        bbox = [cx - ring_r, cy - ring_r, cx + ring_r, cy + ring_r]
        d.arc(bbox, start=ang_deg + 15, end=ang_deg + 95, fill=COLOR_WHITE, width=32)
        # Arrowhead
        tip_ang = math.radians(ang_deg + 105)
        tx = cx + ring_r * math.cos(tip_ang)
        ty = cy + ring_r * math.sin(tip_ang)
        # perpendicular vector for arrow head
        dx = -math.sin(tip_ang)
        dy = math.cos(tip_ang)
        pts = [
            (tx + dx * 35, ty + dy * 35),
            (tx - dx * 20 - dy * 35, ty - dy * 20 + dx * 35),
            (tx - dx * 20 + dy * 35, ty - dy * 20 - dx * 35)
        ]
        d.polygon(pts, fill=COLOR_WHITE)
    img.save(os.path.join(TEX_DIR, "tex_sign_eu_roundabout.png"))

def gen_tex_eu_motorway():
    img = Image.new("RGBA", (768, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([(10, 10), (758, 1014)], radius=40, fill=COLOR_MOTORWAY_BLUE, outline=COLOR_WHITE, width=18)
    
    # Motorway symbol: perspective road lanes with overpass
    # Overpass bridge
    d.rectangle([(160, 480), (608, 520)], fill=COLOR_WHITE)
    d.rectangle([(230, 430), (538, 480)], fill=COLOR_WHITE)
    # Left lane perspective
    d.polygon([(260, 560), (330, 560), (250, 880), (160, 880)], fill=COLOR_WHITE)
    # Right lane perspective
    d.polygon([(438, 560), (508, 560), (608, 880), (518, 880)], fill=COLOR_WHITE)
    img.save(os.path.join(TEX_DIR, "tex_sign_eu_motorway.png"))

# =============================================================================
# 2. Americas & Latin America Signs (MUTCD & CONTRAN)
# =============================================================================

def gen_tex_us_curve():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    center = (512, 512)
    # Outer Yellow Diamond
    draw_diamond(d, center, 980, fill=COLOR_YELLOW)
    # Black inner diamond outline
    draw_diamond(d, center, 900, fill=None, outline=COLOR_BLACK, width=18)
    # Sharp Curve Arrow
    arrow_pts = [
        (460, 680), (530, 680), (530, 520), (620, 520), (620, 580),
        (710, 480), (620, 380), (620, 440), (460, 440)
    ]
    d.polygon(arrow_pts, fill=COLOR_BLACK)
    img.save(os.path.join(TEX_DIR, "tex_sign_us_curve.png"))

def gen_tex_us_speed65():
    img = Image.new("RGBA", (768, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([(15, 15), (753, 1009)], radius=45, fill=COLOR_WHITE, outline=COLOR_BLACK, width=20)
    d.rounded_rectangle([(35, 35), (733, 989)], radius=30, fill=None, outline=COLOR_BLACK, width=8)
    
    font_small = get_font(105, bold=True)
    font_large = get_font(310, bold=True)
    
    d.text((384, 160), "SPEED", fill=COLOR_BLACK, font=font_small, anchor="mm")
    d.text((384, 300), "LIMIT", fill=COLOR_BLACK, font=font_small, anchor="mm")
    d.text((384, 630), "65", fill=COLOR_BLACK, font=font_large, anchor="mm")
    img.save(os.path.join(TEX_DIR, "tex_sign_us_speed65.png"))

def gen_tex_us_oneway():
    img = Image.new("RGBA", (1024, 340), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([(10, 10), (1014, 330)], radius=30, fill=COLOR_WHITE, outline=COLOR_BLACK, width=14)
    # Large black arrow shape pointing right
    arrow_pts = [
        (50, 100), (740, 100), (740, 45), (960, 170), (740, 295), (740, 240), (50, 240)
    ]
    d.polygon(arrow_pts, fill=COLOR_BLACK)
    # White "ONE WAY" text inside the arrow
    font = get_font(95, bold=True)
    d.text((430, 170), "ONE WAY", fill=COLOR_WHITE, font=font, anchor="mm")
    img.save(os.path.join(TEX_DIR, "tex_sign_us_oneway.png"))

def gen_tex_us_interstate():
    img = Image.new("RGBA", (800, 800), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Interstate shield contour
    # Top curved header red, bottom body blue
    # Draw base blue shield
    shield_pts = [
        (400, 770), (120, 600), (100, 240), (220, 210), (400, 260),
        (580, 210), (700, 240), (680, 600)
    ]
    d.polygon(shield_pts, fill=COLOR_BLUE)
    # Red top crown
    crown_pts = [
        (100, 240), (220, 210), (400, 260), (580, 210), (700, 240),
        (700, 120), (560, 140), (400, 80), (240, 140), (100, 120)
    ]
    d.polygon(crown_pts, fill=COLOR_RED)
    # Outer white outline
    full_shield = [
        (400, 80), (560, 140), (700, 120), (700, 240), (680, 600),
        (400, 770), (120, 600), (100, 240), (100, 120), (240, 140)
    ]
    d.polygon(full_shield, fill=None, outline=COLOR_WHITE, width=16)
    
    font_state = get_font(52, bold=True)
    font_num = get_font(230, bold=True)
    d.text((400, 185), "INTERSTATE", fill=COLOR_WHITE, font=font_state, anchor="mm")
    d.text((400, 480), "95", fill=COLOR_WHITE, font=font_num, anchor="mm")
    img.save(os.path.join(TEX_DIR, "tex_sign_us_interstate.png"))

def gen_tex_br_lombada():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    center = (512, 512)
    draw_diamond(d, center, 980, fill=COLOR_YELLOW)
    draw_diamond(d, center, 900, fill=None, outline=COLOR_BLACK, width=18)
    
    # Speed Bump profile (Lombada)
    # Horizontal ground line + raised arc in middle
    d.rectangle([(220, 560), (370, 595)], fill=COLOR_BLACK)
    d.rectangle([(654, 560), (804, 595)], fill=COLOR_BLACK)
    d.pieslice([(360, 430), (664, 730)], start=180, end=0, fill=COLOR_BLACK)
    img.save(os.path.join(TEX_DIR, "tex_sign_br_lombada.png"))

def gen_tex_br_pare():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    center = (512, 512)
    draw_octagon(d, center, 490, fill=COLOR_RED)
    draw_octagon(d, center, 465, fill=None, outline=COLOR_WHITE, width=22)
    font = get_font(260, bold=True)
    d.text(center, "PARE", fill=COLOR_WHITE, font=font, anchor="mm")
    img.save(os.path.join(TEX_DIR, "tex_sign_br_pare.png"))

def gen_tex_br_shield():
    img = Image.new("RGBA", (768, 860), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Brazilian Federal Highway Shield (Mercosul / DNIT shape)
    pts = [
        (384, 820), (80, 620), (80, 160), (384, 80), (688, 160), (688, 620)
    ]
    d.polygon(pts, fill=COLOR_WHITE, outline=COLOR_BLACK, width=14)
    # Green inner badge
    inner_pts = [
        (384, 770), (120, 590), (120, 190), (384, 120), (648, 190), (648, 590)
    ]
    d.polygon(inner_pts, fill=COLOR_HWY_GREEN)
    
    font_br = get_font(120, bold=True)
    font_num = get_font(190, bold=True)
    d.text((384, 270), "BR", fill=COLOR_WHITE, font=font_br, anchor="mm")
    d.text((384, 490), "101", fill=COLOR_WHITE, font=font_num, anchor="mm")
    img.save(os.path.join(TEX_DIR, "tex_sign_br_shield.png"))

# =============================================================================
# 3. Asian & Commonwealth Signs (Japan, UK, Australia)
# =============================================================================

def gen_tex_jp_stop():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    center = (512, 470)
    draw_equilateral_triangle(d, center, 980, fill=COLOR_RED, inverted=True)
    draw_equilateral_triangle(d, center, 920, fill=None, outline=COLOR_WHITE, width=20, inverted=True)
    
    # Bilingual: "止まれ" & "STOP"
    font_jp = get_font(190, bold=True)
    font_en = get_font(95, bold=True)
    d.text((512, 380), "止まれ", fill=COLOR_WHITE, font=font_jp, anchor="mm")
    d.text((512, 570), "STOP", fill=COLOR_WHITE, font=font_en, anchor="mm")
    img.save(os.path.join(TEX_DIR, "tex_sign_jp_stop.png"))

def gen_tex_uk_speedlimit():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([(20, 20), (1004, 1004)], fill=COLOR_WHITE, outline=COLOR_BLACK, width=12)
    # Diagonal black sash
    d.polygon([(160, 860), (280, 940), (864, 164), (744, 84)], fill=COLOR_BLACK)
    img.save(os.path.join(TEX_DIR, "tex_sign_uk_speedlimit.png"))

def gen_tex_au_kangaroo():
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    center = (512, 512)
    draw_diamond(d, center, 980, fill=COLOR_YELLOW)
    draw_diamond(d, center, 900, fill=None, outline=COLOR_BLACK, width=18)
    
    # Kangaroo Silhouette
    roo_pts = [
        (320, 680), (420, 680), (450, 620), (510, 620), (560, 560),
        (560, 480), (620, 420), (650, 420), (670, 360), (640, 350),
        (620, 380), (580, 390), (540, 450), (480, 480), (450, 530),
        (380, 580), (280, 640)
    ]
    d.polygon(roo_pts, fill=COLOR_BLACK)
    d.ellipse([(635, 345), (665, 375)], fill=COLOR_BLACK) # Head
    img.save(os.path.join(TEX_DIR, "tex_sign_au_kangaroo.png"))

# =============================================================================
# 4. Special Distance & City Mileage Boards (Placas de Distância até X Cidade)
# =============================================================================

def gen_tex_dist_paris():
    # European / French Highway Confirmation Board (Green)
    img = Image.new("RGBA", (1536, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([(15, 15), (1521, 1009)], radius=50, fill=COLOR_HWY_GREEN, outline=COLOR_WHITE, width=20)
    d.rounded_rectangle([(35, 35), (1501, 989)], radius=35, fill=None, outline=COLOR_WHITE, width=8)
    
    # Route badge top left
    d.rounded_rectangle([(70, 65), (250, 160)], radius=15, fill=COLOR_RED, outline=COLOR_WHITE, width=6)
    font_badge = get_font(68, bold=True)
    d.text((160, 112), "A 6", fill=COLOR_WHITE, font=font_badge, anchor="mm")
    
    # City entries & distances
    font_city = get_font(105, bold=True)
    font_dist = get_font(105, bold=True)
    
    # Line 1: Paris
    d.text((120, 300), "PARIS", fill=COLOR_WHITE, font=font_city, anchor="lm")
    d.text((1420, 300), "45 km", fill=COLOR_WHITE, font=font_dist, anchor="rm")
    
    # Divider line
    d.line([(100, 420), (1436, 420)], fill=(255, 255, 255, 160), width=5)
    
    # Line 2: Lyon
    d.text((120, 540), "LYON", fill=COLOR_WHITE, font=font_city, anchor="lm")
    d.text((1420, 540), "285 km", fill=COLOR_WHITE, font=font_dist, anchor="rm")
    
    # Divider line
    d.line([(100, 660), (1436, 660)], fill=(255, 255, 255, 160), width=5)
    
    # Line 3: Marseille
    d.text((120, 780), "MARSEILLE", fill=COLOR_WHITE, font=font_city, anchor="lm")
    d.text((1420, 780), "520 km", fill=COLOR_WHITE, font=font_dist, anchor="rm")
    
    img.save(os.path.join(TEX_DIR, "tex_sign_dist_paris.png"))

def gen_tex_dist_newyork():
    # US Interstate Mileage / Destination Board
    img = Image.new("RGBA", (1536, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([(15, 15), (1521, 1009)], radius=50, fill=COLOR_DARK_GREEN, outline=COLOR_WHITE, width=22)
    d.rounded_rectangle([(35, 35), (1501, 989)], radius=35, fill=None, outline=COLOR_WHITE, width=8)
    
    # Header Interstate Badge
    d.rounded_rectangle([(650, 60), (886, 175)], radius=18, fill=COLOR_BLUE, outline=COLOR_WHITE, width=6)
    font_badge = get_font(62, bold=True)
    d.text((768, 117), "I-95", fill=COLOR_WHITE, font=font_badge, anchor="mm")
    
    font_city = get_font(98, bold=True)
    font_dist = get_font(105, bold=True)
    
    # Destination 1: New York
    d.text((120, 320), "NEW YORK", fill=COLOR_WHITE, font=font_city, anchor="lm")
    d.text((1420, 320), "35", fill=COLOR_WHITE, font=font_dist, anchor="rm")
    
    # Destination 2: Philadelphia
    d.text((120, 540), "PHILADELPHIA", fill=COLOR_WHITE, font=font_city, anchor="lm")
    d.text((1420, 540), "110", fill=COLOR_WHITE, font=font_dist, anchor="rm")
    
    # Destination 3: Washington
    d.text((120, 760), "WASHINGTON", fill=COLOR_WHITE, font=font_city, anchor="lm")
    d.text((1420, 760), "235", fill=COLOR_WHITE, font=font_dist, anchor="rm")
    
    img.save(os.path.join(TEX_DIR, "tex_sign_dist_newyork.png"))

def gen_tex_dist_saopaulo():
    # Brazilian / Mercosul Highway Distance Board
    img = Image.new("RGBA", (1536, 1024), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([(15, 15), (1521, 1009)], radius=50, fill=COLOR_HWY_GREEN, outline=COLOR_WHITE, width=20)
    d.rounded_rectangle([(35, 35), (1501, 989)], radius=35, fill=None, outline=COLOR_WHITE, width=8)
    
    # Top blue header banner for route
    d.rounded_rectangle([(70, 60), (420, 165)], radius=15, fill=COLOR_BRAZIL_BLUE, outline=COLOR_WHITE, width=6)
    font_badge = get_font(62, bold=True)
    d.text((245, 112), "SP-330 / ANHANGUERA", fill=COLOR_WHITE, font=get_font(28, bold=True), anchor="mm")
    
    font_city = get_font(95, bold=True)
    font_dist = get_font(95, bold=True)
    
    # Line 1: São Paulo
    d.text((120, 310), "SÃO PAULO", fill=COLOR_WHITE, font=font_city, anchor="lm")
    d.text((1420, 310), "90 km", fill=COLOR_WHITE, font=font_dist, anchor="rm")
    
    d.line([(100, 420), (1436, 420)], fill=(255, 255, 255, 160), width=5)
    
    # Line 2: Campinas
    d.text((120, 530), "CAMPINAS", fill=COLOR_WHITE, font=font_city, anchor="lm")
    d.text((1420, 530), "180 km", fill=COLOR_WHITE, font=font_dist, anchor="rm")
    
    d.line([(100, 640), (1436, 640)], fill=(255, 255, 255, 160), width=5)
    
    # Line 3: Ribeirão Preto
    d.text((120, 760), "RIBEIRÃO PRETO", fill=COLOR_WHITE, font=font_city, anchor="lm")
    d.text((1420, 760), "370 km", fill=COLOR_WHITE, font=font_dist, anchor="rm")
    
    img.save(os.path.join(TEX_DIR, "tex_sign_dist_saopaulo.png"))

def gen_tex_gantry_signs():
    # Sign Left: Downtown / Exit 1 km
    img1 = Image.new("RGBA", (1400, 900), (0, 0, 0, 0))
    d1 = ImageDraw.Draw(img1)
    d1.rounded_rectangle([(15, 15), (1385, 885)], radius=40, fill=COLOR_HWY_GREEN, outline=COLOR_WHITE, width=20)
    d1.text((700, 220), "CENTRO", fill=COLOR_WHITE, font=get_font(120, bold=True), anchor="mm")
    d1.text((700, 370), "DOWNTOWN", fill=COLOR_WHITE, font=get_font(90, bold=True), anchor="mm")
    d1.text((700, 520), "SAÍDA A 1 km / NEXT EXIT 1 km", fill=COLOR_YELLOW, font=get_font(60, bold=True), anchor="mm")
    
    # Downward lane arrow
    arrow = [(640, 630), (760, 630), (760, 720), (820, 720), (700, 820), (580, 720), (640, 720)]
    d1.polygon(arrow, fill=COLOR_WHITE)
    img1.save(os.path.join(TEX_DIR, "tex_sign_gantry_left.png"))
    
    # Sign Right: Airport / Distance 12 km
    img2 = Image.new("RGBA", (1400, 900), (0, 0, 0, 0))
    d2 = ImageDraw.Draw(img2)
    d2.rounded_rectangle([(15, 15), (1385, 885)], radius=40, fill=COLOR_HWY_GREEN, outline=COLOR_WHITE, width=20)
    d2.text((700, 220), "AEROPORTO", fill=COLOR_WHITE, font=get_font(120, bold=True), anchor="mm")
    d2.text((700, 370), "AIRPORT", fill=COLOR_WHITE, font=get_font(90, bold=True), anchor="mm")
    d2.text((700, 520), "PISTA DIRETA - 12 km", fill=COLOR_WHITE, font=get_font(65, bold=True), anchor="mm")
    
    # Upward straight arrow
    arrow2 = [(640, 790), (760, 790), (760, 700), (820, 700), (700, 600), (580, 700), (640, 700)]
    d2.polygon(arrow2, fill=COLOR_WHITE)
    img2.save(os.path.join(TEX_DIR, "tex_sign_gantry_right.png"))
    
    # Exit Tab Banner
    img3 = Image.new("RGBA", (800, 260), (0, 0, 0, 0))
    d3 = ImageDraw.Draw(img3)
    d3.rounded_rectangle([(10, 10), (790, 250)], radius=25, fill=COLOR_YELLOW, outline=COLOR_BLACK, width=12)
    d3.text((400, 130), "EXIT 14 B", fill=COLOR_BLACK, font=get_font(110, bold=True), anchor="mm")
    img3.save(os.path.join(TEX_DIR, "tex_sign_gantry_tab.png"))

def gen_tex_cantilever():
    img = Image.new("RGBA", (1400, 800), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([(15, 15), (1385, 785)], radius=40, fill=COLOR_HWY_GREEN, outline=COLOR_WHITE, width=20)
    d.text((650, 230), "CIDADE INDUSTRIAL", fill=COLOR_WHITE, font=get_font(98, bold=True), anchor="mm")
    d.text((650, 390), "INDUSTRIAL DISTRICT", fill=COLOR_WHITE, font=get_font(72, bold=True), anchor="mm")
    d.text((650, 560), "SAÍDA A 500 METROS", fill=COLOR_YELLOW, font=get_font(65, bold=True), anchor="mm")
    
    # Slanted exit arrow right
    arrow = [(1100, 620), (1160, 560), (1220, 620), (1200, 480), (1060, 500), (1120, 560), (1060, 620)]
    d.polygon(arrow, fill=COLOR_WHITE)
    img.save(os.path.join(TEX_DIR, "tex_sign_cantilever.png"))

def gen_tex_milestone():
    img = Image.new("RGBA", (600, 900), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Concrete stone background with rounded top
    d.rounded_rectangle([(15, 15), (585, 885)], radius=120, fill=(215, 218, 220, 255), outline=(130, 135, 140, 255), width=14)
    # Red dome cap
    d.pieslice([(15, 15), (585, 280)], start=180, end=0, fill=COLOR_RED)
    
    # Shield & Mileage text
    d.rounded_rectangle([(160, 320), (440, 470)], radius=30, fill=COLOR_HWY_GREEN, outline=COLOR_WHITE, width=8)
    d.text((300, 395), "BR-101", fill=COLOR_WHITE, font=get_font(64, bold=True), anchor="mm")
    
    d.text((300, 580), "KM", fill=COLOR_BLACK, font=get_font(75, bold=True), anchor="mm")
    d.text((300, 720), "250", fill=COLOR_RED, font=get_font(130, bold=True), anchor="mm")
    img.save(os.path.join(TEX_DIR, "tex_sign_milestone.png"))

def gen_tex_paddle():
    img = Image.new("RGBA", (300, 900), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([(10, 10), (290, 890)], radius=30, fill=COLOR_DARK_GREEN, outline=COLOR_WHITE, width=12)
    d.text((150, 160), "MILE", fill=COLOR_WHITE, font=get_font(65, bold=True), anchor="mm")
    d.text((150, 420), "1", fill=COLOR_WHITE, font=get_font(180, bold=True), anchor="mm")
    d.text((150, 620), "4", fill=COLOR_WHITE, font=get_font(180, bold=True), anchor="mm")
    d.text((150, 800), "2", fill=COLOR_WHITE, font=get_font(180, bold=True), anchor="mm")
    img.save(os.path.join(TEX_DIR, "tex_sign_paddle.png"))

def gen_tex_vms_led():
    # Variable Message Sign (VMS / PMV) with amber dot-matrix LED look
    img = Image.new("RGBA", (1600, 600), COLOR_LED_BG)
    d = ImageDraw.Draw(img)
    
    # Outer bezel & border
    d.rectangle([(0, 0), (1599, 599)], outline=(60, 64, 70, 255), width=16)
    
    # Amber LED Text
    font_led = get_font(80, bold=True)
    d.text((800, 200), "[ ! ] CIDADE X: A 25 KM", fill=COLOR_LED_AMBER, font=font_led, anchor="mm")
    d.text((800, 380), "TRÂNSITO LENTO - REDUZA", fill=COLOR_LED_AMBER, font=font_led, anchor="mm")
    
    img.save(os.path.join(TEX_DIR, "tex_sign_vms_led.png"))

def gen_tex_city_welcome():
    img = Image.new("RGBA", (1400, 800), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([(15, 15), (1385, 785)], radius=45, fill=(30, 45, 60, 255), outline=(210, 175, 80, 255), width=22)
    d.rounded_rectangle([(35, 35), (1365, 765)], radius=30, fill=None, outline=(210, 175, 80, 255), width=6)
    
    font_sub = get_font(55, bold=True)
    font_main = get_font(110, bold=True)
    font_info = get_font(42, bold=False)
    
    d.text((700, 140), "BEM-VINDO A  *  WELCOME TO", fill=(210, 175, 80, 255), font=font_sub, anchor="mm")
    d.text((700, 310), "SANTA MARIA", fill=COLOR_WHITE, font=font_main, anchor="mm")
    d.text((700, 470), "CIDADE HISTÓRICA E UNIVERSITÁRIA", fill=(190, 215, 235, 255), font=get_font(48, bold=True), anchor="mm")
    
    d.line([(250, 560), (1150, 560)], fill=(210, 175, 80, 200), width=4)
    d.text((700, 650), "ALTITUDE: 450m  *  POPULAÇÃO: 280.000 HABITANTES", fill=(220, 225, 230, 255), font=font_info, anchor="mm")
    
    img.save(os.path.join(TEX_DIR, "tex_sign_city_welcome.png"))

def main():
    print("Generating Worldwide Road Sign Textures...")
    gen_tex_eu_stop()
    gen_tex_eu_yield()
    gen_tex_eu_curve()
    gen_tex_eu_speed80()
    gen_tex_eu_no_entry()
    gen_tex_eu_roundabout()
    gen_tex_eu_motorway()
    gen_tex_us_curve()
    gen_tex_us_speed65()
    gen_tex_us_oneway()
    gen_tex_us_interstate()
    gen_tex_br_lombada()
    gen_tex_br_pare()
    gen_tex_br_shield()
    gen_tex_jp_stop()
    gen_tex_uk_speedlimit()
    gen_tex_au_kangaroo()
    gen_tex_dist_paris()
    gen_tex_dist_newyork()
    gen_tex_dist_saopaulo()
    gen_tex_gantry_signs()
    gen_tex_cantilever()
    gen_tex_milestone()
    gen_tex_paddle()
    gen_tex_vms_led()
    gen_tex_city_welcome()
    print("All 25+ Traffic Sign Textures generated successfully in:", TEX_DIR)

if __name__ == "__main__":
    main()
