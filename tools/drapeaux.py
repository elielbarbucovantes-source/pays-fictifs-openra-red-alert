"""Génère les drapeaux du salon (mods/fictifs/uibits/flags*.png) et l'icône du mod.

Reprend les drapeaux de Red Alert (glyphs.png du moteur) et ajoute ceux des pays fictifs.
Usage : python3 tools/drapeaux.py
"""
import os
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(__file__), "..")
GLYPHS = os.path.join(ROOT, "engine", "mods", "ra", "uibits")
OUT = os.path.join(ROOT, "mods", "fictifs", "uibits")

# Drapeaux d'origine, dans l'ordre de glyphs.png (x = 226, y = 49 + 16 * i)
RA_FLAGS = ["england", "germany", "france", "spain", "turkey", "greece", "ukraine",
            "russia", "allies", "soviet", "RandomSoviet", "RandomAllies", "Random"]

# Pays fictifs, dans l'ordre où ils suivent les drapeaux d'origine.
FICTIFS = ["elielistan", "rubenie", "australouis"]

NAVY = (22, 40, 92)
WHITE = (240, 240, 236)
RED = (214, 24, 30)
GOLD = (222, 176, 52)
GREEN = (30, 110, 60)
DARK_GREEN = (18, 70, 38)
OCEAN = (18, 86, 160)
ISLAND = (46, 150, 70)


def aguila(draw, cx, cy, s, body=WHITE, tips=RED):
    """Aigle aux ailes déployées, pointes des ailes rouge vif. s = demi-envergure."""
    def p(x, y):
        return (cx + x * s, cy + y * s)

    for side in (-1, 1):
        wing = [p(0.08 * side, -0.05), p(0.45 * side, -0.42), p(0.72 * side, -0.52),
                p(0.66 * side, -0.30), p(0.52 * side, -0.12), p(0.12 * side, 0.18)]
        draw.polygon(wing, fill=body)
        tip = [p(0.60 * side, -0.47), p(0.72 * side, -0.52), p(1.0 * side, -0.62),
               p(0.86 * side, -0.38), p(0.66 * side, -0.30)]
        draw.polygon(tip, fill=tips)
    draw.ellipse([p(-0.13, -0.18), p(0.13, 0.30)], fill=body)            # corps
    draw.ellipse([p(-0.09, -0.40), p(0.09, -0.16)], fill=body)           # tête
    draw.polygon([p(0.05, -0.33), p(0.17, -0.28), p(0.05, -0.24)], fill=GOLD)  # bec
    draw.polygon([p(-0.14, 0.26), p(0.14, 0.26), p(0.20, 0.52), p(-0.20, 0.52)], fill=body)  # queue


def elielistan(w, h):
    img = Image.new("RGBA", (w, h), NAVY + (255,))
    d = ImageDraw.Draw(img)
    band = h // 6
    d.rectangle([0, h - band, w, h], fill=RED)      # liseré rouge en bas
    aguila(d, w / 2, h * 0.45, h * 0.62)
    return img


def montagne(draw, cx, base, s, body=GREEN, edge=DARK_GREEN, snow=WHITE):
    """Massif à deux sommets enneigés, cerné de vert sombre. s = demi-largeur, base = pied."""
    def p(x, y):
        return (cx + x * s, base + y * s)

    peaks = ((-0.38, 0.75, 0.55), (0.22, 1.0, 0.70))   # (x du sommet, hauteur, demi-base)
    for grow, colour in ((0.10, edge), (0.0, body)):
        for x, hgt, foot in peaks:
            draw.polygon([p(x - foot - grow, grow * 0.5), p(x, -hgt - grow * 1.4),
                          p(x + foot + grow, grow * 0.5)], fill=colour)
    for x, hgt, foot in peaks:                           # neige : un tiers du sommet, bord dentelé
        k = 0.34
        y0 = -hgt * (1 - k)
        dx = foot * k
        draw.polygon([p(x - dx, y0), p(x, -hgt), p(x + dx, y0), p(x + dx / 3, y0 + 0.06),
                      p(x, y0 - 0.02), p(x - dx / 3, y0 + 0.06)], fill=snow)


def rubenie(w, h):
    img = Image.new("RGBA", (w, h), WHITE + (255,))
    d = ImageDraw.Draw(img)
    d.rectangle([0, h * 2 // 3, w, h], fill=GREEN)  # tiers bas vert
    montagne(d, w / 2, h * 0.80, h * 0.62)
    return img


def etoile(draw, cx, cy, r, fill):
    """Étoile à cinq branches."""
    import math
    pts = []
    for k in range(10):
        a = -math.pi / 2 + k * math.pi / 5
        rr = r if k % 2 == 0 else r * 0.42
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    draw.polygon(pts, fill=fill)


def australouis(w, h):
    """Bleu océan, bande de vagues blanches, archipel de trois îles vertes, étoile."""
    import math
    img = Image.new("RGBA", (w, h), OCEAN + (255,))
    d = ImageDraw.Draw(img)
    top, band = h * 0.62, h * 0.14                        # bande de vagues
    amp, waves = h * 0.035, 3
    upper = [(x, top + amp * math.sin(2 * math.pi * waves * x / w)) for x in range(0, w + 1, max(1, w // 120))]
    lower = [(x, y + band) for x, y in reversed(upper)]
    d.polygon(upper + lower, fill=WHITE + (255,))
    for cx, rw, rh in ((0.36, 0.075, 0.15), (0.57, 0.11, 0.25), (0.79, 0.065, 0.11)):   # îles posées sur les vagues
        d.pieslice([w * (cx - rw), top - h * rh, w * (cx + rw), top + h * rh], 180, 360, fill=ISLAND + (255,))
    etoile(d, w * 0.16, h * 0.28, h * 0.17, WHITE + (255,))
    return img


def flag(name, w, h):
    big = {"elielistan": elielistan, "rubenie": rubenie, "australouis": australouis}[name](w * 8, h * 8)
    return big.resize((w, h), Image.LANCZOS)


def pow2(n):
    """OpenRA refuse les textures dont les côtés ne sont pas des puissances de deux."""
    return 1 << (n - 1).bit_length()


def build(scale, suffix):
    glyphs = Image.open(os.path.join(GLYPHS, "glyphs" + suffix + ".png"))
    names = RA_FLAGS + FICTIFS
    size = (pow2(30 * scale), pow2(16 * scale * len(names)))
    sheet = Image.new("RGBA", size, (0, 0, 0, 0))
    for i, name in enumerate(RA_FLAGS):
        box = (226 * scale, (49 + 16 * i) * scale, 256 * scale, (64 + 16 * i) * scale)
        sheet.paste(glyphs.crop(box), (0, 16 * i * scale))
    for i, name in enumerate(FICTIFS, len(RA_FLAGS)):
        sheet.paste(flag(name, 30 * scale, 15 * scale), (0, 16 * i * scale))
    sheet.save(os.path.join(OUT, "flags" + suffix + ".png"))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for scale, suffix in ((1, ""), (2, "-2x"), (3, "-3x")):
        build(scale, suffix)

    icon = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    d = ImageDraw.Draw(icon)
    d.ellipse([8, 8, 248, 248], fill=NAVY + (255,), outline=RED + (255,), width=12)
    aguila(d, 128, 132, 118)
    icon.resize((32, 32), Image.LANCZOS).save(os.path.join(ROOT, "mods", "fictifs", "icon.png"))
    for name in FICTIFS:
        flag(name, 300, 150).save(os.path.join(ROOT, "tools", "apercu-drapeau-" + name + ".png"))
