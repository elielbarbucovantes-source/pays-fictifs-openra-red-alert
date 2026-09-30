-- Scénario de test automatique du système pétrole.
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

Make = function(type, owner, x, y)
	return Actor.Create(type, true, { Owner = owner, Location = CPos.New(x, y) })
end

Y = 60        -- ligne du pipeline principal
Segments = {}

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	En = Player.GetPlayer("Ennemi")
	Neutre = Player.GetPlayer("Neutral")

	-- Raffinerie en (60,60)-(61,61), pipeline principal de x=62 à x=95.
	Raff = Make("raffinerie.petrole", Me, 60, Y)
	for i = 0, 2 do Make("apwr", Me, 50, 40 + 4 * i) end
	for x = 62, 95 do Segments[x] = Make("pipeline", Me, x, Y) end

	-- 6 derricks à nous collés au pipeline (au-dessus), à moins de 30 cases :
	-- seuls 5 doivent payer. Un 7e à 34 cases : trop loin.
	D = {}
	for i, x in ipairs({ 64, 68, 72, 76, 80, 84 }) do D[i] = Make("oilb", Me, x, Y - 2) end
	Loin = Make("oilb", Me, 94, Y - 2)
	-- Derrick ennemi et derrick neutre collés au pipeline : jamais payés.
	Make("oilb", En, 88, Y + 1)
	Make("oilb", Neutre, 66, Y + 1)
	-- Derrick à nous sans pipeline.
	Make("oilb", Me, 40, 80)

	Last = Me.Cash
	Log("cash initial " .. Last)

	-- Relevé juste après chaque paiement (toutes les 10 s = 250 ticks).
	for n = 1, 9 do
		Trigger.AfterDelay(250 * n + 5, function()
			local c = Me.Cash
			Log("paiement " .. n .. " : +" .. (c - Last))
			Last = c
		end)
	end

	-- Étape 2 (après le 1er paiement) : segment x=74 détruit → coupe D4, D5, D6.
	-- Attendu : les 3 premiers seulement (+300).
	Trigger.AfterDelay(300, function()
		Segments[74].Kill()
		Log("segment x=74 détruit (attendu +300)")
	end)

	-- Étape 3 : segment reconstruit → retour à +500.
	Trigger.AfterDelay(550, function()
		Segments[74] = Make("pipeline", Me, 74, Y)
		Log("segment x=74 reconstruit (attendu +500)")
	end)

	-- Étape 4 : D1 capturé par l'ennemi → D6 prend sa place, toujours +500.
	-- Puis D2 capturé aussi → +400.
	Trigger.AfterDelay(800, function()
		D[1].Owner = En
		Log("D1 capturé par l'ennemi (attendu +500)")
	end)
	Trigger.AfterDelay(1050, function()
		D[2].Owner = En
		Log("D2 capturé par l'ennemi (attendu +400)")
	end)

	-- Étape 5 : un derrick détruit → +300.
	Trigger.AfterDelay(1300, function()
		D[3].Kill()
		Log("D3 détruit (attendu +300)")
	end)
	Trigger.AfterDelay(1350, function()
		local morts = ""
		for x = 62, 95 do if Segments[x].IsDead then morts = morts .. " " .. x end end
		Log("segments détruits après l'explosion de D3 :" .. morts)
	end)

	-- Étape 6 : raffinerie détruite → +0 ; derricks toujours à nous.
	Trigger.AfterDelay(1550, function()
		Raff.Kill()
		Log("raffinerie détruite (attendu +0)")
	end)
	Trigger.AfterDelay(1600, function()
		Log("D4 toujours à nous : " .. tostring(D[4].Owner == Me))
	end)

	-- Étape 7 : nouvelle raffinerie reliée au bout du pipeline (x=96) → +300.
	Trigger.AfterDelay(1800, function()
		Make("raffinerie.petrole", Me, 96, Y)
		Log("nouvelle raffinerie en x=96 (attendu +400 : Loin, D4, D5, D6)")
	end)

	-- Passage : un char traverse le pipeline du sud au nord.
	-- Passage : chercher un coin de terrain dégagé (7x7), y poser un pipeline
	-- horizontal de 7 cases et faire traverser un char du nord au sud.
	local ok = function(c) return Map.TerrainType(c) == "Clear" end
	local site
	for y = 20, 100 do
		for x = 20, 100 do
			if not site then
				local libre = true
				for dx = -3, 3 do for dy = -3, 3 do
					if not ok(CPos.New(x + dx, y + dy)) then libre = false end
				end end
				if libre and (x < 55 or x > 100 or y < 52 or y > 66) and (x < 76 or x > 90 or y < 72 or y > 83) then site = CPos.New(x, y) end
			end
		end
	end
	Log("site de passage : " .. site.X .. "," .. site.Y)
	for dx = -3, 3 do Make("pipeline", Me, site.X + dx, site.Y) end
	Char = Make("2tnk", Me, site.X, site.Y - 3)
	Trigger.AfterDelay(25, function() Char.Move(CPos.New(site.X, site.Y + 3)) end)
	Trigger.AfterDelay(DateTime.Seconds(15), function()
		Log("char : position " .. Char.Location.X .. "," .. Char.Location.Y .. " (a traversé : " .. tostring(Char.Location.Y > site.Y) .. ")")
	end)
	-- Un pipeline peut être visé et détruit par une unité.
	local cible
	for x = 80, 85 do
		local s = Make("pipeline", Me, x, 75)
		if x == 82 then cible = s end
	end
	Tireur = Make("3tnk", En, 82, 80)
	Trigger.AfterDelay(25, function() Tireur.Attack(cible, true, true) end)
	Trigger.AfterDelay(DateTime.Seconds(12), function()
		Log("segment isolé détruit par un char ennemi : " .. tostring(cible.IsDead))
	end)
end
