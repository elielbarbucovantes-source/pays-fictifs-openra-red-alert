-- Duel d'IA : CA1+CA2 (nouvelle IA, modules Combined Arms, type normal)
-- contre AN1+AN2 (ancienne IA de Red Alert, recopiée dans rules.yaml).
-- Les mêmes pays de chaque côté (Rubénie + Ananthanie), positions en diagonale.
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

Spawns = {
	CA1 = CPos.New(27, 102),
	CA2 = CPos.New(102, 27),
	AN1 = CPos.New(27, 27),
	AN2 = CPos.New(102, 102),
}

Bilan = function()
	for _, nom in ipairs({ "CA1", "CA2", "AN1", "AN2" }) do
		local p = Player.GetPlayer(nom)
		local bat, unites, armee = 0, 0, 0
		local types = {}
		local utypes = {}
		for _, a in ipairs(p.GetActors()) do
			if not a.IsDead and a.Type ~= "player" then
				if a.HasProperty("Move") then
					unites = unites + 1
					utypes[a.Type] = (utypes[a.Type] or 0) + 1
					if a.Type ~= "harv" and a.Type ~= "mcv" then
						local ok, c = pcall(Actor.Cost, a.Type)
						if ok then armee = armee + c end
					end
				elseif a.HasProperty("StartBuildingRepairs") then
					bat = bat + 1
					types[a.Type] = (types[a.Type] or 0) + 1
				end
			end
		end
		local liste = ""
		for t, n in pairs(types) do liste = liste .. t .. "=" .. n .. " " end
		local uliste = ""
		for t, n in pairs(utypes) do uliste = uliste .. t .. "=" .. n .. " " end
		Log(nom .. " unites : " .. uliste)
		Log(string.format("%s argent=%d bat=%d unites=%d armee=%d$ tues=%d$ pertes=%d$ batTues=%d batPerdus=%d | %s",
			nom, p.Cash + p.Resources, bat, unites, armee, p.KillsCost, p.DeathsCost, p.BuildingsKilled, p.BuildingsLost, liste))
	end
end

Tick = function()
	if DateTime.GameTime > 0 and DateTime.GameTime % DateTime.Minutes(1) == 0 then
		Bilan()
	end
end

WorldLoaded = function()
	for nom, cell in pairs(Spawns) do
		local p = Player.GetPlayer(nom)
		Actor.Create("mcv", true, { Owner = p, Location = cell })
		p.Cash = 5000
		Log(nom .. " : VCM en " .. cell.X .. "," .. cell.Y .. " bot")
	end
	Camera.Position = WPos.New(64 * 1024, 64 * 1024, 0)
end
