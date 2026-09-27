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

		public readonly Color TimeBarColor = Color.FromArgb(220, 30, 30);

		public override object Create(ActorInitializer init) { return new AguilaGate(this); }
	}

	public class AguilaGate : ITick, ISelectionBar, INotifyKilled
	{
		readonly AguilaGateInfo info;

		// Units that just came out of this end: they must walk away before they can go back in.
		readonly HashSet<Actor> arrivals = new();

		// Units already sent from this end, waiting for their teleport activity to run.
		readonly HashSet<Actor> departing = new();

		Actor partner;
		AguilaGateLink link;
		int remainingTime;
		int scanDelay;
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
			arrivals.RemoveWhere(a => a.IsDead || !inRange.Contains(a));
			departing.RemoveWhere(a => a.IsDead || !inRange.Contains(a));

			var otherEnd = partner.Trait<AguilaGate>();
			foreach (var a in inRange)
			{
				if (a == self || a.IsDead || !a.IsInWorld || arrivals.Contains(a) || departing.Contains(a))
					continue;

				if (!info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(a.Owner)))
					continue;

				if (a.TraitOrDefault<Mobile>() == null)
					continue;

				if (info.ValidFactions.Count > 0 && !info.ValidFactions.Contains(a.Owner.Faction.InternalName))
					continue;

				if (!link.Unlimited && link.Remaining <= 0)
					break;

				departing.Add(a);
				otherEnd.arrivals.Add(a);
				if (!link.Unlimited)
					link.Remaining--;

				a.QueueActivity(false, new Teleport(partner, partner.Location, null, false, false, info.TransitSound));
			}
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
