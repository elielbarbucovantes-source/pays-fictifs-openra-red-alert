"""Modèles 3D des unités de l'Elielistan : blindés aux couleurs du joueur, détails olive et acier,
aigle doré en insigne. Les tourelles sont modélisées autour de leur pivot (0, 0, 0)."""
import math
import rendu3d as R
from rendu3d import Mesh, Mat, merge, prism, box, tube, sphere, rrect, ngon, panels, noise, combine, stripes, wing
from modeles.communs import (blinde, chenilles, canon, antenne, trappe, roue, ACIER, ACIER_SOMBRE, GRILLE, CAOUTCHOUC,
                             JERRICAN, FEU_ROUGE, BACHE, VITRE, fuselage, verriere, missile, GRIS_FONCE, TUYERE)

OLIVE = Mat((92, 96, 62), spec=0.2, tex=combine(panels(4.0, 0.3, 0.85), noise(41, 0.06)))
OR = Mat((212, 170, 50), spec=0.7, shine=30)
PHARE = Mat((230, 226, 190), spec=0.8, shine=30)


def aigle(x, y, z, k=1.0):
    """Petit insigne doré (aigle stylisé) sur une surface horizontale."""
    m = Mesh()
    m.add(box(x - 0.25 * k, y - 0.6 * k, z, x + 0.25 * k, y + 0.6 * k, z + 0.08, OR))
    for s in (1, -1):
        m.add(box(min(x, x + s * 1.1 * k), y - 0.1 * k, z, max(x, x + s * 1.1 * k), y + 0.35 * k, z + 0.08, OR))
    return m


# ---------------------------------------------------------------------------
# Gavilán : voiture de reconnaissance 4×4 blindée, tourelleau mitrailleuse
# ---------------------------------------------------------------------------
def gavilan_caisse():
    peau = blinde(3.0, 42)
    m = Mesh()
    prof = [(-3.4, -6.4), (3.4, -6.4), (3.6, 3.0), (2.6, 6.6), (-2.6, 6.6), (-3.6, 3.0)]
    m.add(prism(prof, 1.4, 3.6, peau, bevel=0.5))
    # capot moteur incliné et pare-brise blindé
    m.add(prism([(-2.8, 2.6), (2.8, 2.6), (2.4, 6.0), (-2.4, 6.0)], 3.6, 4.0, peau, bevel=0.2))
    m.add(box(-2.6, 1.8, 3.6, 2.6, 2.4, 5.0, VITRE))
    # habitacle surélevé arrière
    m.add(prism(rrect(-3.0, -5.6, 3.0, 2.0, 0.8), 3.6, 5.4, peau, bevel=0.5))
    # roues, garde-boue, roue de secours, jerricans, antenne
    for s in (1, -1):
        for y in (-4.0, 4.0):
            m.add(roue(s * 3.4, y, 1.4, 1.4, 1.0 * s / abs(s), s))
        m.add(box(s * 3.9 - 0.5, 4.5, 3.4, s * 3.9 + 0.5, 6.3, 3.8, peau))
    m.add(tube((0, -6.5, 3.2), (0, -7.4, 3.2), 1.3, mat=CAOUTCHOUC, seg=10))
    m.add(box(2.2, -6.2, 5.4, 3.0, -4.4, 6.4, JERRICAN, bevel=0.1))
    m.add(antenne(-2.4, -4.6, 5.4, 6.0))
    for s in (1, -1):
        m.add(tube((s * 2.0, 6.5, 3.0), (s * 2.0, 6.9, 3.0), 0.45, mat=PHARE, seg=6))
    m.add(aigle(0, 4.4, 4.02, 0.8))
    return m


def gavilan_tourelle():
    """Tourelleau ouvert avec bouclier et mitrailleuse lourde."""
    peau = blinde(2.5, 43)
    m = Mesh()
    m.add(tube((0, 0, 0), (0, 0, 0.7), 1.7, 1.6, mat=peau, seg=12))
    m.add(box(-1.8, 0.8, 0.5, 1.8, 1.3, 2.3, peau, bevel=0.15))           # bouclier
    m.add(tube((0, 0.6, 1.5), (0, 4.6, 1.5), 0.22, mat=ACIER_SOMBRE, seg=5))
    m.add(box(-0.4, -0.6, 1.1, 0.4, 0.8, 1.9, ACIER_SOMBRE))
    m.add(box(0.6, -0.4, 1.0, 1.3, 0.3, 1.5, JERRICAN))                     # caisse de munitions
    return m


# ---------------------------------------------------------------------------
# Halcón AT : chasseur de chars chenillé bas, lanceur de missiles escamotable
# ---------------------------------------------------------------------------
def halcon_caisse():
    peau = blinde(4.0, 44)
    m = Mesh()
    prof = [(-4.4, -8.4), (4.4, -8.4), (4.4, 5.6), (3.2, 8.6), (-3.2, 8.6), (-4.4, 5.6)]
    m.add(prism(prof, 1.4, 3.8, peau, bevel=1.0))
    tr = chenilles(17.0, 1.8, 3.6, 5.2, 0.0, wheels=5, skirt=(1.9, 3.5), mat_skirt=peau)
    m.add(tr).add(tr.mirror_x())
    # filets de camouflage roulés et branchages
    m.add(tube((-3.8, -7.8, 4.2), (3.8, -7.8, 4.2), 0.7, mat=BACHE, seg=8))
    for k, (x, y) in enumerate(((-3.0, 4.0), (3.2, -2.0), (-2.6, -5.0))):
        m.add(sphere((x, y, 4.2), 0.9, Mat((58, 80, 40), spec=0.02, tex=noise(45 + k, 0.2, 0.8)), seg=6, rings=3))
    m.add(box(-3.6, -8.2, 3.8, -1.2, -6.6, 4.6, GRILLE))
    m.add(aigle(2.0, 5.0, 3.82, 0.7))
    return m


def halcon_tourelle():
    """Affût bas avec deux tubes de missiles antichar et viseur thermique."""
    peau = blinde(2.5, 46)
    m = Mesh()
    m.add(prism(ngon(0, 0, 2.3, 8), 0.0, 1.0, peau, bevel=0.4))
    for s in (1, -1):
        m.add(tube((s * 1.5, -1.6, 1.8), (s * 1.5, 4.2, 1.8), 0.55, mat=Mat((88, 92, 70), spec=0.2), seg=8))
        m.add(tube((s * 1.5, 4.2, 1.8), (s * 1.5, 4.3, 1.8), 0.4, mat=Mat((30, 30, 30)), seg=8))
    m.add(box(-0.7, 0.0, 1.0, 0.7, 1.6, 2.2, ACIER_SOMBRE))
    m.add(box(-0.5, 1.6, 1.4, 0.5, 1.8, 2.0, VITRE))
    return m


# ---------------------------------------------------------------------------
# Jaguar : char moyen rapide, tourelle oscillante à long canon de 90 mm
# ---------------------------------------------------------------------------
def jaguar_caisse():
    peau = blinde(4.0, 47)
    m = Mesh()
    prof = [(-4.2, -8.6), (4.2, -8.6), (4.2, 5.0), (2.6, 8.8), (-2.6, 8.8), (-4.2, 5.0)]
    m.add(prism(prof, 1.5, 4.0, peau, bevel=1.1))
    tr = chenilles(17.6, 1.9, 3.8, 5.1, 0.0, wheels=5)
    m.add(tr).add(tr.mirror_x())
    for s in (1, -1):
        m.add(box(min(s * 4.1, s * 6.1), -8.8, 3.6, max(s * 4.1, s * 6.1), 8.4, 4.0, peau))
        m.add(box(s * 5.1 - 0.9, -6.5, 4.0, s * 5.1 + 0.9, -2.5, 4.9, Mat((72, 80, 52), spec=0.1)))   # coffres
        m.add(tube((s * 2.6, 8.7, 3.6), (s * 2.6, 9.2, 3.6), 0.4, mat=PHARE, seg=6))
    m.add(box(-3.0, -8.4, 4.0, 3.0, -5.0, 4.3, GRILLE))
    m.add(aigle(2.2, 6.0, 4.02, 0.7))
    return m


def jaguar_tourelle():
    """Tourelle oscillante : partie haute allongée qui porte le canon et le chargeur."""
    peau = blinde(3.0, 48)
    m = Mesh()
    m.add(tube((0, -0.4, 0), (0, -0.4, 0.9), 3.4, 3.2, mat=peau, seg=14))          # collier
    m.add(prism([(-2.4, -5.4), (2.4, -5.4), (2.8, -1.0), (2.2, 2.6), (-2.2, 2.6), (-2.8, -1.0)], 0.9, 3.0, peau, bevel=0.8))
    m.add(canon(2.0, 12.5, 2.0, 0.42, manchon=(0.3, 0.55)))
    m.add(trappe(-1.3, -1.6, 2.95, 0.9))
    m.add(trappe(1.3, -1.2, 2.95, 0.8))
    m.add(box(-1.9, -6.4, 1.2, 1.9, -5.3, 2.6, ACIER_SOMBRE))                      # chargeur automatique
    m.add(antenne(-2.0, -4.6, 3.0, 6.0))
    return m


# ---------------------------------------------------------------------------
# Vigía : véhicule antiaérien 6×6, lanceur de missiles sol-air et radar
# ---------------------------------------------------------------------------
def vigia_caisse():
    peau = blinde(3.5, 49)
    m = Mesh()
    m.add(prism(rrect(-3.4, -9.0, 3.4, 8.6, 1.2), 1.6, 4.2, peau, bevel=0.6))
    m.add(prism([(-3.0, 4.0), (3.0, 4.0), (2.6, 8.8), (-2.6, 8.8)], 4.2, 5.2, peau, bevel=0.3))
    m.add(box(-2.8, 7.6, 4.4, 2.8, 8.3, 5.2, VITRE))
    for s in (1, -1):
        for y in (-6.0, -2.6, 5.4):
            m.add(roue(s * 3.4, y, 1.4, 1.4, 1.0, s))
        m.add(tube((s * 2.2, 8.7, 3.6), (s * 2.2, 9.1, 3.6), 0.4, mat=PHARE, seg=6))
    m.add(box(-3.0, -8.8, 4.2, 3.0, -6.0, 4.6, GRILLE))
    # socle de tourelle arrière (pivot en -7.7, hauteur 7 : voir Turreted.Offset)
    m.add(prism(ngon(0, -7.7, 2.6, 10), 4.2, 7.0, peau, bevel=0.4))
    m.add(aigle(0, 2.0, 4.22, 0.8))
    return m


def vigia_tourelle():
    """Tourelle à deux paniers de quatre missiles, radar de veille rotatif."""
    peau = blinde(2.5, 50)
    m = Mesh()
    m.add(prism(rrect(-1.8, -1.8, 1.8, 1.8, 0.6), 0.0, 1.8, peau, bevel=0.3))
    for s in (1, -1):
        m.add(box(s * 2.0 - 0.9, -1.8, 0.6, s * 2.0 + 0.9, 2.8, 2.4, Mat((96, 100, 70), spec=0.2), bevel=0.2))
        for k in range(4):
            dx, dz = (k % 2) * 0.8 - 0.4, (k // 2) * 0.8 + 1.1
            m.add(tube((s * 2.0 + dx, 2.8, dz), (s * 2.0 + dx, 2.9, dz), 0.3, mat=Mat((200, 60, 40)), seg=6))
    m.add(tube((0, -0.6, 1.8), (0, -0.6, 3.2), 0.2, mat=ACIER_SOMBRE, seg=5))
    m.add(box(-1.8, -0.9, 3.2, 1.8, -0.4, 4.2, Mat((160, 164, 158), spec=0.4)))       # antenne radar plate
    return m


# ---------------------------------------------------------------------------
# Trueno : obusier automoteur chenillé à casemate (tir puis déplacement)
# ---------------------------------------------------------------------------
def trueno():
    peau = blinde(4.0, 51)
    m = Mesh()
    prof = [(-4.2, -8.8), (4.2, -8.8), (4.2, 6.0), (3.0, 8.6), (-3.0, 8.6), (-4.2, 6.0)]
    m.add(prism(prof, 1.5, 3.8, peau, bevel=0.9))
    tr = chenilles(17.6, 1.9, 3.7, 5.1, 0.0, wheels=6)
    m.add(tr).add(tr.mirror_x())
    # casemate arrière, canon long en élévation
    m.add(prism([(-3.8, -8.4), (3.8, -8.4), (3.8, -1.0), (2.6, 1.4), (-2.6, 1.4), (-3.8, -1.0)], 3.8, 7.2, peau, bevel=1.0))
    m.add(tube((0, 0.8, 6.0), (0, 14.0, 8.6), 0.55, 0.5, mat=ACIER, seg=8))
    m.add(tube((0, 13.0, 8.4), (0, 14.4, 8.7), 0.9, mat=ACIER_SOMBRE, seg=8))
    m.add(tube((0, 0.8, 5.8), (0, 3.6, 6.4), 1.3, mat=peau, seg=10))               # masque
    # bêche arrière et caisses d'obus
    m.add(box(-3.4, -9.8, 0.6, 3.4, -8.8, 2.8, ACIER_SOMBRE))
    for s in (1, -1):
        m.add(box(s * 4.3 - 0.6, -7.8, 3.8, s * 4.3 + 0.6, -3.4, 5.0, Mat((80, 86, 56), spec=0.1), bevel=0.1))
    m.add(trappe(2.0, -5.0, 7.1, 0.9))
    m.add(antenne(-2.8, -7.0, 7.2, 6.0))
    m.add(aigle(0, -3.2, 7.22, 0.8))
    return m


# ---------------------------------------------------------------------------
# Carro Aguila : char de bataille principal, tourelle anguleuse, canon de 120 mm
# ---------------------------------------------------------------------------
def carro_caisse():
    peau = blinde(4.5, 52)
    m = Mesh()
    prof = [(-5.0, -10.4), (5.0, -10.4), (5.0, 6.6), (3.6, 10.4), (-3.6, 10.4), (-5.0, 6.6)]
    m.add(prism(prof, 1.6, 4.4, peau, bevel=1.2))
    tr = chenilles(20.6, 2.2, 4.2, 6.0, 0.0, wheels=7, skirt=(2.0, 4.1), mat_skirt=peau)
    m.add(tr).add(tr.mirror_x())
    m.add(box(-4.2, -10.2, 4.4, 4.2, -6.0, 4.8, GRILLE))
    for s in (1, -1):
        m.add(tube((s * 3.4, 10.3, 4.0), (s * 3.4, 10.8, 4.0), 0.45, mat=PHARE, seg=6))
        m.add(box(s * 4.4 - 0.3, -10.55, 3.8, s * 4.4 + 0.3, -10.4, 4.2, FEU_ROUGE))
    m.add(box(-0.9, 7.4, 4.4, 0.9, 8.6, 4.8, ACIER_SOMBRE))                          # épiscope pilote
    return m


def carro_tourelle():
    peau = blinde(3.0, 53)
    m = Mesh()
    # tourelle en coin (blindage espacé), nuque de rangement
    m.add(prism([(-4.2, -4.6), (4.2, -4.6), (4.6, 1.2), (2.6, 4.8), (-2.6, 4.8), (-4.6, 1.2)], 0.0, 3.0, peau, bevel=0.9))
    m.add(prism(rrect(-3.8, -8.0, 3.8, -4.4, 0.6), 0.4, 2.6, peau, bevel=0.4))
    m.add(box(-3.6, -7.8, 2.6, 3.6, -4.6, 2.9, Mat((70, 76, 50), spec=0.1, tex=stripes(0.8, 0.8, axis=0))))   # panier
    m.add(canon(4.0, 16.5, 1.6, 0.55, manchon=(0.2, 0.7)))
    # viseur du chef panoramique et du tireur, mitrailleuse, fumigènes
    m.add(tube((-2.0, -1.0, 3.0), (-2.0, -1.0, 4.4), 0.8, mat=ACIER_SOMBRE, seg=8))
    m.add(box(-2.6, -0.4, 3.9, -1.4, 0.4, 4.6, VITRE))
    m.add(box(1.6, 2.4, 3.0, 3.0, 3.6, 3.8, ACIER_SOMBRE))
    m.add(box(1.8, 3.6, 3.2, 2.8, 3.7, 3.6, VITRE))
    m.add(trappe(2.0, -1.8, 2.95, 0.9))
    m.add(tube((2.0, -1.0, 4.0), (2.0, 1.8, 4.0), 0.15, mat=ACIER_SOMBRE, seg=4))
    for s in (1, -1):
        for k in range(4):
            m.add(tube((s * 4.0, 0.2 + k * 0.7, 2.4), (s * 4.8, 0.5 + k * 0.7, 2.9), 0.25, mat=ACIER_SOMBRE, seg=5))
    m.add(antenne(-3.2, -6.0, 2.9, 7.0))
    m.add(antenne(3.2, -6.0, 2.9, 5.0))
    m.add(aigle(0, -2.4, 3.02, 1.0))
    return m


# ---------------------------------------------------------------------------
# Lanzador : obusier sur camion 6×6 (famille César), canon replié vers l'arrière
# ---------------------------------------------------------------------------
def lanzador(pret=True):
    peau = blinde(3.5, 54)
    m = Mesh()
    # cabine blindée avant
    m.add(prism(rrect(-3.2, 5.0, 3.2, 10.2, 0.8), 1.6, 5.4, peau, bevel=0.6))
    m.add(box(-2.9, 9.6, 3.8, 2.9, 10.25, 5.0, VITRE))
    m.add(box(-3.2, -10.0, 1.4, 3.2, 5.0, 2.6, ACIER_SOMBRE))                         # châssis
    m.add(box(-3.4, -10.0, 2.6, 3.4, 1.0, 3.0, peau))                                 # plateau
    for s in (1, -1):
        for y in (-7.4, -4.4, 6.8):
            m.add(roue(s * 3.3, y, 1.5, 1.5, 1.1, s))
        m.add(box(s * 3.6 - 0.6, -9.0, 3.0, s * 3.6 + 0.6, -2.0, 3.8, Mat((82, 86, 60), spec=0.1)))   # coffres
        m.add(tube((s * 2.4, 10.1, 3.2), (s * 2.4, 10.5, 3.2), 0.4, mat=PHARE, seg=6))
    # affût arrière, canon long de 155 mm (en batterie ou en route)
    m.add(box(-1.6, -9.0, 3.0, 1.6, -6.0, 5.0, peau, bevel=0.3))
    if pret:
        m.add(tube((0, -7.0, 4.6), (0, 6.0, 8.4), 0.5, mat=ACIER, seg=8))
        m.add(tube((0, 5.2, 8.2), (0, 6.6, 8.6), 0.85, mat=ACIER_SOMBRE, seg=8))
        m.add(box(-3.0, -12.0, 0.2, 3.0, -10.4, 1.6, ACIER_SOMBRE))                   # bêche au sol
    else:
        m.add(tube((0, -7.0, 4.6), (0, 7.0, 5.4), 0.5, mat=ACIER, seg=8))
        m.add(tube((0, 6.2, 5.3), (0, 7.6, 5.5), 0.85, mat=ACIER_SOMBRE, seg=8))
    m.add(antenne(-2.6, 6.0, 5.4, 5.0))
    m.add(aigle(0, 7.6, 5.42, 0.8))
    return m


def lanzador_route():
    return lanzador(False)


# ---------------------------------------------------------------------------
# Aviation
# ---------------------------------------------------------------------------
def condor():
    """Hélicoptère lourd à rotor principal unique (famille CH-53) : crochet de charge sous le ventre,
    réservoirs latéraux, poutre de queue à rotor anticouple. Le rotor principal est une surcouche."""
    peau = R.team(spec=0.3, tex=combine(panels(3.2, 0.3, 0.84), noise(61, 0.05)))
    gris = Mat((96, 102, 90), spec=0.3, tex=panels(3.2, 0.3, 0.86))
    m = Mesh()
    m.add(fuselage([(-9.0, 1.4, 1.6, 2.4), (-6.0, 2.8, 2.8, 1.8), (4.0, 3.0, 2.9, 1.7), (9.0, 2.8, 2.6, 1.5),
                    (11.5, 2.0, 1.9, 1.1), (12.8, 0.9, 0.9, 0.8)], peau, seg=16))
    # poutre de queue relevée, dérive et rotor anticouple
    m.add(fuselage([(-20.0, 0.5, 0.6, 4.2), (-14.0, 0.8, 0.9, 3.6), (-8.5, 1.4, 1.5, 3.0)], peau, seg=10))
    m.add(R.loft([[(dx, y, z) for y, z in ((-20.6, 3.6), (-18.4, 3.6), (-19.2, 8.0), (-20.8, 8.2))] for dx in (-0.3, 0.3)], peau))
    for k in range(4):
        m.add(box(0.4, -19.8, 5.9, 0.55, -19.6, 6.1, Mat((50, 50, 52))).rot_y(0).map(lambda p: p).move())
    m.add(tube((0.35, -19.7, 6.0), (0.55, -19.7, 6.0), 1.8, mat=Mat((80, 80, 80), spec=0.05, tex=stripes(0.6, 0.6, axis=2)), seg=12))
    # mât du rotor et capots moteurs
    m.add(prism(rrect(-1.6, -4.0, 1.6, 4.0, 0.8), 4.4, 5.6, peau, bevel=0.5))
    m.add(tube((0, 0, 5.6), (0, 0, 7.0), 0.6, mat=gris, seg=8))
    for s in (1, -1):
        m.add(fuselage([(-4.0, 0.6, 0.6, 4.6), (-3.0, 1.1, 1.0, 4.6), (3.0, 1.1, 1.0, 4.6), (3.8, 0.8, 0.8, 4.6)], gris, seg=10).move(dx=s * 2.3))
        # réservoirs sur ailerons et train
        m.add(fuselage([(-3.0, 0.2, 0.2, 0.3), (-2.0, 1.0, 0.9, 0.3), (3.0, 1.0, 0.9, 0.3), (4.2, 0.2, 0.2, 0.3)], gris, seg=10).move(dx=s * 4.2))
        m.add(box(min(s * 2.8, s * 4.2), -1.0, 0.8, max(s * 2.8, s * 4.2), 1.0, 1.2, peau))
        for k in range(5):
            m.add(box(s * 3.0 - 0.05, -5 + k * 2.2, 2.0, s * 3.0 + 0.05, -4.4 + k * 2.2, 2.7, Mat((40, 60, 80), spec=1.0)))
    for s in (1, -1):
        m.add(box(s * 1.2 - 0.9, 10.2, 1.6, s * 1.2 + 0.9, 12.2, 2.8, Mat((50, 84, 110), spec=1.1, shine=40)))
    m.add(tube((0, 12.0, 1.4), (0, 16.0, 1.2), 0.15, mat=ACIER_SOMBRE, seg=4))                 # perche de ravitaillement
    m.add(tube((0, 0, -1.4), (0, 0, -0.2), 0.25, mat=ACIER_SOMBRE, seg=5))                      # crochet
    m.add(aigle(0, 6.0, 4.7, 1.0))
    return m


def condor_icone():
    m = condor()
    pale = Mat((50, 52, 54), spec=0.2)
    for k in range(7):
        m.add(box(-0.35, 0.0, -0.08, 0.35, 16.0, 0.08, pale).rot_z(k * 360 / 7 + 10).move(dz=7.1))
    return m


def rotor_pales(n, r, larg=0.45):
    m = Mesh()
    for k in range(n):
        m.add(box(-larg / 2, 0.0, -0.06, larg / 2, r, 0.06, Mat((58, 60, 62), spec=0.2)).rot_z(k * 360 / n + 20))
    return m


def pico():
    """Drone d'attaque léger : hexacoptère à nacelle mitrailleuse et caméra."""
    peau = R.team(spec=0.35, tex=panels(1.8, 0.25, 0.85))
    m = Mesh()
    m.add(fuselage([(-2.0, 0.5, 0.4, 0.0), (-1.4, 1.3, 0.8, 0.0), (1.2, 1.3, 0.8, 0.0), (2.2, 0.6, 0.5, -0.1)], peau, seg=10))
    for k in range(6):
        a = math.radians(k * 60 + 30)
        x, y = 3.4 * math.cos(a), 3.4 * math.sin(a)
        m.add(tube((0, 0, 0.3), (x, y, 0.5), 0.22, mat=ACIER_SOMBRE, seg=4))
        m.add(tube((x, y, 0.3), (x, y, 0.9), 0.35, mat=ACIER_SOMBRE, seg=6))
        m.add(rotor_pales(2, 1.2, 0.3).rot_z(k * 37).move(dx=x, dy=y, dz=1.0))
    m.add(tube((0, 0.6, -1.0), (0, 3.6, -1.0), 0.18, mat=ACIER_SOMBRE, seg=4))               # mitrailleuse
    m.add(box(-0.5, -0.6, -1.4, 0.5, 1.0, -0.7, ACIER_SOMBRE))
    m.add(sphere((0, 2.1, -0.3), 0.4, Mat((30, 34, 40), spec=1.4, shine=60), seg=8, rings=4))
    return m.scale(1.7)


def garra():
    """Drone antichar : hélicoptère sans pilote à rotors coaxiaux, deux missiles sur ailerons."""
    peau = R.team(spec=0.35, tex=panels(2.2, 0.25, 0.85))
    m = Mesh()
    m.add(fuselage([(-7.0, 0.3, 0.3, 0.6), (-5.0, 0.5, 0.5, 0.5), (-2.0, 1.4, 1.3, 0.2), (1.5, 1.6, 1.5, 0.0),
                    (3.4, 1.1, 1.1, -0.1), (4.4, 0.3, 0.4, -0.2)], peau, seg=12))
    m.add(box(-0.12, -7.3, 0.6, 0.12, -6.0, 2.2, peau))
    m.add(tube((0, 0, 1.4), (0, 0, 3.0), 0.3, mat=ACIER_SOMBRE, seg=6))
    m.add(rotor_pales(2, 5.6, 0.45).move(dz=2.4))
    m.add(rotor_pales(2, 5.6, 0.45).rot_z(70).move(dz=3.0))
    m.add(sphere((0, 4.0, -0.4), 0.55, Mat((30, 34, 40), spec=1.4, shine=60), seg=8, rings=4))
    for s in (1, -1):
        m.add(box(min(s * 1.2, s * 2.8), -0.8, 0.0, max(s * 1.2, s * 2.8), 0.6, 0.25, peau))
        m.add(missile(s * 2.8, -1.8, 2.6, -0.4, 0.38, Mat((90, 96, 70), spec=0.3)))
    return m.scale(1.35)


def ojo_orbite(frames=32, rayon=60.0, taille=176):
    """Drone de reconnaissance (famille Predator) en orbite : une image par position sur le cercle,
    le drone tangent à sa trajectoire. Sert de surcouche animée à la zone de l'Ojo del Aguila."""
    peau = Mat((150, 154, 150), spec=0.4, tex=panels(3.0, 0.3, 0.88))
    d = Mesh()
    d.add(fuselage([(-6.0, 0.4, 0.4, 0.0), (-4.0, 0.8, 0.8, 0.0), (3.0, 0.9, 1.0, 0.1), (5.0, 0.7, 0.9, 0.2), (6.0, 0.2, 0.3, 0.1)], peau, seg=10))
    for s in (1, -1):
        w = wing((0.6, 1.0), (0.6, -0.4), (10.0, 0.4), (10.0, -0.3), 0.3, peau, thick=0.3)
        d.add(w if s > 0 else w.mirror_x())
        d.add(box(0, -6.2, 0.0, s * 2.6, -5.4, 0.15, peau).rot_y(s * -40, cx=0, cz=0))
    d.add(sphere((0, 4.0, -0.9), 0.7, Mat((30, 34, 40), spec=1.4, shine=60), seg=8, rings=4))
    d.add(rotor_pales(2, 1.1, 0.25).rot_x(90).move(dy=-6.2))
    imgs = []
    for f in range(frames):
        a = 2 * math.pi * f / frames
        x, y = rayon * math.cos(a), rayon * math.sin(a)
        cap = math.degrees(a) + 180.0            # tangent, sens antihoraire
        imgs.append(R.render(d.scale(1.3).rot_z(cap - 90.0).move(dx=x, dy=y), taille, shadow=False, lift=40.0,
                             center=(taille / 2, taille / 2 + 12)))
    return imgs


# ---------------------------------------------------------------------------
# Porta Aguila : porte-aéronefs amphibie (famille Juan Carlos I) — tremplin, radier
# arrière, îlot à deux cheminées et radars plans, défenses rapprochées aux quatre coins.
# Pas d'avions dessinés sur le pont : les vrais appareils s'y posent (AircraftCarrier).
# ---------------------------------------------------------------------------
GRIS_AGUILA = Mat((122, 126, 124), spec=0.35, shine=20, tex=combine(panels(4.5, 0.3, 0.88), noise(211, 0.05)))
JAUNE_PONT = (228, 196, 64)
BLANC_PONT = (232, 232, 224)
BLANC = Mat((226, 228, 222), spec=0.45, shine=24)
NOIR = Mat((34, 34, 36), spec=0.1)
VITRE_PONT = Mat((38, 62, 88), spec=1.2, shine=45)

SPOTS = [(-2.0, y) for y in (-33.0, -21.0, -9.0, 3.0, 15.0)] + [(-2.0, 27.0)]


def _pont_aguila(p, n):
    """Pont d'envol antidérapant et ses marquages (vus d'en haut uniquement)."""
    if n[2] < 0.7:
        return 1.0
    x, y = p[0], p[1]
    # ligne d'axe jaune décalée à bâbord (piste des avions) et tirets blancs de rive
    if abs(x + 2.0) < 0.22 and y < 38.0 and (y / 3.0) % 1.0 < 0.65:
        return JAUNE_PONT
    if abs(abs(x + 1.0) - 9.4) < 0.18 and y < 40.0 and (y / 2.0) % 1.0 < 0.5:
        return BLANC_PONT
    # cercles de poser numérotés (anneau + trait)
    for sx, sy in SPOTS:
        d = math.hypot(x - sx, y - sy)
        if abs(d - 2.6) < 0.2 or (abs(x - sx) < 0.18 and d < 2.6):
            return BLANC_PONT
    # ligne de sécurité jaune le long de l'îlot
    if abs(x - 6.2) < 0.18 and -26.0 < y < 14.0:
        return JAUNE_PONT
    # traces de gomme et plaques de pont
    k = 0.9 if abs(x + 2.0) < 2.2 and y < 32.0 else 1.0
    if (y / 4.0) % 1.0 < 0.04 or ((x + 12.0) / 4.0) % 1.0 < 0.04:
        k *= 0.9
    return k


def _cius(m, x, y, z, s):
    """Défense rapprochée (canon rotatif sous dôme blanc) sur son pied."""
    m.add(tube((x, y, z), (x, y, z + 1.0), 0.9, mat=GRIS_AGUILA, seg=8))
    m.add(sphere((x, y, z + 1.8), 0.9, BLANC, seg=8, rings=5, sz=1.3))
    m.add(tube((x, y + 0.5 * s, z + 1.3), (x, y + 2.2 * s, z + 1.3), 0.22, mat=ACIER_SOMBRE, seg=5))


def _ram(m, x, y, z, rot):
    """Lanceur de missiles RAM : caisson à 21 cellules sur affût."""
    l = Mesh()
    l.add(tube((0, 0, 0), (0, 0, 0.8), 0.7, mat=GRIS_AGUILA, seg=8))
    l.add(box(-0.8, -1.2, 0.8, 0.8, 1.2, 2.0, GRIS_AGUILA, bevel=0.15,
              top=Mat((60, 62, 60), spec=0.1, tex=lambda p, n: 0.6 if (p[0] * 2.5) % 1.0 < 0.4 else 1.0)))
    m.add(l.rot_x(12).rot_z(rot).move(x, y, z))


def porta_aguila():
    m, poly = coque_aguila()
    peau = R.team(spec=0.35, tex=combine(panels(3.0, 0.28, 0.86), noise(212, 0.04)))
    deck = Mat((70, 74, 74), spec=0.12, tex=_pont_aguila)

    # pont d'envol débordant, étrave effilée, tremplin à 12° à la proue
    pts = [(-11.2, -46.0), (9.6, -46.0), (10.4, 20.0), (8.0, 36.0), (4.6, 46.0), (-5.2, 46.0), (-10.4, 34.0), (-11.6, 10.0)]
    m.add(prism(pts, 4.8, 5.4, deck))
    rampe = [(-5.0, 36.0), (4.4, 36.0), (4.6, 47.0), (-5.2, 47.0)]
    m.add(prism(rampe, 5.4, 5.5, deck).map(
        lambda p: (p[0], p[1], p[2] + (max(0.0, p[1] - 36.0) ** 1.35 * 0.13 if p[2] > 5.45 else 0.0))))
    # flancs du tremplin
    for s in (-5.1, 4.5):
        m.add(prism([(s - 0.1, 36.0), (s + 0.1, 36.0), (s + 0.1, 47.0), (s - 0.1, 47.0)], 4.8, 5.5, GRIS_AGUILA).map(
            lambda p: (p[0], p[1], p[2] + (max(0.0, p[1] - 36.0) ** 1.35 * 0.13 if p[2] > 5.45 else 0.0))))
    # coursives sous le pont (filets de sécurité sombres) des deux bords
    for s in (1, -1):
        x0 = 9.6 if s > 0 else -11.2
        m.add(box(min(x0, x0 + s * 1.2), -42.0, 4.5, max(x0, x0 + s * 1.2), 18.0, 4.8,
                  Mat((52, 54, 54), spec=0.1, tex=stripes(0.8, 0.6, axis=1))))

    # ascenseurs : latéral arrière tribord (hors bord) et central avant, liserés jaunes
    asc = Mat((80, 84, 84), spec=0.1, tex=lambda p, n: JAUNE_PONT if n[2] > 0.7 and (
        min(abs(p[0] - 9.8), abs(p[0] - 14.6), abs(p[1] + 40.0), abs(p[1] + 31.0)) < 0.2) else 0.95)
    m.add(box(9.8, -40.0, 4.9, 14.6, -31.0, 5.35, asc))
    m.add(box(-6.0, 20.0, 5.41, 1.4, 27.0, 5.43, Mat((84, 88, 88), spec=0.1, tex=lambda p, n: JAUNE_PONT if (
        min(abs(p[0] + 6.0), abs(p[0] - 1.4), abs(p[1] - 20.0), abs(p[1] - 27.0)) < 0.2) else 0.92)))

    # îlot tribord aux couleurs du joueur : deux blocs, passerelle vitrée tout autour
    m.add(prism(rrect(6.6, -24.0, 10.2, 10.0, 0.8), 5.4, 10.6, peau, bevel=0.35))
    m.add(prism(rrect(7.0, -6.0, 10.0, 8.4, 0.7), 10.6, 13.6, peau, bevel=0.3))
    m.add(prism(rrect(6.9, 2.0, 10.1, 8.6, 0.6), 12.4, 13.4, VITRE_PONT))
    m.add(prism(rrect(7.2, -4.0, 9.8, 6.0, 0.6), 13.6, 15.4, peau, bevel=0.3))
    m.add(prism(rrect(7.0, 1.2, 10.0, 6.2, 0.5), 14.4, 15.2, VITRE_PONT))
    # radars plans sur les quatre faces du mât principal, mât treillis et radômes
    m.add(prism(rrect(7.6, -2.0, 9.4, 3.0, 0.3), 15.4, 19.2, GRIS_AGUILA, bevel=0.2))
    for (ax, ay, bx, by) in ((7.5, -1.4, 7.5, 2.4), (9.5, -1.4, 9.5, 2.4)):
        m.add(box(ax - 0.12, ay, 16.0, ax + 0.12, by, 18.6, Mat((64, 70, 74), spec=0.6, shine=30)))
    m.add(box(7.9, 3.0, 16.0, 9.1, 3.2, 18.6, Mat((64, 70, 74), spec=0.6, shine=30)))
    for dx, dy in ((-0.6, -0.6), (0.6, -0.6), (0.0, 0.6)):
        m.add(tube((8.5 + dx, 0.5 + dy, 19.2), (8.5, 0.5, 24.0), 0.14, mat=ACIER_SOMBRE, seg=4))
    for z in (20.4, 22.2):
        m.add(box(7.6, 0.2, z, 9.4, 0.8, z + 0.2, ACIER_SOMBRE))
    m.add(sphere((8.5, 0.5, 24.6), 0.7, BLANC, seg=8, rings=5))
    m.add(tube((8.5, 0.5, 25.2), (8.5, 0.5, 27.0), 0.07, mat=ACIER_SOMBRE, seg=4))
    m.add(box(7.4, 0.3, 21.2, 9.6, 0.7, 21.5, Mat((180, 184, 180), spec=0.4)))
    for sy in (-3.4, 5.2):
        m.add(sphere((9.6, sy, 16.2), 0.8, BLANC, seg=8, rings=5))
    # deux cheminées inclinées à capuchon noir
    for cy in (-18.0, -11.0):
        m.add(prism(rrect(7.4, cy - 2.2, 9.6, cy + 2.2, 0.8), 10.6, 13.4, peau, bevel=0.3))
        m.add(prism(rrect(7.6, cy - 1.8, 9.4, cy + 1.8, 0.6), 13.4, 13.9, NOIR))
        m.add(tube((8.1, cy - 0.6, 13.6), (8.1, cy - 0.6, 14.4), 0.35, mat=NOIR, seg=6))
        m.add(tube((8.9, cy + 0.6, 13.6), (8.9, cy + 0.6, 14.4), 0.35, mat=NOIR, seg=6))
    # antennes fouets, projecteurs, grue de pont
    for ay in (-22.0, -8.0, 8.4):
        m.add(tube((10.1, ay, 10.6), (10.8, ay, 16.0), 0.06, mat=ACIER_SOMBRE, seg=4))
    m.add(box(6.4, 9.8, 5.4, 7.8, 12.2, 6.6, Mat((210, 176, 40), spec=0.3)))
    m.add(tube((7.1, 11.0, 6.6), (5.0, 14.0, 8.6), 0.25, mat=Mat((210, 176, 40), spec=0.3), seg=5))

    # véhicules de pont : tracteur jaune, camion-incendie rouge, chariot
    m.add(box(4.2, -30.0, 5.4, 5.6, -28.0, 6.2, Mat((214, 180, 44), spec=0.3)))
    m.add(box(4.0, -26.0, 5.4, 5.8, -22.8, 6.6, Mat((196, 40, 32), spec=0.4)))
    m.add(box(4.2, -26.0, 6.6, 5.6, -25.0, 6.9, VITRE_PONT))
    m.add(box(4.4, -20.4, 5.4, 5.4, -19.0, 5.8, Mat((90, 96, 62), spec=0.2)))

    # défenses : 4 CIWS en encorbellement aux coins, 2 RAM, mitrailleuses latérales
    for (x, y, s) in ((10.0, 26.0, 1), (-11.2, 24.0, 1), (10.8, -44.0, -1), (-12.0, -44.0, -1)):
        m.add(box(min(x, x + (1.8 if x > 0 else -1.8)) - 0.2, y - 1.4, 3.6,
                  max(x, x + (1.8 if x > 0 else -1.8)) + 0.2, y + 1.4, 4.6, GRIS_AGUILA))
        _cius(m, x + (0.9 if x > 0 else -0.9), y, 4.6, s)
    _ram(m, 8.6, -27.4, 10.6, 180)
    _ram(m, 8.6, 11.2, 10.6, 0)
    for y in (-8.0, 12.0):
        m.add(tube((-12.4, y, 4.2), (-14.0, y, 4.2), 0.12, mat=ACIER_SOMBRE, seg=4))

    # embarcations de sauvetage (conteneurs blancs) et canots semi-rigides sur les flancs
    for s in (1, -1):
        x = 10.8 if s > 0 else -12.4
        for y in (-22.0, -16.0, -10.0, 16.0, 22.0):
            m.add(tube((x, y - 1.1, 3.6), (x, y + 1.1, 3.6), 0.55, mat=BLANC, seg=8))
    for y in (-3.0, 4.0):
        m.add(box(10.2, y - 1.8, 3.2, 11.6, y + 1.8, 4.0, Mat((200, 92, 36), spec=0.3)))
        m.add(box(10.4, y - 1.4, 3.3, 11.4, y + 1.4, 4.02, Mat((58, 58, 56), spec=0.1)))

    # radier : porte arrière (rampe) et deux engins de débarquement visibles à la poupe
    m.add(box(-6.0, -46.25, 0.6, 6.0, -45.9, 4.0, Mat((66, 70, 70), spec=0.2, tex=stripes(0.9, 0.78, axis=2))))
    m.add(box(-6.4, -46.3, 4.0, 6.4, -45.8, 4.4, Mat(JAUNE_PONT, spec=0.2)))
    # ancres et écubiers
    for s in (1, -1):
        m.add(box(s * 2.4 - 0.5, 42.0, 2.8, s * 2.4 + 0.5, 42.6, 3.6, NOIR))
    # aigle doré de la marine elielistanaise sur le pont avant et marques de rive
    m.add(aigle(0.0, 32.0, 5.43, 2.4))
    m.add(box(-10.8, -45.8, 5.41, 9.2, -45.2, 5.44, Mat(BLANC_PONT, spec=0.1,
          tex=lambda p, n: (40, 40, 40) if (p[0] / 1.2) % 1.0 < 0.5 else 1.0)))
    # feux de pont
    for y in (-40.0, -20.0, 0.0, 20.0):
        m.add(box(-11.0, y, 5.4, -10.7, y + 0.3, 5.5, Mat((250, 244, 200), emit=True)))
    return m


def coque_aguila():
    from modeles.australouis import coque
    m, poly = coque(94.0, 20.0, 4.8, proue=0.24, poupe=0.04, side_mat=GRIS_AGUILA, tonture=0.0)
    # bulbe d'étrave et bouchains visibles au ras de l'eau
    m.add(sphere((0.0, 46.0, 0.2), 1.4, GRIS_AGUILA, seg=8, rings=5, sz=0.7))
    for s in (1, -1):
        m.add(box(s * 9.9 - 0.15, -40.0, 1.6, s * 9.9 + 0.15, 30.0, 1.8, Mat((90, 94, 92), spec=0.2)))
    return m, poly
