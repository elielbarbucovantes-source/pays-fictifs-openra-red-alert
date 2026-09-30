# Australouis : clé de l'acteur -> (nom, lore sur 3 lignes, stratégie)
TEXTES = {
"BASE.AERONAVALE": ("Base aéronavale", """Le cœur de la puissance australouisienne : pistes, hangars et ateliers.
Elle produit, répare et réarme les avions du pays.
Pour un peuple des îles, c'est elle qui relie tout l'archipel.""",
"Construisez-la tôt et protégez-la de la défense antiaérienne : sans elle, vos avions ne se réarment plus."),
"ALBATROS": ("Albatros", """Le chasseur multirôle de l'aéronavale, fierté des îles.
Missiles air-air et air-sol, qui frappent aussi les navires.
Il porte le nom de l'oiseau qui traverse les océans sans se poser.""",
"Chasseur polyvalent : escortez les Manta, chassez les avions ennemis, puis frappez les blindés et les navires."),
"WARTHOG": ("Warthog", """Chasseur de chars blindé, repris de la carte WW3.
Son canon de 30 mm déchire les blindés, ses missiles finissent le travail.
Lent, mais presque impossible à abattre.""",
"Envoyez-le contre les colonnes de chars. Évitez les SAM et les chasseurs, contre lesquels il ne peut rien."),
"MANTA": ("Manta", """Bombardier lourd sur le modèle du B-2 de la carte WW3, en moins furtif.
Un seul passage de six bombes incendiaires, puis retour à la base.
Il glisse au-dessus de l'ennemi comme une raie dans l'eau.""",
"Visez les bâtiments et l'infanterie groupée. Escortez-le avec des Albatros : il est visible au radar."),
"CORMORAN": ("Cormoran", """Hydravion bombardier d'eau, unité spéciale de l'Australouis.
Il écope en survolant la mer, puis pulvérise un brouillard sur une zone.
L'ennemi y perd tout ce qu'il avait découvert : la carte redevient noire.""",
"Faites-lui survoler l'eau 2 secondes, puis Ctrl + clic sur la zone à cacher (base, flotte, colonne en marche). Idéal avant une attaque surprise."),
"BALEINE": ("Baleine", """Hélicoptère de transport lourd, le Chinook des îles.
Dix fantassins d'une île à l'autre, plus vite et plus solide que le modèle allié.
Aucune arme : il compte sur l'escorte.""",
"Débarquez l'infanterie derrière les lignes ou sur une île voisine, sous la couverture des Albatros."),
"MOUSTIQUE": ("Moustique", """Drone kamikaze antichar, bon marché et très fragile.
Il plonge sur un véhicule ou un navire et explose.
Seul, il pique ; en essaim de trois, il tue un char léger.""",
"Lancez-les par groupes de trois ou plus sur les véhicules. Gardez-les loin de la défense antiaérienne, qui les abat d'un coup."),
"MOTHERSHIP": ("Mothership", """Le navire-mère de l'archipel, immense et presque sans armes.
Sous son blindage dort une machine à faire naître des îles : 2 min 50 pour sortir 8×8 cases de terre de l'océan.
Là où il jette l'ancre, l'Australouis peut bâtir une nouvelle base.""",
"Escortez-le jusqu'à un point stratégique, déployez-le (F) et ne le bougez plus jusqu'à la fin. Amenez ensuite un chantier de construction avec un transport."),
"PORTE.DRONE": ("Porte-Drone", """Pas un porte-avions : une plateforme navale de production d'essaims kamikazes.
Sans arme et peu blindé, il fabrique lui-même, gratuitement, six drones à la fois, sans base ni munitions.
Le lot suivant ne sort des ateliers que 20 secondes après la chute du sixième drone : tant qu'un seul vole encore, rien ne se régénère.""",
"Clic droit sur un véhicule ou un navire ennemi : un drone part (cliquez plusieurs cibles pour répartir l'essaim). Réfléchissez avant de sacrifier le dernier drone : vous restez 20 s sans défense. Plusieurs Porte-Drones ont chacun leur propre cycle."),
"L0U15": ("L-0U15", """Destroyer léger, mal armé et peu blindé.
Son sonar est le meilleur de toutes les mers : aucun sous-marin ne lui échappe à 18 cases.
Il ne gagne pas les combats : il dit à la flotte où tirer.""",
"Placez-le au milieu de votre flotte, jamais en tête : il révèle les sous-marins, vos destroyers et vos avions les détruisent."),
"SOUSMARIN.ICBM": ("Sous-marin ICBM", """Sous-marin lance-missiles repris de la carte WW3 (Ukraine).
Ses missiles V3 frappent les côtes et les bases jusqu'à 24 cases.
En plongée, il reste invisible sauf pour les sonars.""",
"Approchez-vous des côtes ennemies en plongée et pilonnez la base de loin. Fuyez les destroyers et le L-0U15 adverse."),
"CHANTIER.AVANCE": ("Chantier naval avancé", """Chantier naval de haute technologie, 3 au maximum.
Seul capable de construire le sous-marin ICBM et le Mothership.
Il abrite aussi le centre d'écoute du Radar blink et les générateurs de houle de l'Engineered Tsunami.""",
"Construisez-en au moins un dès que le centre technique est prêt, dans une baie bien défendue."),
"CENTRALE.ENRICHISSEMENT": ("Centrale d'enrichissement", """Usine qui enrichit le minerai avant sa vente.
Tant qu'elle est debout, chaque chargement rapporte 5 % de plus.
Avec un silo, elle lance le Raffinerie boost.""",
"Construisez-la tôt dans une économie à plusieurs raffineries et protégez-la : c'est une cible de choix pour l'ennemi."),
"CHEAPER": ("Cheaper", """Char produit à la chaîne dans les usines de l'archipel.
Petit canon, blindage léger : seul, il ne vaut presque rien.
Par dizaines, il submerge.""",
"Produisez-en en continu et attaquez en masse. Ne les envoyez pas seuls contre des chars lourds ou des défenses."),
"REVOLUTIONNAIRE": ("Révolutionnaire", """Meneur d'hommes, orateur, agitateur. Un seul à la fois.
Quand sa révolution est prête, tout ce qui l'entoure change de camp : soldats, chars, bâtiments.
Fragile : l'ennemi voudra l'abattre avant qu'il ne parle.""",
"Attendez que la barre violette soit pleine (5 minutes), approchez-le discrètement d'un groupe ennemi ou d'une base, puis déployez-le (F)."),
}
