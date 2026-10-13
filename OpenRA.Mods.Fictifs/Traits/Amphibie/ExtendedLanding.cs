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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Transport compatible avec le débarquement étendu (bouton « Débarquement », touche K) :
	// un clic = débarquement concentré autour du point, un rectangle glissé = débarquement réparti sur la zone.
	// Voir ExtendedLandingManager (répartition) et ExtendedLandingActivity (accostage et déchargement).
	[Desc("Naval transport that can unload over a whole zone (extended landing) or around a point (concentrated landing).")]
	public class ExtendedLandingInfo : ConditionalTraitInfo, Requires<CargoInfo>, Requires<MobileInfo>
	{
		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly Color TargetLineColor = Color.Orange;

		[Desc("Condition (ExternalCondition on the passengers) granted after landing to units loaded at an amphibious base.")]
		public readonly string AssaultCondition = "assaut.amphibie";

		[Desc("Duration of AssaultCondition, in ticks.")]
		public readonly int AssaultDuration = 500;

		[Desc("Ticks between two passengers leaving the transport.")]
		public readonly int UnloadInterval = 3;

		public override object Create(ActorInitializer init) { return new ExtendedLanding(this); }
	}

	public class ExtendedLanding : ConditionalTrait<ExtendedLandingInfo>, IResolveOrder, IOrderVoice, INotifyPassengerExited
	{
		public const string OrderID = "DebarquementZone";

		// Passagers embarqués par une base logistique amphibie (préparés à l'assaut).
		readonly HashSet<Actor> prepared = new();

		public ExtendedLanding(ExtendedLandingInfo info)
			: base(info) { }

		public void MarkPrepared(Actor passenger) { prepared.Add(passenger); }

		public bool IsPrepared(Actor passenger) { return prepared.Contains(passenger); }

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger) { prepared.Remove(passenger); }

		public bool CanLand(Actor self)
		{
			return !IsTraitDisabled && !self.IsDead && self.IsInWorld && !self.Trait<Cargo>().IsEmpty();
		}

		public static Order MakeOrder(World world, Actor transport, CPos start, CPos end, bool queued, Actor operationBase)
		{
			return new Order(OrderID, transport, Target.FromCell(world, end), queued)
			{
				ExtraLocation = start,
				ExtraData = operationBase != null ? operationBase.ActorID : 0
			};
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderID || IsTraitDisabled)
				return;

			var end = self.World.Map.CellContaining(order.Target.CenterPosition);
			Actor operationBase = null;
			if (order.ExtraData != 0)
			{
				operationBase = self.World.GetActorById(order.ExtraData);
				if (operationBase != null && (operationBase.IsDead || operationBase.Owner != self.Owner))
					operationBase = null;
			}

			if (!order.Queued)
				self.CancelActivity();

			self.World.WorldActor.Trait<ExtendedLandingManager>().Register(self, order.ExtraLocation, end, order.Queued, operationBase);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == OrderID ? Info.Voice : null;
		}
	}
}
