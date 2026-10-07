-- Voitures de plusieurs cases : locomotive 2 cases, wagon lourd 3 cases, wagon léger 1 case.
Log = function(msg) print("FERRO " .. DateTime.GameTime .. " : " .. msg) end
Check = function(label, ok) Log((ok and "OK    " or "ECHEC ") .. label) end
P = function(a) return (a.Location.X - X0) .. "," .. (a.Location.Y - Y0) end

WorldLoaded = function()
	local me = Player.GetPlayer("Multi0")
	local en = Player.GetPlayer("Ennemi")
	X0, Y0 = 58, 82
	local m = function(t, p, x, y) return Actor.Create(t, true, { Owner = p, Location = CPos.New(X0 + x, Y0 + y), Facing = Angle.East }) end
	Utils.Do(Map.ActorsInBox(Map.CenterOfCell(CPos.New(X0 - 2, Y0 - 3)), Map.CenterOfCell(CPos.New(X0 + 36, Y0 + 10))), function(a) if a.Type ~= "mpspawn" then a.Destroy() end end)
	-- voie : y=0 de x=0 à 30, puis virage vers le bas en x=30 jusqu'à y=8, puis vers l'ouest jusqu'à x=10
	for x = 0, 30 do m("rail", me, x, 0) end
	for y = 1, 8 do m("rail", me, 30, y) end
	for x = 10, 29 do m("rail", me, x, 8) end
	Loco = m("locomotive.diesel", me, 3, 0)
	WH = m("wagon.lourd", me, 10, 0)
	WL = m("wagon.leger", me, 11, 0)
	Trigger.AfterDelay(5, function()
		local s = ""
		Log("loco " .. P(Loco) .. " ; lourd " .. P(WH) .. " ; léger " .. P(WL))
		Check("wagon lourd + léger bout à bout : rame", WH.CoupleTo(WL) and WH.TrainLength == 2)
		Check("locomotive vient atteler la rame", Loco.CoupleTo(WH))
	end)
	Trigger.AfterDelay(250, function()
		Check("train de 3 voitures (" .. Loco.TrainLength .. ")", Loco.TrainLength == 3)
		Victime = m("e1", en, 30, 4)
		Victime.Stop()
		T0 = DateTime.GameTime
		Loco.RailMove(CPos.New(X0 + 12, Y0 + 8))
	end)
	-- jamais deux voitures du train au même endroit, ni la voiture à plus d'une case de la voie
	local maxd = 0
	local watch
	watch = function()
		if Loco and not Loco.IsDead and Loco.TrainLength == 3 then
			local cs = Loco.TrainCars
			for i = 1, #cs - 1 do
				local a, b = cs[i].CenterPosition, cs[i + 1].CenterPosition
				local d = math.sqrt((a.X - b.X) ^ 2 + (a.Y - b.Y) ^ 2)
				if d < 700 then Overlap = true end
				if d > maxd then maxd = d end
			end
		end
		if DateTime.GameTime < 1500 then Trigger.AfterDelay(1, watch) end
	end
	watch()
	Trigger.AfterDelay(900, function()
		Log("tête " .. P(Loco.TrainCars[1]) .. " queue " .. P(Loco.TrainCars[3]) .. " (" .. (DateTime.GameTime - T0) .. " ticks)")
		Check("train arrivé après le virage (une extrémité en 12,8)", Loco.TrainCars[1].Location.Y - Y0 == 8 and Loco.TrainCars[3].Location.Y - Y0 == 8)
		Check("voitures jamais superposées (écart max " .. math.floor(maxd) .. ")", not Overlap)
		Check("fantassin écrasé dans le virage", Victime.IsDead)
		-- un deuxième train croise le long train sur sa voie (voie double)
		LocoB = m("locomotive.diesel", me, 20, 0)
		LocoB.RailMove(CPos.New(X0 + 15, Y0 + 8))
	end)
	Trigger.AfterDelay(1300, function()
		Log("locomotive B en " .. P(LocoB))
		Check("locomotive B arrivée en 15,8 en croisant le long train", LocoB.Location.Y - Y0 == 8 and LocoB.Location.X - X0 == 15)
		WH.Kill()
	end)
	Trigger.AfterDelay(1310, function()
		Check("après destruction du wagon lourd : locomotive seule ou avec le léger (" .. Loco.TrainLength .. ", léger " .. WL.TrainLength .. ")", Loco.TrainLength + WL.TrainLength <= 3)
		Log("FIN")
	end)
end
