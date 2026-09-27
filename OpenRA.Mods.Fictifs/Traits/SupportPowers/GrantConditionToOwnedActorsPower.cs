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

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Instant support power: grants an external condition for a limited time to every actor of the owner",
		"(including the player actor) that has a matching " + nameof(ExternalCondition) + ".")]
	public class GrantConditionToOwnedActorsPowerInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[Desc("External condition to grant.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). 0 means permanent.")]
		public readonly int Duration = 0;

		public override object Create(ActorInitializer init) { return new GrantConditionToOwnedActorsPower(init.Self, this); }
	}

	public class GrantConditionToOwnedActorsPower : SupportPower
	{
		readonly GrantConditionToOwnedActorsPowerInfo info;

		public GrantConditionToOwnedActorsPower(Actor self, GrantConditionToOwnedActorsPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.IssueOrder(new Order(order, manager.Self, false));
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var targets = self.World.ActorsWithTrait<ExternalCondition>()
				.Where(p => p.Actor.Owner == self.Owner && !p.Actor.IsDead && !p.Actor.Disposed
					&& p.Trait.Info.Condition == info.Condition && p.Trait.CanGrantCondition(self))
				.ToList();

			foreach (var p in targets)
				p.Trait.GrantCondition(p.Actor, self, info.Duration);
		}
	}
}
