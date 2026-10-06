-- Mode Multi-file : le chantier ferroviaire a sa propre file (trains seulement), l'usine ne fait pas de trains.
Log = function(msg) print("FERRO " .. DateTime.GameTime .. " : " .. msg) end
Check = function(label, ok) Log((ok and "OK    " or "ECHEC ") .. label) end
Make = function(type, owner, x, y) return Actor.Create(type, true, { Owner = owner, Location = CPos.New(x, y) }) end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	Me.Cash = 20000
	X0, Y0 = 58, 80
	for i = 0, 3 do Make("apwr", Me, X0 + 4 * i, Y0 - 8) end
	Chantier = Make("chantier.ferroviaire", Me, X0, Y0)
	Usine = Make("weap", Me, X0 + 10, Y0 - 3)
	for x = 4, 8 do Make("rail", Me, X0 + x, Y0 + 2) end

	Trigger.AfterDelay(10, function()
		Check("le chantier lance une locomotive", Chantier.Build({ "locomotive.diesel" }))
		Usine.Build({ "locomotive.diesel" })
	end)
	Trigger.AfterDelay(900, function()
		local locos = Me.GetActorsByType("locomotive.diesel")
		Check("une seule locomotive produite, par le chantier (" .. #locos .. ")", #locos == 1)
		if #locos == 1 then Check("sur la voie du chantier", locos[1].Location.Y == Y0 + 2) end
		Chantier.Build({ "reco.rub" })
		Usine.Build({ "reco.rub" })
	end)
	Trigger.AfterDelay(1800, function()
		local chars = Me.GetActorsByType("reco.rub")
		Check("un seul véhicule (reco.rub), sorti de l'usine (" .. #chars .. ")", #chars == 1)
		if #chars == 1 then Log("char en " .. chars[1].Location.X - X0 .. "," .. chars[1].Location.Y - Y0) end
		Log("FIN")
	end)
end
