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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Instant support power: for a while, the owner sees what every submarine on the map sees,",
		"and enemy submarines are revealed (they get an external condition that should stop their cloak).",
		"Used by the Australouis « Radar blink ».")]
	public class SubmarineVisionPowerInfo : SupportPowerInfo
	{
		[Desc("Duration of the effect, in ticks.")]
		public readonly int Duration = 1500;

		[Desc("Vision radius around each submarine.")]
		public readonly WDist Range = WDist.FromCells(8);

		[Desc("Cloak detection types that make an actor a submarine.")]
		public readonly string DetectionType = "Underwater";

		[Desc("External condition granted to enemy submarines for the duration.")]
		public readonly string RevealCondition = "sonar";

		public override object Create(ActorInitializer init) { return new SubmarineVisionPower(init.Self, this); }
	}

	public class SubmarineVisionPower : SupportPower, ITick, INotifyRemovedFromWorld
	{
		readonly SubmarineVisionPowerInfo info;
		readonly Dictionary<Actor, PPos[]> sources = new();
		int remaining;

		public SubmarineVisionPower(Actor self, SubmarineVisionPowerInfo info)
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
			remaining = info.Duration;

			foreach (var p in self.World.ActorsWithTrait<ExternalCondition>())
			{
				if (p.Trait.Info.Condition != info.RevealCondition || p.Actor.IsDead || !p.Actor.IsInWorld)
					continue;

				if (self.Owner.RelationshipWith(p.Actor.Owner) == PlayerRelationship.Ally)
					continue;

				if (p.Trait.CanGrantCondition(self))
					p.Trait.GrantCondition(p.Actor, self, info.Duration);
			}

			UpdateVision(self);
		}

		IEnumerable<Actor> Submarines(World world)
		{
			return world.ActorsWithTrait<Cloak>()
				.Where(p => p.Actor.IsInWorld && !p.Actor.IsDead && p.Trait.Info.DetectionTypes.Contains(info.DetectionType))
				.Select(p => p.Actor)
				.Distinct();
		}

		void UpdateVision(Actor self)
		{
			var shroud = self.Owner.Shroud;
			ClearVision(self);
			foreach (var sub in Submarines(self.World))
			{
				var cells = Shroud.ProjectedCellsInRange(self.World.Map, sub.CenterPosition, WDist.Zero, info.Range).ToArray();
				shroud.AddSource(sub, Shroud.SourceType.Visibility, cells);
				sources[sub] = cells;
			}
		}

		void ClearVision(Actor self)
		{
			var shroud = self.Owner.Shroud;
			foreach (var key in sources.Keys)
				shroud.RemoveSource(key);

			sources.Clear();
		}

		void ITick.Tick(Actor self)
		{
			if (remaining <= 0)
				return;

			if (--remaining == 0)
			{
				ClearVision(self);
				return;
			}

			if (remaining % 5 == 0)
				UpdateVision(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			remaining = 0;
			ClearVision(self);
		}
	}
}
