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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Aircraft du Pelícano. Au repos, il garde sa piste (aucun autre Pelícano ne l'en chasse",
		"et l'embarquement des troupes ne le fait plus redécoller). Si toutes les pistes sont prises,",
		"il se pose sur le terrain dégagé à côté de l'aérodrome le plus proche et y attend.",
		"Remplace Aircraft (IdleBehavior est ignoré).")]
	public class PelicanoAircraftInfo : AircraftInfo
	{
		[Desc("Distance maximale entre l'aérodrome et l'endroit où il se pose quand les pistes sont prises.")]
		public readonly WDist GroundParkingRange = WDist.FromCells(6);

		public override object Create(ActorInitializer init) { return new PelicanoAircraft(init, this); }
	}

	public class PelicanoAircraft : Aircraft
	{
		readonly PelicanoAircraftInfo info;
		readonly Actor self;

		public PelicanoAircraft(ActorInitializer init, PelicanoAircraftInfo info)
			: base(init, info)
		{
			this.info = info;
			self = init.Self;
		}

		protected override void OnBecomingIdle(Actor self)
		{
			if (AtLandAltitude)
			{
				// Posé sur une piste : on (re)prend la réservation, que l'embarquement
				// a annulée, sans permettre à un autre avion de nous en chasser.
				var below = GetActorBelow();
				if (below != null)
				{
					if (ReservedActor != below || MayYieldReservation)
					{
						if (Reservable.IsAvailableFor(below, self))
							MakeReservation(below);
						else
							GoHome();
					}
				}
				else if (!CanLand(self.Location, blockedByMobile: false))
					GoHome();

				// Posé au sol sur un terrain valable : on attend là.
				return;
			}

			GoHome();
		}

		/// <summary>Piste vraiment libre la plus proche, sinon terrain dégagé à côté d'un aérodrome.</summary>
		void GoHome()
		{
			var freePad = self.World.ActorsHavingTrait<Reservable>()
				.Where(a => !a.IsDead && a.Owner == self.Owner && IsRearmActor(a) && IsFree(a))
				.ClosestTo(self);

			if (freePad != null)
			{
				self.QueueActivity(new ReturnToBase(self, freePad, true));
				return;
			}

			var nearestBase = ReturnToBase.ChooseResupplier(self, false);
			if (nearestBase == null)
			{
				self.QueueActivity(new Land(self));
				return;
			}

			var landing = FindGroundParking(nearestBase);
			if (landing.HasValue)
				self.QueueActivity(new Land(self, Target.FromCell(self.World, landing.Value), WDist.FromCells(2)));
			else
				self.QueueActivity(new FlyIdle(self, Info.NumberOfTicksToVerifyAvailableAirport));
		}

		bool IsRearmActor(Actor a)
		{
			var rearmable = self.Info.TraitInfoOrDefault<RearmableInfo>();
			return rearmable != null && rearmable.RearmActors.Contains(a.Info.Name);
		}

		// Ni réservée (même par un avion prêt à céder sa place), ni visée par un autre avion :
		// sinon deux Pelícanos se chasseraient l'un l'autre à tour de rôle.
		bool IsFree(Actor pad)
		{
			return !self.World.ActorsWithTrait<Aircraft>()
				.Any(p => p.Actor != self && !p.Actor.IsDead && p.Trait.ReservedActor == pad);
		}

		// Cases au sol par distance croissante ; chaque Pelícano prend la sienne
		// (selon son numéro) pour ne pas viser tous la même.
		CPos? FindGroundParking(Actor nearestBase)
		{
			var range = (info.GroundParkingRange.Length + 1023) / 1024;
			var cells = self.World.Map.FindTilesInAnnulus(nearestBase.Location, 2, range)
				.Where(c => CanLand(c))
				.Take(8)
				.ToList();

			if (cells.Count == 0)
				return null;

			return cells[(int)(self.ActorID % (uint)cells.Count)];
		}
	}
}
