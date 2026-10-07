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
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Voiture de train (locomotive ou wagon) : ne se trouve que sur les voies du RailNetwork.
	// Remplace Mobile : c'est la locomotive qui déplace tout le train (activité RailMove).
	//  - clic droit sur le terrain (locomotive) : rouler jusqu'à la voie la plus proche du clic ;
	//  - locomotive + clic droit sur un wagon dételé, ou wagon + clic droit sur la locomotive : atteler ;
	//  - locomotive + clic droit sur un de ses wagons, en gare : dételer ce wagon et ceux qui le suivent ;
	//  - touche de déploiement sur la locomotive : décharger tous les wagons.

	[Desc("Train car (locomotive or wagon) moving only on the tracks of the " + nameof(RailNetwork) + ".")]
	public class RailCarInfo : PausableConditionalTraitInfo, IPositionableInfo, IFacingInfo
	{
		[Desc("A locomotive moves the train; wagons only follow it.")]
		public readonly bool Locomotive = false;

		[Desc("Locomotive speed (WDist per tick).")]
		public readonly int Speed = 190;

		[Desc("Number of track cells the car occupies (its length).")]
		public readonly int Cells = 1;

		[Desc("Most wagons a locomotive can pull.")]
		public readonly int MaxWagons = 4;

		public readonly WAngle TurnSpeed = new(64);

		public readonly WAngle InitialFacing = new(768);

		[Desc("Crush classes of the actors run over by the train.")]
		public readonly BitSet<CrushClass> Crushes = new("infantry");

		[Desc("Speed (percent) while sharing track cells with another train (all tracks are double).")]
		public readonly int CrossingSpeedPercent = 50;

		[Desc("Range (in cells) searched for a track around a clicked cell.")]
		public readonly int TrackSearchRange = 6;

		[VoiceReference]
		public readonly string Voice = "Action";

		[CursorReference]
		public readonly string Cursor = "move";

		[CursorReference]
		public readonly string BlockedCursor = "move-blocked";

		[CursorReference]
		public readonly string CoupleCursor = "enter";

		[CursorReference]
		public readonly string CoupleBlockedCursor = "enter-blocked";

		[CursorReference]
		public readonly string UncoupleCursor = "deploy";

		[CursorReference]
		public readonly string UncoupleBlockedCursor = "deploy-blocked";

		public readonly Color TargetLineColor = Color.Green;

		[Desc("Distance from the car centre to its coupler. Zero: half the car length (Cells × 512) minus CouplerInset.")]
		public readonly WDist CouplerOffset = WDist.Zero;

		public readonly WDist CouplerInset = new(42);

		[Desc("Height of the coupling bar drawn between two coupled cars.")]
		public readonly WDist CouplerHeight = new(170);

		public readonly WDist CouplerWidth = new(90);

		public readonly Color CouplerColor = Color.FromArgb(52, 50, 46);

		public WAngle GetInitialFacing() { return InitialFacing; }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return new Dictionary<CPos, SubCell> { { location, SubCell.FullCell } };
		}

		bool IOccupySpaceInfo.SharesCell => false;

		public bool CanEnterCell(World world, Actor self, CPos cell, SubCell subCell = SubCell.FullCell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			var network = world.WorldActor.Trait<RailNetwork>();
			return network.IsTrack(cell) && (check == BlockedByActor.None || network.TrainAt(cell) == null);
		}

		public override object Create(ActorInitializer init) { return new RailCar(init, this); }
	}

	public class RailCar : PausableConditionalTrait<RailCarInfo>, IPositionable, IFacing, ISync,
		INotifyAddedToWorld, INotifyRemovedFromWorld, IIssueOrder, IResolveOrder, IOrderVoice, IIssueDeployOrder, IRender
	{
		public const string MoveOrder = "RailMove";
		public const string CoupleOrder = "RailCouple";
		public const string UncoupleOrder = "RailUncouple";
		public const string UnloadAllOrder = "RailUnloadAll";

		readonly Actor self;
		public readonly RailNetwork Network;
		WAngle desiredFacing;

		CPos[] cells;
		CPos[] fromCells;

		// Case du milieu de la voiture (une voiture de plusieurs cases en occupe Length consécutives).
		[Sync]
		public CPos Cell => cells[(cells.Length - 1) / 2];

		public CPos FromCell => fromCells[(fromCells.Length - 1) / 2];

		public int Length => Math.Max(1, Info.Cells);

		public IEnumerable<CPos> Cells => cells;

		[Sync]
		public WPos CenterPosition { get; private set; }

		[Sync]
		public WAngle Facing { get; set; }

		public Train Train { get; internal set; }

		// Ticks pendant lesquels la voiture retient le train (chargement hors gare...).
		public int Hold;

		public RailCar(ActorInitializer init, RailCarInfo info)
			: base(info)
		{
			self = init.Self;
			Network = self.World.WorldActor.Trait<RailNetwork>();
			var location = init.GetValue<LocationInit, CPos>(CPos.Zero);
			cells = fromCells = Enumerable.Repeat(location, Length).ToArray();
			CenterPosition = self.World.Map.CenterOfCell(location);
			Facing = desiredFacing = init.GetValue<FacingInit, WAngle>(info.InitialFacing);
		}

		public Actor Self => self;

		public CPos TopLeft => Cell;

		public WAngle TurnSpeed => Info.TurnSpeed;

		public WRot Orientation => WRot.FromYaw(Facing);

		public (CPos Cell, SubCell SubCell)[] OccupiedCells() { return cells.Distinct().Select(c => (c, SubCell.FullCell)).ToArray(); }

		public bool InStation => Network.IsStationFor(Cell, self.Owner);

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
			if (Train == null)
			{
				Network.TryChain(cells[0], Length, Facing, out var chain);
				Train = new Train(Network, this, chain);
				Interpolate(1024);
			}
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			Train?.Remove(this);
			Train = null;
			self.World.RemoveFromMaps(self, this);
		}

		internal void Place(List<CPos> newCells, List<CPos> newFrom)
		{
			if (self.IsInWorld)
				self.World.ActorMap.RemoveInfluence(self, this);

			cells = newCells.ToArray();
			fromCells = newFrom.ToArray();

			if (self.IsInWorld)
				self.World.ActorMap.AddInfluence(self, this);
		}

		internal void StartStep()
		{
			if (Length == 1)
				FaceTowards(FromCell, Cell);
		}

		// Position : milieu de la voiture ; une voiture de plusieurs cases suit l'axe entre ses deux bouts.
		internal void Interpolate(int progress)
		{
			var map = self.World.Map;
			progress = Math.Min(progress, 1024);
			var first = WPos.Lerp(map.CenterOfCell(fromCells[0]), map.CenterOfCell(cells[0]), progress, 1024);
			var n = cells.Length - 1;
			var last = WPos.Lerp(map.CenterOfCell(fromCells[n]), map.CenterOfCell(cells[n]), progress, 1024);
			CenterPosition = WPos.Lerp(first, last, 1, 2);
			if (n > 0 && first != last)
			{
				var yaw = (first - last).Yaw;
				var reverse = yaw + new WAngle(512);
				Facing = desiredFacing = AngleDistance(yaw, Facing) <= AngleDistance(reverse, Facing) ? yaw : reverse;
			}

			if (self.IsInWorld)
				self.World.UpdateMaps(self, this);
		}

		// Les voitures sont symétriques : elles prennent l'axe de la voie le plus proche de leur orientation.
		internal void FaceTowards(CPos from, CPos to)
		{
			if (from == to)
				return;

			var yaw = (self.World.Map.CenterOfCell(to) - self.World.Map.CenterOfCell(from)).Yaw;
			var reverse = yaw + new WAngle(512);
			desiredFacing = AngleDistance(yaw, Facing) <= AngleDistance(reverse, Facing) ? yaw : reverse;
		}

		static int AngleDistance(WAngle a, WAngle b)
		{
			var d = (a - b).Angle;
			return Math.Min(d, 1024 - d);
		}

		internal void TickFacing()
		{
			if (Facing != desiredFacing)
				Facing = OpenRA.Mods.Common.Util.TickFacing(Facing, desiredFacing, Info.TurnSpeed);
		}

		internal void Crush(CPos cell)
		{
			var crushables = self.World.ActorMap.GetActorsAt(cell).Where(a => a != self && !a.IsDead)
				.SelectMany(a => a.TraitsImplementing<ICrushable>().Select(t => new TraitPair<ICrushable>(a, t)))
				.ToList();

			foreach (var c in crushables)
				if (c.Trait.CrushableBy(c.Actor, self, Info.Crushes) && c.Actor.IsAtGroundLevel())
					foreach (var n in c.Actor.TraitsImplementing<INotifyCrushed>())
						n.OnCrush(c.Actor, self, Info.Crushes);
		}

		public int CurrentSpeed => Common.Util.ApplyPercentageModifiers(Info.Speed,
			self.TraitsImplementing<ISpeedModifier>().Select(m => m.GetSpeedModifier()));

		// Point d'attelage : bout de la voiture, sur son axe, du côté de l'autre voiture.
		WPos Coupler(WPos towards)
		{
			var offset = Info.CouplerOffset.Length > 0 ? Info.CouplerOffset.Length : Length * 512 - Info.CouplerInset.Length;
			var axis = new WVec(0, -offset, 0).Rotate(WRot.FromYaw(Facing));
			var d = towards - CenterPosition;
			if (axis.X * d.X + axis.Y * d.Y < 0)
				axis = -axis;

			return CenterPosition + axis + new WVec(0, 0, Info.CouplerHeight.Length);
		}

		// Barre d'attelage entre cette voiture et la suivante du train (dessinée par la première des deux).
		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			var next = Train?.Next(this);
			if (next == null || !next.Self.IsInWorld)
				yield break;

			var a = Coupler(next.CenterPosition);
			var b = next.Coupler(CenterPosition);

			// Un peu plus longue que l'écart, pour qu'elle s'accroche aux tampons même en ligne droite.
			var v = b - a;
			var len = v.Length;
			var pad = len > 0 ? v * 60 / len : WVec.Zero;
			yield return new BeamRenderable(a - pad, 0, v + pad * 2, BeamRenderableShape.Flat, Info.CouplerWidth, Info.CouplerColor);
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr) { yield break; }

		#region IPositionable

		public bool CanExistInCell(CPos location) { return Network.IsTrack(location); }

		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any)
		{
			return Train != null && Train.Moving && fromCells.Contains(location) && !cells.Contains(location);
		}

		public bool CanEnterCell(CPos location, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			return Info.CanEnterCell(self.World, self, location, SubCell.FullCell, ignoreActor, check);
		}

		public SubCell GetValidSubCell(SubCell preferred = SubCell.Any) { return SubCell.FullCell; }

		public SubCell GetAvailableSubCell(CPos location, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			return CanEnterCell(location, ignoreActor, check) ? SubCell.FullCell : SubCell.Invalid;
		}

		// Téléportation (scripts) : seulement pour une voiture seule, pas pour un train attelé.
		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			if (Train != null && Train.Cars.Count > 1)
				return;

			Network.TryChain(cell, Length, Facing, out var chain, Train);
			if (Train != null)
				Train.Relocate(chain);
			else
			{
				cells = fromCells = chain.ToArray();
				Interpolate(1024);
			}
		}

		public void SetPosition(Actor self, WPos pos) { SetPosition(self, self.World.Map.CellContaining(pos)); }

		public void SetCenterPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;
			self.World.UpdateMaps(self, this);
		}

		#endregion

		#region Orders

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				if (Info.Locomotive)
				{
					yield return new RailMoveOrderTargeter(this);
					yield return new UncoupleOrderTargeter(this);
				}

				yield return new CoupleOrderTargeter(this);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == MoveOrder || order.OrderID == CoupleOrder || order.OrderID == UncoupleOrder)
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return Info.Locomotive ? new Order(UnloadAllOrder, self, queued) : null;
		}

		// Seulement s'il y a des passagers à décharger (le Schwerer Gustav garde la touche pour sa mise en batterie).
		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued)
		{
			return Info.Locomotive && !IsTraitDisabled && Train != null && Train.Cars.Any(c => c.Self.Info.HasTraitInfo<TrainCargoInfo>());
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			switch (order.OrderString)
			{
				case MoveOrder:
				{
					if (!Info.Locomotive)
						return;

					var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
					var track = Network.NearestTrack(cell, Info.TrackSearchRange);
					if (track == null)
						return;

					self.QueueActivity(order.Queued, new RailMove(self, track.Value));
					self.ShowTargetLines();
					break;
				}

				case CoupleOrder:
				{
					if (order.Target.Type != TargetType.Actor)
						return;

					var other = order.Target.Actor.TraitOrDefault<RailCar>();
					if (other == null)
						return;

					// La locomotive vient chercher la rame, quel que soit celui qui a reçu l'ordre ;
					// deux rames bout à bout s'attellent sur place.
					var plan = CouplePlan(this, other);
					if (plan.Rake == null || plan.Full)
						return;

					if (plan.Loco != null)
					{
						plan.Loco.Self.QueueActivity(order.Queued, new RailMove(plan.Loco.Self, plan.Rake));
						plan.Loco.Self.ShowTargetLines();
					}
					else if (plan.Rake != null && Train.CanJoin(other.Train))
						Train.Join(other.Train);

					break;
				}

				case UncoupleOrder:
				{
					if (!Info.Locomotive || order.Target.Type != TargetType.Actor)
						return;

					var wagon = order.Target.Actor.TraitOrDefault<RailCar>();
					if (wagon == null)
						return;

					self.QueueActivity(order.Queued, new CallFunc(() =>
					{
						if (Train != null && wagon.Train == Train && wagon.InStation)
							Train.Uncouple(wagon);
					}));

					break;
				}

				case UnloadAllOrder:
				{
					if (!Info.Locomotive || Train == null)
						return;

					foreach (var car in Train.Cars)
						foreach (var cargo in car.Self.TraitsImplementing<TrainCargo>())
							cargo.Unload(car.Self, order.Queued);

					break;
				}
			}
		}

		// Qui vient atteler qui : (locomotive, wagon de la rame visée), ou (null, wagon) pour deux rames.
		// Rake == null : attelage impossible entre ces deux voitures.
		internal static (RailCar Loco, RailCar Rake, bool Full) CouplePlan(RailCar a, RailCar b)
		{
			if (a.Train == null || b.Train == null || a.Train == b.Train || a.Self.Owner != b.Self.Owner)
				return (null, null, false);

			var ta = a.Train;
			var tb = b.Train;
			if (!ta.IsRake && !tb.IsRake)
				return (null, null, false);

			if (ta.IsRake && tb.IsRake)
				return (null, b, ta.Cars.Count + tb.Cars.Count > a.Info.MaxWagons || !ta.CanJoin(tb));

			var loco = ta.IsRake ? tb.Locomotive : ta.Locomotive;
			var rake = ta.IsRake ? a : b;
			return (loco, rake, loco.Train.WagonCount + rake.Train.Cars.Count > loco.Info.MaxWagons);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == MoveOrder || order.OrderString == CoupleOrder || order.OrderString == UncoupleOrder)
				return Info.Voice;

			return null;
		}

		sealed class RailMoveOrderTargeter : IOrderTargeter
		{
			readonly RailCar car;

			public RailMoveOrderTargeter(RailCar car) { this.car = car; }

			public string OrderID => MoveOrder;
			public int OrderPriority => 4;
			public bool IsQueued { get; private set; }

			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers)
			{
				if (target.Type == TargetType.Actor && (target.Actor.Owner != self.Owner || self.World.Selection.Contains(target.Actor)))
					return true;

				return modifiers.HasModifier(TargetModifiers.ForceMove);
			}

			public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
			{
				if (target.Type != TargetType.Terrain)
					return false;

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
				var cell = self.World.Map.CellContaining(target.CenterPosition);
				cursor = !car.IsTraitPaused && car.Network.NearestTrack(cell, car.Info.TrackSearchRange) != null ? car.Info.Cursor : car.Info.BlockedCursor;
				return true;
			}
		}

		sealed class CoupleOrderTargeter : UnitOrderTargeter
		{
			readonly RailCar car;

			public CoupleOrderTargeter(RailCar car)
				: base(CoupleOrder, 6, car.Info.CoupleCursor, false, true)
			{
				this.car = car;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var other = target.TraitOrDefault<RailCar>();
				if (other == null)
					return false;

				// Locomotive ↔ rame (un ou plusieurs wagons attelés), ou rame ↔ rame bout à bout.
				var plan = CouplePlan(car, other);
				if (plan.Rake == null)
					return false;

				cursor = plan.Full ? car.Info.CoupleBlockedCursor : car.Info.CoupleCursor;
				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return false;
			}
		}

		sealed class UncoupleOrderTargeter : UnitOrderTargeter
		{
			readonly RailCar car;

			public UncoupleOrderTargeter(RailCar car)
				: base(UncoupleOrder, 6, car.Info.UncoupleCursor, false, true)
			{
				this.car = car;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var other = target.TraitOrDefault<RailCar>();
				if (other == null || other == car || car.Train == null || other.Train != car.Train)
					return false;

				cursor = other.InStation ? car.Info.UncoupleCursor : car.Info.UncoupleBlockedCursor;
				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return false;
			}
		}

		#endregion
	}
}
