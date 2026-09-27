-- Scénario de test automatique des nouveautés elielistanaises.
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

IsLand = function(c)
	local t = Map.TerrainType(c)
	return t == "Clear" or t == "Road" or t == "Rough"
end

IsWater = function(c) return Map.TerrainType(c) == "Water" end

-- Première case de terre libre la plus proche de (x, y).
FindLand = function(x, y)
	for d = 0, 30 do
		for dx = -d, d do
			for dy = -d, d do
				local c = CPos.New(x + dx, y + dy)
				if IsLand(c) then return c end
			end
		end
	end
end

-- Case d'eau profonde qui touche la terre (pour le porte-avions).
FindCoast = function(x, y)
	for d = 0, 60 do
		for dx = -d, d do
			for dy = -d, d do
				local c = CPos.New(x + dx, y + dy)
				if IsWater(c) and (IsLand(CPos.New(c.X + 1, c.Y)) or IsLand(CPos.New(c.X - 1, c.Y)) or IsLand(CPos.New(c.X, c.Y + 1)) or IsLand(CPos.New(c.X, c.Y - 1))) then
					return c
				end
			end
		end
	end
end

Count = function(type, owner)
	return #Utils.Where(Map.ActorsInWorld, function(a) return a.Type == type and a.Owner == owner and not a.IsDead end)
end

Make = function(type, owner, cell, inWorld)
	if inWorld == nil then inWorld = true end
	return Actor.Create(type, inWorld, { Owner = owner, Location = cell })
end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	En = Player.GetPlayer("Ennemi")
	Log("debut, mpspawn terre = " .. tostring(FindLand(35, 66)))

	-- 1. Nathan contre une centrale ennemie à ~7 cases.
	local n = FindLand(30, 40)
	Nathan = Make("nathan", Me, n)
	Powr = Make("powr", En, FindLand(n.X + 7, n.Y))
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Log("Nathan camoufle apres 4 s : " .. tostring(Nathan.IsCloaked))
		Nathan.Attack(Powr, true, false)
	end)
	Trigger.AfterDelay(DateTime.Seconds(12), function()
		Log("Nathan : centrale detruite = " .. tostring(Powr.IsDead) .. ", Nathan vivant = " .. tostring(not Nathan.IsDead))
	end)

	-- 2. Pelícano : 5 chars, puis un 2e avec 15 fantassins.
	local a = FindLand(60, 60)
	Aero = Make("aerodromo", Me, a)
	Peli = Make("pelicano", Me, FindLand(a.X, a.Y + 4))
	for i = 1, 2 do Peli.LoadPassenger(Make("aguila.nuclear", Me, a, false)) end
	for i = 1, 3 do Peli.LoadPassenger(Make("carro.aguila", Me, a, false)) end
	Peli2 = Make("pelicano", Me, FindLand(a.X + 4, a.Y + 4))
	for i = 1, 15 do Peli2.LoadPassenger(Make("aguila", Me, a, false)) end
	Log("Pelicano charges : " .. Peli.PassengerCount .. " chars, " .. Peli2.PassengerCount .. " fantassins")
	Drop1 = FindLand(a.X + 12, a.Y - 10)
	Drop2 = FindLand(a.X - 12, a.Y - 8)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Peli.ParaDropAt(Drop1)
		Peli2.ParaDropAt(Drop2)
	end)
	Trigger.AfterDelay(DateTime.Seconds(45), function()
		Log("Pelicano 1 : a bord = " .. Peli.PassengerCount .. ", peut relarguer = " .. tostring(Peli.CanParaDrop))
		Log("Pelicano 2 : a bord = " .. Peli2.PassengerCount)
		Log("Au sol : aguila.nuclear=" .. Count("aguila.nuclear", Me) .. " carro.aguila=" .. Count("carro.aguila", Me) .. " aguila=" .. Count("aguila", Me))
	end)

	-- 3. Aguila Carrier : 5 chars chargés puis débarqués.
	local c = FindLand(45, 80)
	Carrier = Make("aguila.carrier", Me, c)
	for i = 1, 5 do Carrier.LoadPassenger(Make("carro.aguila", Me, c, false)) end
	Log("Aguila Carrier charge : " .. Carrier.PassengerCount)
	Trigger.AfterDelay(DateTime.Seconds(2), function() Carrier.UnloadPassengers() end)
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Log("Aguila Carrier apres debarquement : " .. Carrier.PassengerCount .. " a bord")
	end)

	-- 4. Aguila Nuclear détruit : explosion + zone contaminée.
	local z = FindLand(70, 80)
	Victim = Make("aguila.nuclear", Me, z)
	Neighbour = Make("carro.aguila", Me, FindLand(z.X + 2, z.Y))
	Trigger.AfterDelay(DateTime.Seconds(2), function() Victim.Kill() end)
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Log("Zone contaminee presente : " .. (Count("zone.radioactive.1", Me) + Count("zone.radioactive.2", Me)))
		Log("Voisin (2 cases) PV : " .. Neighbour.Health .. "/" .. Neighbour.MaxHealth)
	end)
	Trigger.AfterDelay(DateTime.Seconds(15), function()
		local left = 0
		for i = 1, 5 do left = left + Count("zone.radioactive." .. i, Me) end
		Log("Zone contaminee apres 15 s : " .. left .. " ; voisin PV : " .. (Neighbour.IsDead and "mort" or Neighbour.Health))
	end)

	-- 5. Porte-avions : 2 Harpía appontent, relance, débarquement.
	local w = FindCoast(60, 60)
	Log("Case cotiere : " .. tostring(w))
	Porta = Make("porta.aguila", Me, w)
	for i = 1, 5 do Porta.LoadPassenger(Make("aguila", Me, w, false)) end
	H1 = Make("harpia", Me, FindLand(w.X + 6, w.Y + 6))
	H2 = Make("harpia", Me, FindLand(w.X - 6, w.Y + 6))
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		H1.ReturnToBase(Porta)
		H2.ReturnToBase(Porta)
		Porta.UnloadPassengers()
	end)
	Trigger.AfterDelay(DateTime.Seconds(25), function()
		Log("Porte-avions : avions a bord = " .. Porta.AircraftOnBoard .. ", fantassins a bord = " .. Porta.PassengerCount)
		Porta.LaunchAircraft()
	end)
	Trigger.AfterDelay(DateTime.Seconds(35), function()
		Log("Porte-avions apres decollage : " .. Porta.AircraftOnBoard .. " a bord, harpia en vol = " .. Count("harpia", Me))
	end)

	-- 6. Bombardiers contre une usine ennemie.
	local b = FindLand(80, 40)
	Weap = Make("weap", En, b)
	B2 = Make("b2.spirit", Me, FindLand(b.X - 20, b.Y + 10))
	Tact = Make("bombardier.tactique", Me, FindLand(b.X - 20, b.Y + 12))
	Kirov = Make("kirov", Me, FindLand(b.X - 8, b.Y + 4))
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		B2.Attack(Weap, true, false)
		Tact.Attack(Weap, true, false)
		Kirov.Attack(Weap, true, false)
	end)
	Trigger.AfterDelay(DateTime.Seconds(40), function()
		Log("Usine bombardee : " .. (Weap.IsDead and "detruite" or ("PV " .. Weap.Health .. "/" .. Weap.MaxHealth)))
	end)

	-- 7. Tranchée garnie contre de l'infanterie ennemie ; antenne.
	local t = FindLand(35, 90)
	Trench = Make("tranchee", Me, t)
	for i = 1, 5 do Trench.LoadPassenger(Make("aguila", Me, t, false)) end
	Foes = {}
	for i = 1, 4 do Foes[i] = Make("e1", En, FindLand(t.X + 4, t.Y + i - 2)) end
	Antena = Make("antena", Me, FindLand(20, 30))
	Trigger.AfterDelay(DateTime.Seconds(15), function()
		local alive = #Utils.Where(Foes, function(f) return not f.IsDead end)
		Log("Tranchee : " .. Trench.PassengerCount .. " a l'abri, fantassins ennemis restants = " .. alive)
		Log("Antenne en vie : " .. tostring(not Antena.IsDead))
	end)

	-- 8. Avispa : un fantassin tué d'un coup ; un autre drone abattu par un SAM.
	local v = FindLand(25, 80)
	Tanya = Make("e7", En, FindLand(v.X + 3, v.Y))
	Avispa = Make("avispa", Me, v)
	local s = FindLand(90, 75)
	Sam = Make("agun", En, s)
	Avispa2 = Make("avispa", Me, FindLand(s.X - 3, s.Y))
	Trigger.AfterDelay(DateTime.Seconds(1), function() if not Avispa2.IsDead then Avispa2.Move(FindLand(s.X + 2, s.Y)) end end)
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Log("Avispa : Tanya morte = " .. tostring(Tanya.IsDead) .. ", drone disparu = " .. tostring(Avispa.IsDead or not Avispa.IsInWorld))
		Log("Avispa au-dessus de la DCA : abattu = " .. tostring(Avispa2.IsDead))
	end)

	Trigger.AfterDelay(DateTime.Seconds(70), function()
		Log("Pelicano 2 a 70 s : a bord = " .. Peli2.PassengerCount .. ", au repos = " .. tostring(Peli2.IsIdle) .. ", position = " .. Peli2.Location.X .. "," .. Peli2.Location.Y .. " aerodrome = " .. Aero.Location.X .. "," .. Aero.Location.Y)
		Log("FIN")
	end)
end
