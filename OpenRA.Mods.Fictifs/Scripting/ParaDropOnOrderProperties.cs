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
		[Desc("Fly to the cell, parachute every passenger, then return to base.")]
		public void ParaDropAt(CPos cell)
		{
			if (paraDrop.CanDrop)
				Self.QueueActivity(new ParaDropFlight(Self, Target.FromCell(Self.World, cell)));
		}
	}
}
