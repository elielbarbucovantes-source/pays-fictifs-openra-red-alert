-- Scénario de test automatique de l'aviation australouisienne.
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

IsLand = function(c)
	if c.X < 1 or c.Y < 1 or c.X > 126 or c.Y > 126 then return false end
	local t = Map.TerrainType(c)
	return t == "Clear" or t == "Road" or t == "Rough"
end

IsWater = function(c)
	if c.X < 1 or c.Y < 1 or c.X > 126 or c.Y > 126 then return false end
	return Map.TerrainType(c) == "Water"
end

-- Case au milieu d'une grande étendue d'eau (7x7 cases d'eau).
IsOpenWater = function(c)
	for dx = -3, 3 do
		for dy = -3, 3 do
			if not IsWater(CPos.New(c.X + dx, c.Y + dy)) then return false end
		end
	end
	return true
end

Find = function(x, y, test, maxd)
	for d = 0, maxd or 60 do
		for dx = -d, d do
			for dy = -d, d do
				local c = CPos.New(x + dx, y + dy)
				if test(c) then return c end
			end
		end
	end
end

Make = function(type, owner, cell)
	return Actor.Create(type, true, { Owner = owner, Location = cell })
end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	En = Player.GetPlayer("Ennemi")

	-- 1. Toutes les unités apparaissent.
	local b = Find(60, 60, IsLand)
	Make("base.aeronavale", Me, b)
	Make("hpad", Me, Find(b.X + 5, b.Y, IsLand))
	for _, t in ipairs({ "albatros", "warthog", "manta", "cormoran", "baleine", "moustique" }) do
		local a = Make(t, Me, Find(b.X, b.Y + 6, IsLand))
		Log("apparu : " .. t .. " PV=" .. a.Health)
	end

	-- 2. Moustiques : 2 drones ne suffisent pas contre un char léger, 3 oui.
	local t1 = Find(30, 40, IsLand)
	Tank1 = Make("1tnk", En, t1)
	local t2 = Find(40, 30, IsLand)
	Tank2 = Make("1tnk", En, t2)
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		for i = 1, 2 do Make("moustique", Me, Find(t1.X + 4, t1.Y + 4, IsLand)).Attack(Tank1) end
		for i = 1, 3 do Make("moustique", Me, Find(t2.X + 4, t2.Y + 4, IsLand)).Attack(Tank2) end
	end)
	Trigger.AfterDelay(DateTime.Seconds(15), function()
		Log("char 1 (2 moustiques) : mort=" .. tostring(Tank1.IsDead) .. (Tank1.IsDead and "" or " PV=" .. Tank1.Health))
		Log("char 2 (3 moustiques) : mort=" .. tostring(Tank2.IsDead))
	end)

	-- 3. Warthog, Albatros et Manta tirent.
	local w = Find(80, 40, IsLand)
	Cible1 = Make("2tnk", En, w)
	Make("camera", Me, w)
	local u = Make("warthog", Me, Find(w.X + 8, w.Y + 8, IsLand))
	Trigger.AfterDelay(DateTime.Seconds(1), function() u.Attack(Cible1) end)
	local w2 = Find(40, 85, IsLand)
	Cible2 = Make("powr", En, w2)
	Make("camera", Me, w2)
	local u = Make("manta", Me, Find(w2.X + 10, w2.Y, IsLand))
	Trigger.AfterDelay(DateTime.Seconds(1), function() u.Attack(Cible2) end)
	local w3 = Find(85, 80, IsLand)
	Cible3 = Make("2tnk", En, w3)
	Make("camera", Me, w3)
	local u = Make("albatros", Me, Find(w3.X + 8, w3.Y, IsLand))
	Trigger.AfterDelay(DateTime.Seconds(1), function() u.Attack(Cible3) end)
	Trigger.AfterDelay(DateTime.Seconds(25), function()
		Log("Warthog : char lourd PV=" .. (Cible1.IsDead and "mort" or Cible1.Health))
		Log("Manta : centrale PV=" .. (Cible2.IsDead and "morte" or Cible2.Health))
		Log("Albatros : char lourd PV=" .. (Cible3.IsDead and "mort" or Cible3.Health))
	end)

	-- 4. Cormoran : écope, puis efface ce que l'ennemi a découvert.
	Eau = Find(64, 64, IsOpenWater)
	Zone = Find(Eau.X + 12, Eau.Y, IsLand)
	Log("eau=" .. Eau.X .. "," .. Eau.Y .. " zone=" .. Zone.X .. "," .. Zone.Y)
	Eclaireur = Make("jeep", En, Zone)
	Corm = Make("cormoran", Me, Find(Eau.X - 6, Eau.Y, IsLand))
	Log("Cormoran au depart : eau=" .. Corm.AmmoCount())
	Corm.Move(Eau)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Log("ennemi a decouvert la zone : " .. tostring(En.IsExplored(Zone)))
		Eclaireur.Destroy()
	end)
	Trigger.AfterDelay(DateTime.Seconds(12), function()
		Log("Cormoran apres survol de l'eau : eau=" .. Corm.AmmoCount() .. " position=" .. Corm.Location.X .. "," .. Corm.Location.Y .. " (" .. Map.TerrainType(Corm.Location) .. ")")
		Log("zone toujours decouverte par l'ennemi (sans unite) : " .. tostring(En.IsExplored(Zone)))
		Corm.SprayAt(Zone)
	end)
	for t = 13, 29, 2 do
		Trigger.AfterDelay(DateTime.Seconds(t), function()
			Log("Cormoran t=" .. t .. " eau=" .. Corm.AmmoCount() .. " zone decouverte=" .. tostring(En.IsExplored(Zone)))
		end)
	end
	Trigger.AfterDelay(DateTime.Seconds(30), function()
		Log("apres brouillard : zone decouverte par l'ennemi = " .. tostring(En.IsExplored(Zone)) .. ", eau=" .. Corm.AmmoCount())
		Log("fin aviation")
	end)

	TestMarine()
end

-- Case d'eau avec un carré d'eau libre de 8x8 juste à l'est (+ une marge).
IsOpenWater9 = function(c)
	for dx = -1, 9 do
		for dy = -5, 4 do
			if not IsWater(CPos.New(c.X + dx, c.Y + dy)) then return false end
		end
	end
	return true
end

TestMarine = function()
	-- 5. Révolutionnaire : unités et bâtiment ennemis à moins de 5 cases.
	local r = Find(100, 30, IsLand)
	Revo = Make("revolutionnaire", Me, r)
	Make("camera", Me, r)
	local cibles = {
		Make("e1", En, Find(r.X + 2, r.Y, IsLand)),
		Make("1tnk", En, Find(r.X, r.Y + 3, IsLand)),
		Make("powr", En, Find(r.X - 3, r.Y - 1, IsLand)),
	}
	Loin = Make("e1", En, Find(r.X + 9, r.Y, IsLand))
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Revo.Revolution()
	end)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		for _, a in ipairs(cibles) do Log("revolution : " .. a.Type .. " appartient a " .. a.Owner.InternalName) end
		Log("revolution : e1 a 9 cases appartient a " .. Loin.Owner.InternalName)
	end)

	-- 6. Mothership : une île sort de l'eau.
	Grandes = {}
	for x = 18, 108, 3 do
		for y = 18, 108, 3 do
			local c = CPos.New(x, y)
			if IsOpenWater9(c) then Grandes[#Grandes + 1] = c end
		end
	end
	Large = Grandes[1]
	Log(#Grandes .. " grandes etendues d'eau")
	Log("large=" .. (Large and (Large.X .. "," .. Large.Y) or "aucune"))
	if Large then
		Mother = Make("mothership", Me, Large)
		Avant = {}
		for dx = -12, 12 do
			for dy = -12, 12 do
				local c = CPos.New(Large.X + dx, Large.Y + dy)
				if IsWater(c) then Avant[#Avant + 1] = c end
			end
		end
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Mother.RaiseIsland()
		end)
		Trigger.AfterDelay(DateTime.Seconds(12), function()
			local terre = 0
			local plage = 0
			local sx, sy = 0, 0
			for _, c in ipairs(Avant) do
				local t = Map.TerrainType(c)
				if t == "Clear" then terre = terre + 1; sx = sx + c.X; sy = sy + c.Y end
				if t == "Beach" then plage = plage + 1 end
			end
			if terre > 0 then IleCentre = CPos.New(math.floor(sx / terre), math.floor(sy / terre)) end
			Log("ile : " .. terre .. " cases de terre, " .. plage .. " de plage (attendu 36 et 28)")
			-- Un MCV posé au centre de l'île se déploie.
			if IleCentre then
				Log("centre de l'ile : " .. IleCentre.X .. "," .. IleCentre.Y)
				Mcv = Make("mcv", Me, IleCentre)
				Mcv.Deploy()
			end
		end)
		Trigger.AfterDelay(DateTime.Seconds(18), function()
			local n = #Utils.Where(Map.ActorsInWorld, function(a) return a.Type == "fact" and a.Owner == Me end)
			Log("chantier de construction deploye sur l'ile : " .. tostring(n > 0))
		end)
	end

	-- 7. Radar blink : sous-marins ennemis loin de toute unité alliée.
	local ss = Find(100, 100, IsOpenWater)
	if #Grandes > 1 then ss = Grandes[#Grandes] end
	Log("sous-marins ennemis en " .. ss.X .. "," .. ss.Y)
	Ss = Make("ss", En, ss)
	Msub = Make("msub", En, CPos.New(ss.X + 2, ss.Y))
	Make("chantier.avance", Me, Find(64, 64, IsWater))
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Log("avant radar blink : zone vue=" .. tostring(Me.IsExplored(ss)) .. " ss camoufle=" .. tostring(Ss.IsCloaked) .. " msub camoufle=" .. tostring(Msub.IsCloaked))
		Log("radar blink lance : " .. tostring(Me.ActivatePower("AustralouisRadarBlink")))
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Log("pendant radar blink : zone vue=" .. tostring(Me.IsExplored(ss)) .. " ss camoufle=" .. tostring(Ss.IsCloaked) .. " msub camoufle=" .. tostring(Msub.IsCloaked))
	end)

	-- 8. Centrale d'enrichissement et Raffinerie boost (avec un silo).
	local e = Find(90, 60, IsLand)
	Make("centrale.enrichissement", Me, e)
	for i = 0, 2 do Make("apwr", Me, Find(e.X - 8, e.Y + 4 * i, IsLand)) end
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Log("raffinerie boost sans silo : " .. tostring(Me.ActivatePower("AustralouisRaffinerieBoost")))
		Make("silo", Me, Find(e.X + 5, e.Y, IsLand))
	end)
	for t = 4, 12, 4 do
		Trigger.AfterDelay(DateTime.Seconds(t), function()
			Log("t=" .. t .. " energie=" .. Me.PowerState .. " etat=" .. Me.SupportPowerState("AustralouisRaffinerieBoost") .. " raffinerie boost avec silo : " .. tostring(Me.ActivatePower("AustralouisRaffinerieBoost")))
		end)
	end
	for _, t in ipairs({ "cheaper", "l0u15", "sousmarin.icbm", "revolutionnaire" }) do
		Log("apparu : " .. t)
	end
	Make("cheaper", Me, Find(70, 30, IsLand))
	Make("l0u15", Me, Find(20, 64, IsOpenWater))
	Make("sousmarin.icbm", Me, Find(24, 64, IsOpenWater))
end
