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
	// Débarquement d'un transport (voir ExtendedLandingManager) : aller au point d'accostage attribué,
	// en changer s'il est inaccessible, puis faire sortir les passagers un par un, chacun vers sa case
	// de la zone. Le transport n'attend personne : les autres transports débarquent de leur côté.
	public class ExtendedLandingActivity : Activity
	{
		const int MaxReplans = 4;
		const int MaxBlockedWaits = 6;

		readonly ExtendedLandingManager.LandingPlan plan;
		readonly Cargo cargo;
		readonly Mobile mobile;
		readonly ExtendedLanding landing;
		readonly INotifyUnloadCargo[] notifiers;

		enum Step { Moving, Unloading, Done }
		Step state = Step.Moving;
		int replans, blockedWaits, cooldown;

		public ExtendedLandingActivity(Actor self, ExtendedLandingManager.LandingPlan plan)
		{
			this.plan = plan;
			cargo = self.Trait<Cargo>();
			mobile = self.Trait<Mobile>();
			landing = self.Trait<ExtendedLanding>();
			notifiers = self.TraitsImplementing<INotifyUnloadCargo>().ToArray();
		}

		protected override void OnFirstRun(Actor self)
		{
			QueueChild(mobile.MoveTo(plan.Point, 0, null, true, landing.Info.TargetLineColor));
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return Finish(self, false);

			if (state == Step.Done)
				return true;

			if (cargo.IsEmpty())
				return Finish(self, true);

			if (state == Step.Moving)
			{
				if (cargo.CanUnload(BlockedByActor.Immovable) && NearZone(self))
				{
					state = Step.Unloading;
					QueueChild(new Wait(cargo.Info.BeforeUnloadDelay));
					return false;
				}

				return TryAnotherPoint(self);
			}

			if (--cooldown > 0)
				return false;

			var passenger = cargo.Peek();
			var destination = plan.NextDestination(passenger);
			var exit = ChooseExit(self, passenger, destination);
			if (exit == null)
			{
				// Rivage encombré : on attend un peu, puis on va accoster ailleurs.
				self.NotifyBlocker(cargo.CurrentAdjacentCells);
				if (destination != null)
					plan.Group.Claimed.Remove(destination.Value);

				if (++blockedWaits > MaxBlockedWaits)
				{
					blockedWaits = 0;
					state = Step.Moving;
					return TryAnotherPoint(self);
				}

				QueueChild(new Wait(10));
				return false;
			}

			blockedWaits = 0;
			foreach (var inu in notifiers)
				inu.Unloading(self);

			var prepared = landing.IsPrepared(passenger);
			var spawn = self.CenterPosition;
			var info = landing.Info;
			cargo.Unload(self, passenger);
			self.World.AddFrameEndTask(w =>
			{
				if (passenger.Disposed)
					return;

				var pos = passenger.Trait<IPositionable>();
				pos.SetPosition(passenger, exit.Value.Cell, exit.Value.SubCell);
				pos.SetCenterPosition(passenger, spawn);
				passenger.CancelActivity();
				w.Add(passenger);

				var move = passenger.TraitOrDefault<IMove>();
				if (move != null && destination != null)
					passenger.QueueActivity(move.MoveTo(destination.Value, 0, null, true, info.TargetLineColor));

				if (prepared && !string.IsNullOrEmpty(info.AssaultCondition))
				{
					var external = passenger.TraitsImplementing<ExternalCondition>()
						.FirstOrDefault(t => t.Info.Condition == info.AssaultCondition && t.CanGrantCondition(self));
					external?.GrantCondition(passenger, self, info.AssaultDuration);
				}
			});

			cooldown = info.UnloadInterval;
			return false;
		}

		// Assez près de la zone pour y débarquer (le point attribué, ou une case du rivage voisine).
		bool NearZone(Actor self)
		{
			if (self.Location == plan.Point)
				return true;

			return Util.ExpandFootprint(self.Location, true).Any(c => plan.Group.LandSet.Contains(c));
		}

		bool TryAnotherPoint(Actor self)
		{
			if (replans++ >= MaxReplans || plan.Manager.Replan(plan, self) == null)
			{
				// Plus aucun point libre : on décharge où l'on peut, sinon on renonce.
				if (cargo.CanUnload())
				{
					state = Step.Unloading;
					return false;
				}

				return Finish(self, false);
			}

			QueueChild(mobile.MoveTo(plan.Point, 0, null, true, landing.Info.TargetLineColor));
			return false;
		}

		// Case de sortie libre au contact du transport, la plus proche de la destination du passager.
		(CPos Cell, SubCell SubCell)? ChooseExit(Actor self, Actor passenger, CPos? destination)
		{
			var pos = passenger.Trait<IPositionable>();
			var target = destination ?? self.Location;
			return cargo.CurrentAdjacentCells
				.OrderBy(c => (c - target).LengthSquared).ThenBy(c => c.Y).ThenBy(c => c.X)
				.Select(c => (c, pos.GetAvailableSubCell(c)))
				.Where(s => s.Item2 != SubCell.Invalid)
				.Select(s => ((CPos, SubCell)?)s)
				.FirstOrDefault();
		}

		bool Finish(Actor self, bool unloaded)
		{
			if (state == Step.Done)
				return true;

			state = Step.Done;
			plan.Finished = true;

			if (unloaded && cargo.Info.AfterUnloadDelay > 0)
				QueueChild(new Wait(cargo.Info.AfterUnloadDelay, false));

			// Lancé depuis une base logistique : le transport vide y retourne pour la vague suivante.
			if (unloaded && !IsCanceling && plan.Base != null && !plan.Base.IsDead && plan.Base.Owner == self.Owner)
			{
				var dock = plan.Base.TraitOrDefault<AmphibiousBase>()?.DockCellFor(plan.Base, self);
				if (dock != null)
					QueueChild(mobile.MoveTo(dock.Value, 2, null, true, landing.Info.TargetLineColor));
			}

			return !unloaded || IsCanceling || ChildActivity == null;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (ChildActivity != null)
				foreach (var n in ChildActivity.TargetLineNodes(self))
					yield return n;

			yield return new TargetLineNode(Target.FromCell(self.World, plan.Point), landing.Info.TargetLineColor);
		}
	}
}
