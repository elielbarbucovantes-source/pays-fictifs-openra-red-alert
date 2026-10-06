"""Voitures de train à l'échelle des unités (demande du 2026-10-06) : 1 case ≈ 1 char ≈ 7 m.

Une voiture de N cases fait N × 24 − 2 px de long (RailCar Cells: N), centrée, y vers l'avant.
Les modèles sont construits en « unités de base » (largeur et hauteur des anciens trains, avant
leur agrandissement × 1,5) mais avec y directement en pixels : sprites_hd les met à l'échelle
avec ECHELLE_XZ en largeur et en hauteur seulement.

Plateformes à véhicules : une case de plancher par véhicule ; voitures de troupes : 2 cases ;
locomotives et wagons blindés : 2 cases (14–20 m) ; Schwerer Gustav : 5 cases."""
import math
import rendu3d as R
from rendu3d import Mesh, Mat, prism, box, tube, sphere, rrect, noise, stripes, panels, combine
from modeles.communs import ACIER, ACIER_SOMBRE, GRILLE, VITRE, FEU_BLANC, FEU_ROUGE, BOIS, BACHE, blinde, antenne, canon, trappe, missile
from modeles.ferroviaire import NOIR, JAUNE, TOLE

ECHELLE_XZ = 1.5
PLANCHER = Mat((96, 74, 50), spec=0.05, tex=stripes(1.1, 0.8, axis=1))
CAOUTCHOUC_PONT = Mat((58, 60, 62), spec=0.1, tex=noise(401, 0.06, 1.5))
BLANC = Mat((226, 226, 218), spec=0.3)
ROUGE = Mat((176, 40, 34), spec=0.3)
BLEU = Mat((40, 70, 130), spec=0.35)
VERT_KAKI = Mat((84, 92, 58), spec=0.15)
OBUS = Mat((150, 120, 60), spec=0.6, shine=30)
BOMBE = Mat((76, 82, 70), spec=0.3)
ROUILLE = Mat((120, 74, 46), spec=0.1, tex=noise(402, 0.12, 1.2))


def demi(cells):
    """Demi-longueur de la caisse (sans les attelages, qui dépassent de 0,8)."""
    return (cells * 24 - 2) / 2.0 - 0.8


# ---------------------------------------------------------------------------
# Roulement et châssis
# ---------------------------------------------------------------------------
def essieu(y, z=1.4, r=1.4, x=3.4):
    m = Mesh()
    for s in (1, -1):
        m.add(tube((s * x, y, z), (s * (x + 0.7), y, z), r, mat=NOIR, seg=10))
        m.add(tube((s * (x + 0.7), y, z), (s * (x + 0.8), y, z), r * 0.45, mat=ACIER, seg=8))
    return m


def bogie(y, essieux=2, pas=3.6):
    """Bogie à 2 ou 3 essieux : longerons, boîtes d'essieu, ressorts."""
    m = Mesh()
    L = (essieux - 1) * pas / 2 + 1.6
    m.add(box(-3.9, y - L, 1.0, 3.9, y + L, 2.4, ACIER_SOMBRE))
    for s in (1, -1):
        m.add(box(s * 4.0 - 0.3, y - L + 0.4, 1.6, s * 4.0 + 0.3, y + L - 0.4, 2.2, ACIER))     # ressorts / longerons
    for k in range(essieux):
        m.add(essieu(y - (essieux - 1) * pas / 2 + k * pas))
    return m


def chassis(H, z=2.4, h=1.2, largeur=4.4):
    """Châssis de longueur 2H, attelages et tampons aux deux bouts, marchepieds."""
    m = Mesh()
    m.add(box(-largeur, -H, z, largeur, H, z + h, ACIER_SOMBRE))
    for s in (1, -1):
        m.add(box(-0.6, s * H, z + 0.2, 0.6, s * (H + 0.8), z + 0.9, NOIR))
        for x in (-3.0, 3.0):
            m.add(tube((x, s * H, z + 0.6), (x, s * (H + 0.6), z + 0.6), 0.55, mat=ACIER, seg=6))
        for x in (-largeur, largeur):
            m.add(box(x - 0.2, s * (H - 1.2) - 0.6, z - 0.8, x + 0.2, s * (H - 1.2) + 0.6, z, ACIER))   # marchepieds
    return m


def roulement(H, essieux=2, bogies_par_bout=1):
    """Bogies placés aux bouts de la voiture (plusieurs par bout pour les wagons lourds)."""
    m = Mesh()
    pas_b = (essieux - 1) * 3.6 + 4.2
    for s in (1, -1):
        for k in range(bogies_par_bout):
            m.add(bogie(s * (H - 3.6 - k * pas_b), essieux))
    return m


def feux(H, z, x=2.2, avant=True, arriere=True):
    m = Mesh()
    for s in (1, -1):
        if avant:
            m.add(tube((s * x, H, z), (s * x, H + 0.2, z), 0.45, mat=FEU_BLANC, seg=6))
        if arriere:
            m.add(tube((s * x, -H, z), (s * x, -H - 0.2, z), 0.4, mat=FEU_ROUGE, seg=6))
    return m


# ---------------------------------------------------------------------------
# Locomotives
# ---------------------------------------------------------------------------
def _cabine(y0, y1, z0, z1, peau, largeur=4.3, vitre_avant=None, vitre_arriere=None):
    m = Mesh()
    m.add(prism(rrect(-largeur, y0, largeur, y1, 0.6), z0, z1, peau, bevel=0.5))
    m.add(prism(rrect(-largeur - 0.2, y0 - 0.2, largeur + 0.2, y1 + 0.2, 0.6), z1, z1 + 0.6, ACIER_SOMBRE))
    for s in (1, -1):
        m.add(box(s * largeur - 0.03, y0 + 1.0, z1 - 3.2, s * largeur + 0.03, y1 - 1.0, z1 - 1.0, VITRE))
    if vitre_avant is not None:
        m.add(box(-largeur + 0.5, vitre_avant - 0.05, z1 - 3.0, largeur - 0.5, vitre_avant + 0.05, z1 - 0.9, VITRE))
    if vitre_arriere is not None:
        m.add(box(-largeur + 0.5, vitre_arriere - 0.05, z1 - 3.0, largeur - 0.5, vitre_arriere + 0.05, z1 - 0.9, VITRE))
    return m


def _capot(y0, y1, peau, largeur=3.2, z0=3.6, z1=8.2):
    """Capot moteur : grilles et portes de visite."""
    m = Mesh()
    m.add(prism(rrect(-largeur, y0, largeur, y1, 0.8), z0, z1, peau, bevel=0.6))
    n = max(2, int((y1 - y0) / 4.0))
    for k in range(n):
        y = y0 + 1.0 + k * (y1 - y0 - 2.0) / n
        m.add(box(-largeur + 0.6, y, z1, largeur - 0.6, y + (y1 - y0 - 2.0) / n - 0.8, z1 + 0.3, GRILLE))
        for s in (1, -1):
            m.add(box(s * largeur - 0.04, y + 0.3, z0 + 1.0, s * largeur + 0.04, y + (y1 - y0 - 2.0) / n - 1.0, z1 - 1.0, GRILLE))
    return m


def loco_capot(cells=2, seed=330, bande=JAUNE):
    """Diesel de ligne à cabine avancée et long capot (locomotive commune, côtière)."""
    peau = blinde(3.5, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=3))
    m.add(chassis(H))
    m.add(box(-1.8, -H * 0.3, 0.9, 1.8, H * 0.3, 2.4, Mat((50, 54, 50), spec=0.2)))     # réservoir
    yc = H - 7.0
    m.add(_capot(-H + 0.6, yc - 0.4, peau))
    m.add(_cabine(yc, H - 1.4, 3.6, 10.4, peau, vitre_avant=H - 1.4))
    m.add(prism(rrect(-3.0, H - 1.4, 3.0, H, 0.5), 3.6, 7.0, peau, bevel=0.4))          # nez court
    m.add(tube((0, -H * 0.4, 8.2), (0, -H * 0.4, 10.0), 0.6, mat=NOIR, seg=6))          # échappements
    m.add(tube((0, -H * 0.1, 8.2), (0, -H * 0.1, 9.6), 0.5, mat=NOIR, seg=6))
    for s in (1, -1):
        m.add(box(s * 4.4 - 0.15, -H, 3.6, s * 4.4 + 0.15, H, 4.3, bande))
        m.add(tube((s * 3.6, -H + 0.8, 4.6), (s * 3.6, yc - 0.6, 4.6), 0.12, mat=ACIER, seg=3))  # mains courantes
    m.add(feux(H, 5.6))
    m.add(antenne(-2.0, yc + 2.0, 11.0, 2.5, r=0.12))
    return m


def loco_double(cells=2, seed=331):
    """Diesel lourde à deux cabines et caisse pleine (Rubénie : type TE soviétique)."""
    peau = blinde(3.0, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=3))
    m.add(chassis(H, largeur=4.6))
    m.add(prism(rrect(-4.4, -H + 0.4, 4.4, H - 0.4, 1.0), 3.6, 10.6, peau, bevel=0.8))
    for s in (1, -1):
        y = s * (H - 0.4)
        # faces de cabine en pointe, pare-brise en deux parties
        m.add(prism([(-4.4, y), (4.4, y), (2.6, y + s * 1.6), (-2.6, y + s * 1.6)] if s > 0 else
                    [(-2.6, y + s * 1.6), (2.6, y + s * 1.6), (4.4, y), (-4.4, y)], 3.6, 10.6, peau, bevel=0.6))
        for x in (-1.6, 1.6):
            m.add(box(x - 1.3, y + s * 1.55 - 0.05, 7.6, x + 1.3, y + s * 1.55 + 0.05, 9.6, VITRE))
        m.add(box(s * 4.4 - 0.15 * s, -H + 2.0, 4.2, s * 4.4, H - 2.0, 4.9, Mat((200, 160, 40), spec=0.3)))   # filet
    for k in range(-2, 3):                                                      # grilles latérales et toiture
        y = k * (H - 6.0) / 2.5
        for s in (1, -1):
            m.add(box(s * 4.38, y - 1.4, 6.0, s * 4.42, y + 1.4, 9.4, GRILLE))
        m.add(box(-2.4, y - 1.6, 10.6, 2.4, y + 1.6, 11.2, GRILLE))
    for y in (-H * 0.25, H * 0.25):
        m.add(tube((0, y, 10.6), (0, y, 12.0), 0.7, mat=NOIR, seg=6))
    m.add(feux(H + 1.6, 6.0, x=1.8))
    m.add(antenne(2.6, 0.0, 11.0, 2.8, r=0.12))
    return m


def loco_profilee(cells=2, seed=332, radar=False):
    """Locomotive rapide profilée (Elielistan) : nez en coin, cabine vitrée, carénage bas."""
    peau = blinde(4.0, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=2))
    m.add(chassis(H, h=1.0))
    m.add(box(-4.5, -H, 1.2, 4.5, H, 3.6, peau))                                # jupes carénées
    nez = 8.0
    prof = [(-4.4, -H + 0.4), (4.4, -H + 0.4), (4.4, H - nez), (2.0, H - 0.6), (-2.0, H - 0.6), (-4.4, H - nez)]
    m.add(prism(prof, 3.6, 8.8, peau, bevel=1.2))
    m.add(R.loft([[(-4.0, H - nez - 1.0, 8.8), (4.0, H - nez - 1.0, 8.8)], [(-1.8, H - 1.2, 5.0), (1.8, H - 1.2, 5.0)]], VITRE, cap0=False, cap1=False))
    for s in (1, -1):
        m.add(box(s * 4.4 - 0.1 * s, -H + 1.0, 5.8, s * 4.4, H - nez - 1.0, 6.6, JAUNE))   # bande de vitesse
        m.add(box(s * 4.38, H - nez - 4.0, 6.8, s * 4.42, H - nez - 0.6, 8.4, VITRE))
    m.add(box(-3.0, -H * 0.6, 8.8, 3.0, H * 0.3, 9.4, GRILLE))                   # aérations toiture
    m.add(prism(rrect(-1.2, -H * 0.2, 1.2, -H * 0.05, 0.4), 8.8, 10.4, ACIER_SOMBRE))  # pantographe / mât
    if radar:                                                                  # loco du train aérien : radôme et antennes
        m.add(sphere((0, -H * 0.55, 10.4), 2.4, Mat((220, 220, 214), spec=0.4), seg=12, rings=6, sz=0.55))
        m.add(antenne(-3.0, -H * 0.2, 9.4, 3.0, r=0.12))
        m.add(antenne(3.0, -H * 0.2, 9.4, 3.0, r=0.12))
    m.add(feux(H - 0.6, 4.6, x=1.6))
    return m


def loco_industrielle(cells=2, seed=333):
    """Locomotive lourde de fret (Ananthanie) : caisse anguleuse, bogies à 3 essieux, chevrons de sécurité."""
    peau = blinde(2.5, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=3))
    m.add(chassis(H, h=1.6, largeur=4.7))
    m.add(prism(rrect(-4.4, -H + 1.0, 4.4, H - 4.6, 0.4), 4.0, 10.0, peau, bevel=0.3))
    m.add(prism([(-4.4, H - 4.6), (4.4, H - 4.6), (4.4, H - 1.6), (3.4, H - 0.4), (-3.4, H - 0.4), (-4.4, H - 1.6)], 4.0, 11.4, peau, bevel=0.4))
    m.add(box(-3.6, H - 0.5, 8.6, 3.6, H - 0.35, 10.8, VITRE))
    for s in (1, -1):
        m.add(box(s * 4.38, H - 4.0, 8.6, s * 4.42, H - 1.8, 10.8, VITRE))
        for k in range(int(2 * H / 2.0)):                                       # chevrons jaune / noir sur le châssis
            y = -H + 0.5 + k * 2.0
            m.add(box(s * 4.75 - 0.05, y, 4.0, s * 4.75 + 0.05, y + 1.0, 5.6, JAUNE if k % 2 else NOIR))
    for k in range(4):                                                         # radiateurs de toit
        y = -H + 2.5 + k * (H * 2 - 8.0) / 4
        m.add(box(-3.6, y, 10.0, 3.6, y + 2.6, 10.8, GRILLE))
        m.add(tube((0, y + 1.3, 10.8), (0, y + 1.3, 11.2), 1.1, mat=NOIR, seg=10))   # ventilateurs
    m.add(box(-4.6, H - 0.2, 2.6, 4.6, H + 0.6, 3.8, JAUNE))                      # chasse-pierres
    m.add(feux(H - 0.4, 6.0, x=3.0))
    return m


def loco_blindee(cells=2, seed=334, hauteur=5.0, pente=1.4):
    """Locomotive blindée (PR-35, BP-43, Krajina) : chaudière/moteur sous casemate à flancs inclinés."""
    peau = blinde(2.5, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=3))
    m.add(chassis(H))
    for s in (1, -1):                                                          # jupes blindées
        m.add(box(s * 4.6 - 0.25 * s, -H, 1.0, s * 4.6 + 0.05 * s, H, 3.6, peau))
    m.add(prism(rrect(-4.6, -H + 0.2, 4.6, H - 0.2, 1.2), 3.6, 3.6 + hauteur, peau, bevel=pente))
    m.add(prism(rrect(-2.6, -H + 1.0, 2.6, -H + 6.0, 0.6), 3.6 + hauteur, 3.6 + hauteur + 1.8, peau, bevel=0.5))  # poste de conduite
    m.add(box(-2.2, -H + 0.95, 3.6 + hauteur + 0.6, 2.2, -H + 1.05, 3.6 + hauteur + 1.4, VITRE))
    for s in (1, -1):
        for k in range(int(H / 3)):
            y = -H + 3.0 + k * 6.0
            m.add(box(s * 3.9 - 0.1, y - 0.5, 5.8, s * 3.9 + 0.1, y + 0.5, 6.6, NOIR))   # meurtrières
    m.add(tube((0, H * 0.35, 3.6 + hauteur), (0, H * 0.35, 3.6 + hauteur + 1.2), 0.6, mat=NOIR, seg=6))
    m.add(antenne(-3.0, -H + 3.0, 3.6 + hauteur, 3.0, r=0.12))
    return m


def d311(cells=2, seed=335):
    """Diesel D311 (traction du Schwerer Gustav) : cabine centrale, deux capots."""
    peau = blinde(3.0, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=2))
    m.add(chassis(H))
    m.add(_capot(-H + 0.6, -3.4, peau, largeur=3.0, z1=7.6))
    m.add(_capot(3.4, H - 0.6, peau, largeur=3.0, z1=7.6))
    m.add(_cabine(-3.2, 3.2, 3.6, 10.6, peau, vitre_avant=3.2, vitre_arriere=-3.2))
    for s in (1, -1):
        m.add(box(s * 4.4 - 0.15, -H, 3.6, s * 4.4 + 0.15, H, 4.2, ROUGE))
    m.add(feux(H - 0.6, 5.4, x=2.0))
    return m


def loco_nucleaire(cells=3, seed=336):
    """Locomotive nucléaire (Elielistan) : caisse blindée profilée, réacteur sous dôme au centre,
    ailettes de refroidissement, trèfles de radioactivité, deux cabines en coin."""
    peau = blinde(3.5, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=3, bogies_par_bout=2))
    m.add(chassis(H, h=1.4, largeur=4.7))
    for s in (1, -1):                                                          # jupes blindées
        m.add(box(s * 4.7 - 0.3 * s, -H, 1.0, s * 4.7 + 0.05 * s, H, 3.8, peau))
    nez = 6.0
    prof = [(-4.6, -H + nez), (-1.8, -H + 0.4), (1.8, -H + 0.4), (4.6, -H + nez),
            (4.6, H - nez), (1.8, H - 0.4), (-1.8, H - 0.4), (-4.6, H - nez)]
    m.add(prism(prof, 3.8, 9.4, peau, bevel=1.0))
    for s in (1, -1):                                                          # cabines vitrées aux deux bouts
        yv = s * (H - nez + 0.6)
        m.add(R.loft([[(-3.2, yv, 9.2), (3.2, yv, 9.2)], [(-2.0, s * (H - 2.2), 7.0), (2.0, s * (H - 2.2), 7.0)]][::s], VITRE, cap0=False, cap1=False))
        for x in (-1, 1):
            m.add(box(x * 4.6 - 0.05, s * (H - nez) - 1.6, 7.0, x * 4.6 + 0.05, s * (H - nez) + 1.6, 8.8, VITRE))
        m.add(box(s * 4.6 - 0.12 * s, -H + nez, 5.6, s * 4.6, H - nez, 6.4, JAUNE))   # bande
    # réacteur : socle, dôme blindé, anneau de confinement
    m.add(tube((0, 0, 9.4), (0, 0, 10.6), 4.2, mat=ACIER_SOMBRE, seg=16))
    m.add(sphere((0, 0, 10.6), 4.2, Mat((200, 204, 196), spec=0.5, shine=28), seg=16, rings=8, sz=0.9))
    m.add(tube((0, 0, 11.4), (0, 0, 11.9), 4.0, mat=JAUNE, seg=16))
    for k in range(3):                                                         # trèfle de radioactivité sur le dôme
        a = 2 * math.pi * k / 3
        m.add(sphere((1.8 * math.cos(a), 1.8 * math.sin(a), 13.9), 0.9, NOIR, seg=6, rings=3, sz=0.4))
    m.add(sphere((0, 0, 14.3), 0.5, NOIR, seg=6, rings=3, sz=0.5))
    # radiateurs de refroidissement de part et d'autre du réacteur, tuyauteries vers le dôme
    for s in (1, -1):
        yc = s * (H - nez) * 0.55
        m.add(box(-2.6, yc - 2.4, 9.4, 2.6, yc + 2.4, 10.4, ACIER_SOMBRE))
        m.add(box(-2.2, yc - 2.0, 10.4, 2.2, yc + 2.0, 10.6, GRILLE))
        for x in (-1.6, 1.6):
            m.add(tube((x, yc - s * 2.4, 10.0), (x, s * 3.4, 10.4), 0.35, mat=JAUNE, seg=6))
    for s in (1, -1):                                                          # trèfles latéraux sur fond jaune
        m.add(box(s * 4.62, -2.2, 5.0, s * 4.68, 2.2, 8.6, JAUNE))
        for k in range(3):
            a = 2 * math.pi * k / 3 + math.pi / 2
            m.add(box(s * 4.68, 1.0 * math.cos(a) - 0.5, 6.8 + 1.0 * math.sin(a) - 0.5, s * 4.72, 1.0 * math.cos(a) + 0.5, 6.8 + 1.0 * math.sin(a) + 0.5, NOIR))
    m.add(feux(H - 0.4, 5.0, x=1.4))
    m.add(antenne(-3.2, -H + nez + 2.0, 9.4, 3.0, r=0.12))
    return m


# ---------------------------------------------------------------------------
# Wagons de transport
# ---------------------------------------------------------------------------
def plateforme(cells, seed=340, ranchers=True, cales=True, peau=None, rive=1.4):
    """Wagon plat à ranchers : un véhicule par case de plancher."""
    peau = peau or blinde(4.0, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=2))
    m.add(chassis(H))
    m.add(box(-4.6, -H, 3.6, 4.6, H, 4.2, PLANCHER))
    for s in (1, -1):
        m.add(box(s * 4.6 - 0.4 * s, -H, 3.6, s * 4.6, H, 3.6 + rive, peau))      # rives
        if ranchers:
            n = int(2 * H / 4.0)
            for k in range(n + 1):
                y = -H + 0.8 + k * (2 * H - 1.6) / n
                m.add(box(s * 4.5 - 0.35, y - 0.35, 3.6, s * 4.5 + 0.35, y + 0.35, 6.4, ACIER_SOMBRE))
    for y in (-H + 0.3, H - 0.3):
        m.add(box(-4.6, y - 0.3, 3.6, 4.6, y + 0.3, 5.8, peau))                   # ridelles d'about
    if cales:
        for c in range(cells):                                                 # cales de roues, une paire par véhicule
            yc = -H + 12.0 + c * 24.0 if cells > 1 else 0.0
            for dy in (-6.0, 6.0):
                m.add(box(-2.8, yc + dy - 0.6, 4.2, 2.8, yc + dy + 0.6, 4.9, JAUNE))
    return m


def voiture(cells, seed=341, peau=None, blindee=False):
    """Voiture de troupes : caisse à fenêtres, portes coulissantes, toit arrondi."""
    peau = peau or blinde(3.0, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=2))
    m.add(chassis(H))
    m.add(prism(rrect(-4.3, -H, 4.3, H, 0.8), 3.6, 9.6, peau, bevel=0.6 if blindee else 0.4))
    rings = [[(4.3 * math.cos(math.pi * i / 8), y, 9.6 + 1.6 * math.sin(math.pi * i / 8)) for i in range(9)] for y in (-H, H)]
    m.add(R.loft([list(reversed(r)) for r in rings], Mat((92, 96, 92), spec=0.25)))
    n = int(2 * H / 3.0)
    for s in (1, -1):
        for k in range(n):
            y = -H + 1.8 + k * (2 * H - 3.6) / max(1, n - 1)
            if abs(y) < 2.6:
                continue
            if blindee:
                m.add(box(s * 4.28, y - 0.5, 7.0, s * 4.32, y + 0.5, 7.6, NOIR))    # meurtrières
            else:
                m.add(box(s * 4.28, y - 0.9, 6.6, s * 4.32, y + 0.9, 8.4, VITRE))
        m.add(box(s * 4.29, -2.2, 3.8, s * 4.33, 2.2, 8.8, Mat((60, 64, 58), spec=0.2)))      # porte coulissante
        m.add(box(s * 4.3 - 0.1, -H, 4.4, s * 4.3 + 0.1, H, 5.0, JAUNE))
    for k in range(max(1, int(H / 6))):
        y = -H + 6.0 + k * 12.0
        m.add(box(-0.8, y - 0.8, 11.0, 0.8, y + 0.8, 11.5, GRILLE))            # aérateurs
    return m


def industriel(cells=3, seed=342):
    """Wagon à plancher surbaissé pour charges exceptionnelles : poutres caisson, 2 × 2 bogies à 3 essieux."""
    peau = blinde(3.0, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=3, bogies_par_bout=2))
    pb = 2 * 3.6 + 4.2 + 3.6 + 1.0                                               # longueur des bouts portés par les bogies
    for s in (1, -1):
        y0, y1 = sorted((s * H, s * (H - pb)))
        m.add(prism(rrect(-4.6, y0, 4.6, y1, 0.4), 2.6, 5.6, peau, bevel=0.3))  # têtes sur bogies
        m.add(box(-0.6, s * H, 3.2, 0.6, s * (H + 0.8), 3.9, NOIR))
        m.add(prism([(-4.6, s * (H - pb)), (4.6, s * (H - pb)), (4.6, s * (H - pb - 3.0)), (-4.6, s * (H - pb - 3.0))]
                    if s < 0 else [(-4.6, s * (H - pb - 3.0)), (4.6, s * (H - pb - 3.0)), (4.6, s * (H - pb)), (-4.6, s * (H - pb))],
                    1.6, 5.6, peau, bevel=0.3))                                  # col de cygne
    yc = H - pb - 3.0
    m.add(box(-4.8, -yc, 1.0, 4.8, yc, 2.2, ACIER_SOMBRE))                       # puits surbaissé
    m.add(box(-4.4, -yc, 2.2, 4.4, yc, 2.6, PLANCHER))
    for s in (1, -1):
        m.add(box(s * 4.8 - 0.5 * s, -yc, 1.0, s * 4.8, yc, 4.0, peau))          # poutres latérales
        for k in range(int(2 * yc / 3.0) + 1):
            y = -yc + k * 3.0
            m.add(box(s * 4.85 - 0.06, y, 1.2, s * 4.85 + 0.06, y + 1.5, 3.8, JAUNE if k % 2 else NOIR))
    for k in range(4):                                                         # anneaux d'arrimage et chaînes
        y = -yc + 2.0 + k * (2 * yc - 4.0) / 3
        for s in (1, -1):
            m.add(tube((s * 3.8, y, 2.6), (s * 3.8, y, 3.4), 0.35, mat=ACIER, seg=6))
    return m


def porte_bateaux(cells=3, seed=343):
    """Plateforme à berceaux pour navires : bers en V rembourrés, sangles."""
    peau = blinde(4.0, seed)
    m = plateforme(cells, seed, ranchers=False, cales=False, peau=peau, rive=0.8)
    H = demi(cells)
    for k in range(cells + 1):                                                  # bers en V
        y = -H + 4.0 + k * (2 * H - 8.0) / cells
        for s in (1, -1):
            m.add(prism([(s * 4.4, y - 0.7), (s * 4.4, y + 0.7), (s * 1.2, y + 0.7), (s * 1.2, y - 0.7)], 4.2, 4.2 + 0.1, ACIER_SOMBRE))
            m.add(R.loft([[(s * 4.4, y - 0.7, 8.4), (s * 4.4, y + 0.7, 8.4)], [(s * 1.0, y - 0.7, 4.4), (s * 1.0, y + 0.7, 4.4)]], ACIER_SOMBRE))
            m.add(box(s * 3.4 - 0.5, y - 0.8, 5.2, s * 3.4 + 0.5, y + 0.8, 7.4, Mat((40, 40, 40), spec=0.05)))   # patins
        m.add(box(-0.8, y - 0.8, 4.2, 0.8, y + 0.8, 4.8, Mat((40, 40, 40), spec=0.05)))
    for k in range(cells):
        y = -H + 12.0 + k * 24.0 if cells > 1 else 0.0
        m.add(box(-4.5, y - 0.3, 4.2, 4.5, y + 0.3, 4.4, JAUNE))                  # sangles
    for s in (1, -1):
        m.add(tube((s * 4.6, -H + 1.0, 3.8), (s * 4.6, H - 1.0, 3.8), 0.3, mat=BLANC, seg=4))   # garde-corps
    return m


def citerne(cells=2, seed=344):
    """Wagon-citerne de kérosène : réservoir cylindrique, dôme, passerelle, échelles."""
    peau = blinde(4.0, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=2))
    m.add(chassis(H))
    m.add(box(-4.4, -H, 3.6, 4.4, H, 3.9, ACIER_SOMBRE))
    r = 4.1
    m.add(tube((0, -H + 0.6, 3.9 + r), (0, H - 0.6, 3.9 + r), r, mat=peau, seg=16, caps=False))
    for s in (1, -1):
        m.add(tube((0, s * (H - 0.6), 3.9 + r), (0, s * (H + 0.4), 3.9 + r), r, r * 0.55, mat=peau, seg=16))   # fonds bombés
        m.add(box(s * 4.0 - 0.1, -H + 1.0, 3.9 + r - 0.4, s * 4.0 + 0.1, H - 1.0, 3.9 + r + 0.4, JAUNE))  # bande kérosène
        m.add(box(s * 4.4 - 0.1, -H + 1.5, 3.9, s * 4.4 + 0.1, -H + 2.3, 3.9 + 2 * r, ACIER))           # échelle
    m.add(tube((0, 0, 3.9 + 2 * r - 0.4), (0, 0, 3.9 + 2 * r + 1.2), 1.6, mat=ACIER_SOMBRE, seg=12))   # dôme
    m.add(box(-1.0, -H + 2.0, 3.9 + 2 * r - 0.2, 1.0, H - 2.0, 3.9 + 2 * r, GRILLE))                   # passerelle
    for y in (-H * 0.5, H * 0.5):
        m.add(tube((0, y, 3.9 + 2 * r - 0.3), (0, y, 3.9 + 2 * r + 0.6), 0.6, mat=ROUGE, seg=8))      # trous d'homme
    return m


def _pont_appontage(cells, peau):
    """Plateforme d'appontage : pont en caoutchouc, cercle et « H », bandes de bord."""
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=2))
    m.add(chassis(H))
    m.add(box(-4.8, -H, 3.6, 4.8, H, 4.4, peau))
    m.add(box(-4.6, -H + 0.2, 4.4, 4.6, H - 0.2, 4.6, CAOUTCHOUC_PONT))
    for s in (1, -1):
        m.add(box(s * 4.5 - 0.2, -H + 0.4, 4.6, s * 4.5 + 0.2, H - 0.4, 4.7, JAUNE))
        m.add(tube((s * 4.8, -H + 0.5, 4.6), (s * 4.8, H - 0.5, 4.6), 0.25, mat=BLANC, seg=4))   # filet de sécurité
    pts = [(3.4 * math.cos(2 * math.pi * i / 24), 3.4 * math.sin(2 * math.pi * i / 24)) for i in range(25)]
    for a, b in zip(pts, pts[1:]):                                             # cercle d'appontage
        m.add(R.prism([(a[0] * 1.0, a[1]), (b[0], b[1]), (b[0] * 0.86, b[1] * 0.86), (a[0] * 0.86, a[1] * 0.86)], 4.6, 4.72, BLANC))
    m.add(box(-1.4, -1.6, 4.6, -0.9, 1.6, 4.72, BLANC))                          # « H »
    m.add(box(0.9, -1.6, 4.6, 1.4, 1.6, 4.72, BLANC))
    m.add(box(-0.9, -0.25, 4.6, 0.9, 0.25, 4.72, BLANC))
    for s in (1, -1):                                                          # feux de bord de pont
        for y in (-H + 1.0, H - 1.0):
            m.add(sphere((s * 4.3, y, 4.9), 0.35, Mat((60, 230, 80), emit=True), seg=5, rings=3))
    return m


def plateforme_munitions(cells=3, seed=345):
    """Train aérien : pont d'appontage, râteliers de bombes et de missiles, soute blindée à l'arrière."""
    peau = blinde(4.0, seed)
    H = demi(cells)
    m = _pont_appontage(cells, peau)
    m.add(prism(rrect(-4.6, -H + 0.2, 4.6, -H + 6.0, 0.6), 4.4, 9.4, peau, bevel=0.5))   # soute à munitions
    m.add(box(-3.0, -H + 6.0, 5.0, 3.0, -H + 6.05, 8.6, Mat((60, 64, 58), spec=0.2)))
    for s in (1, -1):
        for k in range(4):                                                      # râteliers latéraux
            y = -H + 8.0 + k * 3.2
            m.add(box(s * 4.0 - 0.6, y - 0.1, 4.6, s * 4.0 + 0.6, y + 0.1, 6.0, ACIER_SOMBRE))
            m.add(tube((s * 4.0, y - 1.2, 6.0), (s * 4.0, y + 1.2, 6.0), 0.55, mat=BOMBE, seg=8))
        m.add(missile(s * 4.0, H - 9.0, H - 3.0, 5.4, r=0.4))
    return m


def plateforme_atelier(cells=3, seed=346):
    """Train aérien : pont d'appontage, atelier à l'arrière avec grue de levage et caisses à outils."""
    peau = blinde(4.0, seed)
    H = demi(cells)
    m = _pont_appontage(cells, peau)
    m.add(prism(rrect(-4.6, -H + 0.2, 4.6, -H + 7.0, 0.6), 4.4, 10.4, peau, bevel=0.5))   # atelier
    for s in (1, -1):
        m.add(box(s * 4.58, -H + 1.5, 6.0, s * 4.62, -H + 5.5, 9.0, VITRE))
    m.add(box(-3.4, -H + 7.0, 4.6, 3.4, -H + 7.05, 9.6, Mat((60, 64, 58), spec=0.2)))
    m.add(tube((3.0, -H + 3.5, 10.4), (3.0, -H + 3.5, 13.0), 0.5, mat=JAUNE, seg=6))      # grue
    m.add(tube((3.0, -H + 3.5, 13.0), (1.0, -H + 11.0, 12.0), 0.35, mat=JAUNE, seg=6))
    m.add(tube((1.0, -H + 11.0, 12.0), (1.0, -H + 11.0, 7.0), 0.06, mat=NOIR, seg=3))
    m.add(box(0.4, -H + 10.4, 6.4, 1.6, -H + 11.6, 7.0, NOIR))
    for s in (1, -1):
        for k in range(3):                                                      # caisses à outils, bouteilles
            y = H - 3.0 - k * 3.0
            m.add(box(s * 4.0 - 0.6, y - 1.0, 4.6, s * 4.0 + 0.6, y + 1.0, 5.8, ROUGE))
        m.add(tube((s * 3.9, -H + 9.0, 4.6), (s * 3.9, -H + 9.0, 7.0), 0.5, mat=Mat((40, 90, 50), spec=0.4), seg=8))
    return m


# ---------------------------------------------------------------------------
# Wagons blindés (caisse ; tourelles à part, pivot dans TOURELLES)
# ---------------------------------------------------------------------------
def wagon_blinde(cells=2, seed=350, hauteur=4.4, pente=1.2, largeur=4.6):
    peau = blinde(2.5, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=2))
    m.add(chassis(H))
    for s in (1, -1):
        m.add(box(s * largeur - 0.25 * s, -H, 1.0, s * largeur + 0.05 * s, H, 3.6, peau))
        for k in range(int(H / 2.5)):
            y = -H + 2.0 + k * 5.0
            m.add(sphere((s * (largeur - 0.4), y, 3.6 + hauteur * 0.6), 0.25, ACIER_SOMBRE, seg=4, rings=2))   # rivets
    m.add(prism(rrect(-largeur, -H + 0.2, largeur, H - 0.2, 1.0), 3.6, 3.6 + hauteur, peau, bevel=pente))
    for s in (1, -1):
        for y in (-H * 0.65, H * 0.65):
            m.add(box(s * (largeur - 0.6) - 0.1, y - 0.5, 5.6, s * (largeur - 0.6) + 0.1, y + 0.5, 6.4, NOIR))   # meurtrières
    return m


def plateforme_dca(cells=2, seed=351):
    """Plateforme antiaérienne : caisse basse à parapets, caisses de munitions."""
    peau = blinde(2.5, seed)
    H = demi(cells)
    m = Mesh()
    m.add(roulement(H, essieux=2))
    m.add(chassis(H))
    m.add(box(-4.6, -H, 3.6, 4.6, H, 4.4, peau))
    for s in (1, -1):
        m.add(box(s * 4.6 - 0.6 * s, -H, 4.4, s * 4.6, H, 7.0, peau, bevel=0.2))  # parapets
        m.add(box(-4.6, s * H - 0.6 * s, 4.4, 4.6, s * H, 7.0, peau, bevel=0.2))
        for y in (-H + 3.0, H - 3.0):
            m.add(box(s * 3.0 - 0.8, y - 1.0, 4.4, s * 3.0 + 0.8, y + 1.0, 5.6, VERT_KAKI))
    return m


def zaamurets(cells=2, seed=352):
    """Zaamurets : automotrice blindée (moteur intégré), caisse haute rivetée, deux coupoles."""
    m = wagon_blinde(cells, seed, hauteur=5.6, pente=1.6, largeur=4.7)
    H = demi(cells)
    peau = blinde(2.5, seed + 1)
    m.add(prism(rrect(-2.2, -H + 0.6, 2.2, -H + 4.4, 0.5), 9.2, 10.8, peau, bevel=0.4))
    m.add(box(-1.8, -H + 0.55, 9.8, 1.8, -H + 0.65, 10.4, VITRE))
    m.add(tube((0, H * 0.45, 9.2), (0, H * 0.45, 10.8), 1.2, mat=ACIER_SOMBRE, seg=8))    # affût DCA
    for x in (-0.5, 0.5):
        m.add(tube((x, H * 0.45, 11.0), (x, H * 0.45 + 2.2, 12.6), 0.18, mat=ACIER, seg=5))
    return m


def gustav(cells=5, seed=353):
    """Schwerer Gustav : affût géant sur 2 × 2 groupes de bogies, longerons, vérins, chambre de chargement."""
    peau = blinde(4.0, seed)
    H = demi(cells)
    m = Mesh()
    for s in (1, -1):
        for k in range(4):
            m.add(bogie(s * (H - 3.6 - k * 7.6), essieux=2))
    m.add(box(-5.2, -H, 2.4, 5.2, H, 4.0, ACIER_SOMBRE))
    for s in (1, -1):
        m.add(box(-0.6, s * H, 2.6, 0.6, s * (H + 0.8), 3.3, NOIR))
        m.add(box(s * 4.2 - 1.0, -H + 1, 4.0, s * 4.2 + 1.0, H - 1, 9.6, peau, bevel=0.4))   # longerons de l'affût
        for k in range(8):
            y = -H + 4.0 + k * (2 * H - 8.0) / 7
            m.add(box(s * 5.2 - 0.2 * s, y - 0.8, 3.6, s * 5.3, y + 0.8, 9.0, ACIER_SOMBRE))  # vérins de calage
    m.add(box(-3.2, -H + 0.5, 4.0, 3.2, -H + 14.0, 11.0, peau, bevel=0.5))        # chambre de chargement
    m.add(box(-2.0, -H + 0.2, 8.0, 2.0, -H + 0.5, 9.4, NOIR))
    for k in range(3):                                                         # obus de 800 mm prêts au chargement
        m.add(tube((-2.2 + k * 2.2, -H + 15.0, 10.0), (-2.2 + k * 2.2, -H + 21.0, 10.0), 0.9, 0.5, mat=OBUS, seg=8))
    return m


# ---------------------------------------------------------------------------
# Tourelles (modélisées autour de leur pivot, à leur hauteur réelle)
# ---------------------------------------------------------------------------
def tourelle_mitrailleuse(z=8.6):
    peau = blinde(2.5, 360)
    m = Mesh()
    m.add(prism(R.ngon(0, 0, 2.0, 10), z, z + 1.8, peau, bevel=0.5))
    m.add(tube((0, 1.6, z + 1.0), (0, 4.2, z + 1.0), 0.18, mat=NOIR, seg=5))
    m.add(trappe(0, -0.6, z + 1.8, 0.7))
    return m


def tourelle_76(z=8.0):
    """76 mm des wagons PL-37."""
    peau = blinde(2.5, 361)
    m = Mesh()
    m.add(prism(R.ngon(0, 0, 3.0, 10, sy=1.1), z, z + 2.6, peau, bevel=0.7))
    m.add(canon(2.6, 10.5, z + 1.2, 0.45, manchon=(0.2, 0.5)))
    m.add(tube((1.6, 2.4, z + 1.8), (1.6, 4.2, z + 1.8), 0.18, mat=NOIR, seg=5))
    m.add(trappe(-0.9, -1.0, z + 2.6, 0.8))
    return m


def tourelle_t34(z=8.0):
    """Tourelle de T-34-85 : coulée, arrondie, canon de 85 mm long, tourelleau."""
    peau = blinde(2.0, 362)
    m = Mesh()
    rings = []
    for zz, k in ((z, 1.0), (z + 1.6, 1.0), (z + 2.6, 0.82)):
        rings.append([(3.3 * k * math.cos(2 * math.pi * i / 14), (3.8 if math.sin(2 * math.pi * i / 14) < 0 else 3.0) * k * math.sin(2 * math.pi * i / 14), zz) for i in range(14)])
    m.add(R.loft(rings, peau))
    m.add(box(-1.2, 2.6, z + 0.5, 1.2, 3.6, z + 2.0, peau, bevel=0.3))              # masque
    m.add(canon(3.4, 13.5, z + 1.25, 0.42, frein=False))
    m.add(trappe(-1.1, -1.4, z + 2.6, 0.9))
    m.add(trappe(1.3, -1.0, z + 2.6, 0.6))
    return m


def tourelle_dca37(z=4.4):
    """Affût jumelé de 37 mm (61-K) avec bouclier."""
    peau = blinde(2.5, 363)
    m = Mesh()
    m.add(tube((0, 0, z), (0, 0, z + 1.0), 2.2, mat=ACIER_SOMBRE, seg=10))
    m.add(box(-2.0, 0.6, z + 1.0, 2.0, 1.0, z + 3.4, peau, bevel=0.2))           # bouclier
    a = math.radians(40)
    for x in (-0.7, 0.7):
        m.add(tube((x, 0.0, z + 2.0), (x, 0.0 + 7.0 * math.cos(a), z + 2.0 + 7.0 * math.sin(a)), 0.2, mat=ACIER, seg=5))
    m.add(box(-1.4, -1.6, z + 1.0, 1.4, 0.2, z + 2.4, ACIER_SOMBRE))
    return m


def tourelle_roquettes(z=6.8):
    """Lance-roquettes à 8 tubes (Krajina)."""
    peau = blinde(2.5, 364)
    m = Mesh()
    m.add(prism(R.ngon(0, 0, 2.6, 10), z, z + 1.8, peau, bevel=0.5))
    m.add(box(-3.0, -2.4, z + 1.8, 3.0, 4.0, z + 4.2, peau, bevel=0.3))
    for x in (-2.0, -0.7, 0.7, 2.0):
        for dz in (2.5, 3.5):
            m.add(tube((x, 4.0, z + dz), (x, 4.1, z + dz), 0.4, mat=NOIR, seg=6))
    return m


def tourelle_30mm(z=6.8):
    """Canon automatique de 30 mm (Krajina)."""
    peau = blinde(2.5, 365)
    m = Mesh()
    m.add(prism(R.ngon(0, 0, 2.4, 10), z, z + 2.2, peau, bevel=0.5))
    m.add(canon(2.0, 9.0, z + 1.2, 0.25, frein=False))
    m.add(box(-2.6, -0.6, z + 0.6, -1.6, 1.4, z + 2.0, ACIER_SOMBRE))             # caisson de munitions
    m.add(trappe(0.6, -1.0, z + 2.2, 0.6))
    return m


def tourelle_zaamurets(z=9.2):
    peau = blinde(2.5, 366)
    m = Mesh()
    m.add(prism(rrect(-3.6, -3.4, 3.6, 3.8, 1.2), z, z + 3.0, peau, bevel=0.9))
    for x in (-1.3, 1.3):
        m.add(canon(3.6, 12.0, z + 1.4, 0.5, x=x, manchon=(0.15, 0.45)))
    m.add(trappe(0.0, -1.6, z + 3.0, 0.9))
    return m


def tourelle_gustav(z=9.6):
    """Berceau et tube de 800 mm (32,5 m), pointé haut."""
    peau = blinde(3.0, 367)
    m = Mesh()
    m.add(box(-3.2, -6.0, z, 3.2, 5.0, z + 4.4, peau, bevel=0.6))
    m.add(tube((-3.8, 0, z + 2.6), (3.8, 0, z + 2.6), 1.6, mat=ACIER_SOMBRE, seg=10))
    a = math.radians(30)
    L = 52.0
    y1, z1 = 3.0 + L * math.cos(a), z + 2.6 + L * math.sin(a)
    m.add(tube((0, -4.0, z + 2.6 - 2.0), (0, y1, z1), 2.4, 1.6, mat=ACIER, seg=12))
    m.add(tube((0, 1.0, z + 2.6), (0, 8.0, z + 2.6 + 7.0 * math.tan(a)), 3.0, mat=ACIER_SOMBRE, seg=12))
    return m


# Voiture : (fonction de caisse, nombre de cases, tourelle ou None, position du pivot (px de base),
# bout du tube par rapport au pivot (y, z)). Le pivot est en unités de base (x, z × ECHELLE_XZ au rendu).
VOITURES = {
    # trains de transport
    "locomotive.diesel": (loco_capot, 2, None),
    "wagon.lourd": (lambda: plateforme(3, 340), 3, None),
    "wagon.leger": (lambda: voiture(2, 341), 2, None),
    "locomotive.rub": (loco_double, 2, None),
    "wagon.lourd.rub": (lambda: plateforme(2, 370), 2, None),
    "wagon.leger.rub": (lambda: voiture(2, 371), 2, None),
    "locomotive.eli": (loco_profilee, 2, None),
    "wagon.lourd.eli": (lambda: plateforme(3, 372, rive=0.8), 3, None),
    "wagon.leger.eli": (lambda: voiture(1, 373), 1, None),
    "locomotive.aerienne": (lambda: loco_profilee(2, 374, radar=True), 2, None),
    "locomotive.nucleaire": (loco_nucleaire, 3, None),
    "wagon.munitions": (plateforme_munitions, 3, None),
    "wagon.carburant": (citerne, 2, None),
    "wagon.atelier.aerien": (plateforme_atelier, 3, None),
    "locomotive.ana": (loco_industrielle, 2, None),
    "wagon.industriel": (industriel, 3, None),
    "wagon.leger.ana": (lambda: voiture(2, 375, blindee=True), 2, None),
    "locomotive.aus": (lambda: loco_capot(2, 376, bande=BLEU), 2, None),
    "wagon.lourd.aus": (lambda: plateforme(2, 377), 2, None),
    "wagon.leger.aus": (lambda: voiture(2, 378), 2, None),
    "wagon.bateaux": (porte_bateaux, 3, None),
    # trains blindés (compositions : rubenie/elielistan/ananthanie trains.yaml)
    "bp42": (lambda: loco_blindee(2, 380), 2, (tourelle_mitrailleuse, (0.0, 6.0, 0.0), (4.2, 9.6))),
    "pl37": (lambda: wagon_blinde(2, 381), 2, (tourelle_76, (0.0, 0.0, 0.0), (10.5, 9.2))),
    "plateforme.dca": (plateforme_dca, 2, (tourelle_dca37, (0.0, 0.0, 0.0), (5.4, 10.9))),
    "bp43": (lambda: loco_blindee(2, 382, hauteur=4.6), 2, (tourelle_mitrailleuse, (0.0, 6.0, -0.4), (4.2, 9.2))),
    "wagon.t34": (lambda: wagon_blinde(2, 383, hauteur=4.4, largeur=4.8), 2, (tourelle_t34, (0.0, 0.0, 0.0), (13.5, 9.25))),
    "zaamurets": (zaamurets, 2, (tourelle_zaamurets, (0.0, -2.0, 0.0), (12.0, 10.6))),
    "krajina": (lambda: loco_blindee(2, 384, hauteur=3.6, pente=1.8), 2, (tourelle_mitrailleuse, (0.0, 6.0, -1.4), (4.2, 8.2))),
    "krajina.roquettes": (lambda: wagon_blinde(2, 385, hauteur=3.2, pente=1.6), 2, (tourelle_roquettes, (0.0, 0.0, 0.0), (4.1, 9.8))),
    "krajina.canon": (lambda: wagon_blinde(2, 386, hauteur=3.2, pente=1.6), 2, (tourelle_30mm, (0.0, 0.0, 0.0), (9.0, 8.0))),
    "d311": (d311, 2, None),
    "gustav": (gustav, 5, (tourelle_gustav, (0.0, 4.0, 0.0), (48.0, 38.2))),
}
