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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Essaim (Ananthanie) : des drones kamikazes décollent du bâtiment qui porte le pouvoir et
	// filent en attaque-mouvement vers la zone choisie, où ils plongent sur ce qu'ils trouvent.

	[Desc("Launches a swarm of drone actors from the power's owner, attack-moving to the target.")]
	public class DroneSwarmPowerInfo : SupportPowerInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Drone actor type.")]
		public readonly string UnitType = null;

		[Desc("Number of drones.")]
		public readonly int Count = 12;

		[Desc("Drones are spread around the target within this distance.")]
		public readonly WDist Spread = WDist.FromCells(3);

		public override object Create(ActorInitializer init) { return new DroneSwarmPower(init.Self, this); }
	}

	public class DroneSwarmPower : SupportPower
	{
		readonly DroneSwarmPowerInfo info;

		public DroneSwarmPower(Actor self, DroneSwarmPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var target = order.Target.CenterPosition;
			var aircraft = self.World.Map.Rules.Actors[info.UnitType].TraitInfoOrDefault<AircraftInfo>();
			var altitude = aircraft?.CruiseAltitude ?? WDist.Zero;
			var facing = (target - self.CenterPosition).Yaw;

			self.World.AddFrameEndTask(w =>
			{
				for (var i = 0; i < info.Count; i++)
				{
					var angle = new WAngle(1024 * i / info.Count);
					var offset = new WVec(0, -info.Spread.Length * (1 + i % 2) / 2, 0).Rotate(WRot.FromYaw(angle));
					var cell = w.Map.Clamp(w.Map.CellContaining(target + offset));
					var spawn = self.CenterPosition + new WVec(0, 0, altitude.Length) + offset / 4;

					var drone = w.CreateActor(info.UnitType, new TypeDictionary
					{
						new OwnerInit(self.Owner),
						new CenterPositionInit(spawn),
						new FacingInit(facing),
					});

					var attackMove = new Order("AttackMove", drone, Target.FromCell(w, cell), false);
					foreach (var ro in drone.TraitsImplementing<IResolveOrder>())
						ro.ResolveOrder(drone, attackMove);
				}
			});
		}
	}
}
