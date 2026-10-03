"""Sprites des tranchées creusées par le Tunnelier avancé (mods/fictifs/bits/canal.png).

16 images de 24 × 24 en RGBA (indépendantes de la palette du jeu de tuiles), une par
combinaison de voisins creusés : bit 1 = nord, 2 = est, 4 = sud, 8 = ouest.
Usage : python3 tools/canal.py
"""
import math
import os
from PIL import Image, PngImagePlugin

T = 24
BITS = os.path.join(os.path.dirname(__file__), "..", "mods", "fictifs", "bits")
FOND = (74, 56, 40)      # fond de tranchée, terre humide
PAROI = (112, 86, 58)    # parois éclairées
DEBLAI = (150, 120, 84)  # bourrelet de terre rejetée sur les bords


def bruit(x, y, k=0):
    v = math.sin(x * 12.9898 + y * 78.233 + k * 37.719) * 43758.5453
    return v - math.floor(v)


def tuile(mask):
    img = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    px = img.load()
    a, b = 6, T - 6  # largeur de la tranchée : cases 6 à 17
    n, e, s, w = [(mask >> i) & 1 for i in range(4)]

    def dans(x, y, marge=0):
        x0, x1, y0, y1 = a - marge, b + marge, a - marge, b + marge
        if x0 <= x < x1 and y0 <= y < y1:
            return True
        if n and x0 <= x < x1 and y < y1:
            return True
        if s and x0 <= x < x1 and y >= y0:
            return True
        if w and y0 <= y < y1 and x < x1:
            return True
        if e and y0 <= y < y1 and x >= x0:
            return True
        return False

    for y in range(T):
        for x in range(T):
            r = bruit(x, y, mask)
            if dans(x, y):
                # paroi éclairée en haut et à gauche (lumière du nord-ouest)
                bord_haut = not dans(x, y - 2)
                bord_gauche = not dans(x - 2, y)
                c = PAROI if (bord_haut or bord_gauche) else FOND
                k = 0.88 + 0.24 * r
                px[x, y] = tuple(min(255, int(v * k)) for v in c) + (255,)
            elif dans(x, y, 3):
                # déblais : bourrelet irrégulier, plus clair, bords effilochés
                if r > 0.25:
                    k = 0.85 + 0.3 * bruit(y, x, 7)
                    px[x, y] = tuple(min(255, int(v * k)) for v in DEBLAI) + (int(150 + 100 * r),)
    return img


def main():
    frames = [tuile(m) for m in range(16)]
    sheet = Image.new("RGBA", (T * 16, T), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.paste(f, (i * T, 0))
    info = PngImagePlugin.PngInfo()
    info.add_text("FrameSize", f"{T},{T}")
    info.add_text("FrameAmount", "16")
    sheet.save(os.path.join(BITS, "canal.png"), pnginfo=info)
    print("canal.png : 16 tuiles")


if __name__ == "__main__":
    main()
