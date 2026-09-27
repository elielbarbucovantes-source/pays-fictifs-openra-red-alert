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
	[Desc("Grants a condition for a limited time once the condition in RequiresCondition stops being active.",
		"Used for units that are vulnerable for a few seconds after being dropped by a Carryall or a parachute.")]
	public class GrantTimedConditionOnConditionEndInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Duration of the granted condition (in ticks).")]
		public readonly int Duration = 100;

		public override object Create(ActorInitializer init) { return new GrantTimedConditionOnConditionEnd(this); }
	}

	public class GrantTimedConditionOnConditionEnd : ConditionalTrait<GrantTimedConditionOnConditionEndInfo>, ITick
	{
		int token = Actor.InvalidConditionToken;
		int remaining;

		public GrantTimedConditionOnConditionEnd(GrantTimedConditionOnConditionEndInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self)
		{
			// The watched condition is back: the timed condition no longer applies.
			remaining = 0;
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		protected override void TraitDisabled(Actor self)
		{
			remaining = Info.Duration;
			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(Info.Condition);
		}

		void ITick.Tick(Actor self)
		{
			if (remaining <= 0 || --remaining > 0)
				return;

			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}
	}
}
