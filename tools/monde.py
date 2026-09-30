"""Prépare les régions du monde réel pour le générateur de cartes.

Pour chaque région, écrit mods/<mod>/mapgen/monde/<id>.png : une image
indexée 8 bits où
  0        = eau (mer, grands lacs)
  1 à 254  = terre, altitude = (valeur - 1) * 25 m  (254 = 6 325 m et plus)
Les ponts de « Monde immense (reliés) » sont décrits à part, dans
<id>.ponts : une ligne « x0 y0 x1 y1 » (pixels de l'image) par pont.

Projections : Mercator (comme les cartes routières) pour les régions, image
carrée ; Miller pour les cartes « Monde immense » (moins de déformation près
des pôles), image rectangulaire, sans l'Antarctique.

Sources (téléchargées dans tools/monde-cache/, ignoré par git) :
  - côtes et lacs : Natural Earth 1:10m (domaine public) ;
  - relief : tuiles Terrarium d'AWS « Terrain Tiles » (SRTM, GMTED, ETOPO1…).

Usage : python3 tools/monde.py [id ...]
"""
import json
import math
import os
import sys
import urllib.request
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
CACHE = os.path.join(ROOT, "tools", "monde-cache")
# Dossier du mod (mods/<id>/, celui qui a un générateur de cartes).
MOD = next(m for m in sorted(os.listdir(os.path.join(ROOT, "mods")))
           if os.path.isdir(os.path.join(ROOT, "mods", m, "mapgen")))
OUT = os.path.join(ROOT, "mods", MOD, "mapgen", "monde")
NE = "https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/"
TILES = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png"

METRES_PAR_PALIER = 25

# Régions : id -> (nom, lon min, lat min, lon max, lat max, côté de l'image).
# Les régions continentales ont une image plus fine (cartes « Gigantesque »).
REGIONS = {
    "monde": ("Monde entier", -180, -58, 180, 75, 1024),
    "europe": ("Europe", -12, 34, 42, 62, 1024),
    "mediterranee": ("Méditerranée", -7, 29, 37, 47, 1024),
    "france": ("France", -5.5, 41.2, 10, 51.4, 512),
    "royaume-uni": ("Îles Britanniques", -11, 49.6, 3, 59.2, 512),
    "iberie": ("Espagne et Portugal", -10, 35.6, 4.5, 44, 512),
    "italie": ("Italie", 6, 36.4, 19, 47.2, 512),
    "balkans": ("Balkans", 13, 36, 30, 47, 512),
    "scandinavie": ("Scandinavie", 4, 54.5, 32, 71.5, 512),
    "mer-noire": ("Mer Noire et Ukraine", 26, 40, 42, 53, 512),
    "moyen-orient": ("Moyen-Orient", 25, 12, 63, 42, 1024),
    "golfe": ("Golfe Persique", 46, 22, 60, 32, 512),
    "afrique-nord": ("Afrique du Nord", -18, 18, 36, 38, 1024),
    "afrique": ("Afrique", -20, -36, 52, 38, 1024),
    "amerique-nord": ("Amérique du Nord", -130, 15, -60, 55, 1024),
    "caraibes": ("Caraïbes", -90, 9, -59, 27, 512),
    "amerique-sud": ("Amérique du Sud", -82, -56, -34, 13, 1024),
    "inde": ("Inde", 67, 5, 92, 36, 1024),
    "asie-est": ("Chine, Corée et Japon", 100, 18, 146, 46, 1024),
    "japon": ("Japon", 128, 30, 146, 46, 512),
    "asie-sud-est": ("Asie du Sud-Est", 92, -11, 128, 22, 1024),
    "australie": ("Australie", 112, -44, 155, -10, 1024),
    "nouvelle-zelande": ("Nouvelle-Zélande", 165, -47.5, 179, -34, 512),
    "islande": ("Islande", -25, 63, -13, 67, 512),
}

# Cartes « Monde immense » : projection de Miller, 512 cases de large en jeu.
MONDE_LARGEUR_CASES = 512
MONDE_PX_PAR_CASE = 4
MONDE_LAT = (-58, 80)
CANAL_CASES = 6
# id : (nom, longitude du bord gauche). La carte ne boucle pas : la version
# reliée est coupée au milieu de l'Atlantique pour garder le détroit de Béring.
MONDES = {
    "monde-realiste": ("Monde immense (réaliste)", -180),
    "monde-separe": ("Monde immense (continents séparés)", -180),
    "monde-ponts": ("Monde immense (continents reliés par des ponts)", -25),
}

# Isthmes trop fins pour l'échelle du monde, élargis pour rester de la terre :
# (largeur en cases, tracé).
ISTHMES = [
    (5, [(-97.5, 17.5), (-92.0, 15.5), (-88.0, 14.5), (-85.5, 12.5), (-84.0, 10.5),
     (-82.5, 9.0), (-80.0, 8.7), (-78.5, 8.8), (-77.0, 8.0)]),         # Amérique centrale
    (7, [(99.0, 10.0), (100.5, 6.5), (102.0, 4.0), (103.5, 1.8)]),      # Péninsule malaise (pont vers Bornéo)
]

# Frontières entre continents, creusées en canaux (lon, lat).
CANAUX = [
    [(-79.9, 10.2), (-79.9, 6.8)],                                       # Panama
    [(32.4, 31.8), (32.5, 29.8), (33.5, 28.0), (35.5, 26.0), (38.0, 22.0),
     (40.5, 17.5), (42.3, 14.5), (43.4, 12.6), (44.8, 11.6)],            # Suez, mer Rouge, Bab-el-Mandeb
    [(-9.0, 35.95), (-2.5, 35.95)],                                      # Gibraltar
    [(26.0, 39.9), (27.5, 40.6), (29.1, 41.3), (29.6, 41.6)],            # Dardanelles, Marmara, Bosphore
    [(38.6, 47.2), (43.0, 46.1), (47.2, 45.2)],                          # Kouma-Manytch (Caucase)
    [(51.6, 46.8), (51.4, 51.3), (58.3, 53.3), (59.2, 58.0),
     (60.0, 63.0), (63.8, 67.4), (67.0, 69.8)],                          # Oural
    [(-169.0, 67.6), (-169.0, 64.4)],                                    # Béring
]

# Ponts (version reliée) : d'une terre ferme à l'autre, horizontaux ou
# verticaux ; le générateur pose le tablier sur toute l'eau entre les deux.
PONTS = [
    ((-84.0, 8.7), (-76.0, 8.7)),        # Panama
    ((29.5, 30.3), (35.5, 30.3)),        # Suez
    ((37.5, 15.3), (44.5, 15.3)),        # Érythrée - Yémen
    ((-5.5, 38.5), (-5.5, 33.5)),        # Gibraltar
    ((26.5, 41.3), (32.0, 41.3)),        # Bosphore
    ((43.0, 49.5), (43.0, 43.5)),        # Caucase
    ((53.0, 54.0), (65.0, 54.0)),        # Oural sud
    ((55.0, 62.0), (67.0, 62.0)),        # Oural nord
    ((-176.0, 65.8), (-162.0, 65.8)),    # Béring
    ((102.0, 1.8), (112.0, 1.8)),        # Malaisie - Bornéo
    ((115.0, -2.5), (136.0, -2.5)),      # Bornéo - Nouvelle-Guinée
    ((142.5, -6.0), (142.5, -13.0)),     # Nouvelle-Guinée - Australie
]


def cached(name, url):
    os.makedirs(CACHE, exist_ok=True)
    path = os.path.join(CACHE, name)
    if not os.path.exists(path):
        print("  téléchargement", url)
        with urllib.request.urlopen(url, timeout=60) as r, open(path, "wb") as f:
            f.write(r.read())
    return path


def merc_y(lat):
    """Latitude -> y de Mercator normalisé (0 en haut à 85,05° N, 1 en bas)."""
    lat = max(-85.05, min(85.05, lat))
    s = math.sin(math.radians(lat))
    return 0.5 - math.log((1 + s) / (1 - s)) / (4 * math.pi)


def merc_lat(y):
    return math.degrees(math.atan(math.sinh(math.pi * (1 - 2 * y))))


def miller_y(lat):
    return 1.25 * math.log(math.tan(math.pi / 4 + 0.4 * math.radians(lat)))


def miller_lat(y):
    return math.degrees(2.5 * math.atan(math.exp(0.8 * y)) - 0.625 * math.pi)


class Mercator:
    """Carré de Mercator centré sur la région."""

    def __init__(self, bbox, size):
        lon0, lat0, lon1, lat1 = bbox
        x0, x1 = (lon0 + 180) / 360, (lon1 + 180) / 360
        y0, y1 = merc_y(lat1), merc_y(lat0)
        side = min(1.0, max(x1 - x0, y1 - y0))
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        self.x0 = cx - side / 2
        self.y0 = min(max(cy - side / 2, 0.0), 1.0 - side)
        self.side = side
        self.w = self.h = size

    def to_px(self, lon, lat):
        return ((lon + 180) / 360 - self.x0) / self.side * self.w, (merc_y(lat) - self.y0) / self.side * self.h

    def to_lonlat(self, px, py):
        return (self.x0 + px / self.w * self.side) * 360 - 180, merc_lat(self.y0 + py / self.h * self.side)


class Miller:
    """Monde entier en projection de Miller, entre deux latitudes, bord gauche à lon0."""

    def __init__(self, lat_min, lat_max, width, lon0=-180):
        self.ytop, self.ybot = miller_y(lat_max), miller_y(lat_min)
        self.w = width
        self.h = round(width * (self.ytop - self.ybot) / (2 * math.pi))
        self.lon0 = lon0

    def to_px(self, lon, lat):
        return (lon - self.lon0) / 360 * self.w, (self.ytop - miller_y(lat)) / (self.ytop - self.ybot) * self.h

    def to_px_wrapped(self, lon, lat):
        x, y = self.to_px(lon, lat)
        return x % self.w, y

    def to_lonlat(self, px, py):
        return self.lon0 + px / self.w * 360, miller_lat(self.ytop - py / self.h * (self.ytop - self.ybot))


def polygons(geojson):
    for feat in json.load(open(geojson))["features"]:
        g = feat["geometry"]
        if g["type"] == "Polygon":
            yield g["coordinates"]
        elif g["type"] == "MultiPolygon":
            yield from g["coordinates"]


def land_mask(proj, land, lakes, ss=3):
    im = Image.new("L", (proj.w * ss, proj.h * ss), 0)
    d = ImageDraw.Draw(im)

    def draw(rings, fill_outer, fill_hole):
        for k, ring in enumerate(rings):
            base = [proj.to_px(lon, lat) for lon, lat in ring]
            for wrap in (-1, 0, 1):          # régions à cheval sur l'antiméridien
                dx = wrap * proj.to_px(180, 0)[0] - wrap * proj.to_px(-180, 0)[0]
                pts = [((x + dx) * ss, y * ss) for x, y in base]
                xs = [p[0] for p in pts]
                ys = [p[1] for p in pts]
                if max(xs) < 0 or min(xs) > proj.w * ss or max(ys) < 0 or min(ys) > proj.h * ss:
                    continue
                if len(pts) >= 3:
                    d.polygon(pts, fill=fill_outer if k == 0 else fill_hole)

    for rings in land:
        draw(rings, 255, 0)
    for rings in lakes:
        draw(rings, 0, 255)
    return im, d


def elevation(proj):
    """Altitude (m) de chaque pixel, d'après les tuiles Terrarium (Mercator)."""
    # Étendue en Mercator et niveau de zoom assez fin pour l'image.
    corners = [proj.to_lonlat(x, y) for x in (0, proj.w / 2, proj.w) for y in (0, proj.h / 2, proj.h)]
    mx = [(lon + 180) / 360 for lon, _ in corners]
    my = [merc_y(lat) for _, lat in corners]
    x0, x1, y0, y1 = min(mx), max(mx), min(my), max(my)
    z = max(0, min(10, math.ceil(math.log2(proj.w / (x1 - x0) / 256))))
    n = 2 ** z
    tx0, tx1 = math.floor(x0 * n), math.floor(x1 * n - 1e-9)
    ty0, ty1 = max(0, math.floor(y0 * n)), min(n - 1, math.floor(y1 * n - 1e-9))
    mosaic = Image.new("RGB", ((tx1 - tx0 + 1) * 256, (ty1 - ty0 + 1) * 256))
    for tx in range(tx0, tx1 + 1):
        for ty in range(ty0, ty1 + 1):
            wx = tx % n
            p = cached(f"terrarium-{z}-{wx}-{ty}.png", TILES.format(z=z, x=wx, y=ty))
            mosaic.paste(Image.open(p).convert("RGB"), ((tx - tx0) * 256, (ty - ty0) * 256))
    px = mosaic.load()
    mw, mh = mosaic.size
    out = []
    for j in range(proj.h):
        row = []
        for i in range(proj.w):
            acc = 0
            for si, sj in ((0.25, 0.25), (0.75, 0.25), (0.25, 0.75), (0.75, 0.75)):
                lon, lat = proj.to_lonlat(i + si, j + sj)
                gx = (((lon + 180) / 360) * n - tx0) * 256
                gy = (merc_y(lat) * n - ty0) * 256
                r, g, b = px[max(0, min(mw - 1, int(gx))), max(0, min(mh - 1, int(gy)))]
                acc += r * 256 + g + b / 256 - 32768
            row.append(acc / 4)
        out.append(row)
    return out


def save(rid, proj, mask, elev):
    im = Image.new("P", (proj.w, proj.h), 0)
    im.putpalette(sum(([v, v, v] for v in range(256)), []))
    px = im.load()
    for j in range(proj.h):
        for i in range(proj.w):
            if mask[i, j] >= 128:
                px[i, j] = 1 + max(0, min(253, int(elev[j][i] // METRES_PAR_PALIER)))
    os.makedirs(OUT, exist_ok=True)
    im.save(os.path.join(OUT, rid + ".png"))


def build_region(rid, land, lakes):
    name, *bbox, size = REGIONS[rid]
    print(f"{rid} ({name})")
    proj = Mercator(bbox, size)
    im, _ = land_mask(proj, land, lakes)
    save(rid, proj, im.resize((proj.w, proj.h), Image.BOX).load(), elevation(proj))


def build_worlds(ids, land, lakes):
    ss = 3
    cell = MONDE_PX_PAR_CASE * ss
    elevations = {}
    for rid in ids:
        name, lon0 = MONDES[rid]
        proj = Miller(MONDE_LAT[0], MONDE_LAT[1], MONDE_LARGEUR_CASES * MONDE_PX_PAR_CASE, lon0)
        print(f"{rid} ({name}) : {proj.w} x {proj.h} px, {MONDE_LARGEUR_CASES} x {proj.h // MONDE_PX_PAR_CASE} cases")
        if lon0 not in elevations:
            elevations[lon0] = elevation(proj)
        im, d = land_mask(proj, land, lakes, ss)

        def line(points, fill, width):
            pts = [tuple(c * ss for c in proj.to_px_wrapped(lon, lat)) for lon, lat in points]
            d.line(pts, fill=fill, width=width * cell, joint="curve")
            r = width / 2 * cell
            for x, y in pts:
                d.ellipse([x - r, y - r, x + r, y + r], fill=fill)

        # Isthmes élargis (les deux Amériques restent reliées par la terre).
        for width, points in ISTHMES:
            line(points, 255, width)

        # Canaux entre les continents : assez larges pour que le générateur les garde.
        if rid != "monde-realiste":
            for points in CANAUX:
                line(points, 0, CANAL_CASES)

        save(rid, proj, im.resize((proj.w, proj.h), Image.BOX).load(), elevations[lon0])

        path = os.path.join(OUT, rid + ".ponts")
        if rid == "monde-ponts":
            with open(path, "w") as f:
                f.write("# x0 y0 x1 y1 (pixels de " + rid + ".png), un pont par ligne\n")
                for (lon_a, lat_a), (lon_b, lat_b) in PONTS:
                    (x0, y0), (x1, y1) = proj.to_px_wrapped(lon_a, lat_a), proj.to_px_wrapped(lon_b, lat_b)
                    if abs(x1 - x0) >= abs(y1 - y0):
                        y1 = y0
                    else:
                        x1 = x0
                    f.write(f"{x0:.1f} {y0:.1f} {x1:.1f} {y1:.1f}\n")
        elif os.path.exists(path):
            os.remove(path)


if __name__ == "__main__":
    ids = sys.argv[1:] or list(REGIONS) + list(MONDES)
    land = list(polygons(cached("ne_10m_land.geojson", NE + "ne_10m_land.geojson")))
    lakes = list(polygons(cached("ne_10m_lakes.geojson", NE + "ne_10m_lakes.geojson")))
    for rid in ids:
        if rid in REGIONS:
            build_region(rid, land, lakes)
    worlds = [rid for rid in ids if rid in MONDES]
    if worlds:
        build_worlds(worlds, land, lakes)
