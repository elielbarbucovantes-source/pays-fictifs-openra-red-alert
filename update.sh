#!/bin/bash
# Mise à jour du mod depuis GitHub, lancée par le bouton « Mettre à jour »
# du menu principal (ou à la main : ./update.sh).
#   ./update.sh [pid du jeu à attendre] [dépôt owner/nom] [branche]
# Attend la fermeture du jeu, récupère la dernière version (git pull, ou
# le ZIP de GitHub pour une installation sans git), recompile, relance.

cd "$(dirname "$0")" || exit 1
PID="$1"
REPO="$2"
BRANCH="${3:-main}"

if [ -z "$REPO" ]; then
	REPO=$(git config --get remote.origin.url 2>/dev/null | sed -E 's#.*github.com[:/]##; s#\.git$##')
fi

echo "== Mise à jour de $REPO ($BRANCH) — $(date)"

if [ -n "$PID" ]; then
	echo "Attente de la fermeture du jeu…"
	while kill -0 "$PID" 2>/dev/null; do sleep 1; done
fi

# Moteur .NET 6 : autorise un .NET plus récent s'il est le seul installé.
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"

RESULT="Mise à jour installée"

if [ -d .git ] && command -v git >/dev/null 2>&1; then
	if ! git pull --ff-only --autostash origin "$BRANCH"; then
		RESULT="Échec de la mise à jour (git pull), voir update.log"
	fi
elif [ -n "$REPO" ]; then
	TMP=$(mktemp -d)
	SHA=$(curl -fsSL -H "Accept: application/vnd.github.sha" "https://api.github.com/repos/$REPO/commits/$BRANCH")
	if curl -fL -o "$TMP/mod.zip" "https://github.com/$REPO/archive/refs/heads/$BRANCH.zip" \
		&& unzip -q "$TMP/mod.zip" -d "$TMP"; then
		SRC=$(find "$TMP" -mindepth 1 -maxdepth 1 -type d | head -n 1)
		cp -R "$SRC"/. . && [ -n "$SHA" ] && echo "$SHA" > .version-commit
	else
		RESULT="Échec du téléchargement, voir update.log"
	fi
	rm -rf "$TMP"
else
	RESULT="Dépôt GitHub inconnu, mise à jour impossible"
fi

if [ "$RESULT" = "Mise à jour installée" ]; then
	echo "Compilation…"
	if ! make; then
		RESULT="Échec de la compilation, voir update.log"
	fi
fi

echo "$RESULT"
echo "$RESULT" > update-result.txt

[ -n "$PID" ] && exec /bin/bash ./launch-game.sh
