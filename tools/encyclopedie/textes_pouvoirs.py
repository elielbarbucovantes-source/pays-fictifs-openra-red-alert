# Pouvoirs de soutien : nom dans les règles -> (nom, lore sur 3 lignes, stratégie)
TEXTES = {
"Ordre prioritaire": ("Ordre prioritaire", """Le président impose une priorité absolue sur une zone.
Pendant 30 secondes, les unités elielistanaises de la zone gagnent +25 % de vitesse, de puissance de feu et de cadence.
Dix ordres par quinquennat (15 minutes de jeu) : ils sont comptés.""",
"Gardez-les pour les moments décisifs : l'assaut final, ou sauver un raid qui tourne mal."),
"Ojo del Aguila": ("Ojo del Aguila", """Reconnaissance totale d'une zone pendant 30 secondes.
Les furtifs sont révélés et tous les ennemis de la zone sont désignés (+25 % de dégâts reçus).
Trouver la faiblesse, première étape de toute opération elielistanaise.""",
"Lancez-le juste avant une frappe ou un raid, sur la zone que vous allez attaquer."),
"Aguila Bridge": ("Aguila Bridge", """Pont temporaire de 10 cases au plus, posé au-dessus de n'importe quel obstacle.
Il dure 90 secondes et peut être détruit.
Attention : tout le monde peut l'emprunter, ennemis compris.""",
"Franchissez une rivière ou un bras de mer là où l'ennemi ne vous attend pas, puis laissez le pont expirer derrière vous."),
"Aguila Gate": ("Aguila Gate", """Tunnel temporaire entre deux zones explorées de la carte.
Vos unités terrestres entrent par une bouche et ressortent par l'autre, dans les deux sens ; en sortant, elles s'écartent d'elles-mêmes de la bouche pour ne pas repartir aussitôt.
Douze passages, 60 secondes : l'aller, puis la retraite.""",
"Ouvrez-le avec un Aguila Carrier chargé de chars : cinq chars passent en un seul passage."),
"Frappe Aguila": ("Frappe Aguila", """Trois avions d'attaque Rayo traversent la zone à très grande vitesse.
Ils bombardent et repartent aussitôt.
Frapper puis disparaître, version aérienne.""",
"Visez les défenses ou les groupes d'artillerie qui bloquent votre raid."),
"Contre-attaque éclair": ("Contre-attaque éclair", """Les unités terrestres rubéniennes de la zone (5 cases) gagnent +40 % de vitesse et −25 % de temps de rechargement pendant 20 secondes.
Le signal de la sortie.
Défendre, puis frapper au moment où l'ennemi s'essouffle.""",
"Déclenchez-la quand l'assaut ennemi se brise sur vos défenses, sur vos chars de contre-attaque."),
"Mobilisation défensive": ("Mobilisation défensive", """Pendant 45 secondes, toute la base se mobilise.
Construction deux fois plus rapide, bâtiments +25 % de blindage et réparation automatique.
S'applique aussi aux bunkers et aux tranchées.""",
"Lancez-la au début d'un assaut ennemi, ou pour relever très vite une ligne de défense."),
"Frappe de précision": ("Frappe de précision", """Un missile de croisière frappe un point précis.
Il détruit presque n'importe quel bâtiment, mais n'abîme presque rien autour.
La décapitation plutôt que la destruction.""",
"Visez le bâtiment clé : Palacio, radar, chantier de construction, aérodrome."),
"Opération Taupe": ("Opération Taupe", """Tunnel creusé entre deux zones explorées de terre ferme.
Réservé aux unités rubéniennes : 20 passages, 90 secondes.
Contourner la ligne ennemie plutôt que la percer.""",
"Faites passer une force de contre-attaque derrière l'ennemi, puis refermez le piège."),
"Supériorité aérienne": ("Supériorité aérienne", """Quatre chasseurs arrivent du bord de la carte.
Ils patrouillent 30 secondes au-dessus de la zone et abattent tout appareil ennemi.
Puis ils repartent.""",
"Couvrez une opération ou contrez une vague de bombardiers, de Pelícano ou de drones elielistanais."),
"Raffinerie boost": ("Raffinerie boost", """Toutes les raffineries tournent à plein régime.
Pendant 40 secondes, le minerai livré rapporte 20 % de plus (en plus des 5 % de la centrale).
Nécessite la centrale d'enrichissement et un silo.""",
"Lancez-le quand plusieurs collecteurs sont sur le point de décharger."),
"Radar blink": ("Radar blink", """Le réseau d'écoute australouisien capte les sonars de toutes les mers.
Pendant 1 minute, vous voyez ce que voit chaque sous-marin de la carte, et les sous-marins ennemis sont révélés.
Porté par le chantier naval avancé.""",
"Lancez-le avant une opération navale pour chasser les sous-marins ennemis, ou pour espionner la côte adverse grâce à eux."),
"Engineered Tsunami": ("Engineered Tsunami", """Les ingénieurs australouisiens savent lever la mer contre une côte choisie.
Une vague géante naît au large et balaie une bande de 10 cases de large sur 8 cases dans les terres, dans le sens choisi.
Tous les joueurs sont prévenus 10 secondes avant l'impact, sans savoir quelle côte est visée.
Pleine puissance sur les 2 premières cases, puis la vague s'affaiblit : l'infanterie du rivage est balayée,
les véhicules et les bâtiments lourds sont très abîmés mais ne sont pas détruits d'office.
Arrache les arbres, renverse les murs et emporte le minerai. Aviation en vol et navires presque épargnés ;
les îles du Mothership résistent. Frappe aussi vos propres troupes. Porté par le chantier naval avancé.""",
"Visez les raffineries, centrales et chantiers navals construits au bord de l'eau, puis débarquez juste après la vague. "
"Contre lui : ne concentrez pas toute votre base sur le rivage et éloignez vos troupes de la côte dès l'alerte."),
"Blitz 7": ("Blitz 7", """La jauge d'offensive ananthanienne : « L'Ananthanie gagne une bataille en 7 minutes. »
Elle se remplit en 7 minutes, seulement quand l'armée n'est pas au combat, plus vite avec des réserves au repos et des Nayrath.
Déclenchée, elle donne 70 s de Blitz à toute l'armée (+30 % de vitesse, +25 % de cadence, +15 % de dégâts), puis 30 s de fatigue.""",
"Préparez : regroupez vos forces loin du front, laissez la jauge se remplir, puis lancez tout en même temps. Ne gâchez pas la fatigue en restant exposé."),
"Essaim": ("Essaim", """Douze drones kamikazes décollent du Haut Commandement.
Ils foncent sur la zone choisie et plongent sur les véhicules, fantassins et défenses qu'ils y trouvent.
Le domaine principal de la technologie ananthanienne.""",
"Lancez-le sur une colonne blindée ou une ligne de défense juste avant votre Blitz."),
"Salve Agni": ("Salve Agni", """Six missiles de croisière partent du Haut Commandement.
Ils frappent la zone choisie, dispersés sur deux cases.
Le « feu » d'Agni, sans préavis.""",
"Idéal contre une base, une batterie de défenses ou des unités groupées. Une Ananta ennemie peut en abattre une partie."),
"Raid furtif": ("Raid furtif", """Trois bombardiers furtifs traversent la zone dans la direction choisie.
Invisibles sauf aux détecteurs, ils la couvrent d'un tapis de bombes.
L'aviation, bras armé de l'armée.""",
"Choisissez la direction pour que le tapis suive l'axe de la cible (colonne, ligne de défenses)."),
"Œil d'Ambar": ("Œil d'Ambar", """Un drone de reconnaissance révèle une zone de 8 cases pendant 30 secondes.
Un renseignement « moyen » : utile, mais limité.
Voir avant de frapper.""",
"Utilisez-le pour guider l'Agnar, l'Agnikhesh ou la Salve Agni sur une cible lointaine."),
"Pont aérien": ("Pont aérien", """Un avion de transport parachute deux chars Vidra, quatre Thals et deux Khesh-Thals.
Une tête de pont en quelques secondes.
La guerre de mouvement, par les airs.""",
"Larguez derrière les lignes ennemies pendant votre Blitz pour prendre l'adversaire à revers."),
"Pacte de Fraternité": ("Pacte de Fraternité", """Le pouvoir de coalition de l'Ananthanie.
Pendant 45 s, dans la zone, vos unités ET celles de vos alliés gagnent +15 % de dégâts et se réparent, et la zone est révélée.
Plus il reste d'alliés en jeu, plus vite il se recharge.""",
"En partie à plusieurs, lancez-le sur l'offensive commune : l'Ananthanie est encore plus dangereuse avec ses alliés."),
}
