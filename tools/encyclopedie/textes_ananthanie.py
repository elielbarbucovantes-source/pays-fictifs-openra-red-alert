# Ananthanie : clé de l'acteur -> (nom, lore sur 3 lignes, stratégie)
TEXTES = {
"THAL": ("Thal", """Le fusilier ananthanien, formé en masse dans des casernes modernes.
Bien équipé, un peu plus de portée que le fusilier standard.
L'Ananthanie ne manque jamais de Thals.""",
"Produisez-les en grand nombre : ils sont formés très vite. Ils tiennent le terrain pendant que blindés et drones frappent."),
"KHESH.THAL": ("Khesh-Thal", """Fantassin lance-missiles polyvalent.
Un missile antichar et un missile antiaérien : il ne choisit pas, il fait les deux.
La pointe de la lance (« khesh ») de l'infanterie ananthanienne.""",
"Mêlez-les aux Thals : ils couvrent la colonne contre les blindés comme contre les hélicoptères."),
"NAYRAK": ("Nayrak", """Opérateur de drones explosifs FPV.
Il pilote son drone jusqu'à la cible, puis en prépare un autre.
Le soldat ananthanien le plus craint des équipages de chars.""",
"Gardez-les derrière la ligne : à 7 cases, leurs drones percent les véhicules. Protégez-les de l'infanterie ennemie au contact."),
"LUNKAR": ("Lunkar", """Éclaireur camouflé, le « lynx » de l'armée.
Il marque véhicules et bâtiments au laser pour toute l'armée, et abat les fantassins de loin.
Invisible à l'arrêt.""",
"Placez-le en avant avant l'offensive : chaque cible marquée prend +25 % de dégâts. Il donne aussi des yeux à l'Agnar."),
"KARNTHAL": ("Karnthal", """Soldat en exosquelette, mitrailleuse lourde.
Très résistant ; les chars ne peuvent pas l'écraser.
Le « rempart » (« karn ») de l'infanterie.""",
"Mettez-les en tête des assauts d'infanterie : ils encaissent pendant que les Thals tirent."),
"VIDRA": ("Vidra", """Char léger de percée, très rapide.
Canon-mitrailleur de 40 mm à cadence élevée.
« L'éclair » : il ouvre la brèche, les autres s'y engouffrent.""",
"Exploitez la vitesse : contournez, frappez l'artillerie et les collecteurs, repliez-vous. Évitez les chars lourds."),
"LUNKRA": ("Lunkra", """Char de combat principal, emblème du blindé ananthanien.
Canon de 120 mm à chargement automatique, tourelle rapide, bonne mobilité.
Ni le plus lourd ni le plus rapide : le meilleur compromis.""",
"Le cœur des colonnes blindées. En nombre, appuyé par les Nayrak et le Lunkar, il gagne la plupart des combats de chars."),
"KARNVASHA": ("Karnvasha", """Char lourd de rupture, tourelle inhabitée.
Canon de 130 mm, mitrailleuse téléopérée, protection active contre missiles et roquettes.
« Tempête contre le rempart » : il enfonce les lignes fortifiées.""",
"Menez les assauts contre les défenses : sa protection active réduit les dégâts des missiles antichars. Méfiez-vous de l'artillerie et des avions."),
"RATHA": ("Ratha", """Véhicule de combat d'infanterie : cinq fantassins à bord.
Canon automatique de 30 mm qui tire au sol comme dans les airs.
Le « char de guerre » des fantassins ananthaniens.""",
"Transportez Khesh-Thals ou Nayraks au plus près, puis couvrez-les avec le canon. Utile aussi contre les hélicoptères."),
"MUKHAR": ("Mukhar", """Lance-drones sur camion blindé.
Il fabrique lui-même quatre munitions rôdeuses qui tournent au-dessus de lui.
Le lot suivant arrive 15 secondes après la destruction de la dernière munition.""",
"Clic droit sur un blindé ennemi pour envoyer une munition. Gardez-le derrière vos chars et à l'abri de la DCA."),
"RODEUSE": ("Munition rôdeuse", """Petit drone explosif à ailes en X, lancé par le Mukhar.
Il tourne au-dessus du champ de bataille, puis plonge sur un véhicule.
Usage unique.""",
"Laissez-les en escorte au repos : elles attaquent ce qui passe à portée du Mukhar."),
"AGNAR": ("Agnar", """Lanceur de missiles de croisière.
Portée extrême, missile guidé très précis et gros dégâts ; cadence lente.
Le « feu » (« agni ») de l'Ananthanie tombe toujours au bon endroit.""",
"Il lui faut des yeux : Lunkar, Nayrath, drones. Tirez, puis changez de position. Prioritaires : défenses et bâtiments."),
"AMBARKESH": ("Ambarkesh", """Défense antiaérienne mobile : canons rapides et missiles sol-air.
La « lance du ciel » accompagne chaque colonne blindée.
Elle abat aussi les drones ennemis.""",
"Une ou deux par colonne suffisent à décourager hélicoptères et drones. Gardez-la derrière les chars."),
"NAYRATH": ("Nayrath", """Poste de commandement mobile.
Radar, très grande vision, détection des unités furtives.
« L'œil » (« nayra ») de l'état-major, au plus près du front.""",
"Suivez l'offensive avec lui : il donne la mini-carte et les yeux de l'Agnar. Sans arme : protégez-le."),
"HAUT.COMMANDEMENT": ("Haut Commandement", """Siège de l'état-major ananthanien.
Un seul exemplaire : tour de verre en coin, héliport, radars.
C'est ici que se prépare la bataille de sept minutes.""",
"Construisez-le pour débloquer le Nayrath. Il portera les pouvoirs de soutien et la jauge d'offensive."),
"FABRIQUE.DRONES": ("Fabrique de drones", """Hall de production de drones et piste d'essai.
Débloque le Nayrak, le Mukhar et la Ruche.
Les unités à drones y sont produites 15 % plus vite.""",
"À construire tôt : les drones sont le domaine principal de l'Ananthanie."),
"INSTITUT": ("Institut d'innovation", """Le centre de recherche ananthanien, sous son dôme d'observation.
L'innovation est la valeur fondamentale du pays.
Débloque le Karnvasha, l'Agnar et le Lunkar.""",
"Un seul exemplaire, mais indispensable pour la fin de partie : protégez-le."),
"KHESHKARN": ("Kheshkarn", """Tourelle de missiles antichars, salve de deux missiles.
Portée de 7,5 cases.
La défense d'usure : chaque char qui approche paie le prix.""",
"Placez-les en profondeur sur les axes des blindés, appuyées par des Ruches contre l'infanterie et l'aviation."),
"BATTERIE.AMBAR": ("Batterie Ambar", """Batterie de missiles sol-air à très longue portée (12 cases).
Lanceur vertical à six conteneurs.
Elle interdit le ciel au-dessus de la base.""",
"Deux batteries couvrent une base entière. Elles ne tirent pas au sol."),
"RUCHE": ("Ruche", """Ruche de drones intercepteurs.
Elle lance des salves de trois drones explosifs sur tout ce qui approche, au sol comme dans les airs.
La défense la plus polyvalente de l'Ananthanie.""",
"Complète bien le Kheshkarn : elle traite hélicoptères, drones et véhicules légers. Faible contre les chars lourds."),
}
