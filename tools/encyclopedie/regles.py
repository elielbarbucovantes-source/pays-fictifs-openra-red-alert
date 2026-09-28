"""Lecture des règles du mod telles que le jeu les charge : fichiers listés dans
mods/fictifs/mod.yaml (Rules, Weapons), fusionnés dans l'ordre, héritages
(Inherits, Inherits@...) et suppressions (-Trait:) résolus comme le fait le moteur.
"""
import os
import re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PACKAGES = {
    "ra": os.path.join(ROOT, "engine", "mods", "ra"),
    "common": os.path.join(ROOT, "engine", "mods", "common"),
    "fictifs": os.path.join(ROOT, "mods", "fictifs"),
}


class Node:
    __slots__ = ("key", "value", "children")

    def __init__(self, key, value="", children=None):
        self.key, self.value, self.children = key, value, children or []

    def get(self, key):
        for c in self.children:
            if c.key == key:
                return c
        return None

    def copy(self):
        return Node(self.key, self.value, [c.copy() for c in self.children])


def strip_comment(line):
    out, i = [], 0
    while i < len(line):
        ch = line[i]
        if ch == "\\" and i + 1 < len(line) and line[i + 1] == "#":
            out.append("#")
            i += 2
            continue
        if ch == "#":
            break
        out.append(ch)
        i += 1
    return "".join(out).rstrip()


def parse(path):
    roots, stack = [], []
    for raw in open(path, encoding="utf-8", errors="replace"):
        raw = raw.rstrip("\n").rstrip("\r")
        line = strip_comment(raw)
        if not line.strip():
            continue
        depth = len(line) - len(line.lstrip("\t"))
        text = line.strip()
        key, sep, value = text.partition(":")
        node = Node(key.strip(), value.strip() if sep else "")
        while stack and stack[-1][0] >= depth:
            stack.pop()
        (stack[-1][1].children if stack else roots).append(node)
        stack.append((depth, node))
    return roots


def merge_into(acc, nodes):
    """Fusion de fichiers : même clé -> valeur remplacée si non vide, enfants fusionnés."""
    index = {n.key: n for n in acc}
    for n in nodes:
        if n.key in index:
            if n.value:
                index[n.key].value = n.value
            merge_into(index[n.key].children, n.children)
        else:
            c = n.copy()
            acc.append(c)
            index[c.key] = c
    return acc


def overlay(acc, nodes):
    """Application d'un nœud sur ses parents résolus (gère -Clé)."""
    for n in nodes:
        if n.key.startswith("-"):
            acc[:] = [a for a in acc if a.key != n.key[1:]]
            continue
        existing = next((a for a in acc if a.key == n.key), None)
        if existing is None:
            acc.append(n.copy())
        else:
            if n.value:
                existing.value = n.value
            overlay(existing.children, n.children)
    return acc


def resolve_all(tree):
    by_key = {n.key: n for n in tree}
    cache = {}

    def resolve(key, seen=()):
        if key in cache:
            return cache[key]
        node = by_key.get(key)
        if node is None:
            return []
        acc = []
        own = []
        for c in node.children:
            if c.key == "Inherits" or c.key.startswith("Inherits@"):
                if c.value not in seen:
                    overlay(acc, [x.copy() for x in resolve(c.value, seen + (key,))])
            else:
                own.append(c)
        overlay(acc, own)
        cache[key] = acc
        return acc

    return {k: Node(k, "", resolve(k)) for k in by_key}


def mod_files(section):
    files, cur = [], None
    for line in open(os.path.join(PACKAGES["fictifs"], "mod.yaml"), encoding="utf-8"):
        line = line.rstrip("\n")
        if re.match(r"^[^\s#]", line):
            cur = line.split(":")[0].strip()
            continue
        if cur == section and line.startswith("\t") and "|" in line:
            pkg, rel = line.strip().split("|", 1)
            files.append(os.path.join(PACKAGES[pkg], rel))
    return files


def load(section):
    tree = []
    for f in mod_files(section):
        merge_into(tree, parse(f))
    return resolve_all(tree)


def load_rules():
    return load("Rules")


def load_weapons():
    return {k.lower(): v for k, v in load("Weapons").items()}


# Pays fictifs : dossier de mods/fictifs/ -> nom du pays.
PAYS = {"elielistan": "Elielistan", "rubenie": "Rubénie", "australouis": "Australouis"}


def pays_des_acteurs():
    """Acteur -> pays, pour les acteurs créés (et pas seulement modifiés) dans le dossier d'un pays."""
    origine = {}
    for f in mod_files("Rules"):
        dossier = os.path.basename(os.path.dirname(f))
        for n in parse(f):
            origine.setdefault(n.key, PAYS.get(dossier))
    return {k: v for k, v in origine.items() if v}


def pouvoirs_des_pays(traits):
    """Nom de pouvoir -> pays, pour les pouvoirs déclarés dans le dossier d'un pays."""
    out = {}
    for f in mod_files("Rules"):
        pays = PAYS.get(os.path.basename(os.path.dirname(f)))
        if not pays:
            continue
        for n in parse(f):
            for c in n.children:
                if c.key.split("@")[0] in traits:
                    nom = c.get("Name")
                    if nom is not None:
                        out[nom.value] = pays
    return out
