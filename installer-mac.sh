#!/bin/bash
# Installation de Pays fictifs sur macOS, en une seule commande dans le Terminal :
#   curl -fsSL https://raw.githubusercontent.com/elielbarbucovantes-source/pays-fictifs-openra-red-alert/main/installer-mac.sh | bash
# Installe ce qui manque (outils Apple, .NET), télécharge le mod dans ~/PaysFictifs,
# crée « Jouer Pays fictifs » sur le Bureau, puis lance le jeu.
# On peut relancer la commande sans risque : elle reprend là où elle s'était arrêtée,
# et remet une installation existante sur la dernière version de GitHub.

set -e
REPO=elielbarbucovantes-source/pays-fictifs-openra-red-alert
DEST="$HOME/PaysFictifs"
LAUNCHER="$HOME/Desktop/Jouer Pays fictifs.command"

echo "== Installation de Pays fictifs"

# git, make et python3 viennent des outils en ligne de commande d'Apple.
if ! xcode-select -p >/dev/null 2>&1; then
	xcode-select --install || true
	echo
	echo "Une fenêtre vient de s'ouvrir pour installer les outils d'Apple."
	echo "Clique sur « Installer », attends la fin, puis relance cette même commande."
	exit 1
fi

# .NET 8, installé dans ~/.dotnet (pas besoin du mot de passe administrateur).
# Toujours le 8 dans ~/.dotnet, même si un autre .NET est déjà installé : tous les joueurs
# doivent avoir le même, sinon les parties se désynchronisent.
if ! "$HOME/.dotnet/dotnet" --list-sdks 2>/dev/null | grep -q "^8\."; then
	echo "== Installation de .NET 8"
	curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
	bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
fi

# Une installation déjà présente est remise exactement sur la dernière version de GitHub :
# réinstaller doit donner la même version que les autres joueurs, et jamais garder l'ancienne.
if [ -d "$DEST/.git" ]; then
	echo "== Mise à jour du mod déjà installé dans $DEST"
	git -C "$DEST" remote set-url origin "https://github.com/$REPO.git"
	git -C "$DEST" fetch origin main
	git -C "$DEST" checkout -f -B main origin/main
	# Fichiers du mod qui ne sont plus sur GitHub : ils changeraient la version.
	git -C "$DEST" clean -fdq -- mods/fictifs OpenRA.Mods.Fictifs
	rm -f "$DEST/.build-commit"
else
	if [ -e "$DEST" ]; then
		# Ancienne installation sans git (ZIP) : mise de côté, puis installation neuve.
		ANCIEN="$DEST-ancien-$(date +%Y%m%d-%H%M%S)"
		echo "== Ancienne installation déplacée dans $ANCIEN"
		mv "$DEST" "$ANCIEN"
	fi
	echo "== Téléchargement du mod dans $DEST"
	git clone "https://github.com/$REPO.git" "$DEST"
fi
echo "== Version installée : $(git -C "$DEST" log -1 --format='%h %s')"
chmod +x "$DEST"/*.sh

# Créé ici (et non téléchargé), le fichier n'est pas bloqué par Gatekeeper.
cat > "$LAUNCHER" <<EOF
#!/bin/bash
exec "$DEST/jouer.sh"
EOF
chmod +x "$LAUNCHER"

echo
echo "== Terminé. Pour jouer ensuite : double-clic sur « Jouer Pays fictifs » sur le Bureau."
echo "   Le jeu se met à jour tout seul à chaque lancement."
echo "   Au premier démarrage, accepte le téléchargement des fichiers de Red Alert (« Quick Install »)."
echo
exec "$DEST/jouer.sh" </dev/tty
