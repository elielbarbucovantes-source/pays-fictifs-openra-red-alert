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
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Deploy order: the ship raises a square island out of the water next to it.",
		"The ship must stay still until the island is finished; moving or dying cancels it.",
		"The island's cells become land (Clear inside, Beach on the edge) for good.",
		"Used by the Australouis Mothership.")]
	public class RaisesIslandInfo : ConditionalTraitInfo, Requires<IMoveInfo>
	{
		[Desc("Side of the square island, in cells.")]
		public readonly int Size = 8;

		[Desc("Ticks needed to raise an island.")]
		public readonly int Duration = 4250;

		[Desc("Terrain types the whole island must be made of.")]
		public readonly HashSet<string> WaterTypes = new() { "Water" };

		[Desc("Terrain type of the inner cells once raised.")]
		public readonly string LandType = "Clear";

		[Desc("Terrain type of the edge cells once raised.")]
		public readonly string ShoreType = "Beach";

		[ActorReference]
		[FieldLoader.Require]
		[Desc("Decoration actor drawn over the island (spawned at its center, owned by the neutral player).")]
		public readonly string IslandActor = null;

		[ActorReference]
		[Desc("Actor marking the island while it rises (removed when finished or cancelled).")]
		public readonly string MarkerActor = null;

		[GrantedConditionReference]
		[Desc("Condition granted to the ship while it raises an island.")]
		public readonly string RaisingCondition = null;

		[Desc("Text shown to the owner when the island is finished.")]
		public readonly string FinishedTextNotification = null;

		[Desc("Text shown to the owner when there is no room for an island.")]
		public readonly string NoRoomTextNotification = null;

		[Desc("Text shown to the owner when the ship moved and the island was abandoned.")]
		public readonly string CancelledTextNotification = null;

		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly string DeployCursor = "deploy";
		public readonly string DeployBlockedCursor = "deploy-blocked";

		public readonly Color ProgressColor = Color.LimeGreen;

		public override object Create(ActorInitializer init) { return new RaisesIsland(this); }
	}

	public class RaisesIsland : ConditionalTrait<RaisesIslandInfo>, ITick, IIssueOrder, IResolveOrder, IOrderVoice,
		IIssueDeployOrder, ISelectionBar, INotifyKilled, INotifyRemovedFromWorld
	{
		const string OrderID = "RaiseIsland";

		// Cells reserved by islands being raised, so that two ships do not pick the same spot.
		static readonly Dictionary<World, HashSet<CPos>> Reserved = new();

		int progress = -1;
		CPos startLocation;
		CPos topLeft;
		Actor marker;
		int token = Actor.InvalidConditionToken;

		public RaisesIsland(RaisesIslandInfo info)
			: base(info) { }

		bool Raising => progress >= 0;

		IEnumerable<CPos> Square(CPos tl)
		{
			for (var y = 0; y < Info.Size; y++)
				for (var x = 0; x < Info.Size; x++)
					yield return new CPos(tl.X + x, tl.Y + y);
		}

		static HashSet<CPos> ReservedCells(World world)
		{
			if (!Reserved.TryGetValue(world, out var set))
			{
				Reserved.Clear();
				set = Reserved[world] = new HashSet<CPos>();
			}

			return set;
		}

		bool IsFree(Actor self, CPos tl, bool checkActors)
		{
			var map = self.World.Map;
			var reserved = ReservedCells(self.World);
			foreach (var c in Square(tl))
			{
				if (!map.Contains(c) || !Info.WaterTypes.Contains(map.GetTerrainInfo(c).Type))
					return false;

				if (c == self.Location || (!Raising && reserved.Contains(c)))
					return false;

				if (checkActors && self.World.ActorMap.GetActorsAt(c).Any(a => a != self))
					return false;
			}

			return true;
		}

		// Squares touching the ship, tried from the closest to the farthest.
		CPos? FindSpot(Actor self)
		{
			var loc = self.Location;
			var n = Info.Size;
			var candidates = new List<CPos>();
			for (var dy = -n; dy <= 1; dy++)
				for (var dx = -n; dx <= 1; dx++)
					candidates.Add(new CPos(loc.X + dx, loc.Y + dy));

			foreach (var tl in candidates.OrderBy(tl => (tl + new CVec(n / 2, n / 2) - loc).LengthSquared))
				if (IsFree(self, tl, true))
					return tl;

			return null;
		}

		void ITick.Tick(Actor self)
		{
			if (!Raising)
				return;

			if (IsTraitDisabled || self.Location != startLocation)
			{
				TextNotificationsManager.AddTransientLine(Info.CancelledTextNotification, self.Owner);
				Cancel(self);
				return;
			}

			if (progress < Info.Duration)
			{
				progress++;
				return;
			}

			// Finished: wait until nothing stands in the way.
			if (!IsFree(self, topLeft, true))
				return;

			var world = self.World;
			var land = world.Map.Rules.TerrainInfo.GetTerrainIndex(Info.LandType);
			var shore = world.Map.Rules.TerrainInfo.GetTerrainIndex(Info.ShoreType);
			var last = Info.Size - 1;
			foreach (var c in Square(topLeft))
			{
				var edge = c.X == topLeft.X || c.Y == topLeft.Y || c.X == topLeft.X + last || c.Y == topLeft.Y + last;
				world.Map.CustomTerrain[c] = edge ? shore : land;
			}

			var center = world.Map.CenterOfCell(topLeft) + new WVec(Info.Size * 512 - 512, Info.Size * 512 - 512, 0);
			var centerCell = world.Map.CellContaining(center);
			var neutral = world.Players.First(p => p.InternalName == "Neutral");
			world.AddFrameEndTask(w => w.CreateActor(Info.IslandActor, new TypeDictionary
			{
				new LocationInit(centerCell),
				new CenterPositionInit(center),
				new OwnerInit(neutral),
			}));

			TextNotificationsManager.AddTransientLine(Info.FinishedTextNotification, self.Owner);
			Cancel(self);
		}

		void Start(Actor self)
		{
			var spot = FindSpot(self);
			if (spot == null)
			{
				TextNotificationsManager.AddTransientLine(Info.NoRoomTextNotification, self.Owner);
				return;
			}

			topLeft = spot.Value;
			startLocation = self.Location;
			progress = 0;
			ReservedCells(self.World).UnionWith(Square(topLeft));

			if (!string.IsNullOrEmpty(Info.RaisingCondition) && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(Info.RaisingCondition);

			if (Info.MarkerActor != null)
			{
				var center = self.World.Map.CenterOfCell(topLeft) + new WVec(Info.Size * 512 - 512, Info.Size * 512 - 512, 0);
				var owner = self.Owner;
				self.World.AddFrameEndTask(w => marker = w.CreateActor(Info.MarkerActor, new TypeDictionary
				{
					new LocationInit(w.Map.CellContaining(center)),
					new CenterPositionInit(center),
					new OwnerInit(owner),
				}));
			}
		}

		void Cancel(Actor self)
		{
			if (Raising)
				ReservedCells(self.World).ExceptWith(Square(topLeft));

			progress = -1;
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);

			var m = marker;
			marker = null;
			if (m != null)
				self.World.AddFrameEndTask(w => { if (!m.Disposed) m.Dispose(); });
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new DeployOrderTargeter(OrderID, 5, () => Raising ? Info.DeployBlockedCursor : Info.DeployCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == OrderID ? new Order(OrderID, self, queued) : null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued) { return new Order(OrderID, self, queued); }

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !IsTraitDisabled && !Raising; }

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == OrderID ? Info.Voice : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == OrderID && !Raising && !IsTraitDisabled)
			{
				self.CancelActivity();
				Start(self);
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e) { Cancel(self); }

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self) { Cancel(self); }

		float ISelectionBar.GetValue()
		{
			return Raising ? (float)progress / Info.Duration : 0;
		}

		Color ISelectionBar.GetColor() { return Info.ProgressColor; }

		bool ISelectionBar.DisplayWhenEmpty => false;
	}
}
