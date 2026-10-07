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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Carries aircraft on board. Aircraft with the " + nameof(CarrierAircraft) + " trait land on the carrier",
		"(the carrier needs the Reservable trait and must be listed in the aircraft's Rearmable.RearmActors)",
		"and are stowed, rearmed and repaired. Units in the carrier's Cargo hold, if any, are repaired too.",
		"The deploy order launches every ready aircraft (and unloads the Cargo hold when possible).")]
	public class AircraftCarrierInfo : TraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Aircraft actor types that can be stowed on this carrier.")]
		public readonly HashSet<string> Types = new();

		[Desc("Maximum number of aircraft on board.")]
		public readonly int MaxAircraft = 5;

		[ActorReference]
		[Desc("Aircraft on board when the carrier is created.")]
		public readonly string[] InitialAircraft = System.Array.Empty<string>();

		[Desc("Ticks needed on board to fully rearm and repair an aircraft.")]
		public readonly int RearmDelay = 250;

		[Desc("Units carried in the Cargo hold are repaired every this many ticks",
			"(a fully damaged passenger is back to full health after RearmDelay ticks).")]
		public readonly int PassengerRepairInterval = 25;

		[Desc("Ticks between two consecutive launches.")]
		public readonly int LaunchInterval = 15;

		[Desc("Aircraft take off on their own as soon as they are rearmed and repaired.")]
		public readonly bool AutoLaunch = false;

		[Desc("An aircraft that reserved the carrier is stowed when closer than this (horizontally)...")]
		public readonly WDist StowRange = new(1536);

		[Desc("...and lower than this altitude (at or above cruise altitude: stowed as soon as it overflies the deck).")]
		public readonly WDist StowAltitude = new(3072);

		[Desc("Distance ahead of the carrier that launched aircraft fly to.")]
		public readonly WDist LaunchDistance = new(3072);

		[Desc("Condition granted while an aircraft is on board, by aircraft type (e.g. to draw it parked on the deck).")]
		public readonly Dictionary<string, string> StowedConditions = new();

		[GrantedConditionReference]
		public IEnumerable<string> LinterStowedConditions => StowedConditions.Values;

		[Desc("Bots launch their ready aircraft automatically every this many ticks. 0 disables.")]
		public readonly int BotLaunchInterval = 250;

		[CursorReference]
		public readonly string LaunchCursor = "deploy";

		[CursorReference]
		public readonly string LaunchBlockedCursor = "deploy-blocked";

		[VoiceReference]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new AircraftCarrier(init, this); }
	}

	public class AircraftCarrier : ITick, IIssueOrder, IResolveOrder, IOrderVoice, IIssueDeployOrder,
		INotifyKilled, INotifyOwnerChanged, INotifyActorDisposing
	{
		sealed class Stowed
		{
			public Actor Actor;
			public int ReadyAt;
			public int Token = Actor.InvalidConditionToken;
		}

		public readonly AircraftCarrierInfo Info;
		readonly Actor self;
		readonly List<Stowed> stowed = new();
		readonly HashSet<Actor> stowing = new();
		int ticks;
		int launchesPending;
		int launchCooldown;

		// Porte-avions qui transporte aussi des troupes (Porta Aguila) : le
		// déploiement fait décoller les avions ET débarquer les troupes.
		Cargo cargo;
		bool cargoResolved;

		Cargo CarriedTroops
		{
			get
			{
				if (!cargoResolved)
				{
					cargo = self.TraitOrDefault<Cargo>();
					cargoResolved = true;
				}

				return cargo;
			}
		}

		bool CanUnloadTroops => CarriedTroops != null && !CarriedTroops.IsEmpty() && CarriedTroops.CanUnload();

		public AircraftCarrier(ActorInitializer init, AircraftCarrierInfo info)
		{
			self = init.Self;
			Info = info;

			foreach (var name in info.InitialAircraft)
			{
				var aircraft = self.World.CreateActor(false, name.ToLowerInvariant(),
					new TypeDictionary { new OwnerInit(self.Owner) });

				stowed.Add(new Stowed { Actor = aircraft, ReadyAt = 0 });
			}
		}

		public int Count => stowed.Count + stowing.Count;
		public bool HasSpace => Count < Info.MaxAircraft;
		public bool HasReadyAircraft => stowed.Any(IsReady);

		// For the pips: true = ready, false = rearming, one entry per aircraft on board.
		public IEnumerable<bool> Readiness => stowed.Select(IsReady);

		public bool Accepts(Actor aircraft)
		{
			return Info.Types.Contains(aircraft.Info.Name);
		}

		bool IsReady(Stowed s) { return ticks >= s.ReadyAt; }

		public void LaunchAll()
		{
			launchesPending = stowed.Count;
		}

		/// <summary>
		/// Déploiement (touche F ou clic sur le navire) : fait décoller les avions prêts
		/// et, si le navire touche la côte, débarque ses troupes.
		/// </summary>
		public void Deploy(bool queued)
		{
			LaunchAll();

			if (CanUnloadTroops)
				self.QueueActivity(queued, new UnloadCargo(self, CarriedTroops.Info.LoadRange));
		}

		void ITick.Tick(Actor self)
		{
			ticks++;
			var onRails = self.Info.HasTraitInfo<RailCarInfo>();

			if (Info.BotLaunchInterval > 0 && self.Owner.IsBot && ticks % Info.BotLaunchInterval == 0)
				LaunchAll();

			// Train aérien : un appareil réarmé et réparé redécolle aussitôt.
			if (Info.AutoLaunch && launchesPending == 0 && stowed.Any(IsReady))
				launchesPending = stowed.Count(IsReady);

			if (Info.PassengerRepairInterval > 0 && ticks % Info.PassengerRepairInterval == 0)
				RepairPassengers();

			if (launchCooldown > 0)
				launchCooldown--;
			else if (launchesPending > 0)
			{
				launchesPending--;
				var next = stowed.FirstOrDefault(IsReady);
				if (next != null)
				{
					Launch(next);
					launchCooldown = Info.LaunchInterval;
				}
				else
					launchesPending = 0;
			}

			// Stow the aircraft that are landing on us.
			foreach (var tp in self.World.ActorsWithTrait<CarrierAircraft>())
			{
				var a = tp.Actor;
				if (a.IsDead || !a.IsInWorld || stowing.Contains(a) || a.Owner != self.Owner || !Accepts(a))
					continue;

				// Either it reserved our deck, or it is waiting to land somewhere and flies over us.
				var aircraft = tp.Trait.Aircraft;
				var chasing = a.CurrentActivity is RejoindrePorteur r && r.Carrier == self;
				if (aircraft.ReservedActor != self && a.CurrentActivity is not ReturnToBase && !chasing)
					continue;

				// Wagon du train aérien : un appareil qui revient s'y poser (munitions épuisées...) le poursuit
				// au lieu de viser l'endroit où le wagon se trouvait.
				if (onRails && aircraft.ReservedActor == self && a.CurrentActivity is ReturnToBase)
				{
					aircraft.UnReserve();
					a.CancelActivity();
					a.QueueActivity(new RejoindrePorteur(a, self));
					continue;
				}

				var delta = a.CenterPosition - self.CenterPosition;
				if (delta.HorizontalLengthSquared > Info.StowRange.LengthSquared)
					continue;

				if (self.World.Map.DistanceAboveTerrain(a.CenterPosition) > Info.StowAltitude)
					continue;

				if (!HasSpace)
				{
					// No room on board: give up the landing and wait for a free slot.
					aircraft.UnReserve();
					a.CancelActivity();
					continue;
				}

				Stow(a, aircraft);
			}
		}

		/// <summary>
		/// Les unités en soute (troupes, véhicules...) sont réparées à bord, au même
		/// rythme que les avions : remises à neuf en RearmDelay ticks.
		/// </summary>
		void RepairPassengers()
		{
			if (CarriedTroops == null || CarriedTroops.IsEmpty())
				return;

			foreach (var passenger in CarriedTroops.Passengers)
			{
				var health = passenger.TraitOrDefault<IHealth>();
				if (health == null || health.IsDead || health.HP >= health.MaxHP)
					continue;

				var step = System.Math.Max(1, (int)((long)health.MaxHP * Info.PassengerRepairInterval / System.Math.Max(1, Info.RearmDelay)));
				health.InflictDamage(passenger, self, new Damage(-System.Math.Min(step, health.MaxHP - health.HP)), true);
			}
		}

		void Stow(Actor a, Aircraft aircraft)
		{
			stowing.Add(a);
			aircraft.UnReserve();
			a.CancelActivity();

			self.World.AddFrameEndTask(w =>
			{
				stowing.Remove(a);
				if (a.IsDead || !a.IsInWorld || self.IsDead || !self.IsInWorld)
					return;

				w.Remove(a);
				stowed.Add(new Stowed { Actor = a, ReadyAt = ticks + Info.RearmDelay, Token = GrantStowedCondition(a) });
			});
		}

		// Avion posé sur le pont (wagon-plateforme du train aérien) : condition par type pour l'afficher.
		int GrantStowedCondition(Actor a)
		{
			return Info.StowedConditions.TryGetValue(a.Info.Name, out var condition)
				? self.GrantCondition(condition) : Actor.InvalidConditionToken;
		}

		void Launch(Stowed s)
		{
			stowed.Remove(s);
			if (s.Token != Actor.InvalidConditionToken)
				self.RevokeCondition(s.Token);

			var a = s.Actor;
			var aircraft = a.Trait<Aircraft>();
			var facing = self.TraitOrDefault<IFacing>();
			var angle = facing != null ? facing.Facing : WAngle.Zero;
			var spawn = self.CenterPosition + new WVec(0, 0, aircraft.Info.CruiseAltitude.Length);
			var ahead = self.CenterPosition + new WVec(0, -Info.LaunchDistance.Length, 0).Rotate(WRot.FromYaw(angle));

			self.World.AddFrameEndTask(w =>
			{
				if (a.IsDead)
					return;

				aircraft.SetPosition(a, spawn);
				aircraft.Facing = angle;
				w.Add(a);

				// Fully rearmed and repaired: only ready aircraft are launched.
				foreach (var pool in a.TraitsImplementing<AmmoPool>())
					pool.GiveAmmo(a, pool.Info.Ammo);

				var health = a.TraitOrDefault<IHealth>();
				if (health != null && health.HP < health.MaxHP)
					health.InflictDamage(a, a, new Damage(health.HP - health.MaxHP), true);

				var cell = w.Map.Clamp(w.Map.CellContaining(ahead));
				a.QueueActivity(aircraft.MoveTo(cell));
			});
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				// Priorité supérieure à l'ordre « Unload » de Cargo (10), qui sinon
				// serait ignoré ou choisi au hasard : Deploy() gère les deux.
				yield return new DeployOrderTargeter("LaunchAircraft", 11,
					() => HasReadyAircraft || CanUnloadTroops ? Info.LaunchCursor : Info.LaunchBlockedCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "LaunchAircraft")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("LaunchAircraft", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return true; }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "LaunchAircraft")
				Deploy(order.Queued);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "LaunchAircraft" && HasReadyAircraft ? Info.Voice : null;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			foreach (var s in stowed)
				s.Actor.Kill(e.Attacker);

			stowed.Clear();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			foreach (var s in stowed)
				s.Actor.Dispose();

			stowed.Clear();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			foreach (var s in stowed)
				s.Actor.ChangeOwner(newOwner);
		}
	}
}
