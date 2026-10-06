"""Modèles 3D du réseau ferroviaire : voie (16 raccordements), gare, chantier ferroviaire,
locomotive diesel, wagons de transport lourd et léger.

Bâtiments et voie : repère centré sur l'emprise, y vers le nord, une case = 24.
Trains : y vers l'avant ; une voiture tient dans une case (22 de long) pour laisser un jour entre elles."""
import math
import rendu3d as R
from rendu3d import Mesh, Mat, prism, box, tube, sphere, rrect, panels, noise, combine, stripes
from modeles.communs import ACIER, ACIER_SOMBRE, GRILLE, VITRE, FEU_BLANC, FEU_ROUGE, blinde, antenne

BALLAST = Mat((126, 118, 104), spec=0.04, tex=noise(301, 0.20, 0.7))
TRAVERSE = Mat((88, 66, 46), spec=0.04, tex=noise(302, 0.10, 1.0))
RAIL = Mat((168, 168, 164), spec=0.8, shine=34)
BETON = Mat((150, 146, 136), spec=0.1, tex=combine(panels(6.0, 0.35, 0.86), noise(303, 0.07, 1.2)))
BETON_SOMBRE = Mat((108, 104, 96), spec=0.1, tex=combine(panels(6.0, 0.35, 0.86), noise(304, 0.07, 1.2)))
BRIQUE = Mat((156, 88, 62), spec=0.05, tex=combine(panels(2.0, 0.25, 0.88), noise(305, 0.08)))
TOIT = Mat((84, 90, 96), spec=0.25, tex=stripes(1.4, 0.8, axis=0))
TOLE = Mat((128, 132, 126), spec=0.3, tex=stripes(1.2, 0.82, axis=0))
JAUNE = Mat((222, 178, 40), spec=0.3)
NOIR = Mat((30, 30, 30), spec=0.2)
SOMBRE = Mat((22, 22, 24), spec=0.05, flat=True)

ECART = 5.6      # demi-écartement des rails (px)
DEMI_BALLAST = 10.5
DEMI_TRAVERSE = 8.6


def _quad_band(p0, p1, half, z0, z1, mat):
    """Bande droite de largeur 2 × half entre deux points (x, y)."""
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    L = math.hypot(dx, dy) or 1.0
    nx, ny = -dy / L * half, dx / L * half
    poly = [(p0[0] + nx, p0[1] + ny), (p0[0] - nx, p0[1] - ny), (p1[0] - nx, p1[1] - ny), (p1[0] + nx, p1[1] + ny)]
    return prism(poly, z0, z1, mat)


def _offset(path, d):
    """Courbe parallèle à une polyligne (décalage d à gauche)."""
    out = []
    for i, p in enumerate(path):
        a = path[max(0, i - 1)]
        b = path[min(len(path) - 1, i + 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]
        L = math.hypot(dx, dy) or 1.0
        out.append((p[0] - dy / L * d, p[1] + dx / L * d))
    return out


def voie(path, traverses=True, z=0.0):
    """Voie le long d'une polyligne : ballast, traverses tous les 3 px, deux rails."""
    m = Mesh()
    for a, b in zip(path, path[1:]):
        m.add(_quad_band(a, b, DEMI_BALLAST, z, z + 0.7, BALLAST))
    if traverses:
        # traverses à intervalles réguliers le long de la courbe
        acc, pas = 1.5, 3.0
        for a, b in zip(path, path[1:]):
            L = math.hypot(b[0] - a[0], b[1] - a[1])
            while acc < L:
                t = acc / L
                c = (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
                dx, dy = (b[0] - a[0]) / L, (b[1] - a[1]) / L
                m.add(_quad_band((c[0] - dy * DEMI_TRAVERSE, c[1] + dx * DEMI_TRAVERSE), (c[0] + dy * DEMI_TRAVERSE, c[1] - dx * DEMI_TRAVERSE), 0.9, z + 0.7, z + 1.3, TRAVERSE))
                acc += pas
            acc -= L
    for s in (1, -1):
        rail = _offset(path, s * ECART)
        for a, b in zip(rail, rail[1:]):
            m.add(_quad_band(a, b, 0.7, z + 1.3, z + 2.5, RAIL))
    return m


def _arc(a, b, n=10):
    """Quart de cercle du milieu du bord a au milieu du bord b (centre : coin commun)."""
    cx, cy = a[0] + b[0], a[1] + b[1]
    t0 = math.atan2(a[1] - cy, a[0] - cx)
    t1 = math.atan2(b[1] - cy, b[0] - cx)
    if t1 - t0 > math.pi:
        t1 -= 2 * math.pi
    elif t0 - t1 > math.pi:
        t1 += 2 * math.pi
    return [(cx + 12 * math.cos(t0 + (t1 - t0) * k / n), cy + 12 * math.sin(t0 + (t1 - t0) * k / n)) for k in range(n + 1)]


BORDS = {1: (0, 12), 2: (12, 0), 4: (0, -12), 8: (-12, 0)}


def rail(mask, degats=False):
    """Segment de voie (1 case) : masque 1 = nord, 2 = est, 4 = sud, 8 = ouest (comme les murs).
    Deux côtés à angle droit : courbe ; trois : voie droite et aiguillages ; un seul : heurtoir."""
    m = Mesh()
    bras = [b for b in (1, 2, 4, 8) if mask & b]
    if not bras:
        m.add(voie([(-12, 0), (12, 0)]))
    elif len(bras) == 1:
        e = BORDS[bras[0]]
        m.add(voie([e, (-e[0] * 0.25, -e[1] * 0.25)]))
        # heurtoir : butoir rouge et blanc
        ux, uy = -e[0] / 12, -e[1] / 12
        c = (ux * 3.0, uy * 3.0)
        m.add(_quad_band((c[0] - uy * 8, c[1] + ux * 8), (c[0] + uy * 8, c[1] - ux * 8), 1.2, 0.7, 5.0, Mat((200, 40, 36), spec=0.3)))
        m.add(_quad_band((c[0] - uy * 8, c[1] + ux * 8), (c[0] + uy * 8, c[1] - ux * 8), 1.25, 3.0, 3.8, Mat((236, 236, 230), spec=0.3)))
    elif len(bras) == 2 and mask in (5, 10):
        a, b = BORDS[bras[0]], BORDS[bras[1]]
        m.add(voie([a, b]))
    elif len(bras) == 2:
        m.add(voie(_arc(BORDS[bras[0]], BORDS[bras[1]])))
    elif len(bras) == 3:
        manquant = 15 - mask
        oppose = {1: 4, 4: 1, 2: 8, 8: 2}[manquant]
        droits = [b for b in bras if b != oppose]
        m.add(voie([BORDS[droits[0]], BORDS[droits[1]]]))
        for d in droits:
            m.add(voie(_arc(BORDS[d], BORDS[oppose]), traverses=False, z=0.05))
    else:
        m.add(voie([(-12, 0), (12, 0)]))
        m.add(voie([(0, -12), (0, 12)], traverses=False, z=0.05))
    if degats:
        m = R.endommage(m, 11)
        for (x, y) in ((3, -2), (-4, 3)):
            m.add(box(x - 1.6, y - 0.6, 0.7, x + 1.6, y + 0.6, 1.3, TRAVERSE).rot_z(35, x, y))
        m.add(prism(R.ngon(1.5, 1.0, 3.2, 8, sy=0.7), 0.7, 0.75, Mat((40, 36, 32), spec=0.02)))
    return m


def rail_icone():
    """Icône : une voie qui tourne."""
    m = Mesh()
    m.add(voie([(-36, 0), (-12, 0)]))
    m.add(voie(_arc((-12, 0), (0, 12))))
    m.add(voie([(0, 12), (0, 36)]))
    return m.move(dx=15, dy=-15)


# ---------------------------------------------------------------------------
# Gare (4×3) : bâtiment voyageurs au nord, voie au milieu, quai couvert au sud
# ---------------------------------------------------------------------------
def gare(t=0.0):
    peau = R.team(spec=0.25, tex=combine(panels(4.0, 0.35, 0.84), noise(306, 0.05)))
    m = Mesh()
    # quai (sud) et dalle du bâtiment (nord)
    m.add(prism(rrect(-48, -36, 48, -12.5, 1.0), 0.0, 1.8, BETON))
    m.add(box(-48, -13.4, 1.8, 48, -12.5, 1.9, JAUNE))
    m.add(prism(rrect(-48, 12.5, 48, 36, 1.0), 0.0, 1.2, BETON_SOMBRE))
    m.add(voie([(-48, 0), (48, 0)]))
    # bâtiment voyageurs : corps en brique, toit à deux pans, avant-corps à horloge
    m.add(prism(rrect(-42, 16, 42, 34, 0.8), 1.2, 14.0, BRIQUE, bevel=0.3))
    m.add(R.loft([[(-43, 15, 14.0), (43, 15, 14.0)], [(-43, 25, 19.0), (43, 25, 19.0)]], TOIT, cap0=False, cap1=False))
    m.add(R.loft([[(-43, 25, 19.0), (43, 25, 19.0)], [(-43, 35, 14.0), (43, 35, 14.0)]], TOIT, cap0=False, cap1=False))
    m.add(prism(rrect(-9, 13, 9, 30, 0.6), 1.2, 22.0, BRIQUE, bevel=0.3))
    m.add(R.loft([[(-10, 12, 22.0), (10, 12, 22.0)], [(-10, 21, 27.0), (10, 21, 27.0)]], peau, cap0=False, cap1=False))
    m.add(tube((0, 12.9, 17.5), (0, 12.6, 17.5), 3.2, mat=Mat((236, 232, 214), spec=0.3), seg=14))      # horloge
    m.add(box(-0.3, 12.4, 17.3, 0.3, 12.5, 19.8, NOIR))
    m.add(box(-0.3, 12.4, 17.2, 2.2, 12.5, 17.8, NOIR))
    for k in range(-3, 4):                                                     # fenêtres et portes
        if k == 0:
            continue
        x = k * 11
        m.add(box(x - 2.6, 15.9, 4.0, x + 2.6, 16.0, 10.5, VITRE))
    m.add(box(-4, 12.8, 1.2, 4, 12.9, 9.0, Mat((70, 50, 36), spec=0.1)))
    m.add(box(-42, 15.6, 13.0, 42, 16.4, 14.0, peau))                          # bandeau aux couleurs
    # marquise sur le quai : poteaux et toiture
    for x in range(-42, 43, 14):
        m.add(tube((x, -24, 1.8), (x, -24, 11.0), 0.6, mat=ACIER_SOMBRE, seg=6))
    m.add(R.loft([[(-46, -34, 11.0), (46, -34, 11.0)], [(-46, -24, 13.0), (46, -24, 13.0)]], peau, cap0=False, cap1=False))
    m.add(R.loft([[(-46, -24, 13.0), (46, -24, 13.0)], [(-46, -13, 11.5), (46, -13, 11.5)]], peau, cap0=False, cap1=False))
    for x in (-30, 0, 30):                                                     # bancs et panneaux
        m.add(box(x - 3, -30, 1.8, x + 3, -29, 3.2, Mat((90, 70, 46), spec=0.05)))
        m.add(box(x - 3.5, -18.4, 7.0, x + 3.5, -18.0, 9.0, Mat((30, 60, 120), spec=0.3)))
    # signal lumineux au bout du quai, lampadaires
    m.add(tube((44, -10, 0.7), (44, -10, 9.0), 0.4, mat=ACIER_SOMBRE, seg=5))
    m.add(box(43.2, -10.6, 7.0, 44.8, -9.4, 10.0, NOIR))
    m.add(sphere((44, -10.7, 9.2), 0.5, FEU_ROUGE, seg=5, rings=3))
    m.add(sphere((44, -10.7, 7.8), 0.5, Mat((60, 230, 80), emit=True), seg=5, rings=3))
    return m


# ---------------------------------------------------------------------------
# Chantier ferroviaire (4×3) : grande halle au nord, voie de sortie au sud, portique
# ---------------------------------------------------------------------------
def chantier_ferroviaire(t=0.0):
    peau = R.team(spec=0.25, tex=combine(panels(4.0, 0.35, 0.84), noise(307, 0.05)))
    m = Mesh()
    m.add(prism(rrect(-48, -36, 48, 36, 1.5), 0.0, 0.8, BETON_SOMBRE))
    m.add(voie([(-48, -24), (48, -24)], z=0.1))
    # halle : murs en tôle, toit en sheds, portes ouvertes sur des voies intérieures
    m.add(prism(rrect(-46, -10, 46, 34, 0.8), 0.8, 18.0, TOLE, bevel=0.3))
    for k in range(6):
        x0 = -46 + k * 92 / 6
        x1 = x0 + 92 / 6
        m.add(R.loft([[(x0, -11, 18.0), (x0, 34, 18.0)], [(x1, -11, 23.0), (x1, 34, 23.0)]], TOIT, cap0=False, cap1=False))
        m.add(box(x1 - 0.4, -11, 18.0, x1, 34, 23.0, VITRE))
    m.add(box(-46, -10.6, 15.0, 46, -9.8, 18.0, peau))                         # bandeau aux couleurs
    for x in (-24, 0, 24):
        m.add(box(x - 8, -10.4, 0.8, x + 8, -10.2, 13.5, SOMBRE))              # portes ouvertes
        m.add(box(x - 8.8, -10.8, 0.8, x - 8, -10.0, 14.2, JAUNE))
        m.add(box(x + 8, -10.8, 0.8, x + 8.8, -10.0, 14.2, JAUNE))
        m.add(voie([(x, -12), (x, -10.4)], traverses=False, z=0.1))
        m.add(voie([(x, -24), (x, -12)], z=0.05))
    # portique roulant au-dessus de la voie de sortie
    for x in (-40, 40):
        for y in (-34, -14):
            m.add(box(x - 0.8, y - 0.8, 0.8, x + 0.8, y + 0.8, 16.0, JAUNE))
        m.add(box(x - 1.0, -34.5, 15.0, x + 1.0, -13.5, 16.5, JAUNE))
    m.add(box(-41, -26, 16.5, 41, -22, 18.5, peau))
    m.add(box(4, -27, 13.5, 10, -21, 16.5, NOIR))                              # treuil
    m.add(tube((7, -24, 13.5), (7, -24, 8.0), 0.15, mat=ACIER_SOMBRE, seg=3))
    # pièces détachées : essieux, cuve et bureau
    for k in range(3):
        m.add(tube((-30 + k * 5, -32.5, 1.6), (-30 + k * 5, -29.5, 1.6), 1.6, mat=ACIER, seg=10))
    m.add(tube((22, -32, 3.0), (32, -32, 3.0), 2.6, mat=Mat((60, 66, 60), spec=0.3), seg=12))
    m.add(antenne(42, 30, 18.0, 8.0))
    return m


# ---------------------------------------------------------------------------
# Trains (y vers l'avant)
# ---------------------------------------------------------------------------
def bogie(y):
    m = Mesh()
    m.add(box(-3.9, y - 3.2, 0.9, 3.9, y + 3.2, 2.4, ACIER_SOMBRE))
    for dy in (-1.8, 1.8):
        for s in (1, -1):
            m.add(tube((s * 3.4, y + dy, 1.4), (s * 4.1, y + dy, 1.4), 1.4, mat=NOIR, seg=10))
    return m


def chassis(L, z=2.4, h=1.2):
    m = Mesh()
    m.add(box(-4.4, -L, z, 4.4, L, z + h, ACIER_SOMBRE))
    for s in (1, -1):                                                          # attelages et tampons
        m.add(box(-0.6, s * L - 0.0, z + 0.2, 0.6, s * (L + 0.8), z + 0.9, NOIR))
        for x in (-3.0, 3.0):
            m.add(tube((x, s * L, z + 0.6), (x, s * (L + 0.6), z + 0.6), 0.55, mat=ACIER, seg=6))
    return m


def locomotive_diesel():
    """Locomotive diesel de manœuvre et de ligne : cabine à l'avant, long capot moteur."""
    peau = blinde(3.5, 310)
    m = Mesh()
    L = 10.6
    m.add(bogie(6.0))
    m.add(bogie(-6.0))
    m.add(chassis(L))
    m.add(box(-1.6, -3.0, 0.9, 1.6, 3.0, 2.4, Mat((50, 54, 50), spec=0.2)))   # réservoir
    # long capot et cabine surélevée
    m.add(prism(rrect(-3.2, -10.2, 3.2, 3.2, 0.8), 3.6, 8.2, peau, bevel=0.6))
    m.add(prism(rrect(-4.2, 3.6, 4.2, 9.4, 0.6), 3.6, 10.4, peau, bevel=0.5))
    m.add(prism(rrect(-3.0, 9.4, 3.0, 10.6, 0.5), 3.6, 7.0, peau, bevel=0.4))   # nez court
    m.add(box(-3.9, 9.38, 7.4, 3.9, 9.42, 9.6, VITRE))
    for s in (1, -1):
        m.add(box(s * 4.18, 4.6, 7.2, s * 4.22, 8.6, 9.4, VITRE))
    m.add(prism(rrect(-4.4, 3.4, 4.4, 9.6, 0.6), 10.4, 11.0, ACIER_SOMBRE))    # toit de cabine
    # grilles, échappements, avertisseur, bande jaune
    for y in (-8.0, -4.5, -1.0):
        m.add(box(-2.6, y, 8.2, 2.6, y + 2.6, 8.5, GRILLE))
    m.add(tube((0, 1.6, 8.2), (0, 1.6, 9.8), 0.6, mat=NOIR, seg=6))
    m.add(tube((0, -9.0, 8.2), (0, -9.0, 9.4), 0.5, mat=NOIR, seg=6))
    for s in (1, -1):
        m.add(box(s * 4.4 - 0.15, -L, 3.6, s * 4.4 + 0.15, L, 4.3, JAUNE))
        m.add(tube((s * 3.5, -10.2, 4.4), (s * 3.5, 3.4, 4.4), 0.12, mat=ACIER, seg=3))      # mains courantes
        m.add(tube((s * 2.2, 10.65, 5.6), (s * 2.2, 10.85, 5.6), 0.45, mat=FEU_BLANC, seg=6))
        m.add(tube((s * 2.2, -10.25, 5.6), (s * 2.2, -10.45, 5.6), 0.4, mat=FEU_ROUGE, seg=6))
    m.add(antenne(-2.0, 6.0, 11.0, 2.5, r=0.12))
    return m


def wagon_lourd():
    """Wagon plat à ranchers pour véhicules : plancher, bords bas, cales."""
    peau = blinde(4.0, 311)
    m = Mesh()
    L = 10.8
    m.add(bogie(7.0))
    m.add(bogie(-7.0))
    m.add(chassis(L))
    m.add(box(-4.6, -L, 3.6, 4.6, L, 4.2, Mat((96, 74, 50), spec=0.05, tex=stripes(1.1, 0.8, axis=1))))   # plancher en bois
    for s in (1, -1):
        m.add(box(s * 4.6 - 0.4 * s, -L, 3.6, s * 4.6, L, 5.0, peau))         # rives
        for y in (-8.0, -4.0, 0.0, 4.0, 8.0):
            m.add(box(s * 4.5 - 0.35, y - 0.35, 3.6, s * 4.5 + 0.35, y + 0.35, 6.4, ACIER_SOMBRE))    # ranchers
    for y in (-L + 0.3, L - 0.3):
        m.add(box(-4.6, y - 0.3, 3.6, 4.6, y + 0.3, 5.8, peau))                # ridelles d'about
    for y in (-6.0, 6.0):
        m.add(box(-2.8, y - 0.6, 4.2, 2.8, y + 0.6, 4.9, JAUNE))               # cales de roues
    return m


def wagon_leger():
    """Wagon de troupes couvert : caisse à fenêtres, portes coulissantes, toit arrondi."""
    peau = blinde(3.0, 312)
    m = Mesh()
    L = 10.8
    m.add(bogie(7.0))
    m.add(bogie(-7.0))
    m.add(chassis(L))
    m.add(prism(rrect(-4.3, -L, 4.3, L, 0.8), 3.6, 9.6, peau, bevel=0.4))
    rings = []
    for y in (-L, L):
        rings.append([(4.3 * math.cos(math.pi * i / 8), y, 9.6 + 1.6 * math.sin(math.pi * i / 8)) for i in range(9)])
    m.add(R.loft([list(reversed(r)) for r in rings], Mat((92, 96, 92), spec=0.25)))
    for s in (1, -1):
        for y in (-8.2, -5.4, 5.4, 8.2):
            m.add(box(s * 4.28, y - 1.0, 6.6, s * 4.32, y + 1.0, 8.4, VITRE))
        m.add(box(s * 4.29, -2.2, 3.8, s * 4.33, 2.2, 8.8, Mat((60, 64, 58), spec=0.2)))     # porte coulissante
        m.add(box(s * 4.3 - 0.1, -L, 4.4, s * 4.3 + 0.1, L, 5.0, JAUNE))
    for y in (-6.0, 0.0, 6.0):
        m.add(box(-0.8, y - 0.8, 11.0, 0.8, y + 0.8, 11.5, GRILLE))            # aérateurs
    return m


# ---------------------------------------------------------------------------
# Trains blindés (une locomotive blindée armée). Tourelles modélisées autour de leur pivot
# (origine), à leur hauteur réelle ; position du pivot dans la caisse : TOURELLES_TRAINS.
# ---------------------------------------------------------------------------
from modeles.communs import canon, trappe, missile  # noqa: E402


def _coque_blindee(L, h, peau, pente=1.2, largeur=4.5):
    """Caisse blindée à flancs inclinés sur châssis et bogies."""
    m = Mesh()
    m.add(bogie(L - 3.8))
    m.add(bogie(-(L - 3.8)))
    m.add(chassis(L))
    for s in (1, -1):                                                          # jupes blindées sur les bogies
        m.add(box(s * largeur - 0.25 * s, -L, 1.0, s * largeur + 0.05 * s, L, 3.6, peau))
    m.add(prism(rrect(-largeur, -L, largeur, L, 1.0), 3.6, 3.6 + h, peau, bevel=pente))
    return m


def bp42():
    """BP-42 : train blindé polyvalent, caisse moyenne, tourelle de 76 mm, meurtrières."""
    peau = blinde(3.0, 320)
    m = _coque_blindee(10.6, 4.4, peau)
    for s in (1, -1):
        for y in (-7.5, -4.5, 5.5, 8.5):
            m.add(box(s * 4.0 - 0.1, y - 0.5, 5.6, s * 4.0 + 0.1, y + 0.5, 6.4, NOIR))      # meurtrières
    m.add(prism(rrect(-2.4, -9.6, 2.4, -5.0, 0.6), 8.0, 9.6, peau, bevel=0.4))           # poste de conduite
    m.add(box(-2.0, -9.65, 8.6, 2.0, -9.55, 9.2, VITRE))
    m.add(antenne(-3.0, -8.0, 8.0, 3.0, r=0.12))
    return m


def bp42_tourelle():
    peau = blinde(2.5, 321)
    m = Mesh()
    m.add(prism(R.ngon(0, 0, 3.0, 10, sy=1.1), 8.0, 10.6, peau, bevel=0.7))
    m.add(canon(2.6, 10.5, 9.2, 0.45, manchon=(0.2, 0.5)))
    m.add(tube((1.6, 2.4, 9.8), (1.6, 4.2, 9.8), 0.18, mat=NOIR, seg=5))               # mitrailleuse coaxiale
    m.add(trappe(-0.9, -1.0, 10.6, 0.8))
    return m


def zaamurets():
    """Zaamurets : train blindé lourd, caisse haute et massive, plaques rivetées, DCA à l'arrière."""
    peau = blinde(2.5, 322)
    m = _coque_blindee(11.0, 5.4, peau, pente=1.6, largeur=4.7)
    for y in range(-9, 10, 3):
        for s in (1, -1):
            m.add(sphere((s * 4.15, y, 6.6), 0.25, ACIER_SOMBRE, seg=4, rings=2))          # rivets
    m.add(prism(rrect(-2.2, -10.4, 2.2, -6.6, 0.5), 9.0, 10.4, peau, bevel=0.4))
    m.add(box(-1.8, -10.45, 9.5, 1.8, -10.35, 10.0, VITRE))
    # affût antiaérien jumelé à l'arrière
    m.add(tube((0, -8.4, 9.0), (0, -8.4, 10.6), 1.2, mat=ACIER_SOMBRE, seg=8))
    for x in (-0.5, 0.5):
        m.add(tube((x, -8.4, 10.8), (x, -6.2, 12.4), 0.18, mat=ACIER, seg=5))
    return m


def zaamurets_tourelle():
    peau = blinde(2.5, 323)
    m = Mesh()
    m.add(prism(rrect(-3.6, -3.4, 3.6, 3.8, 1.2), 9.0, 12.0, peau, bevel=0.9))
    for x in (-1.3, 1.3):                                                      # deux canons de 107 mm
        m.add(canon(3.6, 12.0, 10.4, 0.5, x=x, manchon=(0.15, 0.45)))
    m.add(trappe(0.0, -1.6, 12.0, 0.9))
    return m


def bp43():
    """BP-43 : train blindé lourd offensif, caisse très large et basse, obusier lourd."""
    peau = blinde(3.0, 324)
    m = _coque_blindee(11.2, 4.8, peau, pente=1.4, largeur=4.9)
    m.add(prism(rrect(-2.6, -10.6, 2.6, -7.2, 0.5), 8.4, 9.8, peau, bevel=0.4))
    m.add(box(-2.2, -10.65, 8.9, 2.2, -10.55, 9.4, VITRE))
    for s in (1, -1):                                                          # caisses à obus
        m.add(box(s * 3.2 - 1.0, 6.5, 8.4, s * 3.2 + 1.0, 10.0, 9.4, Mat((70, 78, 50), spec=0.1)))
    return m


def bp43_tourelle():
    peau = blinde(2.5, 325)
    m = Mesh()
    m.add(prism(rrect(-3.8, -4.4, 3.8, 3.6, 1.0), 8.4, 12.6, peau, bevel=1.0))
    m.add(canon(3.4, 14.5, 10.6, 0.8, manchon=(0.1, 0.35)))                     # obusier de 152 mm
    m.add(box(-1.4, 2.8, 9.6, 1.4, 4.4, 11.8, ACIER_SOMBRE))                    # masque
    m.add(trappe(-1.8, -2.4, 12.6, 0.8))
    m.add(trappe(1.8, -2.4, 12.6, 0.8))
    return m


def krajina():
    """Krajina Ekspres : train blindé rapide, caisse profilée en coin, basse."""
    peau = blinde(3.5, 326)
    m = Mesh()
    L = 10.8
    m.add(bogie(7.0))
    m.add(bogie(-7.0))
    m.add(chassis(L, h=1.0))
    prof = [(-4.3, -L), (4.3, -L), (4.3, L - 3.5), (1.6, L), (-1.6, L), (-4.3, L - 3.5)]
    m.add(prism(prof, 3.4, 6.8, peau, bevel=1.2))
    m.add(box(-1.4, L - 0.4, 5.0, 1.4, L - 0.3, 6.2, VITRE))
    for s in (1, -1):
        m.add(box(s * 4.3 - 0.1 * s, -L, 3.6, s * 4.3, L - 3.6, 4.2, JAUNE))          # bande de vitesse
        m.add(tube((s * 1.2, L + 0.05, 4.4), (s * 1.2, L + 0.25, 4.4), 0.4, mat=FEU_BLANC, seg=6))
    m.add(tube((0, -8.0, 6.8), (0, -8.0, 8.0), 0.5, mat=NOIR, seg=6))
    return m


def krajina_tourelle():
    """Lance-roquettes (caisson de 8 tubes) et canon de 30 mm."""
    peau = blinde(2.5, 327)
    m = Mesh()
    m.add(prism(R.ngon(0, 0, 2.6, 10), 6.8, 8.6, peau, bevel=0.5))
    m.add(box(-3.0, -2.4, 8.6, 3.0, 4.0, 11.0, peau, bevel=0.3))
    for x in (-2.0, -0.7, 0.7, 2.0):
        for z in (9.3, 10.3):
            m.add(tube((x, 4.0, z), (x, 4.1, z), 0.4, mat=NOIR, seg=6))
    m.add(canon(2.0, 7.0, 8.0, 0.25, x=3.4, frein=False))
    return m


def gustav():
    """Schwerer Gustav : canon ferroviaire géant (affût sur deux doubles bogies, voiture plus longue qu'une case)."""
    peau = blinde(4.0, 328)
    m = Mesh()
    L = 16.0
    for y in (-12.5, -6.0, 6.0, 12.5):
        m.add(bogie(y))
    m.add(box(-5.0, -L, 2.4, 5.0, L, 4.0, ACIER_SOMBRE))
    for s in (1, -1):                                                          # longerons de l'affût
        m.add(box(s * 4.0 - 1.0, -L + 1, 4.0, s * 4.0 + 1.0, L - 1, 9.0, peau, bevel=0.4))
        for y in (-12.0, -6.0, 6.0, 12.0):
            m.add(box(s * 5.0 - 0.2 * s, y - 0.8, 3.6, s * 5.1, y + 0.8, 8.6, ACIER_SOMBRE))  # vérins de calage
    m.add(box(-3.0, -L + 0.5, 4.0, 3.0, -9.0, 10.0, peau, bevel=0.5))           # chambre de chargement
    m.add(box(-2.0, -L + 0.2, 8.0, 2.0, -L + 0.5, 9.4, NOIR))
    return m


def gustav_tourelle():
    """Berceau et tube de 800 mm, pointé haut."""
    peau = blinde(3.0, 329)
    m = Mesh()
    m.add(box(-3.0, -5.0, 9.0, 3.0, 4.0, 13.0, peau, bevel=0.6))
    m.add(tube((-3.6, 0, 11.5), (3.6, 0, 11.5), 1.4, mat=ACIER_SOMBRE, seg=10))   # tourillons
    a = math.radians(30)
    y1, z1 = 3.0 + 26.0 * math.cos(a), 11.5 + 26.0 * math.sin(a)
    m.add(tube((0, -3.0, 11.5 - 6.0 * math.sin(a) * 0.3), (0, y1, z1), 1.9, 1.3, mat=ACIER, seg=12))
    m.add(tube((0, 1.0, 11.5), (0, 6.0, 11.5 + 5.0 * math.tan(a)), 2.5, mat=ACIER_SOMBRE, seg=12))  # frette
    return m


# Pivot des tourelles dans la caisse (unités du modèle, avant mise à l'échelle) et bout du tube
# (par rapport au pivot) : servent à Turreted.Offset et Armament.LocalOffset dans les yaml.
TOURELLES_TRAINS = {
    "bp42": ((0.0, 2.6, 0.0), (10.5, 9.2)),
    "zaamurets": ((0.0, 2.4, 0.0), (12.0, 10.4)),
    "bp43": ((0.0, 1.4, 0.0), (14.5, 10.6)),
    "krajina": ((0.0, 1.0, 0.0), (4.1, 9.8)),
    "gustav": ((0.0, 2.0, 0.0), (25.5, 24.5)),
}
