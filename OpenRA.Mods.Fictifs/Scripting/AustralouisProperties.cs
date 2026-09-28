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
