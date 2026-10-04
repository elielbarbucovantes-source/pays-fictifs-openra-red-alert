-- Test automatique du lot 3 de l'Ananthanie : jauge Blitz 7, pouvoirs, expérimentales.
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end
IsLand = function(c)
	if c.X < 2 or c.Y < 2 or c.X > 125 or c.Y > 125 then return false end
	local t = Map.TerrainType(c)
	return t == "Clear" or t == "Road"
end
Find = function(x, y, test, maxd)
	for d = 0, maxd or 30 do
		for dx = -d, d do
			for dy = -d, d do
				local c = CPos.New(x + dx, y + dy)
				if test(c) then return c end
			end
		end
	end
end
Make = function(type, owner, cell) return Actor.Create(type, true, { Owner = owner, Location = cell }) end
Hp = function(a) return a.IsDead and "mort" or tostring(a.Health) end
Somme = function(list)
	local s = 0
	for _, a in ipairs(list) do if not a.IsDead then s = s + a.Health end end
	return s
end
Groupe = function(type, owner, x, y, n)
	local l = {}
	for i = 1, n do
		local a = Make(type, owner, Find(x + (i - 1) * 2, y, IsLand))
		a.Stance = "HoldFire"
		l[#l + 1] = a
	end
	return l
end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	En = Player.GetPlayer("Ennemi")
	local b = Find(60, 60, IsLand)
	Make("haut.commandement", Me, b)
	Make("fabrique.drones", Me, Find(b.X + 5, b.Y, IsLand))
	Make("institut", Me, Find(b.X - 5, b.Y, IsLand))
	Make("base.ananthanie", Me, Find(b.X, b.Y + 5, IsLand))

	-- 1. Jauge Blitz 7 : réserve, puis combat, puis déclenchement.
	Reserve = Groupe("thal", Me, b.X - 10, b.Y + 10, 10)
	for t = 1, 40, 3 do
		Trigger.AfterDelay(DateTime.Seconds(t), function() Log("blitz t=" .. t .. " : " .. Me.BlitzGauge()) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Intrus = Make("e1", En, Find(Reserve[1].Location.X, Reserve[1].Location.Y + 3, IsLand))
		for _, a in ipairs(Reserve) do a.Stance = "AttackAnything" end
		Log("intrus ennemi place")
	end)
	Trigger.AfterDelay(DateTime.Seconds(12), function()
		if not Intrus.IsDead then Intrus.Destroy() end
		for _, a in ipairs(Reserve) do a.Stance = "HoldFire" end
	end)
	Trigger.AfterDelay(DateTime.Seconds(36), function()
		Log("Blitz declenche : " .. tostring(Me.ActivatePower("AnanthanieBlitz")))
	end)

	-- 2. Pouvoirs ciblés.
	local function zone(x, y, type, n)
		local c = Find(x, y, IsLand)
		Make("camera", Me, c)
		return c, Groupe(type, En, c.X, c.Y, n)
	end
	local c1, g1 = zone(25, 25, "2tnk", 3)
	local c2, g2 = zone(100, 25, "powr", 2)
	local c3, g3 = zone(25, 100, "e1", 6)
	local c5, g5 = zone(100, 100, "1tnk", 3)
	local avant = { Somme(g1), Somme(g2), Somme(g3), Somme(g5) }
	local oeil = Find(110, 60, IsLand)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Log("Essaim : " .. tostring(Me.ActivatePowerAt("AnanthanieEssaim", c1)))
		Log("Salve Agni : " .. tostring(Me.ActivatePowerAt("AnanthanieSalveAgni", c2)))
		Log("Raid furtif : " .. tostring(Me.ActivatePowerAt("AnanthanieRaidFurtif", c3, 64)))
		Log("oeil : zone exploree avant = " .. tostring(Me.IsExplored(oeil)))
		Log("Oeil d'Ambar : " .. tostring(Me.ActivatePowerAt("AnanthanieOeil", oeil)))
		Log("Pont aerien : " .. tostring(Me.ActivatePowerAt("AnanthaniePontAerien", Find(40, 60, IsLand), 64)))
	end)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		local n = #Utils.Where(Map.ActorsInWorld, function(x) return x.Type == "camera.ambar" end)
		Log("oeil : zone exploree apres = " .. tostring(Me.IsExplored(oeil)) .. " (" .. n .. " camera)")
	end)
	Trigger.AfterDelay(DateTime.Seconds(30), function()
		Log("Essaim : 3 chars moyens " .. avant[1] .. " -> " .. Somme(g1))
		Log("Salve Agni : 2 centrales " .. avant[2] .. " -> " .. Somme(g2))
		Log("Raid furtif : 6 fantassins " .. avant[3] .. " -> " .. Somme(g3))
		local v = #Utils.Where(Map.ActorsInWorld, function(a) return a.Owner == Me and a.Type == "vidra" end)
		Log("Pont aerien : " .. v .. " Vidra au sol (attendu 2)")
	end)

	-- 3. Pacte de Fraternité : une unité blessée se répare dans la zone.
	local f = Find(80, 60, IsLand)
	Blesse = Make("lunkra", Me, f)
	Blesse.Health = 20000
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Log("Fraternite : " .. tostring(Me.ActivatePowerAt("AnanthanieFraternite", f)))
	end)
	Trigger.AfterDelay(DateTime.Seconds(13), function() Log("Fraternite : Lunkra 20000 -> " .. Hp(Blesse)) end)

	-- 4. Vidrakarn : trois chars alignés.
	local v = Find(30, 70, IsLand)
	Vk = Make("vidrakarn", Me, v)
	Vk.Stance = "HoldFire"
	Alignes = {}
	for i = 1, 3 do
		local a = Make("3tnk", En, CPos.New(v.X + 3 + i * 2, v.Y))
		a.Stance = "HoldFire"
		Alignes[#Alignes + 1] = a
	end
	Trigger.AfterDelay(DateTime.Seconds(1), function() Vk.Attack(Alignes[1]) end)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Log("Vidrakarn : chars alignes PV " .. Hp(Alignes[1]) .. ", " .. Hp(Alignes[2]) .. ", " .. Hp(Alignes[3]) .. " (60000 chacun)")
	end)

	-- 5. Ananta : missiles antichars ennemis sur un Lunkra voisin, et un hélicoptère.
	local a = Find(70, 90, IsLand)
	An = Make("ananta", Me, a)
	An.Stance = "HoldFire"
	Cible = Make("lunkra", Me, Find(a.X + 2, a.Y, IsLand))
	Cible.Stance = "HoldFire"
	Tireurs = Groupe("e3", En, a.X + 7, a.Y, 3)
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		for _, t in ipairs(Tireurs) do if not t.IsDead then t.Attack(Cible) end end
	end)
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Log("Ananta : missiles abattus = " .. An.MissilesIntercepted .. ", Lunkra protege PV=" .. Hp(Cible))
		for _, t in ipairs(Tireurs) do if not t.IsDead then t.Destroy() end end
		An.Stance = "AttackAnything"
		Heli = Make("heli", En, Find(a.X, a.Y - 6, IsLand))
		Heli.Stance = "HoldFire"
	end)
	Trigger.AfterDelay(DateTime.Seconds(20), function()
		Log("Ananta : helico " .. Hp(Heli))
	end)

	-- 6. Ambaroth : vague de 6 drones.
	Amb = Make("ambaroth", Me, Find(90, 45, IsLand))
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		local n = #Utils.Where(Map.ActorsInWorld, function(x) return x.Type == "drone.ambaroth" end)
		Log("Ambaroth : " .. n .. " drones (attendu 6), PV=" .. Amb.Health)
	end)

	Trigger.AfterDelay(DateTime.Seconds(45), function() Log("fin") end)
end
