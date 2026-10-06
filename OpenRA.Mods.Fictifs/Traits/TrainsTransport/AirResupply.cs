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
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Wagon de munitions du train aérien mobile (Elielistan) : réarme les avions alliés qui passent
	// au-dessus ou à côté, même en vol et même si le train roule. Le stock du wagon (son AmmoPool)
	// s'épuise et se reconstitue lentement, bien plus vite en gare.
	[Desc("Rearms friendly aircraft in range (airborne too) from the actor's own AmmoPool, which refills over time.")]
	public class AirResupplyInfo : ConditionalTraitInfo, Requires<AmmoPoolInfo>
	{
		[Desc("Horizontal range (altitude is ignored).")]
		public readonly WDist Range = WDist.FromCells(3);

		[Desc("Ticks between two rearming rounds (one ammo per aircraft per round).")]
		public readonly int Interval = 25;

		[Desc("Name of the AmmoPool used as stock.")]
		public readonly string AmmoPool = "primary";

		[Desc("Ticks to get one stock point back, on the tracks.")]
		public readonly int RefillDelay = 100;

		[Desc("Ticks to get one stock point back, in a station.")]
		public readonly int StationRefillDelay = 10;

		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new AirResupply(init.Self, this); }
	}

	public class AirResupply : ConditionalTrait<AirResupplyInfo>, ITick
	{
		readonly RailCar car;
		AmmoPool stock;
		int ticks;
		int refill;

		public AirResupply(Actor self, AirResupplyInfo info)
			: base(info)
		{
			car = self.TraitOrDefault<RailCar>();
		}

		protected override void Created(Actor self)
		{
			stock = self.TraitsImplementing<AmmoPool>().First(p => p.Info.Name == Info.AmmoPool);
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !self.IsInWorld)
				return;

			if (!stock.HasFullAmmo && --refill <= 0)
			{
				stock.GiveAmmo(self, 1);
				refill = car != null && car.InStation ? Info.StationRefillDelay : Info.RefillDelay;
			}

			if (--ticks > 0)
				return;

			ticks = Info.Interval;
			if (!stock.HasAmmo)
				return;

			var aircraft = self.World.FindActorsInCircle(self.CenterPosition, Info.Range)
				.Where(a => a != self && !a.IsDead && a.IsInWorld && a.Info.HasTraitInfo<AircraftInfo>()
					&& Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(a.Owner)));

			foreach (var a in aircraft)
			{
				foreach (var pool in a.TraitsImplementing<AmmoPool>())
				{
					if (!stock.HasAmmo)
						return;

					if (pool.GiveAmmo(a, 1))
						stock.TakeAmmo(self, 1);
				}
			}
		}
	}
}
