-- Scénario de test automatique : réparation des unités en soute d'un porte-avions.
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

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

IsLand = function(c)
	if c.X < 1 or c.Y < 1 or c.X > 126 or c.Y > 126 then return false end
	local t = Map.TerrainType(c)
	return t == "Clear" or t == "Road" or t == "Rough"
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

Pct = function(a) return math.floor(100 * a.Health / a.MaxHealth) end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")

	local sea = Find(64, 64, IsOpenWater)
	PA = Actor.Create("porta.aguila", true, { Owner = Me, Location = sea })

	Fantassin = Actor.Create("e1", false, { Owner = Me })
	Fantassin.Health = math.floor(Fantassin.MaxHealth / 4)
	PA.LoadPassenger(Fantassin)

	Char = Actor.Create("jaguar", false, { Owner = Me })
	Char.Health = math.floor(Char.MaxHealth / 10)
	PA.LoadPassenger(Char)

	-- Témoin : blessé, à terre, il ne doit pas guérir.
	Temoin = Actor.Create("e1", true, { Owner = Me, Location = Find(sea.X, sea.Y, IsLand) })
	Temoin.Health = math.floor(Temoin.MaxHealth / 4)

	Log("à bord : " .. #PA.Passengers .. " unités (attendu 2)")
	for _, t in ipairs({ 0, 125, 260, 300 }) do
		Trigger.AfterDelay(t + 1, function()
			Log("fantassin " .. Pct(Fantassin) .. " %, char " .. Pct(Char) .. " %, témoin à terre " .. Pct(Temoin) .. " %")
		end)
	end

	Trigger.AfterDelay(310, function() Log("FIN") end)
end
