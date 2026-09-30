-- Scénario de test automatique : sortie des tunnels (Aguila Gate / Opération Taupe).
-- Les unités qui ressortent doivent s'écarter seules de la bouche, sans repartir aussitôt.
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

IsLand = function(c)
	if c.X < 2 or c.Y < 2 or c.X > 125 or c.Y > 125 then return false end
	local t = Map.TerrainType(c)
	return t == "Clear" or t == "Road" or t == "Rough"
end

IsOpenLand = function(c)
	for dx = -4, 4 do
		for dy = -4, 4 do
			if not IsLand(CPos.New(c.X + dx, c.Y + dy)) then return false end
		end
	end
	return true
end

Find = function(x, y, test)
	for d = 0, 60 do
		for dx = -d, d do
			for dy = -d, d do
				local c = CPos.New(x + dx, y + dy)
				if test(c) then return c end
			end
		end
	end
end

Dist = function(a, b) return math.sqrt((a.X - b.X) ^ 2 + (a.Y - b.Y) ^ 2) end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")

	A = Find(35, 66, IsOpenLand)
	B = Find(A.X + 12, A.Y, IsOpenLand)
	Entree = Actor.Create("aguila.gate", true, { Owner = Me, Location = A })
	Sortie = Actor.Create("aguila.gate", true, { Owner = Me, Location = B })
	Entree.LinkGate(Sortie, 0)
	Log("entrée " .. A.X .. "," .. A.Y .. " sortie " .. B.X .. "," .. B.Y)

	Units = { }
	for i = 1, 4 do
		local u = Actor.Create("2tnk", true, { Owner = Me, Location = CPos.New(A.X - 4, A.Y - 2 + i) })
		u.Move(A)
		Units[i] = u
	end

	Trigger.AfterDelay(400, function()
		local ok = 0
		for i, u in ipairs(Units) do
			local d = Dist(u.Location, B)
			Log("char " .. i .. " en " .. u.Location.X .. "," .. u.Location.Y .. " : à " .. string.format("%.1f", d) .. " cases de la sortie, " .. (u.IsIdle and "au repos" or "en mouvement"))
			if d >= 3 then ok = ok + 1 end
		end
		Log("sortis et écartés : " .. ok .. " / 4 (attendu 4)")
	end)

	-- Plus de 10 s plus tard, personne ne doit être revenu à l'entrée.
	Trigger.AfterDelay(700, function()
		local back = 0
		for _, u in ipairs(Units) do
			if Dist(u.Location, A) < Dist(u.Location, B) then back = back + 1 end
		end
		Log("revenus côté entrée tout seuls : " .. back .. " (attendu 0)")

		-- Un ordre explicite vers la sortie doit toujours permettre le retour.
		Units[1].Move(B)
	end)

	Trigger.AfterDelay(1000, function()
		local u = Units[1]
		Log("char 1 renvoyé : à " .. string.format("%.1f", Dist(u.Location, A)) .. " cases de l'entrée (attendu >= 3)")
		Log("FIN")
	end)
end
