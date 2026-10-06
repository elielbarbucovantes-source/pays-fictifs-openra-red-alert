-- Attelages : rames de wagons, locomotive qui tire plusieurs wagons, limite de 4, scission.
Log = function(msg) print("FERRO " .. DateTime.GameTime .. " : " .. msg) end
Check = function(label, ok) Log((ok and "OK    " or "ECHEC ") .. label) end

WorldLoaded = function()
	local me = Player.GetPlayer("Multi0")
	X0, Y0 = 58, 80
	local m = function(t, x, y) return Actor.Create(t, true, { Owner = me, Location = CPos.New(X0 + x, Y0 + y) }) end
	Utils.Do(Map.ActorsInBox(Map.CenterOfCell(CPos.New(X0 - 2, Y0 - 3)), Map.CenterOfCell(CPos.New(X0 + 36, Y0 + 4))), function(a) if a.Owner ~= me and a.Type ~= "mpspawn" then a.Destroy() end end)
	for x = 0, 34 do m("rail", x, 0) end
	Loco = m("locomotive.diesel", 2, 0)
	W1 = m("wagon.leger", 10, 0)
	W2 = m("wagon.lourd", 11, 0)
	W3 = m("wagon.leger", 20, 0)
	W4 = m("wagon.lourd", 21, 0)
	W5 = m("wagon.leger", 30, 0)

	Trigger.AfterDelay(10, function()
		Check("wagon + wagon bout à bout : rame de 2", W1.CoupleTo(W2) and W1.TrainLength == 2)
		Check("wagon + wagon éloignés : refusé", not W2.CoupleTo(W3))
		Check("wagon + wagon : 2e rame", W3.CoupleTo(W4) and W4.TrainLength == 2)
		-- wagon d'une rame + clic sur la locomotive : la locomotive vient chercher toute la rame
		Check("ordre wagon → locomotive accepté", W2.CoupleTo(Loco))
	end)
	Trigger.AfterDelay(300, function()
		Check("locomotive + rame de 2 = 3 voitures (" .. Loco.TrainLength .. ")", Loco.TrainLength == 3)
		Check("locomotive → 2e rame accepté", Loco.CoupleTo(W3))
	end)
	Trigger.AfterDelay(700, function()
		Check("locomotive + 4 wagons (" .. Loco.TrainLength .. ")", Loco.TrainLength == 5)
		Check("5e wagon refusé (limite de 4)", not Loco.CoupleTo(W5))
		local cars = Loco.TrainCars
		local s = ""
		for _, c in ipairs(cars) do s = s .. " " .. c.Type .. "@" .. (c.Location.X - X0) end
		Log("train :" .. s)
		-- destruction d'un wagon du milieu : la partie sans locomotive reste une rame
		local li
		for i, c in ipairs(cars) do if c == Loco then li = i end end
		local victime = li <= 2 and cars[li + 2] or cars[li - 2]
		victime.Kill()
		Trigger.AfterDelay(5, function()
			Check("après destruction : locomotive + 1 wagon (" .. Loco.TrainLength .. ")", Loco.TrainLength == 2)
			local reste = li <= 2 and cars[li + 3] or cars[li - 3]
			Check("les wagons au-delà restent une rame (" .. (reste and reste.TrainLength or -1) .. ")", reste ~= nil and reste.TrainLength == 1 + ((li <= 2 and #cars - li - 3) or (li - 4)))
			Loco.RailMove(CPos.New(X0 + 1, Y0))
		end)
	end)
	Trigger.AfterDelay(1000, function()
		Check("la locomotive repart avec son wagon (" .. Loco.TrainLength .. ")", Loco.TrainLength == 2)
		Log("FIN")
	end)
end
