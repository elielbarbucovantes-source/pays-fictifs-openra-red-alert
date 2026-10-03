-- Test automatique des files de production (Normale / Multi-file).
Log = function(msg) print("TEST " .. DateTime.GameTime .. " : " .. msg) end

Make = function(type, owner, x, y)
	return Actor.Create(type, true, { Owner = owner, Location = CPos.New(x, y) })
end

Count = function(type)
	return #Me.GetActorsByType(type)
end

WorldLoaded = function()
	Me = Player.GetPlayer("Multi0")
	Make("fact", Me, 30, 60)
	Make("powr", Me, 34, 60)
	Make("powr", Me, 36, 60)
	Make("apwr", Me, 38, 60)
	T1 = Make("tent", Me, 30, 66)
	T2 = Make("tent", Me, 34, 66)
	W1 = Make("weap", Me, 30, 71)
	W2 = Make("weap", Me, 36, 71)
	Camera.Position = T1.CenterPosition

	Trigger.AfterDelay(5, function()
		Log("classic Player.Build e1 -> " .. tostring(Me.Build({ "e1" })))
		local ok, r1 = pcall(function() return T1.Build({ "e1" }) end)
		local ok2, r2 = pcall(function() return T2.Build({ "e1" }) end)
		local ok3, r3 = pcall(function() return W1.Build({ "jeep" }) end)
		local ok4, r4 = pcall(function() return W2.Build({ "jeep" }) end)
		Log("per-building tent1=" .. tostring(ok and r1) .. " tent2=" .. tostring(ok2 and r2) .. " weap1=" .. tostring(ok3 and r3) .. " weap2=" .. tostring(ok4 and r4))
		if not ok then Log("tent1 error " .. tostring(r1)) end
	end)

	for s = 2, 20, 2 do
		Trigger.AfterDelay(DateTime.Seconds(s), function()
			Log("t=" .. s .. "s e1=" .. Count("e1") .. " jeep=" .. Count("jeep"))
		end)
	end
	Trigger.AfterDelay(DateTime.Seconds(21), function() Log("FIN") end)
end
