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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Like AttackFrontal, but fires as soon as ONE armament is in range of the target",
		"(each armament fires within its own range) instead of waiting for the shortest-ranged one.",
		"For units mixing short and very long ranged weapons (carriers).")]
	public class AttackPorteAvionsInfo : AttackFrontalInfo
	{
		public override object Create(ActorInitializer init) { return new AttackPorteAvions(init.Self, this); }
	}

	public class AttackPorteAvions : AttackFrontal
	{
		public AttackPorteAvions(Actor self, AttackPorteAvionsInfo info)
			: base(self, info) { }

		public override Activity GetAttackActivity(Actor self, AttackSource source, in Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor = null)
		{
			return new AttaquePorteAvions(self, newTarget, allowMove, forceAttack, targetLineColor);
		}
	}
}
