"""Génère les sprites « HD » des pays fictifs à partir des modèles 3D de tools/modeles/.

Chaque entrée produit mods/fictifs/bits/<nom>.png (corps sur N orientations, puis la
tourelle s'il y en a une) et <nom>icon.png (icône de construction 64 × 48).

Usage : python3 tools/sprites_hd.py [nom ...]      (sans argument : tout)
        python3 tools/sprites_hd.py --apercu nom   (planche dans /tmp, sans rien écrire)
"""
import os
import sys
from multiprocessing import Pool

sys.path.insert(0, os.path.dirname(__file__))
import rendu3d as R  # noqa: E402
from modeles import australouis as A  # noqa: E402
from modeles import elielistan as E  # noqa: E402
from modeles import rubenie as U  # noqa: E402
from modeles import ananthanie as N  # noqa: E402

AIR = dict(shadow=False)
MER = dict(shadow=False, waterline=0.0)

# nom : (corps, tourelle, taille, facings, options de rendu, options d'icône)
UNITES = {
    "cheaper": (A.cheaper_caisse, A.cheaper_tourelle, 32, 32, {}, dict(zoom=2.9, dz=2)),
    "albatros": (A.albatros, None, 48, 32, AIR, dict(zoom=2.0, air=True, facing=150, dz=-6)),
    "manta": (A.manta, None, 72, 32, AIR, dict(zoom=1.1, air=True, facing=165, dz=-8)),
    "moustique": (A.moustique, None, 28, 32, AIR, dict(zoom=3.0, air=True, facing=150, dz=-6)),
    "cormoran": (A.cormoran, None, 48, 32, AIR, dict(zoom=1.3, air=True, facing=150, dz=-6)),
    "baleine": (A.baleine, None, 48, 32, AIR, dict(zoom=1.6, air=True, facing=150, dz=-5)),
    "l0u15": (A.l0u15_coque, A.l0u15_tourelle, 64, 32, MER, dict(zoom=1.5, water=True, facing=215, dz=2)),
    "sousmarin.icbm": (A.sousmarin_icbm, None, 64, 32, MER, dict(zoom=1.25, water=True, facing=215, dz=0)),
    "mothership": (A.mothership, None, 112, 32, MER, dict(zoom=0.72, water=True, facing=215, dz=4)),
    "portedrone": (A.porte_drone, None, 72, 32, MER, dict(zoom=1.1, water=True, facing=215, dz=2)),
    # Elielistan
    "gavilan": (E.gavilan_caisse, E.gavilan_tourelle, 32, 32, {}, dict(zoom=3.2, dz=2)),
    "halcon": (E.halcon_caisse, E.halcon_tourelle, 36, 32, {}, dict(zoom=2.6, dz=2)),
    "jaguar": (E.jaguar_caisse, E.jaguar_tourelle, 40, 32, {}, dict(zoom=2.4, dz=2)),
    "vigia": (E.vigia_caisse, E.vigia_tourelle, 36, 32, {}, dict(zoom=2.5, dz=2)),
    "trueno": (E.trueno, None, 40, 32, {}, dict(zoom=2.2, dz=3)),
    "carro.aguila": (E.carro_caisse, E.carro_tourelle, 44, 32, {}, dict(zoom=2.1, dz=2)),
    "lanzador": (E.lanzador, None, 40, 32, {}, dict(zoom=2.2, dz=3)),
    "lanzador.route": (E.lanzador_route, None, 40, 32, {}, None),
    "condor": (E.condor, None, 56, 32, AIR, dict(zoom=1.35, air=True, facing=150, dz=-5)),
    "pico": (E.pico, None, 28, 32, AIR, dict(zoom=3.0, air=True, facing=150, dz=-5)),
    "garra": (E.garra, None, 36, 32, AIR, dict(zoom=2.2, air=True, facing=150, dz=-5)),
    "porta.aguila": (E.porta_aguila, None, 116, 32, MER, dict(zoom=0.62, water=True, facing=215, dz=-6)),
    # Rubénie
    "sicatient": (U.sicatient_caisse, U.sicatient_tourelle, 48, 32, {}, dict(zoom=2.0, dz=2)),
    "contreattaque": (U.contreattaque_caisse, U.contreattaque_tourelle, 44, 32, {}, dict(zoom=2.2, dz=2)),
    "forteresse": (U.forteresse_caisse, U.forteresse_tourelle, 60, 32, {}, dict(zoom=1.6, dz=3)),
    "reco.rub": (U.reco_caisse, U.reco_tourelle, 32, 32, {}, dict(zoom=3.0, dz=2)),
    "aa.rub": (U.aa_caisse, U.aa_tourelle, 40, 32, {}, dict(zoom=2.4, dz=2)),
    "lancemissile": (U.lancemissile, None, 40, 32, {}, dict(zoom=2.1, dz=3)),
    "lancemissile.vide": (U.lancemissile_vide, None, 40, 32, {}, None),
    "canon.auto": (U.canon_auto, None, 48, 32, {}, dict(zoom=1.9, dz=3)),
    "artillerie.lourde": (U.artillerie_lourde, None, 56, 32, {}, dict(zoom=1.7, dz=4)),
    "lrm": (U.lrm, None, 40, 32, {}, dict(zoom=2.1, dz=3)),
    "vehicule.logistique": (U.vehicule_logistique, None, 40, 32, {}, dict(zoom=2.1, dz=3)),
    "bastion": (U.bastion_caisse, [U.bastion_tourelle, U.bastion_missiles], 64, 32, {}, dict(zoom=1.45, dz=4)),
    "tunnelier": (U.tunnelier, None, 48, 32, {}, dict(zoom=1.9, dz=3)),
    "tunnelier.avance": (U.tunnelier_avance, None, 48, 32, {}, dict(zoom=1.8, dz=3)),
    "chasseur.poly": (U.chasseur_poly, None, 48, 32, AIR, dict(zoom=1.8, air=True, facing=150, dz=-6)),
    "intercepteur": (U.intercepteur, None, 52, 32, AIR, dict(zoom=1.7, air=True, facing=150, dz=-6)),
    "avion.attaque": (U.avion_attaque, None, 48, 32, AIR, dict(zoom=1.75, air=True, facing=150, dz=-6)),
    "avion.reco": (U.avion_reco, None, 56, 32, AIR, dict(zoom=1.45, air=True, facing=150, dz=-6)),
    "escorteur.rub": (U.escorteur, U.escorteur_tourelle, 64, 32, MER, dict(zoom=1.45, water=True, facing=215, dz=2)),
    "fregate": (U.fregate, [U.fregate_tourelle, U.fregate_tourelle], 80, 32, MER, dict(zoom=1.1, water=True, facing=215, dz=2)),
    "sous.marin": (U.sous_marin, None, 56, 32, MER, dict(zoom=1.4, water=True, facing=215, dz=0)),
    "porte.avions.rub": (U.porte_avions, None, 112, 32, MER, dict(zoom=0.72, water=True, facing=215, dz=4)),
    # Ananthanie
    "vidra": (N.vidra_caisse, N.vidra_tourelle, 36, 32, {}, dict(zoom=2.9, dz=2)),
    "lunkra": (N.lunkra_caisse, N.lunkra_tourelle, 48, 32, {}, dict(zoom=2.1, dz=2)),
    "karnvasha": (N.karnvasha_caisse, N.karnvasha_tourelle, 56, 32, {}, dict(zoom=1.75, dz=3)),
    "ratha": (N.ratha_caisse, N.ratha_tourelle, 44, 32, {}, dict(zoom=2.3, dz=2)),
    "mukhar": (N.mukhar, None, 40, 32, {}, dict(zoom=2.1, dz=3)),
    "agnar": (N.agnar, None, 40, 32, {}, dict(zoom=2.0, dz=3)),
    "ambarkesh": (N.ambarkesh_caisse, N.ambarkesh_tourelle, 40, 32, {}, dict(zoom=2.4, dz=2)),
    "nayrath": (N.nayrath, None, 44, 32, {}, dict(zoom=2.0, dz=4)),
    "rodeuse": (N.rodeuse, None, 24, 32, AIR, dict(zoom=3.4, air=True, facing=150, dz=-4)),
}

# Position du pivot de tourelle dans le modèle de la caisse (px, voir Turreted.Offset en yaml).
MONTAGE = {
    "l0u15": (0.0, 10.5, 2.4),
    "gavilan": (0.0, 0.0, 5.4),
    "halcon": (0.0, -1.0, 3.8),
    "jaguar": (0.0, -0.5, 4.0),
    "vigia": (0.0, -7.7, 7.0),
    "carro.aguila": (0.0, -0.5, 4.4),
    "sicatient": (0.0, -0.5, 4.6),
    "contreattaque": (0.0, -0.5, 3.8),
    "forteresse": (0.0, -1.0, 5.4),
    "reco.rub": (0.0, -1.0, 4.0),
    "aa.rub": (0.0, -7.7, 7.0),
    # Bastion : deux tourelles de 130 mm latérales (sprite « turret », pivots miroirs) puis le lanceur
    "bastion": [(7.0, 5.15, 0.0), (0.0, -7.7, 0.0)],
    "escorteur.rub": (0.0, 12.1, 2.6),
    "fregate": [(0.0, -23.1, 3.0), (0.0, 19.8, 3.0)],
    "vidra": (0.0, -0.5, 3.5),
    "lunkra": (0.0, -1.0, 4.1),
    "karnvasha": (0.0, -1.5, 5.0),
    "ratha": (0.0, 1.0, 5.2),
    "ambarkesh": (0.0, -1.0, 4.0),
}

# Ombres et icônes : tourelles supplémentaires qui partagent un sprite (Bastion : tourelle bâbord).
EN_PLUS = {
    "bastion": [(U.bastion_tourelle, (-7.0, 5.15, 0.0))],
}


def _tourelles(nom):
    """Liste de (fonction de tourelle, position du pivot dans le modèle de la caisse)."""
    t = UNITES[nom][1]
    if t is None:
        return []
    fns = t if isinstance(t, (list, tuple)) else [t]
    pos = MONTAGE.get(nom, (0.0, 0.0, 0.0))
    pos = pos if isinstance(pos[0], (list, tuple)) else [pos] * len(fns)
    return list(zip(fns, pos))


def _frame(args):
    nom, partie, facing = args
    corps, _, taille, n, opts, _ = UNITES[nom]
    if partie == "corps":
        ombre = None
        if opts.get("shadow", True):
            tt = _tourelles(nom) + EN_PLUS.get(nom, [])
            ombre = R.merge(*[f().move(*p) for f, p in tt]) if tt else None
        return R.render(corps(), taille, facing=facing, shadow_mesh=ombre, **opts)
    # tourelle k : rendue seule autour de son pivot, sans ombre
    fn = _tourelles(nom)[int(partie)][0]
    o = dict(opts)
    o["shadow"] = False
    return R.render(fn(), taille, facing=facing, **o)


ICONE = {"baleine": A.baleine_icone, "condor": E.condor_icone}

# Unités agrandies à la demande (2026-09-30) : modèle, pivots de tourelles et image mis à l'échelle.
# Les décalages yaml (tourelles, bouches à feu, rotors) ont été multipliés d'autant.
ECHELLE = {n: 1.8 for n in ("lanzador", "lanzador.route", "gavilan", "jaguar", "condor", "pico", "garra", "trueno", "vigia",
                            "baleine", "avion.attaque", "lancemissile", "lancemissile.vide", "halcon", "reco.rub", "lrm")}
# un peu trop grands à 1,8 : réduits de 20 % (2026-09-30)
for _n in ("jaguar", "trueno", "lancemissile", "lancemissile.vide", "halcon", "gavilan"):
    ECHELLE[_n] = 1.44
ECHELLE["cormoran"] = 1.5      # au moins la taille d'un bombardier
ECHELLE["cheaper"] = 1.2       # silhouette d'un char moyen de Red Alert (2026-09-30)
# Ananthanie : mêmes proportions que les unités agrandies des autres pays
ECHELLE["vidra"] = 1.2
for _n in ("mukhar", "agnar", "nayrath"):
    ECHELLE[_n] = 1.3


def _echelle(fn, k):
    return lambda: fn().scale(k)


for _n, _k in ECHELLE.items():
    _c, _t, _sz, _nf, _o, _io = UNITES[_n]
    if _t is not None:
        _t = [_echelle(f, _k) for f in _t] if isinstance(_t, (list, tuple)) else _echelle(_t, _k)
    _sz = int(round(_sz * _k / 2.0)) * 2
    if _io is not None:
        _io = dict(_io, zoom=_io["zoom"] / _k)
    UNITES[_n] = (_echelle(_c, _k), _t, _sz, _nf, _o, _io)
    if _n in MONTAGE:
        _p = MONTAGE[_n]
        MONTAGE[_n] = [tuple(v * _k for v in q) for q in _p] if isinstance(_p[0], (list, tuple)) else tuple(v * _k for v in _p)
    if _n in ICONE:
        ICONE[_n] = _echelle(ICONE[_n], _k)


def _icone(nom):
    corps, tourelle, taille, n, opts, iopts = UNITES[nom]
    m = ICONE.get(nom, corps)()
    for f, p in _tourelles(nom) + EN_PLUS.get(nom, []):
        m = R.merge(m, f().move(*p))
    return R.icon(m, **iopts)


def generer(noms, pool):
    for nom in noms:
        corps, tourelle, taille, n, opts, _ = UNITES[nom]
        jobs = [(nom, "corps", f * 360.0 / n) for f in range(n)]
        for k in range(len(_tourelles(nom))):
            jobs += [(nom, str(k), f * 360.0 / n) for f in range(n)]
        frames = pool.map(_frame, jobs)
        R.save(frames, nom)
        if UNITES[nom][5] is not None:
            R.save([_icone(nom)], nom + "icon")
        print(f"{nom} : {len(frames)} images de {taille} px + icône")


def orbite_ojo():
    R.save(E.ojo_orbite(), "ojo.orbite")
    print("ojo.orbite : 32 images")


def apercu(nom, dossier="/tmp"):
    corps, tourelle, taille, n, opts, _ = UNITES[nom]
    fr = [_frame((nom, "corps", f * 360.0 / n)) for f in range(0, n, n // 8)]
    for k in range(len(_tourelles(nom))):
        fr += [_frame((nom, str(k), f * 360.0 / n)) for f in range(0, n, n // 8)]
    R.preview(fr, os.path.join(dossier, nom + "-apercu.png"), scale=3)
    R.preview([_icone(nom)], os.path.join(dossier, nom + "-icone.png"), scale=4, cols=1)



# ---------------------------------------------------------------------------
# Infanterie : feuilles complètes (343 images) au format commun, voir modeles/infanterie.py
# ---------------------------------------------------------------------------
def generer_infanterie(noms, pool):
    from modeles import infanterie as I
    kits = I.kits()
    for nom in noms:
        frames = I.feuille(nom, pool)
        R.save(frames, nom)
        R.save([I.icone(kits[nom])], nom + "icon")
        print(f"{nom} : {len(frames)} images + icône")



# ---------------------------------------------------------------------------
# Bâtiments et défenses (projection oblique, voir modeles/batiments.py)
# ---------------------------------------------------------------------------
def _bat_jobs():
    from modeles import batiments as B
    MAKE = 12
    j = {}

    def simple(nom, fn, cel, marge, n_idle=1, eau=False, feux=None, make=MAKE):
        idle = [(fn, dict(t=None), dict(phase=k / n_idle)) for k in range(n_idle)]
        dmg = [(fn, dict(degats=True, feux=feux), dict(phase=k / n_idle)) for k in range(n_idle)]
        mk = [(fn, dict(t=(k + 1) / make), dict(phase=0.0)) for k in range(make)]
        j[nom] = dict(cel=cel, marge=marge, eau=eau, frames=idle + dmg + mk, n_idle=n_idle, make=make, fn=fn)

    simple("qg.rub", B.qg_rub, (3, 3), (0, 24), 8, feux=[(10, 5, 15, 2.0), (-20, 10, 12, 1.6)])
    simple("centre.logistique", lambda phase=0.0: B.centre_logistique(), (2, 3), (0, 10), 1, feux=[(-10, 10, 8, 1.8)])
    simple("chantier.avance", B.chantier_avance, (3, 3), (0, 20), 8, eau=True, feux=[(-20, 27, 14, 1.8)])
    simple("centrale.enrichissement", B.centrale_enrichissement, (3, 3), (0, 20), 4, feux=[(-10, 20, 14, 2.0)])
    simple("raffinerie.petrole", B.raffinerie_petrole, (2, 2), (0, 22), 4, feux=[(-8, 8, 10, 1.8)], make=8)
    simple("zone.industrielle", B.zone_industrielle, (3, 2), (0, 24), 4)
    # base aéronavale : idle (radar) puis active (idem, feux de piste plus rapides)
    simple("base.aeronavale", B.base_aeronavale, (3, 2), (0, 16), 8, feux=[(26, 14, 22, 1.8)])
    # chantier de construction rubénien : idle, damaged-idle, build (25), damaged-build (25), make (16), dead
    fr = [(B.fact_rub, dict(t=None), dict(phase=0.0)), (B.fact_rub, dict(degats=True, feux=[(-10, 10, 16, 2.2)]), dict(phase=0.0))]
    fr += [(B.fact_rub, dict(t=None), dict(phase=k / 24)) for k in range(25)]
    fr += [(B.fact_rub, dict(degats=True, feux=[(-10, 10, 16, 2.2)]), dict(phase=k / 24)) for k in range(25)]
    fr += [(B.fact_rub, dict(t=(k + 1) / 16), dict(phase=0.0)) for k in range(16)]
    fr += [(B.fact_rub, dict(degats=True, seed=9, feux=[(-10, 10, 16, 3.0), (10, 0, 8, 2.4), (-20, 20, 10, 2.0)]), dict(phase=0.3))]
    j["fact.rub"] = dict(cel=(3, 3), marge=(0, 30), eau=False, frames=fr)
    # tranchées : idle, damaged-idle, make (4)
    for nom, ns, cel, marge in (("tranchee", False, (2, 1), (0, 6)), ("trancheev", True, (1, 2), (6, 4))):
        f = (lambda phase=0.0, ns=ns: B.tranchee(ns))
        j[nom] = dict(cel=cel, marge=marge, eau=False,
                      frames=[(f, dict(t=None), {}), (f, dict(degats=True), {})] + [(f, dict(t=(k + 1) / 4), {}) for k in range(4)])
    # bunker
    j["bunker.rub"] = dict(cel=(1, 1), marge=(6, 8), eau=False,
                           frames=[(lambda phase=0.0: B.bunker(), dict(t=None), {}), (lambda phase=0.0: B.bunker(), dict(degats=True, feux=[(3, 2, 7, 1.4)]), {})]
                           + [(lambda phase=0.0: B.bunker(), dict(t=(k + 1) / MAKE), {}) for k in range(MAKE)])
    # défenses à tourelle : turret, recoil, damaged-turret, damaged-recoil (32 chacune), make (12)
    for nom, socle, tour, marge in (("casemate.at", B.socle_casemate, B.casemate_tourelle, (8, 10)),
                                    ("tourelle.aa", B.socle_aa, B.aa_tourelle, (8, 12)),
                                    ("cotiere", B.socle_cotiere, B.cotiere_tourelle, (10, 12))):
        fr = []
        for dmg in (False, True):
            for recul in (0.0, 1.6):
                for k in range(32):
                    fr.append((socle, dict(degats=dmg, facing=k * 360 / 32, pose=tour(recul)), {}))
        fr += [(socle, dict(t=(k + 1) / MAKE, pose=tour(0.0), facing=200), {}) for k in range(MAKE)]
        j[nom] = dict(cel=(1, 1), marge=marge, eau=False, frames=fr)
    fr = []
    for dmg in (False, True):
        for k in range(32):
            fr.append((B.socle_sam, dict(degats=dmg, facing=k * 360 / 32, pose=B.sam_tourelle()), {}))
    fr += [(B.socle_sam, dict(t=(k + 1) / MAKE, pose=B.sam_tourelle(), facing=200), {}) for k in range(MAKE)]
    j["sam.rub"] = dict(cel=(2, 1), marge=(4, 12), eau=False, frames=fr)
    # Ananthanie
    simple("haut.commandement", N.haut_commandement, (3, 3), (0, 26), 8, feux=[(12, 10, 14, 2.0), (-20, 12, 10, 1.6)])
    simple("fabrique.drones", N.fabrique_drones, (3, 3), (0, 30), 8, feux=[(-10, 16, 14, 2.0), (26, 22, 16, 1.6)])
    simple("institut", N.institut, (2, 3), (0, 20), 4, feux=[(-8, 8, 14, 1.8)])
    for nom, socle, tour, marge in (("kheshkarn", N.socle_kheshkarn, N.kheshkarn_tourelle, (8, 12)),
                                    ("ruche", N.socle_ruche, N.ruche_tourelle, (8, 14))):
        fr = []
        for dmg in (False, True):
            for recul in (0.0, 1.6):
                for k in range(32):
                    fr.append((socle, dict(degats=dmg, facing=k * 360 / 32, pose=tour(recul)), {}))
        fr += [(socle, dict(t=(k + 1) / MAKE, pose=tour(0.0), facing=200), {}) for k in range(MAKE)]
        j[nom] = dict(cel=(1, 1), marge=marge, eau=False, frames=fr)
    fr = []
    for dmg in (False, True):
        for k in range(32):
            fr.append((N.socle_ambar, dict(degats=dmg, facing=k * 360 / 32, pose=N.ambar_tourelle()), {}))
    fr += [(N.socle_ambar, dict(t=(k + 1) / MAKE, pose=N.ambar_tourelle(), facing=200), {}) for k in range(MAKE)]
    j["batterie.ambar"] = dict(cel=(2, 1), marge=(4, 12), eau=False, frames=fr)
    # pipeline : 16 raccordements, intact puis endommagé
    j["pipeline"] = dict(cel=(1, 1), marge=(0, 4), eau=False,
                         frames=[((lambda phase=0.0, m=m, d=d: B.pipeline(m, d)), {}, {}) for d in (False, True) for m in range(16)])
    return j


def _bat_frame(args):
    nom, k = args
    J = _bat_jobs()[nom]
    fn, ro, fo = J["frames"][k]
    mesh = fn(*fo.values()) if fo else fn()
    ro = dict(ro)
    return R.batiment(mesh, J["cel"], marge=J["marge"], eau=J["eau"], **ro)


BAT_ICONES = {}


def _bat_icone(nom):
    from modeles import batiments as B
    J = _bat_jobs()[nom]
    fn, ro, fo = J["frames"][0]
    m = fn(*fo.values()) if fo else fn()
    if ro.get("pose") is not None:
        m = R.merge(m, ro["pose"].rot_z(200))
    zoom = {1: 1.7, 2: 1.05, 3: 0.72}[max(J["cel"])]
    return R.icon(m, zoom=zoom, facing=200, dz=6, water=J["eau"])


def generer_batiments(noms, pool):
    for nom in noms:
        n = len(_bat_jobs()[nom]["frames"])
        frames = pool.map(_bat_frame, [(nom, k) for k in range(n)])
        R.save(frames, nom)
        if nom not in ("pipeline", "zone.industrielle"):
            R.save([_bat_icone(nom)], nom + "icon")
        print(f"{nom} : {n} images de {frames[0].size[0]}×{frames[0].size[1]}")


if __name__ == "__main__":
    args = sys.argv[1:]
    if args and args[0] == "--apercu":
        for nom in args[1:]:
            apercu(nom, os.environ.get("APERCU", "/tmp"))
        sys.exit(0)
    from modeles import infanterie as I
    # l'infanterie garde ses sprites d'origine (demande du 2026-09-30) : modeles/infanterie.py n'est
    # généré que si on le demande explicitement par nom.
    noms = args or list(UNITES) + ["ojo.orbite"] + list(_bat_jobs())
    with Pool() as pool:
        generer([n for n in noms if n in UNITES], pool)
        generer_infanterie([n for n in noms if n in I.kits()], pool)
        generer_batiments([n for n in noms if n in _bat_jobs()], pool)
    if "ojo.orbite" in noms:
        orbite_ojo()
