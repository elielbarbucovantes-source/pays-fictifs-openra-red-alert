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
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Chargement / déchargement d'un wagon (Cargo) : rapide en gare, 4 fois plus lent sur la voie.
	// Pendant l'opération, le train ne peut pas repartir. Hors gare, les unités débarquent en désordre.
	// Le Cargo du wagon doit avoir UnloadTerrainTypes sur un terrain inexistant : son propre ordre
	// « Unload » est alors sans effet et c'est celui-ci qui décharge.

	[Desc("Train wagon loading/unloading speed: fast in a station, slow anywhere else on the tracks.")]
	public class TrainCargoInfo : TraitInfo, Requires<CargoInfo>, Requires<RailCarInfo>
	{
		[Desc("Ticks between two passengers unloading in a station.")]
		public readonly int StationUnloadDelay = 12;

		[Desc("Ticks between two passengers unloading outside a station.")]
		public readonly int TrackUnloadDelay = 48;

		[Desc("Ticks the train is held after each passenger boarding in a station.")]
		public readonly int StationLoadHold = 12;

		[Desc("Ticks the train is held after each passenger boarding outside a station.")]
		public readonly int TrackLoadHold = 48;

		[Desc("Outside a station, unloaded passengers scatter up to this many cells away.")]
		public readonly int ScatterRange = 2;

		[VoiceReference]
		public readonly string Voice = "Action";

		[CursorReference]
		public readonly string UnloadCursor = "deploy";

		[CursorReference]
		public readonly string UnloadBlockedCursor = "deploy-blocked";

		public override object Create(ActorInitializer init) { return new TrainCargo(init.Self, this); }
	}

	public class TrainCargo : IIssueOrder, IResolveOrder, IIssueDeployOrder, IOrderVoice, INotifyPassengerEntered
	{
		public const string OrderID = "TrainUnload";

		public readonly TrainCargoInfo Info;
		public readonly Cargo Cargo;
		public readonly RailCar Car;

		public TrainCargo(Actor self, TrainCargoInfo info)
		{
			Info = info;
			Cargo = self.Trait<Cargo>();
			Car = self.Trait<RailCar>();
		}

		public void Unload(Actor self, bool queued)
		{
			if (!Cargo.IsEmpty())
				self.QueueActivity(queued, new TrainUnload(self));
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new DeployOrderTargeter(OrderID, 11, () => Cargo.IsEmpty() ? Info.UnloadBlockedCursor : Info.UnloadCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == OrderID ? new Order(OrderID, self, queued) : null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued) { return new Order(OrderID, self, queued); }

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return true; }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == OrderID)
				Unload(self, order.Queued);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == OrderID && !Cargo.IsEmpty() ? Info.Voice : null;
		}

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			Car.Hold += Car.InStation ? Info.StationLoadHold : Info.TrackLoadHold;
		}

		internal CPos ScatterCell(Actor self, CPos exit)
		{
			var r = Info.ScatterRange;
			var random = self.World.SharedRandom;
			var cell = exit + new CVec(random.Next(-r, r + 1), random.Next(-r, r + 1));
			return self.World.Map.Contains(cell) ? cell : exit;
		}
	}
}
