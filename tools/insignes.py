"""Génère les insignes de grade du classement Elo (mods/fictifs/uibits/grades*.png).

Une icône carrée par grade, dans l'ordre de ClassementEloStore.Grades ; les images
sont complétées jusqu'à des côtés en puissance de deux (sinon le salon plante).
Lancer : python3 tools/insignes.py
"""
import math
import os
from PIL import Image, ImageDraw

GRADES = ["soldat", "caporal", "sergent", "lieutenant", "capitaine",
          "commandant", "colonel", "general", "marechal"]
S = 96  # dessin en haute résolution, réduit ensuite
OR = (232, 190, 70, 255)
ARGENT = (205, 210, 220, 255)
ROUGE = (200, 40, 40, 255)
GRIS = (165, 165, 165, 255)
FOND = (28, 36, 58, 255)
BORD = (120, 130, 150, 255)
FOND_MARECHAL = (110, 20, 25, 255)


def fond(d, couleur=FOND):
    d.rounded_rectangle([4, 4, S - 5, S - 5], radius=14, fill=couleur, outline=BORD, width=4)


def chevrons(d, n, couleur):
    h = 18
    total = n * h + (n - 1) * 6
    y0 = (S - total) // 2 + 8
    for i in range(n):
        y = y0 + i * (h + 6)
        d.polygon([(16, y), (S // 2, y - 16), (S - 16, y), (S - 16, y + h - 8),
                   (S // 2, y - 16 + h), (16, y + h - 8)], fill=couleur)


def galons(d, n):
    h = 9 if n > 3 else 11
    pas = h + 5
    y0 = (S - (n * pas - 5)) // 2
    for i in range(n):
        d.rectangle([18, y0 + i * pas, S - 19, y0 + i * pas + h - 1], fill=OR)


def etoile(d, cx, cy, r, couleur):
    pts = []
    for k in range(10):
        a = -math.pi / 2 + k * math.pi / 5
        rr = r if k % 2 == 0 else r * 0.42
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    d.polygon(pts, fill=couleur)


def insigne(nom):
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if nom == "marechal":
        fond(d, FOND_MARECHAL)
        etoile(d, S / 2, 30, 18, OR)
        etoile(d, 28, 64, 18, OR)
        etoile(d, S - 28, 64, 18, OR)
        return img
    fond(d)
    if nom == "soldat":
        chevrons(d, 1, GRIS)
    elif nom == "caporal":
        chevrons(d, 2, ROUGE)
    elif nom == "sergent":
        chevrons(d, 3, OR)
    elif nom == "general":
        etoile(d, 30, S / 2, 19, ARGENT)
        etoile(d, S - 30, S / 2, 19, ARGENT)
    else:
        galons(d, {"lieutenant": 2, "capitaine": 3, "commandant": 4, "colonel": 5}[nom])
    return img


def puissance2(n):
    p = 1
    while p < n:
        p *= 2
    return p


def main():
    dossier = os.path.join(os.path.dirname(__file__), "..", "mods", "fictifs", "uibits")
    icones = [insigne(g) for g in GRADES]
    for echelle, suffixe in [(1, ""), (2, "-2x"), (3, "-3x")]:
        t = 16 * echelle
        planche = Image.new("RGBA", (puissance2(t * len(GRADES)), puissance2(t)), (0, 0, 0, 0))
        for i, ic in enumerate(icones):
            planche.paste(ic.resize((t, t), Image.LANCZOS), (i * t, 0))
        planche.save(os.path.join(dossier, f"grades{suffixe}.png"))

    print("grades:")
    print("\tImage: fictifs|uibits/grades.png")
    print("\tImage2x: fictifs|uibits/grades-2x.png")
    print("\tImage3x: fictifs|uibits/grades-3x.png")
    print("\tRegions:")
    for i, g in enumerate(GRADES):
        print(f"\t\t{g}: {i * 16}, 0, 16, 16")


if __name__ == "__main__":
    main()
