"""Génère « Encyclopédie ultime du mod.txt » à la racine du dépôt.

Comme l'encyclopédie du mod ratc : les statistiques sont lues dans les règles du
jeu (regles.py, stats.py), le lore et les stratégies sont dans textes_*.py.
Seul le contenu propre aux pays fictifs est décrit (acteurs créés dans
mods/fictifs/<pays>/) ; le contenu de Red Alert reste celui du jeu de base.
Relancer ce script après un rééquilibrage met l'encyclopédie à jour.

Usage : python3 tools/encyclopedie/generer.py
"""
import os
import sys
import textwrap

sys.path.insert(0, os.path.dirname(__file__))
import regles  # noqa: E402
import stats  # noqa: E402
from textes_elielistan import TEXTES as T_ELI  # noqa: E402
from textes_rubenie import TEXTES as T_RUB  # noqa: E402
from textes_australouis import TEXTES as T_AUS  # noqa: E402
from textes_ananthanie import TEXTES as T_ANA  # noqa: E402
from textes_pouvoirs import TEXTES as T_POW  # noqa: E402

SORTIE = os.path.join(regles.ROOT, "Encyclopédie ultime du mod.txt")
LARGEUR = 92
TEXTES = {**T_ELI, **T_RUB, **T_AUS, **T_ANA}
ORDRE_PAYS = ["Elielistan", "Rubénie", "Australouis", "Ananthanie"]

SECTIONS = [
    ("BÂTIMENTS", lambda s: s["queue"] == "Building"),
    ("DÉFENSES", lambda s: s["queue"] == "Defense"),
    ("INFANTERIE", lambda s: s["queue"] == "Infantry"),
    ("VÉHICULES", lambda s: s["queue"] == "Vehicle"),
    ("AVIATION", lambda s: s["queue"] == "Aircraft"),
    ("MARINE", lambda s: s["queue"] == "Ship"),
    ("TRAINS", lambda s: s["queue"] == "Train"),
]

PAYS = {
"Elielistan": ("Frapper puis disparaître", """
Camp : Alliés (base alliée, usine et héliport propres).
Doctrine : « Tu ne combats pas l'armée elielistanaise. Tu combats l'endroit où elle était il y a dix secondes. »
L'Elielistan mise sur la vitesse, la reconnaissance et la projection : raids rapides, drones, frappes aériennes, puis retraite. Ses unités sont moins blindées que leurs équivalents étrangers, et il perd les combats qui durent.
""", [
"Décrochage : après avoir tiré, une unité elielistanaise gagne +35 % de vitesse pendant 4 secondes (+60 % pour l'artillerie Trueno et Lanzador).",
"Désignation laser : l'Explorador et l'Ojo del Aguila marquent leurs cibles (losange vert) ; une cible désignée subit +25 % de dégâts.",
"Mandat présidentiel : le Palacio dispose de 10 ordres prioritaires par quinquennat de 15 minutes.",
"Largage : un véhicule déposé par le Cóndor ou parachuté par le Pelícano reste sonné 4 secondes (+50 % de dégâts reçus, vitesse réduite, ne tire pas).",
"Transport : un fantassin occupe 1 place, un char (Carro Aguila, Aguila Nuclear) ou une pièce d'artillerie (Trueno, Lanzador) en occupe 3. Aguila Carrier : 5 véhicules ; Pelícano et porte-avions : 15 places (15 fantassins ou 5 véhicules).",
"Projection : jusqu'à 3 antennes de gouvernement provisoire pour construire loin de la base.",
]),
"Rubénie": ("Si ça tient, touche à rien", """
Camp : Soviétiques (base soviétique, QG, chantier naval et base aérienne propres).
Doctrine : défendre, contre-attaquer, se replier, réparer, recommencer.
La Rubénie est une forteresse : tranchées, bunkers, casemates, défense aérienne en couches et artillerie lourde. Ses unités sont plus solides, plus lentes et un peu plus chères que celles de l'Elielistan ; ses armes frappent moins fort par coup mais avec des cadences soutenues et de la portée.
""", [
"Logistique : le chantier de construction, le QG, les centres logistiques et le véhicule logistique ravitaillent les unités terrestres rubéniennes autour d'eux (efficacité normale, réparation lente hors combat).",
"Isolement : une unité hors de tout rayon de ravitaillement depuis 30 secondes est isolée : −25 % de dégâts, −25 % de cadence, plus de réparation.",
"Détection sismique : l'Éclaireur, le véhicule de reconnaissance et le QG repèrent le Tunnelier ennemi sous terre.",
"Aura du Second Galactique : +15 % de dégâts et −15 % de dégâts reçus pour les unités rubéniennes à 5 cases.",
]),
"Australouis": ("Le peuple des îles", """
Camp : Alliés (base alliée, base aéronavale propre).
Doctrine : tenir le ciel et la mer. L'Australouis est un archipel ; sa puissance tient à son aviation et à sa marine.
Il construit la base alliée, plus un chantier naval avancé et une centrale d'enrichissement. Sa caserne forme l'infanterie soviétique et le Révolutionnaire ; son usine produit les chars de base alliés et le Cheaper.
""", [
"Écopage : le Cormoran se remplit en survolant l'eau (2 secondes au total) ; un plein = un brouillard.",
"Brouillard : les ennemis perdent tout ce qu'ils avaient découvert dans un rayon de 6 cases, sauf ce que leurs unités voient encore.",
"Essaim : 3 drones Moustique suffisent à détruire un char léger ; 2 ne suffisent pas.",
"Îles : le Mothership fait sortir de l'océan des îles de 8×8 cases (plage autour, terrain constructible au centre) où un chantier de construction peut s'installer.",
"Révolution : le Révolutionnaire fait passer dans votre camp tout ce qui est ennemi à 5 cases (recharge 5 minutes).",
"Enrichissement : +5 % de revenus du minerai avec la centrale d'enrichissement, +20 % de plus pendant le Raffinerie boost.",
]),
"Ananthanie": ("Je contrôle le champ de bataille", """
Camp : Soviétiques (base soviétique, Haut Commandement, fabrique de drones, institut d'innovation, base aérienne et chantier naval propres).
Devise : « Prospérité, innovation, puissance, Fraternité. »
L'Ananthanie est une superpuissance technologique, démocratie parlementaire très stable. Sa doctrine : blitzkrieg en attaque, usure en défense, quantité ET qualité. Blindés modernes, drones explosifs et missiles : « L'Ananthanie gagne une bataille en 7 minutes. »
""", [
"Masse : le Thal est formé très vite ; l'armée ananthanienne compte sur le nombre autant que sur la technologie.",
"Drones : la fabrique de drones produit 15 % plus vite le Nayrak, le Mukhar et la Ruche.",
"Désignation : le Lunkar marque ses cibles (+25 % de dégâts reçus pendant 3 secondes).",
"Protection active : le Karnvasha reçoit 35 % de dégâts en moins des missiles et roquettes.",
"Faiblesse : les montagnes. Les véhicules ananthaniens perdent 20 % de vitesse en terrain accidenté.",
"Anti-drone : les avions ananthaniens détruisent les drones ennemis à 1 case.",
"Furtivité : le Lunkravyn est invisible tant qu'il ne tire pas.",
"Avion radar : le Nayra-Ambar donne +20 % de portée aux avions ananthaniens et alliés à 10 cases.",
]),
}


def ordre_pays(p):
    return ORDRE_PAYS.index(p) if p in ORDRE_PAYS else len(ORDRE_PAYS)


def para(texte, retrait="    "):
    return textwrap.fill(texte, LARGEUR, initial_indent=retrait, subsequent_indent=retrait)


def puce(texte):
    return textwrap.fill(texte, LARGEUR, initial_indent="  • ", subsequent_indent="    ")


def bloc_stats(s):
    lignes = []
    l1 = []
    if s["cost"]:
        l1.append(f"Coût {stats.fmt_int(s['cost'])} $")
    if s["hp"]:
        l1.append(f"PV {stats.fmt_int(s['hp'])}")
    if s["armor"]:
        l1.append("Blindage " + " + ".join(s["armor"]))
    if s["speed"]:
        l1.append(f"Vitesse {s['speed']}")
    if s["vision"]:
        l1.append(f"Vision {stats.fmt_cells(s['vision'])}")
    if l1:
        lignes.append(" · ".join(l1))
    for w in s["weapons"]:
        lignes.append("Arme : " + stats.weapon_line(w))
    if not s["weapons"] and s["queue"] in ("Infantry", "Vehicle", "Aircraft", "Ship"):
        lignes.append("Arme : aucune")
    l3 = []
    if s["power"]:
        p = int(s["power"])
        l3.append(f"Énergie {'+' if p > 0 else ''}{p}")
    if s["cargo"]:
        l3.append(f"Transport {s['cargo']} places")
    if s["cloak"]:
        l3.append("Furtif")
    if s["detect"]:
        l3.append(f"Détecte les furtifs ({stats.fmt_cells(s['detect'])})")
    if s["limit"]:
        l3.append(f"Limite {s['limit']} exemplaire{'s' if int(s['limit']) > 1 else ''}")
    if l3:
        lignes.append(" · ".join(l3))
    req = f"Prérequis : {s['prereq_text']}"
    if s["techlevel"]:
        req += f" · Niveau technologique : {s['techlevel']}"
    lignes.append(req)
    for p in s["powers"]:
        charge = f" (recharge {stats.fmt_ticks(p['charge'])})" if p["charge"] else ""
        nom = T_POW.get(p["name"], (p["name"],))[0]
        lignes.append(f"Pouvoir de soutien : {nom}{charge}")
    return lignes


def entree(s, manquants):
    nom, lore, strat = TEXTES.get(s["key"], (None, None, None))
    if nom is None:
        manquants.append(s["key"])
        nom = s["name"]
    titre = nom.upper()
    if s["name"] and s["name"].lower() != nom.lower():
        titre += f"  ({s['name']})"
    out = ["-" * LARGEUR, f"■ {titre}", f"  Pays : {s['faction']}"]
    if lore:
        out.append("  Lore :")
        out += ["    " + l.strip() for l in lore.strip().splitlines()]
    elif s["description"]:
        out.append("  Description :")
        out.append(para(s["description"]))
    out.append("  Statistiques :")
    for l in bloc_stats(s):
        out.append(para(l))
    if strat:
        out.append("  Stratégie :")
        out.append(para(strat))
    return out


def main():
    R = regles.load_rules()
    W = regles.load_weapons()
    pays = regles.pays_des_acteurs()
    tous = []
    for k in stats.buildable(R):
        if k in pays:
            s = stats.actor_stats(R, W, k)
            s["faction"] = pays[k]
            s["prereq_text"] = prereq_pays(s["prereq"], R)
            tous.append(s)
    pouvoirs_pays = regles.pouvoirs_des_pays(stats.POWER_TRAITS)
    manquants = []

    out = ["=" * LARGEUR,
           "ENCYCLOPÉDIE ULTIME DU MOD".center(LARGEUR),
           "Pays fictifs pour OpenRA Red Alert".center(LARGEUR),
           "=" * LARGEUR, "",
           para("Chaque bâtiment, défense, unité et pouvoir de soutien propre aux pays fictifs : un peu "
                "d'histoire, les statistiques et la façon de s'en servir. Les bâtiments et unités de Red Alert "
                "que ces pays partagent avec les autres nations (centrales, raffineries, casernes...) sont ceux "
                "du jeu de base.", ""),
           "",
           para("Les statistiques sont lues directement dans les règles du jeu par "
                "tools/encyclopedie/generer.py : après un rééquilibrage, relancez ce script pour mettre "
                "l'encyclopédie à jour.", ""),
           "",
           "Repères :",
           "  • PV : points de vie. Une case = une case de la carte.",
           "  • Les durées sont données à la vitesse de jeu normale (25 images par seconde).",
           "  • Blindage : aucun (infanterie), léger, lourd, béton, léger de bâtiment (bois).",
           ""]
    sommaire = [f"  0. LES PAYS ({len(PAYS)})"]
    corps = ["", "=" * LARGEUR, "0. LES PAYS".center(LARGEUR), "=" * LARGEUR]
    for nom, (devise, texte, regles_pays) in PAYS.items():
        corps += ["-" * LARGEUR, f"■ {nom.upper()} — « {devise} »"]
        corps += [para(l.strip(), "  ") for l in texte.strip().splitlines()]
        corps.append("  Règles propres :")
        corps += [puce(r) for r in regles_pays]

    for i, (titre, filtre) in enumerate(SECTIONS, 1):
        items = sorted((s for s in tous if filtre(s)),
                       key=lambda s: (ordre_pays(s["faction"]), TEXTES.get(s["key"], (s["name"],))[0].lower()))
        sommaire.append(f"  {i}. {titre} ({len(items)})")
        corps += ["", "=" * LARGEUR, f"{i}. {titre}".center(LARGEUR), "=" * LARGEUR]
        courant = None
        for s in items:
            if s["faction"] != courant:
                courant = s["faction"]
                corps += ["", f"  ▶ PAYS : {courant.upper()}", ""]
            corps += entree(s, manquants)

    # Pouvoirs de soutien propres aux pays, regroupés par nom
    pouvoirs = {}
    for s in [stats.actor_stats(R, W, k) for k in stats.buildable(R)]:
        for p in s["powers"]:
            if p["name"] in pouvoirs_pays:
                s["faction"] = pouvoirs_pays[p["name"]]
                pouvoirs.setdefault(p["name"], []).append((s, p))
    n = len(SECTIONS) + 1
    sommaire.append(f"  {n}. POUVOIRS DE SOUTIEN ({len(pouvoirs)})")
    corps += ["", "=" * LARGEUR, f"{n}. POUVOIRS DE SOUTIEN".center(LARGEUR), "=" * LARGEUR]
    courant = None
    for nom_regles, porteurs in sorted(pouvoirs.items(), key=lambda kv: (ordre_pays(pouvoirs_pays[kv[0]]),
                                                                        T_POW.get(kv[0], (kv[0],))[0].lower())):
        if pouvoirs_pays[nom_regles] != courant:
            courant = pouvoirs_pays[nom_regles]
            corps += ["", f"  ▶ PAYS : {courant.upper()}", ""]
        nom, lore, strat = T_POW.get(nom_regles, (None, None, None))
        if nom is None:
            manquants.append(f"pouvoir « {nom_regles} »")
            nom = nom_regles
        corps += ["-" * LARGEUR, f"■ {nom.upper()}"]
        if lore:
            corps.append("  Lore :")
            corps += ["    " + l.strip() for l in lore.strip().splitlines()]
        corps.append("  Statistiques :")
        vus = set()
        for s, p in porteurs:
            porteur = TEXTES.get(s["key"], (s["name"],))[0]
            if porteur in vus:
                continue
            vus.add(porteur)
            charge = f", recharge {stats.fmt_ticks(p['charge'])}" if p["charge"] else ""
            corps.append(para(f"Porté par : {porteur}{charge}"))
        desc = porteurs[0][1]["description"]
        if desc and not lore:
            corps.append(para(f"Effet : {desc}"))
        if strat:
            corps.append("  Stratégie :")
            corps.append(para(strat))

    out += ["SOMMAIRE"] + sommaire + corps + ["", "=" * LARGEUR, "Fin de l'encyclopédie.".center(LARGEUR), "=" * LARGEUR]
    with open(SORTIE, "w", encoding="utf-8") as f:
        f.write("\n".join(out) + "\n")
    print(f"{SORTIE} : {len(tous)} éléments constructibles + {len(pouvoirs)} pouvoirs")
    if manquants:
        print("SANS TEXTE :", ", ".join(manquants))
    inutiles = set(TEXTES) - {s["key"] for s in tous}
    if inutiles:
        print("Textes sans élément correspondant :", ", ".join(sorted(inutiles)))


def prereq_pays(prereq, R):
    """Prérequis lisibles : noms des bâtiments tels qu'affichés en jeu (y compris ceux des pays)."""
    noms = dict(stats.BUILDING_NAMES)
    for k, n in R.items():
        nom = stats.val(n, "Tooltip", "Name")
        if nom and not k.startswith("^"):
            noms.setdefault(k.lower(), TEXTES.get(k, (nom,))[0])
    # Prérequis « nom de bâtiment » fournis sous un autre nom
    noms.update({"aerodromo": "Aeródromo", "palacio": "Palacio Presidencial"})
    out = []
    for t in (prereq or "").split(","):
        t = t.strip()
        if not t or t.startswith("~techlevel") or t.startswith("~!") or \
                t.lstrip("~").split(".")[0] in ("vehicles", "structures", "infantry", "aircraft", "ships"):
            continue
        b = t.lstrip("~")
        out.append(noms.get(b, b))
    return ", ".join(out) if out else "aucun"


if __name__ == "__main__":
    main()
