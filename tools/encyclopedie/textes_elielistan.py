# Elielistan : clé de l'acteur -> (nom, lore sur 3 lignes, stratégie)
TEXTES = {
"AGUILA": ("Fusilier Aguila", """Le fantassin de base de l'armée elielistanaise, recruté dans les hauts plateaux.
Léger, rapide, il porte un fusil un peu plus précis que celui des armées étrangères.
Sa devise : « Tire, bouge, recommence. »""",
"Utilisez-le en petits groupes mobiles pour harceler et occuper le terrain. Abritez-le dans une tranchée dès qu'il doit tenir une position."),
"CAZADOR": ("Cazador", """Chasseur de chars d'embuscade, formé à rester immobile des heures dans les rochers.
Invisible à l'arrêt, il ne se dévoile qu'au moment de tirer sa roquette.
Les équipages ennemis l'appellent « le rocher qui mord ».""",
"Postez-le sur les routes d'approche avant l'arrivée des blindés ennemis, puis déplacez-le après chaque salve."),
"EXPLORADOR": ("Explorador (Recon Aguila)", """Éclaireur d'élite, les yeux de toute l'armée elielistanaise.
Il voit très loin, repère les furtifs et illumine les cibles au laser.
Il ne combat jamais : s'il tire, c'est qu'il est déjà découvert.""",
"Placez-le en avant, immobile, pour guider le Trueno et les frappes aériennes. Chaque cible qu'il désigne subit +25 % de dégâts."),
"NATHAN": ("Nathan", """Infiltré d'élite dont le vrai nom n'apparaît dans aucun registre.
Invisible à l'arrêt, il marque un bâtiment au laser et un avion le rase dans la seconde.
On ne le voit qu'une fois : au moment où la cible s'effondre.""",
"Infiltrez-le près de la base ennemie, laissez-le immobile, puis désignez la cible la plus précieuse (chantier de construction, radar, centrale). Repliez-le aussitôt : tirer le dévoile, et les chiens le trouvent."),
"GAVILAN": ("Gavilán", """Véhicule de reconnaissance ultra-rapide des patrouilles frontalières.
Grande vision, mitrailleuse, et trois fantassins à bord.
Le Cóndor peut le déposer derrière les lignes ennemies.""",
"Explorez la carte et harcelez l'infanterie isolée. Emmenez un Cazador ou un Explorador pour une embuscade à distance."),
"HALCON": ("Halcón AT", """Chasseur de blindés armé d'un missile antichar filoguidé.
Invisible à l'arrêt, il attend que la colonne ennemie passe à portée.
Un tir, un char de moins, et il a déjà changé de colline.""",
"Déployez-le en embuscade sur les flancs, jamais en première ligne. Le Cóndor permet de le repositionner très vite."),
"JAGUAR": ("Jaguar", """Char moyen rapide au canon de 90 mm à cadence élevée.
Moins blindé que ses rivaux, il compense par la vitesse et le volume de feu.
Il ouvre le chemin avant l'arrivée des chars lourds.""",
"Char de manœuvre : attaquez les flancs et les unités de soutien, repliez-vous avant l'arrivée des chars lourds ennemis."),
"VIGIA": ("Vigía", """Défense antiaérienne mobile de l'armée elielistanaise.
Ses missiles sol-air protègent colonnes et bases avancées.
Transportable par Cóndor, elle suit les raids jusqu'au bout.""",
"Accompagnez chaque groupe de raid d'une ou deux Vigía : sans elles, la moindre aviation ennemie décime vos colonnes."),
"TRUENO": ("Trueno", """Artillerie « tirer puis déménager » : trois obus à 14 cases, puis la fuite.
Elle voit à peine plus loin que son canon : il lui faut des yeux.
Après chaque salve, son moteur donne tout ce qu'il a.""",
"Associez-la à un Explorador ou à un drone. Tirez, puis changez de position avant la contre-batterie ennemie. Le Pelícano ou l'Aguila Carrier peuvent l'emmener loin derrière les lignes."),
"CARRO.AGUILA": ("Carro Aguila", """Char de bataille principal, armé d'un canon de 120 mm.
Plus rapide qu'un char lourd, moins blindé que lui.
Le compromis elielistanais : frapper fort sans jamais s'immobiliser.""",
"Formez le cœur de vos colonnes blindées. Transportez-le en Aguila Carrier ou en Pelícano pour frapper loin de votre base."),
"LANZADOR": ("Lanzador Aguila", """Artillerie automotrice sur camion, héritière du canon César.
Trois obus en succession rapide, puis un long rechargement.
Assez rapide pour suivre les raids et disparaître après la salve.""",
"Frappez les défenses et l'infanterie ennemies à distance, puis profitez de la doctrine de décrochage pour vous replier."),
"AGUILA.NUCLEAR": ("Aguila Nuclear", """Char lourd propulsé par un réacteur nucléaire compact : le paradoxe elielistanais.
Blindage de char lourd, canon de 125 mm et vitesse de char léger.
S'il est détruit, son réacteur explose et contamine la zone quelques secondes.""",
"Arrivez très vite, détruisez la cible, repartez. Ne le laissez jamais mourir au milieu de vos propres troupes : l'explosion et la zone contaminée touchent tout le monde."),
"AGUILA.CARRIER": ("Aguila Carrier", """Énorme camion blindé capable d'emporter cinq chars ou pièces d'artillerie.
Aucune arme : il transforme cinq chars en force de raid mobile.
S'il est détruit en route, les véhicules à bord sont perdus.""",
"Chargez des Aguila Nuclear ou des Carro Aguila, passez par l'Aguila Gate et débarquez-les en territoire ennemi. Escortez-le toujours."),
"CONDOR": ("Cóndor", """Hélicoptère de transport lourd, capable de soulever un véhicule entier.
Il dépose Gavilán, Halcón, Jaguar, Vigía ou Trueno n'importe où.
Le véhicule largué reste sonné quelques secondes.""",
"Utilisez-le pour repositionner l'artillerie et les chasseurs de chars, ou pour déposer des Jaguar derrière les lignes. Évitez les zones couvertes par la DCA."),
"BLACKHAWK": ("Black Hawk", """Hélicoptère de combat à deux mitrailleuses, version elielistanaise du Black Hawk.
Sa soute accueille un char (Jaguar, Halcón, Carro Aguila, Aguila Nuclear, Trueno, Lanzador) ou trois fantassins.
Il se pose pour embarquer et débarquer.""",
"Déposez un char ou une équipe de trois fantassins derrière les lignes, puis couvrez le débarquement avec ses mitrailleuses. Il reste fragile face à la DCA et aux chasseurs."),
"AVISPA": ("Avispa", """Drone suicide piloté en vue subjective, le cauchemar des fantassins.
Il fonce sur un soldat et le tue d'un seul coup, en se détruisant.
Minuscule et fragile : un seul tir antiaérien suffit à l'abattre.""",
"Envoyez-les par deux ou trois sur l'infanterie ennemie, surtout les unités d'élite. Contournez la défense antiaérienne : ils n'y survivent pas."),
"PICO": ("Pico", """Drone d'attaque légère armé d'une mitrailleuse.
Peu coûteux, il harcèle l'infanterie et les véhicules légers.
Les soldats ennemis finissent par craindre le bourdonnement.""",
"Harcelez les camions de minerai et l'infanterie isolée. Gardez-le loin de la défense antiaérienne."),
"GARRA": ("Garra", """Drone antichar qui emporte deux missiles puissants.
Après ses deux tirs, il rentre se réarmer à l'héliport.
Deux missiles, souvent un char de moins.""",
"Visez les chars isolés ou endommagés, puis renvoyez-le se réarmer. En groupe, il arrête une percée blindée."),
"HARPIA": ("Harpía", """Avion d'attaque à décollage vertical, armé de missiles air-sol.
Il peut apponter sur le porte-avions Aguila, qui le réarme à bord.
Sa silhouette annonce toujours le débarquement qui va suivre.""",
"Opérez depuis le porte-avions pour frapper la côte ennemie. À court de munitions, il rentre seul au porte-avions ou à l'aérodrome le plus proche."),
"PELICANO": ("Pelícano", """Avion de transport stratégique, le plus gros appareil elielistanais.
Il largue en parachute quinze fantassins ou cinq chars (ou pièces d'artillerie) n'importe où.
Très cher et sans défense : un seul chasseur peut tout ruiner.""",
"Posé sur une piste ou sur un terrain dégagé, faites-y monter votre force, puis maintenez Ctrl et cliquez sur la zone (glisser pour l'axe) pour la larguer. Choisissez une zone dégagée et sans DCA ; les chars restent vulnérables 4 secondes après l'atterrissage."),
"BOMBARDIER.TACTIQUE": ("Bombardier tactique", """Bombardier moyen qui dépose un tapis de bombes incendiaires.
Redoutable contre l'infanterie et les véhicules légers.
Repris de l'arsenal de la Troisième Guerre mondiale.""",
"Visez les concentrations d'infanterie et les bases avancées. Escortez-le ou frappez là où la DCA a été détruite."),
"B2.SPIRIT": ("B-2 Spirit", """Bombardier furtif lourd, invisible au radar ennemi.
Deux passages de six bombes incendiaires lourdes.
Deux appareils au maximum : chaque perte coûte très cher.""",
"Frappez les bâtiments clés loin derrière les lignes. Il est invisible au radar, pas aux yeux : évitez les zones couvertes par la DCA."),
"KIROV": ("Kirov", """Dirigeable blindé, lent comme une procession, chargé de bombes énormes.
Il recharge ses bombes en vol et encaisse des coups terribles.
Quand son ombre recouvre une base, il est souvent trop tard.""",
"Escortez-le de Vigía et de chasseurs, et visez une base dont la DCA est affaiblie. Trois au maximum."),
"PALACIO": ("Palacio Presidencial", """Siège du pouvoir elielistanais, d'où le président commande les opérations.
Il porte les pouvoirs de l'Elielistan : Ojo del Aguila, Aguila Bridge, Aguila Gate, ordres prioritaires.
Un seul exemplaire : le perdre, c'est perdre la voix du président.""",
"Construisez-le dès que le radar est en place et protégez-le bien : sans lui, plus de pouvoirs ni d'antennes de gouvernement provisoire."),
"AERODROMO": ("Aeródromo", """Aérodrome elielistanais : il produit, répare et réarme les avions.
Le Pelícano s'y pose pour embarquer ses troupes.
Le point de départ de toute projection aérienne.""",
"Un avion posé occupe l'aérodrome ; les Pelícanos en trop se posent sur le terrain dégagé à côté. Placez-le à l'abri, au fond de la base."),
"ANTENA": ("Antenne de gouvernement provisoire", """Un drapeau planté dans le territoire conquis.
Construite n'importe où, elle ouvre une petite zone où bâtir une base avancée.
Fragile et visible de l'ennemi : ce n'est pas une forteresse.""",
"Suivez votre armée : antenne, puis réparation, production et défenses légères autour, puis nouvelle offensive. Trois au maximum, protégez-les."),
"TRANCHEE": ("Tranchée (est-ouest)", """Tranchée légère creusée pour tenir une position conquise.
Cinq fantassins y tirent à couvert et en ressortent si elle est détruite.
Moins solide qu'une fortification : elle se construit et s'abandonne vite.""",
"Garnissez-la de Fusiliers Aguila ou de Cazador pour défendre une base avancée autour d'une antenne."),
"TRANCHEEV": ("Tranchée (nord-sud)", """Même tranchée légère, orientée nord-sud.
Cinq fantassins y tirent à couvert.
Choisissez l'orientation face à la menace.""",
"Combinez les deux orientations pour fermer un passage."),
"CENTINELA": ("Centinela", """Escorteur rapide, gardien du groupe naval elielistanais.
Missiles antiaériens, canon léger et grenades anti-sous-marines.
Il repère les sous-marins avant qu'ils ne frappent.""",
"Placez-en un ou deux autour de chaque porte-avions et de chaque frégate : ils couvrent l'air et le dessous de l'eau."),
"TORMENTA": ("Tormenta", """Frégate lance-missiles, la frappe à distance de la marine.
Ses missiles antinavires portent très loin et frappent aussi la côte.
Aucune défense antiaérienne : elle dépend de son escorte.""",
"Restez à distance maximale des navires ennemis et escortez-la d'une Centinela."),
"TIBURON": ("Tiburón", """Sous-marin d'attaque, invisible en plongée.
Ses torpilles lourdes visent les grands bâtiments : porte-avions et croiseurs.
La terreur des flottes lentes.""",
"Chassez les gros navires ennemis en embuscade. Évitez les escorteurs et destroyers, qui le détectent."),
"PORTA.AGUILA": ("Porte-avions Aguila", """Porte-avions amphibie, plus polyvalent que tout ce que l'ennemi aligne.
Cinq Harpía à bord, et quinze fantassins ou cinq chars ou canons à débarquer.
Frappe aérienne et projection terrestre au même endroit.""",
"Approchez de la côte ennemie sous escorte, faites décoller les Harpía, puis collez-vous au rivage pour débarquer les troupes. La touche F fait les deux."),
}
