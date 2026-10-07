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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Stock d'une gare : les unités produites par une usine dont le point de ralliement est sur la gare
	// y entrent (hors du monde, comme dans un transport). Les trains en navette (Navette) y puisent leur
	// chargement. Capacité séparée pour l'infanterie et pour les véhicules ; gare pleine : les unités
	// attendent devant. Gare détruite, vendue ou capturée : les unités ressortent.
	// Déploiement (ou bouton « Vider la gare ») : tout le stock sort et va au point de ralliement de la gare.
	// Toute unité qui sort d'une gare (stock, navette, train déchargé en gare) suit ce point de ralliement ;
	// posé sur une autre gare (clic droit sur elle, gare sélectionnée), elle va y entrer.

	[Desc("Station storage fed by factories whose rally point is on the station; shuttle trains load from it.")]
	public class GareStockInfo : TraitInfo, Requires<RailTrackInfo>
	{
		[Desc("Passenger cargo types stored as infantry.")]
		public readonly HashSet<string> InfantryTypes = new() { "Infantry" };

		[Desc("Passenger cargo types stored as vehicles.")]
		public readonly HashSet<string> VehicleTypes = new() { "Vehicle", "Char", "SuperLourd" };

		public readonly int InfantryCapacity = 20;

		public readonly int VehicleCapacity = 6;

		[Desc("Units enter the station from this many cells away from its footprint.")]
		public readonly int EnterCells = 2;

		[Desc("Ticks between two units leaving the station when it is emptied.")]
		public readonly int ExitInterval = 6;

		[VoiceReference]
		public readonly string Voice = "Action";

		[CursorReference]
		public readonly string EmptyCursor = "deploy";

		[CursorReference]
		public readonly string EmptyBlockedCursor = "deploy-blocked";

		public readonly Color TextColor = Color.FromArgb(255, 240, 220, 120);

		public override object Create(ActorInitializer init) { return new GareStock(init.Self, this); }
	}

	public class GareStock : ITick, IIssueOrder, IResolveOrder, IIssueDeployOrder, IOrderVoice,
		INotifyKilled, INotifySold, INotifyOwnerChanged, INotifyActorDisposing, IRenderAnnotationsWhenSelected
	{
		public const string EmptyOrder = "ViderGare";

		public readonly GareStockInfo Info;
		readonly Actor self;
		readonly RailNetwork network;
		readonly List<Actor> units = new();
		bool emptying;
		int exitTicks;

		public GareStock(Actor self, GareStockInfo info)
		{
			Info = info;
			this.self = self;
			network = self.World.WorldActor.Trait<RailNetwork>();
		}

		public IReadOnlyList<Actor> Units => units;

		public bool IsEmpty => units.Count == 0;

		public int InfantryCount => units.Count(u => IsInfantry(u));

		public int VehicleCount => units.Count - InfantryCount;

		static string CargoType(Actor a) { return a.Info.TraitInfoOrDefault<PassengerInfo>()?.CargoType; }

		bool IsInfantry(Actor a) { return Info.InfantryTypes.Contains(CargoType(a)); }

		// La gare peut-elle stocker ce genre d'unité (infanterie ou véhicule transportable) ?
		public bool CanStore(Actor a)
		{
			var type = CargoType(a);
			return type != null && a.Info.HasTraitInfo<IPositionableInfo>()
				&& (Info.InfantryTypes.Contains(type) || Info.VehicleTypes.Contains(type));
		}

		public bool HasRoomFor(Actor a)
		{
			if (!CanStore(a))
				return false;

			return IsInfantry(a) ? InfantryCount < Info.InfantryCapacity : VehicleCount < Info.VehicleCapacity;
		}

		public bool Usable => !self.IsDead && self.IsInWorld;

		// L'unité est-elle assez près de la gare (à EnterCells cases de son emprise) pour y entrer ?
		public bool InReach(Actor unit)
		{
			var u = unit.Location;
			return self.OccupiesSpace.OccupiedCells()
				.Any(c => System.Math.Max(System.Math.Abs(c.Cell.X - u.X), System.Math.Abs(c.Cell.Y - u.Y)) <= Info.EnterCells);
		}

		// L'unité entre dans la gare (elle quitte le monde à la fin de l'image).
		public bool Store(Actor unit)
		{
			if (!Usable || unit.IsDead || !unit.IsInWorld || units.Contains(unit) || !HasRoomFor(unit))
				return false;

			units.Add(unit);
			unit.CancelActivity();
			self.World.AddFrameEndTask(w =>
			{
				if (!unit.IsDead && unit.IsInWorld)
					w.Remove(unit);
			});

			return true;
		}

		// Retire du stock la première unité acceptée (elle reste hors du monde : un wagon la prend).
		public Actor Take(Func<Actor, bool> accepts)
		{
			var unit = units.FirstOrDefault(u => !u.IsDead && accepts(u));
			if (unit != null)
				units.Remove(unit);

			return unit;
		}

		// Cases où une unité peut sortir : autour de la gare et sur son quai, hors des voies,
		// les plus proches du point de ralliement d'abord.
		public (CPos Cell, SubCell SubCell)? FindExit(Actor unit)
		{
			var pos = unit.TraitOrDefault<IPositionable>();
			if (pos == null)
				return null;

			var occupied = self.OccupiesSpace.OccupiedCells().Select(c => c.Cell).ToList();
			if (occupied.Count == 0)
				occupied.Add(self.Location);

			var minX = occupied.Min(c => c.X) - 1;
			var maxX = occupied.Max(c => c.X) + 1;
			var minY = occupied.Min(c => c.Y) - 1;
			var maxY = occupied.Max(c => c.Y) + 1;

			var rally = self.TraitOrDefault<RallyPoint>();
			var towards = rally != null && rally.Path.Count > 0 ? rally.Path[0] : self.Location + new CVec(1, 3);

			var map = self.World.Map;
			var cells = new List<CPos>();
			for (var y = minY; y <= maxY; y++)
				for (var x = minX; x <= maxX; x++)
					cells.Add(new CPos(x, y));

			foreach (var c in cells.Where(c => map.Contains(c) && !network.IsTrack(c))
				.OrderBy(c => (c - towards).LengthSquared).ThenBy(c => c.Bits))
			{
				var sub = pos.GetAvailableSubCell(c);
				if (sub != SubCell.Invalid)
					return (c, sub);
			}

			return null;
		}

		// Fait sortir une unité qui est hors du monde (stock ou wagon) à côté de la gare.
		// toRally : elle va ensuite au point de ralliement de la gare.
		public bool Release(Actor unit, bool toRally)
		{
			var exit = FindExit(unit);
			if (exit == null)
				return false;

			Place(unit, exit.Value.Cell, exit.Value.SubCell, toRally);
			return true;
		}

		void Place(Actor unit, CPos cell, SubCell subCell, bool toRally)
		{
			var pos = unit.Trait<IPositionable>();
			self.World.AddFrameEndTask(w =>
			{
				if (unit.IsDead || unit.Disposed)
					return;

				pos.SetPosition(unit, cell, subCell);
				unit.CancelActivity();
				if (!unit.IsInWorld)
					w.Add(unit);

				if (toRally)
					SendToRally(unit);
			});
		}

		// L'unité (déjà dans le monde) suit le point de ralliement de la gare. S'il est posé sur une autre
		// gare du joueur, elle y va par ses propres moyens et entre dans son stock (relais entre gares).
		public void SendToRally(Actor unit)
		{
			var move = unit.TraitOrDefault<IMove>();
			var rally = self.TraitOrDefault<RallyPoint>();
			if (move == null || rally == null || rally.Path.Count == 0 || unit.IsDead || !unit.IsInWorld)
				return;

			var path = rally.Path.ToList();
			var relay = LivraisonGare.GareAt(self.World, path[path.Count - 1], self.Owner);
			if (relay == self || relay?.Trait<GareStock>().CanStore(unit) == false)
				relay = null;

			var moves = relay != null ? path.Take(path.Count - 1) : path;
			foreach (var c in moves)
			{
				var target = c;
				unit.QueueActivity(new AttackMoveActivity(unit, () => move.MoveTo(target, 1, evaluateNearestMovableCell: true, targetLineColor: Color.OrangeRed)));
			}

			if (relay != null)
				unit.QueueActivity(new EntrerGare(unit, relay));
		}

		// Tout sort d'un coup (gare détruite, vendue ou capturée) : les unités se dispersent autour.
		void EjectAll()
		{
			foreach (var unit in units.ToList())
			{
				if (unit.IsDead || unit.Disposed)
					continue;

				var exit = FindExit(unit);
				var cell = exit?.Cell ?? self.Location;
				var sub = exit?.SubCell ?? SubCell.Any;
				Place(unit, cell, sub, false);
			}

			units.Clear();
			emptying = false;
		}

		void ITick.Tick(Actor self)
		{
			units.RemoveAll(u => u.IsDead || u.Disposed);
			if (!emptying)
				return;

			if (units.Count == 0)
			{
				emptying = false;
				return;
			}

			if (--exitTicks > 0)
				return;

			exitTicks = Info.ExitInterval;
			var unit = units[0];
			if (Release(unit, true))
				units.RemoveAt(0);
		}

		public void Empty() { emptying = units.Count > 0; }

		void INotifyKilled.Killed(Actor self, AttackInfo e) { EjectAll(); }

		void INotifySold.Selling(Actor self) { }

		void INotifySold.Sold(Actor self) { EjectAll(); }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { EjectAll(); }

		void INotifyActorDisposing.Disposing(Actor self)
		{
			foreach (var unit in units)
				if (!unit.Disposed)
					unit.Dispose();

			units.Clear();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get { yield return new DeployOrderTargeter(EmptyOrder, 5, () => IsEmpty ? Info.EmptyBlockedCursor : Info.EmptyCursor); }
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == EmptyOrder ? new Order(EmptyOrder, self, queued) : null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued) { return new Order(EmptyOrder, self, queued); }

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !IsEmpty; }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == EmptyOrder)
				Empty();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == EmptyOrder && !IsEmpty ? Info.Voice : null;
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (self.World.RenderPlayer != null && !self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var font = Game.Renderer.Fonts["TinyBold"];
			var text = $"Stock : {InfantryCount}/{Info.InfantryCapacity} fant. · {VehicleCount}/{Info.VehicleCapacity} véh.";
			var pos = self.CenterPosition + new WVec(0, 1536 + 256, 0);
			yield return new TextAnnotationRenderable(font, pos, 0, Info.TextColor, text);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => true;
	}

	// Usine : clic droit sur une gare du joueur = point de ralliement sur la gare. Les unités produites
	// (infanterie, véhicules) iront s'y stocker (voir LivraisonGare).
	[Desc("Lets a factory set its rally point on one of its owner's stations (" + nameof(GareStock) + ").",
		"Only active on actors with a RallyPoint and a Production of one of the listed types.")]
	public class RallyPointGareInfo : TraitInfo
	{
		public readonly HashSet<string> ProductionTypes = new() { "Infantry", "Vehicle" };

		[CursorReference]
		public readonly string Cursor = "ability";

		public override object Create(ActorInitializer init) { return new RallyPointGare(init.Self, this); }
	}

	public class RallyPointGare : IIssueOrder, INotifyCreated
	{
		const string RallyOrder = "SetRallyPoint";

		readonly RallyPointGareInfo info;
		bool active;

		public RallyPointGare(Actor self, RallyPointGareInfo info) { this.info = info; }

		void INotifyCreated.Created(Actor self)
		{
			// Usines d'infanterie et de véhicules, et les gares elles-mêmes (relais vers une autre gare).
			active = self.TraitOrDefault<RallyPoint>() != null
				&& (self.Info.HasTraitInfo<GareStockInfo>()
					|| self.Info.TraitInfos<ProductionInfo>().Any(p => p.Produces.Any(info.ProductionTypes.Contains)));
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (active)
					yield return new GareTargeter(info.Cursor);
			}
		}

		// Le point de ralliement est posé sur la case centrale de la gare : RallyPoint traite l'ordre.
		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID != GareTargeter.ID || target.Type != TargetType.Actor)
				return null;

			var cell = self.World.Map.CellContaining(target.Actor.CenterPosition);
			return new Order(RallyOrder, self, Target.FromCell(self.World, cell), queued) { SuppressVisualFeedback = true };
		}

		sealed class GareTargeter : UnitOrderTargeter
		{
			public const string ID = "RallyPointGare";

			public GareTargeter(string cursor)
				: base(ID, 1, cursor, false, true) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				return target != self && target.Owner == self.Owner && target.Info.HasTraitInfo<GareStockInfo>();
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return false;
			}
		}
	}

	// Unité sélectionnée + clic droit sur une gare du joueur : elle va s'y mettre en garnison (dans le stock).
	[Desc("Lets the unit enter one of its owner's stations (" + nameof(GareStock) + ") with a right click.")]
	public class EntreEnGareInfo : TraitInfo
	{
		[CursorReference]
		public readonly string Cursor = "enter";

		[CursorReference]
		public readonly string FullCursor = "enter-blocked";

		[Desc("Voice played if the unit's voice set has it (not checked: voice sets differ).")]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new EntreEnGare(this); }
	}

	public class EntreEnGare : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public const string OrderID = "EntrerGare";

		readonly EntreEnGareInfo info;

		public EntreEnGare(EntreEnGareInfo info) { this.info = info; }

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get { yield return new GarnisonTargeter(info); }
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == OrderID ? new Order(OrderID, self, target, queued) : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderID || order.Target.Type != TargetType.Actor)
				return;

			var gare = order.Target.Actor;
			var stock = gare.TraitOrDefault<GareStock>();
			if (stock == null || gare.Owner != self.Owner || !stock.CanStore(self) || self.TraitOrDefault<IMove>() == null)
				return;

			self.QueueActivity(order.Queued, new EntrerGare(self, gare));
			self.ShowTargetLines();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == OrderID ? info.Voice : null;
		}

		sealed class GarnisonTargeter : UnitOrderTargeter
		{
			readonly EntreEnGareInfo info;

			public GarnisonTargeter(EntreEnGareInfo info)
				: base(EntreEnGare.OrderID, 5, info.Cursor, false, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (target.Owner != self.Owner || modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				var stock = target.TraitOrDefault<GareStock>();
				if (stock == null || !stock.CanStore(self))
					return false;

				cursor = stock.HasRoomFor(self) ? info.Cursor : info.FullCursor;
				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return false;
			}
		}
	}

	// Unité produite avec le point de ralliement de l'usine sur une gare : après son trajet, elle entre dans la gare.
	[TraitLocation(SystemActors.World)]
	[Desc("Units produced by a factory whose rally point is on a station (" + nameof(GareStock) + ") go and wait inside it.")]
	public class LivraisonGareInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new LivraisonGare(); }
	}

	public class LivraisonGare : INotifyOtherProduction
	{
		public static Actor GareAt(World world, CPos cell, Player owner)
		{
			return world.ActorMap.GetActorsAt(cell)
				.FirstOrDefault(a => !a.IsDead && a.Owner == owner && a.Info.HasTraitInfo<GareStockInfo>());
		}

		void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			if (producer.IsDead || produced.IsDead)
				return;

			var rally = producer.TraitOrDefault<RallyPoint>();
			if (rally == null || rally.Path.Count == 0)
				return;

			var gare = GareAt(self.World, rally.Path[rally.Path.Count - 1], producer.Owner);
			if (gare == null || !gare.Trait<GareStock>().CanStore(produced) || produced.TraitOrDefault<IMove>() == null)
				return;

			// Le trajet vers le point de ralliement est déjà en file : l'entrée dans la gare vient après.
			produced.QueueActivity(new EntrerGare(produced, gare));
		}
	}
}
