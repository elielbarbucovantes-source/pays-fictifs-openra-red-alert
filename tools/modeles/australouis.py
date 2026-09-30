"""Modèles 3D des unités australouisiennes."""
import math
from rendu3d import Mesh, merge, prism, box, tube, sphere, rect, rrect, ngon, team
from modeles.communs import (blinde, chenilles, canon, antenne, trappe, ACIER, ACIER_SOMBRE, GRILLE, CAOUTCHOUC,
                             JERRICAN, FEU_ROUGE, CHENILLE)


def cheaper_caisse():
    """Char produit à la chaîne : caisse soudée anguleuse, chenilles nues, moteur arrière."""
    hull = blinde(4.5, 2)
    m = Mesh()
    # caisse : glacis avant incliné, flancs droits, arrière plat
    prof = [(-4.6, -9.0), (4.6, -9.0), (4.6, 7.0), (3.6, 9.2), (-3.6, 9.2), (-4.6, 7.0)]
    m.add(prism(prof, 1.6, 4.4, hull, bevel=0.9))
    # garde-boue au-dessus des chenilles
    for s in (1, -1):
        m.add(box(min(s * 4.4, s * 6.4), -9.2, 4.0, max(s * 4.4, s * 6.4), 9.0, 4.45, hull, bevel=0.1))
    # chenilles
    tr = chenilles(18.4, 2.0, 4.1, 5.4, 0.0, wheels=5)
    m.add(tr).add(tr.mirror_x())
    # grille moteur arrière + échappements
    m.add(box(-3.2, -8.6, 4.4, 3.2, -4.8, 4.75, GRILLE))
    for s in (1, -1):
        m.add(tube((s * 2.4, -9.1, 3.2), (s * 2.4, -9.9, 3.2), 0.5, mat=ACIER_SOMBRE, seg=6))
    # jerricans et caisse à outils sur la plage arrière
    m.add(box(3.4, -8.8, 4.45, 4.3, -6.9, 5.6, JERRICAN, bevel=0.15))
    m.add(box(2.4, -8.8, 4.45, 3.3, -6.9, 5.6, JERRICAN, bevel=0.15))
    m.add(box(-4.2, -8.7, 4.45, -1.8, -7.3, 5.2, ACIER_SOMBRE, bevel=0.2))
    # phares et feux
    for s in (1, -1):
        m.add(tube((s * 3.0, 9.0, 4.0), (s * 3.0, 9.6, 4.0), 0.45, mat=Mat_phare, seg=6))
        m.add(box(s * 3.7 - 0.3, -9.15, 3.6, s * 3.7 + 0.3, -9.0, 4.0, FEU_ROUGE))
    # poste de pilotage : épiscopes
    m.add(box(-2.6, 6.2, 4.4, -1.0, 7.0, 4.8, ACIER_SOMBRE))
    return m


def cheaper_tourelle():
    """Tourelle moulée arrondie, canon moyen court, trappe et antenne."""
    t = blinde(3.5, 5)
    m = Mesh()
    ring = ngon(0, -0.5, 3.9, 12, sx=1.0, sy=1.15)
    m.add(prism(ring, 4.4, 7.4, t, bevel=1.2))
    # masque du canon
    m.add(box(-1.6, 3.3, 5.1, 1.6, 5.0, 6.9, t, bevel=0.4))
    m.add(canon(4.8, 13.0, 6.0, 0.55, manchon=(0.25, 0.6)))
    # tourelleau du chef, trappe du chargeur, mitrailleuse
    m.add(trappe(-1.6, -1.6, 7.3, 1.25))
    m.add(trappe(1.8, -0.8, 7.3, 0.9))
    m.add(tube((1.8, -0.2, 8.0), (1.8, 2.4, 8.0), 0.18, mat=ACIER_SOMBRE, seg=4))
    # coffre arrière
    m.add(box(-2.6, -5.9, 4.9, 2.6, -4.3, 6.6, t, bevel=0.3))
    m.add(antenne(-2.4, -4.8, 6.6, 7.0))
    # fumigènes
    for s in (1, -1):
        for k in range(3):
            m.add(tube((s * 3.0, 1.2 + k * 0.8, 6.5), (s * 3.9, 1.6 + k * 0.8, 6.9), 0.28, mat=ACIER_SOMBRE, seg=5))
    return m


from rendu3d import Mat  # noqa: E402
Mat_phare = Mat((230, 226, 190), spec=0.8, shine=30)


# ---------------------------------------------------------------------------
# Aviation
# ---------------------------------------------------------------------------
from modeles.communs import (fuselage, verriere, missile, cocarde, avec_marques, GRIS_NAVAL,  # noqa: E402
                             GRIS_FONCE, VERRIERE, TUYERE, sillage)
import rendu3d as R  # noqa: E402
from rendu3d import panels, noise, combine, stripes, Mat, wing  # noqa: E402

# Couleurs nationales : bleu océan, vague blanche, îles vertes.
BLEU_AUS = (22, 80, 156)
BLANC = (236, 238, 236)
VERT_AUS = (40, 140, 70)
COCARDE_AUS = [VERT_AUS, BLANC, BLEU_AUS]


def gris_marques(base, *cocardes):
    return Mat(base.color, spec=base.spec, shine=base.shine,
               tex=avec_marques(base.tex, *[cocarde(x, y, r, COCARDE_AUS) for x, y, r in cocardes]))


def albatros():
    """Chasseur omnirôle delta-canard (famille Rafale), gris naval, dérive aux couleurs du joueur."""
    m = Mesh()
    corps = Mat(GRIS_NAVAL.color, spec=0.45, shine=26, tex=combine(panels(3.5, 0.3, 0.86), noise(4, 0.04)))
    peau = R.team(spec=0.45, tex=combine(panels(3.5, 0.3, 0.86), noise(4, 0.04)))
    m.add(fuselage([(-13.0, 1.3, 1.0, 0.0), (-11.5, 2.1, 1.5, 0.1), (-6.0, 2.6, 1.7, 0.2), (0.0, 2.4, 1.7, 0.3),
                    (5.0, 1.8, 1.5, 0.4), (9.0, 1.2, 1.1, 0.3), (12.0, 0.6, 0.6, 0.1), (13.2, 0.3, 0.3, 0.05)], peau, seg=14))
    m.add(fuselage([(13.1, 0.35, 0.35, 0.05), (14.2, 0.05, 0.05, 0.0)], GRIS_FONCE, seg=8))   # radôme
    # entrées d'air sous les ailes, de part et d'autre du fuselage
    for s in (1, -1):
        m.add(fuselage([(-3.0, 0.9, 1.0, -0.9), (2.5, 1.0, 1.0, -0.9), (4.0, 0.6, 0.7, -0.9)], GRIS_FONCE, seg=8).move(dx=s * 2.3))
    # voilure delta + plans canard
    aile = gris_marques(corps, (8.5, -6.5, 1.5), (-8.5, -6.5, 1.5))
    for s in (1, -1):
        w = wing((1.8, 1.5), (1.8, -11.0), (10.5, -8.2), (10.5, -10.6), 0.0, aile, thick=0.7, dihedral=-0.3)
        m.add(w if s > 0 else w.mirror_x())
        c = wing((1.6, 6.5), (1.6, 4.0), (5.0, 3.6), (5.0, 2.8), 0.6, corps, thick=0.4, dihedral=0.3)
        m.add(c if s > 0 else c.mirror_x())
        # missiles en bout d'aile et sous voilure
        m.add(missile(s * 10.7, -10.5, -5.0, -0.1, 0.32))
        m.add(missile(s * 6.5, -8.0, -1.5, -0.8, 0.4))
    # dérive unique aux couleurs du joueur, avec liseré
    fin = R.team(spec=0.35, tex=panels(3.0, 0.3, 0.84))
    m.add(prism([(-0.25, -12.8), (0.25, -12.8), (0.25, -6.0), (-0.25, -6.0)], 0.8, 1.0, fin))
    fin_pts = [(-12.8, 1.0), (-6.0, 1.0), (-10.0, 6.4), (-12.4, 6.6)]
    rings = [[(dx, y, z) for y, z in fin_pts] for dx in (-0.3, 0.3)]
    m.add(R.loft(rings, fin))
    # tuyères jumelées, réacteurs rougeoyants
    for s in (1, -1):
        m.add(tube((s * 0.9, -12.6, 0.1), (s * 0.9, -14.0, 0.1), 0.9, 1.0, mat=TUYERE, seg=10))
        m.add(tube((s * 0.9, -14.0, 0.1), (s * 0.9, -14.05, 0.1), 0.7, mat=Mat((240, 150, 60), emit=True), seg=8))
    # verrière et perche de ravitaillement
    m.add(verriere(3.5, 10.5, 1.1, 1.2, 1.2))
    m.add(tube((0.9, 8.0, 1.3), (0.9, 12.0, 1.1), 0.12, mat=GRIS_FONCE, seg=4))
    return m


def warthog():
    """Avion d'appui rapproché blindé (famille A-10) : aile droite, réacteurs en nacelles, double dérive."""
    m = Mesh()
    vert = Mat((96, 104, 94), spec=0.3, shine=18, tex=combine(panels(3.5, 0.3, 0.84), noise(6, 0.05)))
    peau = R.team(spec=0.3, tex=combine(panels(3.5, 0.3, 0.84), noise(6, 0.05)))
    m.add(fuselage([(-12.0, 0.9, 0.9, 0.6), (-8.0, 1.7, 1.5, 0.5), (-2.0, 2.4, 2.1, 0.3), (4.0, 2.4, 2.2, 0.2),
                    (9.0, 2.0, 2.0, 0.0), (11.5, 1.4, 1.4, -0.2), (12.5, 0.7, 0.7, -0.3)], peau, seg=14))
    # canon rotatif GAU-8 sous le nez
    m.add(tube((0, 12.2, -0.6), (0, 14.2, -0.6), 0.35, mat=Mat((60, 60, 60), spec=0.6), seg=6))
    aile = gris_marques(vert, (10.0, -1.0, 1.4), (-10.0, -1.0, 1.4))
    for s in (1, -1):
        w = wing((1.8, 1.8), (1.8, -3.2), (15.5, 0.9), (15.5, -2.4), -0.6, aile, thick=0.9, dihedral=0.5)
        m.add(w if s > 0 else w.mirror_x())
        # nacelles réacteurs arrière, sur pylônes
        m.add(fuselage([(-7.8, 1.1, 1.1, 2.2), (-6.0, 1.6, 1.6, 2.2), (-2.4, 1.6, 1.6, 2.2), (-1.4, 1.35, 1.35, 2.2)],
                       GRIS_FONCE, seg=12).move(dx=s * 2.7))
        m.add(tube((s * 2.7, -1.35, 2.2), (s * 2.7, -1.4, 2.2), 1.2, mat=Mat((30, 30, 32)), seg=10))
        m.add(box(min(s * 1.6, s * 2.2), -5.5, 1.0, max(s * 1.6, s * 2.2), -3.0, 1.4, vert))
        # empennage : plan fixe + dérives aux couleurs du joueur
        t = wing((0.5, -9.5), (0.5, -12.0), (6.2, -10.0), (6.2, -12.2), 0.8, vert, thick=0.5)
        m.add(t if s > 0 else t.mirror_x())
        fin = R.team(spec=0.35, tex=panels(3.0, 0.3, 0.84))
        pts = [(-12.3, 0.4), (-9.6, 0.4), (-10.4, 4.4), (-12.6, 4.4)]
        rings = [[(s * 6.2 + dx, y, z) for y, z in pts] for dx in (-0.25, 0.25)]
        m.add(R.loft(rings, fin))
        # emports : bombes et roquettes sous voilure
        for k, x in enumerate((5.0, 8.0, 11.5)):
            m.add(fuselage([(-1.8, 0.1, 0.1, -1.4), (-1.2, 0.55, 0.55, -1.4), (1.6, 0.55, 0.55, -1.4), (2.6, 0.1, 0.1, -1.4)],
                           Mat((110, 116, 90), spec=0.3), seg=6).move(dx=s * x))
    m.add(verriere(5.5, 10.5, 1.2, 1.3, 1.6))
    return m


def manta():
    """Bombardier furtif en aile volante, découpé comme une raie manta : bords en dents de scie."""
    m = Mesh()
    import math

    def dessus(p, n):
        # liseré équipe le long du bord d'attaque, reste en gris anthracite à panneaux
        if p[1] > 13.0 - 0.792 * abs(p[0]) - 1.8:
            return 1.0
        k = 0.86 if (p[0] / 4.5) % 1.0 < 0.07 or (p[1] / 4.5) % 1.0 < 0.07 else 1.0
        v = math.sin(p[0] * 3.1 + p[1] * 1.7) * 4
        return (int((82 + v) * k), int((88 + v) * k), int((98 + v) * k))
    peau = R.team(spec=0.35, shine=16, tex=dessus)
    sombre = Mat((58, 62, 70), spec=0.3, shine=16, tex=panels(4.5, 0.3, 0.88))
    # contour de l'aile : bord d'attaque en flèche, bord de fuite en dents de scie
    le = [(0.0, 13.0), (6.0, 8.2), (14.0, 1.8), (22.0, -4.6), (26.0, -7.6)]
    te = [(26.0, -9.0), (22.0, -9.4), (18.0, -6.4), (13.0, -10.2), (8.0, -6.8), (3.5, -10.4), (0.0, -8.0)]
    half = le + te
    for s in (1, -1):
        top, bot = [], []
        for x, y in half:
            # épaisseur : forte au centre (cockpit, soutes), fine en bout d'aile
            e = max(0.35, 2.4 * (1 - x / 26.0) ** 1.6)
            top.append((s * x, y, e * 0.65))
            bot.append((s * x, y, -e * 0.35))
        n = len(half)
        for i in range(n - 1):
            a, b = i, i + 1
            if s > 0:
                m.quad(top[a], top[b], bot[b], bot[a], sombre)
            else:
                m.quad(top[b], top[a], bot[a], bot[b], sombre)
        # surfaces supérieure et inférieure en éventail depuis l'axe
        c_top, c_bot = (0.0, 1.0, 1.8), (0.0, 1.0, -0.8)
        for i in range(n - 1):
            if s > 0:
                m.tri(c_top, top[i + 1], top[i], peau)
                m.tri(c_bot, bot[i], bot[i + 1], sombre)
            else:
                m.tri(c_top, top[i], top[i + 1], peau)
                m.tri(c_bot, bot[i + 1], bot[i], sombre)
        # bosses des réacteurs enterrés, entrées en S et sorties plates
        m.add(fuselage([(-6.5, 0.4, 0.3, 1.1), (-4.0, 1.7, 0.9, 1.2), (1.5, 1.8, 1.0, 1.1), (4.0, 1.0, 0.6, 1.0)], peau, seg=10).move(dx=s * 4.2))
        m.add(box(s * 4.2 - 1.3, 3.2, 1.2, s * 4.2 + 1.3, 3.8, 1.7, Mat((24, 24, 26))))
        m.add(box(s * 4.2 - 1.6, -7.2, 0.6, s * 4.2 + 1.6, -6.5, 1.0, Mat((200, 120, 60), emit=True)))
    # cockpit surélevé, vitres en bande
    m.add(fuselage([(4.0, 0.5, 0.3, 1.6), (7.0, 2.2, 0.9, 1.5), (10.5, 1.8, 0.7, 1.3), (12.5, 0.5, 0.2, 1.0)], peau, seg=12))
    m.add(verriere(7.5, 10.6, 1.5, 0.55, 2.1, Mat((40, 70, 100), spec=1.3, shine=60)))
    return m


def moustique():
    """Munition rôdeuse : corps cylindrique, double aile en X, caméra au nez, hélice propulsive."""
    m = Mesh()
    corps = R.team(spec=0.35, tex=panels(2.0, 0.25, 0.85))
    m.add(fuselage([(-3.6, 0.5, 0.5, 0), (-3.0, 0.75, 0.75, 0), (2.6, 0.75, 0.75, 0), (3.6, 0.55, 0.55, 0),
                    (4.2, 0.1, 0.1, 0)], corps, seg=10))
    m.add(sphere((0, 3.7, -0.15), 0.4, Mat((30, 34, 40), spec=1.4, shine=60), seg=8, rings=4))   # boule optronique
    gris = Mat((150, 154, 150), spec=0.4)
    for ang in (35, -35):
        for y0 in (1.6, -2.4):
            w = wing((0.5, y0 + 0.6), (0.5, y0 - 0.5), (3.8, y0 + 0.2), (3.8, y0 - 0.5), 0.0, gris, thick=0.25)
            m.add(w.rot_y(ang)).add(w.mirror_x().rot_y(-ang))
    # hélice propulsive (disque flou)
    for k in range(3):
        a = k * 120 + 20
        m.add(box(-0.18, -3.9, 0.0, 0.18, -3.7, 1.4, Mat((70, 70, 72), spec=0.3)).rot_y(a))
    return m.scale(1.7)


def cormoran():
    """Hydravion bombardier d'eau amphibie (famille Canadair) : coque de bateau, aile haute,
    deux turbopropulseurs, flotteurs de bout d'aile, livrée jaune et rouge des bombardiers d'eau."""
    m = Mesh()
    jaune = Mat((226, 184, 38), spec=0.4, shine=22, tex=combine(panels(3.5, 0.3, 0.86), noise(3, 0.04)))
    rouge = Mat((196, 44, 36), spec=0.35, shine=20)
    peau = R.team(spec=0.35, tex=panels(3.5, 0.3, 0.86))
    # coque : bas en V (rouge), haut du fuselage jaune
    m.add(fuselage([(-15.0, 0.4, 0.5, 1.2), (-12.0, 1.3, 1.1, 0.8), (-6.0, 2.2, 1.9, 0.3), (2.0, 2.4, 2.1, 0.0),
                    (8.0, 2.3, 2.1, -0.1), (11.5, 1.8, 1.8, -0.2), (13.0, 1.0, 1.1, -0.2), (13.8, 0.2, 0.3, -0.3)], jaune, seg=14))
    m.add(fuselage([(-6.0, 1.6, 0.9, -1.5), (2.0, 2.2, 1.1, -1.9), (9.0, 2.0, 1.0, -1.9), (12.0, 1.0, 0.6, -1.6)], rouge, seg=10))
    # hublots
    for k in range(5):
        for s in (1, -1):
            m.add(box(s * 2.3 - 0.05, -4 + k * 2.2, 0.4, s * 2.3 + 0.05, -3.4 + k * 2.2, 0.9, Mat((40, 60, 80), spec=1.0)))
    # poste de pilotage vitré
    m.add(verriere(9.0, 12.4, 1.8, 1.1, 1.3, VERRIERE))
    # aile haute rectangulaire avec cocardes, portée par le dos
    aile = Mat(jaune.color, spec=0.4, shine=22,
               tex=avec_marques(panels(4.0, 0.3, 0.86), cocarde(13.0, 2.6, 1.5, COCARDE_AUS), cocarde(-13.0, 2.6, 1.5, COCARDE_AUS),
                                lambda p, n: (196, 44, 36) if abs(p[0]) > 16.5 else None))
    for s in (1, -1):
        w = wing((1.0, 5.2), (1.0, 0.2), (19.0, 4.6), (19.0, 1.0), 2.6, aile, thick=0.9)
        m.add(w if s > 0 else w.mirror_x())
        # nacelles moteurs et hélices quadripales (disque)
        m.add(fuselage([(-2.5, 0.6, 0.6, 2.2), (0.0, 1.2, 1.3, 2.0), (4.5, 1.2, 1.2, 2.2), (6.8, 0.7, 0.7, 2.3)], peau, seg=10).move(dx=s * 6.5))
        for k in range(4):
            pale = box(-0.22, 7.0, 0.2, 0.22, 7.15, 3.5, Mat((60, 60, 62), spec=0.3)).rot_y(k * 90 + 30)
            m.add(pale.move(dx=s * 6.5, dz=2.3))
        m.add(sphere((s * 6.5, 7.3, 2.3), 0.5, Mat((200, 44, 36), spec=0.5), seg=8, rings=4))
        # flotteurs de bout d'aile sur mâts
        m.add(fuselage([(-0.2, 0.2, 0.2, -0.3), (1.0, 0.7, 0.6, -0.3), (4.0, 0.7, 0.6, -0.3), (5.0, 0.2, 0.2, -0.3)], rouge, seg=8).move(dx=s * 16.5))
        m.add(box(s * 16.5 - 0.15, 2.0, 0.0, s * 16.5 + 0.15, 3.0, 2.3, jaune))
    # empennage en T : dérive aux couleurs du joueur, plan horizontal en haut
    pts = [(-15.0, 1.0), (-10.5, 1.4), (-12.6, 7.4), (-15.6, 7.6)]
    m.add(R.loft([[(dx, y, z) for y, z in pts] for dx in (-0.35, 0.35)], peau))
    t = wing((0.3, -12.6), (0.3, -15.4), (6.5, -13.6), (6.5, -15.4), 7.2, jaune, thick=0.4)
    m.add(t).add(t.mirror_x())
    return m


def baleine():
    """Hélicoptère de transport lourd à rotors en tandem, fuselage ventru « baleine », rampe arrière."""
    m = Mesh()
    peau = R.team(spec=0.3, tex=combine(panels(3.2, 0.3, 0.84), noise(5, 0.05)))
    gris = Mat((104, 110, 108), spec=0.3, tex=panels(3.2, 0.3, 0.86))
    m.add(fuselage([(-17.0, 1.2, 1.4, 2.4), (-15.0, 2.6, 2.6, 2.0), (-8.0, 3.2, 3.0, 1.6), (4.0, 3.2, 3.0, 1.6),
                    (11.0, 3.0, 2.8, 1.5), (14.5, 2.2, 2.0, 1.1), (16.2, 1.0, 1.0, 0.8)], peau, seg=16))
    # pylône arrière surélevé (rotor arrière plus haut)
    m.add(fuselage([(-17.0, 0.6, 0.6, 4.6), (-15.5, 1.3, 1.5, 4.8), (-11.0, 1.3, 1.5, 4.4), (-8.0, 0.6, 0.6, 3.8)], peau, seg=10))
    m.add(tube((0, -14.5, 5.8), (0, -14.5, 6.8), 0.9, 0.6, mat=gris, seg=8))
    m.add(tube((0, 13.0, 4.2), (0, 13.0, 5.2), 0.9, 0.6, mat=gris, seg=8))
    # poste de pilotage vitré
    for s in (1, -1):
        m.add(box(s * 2.2 - 0.9, 13.0, 1.8, s * 2.2 + 0.9, 15.4, 3.1, Mat((50, 84, 110), spec=1.1, shine=40)))
    # carénages latéraux (réservoirs et train)
    for s in (1, -1):
        m.add(fuselage([(-9.0, 0.6, 0.6, 0.4), (-7.0, 1.3, 1.1, 0.4), (6.0, 1.3, 1.1, 0.4), (8.0, 0.6, 0.6, 0.4)], gris, seg=10).move(dx=s * 3.3))
        for y in (-8.5, 7.5):
            m.add(tube((s * 3.3, y, -1.0), (s * 3.3 + s * 0.3, y, -1.0), 0.8, mat=Mat((40, 40, 40)), seg=8))
        # hublots
        for k in range(6):
            m.add(box(s * 3.15 - 0.05, -6 + k * 2.4, 2.3, s * 3.15 + 0.05, -5.3 + k * 2.4, 3.0, Mat((40, 60, 80), spec=1.0)))
    # bande aux couleurs nationales, portes latérales, marchepied, antennes
    for s in (1, -1):
        m.add(box(s * 3.2 - 0.06, 8.0, 0.6, s * 3.2 + 0.06, 10.2, 3.2, Mat((70, 76, 72), spec=0.2)))
    m.add(box(-2.6, -17.2, -0.6, 2.6, -15.0, 0.6, gris, bevel=0.2))        # rampe arrière
    m.add(tube((0, 2.0, 4.6), (0, 2.0, 6.4), 0.12, mat=Mat((40, 40, 40)), seg=4))
    m.add(tube((1.2, -3.0, 4.6), (1.2, -3.0, 5.6), 0.12, mat=Mat((40, 40, 40)), seg=4))
    # prises d'air des turbines sur le pylône
    for s in (1, -1):
        m.add(fuselage([(-13.5, 0.5, 0.5, 4.6), (-12.5, 0.9, 0.9, 4.6), (-9.5, 0.9, 0.9, 4.6), (-8.8, 0.6, 0.6, 4.6)], gris, seg=8).move(dx=s * 1.8))
    return m


# ---------------------------------------------------------------------------
# Marine
# ---------------------------------------------------------------------------
GRIS_COQUE = Mat((118, 126, 132), spec=0.35, shine=20, tex=combine(panels(5.0, 0.3, 0.88), noise(11, 0.05)))
PONT = Mat((92, 96, 94), spec=0.15, tex=combine(panels(3.0, 0.25, 0.9, axis="x"), noise(12, 0.06)))
ROUILLE = Mat((110, 64, 44), spec=0.1)
LIGNE_EAU = Mat((150, 40, 36), spec=0.2)


def coque(length, beam, franc_bord, proue=0.32, poupe=0.12, deck_mat=None, side_mat=None, tonture=0.6):
    """Coque de navire : étrave effilée, arrière à tableau ; bordé gris, pont à part, ligne de flottaison rouge."""
    y0, y1 = -length / 2, length / 2
    pts = []
    n = 10
    for i in range(n + 1):                   # flanc tribord de la poupe à la proue
        t = i / n
        y = y0 + (y1 - y0) * t
        if t > 1 - proue:
            k = (t - (1 - proue)) / proue
            half = beam / 2 * max(0.0, math.cos(k * math.pi / 2)) ** 0.8 + 0.05
        elif t < poupe:
            half = beam / 2 * (0.82 + 0.18 * t / poupe)
        else:
            half = beam / 2
        pts.append((half, y))
    poly = [(x, y) for x, y in pts] + [(-x, y) for x, y in reversed(pts)]
    poly = [p for i, p in enumerate(poly) if i == 0 or (abs(p[0] - poly[i - 1][0]) > 1e-3 or abs(p[1] - poly[i - 1][1]) > 1e-3)]
    m = Mesh()
    side = side_mat or GRIS_COQUE
    m.add(R.prism(poly, -0.4, 0.25, LIGNE_EAU))
    body = R.prism(poly, 0.25, franc_bord, side, top=deck_mat or PONT)
    # tonture : l'avant remonte
    body = body.map(lambda p: (p[0], p[1], p[2] + (tonture * max(0.0, p[1] / y1) ** 2 if p[2] > 0.3 else 0.0)))
    m.add(body)
    m.add(sillage(poly))
    return m, poly


def l0u15_coque():
    """Destroyer léger : coque fine, grande mâture de sonar et radar, dôme sonar d'étrave."""
    import math as _m  # noqa: F401
    m, poly = coque(44.0, 8.4, 2.4)
    peau = R.team(spec=0.35, tex=combine(panels(3.0, 0.3, 0.86), noise(13, 0.04)))
    # superstructure étagée aux couleurs du joueur
    m.add(R.prism(R.rrect(-3.0, -8.0, 3.0, 4.0, 1.0), 2.4, 5.2, peau, bevel=0.3))
    m.add(R.prism(R.rrect(-2.4, -2.0, 2.4, 3.4, 0.8), 5.2, 7.4, peau, bevel=0.3))
    # passerelle vitrée
    m.add(box(-2.3, 3.0, 6.2, 2.3, 3.5, 7.0, Mat((40, 64, 88), spec=1.1, shine=40)))
    # grand mât-treillis avec radôme sonar de veille lointaine (la spécialité du L-0U15)
    for dx, dy in ((-0.9, -0.2), (0.9, -0.2), (0.0, 1.4)):
        m.add(tube((dx, dy, 7.4), (dx * 0.3, 0.3 + dy * 0.3, 13.0), 0.18, mat=ACIER_SOMBRE, seg=4))
    m.add(sphere((0.0, 0.4, 14.4), 1.7, Mat((226, 228, 224), spec=0.5, shine=20), seg=12, rings=8))
    m.add(box(-2.2, 0.2, 11.0, 2.2, 0.6, 11.3, ACIER_SOMBRE))   # vergue
    # cheminée
    m.add(R.prism(R.rrect(-1.2, -6.8, 1.2, -4.0, 0.5), 5.2, 7.8, GRIS_COQUE, bevel=0.2))
    m.add(box(-0.9, -6.4, 7.8, 0.9, -4.4, 8.0, Mat((30, 30, 30))))
    # antenne sonar remorquée (treuil à la poupe) et hélicoptère léger absent : plateforme
    m.add(tube((-1.4, -19.5, 2.8), (1.4, -19.5, 2.8), 1.0, mat=ACIER_SOMBRE, seg=10))
    m.add(box(-3.2, -17.0, 2.42, 3.2, -10.5, 2.5, Mat((70, 74, 72), spec=0.1,
                                                     tex=lambda p, n: (230, 230, 220) if abs(math.hypot(p[0], p[1] + 13.8) - 2.0) < 0.3 else 1.0)))
    # lance-torpilles, radeaux, bastingage
    for s in (1, -1):
        m.add(tube((s * 3.4, -3.0, 3.0), (s * 3.4, 0.5, 3.0), 0.35, mat=ACIER_SOMBRE, seg=6))
        m.add(tube((s * 3.2, -9.5, 3.0), (s * 3.2, -8.3, 3.0), 0.6, mat=Mat((210, 90, 40)), seg=6))
    # chaîne d'ancre et bitte d'amarrage
    m.add(box(-0.3, 16.0, 2.9, 0.3, 19.0, 3.0, ACIER_SOMBRE))
    return m


def l0u15_tourelle():
    """Tourelle de 76 mm furtive, facettée."""
    peau = R.team(spec=0.4, tex=panels(2.0, 0.25, 0.86))
    m = Mesh()
    m.add(R.prism([(-1.8, -1.8), (1.8, -1.8), (1.8, 0.6), (0.9, 2.0), (-0.9, 2.0), (-1.8, 0.6)], 0.0, 1.6, peau, bevel=0.6))
    m.add(tube((0, 1.6, 0.9), (0, 6.0, 0.9), 0.3, mat=ACIER, seg=6))
    m.add(tube((0, 5.4, 0.9), (0, 6.2, 0.9), 0.42, mat=ACIER_SOMBRE, seg=6))
    return m


def sousmarin_icbm():
    """Sous-marin lanceur d'engins : long fuseau noir, kiosque à barres de plongée, dos de silos."""
    m = Mesh()
    noir = Mat((46, 50, 54), spec=0.35, shine=22, tex=combine(panels(4.0, 0.2, 0.9, axis="y"), noise(14, 0.05)))
    m.add(fuselage([(-26.0, 0.3, 0.3, 0.6), (-22.0, 2.0, 1.4, 0.9), (-12.0, 3.4, 2.2, 1.1), (10.0, 3.4, 2.2, 1.1),
                    (20.0, 2.8, 1.9, 1.0), (25.0, 1.4, 1.1, 0.8), (26.5, 0.3, 0.3, 0.6)], noir, seg=16))
    # dos de silos : 8 trappes aux couleurs du joueur
    m.add(R.prism(R.rrect(-2.0, -12.0, 2.0, 5.0, 1.5), 1.8, 2.8, noir, bevel=0.6))
    for k in range(8):
        y = -10.6 + (k // 2) * 4.0
        x = -0.95 if k % 2 == 0 else 0.95
        m.add(tube((x, y, 2.8), (x, y, 2.95), 0.75, mat=R.team(spec=0.3), seg=10))
    # kiosque et barres de plongée
    m.add(R.prism([(-0.9, 8.0), (0.9, 8.0), (1.0, 11.5), (0.6, 13.2), (-0.6, 13.2), (-1.0, 11.5)], 1.8, 5.6, noir, bevel=0.4))
    m.add(box(-3.4, 10.4, 4.3, 3.4, 11.6, 4.55, noir))
    m.add(tube((0.2, 11.0, 5.6), (0.2, 11.0, 7.0), 0.15, mat=ACIER, seg=4))
    m.add(tube((-0.3, 12.0, 5.6), (-0.3, 12.0, 6.5), 0.2, mat=ACIER, seg=4))
    # gouvernes en croix et hélice carénée
    m.add(box(-4.2, -24.0, 0.2, 4.2, -22.6, 0.45, noir))
    m.add(box(-0.15, -24.0, -1.2, 0.15, -22.6, 2.8, noir))
    m.add(sillage([(3.0, -14), (3.0, 12), (2.2, 20), (0.4, 25.5), (-0.4, 25.5), (-2.2, 20), (-3.0, 12), (-3.0, -14), (-1.6, -22), (1.6, -22)], largeur=0.8))
    return m.move(dz=0.6)


def mothership():
    """Navire-mère terraformeur : immense coque plate, drague aspiratrice, tubes de refoulement
    de sable, grues portiques, citadelle blindée, héliport. Presque sans armes."""
    m, poly = coque(88.0, 30.0, 4.0, proue=0.22, poupe=0.08, tonture=1.2)
    peau = R.team(spec=0.35, tex=combine(panels(4.0, 0.3, 0.86), noise(21, 0.05)))
    blind = Mat((96, 100, 98), spec=0.3, tex=combine(panels(3.0, 0.3, 0.84), noise(22, 0.06)))
    jaune = Mat((214, 170, 40), spec=0.3, tex=stripes(1.2, 0.35, axis=0))
    sable = Mat((200, 176, 120), spec=0.05, tex=noise(23, 0.12, 1.0))
    # citadelle blindée arrière aux couleurs du joueur, étagée
    m.add(R.prism(R.rrect(-12.0, -40.0, 12.0, -22.0, 3.0), 4.0, 9.0, peau, bevel=0.8))
    m.add(R.prism(R.rrect(-8.0, -36.0, 8.0, -26.0, 2.0), 9.0, 13.0, peau, bevel=0.6))
    m.add(box(-7.8, -26.6, 11.0, 7.8, -26.0, 12.4, Mat((40, 64, 88), spec=1.1, shine=40)))   # passerelle vitrée
    for x in (-5.0, 5.0):                                                                     # radars et mâts
        m.add(tube((x, -31.0, 13.0), (x, -31.0, 17.0), 0.3, mat=ACIER_SOMBRE, seg=5))
        m.add(sphere((x, -31.0, 17.6), 1.3, Mat((222, 224, 220), spec=0.4), seg=10, rings=6))
    m.add(R.prism(R.rrect(-2.5, -39.5, 2.5, -35.0, 1.0), 13.0, 16.0, blind, bevel=0.4))   # cheminée
    m.add(box(-2.0, -39.0, 16.0, 2.0, -35.5, 16.2, Mat((28, 28, 28))))
    # héliport sur la plage arrière
    m.add(box(-11.0, -43.5, 4.0, 11.0, -40.5, 4.2, blind))
    # grand puits de sable au centre : cales ouvertes remplies de sable
    for y0 in (-18.0, -4.0, 10.0):
        m.add(R.prism(R.rrect(-11.0, y0, 11.0, y0 + 12.0, 1.0), 4.0, 5.2, blind, top=blind))
        m.add(box(-10.0, y0 + 1.0, 5.0, 10.0, y0 + 11.0, 5.4, sable))
    # portiques jaunes enjambant les cales, avec treuils
    for y in (-12.0, 4.0, 18.0):
        for s in (1, -1):
            m.add(box(s * 13.0 - 0.8, y - 0.8, 4.0, s * 13.0 + 0.8, y + 0.8, 13.0, jaune))
        m.add(box(-13.8, y - 1.0, 13.0, 13.8, y + 1.0, 14.6, jaune))
        m.add(box(-2.0, y - 1.4, 11.4, 2.0, y + 1.4, 13.0, ACIER_SOMBRE))
    # tubes de refoulement du sable vers l'avant, sur chevalets, et buse orientable à l'étrave
    for s in (1, -1):
        m.add(tube((s * 9.0, -20.0, 6.2), (s * 9.0, 34.0, 6.2), 1.1, mat=Mat((70, 72, 70), spec=0.4), seg=10))
        for y in range(-16, 34, 8):
            m.add(box(s * 9.0 - 0.3, y - 0.3, 4.0, s * 9.0 + 0.3, y + 0.3, 5.3, ACIER_SOMBRE))
    m.add(tube((0, 32.0, 7.0), (0, 44.0, 9.0), 1.4, 0.9, mat=jaune, seg=10))
    m.add(R.prism(R.rrect(-10.0, 30.0, 10.0, 34.0, 1.5), 4.0, 7.2, blind, bevel=0.5))
    # élindes de dragage le long des flancs (bras aspirateurs)
    for s in (1, -1):
        m.add(tube((s * 15.6, 20.0, 3.4), (s * 15.9, -30.0, 1.2), 1.0, mat=ACIER_SOMBRE, seg=8))
        m.add(box(s * 15.6 - 1.4, -34.0, 0.4, s * 15.6 + 1.4, -29.0, 2.6, jaune))
    # canons antiaériens légers (l'unique armement)
    for s in (1, -1):
        m.add(tube((s * 10.0, -21.0, 9.0), (s * 10.0, -21.0, 10.2), 1.0, mat=blind, seg=8))
        m.add(tube((s * 10.0, -21.0, 10.6), (s * 10.0, -17.0, 12.0), 0.2, mat=ACIER_SOMBRE, seg=4))
    return m


def porte_drone():
    """Porte-drones léger : pont d'envol continu, rails de catapulte, îlot déporté, hangar à drones."""
    m, poly = coque(58.0, 14.0, 3.2, proue=0.26, tonture=0.8)
    peau = R.team(spec=0.35, tex=combine(panels(3.5, 0.3, 0.86), noise(31, 0.05)))

    def pont(p, n):
        # marquages du pont d'envol : axe jaune, rails de catapulte, cases numérotées
        if abs(p[0]) < 0.25:
            return (220, 190, 60)
        if abs(abs(p[0]) - 3.0) < 0.2 and p[1] > 8:
            return (40, 40, 40)
        if abs(p[1] % 8.0) < 0.2 and abs(p[0]) < 5.5:
            return (230, 230, 220)
        return 0.85 if (p[0] / 2.0) % 1.0 < 0.08 else 1.0
    deck = Mat((78, 82, 84), spec=0.1, tex=pont)
    m.add(R.prism(R.inset(R.ccw(poly), 0.2), 3.2, 3.8, deck))
    # îlot tribord aux couleurs du joueur, mât à antennes
    m.add(R.prism(R.rrect(4.6, -8.0, 7.2, 4.0, 0.8), 3.8, 8.0, peau, bevel=0.4))
    m.add(box(4.8, 3.6, 7.0, 7.0, 4.1, 7.8, Mat((40, 64, 88), spec=1.1, shine=40)))
    m.add(tube((5.9, -2.0, 8.0), (5.9, -2.0, 12.0), 0.2, mat=ACIER_SOMBRE, seg=4))
    m.add(box(4.6, -2.3, 10.6, 7.2, -1.7, 10.9, ACIER_SOMBRE))
    m.add(sphere((5.9, -5.5, 8.8), 0.9, Mat((222, 224, 220), spec=0.4), seg=8, rings=5))
    # drones parqués en rangées sur le pont (ailes en X)
    import rendu3d as _R  # noqa: F401
    for k in range(6):
        y = -22.0 + k * 4.2
        for x in (-3.0, 0.8):
            d = moustique().scale(0.55).rot_z(0).move(dx=x, dy=y, dz=4.5)
            m.add(d)
    # rails de lancement à l'avant
    for s in (1, -1):
        m.add(box(s * 3.0 - 0.3, 10.0, 3.8, s * 3.0 + 0.3, 26.0, 4.2, ACIER))
    # CIWS de défense rapprochée
    m.add(tube((-4.5, 24.0, 3.8), (-4.5, 24.0, 5.2), 0.9, mat=Mat((210, 212, 210), spec=0.4), seg=8))
    return m


def baleine_icone():
    """Baleine avec ses deux rotors (pour l'icône ; en jeu les rotors sont des surcouches animées)."""
    m = baleine()
    pale = Mat((50, 52, 54), spec=0.2)
    for y, z in ((13.0, 5.4), (-14.5, 7.0)):
        for k in range(3):
            m.add(box(-0.35, -13.0, -0.08, 0.35, 13.0, 0.08, pale).rot_z(k * 60 + 15).move(dy=y, dz=z))
    return m
