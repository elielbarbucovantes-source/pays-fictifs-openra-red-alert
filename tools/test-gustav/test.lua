Log = function(msg) print("FERRO " .. DateTime.GameTime .. " : " .. msg) end
WorldLoaded = function()
	local me = Player.GetPlayer("Multi0")
	local en = Player.GetPlayer("Ennemi")
	X0, Y0 = 58, 82
	local m = function(t, p, x, y) return Actor.Create(t, true, { Owner = p, Location = CPos.New(X0 + x, Y0 + y) }) end
	Utils.Do(Map.ActorsInBox(Map.CenterOfCell(CPos.New(X0 - 2, Y0 - 9)), Map.CenterOfCell(CPos.New(X0 + 40, Y0 + 10))), function(a) if a.Type ~= "mpspawn" then a.Destroy() end end)
	for x = 0, 20 do m("rail", me, x, 0) end
	G = m("gustav", me, 8, 0)
	Pres = m("powr", en, 10, 8)
	Loin = m("powr", en, 33, -2)
	Trigger.AfterDelay(40, function() G.ToggleDeploy() end)
	-- éclaireur près de la cible lointaine (le Gustav ne voit qu'à 8 cases)
	m("e1", me, 31, 0)
	Trigger.AfterDelay(300, function() G.Attack(Pres, true, true) Log("attaque proche (non vue, déjà repérée)") end)
	for t = 320, 900, 60 do Trigger.AfterDelay(t, function() Log("proche " .. (Pres.IsDead and 0 or Pres.Health) .. " loin " .. (Loin.IsDead and 0 or Loin.Health) .. " inactif " .. tostring(G.IsIdle)) end) end
	Trigger.AfterDelay(950, function() G.Attack(Loin, true, true) Log("attaque loin") end)
	for t = 1000, 2700, 100 do Trigger.AfterDelay(t, function() Log("proche " .. (Pres.IsDead and 0 or Pres.Health) .. " loin " .. (Loin.IsDead and 0 or Loin.Health) .. " inactif " .. tostring(G.IsIdle)) end) end
	Trigger.AfterDelay(2750, function() Log("FIN") end)
end
