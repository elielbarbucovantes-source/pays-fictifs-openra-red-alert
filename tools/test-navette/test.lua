-- Test des navettes ferroviaires : usines → gare A (stock), train en navette A → B, arrêt manuel,
-- reprise, « Vider la gare », gare détruite. Lignes préfixées « NAV » dans lua.log.
Log = function(msg) print("NAV " .. DateTime.GameTime .. " : " .. msg) end
Check = function(label, ok) Log((ok and "OK    " or "ECHEC ") .. label) end
Make = function(type, x, y) return Actor.Create(type, true, { Owner = Me, Location = CPos.New(X0 + x, Y0 + y) }) end

-- Unités d'un type dans le monde autour d'un point (relatif)
CountNear = function(types, x, y, r)
	local tl = Map.CenterOfCell(CPos.New(X0 + x - r, Y0 + y - r))
	local br = Map.CenterOfCell(CPos.New(X0 + x + r, Y0 + y + r))
	local n = 0
	Utils.Do(Map.ActorsInBox(tl, br), function(a)
		if a.Owner == Me then
			for _, t in ipairs(types) do if a.Type == t then n = n + 1 end end
		end
	end)
	return n
end

Aboard = function()
	return WLeger.PassengerCount + WLourd.PassengerCount
end

-- Plan : gare A (2,1) voie y=2 x=2..5 ; rails x=6..25 ; gare B (26,1) voie x=26..29 ; rails x=30..33.
-- Caserne et usine au sud de la gare A, train garé sur la voie entre les deux gares.
WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	Me.Cash = 50000
	local ok = function(c) local t = Map.TerrainType(c) return t == "Clear" or t == "Road" or t == "Rough" end
	local best = -1
	for y = 18, 100, 2 do
		for x = 18, 80, 2 do
			local n = 0
			for dx = -1, 36 do for dy = -1, 11 do
				if ok(CPos.New(x + dx, y + dy)) then n = n + 1 end
			end end
			if n > best then best, X0, Y0 = n, x, y end
		end
	end
	Log("site " .. X0 .. "," .. Y0 .. " score " .. best)
	Utils.Do(Map.ActorsInBox(Map.CenterOfCell(CPos.New(X0 - 2, Y0 - 9)), Map.CenterOfCell(CPos.New(X0 + 38, Y0 + 12))),
		function(a) if a.Owner ~= Me and a.Type ~= "mpspawn" then a.Destroy() end end)
	for i = 0, 3 do Make("apwr", 4 * i, -8) end

	GareA = Make("gare", 2, 1)
	for x = 6, 25 do Make("rail", x, 2) end
	GareB = Make("gare", 26, 1)
	for x = 30, 33 do Make("rail", x, 2) end
	Barr = Make("barr", 1, 6)
	Weap = Make("weap", 6, 6)

	Loco = Make("locomotive.diesel", 16, 2)
	WLeger = Make("wagon.leger", 14, 2)
	WLourd = Make("wagon.lourd", 12, 2)

	if Quick then Trigger.AfterDelay(10, Part2) return end
	Trigger.AfterDelay(5, function()
		WLeger.CoupleTo(WLourd)
		Loco.CoupleTo(WLeger)
		-- point de ralliement sur la gare (case centrale, comme le clic droit sur la gare)
		local centre = CPos.New(GareA.Location.X + 2, GareA.Location.Y + 1)
		Barr.RallyPoint = centre
		Weap.RallyPoint = centre
		for i = 1, 6 do Barr.Produce("e1") end
		for i = 1, 2 do Weap.Produce("jeep") end
	end)

	Trigger.AfterDelay(500, function()
		Check("8 unités produites entrées dans la gare A (stock " .. GareA.StoredUnits .. ")", GareA.StoredUnits == 8)
		Check("aucune unité restée dehors près de la gare A", CountNear({ "e1", "jeep" }, 4, 3, 6) == 0)
		Loco.StartShuttle(GareA, GareB)
		T0 = DateTime.GameTime
		Trigger.AfterDelay(5, Watch)
	end)
end

Phase = "charge"
Watch = function()
	local t = DateTime.GameTime - T0
	if Phase == "charge" and Aboard() == 8 and GareA.StoredUnits == 0 then
		Log("chargé en " .. t .. " ticks (inf " .. WLeger.PassengerCount .. ", véh " .. WLourd.PassengerCount .. ")")
		Phase = "trajet"
	elseif Phase == "trajet" and Aboard() == 0 then
		Log("déchargé à " .. t .. " ticks")
		Phase = "decharge"
		Trigger.AfterDelay(150, AfterUnload)
		return
	end
	if t > 4000 then
		Check("navette complète (phase bloquée : " .. Phase .. ", à bord " .. Aboard() .. ", stock A " .. GareA.StoredUnits .. ")", false)
		Log("FIN")
		return
	end
	Trigger.AfterDelay(5, Watch)
end

AfterUnload = function()
	Check("e1 et jeeps déposés autour de la gare B (" .. CountNear({ "e1", "jeep" }, 28, 3, 7) .. ")", CountNear({ "e1", "jeep" }, 28, 3, 7) == 8)
	Check("navette toujours active (retour vers A)", Loco.ShuttleActive)
	-- ordre manuel : la navette s'arrête
	Loco.Stop()
	Trigger.AfterDelay(10, function()
		Check("ordre manuel (Stop) : navette arrêtée", not Loco.ShuttleActive)
		Loco.ResumeShuttle()
		Trigger.AfterDelay(10, function()
			Check("bouton Navette : reprise", Loco.ShuttleActive)
			Loco.Stop()
			-- nouvelles unités dans la gare A, puis « Vider la gare »
			for i = 1, 3 do Barr.Produce("e1") end
			Trigger.AfterDelay(350, function()
				Check("3 nouvelles unités stockées (" .. GareA.StoredUnits .. ")", GareA.StoredUnits == 3)
				GareA.EmptyStation()
				Trigger.AfterDelay(120, function()
					Check("gare vidée (" .. GareA.StoredUnits .. ")", GareA.StoredUnits == 0)
					Check("3 e1 sortis près de la gare A (" .. CountNear({ "e1" }, 4, 4, 6) .. ")", CountNear({ "e1" }, 4, 4, 6) == 3)
					Utils.Do(Map.ActorsInBox(Map.CenterOfCell(CPos.New(X0 - 3, Y0 - 3)), Map.CenterOfCell(CPos.New(X0 + 11, Y0 + 11))),
						function(a) if a.Type == "e1" then a.Destroy() end end)
					for i = 1, 2 do Barr.Produce("e1") end
					Trigger.AfterDelay(300, function()
						Check("2 unités stockées avant destruction (" .. GareA.StoredUnits .. ")", GareA.StoredUnits == 2)
						GareA.Kill()
						Trigger.AfterDelay(30, function()
							Check("gare détruite : les 2 unités ressortent (" .. CountNear({ "e1" }, 4, 3, 6) .. ")", CountNear({ "e1" }, 4, 3, 6) == 2)
							Part2()
						end)
					end)
				end)
			end)
		end)
	end)
end

-- 2e partie : garnison sur ordre, voie double (croisement), atterrissage sur le train aérien en marche.
-- Ligne y=11 de x=0 à 35.
Part2 = function()
	for x = 0, 35 do Make("rail", x, 11) end
	local before = GareB.StoredUnits
	G = {}
	for i = 1, 3 do G[i] = Make("e1", 30 + i, 5) end
	G[4] = Make("jeep", 34, 6)
	Trigger.AfterDelay(5, function()
		Utils.Do(G, function(u) u.EnterStation(GareB) end)
		T1 = Make("locomotive.diesel", 4, 11)
		T2 = Make("locomotive.diesel", 32, 11)
		Trigger.AfterDelay(5, function()
			T1.RailMove(CPos.New(X0 + 34, Y0 + 11))
			T2.RailMove(CPos.New(X0 + 1, Y0 + 11))
			Start = DateTime.GameTime
			Trigger.AfterDelay(5, WatchCross)
		end)
	end)
	Trigger.AfterDelay(300, function()
		Check("4 unités en garnison dans la gare B (" .. (GareB.StoredUnits - before) .. ")", GareB.StoredUnits - before == 4)
		Utils.Do(G, function(u)
			if u.IsInWorld then Log("  dehors : " .. u.Type .. " en " .. (u.Location.X - X0) .. "," .. (u.Location.Y - Y0) .. (u.IsDead and " (mort)" or "") .. (u.IsIdle and " inactif" or "")) end
		end)
	end)
end

WatchCross = function()
	local t = DateTime.GameTime - Start
	if T1.Location.X - X0 >= 34 and T2.Location.X - X0 <= 2 then
		Check("voie double : les deux trains se sont croisés et sont arrivés (" .. t .. " ticks)", true)
		Trigger.AfterDelay(5, AirTrain)
		return
	end
	if t > 900 then
		Check("voie double : croisement (T1 " .. (T1.Location.X - X0) .. ", T2 " .. (T2.Location.X - X0) .. ")", false)
		Log("FIN")
		return
	end
	Trigger.AfterDelay(5, WatchCross)
end

AirTrain = function()
	LocoA = Make("locomotive.aerienne", 22, 11)
	Atelier = Make("wagon.atelier.aerien", 20, 11)
	H = {}
	for i = 1, 3 do H[i] = Make("harpia", 24 + 2 * i, 1) end
	Trigger.AfterDelay(5, function()
		Atelier.CoupleTo(LocoA)
		Trigger.AfterDelay(20, function()
			LocoA.RailMove(CPos.New(X0 + 6, Y0 + 11))
			Trigger.AfterDelay(15, function()
				Check("train aérien en marche", LocoA.Location.X - X0 < 22)
				Utils.Do(H, function(h) h.LandOn(Atelier) end)
				Start = DateTime.GameTime
				Trigger.AfterDelay(5, WatchLanding)
			end)
		end)
	end)
end

Aboard3 = function()
	local n = 0
	Utils.Do(H, function(h) if not h.IsInWorld and not h.IsDead then n = n + 1 end end)
	return n
end

WatchLanding = function()
	local t = DateTime.GameTime - Start
	if Aboard3() == 3 then
		Check("3 Harpía posées sur le wagon en marche en " .. t .. " ticks (à bord : " .. Atelier.AircraftOnBoard .. ")", Atelier.AircraftOnBoard == 3)
		Landed = DateTime.GameTime
		Trigger.AfterDelay(5, WatchTakeoff)
		return
	end
	if t > 900 then
		Check("3 Harpía posées sur le wagon en marche (" .. Aboard3() .. " à bord)", false)
		Log("FIN")
		return
	end
	Trigger.AfterDelay(5, WatchLanding)
end

-- RearmDelay 375 : elles doivent redécoller seules une fois prêtes.
WatchTakeoff = function()
	local t = DateTime.GameTime - Landed
	if Aboard3() == 0 then
		Check("les 3 Harpía redécollent seules une fois réarmées (" .. t .. " ticks après la 3e)", Atelier.AircraftOnBoard == 0)
		Part3()
		return
	end
	if t > 700 then
		Check("redécollage automatique (" .. Aboard3() .. " encore à bord)", false)
		Log("FIN")
		return
	end
	Trigger.AfterDelay(5, WatchTakeoff)
end

-- 3e partie : point de ralliement de la gare B posé sur une gare C → les unités qui sortent de B
-- (« Vider la gare », train déchargé à la main en gare B) vont à pied jusqu'à C et y entrent.
Part3 = function()
	GareC = Make("gare", 14, 5)
	GareB.RallyPoint = CPos.New(GareC.Location.X + 2, GareC.Location.Y + 1)
	local fromB = GareB.StoredUnits
	Log("stock B avant relais : " .. fromB)
	-- train à quai en gare B : wagon léger (2 cases) + locomotive, 2 fantassins à bord
	WL2 = Make("wagon.leger", 27, 2)
	Loco2 = Make("locomotive.diesel", 29, 2)
	Trigger.AfterDelay(5, function()
		WL2.CoupleTo(Loco2)
		for i = 1, 2 do WL2.LoadPassenger(Actor.Create("e1", false, { Owner = Me })) end
		GareB.EmptyStation()
		Trigger.AfterDelay(20, function()
			Check("train à quai chargé (" .. WL2.PassengerCount .. ")", WL2.PassengerCount == 2)
			Loco2.UnloadTrain()
		end)
		Trigger.AfterDelay(700, function()
			Check("gare B vidée (" .. GareB.StoredUnits .. ")", GareB.StoredUnits == 0)
			Check("relais : stock B (" .. fromB .. ") + 2 du train entrés dans la gare C (" .. GareC.StoredUnits .. ")", GareC.StoredUnits == fromB + 2)
			Log("FIN")
		end)
	end)
end
