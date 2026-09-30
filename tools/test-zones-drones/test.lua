-- Scénario de test automatique : zones industrielles et Porte-Drone.
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

IsOpenWater = function(c)
	for dx = -3, 3 do
		for dy = -3, 3 do
			if not IsWater(CPos.New(c.X + dx, c.Y + dy)) then return false end
		end
	end
	return true
end

-- Terrain libre 3x2 + marge (pour la zone industrielle).
IsLand5 = function(c)
	for dx = -1, 3 do
		for dy = -1, 2 do
			if not IsLand(CPos.New(c.X + dx, c.Y + dy)) then return false end
		end
	end
	return #Map.ActorsInBox(WPos.New((c.X - 1) * 1024, (c.Y - 1) * 1024, 0), WPos.New((c.X + 4) * 1024, (c.Y + 3) * 1024, 0)) == 0
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

Drones = function()
	local n = 0
	for _, d in ipairs(Me.GetActorsByType("drone.essaim")) do
		if not d.IsDead then n = n + 1 end
	end
	return n
end

At = function(seconds, f) Trigger.AfterDelay(DateTime.Seconds(seconds), f) end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	En = Player.GetPlayer("Ennemi")

	-- ===== 1. Zones posées automatiquement (option forcée à 2) =====
	Trigger.AfterDelay(5, function()
		local txt, n = "", 0
		for _, z in ipairs(Player.GetPlayer("Neutral").GetActorsByType("zone.industrielle")) do
			if z ~= Z then
				n = n + 1
				txt = txt .. " (" .. z.Location.X .. "," .. z.Location.Y .. ")"
			end
		end
		Log("zones automatiques : " .. n .. txt .. " (attendu 2) ; base en 35,66")
		Z = Make("zone.industrielle", Player.GetPlayer("Neutral"), ZC)
		Log("zone de test en " .. ZC.X .. "," .. ZC.Y)
	end)

	-- ===== 2. Zone manuelle : contrôle par présence =====
	local zc = Find(80, 40, IsLand5)
	ZC = zc
	local inside = CPos.New(zc.X + 1, zc.Y + 3)

	-- Base pour mesurer le temps de production des véhicules.
	local b = Find(40, 40, IsLand5)
	Make("fact", Me, b)
	Make("weap", Me, Find(b.X + 5, b.Y, IsLand5))
	for i = 0, 2 do Make("apwr", Me, Find(b.X, b.Y + 5 + 4 * i, IsLand5)) end

	-- Production sans zone.
	At(1, function()
		T0 = DateTime.GameTime
		Me.Build({ "jeep" }, function() Log("jeep SANS zone : " .. (DateTime.GameTime - T0) .. " ticks") end)
	end)

	-- Un hélico dans la zone ne compte pas.
	At(1, function() Heli = Make("heli", Me, inside) end)
	At(8, function()
		Log("apres 7 s d'helico seul : proprietaire=" .. Z.Owner.InternalName .. " (attendu Neutral)")
		Heli.Destroy()
		Tank = Make("2tnk", Me, inside)
		Tank.Stance = "HoldFire"
	end)
	At(10, function()
		Log("tank depuis 2 s : proprietaire=" .. Z.Owner.InternalName .. " (attendu Neutral, 5 s necessaires)")
	end)
	At(14, function()
		Log("tank depuis 6 s : proprietaire=" .. Z.Owner.InternalName .. " prerequis=" .. tostring(Me.HasPrerequisites({ "zone.industrielle" })) .. " (attendu Multi0 true)")
		T1 = DateTime.GameTime
		Me.Build({ "jeep" }, function() Log("jeep AVEC zone : " .. (DateTime.GameTime - T1) .. " ticks (attendu ~87 %)") end)
	end)

	-- Ennemi présent : contestée.
	At(22, function()
		Foe = Make("3tnk", En, CPos.New(zc.X + 3, zc.Y + 3))
		Foe.Stance = "HoldFire"
	end)
	At(24, function()
		Log("contestee : proprietaire=" .. Z.Owner.InternalName .. " prerequis=" .. tostring(Me.HasPrerequisites({ "zone.industrielle" })) .. " (attendu Multi0 false)")
	end)
	At(30, function()
		Log("toujours contestee apres 8 s : proprietaire=" .. Z.Owner.InternalName .. " (attendu Multi0)")
		Tank.Destroy()
	end)
	At(33, function()
		Log("ennemi seul depuis 3 s : proprietaire=" .. Z.Owner.InternalName .. " (attendu Multi0)")
	end)
	At(37, function()
		Log("ennemi seul depuis 7 s : proprietaire=" .. Z.Owner.InternalName .. " Me prerequis=" .. tostring(Me.HasPrerequisites({ "zone.industrielle" })) .. " En prerequis=" .. tostring(En.HasPrerequisites({ "zone.industrielle" })) .. " (attendu Ennemi false true)")
		Foe.Destroy()
	end)
	At(42, function()
		Log("zone vide : proprietaire=" .. Z.Owner.InternalName .. " (attendu Ennemi)")
		T2 = DateTime.GameTime
		Me.Build({ "jeep" }, function() Log("jeep SANS zone (2e mesure) : " .. (DateTime.GameTime - T2) .. " ticks") end)
	end)

	-- ===== 3. Porte-Drone =====
	local w = Find(64, 64, IsOpenWater)
	Make("camera", Me, w)
	PD = Make("porte.drone", Me, w)
	At(2, function() Log("drones au depart : " .. Drones() .. " (attendu 6)") end)
	At(4, function()
		local ds = Me.GetActorsByType("drone.essaim")
		for i = 1, 5 do ds[i].Kill() end
		Log("5 drones abattus")
	end)
	At(5, function() Log("apres 5 morts : " .. Drones() .. " (attendu 1)") end)
	At(30, function()
		Log("26 s plus tard, 1 survivant : " .. Drones() .. " (attendu 1 : pas de nouveau lot)")
		Me.GetActorsByType("drone.essaim")[1].Kill()
		Log("6e drone abattu")
	end)
	At(49, function() Log("19 s apres le 6e : " .. Drones() .. " (attendu 0)") end)
	At(51, function() Log("21 s apres le 6e : " .. Drones() .. " (attendu 6)") end)

	-- Les drones suivent le navire.
	At(52, function()
		PD.Move(Find(w.X + 12, w.Y, IsOpenWater) or w)
	end)
	At(66, function()
		local far = 0
		for _, d in ipairs(Me.GetActorsByType("drone.essaim")) do
			local dx = d.CenterPosition.X - PD.CenterPosition.X
			local dy = d.CenterPosition.Y - PD.CenterPosition.Y
			if dx * dx + dy * dy > 4096 * 4096 then far = far + 1 end
		end
		Log("navire en " .. PD.Location.X .. "," .. PD.Location.Y .. " (parti de " .. w.X .. "," .. w.Y .. ")")
		Log("navire deplace de " .. (PD.Location.X - w.X) .. " cases ; drones a plus de 4 cases : " .. far .. " (attendu 0)")
		-- Un drone attaque un navire ennemi.
		Cible = Make("dd", En, Find(PD.Location.X + 6, PD.Location.Y, IsOpenWater))
		Cible.Stance = "HoldFire"
	end)
	At(67, function()
		if not Cible.IsDead then Me.GetActorsByType("drone.essaim")[1].Attack(Cible) end
	end)
	At(72, function()
		Log("destroyer ennemi pres du navire : detruit=" .. tostring(Cible.IsDead) .. ", drones restants " .. Drones())
		PD.Kill()
	end)
	At(73, function() Log("navire detruit : drones restants " .. Drones() .. " (attendu 0)") end)
	At(75, function() Log("FIN") end)
end
