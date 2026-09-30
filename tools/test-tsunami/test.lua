-- Scénario de test automatique : Engineered Tsunami.
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

IsType = function(c, t)
	if c.X < 1 or c.Y < 1 or c.X > 126 or c.Y > 126 then return false end
	return Map.TerrainType(c) == t
end
IsLand = function(c) return IsType(c, "Clear") or IsType(c, "Road") or IsType(c, "Rough") or IsType(c, "Beach") end
IsWater = function(c) return IsType(c, "Water") end

-- Côte ouest->est (vague vers l'est = direction 192) : trouvée avec le test du pouvoir
-- lui-même, plus de la terre dégagée devant et 3 cases d'eau derrière la case visée.
East = 192
IsCoast = function(c)
	if not Me.TsunamiTargetValid(c, East) then return false end
	for dx = -3, -1 do
		if not IsWater(CPos.New(c.X + dx, c.Y)) then return false end
	end
	for dy = -4, 4 do
		for dx = 1, 7 do
			if not IsLand(CPos.New(c.X + dx, c.Y + dy)) then return false end
		end
	end
	return #Map.ActorsInBox(WPos.New(c.X * 1024, (c.Y - 7) * 1024, 0), WPos.New((c.X + 13) * 1024, (c.Y + 8) * 1024, 0)) == 0
end

Find = function(test)
	for x = 6, 110 do
		for y = 8, 118 do
			local c = CPos.New(x, y)
			if test(c) then return c end
		end
	end
end

Make = function(type, owner, cell)
	return Actor.Create(type, true, { Owner = owner, Location = cell })
end

Pct = function(a)
	if a.IsDead then return "mort" end
	return tostring(math.floor(100 * a.Health / a.MaxHealth)) .. "%"
end

WorldLoaded = function()
	local me = Player.GetPlayer("Multi0")
	Me = me
	local foe = Player.GetPlayer("Ennemi")
	local yard0 = Make("chantier.avance", me, CPos.New(20, 20))
	local coast = Find(IsCoast)
	if not coast then Log("ECHEC pas de côte trouvée") Log("FIN") return end
	Log("côte " .. coast.X .. "," .. coast.Y)

	-- Base du lanceur loin de là (le chantier porte le pouvoir).

	-- Validité : quelles directions acceptées sur cette côte ; une case en pleine terre refusée.
	local ok = {}
	for f = 0, 224, 32 do
		if me.TsunamiTargetValid(coast, f) then ok[#ok + 1] = tostring(f) end
	end
	Log("directions valides : " .. table.concat(ok, " "))
	Log("en pleine terre valide ? " .. tostring(me.TsunamiTargetValid(CPos.New(coast.X + 10, coast.Y), -1)))

	local at = function(dx, dy)
		local c = CPos.New(coast.X + dx, coast.Y + dy)
		if dx >= 0 and not IsLand(c) then Log("attention : " .. dx .. "," .. dy .. " n'est pas de la terre") end
		return c
	end
	Units = {
		["fantassin rivage"] = Make("e1", foe, at(1, -2)),
		["fantassin fond (7)"] = Make("e1", foe, at(7, -2)),
		["fantassin allié rivage"] = Make("e1", me, at(1, 2)),
		["jeep rivage"] = Make("jeep", foe, at(1, 0)),
		["char lourd rivage"] = Make("3tnk", foe, at(1, 3)),
		["centrale (bois)"] = Make("powr", foe, at(3, -4)),
		["mur"] = Make("brik", foe, at(2, 1)),
		["arbre"] = Make("t01", Player.GetPlayer("Neutral"), at(4, 1)),
		["destroyer au large"] = Make("dd", foe, at(-3, 0)),
		["fantassin hors bande (fond 11)"] = Make("e1", foe, at(11, 0)),
		["fantassin hors bande (côté)"] = Make("e1", foe, at(4, 7)),
	}
	Heli = Actor.Create("heli", true, { Owner = foe, CenterPosition = WPos.New((coast.X + 3) * 1024 + 512, coast.Y * 1024 + 512, 2048) })
	Units["hélico en vol"] = Heli
	-- Personne ne tire : seuls les dégâts de la vague comptent.
	for k, a in pairs(Units) do
		if a.HasProperty("Stance") then a.Stance = "HoldFire" end
	end
	Make("camera", me, at(0, 0))
	Make("camera", me, at(6, 0))
	local view = WPos.New((coast.X + 2) * 1024, coast.Y * 1024, 0)
	for t = 5, 460, 3 do
		Trigger.AfterDelay(t, function() Camera.Position = view end)
	end
	Trigger.AfterDelay(200, function() Log("caméra " .. Camera.Position.X .. "," .. Camera.Position.Y) end)

	Trigger.AfterDelay(25, function()
		Log("lancement : " .. tostring(me.ActivatePowerAt("AustralouisTsunami", coast, -1)))
		Log("état : " .. me.SupportPowerState("AustralouisTsunami"))
	end)
	Trigger.AfterDelay(25 + 200, function()
		local s = ""
		for k, a in pairs(Units) do s = s .. k .. "=" .. Pct(a) .. " " end
		Log("pendant l'alerte (8 s) : " .. s)
	end)
	Trigger.AfterDelay(25 + 450, function()
		for k, a in pairs(Units) do Log("après : " .. k .. " = " .. Pct(a)) end
		if not Heli.IsDead then Log("altitude hélico " .. Heli.CenterPosition.Z) end
		Log("relance immédiate : " .. tostring(me.ActivatePowerAt("AustralouisTsunami", coast, -1)))
		Log("FIN")
	end)
end
