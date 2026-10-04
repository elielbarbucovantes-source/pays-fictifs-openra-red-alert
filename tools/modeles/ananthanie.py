"""Modèles 3D de l'Ananthanie : matériel de superpuissance technologique. Blindés modernes
anguleux (glacis très incliné, jupes, tourelles en coin), capteurs et brouilleurs partout,
drones et missiles. Insigne : rose des vents-étoile (vert citron, vert pomme, blanc, jaune)."""
import math
import rendu3d as R
from rendu3d import Mesh, Mat, merge, prism, box, tube, sphere, rrect, ngon, panels, noise, combine, stripes, wing
from modeles.communs import (blinde, chenilles, canon, antenne, trappe, roue, ACIER, ACIER_SOMBRE, GRILLE, CAOUTCHOUC,
                             VITRE, FEU_ROUGE, fuselage, verriere, missile, TUYERE)

CITRON = (126, 211, 33)
POMME = (52, 201, 36)
SOLEIL = (253, 184, 39)
BLANC = (236, 238, 232)
CAPTEUR = Mat((40, 44, 50), spec=1.0, shine=40)              # optiques, radars à antennes plates
COMPOSITE = Mat((96, 100, 92), spec=0.2, tex=combine(panels(2.0, 0.25, 0.82), noise(301, 0.05)))
LUEUR = Mat((150, 240, 90), emit=True)                        # voyants vert citron
PHARE = Mat((230, 230, 210), spec=0.8, shine=30)


def peau(seed, step=4.0):
    """Tôle équipe aux lignes de panneaux nettes (blindage modulaire)."""
    return R.team(spec=0.35, shine=22, tex=combine(panels(step, 0.35, 0.8), noise(seed, 0.035)))


def rose(x, y, z, k=1.0):
    """Insigne posé à plat : étoile à quatre branches (blanc / vert pomme) et cœur jaune."""
    m = Mesh()
    for i in range(4):
        a = math.pi / 2 * i
        c, s = math.cos(a), math.sin(a)
        tip = (x + c * 1.4 * k, y + s * 1.4 * k)
        g = (x - s * 0.32 * k, y + c * 0.32 * k)
        d = (x + s * 0.32 * k, y - c * 0.32 * k)
        m.add(prism([(x, y), d, tip], z, z + 0.08, Mat(BLANC, spec=0.2)))
        m.add(prism([(x, y), tip, g], z, z + 0.08, Mat(POMME, spec=0.2)))
    m.add(prism(ngon(x, y, 0.32 * k, 8), z + 0.08, z + 0.14, Mat(SOLEIL, spec=0.3)))
    return m


def jupes(m, L, x, z0, z1, mat, y0=None):
    """Jupes latérales modulaires (plaques séparées) des deux côtés."""
    y0 = -L / 2 if y0 is None else y0
    n = int(L / 3.2)
    for s in (1, -1):
        for i in range(n):
            ya = y0 + i * L / n
            m.add(box(min(s * x, s * (x + 0.45)), ya + 0.08, z0, max(s * x, s * (x + 0.45)), ya + L / n - 0.08, z1, mat, bevel=0.1))
    return m


def capteurs_aps(m, pts, z):
    """Capteurs et lanceurs de la protection active : petits blocs sombres aux angles."""
    for x, y, a in pts:
        b = box(-0.7, -0.5, 0.0, 0.7, 0.5, 1.1, CAPTEUR, bevel=0.15).rot_z(a)
        m.add(b.move(x, y, z))
    return m


def periscope(x, y, z, h=1.6, r=0.7):
    """Viseur panoramique du chef (tête tournante à optique sombre)."""
    return merge(tube((x, y, z), (x, y, z + h), r * 0.6, mat=ACIER_SOMBRE, seg=8),
                 box(x - r, y - r, z + h, x + r, y + r, z + h + r * 1.4, ACIER_SOMBRE, bevel=0.2),
                 box(x - r * 0.7, y + r - 0.05, z + h + 0.2, x + r * 0.7, y + r + 0.1, z + h + r * 1.2, CAPTEUR))


def tourelleau_rws(x, y, z, k=1.0):
    """Tourelleau téléopéré : boîte de capteurs et mitrailleuse."""
    m = Mesh()
    m.add(box(x - 0.9 * k, y - 0.9 * k, z, x + 0.9 * k, y + 0.9 * k, z + 1.2 * k, ACIER_SOMBRE, bevel=0.2))
    m.add(box(x - 0.6 * k, y + 0.8 * k, z + 0.4 * k, x + 0.6 * k, y + 1.0 * k, z + 1.0 * k, CAPTEUR))
    m.add(tube((x + 0.5 * k, y, z + 0.7 * k), (x + 0.5 * k, y + 3.0 * k, z + 0.7 * k), 0.15 * k, mat=ACIER_SOMBRE, seg=4))
    return m


# ---------------------------------------------------------------------------
# Vidra : char léger de percée, bas et effilé, canon-mitrailleur de 40 mm
# ---------------------------------------------------------------------------
def vidra_caisse():
    p = peau(311, 3.5)
    m = Mesh()
    prof = [(-4.0, -8.6), (4.0, -8.6), (4.0, 5.0), (2.4, 8.8), (-2.4, 8.8), (-4.0, 5.0)]
    m.add(prism(prof, 1.4, 3.5, p, bevel=1.2))
    tr = chenilles(16.8, 1.8, 3.3, 4.9, 0.0, wheels=5)
    m.add(tr).add(tr.mirror_x())
    jupes(m, 15.0, 5.8, 1.6, 3.3, p, y0=-7.2)
    m.add(box(-3.2, -8.4, 3.5, 3.2, -5.6, 3.8, GRILLE))
    for s in (1, -1):
        m.add(tube((s * 2.4, 8.5, 2.6), (s * 2.4, 8.9, 2.6), 0.35, mat=PHARE, seg=6))
    m.add(rose(-2.0, 4.0, 3.52, 0.8))
    return m


def vidra_tourelle():
    p = peau(312, 2.5)
    m = Mesh()
    m.add(prism([(-2.8, -3.0), (2.8, -3.0), (3.2, 0.6), (1.6, 3.2), (-1.6, 3.2), (-3.2, 0.6)], 0.0, 2.0, p, bevel=0.8))
    m.add(canon(2.4, 10.0, 1.1, 0.32, frein=False, manchon=(0.1, 0.35)))
    m.add(box(-2.6, -0.6, 1.0, -1.8, 1.6, 2.2, ACIER_SOMBRE))                          # lance-missile latéral
    m.add(tube((-2.2, 1.6, 1.6), (-2.2, 2.0, 1.6), 0.35, mat=CAPTEUR, seg=6))
    m.add(periscope(1.2, -1.2, 2.0, 1.0, 0.55))
    m.add(antenne(-2.0, -2.6, 2.0, 5.0))
    return m


# ---------------------------------------------------------------------------
# Lunkra : char de combat principal (famille Leopard 2A7) — tourelle en coin
# ---------------------------------------------------------------------------
def lunkra_caisse():
    p = peau(313)
    m = Mesh()
    prof = [(-4.8, -10.4), (4.8, -10.4), (4.8, 6.4), (3.6, 10.6), (-3.6, 10.6), (-4.8, 6.4)]
    m.add(prism(prof, 1.5, 4.1, p, bevel=1.3))
    tr = chenilles(20.4, 2.1, 3.9, 5.9, 0.0, wheels=7)
    m.add(tr).add(tr.mirror_x())
    jupes(m, 18.0, 6.9, 1.8, 4.0, p, y0=-8.4)
    m.add(box(-4.6, -10.2, 4.1, 4.6, -6.4, 4.5, GRILLE))
    m.add(box(-4.2, -10.9, 2.0, 4.2, -10.3, 3.6, COMPOSITE))                           # bac arrière
    for s in (1, -1):
        m.add(tube((s * 3.4, 10.5, 3.0), (s * 3.4, 10.9, 3.0), 0.4, mat=PHARE, seg=6))
        m.add(box(s * 2.2 - 0.5, 9.2, 4.0, s * 2.2 + 0.5, 10.0, 4.4, CAPTEUR))
    m.add(rose(3.2, -3.0, 4.12, 1.0))
    return m


def lunkra_tourelle():
    p = peau(314, 3.0)
    m = Mesh()
    # tourelle à blindage en coin (flèche vers l'avant)
    m.add(prism([(-4.0, -4.6), (4.0, -4.6), (4.4, 1.6), (3.4, 4.4), (0.0, 6.2), (-3.4, 4.4), (-4.4, 1.6)], 0.0, 2.8, p, bevel=0.9))
    m.add(box(-3.6, -6.6, 0.4, 3.6, -4.4, 2.4, COMPOSITE, bevel=0.3))                  # panier arrière
    m.add(canon(5.2, 16.4, 1.4, 0.48, manchon=(0.15, 0.6)))
    m.add(box(-1.4, 4.6, 0.6, 1.4, 6.0, 2.2, p, bevel=0.3))                            # masque
    m.add(periscope(2.2, -1.6, 2.8))
    m.add(box(-2.8, 1.6, 2.8, -1.4, 3.0, 3.6, ACIER_SOMBRE, bevel=0.2))                # viseur du tireur
    m.add(box(-2.6, 2.9, 3.0, -1.6, 3.05, 3.5, CAPTEUR))
    m.add(trappe(-1.8, -2.2, 2.75, 0.9))
    for s in (1, -1):
        for k in range(3):
            m.add(tube((s * 3.9, -3.6 + k * 0.7, 1.8), (s * 4.6, -3.4 + k * 0.7, 2.3), 0.22, mat=ACIER_SOMBRE, seg=5))
    m.add(antenne(-3.0, -4.0, 2.8, 6.5))
    m.add(antenne(3.0, -4.0, 2.8, 4.5))
    return m


# ---------------------------------------------------------------------------
# Karnvasha : char lourd de rupture, tourelle téléopérée et protection active
# ---------------------------------------------------------------------------
def karnvasha_caisse():
    p = peau(315, 4.5)
    m = Mesh()
    prof = [(-5.8, -12.4), (5.8, -12.4), (5.8, 8.0), (4.4, 12.6), (-4.4, 12.6), (-5.8, 8.0)]
    m.add(prism(prof, 1.7, 5.0, p, bevel=1.5))
    tr = chenilles(24.2, 2.4, 4.6, 7.0, 0.0, wheels=7)
    m.add(tr).add(tr.mirror_x())
    jupes(m, 21.6, 8.2, 2.0, 4.8, p, y0=-10.4)
    # blindage additionnel du glacis et de la caisse
    m.add(prism([(-4.6, 7.4), (4.6, 7.4), (3.8, 12.0), (-3.8, 12.0)], 5.0, 5.6, COMPOSITE, bevel=0.3))
    m.add(box(-5.4, -12.2, 5.0, 5.4, -8.0, 5.5, GRILLE))
    capteurs_aps(m, [(5.2, 9.0, 30), (-5.2, 9.0, -30), (5.2, -11.0, 150), (-5.2, -11.0, -150)], 5.0)
    for s in (1, -1):
        m.add(tube((s * 4.2, 12.5, 3.6), (s * 4.2, 12.9, 3.6), 0.45, mat=PHARE, seg=6))
    m.add(rose(-3.6, -4.0, 5.02, 1.2))
    return m


def karnvasha_tourelle():
    p = peau(316, 3.0)
    m = Mesh()
    # tourelle inhabitée basse et facettée
    m.add(prism([(-4.6, -5.6), (4.6, -5.6), (5.0, 2.0), (3.0, 5.6), (-3.0, 5.6), (-5.0, 2.0)], 0.0, 3.0, p, bevel=1.2))
    m.add(canon(5.0, 18.4, 1.5, 0.56, manchon=(0.15, 0.55)))
    m.add(box(-1.6, 4.8, 0.4, 1.6, 6.4, 2.6, p, bevel=0.3))
    # protection active : radars plats sur les faces et lanceurs en couronne
    for s in (1, -1):
        m.add(box(s * 4.4 - 0.15, -1.0, 1.0, s * 4.4 + 0.15, 1.6, 2.6, CAPTEUR).rot_z(s * 8))
    capteurs_aps(m, [(3.6, 3.6, 40), (-3.6, 3.6, -40), (3.8, -4.8, 140), (-3.8, -4.8, -140)], 3.0)
    m.add(periscope(2.4, -2.0, 3.0, 1.8, 0.8))
    m.add(tourelleau_rws(-2.2, -2.2, 3.0, 1.0))
    m.add(antenne(-3.4, -4.8, 3.0, 7.0))
    m.add(antenne(3.4, -4.8, 3.0, 5.0))
    return m


# ---------------------------------------------------------------------------
# Ratha : véhicule de combat d'infanterie, tourelle à canon de 30 mm et missiles
# ---------------------------------------------------------------------------
def ratha_caisse():
    p = peau(317)
    m = Mesh()
    prof = [(-4.4, -10.0), (4.4, -10.0), (4.4, 6.0), (3.2, 10.0), (-3.2, 10.0), (-4.4, 6.0)]
    m.add(prism(prof, 1.5, 5.2, p, bevel=1.1))
    tr = chenilles(19.0, 1.9, 3.6, 5.4, 0.0, wheels=6)
    m.add(tr).add(tr.mirror_x())
    jupes(m, 17.0, 6.3, 1.8, 3.8, p, y0=-8.0)
    m.add(box(-3.0, -10.4, 1.6, 3.0, -10.0, 4.8, COMPOSITE))                            # rampe arrière
    for x in (-2.4, 2.4):
        m.add(box(x - 1.2, -8.6, 5.2, x + 1.2, -5.0, 5.5, ACIER_SOMBRE, bevel=0.2))   # trappes de toit
    for s in (1, -1):
        m.add(tube((s * 3.0, 9.9, 3.8), (s * 3.0, 10.3, 3.8), 0.4, mat=PHARE, seg=6))
    m.add(rose(-2.6, 6.6, 5.22, 0.9))
    return m


def ratha_tourelle():
    p = peau(318, 2.5)
    m = Mesh()
    m.add(prism([(-3.0, -2.8), (3.0, -2.8), (3.2, 1.0), (1.8, 3.0), (-1.8, 3.0), (-3.2, 1.0)], 0.0, 2.2, p, bevel=0.7))
    m.add(canon(2.6, 9.6, 1.2, 0.26, frein=False, manchon=(0.0, 0.3)))
    m.add(box(2.8, -1.6, 0.8, 4.2, 1.8, 2.2, ACIER_SOMBRE, bevel=0.2))                  # double lance-missile
    for z in (1.2, 1.8):
        m.add(tube((3.5, 1.7, z), (3.5, 2.0, z), 0.28, mat=CAPTEUR, seg=6))
    m.add(periscope(-1.6, -1.0, 2.2, 1.0, 0.6))
    m.add(antenne(-2.4, -2.4, 2.2, 5.0))
    return m


# ---------------------------------------------------------------------------
# Mukhar : lance-drones 8×8, conteneurs de munitions rôdeuses sur le plateau
# ---------------------------------------------------------------------------
def mukhar():
    p = peau(319)
    m = Mesh()
    m.add(prism(rrect(-3.6, 5.6, 3.6, 11.6, 1.2), 1.6, 6.0, p, bevel=0.8))             # cabine blindée
    m.add(box(-3.2, 11.1, 4.2, 3.2, 11.7, 5.4, VITRE))
    m.add(box(-3.4, -11.0, 1.4, 3.4, 5.6, 2.8, ACIER_SOMBRE))
    m.add(prism(rrect(-3.8, -11.4, 3.8, 5.4, 0.6), 2.8, 3.4, p))
    for s in (1, -1):
        for y in (-8.8, -5.4, 4.0, 7.6):
            m.add(roue(s * 3.5, y, 1.6, 1.6, 1.1, s))
        m.add(tube((s * 2.6, 11.5, 3.6), (s * 2.6, 11.9, 3.6), 0.4, mat=PHARE, seg=6))
    # caisson lanceur incliné : 2 × 2 tubes à couvercles vert citron
    lance = Mesh()
    lance.add(box(-3.2, -6.0, 0.0, 3.2, 6.0, 3.6, p, bevel=0.4))
    for x in (-1.6, 1.6):
        for z in (0.9, 2.7):
            lance.add(box(x - 1.3, 5.95, z - 0.8, x + 1.3, 6.15, z + 0.8, Mat(CITRON, spec=0.3)))
    m.add(lance.rot_x(18).move(0, -3.6, 3.6))
    # mât de liaison de données
    m.add(tube((2.6, 6.4, 6.0), (2.6, 6.4, 9.6), 0.25, mat=ACIER_SOMBRE, seg=4))
    m.add(box(1.8, 6.2, 9.6, 3.4, 6.6, 10.6, CAPTEUR))
    m.add(rose(-1.6, 8.4, 6.02, 0.9))
    return m


# Munition rôdeuse (famille Switchblade) : corps fin, ailes en X, hélice arrière.
def rodeuse():
    corps = Mat((214, 218, 206), spec=0.4, tex=noise(320, 0.04))
    m = fuselage([(-4.6, 0.25, 0.25, 0.0), (-3.6, 0.7, 0.7, 0.0), (2.8, 0.7, 0.7, 0.0), (4.4, 0.35, 0.35, 0.0), (5.0, 0.05, 0.05, 0.0)],
                 corps, seg=8)
    m.add(box(-0.5, 4.3, -0.3, 0.5, 4.6, 0.3, CAPTEUR))
    for a in (35, -35, 145, -145):
        w = box(0.0, -0.6, -0.08, 4.6, 0.6, 0.08, R.team(spec=0.3))
        m.add(w.rot_y(a).move(0, 1.4, 0))
    m.add(tube((0, -4.8, 0), (0, -4.9, 0), 1.4, mat=Mat((60, 60, 60), spec=0.2), seg=10))
    m.add(rose(0, 0.0, 0.72, 0.5))
    return m


# ---------------------------------------------------------------------------
# Agnar : lanceur de missiles de croisière, deux conteneurs relevés
# ---------------------------------------------------------------------------
def agnar():
    p = peau(321)
    m = Mesh()
    m.add(prism(rrect(-3.4, 6.4, 3.4, 12.0, 1.0), 1.6, 5.6, p, bevel=0.7))
    m.add(box(-3.0, 11.5, 4.0, 3.0, 12.05, 5.1, VITRE))
    m.add(box(-3.2, -12.4, 1.4, 3.2, 6.4, 2.8, ACIER_SOMBRE))
    m.add(prism(rrect(-3.6, -12.6, 3.6, 6.2, 0.6), 2.8, 3.4, p))
    for s in (1, -1):
        for y in (-10.0, -7.0, 4.6, 7.8):
            m.add(roue(s * 3.4, y, 1.6, 1.6, 1.1, s))
        m.add(tube((s * 2.4, 11.9, 3.4), (s * 2.4, 12.3, 3.4), 0.4, mat=PHARE, seg=6))
    # deux conteneurs-lanceurs carrés, relevés vers l'arrière
    lance = Mesh()
    for x in (-1.6, 1.6):
        lance.add(box(x - 1.4, -9.0, 0.0, x + 1.4, 7.0, 2.6, p, bevel=0.3))
        lance.add(box(x - 1.1, 6.95, 0.3, x + 1.1, 7.15, 2.3, Mat(CITRON, spec=0.3)))
    m.add(lance.rot_x(14).move(0, -2.0, 3.4))
    m.add(antenne(-2.8, 7.0, 5.6, 5.0))
    m.add(rose(1.4, 9.0, 5.62, 0.9))
    return m


# ---------------------------------------------------------------------------
# Ambarkesh : DCA chenillée, tourelle à canons et missiles, radar de veille
# ---------------------------------------------------------------------------
def ambarkesh_caisse():
    p = peau(322, 3.5)
    m = Mesh()
    prof = [(-4.2, -9.0), (4.2, -9.0), (4.2, 6.0), (3.0, 9.0), (-3.0, 9.0), (-4.2, 6.0)]
    m.add(prism(prof, 1.4, 4.0, p, bevel=0.9))
    tr = chenilles(17.8, 1.9, 3.7, 5.1, 0.0, wheels=6)
    m.add(tr).add(tr.mirror_x())
    jupes(m, 15.6, 6.0, 1.7, 3.6, p, y0=-7.6)
    m.add(box(-3.4, -8.8, 4.0, 3.4, -5.4, 4.4, GRILLE))
    m.add(rose(2.6, 5.8, 4.02, 0.8))
    return m


def ambarkesh_tourelle():
    p = peau(323, 2.5)
    m = Mesh()
    m.add(prism(rrect(-3.0, -3.0, 3.0, 2.8, 0.9), 0.0, 2.6, p, bevel=0.5))
    for s in (1, -1):
        m.add(tube((s * 3.4, 0.4, 1.4), (s * 3.4, 7.0, 1.9), 0.24, mat=ACIER_SOMBRE, seg=5))     # canons latéraux
        m.add(box(s * 3.0 - (0.0 if s > 0 else 1.0), -1.4, 1.0, s * 3.0 + (1.0 if s > 0 else 0.0), 1.4, 2.0, p))
        for z in (2.3, 3.3):                                                                   # missiles
            m.add(tube((s * 2.0, -2.0, z), (s * 2.0, 3.0, z), 0.42, mat=Mat((222, 224, 216), spec=0.4), seg=6))
            m.add(tube((s * 2.0, 3.0, z), (s * 2.0, 3.8, z), 0.42, 0.05, mat=Mat(CITRON, spec=0.3), seg=6))
    # radar de veille tournant à panneau plat
    m.add(tube((0, -2.0, 2.6), (0, -2.0, 4.4), 0.3, mat=ACIER_SOMBRE, seg=5))
    m.add(box(-2.0, -2.2, 4.4, 2.0, -1.8, 6.0, CAPTEUR).rot_x(-15, cy=-2.0, cz=4.4))
    return m


# ---------------------------------------------------------------------------
# Nayrath : poste de commandement mobile 8×8, shelter, mât télescopique et radar
# ---------------------------------------------------------------------------
def nayrath():
    p = peau(324)
    m = Mesh()
    m.add(prism(rrect(-3.6, 5.2, 3.6, 11.4, 1.2), 1.6, 6.0, p, bevel=0.8))
    m.add(box(-3.2, 10.9, 4.2, 3.2, 11.5, 5.4, VITRE))
    m.add(box(-3.4, -11.2, 1.4, 3.4, 5.2, 2.8, ACIER_SOMBRE))
    m.add(prism(rrect(-3.8, -11.4, 3.8, 4.6, 0.8), 2.8, 8.2, p, bevel=0.6))              # shelter de commandement
    m.add(box(-3.85, -6.0, 4.6, -3.75, -1.0, 6.4, CAPTEUR))
    m.add(box(3.75, -6.0, 4.6, 3.85, -1.0, 6.4, CAPTEUR))
    for s in (1, -1):
        for y in (-9.0, -5.6, 3.8, 7.4):
            m.add(roue(s * 3.5, y, 1.6, 1.6, 1.1, s))
        m.add(tube((s * 2.6, 11.3, 3.6), (s * 2.6, 11.7, 3.6), 0.4, mat=PHARE, seg=6))
    # mât télescopique et radar plat, paraboles satellites, antennes fouet
    m.add(tube((0, -8.0, 8.2), (0, -8.0, 15.0), 0.45, 0.3, mat=ACIER_SOMBRE, seg=6))
    m.add(box(-2.6, -8.3, 15.0, 2.6, -7.7, 17.4, CAPTEUR))
    m.add(sphere((-2.0, 1.6, 8.6), 1.5, Mat((214, 216, 208), spec=0.5), seg=10, rings=4, sz=0.4).rot_x(-35, cy=1.6, cz=8.6))
    m.add(sphere((2.2, 1.0, 8.6), 1.1, Mat((214, 216, 208), spec=0.5), seg=10, rings=4, sz=0.4).rot_x(-35, cy=1.0, cz=8.6))
    for x, y in ((-3.0, -10.6), (3.0, -10.6), (-3.0, 6.0)):
        m.add(antenne(x, y, 8.2 if y < 5 else 6.0, 6.0))
    m.add(sphere((0, -8.0, 17.6), 0.35, LUEUR, seg=6, rings=3))
    m.add(rose(0.0, -4.0, 8.22, 1.4))
    return m


# ===========================================================================
# BÂTIMENTS (projection oblique, centre de l'emprise, y vers le nord)
# ===========================================================================
BETON_CLAIR = Mat((176, 178, 172), spec=0.15, tex=combine(panels(6.0, 0.3, 0.88), noise(331, 0.05, 1.2)))
BETON_SOMBRE = Mat((112, 114, 110), spec=0.1, tex=combine(panels(6.0, 0.35, 0.86), noise(332, 0.06, 1.2)))
VERRE = Mat((80, 130, 150), spec=1.1, shine=46, tex=stripes(2.4, 0.85, axis=0))
VERRE_V = Mat((80, 130, 150), spec=1.1, shine=46, tex=stripes(2.4, 0.85, axis=1))
LAMPE = Mat((255, 236, 150), emit=True)
LAMPE_V = Mat((160, 255, 90), emit=True)


def blinde_b(seed):
    return R.team(spec=0.3, tex=combine(panels(4.0, 0.3, 0.84), noise(seed, 0.04)))


def drapeau(x, y, z, h=10.0):
    """Mât et drapeau vert citron à liseré vert pomme, insigne blanc."""
    def tex(p, n):
        if p[0] < x + 1.6:
            return POMME
        u, v = p[0] - (x + 5.2), p[2] - (z + h - 2.6)
        if abs(u) + abs(v) < 1.3:
            return BLANC
        return 1.0
    return merge(tube((x, y, z), (x, y, z + h), 0.3, mat=ACIER, seg=4),
                 box(x + 0.2, y - 0.1, z + h - 5.0, x + 9.0, y + 0.1, z + h - 0.2, Mat(CITRON, flat=True, tex=tex)))


# Haut Commandement (3×3, rangée sud dégagée) : tour de verre en coin, héliport, antennes
def haut_commandement(t=0.0):
    p = blinde_b(341)
    m = Mesh()
    m.add(prism(rrect(-34, -12, 34, 34, 4.0), 0.0, 2.0, BETON_SOMBRE, bevel=0.8))
    m.add(prism(rrect(-30, -6, 30, 30, 4.0), 2.0, 12.0, BETON_CLAIR, bevel=1.5))         # socle
    for k in range(9):                                                                    # bandeau vitré
        m.add(box(-27 + k * 6.0, -6.2, 6.0, -23 + k * 6.0, -5.9, 10.0, VERRE_V))
    # tour en coin (flèche vers le sud), verre et tôle équipe
    m.add(prism([(-14, 4), (14, 4), (18, 18), (0, 28), (-18, 18)], 12.0, 30.0, p, bevel=1.4))
    for k in range(4):
        z = 15.0 + k * 4.0
        m.add(prism([(-14.3, 3.8), (14.3, 3.8), (14.3, 4.4), (-14.3, 4.4)], z, z + 2.2, VERRE))
    m.add(prism([(-10, 8), (10, 8), (12, 17), (0, 24), (-12, 17)], 30.0, 32.0, BETON_SOMBRE, bevel=0.5))
    # héliport sur l'aile est
    m.add(prism(ngon(23, 20, 7.0, 16), 12.0, 12.6, Mat((70, 74, 72), spec=0.1)))
    m.add(prism(rrect(21.6, 16.5, 22.4, 23.5, 0.2), 12.6, 12.7, Mat(BLANC)))
    m.add(prism(rrect(20.2, 19.6, 25.8, 20.4, 0.2), 12.6, 12.7, Mat(BLANC)))
    # radar tournant (t = phase), antennes, paraboles
    radar = box(-5.0, -0.3, 0.0, 5.0, 0.3, 2.6, CAPTEUR)
    m.add(tube((-4, 16, 32), (-4, 16, 36), 0.6, mat=ACIER_SOMBRE, seg=6))
    m.add(radar.rot_z(t * 360).move(-4, 16, 36))
    m.add(tube((6, 18, 32), (6, 18, 46), 0.5, 0.25, mat=ACIER_SOMBRE, seg=5))
    m.add(sphere((6, 18, 46.4), 0.6, LAMPE_V if int(t * 4) % 2 == 0 else Mat((40, 80, 30)), seg=6, rings=3))
    for x, y in ((-24, 22), (26, 2)):
        m.add(sphere((x, y, 13.0), 3.0, Mat((214, 216, 208), spec=0.5), seg=10, rings=5, sz=0.5).rot_x(-40, cy=y, cz=13))
    m.add(drapeau(-26, 6, 12.0, 12.0))
    m.add(box(-5.0, -6.4, 2.0, 5.0, -6.0, 9.0, Mat((40, 44, 46))))                        # entrée
    m.add(rose(0, 15, 32.02, 4.0))
    return m


# Fabrique de drones (3×3, rangée sud dégagée) : hall à sheds, piste d'essai, drones en vol
def fabrique_drones(t=0.0):
    p = blinde_b(342)
    m = Mesh()
    m.add(prism(rrect(-34, -12, 34, 34, 3.0), 0.0, 1.6, BETON_SOMBRE, bevel=0.6))
    m.add(prism(rrect(-32, 2, 20, 32, 2.0), 1.6, 12.0, BETON_CLAIR, bevel=1.0))           # hall
    for k in range(5):                                                                   # toiture en sheds vitrés
        x0 = -31 + k * 10.2
        m.add(R.loft([[(x0, 3, 12.0), (x0 + 6.5, 3, 12.0), (x0 + 6.5, 3, 16.0)],
                      [(x0, 31, 12.0), (x0 + 6.5, 31, 12.0), (x0 + 6.5, 31, 16.0)]], p))
        m.add(box(x0 + 6.5, 3, 12.0, x0 + 6.9, 31, 16.0, VERRE))
    m.add(box(-24, 1.6, 1.6, -10, 2.2, 10.0, Mat((60, 64, 62), tex=stripes(0.8, 0.8, axis=2))))   # porte
    # tour de contrôle et antenne de liaison
    m.add(prism(rrect(22, 14, 32, 30, 1.5), 1.6, 18.0, p, bevel=0.8))
    m.add(box(21.8, 14, 14.0, 32.2, 30, 16.6, VERRE_V))
    m.add(tube((27, 22, 18.0), (27, 22, 26.0), 0.3, mat=ACIER_SOMBRE, seg=4))
    # piste d'essai et drones qui tournent (t = phase)
    m.add(prism(rrect(-30, -10, 30, -2, 1.0), 1.6, 1.8, Mat((72, 76, 74), spec=0.1)))
    for k in range(6):
        m.add(box(-26 + k * 10, -6.3, 1.8, -22 + k * 10, -5.7, 1.85, Mat(BLANC)))
    for k in range(3):
        a = 2 * math.pi * (t + k / 3.0)
        x, y = 4 + 18 * math.cos(a), 12 + 10 * math.sin(a)
        d = rodeuse().scale(0.9).rot_z(math.degrees(a) + 180).move(x, y, 24.0)
        m.add(d)
    m.add(rose(-6, 17, 16.02, 3.0))
    return m


# Institut d'innovation (2×3, rangée sud dégagée) : dôme de recherche, laboratoire vitré, antenne
def institut(t=0.0):
    p = blinde_b(343)
    m = Mesh()
    m.add(prism(rrect(-23, -34, 23, 34, 2.0), 0.0, 1.2, BETON_SOMBRE))
    m.add(prism(rrect(-20, -6, 20, 30, 3.0), 1.2, 14.0, BETON_CLAIR, bevel=1.2))
    for k in range(6):
        m.add(box(-18 + k * 6.2, -6.2, 4.0, -14 + k * 6.2, -5.9, 12.0, VERRE_V))
    m.add(tube((0, 14, 14.0), (0, 14, 16.0), 12.0, mat=p, seg=24))
    m.add(sphere((0, 14, 16.0), 12.0, Mat((200, 214, 220), spec=1.0, shine=40, tex=stripes(3.0, 0.88, axis=0)),
                 seg=24, rings=8, sz=0.7))
    # fente d'observation et anneau lumineux
    m.add(box(-1.2, 2.5, 18.0, 1.2, 14.0, 24.0, Mat((40, 44, 50))))
    for k in range(12):
        a = 2 * math.pi * k / 12
        on = (k + int(t * 12)) % 3 == 0
        m.add(sphere((12.2 * math.cos(a), 14 + 12.2 * math.sin(a), 15.0), 0.6, LAMPE_V if on else Mat((60, 90, 40)), seg=6, rings=3))
    # antenne de transmission et panneau solaire
    m.add(tube((-16, 26, 14.0), (-16, 26, 34.0), 0.5, 0.2, mat=ACIER_SOMBRE, seg=5))
    m.add(sphere((-16, 26, 34.4), 0.5, LAMPE, seg=6, rings=3))
    m.add(box(8, -16, 1.2, 20, -8, 3.4, Mat((40, 50, 80), spec=1.0, tex=panels(2.0, 0.3, 0.7))).rot_x(0))
    m.add(box(-20, -22, 1.2, -6, -14, 2.0, Mat((70, 74, 72), spec=0.1)))
    m.add(drapeau(14, 26, 14.0, 10.0))
    m.add(rose(-12, 2, 14.02, 2.6))
    return m


# ---------------------------------------------------------------------------
# Défenses à tourelle
# ---------------------------------------------------------------------------
def socle_kheshkarn():
    m = Mesh()
    m.add(prism(rrect(-11, -11, 11, 11, 3.5), 0.0, 3.4, BETON_CLAIR, bevel=1.4))
    m.add(prism(ngon(0, 0, 8.0, 16), 3.4, 3.8, BETON_SOMBRE))
    return m


def kheshkarn_tourelle(recul=0.0):
    """Tourelle à missiles antichars : deux conteneurs latéraux et boule optronique."""
    p = blinde_b(344)
    m = Mesh()
    m.add(prism([(-5.0, -5.6), (5.0, -5.6), (5.4, 1.0), (3.0, 5.4), (-3.0, 5.4), (-5.4, 1.0)], 3.8, 8.2, p, bevel=1.6))
    for s in (1, -1):
        m.add(box(s * 5.0 - (0 if s > 0 else 2.6), -3.0 - recul * 0.4, 5.4, s * 5.0 + (2.6 if s > 0 else 0), 6.8 - recul * 0.4, 8.0, p, bevel=0.3))
        for z in (6.0, 7.4):
            m.add(tube((s * 6.3, 6.7 - recul * 0.4, z), (s * 6.3, 7.0 - recul * 0.4, z), 0.5, mat=Mat(CITRON, spec=0.3), seg=8))
    m.add(sphere((0, 3.8, 9.2), 1.6, CAPTEUR, seg=10, rings=5))
    m.add(rose(0, -2.0, 8.22, 1.2))
    return m


def socle_ambar():
    """Emprise 2×1 : dalle, poste de tir, radar à antenne plate fixe."""
    m = Mesh()
    m.add(prism(rrect(-22, -10, 22, 10, 2.0), 0.0, 1.6, BETON_CLAIR, bevel=0.6))
    m.add(prism(rrect(-21, -8, -11, 5, 1.2), 1.6, 7.0, blinde_b(345), bevel=0.8))
    m.add(box(-20, -2.6, 7.0, -12, -2.0, 13.0, CAPTEUR).rot_x(-20, cy=-2.3, cz=7.0))
    for k in range(2):
        m.add(box(12.5, -8 + k * 5, 1.6, 21, -4 + k * 5, 4.4, blinde_b(346), bevel=0.2))
    m.add(rose(-16, -5.0, 7.02, 1.2))
    return m


def ambar_tourelle(recul=0.0):
    """Lanceur vertical à six conteneurs, relevé."""
    p = blinde_b(347)
    m = Mesh()
    m.add(tube((0, 0, 1.6), (0, 0, 3.2), 4.4, 4.0, mat=p, seg=14))
    m.add(box(-1.4, -1.2, 3.2, 1.4, 1.2, 6.0, p))
    rampe = Mesh()
    rampe.add(box(-4.2, -5.0, -0.8, 4.2, 5.0, 3.2, p, bevel=0.4))
    for i in range(3):
        for j in range(2):
            x, z = -2.8 + i * 2.8, 0.2 + j * 1.9
            rampe.add(box(x - 1.1, 4.95, z - 0.7, x + 1.1, 5.15, z + 0.7, Mat(CITRON, spec=0.3)))
    m.add(rampe.rot_x(40).move(dz=6.4))
    return m


def socle_ruche():
    m = Mesh()
    m.add(prism(ngon(0, 0, 11.0, 6, start=math.pi / 6), 0.0, 3.0, BETON_CLAIR, bevel=1.2))
    m.add(prism(ngon(0, 0, 8.5, 6, start=math.pi / 6), 3.0, 3.4, BETON_SOMBRE))
    return m


def ruche_tourelle(recul=0.0):
    """Ruche : tambour hexagonal d'alvéoles de lancement, drones posés sur le dessus."""
    p = blinde_b(348)
    m = Mesh()
    m.add(prism(ngon(0, 0, 7.0, 6, start=math.pi / 6), 3.4, 10.0, p, bevel=1.4))
    for k in range(6):                                   # alvéoles (hexagones sombres en façade)
        a = math.pi / 3 * k + math.pi / 2
        cx, cy = 5.6 * math.cos(a), 5.6 * math.sin(a)
        m.add(prism(ngon(cx, cy, 1.3, 6), 9.9, 10.15, Mat(CITRON, spec=0.3) if k % 2 == 0 else ACIER_SOMBRE))
    m.add(prism(ngon(0, 0, 2.4, 6), 10.0, 11.4, ACIER_SOMBRE, bevel=0.4))
    m.add(sphere((0, 0, 11.8), 1.2, CAPTEUR, seg=8, rings=4))
    for k in range(2):                                  # drones prêts au départ
        if recul > 0 and k == 0:
            continue
        a = math.pi * k + 0.6
        m.add(rodeuse().scale(0.7).rot_z(math.degrees(a)).move(3.6 * math.cos(a), 3.6 * math.sin(a), 10.8))
    return m
