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

## Installation

Prérequis (Linux) : `git`, `make`, `curl`, `unzip` et le [SDK .NET 6](https://dotnet.microsoft.com/download/dotnet/6.0). Avec seulement .NET 8, exporter `DOTNET_ROLL_FORWARD=LatestMajor`.

```sh
git clone https://github.com/elielbarbucovantes-source/pays-fictifs-openra-red-alert.git
cd pays-fictifs-openra-red-alert
make              # télécharge le moteur OpenRA (release-20231010) et compile le mod
./launch-game.sh
```

Sous Windows : `make.cmd` puis `launch-game.cmd`.

Sous macOS : installer le [SDK .NET 6](https://dotnet.microsoft.com/download/dotnet/6.0), puis les mêmes commandes que sous Linux dans le Terminal. Si seul un .NET plus récent est installé, lancer avec `DOTNET_ROLL_FORWARD=LatestMajor ./launch-game.sh`.

Sans git : bouton vert « Code » → « Download ZIP » sur la page GitHub, décompresser, puis `make` et `./launch-game.sh` dans le dossier.

Au premier lancement, le jeu propose de télécharger les fichiers d'origine de Red Alert (version gratuite de 2008) : choisir « Quick Install ».

## Organisation

| Chemin | Rôle |
| --- | --- |
| `mods/fictifs/common/` | Règles partagées : factions, désignation laser, mandat présidentiel, IA |
| `mods/fictifs/elielistan/` | Unités, armes, pouvoirs et sprites de l'Elielistan |
| `mods/fictifs/rubenie/` | Unités, armes, défenses et sprites de la Rubénie |
| `mods/fictifs/australouis/` | Aviation, marine, forces terrestres, bâtiments et pouvoirs de l'Australouis |
| `OpenRA.Mods.Fictifs/` | Code C# : Aguila Gate et Opération Taupe, Aguila Bridge, ordres prioritaires, vulnérabilité après largage, Tunnelier, Mobilisation défensive, Supériorité aérienne, écopage et brouillard du Cormoran, îles du Mothership, révolution, Radar blink |
| `tools/` | Scripts Python qui génèrent drapeaux, icônes et sprites |

## Crédits

- Mod : Leile.
- Sprites des drones, du pont et de plusieurs véhicules (Leclerc, PzH, M777, Katioucha, obusier, Kouznetsov, dépôt « pionnier » ; Su-33, F-22, A-10, Battle Fortress, destroyer et sous-marin de la carte WW3) repris du mod « Mod moderne » (ratc) de Leile.
- Sprites du tireur d'élite, du B-2, du bombardier tactique, du Kirov et des bombes : carte « Europe: WW3 » de Trump, H, Therapist, Leile, Ruben et d'autres ; merci à Frenzy, Widow, Pinkthoth, SirCake, MedalMonkey, Inq8, Zypres et bien d'autres pour les graphismes, le code et l'aide.
- Basé sur l'[OpenRA Mod SDK](https://github.com/OpenRA/OpenRAModSDK).

## Licence

Le moteur OpenRA, les scripts du SDK et le code C# du mod sont sous licence [GPLv3](COPYING).
