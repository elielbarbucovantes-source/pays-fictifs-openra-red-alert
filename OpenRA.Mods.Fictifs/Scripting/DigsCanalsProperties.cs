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
	[ScriptPropertyGroup("Movement")]
	public class DigsCanalsProperties : ScriptActorProperties, Requires<DigsCanalsInfo>
	{
		readonly DigsCanals digs;

		public DigsCanalsProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			digs = self.Trait<DigsCanals>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Dig a canal along the line from start to end (like the deploy key and a dragged line).")]
		public void DigCanal(CPos start, CPos end)
		{
			Self.QueueActivity(new DigCanal(Self, DigsCanals.Line(start, end, digs.Info.MaxLength)));
		}
	}
}
