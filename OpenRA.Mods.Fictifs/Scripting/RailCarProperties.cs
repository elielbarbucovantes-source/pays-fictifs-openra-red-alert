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
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Scripting
{
	[ScriptPropertyGroup("Movement")]
	public class RailCarProperties : ScriptActorProperties, Requires<RailCarInfo>
	{
		readonly RailCar car;

		public RailCarProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			car = self.Trait<RailCar>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Locomotive: drive the train to the track nearest to the cell.")]
		public void RailMove(CPos cell)
		{
			var track = car.Network.NearestTrack(cell, car.Info.TrackSearchRange);
			if (car.Info.Locomotive && track != null)
				Self.QueueActivity(new RailMove(Self, track.Value));
		}

		[ScriptActorPropertyActivity]
		[Desc("Locomotive: drive to the uncoupled wagon and couple it.")]
		public void CoupleWagon(Actor wagon)
		{
			if (car.Info.Locomotive)
				Self.QueueActivity(new RailMove(Self, wagon.Trait<RailCar>()));
		}

		[Desc("Same as a right click from this car on the other one: the locomotive comes and couples the rake,",
			"or two rakes standing end to end are coupled at once. Returns false if they cannot be coupled.")]
		public bool CoupleTo(Actor other)
		{
			var o = other.Trait<RailCar>();
			var plan = RailCar.CouplePlan(car, o);
			if (plan.Rake == null || plan.Full)
				return false;

			if (plan.Loco != null)
				plan.Loco.Self.QueueActivity(new RailMove(plan.Loco.Self, plan.Rake));
			else
				car.Train.Join(o.Train);

			return true;
		}

		[Desc("Locomotive: uncouple the wagon and the wagons behind it (only in a station).")]
		public bool UncoupleWagon(Actor wagon)
		{
			var w = wagon.Trait<RailCar>();
			if (car.Train == null || w.Train != car.Train || !w.InStation)
				return false;

			car.Train.Uncouple(w);
			return true;
		}

		[Desc("Unload every wagon of the train.")]
		public void UnloadTrain()
		{
			if (car.Train == null)
				return;

			foreach (var c in car.Train.Cars)
				foreach (var cargo in c.Self.TraitsImplementing<TrainCargo>())
					cargo.Unload(c.Self, false);
		}

		[Desc("Number of cars (locomotive and wagons) in the train of this car.")]
		public int TrainLength => car.Train?.Cars.Count ?? 0;

		[Desc("Cars of the train, from front to back.")]
		public Actor[] TrainCars => car.Train?.Cars.Select(c => c.Self).ToArray() ?? new Actor[0];

		[Desc("Is this car on a station (or depot) track of its owner or an ally?")]
		public bool InStation => car.InStation;

		[Desc("Is the train moving between two cells?")]
		public bool TrainMoving => car.Train != null && car.Train.Moving;
	}
}
