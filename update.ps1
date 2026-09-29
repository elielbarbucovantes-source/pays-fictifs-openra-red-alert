# Mise à jour du mod depuis GitHub (Windows), lancée par le bouton
# « Mettre à jour » du menu principal (ou à la main).
#   update.ps1 [pid du jeu à attendre] [dépôt owner/nom] [branche]
# Attend la fermeture du jeu, récupère la dernière version (git pull, ou
# le ZIP de GitHub pour une installation sans git), recompile, relance.
param([string]$GamePid, [string]$Repo, [string]$Branch = "main")

Set-Location $PSScriptRoot
Start-Transcript -Path "update.log" | Out-Null

if (-not $Repo) {
	$url = git config --get remote.origin.url 2>$null
	if ($url) { $Repo = ($url -replace '.*github.com[:/]', '') -replace '\.git$', '' }
}

Write-Host "== Mise à jour de $Repo ($Branch)"

if ($GamePid) {
	Write-Host "Attente de la fermeture du jeu..."
	while (Get-Process -Id $GamePid -ErrorAction SilentlyContinue) { Start-Sleep -Seconds 1 }
}

if (-not $env:DOTNET_ROLL_FORWARD) { $env:DOTNET_ROLL_FORWARD = "Major" }

$ok = "Mise à jour installée"
$result = $ok

if ((Test-Path ".git") -and (Get-Command git -ErrorAction SilentlyContinue)) {
	git pull --ff-only --autostash origin $Branch
	if ($LASTEXITCODE -ne 0) { $result = "Échec de la mise à jour (git pull), voir update.log" }
}
elseif ($Repo) {
	$tmp = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid())
	New-Item -ItemType Directory $tmp | Out-Null
	try {
		$headers = @{ "Accept" = "application/vnd.github.sha"; "User-Agent" = "OpenRA-mod-updater" }
		$sha = (Invoke-WebRequest -UseBasicParsing -Headers $headers "https://api.github.com/repos/$Repo/commits/$Branch").Content
		Invoke-WebRequest -UseBasicParsing "https://github.com/$Repo/archive/refs/heads/$Branch.zip" -OutFile "$tmp\mod.zip"
		Expand-Archive "$tmp\mod.zip" -DestinationPath $tmp
		$src = Get-ChildItem $tmp -Directory | Select-Object -First 1
		Copy-Item -Path "$($src.FullName)\*" -Destination . -Recurse -Force
		if ($sha) { Set-Content ".version-commit" $sha.Trim() }
	}
	catch {
		Write-Host $_
		$result = "Échec du téléchargement, voir update.log"
	}
	Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}
else {
	$result = "Dépôt GitHub inconnu, mise à jour impossible"
}

if ($result -eq $ok) {
	Write-Host "Compilation..."
	& .\make.cmd all
	if ($LASTEXITCODE -ne 0) { $result = "Échec de la compilation, voir update.log" }
}

Write-Host $result
Set-Content "update-result.txt" $result -Encoding UTF8
Stop-Transcript | Out-Null

if ($GamePid) { Start-Process ".\launch-game.cmd" }
