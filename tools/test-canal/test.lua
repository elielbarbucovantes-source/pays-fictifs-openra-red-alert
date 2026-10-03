-- Scénario de test automatique : Tunnelier avancé (canaux).
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

IsType = function(c, t)
	if c.X < 1 or c.Y < 1 or c.X > 126 or c.Y > 126 then return false end
	return Map.TerrainType(c) == t
end
IsLand = function(c) return IsType(c, "Clear") or IsType(c, "Road") or IsType(c, "Beach") end
IsWater = function(c) return IsType(c, "Water") end

-- Côte : de l'eau à l'ouest, de la terre dégagée sur 9 cases à l'est, rangées -1 à +4.
IsCoast = function(c)
	if not IsWater(c) or not IsWater(CPos.New(c.X - 1, c.Y)) then return false end
	for dy = 0, 3 do
		for dx = 1, 8 do
			if not IsLand(CPos.New(c.X + dx, c.Y + dy)) then return false end
		end
	end
	return #Map.ActorsInBox(WPos.New(c.X * 1024, (c.Y - 2) * 1024, 0), WPos.New((c.X + 11) * 1024, (c.Y + 6) * 1024, 0)) == 0
end

Find = function(test)
	for x = 18, 108 do
		for y = 18, 106 do
			local c = CPos.New(x, y)
			if test(c) then return c end
		end
	end
end

Make = function(type, owner, cell)
	return Actor.Create(type, true, { Owner = owner, Location = cell })
end

Row = function(y, x0, x1)
	local s = ""
	for x = x0, x1 do
		local t = Map.TerrainType(CPos.New(x, y))
		s = s .. (t == "Water" and "~" or t == "Rough" and "#" or t == "Clear" and "." or t == "Beach" and "b" or t == "Tree" and "T" or "?")
	end
	return s
end

State = function(a) if a.IsDead then return "mort" end return "vivant en " .. a.Location.X .. "," .. a.Location.Y end

WorldLoaded = function()
	local me = Player.GetPlayer("Multi0")
	local foe = Player.GetPlayer("Ennemi")
	local coast = Find(IsCoast)
	if not coast then Log("ECHEC pas de côte trouvée") Log("FIN") return end
	local X, Y = coast.X, coast.Y
	local at = function(dx, dy) return CPos.New(X + dx, Y + dy) end
	Log("côte " .. X .. "," .. Y)

	-- Ligne 1 : de l'intérieur (6) vers la mer (1). Un fantassin et une jeep ennemis dans le tracé.
	local t1 = Make("tunnelier.avance", me, at(8, -1))
	local e1 = Make("e1", foe, at(4, 0))
	local jeep = Make("jeep", foe, at(3, 0))
	e1.Stance = "HoldFire"
	jeep.Stance = "HoldFire"
	-- Ligne 2 : un arbre au milieu (case 3), ne touche pas la mer (commence en 2).
	local t2 = Make("tunnelier.avance", me, at(6, 4))
	Tree = Make("t01", Player.GetPlayer("Neutral"), at(4, 2)) -- tronc en bas à gauche : case (4, 3)
	Log("arbre en " .. Tree.Location.X .. "," .. Tree.Location.Y .. " ; acteurs sur la case : " .. #Map.ActorsInBox(WPos.New((X + 4) * 1024, (Y + 3) * 1024, 0), WPos.New((X + 5) * 1024, (Y + 4) * 1024, 0)))

	Make("camera", me, at(4, 1))
	local view = WPos.New((X + 4) * 1024, (Y + 1) * 1024, 0)
	for t = 5, 1500, 3 do
		Trigger.AfterDelay(t, function() Camera.Position = view end)
	end

	Trigger.AfterDelay(10, function()
		t1.DigCanal(at(6, 0), at(1, 0))
		t2.DigCanal(at(2, 3), at(6, 3))
	end)
	for t = 60, 900, 120 do
		Trigger.AfterDelay(t, function()
			Log("rangée 0 : " .. Row(Y, X - 1, X + 8) .. " | rangée 3 : " .. Row(Y + 3, X - 1, X + 8)
				.. " | t1 " .. State(t1) .. " e1 " .. State(e1) .. " jeep " .. State(jeep))
		end)
	end
	Trigger.AfterDelay(910, function()
		Ship = Make("dd", me, at(-2, 0))
		Ship.Move(at(5, 0))
	end)
	Trigger.AfterDelay(1400, function()
		Log("destroyer " .. State(Ship) .. " (cible " .. (X + 5) .. "," .. Y .. ")")
		Log("t1 " .. State(t1) .. " ; t2 " .. State(t2) .. " ; arbre " .. State(Tree))
		Log("FIN")
	end)
end
