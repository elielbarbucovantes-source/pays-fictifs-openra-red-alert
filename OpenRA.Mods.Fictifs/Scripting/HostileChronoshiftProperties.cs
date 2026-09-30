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
	[ScriptPropertyGroup("Support Powers")]
	public class HostileChronoshiftProperties : ScriptActorProperties, Requires<HostileChronoshiftPowerInfo>
	{
		public HostileChronoshiftProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[Desc("Fire this building's chronoshift power (if charged) as the player would:",
			"units around source are sent to the same pattern around target. Returns true if the power was ready.")]
		public bool FireChronoshift(CPos source, CPos target)
		{
			var player = Self.Owner.PlayerActor;
			var manager = player.Trait<SupportPowerManager>();
			var power = Self.TraitsImplementing<HostileChronoshiftPower>().FirstOrDefault(p => !p.IsTraitDisabled);
			if (power == null)
				return false;

			var instance = manager.Powers.Values.FirstOrDefault(i => i.Instances.Contains(power));
			if (instance == null || !instance.Ready)
				return false;

			var order = new Order(power.Info.OrderName, player, Target.FromCell(Self.World, target), false) { ExtraLocation = source };
			((IResolveOrder)manager).ResolveOrder(player, order);
			return true;
		}
	}
}
