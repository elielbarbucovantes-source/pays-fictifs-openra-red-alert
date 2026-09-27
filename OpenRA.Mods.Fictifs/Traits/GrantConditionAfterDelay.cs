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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Grants a condition once RequiresCondition has been active without interruption for a given time.",
		"The condition is revoked as soon as RequiresCondition stops being active.",
		"Used for Rubénie units that become isolated after staying out of supply range.")]
	public class GrantConditionAfterDelayInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Ticks RequiresCondition must stay active before the condition is granted.")]
		public readonly int Delay = 750;

		public override object Create(ActorInitializer init) { return new GrantConditionAfterDelay(this); }
	}

	public class GrantConditionAfterDelay : ConditionalTrait<GrantConditionAfterDelayInfo>, ITick
	{
		int token = Actor.InvalidConditionToken;
		int elapsed;

		public GrantConditionAfterDelay(GrantConditionAfterDelayInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self)
		{
			elapsed = 0;
		}

		protected override void TraitDisabled(Actor self)
		{
			elapsed = 0;
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || token != Actor.InvalidConditionToken)
				return;

			if (++elapsed >= Info.Delay)
				token = self.GrantCondition(Info.Condition);
		}
	}
}
