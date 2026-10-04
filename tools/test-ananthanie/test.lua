-- Scénario de test automatique de l'Ananthanie (lot 1).
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

IsLand = function(c)
	if c.X < 1 or c.Y < 1 or c.X > 126 or c.Y > 126 then return false end
	local t = Map.TerrainType(c)
	return t == "Clear" or t == "Road"
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

Hp = function(a) return a.IsDead and "mort" or tostring(a.Health) end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	En = Player.GetPlayer("Ennemi")

	-- 1. Tout apparaît.
	local b = Find(60, 60, IsLand)
	for _, t in ipairs({ "thal", "khesh.thal", "nayrak", "lunkar", "karnthal", "vidra", "lunkra", "karnvasha",
		"ratha", "mukhar", "agnar", "ambarkesh", "nayrath" }) do
		local a = Make(t, Me, Find(b.X, b.Y, IsLand))
		Log("apparu : " .. t .. " PV=" .. a.Health)
	end
	for _, t in ipairs({ "haut.commandement", "fabrique.drones", "institut", "kheshkarn", "batterie.ambar", "ruche" }) do
		Log("batiment : " .. t .. " cout=" .. Actor.Cost(t))
	end

	-- 2. Protection active : mêmes missiles sur un Karnvasha et un Mammouth.
	local p = Find(30, 40, IsLand)
	Karn = Make("karnvasha", Me, p)
	Mam = Make("4tnk", Me, Find(p.X, p.Y + 8, IsLand))
	Karn.Stance = "HoldFire"
	Mam.Stance = "HoldFire"
	Make("camera", En, p)
	Make("camera", En, Mam.Location)
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		local s1 = Make("e3", En, Find(p.X + 4, p.Y, IsLand))
		local s2 = Make("e3", En, Find(p.X + 4, p.Y + 8, IsLand))
		s1.Attack(Karn)
		s2.Attack(Mam)
		Trigger.AfterDelay(DateTime.Seconds(12), function()
			Log("protection active : Karnvasha a perdu " .. (80000 - Karn.Health) .. ", Mammouth a perdu " .. (Mam.MaxHealth - Mam.Health) .. " (attendu ~65 %)")
			s1.Destroy(); s2.Destroy()
		end)
	end)

	-- 3. Mukhar : 4 munitions rôdeuses, attaque d'un char.
	local m = Find(80, 40, IsLand)
	Muk = Make("mukhar", Me, m)
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		local n = #Utils.Where(Map.ActorsInWorld, function(a) return a.Type == "rodeuse" and a.Owner == Me end)
		Log("munitions rodeuses : " .. n .. " (attendu 4)")
		Cible = Make("2tnk", En, Find(m.X + 6, m.Y, IsLand))
		Make("camera", Me, Cible.Location)
	end)
	Trigger.AfterDelay(DateTime.Seconds(20), function()
		Log("char attaque par les rodeuses : PV=" .. Hp(Cible))
	end)

	-- 4. Nayrak : drone FPV sur un char léger.
	local n = Find(40, 85, IsLand)
	Nay = Make("nayrak", Me, n)
	Leger = Make("1tnk", En, Find(n.X + 5, n.Y, IsLand))
	Leger.Stance = "HoldFire"
	Trigger.AfterDelay(DateTime.Seconds(1), function() Nay.Attack(Leger) end)
	Trigger.AfterDelay(DateTime.Seconds(6), function() Log("Nayrak : char leger PV=" .. Hp(Leger) .. " / 26000") end)

	-- 5. Ruche : contre un hélicoptère et une jeep.
	local r = Find(85, 80, IsLand)
	Make("ruche", Me, r)
	Make("powr", Me, Find(r.X + 3, r.Y + 3, IsLand))
	Heli = Make("heli", En, Find(r.X + 6, r.Y, IsLand))
	Jeep = Make("jeep", En, Find(r.X, r.Y + 6, IsLand))
	Heli.Stance = "HoldFire"
	Jeep.Stance = "HoldFire"
	Trigger.AfterDelay(DateTime.Seconds(15), function() Log("Ruche : helico PV=" .. Hp(Heli) .. ", jeep PV=" .. Hp(Jeep)) end)

	-- 6. Agnar : missile de croisière à 14 cases sur une centrale.
	local a = Find(100, 30, IsLand)
	Ag = Make("agnar", Me, a)
	Centrale = Make("powr", En, Find(a.X - 14, a.Y, IsLand))
	Make("camera", Me, Centrale.Location)
	Trigger.AfterDelay(DateTime.Seconds(1), function() Ag.Attack(Centrale) end)
	Trigger.AfterDelay(DateTime.Seconds(15), function() Log("Agnar : centrale PV=" .. Hp(Centrale)) end)

	-- 7. Lunkar : désigne un char, le Lunkra tire dessus.
	local l = Find(60, 100, IsLand)
	Lk = Make("lunkar", Me, l)
	Cible2 = Make("3tnk", En, Find(l.X + 5, l.Y, IsLand))
	Cible2.Stance = "HoldFire"
	Trigger.AfterDelay(DateTime.Seconds(1), function() Lk.Attack(Cible2) end)
	Trigger.AfterDelay(DateTime.Seconds(5), function() Log("Lunkar : char lourd PV=" .. Hp(Cible2) .. " (le laser ne fait pas de degats)") end)

	Trigger.AfterDelay(DateTime.Seconds(30), function() Log("fin") end)
end
