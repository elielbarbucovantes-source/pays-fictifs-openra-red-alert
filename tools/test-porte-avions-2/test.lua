-- Scénario de test automatique : porte-avions (armes, chantier mobile en
-- multi-file, remplacement du radar / centre technique, réparation en 4 s).
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

InMap = function(c) return c.X >= 17 and c.Y >= 17 and c.X <= 110 and c.Y <= 110 end
IsWater = function(c) return InMap(c) and Map.TerrainType(c) == "Water" end
IsLand = function(c)
	if not InMap(c) then return false end
	local t = Map.TerrainType(c)
	return t == "Clear" or t == "Road" or t == "Rough"
end
IsOpenWater = function(c)
	for dx = -3, 3 do
		for dy = -3, 3 do
			if not IsWater(CPos.New(c.X + dx, c.Y + dy)) then return false end
		end
	end
	return true
end
IsOpenLand = function(c)
	for dx = 0, 3 do
		for dy = 0, 3 do
			if not IsLand(CPos.New(c.X + dx, c.Y + dy)) then return false end
		end
	end
	return true
end

Taken = { }
Free = function(c)
	for _, t in ipairs(Taken) do
		if math.abs(t.X - c.X) < 5 and math.abs(t.Y - c.Y) < 5 then return false end
	end
	return true
end

Find = function(x, y, test)
	for d = 0, 90 do
		for dx = -d, d do
			for dy = -d, d do
				if math.max(math.abs(dx), math.abs(dy)) == d then
					local c = CPos.New(x + dx, y + dy)
					if test(c) and Free(c) then return c end
				end
			end
		end
	end
end

-- Case de terre à une distance comprise entre dmin et dmax (en cases) de c.
Ring = function(c, dmin, dmax)
	return Find(c.X, c.Y, function(p)
		local d = math.sqrt((p.X - c.X) ^ 2 + (p.Y - c.Y) ^ 2)
		return d >= dmin and d <= dmax and IsLand(p)
	end)
end

Pct = function(a)
	if a.IsDead then return 0 end
	return math.floor(100 * a.Health / a.MaxHealth)
end

Pre = function(p) return tostring(Me.HasPrerequisites({ p })) end

Ship = function(type, x, y)
	local c = Find(x, y, IsOpenWater)
	table.insert(Taken, c)
	return Actor.Create(type, true, { Owner = Me, Location = c }), c
end

Cible = function(type, c)
	local a = Actor.Create(type, true, { Owner = Enemy, Location = c })
	a.Stance = "HoldFire"
	return a
end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	Enemy = Player.GetPlayer("Ennemi")

	Log("sans porte-avions : structures.elielistan=" .. Pre("structures.elielistan") .. " dome=" .. Pre("dome"))

	PA, seaE = Ship("porta.aguila", 40, 40)
	RUB, seaR = Ship("porte.avions.rub", 90, 40)
	ANA, seaA = Ship("porte.avions.ana", 40, 90)
	AUS, seaU = Ship("porte.avions.aus", 90, 90)
	Log("porte-avions : " .. tostring(PA.Location) .. " " .. tostring(RUB.Location) .. " " .. tostring(ANA.Location) .. " " .. tostring(AUS.Location))

	-- Cibles ennemies à terre : Trueno de l'Aguila à ~18 cases, V2 de l'Ananthanie
	-- à ~24 cases, nuée de l'Australouis à ~24 cases, canons mer-sol à ~10 cases.
	CTrueno = Cible("3tnk", Ring(seaE, 16, 19))
	CMerSol = Cible("3tnk", Ring(seaR, 8, 11))
	CV2 = Cible("3tnk", Ring(seaA, 22, 25))
	CEssaim = Cible("3tnk", Ring(seaU, 22, 25))
	CEssaimBat = Actor.Create("powr", true, { Owner = Enemy, Location = Ring(seaU, 20, 25) })
	-- Navire ennemi à ~7 cases (missiles mer-mer) et avion au-dessus (AA).
	local c = Find(seaR.X + 7, seaR.Y, IsOpenWater)
	CMerMer = Cible("dd", c)
	CAir = Actor.Create("yak", true, { Owner = Enemy, Location = CPos.New(seaE.X + 12, seaE.Y) })
	CAir.Stance = "HoldFire"
	CAir.Move(seaE)
	Log("distances : trueno " .. tostring(CTrueno.Location) .. " v2 " .. tostring(CV2.Location) .. " essaim " .. tostring(CEssaim.Location) .. " batiment " .. tostring(CEssaimBat.Location))

	-- Réparation en 4 s : char à 10 % et fantassin à 25 % dans la soute de l'Ambarnath.
	Char = Actor.Create("3tnk", false, { Owner = Me })
	Char.Health = math.floor(Char.MaxHealth / 10)
	ANA.LoadPassenger(Char)
	Fantassin = Actor.Create("e1", false, { Owner = Me })
	Fantassin.Health = math.floor(Fantassin.MaxHealth / 4)
	ANA.LoadPassenger(Fantassin)

	Trigger.AfterDelay(30, function()
		Log("avec porte-avions : structures.elielistan=" .. Pre("structures.elielistan") .. " structures.rubenie=" .. Pre("structures.rubenie")
			.. " structures.ananthanie=" .. Pre("structures.ananthanie") .. " structures.australouis=" .. Pre("structures.australouis"))
		Log("jamais eu : dome=" .. Pre("dome") .. " atek=" .. Pre("atek") .. " stek=" .. Pre("stek") .. " institut=" .. Pre("institut") .. " (attendu false)")
		Log("réparation t+1,2 s : char " .. Pct(Char) .. " %, fantassin " .. Pct(Fantassin) .. " %")

		-- Chantier mobile en multi-file : file Bâtiments propre au porte-avions.
		local ok, r = pcall(function() return PA.Build({ "powr" }) end)
		Log("PA.Build(powr) -> " .. tostring(ok and r) .. (ok and "" or (" erreur " .. tostring(r))))
		local ok2, r2 = pcall(function() return PA.Build({ "pbox" }) end)
		Log("PA.Build(pbox) défense -> " .. tostring(ok2 and r2) .. (ok2 and "" or (" erreur " .. tostring(r2))))

		-- Bâtiments réels, détruits ensuite : le porte-avions doit les remplacer.
		local l1 = Find(seaE.X, seaE.Y, IsOpenLand)
		table.insert(Taken, l1)
		Dome = Actor.Create("dome", true, { Owner = Me, Location = l1 })
		local l2 = Find(seaE.X, seaE.Y, IsOpenLand)
		table.insert(Taken, l2)
		Atek = Actor.Create("atek", true, { Owner = Me, Location = l2 })
		local l3 = Find(seaE.X, seaE.Y, IsOpenLand)
		table.insert(Taken, l3)
		Fact = Actor.Create("fact", true, { Owner = Me, Location = l3 })
	end)

	Trigger.AfterDelay(102, function()
		Log("réparation t+4 s : char " .. Pct(Char) .. " %, fantassin " .. Pct(Fantassin) .. " % (attendu 100)")
		Log("PA produit (powr) : " .. tostring(PA.IsProducing("powr")))
		local ok, r = pcall(function() return Fact.Build({ "powr" }) end)
		Log("Fact.Build(powr) en multi-file -> " .. tostring(ok and r))
		Log("bâtiments présents : dome=" .. Pre("dome") .. " atek=" .. Pre("atek"))
		Dome.Kill()
		Atek.Kill()
	end)

	Trigger.AfterDelay(180, function()
		Log("bâtiments détruits, remplacés : dome=" .. Pre("dome") .. " atek=" .. Pre("atek") .. " techcenter=" .. Pre("techcenter") .. " (attendu true)")
	end)

	for _, t in ipairs({ 250, 500, 750 }) do
		Trigger.AfterDelay(t, function()
			Log("cibles : trueno " .. Pct(CTrueno) .. " %, mer-sol " .. Pct(CMerSol) .. " %, v2 " .. Pct(CV2) .. " %, essaim " .. Pct(CEssaim)
				.. " %, bâtiment (essaim, intouché attendu) " .. Pct(CEssaimBat) .. " %, mer-mer " .. Pct(CMerMer) .. " %, avion " .. Pct(CAir) .. " %")
		end)
	end

	Trigger.AfterDelay(760, function()
		PA.Kill()
		RUB.Kill()
		ANA.Kill()
		AUS.Kill()
	end)

	Trigger.AfterDelay(820, function()
		Log("sans porte-avions : dome=" .. Pre("dome") .. " atek=" .. Pre("atek") .. " (attendu false)")
		Log("FIN")
	end)
end
