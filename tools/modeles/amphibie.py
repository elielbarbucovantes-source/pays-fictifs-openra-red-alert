"""Logistique amphibie : Base logistique amphibie (3×3, quai sur l'eau au sud), Barcaza (Elielistan),
chaland lourd (Rubénie). Unités : 1 case = 24 px ; y positif = nord (haut de l'écran)."""
import math
import rendu3d as R
from rendu3d import Mesh, Mat, prism, box, tube, sphere, rrect, panels, noise, combine, stripes
from modeles.communs import ACIER, ACIER_SOMBRE, GRILLE, VITRE, FEU_ROUGE, FEU_BLANC, BACHE, BOIS, antenne
from modeles.australouis import coque
from modeles.ferroviaire import BETON, BETON_SOMBRE, TOIT, TOLE, JAUNE, NOIR, SOMBRE

BOIS_QUAI = Mat((112, 86, 58), spec=0.05, tex=combine(stripes(1.1, 0.8, axis=0), noise(401, 0.06)))
PIEU = Mat((70, 56, 40), spec=0.05)
CONTENEURS = [Mat(c, spec=0.2, tex=stripes(0.8, 0.82, axis=0)) for c in ((150, 60, 40), (50, 90, 130), (190, 150, 50), (70, 110, 70))]
OLIVE = Mat((92, 96, 62), spec=0.2, tex=combine(panels(4.0, 0.3, 0.85), noise(402, 0.06)))
RADIER = Mat((70, 74, 72), spec=0.1, tex=stripes(2.0, 0.88, axis=1))


def _peau(seed, step=4.0):
    return R.team(spec=0.3, shine=20, tex=combine(panels(step, 0.35, 0.84), noise(seed, 0.04)))


# ---------------------------------------------------------------------------
# Base logistique amphibie (3×3) : à terre (y > -12) hangar, tour de contrôle, aire
# d'embarquement balisée, conteneurs ; sur l'eau (y < -12) quai sur pieux et deux appontements.
# ---------------------------------------------------------------------------
def base_amphibie(t=0.0):
    peau = _peau(403)
    m = Mesh()
    m.add(prism(rrect(-36, -12, 36, 36, 1.0), 0.0, 1.0, BETON_SOMBRE))
    # aire d'embarquement : bandes jaunes en chevrons
    m.add(prism(rrect(-34, -11, 8, 6, 0.4), 1.0, 1.1, BETON))
    for k in range(7):
        x = -32 + k * 6
        m.add(box(x, -10.5, 1.1, x + 2.4, -9.6, 1.18, JAUNE))
        m.add(box(x, 4.6, 1.1, x + 2.4, 5.5, 1.18, JAUNE))
    # rampe de mise à l'eau au milieu du quai
    m.add(R.loft([[(-6, -12, 1.0), (6, -12, 1.0)], [(-6, -20, 0.05), (6, -20, 0.05)]], BETON, cap0=False, cap1=False))
    # hangar logistique (nord-ouest) : toit cintré aux couleurs, portes ouvertes au sud
    m.add(prism(rrect(-34, 10, 4, 34, 0.6), 1.0, 13.0, TOLE, bevel=0.3))
    arc = [[(-35, 10 + 24 * (1 - math.cos(a)) / 2, 13.0 + 6.0 * math.sin(a)), (5, 10 + 24 * (1 - math.cos(a)) / 2, 13.0 + 6.0 * math.sin(a))]
           for a in [i * math.pi / 8 for i in range(9)]]
    m.add(R.loft(arc, peau, cap0=False, cap1=False))
    for x in (-24, -8):
        m.add(box(x - 6, 9.7, 1.0, x + 6, 9.9, 10.5, SOMBRE))
        m.add(box(x - 6.6, 9.4, 1.0, x - 6, 10.0, 11.0, JAUNE))
        m.add(box(x + 6, 9.4, 1.0, x + 6.6, 10.0, 11.0, JAUNE))
    # tour de contrôle des opérations (nord-est) : fût, vigie vitrée, radar
    m.add(prism(rrect(14, 16, 26, 28, 1.0), 1.0, 8.0, BETON, bevel=0.3))
    m.add(prism(rrect(17, 19, 23, 25, 0.6), 8.0, 20.0, BETON, bevel=0.2))
    m.add(prism(rrect(15.5, 17.5, 24.5, 26.5, 0.8), 20.0, 24.0, peau, bevel=0.3))
    for (x0, y0, x1, y1) in ((15.4, 17.4, 24.6, 17.6), (15.4, 26.4, 24.6, 26.6), (15.4, 17.4, 15.6, 26.6), (24.4, 17.4, 24.6, 26.6)):
        m.add(box(x0, y0, 21.0, x1, y1, 23.2, VITRE))
    m.add(prism(rrect(16, 18, 24, 26, 0.5), 24.0, 24.6, ACIER_SOMBRE))
    m.add(tube((20, 22, 24.6), (20, 22, 27.0), 0.3, mat=ACIER_SOMBRE, seg=5))
    m.add(box(17.0, 21.6, 27.0, 23.0, 22.4, 28.4, ACIER))
    m.add(antenne(22.5, 25.0, 24.6, 5.0))
    m.add(sphere((16.2, 17.6, 24.8), 0.5, FEU_ROUGE, seg=5, rings=3))
    # conteneurs empilés (est) et caisses sur l'aire
    for i, (x, y, z) in enumerate(((12, 4, 1.0), (12, -3, 1.0), (24, 4, 1.0), (12, 4, 5.0), (24, -3, 1.0), (24, 4, 5.0))):
        m.add(box(x, y, z, x + 10, y + 5.4, z + 4.0, CONTENEURS[i % 4], bevel=0.2))
    for x, y in ((-28, -6), (-20, 0), (-12, -6), (-4, 0)):
        m.add(box(x, y, 1.1, x + 3.4, y + 3.4, 3.2, BACHE, bevel=0.3))
    # quai sur pieux (bord de l'eau) et deux appontements vers le large
    m.add(box(-36, -16, 0.6, 36, -12, 1.4, BOIS_QUAI))
    for x in range(-34, 35, 6):
        m.add(tube((x, -15.5, 0.0), (x, -15.5, 0.6), 0.6, mat=PIEU, seg=6))
    for x in (-24, 24):
        m.add(box(x - 2.4, -36, 0.6, x + 2.4, -16, 1.3, BOIS_QUAI))
        for y in range(-34, -16, 5):
            for s in (-1, 1):
                m.add(tube((x + s * 2.0, y, 0.0), (x + s * 2.0, y, 0.6), 0.5, mat=PIEU, seg=6))
        m.add(sphere((x, -35, 2.0), 0.5, FEU_BLANC, seg=5, rings=3))
        m.add(tube((x, -35, 1.3), (x, -35, 2.0), 0.2, mat=ACIER_SOMBRE, seg=4))
    # grue de quai (portique) au-dessus de la rampe
    for x in (-10, 10):
        m.add(box(x - 0.8, -14.5, 1.4, x + 0.8, -13.0, 16.0, JAUNE))
    m.add(box(-11, -16, 16.0, 11, -11.5, 17.6, JAUNE))
    m.add(box(-2, -14.5, 12.0, 2, -13.0, 16.0, ACIER_SOMBRE))
    m.add(box(-34, -12.6, 1.0, 36, -12.0, 1.6, NOIR))
    return m


# ---------------------------------------------------------------------------
# Barcaza (Elielistan) : péniche de débarquement rapide, coque courte, rampe avant.
# ---------------------------------------------------------------------------
def barcaza():
    m, _ = coque(30.0, 8.0, 1.8, proue=0.06, poupe=0.1, tonture=0.0)
    p = _peau(404, 3.0)
    m.add(box(-3.4, -6.0, 1.8, 3.4, 12.0, 1.9, RADIER))
    for s in (1, -1):
        m.add(box(s * 3.4 - (0.5 if s > 0 else -0.5), -6.0, 1.8, s * 3.9, 12.0, 3.6, p))
    m.add(box(-3.4, 12.0, 1.8, 3.4, 13.0, 3.8, p, bevel=0.2))                      # rampe relevée
    m.add(prism(rrect(-3.8, -14.6, 3.8, -6.0, 0.8), 1.8, 4.6, p, bevel=0.3))
    m.add(prism(rrect(-2.4, -12.0, 2.4, -7.6, 0.6), 4.6, 6.6, p, bevel=0.3))
    m.add(box(-2.3, -7.9, 5.4, 2.3, -7.5, 6.3, VITRE))
    m.add(antenne(1.2, -11.0, 6.6, 3.0))
    # deux jeeps sous bâche
    for y in (-2.0, 6.0):
        m.add(box(-2.0, y - 2.6, 1.9, 2.0, y + 2.6, 3.6, OLIVE, bevel=0.4))
        m.add(box(-1.8, y - 1.0, 3.6, 1.8, y + 2.2, 4.4, BACHE, bevel=0.3))
    return m


# ---------------------------------------------------------------------------
# Chaland lourd (Rubénie) : grand bâtiment de débarquement, radier ouvert, mitrailleuses.
# ---------------------------------------------------------------------------
def chaland():
    m, _ = coque(46.0, 12.0, 2.6, proue=0.08, poupe=0.1, tonture=0.0)
    p = _peau(405, 3.5)
    m.add(box(-5.0, -8.0, 2.6, 5.0, 19.0, 2.7, RADIER))
    for s in (1, -1):
        m.add(box(s * 5.0 - (0.7 if s > 0 else -0.7), -8.0, 2.6, s * 5.7, 19.0, 5.4, p))
        # mitrailleuses sur les flancs
        m.add(box(s * 5.35 - 0.6, 14.0, 5.4, s * 5.35 + 0.6, 15.2, 6.6, ACIER_SOMBRE))
        m.add(tube((s * 5.35, 15.2, 6.1), (s * 5.35, 18.0, 6.1), 0.18, mat=ACIER_SOMBRE, seg=4))
    m.add(box(-5.0, 19.0, 2.6, 5.0, 20.2, 5.6, p, bevel=0.2))
    m.add(prism(rrect(-5.6, -22.4, 5.6, -8.0, 1.0), 2.6, 7.6, p, bevel=0.4))
    m.add(prism(rrect(-3.8, -18.0, 3.8, -10.0, 0.8), 7.6, 11.0, p, bevel=0.4))
    m.add(box(-3.7, -10.3, 9.4, 3.7, -9.8, 10.6, VITRE))
    m.add(tube((0, -14.0, 11.0), (0, -14.0, 15.0), 0.3, mat=ACIER_SOMBRE, seg=5))
    m.add(box(-2.2, -14.4, 14.2, 2.2, -13.6, 14.8, ACIER))
    m.add(tube((-2.4, -19.0, 7.6), (-2.4, -19.0, 10.4), 0.9, mat=NOIR, seg=8))
    # cargaison : deux chars et une section d'infanterie
    for y in (-3.0, 7.0):
        m.add(box(-3.2, y - 3.6, 2.7, 3.2, y + 3.6, 4.6, OLIVE, bevel=0.5))
        m.add(prism(rrect(-1.8, y - 1.6, 1.8, y + 1.8, 0.8), 4.6, 5.8, OLIVE, bevel=0.4))
        m.add(tube((0, y + 1.0, 5.2), (0, y + 5.4, 5.2), 0.3, mat=ACIER_SOMBRE, seg=5))
    for x in (-3.0, -1.0, 1.0, 3.0):
        m.add(box(x - 0.5, 14.5, 2.7, x + 0.5, 15.5, 4.2, Mat((96, 100, 72), spec=0.1)))
    m.add(box(-4.6, 17.0, 2.7, 4.6, 18.4, 3.4, BOIS))
    return m
