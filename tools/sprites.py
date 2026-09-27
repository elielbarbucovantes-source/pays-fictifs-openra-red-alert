"""Génère les sprites propres aux pays fictifs dans mods/fictifs/bits/.

Images indexées sur la palette de Red Alert (reprise de bits/luna.png), en évitant
les couleurs spéciales : ombres, couleurs du joueur (80-95) et couleurs animées.
Usage : python3 tools/sprites.py
"""
import math
import os
from PIL import Image, ImageDraw
from drapeaux import aguila, montagne, NAVY, RED, WHITE, GOLD, GREEN, DARK_GREEN

ROOT = os.path.join(os.path.dirname(__file__), "..")
BITS = os.path.join(ROOT, "mods", "fictifs", "bits")
SS = 4  # suréchantillonnage avant réduction

PALETTE = Image.open(os.path.join(BITS, "luna.png")).getpalette()[:768]
ALLOWED = [i for i in range(16, 240) if not (80 <= i <= 103)]
_cache = {}


def nearest(rgb):
    if rgb not in _cache:
        _cache[rgb] = min(ALLOWED, key=lambda i: sum((PALETTE[3 * i + k] - rgb[k]) ** 2 for k in range(3)))
    return _cache[rgb]


def to_indexed(frames, w, h):
    """Assemble des images RGBA (taille finale) en une feuille indexée horizontale."""
    sheet = Image.new("P", (w * len(frames), h), 0)
    sheet.putpalette(PALETTE)
    for n, im in enumerate(frames):
        px = im.load()
        for y in range(h):
            for x in range(w):
                r, g, b, a = px[x, y]
                if a >= 128:
                    sheet.putpixel((n * w + x, y), nearest((r, g, b)))
    return sheet


def save(sheet, name, w, h, count):
    from PIL import PngImagePlugin
    info = PngImagePlugin.PngInfo()
    info.add_text("FrameSize", f"{w},{h}")
    info.add_text("FrameAmount", str(count))
    sheet.save(os.path.join(BITS, name + ".png"), pnginfo=info, transparency=0)


def canvas(w, h, bg=(0, 0, 0, 0)):
    im = Image.new("RGBA", (w * SS, h * SS), bg)
    return im, ImageDraw.Draw(im)


def shrink(im, w, h):
    return im.resize((w, h), Image.LANCZOS)


# ---------------------------------------------------------------------------
# Aguila Gate : bouche de tunnel (48x48, 8 images d'animation)
# ---------------------------------------------------------------------------
def gate_frame(t):
    w = h = 48
    im, d = canvas(w, h)
    c = w * SS / 2
    d.ellipse([c - 21 * SS, c - 13 * SS, c + 21 * SS, c + 15 * SS], fill=(92, 84, 68, 255))       # déblais
    d.ellipse([c - 18 * SS, c - 10 * SS, c + 18 * SS, c + 12 * SS], fill=(150, 20, 24, 255))       # anneau rouge
    d.ellipse([c - 15 * SS, c - 8 * SS, c + 15 * SS, c + 10 * SS], fill=(16, 16, 20, 255))         # puits
    for k in range(6):                                                                         # spirale
        a = t * math.pi / 4 + k * math.pi / 3
        x, y = c + math.cos(a) * 10 * SS, c + 1 * SS + math.sin(a) * 6 * SS
        d.ellipse([x - 2 * SS, y - 1.5 * SS, x + 2 * SS, y + 1.5 * SS], fill=(230, 60, 40, 255))
    for side in (-1, 1):                                                                       # étais
        d.rectangle([c + side * 19 * SS - 2 * SS, c - 14 * SS, c + side * 19 * SS + 2 * SS, c + 4 * SS], fill=(120, 110, 96, 255))
    return shrink(im, w, h)


# ---------------------------------------------------------------------------
# Icônes (64x48) : palais et pouvoirs de soutien
# ---------------------------------------------------------------------------
def icon_base(bg=NAVY):
    im, d = canvas(64, 48, bg + (255,))
    d.rectangle([0, 0, 64 * SS - 1, 48 * SS - 1], outline=RED + (255,), width=2 * SS)
    return im, d


def icon_palacio():
    im, d = icon_base((60, 70, 60))
    s = SS
    d.rectangle([10 * s, 26 * s, 54 * s, 42 * s], fill=(210, 204, 186, 255))
    d.polygon([(8 * s, 27 * s), (32 * s, 16 * s), (56 * s, 27 * s)], fill=(190, 184, 166, 255))
    for x in range(14, 54, 8):
        d.rectangle([x * s, 29 * s, (x + 3) * s, 41 * s], fill=(160, 154, 138, 255))
    d.line([(32 * s, 16 * s), (32 * s, 5 * s)], fill=(40, 40, 40, 255), width=s)
    d.rectangle([32 * s, 5 * s, 42 * s, 11 * s], fill=NAVY + (255,))
    d.rectangle([32 * s, 10 * s, 42 * s, 11 * s], fill=RED + (255,))
    return shrink(im, 64, 48)


def icon_ojo():
    im, d = icon_base()
    s = SS
    d.ellipse([12 * s, 12 * s, 52 * s, 36 * s], fill=WHITE + (255,))
    d.ellipse([24 * s, 14 * s, 40 * s, 34 * s], fill=GOLD + (255,))
    d.ellipse([28 * s, 19 * s, 36 * s, 29 * s], fill=(10, 10, 10, 255))
    d.line([(12 * s, 24 * s), (4 * s, 20 * s)], fill=RED + (255,), width=2 * s)
    d.line([(52 * s, 24 * s), (60 * s, 20 * s)], fill=RED + (255,), width=2 * s)
    return shrink(im, 64, 48)


def icon_gate():
    im, d = icon_base((50, 44, 36))
    s = SS
    for cx in (18, 46):
        d.ellipse([(cx - 11) * s, 14 * s, (cx + 11) * s, 36 * s], fill=(150, 20, 24, 255))
        d.ellipse([(cx - 8) * s, 17 * s, (cx + 8) * s, 33 * s], fill=(10, 10, 12, 255))
    d.line([(22 * s, 12 * s), (42 * s, 12 * s)], fill=WHITE + (255,), width=2 * s)
    d.polygon([(42 * s, 8 * s), (48 * s, 12 * s), (42 * s, 16 * s)], fill=WHITE + (255,))
    return shrink(im, 64, 48)


def icon_bridge():
    im, d = icon_base((70, 110, 150))
    s = SS
    d.polygon([(0, 30 * s), (14 * s, 22 * s), (14 * s, 48 * s), (0, 48 * s)], fill=(120, 100, 70, 255))
    d.polygon([(64 * s, 30 * s), (50 * s, 22 * s), (50 * s, 48 * s), (64 * s, 48 * s)], fill=(120, 100, 70, 255))
    d.rectangle([10 * s, 20 * s, 54 * s, 26 * s], fill=(90, 96, 90, 255))
    for x in range(14, 54, 8):
        d.line([(x * s, 20 * s), ((x + 4) * s, 26 * s)], fill=(50, 54, 50, 255), width=s)
    d.rectangle([10 * s, 26 * s, 12 * s, 40 * s], fill=RED + (255,))
    d.rectangle([52 * s, 26 * s, 54 * s, 40 * s], fill=RED + (255,))
    return shrink(im, 64, 48)


def icon_orden():
    im, d = icon_base((110, 20, 24))
    s = SS
    d.rectangle([16 * s, 6 * s, 48 * s, 42 * s], fill=(236, 230, 210, 255))
    for y in (12, 17, 22, 27):
        d.line([(20 * s, y * s), (44 * s, y * s)], fill=(90, 90, 90, 255), width=s)
    d.ellipse([34 * s, 29 * s, 46 * s, 41 * s], fill=RED + (255,))
    d.text((5 * s, 4 * s), "10", fill=GOLD + (255,))
    return shrink(im, 64, 48)


def icon_strike():
    im, d = icon_base((40, 60, 90))
    aguila(d, 32 * SS, 22 * SS, 30 * SS)
    s = SS
    for x in (14, 32, 50):
        d.line([(x * s, 36 * s), (x * s, 44 * s)], fill=(250, 180, 40, 255), width=2 * s)
    return shrink(im, 64, 48)


def icon_pelicano():
    """Avion logistique : gros porteur et parachutes."""
    im, d = icon_base((70, 100, 130))
    s = SS
    d.polygon([(6 * s, 16 * s), (58 * s, 16 * s), (52 * s, 22 * s), (12 * s, 22 * s)], fill=(200, 200, 196, 255))  # ailes
    d.rectangle([28 * s, 6 * s, 36 * s, 30 * s], fill=(170, 170, 166, 255))                                      # fuselage
    d.polygon([(24 * s, 28 * s), (40 * s, 28 * s), (36 * s, 32 * s), (28 * s, 32 * s)], fill=(150, 150, 146, 255))
    for x in (14, 32, 50):                                                                                        # parachutes
        d.pieslice([(x - 6) * s, 30 * s, (x + 6) * s, 42 * s], 180, 360, fill=WHITE + (255,))
        d.line([((x - 6) * s, 36 * s), (x * s, 44 * s), ((x + 6) * s, 36 * s)], fill=(40, 40, 40, 255), width=s)
    return shrink(im, 64, 48)


def icon_antena():
    """Antenne de gouvernement provisoire : mât, drapeau, ondes."""
    im, d = icon_base((60, 70, 60))
    s = SS
    d.polygon([(26 * s, 44 * s), (32 * s, 16 * s), (38 * s, 44 * s)], outline=(200, 200, 200, 255), width=s)
    for y in (24, 32, 40):
        d.line([((32 - (y - 16) * 6 // 28) * s, y * s), ((32 + (y - 16) * 6 // 28) * s, y * s)], fill=(200, 200, 200, 255), width=s)
    for r in (5, 10):
        d.arc([(32 - r) * s, (16 - r) * s, (32 + r) * s, (16 + r) * s], 200, 340, fill=GOLD + (255,), width=s)
    d.rectangle([38 * s, 24 * s, 56 * s, 34 * s], fill=NAVY + (255,))
    d.rectangle([38 * s, 32 * s, 56 * s, 34 * s], fill=RED + (255,))
    d.line([(38 * s, 24 * s), (38 * s, 44 * s)], fill=(60, 60, 60, 255), width=s)
    return shrink(im, 64, 48)


def icon_nuclear():
    """Aguila Nuclear : char lourd et trèfle radioactif."""
    im, d = icon_base((50, 60, 40))
    s = SS
    d.rectangle([6 * s, 28 * s, 50 * s, 40 * s], fill=(96, 104, 80, 255))      # caisse
    d.ellipse([6 * s, 36 * s, 50 * s, 44 * s], fill=(40, 40, 36, 255))         # chenilles
    d.rectangle([16 * s, 20 * s, 38 * s, 29 * s], fill=(110, 120, 92, 255))    # tourelle
    d.rectangle([38 * s, 23 * s, 60 * s, 26 * s], fill=(80, 86, 66, 255))      # canon
    cx, cy, r = 50 * s, 12 * s, 9 * s
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(250, 210, 30, 255))
    for a in (90, 210, 330):
        d.pieslice([cx - r + s, cy - r + s, cx + r - s, cy + r - s], a - 30, a + 30, fill=(10, 10, 10, 255))
    d.ellipse([cx - 2 * s, cy - 2 * s, cx + 2 * s, cy + 2 * s], fill=(10, 10, 10, 255))
    return shrink(im, 64, 48)


# ---------------------------------------------------------------------------
# Rubénie : icône du QG (bunker de commandement adossé à la montagne)
# ---------------------------------------------------------------------------
def icon_qg():
    im, d = canvas(64, 48, (170, 190, 200, 255))
    s = SS
    montagne(d, 32 * s, 40 * s, 30 * s)
    d.rectangle([12 * s, 30 * s, 52 * s, 44 * s], fill=(120, 124, 110, 255))      # casemate
    d.rectangle([12 * s, 28 * s, 52 * s, 31 * s], fill=(90, 94, 84, 255))         # toit
    for x in (18, 30, 42):
        d.rectangle([x * s, 34 * s, (x + 5) * s, 37 * s], fill=(30, 30, 30, 255))  # meurtrières
    d.line([(47 * s, 28 * s), (47 * s, 8 * s)], fill=(40, 40, 40, 255), width=s)
    d.rectangle([47 * s, 8 * s, 59 * s, 12 * s], fill=WHITE + (255,))
    d.rectangle([47 * s, 12 * s, 59 * s, 16 * s], fill=GREEN + (255,))
    d.rectangle([0, 0, 64 * s - 1, 48 * s - 1], outline=GREEN + (255,), width=2 * s)
    return shrink(im, 64, 48)


def icon_avion_reco():
    """Avion de reconnaissance vu de dessus, grandes ailes droites, sur ciel."""
    im, d = canvas(64, 48, (120, 150, 190, 255))
    s = SS
    body = (200, 200, 196, 255)
    d.polygon([(30 * s, 6 * s), (34 * s, 6 * s), (35 * s, 42 * s), (29 * s, 42 * s)], fill=body)      # fuselage
    d.polygon([(4 * s, 22 * s), (60 * s, 22 * s), (60 * s, 26 * s), (4 * s, 26 * s)], fill=body)      # ailes
    d.polygon([(24 * s, 38 * s), (40 * s, 38 * s), (40 * s, 41 * s), (24 * s, 41 * s)], fill=body)    # empennage
    d.ellipse([29 * s, 12 * s, 35 * s, 18 * s], fill=(40, 60, 80, 255))                              # verrière
    d.rectangle([0, 0, 64 * s - 1, 48 * s - 1], outline=GREEN + (255,), width=2 * s)
    return shrink(im, 64, 48)


# ---------------------------------------------------------------------------
# Rubénie : Opération Taupe (bouche de tunnel, 48x48, 8 images) et icônes
# des pouvoirs du QG (64x48, liseré vert)
# ---------------------------------------------------------------------------
def taupe_frame(t):
    """Galerie creusée dans la terre : monticule, trou sombre, mottes qui retombent."""
    w = h = 48
    im, d = canvas(w, h)
    c = w * SS / 2
    d.ellipse([c - 22 * SS, c - 12 * SS, c + 22 * SS, c + 16 * SS], fill=(98, 72, 44, 255))       # déblais
    d.ellipse([c - 17 * SS, c - 8 * SS, c + 17 * SS, c + 12 * SS], fill=(126, 94, 58, 255))       # monticule
    d.ellipse([c - 12 * SS, c - 5 * SS, c + 12 * SS, c + 9 * SS], fill=(22, 16, 12, 255))         # galerie
    for side in (-1, 1):                                                                        # boisage
        d.rectangle([c + side * 13 * SS - 2 * SS, c - 7 * SS, c + side * 13 * SS + 2 * SS, c + 8 * SS], fill=(150, 116, 70, 255))
    d.rectangle([c - 15 * SS, c - 9 * SS, c + 15 * SS, c - 6 * SS], fill=(150, 116, 70, 255))
    for k in range(5):                                                                          # mottes
        phase = (t + k * 1.6) % 8 / 8
        a = k * 2 * math.pi / 5
        x = c + math.cos(a) * (8 + 10 * phase) * SS
        y = c + 2 * SS + math.sin(a) * (5 + 6 * phase) * SS - math.sin(phase * math.pi) * 8 * SS
        r = 2 * SS
        d.ellipse([x - r, y - r, x + r, y + r], fill=(88, 64, 40, 255))
    return shrink(im, w, h)


def icon_rub(bg=(70, 90, 70)):
    im, d = canvas(64, 48, bg + (255,))
    return im, d


def icon_rub_done(im, d):
    d.rectangle([0, 0, 64 * SS - 1, 48 * SS - 1], outline=GREEN + (255,), width=2 * SS)
    return shrink(im, 64, 48)


def icon_taupe():
    im, d = icon_rub((110, 150, 90))
    s = SS
    d.rectangle([0, 30 * s, 64 * s, 48 * s], fill=(110, 80, 48, 255))                         # sol
    d.line([(10 * s, 38 * s), (26 * s, 42 * s), (40 * s, 36 * s), (54 * s, 40 * s)], fill=(30, 20, 14, 255), width=4 * s)
    for x in (10, 54):                                                                        # bouches
        d.ellipse([(x - 7) * s, 26 * s, (x + 7) * s, 34 * s], fill=(126, 94, 58, 255))
        d.ellipse([(x - 4) * s, 28 * s, (x + 4) * s, 33 * s], fill=(22, 16, 12, 255))
    d.polygon([(26 * s, 12 * s), (38 * s, 12 * s), (38 * s, 8 * s), (46 * s, 15 * s), (38 * s, 22 * s),
               (38 * s, 18 * s), (26 * s, 18 * s)], fill=WHITE + (255,))                        # flèche
    return icon_rub_done(im, d)


def icon_eclair():
    im, d = icon_rub((50, 60, 50))
    s = SS
    d.polygon([(36 * s, 4 * s), (18 * s, 27 * s), (30 * s, 27 * s), (24 * s, 44 * s), (46 * s, 18 * s),
               (33 * s, 18 * s), (40 * s, 4 * s)], fill=GOLD + (255,))
    for y in (14, 24, 34):                                                                    # traînées de vitesse
        d.line([(4 * s, y * s), (14 * s, y * s)], fill=WHITE + (255,), width=2 * s)
    return icon_rub_done(im, d)


def icon_mobilisation():
    im, d = icon_rub((60, 70, 80))
    s = SS
    d.polygon([(32 * s, 5 * s), (52 * s, 12 * s), (50 * s, 30 * s), (32 * s, 44 * s), (14 * s, 30 * s),
               (12 * s, 12 * s)], fill=GREEN + (255,), outline=WHITE + (255,))                  # bouclier
    montagne(d, 32 * s, 32 * s, 14 * s, body=WHITE, edge=DARK_GREEN, snow=(180, 200, 210))
    return icon_rub_done(im, d)


def icon_superiorite():
    im, d = icon_rub((110, 140, 190))
    s = SS
    body = (70, 80, 90, 255)
    for cx, cy in ((20, 16), (44, 16), (20, 34), (44, 34)):                                   # 4 chasseurs
        d.polygon([(cx * s, (cy - 7) * s), ((cx + 8) * s, (cy + 4) * s), ((cx + 2) * s, (cy + 3) * s),
                   ((cx + 3) * s, (cy + 7) * s), ((cx - 3) * s, (cy + 7) * s), ((cx - 2) * s, (cy + 3) * s),
                   ((cx - 8) * s, (cy + 4) * s)], fill=body)
    d.ellipse([28 * s, 21 * s, 36 * s, 29 * s], outline=WHITE + (255,), width=s)             # zone
    return icon_rub_done(im, d)


def icon_precision():
    im, d = icon_rub((50, 50, 50))
    s = SS
    d.ellipse([16 * s, 8 * s, 48 * s, 40 * s], outline=(214, 24, 30, 255), width=2 * s)       # réticule
    d.ellipse([27 * s, 19 * s, 37 * s, 29 * s], outline=(214, 24, 30, 255), width=2 * s)
    for a, b in (((32, 2), (32, 14)), ((32, 34), (32, 46)), ((10, 24), (22, 24)), ((42, 24), (54, 24))):
        d.line([(a[0] * s, a[1] * s), (b[0] * s, b[1] * s)], fill=(214, 24, 30, 255), width=2 * s)
    d.polygon([(44 * s, 4 * s), (48 * s, 6 * s), (36 * s, 20 * s), (34 * s, 18 * s)], fill=WHITE + (255,))  # missile
    return icon_rub_done(im, d)


if __name__ == "__main__":
    save(to_indexed([gate_frame(t) for t in range(8)], 48, 48), "aguilagate", 48, 48, 8)
    save(to_indexed([taupe_frame(t) for t in range(8)], 48, 48), "taupegate", 48, 48, 8)
    for name, fn in (("palacioicon", icon_palacio), ("ojoicon", icon_ojo), ("gateicon", icon_gate),
                     ("bridgeicon", icon_bridge), ("ordenicon", icon_orden), ("strikeicon", icon_strike),
                     ("pelicanoicon", icon_pelicano), ("antenaicon", icon_antena), ("nuclearicon", icon_nuclear),
                     ("qgicon", icon_qg), ("avionrecoicon", icon_avion_reco),
                     ("taupeicon", icon_taupe), ("eclairicon", icon_eclair),
                     ("mobilisationicon", icon_mobilisation), ("superioriteicon", icon_superiorite),
                     ("precisionicon", icon_precision)):
        save(to_indexed([fn()], 64, 48), name, 64, 48, 1)
