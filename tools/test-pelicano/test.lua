-- Test automatique : largage du Pelícano le long d'une ligne (direction imposée ou automatique).
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

IsLand = function(c)
	local t = Map.TerrainType(c)
	return t == "Clear" or t == "Road" or t == "Rough"
end

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

Make = function(type, owner, cell, inWorld)
	return Actor.Create(type, inWorld ~= false, { Owner = owner, Location = cell })
end

Etendue = function(type, owner)
	local xs, ys, n = {}, {}, 0
	for _, a in ipairs(Map.ActorsInWorld) do
		if a.Type == type and a.Owner == owner and not a.IsDead and a.IsInWorld then
			n = n + 1
			xs[#xs + 1] = a.Location.X
			ys[#ys + 1] = a.Location.Y
		end
	end
	if n == 0 then return "aucun" end
	table.sort(xs) table.sort(ys)
	return n .. " au sol, x " .. xs[1] .. ".." .. xs[#xs] .. ", y " .. ys[1] .. ".." .. ys[#ys]
end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	local a = FindLand(60, 60)
	Aero = Make("aerodromo", Me, a)
	Peli = Make("pelicano", Me, FindLand(a.X, a.Y + 4))
	for i = 1, 15 do Peli.LoadPassenger(Make("aguila", Me, a, false)) end
	Peli2 = Make("pelicano", Me, FindLand(a.X + 4, a.Y + 4))
	for i = 1, 5 do Peli2.LoadPassenger(Make("carro.aguila", Me, a, false)) end
	Drop1 = FindLand(a.X + 14, a.Y - 10)
	Drop2 = FindLand(a.X - 12, a.Y - 8)
	Log("zones : " .. Drop1.X .. "," .. Drop1.Y .. " (axe est-ouest, facing 192) et " .. Drop2.X .. "," .. Drop2.Y .. " (auto)")
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Peli.ParaDropAt(Drop1, 192)
		Peli2.ParaDropAt(Drop2, -1)
	end)
	Trigger.AfterDelay(DateTime.Seconds(50), function()
		Log("Pelicano 1 : a bord = " .. Peli.PassengerCount .. ", peut relarguer = " .. tostring(Peli.CanParaDrop))
		Log("Pelicano 2 : a bord = " .. Peli2.PassengerCount)
		Log("fantassins : " .. Etendue("aguila", Me))
		Log("chars : " .. Etendue("carro.aguila", Me))
		Log("FIN")
	end)
end
