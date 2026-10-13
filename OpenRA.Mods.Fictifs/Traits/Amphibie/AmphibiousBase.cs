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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Base logistique amphibie : centre des opérations de débarquement.
	//  - Stock : comme une gare (GareStock), les unités y entrent (clic droit sur la base, ou point de
	//    ralliement d'une usine posé sur la base) et y attendent, hors du monde ;
	//  - Quai : jusqu'à MaxDocked transports à quai ; les transports au repos dans son rayon viennent
	//    y prendre une place libre ;
	//  - Chargement : la base remplit les transports à quai avec son stock (selon la place et le type
	//    de cargaison) ; les unités embarquées ainsi sont « préparées » (voir ExtendedLanding) ;
	//  - Coordination : base sélectionnée, le bouton « Débarquement » envoie tous ses transports
	//    chargés en débarquement, puis ils reviennent à quai ;
	//  - Navettes amphibies (NavetteAmphibie) : elles chargent et déchargent à quai par le stock.
	[Desc("Amphibious logistics base: docks up to MaxDocked transports, fills them from its stock and launches landings.")]
	public class AmphibiousBaseInfo : ConditionalTraitInfo, Requires<GareStockInfo>
	{
		[Desc("Berths are water cells within this distance; idle transports within it come to a berth.")]
		public readonly WDist DockRange = WDist.FromCells(9);

		[Desc("Maximum number of transports at the quay.")]
		public readonly int MaxDocked = 10;

		[Desc("Ticks between two units moved between the stock and each docked transport.")]
		public readonly int TransferInterval = 8;

		public readonly Color DockRangeColor = Color.FromArgb(128, Color.Orange);

		public readonly Color BerthColor = Color.FromArgb(160, Color.LimeGreen);

		public override object Create(ActorInitializer init) { return new AmphibiousBase(init.Self, this); }
	}

	public class AmphibiousBase : ConditionalTrait<AmphibiousBaseInfo>, ITick, IRenderAnnotationsWhenSelected, INotifyOwnerChanged
	{
		readonly Actor self;
		readonly Dictionary<Actor, CPos> berths = new();
		GareStock stock;
		int ticks;

		public AmphibiousBase(Actor self, AmphibiousBaseInfo info)
			: base(info)
		{
			this.self = self;
		}

		public GareStock Stock => stock ??= self.Trait<GareStock>();

		public int StockCount => Stock.Units.Count;

		public int DockedCount => berths.Count;

		IEnumerable<CPos> CellsInRange(WDist range)
		{
			var map = self.World.Map;
			var center = map.CellContaining(self.CenterPosition);
			var r = (range.Length + 1023) / 1024;
			return map.FindTilesInCircle(center, r).Where(c => (map.CenterOfCell(c) - self.CenterPosition).HorizontalLengthSquared <= range.LengthSquared);
		}

		static bool Shuttling(Actor t)
		{
			return t.TraitOrDefault<NavetteAmphibie>()?.Active == true;
		}

		bool InRange(Actor a)
		{
			return (a.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= Info.DockRange.LengthSquared;
		}

		// Transports du joueur rattachés à la base : au repos ou à quai dans son rayon (hors navettes).
		public IEnumerable<Actor> Transports()
		{
			return self.World.FindActorsInCircle(self.CenterPosition, Info.DockRange)
				.Where(a => a.Owner == self.Owner && !a.IsDead && a.IsInWorld && a.Info.HasTraitInfo<ExtendedLandingInfo>() && !Shuttling(a))
				.OrderBy(a => a.ActorID);
		}

		public IEnumerable<Actor> LoadedTransports()
		{
			return Transports().Where(a => a.Trait<ExtendedLanding>().CanLand(a));
		}

		public static IEnumerable<Actor> LoadedTransports(IEnumerable<Actor> bases)
		{
			return bases.SelectMany(b => b.TraitOrDefault<AmphibiousBase>()?.LoadedTransports() ?? Enumerable.Empty<Actor>()).Distinct();
		}

		// Place à quai de ce transport (réservée) : la case d'eau libre la plus proche de la base, espacée
		// des autres places si possible. Quai complet (MaxDocked places prises) : null.
		public CPos? DockCellFor(Actor baseActor, Actor transport)
		{
			if (IsTraitDisabled || transport.Owner != self.Owner)
				return null;

			Cleanup();
			if (berths.TryGetValue(transport, out var mine))
				return mine;

			if (berths.Count >= Info.MaxDocked)
				return null;

			var mobile = transport.TraitOrDefault<Mobile>();
			if (mobile == null)
				return null;

			var manager = self.World.WorldActor.Trait<ExtendedLandingManager>();
			var pathFinder = self.World.WorldActor.TraitOrDefault<IPathFinder>();
			var reserved = berths.Values.ToHashSet();
			var candidates = CellsInRange(Info.DockRange)
				.Where(c => manager.Passable(mobile.Locomotor, c) && !reserved.Contains(c))
				.Where(c => !self.World.ActorMap.GetActorsAt(c).Any(a => a != transport && a.Info.HasTraitInfo<MobileInfo>()))
				.OrderBy(c => (self.World.Map.CenterOfCell(c) - self.CenterPosition).HorizontalLengthSquared)
				.ThenBy(c => c.Y).ThenBy(c => c.X)
				.ToList();

			for (var spacing = 2; spacing >= 1; spacing--)
			{
				foreach (var c in candidates)
				{
					if (reserved.Any(r => Math.Max(Math.Abs(r.X - c.X), Math.Abs(r.Y - c.Y)) < spacing))
						continue;

					if (pathFinder != null && !pathFinder.PathExistsForLocomotor(mobile.Locomotor, transport.Location, c))
						continue;

					berths[transport] = c;
					return c;
				}
			}

			return null;
		}

		public void ReleaseBerth(Actor transport) { berths.Remove(transport); }

		public bool AtBerth(Actor transport)
		{
			return berths.TryGetValue(transport, out var c) && transport.Location == c;
		}

		void Cleanup()
		{
			foreach (var t in berths.Keys.Where(t => t.IsDead || !t.IsInWorld || t.Owner != self.Owner).ToList())
				berths.Remove(t);
		}

		// Un passager du stock embarque dans le transport à quai. Faux s'il n'y a personne qui convienne.
		public bool LoadOne(Actor transport)
		{
			if (IsTraitDisabled || !AtBerth(transport))
				return false;

			var cargo = transport.Trait<Cargo>();
			var unit = Stock.Take(u => Navette.Accepts(cargo, u));
			if (unit == null)
				return false;

			cargo.Load(transport, unit);
			transport.TraitOrDefault<ExtendedLanding>()?.MarkPrepared(unit);
			return true;
		}

		// Un passager du transport à quai entre dans le stock ; stock plein pour lui : il descend
		// sur la rive et attend devant la base qu'une place se libère. Faux s'il ne se passe rien.
		public bool UnloadOne(Actor transport)
		{
			var cargo = transport.Trait<Cargo>();
			if (IsTraitDisabled || !AtBerth(transport) || cargo.IsEmpty())
				return false;

			var passenger = cargo.Passengers.FirstOrDefault(p => Stock.HasRoomFor(p)) ?? cargo.Peek();
			if (Stock.HasRoomFor(passenger))
			{
				cargo.Unload(transport, passenger);
				Stock.Receive(passenger);
				return true;
			}

			var exit = Stock.FindExit(passenger);
			if (exit == null)
				return false;

			cargo.Unload(transport, passenger);
			var pos = passenger.Trait<IPositionable>();
			var spawn = transport.CenterPosition;
			var baseActor = self;
			self.World.AddFrameEndTask(w =>
			{
				if (passenger.Disposed)
					return;

				pos.SetPosition(passenger, exit.Value.Cell, exit.Value.SubCell);
				pos.SetCenterPosition(passenger, spawn);
				passenger.CancelActivity();
				w.Add(passenger);
				if (!baseActor.IsDead && baseActor.Owner == passenger.Owner && passenger.TraitOrDefault<IMove>() != null)
					passenger.QueueActivity(new EntrerGare(passenger, baseActor));
			});

			return true;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !self.IsInWorld || --ticks > 0)
				return;

			ticks = Info.TransferInterval;
			Cleanup();

			// Transport laissé ailleurs par le joueur : sa place se libère.
			foreach (var t in berths.Keys.Where(t => t.IsIdle && !InRange(t)).ToList())
				berths.Remove(t);

			foreach (var t in Transports())
			{
				if (!t.IsIdle)
					continue;

				if (AtBerth(t))
				{
					LoadOne(t);
					continue;
				}

				// Transport au repos dans le rayon : il vient prendre sa place à quai (s'il en reste).
				var dock = DockCellFor(self, t);
				if (dock != null && dock.Value != t.Location)
					t.QueueActivity(t.Trait<Mobile>().MoveTo(dock.Value, 0, null, true, Color.Orange));
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			berths.Clear();
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled || self.Owner != self.World.LocalPlayer)
				yield break;

			yield return new RangeCircleAnnotationRenderable(self.CenterPosition, Info.DockRange, 0, Info.DockRangeColor, 1, Color.FromArgb(96, Color.Black), 3);
			foreach (var c in berths.Values)
				yield return new RangeCircleAnnotationRenderable(self.World.Map.CenterOfCell(c), new WDist(384), 0, Info.BerthColor, 1, Color.FromArgb(96, Color.Black), 3);

			var font = Game.Renderer.Fonts["TinyBold"];
			var loaded = berths.Keys.Count(t => !t.Trait<Cargo>().IsEmpty());
			var text = $"À quai : {berths.Count}/{Info.MaxDocked} — chargés : {loaded}";
			yield return new TextAnnotationRenderable(font, self.CenterPosition + new WVec(0, -1536, 0), 0, Color.White, text);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;
	}
}
