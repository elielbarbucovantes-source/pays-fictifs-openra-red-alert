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
	// Gare portuaire : un transport naval à l'arrêt au quai passe directement ses passagers dans les wagons
	// en gare (marine → port → rail), sans les faire débarquer à terre.
	[Desc("Passengers of idle naval transports near this station are moved straight into the wagons standing on its tracks.")]
	public class PortTransferInfo : ConditionalTraitInfo, Requires<RailTrackInfo>
	{
		[Desc("Distance from the building centre within which naval transports are served.")]
		public readonly WDist Range = WDist.FromCells(4);

		[Desc("Ticks between two passengers moved into each wagon.")]
		public readonly int Interval = 15;

		[Desc("Terrain types a transport must be on to count as a ship.")]
		public readonly HashSet<string> WaterTerrainTypes = new() { "Water" };

		[Desc("Only wagons whose actor type is listed are loaded. Empty means every wagon.")]
		public readonly HashSet<string> WagonTypes = new();

		public override object Create(ActorInitializer init) { return new PortTransfer(init.Self, this); }
	}

	public class PortTransfer : ConditionalTrait<PortTransferInfo>, ITick
	{
		readonly RailTrack track;
		readonly RailNetwork network;
		int ticks;

		public PortTransfer(Actor self, PortTransferInfo info)
			: base(info)
		{
			track = self.Trait<RailTrack>();
			network = self.World.WorldActor.Trait<RailNetwork>();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !self.IsInWorld || --ticks > 0)
				return;

			ticks = Info.Interval;

			var wagons = track.Cells
				.Select(c => network.TrainAt(c)?.CarAt(c))
				.Distinct()
				.Where(car => car != null && car.Self.Owner == self.Owner && !car.Self.IsDead && (car.Train == null || !car.Train.Moving)
					&& (Info.WagonTypes.Count == 0 || Info.WagonTypes.Contains(car.Self.Info.Name)))
				.Select(car => (Actor: car.Self, Cargo: car.Self.TraitOrDefault<Cargo>()))
				.Where(w => w.Cargo != null)
				.ToList();

			if (wagons.Count == 0)
				return;

			var map = self.World.Map;
			var ships = self.World.FindActorsInCircle(self.CenterPosition, Info.Range)
				.Where(a => a.Owner == self.Owner && !a.IsDead && a.IsIdle && !a.Info.HasTraitInfo<RailCarInfo>()
					&& !a.Info.HasTraitInfo<AircraftInfo>() && Info.WaterTerrainTypes.Contains(map.GetTerrainInfo(a.Location).Type))
				.Select(a => (Actor: a, Cargo: a.TraitOrDefault<Cargo>()))
				.Where(s => s.Cargo != null && !s.Cargo.IsEmpty())
				.ToList();

			foreach (var wagon in wagons)
			{
				foreach (var ship in ships)
				{
					var passenger = ship.Cargo.Passengers.FirstOrDefault(p => Accepts(wagon.Cargo, p));
					if (passenger == null)
						continue;

					ship.Cargo.Unload(ship.Actor, passenger);
					wagon.Cargo.Load(wagon.Actor, passenger);
					break;
				}
			}
		}

		static bool Accepts(Cargo wagon, Actor passenger)
		{
			var p = passenger.TraitOrDefault<Passenger>();
			return p != null && wagon.Info.Types.Contains(p.Info.CargoType) && wagon.HasSpace(p.Info.Weight);
		}
	}
}
