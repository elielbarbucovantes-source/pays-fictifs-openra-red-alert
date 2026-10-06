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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Condition accordée seulement après un délai continu d'activation (mise en batterie du Schwerer Gustav).
	[Desc("Grants a condition once this trait has been enabled for Delay ticks in a row; revoked as soon as it is disabled.")]
	public class GrantConditionAfterDelayInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		public readonly string Condition = null;

		[Desc("Ticks before the condition is granted.")]
		public readonly int Delay = 250;

		[Desc("Show a selection bar while waiting.")]
		public readonly bool ShowSelectionBar = true;

		public readonly Color SelectionBarColor = Color.Orange;

		public override object Create(ActorInitializer init) { return new GrantConditionAfterDelay(this); }
	}

	public class GrantConditionAfterDelay : ConditionalTrait<GrantConditionAfterDelayInfo>, ITick, ISelectionBar
	{
		int ticks;
		int token = Actor.InvalidConditionToken;

		public GrantConditionAfterDelay(GrantConditionAfterDelayInfo info)
			: base(info) { }

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || token != Actor.InvalidConditionToken)
				return;

			if (++ticks >= Info.Delay)
				token = self.GrantCondition(Info.Condition);
		}

		protected override void TraitDisabled(Actor self)
		{
			ticks = 0;
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || IsTraitDisabled || token != Actor.InvalidConditionToken)
				return 0;

			return (float)ticks / Info.Delay;
		}

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }

		bool ISelectionBar.DisplayWhenEmpty => false;
	}
}
