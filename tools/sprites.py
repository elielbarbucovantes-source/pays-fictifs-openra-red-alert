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


# ---------------------------------------------------------------------------
# Australouis : Cormoran, hydravion bombardier d'eau (48x48, 32 orientations)
# ---------------------------------------------------------------------------
JAUNE, ROUGE_H, GRIS_H, SOMBRE_H = (236, 196, 40), (196, 30, 30), (150, 150, 150), (60, 60, 60)


def cormoran_top(w=48):
    """Vu de dessus, nez vers le haut."""
    s = SS
    im, d = canvas(w, w)
    c = w / 2
    def r(x0, y0, x1, y1, fill):
        d.rectangle([(c + x0) * s, (c + y0) * s, (c + x1) * s, (c + y1) * s], fill=fill + (255,))
    d.ellipse([(c - 3) * s, (c - 19) * s, (c + 3) * s, (c - 12) * s], fill=JAUNE + (255,))   # nez
    r(-3, -16, 3, 14, JAUNE)                                                                 # coque
    r(-3, -2, 3, 2, ROUGE_H)                                                                 # bande rouge
    d.polygon([((c - 3) * s, (c + 14) * s), ((c + 3) * s, (c + 14) * s), ((c + 1) * s, (c + 20) * s),
               ((c - 1) * s, (c + 20) * s)], fill=JAUNE + (255,))                              # queue
    r(-8, 16, 8, 19, JAUNE)                                                                  # empennage
    r(-8, 16, -6, 19, ROUGE_H); r(6, 16, 8, 19, ROUGE_H)
    r(-22, -7, 22, -2, JAUNE)                                                                # aile haute
    r(-22, -7, -19, -2, ROUGE_H); r(19, -7, 22, -2, ROUGE_H)                                 # saumons rouges
    for x in (-11, 11):                                                                      # moteurs + hélices
        r(x - 2, -11, x + 2, 1, GRIS_H)
        r(x - 5, -12, x + 5, -11, SOMBRE_H)
    for x in (-19, 19):                                                                      # flotteurs
        r(x - 1, -5, x + 1, 1, GRIS_H)
    d.rectangle([(c - 2) * s, (c - 15) * s, (c + 2) * s, (c - 13) * s], fill=(70, 110, 150, 255))  # verrière
    return im


def cormoran_frames(w=48):
    top = cormoran_top(w)
    return [shrink(top.rotate(f * 360 / 32, resample=Image.BICUBIC), w, w) for f in range(32)]


def icon_cormoran():
    im, d = canvas(64, 48, (40, 90, 150, 255))
    s = SS
    d.rectangle([0, 34 * s, 64 * s, 48 * s], fill=(28, 70, 130, 255))                       # mer
    for i in range(14):                                                                      # brume pulvérisée
        x, y = 8 + (i * 7) % 40, 22 + (i * 5) % 12
        d.ellipse([x * s, y * s, (x + 9) * s, (y + 6) * s], fill=(225, 230, 235, 255))
    top = cormoran_top(48).resize((40 * s, 40 * s), Image.LANCZOS).rotate(90 + 10, resample=Image.BICUBIC)
    im.alpha_composite(top, (22 * s, 0))
    return shrink(im, 64, 48)


# ---------------------------------------------------------------------------
# Australouis : Mothership (96x96, 32 orientations), île 8x8 (192x192),
# émergence de l'île (4 images) et icônes.
# ---------------------------------------------------------------------------
COQUE, COQUE_SOMBRE, PONT, VERRE = (120, 128, 136), (70, 76, 84), (150, 156, 160), (90, 150, 190)
SABLE, SABLE_SOMBRE, HERBE, HERBE_SOMBRE = (216, 196, 140), (180, 160, 110), (80, 140, 60), (50, 105, 45)


def mothership_top(w=96):
    """Vu de dessus, proue vers le haut : immense coque, dôme vert (la « graine » d'île), grues."""
    s = SS
    im, d = canvas(w, w)
    c = w / 2
    def P(x, y):
        return ((c + x) * s, (c + y) * s)
    hull = [P(0, -44), P(9, -34), P(12, -18), P(12, 36), P(9, 42), P(-9, 42), P(-12, 36), P(-12, -18), P(-9, -34)]
    d.polygon(hull, fill=COQUE_SOMBRE + (255,))
    inner = [P(0, -40), P(7, -32), P(10, -17), P(10, 35), P(8, 40), P(-8, 40), P(-10, 35), P(-10, -17), P(-7, -32)]
    d.polygon(inner, fill=COQUE + (255,))
    d.rectangle([*P(-8, -14), *P(8, 22)], fill=PONT + (255,))                         # pont central
    d.ellipse([*P(-7, -8), *P(7, 6)], fill=HERBE_SOMBRE + (255,))                    # dôme de la graine d'île
    d.ellipse([*P(-5, -6), *P(5, 4)], fill=HERBE + (255,))
    d.rectangle([*P(-6, 26), *P(6, 36)], fill=COQUE_SOMBRE + (255,))                 # château arrière
    d.rectangle([*P(-4, 28), *P(4, 31)], fill=VERRE + (255,))
    for y in (-22, 14):                                                              # grues
        d.rectangle([*P(-11, y), *P(11, y + 2)], fill=(200, 170, 40, 255))
    d.rectangle([*P(-2, -30), *P(2, -20)], fill=COQUE_SOMBRE + (255,))               # petite tourelle
    return im


def mothership_frames(w=96):
    top = mothership_top(w)
    return [shrink(top.rotate(f * 360 / 32, resample=Image.BICUBIC), w, w) for f in range(32)]


def ile_frame(size=192, seed=7):
    """Île carrée aux bords irréguliers : plage tout autour, herbe au centre."""
    import random
    rnd = random.Random(seed)
    s = SS
    im, d = canvas(size, size)
    n = size * s
    def blob(margin, fill, jitter):
        pts = []
        steps = 48
        for k in range(steps):
            a = 2 * math.pi * k / steps
            # carré arrondi (superellipse), bord irrégulier
            ca, sa = math.cos(a), math.sin(a)
            r = 1 / max(abs(ca), abs(sa)) ** 0.85 if max(abs(ca), abs(sa)) > 0 else 1
            rad = (n / 2 - margin * s) * min(r, 1.25) - rnd.uniform(0, jitter) * s
            pts.append((n / 2 + rad * ca, n / 2 + rad * sa))
        d.polygon(pts, fill=fill + (255,))
    blob(2, SABLE_SOMBRE, 5)
    blob(5, SABLE, 5)
    blob(22, HERBE_SOMBRE, 6)
    blob(26, HERBE, 6)
    for _ in range(90):                                                              # touffes d'herbe
        x, y = rnd.uniform(0.25, 0.75) * n, rnd.uniform(0.25, 0.75) * n
        d.ellipse([x, y, x + 3 * s, y + 2 * s], fill=HERBE_SOMBRE + (255,))
    for _ in range(40):                                                              # grains de sable
        x, y = rnd.uniform(0.05, 0.95) * n, rnd.uniform(0.05, 0.95) * n
        if abs(x - n / 2) > 0.36 * n or abs(y - n / 2) > 0.36 * n:
            d.ellipse([x, y, x + 2 * s, y + 2 * s], fill=SABLE_SOMBRE + (255,))
    return shrink(im, size, size)


def ile_emergence_frame(t, size=192):
    """Remous, écume et hauts-fonds là où l'île va sortir de l'eau."""
    import random
    rnd = random.Random(100 + t)
    s = SS
    im, d = canvas(size, size)
    n = size * s
    d.ellipse([0.18 * n, 0.18 * n, 0.82 * n, 0.82 * n], fill=(60, 110, 120, 255))    # haut-fond
    for k in range(3):                                                               # cercles d'écume
        r = (0.18 + 0.1 * ((k + t / 4) % 3)) * n
        d.ellipse([n / 2 - r, n / 2 - r, n / 2 + r, n / 2 + r], outline=(225, 235, 240, 255), width=2 * s)
    for _ in range(60):
        a, r = rnd.uniform(0, 2 * math.pi), rnd.uniform(0.05, 0.45) * n
        x, y = n / 2 + r * math.cos(a), n / 2 + r * math.sin(a)
        d.ellipse([x, y, x + 3 * s, y + 3 * s], fill=(235, 240, 245, 255))
    return shrink(im, size, size)


def icon_aus(bg=(18, 86, 160)):
    return canvas(64, 48, bg + (255,))


def icon_aus_done(im):
    return shrink(im, 64, 48)


def icon_mothership():
    im, d = icon_aus()
    s = SS
    d.rectangle([0, 34 * s, 64 * s, 48 * s], fill=(12, 60, 120, 255))
    top = mothership_top(96).rotate(90, resample=Image.BICUBIC).resize((64 * s, 64 * s), Image.LANCZOS)
    im.alpha_composite(top, (0, -8 * s))
    return icon_aus_done(im)


def icon_revolutionnaire():
    im, d = icon_aus((150, 30, 30))
    s = SS
    d.polygon([(30 * s, 4 * s), (58 * s, 10 * s), (30 * s, 18 * s)], fill=(240, 240, 236, 255))   # drapeau
    d.rectangle([28 * s, 4 * s, 30 * s, 44 * s], fill=(60, 40, 20, 255))                           # hampe
    d.ellipse([10 * s, 14 * s, 24 * s, 26 * s], fill=(230, 190, 150, 255))                          # tête
    d.polygon([(8 * s, 44 * s), (10 * s, 28 * s), (24 * s, 28 * s), (26 * s, 44 * s)], fill=(40, 40, 40, 255))
    d.rectangle([22 * s, 20 * s, 28 * s, 26 * s], fill=(230, 190, 150, 255))                        # poing levé
    etoile_pts = []
    for k in range(10):
        a = -math.pi / 2 + k * math.pi / 5
        r = 5 if k % 2 == 0 else 2
        etoile_pts.append(((44 + r * math.cos(a)) * s, (30 + r * math.sin(a)) * s))
    d.polygon(etoile_pts, fill=(236, 196, 40, 255))
    return icon_aus_done(im)


def icon_chantier_avance():
    im, d = icon_aus()
    s = SS
    d.rectangle([0, 30 * s, 64 * s, 48 * s], fill=(12, 60, 120, 255))
    d.rectangle([6 * s, 18 * s, 58 * s, 34 * s], fill=(110, 116, 122, 255))                       # quai
    d.rectangle([10 * s, 6 * s, 14 * s, 20 * s], fill=(200, 170, 40, 255))                        # grue
    d.rectangle([10 * s, 6 * s, 34 * s, 9 * s], fill=(200, 170, 40, 255))
    d.polygon([(20 * s, 38 * s), (52 * s, 38 * s), (48 * s, 44 * s), (24 * s, 44 * s)], fill=(70, 76, 84, 255))  # coque
    d.ellipse([46 * s, 4 * s, 58 * s, 16 * s], fill=(236, 196, 40, 255))                          # étoile de rang
    return icon_aus_done(im)


def icon_raffinerie_boost():
    im, d = icon_aus((40, 60, 40))
    s = SS
    for i, x in enumerate((8, 20, 32)):                                                           # pépites de minerai
        d.ellipse([x * s, (26 + 4 * (i % 2)) * s, (x + 12) * s, (38 + 4 * (i % 2)) * s], fill=(222, 176, 52, 255))
    d.polygon([(48 * s, 4 * s), (60 * s, 20 * s), (52 * s, 20 * s), (52 * s, 40 * s), (44 * s, 40 * s),
               (44 * s, 20 * s), (36 * s, 20 * s)], fill=(120, 220, 90, 255))                     # flèche de hausse
    return icon_aus_done(im)


def icon_radar_blink():
    im, d = icon_aus((10, 30, 50))
    s = SS
    for r in (20, 14, 8):                                                                         # sonar
        d.ellipse([(32 - r) * s, (24 - r) * s, (32 + r) * s, (24 + r) * s], outline=(80, 220, 120, 255), width=s)
    d.pieslice([12 * s, 4 * s, 52 * s, 44 * s], 300, 340, fill=(80, 220, 120, 255))
    for x, y in ((18, 14), (44, 30), (24, 34)):                                                   # sous-marins repérés
        d.ellipse([x * s, y * s, (x + 8) * s, (y + 3) * s], fill=(240, 240, 236, 255))
    return icon_aus_done(im)



# ---------------------------------------------------------------------------
# Système pétrole : pipeline (segments comme des murs) et Oil Refinery
# ---------------------------------------------------------------------------
PIPE = (96, 100, 104)
PIPE_DARK = (54, 56, 60)
PIPE_LIGHT = (156, 160, 164)
RUST = (120, 72, 40)


def pipeline_frame(mask, damaged=False):
    """Segment 24x24. mask : 1 = haut, 2 = droite, 4 = bas, 8 = gauche (WithWallSpriteBody)."""
    im, d = canvas(24, 24)
    s = SS
    c0, c1 = 9 * s, 15 * s                         # le tube fait 6 px de large
    body = RUST if damaged else PIPE
    arms = {1: (c0, 0, c1, c1), 2: (c0, c0, 24 * s, c1), 4: (c0, c0, c1, 24 * s), 8: (0, c0, c1, c1)}
    if mask == 0:                                  # segment isolé : tronçon horizontal
        mask = 2 | 8
    for bit, (x0, y0, x1, y1) in arms.items():
        if mask & bit:
            d.rectangle([x0, y0 + s, x1, y1 + s], fill=(20, 20, 20, 110))     # ombre
    for bit, (x0, y0, x1, y1) in arms.items():
        if mask & bit:
            d.rectangle([x0, y0, x1, y1], fill=body + (255,))
            if bit in (2, 8):
                d.rectangle([x0, c0, x1, c0 + s], fill=PIPE_LIGHT + (255,))
                d.rectangle([x0, c1 - s, x1, c1], fill=PIPE_DARK + (255,))
            else:
                d.rectangle([c0, y0, c0 + s, y1], fill=PIPE_LIGHT + (255,))
                d.rectangle([c1 - s, y0, c1, y1], fill=PIPE_DARK + (255,))
    # bride de raccord au centre
    d.rectangle([8 * s, 8 * s, 16 * s, 16 * s], fill=PIPE_DARK + (255,))
    d.rectangle([9 * s, 9 * s, 15 * s, 15 * s], fill=(body if not damaged else (90, 60, 40)) + (255,))
    d.ellipse([11 * s, 11 * s, 13 * s, 13 * s], fill=(30, 30, 30, 255))
    if damaged:
        d.line([(10 * s, 10 * s), (14 * s, 15 * s)], fill=(20, 20, 20, 255), width=s)
        d.ellipse([13 * s, 14 * s, 18 * s, 18 * s], fill=(12, 12, 12, 200))   # fuite de pétrole
    return shrink(im, 24, 24)


def raffinerie_frame(t, damaged=False, build=None):
    """Oil Refinery 2x2 (48x48) : deux cuves, colonne de distillation, torchère animée."""
    w = h = 48
    im, d = canvas(w, h)
    s = SS
    d.rectangle([1 * s, 20 * s, 47 * s, 47 * s], fill=(92, 88, 80, 255))             # dalle béton
    d.rectangle([1 * s, 20 * s, 47 * s, 21 * s], fill=(130, 126, 116, 255))
    tank = (170, 164, 150) if not damaged else (120, 110, 96)
    for x0 in (3, 17):                                                              # cuves
        d.rectangle([x0 * s, 26 * s, (x0 + 12) * s, 42 * s], fill=tank + (255,))
        d.ellipse([x0 * s, 22 * s, (x0 + 12) * s, 30 * s], fill=(200, 194, 180, 255) if not damaged else (130, 120, 104, 255))
        d.ellipse([x0 * s, 38 * s, (x0 + 12) * s, 46 * s], fill=tank + (255,))
        d.rectangle([(x0 + 10) * s, 28 * s, (x0 + 12) * s, 43 * s], fill=(110, 106, 96, 255))
        d.rectangle([x0 * s, 33 * s, (x0 + 12) * s, 35 * s], fill=(150, 30, 30, 255))  # bande rouge
    d.rectangle([33 * s, 6 * s, 39 * s, 44 * s], fill=(120, 124, 130, 255))            # colonne
    d.rectangle([33 * s, 6 * s, 34 * s, 44 * s], fill=(170, 174, 180, 255))
    for y in range(10, 44, 6):
        d.rectangle([32 * s, y * s, 40 * s, (y + 1) * s], fill=(70, 72, 76, 255))
    d.rectangle([42 * s, 4 * s, 44 * s, 40 * s], fill=(80, 82, 86, 255))              # torchère
    d.line([(29 * s, 30 * s), (33 * s, 30 * s)], fill=PIPE + (255,), width=2 * s)      # tuyauterie
    d.line([(39 * s, 36 * s), (46 * s, 36 * s)], fill=PIPE + (255,), width=2 * s)
    if not damaged or t % 2 == 0:
        fl = [(255, 220, 90), (255, 170, 40), (240, 110, 30), (255, 190, 60)][t % 4]
        hgt = [7, 9, 6, 8][t % 4]
        d.polygon([(41 * s, 4 * s), (45 * s, 4 * s), (43 * s + (t % 2) * s, (4 - hgt) * s + 2 * s)], fill=fl + (255,))
    if damaged:
        for x, y in ((8, 30), (22, 38), (36, 20)):
            d.ellipse([x * s, y * s, (x + 5) * s, (y + 4) * s], fill=(24, 22, 20, 255))
    im = shrink(im, w, h)
    if build is not None:                                                          # animation de construction
        cut = int(h * (1 - build))
        mask = Image.new("L", (w, h), 0)
        mask.paste(255, (0, cut, w, h))
        empty = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        im = Image.composite(im, empty, mask)
    return im


def icon_raffinerie_petrole():
    im, d = canvas(64, 48, (70, 60, 40, 255))
    s = SS
    d.rectangle([0, 0, 64 * s - 1, 48 * s - 1], outline=(20, 20, 20, 255), width=2 * s)
    ref = raffinerie_frame(1).resize((48 * s, 48 * s), Image.LANCZOS)
    im.alpha_composite(ref, (8 * s, 0))
    d.ellipse([3 * s, 30 * s, 13 * s, 44 * s], fill=(20, 18, 16, 255))                # goutte de pétrole
    d.polygon([(4 * s, 34 * s), (8 * s, 24 * s), (12 * s, 34 * s)], fill=(20, 18, 16, 255))
    return shrink(im, 64, 48)


def icon_pipeline():
    im, d = canvas(64, 48, (70, 60, 40, 255))
    s = SS
    d.rectangle([0, 0, 64 * s - 1, 48 * s - 1], outline=(20, 20, 20, 255), width=2 * s)
    for mask, x, y in ((2 | 8, 4, 12), (2 | 8, 20, 12), (8 | 4, 36, 12), (1 | 4, 36, 28)):
        seg = pipeline_frame(mask).resize((16 * s, 16 * s), Image.LANCZOS)
        im.alpha_composite(seg, (x * s, y * s))
    d.ellipse([46 * s, 30 * s, 56 * s, 44 * s], fill=(20, 18, 16, 255))
    d.polygon([(47 * s, 34 * s), (51 * s, 24 * s), (55 * s, 34 * s)], fill=(20, 18, 16, 255))
    return shrink(im, 64, 48)


# ---------------------------------------------------------------------------
# Porte-Drone (Australouis) : navire plat, six plots de drones sur le pont
# ---------------------------------------------------------------------------
PLOT, PLOT_BORD = (60, 64, 70), (230, 200, 60)


def porte_drone_top(w=64):
    """Vu de dessus, proue vers le haut : coque fine, pont d'envol avec 6 plots, îlot à tribord."""
    s = SS
    im, d = canvas(w, w)
    c = w / 2
    def P(x, y):
        return ((c + x) * s, (c + y) * s)
    hull = [P(0, -29), P(6, -21), P(8, -8), P(8, 26), P(5, 29), P(-5, 29), P(-8, 26), P(-8, -8), P(-6, -21)]
    d.polygon(hull, fill=COQUE_SOMBRE + (255,))
    deck = [P(0, -25), P(5, -19), P(6, -8), P(6, 25), P(-6, 25), P(-6, -8), P(-5, -19)]
    d.polygon(deck, fill=PONT + (255,))
    for k in range(3):                                                               # 6 plots de drones
        y = -14 + k * 11
        for x in (-3, 3):
            d.ellipse([*P(x - 2.5, y - 2.5), *P(x + 2.5, y + 2.5)], fill=PLOT_BORD + (255,))
            d.ellipse([*P(x - 1.7, y - 1.7), *P(x + 1.7, y + 1.7)], fill=PLOT + (255,))
    d.rectangle([*P(6, 14), *P(9, 23)], fill=COQUE_SOMBRE + (255,))                  # îlot
    d.rectangle([*P(6.5, 16), *P(8.5, 18)], fill=VERRE + (255,))
    d.ellipse([*P(-2, 21), *P(2, 25)], fill=(200, 60, 50, 255))                      # antenne de liaison
    return im


def porte_drone_frames(w=64):
    top = porte_drone_top(w)
    return [shrink(top.rotate(f * 360 / 32, resample=Image.BICUBIC), w, w) for f in range(32)]


def icon_porte_drone():
    im, d = icon_aus()
    s = SS
    d.rectangle([0, 34 * s, 64 * s, 48 * s], fill=(12, 60, 120, 255))
    top = porte_drone_top(64).rotate(90, resample=Image.BICUBIC).resize((60 * s, 60 * s), Image.LANCZOS)
    im.alpha_composite(top, (2 * s, -6 * s))
    for x, y in ((10, 6), (20, 3), (30, 7)):                                           # drones en vol
        d.polygon([(x * s, y * s), ((x + 4) * s, (y + 2) * s), (x * s, (y + 4) * s), ((x + 1) * s, (y + 2) * s)],
                  fill=(30, 30, 34, 255))
    return icon_aus_done(im)


# ---------------------------------------------------------------------------
# Zone industrielle (neutre, 3x2 cases) : ateliers à sheds, cheminées qui fument
# ---------------------------------------------------------------------------
def zone_industrielle_frame(t, w=72, h=48):
    import random
    rnd = random.Random(40 + t)
    s = SS
    im, d = canvas(w, h)
    d.rectangle([1 * s, 12 * s, 71 * s, 47 * s], fill=(96, 94, 88, 255))            # dalle
    d.rectangle([1 * s, 12 * s, 71 * s, 13 * s], fill=(136, 132, 124, 255))
    for k in range(4):                                                              # toits en sheds
        x0 = 4 + k * 11
        d.rectangle([x0 * s, 22 * s, (x0 + 10) * s, 44 * s], fill=(150, 110, 80, 255))
        for y in range(22, 44, 4):
            d.polygon([(x0 * s, (y + 4) * s), ((x0 + 10) * s, (y + 4) * s), ((x0 + 10) * s, y * s)],
                      fill=(110, 80, 60, 255))
            d.line([(x0 * s, (y + 4) * s), ((x0 + 10) * s, y * s)], fill=(170, 200, 220, 255), width=s)
    d.rectangle([50 * s, 26 * s, 68 * s, 44 * s], fill=(120, 124, 130, 255))          # entrepôt
    d.rectangle([50 * s, 26 * s, 68 * s, 28 * s], fill=(160, 164, 170, 255))
    d.rectangle([56 * s, 36 * s, 62 * s, 44 * s], fill=(60, 62, 66, 255))              # porte
    d.rectangle([52 * s, 30 * s, 66 * s, 32 * s], fill=(230, 190, 40, 255))           # bande jaune
    for x, top in ((10, 12), (22, 16), (46, 10)):                                         # cheminées
        d.rectangle([x * s, top * s, (x + 4) * s, 24 * s], fill=(140, 70, 50, 255))
        d.rectangle([x * s, (top + 3) * s, (x + 4) * s, (top + 4) * s], fill=(230, 230, 230, 255))
        for k in range(3):                                                         # fumée
            r = 2 + k + (t % 4) * 0.5
            cx = x + 2 + k * 2 + rnd.uniform(-1, 1)
            cy = top - 2 - k * 3 - (t % 4)
            if cy - r > 0:
                g = 150 + k * 20
                d.ellipse([(cx - r) * s, (cy - r) * s, (cx + r) * s, (cy + r) * s], fill=(g, g, g, 200))
    return shrink(im, w, h)


# ---------------------------------------------------------------------------
# Engineered Tsunami : crête de vague (32x32, 4 images) et icône du pouvoir
# ---------------------------------------------------------------------------
def tsunami_crest_frame(shape, t, size=32):
    """Crête d'écume : 4 formes (hauteur, bosses) × 4 images de bouillonnement."""
    import random
    rnd = random.Random(40 + 17 * shape + t)
    im, d = canvas(size, size)
    s = SS
    n = size * s
    top = (0.10, 0.22, 0.16, 0.30)[shape] * n                                           # hauteur de la lèvre
    lean = (0.0, 0.08, -0.06, 0.04)[shape] * n                                           # penchée ou droite
    d.ellipse([0.06 * n, top + 0.22 * n, 0.94 * n, 0.98 * n], fill=(14, 52, 110, 255))     # pied sombre
    d.ellipse([0.12 * n + lean, top + 0.10 * n, 0.88 * n + lean, 0.84 * n], fill=(30, 96, 170, 255))
    d.ellipse([0.22 * n + lean, top + 0.04 * n, 0.78 * n + lean, top + 0.40 * n], fill=(96, 170, 220, 255))
    humps = 2 + shape % 3
    for h in range(humps):                                                              # rouleaux d'écume
        cx = n * (0.2 + 0.6 * (h + 0.5) / humps) + lean + rnd.uniform(-2, 2) * s
        cy = top + 0.14 * n + ((h + t) % 2) * 1.5 * s
        for _ in range(9):
            x = cx + rnd.uniform(-0.13, 0.13) * n
            y = cy + rnd.uniform(-0.07, 0.07) * n
            k = rnd.uniform(1.8, 4.2) * s
            g = rnd.choice(((236, 242, 246), (214, 228, 238), (250, 250, 250)))
            d.ellipse([x - k, y - k, x + k, y + k], fill=g + (255,))
    for _ in range(3 + (t + shape) % 4):                                                # embruns
        x, y = rnd.uniform(0.15, 0.85) * n + lean, top - rnd.uniform(0.0, 0.10) * n
        k = rnd.uniform(0.8, 1.6) * s
        d.ellipse([x - k, y - k, x + k, y + k], fill=(220, 232, 240, 255))
    for _ in range(4):                                                                  # stries dans l'eau
        x, y = rnd.uniform(0.2, 0.8) * n, rnd.uniform(0.55, 0.85) * n
        d.line([x, y, x + rnd.uniform(3, 6) * s, y - 1 * s], fill=(120, 180, 220, 255), width=s)
    return shrink(im, size, size)


def tsunami_foam_frame(shape, t, size=32):
    """Traînée derrière la crête : taches d'écume éparses sur l'eau qui reflue."""
    import random
    rnd = random.Random(900 + 31 * shape + t)
    im, d = canvas(size, size)
    s = SS
    n = size * s
    for _ in range(3 + shape):
        x, y = rnd.uniform(0.15, 0.85) * n, rnd.uniform(0.25, 0.8) * n
        w, h = rnd.uniform(0.10, 0.22) * n, rnd.uniform(0.05, 0.10) * n
        d.ellipse([x - w, y - h, x + w, y + h], fill=(40, 110, 180, 255))
        for _ in range(5):
            fx, fy = x + rnd.uniform(-w, w) * 0.8, y + rnd.uniform(-h, h) * 0.8
            k = rnd.uniform(1.0, 2.6) * s
            d.ellipse([fx - k, fy - k, fx + k, fy + k], fill=(226, 236, 244, 255))
    return shrink(im, size, size)


def icon_tsunami():
    im, d = icon_aus((150, 200, 230))
    s = SS
    d.rectangle([0, 34 * s, 64 * s, 48 * s], fill=(200, 180, 120, 255))                   # plage
    d.rectangle([40 * s, 22 * s, 50 * s, 36 * s], fill=(110, 110, 116, 255))              # usine côtière
    d.rectangle([52 * s, 26 * s, 60 * s, 36 * s], fill=(140, 90, 60, 255))
    d.rectangle([44 * s, 12 * s, 47 * s, 22 * s], fill=(90, 90, 96, 255))
    d.pieslice([-30 * s, 2 * s, 42 * s, 74 * s], 270, 360, fill=(20, 80, 160, 255))      # grande vague
    d.pieslice([-18 * s, 10 * s, 34 * s, 62 * s], 270, 360, fill=(40, 120, 200, 255))
    d.rectangle([0, 38 * s, 36 * s, 48 * s], fill=(20, 80, 160, 255))
    for k in range(8):                                                                    # rouleau d'écume
        x, y = (6 + 4 * k) * s, (2 + abs(4 - k) * 1.5 + 3) * s
        d.ellipse([x - 3 * s, y - 3 * s, x + 3 * s, y + 3 * s], fill=(240, 244, 248, 255))
    return icon_aus_done(im)


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
    save(to_indexed([ile_frame()], 192, 192), "ile", 192, 192, 1)
    save(to_indexed([ile_emergence_frame(t) for t in range(4)], 192, 192), "ileemergence", 192, 192, 4)
    # Cormoran, Mothership, Porte-Drone, raffinerie, pipeline, zone industrielle et leurs icônes :
    # désormais rendus en 3D par tools/sprites_hd.py.
    for name, fn in (("raffinerieboosticon", icon_raffinerie_boost),
                     ("radarblinkicon", icon_radar_blink)):
        save(to_indexed([fn()], 64, 48), name, 64, 48, 1)
    save(to_indexed([icon_pipeline()], 64, 48), "pipelineicon", 64, 48, 1)
    save(to_indexed([tsunami_crest_frame(k, t) for k in range(4) for t in range(4)], 32, 32), "tsunami", 32, 32, 16)
    save(to_indexed([tsunami_foam_frame(k, t) for k in range(4) for t in range(4)], 32, 32), "tsunamiecume", 32, 32, 16)
    save(to_indexed([icon_tsunami()], 64, 48), "tsunamiicon", 64, 48, 1)
