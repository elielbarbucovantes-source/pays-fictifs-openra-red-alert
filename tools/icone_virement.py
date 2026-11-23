#!/usr/bin/env python3
"""Icône du bouton « Virement » (en haut à gauche en partie) : deux pièces et une flèche.

Produit mods/fictifs/uibits/virement.png, virement-2x.png, virement-3x.png :
deux icônes de 24×24 (normale, désactivée) côte à côte, à l'échelle 1, 2 et 3.
La planche est complétée par du transparent jusqu'à des dimensions en puissance de deux :
le moteur refuse (crash en partie) une image de chrome qui ne l'est pas.
"""
import os
from PIL import Image, ImageDraw

ICI = os.path.dirname(os.path.abspath(__file__))
SORTIE = os.path.join(ICI, "..", "mods", "fictifs", "uibits")
TAILLE = 24
SURECHANTILLON = 8


def piece(d, cx, cy, r, s, or_, bord, reflet):
    d.ellipse([(cx - r) * s, (cy - r) * s, (cx + r) * s, (cy + r) * s], fill=bord)
    r2 = r - 1.3
    d.ellipse([(cx - r2) * s, (cy - r2) * s, (cx + r2) * s, (cy + r2) * s], fill=or_)
    r3 = r - 3.2
    d.ellipse([(cx - r3) * s, (cy - r3) * s, (cx + r3) * s, (cy + r3) * s], outline=bord, width=max(1, int(0.9 * s)))
    d.ellipse([(cx - r2 + 1.5) * s, (cy - r2 + 1.2) * s, (cx - r2 + 4) * s, (cy - r2 + 3) * s], fill=reflet)


def icone(echelle, desactive):
    s = echelle * SURECHANTILLON
    im = Image.new("RGBA", (TAILLE * s, TAILLE * s), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    if desactive:
        or_, bord, reflet, fleche, contour = (120, 120, 120, 255), (70, 70, 70, 255), (170, 170, 170, 255), (150, 150, 150, 255), (40, 40, 40, 255)
    else:
        or_, bord, reflet, fleche, contour = (236, 190, 60, 255), (140, 96, 20, 255), (255, 240, 170, 255), (120, 230, 120, 255), (20, 70, 20, 255)

    piece(d, 8, 9, 7, s, or_, bord, reflet)
    piece(d, 13, 14, 7, s, or_, bord, reflet)

    # Flèche vers la droite, en bas.
    pts = [(12, 18.5), (18, 18.5), (18, 16), (23, 20), (18, 24), (18, 21.5), (12, 21.5)]
    pts = [(x, y - 1) for x, y in pts]
    d.polygon([(x * s, y * s) for x, y in pts], fill=fleche, outline=contour, width=max(1, int(0.8 * s)))
    return im.resize((TAILLE * echelle, TAILLE * echelle), Image.LANCZOS)


def puissance_de_deux(n):
    p = 1
    while p < n:
        p *= 2
    return p


def main():
    for echelle, suffixe in ((1, ""), (2, "-2x"), (3, "-3x")):
        taille = (puissance_de_deux(2 * TAILLE * echelle), puissance_de_deux(TAILLE * echelle))
        planche = Image.new("RGBA", taille, (0, 0, 0, 0))
        planche.paste(icone(echelle, False), (0, 0))
        planche.paste(icone(echelle, True), (TAILLE * echelle, 0))
        planche.save(os.path.join(SORTIE, "virement%s.png" % suffixe))


if __name__ == "__main__":
    main()
