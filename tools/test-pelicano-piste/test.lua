Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end
IsLand = function(c) local t = Map.TerrainType(c) return t == "Clear" or t == "Road" or t == "Rough" end
FindLand = function(x, y)
	for d = 0, 30 do for dx = -d, d do for dy = -d, d do
		local c = CPos.New(x + dx, y + dy)
		if IsLand(c) then return c end
	end end end
end
Open = function(x, y, r)
	for ix = -r, r do for iy = -r, r do
		if not IsLand(CPos.New(x + ix, y + iy)) then return false end
	end end
	return true
end
FindOpen = function(x, y, r)
	for d = 0, 40, 2 do for dx = -d, d, 2 do for dy = -d, d, 2 do
		if Open(x + dx, y + dy, r) then return CPos.New(x + dx, y + dy) end
	end end end
end
Make = function(type, owner, cell) return Actor.Create(type, true, { Owner = owner, Location = cell }) end

Landed = function(p) return p.CenterPosition.Z < 64 end
Desc = function(i, p)
	local where = "au sol"
	if p.Location.X >= Aero.Location.X and p.Location.X <= Aero.Location.X + 2 and p.Location.Y >= Aero.Location.Y and p.Location.Y <= Aero.Location.Y + 1 then where = "SUR LA PISTE" end
	if not Landed(p) then where = "EN VOL" end
	return "Pelicano " .. i .. " : " .. where .. " (" .. p.Location.X .. "," .. p.Location.Y .. "), a bord " .. p.PassengerCount .. ", decollages apres 40 s : " .. TakeOffs[i]
end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	local a = FindOpen(60, 60, 5)
	Log("aerodrome place en " .. a.X .. "," .. a.Y)
	Aero = Make("aerodromo", Me, a)
	P = {} TakeOffs = {} Was = {} Inf = {}
	for i = 1, 3 do
		P[i] = Make("pelicano", Me, FindLand(a.X + 8 * i, a.Y + 10))
		TakeOffs[i] = 0
	end
	local tick
	tick = function()
		for i = 1, 3 do
			local l = Landed(P[i])
			if DateTime.GameTime > DateTime.Seconds(40) and Was[i] == true and not l then TakeOffs[i] = TakeOffs[i] + 1 end
			Was[i] = l
		end
		Trigger.AfterDelay(5, tick)
	end
	tick()
	Trigger.AfterDelay(DateTime.Seconds(40), function()
		for i = 1, 3 do Log("40 s : " .. Desc(i, P[i])) end
		for i = 1, 3 do
			if Landed(P[i]) then
				for k = 1, 4 do
					local e = Make("e1", Me, FindLand(a.X - 3, a.Y + 3 + k))
					e.EnterTransport(P[i])
					Inf[#Inf + 1] = e
				end
			end
		end
	end)
	Trigger.AfterDelay(DateTime.Seconds(90), function()
		for i = 1, 3 do Log("90 s : " .. Desc(i, P[i])) end
		for _, e in ipairs(Inf) do if e.IsInWorld then Log("e1 dehors en " .. e.Location.X .. "," .. e.Location.Y .. " inactif=" .. tostring(e.IsIdle)) end end
		Log("aerodrome en " .. Aero.Location.X .. "," .. Aero.Location.Y)
		Log("FIN")
	end)
end
