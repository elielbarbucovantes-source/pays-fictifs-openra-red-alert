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

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Sends a squadron from the map edge to patrol over the target area for a limited time.",
		"The aircraft need the " + nameof(AirPatrol) + " trait.")]
	public class AirPatrolPowerInfo : SupportPowerInfo
	{
		[ActorReference(new[] { typeof(AircraftInfo), typeof(AirPatrolInfo) })]
		[FieldLoader.Require]
		[Desc("Aircraft type sent on patrol.")]
		public readonly string UnitType = null;

		[Desc("Number of aircraft.")]
		public readonly int SquadSize = 4;

		[Desc("Offset between two aircraft of the squadron, relative to the flight direction.")]
		public readonly WVec SquadOffset = new(-1536, 1536, 0);

		[Desc("Time spent patrolling over the area (in ticks), counted from the squadron's arrival.")]
		public readonly int PatrolDuration = 750;

		[Desc("Number of different directions the squadron may come from.")]
		public readonly int QuantizedFacings = 8;

		[Desc("Distance beyond the map edge where the aircraft appear.")]
		public readonly WDist Cordon = new(5120);

		public override object Create(ActorInitializer init) { return new AirPatrolPower(init.Self, this); }
	}

	public class AirPatrolPower : SupportPower
	{
		readonly AirPatrolPowerInfo info;

		public AirPatrolPower(Actor self, AirPatrolPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var world = self.World;
			var facing = new WAngle(1024 * world.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings);
			var altitude = world.Map.Rules.Actors[info.UnitType].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
			var rotation = WRot.FromYaw(facing);
			var delta = new WVec(0, -1024, 0).Rotate(rotation);
			var center = order.Target.CenterPosition;
			var target = center + new WVec(0, 0, altitude);
			var startEdge = target - (world.Map.DistanceToEdge(target, -delta) + info.Cordon).Length * delta / 1024;

			world.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				for (var i = 0; i < info.SquadSize; i++)
				{
					// Line abreast, alternately left and right of the leader.
					var rank = (i + 1) / 2 * (i % 2 == 0 ? 1 : -1);
					var so = info.SquadOffset;
					var offset = new WVec(rank * so.Y, -System.Math.Abs(rank) * so.X, 0).Rotate(rotation);

					var a = w.CreateActor(info.UnitType, new TypeDictionary
					{
						new CenterPositionInit(startEdge + offset),
						new OwnerInit(self.Owner),
						new FacingInit(facing),
					});

					a.Trait<AirPatrol>().Start(center, info.PatrolDuration);
					a.QueueActivity(new Fly(a, Target.FromPos(target + offset)));
				}
			});
		}
	}
}
