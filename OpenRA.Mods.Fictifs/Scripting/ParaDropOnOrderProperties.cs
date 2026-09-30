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

using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Scripting
{
	[ScriptPropertyGroup("Transport")]
	public class ParaDropOnOrderProperties : ScriptActorProperties, Requires<ParaDropOnOrderInfo>
	{
		readonly ParaDropOnOrder paraDrop;

		public ParaDropOnOrderProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			paraDrop = self.Trait<ParaDropOnOrder>();
		}

		[Desc("True if the aircraft has passengers and is not reloading after a previous drop.")]
		public bool CanParaDrop => paraDrop.CanDrop;

		[ScriptActorPropertyActivity]
		[Desc("Fly along a drop line centred on the cell (facing 0-255 gives its direction, -1 = from the aircraft),",
			"parachute every passenger on the way, then return to base.")]
		public void ParaDropAt(CPos cell, int facing = -1)
		{
			if (!paraDrop.CanDrop)
				return;

			var center = Self.World.Map.CenterOfCell(cell);
			var dir = ParaDropOnOrder.Direction(facing < 0 ? null : WAngle.FromFacing(facing), Self.CenterPosition, center);
			Self.QueueActivity(new ParaDropFlight(Self, center, dir));
		}
	}
}
