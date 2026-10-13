-- Scénario de test automatique : débarquement étendu, débarquement concentré, base logistique amphibie.
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

IsType = function(c, t)
	if c.X < 17 or c.Y < 17 or c.X > 110 or c.Y > 110 then return false end
	return Map.TerrainType(c) == t
end
IsLand = function(c) return IsType(c, "Clear") or IsType(c, "Road") or IsType(c, "Rough") or IsType(c, "Beach") end
IsWater = function(c) return IsType(c, "Water") end

-- Côte nord-sud, mer à l'ouest : H cases de haut, 5 cases d'eau à l'ouest, D cases de terre à l'est.
IsCoast = function(c, h, d)
	for dy = 0, h - 1 do
		for dx = -5, -1 do
			if not IsWater(CPos.New(c.X + dx, c.Y + dy)) then return false end
		end
		for dx = 0, d - 1 do
			if not IsLand(CPos.New(c.X + dx, c.Y + dy)) then return false end
		end
	end
	return #Map.ActorsInBox(WPos.New((c.X - 6) * 1024, c.Y * 1024, 0), WPos.New((c.X + d) * 1024, (c.Y + h) * 1024, 0)) == 0
end

Find = function(test, ymin)
	for x = 18, 105 do
		for y = ymin or 18, 105 do
			local c = CPos.New(x, y)
			if test(c) then return c end
		end
	end
end

Make = function(type, owner, cell)
	return Actor.Create(type, true, { Owner = owner, Location = cell })
end

Loaded = function(type, owner, cell, cargo)
	local t = Make(type, owner, cell)
	local list = {}
	for _, u in ipairs(cargo) do
		local p = Actor.Create(u, false, { Owner = owner })
		t.LoadPassenger(p)
		list[#list + 1] = p
	end
	return t, list
end

Key = function(c) return c.X .. "," .. c.Y end

-- Bilan d'un débarquement : unités sorties, dans la zone, cases distinctes.
Report = function(name, units, x0, y0, x1, y1)
	local out, inside, cells, n = 0, 0, {}, 0
	for _, u in ipairs(units) do
		if not u.IsDead and u.IsInWorld then
			out = out + 1
			local c = u.Location
			if c.X >= x0 and c.X <= x1 and c.Y >= y0 and c.Y <= y1 then inside = inside + 1 end
			if not cells[Key(c)] then cells[Key(c)] = true n = n + 1 end
		end
	end
	Log(name .. " : " .. out .. "/" .. #units .. " débarqués, " .. inside .. " dans la zone, " .. n .. " cases distinctes")
	return out, inside, n
end

Me = nil

-- Coordonnées relevées sur la carte (côtes de l'île à l'est et à l'ouest).
WorldLoaded = function()
	local me = Player.GetPlayer("Multi0")
	Me = me
	Camera.Position = WPos.New(95 * 1024, 82 * 1024, 0)
	for t = 5, 1300, 5 do
		Trigger.AfterDelay(t, function() Camera.Position = WPos.New(95 * 1024, 88 * 1024, 0) end)
	end

	-- 1) Débarquement étendu : côte nord-sud, mer à l'est. 3 LST chargés en file.
	local units = {}
	local lsts = {}
	for i = 0, 2 do
		local t, list = Loaded("lst", me, CPos.New(104, 80 + 3 * i), { "e1", "e1", "e1", "2tnk", "e3" })
		lsts[#lsts + 1] = t
		for _, u in ipairs(list) do units[#units + 1] = u end
	end
	local x0, y0, x1, y1 = 90, 77, 99, 88
	-- Une centrale dans la zone : ses cases doivent être évitées.
	Make("powr", me, CPos.New(92, 79))
	Trigger.AfterDelay(10, function()
		for _, t in ipairs(lsts) do t.LandInZone(CPos.New(x0, y0), CPos.New(x1, y1)) end
		Log("ordre de débarquement étendu donné à " .. #lsts .. " transports")
	end)
	Trigger.AfterDelay(100, function()
		local s = ""
		for _, t in ipairs(lsts) do s = s .. Key(t.Location) .. " " end
		Log("transports en route : " .. s)
	end)
	Trigger.AfterDelay(800, function()
		local s = ""
		local seen, distinct = {}, 0
		for _, t in ipairs(lsts) do
			s = s .. Key(t.Location) .. "(" .. t.PassengerCount .. ") "
			if not seen[Key(t.Location)] then seen[Key(t.Location)] = true distinct = distinct + 1 end
		end
		Log("transports à la fin : " .. s .. "- points distincts " .. distinct)
		local _, inside, n = Report("étendu", units, x0, y0, x1, y1)
		local onBlock, ys = 0, {}
		for _, u in ipairs(units) do
			if not u.IsDead and u.IsInWorld then
				local c = u.Location
				if c.X >= 92 and c.X <= 93 and c.Y >= 79 and c.Y <= 80 then onBlock = onBlock + 1 end
				ys[#ys + 1] = Key(c)
			end
		end
		Log("positions : " .. table.concat(ys, " "))
		Log("unités sous la centrale : " .. onBlock)
		if inside >= 13 and n >= 8 and distinct >= 3 and onBlock == 0 then Log("OK étendu") else Log("ECHEC étendu") end
	end)

	-- 2) Débarquement concentré (un clic) sur la côte ouest.
	local t2, list2 = Loaded("barcaza", me, CPos.New(21, 60), { "e1", "e1", "e1", "e1", "e1", "e1" })
	Trigger.AfterDelay(10, function() t2.LandInZone(CPos.New(28, 60), CPos.New(28, 60)) end)
	Trigger.AfterDelay(700, function()
		local _, inside = Report("concentré", list2, 26, 58, 30, 62)
		if inside >= 5 then Log("OK concentré") else Log("ECHEC concentré") end
	end)

	-- 3) Zone sans case accessible (en pleine mer) : ordre refusé, le transport ne bouge pas.
	local t3 = Loaded("lst", me, CPos.New(108, 72), { "e1" })
	Trigger.AfterDelay(12, function() t3.LandInZone(CPos.New(108, 74), CPos.New(110, 76)) end)
	Trigger.AfterDelay(300, function()
		Log("zone en mer : passagers " .. t3.PassengerCount .. ", position " .. Key(t3.Location) .. " (attendu 1, 108,72)")
	end)

	-- 4) Base logistique amphibie (quai à l'est) : stock, 10 places à quai sur 12 transports, chargement, opération.
	local base = Actor.Create("base.amphibie.e", true, { Owner = me, Location = CPos.New(93, 91) })
	local fleet = {}
	for y = 86, 100, 2 do
		for x = 97, 104, 2 do
			local c = CPos.New(x, y)
			if #fleet < 12 and IsWater(c) and IsWater(CPos.New(x + 1, y)) and IsWater(CPos.New(x, y + 1)) then
				fleet[#fleet + 1] = Make("lst", me, c)
			end
		end
	end
	Log("flotte créée : " .. #fleet .. " LST")
	local troops = {}
	for i = 0, 7 do troops[#troops + 1] = Make("e1", me, CPos.New(86 + (i % 3), 92 + math.floor(i / 3))) end
	troops[#troops + 1] = Make("2tnk", me, CPos.New(85, 95))
	troops[#troops + 1] = Make("2tnk", me, CPos.New(87, 96))
	Trigger.AfterDelay(15, function()
		for _, u in ipairs(troops) do u.EnterStation(base) end
	end)
	Trigger.AfterDelay(150, function()
		Log("base : stock " .. base.StockedUnits .. ", à quai " .. base.DockedCount)
	end)
	Trigger.AfterDelay(500, function()
		local aboard, loaded = 0, 0
		for _, t in ipairs(fleet) do
			aboard = aboard + t.PassengerCount
			if t.PassengerCount > 0 then loaded = loaded + 1 end
		end
		Log("base : stock " .. base.StockedUnits .. ", à quai " .. base.DockedCount .. ", à bord " .. aboard .. " dans " .. loaded .. " transports")
		if base.StockedUnits == 0 and base.DockedCount == 10 and aboard == #troops then Log("OK chargement") else Log("ECHEC chargement") end
		base.LaunchOperation(CPos.New(95, 96), CPos.New(102, 104))
	end)
	Trigger.AfterDelay(1300, function()
		local _, inside = Report("opération de la base", troops, 95, 96, 102, 104)
		Log("base après l'opération : à quai " .. base.DockedCount)
		if inside >= #troops - 1 then Log("OK base") else Log("ECHEC base") end
	end)

	-- 5) Navette base → plage : base B (îlot sud-ouest), 10 fantassins en stock, 2 LST en navette vers la côte ouest.
	local baseB = Actor.Create("base.amphibie.e", true, { Owner = me, Location = CPos.New(31, 100) })
	local cargoB = {}
	for i = 0, 9 do cargoB[#cargoB + 1] = Make("e1", me, CPos.New(27 + (i % 4), 101 + math.floor(i / 4))) end
	local shuttles = { Make("lst", me, CPos.New(36, 99)), Make("lst", me, CPos.New(37, 102)) }
	Trigger.AfterDelay(15, function()
		for _, u in ipairs(cargoB) do u.EnterStation(baseB) end
	end)
	Trigger.AfterDelay(60, function()
		Log("base B : stock " .. baseB.StockedUnits)
		for _, t in ipairs(shuttles) do t.ShuttleToBeach(baseB, CPos.New(26, 64), CPos.New(31, 69)) end
	end)
	Trigger.AfterDelay(400, function()
		local s = ""
		for _, t in ipairs(shuttles) do s = s .. Key(t.Location) .. "(" .. t.PassengerCount .. (t.AmphibiousShuttleActive and ",navette) " or ") ") end
		Log("navettes B : " .. s .. "- stock " .. baseB.StockedUnits)
	end)
	Trigger.AfterDelay(1600, function()
		local _, inside = Report("navette base → plage", cargoB, 26, 64, 31, 69)
		local active = 0
		for _, t in ipairs(shuttles) do if t.AmphibiousShuttleActive then active = active + 1 end end
		Log("base B : stock " .. baseB.StockedUnits .. ", navettes actives " .. active)
		if inside >= 9 and baseB.StockedUnits == 0 and active == 2 then Log("OK navette base-plage") else Log("ECHEC navette base-plage") end
	end)

	-- 6) Navette plage → base : 6 fantassins sur l'îlot nord-est, un LST les amène dans le stock de la base C.
	local baseC = Actor.Create("base.amphibie.e", true, { Owner = me, Location = CPos.New(91, 23) })
	local beachUnits = {}
	for _, c in ipairs({ { 99, 21 }, { 100, 21 }, { 99, 22 }, { 100, 22 }, { 101, 22 }, { 100, 23 } }) do
		beachUnits[#beachUnits + 1] = Make("e1", me, CPos.New(c[1], c[2]))
	end
	local s3 = Make("lst", me, CPos.New(95, 21))
	Trigger.AfterDelay(20, function() s3.ShuttleFromBeach(CPos.New(97, 20), CPos.New(106, 24), baseC) end)
	for _, d in ipairs({ 300, 700, 1200 }) do
		Trigger.AfterDelay(d, function()
			local u = ""
			for _, b in ipairs(beachUnits) do u = u .. (b.IsInWorld and (Key(b.Location) .. (b.IsIdle and "i " or "m ")) or "abord ") end
			Log("navette C : " .. Key(s3.Location) .. (s3.IsIdle and " i" or " m") .. (s3.AmphibiousShuttleActive and " navette" or "") .. " passagers " .. s3.PassengerCount .. ", stock C " .. baseC.StockedUnits .. " | " .. u)
		end)
	end
	Trigger.AfterDelay(2200, function()
		Log("navette plage → base : stock C " .. baseC.StockedUnits .. ", passagers " .. s3.PassengerCount)
		if baseC.StockedUnits == 6 then Log("OK navette plage-base") else Log("ECHEC navette plage-base") end
		Log("FIN")
	end)
end
