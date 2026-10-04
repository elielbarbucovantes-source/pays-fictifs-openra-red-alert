"""Icônes des pouvoirs de l'Ananthanie (64 × 48, IconImage: ananthanie-powers).
Usage : python3 tools/icones_ananthanie.py"""
import math
from sprites import canvas, shrink, to_indexed, save, SS
from drapeaux import WHITE, CITRON, POMME, SOLEIL

S = SS


def fond(bg):
    return canvas(64, 48, bg + (255,))


def fini(im, d):
    d.rectangle([0, 0, 64 * S - 1, 48 * S - 1], outline=CITRON + (255,), width=2 * S)
    return shrink(im, 64, 48)


def p(x, y):
    return (x * S, y * S)


def drone(d, cx, cy, k=1.0, col=(230, 232, 226)):
    for a in (45, 135):
        r = math.radians(a)
        dx, dy = math.cos(r) * 5 * k, math.sin(r) * 5 * k
        d.line([p(cx - dx, cy - dy), p(cx + dx, cy + dy)], fill=col + (255,), width=max(1, int(1.6 * k * S)))
    d.ellipse([p(cx - 1.4 * k, cy - 1.4 * k), p(cx + 1.4 * k, cy + 1.4 * k)], fill=CITRON + (255,))


def icon_blitz():
    im, d = fond((40, 60, 30))
    # jauge horizontale pleine et chiffre 7 en éclair
    d.rectangle([p(6, 36), p(58, 42)], outline=WHITE + (255,), width=S)
    d.rectangle([p(8, 38), p(56, 40)], fill=SOLEIL + (255,))
    d.polygon([p(18, 6), p(46, 6), p(32, 24), p(38, 24), p(24, 34), p(28, 22), p(22, 22), p(34, 12), p(18, 12)],
              fill=CITRON + (255,), outline=WHITE + (255,))
    return fini(im, d)


def icon_essaim():
    im, d = fond((60, 70, 80))
    for k, (x, y) in enumerate(((14, 12), (28, 9), (44, 13), (20, 24), (36, 22), (50, 26), (12, 36), (28, 36), (44, 38))):
        drone(d, x, y, 0.9 + 0.15 * (k % 2))
    return fini(im, d)


def icon_salve():
    im, d = fond((70, 50, 40))
    for k in range(3):
        x = 14 + k * 14
        d.polygon([p(x, 6 + k * 3), p(x + 3, 10 + k * 3), p(x + 3, 30 + k * 2), p(x - 3, 30 + k * 2), p(x - 3, 10 + k * 3)],
                  fill=(226, 226, 220, 255))
        d.polygon([p(x - 2, 30 + k * 2), p(x + 2, 30 + k * 2), p(x, 40 + k * 2)], fill=SOLEIL + (255,))
        d.rectangle([p(x - 3, 12 + k * 3), p(x + 3, 14 + k * 3)], fill=CITRON + (255,))
    return fini(im, d)


def icon_raid():
    im, d = fond((50, 54, 60))
    for cx, cy in ((32, 12), (16, 24), (48, 24)):
        d.polygon([p(cx, cy - 6), p(cx + 11, cy + 4), p(cx + 6, cy + 3), p(cx, cy + 6), p(cx - 6, cy + 3), p(cx - 11, cy + 4)],
                  fill=(98, 104, 110, 255), outline=(30, 32, 34, 255))
    for x in range(8, 58, 6):
        d.ellipse([p(x, 38), p(x + 3, 41)], fill=SOLEIL + (255,))
    return fini(im, d)


def icon_oeil():
    im, d = fond((30, 50, 70))
    d.ellipse([p(10, 12), p(54, 36)], fill=WHITE + (255,))
    d.ellipse([p(24, 13), p(40, 35)], fill=POMME + (255,))
    d.ellipse([p(28, 18), p(36, 30)], fill=(10, 10, 10, 255))
    drone(d, 52, 8, 0.9)
    return fini(im, d)


def icon_pont():
    im, d = fond((80, 110, 130))
    d.polygon([p(32, 4), p(35, 8), p(35, 12), p(58, 15), p(58, 17), p(35, 17), p(34, 22), p(30, 22), p(29, 17), p(6, 17),
               p(6, 15), p(29, 12), p(29, 8)], fill=(150, 156, 160, 255))
    for x, y in ((18, 30), (32, 34), (46, 30)):
        d.pieslice([p(x - 6, y - 6), p(x + 6, y + 6)], 180, 360, fill=WHITE + (255,))
        d.line([p(x - 6, y), p(x, y + 8)], fill=WHITE + (255,), width=S)
        d.line([p(x + 6, y), p(x, y + 8)], fill=WHITE + (255,), width=S)
        d.rectangle([p(x - 2, y + 7), p(x + 2, y + 10)], fill=CITRON + (255,))
    return fini(im, d)


def icon_fraternite():
    im, d = fond((40, 70, 50))
    # deux mains jointes stylisées : deux bras qui se croisent, rose des vents au-dessus
    d.polygon([p(4, 40), p(14, 40), p(34, 24), p(28, 18)], fill=CITRON + (255,))
    d.polygon([p(60, 40), p(50, 40), p(30, 24), p(36, 18)], fill=(90, 150, 210, 255))
    d.ellipse([p(26, 18), p(38, 30)], fill=SOLEIL + (255,))
    for k in range(4):
        a = math.pi / 2 * k - math.pi / 2
        d.polygon([p(32 + 12 * math.cos(a), 10 + 8 * math.sin(a)), p(32 + 2 * math.cos(a + 1.57), 10 + 2 * math.sin(a + 1.57)),
                   p(32 - 2 * math.cos(a + 1.57), 10 - 2 * math.sin(a + 1.57))], fill=WHITE + (255,))
    return fini(im, d)


if __name__ == "__main__":
    for name, fn in (("blitzicon", icon_blitz), ("essaimicon", icon_essaim), ("salveicon", icon_salve),
                     ("raidicon", icon_raid), ("oeilicon", icon_oeil), ("ponticon", icon_pont),
                     ("fraterniteicon", icon_fraternite)):
        save(to_indexed([fn()], 64, 48), name, 64, 48, 1)
    print("7 icônes")
