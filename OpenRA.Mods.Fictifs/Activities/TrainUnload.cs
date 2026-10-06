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

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Activities
{
	// Débarque les passagers d'un wagon un par un, train à l'arrêt.
	public class TrainUnload : Activity
	{
		readonly TrainCargo trainCargo;
		int delay;

		public TrainUnload(Actor self)
		{
			trainCargo = self.Trait<TrainCargo>();
		}

		public override bool Tick(Actor self)
		{
			var cargo = trainCargo.Cargo;
			var car = trainCargo.Car;
			if (IsCanceling || cargo.IsEmpty())
				return true;

			if (car.Train != null && car.Train.Moving)
				return false;

			car.Hold = Math.Max(car.Hold, 2);
			if (delay-- > 0)
				return false;

			var passenger = cargo.Peek();
			var pos = passenger.Trait<IPositionable>();
			var exits = cargo.CurrentAdjacentCells
				.Shuffle(self.World.SharedRandom)
				.Select(c => (Cell: c, SubCell: pos.GetAvailableSubCell(c)))
				.Where(e => e.SubCell != SubCell.Invalid)
				.ToList();

			if (exits.Count == 0)
			{
				delay = 10;
				return false;
			}

			var exit = exits[0];
			var inStation = car.InStation;
			var scatter = inStation ? exit.Cell : trainCargo.ScatterCell(self, exit.Cell);
			var spawn = self.CenterPosition;
			cargo.Unload(self);
			self.World.AddFrameEndTask(w =>
			{
				if (passenger.Disposed)
					return;

				pos.SetPosition(passenger, exit.Cell, exit.SubCell);
				pos.SetCenterPosition(passenger, spawn);
				passenger.CancelActivity();
				w.Add(passenger);

				var move = passenger.TraitOrDefault<IMove>();
				if (move != null && scatter != exit.Cell)
					passenger.QueueActivity(move.MoveTo(scatter, 1));
			});

			delay = inStation ? trainCargo.Info.StationUnloadDelay : trainCargo.Info.TrackUnloadDelay;
			return false;
		}
	}
}
