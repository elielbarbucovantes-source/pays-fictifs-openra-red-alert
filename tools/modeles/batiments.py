"""Modèles 3D des bâtiments et défenses (projection oblique, repère : centre de l'emprise,
y vers le nord, 1 unité = 1 pixel, une case = 24)."""
import math
import random
import rendu3d as R
from rendu3d import Mesh, Mat, merge, prism, box, tube, sphere, rrect, ngon, panels, noise, combine, stripes
from modeles.communs import ACIER, ACIER_SOMBRE, GRILLE, VITRE, BACHE, BOIS, JERRICAN, FEU_ROUGE, antenne
from modeles.rubenie import montagne, SACS, VERT_RUB, BLANC

BETON = Mat((150, 146, 136), spec=0.1, tex=combine(panels(6.0, 0.35, 0.86), noise(201, 0.07, 1.2)))
BETON_SOMBRE = Mat((110, 106, 98), spec=0.1, tex=combine(panels(6.0, 0.35, 0.86), noise(202, 0.07, 1.2)))
TERRE = Mat((104, 84, 58), spec=0.02, tex=noise(203, 0.14, 0.9))
TERRE_SOMBRE = Mat((66, 52, 36), spec=0.02, tex=noise(204, 0.14, 0.9))
CAMO = Mat((74, 88, 56), spec=0.02, flat=True, tex=noise(205, 0.25, 0.7))
TOLE = Mat((126, 130, 124), spec=0.3, tex=stripes(1.2, 0.82, axis=0))
LAMPE = Mat((255, 236, 150), emit=True)
LAMPE_R = Mat((255, 60, 40), emit=True)


def blinde_b(seed):
    return R.team(spec=0.25, tex=combine(panels(4.0, 0.35, 0.84), noise(seed, 0.05)))


def sacs_ligne(x0, y0, x1, y1, z=0.0, rangs=2, taille=1.4):
    """Rangée de sacs de sable (empilés en quinconce)."""
    m = Mesh()
    L = math.hypot(x1 - x0, y1 - y0)
    n = max(1, int(L / (taille * 1.1)))
    for r in range(rangs):
        for i in range(n + (r % 2 == 0)):
            t = (i + (0.5 if r % 2 else 0.0)) / max(1, n)
            if t > 1.0:
                continue
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            m.add(sphere((x, y, z + 0.5 + r * 0.8), taille * 0.62, SACS, seg=6, rings=3, sz=0.55))
    return m


# ---------------------------------------------------------------------------
# Tranchée (2×1 est-ouest ; la version nord-sud est tournée)
# ---------------------------------------------------------------------------
def tranchee(nord_sud=False, etat=1.0):
    m = Mesh()
    L, W = 22.0, 9.0
    # déblai en creux : fond sombre, parois en planches, parapets de sacs
    m.add(box(-L, -W / 2, 0.0, L, W / 2, 0.05, TERRE_SOMBRE))
    m.add(box(-L + 1, -W / 2 + 1.2, 0.05, L - 1, W / 2 - 1.2, 0.12, Mat((92, 72, 46), spec=0.02, tex=stripes(1.4, 0.75, axis=0))))  # caillebotis
    for s in (1, -1):
        m.add(box(-L, s * W / 2 - 0.9, 0.0, L, s * W / 2 + 0.9, 1.2, TERRE))
        m.add(sacs_ligne(-L + 1, s * (W / 2 + 0.2), L - 1, s * (W / 2 + 0.2), 1.0, rangs=2 if etat > 0.5 else 1))
    for x in (-L, L):
        m.add(box(x - 0.9, -W / 2, 0.0, x + 0.9, W / 2, 1.2, TERRE))
    # poteaux et barbelés sur le parapet avant, caisses de munitions
    for x in range(-18, 20, 6):
        m.add(tube((x, -W / 2 - 1.6, 1.0), (x, -W / 2 - 1.6, 3.2), 0.18, mat=BOIS, seg=4))
    m.add(tube((-18, -W / 2 - 1.6, 2.8), (18, -W / 2 - 1.6, 2.8), 0.06, mat=ACIER_SOMBRE, seg=3))
    m.add(box(-12, 0.5, 0.1, -9.5, 2.4, 1.3, Mat((80, 86, 56), spec=0.1)))
    m.add(box(8, -1.8, 0.1, 10, 0.2, 1.1, Mat((80, 86, 56), spec=0.1)))
    if nord_sud:
        m = m.rot_z(90)
    return m


# ---------------------------------------------------------------------------
# Bunker rubénien : casemate de béton à 5 meurtrières, camouflé, sacs de sable
# ---------------------------------------------------------------------------
def bunker():
    m = Mesh()
    m.add(prism(ngon(0, 0, 11.5, 8, math.pi / 8), 0.0, 5.0, BETON, bevel=2.2))
    m.add(prism(ngon(0, 0, 8.0, 8, math.pi / 8), 5.0, 6.2, BETON_SOMBRE, bevel=1.0))
    for k in range(5):
        a = math.radians(-90 + (k - 2) * 38)
        x, y = 10.6 * math.cos(a), 10.6 * math.sin(a)
        m.add(box(-1.6, -0.5, 2.4, 1.6, 0.5, 3.3, Mat((20, 20, 20))).rot_z(math.degrees(a) + 90).move(x, y))
    m.add(sacs_ligne(-10, -12.4, 10, -12.4, 0.0, rangs=2, taille=1.6))
    # filet de camouflage sur le toit, périscope, drapeau à la montagne
    m.add(sphere((2, 2, 6.2), 5.0, CAMO, seg=8, rings=3, sz=0.25))
    m.add(tube((-3.0, 1.0, 6.2), (-3.0, 1.0, 7.6), 0.3, mat=ACIER_SOMBRE, seg=4))
    m.add(tube((6.0, 5.0, 6.0), (6.0, 5.0, 12.0), 0.12, mat=ACIER_SOMBRE, seg=3))
    m.add(box(6.1, 4.95, 10.0, 9.6, 5.05, 12.0, Mat(BLANC, flat=True)))
    m.add(montagne(7.8, 5.0, 10.9, 0.7).rot_x(90, cy=5.0, cz=10.9).move(dy=-0.1))
    return m


# ---------------------------------------------------------------------------
# Tourelles fixes : socle immobile, partie tournante rendue sur 32 orientations
# ---------------------------------------------------------------------------
def socle_casemate():
    m = Mesh()
    m.add(prism(rrect(-11, -11, 11, 11, 3.0), 0.0, 3.6, BETON, bevel=1.6))
    m.add(sacs_ligne(-10, -12.2, 10, -12.2, 0.0, rangs=1, taille=1.5))
    return m


def casemate_tourelle(recul=0.0):
    """Casemate antichar : bouclier blindé incliné et long canon de 100 mm."""
    peau = blinde_b(206)
    m = Mesh()
    m.add(prism([(-6.4, -6.0), (6.4, -6.0), (7.0, 1.0), (4.0, 6.4), (-4.0, 6.4), (-7.0, 1.0)], 3.6, 8.4, peau, bevel=2.0))
    m.add(tube((0, 5.0 - recul, 6.4), (0, 17.0 - recul, 6.4), 0.62, 0.55, mat=ACIER, seg=8))
    m.add(tube((0, 15.6 - recul, 6.4), (0, 17.4 - recul, 6.4), 1.0, mat=ACIER_SOMBRE, seg=8))
    m.add(box(-5.0, -7.6, 3.6, 5.0, -6.0, 6.0, Mat((82, 86, 60), spec=0.1)))
    m.add(montagne(3.2, -2.0, 8.42, 0.9))
    return m


def socle_aa():
    m = Mesh()
    m.add(prism(ngon(0, 0, 10.5, 12), 0.0, 2.6, BETON, bevel=1.2))
    m.add(sacs_ligne(-9, -10.8, 9, -10.8, 0.0, rangs=2, taille=1.5))
    return m


def aa_tourelle(recul=0.0):
    """Canon antiaérien bitube de 57 mm sur affût tournant, radar de conduite de tir."""
    peau = blinde_b(207)
    m = Mesh()
    m.add(tube((0, 0, 2.6), (0, 0, 4.2), 5.6, 5.2, mat=peau, seg=14))
    m.add(box(-3.6, -3.8, 4.2, 3.6, 2.4, 8.0, peau, bevel=0.8))
    for x in (-1.3, 1.3):
        m.add(tube((x, 1.6 - recul, 7.0), (x, 13.0 - recul, 12.4), 0.42, mat=ACIER, seg=6))
        m.add(tube((x, 11.8 - recul, 11.8), (x, 13.4 - recul, 12.6), 0.62, mat=ACIER_SOMBRE, seg=6))
    m.add(tube((0, -3.8, 8.0), (0, -3.8, 9.2), 0.3, mat=ACIER_SOMBRE, seg=4))
    m.add(tube((0, -4.1, 9.8), (0, -3.5, 9.8), 1.8, mat=Mat((170, 174, 166), spec=0.4), seg=12))
    return m


def socle_sam():
    """Emprise 2×1 : dalle, cabine de conduite, conteneurs de rechange."""
    m = Mesh()
    m.add(prism(rrect(-22, -10, 22, 10, 2.0), 0.0, 1.6, BETON, bevel=0.6))
    m.add(prism(rrect(-21, -8, -12, 4, 1.0), 1.6, 7.4, blinde_b(208), bevel=0.8))
    m.add(tube((-16.5, -2, 7.4), (-16.5, -2, 11.0), 0.3, mat=ACIER_SOMBRE, seg=4))
    m.add(box(-19.5, -2.3, 10.4, -13.5, -1.7, 12.6, Mat((170, 174, 166), spec=0.4)))
    for k in range(2):
        m.add(box(12.5, -8 + k * 5, 1.6, 21, -4 + k * 5, 4.4, Mat((84, 92, 64), spec=0.1), bevel=0.2))
    m.add(montagne(-16.5, -6.0, 7.42, 0.9))
    return m


def sam_tourelle(recul=0.0):
    """Rampe quadruple de missiles sol-air, relevée."""
    peau = blinde_b(209)
    m = Mesh()
    m.add(tube((0, 0, 1.6), (0, 0, 3.4), 4.2, 3.8, mat=peau, seg=12))
    m.add(box(-1.2, -1.0, 3.4, 1.2, 1.0, 6.0, peau))
    rampe = Mesh()
    for x in (-2.6, -0.9, 0.9, 2.6):
        rampe.add(fus(x))
    rampe.add(box(-3.8, -4.0, -0.6, 3.8, 4.0, 0.0, peau))
    m.add(rampe.rot_x(35).move(dz=6.6))
    return m


def fus(x):
    blanc = Mat((220, 222, 214), spec=0.4)
    return merge(tube((x, -4.0, 0.6), (x, 3.4, 0.6), 0.55, mat=blanc, seg=8),
                 tube((x, 3.4, 0.6), (x, 5.0, 0.6), 0.55, 0.05, mat=Mat((200, 60, 40), spec=0.3), seg=8))


def socle_cotiere():
    m = Mesh()
    m.add(prism(ngon(0, 0, 11.6, 10), 0.0, 4.4, BETON, bevel=1.8))
    m.add(prism(ngon(0, 0, 7.6, 10), 4.4, 5.2, BETON_SOMBRE))
    return m


def cotiere_tourelle(recul=0.0):
    """Artillerie côtière : tourelle navale de 152 mm posée sur un fort de béton."""
    peau = blinde_b(210)
    m = Mesh()
    m.add(prism([(-6.0, -7.0), (6.0, -7.0), (6.6, 2.0), (4.0, 6.4), (-4.0, 6.4), (-6.6, 2.0)], 5.2, 10.4, peau, bevel=2.0))
    for x in (-1.6, 1.6):
        m.add(tube((x, 5.0 - recul, 7.8), (x, 20.0 - recul, 8.8), 0.62, 0.52, mat=ACIER, seg=8))
    m.add(tube((0, -6.0, 10.4), (0, -6.0, 13.0), 0.3, mat=ACIER_SOMBRE, seg=4))
    m.add(box(-2.4, -6.3, 12.4, 2.4, -5.7, 13.6, Mat((170, 174, 166), spec=0.4)))
    m.add(montagne(0, -2.0, 10.42, 1.0))
    return m


# ---------------------------------------------------------------------------
# QG rubénien (3×3, rangée sud dégagée) : poste de commandement semi-enterré
# ---------------------------------------------------------------------------
def qg_rub(t=0.0):
    peau = blinde_b(211)
    m = Mesh()
    m.add(prism(rrect(-34, -12, 34, 34, 4.0), 0.0, 2.0, BETON_SOMBRE, bevel=0.8))          # dalle
    m.add(prism(rrect(-28, -6, 28, 30, 5.0), 2.0, 14.0, BETON, bevel=3.0))                 # blockhaus
    m.add(prism(rrect(-18, 0, 18, 24, 3.0), 14.0, 22.0, peau, bevel=1.6))                  # étage de commandement
    for k in range(6):
        m.add(box(-16 + k * 6.2, -0.1, 17.0, -12 + k * 6.2, 0.2, 19.4, VITRE))
    m.add(box(-6.0, -6.2, 2.0, 6.0, -5.8, 9.0, Mat((40, 40, 40))))                         # porte blindée
    # grand mât d'antennes, radar tournant (t = phase), paraboles
    m.add(tube((20, 20, 14), (20, 20, 44), 0.6, 0.3, mat=ACIER_SOMBRE, seg=5))
    for z in (26, 34, 40):
        m.add(box(16, 19.8, z, 24, 20.2, z + 0.4, ACIER_SOMBRE))
    radar = box(-6.0, -0.4, 0.0, 6.0, 0.4, 3.0, Mat((180, 184, 176), spec=0.5))
    m.add(radar.rot_z(t * 360).move(-8, 12, 22.4))
    m.add(tube((-8, 12, 22.0), (-8, 12, 22.6), 0.8, mat=ACIER_SOMBRE, seg=6))
    for x, y in ((24, 2), (-24, 4)):
        m.add(sphere((x, y, 16.0), 3.2, Mat((210, 212, 206), spec=0.5), seg=10, rings=5, sz=0.5).rot_x(-40, cy=y, cz=16))
    # drapeau de la Rubénie (blanc et vert, montagne)
    m.add(tube((-12, 22, 22), (-12, 22, 32), 0.3, mat=ACIER, seg=4))
    drap = Mat(BLANC, flat=True, tex=lambda p, n: VERT_RUB if p[2] < 28.2 else 1.0)
    m.add(box(-11.8, 21.9, 26.0, -3.0, 22.1, 31.6, drap))
    # sacs de sable, feux de balisage
    m.add(sacs_ligne(-30, -9.0, -8, -9.0, 2.0, rangs=2, taille=1.8))
    m.add(sacs_ligne(8, -9.0, 30, -9.0, 2.0, rangs=2, taille=1.8))
    m.add(sphere((20, 20, 44.5), 0.7, LAMPE_R if int(t * 4) % 2 == 0 else Mat((90, 20, 20)), seg=6, rings=3))
    m.add(montagne(0, 12, 22.02, 3.0))
    return m


# ---------------------------------------------------------------------------
# Centre logistique (2×3, rangée sud dégagée) : entrepôt, cuves, quai de chargement
# ---------------------------------------------------------------------------
def centre_logistique():
    peau = blinde_b(212)
    m = Mesh()
    m.add(prism(rrect(-23, -34, 23, 34, 2.0), 0.0, 1.0, BETON_SOMBRE))
    # entrepôt à toit en tôle ondulée arrondi (hangar)
    rings = []
    for y in (-2.0, 30.0):
        rings.append([(15.0 * math.cos(math.pi * i / 12), y, 1.0 + 13.0 * math.sin(math.pi * i / 12)) for i in range(13)])
    m.add(R.loft([list(reversed(r)) for r in rings], Mat((150, 154, 146), spec=0.35, tex=stripes(1.2, 0.8, axis=0))))
    m.add(box(-8, -2.2, 1.0, 8, -1.8, 10.0, peau))                                   # façade
    m.add(box(-5, -2.5, 1.0, 5, -2.1, 8.0, Mat((50, 52, 50), tex=stripes(0.8, 0.8, axis=2))))   # rideau
    # cuves de carburant et caisses palettisées
    for x, y in ((18, 22), (18, 12)):
        m.add(tube((x, y, 1.0), (x, y, 10.0), 3.8, mat=peau, seg=14))
        m.add(sphere((x, y, 10.0), 3.8, peau, seg=14, rings=4, sz=0.3))
    for k in range(6):
        x, y = -20 + (k % 3) * 3.4, -14 + (k // 3) * 3.6
        m.add(box(x, y, 1.0, x + 3.0, y + 3.0, 3.8, Mat((110, 96, 62), spec=0.05, tex=stripes(0.7, 0.8, axis=2))))
    m.add(box(8, -18, 1.0, 20, -8, 5.0, JERRICAN, bevel=0.3))                         # conteneur
    # grue de quai et projecteur
    m.add(tube((-18, 6, 1.0), (-18, 6, 18.0), 0.6, mat=Mat((214, 170, 40), spec=0.3), seg=6))
    m.add(box(-18.4, -8, 17.0, -17.6, 8, 17.8, Mat((214, 170, 40), spec=0.3)))
    m.add(tube((-18, -7, 17.0), (-18, -7, 10.0), 0.08, mat=ACIER_SOMBRE, seg=3))
    m.add(montagne(0, 14, 14.05, 2.2))
    return m


# ---------------------------------------------------------------------------
# Australouis
# ---------------------------------------------------------------------------
BLEU_AUS = (22, 80, 156)
PONTON = Mat((104, 106, 100), spec=0.15, tex=combine(panels(4.0, 0.35, 0.84), noise(221, 0.06)))


def chantier_avance(t=0.0):
    """Chantier naval avancé (3×3, sur l'eau) : cale sèche, portique géant, coque en construction,
    atelier aux couleurs du joueur. La phase t anime les lampes de soudure."""
    peau = blinde_b(222)
    m = Mesh()
    # quais en U autour d'un bassin, sur pilotis
    for x0, x1 in ((-34, -20), (20, 34)):
        m.add(prism(rrect(x0, -34, x1, 34, 1.5), -0.5, 3.0, PONTON, bevel=0.6))
    m.add(prism(rrect(-34, 20, 34, 34, 1.5), -0.5, 3.0, PONTON, bevel=0.6))
    # coque de navire en construction dans le bassin (demi-coque, membrures)
    for k in range(9):
        y = -24 + k * 5.0
        w = 9.0 if k < 7 else 9.0 - (k - 6) * 3.0
        m.add(box(-w, y - 0.4, 0.0, w, y + 0.4, 5.0, ACIER_SOMBRE))
    m.add(box(-9.0, -26, 0.0, 9.0, 14, 1.0, Mat((150, 40, 36), spec=0.2)))
    m.add(box(-9.2, -26, 1.0, -8.6, 14, 5.0, Mat((120, 124, 128), spec=0.3, tex=panels(3.0, 0.3, 0.85))))
    # portique roulant jaune enjambant le bassin
    jaune = Mat((214, 170, 40), spec=0.3, tex=stripes(1.4, 0.4, axis=2))
    for x in (-27, 27):
        for y in (-6, 2):
            m.add(box(x - 1.0, y - 1.0, 3.0, x + 1.0, y + 1.0, 30.0, jaune))
    m.add(box(-28, -7, 28.0, 28, 3, 31.0, jaune))
    m.add(box(-4, -5, 24.0, 4, 1, 28.0, ACIER_SOMBRE))                               # chariot
    m.add(tube((0, -2, 24.0), (0, -2, 8.0), 0.12, mat=ACIER_SOMBRE, seg=3))
    # atelier et bureaux aux couleurs du joueur, étoile de rang
    m.add(prism(rrect(-32, 21, -6, 33, 1.0), 3.0, 14.0, peau, bevel=0.8))
    m.add(prism(rrect(8, 22, 32, 33, 1.0), 3.0, 10.0, peau, bevel=0.6))
    for k in range(5):
        m.add(box(-30 + k * 5, 20.8, 9.0, -27 + k * 5, 21.0, 11.0, VITRE))
    m.add(sphere((-19, 27, 14.4), 2.2, Mat((236, 196, 40), spec=0.6, shine=30), seg=10, rings=4, sz=0.4))
    # grues de quai, bittes d'amarrage, feux de soudure
    for x in (-27, 27):
        m.add(tube((x, -28, 3.0), (x, -28, 18.0), 0.7, mat=jaune, seg=6))
        m.add(box(x - 0.5, -28, 17.0, x + 0.5 - (0 if x < 0 else 12), -27, 18.0, jaune) if x < 0 else
              box(x - 12, -28.5, 17.0, x + 0.5, -27.5, 18.0, jaune))
    for (x, y) in ((-6, -10), (5, 4), (-3, 8)):
        on = (int(t * 8) + x) % 3 == 0
        m.add(sphere((x, y, 5.6), 0.8, Mat((200, 230, 255), emit=True) if on else ACIER_SOMBRE, seg=6, rings=3))
    return m


def centrale_enrichissement(t=0.0):
    """Centrale d'enrichissement (3×3, rangée sud dégagée) : hall de centrifugeuses, cuves de
    minerai lumineuses (animées), cheminée, conduites."""
    peau = blinde_b(223)
    m = Mesh()
    m.add(prism(rrect(-34, -12, 34, 34, 3.0), 0.0, 1.5, BETON_SOMBRE))
    m.add(prism(rrect(-30, 4, 10, 32, 2.0), 1.5, 14.0, peau, bevel=1.4))               # hall
    m.add(box(-30, 4, 14.0, 10, 32, 14.6, Mat((120, 124, 118), spec=0.2, tex=stripes(2.0, 0.8, axis=0))))
    for k in range(6):                                                                   # centrifugeuses en façade
        m.add(tube((-27 + k * 6.4, 2.0, 1.5), (-27 + k * 6.4, 2.0, 11.0), 1.8, mat=Mat((196, 200, 196), spec=0.6, shine=30), seg=10))
        m.add(sphere((-27 + k * 6.4, 2.0, 11.0), 1.8, Mat((196, 200, 196), spec=0.6, shine=30), seg=10, rings=3, sz=0.5))
    # cuves de minerai en cours d'enrichissement (lueur dorée pulsante)
    lueur = 0.6 + 0.4 * math.sin(t * 2 * math.pi)
    for (x, y) in ((20, 22), (28, 12), (20, 4)):
        m.add(tube((x, y, 1.5), (x, y, 9.0), 4.6, mat=ACIER, seg=14))
        m.add(tube((x, y, 9.0), (x, y, 9.3), 3.8, mat=Mat((int(240 * lueur), int(190 * lueur), int(60 * lueur)), emit=True), seg=14))
    # conduites et cheminée
    m.add(tube((10, 20, 8.0), (16, 22, 8.0), 0.8, mat=ACIER_SOMBRE, seg=6))
    m.add(tube((10, 12, 6.0), (24, 12, 6.0), 0.8, mat=ACIER_SOMBRE, seg=6))
    m.add(tube((-24, 24, 14.0), (-24, 24, 32.0), 2.2, 1.6, mat=BETON, seg=10))
    for z in (26, 30):
        m.add(tube((-24, 24, z), (-24, 24, z + 1.0), 2.0, mat=Mat((200, 40, 36), spec=0.2), seg=10))
    m.add(sphere((-24, 24, 32.5), 0.6, LAMPE_R if int(t * 4) % 2 == 0 else Mat((90, 20, 20)), seg=6, rings=3))
    return m


def base_aeronavale(t=0.0):
    """Base aéronavale (3×2) : piste et parking, tour de contrôle à radar tournant (t),
    hangar à toit arrondi aux couleurs du joueur, marquages bleu océan."""
    peau = blinde_b(224)
    m = Mesh()
    def piste(p, n):
        if abs(p[1] + 8.0) < 0.35 and int(p[0] / 4) % 2 == 0:
            return (236, 236, 226)
        if abs(p[1] + 16.0) < 0.3 or abs(p[1] + 0.2) < 0.3:
            return (220, 196, 60)
        return 1.0
    m.add(prism(rrect(-36, -24, 36, 24, 2.0), 0.0, 1.0, Mat((88, 90, 88), spec=0.1, tex=combine(piste, noise(225, 0.06)))))
    # hangar
    rings = []
    for x in (-34.0, -10.0):
        rings.append([(x, 10.0 * math.cos(math.pi * i / 12) + 12.0, 1.0 + 11.0 * math.sin(math.pi * i / 12)) for i in range(13)])
    m.add(R.loft(rings, peau))
    m.add(box(-10.2, 3.0, 1.0, -9.8, 21.0, 9.0, Mat((50, 52, 50), tex=stripes(0.9, 0.8, axis=2))))
    # tour de contrôle
    m.add(prism(rrect(22, 8, 30, 20, 1.0), 1.0, 18.0, BETON, bevel=0.6))
    m.add(prism(rrect(20.5, 6.5, 31.5, 21.5, 1.4), 18.0, 22.0, Mat((50, 84, 110), spec=1.0, shine=40)))
    m.add(box(20.5, 6.5, 22.0, 31.5, 21.5, 22.8, peau))
    radar = box(-4.5, -0.4, 0.0, 4.5, 0.4, 2.4, Mat((180, 184, 176), spec=0.5))
    m.add(radar.rot_z(t * 360).move(26, 14, 24.2))
    m.add(tube((26, 14, 22.8), (26, 14, 24.2), 0.5, mat=ACIER_SOMBRE, seg=5))
    # manche à air, feux de piste, avion garé
    m.add(tube((6, 18, 1.0), (6, 18, 9.0), 0.2, mat=ACIER, seg=4))
    m.add(tube((6.2, 18, 8.4), (10, 18, 7.8), 0.7, 0.35, mat=Mat((236, 110, 40), spec=0.1), seg=6))
    for x in range(-32, 36, 8):
        m.add(sphere((x, -21.5, 1.3), 0.5, LAMPE if (int(t * 8) + x // 8) % 4 == 0 else Mat((120, 110, 70)), seg=5, rings=3))
    return m


# ---------------------------------------------------------------------------
# Chantier de construction rubénien (3×3) : usine de préfabrication sous grue
# ---------------------------------------------------------------------------
def fact_rub(t=0.0):
    peau = blinde_b(226)
    m = Mesh()
    m.add(prism(rrect(-35, -35, 35, 35, 3.0), 0.0, 1.6, BETON_SOMBRE))
    m.add(prism(rrect(-30, -8, 12, 30, 3.0), 1.6, 16.0, peau, bevel=2.0))                    # atelier principal
    m.add(box(-30, -8, 16.0, 12, 30, 16.8, Mat((120, 124, 118), spec=0.2, tex=stripes(2.0, 0.8, axis=1))))
    m.add(box(-18, -8.4, 1.6, -2, -8.0, 12.0, Mat((50, 52, 50), tex=stripes(0.9, 0.8, axis=2))))   # grande porte
    m.add(prism(rrect(14, 10, 32, 30, 2.0), 1.6, 10.0, BETON, bevel=1.0))                     # bureaux
    for k in range(3):
        m.add(box(16 + k * 5.4, 9.8, 6.0, 19.6 + k * 5.4, 10.0, 8.0, VITRE))
    # grue à tour qui pivote (animation « build »)
    m.add(prism(rrect(-2, -2, 2, 2, 0.3), 16.8, 42.0, Mat((214, 170, 40), spec=0.3, tex=stripes(1.5, 0.45, axis=2))))
    fleche = merge(box(-2, -1, 0, 26, 1, 1.6, Mat((214, 170, 40), spec=0.3)), box(-10, -1.4, -0.4, -2, 1.4, 2.4, BETON_SOMBRE),
                   tube((22, 0, 0), (22, 0, -14 - 6 * math.sin(t * math.pi)), 0.1, mat=ACIER_SOMBRE, seg=3),
                   box(20, -2, -16 - 6 * math.sin(t * math.pi), 24, 2, -14 - 6 * math.sin(t * math.pi), peau))
    m.add(fleche.rot_z(-40 + 80 * t).move(0, 0, 42.0))
    # parc de matériaux : poutrelles, conteneurs, drapeau
    for k in range(4):
        m.add(box(-32, -30 + k * 2.2, 1.6 + (k % 2) * 1.2, -12, -28.6 + k * 2.2, 2.6 + (k % 2) * 1.2, ACIER))
    m.add(box(4, -30, 1.6, 18, -22, 7.0, JERRICAN, bevel=0.3))
    m.add(tube((26, 28, 10.0), (26, 28, 22.0), 0.3, mat=ACIER, seg=4))
    drap = Mat(BLANC, flat=True, tex=lambda p, n: VERT_RUB if p[2] < 18.2 else 1.0)
    m.add(box(26.2, 27.9, 16.0, 34.0, 28.1, 21.6, drap))
    m.add(montagne(-9, 11, 16.82, 4.0))
    return m


# ---------------------------------------------------------------------------
# Système pétrole et zone industrielle (communs à tous les pays)
# ---------------------------------------------------------------------------
def raffinerie_petrole(t=0.0):
    """Raffinerie de pétrole (2×2) : colonnes de distillation, torchère animée, cuves, conduites."""
    peau = blinde_b(227)
    m = Mesh()
    m.add(prism(rrect(-23, -23, 23, 23, 2.0), 0.0, 1.2, BETON_SOMBRE))
    for (x, y, r, h) in ((-12, 10, 3.2, 26), (-4, 12, 2.4, 20), (-12, 0, 2.0, 16)):
        m.add(tube((x, y, 1.2), (x, y, h), r, r * 0.9, mat=Mat((196, 198, 190), spec=0.5, shine=24, tex=stripes(4.0, 0.9, axis=2)), seg=12))
        for z in range(6, int(h), 5):
            m.add(tube((x, y, z), (x, y, z + 0.4), r + 0.6, mat=ACIER_SOMBRE, seg=12))
    for (x, y) in ((10, 12), (16, 2)):
        m.add(tube((x, y, 1.2), (x, y, 8.0), 5.0, mat=peau, seg=16))
        m.add(sphere((x, y, 8.0), 5.0, peau, seg=16, rings=4, sz=0.25))
    m.add(tube((-4, 12, 14.0), (10, 12, 8.0), 0.6, mat=ACIER_SOMBRE, seg=6))
    m.add(tube((-12, 0, 8.0), (16, 2, 6.0), 0.6, mat=ACIER_SOMBRE, seg=6))
    # torchère : flamme vacillante
    m.add(tube((18, -16, 1.2), (18, -16, 28.0), 0.5, mat=ACIER, seg=5))
    f = 0.7 + 0.3 * math.sin(t * 2 * math.pi * 2)
    m.add(sphere((18, -16, 29.5), 1.6 * f + 0.4, Mat((255, 150, 40), emit=True), seg=6, rings=4, sz=1.6))
    m.add(sphere((18, -16, 29.3), 0.9 * f, Mat((255, 236, 140), emit=True), seg=6, rings=3, sz=1.4))
    # pompes et vannes
    for k in range(3):
        m.add(box(-18 + k * 5, -18, 1.2, -15 + k * 5, -14, 4.0, Mat((200, 40, 36), spec=0.3)))
    return m


def pipeline(mask, degats=False):
    """Segment de pipeline (1 case) : masque 1 = nord, 2 = est, 4 = sud, 8 = ouest (comme les murs)."""
    m = Mesh()
    tuyau = Mat((104, 108, 110), spec=0.55, shine=26, tex=stripes(6.0, 0.9, axis=0 if mask in (2, 8, 10) else 1))
    z = 3.2
    dirs = [(1, (0, 12)), (2, (12, 0)), (4, (0, -12)), (8, (-12, 0))]
    for bit, (dx, dy) in dirs:
        if mask & bit:
            m.add(tube((0, 0, z), (dx, dy, z), 1.8, mat=tuyau, seg=10))
            m.add(tube((dx * 0.55, dy * 0.55, z), (dx * 0.6, dy * 0.6, z), 2.2, mat=ACIER_SOMBRE, seg=10))   # bride
    if bin(mask).count("1") != 2 or mask in (0, 1, 2, 4, 8) or mask not in (5, 10):
        m.add(sphere((0, 0, z), 2.2, tuyau, seg=10, rings=6))
        m.add(tube((0, 0, z + 1.8), (0, 0, z + 3.2), 0.5, mat=Mat((200, 40, 36), spec=0.3), seg=6))     # vanne
    # supports
    m.add(box(-1.6, -0.8, 0.0, 1.6, 0.8, z - 1.4, BETON))
    if degats:
        m = R.endommage(m, 7)
        m.add(sphere((0.6, 0.4, z + 0.6), 1.2, Mat((20, 18, 16), spec=0.6, shine=30), seg=6, rings=3, sz=0.4))   # fuite
        m.add(prism(ngon(2.0, -2.0, 3.0, 8, sy=0.6), 0.02, 0.06, Mat((16, 14, 12), spec=0.8, shine=40)))
    return m


def zone_industrielle(t=0.0):
    """Zone industrielle neutre (3×2) : usines à sheds, cheminées fumantes (t), parc de conteneurs."""
    m = Mesh()
    m.add(prism(rrect(-36, -24, 36, 24, 2.0), 0.0, 0.8, BETON_SOMBRE))
    brique = Mat((150, 84, 60), spec=0.05, tex=combine(panels(2.0, 0.25, 0.88), noise(228, 0.08)))
    m.add(prism(rrect(-34, -4, -2, 22, 1.0), 0.8, 11.0, brique, bevel=0.4))
    for k in range(5):                                                                  # toit en sheds
        x0 = -34 + k * 6.4
        m.add(R.loft([[(x0, -4, 11.0), (x0 + 6.4, -4, 11.0), (x0 + 6.4, -4, 15.0)], [(x0, 22, 11.0), (x0 + 6.4, 22, 11.0), (x0 + 6.4, 22, 15.0)]],
                     Mat((130, 132, 128), spec=0.3, tex=stripes(1.0, 0.85, axis=0))))
        m.add(box(x0 + 6.2, -4, 11.0, x0 + 6.4, 22, 15.0, VITRE))
    m.add(prism(rrect(4, 2, 34, 22, 1.0), 0.8, 8.0, Mat((120, 124, 120), spec=0.2, tex=stripes(1.2, 0.85, axis=1))))
    for (x, y) in ((-28, 18), (-20, 18)):
        m.add(tube((x, y, 11.0), (x, y, 30.0), 1.8, 1.4, mat=brique, seg=10))
        for k in range(3):                                                              # fumée
            z = 31.0 + k * 3.0 + (t * 3.0) % 3.0
            g = 150 + k * 20
            m.add(sphere((x + k * 1.4 + t, y + k * 0.8, z), 1.6 + k * 0.7, Mat((g, g, g - 6), spec=0.0, flat=True), seg=6, rings=3))
    for k in range(6):                                                                  # conteneurs
        x, y = 6 + (k % 3) * 9, -20 + (k // 3) * 6
        c = ((170, 60, 40), (40, 90, 150), (200, 150, 40), (60, 120, 70))[k % 4]
        m.add(box(x, y, 0.8, x + 8, y + 4, 4.4, Mat(c, spec=0.2, tex=stripes(0.6, 0.85, axis=0))))
    return m
