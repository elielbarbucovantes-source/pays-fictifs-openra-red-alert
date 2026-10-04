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
"BASE.ANANTHANIE": ("Base aérienne ananthanienne", """Aérodrome de l'aviation ananthanienne.
Elle produit, répare et réarme avions, drones Ulkar et hélicoptères Kheshar.
L'aviation est le cerveau et le bras armé de l'armée.""",
"Construisez-en tôt : l'aviation est la force principale de l'Ananthanie."),
"LUNKRAVYN": ("Lunkravyn", """Chasseur furtif multirôle, emblème de l'Ananthanie.
Invisible tant qu'il ne tire pas ; missiles air-air et bombes guidées.
Le « lynx volant » : on ne le voit qu'au moment où il frappe.""",
"Frappez les cibles prioritaires (défenses AA, artillerie, chars lourds), puis rentrez vous réarmer. Les unités anti-furtives le repèrent."),
"VIDRAVYN": ("Vidravyn", """Intercepteur léger et rapide, produit en nombre.
Missiles air-air seulement.
L'« éclair volant » qui tient le ciel pendant que les autres frappent.""",
"Laissez-les en position défensive au-dessus de la base ou de l'offensive : ils engagent seuls les avions et drones ennemis."),
"AGNIVYN": ("Agnivyn", """Bombardier d'attaque à aile volante.
Missiles de croisière légers tirés à 10 cases, hors de portée de la plupart des défenses.
Le « feu volant ».""",
"Visez les défenses et bâtiments depuis la limite de portée. Évitez les SAM à longue portée et escortez-le de Vidravyn."),
"ULKAR": ("Ulkar", """Drone armé longue endurance, de la famille Reaper.
Lent, vole haut, grande vision, six missiles antichars.
Bon marché : la quantité au service de la qualité.""",
"Envoyez-les par trois ou quatre chasser les blindés isolés. Fragiles face aux systèmes anti-drone et aux intercepteurs."),
"KHESHAR": ("Kheshar", """Hélicoptère d'attaque en tandem.
Canon de 30 mm et quatre missiles antichars.
La « lance » de l'aviation légère, au plus près du sol.""",
"Appui rapproché des offensives : missiles sur les chars, canon sur l'infanterie. Craint la DCA mobile."),
"NAYRA.AMBAR": ("Nayra-Ambar", """Avion radar, « l'œil du ciel ».
Très grande vision, détection des unités furtives, aucune arme.
Les avions ananthaniens et alliés proches gagnent 20 % de portée.""",
"Gardez-le derrière la zone de combat, protégé par des Vidravyn : il voit tout et allonge la portée de votre aviation."),
"CHANTIER.ANA": ("Chantier naval ananthanien", """Chantier naval de la grande façade maritime ananthanienne.
Produit et répare patrouilleurs, frégates, destroyers, sous-marins et navires amphibies.
La supériorité navale fait partie de la doctrine.""",
"Construisez-le dès que la carte a de l'eau : la flotte ouvre la côte aux offensives."),
"VASHA": ("Vasha", """Patrouilleur rapide lance-drones.
Salves de drones explosifs contre les navires et la côte, grenades anti-sous-marines.
La « tempête » des eaux côtières.""",
"Chassez les navires légers et harcelez la côte. Restez loin des destroyers et de l'aviation."),
"AMBARKARN": ("Ambarkarn", """Frégate de défense aérienne.
Missiles sol-air à 11 cases, canon, grenades anti-sous-marines.
Le « rempart du ciel » de la flotte.""",
"Toujours une Ambarkarn par groupe naval : elle protège l'Agnikhesh des avions et repère les sous-marins."),
"AGNIKHESH": ("Agnikhesh", """Destroyer lance-missiles furtif.
Salves de missiles de croisière à 17 cases, contre la terre comme contre les navires.
La « lance de feu » : la flotte frappe loin à l'intérieur des terres.""",
"Restez au large, guidé par le Nayra-Ambar ou un Lunkar, et détruisez défenses et bâtiments côtiers. À escorter."),
"ULMAR": ("Ulmar", """Sous-marin d'attaque moderne, rapide et silencieux.
Invisible en plongée, torpilles lourdes à longue portée.
La « vague » qui frappe sans prévenir.""",
"Embusquez-le sur les routes des navires ennemis. Fuyez frégates et patrouilleurs qui détectent les sous-marins."),
"RATHAMBAR": ("Rathambar", """Navire amphibie : huit places, chars comme fantassins.
Il débarque sur les plages et la côte.
Indispensable aux offensives d'une puissance maritime.""",
"Chargez Vidra, Lunkra et Ratha, débarquez sous la couverture de la flotte, et frappez là où l'ennemi ne vous attend pas."),
"VIDRAKARN": ("Vidrakarn", """Char expérimental à canon électromagnétique.
Son projectile traverse tout ce qui se trouve sur sa trajectoire, sur 9 cases.
« Le rempart de l'éclair » : une seule ligne de tir suffit à briser une colonne.""",
"Placez-le face à l'axe d'arrivée de l'ennemi pour aligner plusieurs cibles. Protégez-le de l'infanterie et des avions."),
"ANANTA": ("Ananta", """Véhicule expérimental à laser de défense.
Il abat avions et drones, détruit en vol les missiles ennemis et tire aussi au sol.
« L'infini » : le bouclier de l'armée ananthanienne.""",
"Accompagnez vos colonnes : il les protège des missiles antichars, des drones et des frappes de missiles."),
"AMBAROTH": ("Ambaroth", """Forteresse volante expérimentale, très lente et très blindée.
Elle lâche des vagues de six drones explosifs et se défend avec des canons antiaériens.
La « porte du ciel » : quand elle apparaît, la bataille est presque finie.""",
"Escortez-la de Vidravyn et d'Ananta. Elle avance lentement : préparez son arrivée."),
"ESSAIM.DRONE": ("Drone de l'Essaim", """Drone kamikaze lancé par le pouvoir Essaim.
Il plonge sur les véhicules, fantassins et défenses.
Il se détruit au bout d'une minute s'il ne trouve rien.""",
"Pas de micro-gestion : il attaque seul ce qu'il trouve dans la zone."),
}
