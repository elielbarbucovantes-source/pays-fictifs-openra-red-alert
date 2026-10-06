-- Trains blindés : tir automatique, transport, vitesse du Krajina Ekspres, Schwerer Gustav.
Log = function(msg) print("FERRO " .. DateTime.GameTime .. " : " .. msg) end
Check = function(label, ok) Log((ok and "OK    " or "ECHEC ") .. label) end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	En = Player.GetPlayer("Ennemi")
	X0, Y0 = 58, 82
	local m = function(t, p, x, y) return Actor.Create(t, true, { Owner = p, Location = CPos.New(X0 + x, Y0 + y) }) end
	Utils.Do(Map.ActorsInBox(Map.CenterOfCell(CPos.New(X0 - 2, Y0 - 9)), Map.CenterOfCell(CPos.New(X0 + 40, Y0 + 10))),
		function(a) if a.Type ~= "mpspawn" then a.Destroy() end end)
	for x = 0, 36 do m("rail", Me, x, 0) m("rail", Me, x, 8) m("rail", Me, x, -6) end

	-- 1. BP-42 : tir automatique sur un char ennemi à 3 cases, 5 fantassins à bord
	BP42 = m("bp42", Me, 8, 0)
	Cible = m("3tnk", En, 8, 3)
	Cible.Stop()
	local pv0 = Cible.Health
	for i = 1, 6 do
		local e = m("e1", Me, i, 2)
		e.EnterTransport(BP42)
	end
	Trigger.AfterDelay(400, function()
		Check("BP-42 a tiré seul sur le char (PV " .. (Cible.IsDead and 0 or Cible.Health) .. "/" .. pv0 .. ")", Cible.IsDead or Cible.Health < pv0)
		Check("BP-42 : 5 fantassins à bord (" .. BP42.PassengerCount .. ")", BP42.PassengerCount == 5)
	end)

	-- 2. Krajina Ekspres : vitesse (28 cases)
	Krajina = m("krajina", Me, 6, 8)
	Trigger.AfterDelay(20, function()
		T0 = DateTime.GameTime
		Krajina.RailMove(CPos.New(X0 + 31, Y0 + 8))
		local w
		w = function()
			if (Krajina.TrainCars[1].Location.X == X0 + 31 or Krajina.TrainCars[#Krajina.TrainCars].Location.X == X0 + 31) and not Krajina.TrainMoving then
				Log("Krajina : 28 cases en " .. (DateTime.GameTime - T0) .. " ticks")
				Check("Krajina plus rapide que la locomotive diesel (< 140 ticks)", DateTime.GameTime - T0 < 140)
			else
				Trigger.AfterDelay(1, w)
			end
		end
		w()
	end)

	-- 3. Zaamurets et BP-43 : créés et tirent (char ennemi à portée)
	Zaa = m("zaamurets", Me, 20, 0)
	BP43 = m("bp43", Me, 36, 0)
	C2 = m("3tnk", En, 24, 4)
	C3 = m("3tnk", En, 33, 5)
	C2.Stop() C3.Stop()
	local p2, p3 = C2.Health, C3.Health
	Trigger.AfterDelay(400, function()
		Check("Zaamurets a tiré", C2.IsDead or C2.Health < p2)
		Check("BP-43 a tiré", C3.IsDead or C3.Health < p3)
	end)

	Trigger.AfterDelay(10, function()
		Check("BP-42 : 4 voitures (" .. BP42.TrainLength .. ")", BP42.TrainLength == 4)
		Check("BP-43 : 7 voitures (" .. BP43.TrainLength .. ")", BP43.TrainLength == 7)
		Check("Krajina : 3 voitures (" .. Krajina.TrainLength .. ")", Krajina.TrainLength == 3)
		Check("Gustav : 3 voitures (" .. Gustav.TrainLength .. ")", Gustav.TrainLength == 3)
		local s = ""
		for _, c in ipairs(Gustav.TrainCars) do s = s .. " " .. c.Type .. "@" .. (c.Location.X - X0) end
		Log("Gustav :" .. s)
		s = ""
		for _, c in ipairs(BP43.TrainCars) do s = s .. " " .. c.Type .. "@" .. (c.Location.X - X0) end
		Log("BP-43 :" .. s)
		local vus, double = {}, false
		for _, t in ipairs({ BP42, BP43, Krajina, Gustav }) do
			for _, c in ipairs(t.TrainCars) do
				local k = c.Location.X .. "," .. c.Location.Y
				if vus[k] then double = true end
				vus[k] = true
			end
		end
		Check("aucune voiture empilée sur une autre", not double)
	end)

	-- 4. Schwerer Gustav : pas de tir avant la mise en batterie, immobile en batterie, tir à 30 cases
	Gustav = m("gustav", Me, 8, -6)
	Usine = m("powr", En, 33, -8)
	m("e1", Me, 30, -4) -- éclaireur : le Gustav ne voit qu'à 8 cases
	local u0 = Usine.Health
	Trigger.AfterDelay(30, function() Gustav.Attack(Usine, true, true) end)
	Trigger.AfterDelay(330, function()
		Check("Gustav ne tire pas avant la mise en batterie", Usine.Health == u0)
		Gustav.ToggleDeploy()
	end)
	Trigger.AfterDelay(400, function()
		Check("Gustav déployé", Gustav.IsDeployed)
		local x = Gustav.Location.X
		Gustav.RailMove(CPos.New(X0 + 20, Y0 - 6))
		Trigger.AfterDelay(150, function()
			Check("Gustav immobile en batterie", Gustav.Location.X == x)
			Gustav.Stop()
		end)
		Trigger.AfterDelay(250, function()
			Gustav.Attack(Usine, true, true)
			Log("Gustav attaque (en batterie depuis ~70 ticks)")
		end)
		for t = 300, 800, 100 do
			Trigger.AfterDelay(t, function() Log("centrale PV " .. (Usine.IsDead and 0 or Usine.Health) .. ", Gustav inactif=" .. tostring(Gustav.IsIdle) .. " déployé=" .. tostring(Gustav.IsDeployed) .. " peut viser=" .. tostring(Gustav.CanTarget(Usine))) end)
		end
	end)
	Trigger.AfterDelay(1200, function()
		Check("obus du Gustav à 31 cases (PV " .. (Usine.IsDead and 0 or Usine.Health) .. "/" .. u0 .. ")", Usine.IsDead or Usine.Health < u0)
		Gustav.ToggleDeploy()
	end)
	Trigger.AfterDelay(1300, function()
		Check("Gustav replié", not Gustav.IsDeployed)
		local x = Gustav.Location.X
		Gustav.RailMove(CPos.New(X0 + 22, Y0 - 6))
		Trigger.AfterDelay(250, function()
			Check("Gustav repart après repli (" .. (Gustav.Location.X - X0) .. ")", Gustav.Location.X > x)
			Log("FIN")
		end)
	end)
end
