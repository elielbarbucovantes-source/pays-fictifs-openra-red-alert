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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Débarquement étendu : les transports qui reçoivent le même ordre de zone pendant la même image
	// forment une opération. Le gestionnaire répartit leurs points d'accostage le long de la zone
	// (un par transport, espacés), puis partage les cases de la zone entre eux (chaque case va au point
	// d'accostage le plus proche) : chaque transport décharge seul, vers sa part de la zone.
	// Tout est calculé dans la simulation (ordres, Tick), dans un ordre fixe : sûr en multijoueur.
	[TraitLocation(SystemActors.World)]
	[Desc("Coordinates extended (zone) landings: spreads the landing points of every transport given the same zone order.")]
	public class ExtendedLandingManagerInfo : TraitInfo
	{
		[Desc("Cells around the zone where transports may stop to unload.")]
		public readonly int LandingMargin = 4;

		[Desc("Largest zone side, in cells.")]
		public readonly int MaxSize = 32;

		[Desc("Side of the square zone used for a single click (concentrated landing).")]
		public readonly int ConcentratedSize = 5;

		[Desc("Minimum distance (cells) between two landing points of the same operation, when the coast allows it.")]
		public readonly int Spacing = 2;

		[Desc("Text shown when no cell of the zone is usable.")]
		public readonly string NoCellsTextNotification = "Débarquement impossible : aucune case accessible aux troupes dans la zone.";

		[Desc("Text shown when a transport finds no place to unload near the zone.")]
		public readonly string NoLandingTextNotification = "Débarquement : un transport ne trouve aucun point d'accostage près de la zone.";

		public override object Create(ActorInitializer init) { return new ExtendedLandingManager(init.World, this); }
	}

	public class ExtendedLandingManager : ITick
	{
		public readonly ExtendedLandingManagerInfo Info;
		readonly World world;
		readonly List<LandingGroup> pending = new();
		readonly List<LandingGroup> solo = new();
		BuildingInfluence influence;

		public ExtendedLandingManager(World world, ExtendedLandingManagerInfo info)
		{
			this.world = world;
			Info = info;
		}

		BuildingInfluence Influence => influence ??= world.WorldActor.TraitOrDefault<BuildingInfluence>();

		// Rectangle normalisé ; un clic (a == b) donne la zone carrée du débarquement concentré.
		public (CPos TopLeft, CPos BottomRight) Zone(CPos a, CPos b)
		{
			if (a == b)
			{
				var r = Info.ConcentratedSize / 2;
				a = new CPos(a.X - r, a.Y - r);
				b = new CPos(b.X + r, b.Y + r);
			}

			var x0 = Math.Min(a.X, b.X);
			var y0 = Math.Min(a.Y, b.Y);
			var x1 = Math.Min(Math.Max(a.X, b.X), x0 + Info.MaxSize - 1);
			var y1 = Math.Min(Math.Max(a.Y, b.Y), y0 + Info.MaxSize - 1);
			var bounds = world.Map.Bounds;
			x0 = Math.Max(x0, bounds.Left);
			y0 = Math.Max(y0, bounds.Top);
			x1 = Math.Min(x1, bounds.Right - 1);
			y1 = Math.Min(y1, bounds.Bottom - 1);
			return (new CPos(x0, y0), new CPos(Math.Max(x0, x1), Math.Max(y0, y1)));
		}

		public static IEnumerable<CPos> Cells(CPos tl, CPos br)
		{
			for (var y = tl.Y; y <= br.Y; y++)
				for (var x = tl.X; x <= br.X; x++)
					yield return new CPos(x, y);
		}

		public static IEnumerable<Locomotor> PassengerLocomotors(IEnumerable<Actor> transports)
		{
			return transports
				.SelectMany(t => t.TraitOrDefault<Cargo>()?.Passengers ?? Enumerable.Empty<Actor>())
				.Select(p => p.TraitOrDefault<Mobile>()?.Locomotor)
				.Where(l => l != null)
				.Distinct();
		}

		public bool Passable(Locomotor locomotor, CPos cell)
		{
			return world.Map.Contains(cell) && locomotor.MovementCostForCell(cell) != PathGraph.MovementCostForUnreachableCell
				&& (Influence == null || !Influence.AnyBuildingAt(cell));
		}

		// Cases de la zone où au moins un des passagers peut se tenir.
		public List<CPos> LandCells(CPos tl, CPos br, IEnumerable<Locomotor> locomotors)
		{
			var locs = locomotors.ToList();
			return Cells(tl, br).Where(c => locs.Any(l => Passable(l, c))).ToList();
		}

		public void Register(Actor transport, CPos a, CPos b, bool queued, Actor operationBase)
		{
			var (tl, br) = Zone(a, b);
			var group = pending.FirstOrDefault(g => g.Owner == transport.Owner && g.TopLeft == tl && g.BottomRight == br);
			if (group == null)
			{
				group = new LandingGroup(transport.Owner, tl, br);
				pending.Add(group);
			}

			if (!group.Members.Any(m => m.Actor == transport))
				group.Members.Add((transport, queued, operationBase));
		}

		void ITick.Tick(Actor self)
		{
			if (pending.Count == 0)
				return;

			foreach (var g in pending)
				Assign(g);

			pending.Clear();
		}

		void Notify(Player player, string text)
		{
			if (!string.IsNullOrEmpty(text))
				TextNotificationsManager.AddTransientLine(text, player);
		}

		void Assign(LandingGroup g)
		{
			var members = g.Members.Where(m => !m.Actor.IsDead && m.Actor.IsInWorld && m.Actor.Owner == g.Owner).ToList();
			if (members.Count == 0)
				return;

			g.Land = LandCells(g.TopLeft, g.BottomRight, PassengerLocomotors(members.Select(m => m.Actor)));
			if (g.Land.Count == 0)
			{
				Notify(g.Owner, Info.NoCellsTextNotification);
				return;
			}

			g.LandSet = g.Land.ToHashSet();

			// Axe de répartition : le grand côté de la zone.
			var horizontal = g.BottomRight.X - g.TopLeft.X >= g.BottomRight.Y - g.TopLeft.Y;
			int Proj(CPos c) => horizontal ? c.X : c.Y;
			int Cross(CPos c) => horizontal ? c.Y : c.X;
			var axisMin = Proj(g.TopLeft);
			var axisLen = Proj(g.BottomRight) - axisMin + 1;

			var ordered = members
				.OrderBy(m => Proj(m.Actor.Location)).ThenBy(m => Cross(m.Actor.Location)).ThenBy(m => m.Actor.ActorID)
				.ToList();

			var n = ordered.Count;
			for (var i = 0; i < n; i++)
			{
				var m = ordered[i];
				var mobile = m.Actor.TraitOrDefault<Mobile>();
				if (mobile == null)
					continue;

				var candidates = g.CandidatesFor(this, m.Actor, mobile.Locomotor);
				var target = axisMin + (2 * i + 1) * axisLen / (2 * n);
				var point = PickPoint(g, candidates, c => Math.Abs(Proj(c) - target) * 4 + DistanceToZone(g, c));
				if (point == null)
				{
					Notify(g.Owner, Info.NoLandingTextNotification);
					continue;
				}

				var plan = new LandingPlan(this, g, m.Actor, point.Value, m.Base);
				g.Plans.Add(plan);
			}

			// Chaque case de la zone revient au point d'accostage le plus proche.
			foreach (var c in g.Land)
			{
				LandingPlan best = null;
				var bestDist = int.MaxValue;
				foreach (var p in g.Plans)
				{
					var d = (c - p.Point).LengthSquared;
					if (d < bestDist)
					{
						best = p;
						bestDist = d;
					}
				}

				best?.Cells.Add(c);
			}

			foreach (var p in g.Plans)
			{
				p.Prepare();
				var m = ordered.First(o => o.Actor == p.Transport);
				p.Transport.QueueActivity(m.Queued, new ExtendedLandingActivity(p.Transport, p));
				p.Transport.ShowTargetLines();
			}
		}

		// Plan d'un transport seul, calculé tout de suite (navette amphibie) : point d'accostage le plus près
		// du centre de la zone, toute la zone pour lui (share : partagée avec les autres navettes en cours). Cases de la zone : celles où au moins un des
		// locomoteurs donnés passe. Null si aucune case ou aucun point ne convient.
		public LandingPlan PlanFor(Actor transport, CPos a, CPos b, IEnumerable<Locomotor> locomotors, bool share = true)
		{
			var mobile = transport.TraitOrDefault<Mobile>();
			if (mobile == null)
				return null;

			var (tl, br) = Zone(a, b);

			// Plusieurs navettes sur la même plage en même temps : elles partagent l'opération (cases prises,
			// espacement des points d'accostage). Une fois tous ses plans terminés, l'opération est oubliée.
			solo.RemoveAll(o => o.Plans.All(p => p.Finished || p.Transport.IsDead || !p.Transport.IsInWorld));
			var g = share ? solo.FirstOrDefault(o => o.Owner == transport.Owner && o.TopLeft == tl && o.BottomRight == br) : null;
			if (g == null)
			{
				g = new LandingGroup(transport.Owner, tl, br);
				g.Land = LandCells(tl, br, locomotors);
				if (g.Land.Count == 0)
					return null;

				g.LandSet = g.Land.ToHashSet();
			}

			var center = new CPos((tl.X + br.X) / 2, (tl.Y + br.Y) / 2);
			var point = PickPoint(g, g.CandidatesFor(this, transport, mobile.Locomotor), c => DistanceToZone(g, c) * 4 + (c - center).Length);
			if (point == null)
				return null;

			var plan = new LandingPlan(this, g, transport, point.Value, null);
			plan.Cells.AddRange(g.Land);
			g.Plans.Add(plan);
			plan.Prepare();
			if (share && !solo.Contains(g))
				solo.Add(g);

			return plan;
		}

		public Locomotor LocomotorByName(string name)
		{
			return world.WorldActor.TraitsImplementing<Locomotor>().FirstOrDefault(l => l.Info.Name == name);
		}

		static int DistanceToZone(LandingGroup g, CPos c)
		{
			var dx = Math.Max(0, Math.Max(g.TopLeft.X - c.X, c.X - g.BottomRight.X));
			var dy = Math.Max(0, Math.Max(g.TopLeft.Y - c.Y, c.Y - g.BottomRight.Y));
			return dx + dy;
		}

		// Point libre le mieux placé, à distance des autres points de l'opération si possible.
		CPos? PickPoint(LandingGroup g, List<CPos> candidates, Func<CPos, int> score, CPos? near = null, HashSet<CPos> exclude = null)
		{
			CPos? best = null;
			var bestScore = int.MaxValue;
			for (var pass = 0; pass < 2 && best == null; pass++)
			{
				var spacing = pass == 0 ? Info.Spacing : 1;
				foreach (var c in candidates)
				{
					if (exclude != null && exclude.Contains(c))
						continue;

					if (g.Plans.Any(p => !p.Finished && Math.Max(Math.Abs(p.Point.X - c.X), Math.Abs(p.Point.Y - c.Y)) < spacing))
						continue;

					var s = score(c) + (near != null ? (c - near.Value).Length : 0);
					if (s < bestScore)
					{
						best = c;
						bestScore = s;
					}
				}
			}

			return best;
		}

		// Le point prévu est inaccessible (bloqué, occupé) : un autre point libre, près du transport.
		public CPos? Replan(LandingPlan plan, Actor transport)
		{
			var mobile = transport.TraitOrDefault<Mobile>();
			if (mobile == null)
				return null;

			var g = plan.Group;
			var candidates = g.CandidatesFor(this, transport, mobile.Locomotor);
			plan.Tried.Add(plan.Point);
			var point = PickPoint(g, candidates, c => DistanceToZone(g, c) * 2, transport.Location, plan.Tried);
			if (point == null)
				return null;

			plan.Point = point.Value;
			return point;
		}

		public class LandingGroup
		{
			public readonly Player Owner;
			public readonly CPos TopLeft, BottomRight;
			public readonly List<(Actor Actor, bool Queued, Actor Base)> Members = new();
			public readonly List<LandingPlan> Plans = new();
			public readonly HashSet<CPos> Claimed = new();
			public List<CPos> Land = new();
			public HashSet<CPos> LandSet = new();
			readonly Dictionary<string, List<CPos>> candidates = new();

			public LandingGroup(Player owner, CPos tl, CPos br)
			{
				Owner = owner;
				TopLeft = tl;
				BottomRight = br;
			}

			// Cases où ce transport peut s'arrêter avec une case de la zone (ou son bord) accessible à côté.
			public List<CPos> CandidatesFor(ExtendedLandingManager manager, Actor transport, Locomotor locomotor)
			{
				if (!candidates.TryGetValue(locomotor.Info.Name, out var list))
				{
					var margin = manager.Info.LandingMargin;
					var tl = new CPos(TopLeft.X - margin, TopLeft.Y - margin);
					var br = new CPos(BottomRight.X + margin, BottomRight.Y + margin);
					list = Cells(tl, br)
						.Where(c => manager.Passable(locomotor, c))
						.Where(c => Util.ExpandFootprint(c, true).Any(n => n != c && LandSet.Contains(n)))
						.ToList();

					candidates[locomotor.Info.Name] = list;
				}

				var pathFinder = manager.world.WorldActor.TraitOrDefault<IPathFinder>();
				if (pathFinder == null)
					return list;

				var from = transport.Location;
				return list.Where(c => pathFinder.PathExistsForLocomotor(locomotor, from, c)).ToList();
			}
		}

		public class LandingPlan
		{
			public readonly ExtendedLandingManager Manager;
			public readonly LandingGroup Group;
			public readonly Actor Transport;
			public readonly Actor Base;
			public readonly List<CPos> Cells = new();
			public readonly HashSet<CPos> Tried = new();
			public CPos Point;
			public bool Finished;
			readonly List<CPos> order = new();
			int next;

			public LandingPlan(ExtendedLandingManager manager, LandingGroup group, Actor transport, CPos point, Actor operationBase)
			{
				Manager = manager;
				Group = group;
				Transport = transport;
				Point = point;
				Base = operationBase;
			}

			// Destinations espacées dans la part de zone du transport, du rivage vers l'intérieur :
			// cases triées par distance au point d'accostage, prises de stride en stride, puis les autres.
			public void Prepare()
			{
				var p = Point;
				Cells.Sort((a, b) =>
				{
					var c = (a - p).LengthSquared.CompareTo((b - p).LengthSquared);
					if (c != 0)
						return c;

					return a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X);
				});

				var passengers = Transport.TraitOrDefault<Cargo>()?.PassengerCount ?? 0;
				var stride = Math.Max(1, Cells.Count / Math.Max(1, passengers));
				order.Clear();
				for (var start = 0; start < stride; start++)
					for (var i = start; i < Cells.Count; i += stride)
						order.Add(Cells[i]);

				next = 0;
			}

			public CPos? NextDestination(Actor passenger)
			{
				var locomotor = passenger.TraitOrDefault<Mobile>()?.Locomotor;
				if (locomotor == null)
					return null;

				bool Usable(CPos c) => !Group.Claimed.Contains(c) && Manager.Passable(locomotor, c);

				for (var k = next; k < order.Count; k++)
				{
					if (!Usable(order[k]))
						continue;

					// Une case sautée (prise par un véhicule voisin, devenue inaccessible) reste disponible plus tard.
					if (k == next)
						next++;

					Group.Claimed.Add(order[k]);
					return order[k];
				}

				// Part épuisée ou devenue inaccessible : n'importe quelle case libre de la zone, la plus proche.
				var p = Point;
				var other = Group.Land.Where(Usable).OrderBy(c => (c - p).LengthSquared).ThenBy(c => c.Y).ThenBy(c => c.X)
					.Select(c => (CPos?)c).FirstOrDefault();
				if (other != null)
				{
					Group.Claimed.Add(other.Value);
					return other;
				}

				// Toute la zone est prise : on partage les cases déjà attribuées (l'infanterie tient à plusieurs).
				return Group.Land.Where(c => Manager.Passable(locomotor, c)).OrderBy(c => (c - p).LengthSquared)
					.Select(c => (CPos?)c).FirstOrDefault();
			}
		}
	}
}
