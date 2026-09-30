#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Scripting
{
	[ScriptPropertyGroup("Ability")]
	public class RevolutionProperties : ScriptActorProperties, Requires<RevolutionOnDeployInfo>
	{
		public RevolutionProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[Desc("Start the revolution now, if it is charged (same as the deploy key).")]
		public void Revolution()
		{
			IResolveOrder trait = Self.Trait<RevolutionOnDeploy>();
			trait.ResolveOrder(Self, new Order("Revolution", Self, false));
		}
	}

	[ScriptPropertyGroup("Ability")]
	public class RaisesIslandProperties : ScriptActorProperties, Requires<RaisesIslandInfo>
	{
		public RaisesIslandProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[Desc("Start raising an island next to the ship (same as the deploy key).")]
		public void RaiseIsland()
		{
			IResolveOrder trait = Self.Trait<RaisesIsland>();
			trait.ResolveOrder(Self, new Order("RaiseIsland", Self, false));
		}
	}

	[ScriptPropertyGroup("Player")]
	public class ActivatePowerProperties : ScriptPlayerProperties
	{
		public ActivatePowerProperties(ScriptContext context, Player player)
			: base(context, player) { }

		[Desc("Activate a charged support power that needs no target, by its OrderName. Returns false if none was ready.")]
		public bool ActivatePower(string orderName)
		{
			var manager = Player.PlayerActor.Trait<SupportPowerManager>();
			var power = manager.Powers.Values.FirstOrDefault(p => p.Info != null && p.Info.OrderName == orderName && p.Ready);
			if (power == null)
				return false;

			power.Activate(new Order(power.Key, manager.Self, false));
			return true;
		}

		[Desc("Activate a charged targeted support power on a cell, by its OrderName. " +
			"Facing (0-255, -1 = automatic) is used by directional powers such as the tsunami. Returns false if none was ready.")]
		public bool ActivatePowerAt(string orderName, CPos cell, int facing = -1)
		{
			var manager = Player.PlayerActor.Trait<SupportPowerManager>();
			var power = manager.Powers.Values.FirstOrDefault(p => p.Info != null && p.Info.OrderName == orderName && p.Ready);
			if (power == null)
				return false;

			power.Activate(new Order(power.Key, manager.Self, Target.FromCell(Player.World, cell), false)
			{
				ExtraData = facing < 0 ? uint.MaxValue : (uint)facing
			});

			return true;
		}

		[Desc("Is this cell a valid tsunami target for the given facing (0-255, -1 = best direction)?")]
		public bool TsunamiTargetValid(CPos cell, int facing = -1)
		{
			var power = Player.World.ActorsWithTrait<TsunamiPower>().FirstOrDefault(p => p.Actor.Owner == Player).Trait;
			if (power == null)
				return false;

			var f = facing < 0 ? power.BestFacing(Player.World, cell) : WAngle.FromFacing(facing);
			return f.HasValue && new TsunamiBand(Player.World.Map, cell, f.Value, power.TsunamiInfo).IsValid(out _, out _);
		}

		[Desc("Describe the state of a support power by its OrderName (for tests and missions).")]
		public string SupportPowerState(string orderName)
		{
			var manager = Player.PlayerActor.Trait<SupportPowerManager>();
			var power = manager.Powers.Values.FirstOrDefault(p => p.Info != null && p.Info.OrderName == orderName);
			if (power == null)
				return "absent";

			return $"active={power.Active} disabled={power.Disabled} ready={power.Ready} remaining={power.RemainingTicks}";
		}
	}
}
