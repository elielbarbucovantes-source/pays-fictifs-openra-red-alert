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
	[ScriptPropertyGroup("Combat")]
	public class ScoopsWaterProperties : ScriptActorProperties, Requires<ScoopsWaterInfo>, Requires<AttackBaseInfo>
	{
		readonly AttackBase attack;

		public ScoopsWaterProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			attack = self.TraitsImplementing<AttackBase>().First();
		}

		[ScriptActorPropertyActivity]
		[Desc("Fly over the given cell and spray the water load there (like Ctrl + click on the ground).")]
		public void SprayAt(CPos cell)
		{
			attack.AttackTarget(Target.FromCell(Self.World, cell), AttackSource.Default, true, true, true);
		}
	}
}
