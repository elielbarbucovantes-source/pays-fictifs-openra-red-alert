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
using OpenRA.Activities;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Activities
{
	// La locomotive emmène son train jusqu'à une case de voie (ou jusqu'à un wagon à atteler).
	// Le train part par le bout le plus proche du but (tête ou queue : il peut rouler dans les deux sens).
	// Un autre train sur le chemin bloque : on attend, puis on cherche un autre itinéraire.
	public class RailMove : Activity
	{
		readonly RailCar loco;
		readonly RailNetwork network;
		readonly CPos destination;
		readonly RailCar wagon;

		List<CPos> path;
		bool atFront;
		int waited;

		public RailMove(Actor self, CPos destination)
		{
			loco = self.Trait<RailCar>();
			network = loco.Network;
			this.destination = destination;
		}

		public RailMove(Actor self, RailCar wagon)
			: this(self, wagon.Cell)
		{
			this.wagon = wagon;
		}

		CPos Goal => wagon != null ? wagon.Cell : destination;

		bool WagonGone => wagon != null && (wagon.Self.IsDead || !wagon.Self.IsInWorld || wagon.Train == null
			|| !wagon.Train.IsRake || wagon.Self.Owner != loco.Self.Owner);

		public override bool Tick(Actor self)
		{
			var train = loco.Train;
			if (train == null)
				return true;

			// Le pas en cours se termine toujours (le train ne s'arrête qu'au milieu d'une case).
			if (train.Moving)
				return false;

			if (IsCanceling || loco.IsTraitDisabled || WagonGone || train.Locomotive != loco)
				return true;

			if (wagon != null && wagon.Train == train)
				return true;

			if (train.Held)
				return false;

			if (path == null && !Plan(train, false))
				return true;

			// Arrivé : le bout du train qui menait est sur le but.
			if (path.Count == 0)
				return true;

			var next = path[0];
			var other = network.TrainAt(next);

			// La rame visée (le wagon et ceux qui lui sont attelés) : on l'attelle par le bout touché.
			if (wagon != null && other == wagon.Train)
			{
				if (train.WagonCount + other.Cars.Count <= loco.Info.MaxWagons)
					train.Couple(other, next, atFront);

				return true;
			}

			if (!network.IsTrack(next))
				return !Plan(train, false);

			// Une rame du même joueur sur le chemin : on l'attelle et on la pousse.
			if (other != null && other != train && train.CanCouple(other, next)
				&& other.Cars[0].Self.Owner == loco.Self.Owner && train.WagonCount + other.Cars.Count <= loco.Info.MaxWagons)
			{
				train.Couple(other, next, atFront);
				path.RemoveAt(0);
				return false;
			}

			var trailing = atFront ? train.Back : train.Front;
			if (other != null && (other != train || next != trailing))
			{
				if (++waited % loco.Info.RepathDelay == 0)
					Plan(train, true);

				return false;
			}

			waited = 0;
			path.RemoveAt(0);
			train.BeginStep(next, atFront, loco.CurrentSpeed);
			return false;
		}

		// Chemin depuis la tête et depuis la queue ; on garde le plus court.
		// avoidTrains : les autres trains comptent comme obstacles (après une attente).
		bool Plan(Train train, bool avoidTrains)
		{
			var goal = Goal;
			if (train.Front == goal || train.Back == goal)
			{
				path = new List<CPos>();
				return true;
			}

			List<CPos> best = null;
			var bestFront = true;
			foreach (var front in train.Cars.Count > 1 ? new[] { true, false } : new[] { true })
			{
				var start = front ? train.Front : train.Back;
				var trailing = front ? train.Back : train.Front;
				var p = network.FindPath(start, goal, c =>
				{
					var t = network.TrainAt(c);
					if (t == null)
						return false;

					// Le train peut entrer dans la case que sa propre queue libère ; un autre train bloque si avoidTrains.
					return t == train ? c != trailing : avoidTrains;
				});

				if (p != null && (best == null || p.Count < best.Count))
				{
					best = p;
					bestFront = front;
				}
			}

			if (best == null)
				return path != null;

			path = best;
			atFront = bestFront;
			return true;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromCell(self.World, Goal), loco.Info.TargetLineColor);
		}
	}
}
