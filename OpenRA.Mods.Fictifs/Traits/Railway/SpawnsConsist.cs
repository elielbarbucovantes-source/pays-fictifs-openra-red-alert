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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Train blindé produit avec sa composition réelle : les voitures listées sont créées
	// attelées à celle-ci dès son arrivée sur la voie (derrière : Behind, devant : InFront).
	[Desc("Spawns train cars coupled to this one when it enters the world (armoured train consists).")]
	public class SpawnsConsistInfo : TraitInfo, Requires<RailCarInfo>
	{
		[ActorReference]
		[Desc("Cars coupled behind this one, in order.")]
		public readonly string[] Behind = Array.Empty<string>();

		[ActorReference]
		[Desc("Cars coupled in front of this one, in order (the first is right in front).")]
		public readonly string[] InFront = Array.Empty<string>();

		// Longueur totale du train (cases), pour vérifier la place à la sortie du chantier.
		public int TotalCells(ActorInfo self, Ruleset rules)
		{
			return Behind.Concat(InFront).Sum(a => Math.Max(1, rules.Actors[a].TraitInfo<RailCarInfo>().Cells))
				+ Math.Max(1, self.TraitInfo<RailCarInfo>().Cells);
		}

		public override object Create(ActorInitializer init) { return new SpawnsConsist(this); }
	}

	public class SpawnsConsist : INotifyAddedToWorld
	{
		readonly SpawnsConsistInfo info;
		bool spawned;

		public SpawnsConsist(SpawnsConsistInfo info) { this.info = info; }

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (spawned)
				return;

			spawned = true;
			self.World.AddFrameEndTask(w =>
			{
				var car = self.TraitOrDefault<RailCar>();
				if (self.IsDead || car?.Train == null)
					return;

				foreach (var a in info.Behind)
					Add(w, self, car, a, false);

				foreach (var a in info.InFront)
					Add(w, self, car, a, true);
			});
		}

		static void Add(World w, Actor self, RailCar car, string type, bool front)
		{
			var train = car.Train;
			var network = car.Network;
			var end = front ? train.Front : train.Back;
			var inner = train.Chain.Count > 1 ? (front ? train.Chain[1] : train.Chain[train.Chain.Count - 2]) : end;

			// Case libre contre le bout du train, de préférence dans son prolongement
			// (train d'une case : derrière ou devant selon l'orientation de la voiture).
			var dir = end - inner;
			if (dir == CVec.Zero)
			{
				var v = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(car.Facing));
				dir = Math.Abs(v.X) > Math.Abs(v.Y) ? new CVec(Math.Sign(v.X), 0) : new CVec(0, Math.Sign(v.Y));
				if (!front)
					dir = -dir;
			}

			var straight = end + dir;
			var start = network.IsTrack(straight) && network.TrainAt(straight) == null ? straight
				: new[] { new CVec(0, -1), new CVec(1, 0), new CVec(0, 1), new CVec(-1, 0) }.Select(d => end + d)
					.Where(c => network.IsTrack(c) && network.TrainAt(c) == null).DefaultIfEmpty(end).First();

			// La nouvelle voiture regarde dans le sens du train, en s'éloignant de lui.
			var away = w.Map.CenterOfCell(start) - w.Map.CenterOfCell(end);
			var facing = away.HorizontalLengthSquared > 0 ? away.Yaw : car.Facing;
			var added = w.CreateActor(type, new TypeDictionary
			{
				new LocationInit(start),
				new OwnerInit(self.Owner),
				new FacingInit(facing + new WAngle(512)),
				new FactionInit(self.Owner.Faction.InternalName),
			});

			var rake = added.Trait<RailCar>().Train;
			if (rake == null || rake == train)
				return;

			// Le bout de la rame posée contre le train est celui qui est sur start.
			var at = rake.Front == start || rake.Back == start ? start : (front ? rake.Back : rake.Front);
			if (train.CanCouple(rake, at))
				train.Couple(rake, at, front);
		}
	}
}
