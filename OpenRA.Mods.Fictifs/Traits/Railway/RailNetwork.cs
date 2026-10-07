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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Réseau ferroviaire : les rails, les voies des gares et des chantiers forment un ensemble de cases
	// reliées par les côtés (comme les pipelines). Les trains (RailCar) ne roulent que sur ces cases et
	// ne se bloquent pas : toutes les voies sont doubles, plusieurs trains peuvent partager une case
	// (ils ralentissent quand ils se croisent, voir RailMove).
	// Le bonus logistique (chantier et gare du même joueur sur un même réseau) est recalculé ici.

	[TraitLocation(SystemActors.World)]
	[Desc("Railway network: track cells, train occupancy, train movement and the logistics bonus.")]
	public class RailNetworkInfo : TraitInfo
	{
		[Desc("Ticks between two logistics bonus updates.")]
		public readonly int BonusInterval = 25;

		public override object Create(ActorInitializer init) { return new RailNetwork(this); }
	}

	public class RailNetwork : ITick
	{
		static readonly CVec[] Neighbours = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };

		readonly RailNetworkInfo info;
		readonly Dictionary<CPos, RailTrack> tracks = new();
		readonly Dictionary<CPos, List<Train>> occupancy = new();
		readonly List<Train> trains = new();
		int bonusTicks;

		public RailNetwork(RailNetworkInfo info) { this.info = info; }

		public bool IsTrack(CPos cell) { return tracks.ContainsKey(cell); }

		public RailTrack TrackAt(CPos cell) { return tracks.TryGetValue(cell, out var t) ? t : null; }

		// Premier train sur la case (null si aucun).
		public Train TrainAt(CPos cell) { return occupancy.TryGetValue(cell, out var t) ? t[0] : null; }

		static readonly Train[] NoTrains = System.Array.Empty<Train>();

		// Tous les trains sur la case (voie double : un train peut en croiser un autre).
		public IReadOnlyList<Train> TrainsAt(CPos cell) { return occupancy.TryGetValue(cell, out var t) ? t : NoTrains; }

		// Le train partage-t-il la case avec un autre ?
		public bool Shared(CPos cell, Train train) { return TrainsAt(cell).Any(t => t != train); }

		// Voie d'une gare (ou d'un chantier) du joueur ou d'un allié : chargement rapide, dételage permis.
		public bool IsStationFor(CPos cell, Player player)
		{
			return tracks.TryGetValue(cell, out var t) && (t.Info.Station || t.Info.Depot)
				&& !t.Self.IsDead && t.Self.Owner.IsAlliedWith(player);
		}

		internal void AddTrack(CPos cell, RailTrack track) { tracks[cell] = track; }

		internal void RemoveTrack(CPos cell, RailTrack track)
		{
			if (tracks.TryGetValue(cell, out var t) && t == track)
				tracks.Remove(cell);
		}

		internal void AddTrain(Train train) { trains.Add(train); }

		internal void RemoveTrain(Train train)
		{
			trains.Remove(train);
			SetOccupancy(train, Enumerable.Empty<CPos>());
		}

		internal void SetOccupancy(Train train, IEnumerable<CPos> cells)
		{
			foreach (var c in train.Occupied)
			{
				if (occupancy.TryGetValue(c, out var list) && list.Remove(train) && list.Count == 0)
					occupancy.Remove(c);
			}

			train.Occupied.Clear();
			foreach (var c in cells)
			{
				if (!train.Occupied.Add(c))
					continue;

				if (!occupancy.TryGetValue(c, out var list))
					occupancy[c] = list = new List<Train>();

				list.Add(train);
			}
		}

		// Case de voie la plus proche (en anneaux) : clic à côté des rails.
		public CPos? NearestTrack(CPos cell, int range)
		{
			if (tracks.ContainsKey(cell))
				return cell;

			for (var r = 1; r <= range; r++)
			{
				CPos? best = null;
				var bestDist = int.MaxValue;
				for (var dy = -r; dy <= r; dy++)
				{
					for (var dx = -r; dx <= r; dx++)
					{
						if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
							continue;

						var c = cell + new CVec(dx, dy);
						var d = dx * dx + dy * dy;
						if (d < bestDist && tracks.ContainsKey(c))
						{
							best = c;
							bestDist = d;
						}
					}
				}

				if (best != null)
					return best;
			}

			return null;
		}

		// Cases d'une voiture de n cases posée en start : start puis les cases de voie libres derrière
		// elle (à l'opposé de facing, en ligne droite si possible). false s'il n'y a pas la place.
		public bool TryChain(CPos start, int n, WAngle facing, out List<CPos> cells, Train ignore = null)
		{
			cells = new List<CPos> { start };
			var v = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(facing));
			var back = Math.Abs(v.X) > Math.Abs(v.Y) ? new CVec(-Math.Sign(v.X), 0) : new CVec(0, -Math.Sign(v.Y));
			var ok = true;
			while (cells.Count < n)
			{
				var last = cells[cells.Count - 1];
				var dir = cells.Count > 1 ? last - cells[cells.Count - 2] : back;
				CPos? found = null;
				foreach (var d in new[] { dir }.Concat(Neighbours))
				{
					var c = last + d;
					var t = TrainAt(c);
					if (IsTrack(c) && !cells.Contains(c) && (t == null || t == ignore))
					{
						found = c;
						break;
					}
				}

				if (found == null)
				{
					ok = false;
					found = last;
				}

				cells.Add(found.Value);
			}

			return ok;
		}

		// Plus court chemin sur les voies (4 voisins), sans la case de départ, jusqu'à la case but.
		// La case but est atteignable même si blocked() la refuse (wagon à atteler).
		public List<CPos> FindPath(CPos start, CPos goal, Func<CPos, bool> blocked)
		{
			if (start == goal)
				return new List<CPos>();

			var parent = new Dictionary<CPos, CPos> { { start, start } };
			var open = new Queue<CPos>();
			open.Enqueue(start);
			while (open.Count > 0)
			{
				var c = open.Dequeue();
				foreach (var d in Neighbours)
				{
					var n = c + d;
					if (parent.ContainsKey(n) || !tracks.ContainsKey(n))
						continue;

					if (n != goal && blocked(n))
						continue;

					parent[n] = c;
					if (n == goal)
					{
						var path = new List<CPos>();
						for (var p = n; p != start; p = parent[p])
							path.Add(p);

						path.Reverse();
						return path;
					}

					open.Enqueue(n);
				}
			}

			return null;
		}

		void ITick.Tick(Actor self)
		{
			foreach (var t in trains.ToArray())
				t.Tick();

			if (--bonusTicks <= 0)
			{
				bonusTicks = info.BonusInterval;
				UpdateBonus();
			}
		}

		// Composantes connexes des voies de chaque joueur : chantier + gare dans la même = réseau relié.
		void UpdateBonus()
		{
			var connected = new HashSet<RailTrack>();
			var seen = new HashSet<CPos>();
			foreach (var start in tracks.Keys.OrderBy(c => c.Bits))
			{
				if (seen.Contains(start))
					continue;

				var owner = tracks[start].Self.Owner;
				var component = new List<RailTrack>();
				var open = new Queue<CPos>();
				open.Enqueue(start);
				seen.Add(start);
				while (open.Count > 0)
				{
					var c = open.Dequeue();
					component.Add(tracks[c]);
					foreach (var d in Neighbours)
					{
						var n = c + d;
						if (!seen.Contains(n) && tracks.TryGetValue(n, out var t) && t.Self.Owner == owner)
						{
							seen.Add(n);
							open.Enqueue(n);
						}
					}
				}

				if (component.Any(t => t.Info.Depot) && component.Any(t => t.Info.Station))
					foreach (var t in component)
						connected.Add(t);
			}

			foreach (var t in tracks.Values.Distinct())
				t.SetConnected(connected.Contains(t));
		}
	}

	[Desc("Track cells of a rail segment, a station or a train depot (see " + nameof(RailNetwork) + ").")]
	public class RailTrackInfo : TraitInfo
	{
		[Desc("Track cells, relative to the top-left cell of the actor.")]
		public readonly CVec[] Cells = { CVec.Zero };

		[Desc("Station track: fast loading and unloading, wagons can be uncoupled.")]
		public readonly bool Station = false;

		[Desc("Train depot track: wagons can be uncoupled. A depot and a station on the same network give the logistics bonus.")]
		public readonly bool Depot = false;

		[GrantedConditionReference]
		[Desc("Condition granted while this actor is on a network of its owner holding both a depot and a station.")]
		public readonly string ConnectedCondition = null;

		[Desc("Wall type of the rail segments (WithWallSpriteBody.Type) that connect to the track cells.")]
		public readonly string WallType = "rail";

		public override object Create(ActorInitializer init) { return new RailTrack(init.Self, this); }
	}

	public class RailTrack : INotifyAddedToWorld, INotifyRemovedFromWorld, IWallConnector
	{
		public readonly RailTrackInfo Info;
		public readonly Actor Self;
		readonly RailNetwork network;
		int token = Actor.InvalidConditionToken;

		public RailTrack(Actor self, RailTrackInfo info)
		{
			Info = info;
			Self = self;
			network = self.World.WorldActor.Trait<RailNetwork>();
		}

		public IEnumerable<CPos> Cells => Info.Cells.Select(c => Self.Location + c);

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			foreach (var c in Cells)
				network.AddTrack(c, this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			foreach (var c in Cells)
				network.RemoveTrack(c, this);

			SetConnected(false);
		}

		internal void SetConnected(bool connected)
		{
			if (string.IsNullOrEmpty(Info.ConnectedCondition))
				return;

			if (connected && token == Actor.InvalidConditionToken && !Self.IsDead)
				token = Self.GrantCondition(Info.ConnectedCondition);
			else if (!connected && token != Actor.InvalidConditionToken)
				token = Self.RevokeCondition(token);
		}

		// Les rails dessinent une liaison vers toute case de voie voisine : autre segment, voie d'une gare ou d'un chantier.
		// (WithWallSpriteBody ne consulte que le premier IWallConnector d'un acteur : sur un segment, c'est celui-ci.)
		bool IWallConnector.AdjacentWallCanConnect(Actor self, CPos wallLocation, string wallType, out CVec facing)
		{
			facing = CVec.Zero;
			if (wallType != Info.WallType)
				return false;

			foreach (var cell in Cells)
			{
				var delta = wallLocation - cell;
				if (Math.Abs(delta.X) + Math.Abs(delta.Y) == 1)
				{
					facing = delta;
					return true;
				}
			}

			return false;
		}

		void IWallConnector.SetDirty() { }
	}

	// Un train : locomotive et wagons attelés, dans l'ordre physique (de l'avant vers l'arrière).
	// Il occupe une chaîne de cases de voie (Chain, de l'avant vers l'arrière) ; chaque voiture en prend
	// RailCar.Info.Cells consécutives. Une rame sans locomotive (un wagon seul en est une) ne bouge pas.
	// Le train avance case par case : à chaque pas, la chaîne glisse d'une case, la tête (ou la queue
	// quand il roule dans l'autre sens) entre dans une nouvelle case et toutes les voitures suivent.
	public class Train
	{
		public readonly List<RailCar> Cars = new();
		internal readonly HashSet<CPos> Occupied = new();
		readonly RailNetwork network;
		List<CPos> chain = new();
		List<CPos> from = new();
		int progress;
		int speed;

		public Train(RailNetwork network, RailCar car, IEnumerable<CPos> cells)
			: this(network, new List<RailCar> { car }, cells.ToList()) { }

		Train(RailNetwork network, List<RailCar> cars, List<CPos> cells)
		{
			this.network = network;
			Cars.AddRange(cars);
			chain = cells;
			from = cells.ToList();
			foreach (var c in cars)
				c.Train = this;

			network.AddTrain(this);
			Assign();
		}

		public bool Moving { get; private set; }

		public RailCar Locomotive => Cars.FirstOrDefault(c => c.Info.Locomotive);

		public int WagonCount => Cars.Count(c => !c.Info.Locomotive);

		public IReadOnlyList<CPos> Chain => chain;

		public CPos Front => chain[0];

		public CPos Back => chain[chain.Count - 1];

		// Chargement ou déchargement en cours sur une voiture : le train ne repart pas.
		public bool Held => Cars.Any(c => c.Hold > 0 || c.IsTraitPaused);

		public bool Contains(CPos cell) { return chain.Contains(cell); }

		// Voiture qui occupe cette case (null si aucune).
		public RailCar CarAt(CPos cell)
		{
			var k = chain.IndexOf(cell);
			if (k < 0)
				return null;

			foreach (var c in Cars)
			{
				if (k < c.Length)
					return c;

				k -= c.Length;
			}

			return null;
		}

		// Voiture seule déplacée par un script.
		internal void Relocate(List<CPos> cells)
		{
			chain = cells;
			from = cells.ToList();
			Assign();
			foreach (var car in Cars)
				car.Interpolate(1024);
		}

		// Répartit la chaîne entre les voitures et met à jour l'occupation des cases.
		void Assign()
		{
			var o = 0;
			foreach (var c in Cars)
			{
				c.Place(chain.GetRange(o, c.Length), from.GetRange(o, c.Length));
				o += c.Length;
			}

			network.SetOccupancy(this, chain.Concat(from).Distinct().ToList());
		}

		// Un pas : la tête (atFront) ou la queue entre dans next, toute la chaîne suit.
		public void BeginStep(CPos next, bool atFront, int stepSpeed)
		{
			from = chain.ToList();
			if (atFront)
			{
				chain.Insert(0, next);
				chain.RemoveAt(chain.Count - 1);
			}
			else
			{
				chain.RemoveAt(0);
				chain.Add(next);
			}

			speed = Math.Max(1, stepSpeed);
			progress = 0;
			Moving = true;
			Assign();

			foreach (var car in Cars)
				car.StartStep();

			// Le train écrase l'infanterie sur la case où il entre.
			(atFront ? Cars[0] : Cars[Cars.Count - 1]).Crush(next);
		}

		internal void Tick()
		{
			foreach (var car in Cars)
			{
				if (car.Hold > 0)
					car.Hold--;

				car.TickFacing();
			}

			if (!Moving)
				return;

			progress += speed;
			if (progress >= 1024)
			{
				FinishStep();
				return;
			}

			foreach (var car in Cars)
				car.Interpolate(progress);
		}

		void FinishStep()
		{
			Moving = false;
			from = chain.ToList();
			Assign();
			foreach (var car in Cars)
				car.Interpolate(1024);
		}

		static bool Adjacent(CPos a, CPos b) { return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1; }

		// Rame : wagons attelés entre eux, sans locomotive (un wagon seul en est une). Elle ne bouge pas seule.
		public bool IsRake => Locomotive == null;

		// Atteler une rame par son bout qui est sur la case at, devant (atFront) ou derrière le train.
		public bool CanCouple(Train rake, CPos at)
		{
			return rake != this && rake.IsRake && !Moving && !rake.Moving && (at == rake.Front || at == rake.Back);
		}

		public void Couple(Train rake, CPos at, bool atFront)
		{
			if (!CanCouple(rake, at))
				return;

			// Le bout de la rame qui touche le train doit se retrouver contre lui.
			var cars = rake.Cars.ToList();
			var cells = rake.chain.ToList();
			if (atFront ? rake.Back != at : rake.Front != at)
			{
				cars.Reverse();
				cells.Reverse();
			}

			network.RemoveTrain(rake);
			Cars.InsertRange(atFront ? 0 : Cars.Count, cars);
			chain.InsertRange(atFront ? 0 : chain.Count, cells);
			from = chain.ToList();
			foreach (var c in cars)
				c.Train = this;

			Assign();
		}

		// Deux rames bout à bout : elles n'en font plus qu'une.
		public bool CanJoin(Train rake)
		{
			return rake != this && IsRake && rake.IsRake && !Moving && !rake.Moving
				&& (Adjacent(Front, rake.Front) || Adjacent(Front, rake.Back) || Adjacent(Back, rake.Front) || Adjacent(Back, rake.Back));
		}

		public void Join(Train rake)
		{
			if (!CanJoin(rake))
				return;

			if (Adjacent(Back, rake.Front) || Adjacent(Back, rake.Back))
				Couple(rake, Adjacent(Back, rake.Front) ? rake.Front : rake.Back, false);
			else
				Couple(rake, Adjacent(Front, rake.Back) ? rake.Back : rake.Front, true);
		}

		// Dételer : le wagon et tout ce qui est derrière lui (côté opposé à la locomotive), qui restent une rame.
		public void Uncouple(RailCar wagon)
		{
			var loco = Locomotive;
			var i = Cars.IndexOf(wagon);
			if (Moving || loco == null || i < 0 || wagon == loco)
				return;

			Separate(i > Cars.IndexOf(loco) ? Cars.Count - i : -(i + 1));
		}

		// Une voiture quitte le monde (détruite, vendue) : le reste du train se scinde autour d'elle.
		public void Remove(RailCar car)
		{
			if (Moving)
				FinishStep();

			var i = Cars.IndexOf(car);
			if (i < 0)
				return;

			var o = Cars.Take(i).Sum(c => c.Length);
			Cars.RemoveAt(i);
			chain.RemoveRange(o, car.Length);
			from = chain.ToList();
			car.Train = null;
			if (Cars.Count == 0)
			{
				network.RemoveTrain(this);
				return;
			}

			// Les voitures d'un côté du trou (celui sans locomotive) forment une rame à part.
			var loco = Locomotive;
			var l = loco != null ? Cars.IndexOf(loco) : -1;
			if (l < 0 || l < i)
				Separate(Cars.Count - i);
			else
				Separate(-i);

			Assign();
		}

		// count > 0 : les count dernières voitures partent en rame ; count < 0 : les -count premières.
		void Separate(int count)
		{
			if (count == 0)
				return;

			var n = Math.Abs(count);
			List<RailCar> cars;
			List<CPos> cells;
			if (count > 0)
			{
				cars = Cars.Skip(Cars.Count - n).ToList();
				var len = cars.Sum(c => c.Length);
				cells = chain.Skip(chain.Count - len).ToList();
				Cars.RemoveRange(Cars.Count - n, n);
				chain.RemoveRange(chain.Count - len, len);
			}
			else
			{
				cars = Cars.Take(n).ToList();
				var len = cars.Sum(c => c.Length);
				cells = chain.Take(len).ToList();
				Cars.RemoveRange(0, n);
				chain.RemoveRange(0, len);
			}

			from = chain.ToList();
			new Train(network, cars, cells);
			Assign();
		}

		// Voitures attelées de part et d'autre (pour dessiner les attelages).
		public RailCar Next(RailCar car)
		{
			var i = Cars.IndexOf(car);
			return i >= 0 && i + 1 < Cars.Count ? Cars[i + 1] : null;
		}
	}
}
