"""Modèles 3D des unités de la Rubénie : blindés lourds et rustiques d'inspiration soviétique,
tôles épaisses, galets nombreux, insigne de la montagne (vert et blanc)."""
import math
import rendu3d as R
from rendu3d import Mesh, Mat, merge, prism, box, tube, sphere, rrect, ngon, panels, noise, combine, stripes, wing
from modeles.communs import (blinde, chenilles, canon, antenne, trappe, roue, ACIER, ACIER_SOMBRE, GRILLE, CAOUTCHOUC,
                             JERRICAN, FEU_ROUGE, BACHE, VITRE, BOIS, fuselage, verriere, missile, GRIS_FONCE, TUYERE,
                             GRIS_NAVAL, sillage)

VERT_RUB = (46, 120, 64)
BLANC = (236, 238, 232)
PHARE = Mat((230, 226, 190), spec=0.8, shine=30)
SACS = Mat((150, 132, 92), spec=0.02, flat=True, tex=noise(71, 0.12, 0.7))
KAKI = Mat((88, 92, 70), spec=0.12, tex=combine(panels(3.0, 0.3, 0.86), noise(72, 0.08)))


def montagne(x, y, z, k=1.0):
    """Insigne : triangle vert et sommet enneigé, posé à plat."""
    m = Mesh()
    base = [(x - 1.2 * k, y - 0.8 * k), (x + 1.2 * k, y - 0.8 * k), (x, y + 1.0 * k)]
    m.add(prism(base, z, z + 0.08, Mat(VERT_RUB, spec=0.2)))
    m.add(prism([(x - 0.4 * k, y + 0.35 * k), (x + 0.4 * k, y + 0.35 * k), (x, y + 1.0 * k)], z + 0.08, z + 0.12, Mat(BLANC)))
    return m


def reservoirs(m, y0, s_list=(1, -1), x=4.6, z=4.0):
    """Fûts de carburant largables à l'arrière, typiques des chars soviétiques."""
    for s in s_list:
        m.add(tube((s * x, y0, z + 0.9), (s * x, y0 - 3.0, z + 0.9), 0.9, mat=Mat((74, 82, 58), spec=0.2), seg=10))
    return m


# ---------------------------------------------------------------------------
# « Si ça tient, touche à rien » : vieux char lourd bi-tube, rafistolé mais increvable
# ---------------------------------------------------------------------------
def sicatient_caisse():
    peau = blinde(4.0, 73)
    m = Mesh()
    prof = [(-5.2, -10.6), (5.2, -10.6), (5.2, 7.4), (3.8, 10.8), (-3.8, 10.8), (-5.2, 7.4)]
    m.add(prism(prof, 1.6, 4.6, peau, bevel=1.2))
    tr = chenilles(21.0, 2.3, 4.3, 6.2, 0.0, wheels=6)
    m.add(tr).add(tr.mirror_x())
    for s in (1, -1):
        m.add(box(min(s * 5.0, s * 7.4), -10.8, 4.2, max(s * 5.0, s * 7.4), 10.6, 4.6, peau))
        # plaques de blindage rapportées, boulonnées de travers
        m.add(box(s * 7.4 - (0.3 if s > 0 else -0.3), -4.0, 1.4, s * 7.5, 3.2, 4.1, Mat((100, 96, 80), spec=0.2, tex=noise(74, 0.12))).rot_z(s * 2))
    # sacs de sable sur le glacis, rondins de désembourbage, chaîne
    for k in range(4):
        m.add(sphere((-3.0 + k * 2.0, 8.4, 4.9), 1.0, SACS, seg=6, rings=3, sz=0.55))
    m.add(tube((-4.6, -10.2, 5.2), (4.6, -10.2, 5.2), 0.55, mat=BOIS, seg=8))
    m.add(box(-4.2, -9.8, 4.6, 4.2, -6.0, 5.0, GRILLE))
    reservoirs(m, -7.0)
    for s in (1, -1):
        m.add(tube((s * 3.6, 10.7, 4.2), (s * 3.6, 11.2, 4.2), 0.45, mat=PHARE, seg=6))
    m.add(montagne(3.6, 6.0, 4.62))
    return m


def sicatient_tourelle():
    peau = blinde(3.0, 75)
    m = Mesh()
    m.add(prism(ngon(0, -0.5, 4.6, 14, sy=1.12), 0.0, 3.4, peau, bevel=1.5))       # tourelle moulée en dôme
    m.add(box(-2.4, 3.6, 0.8, 2.4, 5.4, 2.8, peau, bevel=0.4))
    for x in (-1.1, 1.1):
        m.add(canon(5.0, 15.0, 1.8, 0.48, x=x, frein=True))
    m.add(trappe(-1.8, -1.6, 3.35, 1.1))
    m.add(tube((1.8, -1.2, 3.4), (1.8, -1.2, 4.2), 0.9, mat=ACIER_SOMBRE, seg=8))
    m.add(tube((1.8, -0.6, 4.6), (1.8, 2.4, 4.6), 0.2, mat=ACIER_SOMBRE, seg=4))    # DShK
    # caisses de munitions et outils sanglés sur l'arrière, rustine soudée
    m.add(box(-3.4, -6.2, 0.6, 3.4, -4.4, 2.4, Mat((82, 86, 60), spec=0.1), bevel=0.2))
    m.add(box(1.2, 1.0, 3.3, 3.2, 2.4, 3.55, Mat((120, 110, 90), spec=0.1)))
    m.add(antenne(-3.0, -4.0, 3.2, 7.0))
    return m


# ---------------------------------------------------------------------------
# Char de contre-attaque : char moyen rapide et bas, canon lisse, blindage réactif
# ---------------------------------------------------------------------------
def reactif(x0, y0, x1, y1, z, mat, pas=1.2):
    """Briques de blindage réactif en damier sur une surface horizontale."""
    m = Mesh()
    y = y0
    while y < y1 - 0.2:
        x = x0
        while x < x1 - 0.2:
            m.add(box(x, y, z, min(x + pas - 0.15, x1), min(y + pas - 0.15, y1), z + 0.35, mat))
            x += pas
        y += pas
    return m


def contreattaque_caisse():
    peau = blinde(4.0, 76)
    m = Mesh()
    prof = [(-4.4, -9.4), (4.4, -9.4), (4.4, 6.4), (3.0, 9.6), (-3.0, 9.6), (-4.4, 6.4)]
    m.add(prism(prof, 1.4, 3.8, peau, bevel=1.1))
    tr = chenilles(18.8, 2.0, 3.8, 5.4, 0.0, wheels=6, skirt=(1.8, 3.7), mat_skirt=peau)
    m.add(tr).add(tr.mirror_x())
    m.add(reactif(-3.4, 6.2, 3.4, 9.0, 3.8, peau))
    m.add(box(-3.6, -9.2, 3.8, 3.6, -6.0, 4.1, GRILLE))
    reservoirs(m, -6.4, x=4.2, z=3.6)
    m.add(montagne(-2.6, 3.4, 3.82, 0.8))
    return m


def contreattaque_tourelle():
    peau = blinde(3.0, 77)
    m = Mesh()
    m.add(prism([(-3.6, -3.6), (3.6, -3.6), (4.0, 0.8), (2.0, 3.6), (-2.0, 3.6), (-4.0, 0.8)], 0.0, 2.6, peau, bevel=1.0))
    m.add(reactif(-3.0, 0.6, 3.0, 3.0, 2.6, peau, 1.0))
    m.add(canon(3.0, 14.0, 1.4, 0.45, manchon=(0.25, 0.55)))
    m.add(trappe(-1.6, -1.4, 2.55, 0.9))
    m.add(tube((1.6, -1.0, 2.6), (1.6, -1.0, 3.3), 0.7, mat=ACIER_SOMBRE, seg=8))
    for s in (1, -1):
        for k in range(4):
            m.add(tube((s * 3.6, -1.0 + k * 0.7, 2.0), (s * 4.4, -0.8 + k * 0.7, 2.5), 0.25, mat=ACIER_SOMBRE, seg=5))
    m.add(antenne(-2.6, -3.0, 2.6, 6.0))
    return m


# ---------------------------------------------------------------------------
# Char lourd de forteresse : mastodonte bi-tube de 140 mm, nacelles de missiles
# ---------------------------------------------------------------------------
def forteresse_caisse():
    peau = blinde(4.5, 78)
    m = Mesh()
    prof = [(-6.4, -12.8), (6.4, -12.8), (6.4, 9.0), (4.8, 13.0), (-4.8, 13.0), (-6.4, 9.0)]
    m.add(prism(prof, 1.8, 5.4, peau, bevel=1.4))
    for x in (7.4, -7.4):      # doubles trains de chenilles (quatre bandes)
        for dx in (-1.25, 1.25):
            tr = chenilles(24.6, 2.3, 4.8, x + dx * (1 if x > 0 else -1), 0.0, wheels=7)
            m.add(tr)
    for s in (1, -1):
        m.add(box(min(s * 6.0, s * 10.0), -13.0, 4.8, max(s * 6.0, s * 10.0), 12.8, 5.3, peau, bevel=0.2))
    m.add(box(-5.0, -12.4, 5.4, 5.0, -8.0, 5.8, GRILLE))
    for s in (1, -1):
        m.add(tube((s * 3.4, -12.9, 4.4), (s * 3.4, -14.0, 4.6), 0.6, mat=ACIER_SOMBRE, seg=6))
        m.add(tube((s * 4.4, 12.9, 4.8), (s * 4.4, 13.4, 4.8), 0.5, mat=PHARE, seg=6))
    m.add(montagne(4.4, 8.0, 5.42, 1.2))
    return m


def forteresse_tourelle():
    peau = blinde(3.0, 79)
    m = Mesh()
    m.add(prism([(-5.2, -6.0), (5.2, -6.0), (5.8, 1.0), (3.6, 5.4), (-3.6, 5.4), (-5.8, 1.0)], 0.0, 4.0, peau, bevel=1.3))
    for x in (-1.6, 1.6):
        m.add(canon(5.0, 21.0, 2.0, 0.6, x=x, manchon=(0.2, 0.6)))
    # nacelles de missiles latérales (8 tubes chacune)
    for s in (1, -1):
        m.add(box(s * 8.6 - 1.6, -3.0, 1.2, s * 8.6 + 1.6, 3.0, 4.0, peau, bevel=0.3))
        m.add(box(min(s * 5.6, s * 7.0), -1.0, 2.2, max(s * 5.6, s * 7.0), 1.0, 3.0, ACIER_SOMBRE))
        for k in range(8):
            dx, dz = (k % 4) * 0.75 - 1.1, (k // 4) * 1.0 + 2.1
            m.add(tube((s * 8.6 + dx, 3.0, dz), (s * 8.6 + dx, 3.1, dz), 0.3, mat=Mat((200, 60, 40)), seg=6))
    m.add(trappe(-2.4, -2.4, 3.95, 1.2))
    m.add(trappe(2.4, -1.4, 3.95, 1.0))
    m.add(tube((2.4, -0.6, 4.8), (2.4, 3.0, 4.8), 0.22, mat=ACIER_SOMBRE, seg=4))
    m.add(box(-4.6, -8.0, 0.8, 4.6, -6.0, 3.4, Mat((82, 86, 60), spec=0.1), bevel=0.2))
    m.add(antenne(-4.0, -5.0, 4.0, 8.0))
    m.add(antenne(4.0, -5.0, 4.0, 6.0))
    return m


# ---------------------------------------------------------------------------
# Véhicule de reconnaissance : blindé amphibie 4×4 (famille BRDM), tourelle conique
# ---------------------------------------------------------------------------
def reco_caisse():
    peau = blinde(3.0, 80)
    m = Mesh()
    prof = [(-3.4, -6.8), (3.4, -6.8), (3.6, 3.0), (2.2, 7.2), (-2.2, 7.2), (-3.6, 3.0)]
    m.add(prism(prof, 1.4, 4.0, peau, bevel=1.0))
    m.add(box(-2.4, 3.0, 3.6, 2.4, 3.6, 4.4, VITRE))
    for s in (1, -1):
        for y in (-4.0, 4.0):
            m.add(roue(s * 3.4, y, 1.4, 1.4, 1.0, s))
        m.add(tube((s * 3.6, -1.2, 1.2), (s * 3.6, 1.2, 1.2), 0.6, mat=CAOUTCHOUC, seg=8))   # roues ventrales
    m.add(box(-1.2, -6.8, 1.6, 1.2, -7.4, 2.6, ACIER_SOMBRE))                           # hydrojet
    m.add(antenne(2.6, -5.0, 4.0, 6.0))
    m.add(montagne(-2.0, -4.2, 4.02, 0.7))
    return m


def reco_tourelle():
    peau = blinde(2.5, 81)
    m = Mesh()
    m.add(tube((0, 0, 0), (0, 0, 1.8), 1.9, 1.3, mat=peau, seg=10))
    m.add(tube((0, 1.0, 1.0), (0, 5.0, 1.0), 0.22, mat=ACIER_SOMBRE, seg=5))
    m.add(tube((0.6, 1.0, 1.2), (0.6, 3.0, 1.2), 0.14, mat=ACIER_SOMBRE, seg=4))
    return m


# ---------------------------------------------------------------------------
# Véhicule antiaérien : châssis chenillé, tourelle quadritube et radar (famille Chilka)
# ---------------------------------------------------------------------------
def aa_caisse():
    peau = blinde(3.5, 82)
    m = Mesh()
    prof = [(-4.2, -8.8), (4.2, -8.8), (4.2, 6.0), (3.0, 8.8), (-3.0, 8.8), (-4.2, 6.0)]
    m.add(prism(prof, 1.4, 4.0, peau, bevel=0.9))
    tr = chenilles(17.6, 1.9, 3.7, 5.1, 0.0, wheels=6)
    m.add(tr).add(tr.mirror_x())
    m.add(prism(ngon(0, -7.7, 3.2, 12), 4.0, 7.0, peau, bevel=0.5))
    m.add(box(-3.4, -3.0, 4.0, 3.4, 4.0, 4.6, GRILLE))
    m.add(montagne(2.4, 5.6, 4.02, 0.8))
    return m


def aa_tourelle():
    peau = blinde(2.5, 83)
    m = Mesh()
    m.add(prism(rrect(-3.0, -2.6, 3.0, 2.6, 0.8), 0.0, 2.2, peau, bevel=0.5))
    for x in (-1.4, -0.5, 0.5, 1.4):
        m.add(tube((x, 2.4, 1.2), (x, 6.8, 1.6), 0.2, mat=ACIER_SOMBRE, seg=5))
    m.add(tube((0, -1.6, 2.2), (0, -1.6, 3.0), 0.3, mat=ACIER_SOMBRE, seg=5))
    m.add(tube((0, -1.6, 3.4), (0, -1.0, 3.4), 1.4, 1.6, mat=Mat((170, 174, 166), spec=0.4), seg=12))   # radar
    return m


# ---------------------------------------------------------------------------
# Lance-missiles mobile : camion 8×8 à missile balistique érigé
# ---------------------------------------------------------------------------
def lancemissile(charge=True):
    peau = blinde(3.5, 84)
    m = Mesh()
    m.add(prism(rrect(-3.2, 6.0, 3.2, 11.4, 1.0), 1.6, 5.2, peau, bevel=0.6))
    m.add(box(-2.9, 10.8, 3.8, 2.9, 11.45, 4.9, VITRE))
    m.add(box(-3.2, -12.0, 1.4, 3.2, 6.0, 2.8, ACIER_SOMBRE))
    m.add(box(-3.4, -12.0, 2.8, 3.4, 5.6, 3.2, peau))
    for s in (1, -1):
        for y in (-9.0, -6.2, 5.0, 8.0):
            m.add(roue(s * 3.3, y, 1.5, 1.5, 1.1, s))
        m.add(tube((s * 2.4, 11.3, 3.4), (s * 2.4, 11.7, 3.4), 0.4, mat=PHARE, seg=6))
    # rampe érectrice
    m.add(box(-1.8, -11.0, 3.2, 1.8, 4.6, 3.8, peau))
    if charge:
        blanc = Mat((220, 222, 214), spec=0.4, tex=lambda p, n: (40, 40, 40) if (p[1] % 3.0) < 0.3 else 1.0)
        m.add(fuselage([(-11.0, 1.1, 1.1, 5.0), (2.0, 1.1, 1.1, 5.0), (4.0, 0.8, 0.8, 5.0), (5.6, 0.1, 0.1, 5.0)], blanc, seg=10))
        for s in (1, -1):
            m.add(box(min(s * 0.9, s * 2.0), -11.0, 4.9, max(s * 0.9, s * 2.0), -9.0, 5.1, blanc))
    m.add(antenne(-2.6, 7.0, 5.2, 5.0))
    m.add(montagne(0, 8.6, 5.22, 0.9))
    return m


def lancemissile_vide():
    return lancemissile(False)


# ---------------------------------------------------------------------------
# Canon automoteur : obusier chenillé de 152 mm à grosse tourelle (famille Msta)
# ---------------------------------------------------------------------------
def canon_auto():
    peau = blinde(4.0, 85)
    m = Mesh()
    prof = [(-4.8, -10.4), (4.8, -10.4), (4.8, 7.0), (3.4, 10.2), (-3.4, 10.2), (-4.8, 7.0)]
    m.add(prism(prof, 1.5, 4.2, peau, bevel=1.0))
    tr = chenilles(20.6, 2.1, 4.0, 5.8, 0.0, wheels=7)
    m.add(tr).add(tr.mirror_x())
    m.add(prism([(-4.4, -9.6), (4.4, -9.6), (4.6, 1.0), (3.0, 3.6), (-3.0, 3.6), (-4.6, 1.0)], 4.2, 8.2, peau, bevel=1.1))
    m.add(tube((0, 2.6, 6.2), (0, 18.6, 8.2), 0.62, 0.55, mat=ACIER, seg=8))
    m.add(tube((0, 17.4, 8.0), (0, 19.2, 8.3), 1.0, mat=ACIER_SOMBRE, seg=8))
    m.add(tube((0, 9.0, 7.1), (0, 11.0, 7.4), 0.85, mat=ACIER_SOMBRE, seg=8))             # évacuateur
    m.add(trappe(-2.4, -4.0, 8.15, 1.0))
    m.add(tube((2.4, -4.0, 8.2), (2.4, -4.0, 9.0), 0.8, mat=ACIER_SOMBRE, seg=8))
    m.add(box(-4.0, -10.4, 5.0, 4.0, -9.6, 7.6, Mat((82, 86, 60), spec=0.1)))
    m.add(antenne(-3.6, -8.0, 8.2, 7.0))
    m.add(montagne(0, -2.0, 8.22, 1.0))
    return m


# ---------------------------------------------------------------------------
# Artillerie lourde : canon géant sur châssis chenillé ouvert (famille Pion), bêche arrière
# ---------------------------------------------------------------------------
def artillerie_lourde():
    peau = blinde(4.0, 86)
    m = Mesh()
    m.add(prism(rrect(-4.6, -8.0, 4.6, 10.4, 1.2), 1.5, 4.0, peau, bevel=0.8))
    m.add(prism(rrect(-4.2, 5.4, 4.2, 10.2, 1.0), 4.0, 6.4, peau, bevel=0.6))           # cabine avant
    m.add(box(-3.8, 9.8, 5.0, 3.8, 10.3, 6.0, VITRE))
    tr = chenilles(19.0, 2.1, 3.9, 5.6, 0.0, wheels=6)
    m.add(tr.move(dy=1.0)).add(tr.mirror_x().move(dy=1.0))
    # affût à l'arrière, tube énorme pointé vers l'avant par-dessus la cabine
    m.add(prism(ngon(0, -4.0, 3.0, 10), 4.0, 5.6, ACIER_SOMBRE))
    for s in (1, -1):
        m.add(box(s * 1.9 - 0.4, -6.0, 5.6, s * 1.9 + 0.4, -2.0, 8.6, peau))
    m.add(tube((0, -9.0, 7.4), (0, 22.0, 11.0), 0.9, 0.75, mat=ACIER, seg=10))
    m.add(tube((0, -9.6, 7.3), (0, -6.0, 7.7), 1.3, mat=ACIER_SOMBRE, seg=10))            # culasse
    for s in (1, -1):
        m.add(tube((s * 1.4, -4.0, 7.2), (s * 1.4, 4.0, 8.2), 0.35, mat=ACIER_SOMBRE, seg=6))   # freins de recul
    m.add(box(-4.2, -10.6, 0.2, 4.2, -8.0, 2.6, ACIER_SOMBRE, bevel=0.2))                  # bêche
    m.add(box(-3.8, 0.0, 4.0, -2.2, 3.8, 5.6, Mat((82, 86, 60), spec=0.1)))               # obus
    m.add(montagne(2.4, 7.4, 6.42, 0.9))
    return m


# ---------------------------------------------------------------------------
# Lance-roquettes multiple : camion 6×6, panier de 40 tubes (famille BM-21)
# ---------------------------------------------------------------------------
def lrm():
    peau = blinde(3.5, 87)
    m = Mesh()
    m.add(prism(rrect(-3.0, 5.0, 3.0, 10.4, 0.9), 1.6, 5.0, peau, bevel=0.6))
    m.add(box(-2.7, 9.8, 3.6, 2.7, 10.45, 4.8, VITRE))
    m.add(box(-3.0, -10.0, 1.4, 3.0, 5.0, 2.6, ACIER_SOMBRE))
    for s in (1, -1):
        for y in (-7.4, -4.4, 7.0):
            m.add(roue(s * 3.1, y, 1.5, 1.5, 1.1, s))
        m.add(tube((s * 2.2, 10.3, 3.2), (s * 2.2, 10.7, 3.2), 0.4, mat=PHARE, seg=6))
    m.add(prism(ngon(0, -4.0, 2.2, 10), 2.6, 4.0, ACIER_SOMBRE))
    # panier de tubes relevé vers l'avant (4 rangées de 10)
    tubes = Mesh()
    for row in range(4):
        for col in range(10):
            x = -2.7 + col * 0.6
            z = 0.0 + row * 0.6
            tubes.add(tube((x, -5.0, z), (x, 5.0, z), 0.27, mat=Mat((82, 88, 64), spec=0.2), seg=5))
    tubes.add(box(-3.1, -5.2, -0.4, 3.1, 5.0, 0.0, peau))
    m.add(tubes.rot_x(20).move(dy=-3.4, dz=5.4))
    m.add(montagne(0, 7.6, 5.02, 0.9))
    return m


# ---------------------------------------------------------------------------
# Véhicule logistique : camion 6×6 bâché, conteneur et citerne
# ---------------------------------------------------------------------------
def vehicule_logistique():
    peau = blinde(3.5, 88)
    m = Mesh()
    m.add(prism(rrect(-3.2, 5.6, 3.2, 11.0, 0.9), 1.6, 5.4, peau, bevel=0.6))
    m.add(box(-2.9, 10.4, 3.8, 2.9, 11.05, 5.0, VITRE))
    m.add(box(-3.2, -11.0, 1.4, 3.2, 5.6, 2.6, ACIER_SOMBRE))
    # bâche sur arceaux (arrondie) et caisses à l'arrière
    rings = []
    for y in (-10.6, 4.8):
        rings.append([(3.3 * math.cos(math.pi * i / 8), y, 2.6 + 3.8 * math.sin(math.pi * i / 8) ** 0.7) for i in range(9)])
    m.add(R.loft([list(reversed(r)) for r in rings], BACHE))
    for k in range(4):
        m.add(box(-3.35, -9.0 + k * 3.6, 2.6, 3.35, -8.7 + k * 3.6, 6.2, Mat((80, 76, 56), spec=0.05)))
    for s in (1, -1):
        for y in (-8.6, -5.6, 7.6):
            m.add(roue(s * 3.3, y, 1.5, 1.5, 1.1, s))
        m.add(tube((s * 2.4, 10.9, 3.4), (s * 2.4, 11.3, 3.4), 0.4, mat=PHARE, seg=6))
        m.add(box(s * 3.4 - 0.4, 2.0, 1.8, s * 3.4 + 0.4, 5.0, 3.0, JERRICAN))
    m.add(montagne(0, 8.2, 5.42, 0.9))
    return m


# ---------------------------------------------------------------------------
# Bastion roulant : forteresse chenillée, deux tourelles de 130 mm, lance-missiles dorsal
# ---------------------------------------------------------------------------
def bastion_caisse():
    peau = blinde(4.5, 89)
    m = Mesh()
    prof = [(-8.6, -14.0), (8.6, -14.0), (8.6, 10.0), (6.0, 14.2), (-6.0, 14.2), (-8.6, 10.0)]
    m.add(prism(prof, 1.8, 5.2, peau, bevel=1.4))
    for s in (1, -1):
        for dx in (0.0, 2.5):
            tr = chenilles(27.0, 2.3, 4.8, s * (9.4 + dx), 0.0, wheels=8)
            m.add(tr)
        m.add(box(min(s * 8.2, s * 12.2), -14.2, 4.8, max(s * 8.2, s * 12.2), 14.0, 5.3, peau, bevel=0.2))
    # casemate centrale à meurtrières (fantassins à bord) et poste de commandement
    m.add(prism(rrect(-6.0, -12.0, 6.0, 4.0, 1.5), 5.2, 9.0, peau, bevel=1.0))
    for s in (1, -1):
        for k in range(4):
            m.add(box(s * 6.0 - 0.15, -10.0 + k * 3.4, 6.6, s * 6.0 + 0.15, -9.0 + k * 3.4, 7.2, Mat((24, 24, 24))))
    m.add(prism(rrect(-2.6, -2.0, 2.6, 2.8, 0.8), 9.0, 10.6, peau, bevel=0.4))
    m.add(box(-2.4, 2.5, 9.6, 2.4, 2.9, 10.3, VITRE))
    m.add(antenne(-5.0, -11.0, 9.0, 9.0))
    m.add(antenne(5.0, -11.0, 9.0, 7.0))
    m.add(box(-5.0, -14.0, 5.2, 5.0, -12.2, 6.0, GRILLE))
    m.add(montagne(0, 8.0, 5.22, 1.4))
    return m


def bastion_tourelle():
    """Tourelle de 130 mm latérale (le pivot est au niveau du sol, la tourelle sur le pont à 5,2)."""
    peau = blinde(3.0, 90)
    m = Mesh()
    m.add(prism(ngon(0, 0, 3.0, 10, sy=1.1), 5.2, 7.4, peau, bevel=0.8))
    m.add(canon(2.6, 12.0, 6.3, 0.5, manchon=(0.3, 0.6)))
    m.add(trappe(-0.8, -1.2, 7.35, 0.8))
    return m


def bastion_missiles():
    """Lanceur de missiles dorsal (pivot au sol, lanceur sur le toit à 9)."""
    peau = blinde(2.5, 91)
    m = Mesh()
    m.add(prism(ngon(0, 0, 1.6, 8), 9.0, 9.8, peau))
    m.add(box(-1.8, -1.6, 9.8, 1.8, 2.6, 11.4, Mat((96, 100, 70), spec=0.2), bevel=0.2))
    for k in range(6):
        dx, dz = (k % 3) * 1.1 - 1.1, (k // 3) * 0.7 + 10.2
        m.add(tube((dx, 2.6, dz), (dx, 2.7, dz), 0.3, mat=Mat((200, 60, 40)), seg=6))
    return m


# ---------------------------------------------------------------------------
# Tunnelier : foreuse chenillée à tête rotative conique et bras de déblaiement
# ---------------------------------------------------------------------------
def tunnelier():
    peau = blinde(4.0, 92)
    m = Mesh()
    prof = [(-5.0, -11.0), (5.0, -11.0), (5.0, 6.0), (-5.0, 6.0)]
    m.add(prism(prof, 1.6, 5.6, peau, bevel=1.0))
    tr = chenilles(20.0, 2.3, 4.3, 6.0, 0.0, wheels=7)
    m.add(tr.move(dy=-2.0)).add(tr.mirror_x().move(dy=-2.0))
    # tête de forage conique à dents en spirale
    fore = Mat((150, 146, 132), spec=0.6, shine=24, tex=stripes(0.9, 0.7, axis=1))
    m.add(tube((0, 6.0, 3.6), (0, 7.6, 3.6), 4.6, 4.2, mat=ACIER_SOMBRE, seg=14))
    m.add(tube((0, 7.6, 3.6), (0, 14.0, 3.6), 4.0, 0.3, mat=fore, seg=14))
    for k in range(10):
        a = k * 2 * math.pi / 10
        r0 = 4.0 - k * 0.3
        m.add(box(-0.3, 7.8 + k * 0.6, -0.3, 0.3, 8.6 + k * 0.6, 0.3, ACIER).move(dx=r0 * math.cos(a), dz=3.6 + r0 * math.sin(a)))
    # déblais sur l'arrière (bande transporteuse) et cabine de pilotage
    m.add(box(-2.4, -12.0, 5.6, 2.4, -2.0, 6.2, Mat((60, 60, 58), tex=stripes(0.8, 0.7, axis=1))))
    m.add(sphere((0, -9.0, 6.4), 1.8, Mat((120, 96, 70), spec=0.02, tex=noise(93, 0.2, 0.6)), seg=8, rings=4, sz=0.5))
    m.add(prism(rrect(2.2, -1.0, 4.8, 4.6, 0.6), 5.6, 7.6, peau, bevel=0.3))
    m.add(box(2.4, 4.2, 6.4, 4.6, 4.65, 7.3, VITRE))
    m.add(tube((-3.4, -3.0, 5.6), (-3.4, -3.0, 8.4), 0.5, mat=ACIER_SOMBRE, seg=6))
    m.add(montagne(-2.4, 2.4, 5.62, 0.9))
    return m


# ---------------------------------------------------------------------------
# Aviation : camouflage gris-bleu à taches vertes, étoile-montagne sur les ailes
# ---------------------------------------------------------------------------
def marque_rub(cx, cy, r):
    def f(p, n):
        if n[2] < 0.3:
            return None
        dx, dy = p[0] - cx, p[1] - cy
        if abs(dx) > r or dy < -r * 0.8 or dy > r:
            return None
        if abs(dx) <= (r - dy) * 0.6:
            return BLANC if dy > r * 0.35 else VERT_RUB
        return None
    return f


def camo(seed):
    def f(p, n):
        v = math.sin(p[0] * 0.45 + seed) + math.sin(p[1] * 0.38 + 2 * seed) + math.sin((p[0] + p[1]) * 0.21)
        k = 0.86 if (p[0] / 3.5) % 1.0 < 0.08 or (p[1] / 3.5) % 1.0 < 0.08 else 1.0
        if v > 0.9:
            return (int(96 * k), int(116 * k), int(92 * k))
        return (int(136 * k), int(146 * k), int(152 * k))
    return f


def avec(base, *marques):
    def f(p, n):
        for m in marques:
            c = m(p, n)
            if c is not None:
                return c
        return base(p, n)
    return f


def chasseur_poly():
    """Chasseur embarqué lourd bi-dérive (famille Su-33) : plans canard, crosse d'appontage."""
    m = Mesh()
    peau = R.team(spec=0.4, tex=combine(panels(3.5, 0.3, 0.86), noise(94, 0.04)))
    aile = Mat((136, 146, 152), spec=0.4, tex=avec(camo(1.0), marque_rub(9.0, -5.0, 1.6), marque_rub(-9.0, -5.0, 1.6)))
    m.add(fuselage([(-14.0, 1.0, 0.8, 0.2), (-11.0, 2.6, 1.4, 0.3), (-4.0, 3.2, 1.6, 0.4), (3.0, 2.4, 1.6, 0.5),
                    (8.0, 1.4, 1.3, 0.4), (12.0, 0.7, 0.7, 0.2), (14.6, 0.1, 0.1, 0.0)], peau, seg=14))
    for s in (1, -1):
        w = wing((2.6, 3.0), (2.6, -9.0), (12.4, -6.4), (12.4, -8.6), 0.0, aile, thick=0.7)
        m.add(w if s > 0 else w.mirror_x())
        c = wing((1.8, 7.4), (1.8, 5.6), (4.4, 5.0), (4.4, 4.4), 0.5, peau, thick=0.35)
        m.add(c if s > 0 else c.mirror_x())
        t = wing((1.8, -9.6), (1.8, -12.8), (6.6, -12.0), (6.6, -13.6), 0.1, aile, thick=0.4)
        m.add(t if s > 0 else t.mirror_x())
        pts = [(-13.4, 1.0), (-8.4, 1.0), (-11.0, 6.0), (-13.0, 6.2)]
        m.add(R.loft([[(s * 1.8 + dx + s * (z - 1.0) * 0.15, y, z) for y, z in pts] for dx in (-0.25, 0.25)], peau))
        m.add(tube((s * 1.2, -13.4, 0.2), (s * 1.2, -14.8, 0.2), 0.95, 1.05, mat=TUYERE, seg=10))
        m.add(fuselage([(-4.0, 1.0, 1.0, -1.1), (3.0, 1.2, 1.1, -1.1), (4.2, 0.9, 0.9, -1.0)], GRIS_FONCE, seg=8).move(dx=s * 1.8))
        for x in (6.0, 9.0):
            m.add(missile(s * x, -7.0, -1.0, -0.8, 0.4))
        m.add(missile(s * 12.6, -8.6, -3.4, -0.1, 0.32))
    m.add(verriere(4.5, 11.0, 1.1, 1.3, 1.4))
    m.add(tube((0, -12.0, -0.6), (0, -14.6, -1.0), 0.12, mat=ACIER_SOMBRE, seg=4))            # crosse
    return m


def intercepteur():
    """Intercepteur lourd à haute vitesse (famille MiG-31) : grandes entrées d'air, aile trapèze."""
    m = Mesh()
    peau = R.team(spec=0.45, tex=combine(panels(3.5, 0.3, 0.86), noise(95, 0.04)))
    gris = Mat((150, 156, 160), spec=0.45, tex=avec(combine(panels(3.5, 0.3, 0.86), noise(96, 0.04)),
                                                  marque_rub(8.0, -6.0, 1.5), marque_rub(-8.0, -6.0, 1.5)))
    m.add(fuselage([(-15.0, 1.4, 1.0, 0.2), (-12.0, 3.0, 1.5, 0.3), (-2.0, 3.4, 1.6, 0.4), (5.0, 2.6, 1.5, 0.4),
                    (10.0, 1.2, 1.1, 0.3), (14.0, 0.5, 0.5, 0.1), (15.6, 0.1, 0.1, 0.0)], peau, seg=14))
    for s in (1, -1):
        m.add(box(s * 3.4 - 1.2, 0.0, -0.6, s * 3.4 + 1.2, 6.0, 1.6, GRIS_FONCE, bevel=0.3))
        m.add(box(s * 3.4 - 0.9, 5.9, -0.3, s * 3.4 + 0.9, 6.05, 1.3, Mat((20, 20, 22))))
        w = wing((3.8, 0.6), (3.8, -8.4), (13.0, -5.8), (13.0, -8.6), 0.4, gris, thick=0.6, dihedral=-0.4)
        m.add(w if s > 0 else w.mirror_x())
        t = wing((2.8, -10.0), (2.8, -14.0), (7.6, -12.8), (7.6, -14.4), 0.4, gris, thick=0.4)
        m.add(t if s > 0 else t.mirror_x())
        pts = [(-14.6, 1.2), (-9.4, 1.2), (-12.2, 6.2), (-14.2, 6.4)]
        m.add(R.loft([[(s * 2.4 + dx + s * (z - 1.2) * 0.2, y, z) for y, z in pts] for dx in (-0.25, 0.25)], peau))
        m.add(tube((s * 1.5, -14.6, 0.2), (s * 1.5, -16.2, 0.2), 1.2, 1.3, mat=TUYERE, seg=10))
        for y in (-4.0, 1.0):
            m.add(missile(s * 1.6, y - 3.0, y + 3.0, -1.5, 0.5))
    m.add(verriere(5.5, 12.0, 1.2, 1.3, 1.4))
    return m


def avion_attaque():
    """Avion d'attaque au sol blindé (famille Su-25) : aile haute droite, nombreux points d'emport."""
    m = Mesh()
    peau = R.team(spec=0.3, tex=combine(panels(3.5, 0.3, 0.84), noise(97, 0.05)))
    aile = Mat((136, 146, 152), spec=0.3, tex=avec(camo(2.0), marque_rub(10.0, -2.2, 1.6), marque_rub(-10.0, -2.2, 1.6)))
    m.add(fuselage([(-12.0, 0.8, 1.0, 0.8), (-8.0, 1.6, 1.5, 0.6), (0.0, 2.2, 1.9, 0.4), (6.0, 2.0, 1.9, 0.2),
                    (10.0, 1.3, 1.4, 0.0), (12.4, 0.4, 0.5, -0.2)], peau, seg=14))
    for s in (1, -1):
        m.add(fuselage([(-6.0, 1.0, 1.0, 0.0), (-4.0, 1.5, 1.4, 0.0), (3.0, 1.5, 1.4, 0.0), (4.4, 1.2, 1.1, 0.0)], GRIS_FONCE, seg=10).move(dx=s * 2.4))
        w = wing((3.2, 1.6), (3.2, -3.4), (14.4, -1.0), (14.4, -3.8), 0.4, aile, thick=0.8, dihedral=-0.6)
        m.add(w if s > 0 else w.mirror_x())
        t = wing((0.6, -9.0), (0.6, -12.0), (5.6, -10.4), (5.6, -12.2), 1.6, aile, thick=0.4)
        m.add(t if s > 0 else t.mirror_x())
        for k, x in enumerate((5.4, 7.8, 10.2, 12.4)):
            mat = Mat((96, 104, 80), spec=0.3) if k % 2 == 0 else Mat((190, 190, 186), spec=0.3)
            m.add(fuselage([(-3.4, 0.1, 0.1, -0.9), (-2.6, 0.5, 0.5, -0.9), (1.4, 0.5, 0.5, -0.9), (2.2, 0.1, 0.1, -0.9)], mat, seg=6).move(dx=s * x))
    pts = [(-12.6, 0.8), (-8.4, 0.8), (-10.8, 6.0), (-12.8, 6.2)]
    m.add(R.loft([[(dx, y, z) for y, z in pts] for dx in (-0.3, 0.3)], peau))
    m.add(verriere(5.0, 9.4, 1.2, 1.3, 1.6))
    m.add(tube((0.8, 11.0, -0.8), (0.8, 13.0, -0.8), 0.22, mat=ACIER_SOMBRE, seg=5))
    return m


def avion_reco():
    """Avion de reconnaissance haute altitude (famille M-55) : très grande envergure, bipoutre."""
    m = Mesh()
    peau = R.team(spec=0.35, tex=panels(3.5, 0.3, 0.86))
    gris = Mat((160, 164, 166), spec=0.35, tex=avec(panels(4.0, 0.3, 0.88), marque_rub(13.0, 0.0, 1.4), marque_rub(-13.0, 0.0, 1.4)))
    m.add(fuselage([(-4.0, 1.4, 1.2, 0.3), (-1.0, 1.8, 1.6, 0.4), (5.0, 1.6, 1.4, 0.3), (8.4, 0.9, 0.9, 0.2), (9.6, 0.2, 0.3, 0.1)], peau, seg=12))
    m.add(verriere(3.2, 7.0, 1.0, 1.1, 1.3))
    m.add(tube((0, -4.2, 0.4), (0, -5.2, 0.4), 1.0, 0.8, mat=TUYERE, seg=10))
    for s in (1, -1):
        w = wing((1.4, 1.8), (1.4, -1.2), (19.0, 0.8), (19.0, -0.6), 0.6, gris, thick=0.5, dihedral=0.8)
        m.add(w if s > 0 else w.mirror_x())
        m.add(fuselage([(-12.0, 0.3, 0.3, 0.6), (-8.0, 0.55, 0.55, 0.6), (1.0, 0.55, 0.55, 0.6), (2.4, 0.3, 0.3, 0.6)], gris, seg=8).move(dx=s * 3.4))
        pts = [(-12.6, 0.8), (-9.8, 0.8), (-11.0, 4.4), (-12.6, 4.6)]
        m.add(R.loft([[(s * 3.4 + dx, y, z) for y, z in pts] for dx in (-0.2, 0.2)], peau))
    m.add(box(-3.6, -12.4, 4.2, 3.6, -10.8, 4.5, gris))
    m.add(fuselage([(-1.6, 0.4, 0.4, -1.2), (-0.6, 0.9, 0.7, -1.2), (2.6, 0.9, 0.7, -1.2), (3.6, 0.4, 0.4, -1.2)], GRIS_FONCE, seg=8))
    return m


# ---------------------------------------------------------------------------
# Marine
# ---------------------------------------------------------------------------
GRIS_RUB = Mat((110, 118, 116), spec=0.35, shine=20, tex=combine(panels(5.0, 0.3, 0.88), noise(101, 0.05)))


def _coque(longueur, largeur, franc_bord, **kw):
    from modeles.australouis import coque
    return coque(longueur, largeur, franc_bord, side_mat=GRIS_RUB, **kw)


def escorteur():
    """Escorteur (corvette) : lance-missiles antinavires obliques, mât radar, hangar."""
    m, _ = _coque(46.0, 8.8, 2.6)
    peau = R.team(spec=0.35, tex=combine(panels(3.0, 0.3, 0.86), noise(102, 0.04)))
    m.add(prism(rrect(-3.2, -10.0, 3.2, 4.0, 1.0), 2.6, 5.6, peau, bevel=0.3))
    m.add(prism(rrect(-2.4, -1.0, 2.4, 3.6, 0.8), 5.6, 7.8, peau, bevel=0.3))
    m.add(box(-2.3, 3.2, 6.6, 2.3, 3.7, 7.4, Mat((40, 64, 88), spec=1.1, shine=40)))
    m.add(tube((0, 0.8, 7.8), (0, 0.8, 12.4), 0.3, mat=ACIER_SOMBRE, seg=5))
    m.add(box(-2.0, 0.4, 10.6, 2.0, 1.2, 11.6, Mat((170, 174, 166), spec=0.4)))
    m.add(sphere((0, 0.8, 12.8), 0.8, Mat((222, 224, 220), spec=0.4), seg=8, rings=5))
    # conteneurs de missiles antinavires inclinés (4 de chaque bord)
    for s in (1, -1):
        for k in range(2):
            m.add(box(-0.8, -1.8, -0.5, 0.8, 1.8, 0.5, Mat((96, 104, 90), spec=0.2)).rot_x(-20).move(dx=s * (2.0 + k * 1.7), dy=-14.0, dz=3.8))
    m.add(box(-3.2, -21.0, 2.62, 3.2, -15.0, 2.7, Mat((70, 74, 72), spec=0.1,
                                                    tex=lambda p, n: (230, 230, 220) if abs(math.hypot(p[0], p[1] + 18.0) - 1.9) < 0.3 else 1.0)))
    m.add(montagne(0, 8.0, 2.62, 1.0))
    return m


def escorteur_tourelle():
    peau = R.team(spec=0.4, tex=panels(2.0, 0.25, 0.86))
    m = Mesh()
    m.add(prism([(-1.8, -1.8), (1.8, -1.8), (1.8, 0.6), (0.9, 2.0), (-0.9, 2.0), (-1.8, 0.6)], 0.0, 1.6, peau, bevel=0.6))
    m.add(tube((0, 1.6, 0.9), (0, 5.6, 0.9), 0.32, mat=ACIER, seg=6))
    return m


def fregate():
    """Frégate lance-missiles : deux tourelles, silos verticaux, grande superstructure radar."""
    m, _ = _coque(62.0, 11.0, 3.0)
    peau = R.team(spec=0.35, tex=combine(panels(3.0, 0.3, 0.86), noise(103, 0.04)))
    m.add(prism(rrect(-4.0, -12.0, 4.0, 8.0, 1.2), 3.0, 6.4, peau, bevel=0.4))
    m.add(prism(rrect(-3.0, -2.0, 3.0, 6.0, 1.0), 6.4, 9.4, peau, bevel=0.4))
    m.add(box(-2.9, 5.6, 8.2, 2.9, 6.1, 9.0, Mat((40, 64, 88), spec=1.1, shine=40)))
    # radars à faces planes sur la tour
    for s in (1, -1):
        m.add(box(s * 3.05 - 0.1, 0.0, 7.0, s * 3.05 + 0.1, 3.0, 9.0, Mat((170, 174, 166), spec=0.4)))
    m.add(tube((0, 2.0, 9.4), (0, 2.0, 14.0), 0.3, mat=ACIER_SOMBRE, seg=5))
    m.add(box(-2.6, 1.6, 12.4, 2.6, 2.4, 12.8, ACIER_SOMBRE))
    m.add(R.prism(rrect(-1.2, -9.6, 1.2, -6.4, 0.5), 6.4, 9.0, GRIS_RUB, bevel=0.2))
    # silos de lancement vertical à l'avant (grille de trappes)
    for k in range(12):
        x, y = (k % 3) * 1.4 - 1.4, 13.0 + (k // 3) * 1.4
        m.add(box(x - 0.55, y - 0.55, 3.0, x + 0.55, y + 0.55, 3.2, Mat((70, 74, 72), spec=0.1)))
    m.add(montagne(0, -20.0, 3.02, 1.2))
    return m


def fregate_tourelle():
    peau = R.team(spec=0.4, tex=panels(2.0, 0.25, 0.86))
    m = Mesh()
    m.add(prism([(-2.2, -2.4), (2.2, -2.4), (2.2, 0.8), (1.1, 2.6), (-1.1, 2.6), (-2.2, 0.8)], 0.0, 2.0, peau, bevel=0.7))
    for x in (-0.6, 0.6):
        m.add(tube((x, 2.0, 1.1), (x, 7.4, 1.1), 0.32, mat=ACIER, seg=6))
    return m


def sous_marin():
    """Sous-marin d'attaque : coque en goutte d'eau, kiosque bas, gouvernail en X."""
    m = Mesh()
    noir = Mat((50, 54, 56), spec=0.35, shine=22, tex=combine(panels(4.0, 0.2, 0.9, axis="y"), noise(104, 0.05)))
    m.add(fuselage([(-20.0, 0.3, 0.3, 0.6), (-16.0, 1.8, 1.3, 0.9), (-8.0, 3.0, 2.0, 1.1), (8.0, 3.0, 2.0, 1.1),
                    (15.0, 2.4, 1.7, 1.0), (19.0, 1.0, 0.8, 0.8), (20.4, 0.3, 0.3, 0.6)], noir, seg=16))
    m.add(R.prism([(-0.8, 3.0), (0.8, 3.0), (0.9, 6.4), (0.5, 8.0), (-0.5, 8.0), (-0.9, 6.4)], 2.0, 5.0, noir, bevel=0.4))
    m.add(box(-2.8, 5.4, 3.8, 2.8, 6.4, 4.0, noir))
    m.add(tube((0.2, 6.0, 5.0), (0.2, 6.0, 6.3), 0.15, mat=ACIER, seg=4))
    for a in (45, -45):
        m.add(box(-3.4, -18.4, -0.12, 3.4, -17.0, 0.12, noir).rot_y(a).move(dz=0.9))
    m.add(montagne(0, 6.0, 5.02, 0.6))
    m.add(sillage([(2.6, -10), (2.6, 9), (1.8, 16), (0.3, 20), (-0.3, 20), (-1.8, 16), (-2.6, 9), (-2.6, -10), (-1.2, -17), (1.2, -17)], largeur=0.8))
    return m.move(dz=0.6)


def porte_avions():
    """Porte-avions à pont oblique et tremplin de décollage (famille Kouznetsov)."""
    m, poly = _coque(92.0, 22.0, 4.2, proue=0.2, tonture=1.6)
    peau = R.team(spec=0.35, tex=combine(panels(4.0, 0.3, 0.86), noise(105, 0.05)))

    def pont(p, n):
        if abs(p[0] - (p[1] - 10) * 0.12) < 0.25 and p[1] < 30:
            return (224, 196, 70)
        if abs(p[0]) < 0.25 and p[1] > 20:
            return (230, 230, 220)
        if abs(p[1] % 10.0) < 0.2:
            return (200, 200, 190)
        return 0.86 if (p[0] / 2.5) % 1.0 < 0.06 else 1.0
    deck = Mat((74, 78, 80), spec=0.1, tex=pont)
    # pont d'envol débordant (oblique à bâbord) et tremplin relevé à la proue
    pts = [(-11.0, -44.0), (11.0, -44.0), (11.0, 30.0), (6.0, 44.0), (-6.0, 44.0), (-14.0, 14.0), (-14.0, -10.0)]
    m.add(prism(pts, 4.2, 4.8, deck))
    m.add(prism([(-6.0, 34.0), (6.0, 34.0), (6.0, 44.0), (-6.0, 44.0)], 4.8, 5.0, deck).map(
        lambda p: (p[0], p[1], p[2] + (max(0.0, p[1] - 34.0) * 0.25 if p[2] > 4.9 else 0.0))))
    # îlot tribord aux couleurs du joueur, radars et cheminée
    m.add(prism(rrect(7.0, -12.0, 10.6, 6.0, 1.0), 4.8, 12.0, peau, bevel=0.4))
    m.add(prism(rrect(7.6, -8.0, 10.0, 2.0, 0.8), 12.0, 15.0, peau, bevel=0.3))
    m.add(box(7.2, 5.6, 10.0, 10.4, 6.1, 11.4, Mat((40, 64, 88), spec=1.1, shine=40)))
    m.add(tube((8.8, -3.0, 15.0), (8.8, -3.0, 19.0), 0.35, mat=ACIER_SOMBRE, seg=5))
    m.add(sphere((8.8, -3.0, 19.6), 1.1, Mat((222, 224, 220), spec=0.4), seg=10, rings=6))
    m.add(box(7.0, -10.0, 14.0, 10.6, -9.4, 16.0, Mat((170, 174, 166), spec=0.4)))
    m.add(prism(rrect(8.0, -12.6, 10.2, -10.4, 0.4), 12.0, 14.6, GRIS_RUB, bevel=0.2))
    # ascenseurs, brins d'arrêt
    for y in (-30.0, -22.0):
        m.add(box(-9.0, y, 4.82, -3.0, y + 5.0, 4.9, Mat((64, 66, 68), spec=0.1)))
    # pas d'avions dessinés sur le pont : les vrais chasseurs s'y posent (AircraftCarrier)
    for s in (1, -1):
        m.add(tube((s * 9.6, 26.0, 4.8), (s * 9.6, 26.0, 6.2), 0.9, mat=Mat((210, 212, 210), spec=0.4), seg=8))
    m.add(montagne(0, -36.0, 4.82, 2.0))
    return m
