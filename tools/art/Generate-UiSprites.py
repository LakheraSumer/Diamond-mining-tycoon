"""Generate polished UI sprites and icons for Diamond Mine Tycoon matching the reference."""
from pathlib import Path
import math
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/_Project/Resources/Ui"
OUT.mkdir(parents=True, exist_ok=True)

# 1. 9-slice cream card
def gen_card():
    im = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # subtle drop shadow
    d.rounded_rectangle([4, 6, 124, 126], radius=20, fill=(20, 33, 44, 25))
    # card body
    d.rounded_rectangle([4, 4, 124, 122], radius=20, fill=(249, 246, 237, 252), outline=(226, 218, 199, 255), width=2)
    im.save(OUT / "card_bg.png")

# 2. 9-slice dark navy currency pill
def gen_dark_pill():
    im = Image.new("RGBA", (128, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([2, 2, 126, 62], radius=28, fill=(24, 37, 50, 230), outline=(48, 70, 92, 255), width=2)
    im.save(OUT / "dark_pill.png")

# 3. 9-slice green primary button
def gen_btn_green():
    im = Image.new("RGBA", (128, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([2, 6, 126, 62], radius=16, fill=(35, 115, 45, 255))
    d.rounded_rectangle([2, 2, 126, 58], radius=16, fill=(62, 168, 67, 255))
    d.rounded_rectangle([4, 4, 124, 28], radius=12, fill=(90, 200, 95, 220))
    im.save(OUT / "btn_green.png")

# 4. 9-slice blue button
def gen_btn_blue():
    im = Image.new("RGBA", (128, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([2, 6, 126, 62], radius=16, fill=(25, 95, 175, 255))
    d.rounded_rectangle([2, 2, 126, 58], radius=16, fill=(44, 139, 232, 255))
    d.rounded_rectangle([4, 4, 124, 28], radius=12, fill=(80, 165, 250, 220))
    im.save(OUT / "btn_blue.png")

# 5. Circular navy button
def gen_btn_circle():
    im = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([6, 10, 122, 126], fill=(12, 18, 25, 120))
    d.ellipse([6, 6, 122, 122], fill=(42, 64, 82, 255))
    d.ellipse([10, 10, 118, 118], fill=(24, 37, 50, 255))
    im.save(OUT / "btn_circle.png")

# 6. Virtual joystick base & knob
def gen_joystick():
    im = Image.new("RGBA", (160, 160), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([6, 6, 154, 154], fill=(20, 32, 45, 140), outline=(120, 160, 190, 160), width=3)
    cx, cy = 80, 80
    for ang in (0, 90, 180, 270):
        rad = math.radians(ang)
        ax = cx + math.cos(rad) * 62
        ay = cy + math.sin(rad) * 62
        d.ellipse([ax-5, ay-5, ax+5, ay+5], fill=(200, 230, 255, 180))
    im.save(OUT / "joystick_base.png")

    kn = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    dk = ImageDraw.Draw(kn)
    dk.ellipse([4, 6, 92, 94], fill=(10, 18, 26, 120))
    dk.ellipse([4, 4, 92, 92], fill=(40, 70, 95, 230), outline=(130, 190, 240, 240), width=3)
    dk.ellipse([14, 14, 82, 82], fill=(60, 105, 140, 255))
    dk.ellipse([38, 38, 58, 58], fill=(180, 225, 255, 240))
    kn.save(OUT / "joystick_knob.png")

# 7. Shiny Gold Coin Icon
def gen_coin():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([4, 8, 92, 94], fill=(140, 90, 15, 160))
    d.ellipse([4, 4, 92, 92], fill=(235, 175, 40, 255), outline=(255, 220, 90, 255), width=3)
    d.ellipse([14, 14, 82, 82], fill=(248, 196, 52, 255), outline=(200, 140, 20, 255), width=2)
    d.polygon([(48, 24), (66, 48), (48, 72), (30, 48)], fill=(255, 235, 130, 255))
    d.polygon([(48, 28), (62, 48), (48, 68), (34, 48)], fill=(255, 248, 190, 255))
    im.save(OUT / "icon_coin.png")

# 8. Faceted Cyan Diamond Icon
def gen_diamond():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon([(26, 30), (70, 30), (88, 46), (48, 88), (8, 46)], fill=(20, 120, 165, 255))
    d.polygon([(32, 32), (64, 32), (58, 46), (38, 46)], fill=(120, 225, 255, 255))
    d.polygon([(26, 32), (32, 32), (38, 46), (10, 46)], fill=(55, 175, 225, 255))
    d.polygon([(64, 32), (70, 32), (86, 46), (58, 46)], fill=(75, 195, 240, 255))
    d.polygon([(10, 46), (38, 46), (48, 86)], fill=(35, 155, 215, 255))
    d.polygon([(38, 46), (58, 46), (48, 86)], fill=(90, 215, 255, 255))
    d.polygon([(58, 46), (86, 46), (48, 86)], fill=(45, 170, 225, 255))
    d.ellipse([24, 26, 34, 36], fill=(255, 255, 255, 230))
    im.save(OUT / "icon_diamond.png")

# 9. Settings Gear Icon
def gen_gear():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx, cy = 48, 48
    for i in range(8):
        ang = i * 45
        rad = math.radians(ang)
        cos_a, sin_a = math.cos(rad), math.sin(rad)
        cos_p, sin_p = -sin_a, cos_a
        w, l = 8, 38
        p1 = (cx + cos_a*l - cos_p*w, cy + sin_a*l - sin_p*w)
        p2 = (cx + cos_a*l + cos_p*w, cy + sin_a*l + sin_p*w)
        p3 = (cx - cos_a*l + cos_p*w, cy - sin_a*l + sin_p*w)
        p4 = (cx - cos_a*l - cos_p*w, cy - sin_a*l - sin_p*w)
        d.polygon([p1, p2, p3, p4], fill=(255, 255, 255, 240))
    d.ellipse([20, 20, 76, 76], fill=(255, 255, 255, 240))
    d.ellipse([34, 34, 62, 62], fill=(0, 0, 0, 0))
    im.save(OUT / "icon_gear.png")

# 10. Hand Interact Icon
def gen_hand():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([28, 40, 68, 78], radius=10, fill=(255, 255, 255, 250))
    d.rounded_rectangle([30, 20, 39, 46], radius=4, fill=(255, 255, 255, 250))
    d.rounded_rectangle([40, 16, 49, 46], radius=4, fill=(255, 255, 255, 250))
    d.rounded_rectangle([50, 18, 59, 46], radius=4, fill=(255, 255, 255, 250))
    d.rounded_rectangle([60, 24, 68, 46], radius=4, fill=(255, 255, 255, 250))
    d.polygon([(28, 50), (14, 52), (18, 64), (30, 62)], fill=(255, 255, 255, 250))
    d.ellipse([12, 48, 22, 58], fill=(255, 255, 255, 250))
    im.save(OUT / "icon_hand.png")

# 11. Pickaxe Icon
def gen_pickaxe():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.line([(24, 76), (68, 30)], fill=(165, 115, 60, 255), width=8)
    d.arc([22, 12, 84, 74], 215, 325, fill=(210, 220, 230, 255), width=10)
    d.line([(68, 28), (74, 24)], fill=(240, 245, 255, 255), width=5)
    im.save(OUT / "icon_pickaxe.png")

# 12. Diamond Ring Icon
def gen_ring():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([18, 28, 78, 88], outline=(245, 185, 45, 255), width=10)
    d.ellipse([21, 31, 75, 85], outline=(255, 225, 95, 255), width=3)
    d.polygon([(36, 24), (60, 24), (68, 34), (48, 52), (28, 34)], fill=(50, 185, 235, 255), outline=(255, 255, 255, 255), width=1)
    d.polygon([(40, 24), (56, 24), (52, 34), (44, 34)], fill=(180, 240, 255, 255))
    d.polygon([(44, 34), (52, 34), (48, 50)], fill=(120, 220, 255, 255))
    d.polygon([(68, 14), (72, 22), (80, 26), (72, 30), (68, 38), (64, 30), (56, 26), (64, 22)], fill=(255, 255, 255, 240))
    im.save(OUT / "icon_ring.png")

# 13. Gold Necklace Icon
def gen_necklace():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.arc([16, 16, 80, 72], 30, 150, fill=(245, 185, 45, 255), width=4)
    d.ellipse([45, 68, 51, 74], fill=(245, 185, 45, 255))
    d.polygon([(42, 74), (54, 74), (57, 80), (48, 92), (39, 80)], fill=(50, 185, 235, 255), outline=(255, 255, 255, 255), width=1)
    im.save(OUT / "icon_necklace.png")

# 14. Gem Earrings Icon
def gen_earrings():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for cx in (34, 62):
        d.arc([cx-6, 14, cx+6, 28], 180, 360, fill=(245, 185, 45, 255), width=3)
        d.line([(cx+6, 21), (cx+6, 38)], fill=(245, 185, 45, 255), width=3)
        d.ellipse([cx-9, 46, cx+9, 74], fill=(50, 185, 235, 255), outline=(255, 255, 255, 255), width=1)
        d.polygon([(cx-9, 60), (cx+9, 60), (cx, 40)], fill=(50, 185, 235, 255))
        d.ellipse([cx-4, 52, cx+4, 62], fill=(160, 235, 255, 240))
    im.save(OUT / "icon_earrings.png")

# 15. Raw Ore / Stone Icon
def gen_ore():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    pts = [(24, 38), (42, 22), (68, 26), (82, 48), (74, 74), (46, 82), (22, 68)]
    d.polygon(pts, fill=(95, 105, 115, 255), outline=(140, 155, 168, 255), width=2)
    d.polygon([(24, 38), (42, 22), (54, 48), (36, 56)], fill=(125, 138, 150, 255))
    d.polygon([(42, 22), (68, 26), (72, 48), (54, 48)], fill=(145, 160, 175, 255))
    d.polygon([(54, 48), (72, 48), (74, 74), (46, 82)], fill=(80, 90, 100, 255))
    im.save(OUT / "icon_ore.png")

# 16. Gold Bar Icon
def gen_gold_bar():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon([(28, 48), (68, 48), (78, 68), (18, 68)], fill=(255, 215, 55, 255), outline=(200, 150, 20, 255), width=2)
    d.polygon([(32, 50), (64, 50), (60, 56), (36, 56)], fill=(255, 245, 140, 255))
    d.polygon([(18, 68), (78, 68), (74, 76), (14, 76)], fill=(210, 155, 25, 255))
    im.save(OUT / "icon_gold_bar.png")

# 17. Carry Sack Icon
def gen_sack():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([22, 34, 74, 86], fill=(135, 95, 60, 255))
    d.polygon([(38, 26), (58, 26), (62, 38), (34, 38)], fill=(115, 75, 45, 255))
    d.rounded_rectangle([42, 22, 54, 28], radius=2, fill=(210, 180, 110, 255))
    im.save(OUT / "icon_sack.png")

# 18. Cart with Diamonds Icon
def gen_cart_diamonds():
    im = Image.new("RGBA", (128, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for k in range(12):
        dx = 30 + (k % 4) * 18
        dy = 20 + (k // 4) * 14
        d.polygon([(dx, dy-6), (dx+8, dy), (dx, dy+6), (dx-8, dy)], fill=(70, 205, 255, 255))
    d.polygon([(22, 40), (106, 40), (96, 74), (32, 74)], fill=(75, 105, 120, 255), outline=(180, 195, 205, 255), width=2)
    d.line([(26, 54), (102, 54)], fill=(225, 175, 45, 255), width=4)
    d.ellipse([32, 68, 52, 88], fill=(35, 45, 55, 255), outline=(175, 185, 195, 255), width=3)
    d.ellipse([76, 68, 96, 88], fill=(35, 45, 55, 255), outline=(175, 185, 195, 255), width=3)
    im.save(OUT / "icon_cart_diamonds.png")

# 19. Trophies
def gen_trophies():
    colors = [
        ("gold", (245, 190, 45), (255, 230, 95)),
        ("silver", (185, 195, 205), (235, 240, 245)),
        ("bronze", (195, 115, 65), (235, 160, 110))
    ]
    for name, c1, c2 in colors:
        im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.polygon([(18, 14), (46, 14), (42, 38), (22, 38)], fill=c1, outline=c2, width=1)
        d.arc([10, 16, 26, 32], 90, 270, fill=c1, width=3)
        d.arc([38, 16, 54, 32], 270, 90, fill=c1, width=3)
        d.rectangle([29, 38, 35, 46], fill=c1)
        d.rounded_rectangle([20, 46, 44, 52], radius=2, fill=c1)
        im.save(OUT / f"trophy_{name}.png")

# 20. Avatars for Leaderboard
def gen_avatars():
    avs = [
        ("fox", (235, 115, 45)),
        ("maven", (220, 165, 130)),
        ("raccoon", (115, 125, 135)),
        ("sunny", (245, 195, 55)),
        ("otter", (155, 105, 65)),
        ("you", (45, 155, 235)),
        ("panda", (240, 240, 240)),
        ("whale", (65, 120, 185)),
        ("lynx", (205, 145, 75))
    ]
    for name, col in avs:
        im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.ellipse([2, 2, 62, 62], fill=col, outline=(255, 255, 255, 240), width=3)
        d.ellipse([20, 26, 26, 32], fill=(25, 35, 45, 255))
        d.ellipse([38, 26, 44, 32], fill=(25, 35, 45, 255))
        d.arc([24, 32, 40, 44], 20, 160, fill=(25, 35, 45, 255), width=2)
        im.save(OUT / f"avatar_{name}.png")

# 21. Order Speech Bubble
def gen_bubble():
    im = Image.new("RGBA", (140, 110), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([8, 8, 132, 82], fill=(255, 255, 255, 252), outline=(215, 225, 235, 255), width=3)
    d.polygon([(46, 75), (28, 104), (64, 78)], fill=(255, 255, 255, 252))
    d.line([(46, 75), (28, 104)], fill=(215, 225, 235, 255), width=3)
    d.line([(28, 104), (64, 78)], fill=(215, 225, 235, 255), width=3)
    im.save(OUT / "speech_bubble.png")

# Run all
gen_card(); gen_dark_pill(); gen_btn_green(); gen_btn_blue(); gen_btn_circle(); gen_joystick()
gen_coin(); gen_diamond(); gen_gear(); gen_hand(); gen_pickaxe(); gen_ring(); gen_necklace(); gen_earrings()
gen_ore(); gen_gold_bar(); gen_sack(); gen_cart_diamonds(); gen_trophies(); gen_avatars(); gen_bubble()
print("All UI sprites and icons generated successfully in Assets/_Project/Resources/Ui/")
