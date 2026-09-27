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
	[Desc("Grants a condition while the actor moves, and keeps it for a short while after it stops,",
		"so that brief halts (waiting for a path, turning) do not revoke it.",
		"Used by the Rubénie Tunnelier, which travels underground and surfaces when it stops.")]
	public class GrantConditionWhileMovingInfo : ConditionalTraitInfo, Requires<MobileInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Ticks the condition is kept after the actor stops moving.")]
		public readonly int LingerDelay = 20;

		public override object Create(ActorInitializer init) { return new GrantConditionWhileMoving(this); }
	}

	public class GrantConditionWhileMoving : ConditionalTrait<GrantConditionWhileMovingInfo>, INotifyMoving, ITick
	{
		int token = Actor.InvalidConditionToken;
		bool moving;
		int linger;

		public GrantConditionWhileMoving(GrantConditionWhileMovingInfo info)
			: base(info) { }

		void INotifyMoving.MovementTypeChanged(Actor self, MovementType type)
		{
			moving = type.HasMovementType(MovementType.Horizontal);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (moving)
			{
				linger = Info.LingerDelay;
				if (token == Actor.InvalidConditionToken)
					token = self.GrantCondition(Info.Condition);

				return;
			}

			if (token != Actor.InvalidConditionToken && --linger <= 0)
				token = self.RevokeCondition(token);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}
	}
}
