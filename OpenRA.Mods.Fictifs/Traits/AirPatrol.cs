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

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Aircraft sent by an " + nameof(AirPatrolPower) + ": circles over the patrol area for a limited time,",
		"engaging targets with its " + nameof(AutoTarget) + ", then flies off the map.")]
	public class AirPatrolInfo : TraitInfo
	{
		[Desc("The patrol time starts once the aircraft is this close to the patrol centre.")]
		public readonly WDist ArrivalRange = WDist.FromCells(4);

		[Desc("An idle aircraft further than this from the patrol centre flies back to it.")]
		public readonly WDist LeashRange = WDist.FromCells(6);

		[Desc("The aircraft leaves after this many times the patrol duration even if it never reached the area",
			"(e.g. because its owner sent it elsewhere).")]
		public readonly int MaxLifetimeFactor = 3;

		public override object Create(ActorInitializer init) { return new AirPatrol(this); }
	}

	public class AirPatrol : ITick
	{
		readonly AirPatrolInfo info;
		WPos center;
		int remaining, lifetime;
		bool active, arrived, leaving;

		public AirPatrol(AirPatrolInfo info)
		{
			this.info = info;
		}

		public void Start(WPos center, int duration)
		{
			this.center = center;
			remaining = duration;
			lifetime = duration * info.MaxLifetimeFactor;
			active = true;
		}

		void ITick.Tick(Actor self)
		{
			if (!active || leaving || self.IsDead)
				return;

			var distance = (self.CenterPosition - center).HorizontalLength;
			if (--lifetime <= 0 || (arrived && --remaining <= 0))
			{
				leaving = true;

				// Stop engaging, so that return fire does not cancel the departure.
				self.TraitOrDefault<AutoTarget>()?.SetStance(self, UnitStance.HoldFire);
				self.CancelActivity();
				self.QueueActivity(new FlyOffMap(self));
				self.QueueActivity(new RemoveSelf());
				return;
			}

			if (!arrived)
			{
				arrived = distance <= info.ArrivalRange.Length;
				return;
			}

			// Chasing a target may have drawn the aircraft away: bring it back once it has nothing to do.
			if (self.CurrentActivity is FlyIdle && distance > info.LeashRange.Length)
				self.QueueActivity(false, new Fly(self, Target.FromPos(center)));
		}
	}
}
