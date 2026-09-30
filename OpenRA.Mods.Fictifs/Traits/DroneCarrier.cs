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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Porte-Drone : plateforme navale qui fabrique elle-même ses drones kamikazes, par lots.
	// RÈGLE ESSENTIELLE : le lot suivant n'est lancé qu'une fois TOUS les drones du lot
	// actuel détruits ; le compte à rebours démarre à la mort du dernier, pas du premier.

	[Desc("Spawns drones in batches. The next batch only starts RegenDelay ticks after the last drone",
		"of the current batch is gone. Right-clicking an enemy sends one drone; idle drones follow the carrier.")]
	public class DroneCarrierInfo : TraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Drone actor type.")]
		public readonly string Drone = null;

		[Desc("Drones per batch.")]
		public readonly int BatchSize = 6;

		[Desc("Ticks between the death of the batch's last drone and the next batch.")]
		public readonly int RegenDelay = 500;

		[Desc("Idle drones farther than this from the carrier fly back to it.")]
		public readonly WDist FollowRange = new(3584);

		[Desc("Distance of the drones' escort positions from the carrier.")]
		public readonly WDist EscortRadius = new(2048);

		[Desc("Ticks between two checks of the idle drones.")]
		public readonly int FollowInterval = 25;

		[CursorReference]
		public readonly string AttackCursor = "attack";

		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly string ReadyTextNotification = null;

		public override object Create(ActorInitializer init) { return new DroneCarrier(init.Self, this); }
	}

	public class DroneCarrier : ITick, IIssueOrder, IResolveOrder, IOrderVoice, ISelectionBar,
		INotifyKilled, INotifyActorDisposing, INotifyOwnerChanged
	{
		public readonly DroneCarrierInfo Info;
		readonly List<Actor> drones = new();
		readonly BitSet<TargetableType> droneTargets;
		bool firstBatchDone;
		int regenRemaining = -1;
		int followTicks;

		public DroneCarrier(Actor self, DroneCarrierInfo info)
		{
			Info = info;

			// Ce que les drones peuvent attaquer : le clic droit du navire ne propose que ces cibles.
			var droneInfo = self.World.Map.Rules.Actors[info.Drone];
			droneTargets = droneInfo.TraitInfos<ArmamentInfo>()
				.Select(a => a.WeaponInfo)
				.Where(w => w != null)
				.Aggregate(default(BitSet<TargetableType>), (acc, w) => acc.Union(w.ValidTargets));
		}

		public int Alive => drones.Count;
		public bool Regenerating => regenRemaining >= 0;

		void ITick.Tick(Actor self)
		{
			// Un drone perdu : détruit, plongé (retiré après son impact) ou passé à l'ennemi.
			drones.RemoveAll(d => d.IsDead || d.Owner != self.Owner);

			if (!firstBatchDone)
			{
				firstBatchDone = true;
				SpawnBatch(self);
				return;
			}

			if (drones.Count == 0 && !Regenerating)
				regenRemaining = Info.RegenDelay;

			if (Regenerating && --regenRemaining < 0)
			{
				SpawnBatch(self);
				if (!string.IsNullOrEmpty(Info.ReadyTextNotification))
					TextNotificationsManager.AddTransientLine(Info.ReadyTextNotification, self.Owner);
			}

			if (++followTicks >= Info.FollowInterval)
			{
				followTicks = 0;
				for (var i = 0; i < drones.Count; i++)
				{
					var d = drones[i];
					// Un drone en vol stationnaire n'est jamais « idle » : il exécute FlyIdle.
					if (!d.IsInWorld || !(d.IsIdle || d.CurrentActivity is FlyIdle))
						continue;

					if ((d.CenterPosition - self.CenterPosition).HorizontalLengthSquared > Info.FollowRange.LengthSquared)
						d.QueueActivity(false, d.Trait<IMove>().MoveTo(EscortCell(self, i), 2));
				}
			}
		}

		CPos EscortCell(Actor self, int slot)
		{
			var angle = new WAngle(1024 * slot / Info.BatchSize);
			var offset = new WVec(0, -Info.EscortRadius.Length, 0).Rotate(WRot.FromYaw(angle));
			return self.World.Map.Clamp(self.World.Map.CellContaining(self.CenterPosition + offset));
		}

		void SpawnBatch(Actor self)
		{
			regenRemaining = -1;
			var owner = self.Owner;
			var facing = self.TraitOrDefault<IFacing>()?.Facing ?? WAngle.Zero;
			var aircraft = self.World.Map.Rules.Actors[Info.Drone].TraitInfoOrDefault<AircraftInfo>();
			var altitude = aircraft?.CruiseAltitude ?? WDist.Zero;

			for (var i = 0; i < Info.BatchSize; i++)
			{
				var slot = i;
				var spawn = self.CenterPosition + new WVec(0, 0, altitude.Length);
				var drone = self.World.CreateActor(false, Info.Drone, new TypeDictionary
				{
					new OwnerInit(owner),
					new CenterPositionInit(spawn),
					new FacingInit(facing),
				});

				drones.Add(drone);
				self.World.AddFrameEndTask(w =>
				{
					if (drone.Disposed || self.IsDead)
						return;

					w.Add(drone);
					drone.QueueActivity(drone.Trait<IMove>().MoveTo(EscortCell(self, slot), 2));
				});
			}
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get { yield return new TargetTypeOrderTargeter(droneTargets, "LaunchDrone", 6, Info.AttackCursor, true, false); }
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == "LaunchDrone" ? new Order(order.OrderID, self, target, queued) : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "LaunchDrone")
				return;

			// Un seul drone par clic : le plus proche parmi ceux qui n'attaquent pas encore.
			var targetPos = order.Target.CenterPosition;
			var drone = drones
				.Where(d => d.IsInWorld && !d.IsDead && d.CurrentActivity is not FlyAttack)
				.OrderBy(d => (d.CenterPosition - targetPos).LengthSquared)
				.ThenBy(d => d.ActorID)
				.FirstOrDefault();

			if (drone == null)
				return;

			var attack = new Order("Attack", drone, order.Target, false);
			foreach (var ro in drone.TraitsImplementing<IResolveOrder>())
				ro.ResolveOrder(drone, attack);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "LaunchDrone" ? Info.Voice : null;
		}

		float ISelectionBar.GetValue()
		{
			return Regenerating ? 1f - (float)regenRemaining / Info.RegenDelay : 0;
		}

		Color ISelectionBar.GetColor() { return Color.Cyan; }

		bool ISelectionBar.DisplayWhenEmpty => false;

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// Sans leur navire, les drones perdent la liaison et s'écrasent.
			foreach (var d in drones.Where(d => !d.IsDead && d.IsInWorld))
				d.Kill(e.Attacker);

			drones.Clear();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			foreach (var d in drones.Where(d => !d.IsDead))
				d.Dispose();

			drones.Clear();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Déjà dans une tâche de fin d'image (Actor.ChangeOwner) : les drones suivent aussitôt.
			foreach (var d in drones.Where(d => !d.IsDead))
				d.ChangeOwnerSync(newOwner);
		}
	}
}
