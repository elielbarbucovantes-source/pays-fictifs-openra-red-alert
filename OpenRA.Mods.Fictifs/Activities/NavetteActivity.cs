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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Activities
{
	// Boucle de la navette (voir Navette) : aller charger → charger → aller décharger → décharger → ...
	// Le chargement et le déchargement se font en gare quel que soit l'endroit des wagons le long du quai :
	// le train s'arrête la tête (ou la queue) au bout le plus éloigné des voies de la gare.
	public class NavetteActivity : Activity
	{
		enum Phase { AllerCharger, Charger, AllerDecharger, Decharger }

		const int RetryDelay = 25;

		readonly Navette navette;
		readonly RailCar loco;
		Phase phase;
		int transferTicks;
		int idle;
		bool tried;

		public NavetteActivity(Actor self)
		{
			navette = self.Trait<Navette>();
			loco = navette.Car;
		}

		Actor Gare => phase == Phase.AllerCharger || phase == Phase.Charger ? navette.LoadGare : navette.UnloadGare;

		public override bool Tick(Actor self)
		{
			var train = loco.Train;
			if (IsCanceling || !navette.Usable || !navette.HasProgram || train == null || train.Locomotive != loco)
				return true;

			if (train.Moving)
				return false;

			var gare = Gare;
			var stock = gare.Trait<GareStock>();
			var wagons = navette.Wagons(stock).ToList();
			if (wagons.Count == 0)
				return true;

			switch (phase)
			{
				case Phase.AllerCharger:
				case Phase.AllerDecharger:
				{
					if (InGare(train, gare))
					{
						phase = phase == Phase.AllerCharger ? Phase.Charger : Phase.Decharger;
						transferTicks = 0;
						idle = 0;
						tried = false;
						return false;
					}

					// Le déplacement précédent n'a pas abouti (pas de chemin) : on patiente avant de réessayer.
					if (tried)
						QueueChild(new Wait(RetryDelay));

					var goal = GoalCell(train, gare);
					if (goal != null)
						QueueChild(new RailMove(self, goal.Value));

					tried = true;
					return false;
				}

				case Phase.Charger:
				{
					if (wagons.All(w => Navette.Full(w.Cargo)))
					{
						Depart();
						return false;
					}

					idle++;
					if (--transferTicks <= 0)
					{
						transferTicks = navette.Info.TransferInterval;
						foreach (var w in wagons)
						{
							var cargo = w.Cargo;
							var unit = stock.Take(u => Navette.Accepts(cargo, u));
							if (unit == null)
								continue;

							cargo.Load(w.Car.Self, unit);
							idle = 0;
						}
					}

					if (idle >= navette.Info.MaxWait && wagons.Any(w => !w.Cargo.IsEmpty()))
						Depart();

					return false;
				}

				case Phase.Decharger:
				{
					if (wagons.All(w => w.Cargo.IsEmpty()))
					{
						phase = Phase.AllerCharger;
						tried = false;
						return false;
					}

					if (--transferTicks > 0)
						return false;

					transferTicks = navette.Info.TransferInterval;
					foreach (var w in wagons)
					{
						if (w.Cargo.IsEmpty())
							continue;

						var passenger = w.Cargo.Peek();
						if (stock.FindExit(passenger) == null)
							continue;

						w.Cargo.Unload(w.Car.Self, passenger);
						stock.Release(passenger, true);
					}

					return false;
				}
			}

			return false;
		}

		void Depart()
		{
			phase = Phase.AllerDecharger;
			tried = false;
		}

		static bool InGare(Train train, Actor gare)
		{
			var track = gare.Trait<RailTrack>();
			return track.Cells.Any(train.Contains);
		}

		// Case de la gare la plus éloignée du train (le long des voies) : le train entre en entier sous la halle.
		CPos? GoalCell(Train train, Actor gare)
		{
			var network = loco.Network;
			CPos? best = null;
			var bestDist = -1;
			foreach (var cell in gare.Trait<RailTrack>().Cells)
			{
				var dist = int.MaxValue;
				foreach (var end in new[] { train.Front, train.Back })
				{
					var path = network.FindPath(end, cell, c => train.Contains(c));
					if (path != null && path.Count < dist)
						dist = path.Count;
				}

				if (dist != int.MaxValue && dist > bestDist)
				{
					best = cell;
					bestDist = dist;
				}
			}

			return best;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (!navette.HasProgram)
				yield break;

			var gare = Gare;
			var other = gare == navette.LoadGare ? navette.UnloadGare : navette.LoadGare;
			yield return new TargetLineNode(Target.FromActor(gare), gare == navette.LoadGare ? navette.Info.LoadColor : navette.Info.UnloadColor);
			yield return new TargetLineNode(Target.FromActor(other), other == navette.LoadGare ? navette.Info.LoadColor : navette.Info.UnloadColor);
		}
	}
}
