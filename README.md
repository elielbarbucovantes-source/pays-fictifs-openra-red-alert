# Pays fictifs — mod pour OpenRA Red Alert

Un mod pour [OpenRA](https://www.openra.net) (Red Alert) qui ajoute des pays fictifs jouables, chacun avec sa doctrine, ses unités et ses pouvoirs.

## Pays

### 🇪🇱 Elielistan : « Tu combats l'endroit où elle était il y a dix secondes »

Faction européenne très rapide, fondée sur le hit-and-run : repérer, frapper, disparaître, recommencer ailleurs. Son emblème est l'Aguila, un aigle aux pointes d'ailes rouge vif.

- **Doctrine du décrochage** : après avoir tiré, chaque unité gagne de la vitesse pendant 4 secondes. En contrepartie, elle est moins blindée que les unités équivalentes.
- **Infanterie** : Fusilier Aguila, Cazador (antichar invisible à l'arrêt), Explorador (éclaireur, désignation laser).
- **Véhicules** : Gavilán (reconnaissance), Halcón AT (chasseur de chars furtif), Jaguar (char moyen rapide), Vigía (DCA), Trueno (artillerie « tirer puis déménager »), Carro Aguila (char de bataille principal), Lanzador (missile longue portée).
- **Aviation** : Cóndor (hélicoptère qui accroche et largue des véhicules, vulnérables 4 s après le largage), drones Ojo (reconnaissance), Pico (attaque légère) et Garra (antichar).
- **Palacio Presidencial** et pouvoirs de soutien :
  - **Ordre prioritaire** : bonus de vitesse, de feu et de cadence sur une zone. 10 ordres par quinquennat (15 minutes).
  - **Ojo del Aguila** : révèle une zone, détecte le camouflage et désigne tous les ennemis.
  - **Aguila Bridge** : pont temporaire au-dessus de n'importe quel obstacle, utilisable par tout le monde.
  - **Aguila Gate** : tunnel temporaire entre deux points de la carte.
  - **Frappe Aguila** (héliport) : trois avions Rayo bombardent puis repartent.

L'Elielistan construit la base alliée de Red Alert (sans la chronosphère).

### Rubénie : « Si ça tient, touche à rien »

Faction défensive au drapeau blanc et vert frappé d'une montagne. Ses unités sont plus solides et plus lentes que celles de l'Elielistan, un peu plus chères, et gagnent les combats qui durent : défendre, contre-attaquer, se replier, réparer, recommencer.

- **Infanterie** : Fusilier rubénien, Grenadier (zone d'effet), Chasseur antichar (longue portée), Éclaireur (camouflé, grande vision, détection), Second Galactique (officier d'élite unique : aura de +15 % de dégâts et −15 % de dégâts reçus, Tesla, charges de démolition).
- **Chars** : « Si ça tient, touche à rien » (char de bataille qui se répare seul hors combat), Char de contre-attaque (rapide, léger), Char lourd de forteresse (blindage extrême, très lent).
- **Véhicules et artillerie** : véhicule de reconnaissance, véhicule antiaérien, lance-missiles mobile, canon automoteur, artillerie lourde (18 cases), lance-roquettes multiple.
- **Défenses** : tranchées, bunker à 5 places, casemate antichar, tourelle AA, SAM, artillerie côtière.
- **Aviation** (base aérienne) : chasseur polyvalent (air-air, air-sol, antinavire ; seul à apponter sur le porte-avions), intercepteur, avion d'attaque au sol, avion de reconnaissance.
- **Marine** (chantier naval rubénien) : escorteur (bouclier antiaérien du groupe), frégate lance-missiles, sous-marin, porte-avions (3 chasseurs polyvalents).
- **Logistique** : le centre logistique, le QG, le chantier de construction, le véhicule logistique et le Bastion roulant ravitaillent les unités terrestres proches. Ravitaillées, elles sont les plus fortes du jeu (+10 % de dégâts, −10 % de dégâts reçus, réparation lente hors combat) ; sans ravitaillement, un peu en dessous de la moyenne (−10 % de dégâts, +10 % de dégâts reçus). Le bonus dure encore 10 secondes après la sortie du rayon. Le véhicule logistique répare aussi vite, même au combat, les unités qui l'entourent.
- **Bastion roulant** : la Battle Fortress à la rubénienne. Deux canons de 130 mm et des missiles qui ne tirent que si des fantassins sont à bord, ravitaillement embarqué (toujours ravitaillé lui-même, ravitaille les unités autour), réparation autonome.
- **Tunnelier** : livré avec 10 chasseurs antichars, il roule sous terre à mi-vitesse, invisible sauf pour les détecteurs sismiques (Éclaireur, véhicule de reconnaissance, QG, dômes radar). À l'arrêt, il émerge : 3 secondes immobile et vulnérable.
- **QG rubénien** et pouvoirs de soutien :
  - **Contre-attaque éclair** : +40 % de vitesse et −25 % de temps de rechargement pendant 20 secondes, sur une zone.
  - **Mobilisation défensive** : pendant 45 secondes, construction deux fois plus rapide, bâtiments plus blindés qui se réparent seuls.
  - **Frappe de précision** : un missile qui détruit presque n'importe quel bâtiment, sans raser les alentours.
  - **Opération Taupe** : tunnel temporaire sur la terre ferme, réservé aux unités rubéniennes.
  - **Supériorité aérienne** : quatre chasseurs patrouillent 30 secondes au-dessus d'une zone.

La Rubénie construit la base soviétique, avec sa propre base aérienne et son propre chantier naval.

L'IA sait jouer la Rubénie : elle construit ses bâtiments et défenses, produit ses unités terrestres, aériennes et navales, et utilise quatre pouvoirs du QG. Le Tunnelier, le porte-avions, l'avion de reconnaissance et l'Opération Taupe restent réservés aux joueurs humains.

### Australouis : le peuple des îles

Faction alliée d'un archipel, dont la puissance tient à l'aviation et à la marine. Son drapeau bleu océan porte une bande de vagues blanches, trois îles vertes et une étoile.

- **Base aéronavale** : produit, répare et réarme les avions australouisiens.
- **Albatros** : chasseur multirôle (un Rafale à l'australouisienne), missiles air-air et air-sol.
- **Warthog** : l'A-10 de la carte WW3, canon de 30 mm et missiles antichars.
- **Manta** : bombardier lourd sur le modèle du B-2 de la carte WW3, en moins bien (visible au radar, un seul passage de bombes).
- **Baleine** : hélicoptère de transport lourd (10 fantassins), à la place du Chinook.
- **Moustique** : drone kamikaze antichar, très fragile ; il en faut trois pour détruire un char léger.
- **Cormoran** : hydravion bombardier d'eau. Il écope en survolant l'eau, puis pulvérise un brouillard (Ctrl + clic) : les ennemis perdent tout ce qu'ils avaient découvert dans la zone, qui redevient noire.

- **Marine** : les navires alliés, plus le **L-0U15** (destroyer faible dont le sonar détecte les sous-marins à 18 cases), le **sous-marin ICBM** de la carte WW3 (missiles V3 à 24 cases) et le **Mothership**, navire-mère géant presque sans armes qui fait sortir de l'océan des îles de 8×8 cases (2 min 50, sans bouger). Un chantier de construction débarqué sur l'île peut y bâtir une base.
- **Chantier naval avancé** (centre technique, 2 000 $, 3 au maximum) : seul à produire le sous-marin ICBM et le Mothership.
- **Centrale d'enrichissement** (centre technique, 2 500 $, 1 au maximum) : +5 % de revenus du minerai tant qu'elle est debout.
- **Infanterie** : le jeu soviétique (fusilier, grenadier, lance-roquettes, lance-flammes, ingénieur, voleur), plus le **Révolutionnaire** : quand sa révolution est prête (5 minutes), toutes les unités et tous les bâtiments ennemis à 5 cases passent dans son camp.
- **Chars** : char léger et char moyen alliés, plus le **Cheaper**, char produit en masse, pas cher et faible.
- **Pouvoirs** : **Raffinerie boost** (centrale d'enrichissement + silo : +20 % de revenus du minerai pendant 40 secondes) et **Radar blink** (chantier naval avancé : pendant 1 minute, vision de tous les sous-marins de la carte, sous-marins ennemis révélés).

L'IA ne sait pas encore utiliser les unités et pouvoirs propres à l'Australouis.

### Ananthanie : « L'Ananthanie gagne une bataille en 7 minutes »

Superpuissance technologique au drapeau vert citron, liseré de vert pomme et de blanc, frappé d'une rose des vents en étoile. Son animal est le lynx. Sa doctrine est le blitzkrieg en attaque et l'usure en défense : quantité et qualité, blindés modernes, drones explosifs et missiles. Son point faible, ce sont les montagnes : ses véhicules perdent 20 % de vitesse en terrain accidenté.

- **Infanterie** : Thal (fusilier formé très vite), Khesh-Thal (missiles antichars et antiaériens), Nayrak (opérateur de drones explosifs), Lunkar (éclaireur camouflé qui marque ses cibles : +25 % de dégâts reçus), Karnthal (exosquelette, mitrailleuse lourde).
- **Blindés et véhicules** : Vidra (char léger très rapide), Lunkra (char de combat principal), Karnvasha (char lourd à protection active : −35 % de dégâts des missiles et roquettes), Ratha (transport de 5 fantassins), Mukhar (lance-drones : 4 munitions rôdeuses gratuites), Agnar (missiles de croisière à 16 cases), Ambarkesh (défense antiaérienne mobile), Nayrath (poste de commandement mobile).
- **Bâtiments** : Haut Commandement (pouvoirs), fabrique de drones, institut d'innovation (technologies avancées), base aérienne et chantier naval ananthaniens. Défenses : Kheshkarn (missiles antichars), Batterie Ambar (sol-air à 12 cases), Ruche (drones intercepteurs).
- **Aviation** : Lunkravyn (chasseur furtif, invisible tant qu'il ne tire pas), Vidravyn (intercepteur), Agnivyn (bombardier à missiles de croisière), Ulkar (drone armé), Kheshar (hélicoptère d'attaque), Nayra-Ambar (avion radar : +20 % de portée aux avions alliés proches).
- **Marine** : Vasha (patrouilleur lance-drones), Ambarkarn (frégate antiaérienne), Agnikhesh (destroyer lance-missiles), Ulmar (sous-marin d'attaque), Rathambar (navire amphibie).
- **Unités expérimentales** (une de chaque au maximum) : Vidrakarn (canon électromagnétique qui transperce tout ce qui est aligné), Ananta (laser qui abat avions, drones et missiles en vol), Ambaroth (forteresse volante qui lâche des vagues de drones explosifs).
- **Blitz 7** : une jauge d'offensive qui se remplit en 7 minutes, seulement hors combat, et plus vite avec des unités en réserve et des Nayrath. Déclenchée, elle donne 70 secondes de Blitz à toute l'armée (+30 % de vitesse, +25 % de cadence, +15 % de dégâts), puis 30 secondes de fatigue.
- **Pouvoirs du Haut Commandement** : Essaim (12 drones kamikazes), Salve Agni (6 missiles de croisière), Raid furtif (3 bombardiers furtifs), Œil d'Ambar (reconnaissance), Pont aérien (2 chars Vidra, 4 Thal et 2 Khesh-Thal parachutés) et Pacte de Fraternité (pouvoir de coalition : pendant 45 secondes, +15 % de dégâts et réparation pour tes unités et celles de tes alliés dans la zone).

L'Ananthanie construit la base soviétique, avec ses propres bâtiments en plus.

L'IA sait jouer l'Ananthanie : elle construit ses bâtiments et défenses, produit ses unités terrestres, aériennes et navales et ses unités expérimentales, et utilise Blitz 7 et les pouvoirs du Haut Commandement. Le Nayrath, le Nayra-Ambar, le Rathambar et l'Œil d'Ambar restent réservés aux joueurs humains.

## Installation

Prérequis (Linux) : `git`, `make`, `curl`, `unzip` et le [SDK .NET 6](https://dotnet.microsoft.com/download/dotnet/6.0). Avec seulement .NET 8, exporter `DOTNET_ROLL_FORWARD=LatestMajor`.

```sh
git clone https://github.com/elielbarbucovantes-source/pays-fictifs-openra-red-alert.git
cd pays-fictifs-openra-red-alert
make              # télécharge le moteur OpenRA (release-20231010) et compile le mod
./launch-game.sh
```

Sous Windows : `make.cmd` puis `launch-game.cmd`.

Sous macOS : une seule commande dans le Terminal installe tout (outils Apple, .NET 8, le mod dans `~/PaysFictifs`) et crée **Jouer Pays fictifs** sur le Bureau :

```sh
curl -fsSL https://raw.githubusercontent.com/elielbarbucovantes-source/pays-fictifs-openra-red-alert/main/installer-mac.sh | bash
```

Ensuite, double-cliquer sur **Jouer Pays fictifs** pour jouer.

Lancer avec `./jouer.sh` plutôt que `./launch-game.sh` (Linux et macOS) : à chaque lancement, le script récupère la dernière version publiée sur GitHub, recompile si besoin, ouvre une fois le pare-feu à ZeroTier (mot de passe administrateur demandé), puis lance le jeu.

Sans git : bouton vert « Code » → « Download ZIP » sur la page GitHub, décompresser, puis `make` et `./launch-game.sh` dans le dossier.

Au premier lancement, le jeu propose de télécharger les fichiers d'origine de Red Alert (version gratuite de 2008) : choisir « Quick Install ».

## Jouer en ligne avec un ami (ZeroTier)

1. Installer [ZeroTier](https://www.zerotier.com/download/) tous les deux et rejoindre le même réseau. Sur my.zerotier.com, cocher **Auth** pour les deux machines. Sur Mac, autoriser l'extension réseau de ZeroTier dans les Réglages Système.
2. Lancer le jeu avec `jouer.sh` (ou **Jouer Pays fictifs** sur Mac) : il se met à jour tout seul, donc les deux joueurs ont la même version.
3. L'hôte va dans **Multijoueur** → **Create**. Son adresse ZeroTier s'affiche, avec un bouton **Copier mon adresse**. Il l'envoie à l'autre joueur.
4. L'autre joueur colle l'adresse dans le champ à côté de **Rejoindre mon ami** dans l'écran Multijoueur, puis clique sur le bouton. L'adresse est mémorisée pour les fois suivantes.

Cartes générées ou personnelles : elles ne sont pas sur le Resource Center d'OpenRA, alors l'hôte les partage lui-même. Quand l'hôte choisit une telle carte, le jeu de l'autre joueur la télécharge tout seul chez lui (port 1235, ouvert par le jeu de l'hôte pendant qu'il héberge) et l'enregistre dans ses Custom Maps.

La version du mod contient le commit installé (`release-20231010+8f4e95b`, par exemple). Si les deux joueurs n'ont pas la même, la connexion est refusée avec un message qui demande de se mettre à jour, au lieu d'une désynchronisation en cours de partie. Les sauvegardes et les replays sont rangés par version : ceux d'une ancienne version n'apparaissent plus après une mise à jour (ils ne se rejoueraient de toute façon pas correctement avec des règles différentes).

## Classement Elo et grades

Chaque escarmouche ou partie en ligne terminée (avec un gagnant) compte pour le classement Elo. Tout le monde commence à 1000. Les parties contre l'IA comptent aussi : chaque IA a un Elo fixe (Turtle 900, Naval 1000, Normal 1100, Rush 1200) et ne gagne ni ne perd de points. En équipe, c'est la moyenne de l'équipe qui compte, et chaque joueur reçoit la même variation. Les 10 premières parties bougent plus vite (K = 40, puis 24). Les missions et les replays ne comptent pas.

Grades : Soldat (< 1050), Caporal (1050), Sergent (1100), Lieutenant (1175), Capitaine (1250), Commandant (1350), Colonel (1450), Général (1575), Maréchal (1700).

- **Salon** : l'encadré « Classement Elo » à droite du chat montre l'insigne, le grade, l'Elo et le bilan V/D de chaque joueur. Quand il n'y a que deux camps, il affiche aussi les chances de victoire.
- **Fin de partie** : un message dans le chat donne la variation (`Stal1n : 1000 → 1030 (+30 Elo) — Soldat`) et annonce les promotions.

L'historique est enregistré dans `fictifs-elo.json` (dossier de configuration d'OpenRA). L'Elo est recalculé à partir de cet historique. En ligne, le jeu de l'autre joueur récupère l'historique de l'hôte et lui envoie le sien (port 1235, comme pour les cartes). Les deux PC affichent donc les mêmes chiffres, même si l'un a joué seul contre l'IA entre-temps. Les joueurs sont reconnus par leur pseudo : en changer revient à repartir de zéro.

## Générateur de cartes aléatoires

Dans le sélecteur de cartes (lobby d'escarmouche ou de partie en réseau), le bouton **Générer une carte** ouvre le générateur de cartes aléatoires d'OpenRA. Il est repris du playtest-20260222 et adapté au moteur release-20231010 du mod. On y choisit le climat, la taille, le type de terrain, la forme, le nombre de joueurs, la symétrie, les ressources, les bâtiments technologiques, les zones d'expansion, les villages civils et les routes. **Nouvelle carte** tire une autre graine.

Différence avec le playtest : quand on clique sur **Jouer cette carte**, la carte est enregistrée comme un fichier `.oramap` dans le dossier des cartes de l'utilisateur (`maps/ra/release-20231010/aleatoire-…oramap`). Elle apparaît ensuite dans l'onglet **Custom Maps**, et on peut la rejouer, la partager ou l'ouvrir dans l'éditeur comme n'importe quelle autre carte.

Pour tester sans lancer le jeu : `./utility.sh --generate-random-maps 20 /tmp/cartes` génère 20 cartes avec des réglages au hasard, puis les enregistre avec leur aperçu.

## Organisation

| Chemin | Rôle |
| --- | --- |
| `mods/fictifs/common/` | Règles partagées : factions, désignation laser, mandat présidentiel, IA |
| `mods/fictifs/elielistan/` | Unités, armes, pouvoirs et sprites de l'Elielistan |
| `mods/fictifs/rubenie/` | Unités, armes, défenses et sprites de la Rubénie |
| `mods/fictifs/australouis/` | Aviation, marine, forces terrestres, bâtiments et pouvoirs de l'Australouis |
| `OpenRA.Mods.Fictifs/` | Code C# : Aguila Gate et Opération Taupe, Aguila Bridge, ordres prioritaires, vulnérabilité après largage, Tunnelier, Mobilisation défensive, Supériorité aérienne, écopage et brouillard du Cormoran, îles du Mothership, révolution, Radar blink |
| `mods/fictifs/mapgen/` | Réglages du générateur de cartes aléatoires, pinceaux de tuiles des tilesets, sélecteur de cartes et panneau du générateur |
| `OpenRA.Mods.Fictifs/MapGen/` | Générateur de cartes aléatoires rétroporté d'OpenRA (playtest-20260222) |
| `tools/` | Scripts Python qui génèrent drapeaux, icônes et sprites (`sprites_hd.py` : sprites « HD » rendus en 3D à partir des modèles de `tools/modeles/`, avec le moteur `rendu3d.py`) |

## Crédits

- Mod : Leile.
- Les unités, fantassins, défenses et bâtiments propres aux trois pays sont modélisés en 3D et rendus en sprites au format Red Alert par `tools/sprites_hd.py` (Python et Pillow uniquement) : `python3 tools/sprites_hd.py [nom …]`.
- Sprites repris pour les unités qui n'ont pas encore leur modèle 3D : drones, pont et plusieurs véhicules (Leclerc, PzH, M777, Katioucha, obusier, Kouznetsov, dépôt « pionnier » ; Su-33, F-22, A-10, Battle Fortress, destroyer et sous-marin de la carte WW3) repris du mod « Mod moderne » (ratc) de Leile.
- Sprites du tireur d'élite, du B-2, du bombardier tactique, du Kirov et des bombes : carte « Europe: WW3 » de Trump, H, Therapist, Leile, Ruben et d'autres ; merci à Frenzy, Widow, Pinkthoth, SirCake, MedalMonkey, Inq8, Zypres et bien d'autres pour les graphismes, le code et l'aide.
- Générateur de cartes aléatoires : code et données d'[OpenRA](https://github.com/OpenRA/OpenRA) (playtest-20260222, licence GPL v3) par les développeurs et contributeurs d'OpenRA, adaptés au moteur release-20231010.
- Régions du monde réel du générateur (`mods/fictifs/mapgen/monde/`, produites par `tools/monde.py`) : côtes et lacs de [Natural Earth](https://www.naturalearthdata.com/) (domaine public) ; relief des [Terrain Tiles](https://registry.opendata.aws/terrain-tiles/) d'AWS (SRTM, GMTED2010, ETOPO1 et autres sources publiques, voir leur page d'attribution).
- Basé sur l'[OpenRA Mod SDK](https://github.com/OpenRA/OpenRAModSDK).

## Licence

Le moteur OpenRA, les scripts du SDK et le code C# du mod sont sous licence [GPLv3](COPYING).
