-- Scénario de test automatique du réseau ferroviaire. Lignes préfixées « FERRO » dans lua.log.
Log = function(msg) print("FERRO " .. DateTime.GameTime .. " : " .. msg) end

Make = function(type, owner, x, y)
	return Actor.Create(type, true, { Owner = owner, Location = CPos.New(x, y) })
end

C = function(dx, dy) return CPos.New(X0 + dx, Y0 + dy) end
MakeAt = function(type, owner, dx, dy) return Make(type, owner, X0 + dx, Y0 + dy) end

Check = function(label, ok)
	Log((ok and "OK    " or "ECHEC ") .. label)
end

Pos = function(a) return a.Location.X - X0 .. "," .. a.Location.Y - Y0 end

-- Plan (relatif à X0, Y0) :
--   chantier (0,0) 4×3, voie y=2 de x=0 à 3
--   voie principale y=2 de x=4 à 20, gare (21,1) voie y=2 de x=21 à 24, voie x=25 à 35
--   évitement : (10,3) (10,4) (10,5) → (11..15,5) → (16,5) (16,4) (16,3) → rejoint (16,2)
WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	En = Player.GetPlayer("Ennemi")
	Me.Cash = 20000

	-- Coin le plus dégagé de 38 × 11 cases (score : cases de terrain dégagé)
	local ok = function(c) local t = Map.TerrainType(c) return t == "Clear" or t == "Road" or t == "Rough" end
	local best = -1
	for y = 18, 100, 2 do
		for x = 18, 80, 2 do
			local n = 0
			for dx = -1, 36 do for dy = -1, 9 do
				if ok(CPos.New(x + dx, y + dy)) then n = n + 1 end
			end end
			if n > best then best, X0, Y0 = n, x, y end
		end
	end
	Log("score du site " .. best .. " / " .. 38 * 11)
	Log("site " .. X0 .. "," .. Y0)
	local tl = Map.CenterOfCell(CPos.New(X0 - 2, Y0 - 2))
	local br = Map.CenterOfCell(CPos.New(X0 + 38, Y0 + 9))
	Utils.Do(Map.ActorsInBox(tl, br), function(a) if a.Owner ~= Me and a.Type ~= "mpspawn" then a.Destroy() end end)
	for i = 0, 3 do Make("apwr", Me, X0 + 4 * i, Y0 - 8) end

	Chantier = MakeAt("chantier.ferroviaire", Me, 0, 0)
	Rails = {}
	for x = 4, 20 do Rails[x .. ",2"] = MakeAt("rail", Me, x, 2) end
	Gare = MakeAt("gare", Me, 21, 1)
	for x = 25, 35 do Rails[x .. ",2"] = MakeAt("rail", Me, x, 2) end
	for _, p in ipairs({ { 10, 3 }, { 10, 4 }, { 10, 5 }, { 11, 5 }, { 12, 5 }, { 13, 5 }, { 14, 5 }, { 15, 5 }, { 16, 5 }, { 16, 4 }, { 16, 3 } }) do
		Rails[p[1] .. "," .. p[2]] = MakeAt("rail", Me, p[1], p[2])
	end

	-- 1. Bonus logistique : chantier et gare reliés.
	Trigger.AfterDelay(40, function()
		Check("bonus actif avec chantier et gare reliés", Me.HasPrerequisites({ "reseau.ferroviaire" }))
		-- coupure de la voie principale : l'évitement relie encore
		Rails["13,2"].Kill()
	end)
	Trigger.AfterDelay(80, function()
		Check("bonus toujours actif (évitement) après coupure en x=13", Me.HasPrerequisites({ "reseau.ferroviaire" }))
		Rails["13,5"].Kill()
	end)
	Trigger.AfterDelay(120, function()
		Check("bonus perdu après coupure de l'évitement", not Me.HasPrerequisites({ "reseau.ferroviaire" }))
		Rails["13,2"] = MakeAt("rail", Me, 13, 2)
		Rails["13,5"] = MakeAt("rail", Me, 13, 5)
	end)
	Trigger.AfterDelay(160, function()
		Check("bonus rétabli après réparation", Me.HasPrerequisites({ "reseau.ferroviaire" }))
		-- 2. Production : par la file Train du joueur (onglet Trains)
		Started = DateTime.GameTime
		local ok = Me.Build({ "locomotive.diesel", "wagon.leger", "wagon.lourd" }, function(units)
			Log("produits par la file : " .. #units .. " en " .. (DateTime.GameTime - Started) .. " ticks")
			Trigger.AfterDelay(10, Step3)
		end)
		Check("production lancée dans la file Train", ok)
	end)

	-- 3. Attelage des deux wagons
	Step3 = function()
		Loco = Me.GetActorsByType("locomotive.diesel")[1]
		WL = Me.GetActorsByType("wagon.leger")[1]
		WH = Me.GetActorsByType("wagon.lourd")[1]
		Check("locomotive et wagons produits", Loco ~= nil and WL ~= nil and WH ~= nil)
		if not Loco or not WL or not WH then return end
		Log("loco " .. Pos(Loco) .. " ; léger " .. Pos(WL) .. " ; lourd " .. Pos(WH))
		Check("production sur la voie du chantier", Loco.Location.Y - Y0 == 2 and WL.Location.Y - Y0 == 2 and WH.Location.Y - Y0 == 2)
		-- la locomotive sort du chantier vers l'est, puis vient atteler les deux wagons
		-- (le plus à l'est d'abord : l'autre est derrière lui sur la voie du chantier)
		Loco.RailMove(C(8, 2))
		if WL.Location.X > WH.Location.X then
			Loco.CoupleWagon(WL)
			Loco.CoupleWagon(WH)
		else
			Loco.CoupleWagon(WH)
			Loco.CoupleWagon(WL)
		end
		Trigger.AfterDelay(350, Step4)
	end

	Step4 = function()
		Check("train de 3 voitures après attelage (" .. Loco.TrainLength .. ")", Loco.TrainLength == 3)
		-- 4. Chargement : 10 fantassins dans le wagon léger, 3 jeeps dans le lourd
		Inf = {}
		for i = 1, 10 do
			Inf[i] = MakeAt("e1", Me, i % 5, 6 + math.floor(i / 5))
			Inf[i].EnterTransport(WL)
		end
		Jeeps = {}
		for i = 1, 3 do
			Jeeps[i] = MakeAt("jeep", Me, 4 + i * 2, 8)
			Jeeps[i].EnterTransport(WH)
		end
		-- une 4e jeep : pas de place
		Jeep4 = MakeAt("jeep", Me, 14, 8)
		Jeep4.EnterTransport(WH)
		Trigger.AfterDelay(650, Step5)
	end

	-- 5. Trajet vers la gare (vitesse)
	Step5 = function()
		Check("10 fantassins à bord (" .. WL.PassengerCount .. ")", WL.PassengerCount == 10)
		Check("3 jeeps à bord (" .. WH.PassengerCount .. ")", WH.PassengerCount == 3)
		Check("4e jeep restée dehors", not Jeep4.IsDead and Jeep4.IsInWorld)
		for i = 1, 3 do Log("jeep " .. i .. " : mort=" .. tostring(Jeeps[i].IsDead) .. " dans le monde=" .. tostring(Jeeps[i].IsInWorld)) end
		Log("jeep 4 : mort=" .. tostring(Jeep4.IsDead) .. " dans le monde=" .. tostring(Jeep4.IsInWorld))
		Depart = DateTime.GameTime
		DepartX = Loco.Location.X
		Loco.RailMove(C(24, 2))
		Log("départ de " .. Pos(Loco) .. " (train : " .. Pos(Loco.TrainCars[1]) .. " → " .. Pos(Loco.TrainCars[3]) .. ")")
		Trigger.AfterDelay(5, Step5b)
	end

	Step5b = function()
		ArriveeWatch = function()
			if not Loco.TrainMoving and (Loco.TrainCars[1].Location == C(24, 2) or Loco.TrainCars[3].Location == C(24, 2)) then
				local t = DateTime.GameTime - Depart
				local front = Loco.TrainCars[1]
				Log("arrivée en gare en " .. t .. " ticks ; tête " .. Pos(front))
				Check("tous les wagons en gare", WL.InStation and WH.InStation)
				-- 6. Déchargement en gare
				UnloadStart = DateTime.GameTime
				Loco.UnloadTrain()
				Trigger.AfterDelay(1, UnloadWatch)
			else
				Trigger.AfterDelay(1, ArriveeWatch)
			end
		end
		ArriveeWatch()
	end

	UnloadWatch = function()
		if WL.PassengerCount == 0 and WH.PassengerCount == 0 then
			local t = DateTime.GameTime - UnloadStart
			Log("déchargement en gare : " .. t .. " ticks (10 fantassins + 3 jeeps)")
			Check("déchargement en gare rapide (< 200 ticks)", t < 200)
			Trigger.AfterDelay(25, OffStation)
		else
			if (DateTime.GameTime - UnloadStart) % 100 == 0 then
				Log("déchargement : léger " .. WL.PassengerCount .. ", lourd " .. WH.PassengerCount .. ", léger inactif=" .. tostring(WL.IsIdle) .. ", lourd inactif=" .. tostring(WH.IsIdle))
			end
			Trigger.AfterDelay(1, UnloadWatch)
		end
	end

	-- 7. Rechargement de 4 fantassins, puis déchargement hors gare (x=31)
	OffStation = function()
		for i = 1, 4 do Inf[i].EnterTransport(WL) end
		Trigger.AfterDelay(300, function()
			Check("4 fantassins rembarqués (" .. WL.PassengerCount .. ")", WL.PassengerCount == 4)
			Loco.RailMove(C(33, 2))
			Trigger.AfterDelay(150, function()
				Check("train hors gare", not WL.InStation)
				Check("dételage refusé hors gare", not Loco.UncoupleWagon(WL))
				UnloadStart = DateTime.GameTime
				WL.UnloadPassengers()
				Loco.UnloadTrain()
				Trigger.AfterDelay(1, UnloadWatch2)
			end)
		end)
	end

	UnloadWatch2 = function()
		if WL.PassengerCount == 0 then
			local t = DateTime.GameTime - UnloadStart
			Log("déchargement hors gare : " .. t .. " ticks (4 fantassins)")
			Check("hors gare 4× plus lent (>= 140 ticks pour 4)", t >= 140)
			Trigger.AfterDelay(25, Blocking)
		else
			Trigger.AfterDelay(1, UnloadWatch2)
		end
	end

	-- 8. Voie double : le train A stationne sur la voie principale en x=13 ; la locomotive B (x=19)
	--    va en x=5 tout droit, en le croisant (sans être bloquée).
	Blocking = function()
		Loco.RailMove(C(13, 2))
		Trigger.AfterDelay(250, function()
			Log("train A : " .. Pos(Loco.TrainCars[1]) .. " → " .. Pos(Loco.TrainCars[#Loco.TrainCars]))
			LocoB = MakeAt("locomotive.diesel", Me, 19, 2)
			LocoB.RailMove(C(5, 2))
			Trigger.AfterDelay(400, function()
				Check("locomotive B arrivée en x=5 en croisant le train A (" .. Pos(LocoB) .. ")", LocoB.Location == C(5, 2))
				Overlap()
				Uncouple()
			end)
		end)
		-- voie double : les deux trains partagent des cases en se croisant
		Overlap = function()
			if Overlapped then return end
			if LocoB and not LocoB.IsDead then
				for _, c in ipairs(Loco.TrainCars) do
					if c.Location == LocoB.Location then Overlapped = true end
				end
			end
		end
		local watch
		watch = function() Overlap() if DateTime.GameTime < 6000 then Trigger.AfterDelay(1, watch) end end
		watch()
	end

	-- 9. Dételage en gare, train qui perd un wagon détruit
	Uncouple = function()
		Check("les deux trains se sont croisés sur la même voie", Overlapped)
		Loco.RailMove(C(24, 2))
		Trigger.AfterDelay(200, function()
			Log("train A en gare : " .. Pos(Loco.TrainCars[1]) .. " → " .. Pos(Loco.TrainCars[#Loco.TrainCars]))
			local queue = Loco.TrainCars[#Loco.TrainCars]
			if queue == Loco then queue = Loco.TrainCars[1] end
			Check("dételage accepté en gare", Loco.UncoupleWagon(queue))
			Check("train de 2 voitures après dételage (" .. Loco.TrainLength .. ")", Loco.TrainLength == 2)
			-- infanterie ennemie sur la voie : écrasée au passage
			Victime = MakeAt("e1", En, 28, 2)
			Victime.Stop()
			Loco.RailMove(C(34, 2))
			Trigger.AfterDelay(150, function()
				Check("fantassin ennemi écrasé sur la voie", Victime.IsDead)
				-- wagon détruit en route : pas de plantage, le train continue
				local w = Loco.TrainCars[1] == Loco and Loco.TrainCars[2] or Loco.TrainCars[1]
				Loco.RailMove(C(6, 2))
				Trigger.AfterDelay(20, function()
					w.Kill()
				end)
				Trigger.AfterDelay(400, function()
					Check("wagon détruit retiré du train (" .. Loco.TrainLength .. " : le wagon dételé reste en gare, le train le croise)", Loco.TrainLength == 1 and w.IsDead)
					Log("locomotive en " .. Pos(Loco))
					Crossing()
				end)
			end)
		end)
	end

	-- 10. Un char traverse la voie ; un char ennemi la vise et met du temps à la détruire
	Crossing = function()
		Char = MakeAt("2tnk", Me, 30, 0)
		Char.Move(C(30, 5))
		for t = 50, 300, 50 do
			Trigger.AfterDelay(t, function() Log("char " .. Pos(Char) .. " mort=" .. tostring(Char.IsDead) .. " inactif=" .. tostring(Char.IsIdle)) end)
		end
		Trigger.AfterDelay(300, function()
			Check("le char a traversé la voie (" .. Pos(Char) .. ")", Char.Location.Y - Y0 > 2)
			Shooting()
		end)
	end

	Shooting = function()
		local cible = Rails["34,2"]
		Tireur = MakeAt("3tnk", En, 34, 6)
		Tireur.Attack(cible, true, true)
		local t0 = DateTime.GameTime
		Trigger.OnKilled(cible, function()
			Log("segment de voie détruit par un char lourd en " .. (DateTime.GameTime - t0) .. " ticks")
		end)
		Trigger.AfterDelay(1200, function()
			Log("segment visé : mort=" .. tostring(cible.IsDead) .. (cible.IsDead and "" or (" PV " .. cible.Health .. "/" .. cible.MaxHealth)))
			Log("FIN")
		end)
	end
end
