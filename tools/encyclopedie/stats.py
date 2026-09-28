"""Extraction des statistiques de chaque élément constructible, à partir des règles résolues."""
import re
import regles

ARMOR = {"None": "aucun", "Wood": "léger de bâtiment (bois)", "Light": "léger", "Heavy": "lourd",
         "Concrete": "béton", "ERA": "réactif (ERA)", "APS": "protection active (APS)"}

# Qui peut construire quoi : prérequis « ~<file>.<nation> » ou centre technique national.
NATIONS = {
    "england": "Royaume-Uni", "france": "France", "germany": "Allemagne", "usa": "USA", "spain": "Espagne",
    "japan": "Japon", "russia": "Russie", "ukraine": "Ukraine", "china": "Chine", "turkey": "Turquie",
    "greece": "Grèce", "india": "Inde", "allies": "tous les Alliés", "soviet": "tous les Soviétiques",
}
TECH = {"ustek": "usa", "gtek": "germany", "etek": "england", "ftek": "france", "sptek": "spain",
        "ctek": "china", "rtek": "russia", "utek": "ukraine", "ttek": "turkey", "grtek": "greece"}
BUILDING_NAMES = {
    "fact": "Chantier de construction", "powr": "Centrale", "apwr": "Centrale avancée", "proc": "Raffinerie",
    "weap": "Usine d'armement", "dome": "Radar Dome", "fix": "Service Depot", "atek": "Centre technique allié",
    "stek": "Centre technique soviétique", "techcenter": "un centre technique", "tent": "Caserne alliée",
    "barr": "Caserne soviétique", "barracks": "une caserne", "kenn": "Chenil", "hpad": "Héliport",
    "afld": "Aérodrome", "syrd": "Chantier naval", "spen": "Base sous-marine", "tsla": "Bobine Tesla",
    "ftur": "Tour lance-flammes", "pris": "Tour prisme", "abase": "Base aérienne", "shpad": "Héliport soviétique",
    "hosp": "Hôpital (capturé)", "bio": "Laboratoire biologique (capturé)", "anypower": "une centrale",
    "genie-deploye": "un Ingénieur militaire déployé à proximité",
}


def cells(v):
    """'5c512' -> 5.5 ; '8c0' -> 8"""
    m = re.match(r"^(-?\d+)c(\d+)$", v.strip())
    if m:
        return int(m.group(1)) + int(m.group(2)) / 1024
    try:
        return int(v) / 1024
    except ValueError:
        return None


def fmt_cells(v):
    c = cells(v)
    if c is None:
        return v
    s = f"{c:.2f}".rstrip("0").rstrip(".").replace(".", ",")
    return f"{s} case" + ("s" if c >= 2 else "")


def fmt_int(v):
    try:
        return f"{int(v):,}".replace(",", " ")
    except ValueError:
        return v


def fmt_ticks(t):
    """Ticks de jeu (25 par seconde à vitesse normale) -> durée lisible."""
    s = int(t) / 25
    if s >= 60:
        m, r = divmod(round(s), 60)
        return f"{m} min {r:02d} s" if r else f"{m} min"
    return f"{s:.1f} s".replace(".0 s", " s").replace(".", ",")


def val(node, *path):
    for p in path:
        if node is None:
            return None
        node = node.get(p)
    return node.value if node is not None else None


def faction(prereq, key):
    toks = [t.strip() for t in (prereq or "").split(",")]
    for t in toks:
        m = re.match(r"^~(vehicles|structures|infantry|aircraft|ships)\.(\w+)$", t)
        if m and m.group(2) in NATIONS:
            return NATIONS[m.group(2)]
    for t in toks:
        b = t.lstrip("~")
        if b in TECH:
            return NATIONS[TECH[b]]
    if "~!structures.france" in toks:
        return "toutes sauf la France (qui a sa propre version)"
    if "~!structures.ukraine" in toks:
        return "tous les Soviétiques sauf l'Ukraine (qui a sa propre version)"
    if "~!infantry.england" in toks:
        return "toutes sauf le Royaume-Uni (qui a sa propre version)"
    if "~!grtek" in toks:
        return "Grèce (avant son centre technique)"
    if "~sov.tech" in toks:
        return "Soviétiques"
    # Bâtiment de production propre à un camp ou à une nation
    bases = [t.lstrip("~") for t in toks]
    if "abase" in bases:
        return NATIONS["usa"]
    if "shpad" in bases:
        return NATIONS["turkey"]
    if any(b in ("tent", "hpad", "syrd", "atek", "pris") for b in bases):
        return NATIONS["allies"]
    if any(b in ("barr", "kenn", "afld", "spen", "stek", "ftur", "tsla") for b in bases):
        return NATIONS["soviet"]
    return "toutes"


def prereq_text(prereq):
    out = []
    for t in (prereq or "").split(","):
        t = t.strip()
        if not t or t.startswith("~techlevel") or t.startswith("~!") or re.match(r"^~(vehicles|structures|infantry|aircraft|ships)\.", t):
            continue
        b = t.lstrip("~")
        if b in TECH:
            out.append(f"Centre technique ({NATIONS[TECH[b]]})")
        elif b in BUILDING_NAMES:
            out.append(BUILDING_NAMES[b])
        elif b not in ("sov.tech",):
            out.append(b)
    return ", ".join(out) if out else "aucun"


def techlevel(prereq):
    m = re.search(r"~techlevel\.(\w+)", prereq or "")
    return {"infonly": "infanterie seule", "low": "bas", "medium": "moyen", "high": "élevé",
            "unrestricted": "illimité (superarmes)", "naval": "naval"}.get(m.group(1), m.group(1)) if m else None


def weapon_stats(W, name):
    w = W.get(name.lower())
    if w is None:
        return None
    dmg = 0
    for c in w.children:
        if c.key.startswith("Warhead") and c.value in ("SpreadDamage", "TargetDamage", "HealthPercentageDamage"):
            d = c.get("Damage")
            if d is not None:
                try:
                    dmg = max(dmg, int(d.value))
                except ValueError:
                    pass
            if c.value == "HealthPercentageDamage":
                sp = c.get("Damage")
    cluster = next((c for c in w.children if c.key.startswith("Warhead") and c.value == "FireCluster"), None)
    info = {
        "name": name,
        "damage": dmg,
        "range": val(w, "Range"),
        "reload": val(w, "ReloadDelay"),
        "burst": val(w, "Burst"),
        "heal": dmg < 0,
    }
    if cluster is not None:
        sub = weapon_stats(W, cluster.get("Weapon").value)
        info["cluster"] = (val(cluster, "RandomClusterCount"), sub)
    return info


def weapon_line(ws):
    parts = []
    if ws.get("cluster"):
        n, sub = ws["cluster"]
        return f"{ws['name']} : salve de {n} projectiles de {fmt_int(sub['damage'])} dégâts chacun"
    if ws["damage"] >= 1000000:
        parts.append("détruit la cible d'un seul coup")
    elif ws["damage"]:
        parts.append(("soigne " if ws["heal"] else "") + f"{fmt_int(abs(ws['damage']))} " + ("PV" if ws["heal"] else "dégâts"))
    if ws["burst"] and ws["burst"] not in ("1",):
        parts.append(f"rafale de {ws['burst']}")
    if ws["range"]:
        parts.append(f"portée {fmt_cells(ws['range'])}")
    if ws["reload"]:
        parts.append(f"recharge {fmt_ticks(ws['reload'])}")
    return f"{ws['name']} : " + (", ".join(parts) if parts else "sans dégâts directs")


def actor_stats(R, W, key):
    n = R[key]
    s = {"key": key}
    s["name"] = val(n, "Tooltip", "Name") or key
    b = n.get("Buildable")
    s["queue"] = val(b, "Queue")
    s["prereq"] = val(b, "Prerequisites") or ""
    s["faction"] = faction(s["prereq"], key)
    s["prereq_text"] = prereq_text(s["prereq"])
    s["techlevel"] = techlevel(s["prereq"])
    s["limit"] = val(b, "BuildLimit")
    s["description"] = (val(b, "Description") or "").replace("\\n", " ").replace("  ", " ")
    s["cost"] = val(n, "Valued", "Cost")
    s["hp"] = val(n, "Health", "HP")
    armors = [c for c in n.children if c.key == "Armor" or c.key.startswith("Armor@")]
    s["armor"] = [ARMOR.get(val(a, "Type"), val(a, "Type")) for a in armors if val(a, "Type")]
    s["speed"] = val(n, "Mobile", "Speed") or val(n, "Aircraft", "Speed")
    s["vision"] = val(n, "RevealsShroud", "Range")
    s["power"] = val(n, "Power", "Amount")
    s["cargo"] = val(n, "Cargo", "MaxWeight")
    s["cloak"] = n.get("Cloak") is not None
    s["detect"] = val(n, "DetectCloaked", "Range")
    weapons, seen = [], set()
    for c in n.children:
        if c.key == "Armament" or c.key.startswith("Armament@"):
            w = val(c, "Weapon")
            if w and w.lower() not in seen:
                seen.add(w.lower())
                ws = weapon_stats(W, w)
                if ws:
                    # Arme utilisée seulement depuis une tranchée ou un bunker.
                    if val(c, "Name") == "garrisoned":
                        ws = dict(ws, name=ws["name"] + " (en garnison)")
                    weapons.append(ws)
    s["weapons"] = weapons
    s["powers"] = support_powers(n)
    return s


POWER_TRAITS = ("NukePower", "GrantExternalConditionPower", "ChronoshiftPower", "GpsPower", "AirstrikePower",
                "ParatroopersPower", "SpawnActorPower", "IonCannonPower", "ProduceActorPower", "AttackOrderPower",
                # Pouvoirs propres aux pays fictifs (OpenRA.Mods.Fictifs)
                "PriorityOrderPower", "AguilaBridgePower", "AguilaGatePower", "AirPatrolPower",
                "GrantConditionToOwnedActorsPower")


def support_powers(n):
    out = []
    for c in n.children:
        t = c.key.split("@")[0]
        if t not in POWER_TRAITS:
            continue
        name = val(c, "Name")
        if not name or "Timer" in name or "Temporary" in name:
            continue
        out.append({"trait": c.key, "name": name, "charge": val(c, "ChargeInterval"),
                    "description": (val(c, "Description") or "").replace("\\n", " "),
                    "oneshot": val(c, "OneShot")})
    return out


def buildable(R):
    keys = []
    for k, n in R.items():
        if k.startswith("^"):
            continue
        b = n.get("Buildable")
        if b is None or b.get("Queue") is None:
            continue
        if "~disabled" in (val(b, "Prerequisites") or ""):
            continue
        keys.append(k)
    return keys
