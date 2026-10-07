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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Lets this aircraft land on an " + nameof(AircraftCarrier) + " when ordered onto it (right-click on the carrier).",
		"Out of ammo, it returns to the carrier on its own if the carrier is listed in Rearmable.RearmActors.")]
	public class CarrierAircraftInfo : TraitInfo, Requires<AircraftInfo>
	{
		[CursorReference]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		public readonly string EnterBlockedCursor = "enter-blocked";

		[VoiceReference]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new CarrierAircraft(init.Self, this); }
	}

	public class CarrierAircraft : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly CarrierAircraftInfo Info;
		public readonly Aircraft Aircraft;
		readonly Actor self;

		public CarrierAircraft(Actor self, CarrierAircraftInfo info)
		{
			Info = info;
			this.self = self;
			Aircraft = self.Trait<Aircraft>();
		}

		static AircraftCarrier CarrierFor(Actor self, Actor target)
		{
			if (target == null || target.IsDead || target.Owner != self.Owner)
				return null;

			var carrier = target.TraitOrDefault<AircraftCarrier>();
			return carrier != null && carrier.Accepts(self) ? carrier : null;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<AircraftCarrierInfo>(
					"EnterCarrier",
					7,
					Info.EnterCursor,
					Info.EnterBlockedCursor,
					(target, modifiers) => CarrierFor(self, target) != null,
					target => CarrierFor(self, target)?.HasSpace ?? false);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "EnterCarrier")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterCarrier" || order.Target.Type != TargetType.Actor)
				return;

			var carrier = CarrierFor(self, order.Target.Actor);
			if (carrier == null)
				return;

			// Porteur mobile sur rails (train aérien) : poursuite du wagon ; sinon approche classique.
			if (order.Target.Actor.Info.HasTraitInfo<RailCarInfo>())
				self.QueueActivity(order.Queued, new RejoindrePorteur(self, order.Target.Actor));
			else
				self.QueueActivity(order.Queued, new ReturnToBase(self, order.Target.Actor, true));
			self.ShowTargetLines();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "EnterCarrier" ? Info.Voice : null;
		}
	}
}
