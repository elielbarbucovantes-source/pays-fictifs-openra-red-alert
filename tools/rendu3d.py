"""Petit moteur de rendu 3D pour les sprites des pays fictifs (Python + PIL seulement).

Les unités sont modélisées avec des primitives (prismes biseautés, lofts, tubes,
sphères), puis rendues comme les sprites d'origine de Red Alert :
  - vue orthographique de trois quarts (caméra au sud, inclinée de PITCH degrés) ;
  - lumière venant d'en haut à gauche, ombre portée en bas à droite (index 4 de la
    palette, rendu translucide par le jeu) ;
  - surfaces « équipe » converties sur la rampe de couleurs du joueur (index 80-95) ;
  - suréchantillonnage, liseré sombre sur la silhouette, léger tramage.

Repère du modèle : 1 unité = 1 pixel du jeu (une case = 24), x vers la droite (est),
y vers l'avant (nord pour l'image 0), z vers le haut. Les images suivent l'ordre des
sprites du mod : l'image f est le modèle tourné de f × 360/N degrés dans le sens
antihoraire.
"""
import math
import os
from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), "..")
BITS = os.path.join(ROOT, "mods", "fictifs", "bits")
PALETTE = Image.open(os.path.join(BITS, "luna.png")).getpalette()[:768]
ALLOWED = [i for i in range(16, 240) if not (80 <= i <= 103)]
TEAM = list(range(80, 96))
SHADOW = 4

PITCH = 55.0                       # inclinaison de la caméra (90 = vue de dessus)
OBLIQUE = False                    # bâtiments : sol non raccourci (aligné sur la grille), hauteur vers le haut
KZ = 0.9                           # projection oblique : pixels d'écran par unité de hauteur
LIGHT = (-0.50, -0.12, 0.86)       # direction vers la lumière : haut-gauche, légèrement côté caméra
OMBRE = (-0.55, 0.45, 0.80)        # direction qui projette les ombres (vers le bas à droite de l'écran)
SS = 4                             # suréchantillonnage


# ---------------------------------------------------------------------------
# Vecteurs
# ---------------------------------------------------------------------------
def add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def mul(a, k): return (a[0] * k, a[1] * k, a[2] * k)
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def norm(a):
    n = math.sqrt(dot(a, a)) or 1.0
    return (a[0] / n, a[1] / n, a[2] / n)


L = norm(LIGHT)
LO = norm(OMBRE)


# ---------------------------------------------------------------------------
# Matériaux
# ---------------------------------------------------------------------------
class Mat:
    """Couleur de base, part « équipe », brillance, texture procédurale.

    tex(p, n) reçoit le point du modèle (avant rotation) et la normale, et renvoie un
    facteur multiplicatif (1 = inchangé) ou un triplet de couleur qui remplace la base.
    """

    def __init__(self, color, team=False, spec=0.25, shine=18, tex=None, emit=False, flat=False):
        self.color = color
        self.team = team
        self.spec = spec
        self.shine = shine
        self.tex = tex
        self.emit = emit           # lumières, écrans : pas d'ombrage
        self.flat = flat           # ombrage atténué (bâches, tissus)


# Teinte de base des surfaces équipe : milieu de la rampe du joueur.
TEAM_BASE = (190, 172, 96)


def team(spec=0.3, tex=None, shine=18):
    return Mat(TEAM_BASE, team=True, spec=spec, tex=tex, shine=shine)


# Textures procédurales courantes -------------------------------------------------
def panels(step=6.0, width=0.5, dark=0.78, axis="xy"):
    """Lignes de panneaux (tôles) tous les step pixels."""
    def f(p, n):
        k = 1.0
        for i, c in enumerate(p):
            if "xyz"[i] not in axis or abs(n[i]) > 0.7:
                continue
            if (c / step) % 1.0 < width / step:
                k = dark
        return k
    return f


def stripes(step=2.0, dark=0.7, axis=1):
    """Rayures (chenilles, grilles) le long d'un axe."""
    def f(p, n):
        return dark if (p[axis] / step) % 1.0 < 0.5 else 1.0
    return f


def noise(seed=1, amount=0.10, scale=3.0):
    """Taches légères (usure, camouflage discret)."""
    def f(p, n):
        x, y, z = p[0] / scale, p[1] / scale, p[2] / scale
        v = math.sin(x * 12.9898 + y * 78.233 + z * 37.719 + seed) * 43758.5453
        return 1.0 - amount + 2 * amount * (v - math.floor(v))
    return f


def combine(*fs):
    def f(p, n):
        k = 1.0
        for g in fs:
            r = g(p, n)
            if isinstance(r, tuple):
                return r
            k *= r
        return k
    return f


# ---------------------------------------------------------------------------
# Maillages
# ---------------------------------------------------------------------------
class Mesh:
    def __init__(self, tris=None):
        self.tris = tris or []     # (a, b, c, mat), sens antihoraire vu de l'extérieur

    def add(self, other):
        self.tris.extend(other.tris)
        return self

    def tri(self, a, b, c, mat):
        self.tris.append((a, b, c, mat))

    def quad(self, a, b, c, d, mat):
        self.tris.append((a, b, c, mat))
        self.tris.append((a, c, d, mat))

    def map(self, f):
        return Mesh([(f(a), f(b), f(c), m) for a, b, c, m in self.tris])

    def move(self, dx=0.0, dy=0.0, dz=0.0):
        return self.map(lambda p: (p[0] + dx, p[1] + dy, p[2] + dz))

    def scale(self, sx, sy=None, sz=None):
        sy = sx if sy is None else sy
        sz = sx if sz is None else sz
        return self.map(lambda p: (p[0] * sx, p[1] * sy, p[2] * sz))

    def rot_z(self, deg, cx=0.0, cy=0.0):
        a = math.radians(deg)
        c, s = math.cos(a), math.sin(a)
        return self.map(lambda p: (cx + (p[0] - cx) * c - (p[1] - cy) * s, cy + (p[0] - cx) * s + (p[1] - cy) * c, p[2]))

    def rot_x(self, deg, cy=0.0, cz=0.0):
        """Tangage : deg > 0 lève l'avant (+y)."""
        a = math.radians(deg)
        c, s = math.cos(a), math.sin(a)
        return self.map(lambda p: (p[0], cy + (p[1] - cy) * c - (p[2] - cz) * s, cz + (p[1] - cy) * s + (p[2] - cz) * c))

    def rot_y(self, deg, cx=0.0, cz=0.0):
        """Roulis : deg > 0 lève le côté gauche."""
        a = math.radians(deg)
        c, s = math.cos(a), math.sin(a)
        return self.map(lambda p: (cx + (p[0] - cx) * c + (p[2] - cz) * s, p[1], cz - (p[0] - cx) * s + (p[2] - cz) * c))

    def mirror_x(self):
        return Mesh([((-c[0], c[1], c[2]), (-b[0], b[1], b[2]), (-a[0], a[1], a[2]), m) for a, b, c, m in self.tris])

    def sym(self):
        """Ajoute le symétrique gauche/droite."""
        return Mesh(self.tris + self.mirror_x().tris)


def merge(*meshes):
    m = Mesh()
    for x in meshes:
        m.add(x)
    return m


def polygon_area(poly):
    return sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1] for i in range(len(poly))) / 2


def ccw(poly):
    return poly if polygon_area(poly) > 0 else list(reversed(poly))


def loft(rings, mat, cap0=True, cap1=True, mats=None):
    """Relie des anneaux de points 3D de même taille (sens antihoraire vu du côté +)."""
    m = Mesh()
    for k in range(len(rings) - 1):
        r0, r1 = rings[k], rings[k + 1]
        mk = mats[k] if mats else mat
        n = len(r0)
        for i in range(n):
            j = (i + 1) % n
            m.quad(r0[i], r0[j], r1[j], r1[i], mk)
    if cap0:
        r = rings[0]
        for i in range(1, len(r) - 1):
            m.tri(r[0], r[i + 1], r[i], mat if not mats else mats[0])
    if cap1:
        r = rings[-1]
        for i in range(1, len(r) - 1):
            m.tri(r[0], r[i], r[i + 1], mat if not mats else mats[-1])
    return m


def prism(poly, z0, z1, mat, bevel=0.0, top=None, bottom_bevel=0.0):
    """Extrusion verticale d'un polygone (x, y), avec chanfrein en haut (et en bas).

    top : matériau du dessus (par défaut mat)."""
    poly = ccw(poly)
    rings = []
    if bottom_bevel:
        rings.append([(x, y, z0) for x, y in inset(poly, bottom_bevel)])
        rings.append([(x, y, z0 + bottom_bevel) for x, y in poly])
    else:
        rings.append([(x, y, z0) for x, y in poly])
    if bevel:
        rings.append([(x, y, z1 - bevel) for x, y in poly])
        rings.append([(x, y, z1) for x, y in inset(poly, bevel)])
    else:
        rings.append([(x, y, z1) for x, y in poly])
    m = loft(rings, mat, cap1=False)
    r = rings[-1]
    for i in range(1, len(r) - 1):
        m.tri(r[0], r[i], r[i + 1], top or mat)
    return m


def inset(poly, d):
    """Rétrécit un polygone convexe (ou presque) de d."""
    n = len(poly)
    out = []
    for i in range(n):
        p0, p1, p2 = poly[i - 1], poly[i], poly[(i + 1) % n]
        e0 = norm((p1[0] - p0[0], p1[1] - p0[1], 0))
        e1 = norm((p2[0] - p1[0], p2[1] - p1[1], 0))
        n0 = (-e0[1], e0[0])
        n1 = (-e1[1], e1[0])
        b = (n0[0] + n1[0], n0[1] + n1[1])
        bl = math.hypot(*b) or 1
        b = (b[0] / bl, b[1] / bl)
        cosh = b[0] * n0[0] + b[1] * n0[1]
        k = d / max(cosh, 0.3)
        out.append((p1[0] + b[0] * k, p1[1] + b[1] * k))
    return out


def rect(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def rrect(x0, y0, x1, y1, c):
    """Rectangle aux coins coupés."""
    return [(x0 + c, y0), (x1 - c, y0), (x1, y0 + c), (x1, y1 - c), (x1 - c, y1), (x0 + c, y1), (x0, y1 - c), (x0, y0 + c)]


def ngon(cx, cy, r, n, start=0.0, sx=1.0, sy=1.0):
    return [(cx + r * sx * math.cos(start + 2 * math.pi * i / n), cy + r * sy * math.sin(start + 2 * math.pi * i / n)) for i in range(n)]


def box(x0, y0, z0, x1, y1, z1, mat, bevel=0.0, top=None):
    return prism(rect(x0, y0, x1, y1), z0, z1, mat, bevel, top)


def tube(p0, p1, r0, r1=None, mat=None, seg=10, caps=True, sy=1.0):
    """Cylindre / cône de p0 à p1 (rayons r0, r1)."""
    r1 = r0 if r1 is None else r1
    axis = norm(sub(p1, p0))
    ref = (0, 0, 1) if abs(axis[2]) < 0.9 else (1, 0, 0)
    u = norm(cross(axis, ref))
    v = cross(u, axis)
    rings = []
    for p, r in ((p0, r0), (p1, r1)):
        rings.append([add(p, add(mul(u, r * math.cos(2 * math.pi * i / seg)), mul(v, r * sy * math.sin(2 * math.pi * i / seg)))) for i in range(seg)])
    rings = [list(reversed(x)) for x in rings]
    return loft(rings, mat, caps, caps)


def lathe(axis_pts, mat, seg=12, sx=1.0, sz=1.0, mats=None, caps=True):
    """Corps de révolution le long de y : axis_pts = [(y, rayon, z_centre)]."""
    rings = []
    for y, r, zc in axis_pts:
        r = max(r, 0.01)
        rings.append([(r * sx * math.cos(2 * math.pi * i / seg), y, zc + r * sz * math.sin(2 * math.pi * i / seg)) for i in range(seg)])
    return loft(rings, mat, caps, caps, mats)


def sphere(c, r, mat, seg=10, rings=6, sz=1.0):
    m = Mesh()
    pts = []
    for j in range(rings + 1):
        t = math.pi * j / rings
        pts.append([(c[0] + r * math.sin(t) * math.cos(2 * math.pi * i / seg), c[1] + r * math.sin(t) * math.sin(2 * math.pi * i / seg), c[2] + r * sz * math.cos(t)) for i in range(seg)])
    for j in range(rings):
        for i in range(seg):
            k = (i + 1) % seg
            m.quad(pts[j][i], pts[j + 1][i], pts[j + 1][k], pts[j][k], mat)
    return m


def plate(poly, z, mat, thick=0.6):
    """Plaque mince horizontale (ailes, ponts)."""
    return prism(poly, z - thick / 2, z + thick / 2, mat)


def wing(root_le, root_te, tip_le, tip_te, z, mat, thick=0.8, dihedral=0.0, tip_z=None):
    """Aile droite (x > 0) définie par les points (x, y) bord d'attaque/fuite."""
    tz = z + (tip_z if tip_z is not None else dihedral)
    top = [(root_le[0], root_le[1], z + thick / 2), (tip_le[0], tip_le[1], tz + thick / 4),
           (tip_te[0], tip_te[1], tz + thick / 4), (root_te[0], root_te[1], z + thick / 2)]
    bot = [(p[0], p[1], p[2] - (thick if i in (0, 3) else thick / 2)) for i, p in enumerate(top)]
    m = Mesh()
    m.quad(top[0], top[3], top[2], top[1], mat)
    m.quad(bot[0], bot[1], bot[2], bot[3], mat)
    for i in range(4):
        j = (i + 1) % 4
        m.quad(top[i], top[j], bot[j], bot[i], mat)
    return m


# ---------------------------------------------------------------------------
# Rendu
# ---------------------------------------------------------------------------
_near = {}


def nearest(rgb, pool=ALLOWED):
    key = (rgb, pool is TEAM)
    if key not in _near:
        _near[key] = min(pool, key=lambda i: (PALETTE[3 * i] - rgb[0]) ** 2 * 3 + (PALETTE[3 * i + 1] - rgb[1]) ** 2 * 4 + (PALETTE[3 * i + 2] - rgb[2]) ** 2 * 2)
    return _near[key]


def _view():
    if OBLIQUE:
        return norm((0.0, KZ, -1.0)), (0.0, 1.0, KZ)
    p = math.radians(PITCH)
    fwd = (0.0, math.cos(p), -math.sin(p))       # direction de visée
    up = (0.0, math.sin(p), math.cos(p))         # haut de l'écran
    return fwd, up


def shade(mat, n, p):
    fwd, _ = _view()
    base = mat.color
    k = 1.0
    if mat.tex:
        t = mat.tex(p, n)
        if isinstance(t, tuple):
            base = t
        else:
            k = t
    if mat.emit:
        return tuple(min(255, int(c * k)) for c in base)
    diff = max(0.0, dot(n, L))
    if mat.flat:
        diff = 0.5 + 0.5 * diff
    amb = 0.30 + 0.16 * max(0.0, n[2])          # ciel plus clair au-dessus
    h = norm(sub(L, fwd))
    spec = mat.spec * max(0.0, dot(n, h)) ** mat.shine
    rim = 1.0 - 0.18 * (1.0 - abs(dot(n, fwd)))  # bords rasants un peu assombris
    out = []
    for c in base:
        v = c * k * (amb + 0.92 * diff) * rim + 255 * spec
        out.append(max(0, min(255, int(v))))
    return tuple(out)


def render(mesh, size, facing=0.0, center=None, shadow=True, outline=0.72, dither=0.35,
           lift=0.0, frame_h=None, shadow_mesh=None, rgba=False, scale=1.0, waterline=None, pitch=None,
           oblique=None, clip_z=None):
    """Rend le maillage tourné de facing degrés (antihoraire). Renvoie une image « P »
    de taille (w, h) aux couleurs de la palette (0 = transparent, 4 = ombre).

    center : position écran (px) du point (0, 0, 0) ; par défaut le centre de l'image.
    lift   : altitude ajoutée (ne décale que le corps, pas l'ombre).
    """
    global PITCH, OBLIQUE
    if pitch is not None or oblique is not None:
        old = (PITCH, OBLIQUE)
        PITCH = pitch if pitch is not None else PITCH
        OBLIQUE = oblique if oblique is not None else OBLIQUE
        try:
            return render(mesh, size, facing, center, shadow, outline, dither, lift, frame_h, shadow_mesh, rgba, scale,
                          waterline, None, None, clip_z)
        finally:
            PITCH, OBLIQUE = old
    w = size if isinstance(size, int) else size[0]
    h = (size if isinstance(size, int) else size[1]) if frame_h is None else frame_h
    W, H = w * SS, h * SS
    cx, cy = center if center else (w / 2, h / 2)
    fwd, up = _view()
    a = math.radians(facing)
    ca, sa = math.cos(a), math.sin(a)

    def world(p):
        return ((p[0] * ca - p[1] * sa) * scale, (p[0] * sa + p[1] * ca) * scale, p[2] * scale + lift)

    def screen(q):
        return ((cx + q[0]) * SS, (cy - dot(q, up)) * SS, dot(q, fwd))

    zbuf = [1e9] * (W * H)
    cbuf = [None] * (W * H)
    tbuf = [False] * (W * H)
    sbuf = bytearray(W * H)

    for a3, b3, c3, mat in mesh.tris:
        if waterline is not None and max(a3[2], b3[2], c3[2]) < waterline:
            continue
        wa, wb, wc = world(a3), world(b3), world(c3)
        n = cross(sub(wb, wa), sub(wc, wa))
        if dot(n, n) < 1e-12:
            continue
        n = norm(n)
        if shadow:
            _raster_shadow(sbuf, W, H, cx, cy, up, (wa, wb, wc), lift)
        if dot(n, fwd) >= 0:
            continue
        # normale et point dans le repère du modèle (pour les textures)
        n_model = norm(cross(sub(b3, a3), sub(c3, a3)))
        flat_col = None if mat.tex else shade(mat, n, a3)
        sa_, sb_, sc_ = screen(wa), screen(wb), screen(wc)
        _raster(zbuf, cbuf, tbuf, W, H, sa_, sb_, sc_, a3, b3, c3, mat, n, n_model, flat_col,
                (wa[2], wb[2], wc[2]) if clip_z is not None else None, clip_z)

    if shadow and shadow_mesh is not None:
        for a3, b3, c3, mat in shadow_mesh.tris:
            _raster_shadow(sbuf, W, H, cx, cy, up, (world(a3), world(b3), world(c3)), lift)

    body = _downsample(cbuf, tbuf, sbuf, w, h, outline)
    if rgba:
        return body
    return _quantize(body, w, h, dither)


def _raster(zbuf, cbuf, tbuf, W, H, A, B, C, pa, pb, pc, mat, n, n_model, flat_col, wz=None, clip=None):
    x0 = max(0, int(math.floor(min(A[0], B[0], C[0]))))
    x1 = min(W - 1, int(math.ceil(max(A[0], B[0], C[0]))))
    y0 = max(0, int(math.floor(min(A[1], B[1], C[1]))))
    y1 = min(H - 1, int(math.ceil(max(A[1], B[1], C[1]))))
    if x0 > x1 or y0 > y1:
        return
    d = (B[1] - C[1]) * (A[0] - C[0]) + (C[0] - B[0]) * (A[1] - C[1])
    if abs(d) < 1e-9:
        return
    inv = 1.0 / d
    shade_cache = {}
    for y in range(y0, y1 + 1):
        py = y + 0.5
        row = y * W
        for x in range(x0, x1 + 1):
            px = x + 0.5
            l0 = ((B[1] - C[1]) * (px - C[0]) + (C[0] - B[0]) * (py - C[1])) * inv
            if l0 < -1e-6:
                continue
            l1 = ((C[1] - A[1]) * (px - C[0]) + (A[0] - C[0]) * (py - C[1])) * inv
            if l1 < -1e-6:
                continue
            l2 = 1.0 - l0 - l1
            if l2 < -1e-6:
                continue
            if wz is not None and l0 * wz[0] + l1 * wz[1] + l2 * wz[2] < clip:
                continue
            z = l0 * A[2] + l1 * B[2] + l2 * C[2]
            i = row + x
            if z >= zbuf[i]:
                continue
            zbuf[i] = z
            if flat_col is not None:
                cbuf[i] = flat_col
                tbuf[i] = mat.team
                continue
            p = (l0 * pa[0] + l1 * pb[0] + l2 * pc[0], l0 * pa[1] + l1 * pb[1] + l2 * pc[1], l0 * pa[2] + l1 * pb[2] + l2 * pc[2])
            t = mat.tex(p, n_model)
            key = t if isinstance(t, tuple) else round(t, 2)
            col = shade_cache.get(key)
            if col is None:
                if isinstance(t, tuple):
                    col = shade(Mat(t, False, mat.spec, mat.shine, None, mat.emit, mat.flat), n, p)
                else:
                    c0 = shade(Mat(mat.color, mat.team, mat.spec, mat.shine, None, mat.emit, mat.flat), n, p)
                    col = tuple(max(0, min(255, int(c * t))) for c in c0)
                shade_cache[key] = col
            cbuf[i] = col
            tbuf[i] = mat.team and not isinstance(t, tuple)


def _raster_shadow(sbuf, W, H, cx, cy, up, tri, lift):
    pts = []
    for q in tri:
        z = q[2]
        g = (q[0] - LO[0] * z / LO[2], q[1] - LO[1] * z / LO[2], 0.0)
        pts.append(((cx + g[0]) * SS, (cy - dot(g, up)) * SS))
    A, B, C = pts
    d = (B[1] - C[1]) * (A[0] - C[0]) + (C[0] - B[0]) * (A[1] - C[1])
    if abs(d) < 1e-9:
        return
    x0 = max(0, int(min(A[0], B[0], C[0])))
    x1 = min(W - 1, int(max(A[0], B[0], C[0])) + 1)
    y0 = max(0, int(min(A[1], B[1], C[1])))
    y1 = min(H - 1, int(max(A[1], B[1], C[1])) + 1)
    inv = 1.0 / d
    for y in range(y0, y1 + 1):
        py = y + 0.5
        for x in range(x0, x1 + 1):
            px = x + 0.5
            l0 = ((B[1] - C[1]) * (px - C[0]) + (C[0] - B[0]) * (py - C[1])) * inv
            l1 = ((C[1] - A[1]) * (px - C[0]) + (A[0] - C[0]) * (py - C[1])) * inv
            if l0 >= 0 and l1 >= 0 and l0 + l1 <= 1:
                sbuf[y * W + x] = 1


def _downsample(cbuf, tbuf, sbuf, w, h, outline):
    W = w * SS
    n = SS * SS
    body = {}
    for y in range(h):
        for x in range(w):
            cols, teams, shad = [], 0, 0
            for j in range(SS):
                row = (y * SS + j) * W + x * SS
                for i in range(SS):
                    c = cbuf[row + i]
                    if c is not None:
                        cols.append(c)
                        teams += tbuf[row + i]
                    elif sbuf[row + i]:
                        shad += 1
            if len(cols) * 2 >= n:
                r = sum(c[0] for c in cols) / len(cols)
                g = sum(c[1] for c in cols) / len(cols)
                b = sum(c[2] for c in cols) / len(cols)
                body[(x, y)] = [r, g, b, teams * 2 >= len(cols)]
            elif shad + len(cols) >= n * 0.5 and shad:
                body[(x, y)] = None     # ombre
    # liseré : pixels du corps en bord de silhouette légèrement assombris
    if outline:
        edge = []
        for (x, y), v in body.items():
            if v is None:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nb = body.get((x + dx, y + dy), "vide")
                if nb == "vide" or nb is None:
                    edge.append((x, y))
                    break
        for p in edge:
            v = body[p]
            body[p] = [v[0] * outline, v[1] * outline, v[2] * outline, v[3]]
    return body


def _quantize(body, w, h, dither, pool=None):
    img = Image.new("P", (w, h), 0)
    img.putpalette(PALETTE)
    err = {}
    for y in range(h):
        for x in range(w):
            v = body.get((x, y), "vide")
            if v == "vide":
                continue
            if v is None:
                img.putpixel((x, y), SHADOW)
                continue
            e = err.get((x, y), (0, 0, 0))
            rgb = tuple(max(0, min(255, int(round(v[i] + e[i])))) for i in range(3))
            idx = nearest(rgb, TEAM if v[3] else (pool or ALLOWED))
            img.putpixel((x, y), idx)
            if dither:
                q = PALETTE[3 * idx:3 * idx + 3]
                d = [(rgb[i] - q[i]) * dither for i in range(3)]
                for (dx, dy), k in (((1, 0), 7 / 16), ((-1, 1), 3 / 16), ((0, 1), 5 / 16), ((1, 1), 1 / 16)):
                    t = (x + dx, y + dy)
                    o = err.get(t, (0, 0, 0))
                    err[t] = (o[0] + d[0] * k, o[1] + d[1] * k, o[2] + d[2] * k)
    return img


# ---------------------------------------------------------------------------
# Feuilles de sprites
# ---------------------------------------------------------------------------
def sheet(frames):
    w, h = frames[0].size
    s = Image.new("P", (w * len(frames), h), 0)
    s.putpalette(PALETTE)
    for i, f in enumerate(frames):
        s.paste(f, (i * w, 0))
    return s


def save(frames, name):
    from PIL import PngImagePlugin
    w, h = frames[0].size
    info = PngImagePlugin.PngInfo()
    info.add_text("FrameSize", f"{w},{h}")
    info.add_text("FrameAmount", str(len(frames)))
    sheet(frames).save(os.path.join(BITS, name + ".png"), pnginfo=info, transparency=0)


def facings(mesh, size, n=32, **kw):
    return [render(mesh, size, facing=f * 360.0 / n, **kw) for f in range(n)]


def preview(frames, path, scale=4, cols=8, bg=(60, 88, 60)):
    """Planche d'aperçu en couleurs (équipe = rampe d'origine, ombre translucide)."""
    w, h = frames[0].size
    rows = (len(frames) + cols - 1) // cols
    out = Image.new("RGB", (w * cols * scale, h * rows * scale), bg)
    for k, f in enumerate(frames):
        rgba = Image.new("RGBA", f.size, (0, 0, 0, 0))
        src = f.load()
        px = rgba.load()
        for y in range(h):
            for x in range(w):
                i = src[x, y]
                if i == 0:
                    continue
                if i == SHADOW:
                    px[x, y] = (0, 0, 0, 110)
                else:
                    px[x, y] = tuple(PALETTE[3 * i:3 * i + 3]) + (255,)
        big = rgba.resize((w * scale, h * scale), Image.NEAREST)
        out.paste(big, ((k % cols) * w * scale, (k // cols) * h * scale), big)
    out.save(path)


# ---------------------------------------------------------------------------
# Icônes de construction (64 × 48, palette « chrome » : la rampe équipe reste dorée)
# ---------------------------------------------------------------------------
ICON_POOL = [i for i in range(16, 240) if not (96 <= i <= 103)]


def icon(mesh, zoom=2.0, facing=215.0, lift=0.0, ground=(74, 84, 62), sky=(98, 118, 128), dz=0.0,
         water=False, air=False, dx=0.0):
    """Portrait de l'unité sur un fond dégradé (sol ou mer, ciel), comme les icônes RA."""
    w, h = 64, 48
    global PITCH
    old = PITCH
    PITCH = 38.0
    try:
        body = render(mesh, (w, h), facing=facing, center=(w / 2 + dx, h * 0.62 + dz), shadow=not air,
                      outline=0.8, rgba=True, scale=zoom, lift=lift)
    finally:
        PITCH = old
    import random
    rnd = random.Random(7)
    if water:
        ground = (40, 76, 108)
    full = {}
    for y in range(h):
        for x in range(w):
            t = y / (h - 1)
            if air:
                c = [sky[i] * (1.1 - 0.35 * t) for i in range(3)]
            elif t < 0.3:
                c = [sky[i] * (1.05 - 0.4 * t) for i in range(3)]
            else:
                k = 0.75 + 0.35 * (t - 0.3)
                c = [ground[i] * k for i in range(3)]
                if water and (x * 3 + y * 7) % 11 == 0:
                    c = [v * 1.25 for v in c]
            n = rnd.uniform(-2, 2)
            vign = 1.0 - 0.35 * (((x - w / 2) / (w / 2)) ** 2 + ((y - h / 2) / (h / 2)) ** 2) / 2
            full[(x, y)] = [max(0, min(255, (v + n) * vign)) for v in c] + [False]
    for p, v in body.items():
        if v is None:
            c = full[p]
            full[p] = [c[0] * 0.55, c[1] * 0.55, c[2] * 0.55, False]
        else:
            full[p] = v
    img = _quantize(full, w, h, 0.22, ICON_POOL)
    # cadre biseauté comme les icônes d'origine
    for x in range(w):
        img.putpixel((x, 0), nearest((20, 20, 20), ICON_POOL))
        img.putpixel((x, h - 1), nearest((20, 20, 20), ICON_POOL))
    for y in range(h):
        img.putpixel((0, y), nearest((20, 20, 20), ICON_POOL))
        img.putpixel((w - 1, y), nearest((20, 20, 20), ICON_POOL))
    return img


# ---------------------------------------------------------------------------
# Bâtiments : dégâts
# ---------------------------------------------------------------------------
def _bruit2(x, y, seed):
    """Bruit de valeur lissé (0..1) pour des taches irrégulières."""
    def h(i, j):
        v = math.sin(i * 127.1 + j * 311.7 + seed * 74.7) * 43758.5453
        return v - math.floor(v)
    i, j = math.floor(x), math.floor(y)
    fx, fy = x - i, y - j
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b, c, d = h(i, j), h(i + 1, j), h(i, j + 1), h(i + 1, j + 1)
    return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy


def _degats_tex(base_tex, seed):
    def f(p, n):
        u, v = p[0] + p[2] * 0.7, p[1] - p[2] * 0.5
        suie = _bruit2(u / 7.0, v / 7.0, seed) * 0.65 + _bruit2(u / 2.5, v / 2.5, seed + 9) * 0.35
        w = _bruit2(u * 1.7, v * 1.7, seed + 3)
        t = base_tex(p, n) if base_tex else 1.0
        if suie > 0.68:                                  # traces de suie et de brûlé
            k = 0.3 if suie > 0.8 else 0.55
            return (30, 27, 25) if suie > 0.86 else (t * k if not isinstance(t, tuple) else tuple(int(c * k) for c in t))
        if w > 0.93:                                     # impacts
            return (26, 24, 22)
        return t
    return f


def endommage(mesh, seed=3):
    """Même maillage avec suie, impacts ; les matériaux émissifs (lampes) s'éteignent en partie."""
    cache = {}
    out = Mesh()
    for a, b, c, m in mesh.tris:
        k = id(m)
        if k not in cache:
            if m.emit:
                cache[k] = Mat(tuple(int(v * 0.5) for v in m.color), m.team, 0.1, m.shine)
            else:
                cache[k] = Mat(m.color, m.team, m.spec * 0.5, m.shine, _degats_tex(m.tex, seed), m.emit, m.flat)
        out.tri(a, b, c, cache[k])
    return out


def debris(x0, y0, x1, y1, n=10, seed=5):
    """Gravats et plaques tombées au sol."""
    import random
    rnd = random.Random(seed)
    m = Mesh()
    for _ in range(n):
        x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
        w, d, h = rnd.uniform(0.6, 1.8), rnd.uniform(0.6, 1.6), rnd.uniform(0.3, 1.0)
        c = rnd.choice(((84, 80, 74), (60, 56, 52), (110, 104, 94)))
        m.add(box(x - w / 2, y - d / 2, 0.0, x + w / 2, y + d / 2, h, Mat(c, spec=0.05)).rot_z(rnd.uniform(0, 90), x, y))
    return m


def batiment(mesh, cellules, t=None, degats=False, seed=3, feux=None, shadow=True, outline=0.7, eau=False, marge=(0, 0),
             facing=0.0, pose=None):
    """Rend un bâtiment dont l'emprise fait cellules = (largeur, hauteur) cases : image de la taille de
    l'emprise, centrée sur son centre, en projection oblique. t (0..1) : construction qui sort du sol."""
    w, h = cellules[0] * 24 + 2 * marge[0], cellules[1] * 24 + 2 * marge[1]
    if pose is not None:                     # partie tournante (tourelle) ajoutée après rotation
        mesh = merge(mesh, pose.rot_z(facing))
    m = endommage(mesh, seed) if degats else mesh
    if degats and feux:
        for (x, y, z, r) in feux:
            m = merge(m, sphere((x, y, z), r, Mat((255, 170, 60), emit=True), seg=6, rings=3),
                      sphere((x + r * 0.3, y, z + r * 0.8), r * 0.6, Mat((255, 230, 120), emit=True), seg=6, rings=3))
    clip = None
    if t is not None:
        haut = max(max(p[2] for p in tri[:3]) for tri in mesh.tris)
        m = m.move(dz=-(1.0 - t) * (haut + 0.5))
        clip = 0.0
    return render(m, (w, h), facing=0.0, center=(w / 2, h / 2), shadow=shadow and not eau and t is None, oblique=True,
                  clip_z=clip, outline=outline, dither=0.3)
