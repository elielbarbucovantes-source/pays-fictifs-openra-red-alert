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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Marks the actor as a drone, which anti-drone systems (DestroysNearbyDrones) destroy on contact.")]
	public class DroneInfo : TraitInfo<Drone> { }

	public class Drone { }

	[Desc("Anti-drone system: automatically destroys enemy drones (actors with the Drone trait) within range.",
		"Only active for actors of the listed factions (the faction of the actor, else of its owner).",
		"Used by the Australouis and Rubénie aircraft.")]
	public class DestroysNearbyDronesInfo : ConditionalTraitInfo
	{
		[Desc("Radius around the actor, measured horizontally (altitude is ignored).")]
		public readonly WDist Range = WDist.FromCells(1);

		[Desc("Ticks between two scans.")]
		public readonly int ScanInterval = 2;

		[Desc("Factions for which the system is active. Empty means all factions.")]
		public readonly HashSet<string> Factions = new();

		[Desc("Player relationships of the drones to destroy.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Enemy;

		[Desc("Damage types used to kill the drone.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[Desc("Sound played where a drone is destroyed.")]
		public readonly string Sound = null;

		public override object Create(ActorInitializer init) { return new DestroysNearbyDrones(init, this); }
	}

	public class DestroysNearbyDrones : ConditionalTrait<DestroysNearbyDronesInfo>, ITick, INotifyOwnerChanged
	{
		readonly string actorFaction;
		bool factionAllowed;
		int ticks;

		public DestroysNearbyDrones(ActorInitializer init, DestroysNearbyDronesInfo info)
			: base(info)
		{
			actorFaction = init.GetValue<FactionInit, string>((string)null);
			factionAllowed = IsFactionAllowed(init.Self.Owner);
		}

		bool IsFactionAllowed(Player owner)
		{
			if (Info.Factions.Count == 0)
				return true;

			return Info.Factions.Contains(actorFaction ?? owner.Faction.InternalName);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			factionAllowed = IsFactionAllowed(newOwner);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !factionAllowed || !self.IsInWorld || self.IsDead)
				return;

			if (--ticks > 0)
				return;

			ticks = Info.ScanInterval;

			var drones = self.World.FindActorsInCircle(self.CenterPosition, Info.Range)
				.Where(a => a != self && !a.IsDead && a.IsInWorld
					&& a.Info.HasTraitInfo<DroneInfo>()
					&& Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(a.Owner)))
				.ToList();

			foreach (var drone in drones)
			{
				if (Info.Sound != null)
					Game.Sound.Play(SoundType.World, Info.Sound, drone.CenterPosition);

				drone.Kill(self, Info.DamageTypes);
			}
		}
	}
}
