# Rubénie : clé de l'acteur -> (nom, lore sur 3 lignes, stratégie)
TEXTES = {
"FUSILIER": ("Fusilier rubénien", """Le fantassin de base de la Rubénie, solide mais lent.
Son fusil tire avec une cadence soutenue plutôt que des coups puissants.
Il est à son meilleur derrière un parapet.""",
"Garnissez tranchées et bunkers de fusiliers : à couvert, ils gagnent les échanges de tirs qui durent."),
"GRENADIER": ("Grenadier", """Lanceur de grenades à zone d'effet élargie.
Redoutable contre les groupes, en ville et dans les tranchées ennemies.
Là où il passe, personne ne reste groupé.""",
"Visez l'infanterie groupée et les positions retranchées. Protégez-le des véhicules."),
"CHASSEUR.AT": ("Chasseur antichar", """Fantassin armé d'un missile antichar à longue portée, lent à recharger.
Il attend les blindés depuis un bunker ou une embuscade.
Le Tunnelier en emporte dix vers le cœur du dispositif ennemi.""",
"Placez-le en bunker sur les axes de passage des chars, ou faites-le surgir avec le Tunnelier."),
"ECLAIREUR": ("Éclaireur", """Éclaireur camouflé, invisible à l'arrêt.
Il voit très loin, repère les furtifs et même le Tunnelier sous terre.
Il ne tire que pour se défendre.""",
"Placez-le immobile en avant de vos lignes pour guider l'artillerie lourde et repérer les raids."),
"SECOND.GALACTIQUE": ("Second Galactique", """Officier d'élite, à la fois chef et combattant.
Son aura renforce les unités rubéniennes proches ; il manie une décharge Tesla et pose des charges.
Un seul exemplaire : l'ennemi voudra l'abattre en priorité.""",
"Gardez-le au milieu de vos troupes pour que son aura (5 cases) profite au plus grand nombre. Ne l'exposez pas en tête."),
"SICATIENT": ("« Si ça tient, touche à rien »", """Char de bataille principal, canon de 115 mm et gros blindage.
Son nom est aussi la devise du pays.
Hors combat, son équipage le remet en état, tant qu'il reste ravitaillé.""",
"Char de ligne : tenez la position, laissez l'ennemi s'épuiser, réparez-vous, recommencez. Restez dans le rayon de ravitaillement."),
"CONTREATTAQUE": ("Char de contre-attaque", """Char rapide conçu pour sortir, frapper et rentrer.
Canon de 90 mm à cadence rapide, blindage léger pour un char.
L'arme de la seconde moitié de la doctrine : la contre-attaque.""",
"Gardez-le derrière vos défenses et lancez-le quand l'assaut ennemi s'essouffle, idéalement sous Contre-attaque éclair."),
"FORTERESSE": ("Char lourd de forteresse", """Char lourd défensif au blindage extrême.
Canon de 140 mm et missiles antichars et antiaériens.
Très lent : il ne recule pas, il n'avance guère non plus.""",
"Point d'ancrage d'une ligne défensive. Protégez-le de l'artillerie ennemie, son seul vrai prédateur."),
"RECO.RUB": ("Véhicule de reconnaissance", """Véhicule rapide à grande vision, avec deux fantassins à bord.
Il détecte les furtifs et le Tunnelier sous terre.
Les yeux mobiles de l'armée rubénienne.""",
"Patrouillez devant vos lignes pour repérer les raids elielistanais et les unités camouflées."),
"AA.RUB": ("Véhicule antiaérien", """Défense antiaérienne mobile à canons de 30 mm.
Plus de portée que le Flak classique.
Il protège convois, chars, artillerie et bases avancées.""",
"Accompagnez chaque groupe blindé et chaque batterie d'artillerie d'au moins un véhicule antiaérien."),
"LANCEMISSILE": ("Lance-missiles mobile", """Missile balistique à très longue portée (13 cases).
Rechargement lent : tirer, changer de position, tirer.
Une seule salve peut ouvrir une brèche.""",
"Frappez les bâtiments et les concentrations de loin. Il ne survit pas au contact : gardez-le bien en arrière."),
"CANON.AUTO": ("Canon automoteur", """Artillerie automotrice à obus de 152 mm, portée de 11 cases.
Assez mobile pour suivre les blindés.
L'appui feu de toutes les contre-attaques.""",
"Suivez vos chars à distance et pilonnez les positions ennemies avant l'assaut."),
"ARTILLERIE.LOURDE": ("Artillerie lourde", """Artillerie tractée à la portée extrême (18 cases) et aux dégâts énormes.
Sa cadence est très lente et elle se déplace à peine.
Installée, elle interdit toute une région.""",
"Placez-la derrière une ligne défensive solide avec un Éclaireur en avant. Elle est sans défense au contact."),
"LRM": ("Lance-roquettes multiple", """Salve de 12 roquettes sur une zone, puis long rechargement.
Il écrase l'infanterie, les convois et les concentrations.
Le vacarme de sa salve s'entend à des kilomètres.""",
"Visez les groupes et les convois. Repliez-le pendant le rechargement."),
"VEHICULE.LOGISTIQUE": ("Véhicule logistique", """Ravitaillement mobile et atelier de campagne.
Il prolonge la logistique loin de la base et répare vite les unités proches, même au combat.
Aucune arme : sa perte isole toute une colonne.""",
"Faites-le suivre chaque offensive pour éviter l'isolement (−25 % de dégâts et de cadence). Protégez-le."),
"TUNNELIER": ("Tunnelier", """Engin de sape qui avance sous terre, invisible et intouchable.
Il emporte dix chasseurs antichars jusqu'au cœur du dispositif ennemi.
En émergeant, il reste quelques secondes immobile et vulnérable.""",
"Faites-le émerger à l'abri des vues, puis débarquez les chasseurs antichars (F) sur les blindés ou l'artillerie ennemis. Seuls les détecteurs sismiques le repèrent."),
"QG.RUB": ("QG rubénien", """Quartier général et siège du commandement rubénien.
Il ravitaille les unités proches, détecte le Tunnelier et porte les cinq pouvoirs de soutien.
Un seul exemplaire : le cœur de la forteresse.""",
"Construisez-le au centre de votre base, bien protégé : il débloque le Second Galactique et le Tunnelier."),
"CENTRE.LOGISTIQUE": ("Centre logistique", """Grand dépôt qui ravitaille les unités terrestres dans un rayon de 14 cases.
Les unités ravitaillées gardent leur efficacité et se réparent lentement hors combat.
Plusieurs centres se relaient : en détruire un ne suffit pas.""",
"Couvrez vos lignes de front de centres logistiques ; hors de leur rayon pendant 30 s, vos unités deviennent isolées."),
"BASE.AERIENNE": ("Base aérienne", """Base qui produit, répare et réarme les avions rubéniens.
Chasseur polyvalent, intercepteur, avion d'attaque et avion de reconnaissance.
La couverture aérienne de la forteresse.""",
"Placez-la au fond de la base, sous la protection de SAM et de tourelles AA."),
"CHANTIER.RUB": ("Chantier naval rubénien", """Chantier qui produit et répare les navires rubéniens.
Escorteurs, frégates, sous-marins et porte-avions.
La porte de la marine lourde et défensive.""",
"Protégez-le avec de l'artillerie côtière : c'est la cible prioritaire des raids navals."),
"TRANCHEE.RUB": ("Tranchée (est-ouest)", """Tranchée rubénienne, plus solide que celle de l'Elielistan.
Cinq fantassins y tirent à couvert et en ressortent si elle est détruite.
La première ligne de la forteresse.""",
"Garnissez-la de fusiliers et de grenadiers devant vos défenses lourdes."),
"TRANCHEEV.RUB": ("Tranchée (nord-sud)", """Même tranchée rubénienne, orientée nord-sud.
Cinq fantassins y tirent à couvert.
Choisissez l'orientation face à la menace.""",
"Combinez les deux orientations pour fermer un passage."),
"BUNKER.RUB": ("Bunker", """Casemate d'infanterie très résistante, livrée avec un fusilier.
Cinq occupants tirent par les meurtrières ; elle détecte les unités furtives.
Sa puissance dépend de sa garnison.""",
"Garnissez-le de chasseurs antichars contre les blindés, de grenadiers contre l'infanterie."),
"CASEMATE.AT": ("Casemate antichar", """Canon antichar fixe à longue portée (8 cases).
Il tient les axes d'approche des blindés.
Impuissant contre l'infanterie et l'aviation.""",
"Couvrez-la avec des tranchées et une défense antiaérienne."),
"TOURELLE.AA": ("Tourelle AA", """Défense antiaérienne rapprochée à canons rapides.
Elle abat avions et hélicoptères qui s'approchent trop.
La dernière couche de la défense aérienne.""",
"Associez-la aux SAM : les SAM frappent loin, les tourelles achèvent."),
"SAM.RUB": ("SAM", """Missiles sol-air à longue portée (11 cases).
Priorité aux avions et aux hélicoptères.
La première couche de la défense aérienne.""",
"Placez-les en retrait des lignes pour couvrir toute la base."),
"COTIERE": ("Artillerie côtière", """Canon lourd de défense côtière, portée de 14 cases.
Il n'engage que les navires.
Aucun porte-avions ne s'approche de la côte sans en payer le prix.""",
"Défendez vos côtes et votre chantier naval contre les débarquements elielistanais."),
"CHASSEUR.POLY": ("Chasseur polyvalent", """Le « couteau suisse » de l'aviation rubénienne.
Missiles air-air et air-sol, aussi contre les navires ; grande vision.
Seul avion capable d'apponter sur le porte-avions rubénien.""",
"Utilisez-le pour la supériorité aérienne comme pour les frappes. Il rentre seul se réarmer à court de munitions."),
"INTERCEPTEUR": ("Intercepteur", """Intercepteur très rapide armé de missiles air-air lourds.
Il ne peut pas attaquer le sol.
En position défensive, il engage seul les avions ennemis.""",
"Laissez-le en défense au-dessus de la base contre les bombardiers et les Pelícano ennemis."),
"AVION.ATTAQUE": ("Avion d'attaque", """Avion d'attaque au sol, lent mais blindé.
Huit missiles antichars lourds.
L'appui aérien des contre-attaques.""",
"Frappez les colonnes blindées une fois la DCA ennemie neutralisée."),
"AVION.RECO": ("Avion de reconnaissance", """Avion très rapide à la vision immense (16 cases), sans arme.
Il détecte les unités furtives et tourne au-dessus de la zone demandée.
Il sait où l'ennemi se cache.""",
"Envoyez-le repérer les raids et les unités camouflées avant de lancer l'artillerie lourde."),
"ESCORTEUR": ("Escorteur", """Escorteur de la marine rubénienne.
Missiles antiaériens à longue portée, missiles antinavires, grenades anti-sous-marines.
Il détecte les sous-marins.""",
"Escortez le porte-avions et les frégates : il couvre l'air et le dessous de l'eau."),
"FREGATE": ("Frégate lance-missiles", """Frégate armée de missiles antinavires à 13 cases.
Elle frappe aussi les cibles côtières.
Aucune défense antiaérienne : elle a besoin d'escorte.""",
"Restez à distance maximale et escortez-la d'un escorteur."),
"SOUS.MARIN": ("Sous-marin", """Sous-marin d'attaque, invisible en plongée.
Ses torpilles lourdes frappent les navires en embuscade.
La menace invisible des routes maritimes.""",
"Embusquez-le sur le trajet des porte-avions ennemis. Évitez les escorteurs."),
"PORTE.AVIONS.RUB": ("Porte-avions rubénien", """Base aérienne mobile, plus résistante que son équivalent elielistanais.
Trois chasseurs polyvalents au maximum, réarmés à bord.
Défense antiaérienne rapprochée, mais aucune arme contre les sous-marins.""",
"Faites-le escorter par des escorteurs et utilisez ses chasseurs pour couvrir vos côtes et votre flotte."),
"BASTION": ("Bastion roulant", """Forteresse roulante : deux canons de 130 mm et un lanceur de missiles.
Ses armes ne tirent que si des fantassins sont à bord ; il est livré avec quatre hommes.
Il ravitaille les unités autour de lui et n'est jamais isolé.""",
"Faites-en le centre d'une contre-attaque : il porte sa propre logistique. Gardez-le toujours garni et protégez-le de l'artillerie. Trois au maximum."),
}
