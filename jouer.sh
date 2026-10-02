#!/bin/bash
# Lance Pays fictifs (Linux et macOS) après s'être mis à jour :
#   1. récupère la dernière version publiée sur GitHub (update.sh) ;
#   2. recompile si le code a changé depuis la dernière compilation ;
#   3. la première fois, ouvre le pare-feu à ZeroTier pour pouvoir héberger ;
#   4. lance le jeu.
# Tous les joueurs ont ainsi la même version, ce qui évite les désynchronisations.

cd "$(dirname "$0")" || exit 1
BRANCH=main
REPO=elielbarbucovantes-source/pays-fictifs-openra-red-alert

# Terminal ou lanceur graphique (.desktop) ? À tester avant de rediriger la sortie.
INTERACTIVE=
[ -t 1 ] && INTERACTIVE=1
exec > >(tee jouer.log) 2>&1

# Moteur .NET 6 : autorise le .NET 8 installé ; ~/.dotnet vient de installer-mac.sh.
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-LatestMajor}"
if [ -x "$HOME/.dotnet/dotnet" ]; then
	export DOTNET_ROOT="$HOME/.dotnet"
	export PATH="$HOME/.dotnet:$PATH"
fi

notify() {
	echo "$1"
	[ -z "$INTERACTIVE" ] && command -v notify-send >/dev/null 2>&1 && notify-send -i "$PWD/mods/fictifs/icon.png" "Pays fictifs" "$1"
}

local_commit() {
	if [ -d .git ]; then git rev-parse HEAD 2>/dev/null; else cat .version-commit 2>/dev/null; fi
}

# --- 1. Mise à jour ---------------------------------------------------------
NEEDS_UPDATE=
if [ -d .git ] && command -v git >/dev/null 2>&1; then
	# Seulement si GitHub est en avance : des commits locaux non publiés ne sont pas écrasés.
	if git fetch -q origin "$BRANCH" 2>/dev/null \
		&& [ "$(git rev-parse HEAD)" != "$(git rev-parse FETCH_HEAD)" ] \
		&& git merge-base --is-ancestor HEAD FETCH_HEAD; then
		NEEDS_UPDATE=1
	fi
else
	REMOTE=$(curl -fsS -m 10 -H "Accept: application/vnd.github.sha" "https://api.github.com/repos/$REPO/commits/$BRANCH" 2>/dev/null)
	[ -n "$REMOTE" ] && [ "$REMOTE" != "$(local_commit)" ] && NEEDS_UPDATE=1
fi

if [ -n "$NEEDS_UPDATE" ]; then
	notify "Nouvelle version trouvée : mise à jour et compilation (une à deux minutes)…"
	/bin/bash ./update.sh "" "$REPO" "$BRANCH"
	[ "$(cat update-result.txt 2>/dev/null)" = "Mise à jour installée" ] && local_commit > .build-commit
fi

# --- 2. Compilation ---------------------------------------------------------
if [ ! -f engine/bin/OpenRA.Mods.Fictifs.dll ] || [ "$(cat .build-commit 2>/dev/null)" != "$(local_commit)" ]; then
	notify "Compilation du mod…"
	if make; then
		local_commit > .build-commit
	else
		notify "Échec de la compilation, voir jouer.log"
		exit 1
	fi
fi

# --- 3. Pare-feu : laisser entrer les connexions venant de ZeroTier ---------
# Une seule fois par interface ; la liste de celles déjà ouvertes est dans .pare-feu-ok.
as_root() {
	if [ -n "$INTERACTIVE" ] || [ "$(uname -s)" = Darwin ]; then
		sudo "$@"
	elif command -v pkexec >/dev/null 2>&1; then
		pkexec "$@"
	else
		return 1
	fi
}

open_firewall() {
	local key="$1"
	shift
	grep -qxF "$key" .pare-feu-ok 2>/dev/null && return
	echo "Ouverture du pare-feu pour ZeroTier ($key) : le mot de passe administrateur est demandé une seule fois."
	if "$@"; then
		echo "$key" >> .pare-feu-ok
	else
		echo "Pare-feu non modifié : un autre joueur ne pourra peut-être pas rejoindre tes parties."
	fi
}

if [ "$(uname -s)" = Darwin ]; then
	FW=/usr/libexec/ApplicationFirewall/socketfilterfw
	if "$FW" --getglobalstate 2>/dev/null | grep -q enabled; then
		DOTNET=$(python3 -c 'import os, shutil; print(os.path.realpath(shutil.which("dotnet") or ""))')
		[ -n "$DOTNET" ] && open_firewall "mac:$DOTNET" \
			sh -c "sudo '$FW' --add '$DOTNET' >/dev/null && sudo '$FW' --unblockapp '$DOTNET' >/dev/null"
	fi
else
	for IFACE in $(ip -o link 2>/dev/null | awk -F': ' '{print $2}' | grep '^zt'); do
		if command -v ufw >/dev/null 2>&1 && grep -q '^ENABLED=yes' /etc/ufw/ufw.conf 2>/dev/null; then
			open_firewall "ufw:$IFACE" as_root ufw allow in on "$IFACE"
		elif command -v firewall-cmd >/dev/null 2>&1 && firewall-cmd --state >/dev/null 2>&1; then
			open_firewall "firewalld:$IFACE" as_root sh -c \
				"firewall-cmd --permanent --zone=trusted --add-interface='$IFACE' && firewall-cmd --reload"
		fi
	done
fi

# --- 4. Lancement -----------------------------------------------------------
exec /bin/bash ./launch-game.sh "$@"
