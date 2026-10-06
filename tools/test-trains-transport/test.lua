-- Trains de transport par pays : gare portuaire, wagon porte-bateaux, wagon industriel, train aérien.
-- Lignes préfixées « TT » dans lua.log.
Log = function(msg) print("TT " .. DateTime.GameTime .. " : " .. msg) end
Check = function(label, ok) Log((ok and "OK    " or "ECHEC ") .. label) end

IsType = function(c, t)
	if c.X < 2 or c.Y < 2 or c.X > 149 or c.Y > 149 then return false end
	return Map.TerrainType(c) == t
end
IsLand = function(c) return IsType(c, "Clear") or IsType(c, "Beach") or IsType(c, "Rough") or IsType(c, "Road") end
IsWater = function(c) return IsType(c, "Water") end

-- Côte nord→sud : 2 rangées de terre (bâtiment, voie) puis 3 rangées d'eau (quai, navires).
IsSite = function(x, y)
	for dx = -1, 4 do
		if not IsLand(CPos.New(x + dx, y)) or not IsLand(CPos.New(x + dx, y + 1)) then return false end
	end
	-- quai : au moins les 2 cases du milieu dans l'eau (comme PortBuilding), eau dessous pour les navires
	for dx = 1, 2 do
		for dy = 2, 4 do
			if not IsWater(CPos.New(x + dx, y + dy)) then return false end
		end
	end
	for dx = 0, 3 do
		if not (IsWater(CPos.New(x + dx, y + 2)) or IsLand(CPos.New(x + dx, y + 2))) then return false end
	end
	return true
end

M = function(t, x, y) return Actor.Create(t, true, { Owner = Me, Location = CPos.New(x, y) }) end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	Me.Cash = 50000

	-- Carte Archipelago (152 × 152) ; marge pour la voie du train aérien au nord.
	for y = 8, 140 do
		for x = 6, 140 do
			if not X0 and IsSite(x, y) then X0, Y0 = x, y end
		end
	end
	if not X0 then Log("ECHEC pas de côte trouvée") Log("FIN") return end
	Log("site " .. X0 .. "," .. Y0)
	Utils.Do(Map.ActorsInBox(Map.CenterOfCell(CPos.New(X0 - 3, Y0 - 3)), Map.CenterOfCell(CPos.New(X0 + 6, Y0 + 6))), function(a)
		if a.Owner ~= Me and a.Type ~= "mpspawn" then a.Destroy() end
	end)

	-- 1. Gare portuaire, wagons en gare (2 cases chacun ; Location = bout est, la voiture s'étend vers l'ouest),
	--    transport naval chargé à quai
	Port = M("gare.portuaire", X0, Y0)
	WLourd = M("wagon.lourd.aus", X0 + 1, Y0 + 1)
	WLeger = M("wagon.leger.aus", X0 + 3, Y0 + 1)
	Lst = M("lst", X0 + 1, Y0 + 3)
	for i = 1, 2 do Lst.LoadPassenger(Actor.Create("jeep", false, { Owner = Me })) end
	for i = 1, 3 do Lst.LoadPassenger(Actor.Create("e1", false, { Owner = Me })) end
	Pt = M("pt", X0 + 2, Y0 + 4)
	-- voie à terre au nord du port : wagon industriel puis train aérien
	for x = -1, 9 do M("rail", X0 + x, Y0 - 3) end

	-- Usine + chantier pour la file Train (pays : Australouis), sur un bloc de terre de 16 × 7 loin du port
	for y = 6, 140, 3 do
		for x = 6, 130, 3 do
			if not BX and (x > X0 + 12 or y > Y0 + 12) then
				local ok = true
				for dx = 0, 15 do for dy = 0, 6 do
					if ok and not IsLand(CPos.New(x + dx, y + dy)) then ok = false end
				end end
				if ok then BX, BY = x, y end
			end
		end
	end
	Log("base " .. tostring(BX) .. "," .. tostring(BY))
	M("weap", BX, BY)
	M("chantier.ferroviaire", BX + 4, BY)
	for i = 0, 2 do M("apwr", BX + 9 + 2 * i, BY + 4) end

	Trigger.AfterDelay(10, function()
		Check("trains communs cachés (vehicles.australouis présent)", Me.HasPrerequisites({ "vehicles.australouis" }))
		Check("file Train : locomotive côtière proposée", Me.Build({ "locomotive.aus" }))
		Log("longueurs : lourd " .. WLourd.TrainLength .. " voiture(s), cases " .. (WLourd.Location.X - X0) .. " ; léger " .. (WLeger.Location.X - X0))
	end)

	Trigger.AfterDelay(250, function()
		Check("transport naval vidé à quai (" .. Lst.PassengerCount .. ")", Lst.PassengerCount == 0)
		Check("jeeps passées dans le wagon lourd (" .. WLourd.PassengerCount .. ")", WLourd.PassengerCount == 2)
		Check("fantassins passés dans le wagon léger (" .. WLeger.PassengerCount .. ")", WLeger.PassengerCount == 3)
		WLourd.UnloadTrain()
		WLeger.UnloadTrain()
		-- wagon industriel (3 cases) sur la voie nord, un char abîmé y monte
		WIndus = M("wagon.industriel", X0 + 3, Y0 - 3)
		Tank = M("3tnk", X0 + 5, Y0 - 5)
		Tank.Health = Tank.MaxHealth / 2
		Tank.EnterTransport(WIndus)
	end)

	Trigger.AfterDelay(340, function()
		Check("jeeps descendues du wagon lourd (" .. WLourd.PassengerCount .. ")", WLourd.PassengerCount == 0)
		Check("fantassins descendus du wagon léger (" .. WLeger.PassengerCount .. ")", WLeger.PassengerCount == 0)
		WLourd.Destroy()
		WLeger.Destroy()
		Trigger.AfterDelay(2, function()
			WBateaux = M("wagon.bateaux", X0 + 3, Y0 + 1)
			Pt.EnterTransport(WBateaux)
		end)
		Check("char monté dans le wagon industriel", not Tank.IsInWorld)
		WIndus.UnloadTrain()
	end)

	Trigger.AfterDelay(560, function()
		Check("vedette embarquée sur le wagon porte-bateaux (" .. WBateaux.PassengerCount .. ")", WBateaux.PassengerCount == 1 and not Pt.IsInWorld)
		WBateaux.UnloadTrain()
		Check("char descendu du wagon industriel", Tank.IsInWorld)
		Hp0 = Tank.Health
	end)

	Trigger.AfterDelay(760, function()
		Check("vedette débarquée sur l'eau", Pt.IsInWorld and IsWater(Pt.Location))
		Check("char réparé après la descente (" .. Hp0 .. " → " .. Tank.Health .. ")", Tank.Health > Hp0)
		WIndus.Destroy()
		-- 2. Train aérien sur la voie nord : hélicoptère vide et abîmé au-dessus, Harpía vide qui vient se poser
		Trigger.AfterDelay(2, function()
			Loco = M("locomotive.aerienne", X0 + 9, Y0 - 3)
			Mun = M("wagon.munitions", X0 + 7, Y0 - 3)
			Atelier = M("wagon.atelier.aerien", X0 + 4, Y0 - 3)
			Carbu = M("wagon.carburant", X0 + 1, Y0 - 3)
			Heli = M("heli", X0 + 2, Y0 - 6)
			Heli.Reload("primary", -100)
			Heli.Health = Heli.MaxHealth / 2
			Heli.Move(CPos.New(X0 + 5, Y0 - 4))
			StockAvant = Mun.AmmoCount("stock")
			Harpia = M("harpia", X0 + 12, Y0 - 12)
			Harpia.Reload("primary", -100)
			Harpia.ReturnToBase(Atelier)
		end)
	end)
	Trigger.AfterDelay(960, function()
		Check("hélicoptère en vol", Heli.IsInWorld and not Heli.IsDead)
		Check("hélicoptère réarmé par le wagon (" .. Heli.AmmoCount("primary") .. ")", Heli.AmmoCount("primary") > 0)
		Check("stock du wagon entamé (" .. StockAvant .. " → " .. Mun.AmmoCount("stock") .. ")", Mun.AmmoCount("stock") < StockAvant)
		HeliHp = Heli.Health
	end)
	Trigger.AfterDelay(1260, function()
		Check("hélicoptère réparé par l'atelier (" .. HeliHp .. " → " .. Heli.Health .. ")", Heli.Health > HeliHp)
		Check("Harpía posée sur le wagon-atelier", not Harpia.IsInWorld and not Harpia.IsDead)
		-- 3. Train nucléaire : explosion du réacteur à la destruction (ennemis et alliés)
		Utils.Do({ Loco, Mun, Atelier, Carbu }, function(a) a.Destroy() end)
		Trigger.AfterDelay(2, function()
			Nuc = M("locomotive.nucleaire", X0 + 9, Y0 - 3)
			Ennemi = Actor.Create("3tnk", true, { Owner = Player.GetPlayer("Ennemi"), Location = CPos.New(X0 + 9, Y0 - 6) })
			Allie = M("e1", X0 + 6, Y0 - 5)
			EnnemiHp = Ennemi.Health
		end)
		Trigger.AfterDelay(20, function()
			Check("train nucléaire : 3 cases, " .. Nuc.TrainLength .. " voiture", Nuc.TrainLength == 1)
			Nuc.Kill()
		end)
		Trigger.AfterDelay(60, function()
			Check("explosion : char ennemi à 3 cases touché (" .. EnnemiHp .. " → " .. (Ennemi.IsDead and 0 or Ennemi.Health) .. ")", Ennemi.IsDead or Ennemi.Health < EnnemiHp)
			Check("explosion : fantassin allié tué", Allie.IsDead)
			Log("FIN")
		end)
	end)
end
