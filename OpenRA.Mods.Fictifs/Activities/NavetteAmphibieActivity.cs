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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Activities
{
	// Boucle d'une navette amphibie (voir NavetteAmphibie) :
	//  aller à l'escale de chargement → se remplir → aller à l'escale de déchargement → vider → recommencer.
	// Base : place à quai réservée, transferts avec le stock (AmphibiousBase.LoadOne / UnloadOne).
	// Plage : chargement des unités du joueur présentes dans la zone ; déchargement = débarquement
	// étendu sur toute la zone (ExtendedLandingActivity).
	public class NavetteAmphibieActivity : Activity
	{
		const int RetryDelay = 50;
		const int CallInterval = 25;

		enum Phase { AllerCharger, Charger, AllerDecharger, Decharger }

		readonly NavetteAmphibie navette;
		readonly Cargo cargo;
		readonly Mobile mobile;
		readonly ExtendedLandingManager manager;
		readonly HashSet<Actor> called = new();

		Phase phase = Phase.AllerCharger;
		ExtendedLandingManager.LandingPlan beach;
		bool tried;
		int transferTicks, idle, lastCount;

		public NavetteAmphibieActivity(Actor self)
		{
			navette = self.Trait<NavetteAmphibie>();
			cargo = self.Trait<Cargo>();
			mobile = self.Trait<Mobile>();
			manager = self.World.WorldActor.Trait<ExtendedLandingManager>();
		}

		Escale Stop => phase == Phase.AllerCharger || phase == Phase.Charger ? navette.Load : navette.Unload;

		int Weight => cargo.Passengers.Sum(p => p.Info.TraitInfo<PassengerInfo>().Weight);

		bool Full => Weight >= cargo.Info.MaxWeight;

		public override bool Tick(Actor self)
		{
			if (IsCanceling || !navette.Usable || !navette.HasProgram)
				return true;

			var stop = Stop;
			switch (phase)
			{
				case Phase.AllerCharger:
				case Phase.AllerDecharger:
					return stop.IsBase ? GoToBase(self, stop.Base) : GoToBeach(self, stop);

				case Phase.Charger:
				{
					if (stop.IsBase)
					{
						var ab = stop.Base.Trait<AmphibiousBase>();
						if (!ab.AtBerth(self))
						{
							Next(Phase.AllerCharger);
							return false;
						}

						if (--transferTicks <= 0)
						{
							transferTicks = navette.Info.TransferInterval;
							ab.LoadOne(self);
						}
					}
					else
					{
						Board(self);
						if (--transferTicks <= 0)
						{
							transferTicks = CallInterval;
							CallUnits(self, stop);
						}
					}

					if (cargo.PassengerCount > lastCount)
						idle = 0;
					else
						idle++;

					lastCount = cargo.PassengerCount;
					if (Full || (!cargo.IsEmpty() && idle >= navette.Info.MaxWait))
					{
						Depart(self, stop);
						Next(Phase.AllerDecharger);
					}

					return false;
				}

				case Phase.Decharger:
				{
					if (!stop.IsBase)
					{
						// Le débarquement sur la plage est terminé (ou a échoué : on recommence le cycle avec ce qui reste).
						Next(Phase.AllerCharger);
						return false;
					}

					var ab = stop.Base.Trait<AmphibiousBase>();
					if (cargo.IsEmpty())
					{
						ab.ReleaseBerth(self);
						Next(Phase.AllerCharger);
						return false;
					}

					if (!ab.AtBerth(self))
					{
						Next(Phase.AllerDecharger);
						return false;
					}

					if (--transferTicks <= 0)
					{
						transferTicks = navette.Info.TransferInterval;
						ab.UnloadOne(self);
					}

					return false;
				}
			}

			return false;
		}

		void Next(Phase next)
		{
			phase = next;
			tried = false;
			transferTicks = 0;
			idle = 0;
			lastCount = cargo.PassengerCount;
			beach = null;
		}

		// Aller à sa place à quai ; quai complet : attendre au large, près de la base.
		bool GoToBase(Actor self, Actor baseActor)
		{
			var ab = baseActor.Trait<AmphibiousBase>();
			if (ab.AtBerth(self))
			{
				Next(phase == Phase.AllerCharger ? Phase.Charger : Phase.Decharger);
				return false;
			}

			if (tried)
				QueueChild(new Wait(RetryDelay));

			tried = true;
			var berth = ab.DockCellFor(baseActor, self);
			if (berth != null)
				QueueChild(mobile.MoveTo(berth.Value, 0, null, true, LineColor));
			else
				QueueChild(mobile.MoveWithinRange(Target.FromActor(baseActor), ab.Info.DockRange + WDist.FromCells(3), targetLineColor: LineColor));

			return false;
		}

		Color LineColor => phase == Phase.AllerCharger ? navette.Info.LoadColor : navette.Info.UnloadColor;

		bool GoToBeach(Actor self, Escale stop)
		{
			if (phase == Phase.AllerDecharger)
			{
				if (cargo.IsEmpty())
				{
					Next(Phase.AllerCharger);
					return false;
				}

				var plan = manager.PlanFor(self, stop.TopLeft, stop.BottomRight, ExtendedLandingManager.PassengerLocomotors(new[] { self }));
				if (plan == null)
				{
					QueueChild(new Wait(RetryDelay * 2));
					return false;
				}

				phase = Phase.Decharger;
				QueueChild(new ExtendedLandingActivity(self, plan));
				return false;
			}

			// Plage de chargement : accoster au point le plus proche du centre de la zone.
			if (beach != null && (self.Location == beach.Point
				|| Util.ExpandFootprint(self.Location, true).Any(c => beach.Group.LandSet.Contains(c))))
			{
				var keep = beach;
				Next(Phase.Charger);
				beach = keep;
				return false;
			}

			if (tried)
				QueueChild(new Wait(RetryDelay));

			tried = true;
			var foot = manager.LocomotorByName(navette.Info.BeachLocomotor);
			beach = foot != null ? manager.PlanFor(self, stop.TopLeft, stop.BottomRight, new[] { foot }, false) : null;
			if (beach != null)
				QueueChild(mobile.MoveTo(beach.Point, 0, null, true, LineColor));

			return false;
		}

		// Plage de chargement : les unités du joueur au repos dans la zone sont appelées au bord du transport,
		// tant qu'il y a de la place ; arrivées à son contact, elles montent. (Pas de RideTransport : en
		// réservant sa place, il annulerait l'activité du transport.)
		void CallUnits(Actor self, Escale stop)
		{
			called.RemoveWhere(u => u.IsDead || !u.IsInWorld || u.Owner != self.Owner || (u.IsIdle && !Boarding(self, u)));
			var space = cargo.Info.MaxWeight - Weight - called.Sum(u => u.Info.TraitInfo<PassengerInfo>().Weight);
			if (space <= 0)
				return;

			var units = self.World.ActorsHavingTrait<Passenger>()
				.Where(u => u.Owner == self.Owner && u.IsInWorld && !u.IsDead && u.IsIdle && !called.Contains(u)
					&& stop.Contains(u.Location) && u.TraitOrDefault<Mobile>() != null)
				.OrderBy(u => (u.CenterPosition - self.CenterPosition).HorizontalLengthSquared).ThenBy(u => u.ActorID)
				.ToList();

			foreach (var u in units)
			{
				var pi = u.Info.TraitInfo<PassengerInfo>();
				if (pi.Weight > space || !cargo.Info.Types.Contains(pi.CargoType))
					continue;

				space -= pi.Weight;
				called.Add(u);
				u.QueueActivity(false, u.Trait<IMove>().MoveWithinRange(Target.FromActor(self), BoardRange, targetLineColor: navette.Info.LoadColor));
				u.ShowTargetLines();
			}
		}

		static readonly WDist BoardRange = new(1536);

		static bool Boarding(Actor self, Actor u)
		{
			return (u.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= (BoardRange.Length + 256) * (BoardRange.Length + 256);
		}

		// Les unités appelées qui touchent le transport montent à bord.
		void Board(Actor self)
		{
			foreach (var u in called.Where(u => !u.IsDead && u.IsInWorld && u.Owner == self.Owner && Boarding(self, u)).ToList())
			{
				called.Remove(u);
				u.CancelActivity();
				self.World.AddFrameEndTask(w =>
				{
					if (self.IsDead || !self.IsInWorld || u.IsDead || !u.IsInWorld || !cargo.HasSpace(u.Info.TraitInfo<PassengerInfo>().Weight))
						return;

					cargo.Load(self, u);
					w.Remove(u);
				});
			}
		}

		// Départ : les unités encore en chemin restent sur la plage ; la place à quai se libère.
		void Depart(Actor self, Escale stop)
		{
			foreach (var u in called)
				if (!u.IsDead && u.IsInWorld)
					u.CancelActivity();

			called.Clear();
			if (stop.IsBase)
				stop.Base.Trait<AmphibiousBase>().ReleaseBerth(self);
		}

		protected override void OnLastRun(Actor self)
		{
			foreach (var u in called)
				if (!u.IsDead && u.IsInWorld)
					u.CancelActivity();

			called.Clear();
			foreach (var e in new[] { navette.Load, navette.Unload })
				if (e != null && e.IsBase && !e.Base.IsDead)
					e.Base.TraitOrDefault<AmphibiousBase>()?.ReleaseBerth(self);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (ChildActivity != null)
				foreach (var n in ChildActivity.TargetLineNodes(self))
					yield return n;

			if (navette.HasProgram)
			{
				var stop = Stop;
				var target = stop.IsBase ? Target.FromActor(stop.Base) : Target.FromPos(stop.Center(self.World));
				yield return new TargetLineNode(target, LineColor);
			}
		}
	}
}
