"""Pièces communes aux modèles 3D : chenilles, roues, canons, antennes, matériaux."""
import math
from rendu3d import (Mat, Mesh, team, merge, prism, box, tube, sphere, lathe, rect, rrect, ngon,
                     panels, stripes, noise, combine)

# Matériaux de base -----------------------------------------------------------------
ACIER = Mat((112, 114, 108), spec=0.35, shine=20)
ACIER_SOMBRE = Mat((70, 72, 70), spec=0.3)
CAOUTCHOUC = Mat((46, 44, 42), spec=0.08)
CHENILLE = Mat((58, 54, 50), spec=0.15, tex=stripes(1.4, 0.62, axis=1))
GRILLE = Mat((60, 62, 58), spec=0.1, tex=stripes(1.0, 0.55, axis=0))
VITRE = Mat((70, 110, 140), spec=0.9, shine=40)
FEU_ROUGE = Mat((220, 40, 30), emit=True)
FEU_BLANC = Mat((240, 236, 200), emit=True)
BACHE = Mat((104, 100, 72), spec=0.05, flat=True, tex=noise(3, 0.08))
BOIS = Mat((120, 88, 52), spec=0.05, tex=stripes(0.9, 0.85, axis=0))
JERRICAN = Mat((80, 92, 52), spec=0.2)
PEAU = Mat((200, 150, 110), spec=0.1)


def blinde(step=5.0, seed=1):
    """Tôle équipe avec lignes de panneaux et légère usure."""
    return team(spec=0.3, tex=combine(panels(step, 0.45, 0.8), noise(seed, 0.05)))


def chenilles(length, width, height, x, z=0.0, wheels=5, skirt=None, mat_skirt=None):
    """Train de roulement complet d'un côté (x = centre), symétrisé par l'appelant."""
    m = Mesh()
    y0, y1 = -length / 2, length / 2
    # bande de chenille : profil arrondi aux extrémités
    prof = [(y0 + 1.5, z), (y1 - 1.5, z), (y1, z + height * 0.45), (y1 - 1.0, z + height),
            (y0 + 1.0, z + height), (y0, z + height * 0.45)]
    rings = []
    for dx in (-width / 2, width / 2):
        rings.append([(x + dx, py, pz) for py, pz in prof])
    from rendu3d import loft
    m.add(loft(rings[::-1] if False else rings, CHENILLE))
    # galets visibles sur le flanc extérieur
    side = 1 if x > 0 else -1
    xo = x + side * (width / 2 + 0.05)
    for i in range(wheels):
        wy = y0 + 2.2 + (length - 4.4) * i / max(1, wheels - 1)
        m.add(tube((xo, wy, z + height * 0.45), (xo + side * 0.35, wy, z + height * 0.45), height * 0.36,
                   mat=ACIER_SOMBRE, seg=8))
    # jupe de protection
    if skirt:
        sz0, sz1 = skirt
        m.add(box(min(xo, xo + side * 0.5), y0 + 1.2, z + sz0, max(xo, xo + side * 0.5), y1 - 0.8, z + sz1,
                  mat_skirt or team(), bevel=0.15))
    return m


def canon(y0, y1, z, r, x=0.0, mat=None, frein=True, manchon=None):
    """Tube de canon le long de y, avec frein de bouche et manchon thermique."""
    mat = mat or ACIER
    m = tube((x, y0, z), (x, y1, z), r, r * 0.9, mat=mat, seg=8)
    if manchon:
        a, b = manchon
        m.add(tube((x, y0 + (y1 - y0) * a, z), (x, y0 + (y1 - y0) * b, z), r * 1.35, mat=ACIER_SOMBRE, seg=8))
    if frein:
        m.add(tube((x, y1 - 1.2, z), (x, y1 + 0.2, z), r * 1.7, mat=ACIER_SOMBRE, seg=8))
    return m


def antenne(x, y, z, h, r=0.18):
    return merge(tube((x, y, z), (x, y, z + h), r, r * 0.5, mat=ACIER_SOMBRE, seg=4),
                 sphere((x, y, z + 0.2), 0.45, ACIER_SOMBRE, seg=6, rings=3))


def trappe(x, y, z, r, mat=None):
    """Tourelleau / trappe ronde avec poignée."""
    return merge(tube((x, y, z), (x, y, z + 0.6), r, r * 0.92, mat=mat or team(), seg=10),
                 box(x - r * 0.5, y - 0.15, z + 0.6, x + r * 0.5, y + 0.15, z + 0.85, ACIER_SOMBRE))


def roue(x, y, z, r, w, side):
    return merge(tube((x, y, z), (x + side * w, y, z), r, mat=CAOUTCHOUC, seg=10),
                 tube((x + side * w, y, z), (x + side * (w + 0.1), y, z), r * 0.55, mat=ACIER, seg=8))


# ---------------------------------------------------------------------------
# Aviation
# ---------------------------------------------------------------------------
GRIS_NAVAL = Mat((128, 138, 146), spec=0.4, shine=24, tex=combine(panels(4.0, 0.35, 0.86), noise(4, 0.04)))
GRIS_FONCE = Mat((84, 90, 98), spec=0.35, shine=22, tex=panels(4.0, 0.35, 0.86))
VERRIERE = Mat((60, 96, 128), spec=1.2, shine=50)
TUYERE = Mat((58, 56, 54), spec=0.5, shine=30)
FLAMME = Mat((250, 200, 90), emit=True)


def fuselage(sections, mat, seg=12, mats=None):
    """sections = [(y, demi-largeur, demi-hauteur, z_centre)] : corps à sections elliptiques."""
    import rendu3d as R
    rings = []
    for y, rx, rz, zc in sections:
        rx, rz = max(rx, 0.02), max(rz, 0.02)
        rings.append([(rx * math.cos(2 * math.pi * i / seg), y, zc + rz * math.sin(2 * math.pi * i / seg)) for i in range(seg)])
    return R.loft(rings, mat, True, True, mats)


def verriere(y0, y1, w, h, z, mat=None):
    """Verrière en goutte d'eau posée sur le dos."""
    n = 6
    secs = []
    for i in range(n + 1):
        t = i / n
        k = math.sin(math.pi * min(1.0, t * 1.15)) if t < 0.87 else math.sin(math.pi * 0.87 * 1.15) * (1 - (t - 0.87) / 0.13)
        secs.append((y0 + (y1 - y0) * t, w * max(k, 0.05), h * max(k, 0.05), z))
    m = fuselage(secs, mat or VERRIERE, seg=10)
    # on ne garde que la moitié haute
    return Mesh([t for t in m.tris if min(t[0][2], t[1][2], t[2][2]) >= z - 0.05])


def missile(x, y0, y1, z, r=0.45, mat=None, ailettes=True):
    mat = mat or Mat((200, 200, 196), spec=0.4)
    m = merge(tube((x, y0, z), (x, y1 - r * 2.5, z), r, mat=mat, seg=6),
              tube((x, y1 - r * 2.5, z), (x, y1, z), r, 0.05, mat=mat, seg=6))
    if ailettes:
        for dx, dz in ((r * 1.8, 0), (-r * 1.8, 0), (0, r * 1.8), (0, -r * 1.8)):
            m.add(box(min(x, x + dx) - 0.08, y0, min(z, z + dz) - 0.08, max(x, x + dx) + 0.08, y0 + 1.2, max(z, z + dz) + 0.08, mat))
    return m


def cocarde(cx, cy, r, couleurs):
    """Cocarde circulaire (texture) : couleurs du centre vers l'extérieur."""
    def f(p, n):
        d = math.hypot(p[0] - cx, p[1] - cy)
        if d > r or n[2] < 0.3:
            return None
        k = int(d / r * len(couleurs))
        return couleurs[min(k, len(couleurs) - 1)]
    return f


def avec_marques(base_tex, *marques):
    """Texture de base + cocardes / bandes (renvoient une couleur ou None)."""
    def f(p, n):
        for m in marques:
            c = m(p, n)
            if c is not None:
                return c
        return base_tex(p, n) if base_tex else 1.0
    return f


def sillage(poly, mat=None, largeur=1.0, z=0.1):
    """Écume à la ligne de flottaison : anneau autour d'une coque (polygone x, y)."""
    import rendu3d as R
    mat = mat or Mat((196, 220, 234), spec=0.1, emit=True, tex=noise(9, 0.2, 1.2))
    inner = R.ccw(poly)
    outer = R.inset(inner, -largeur)
    m = Mesh()
    n = len(inner)
    for i in range(n):
        j = (i + 1) % n
        m.quad((inner[i][0], inner[i][1], z), (outer[i][0], outer[i][1], z), (outer[j][0], outer[j][1], z), (inner[j][0], inner[j][1], z), mat)
    return m
