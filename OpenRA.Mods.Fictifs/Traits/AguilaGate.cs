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
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Shared between the two ends of a tunnel.
	public class AguilaGateLink
	{
		public int Remaining;
		public readonly bool Unlimited;

		public AguilaGateLink(int capacity)
		{
			Remaining = capacity;
			Unlimited = capacity <= 0;
		}
	}

	[Desc("One end of a temporary tunnel. Friendly ground units that come close to it reappear at the other end.")]
	public class AguilaGateInfo : TraitInfo
	{
		[Desc("Units closer than this to the gate are sent through the tunnel.")]
		public readonly WDist Range = new(1536);

		[Desc("Ticks before the tunnel closes on its own.")]
		public readonly int LifeTime = 1500;

		[Desc("Player relationships allowed to use the tunnel.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("Only units belonging to players of these factions may use the tunnel. Empty means any faction.")]
		public readonly HashSet<string> ValidFactions = new();

		[Desc("Sound played at both ends when a unit goes through.")]
		public readonly string TransitSound = null;

		[Desc("Interval (in ticks) between two checks for units to send through.")]
		public readonly int ScanInterval = 5;

		[Desc("After coming out, units move this far from the exit (in cells, beyond Range) so they don't go straight back in.")]
		public readonly int ClearDistance = 2;

		[Desc("Ticks a unit sent through keeps its 'just arrived' protection at the exit even if it hasn't appeared there yet.")]
		public readonly int ArrivalGrace = 100;

		public readonly Color TimeBarColor = Color.FromArgb(220, 30, 30);

		public override object Create(ActorInitializer init) { return new AguilaGate(this); }
	}

	public class AguilaGate : ITick, ISelectionBar, INotifyKilled
	{
		readonly AguilaGateInfo info;

		// Units that just came out of this end (value: ticks since sent): they must walk away before they can go back in.
		readonly Dictionary<Actor, int> arrivals = new();

		// Arrivals already seen near this end: they lose their protection as soon as they leave the range.
		readonly HashSet<Actor> arrivalsSeen = new();

		// Units already sent from this end, waiting for their teleport activity to run.
		readonly HashSet<Actor> departing = new();

		Actor partner;
		AguilaGateLink link;
		int remainingTime;
		int scanDelay;
		int exitCount;
		bool closing;

		public AguilaGate(AguilaGateInfo info)
		{
			this.info = info;
			remainingTime = info.LifeTime;
		}

		public void Link(Actor partner, AguilaGateLink link)
		{
			this.partner = partner;
			this.link = link;
		}

		void ITick.Tick(Actor self)
		{
			if (closing || partner == null)
				return;

			// Once the last allowed unit is sent, leave it a moment to go through before collapsing.
			if (!link.Unlimited && link.Remaining <= 0 && remainingTime > 25)
				remainingTime = 25;

			if (partner.IsDead || partner.Disposed || --remainingTime <= 0)
			{
				Close(self);
				return;
			}

			if (--scanDelay > 0)
				return;

			scanDelay = info.ScanInterval;

			var inRange = self.World.FindActorsInCircle(self.CenterPosition, info.Range).ToHashSet();
			departing.RemoveWhere(a => a.IsDead || !inRange.Contains(a));

			// Une unité envoyée n'apparaît ici qu'après quelques ticks (fin de son pas en cours) :
			// on ne lui retire sa protection qu'une fois qu'elle est arrivée puis repartie.
			foreach (var a in arrivals.Keys.ToList())
			{
				arrivals[a] += info.ScanInterval;
				if (inRange.Contains(a))
					arrivalsSeen.Add(a);
				else if (a.IsDead || arrivalsSeen.Contains(a) || arrivals[a] > info.ArrivalGrace)
				{
					arrivals.Remove(a);
					arrivalsSeen.Remove(a);
				}
			}

			var otherEnd = partner.Trait<AguilaGate>();
			foreach (var a in inRange)
			{
				if (a == self || a.IsDead || !a.IsInWorld || arrivals.ContainsKey(a) || departing.Contains(a))
					continue;

				if (!info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(a.Owner)))
					continue;

				var mobile = a.TraitOrDefault<Mobile>();
				if (mobile == null)
					continue;

				if (info.ValidFactions.Count > 0 && !info.ValidFactions.Contains(a.Owner.Faction.InternalName))
					continue;

				if (!link.Unlimited && link.Remaining <= 0)
					break;

				departing.Add(a);
				otherEnd.arrivals[a] = 0;
				otherEnd.arrivalsSeen.Remove(a);
				if (!link.Unlimited)
					link.Remaining--;

				a.QueueActivity(false, new Teleport(partner, partner.Location, null, false, false, info.TransitSound));

				// En sortant, l'unité s'écarte d'elle-même de la bouche, dans le sens du passage.
				var clearCell = otherEnd.FindClearCell(partner, self, mobile);
				if (clearCell != null)
					a.QueueActivity(true, mobile.MoveTo(clearCell.Value, 0, null, true));
			}
		}

		/// <summary>
		/// Case libre hors de portée de cette bouche (self), dans le prolongement du trajet
		/// depuis l'autre bouche (from). Les unités successives s'étalent de part et d'autre.
		/// </summary>
		CPos? FindClearCell(Actor self, Actor from, Mobile mobile)
		{
			var map = self.World.Map;
			var rangeCells = (info.Range.Length + 1023) / 1024;
			var distance = rangeCells + info.ClearDistance;

			var dir = self.CenterPosition - from.CenterPosition;
			dir = new WVec(dir.X, dir.Y, 0);
			var length = dir.Length;
			if (length == 0)
			{
				dir = new WVec(0, 1024, 0);
				length = 1024;
			}

			// 0, +1, -1, +2, -2 cases de décalage latéral, puis on recommence.
			var k = exitCount++ % 5;
			var lateral = (k + 1) / 2 * (k % 2 == 1 ? 1 : -1);
			var side = new WVec(-dir.Y, dir.X, 0);
			var ideal = self.CenterPosition + dir * (distance * 1024) / length + side * (lateral * 1024) / length;
			var idealCell = map.CellContaining(ideal);

			CPos? best = null;
			var bestScore = int.MaxValue;
			foreach (var c in map.FindTilesInAnnulus(self.Location, rangeCells + 1, distance + 3))
			{
				if (!mobile.CanEnterCell(c, null, BlockedByActor.Immovable))
					continue;

				var score = (c - idealCell).LengthSquared;
				if (score < bestScore)
				{
					best = c;
					bestScore = score;
				}
			}

			return best;
		}

		void Close(Actor self)
		{
			closing = true;
			self.World.AddFrameEndTask(w =>
			{
				if (!self.Disposed)
					self.Dispose();
			});
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// Destroying one end collapses the whole tunnel.
			if (partner != null && !partner.IsDead && !partner.Disposed)
				partner.Kill(e.Attacker);
		}

		float ISelectionBar.GetValue()
		{
			return info.LifeTime > 0 ? (float)remainingTime / info.LifeTime : 0;
		}

		Color ISelectionBar.GetColor() { return info.TimeBarColor; }

		bool ISelectionBar.DisplayWhenEmpty => false;
	}
}
