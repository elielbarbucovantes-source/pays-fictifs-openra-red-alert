-- Scénario de test automatique : Chronosphère et unités ennemies envoyées en terrain interdit.
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

InMap = function(c) return c.X >= 3 and c.Y >= 3 and c.X <= 124 and c.Y <= 124 end
IsLand = function(c) local t = Map.TerrainType(c) return t == "Clear" or t == "Road" or t == "Rough" end
IsWater = function(c) return Map.TerrainType(c) == "Water" end

Around = function(test, r)
	return function(c)
		if not InMap(c) then return false end
		for dx = -r, r do
			for dy = -r, r do
				if not test(CPos.New(c.X + dx, c.Y + dy)) then return false end
			end
		end
		return true
	end
end

Find = function(x, y, test)
	for d = 0, 70 do
		for dx = -d, d do
			for dy = -d, d do
				local c = CPos.New(x + dx, y + dy)
				if test(c) then return c end
			end
		end
	end
end

State = function(a) if a.IsDead then return "détruit" end return a.Location.X .. "," .. a.Location.Y end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	En = Player.GetPlayer("Ennemi")

	local base = Find(35, 66, Around(IsLand, 3))
	Pdox = Actor.Create("pdox", true, { Owner = Me, Location = base })
	for i = 0, 2 do Actor.Create("apwr", true, { Owner = Me, Location = CPos.New(base.X + 3 + 3 * i, base.Y + 4) }) end

	-- 1) char ennemi + char allié sur terre, envoyés en mer
	L1 = Find(base.X - 8, base.Y - 8, Around(IsLand, 2))
	Ennemi1 = Actor.Create("3tnk", true, { Owner = En, Location = L1 })
	Allie1 = Actor.Create("2tnk", true, { Owner = Me, Location = CPos.New(L1.X + 1, L1.Y) })
	Sea = Find(64, 64, Around(IsWater, 2))
	Log("mer " .. Sea.X .. "," .. Sea.Y .. " ; terre " .. L1.X .. "," .. L1.Y)

	-- 2) navire ennemi en mer, envoyé à terre
	Navire = Actor.Create("ss", true, { Owner = En, Location = Sea })
	Land2 = Find(L1.X, L1.Y + 12, Around(IsLand, 2))

	-- 3) char ennemi envoyé sur une terre libre : téléportation normale
	L3 = Find(base.X + 14, base.Y - 10, Around(IsLand, 2))
	Ennemi3 = Actor.Create("3tnk", true, { Owner = En, Location = L3 })
	Dest3 = Find(L3.X + 6, L3.Y + 6, Around(IsLand, 2))

	Trigger.AfterDelay(60, function()
		Log("tir 1 : prêt=" .. tostring(Pdox.FireChronoshift(L1, Sea)))
	end)
	Trigger.AfterDelay(80, function()
		Log("char ennemi vers la mer : " .. State(Ennemi1) .. " (attendu détruit)")
		Log("char allié vers la mer : " .. State(Allie1) .. " (attendu " .. (L1.X + 1) .. "," .. L1.Y .. ", pas bougé)")
	end)
	Trigger.AfterDelay(150, function()
		Log("tir 2 : prêt=" .. tostring(Pdox.FireChronoshift(Sea, Land2)))
	end)
	Trigger.AfterDelay(170, function()
		Log("sous-marin ennemi vers la terre : " .. State(Navire) .. " (attendu détruit)")
	end)
	Trigger.AfterDelay(240, function()
		Log("tir 3 : prêt=" .. tostring(Pdox.FireChronoshift(L3, Dest3)))
	end)
	Trigger.AfterDelay(290, function()
		Log("char ennemi vers terre libre : " .. State(Ennemi3) .. " (attendu " .. Dest3.X .. "," .. Dest3.Y .. ")")
		Log("FIN")
	end)
end
