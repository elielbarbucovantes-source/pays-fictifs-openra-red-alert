-- Locomotive nucléaire (3 cases) seule : doit rouler dans les deux sens.
Log = function(msg) print("FERRO " .. DateTime.GameTime .. " : " .. msg) end
Check = function(label, ok) Log((ok and "OK    " or "ECHEC ") .. label) end

WorldLoaded = function()
	local me = Player.GetPlayer("Multi0")
	X0, Y0 = 58, 82
	local m = function(t, x, y) return Actor.Create(t, true, { Owner = me, Location = CPos.New(X0 + x, Y0 + y), Facing = Angle.East }) end
	Utils.Do(Map.ActorsInBox(Map.CenterOfCell(CPos.New(X0 - 2, Y0 - 3)), Map.CenterOfCell(CPos.New(X0 + 36, Y0 + 10))), function(a) if a.Type ~= "mpspawn" then a.Destroy() end end)
	for x = 0, 30 do m("rail", x, 0) end
	Loco = m("locomotive.nucleaire", 15, 0)
	Trigger.AfterDelay(5, function()
		Log("départ x=" .. (Loco.Location.X - X0))
		Loco.RailMove(CPos.New(X0 + 1, Y0))
	end)
	Trigger.AfterDelay(300, function()
		local x = Loco.Location.X - X0
		Check("recule jusqu'à x=1 (x=" .. x .. ")", x <= 3)
		Loco.RailMove(CPos.New(X0 + 29, Y0))
	end)
	Trigger.AfterDelay(600, function()
		local x = Loco.Location.X - X0
		Check("repart vers x=29 (x=" .. x .. ")", x >= 27)
		Log("FIN")
	end)
end
