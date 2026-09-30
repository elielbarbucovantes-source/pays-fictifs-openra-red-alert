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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Fictifs.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Holding Ctrl while loaded ParaDropOnOrder aircraft are selected switches to the drop targeting",
		"(left-click the zone, drag for the direction). Released before clicking: back to normal orders.")]
	public class ParaDropHotkeyInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new ParaDropHotkey(); }
	}

	public class ParaDropHotkey : ITickRender
	{
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			var world = self.World;
			if (world.LocalPlayer == null || !(world.OrderGenerator is UnitOrderGenerator))
				return;

			if (!Game.GetModifierKeys().HasModifier(Modifiers.Ctrl))
				return;

			var trait = world.Selection.Actors
				.Where(a => a.IsInWorld && !a.IsDead && a.Owner == world.LocalPlayer)
				.Select(a => a.TraitOrDefault<ParaDropOnOrder>())
				.FirstOrDefault(t => t != null && !t.IsEmpty);

			if (trait != null)
				world.OrderGenerator = new ParaDropTargeting(world, trait.Info);
		}
	}
}
