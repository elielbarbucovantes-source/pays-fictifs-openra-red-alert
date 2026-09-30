"""Fantassins articulés et leurs animations, au format des feuilles d'infanterie de Red Alert.

Un fantassin est un squelette simple (bassin, torse, tête, bras et jambes en deux segments)
habillé selon un « équipement » (tenue, casque, arme, sac…). Chaque image est une pose :
angles des articulations, inclinaison du buste, position de l'arme. Les animations (course,
tir, couché, reptation, morts…) sont des suites de poses rendues sur 8 orientations.

Disposition de la feuille (commune à tous les fantassins du mod) :
  stand 0 (8) · stand2 8 (8) · run 16 (6×8) · shoot 64 (8×8) · liedown 128 (2×8)
  prone-run 144 (4×8) · standup 176 (2×8) · prone-shoot 192 (8×8) · idle1 256 (16)
  idle2 272 (16) · die1 288 (8) · die2 296 (8) · die3 304 (8) · die4 312 (12) · die5 324 (18)
  parachute 342 (1)             → 343 images
"""
import math
import rendu3d as R
from rendu3d import Mesh, Mat, merge, box, tube, sphere, prism, ngon, noise, combine, panels

TAILLE = (36, 36)
ORIGINE = (18, 25)          # position des pieds dans l'image (le centre de l'image est le centre de l'unité)
PITCH_INF = 32.0            # caméra plus basse : les fantassins se voient presque de côté, comme dans RA
FACINGS = 8

PEAU = Mat((198, 148, 108), spec=0.15)
PEAU_SOMBRE = Mat((150, 104, 74), spec=0.15)
BOTTES = Mat((40, 36, 32), spec=0.2)
ARME = Mat((52, 52, 54), spec=0.5, shine=24)
BOIS = Mat((112, 76, 44), spec=0.2)
FLAMME = Mat((255, 196, 80), emit=True)
FLAMME2 = Mat((250, 120, 40), emit=True)
SANG = Mat((120, 14, 12), spec=0.3)
CALCINE = Mat((40, 34, 30), spec=0.05)


class Kit:
    """Équipement d'un fantassin (couleurs, coiffure, arme)."""

    def __init__(self, tenue=None, gilet=None, casque="casque", casque_mat=None, arme="fusil", sac=None,
                 peau=PEAU, lueur=None, brassard=None, drapeau=None, cape=None):
        self.tenue = tenue or Mat((236, 214, 124), team=True, spec=0.2, tex=noise(111, 0.07, 1.0))
        self.gilet = gilet or Mat((74, 76, 58), spec=0.1)
        self.casque = casque            # "casque", "bob", "chapka", "beret", "visiere", None
        self.casque_mat = casque_mat or Mat((80, 86, 62), spec=0.3)
        self.arme = arme                # "fusil", "carabine", "lance", "grenade", "pistolet", "canon_energie"
        self.sac = sac                  # None, "sac", "radio", "tube"
        self.peau = peau
        self.lueur = lueur              # couleur de l'arme à énergie
        self.brassard = brassard
        self.drapeau = drapeau
        self.cape = cape


# ---------------------------------------------------------------------------
# Squelette
# ---------------------------------------------------------------------------
CUISSE, TIBIA = 3.4, 3.3
BRAS, AVANT_BRAS = 2.6, 2.4
BASSIN_Z = CUISSE + TIBIA + 0.5
TORSE = 4.4
EPAULE_X, HANCHE_X = 1.95, 0.95


def _membre(origine, a, b, l1, l2, side=0.0):
    """Segment à deux os dans le plan sagittal. a : angle du premier os (0 = vers le bas,
    + = vers l'avant), b : pliure du second (+ = même sens), side : écart latéral (rad)."""
    d1 = (math.sin(side), math.sin(a) * math.cos(side), -math.cos(a) * math.cos(side))
    j = (origine[0] + d1[0] * l1, origine[1] + d1[1] * l1, origine[2] + d1[2] * l1)
    c = a + b
    d2 = (math.sin(side) * 0.6, math.sin(c), -math.cos(c))
    e = (j[0] + d2[0] * l2, j[1] + d2[1] * l2, j[2] + d2[2] * l2)
    return j, e


def arme_mesh(kit, flash=False):
    """Arme dans le repère des mains : canon vers +y, poignée à l'origine."""
    m = Mesh()
    k = kit.arme
    if k in ("fusil", "carabine"):
        L = 5.2 if k == "fusil" else 4.2
        m.add(box(-0.22, -2.0, -0.35, 0.22, L - 2.0, 0.25, ARME))
        m.add(box(-0.2, -3.0, -0.5, 0.2, -1.6, 0.2, BOIS))                  # crosse
        m.add(box(-0.18, 0.2, -1.1, 0.18, 0.8, -0.3, ARME))                  # chargeur
        m.add(tube((0, L - 2.0, 0.0), (0, L - 1.2, 0.0), 0.12, mat=ARME, seg=4))
        tip = L - 1.2
    elif k == "lance":
        m.add(tube((0, -3.6, 0.9), (0, 3.4, 0.9), 0.55, mat=Mat((84, 92, 64), spec=0.3), seg=8))
        m.add(tube((0, 3.4, 0.9), (0, 4.6, 0.9), 0.75, 0.3, mat=Mat((70, 76, 56), spec=0.3), seg=8))   # roquette
        m.add(box(-0.2, 0.0, -0.4, 0.2, 0.6, 0.6, ARME))
        tip = 4.6
    elif k == "grenade":
        m.add(sphere((0, 0.3, 0.2), 0.45, Mat((70, 84, 52), spec=0.3), seg=6, rings=4))
        tip = 0.5
    elif k == "designateur":
        m.add(box(-0.45, -0.6, -0.45, 0.45, 1.4, 0.45, Mat((64, 70, 56), spec=0.3)))
        m.add(tube((0, 1.4, 0.1), (0, 1.6, 0.1), 0.3, mat=Mat((230, 40, 30), emit=True), seg=6))
        m.add(tube((0, -0.4, -0.45), (0, -0.4, -1.8), 0.12, mat=ARME, seg=3))          # trépied replié
        tip = 1.6
    elif k == "pistolet":
        m.add(box(-0.15, -0.2, -0.3, 0.15, 1.2, 0.2, ARME))
        tip = 1.2
    elif k == "canon_energie":
        m.add(box(-0.35, -2.4, -0.45, 0.35, 3.2, 0.45, Mat((150, 156, 170), spec=0.7, shine=30)))
        for i in range(3):
            m.add(tube((0, 0.4 + i * 1.0, 0.0), (0, 0.8 + i * 1.0, 0.0), 0.55, mat=Mat(kit.lueur or (120, 200, 255), emit=True), seg=6))
        tip = 3.4
    else:
        tip = 0.0
    if flash and k == "designateur":
        m.add(tube((0, tip, 0.1), (0, tip + 6.0, 0.1), 0.1, mat=Mat((255, 40, 30), emit=True), seg=3))
    elif flash and k not in ("grenade", None):
        c = Mat(kit.lueur or (255, 230, 120), emit=True)
        m.add(sphere((0, tip + 0.6, 0.0), 0.7, c, seg=6, rings=3))
        m.add(tube((0, tip, 0.0), (0, tip + 1.6, 0.0), 0.35, 0.05, mat=Mat((255, 250, 200), emit=True), seg=5))
    return m


def soldat(kit, p=None):
    """Construit le fantassin dans une pose. p : dict d'angles (degrés) et d'options."""
    p = dict(p or {})
    g = lambda k, d=0.0: p.get(k, d)  # noqa: E731
    rad = math.radians
    tenue, peau = kit.tenue, kit.peau
    m = Mesh()
    crouch = g("crouch")
    bz = BASSIN_Z - crouch
    # jambes
    for s, kh, kk in ((1, "hanche_d", "genou_d"), (-1, "hanche_g", "genou_g")):
        hip = (s * HANCHE_X, 0.0, bz)
        knee, foot = _membre(hip, rad(g(kh)), -rad(g(kk)), CUISSE, TIBIA, rad(g("ecart", 3.0)) * s)
        m.add(tube(hip, knee, 0.95, 0.78, mat=tenue, seg=6))
        m.add(tube(knee, foot, 0.76, 0.62, mat=tenue, seg=6))
        m.add(box(foot[0] - 0.55, foot[1] - 0.6, foot[2] - 0.2, foot[0] + 0.55, foot[1] + 1.2, foot[2] + 0.7, BOTTES))
    # bassin
    m.add(box(-1.5, -0.9, bz - 0.5, 1.5, 0.9, bz + 0.8, tenue))
    # haut du corps (repère au bassin, incliné ensuite)
    h = Mesh()
    top = bz + TORSE
    h.add(prism(R.rrect(-1.9, -1.05, 1.9, 1.05, 0.45), bz + 0.6, top, tenue, bevel=0.35))
    h.add(prism(R.rrect(-1.7, -1.2, 1.7, 1.15, 0.4), bz + 1.4, top - 0.7, kit.gilet, bevel=0.25))   # gilet / sangles
    h.add(box(-1.6, 1.1, bz + 1.6, -0.4, 1.5, bz + 2.5, kit.gilet))                               # poches
    h.add(box(0.4, 1.1, bz + 1.6, 1.6, 1.5, bz + 2.5, kit.gilet))
    if kit.brassard:
        h.add(tube((-1.55, 0, top - 0.9), (-1.95, 0, top - 1.8), 0.62, mat=Mat(kit.brassard, spec=0.1), seg=6))
    # sac / radio / tube de roquettes
    if kit.sac == "sac":
        h.add(box(-1.1, -1.9, bz + 1.4, 1.1, -0.8, top - 0.4, Mat((88, 84, 60), spec=0.05), bevel=0.25))
    elif kit.sac == "radio":
        h.add(box(-1.0, -1.8, bz + 1.4, 1.0, -0.8, top - 0.2, Mat((70, 74, 60), spec=0.2), bevel=0.2))
        h.add(tube((0.6, -1.4, top - 0.2), (0.8, -1.8, top + 4.0), 0.07, mat=ARME, seg=3))
    elif kit.sac == "tube":
        h.add(tube((-0.6, -1.2, bz + 1.0), (0.6, -1.2, top + 1.6), 0.5, mat=Mat((84, 92, 64), spec=0.3), seg=6))
    if kit.drapeau:
        h.add(tube((0.9, -1.2, bz + 1.0), (0.9, -1.2, top + 4.5), 0.08, mat=BOIS, seg=3))
        h.add(box(0.95, -1.25, top + 2.4, 3.2, -1.15, top + 4.4, Mat(kit.drapeau, spec=0.05, flat=True)))
    if kit.cape:
        h.add(prism([(-1.5, -1.0), (1.5, -1.0), (1.9, -1.6), (-1.9, -1.6)], bz - 1.4, top - 0.2, Mat(kit.cape, spec=0.1, flat=True)))
    # tête et coiffure
    hy, hz = 0.1, top + 1.3
    head = Mesh()
    head.add(tube((0, 0, top - 0.1), (0, 0, top + 0.4), 0.45, mat=peau, seg=6))
    head.add(sphere((0, hy, hz), 1.2, peau, seg=8, rings=5, sz=1.1))
    c = kit.casque
    cm = kit.casque_mat
    if c == "casque":
        head.add(sphere((0, hy - 0.05, hz + 0.25), 1.28, cm, seg=10, rings=4, sz=0.85).map(lambda q: (q[0], q[1], max(q[2], hz - 0.1))))
    elif c == "bob":
        head.add(tube((0, hy, hz + 0.5), (0, hy, hz + 0.65), 1.9, mat=cm, seg=10))
        head.add(sphere((0, hy, hz + 0.6), 1.1, cm, seg=8, rings=4, sz=0.7))
    elif c == "chapka":
        head.add(sphere((0, hy, hz + 0.35), 1.35, cm, seg=10, rings=5, sz=0.9).map(lambda q: (q[0], q[1], max(q[2], hz - 0.6))))
        for s in (1, -1):
            head.add(box(s * 1.2 - 0.25, hy - 0.6, hz - 0.8, s * 1.2 + 0.25, hy + 0.6, hz + 0.3, cm))
    elif c == "beret":
        head.add(sphere((0.3, hy - 0.2, hz + 0.75), 1.2, cm, seg=10, rings=4, sz=0.45))
        head.add(box(-0.2, hy + 0.9, hz + 0.5, 0.2, hy + 1.2, hz + 0.9, Mat((230, 190, 40), emit=True)))
    elif c == "visiere":
        head.add(sphere((0, hy, hz + 0.1), 1.35, cm, seg=10, rings=6, sz=1.05))
        head.add(box(-0.9, hy + 0.9, hz - 0.2, 0.9, hy + 1.3, hz + 0.45, Mat(kit.lueur or (120, 200, 255), emit=True)))
    head = head.rot_z(g("tete"), 0, 0).rot_x(g("tete_haut"), cy=0, cz=top)
    h.add(head)
    # bras et arme
    arme_pose = g("arme", "port")
    for s, ks, ke in ((1, "epaule_d", "coude_d"), (-1, "epaule_g", "coude_g")):
        sh = (s * EPAULE_X, 0.0, top - 0.5)
        elb, hand = _membre(sh, rad(g(ks)), rad(g(ke)), BRAS, AVANT_BRAS, rad(g("bras_ecart", 12.0)) * s + rad(g(ks + "_x")) * s)
        h.add(tube(sh, elb, 0.72, 0.62, mat=tenue, seg=6))
        h.add(tube(elb, hand, 0.6, 0.52, mat=tenue, seg=6))
        h.add(sphere(hand, 0.52, peau, seg=6, rings=3))
        if s == 1:
            main_d = hand
    if arme_pose != "aucune" and kit.arme:
        w = arme_mesh(kit, g("flash", False))
        if arme_pose == "vise":
            w = w.move(0.35, 1.0, top - 0.7) if kit.arme != "lance" else w.move(0.9, -0.2, top - 0.1)
        elif arme_pose == "port":
            w = w.rot_x(35).rot_z(-35).move(0.2, 1.0, bz + 2.4) if kit.arme not in ("lance",) else w.rot_x(10).move(0.9, 0.0, top - 0.3)
        elif arme_pose == "main":
            w = w.rot_x(g("arme_angle", -60)).move(*main_d)
        elif arme_pose == "haut":
            w = w.rot_x(70).move(0.3, 0.6, top + 1.2)
        h.add(w)
    lean = g("buste")
    h = h.rot_x(-lean, cy=0.0, cz=bz).rot_z(g("torsion"), 0, 0)
    m.add(h)
    # pose globale : couché, chute, vol
    if g("couche"):
        m = m.rot_x(-g("couche"), cy=0.0, cz=0.0).move(dz=g("couche_z", 1.0))
    if g("chute"):
        m = m.rot_x(g("chute"), cy=0.0, cz=0.0)
    if g("roulis"):
        m = m.rot_y(g("roulis"))
    if g("vrille"):
        m = m.rot_z(g("vrille"))
    m = m.move(0.0, g("dy"), g("dz"))
    return m


# ---------------------------------------------------------------------------
# Poses et animations
# ---------------------------------------------------------------------------
DEBOUT = dict(hanche_d=2, hanche_g=-2, genou_d=4, genou_g=4, epaule_d=28, coude_d=70, epaule_g=18, coude_g=80, arme="port")
DEBOUT2 = dict(DEBOUT, tete=25, hanche_d=6, genou_d=10, crouch=0.2)
VISE = dict(hanche_d=14, hanche_g=-12, genou_d=6, genou_g=8, epaule_d=62, coude_d=30, epaule_g=78, coude_g=12,
            bras_ecart=18, arme="vise", buste=6)


def course(i):
    t = 2 * math.pi * i / 6
    a = 38 * math.sin(t)
    return dict(hanche_d=a, hanche_g=-a, genou_d=30 + 30 * max(0.0, -math.sin(t)), genou_g=30 + 30 * max(0.0, math.sin(t)),
                epaule_d=34 - 0.3 * a, coude_d=80, epaule_g=24 + 0.4 * a, coude_g=85, buste=14, arme="port",
                dz=0.35 * abs(math.cos(t)), crouch=0.4)


def tir(i, kit):
    p = dict(VISE)
    if kit.arme == "grenade":
        # lancer : armer le bras, puis projeter
        seq = [(-40, 40, 0), (-80, 60, 0), (-120, 80, -6), (-150, 60, -8), (-60, 20, 10), (40, 10, 16), (60, 20, 10), (30, 50, 4)]
        e, c, b = seq[i]
        return dict(hanche_d=14, hanche_g=-14, genou_d=10, genou_g=6, epaule_d=e, coude_d=c, epaule_g=40, coude_g=30,
                    buste=b, arme="main" if i < 5 else "aucune", arme_angle=-40)
    recul = [0.0, 1.0, 0.6, 0.2, 0.0, 1.0, 0.6, 0.2][i]
    p["buste"] = 6 - 4 * recul
    p["flash"] = recul >= 1.0
    return p


def couche(extra=None):
    p = dict(hanche_d=0, hanche_g=0, genou_d=6, genou_g=6, epaule_d=120, coude_d=-40, epaule_g=130, coude_g=-50,
             bras_ecart=22, couche=90, couche_z=0.9, arme="vise", tete_haut=-40)
    p.update(extra or {})
    return p


def reptation(i):
    t = 2 * math.pi * i / 4
    a = 20 * math.sin(t)
    return couche(dict(hanche_d=a, hanche_g=-a, genou_d=40 + 30 * max(0.0, math.sin(t)), genou_g=40 + 30 * max(0.0, -math.sin(t)),
                       epaule_d=130 + a, epaule_g=130 - a, arme="port", dy=0.3 * math.sin(t)))


def tir_couche(i):
    recul = [0.0, 1.0, 0.6, 0.2, 0.0, 1.0, 0.6, 0.2][i]
    return couche(dict(flash=recul >= 1.0, dy=-0.3 * recul))


def transition(k):
    """k = 0 : à genoux ; k = 1 : presque à plat."""
    return [dict(DEBOUT, crouch=2.2, hanche_d=70, genou_d=110, hanche_g=-10, genou_g=100, buste=30),
            couche(dict(couche=55, couche_z=2.4, genou_d=60, genou_g=50))][k]


def attente1(i):
    """Regarde à gauche puis à droite, change d'appui."""
    t = i / 15
    return dict(DEBOUT, tete=60 * math.sin(2 * math.pi * t), hanche_d=4 * math.sin(4 * math.pi * t), genou_d=6 + 6 * max(0.0, math.sin(4 * math.pi * t)))


def attente2(i, kit):
    """S'essuie le front / vérifie son arme (le Révolutionnaire lève le poing)."""
    t = math.sin(math.pi * i / 15)
    if kit.drapeau:
        return dict(DEBOUT, epaule_d=28 + 150 * t, coude_d=70 - 50 * t, arme="main", arme_angle=-80)
    return dict(DEBOUT, epaule_g=18 + 110 * t, coude_g=80 + 40 * t, tete_haut=-10 * t)


def mort(n, i):
    """Cinq morts : 1 en arrière, 2 en avant, 3 projeté, 4 soufflé, 5 brûlé."""
    if n == 1:
        t = min(1.0, i / 6)
        return dict(DEBOUT, chute=88 * t * t, epaule_d=28 + 80 * t, epaule_g=18 + 90 * t, coude_d=20, coude_g=20,
                    genou_d=20 * (1 - t), arme="aucune" if i > 3 else "port", sang=i >= 6, dz=0.4 * t)
    if n == 2:
        t = min(1.0, i / 6)
        k = dict(DEBOUT, crouch=2.0 * min(1.0, t * 2), hanche_d=60 * min(1.0, t * 2), genou_d=100 * min(1.0, t * 2),
                 hanche_g=50 * min(1.0, t * 2), genou_g=100 * min(1.0, t * 2), buste=40 * t, arme="aucune")
        if t > 0.5:
            k.update(chute=-90 * (t - 0.5) * 2, crouch=0.0, hanche_d=10, hanche_g=-5, genou_d=10, genou_g=10,
                     epaule_d=150, epaule_g=160, coude_d=20, coude_g=10, dz=0.9 * (t - 0.5) * 2)
        k["sang"] = i >= 6
        return k
    if n == 3:
        t = i / 7
        return dict(DEBOUT, chute=100 * t, dz=6 * math.sin(math.pi * min(1.0, t * 1.1)) + 0.8 * t, dy=-4 * t,
                    epaule_d=150, epaule_g=140, coude_d=10, coude_g=20, hanche_d=40 * (1 - t), genou_d=60 * (1 - t),
                    hanche_g=-30 * (1 - t), arme="aucune", vrille=40 * t, sang=i == 7)
    if n == 4:
        t = i / 11
        return dict(DEBOUT, chute=-100 * t, roulis=70 * t, dz=9 * math.sin(math.pi * min(1.0, t * 1.2)) + 0.8 * t, dy=5 * t,
                    epaule_d=160, epaule_g=170, coude_d=60, coude_g=50, hanche_d=60 * math.sin(3 * t), genou_d=80,
                    arme="aucune", vrille=200 * t, sang=i >= 10, noirci=0.5)
    # n == 5 : brûlé vif — titube dans les flammes puis s'effondre, calciné
    t = i / 17
    k = dict(course(i % 6), epaule_d=150 + 20 * math.sin(i), epaule_g=140 + 20 * math.cos(i), coude_d=40, coude_g=30,
             arme="aucune", feu=1.0 - max(0.0, t - 0.7) * 3, noirci=min(1.0, t * 1.4))
    if t > 0.55:
        u = min(1.0, (t - 0.55) / 0.3)
        k.update(chute=-90 * u, dz=0.9 * u, hanche_d=10, hanche_g=0, genou_d=20, genou_g=10)
    return k


def parachute():
    return dict(DEBOUT, epaule_d=175, epaule_g=175, coude_d=10, coude_g=10, bras_ecart=25, hanche_d=6, hanche_g=-4,
                genou_d=14, genou_g=10, arme="aucune")


# ---------------------------------------------------------------------------
# Rendu de la feuille
# ---------------------------------------------------------------------------
def _habille(kit, pose):
    m = soldat(_kit_pour(kit, pose), pose)
    if pose.get("sang"):
        m.add(prism(ngon(0.0, -3.0 if pose.get("chute", 0) > 0 else 3.0, 1.6, 8, sy=0.7), 0.02, 0.06, SANG))
    if pose.get("feu"):
        f = pose["feu"]
        import random
        rnd = random.Random(int(f * 1000) + int(pose.get("epaule_d", 0)))
        for _ in range(9):
            x, y, z = rnd.uniform(-1.4, 1.4), rnd.uniform(-1.0, 1.0), rnd.uniform(1.0, 13.0 * f + 1.0)
            m.add(sphere((x, y, z), rnd.uniform(0.6, 1.3) * f + 0.2, FLAMME if rnd.random() < 0.5 else FLAMME2, seg=6, rings=3))
    return m


def _kit_pour(kit, pose):
    n = pose.get("noirci", 0.0)
    if not n:
        return kit
    k = Kit(tenue=Mat((int(90 * (1 - n) + 36 * n), int(84 * (1 - n) + 30 * n), int(60 * (1 - n) + 26 * n)), spec=0.05),
            gilet=CALCINE, casque=kit.casque, casque_mat=CALCINE, arme=kit.arme, sac=kit.sac,
            peau=Mat((int(198 * (1 - n) + 50 * n), int(148 * (1 - n) + 40 * n), int(108 * (1 - n) + 34 * n))), lueur=kit.lueur)
    return k


def image(kit, pose, facing):
    m = _habille(kit, pose)
    img = R.render(m, TAILLE, facing=facing, center=ORIGINE, pitch=PITCH_INF, outline=0.62, dither=0.25, shadow=False)
    # ombre : tache ovale décalée vers le bas à droite, sous les pieds (comme les fantassins RA)
    if not pose.get("sans_ombre"):
        px = img.load()
        couche_ = pose.get("couche") or abs(pose.get("chute", 0)) > 45
        rx, ry = (5.5, 2.2) if couche_ else (3.4, 1.6)
        cx, cy = ORIGINE[0] + 1.5, ORIGINE[1] + 0.5
        for y in range(TAILLE[1]):
            for x in range(TAILLE[0]):
                if px[x, y] == 0 and ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                    px[x, y] = R.SHADOW
    return img


_KITS = {}


def image_nom(nom, pose, facing):
    """Comme image(), l'équipement étant désigné par son nom (utilisable par un Pool)."""
    if not _KITS:
        _KITS.update(kits())
    return image(_KITS[nom], pose, facing)


def feuille(nom, pool=None):
    """Les 343 images dans l'ordre de la disposition commune."""
    kit = kits()[nom]
    jobs = []
    F = [f * 360.0 / FACINGS for f in range(FACINGS)]
    for f in F:
        jobs.append((kit, DEBOUT, f))
    for f in F:
        jobs.append((kit, DEBOUT2, f))
    for f in F:
        for i in range(6):
            jobs.append((kit, course(i), f))
    for f in F:
        for i in range(8):
            jobs.append((kit, tir(i, kit), f))
    for f in F:
        for k in range(2):
            jobs.append((kit, transition(k), f))
    for f in F:
        for i in range(4):
            jobs.append((kit, reptation(i), f))
    for f in F:
        for k in (1, 0):
            jobs.append((kit, transition(k), f))
    for f in F:
        for i in range(8):
            jobs.append((kit, tir_couche(i) if kit.arme != "grenade" else couche(dict(epaule_d=150 - 30 * (i % 4), arme="aucune")), f))
    face = 200.0
    for i in range(16):
        jobs.append((kit, attente1(i), face))
    for i in range(16):
        jobs.append((kit, attente2(i, kit), face))
    for n, L in ((1, 8), (2, 8), (3, 8), (4, 12), (5, 18)):
        for i in range(L):
            jobs.append((kit, mort(n, i), face))
    jobs.append((kit, dict(parachute(), sans_ombre=True), 180.0))
    jobs = [(nom, p, f) for _, p, f in jobs]
    if pool:
        return pool.starmap(image_nom, jobs)
    return [image_nom(*j) for j in jobs]


def sequences(nom, attaque="shoot"):
    """Bloc de séquences yaml correspondant à la disposition commune."""
    f = f"fictifs|bits/{nom}.png"
    prone_attaque = "prone-shoot" if attaque == "shoot" else "prone-" + attaque
    return f"""{nom}:
	Defaults:
		Filename: {f}
	stand:
		Facings: 8
	stand2:
		Start: 8
		Facings: 8
	run:
		Start: 16
		Length: 6
		Facings: 8
		Tick: 100
	{attaque}:
		Start: 64
		Length: 8
		Facings: 8
	liedown:
		Start: 128
		Length: 2
		Facings: 8
	prone-stand:
		Start: 144
		Stride: 4
		Facings: 8
	prone-stand2:
		Start: 144
		Stride: 4
		Facings: 8
	prone-run:
		Start: 144
		Length: 4
		Facings: 8
		Tick: 100
	standup:
		Start: 176
		Length: 2
		Facings: 8
	{prone_attaque}:
		Start: 192
		Length: 8
		Facings: 8
	idle1:
		Start: 256
		Length: 16
		Tick: 120
	idle2:
		Start: 272
		Length: 16
		Tick: 120
	die1:
		Start: 288
		Length: 8
		Tick: 80
	die2:
		Start: 296
		Length: 8
		Tick: 80
	die3:
		Start: 304
		Length: 8
		Tick: 80
	die4:
		Start: 312
		Length: 12
		Tick: 80
	die5:
		Start: 324
		Length: 18
		Tick: 80
	die6:
		Filename: electro.tem
		TilesetFilenames:
			SNOW: electro.sno
		Frames: 0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13
		Length: *
		Tick: 80
	die-crushed:
		Filename: corpse1.tem
		TilesetFilenames:
			SNOW: corpse1.sno
		Length: *
		Tick: 1600
		ZOffset: -511
	parachute:
		Start: 342
	garrison-muzzle:
		Filename:
		Length: 12
		Facings: 8
		Combine:
			0:
				Filename: minigun.shp
				Length: 12
				Frames: 0,1,2,3,4,5,0,1,2,3,4,5
			1:
				Filename: minigun.shp
				Length: 12
				Frames: 6,7,8,9,10,11,6,7,8,9,10,11
			2:
				Filename: minigun.shp
				Length: 12
				Frames: 12,13,14,15,16,17,12,13,14,15,16,17
			3:
				Filename: minigun.shp
				Length: 12
				Frames: 18,19,20,21,22,23,18,19,20,21,22,23
			4:
				Filename: minigun.shp
				Length: 12
				Frames: 24,25,26,27,28,29,24,25,26,27,28,29
			5:
				Filename: minigun.shp
				Length: 12
				Frames: 30,31,32,33,34,35,30,31,32,33,34,35
			6:
				Filename: minigun.shp
				Length: 12
				Frames: 36,37,38,39,40,41,36,37,38,39,40,41
			7:
				Filename: minigun.shp
				Length: 12
				Frames: 42,43,44,45,46,47,42,43,44,45,46,47
	icon:
		Filename: fictifs|bits/{nom}icon.png
"""


# ---------------------------------------------------------------------------
# Équipements des fantassins du mod
# ---------------------------------------------------------------------------
OLIVE = Mat((86, 94, 58), spec=0.25)
VERT_SOMBRE = Mat((60, 78, 52), spec=0.25)
ROUGE = (180, 30, 28)


def kits():
    return {
        # Elielistan : treillis aux couleurs du joueur, casque olive, aigle
        "aguila": Kit(casque="casque", casque_mat=OLIVE, arme="fusil", sac="sac"),
        "cazador": Kit(casque="bob", casque_mat=Mat((70, 84, 50), spec=0.1, tex=noise(121, 0.2, 0.8)), arme="lance",
                       gilet=Mat((58, 72, 44), spec=0.05, tex=noise(122, 0.25, 0.6))),
        "explorador": Kit(casque="bob", casque_mat=OLIVE, arme="designateur", sac="radio"),
        # Rubénie : capote, casque soviétique vert sombre, chapka pour les chasseurs
        "fusilier": Kit(casque="casque", casque_mat=VERT_SOMBRE, arme="fusil", sac="sac", gilet=Mat((96, 84, 60), spec=0.05)),
        "grenadier": Kit(casque="casque", casque_mat=VERT_SOMBRE, arme="grenade", sac="sac", gilet=Mat((82, 90, 60), spec=0.1)),
        "chasseur.at": Kit(casque="chapka", casque_mat=Mat((104, 90, 70), spec=0.05), arme="lance", sac="tube"),
        "eclaireur": Kit(casque="beret", casque_mat=VERT_SOMBRE, arme="pistolet", sac="radio", gilet=Mat((72, 80, 56), spec=0.05)),
        "second.galactique": Kit(casque="visiere", casque_mat=Mat((170, 176, 190), spec=0.8, shine=30), arme="canon_energie",
                                 gilet=Mat((150, 156, 170), spec=0.7, shine=30), lueur=(120, 200, 255), cape=(120, 24, 30)),
        # Australouis
        "revolutionnaire": Kit(casque="beret", casque_mat=Mat(ROUGE, spec=0.1), arme="pistolet", brassard=ROUGE,
                               drapeau=ROUGE, gilet=Mat((64, 60, 52), spec=0.05)),
    }


def icone(kit):
    """Portrait en pied, arme en main."""
    m = _habille(kit, dict(VISE) if kit.arme not in ("grenade", "designateur") else dict(DEBOUT))
    return R.icon(m, zoom=2.9, facing=240, dz=12)
