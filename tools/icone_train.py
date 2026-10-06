"""Génère l'icône de l'onglet de production « Trains » (mods/fictifs/uibits/train-icons*.png).

Même style que les glyphes RA (production-icons) : silhouette blanche, grise si
l'onglet est désactivé, orange quand un train est prêt. Trois états côte à côte,
images complétées jusqu'à des côtés en puissance de deux (sinon le jeu plante).
Lancer : python3 tools/icone_train.py
"""
import os
from PIL import Image, ImageDraw

S = 128  # dessin en haute résolution, réduit ensuite
ETATS = [("train", (255, 255, 255)), ("train-disabled", (128, 128, 128)), ("train-alert", (255, 192, 0))]
SORTIE = os.path.join(os.path.dirname(__file__), "..", "mods", "fictifs", "uibits")


def silhouette():
    """Locomotive vue de profil, cabine à droite, sur un rail."""
    m = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(m)
    # Caisse basse (capot moteur) et cabine haute.
    d.rounded_rectangle([10, 52, 92, 92], radius=8, fill=255)
    d.rounded_rectangle([72, 22, 118, 92], radius=8, fill=255)
    # Fenêtre de la cabine (évidée).
    d.rounded_rectangle([84, 32, 108, 52], radius=4, fill=0)
    # Cheminée.
    d.rectangle([26, 36, 38, 54], fill=255)
    # Roues : évidées autour pour les détacher de la caisse.
    for cx in (30, 62, 98):
        d.ellipse([cx - 15, 82, cx + 15, 112], fill=0)
        d.ellipse([cx - 11, 86, cx + 11, 108], fill=255)
    # Rail.
    d.rectangle([2, 116, S - 3, 124], fill=255)
    return m


def planche(taille, largeur, hauteur):
    masque = silhouette().resize((taille, taille), Image.LANCZOS)
    img = Image.new("RGBA", (largeur, hauteur), (255, 255, 255, 0))
    for i, (_, couleur) in enumerate(ETATS):
        icone = Image.new("RGBA", (taille, taille), couleur + (0,))
        icone.putalpha(masque)
        img.paste(icone, (i * taille, 0))
    return img


for suffixe, taille, largeur, hauteur in (("", 16, 64, 16), ("-2x", 32, 128, 32), ("-3x", 48, 256, 64)):
    planche(taille, largeur, hauteur).save(os.path.join(SORTIE, f"train-icons{suffixe}.png"))
