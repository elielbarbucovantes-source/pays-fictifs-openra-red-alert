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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// « Blitz 7 » : la jauge d'offensive de l'Ananthanie. C'est un pouvoir qui donne une condition
	// à toutes les unités du joueur (GrantConditionToOwnedActorsPower), mais sa recharge dépend
	// de la préparation de l'armée :
	//   - elle s'arrête tant qu'une unité (SignalsCombat) a tiré ou été touchée récemment ;
	//   - elle accélère avec le nombre d'unités au repos (la réserve) et avec les postes de
	//     commandement (BoostActors).

	[Desc("Offensive gauge: grants a condition to every owned actor, but only charges while the army",
		"is out of combat, faster with idle units in reserve and with BoostActors.")]
	public class BlitzGaugePowerInfo : GrantConditionToOwnedActorsPowerInfo
	{
		[Desc("An actor that fired or was hit by an enemy less than this many ticks ago is in combat.")]
		public readonly int CombatMemory = 125;

		[Desc("Extra charge speed (percent) per idle unit in reserve.")]
		public readonly int ReserveBonusPerUnit = 7;

		[Desc("Maximum extra charge speed (percent) from the reserve.")]
		public readonly int MaxReserveBonus = 133;

		[Desc("Actor types that each add BoostPercent to the charge speed.")]
		public readonly HashSet<string> BoostActors = new();

		[Desc("Extra charge speed (percent) per BoostActors actor.")]
		public readonly int BoostPercent = 15;

		[Desc("Ticks between two counts of the army.")]
		public readonly int ScanInterval = 25;

		public override object Create(ActorInitializer init) { return new BlitzGaugePower(init.Self, this); }
	}

	public class BlitzGaugePower : GrantConditionToOwnedActorsPower
	{
		public readonly BlitzGaugePowerInfo GaugeInfo;

		public BlitzGaugePower(Actor self, BlitzGaugePowerInfo info)
			: base(self, info)
		{
			GaugeInfo = info;
		}

		public override SupportPowerInstance CreateInstance(string key, SupportPowerManager manager)
		{
			return new BlitzGaugeInstance(key, GaugeInfo, manager);
		}
	}

	public class BlitzGaugeInstance : SupportPowerInstance
	{
		readonly BlitzGaugePowerInfo info;
		int scanTicks;

		// Vitesse de charge en centièmes de tick par tick (100 = normale, 0 = arrêtée).
		public int Rate { get; private set; } = 100;
		public bool InCombat { get; private set; }
		public int Reserve { get; private set; }

		public BlitzGaugeInstance(string key, BlitzGaugePowerInfo info, SupportPowerManager manager)
			: base(key, info, manager)
		{
			this.info = info;
		}

		public override void Tick()
		{
			base.Tick();
			if (!Active || remainingSubTicks <= 0)
				return;

			if (--scanTicks <= 0)
			{
				scanTicks = info.ScanInterval;
				Scan();
			}

			// base.Tick a déjà retiré 100 : on corrige vers la vitesse voulue.
			remainingSubTicks = (remainingSubTicks + 100 - Rate).Clamp(0, TotalTicks * 100);
		}

		void Scan()
		{
			var self = Manager.Self;
			var world = self.World;
			var tick = world.WorldTick;
			var owner = self.Owner;

			var inCombat = false;
			var reserve = 0;
			foreach (var p in world.ActorsWithTrait<SignalsCombat>())
			{
				if (p.Actor.Owner != owner || p.Actor.IsDead || !p.Actor.IsInWorld)
					continue;

				if (tick - p.Trait.LastCombatTick < info.CombatMemory)
					inCombat = true;
				else if (p.Actor.IsIdle)
					reserve++;
			}

			var boosts = info.BoostActors.Count == 0 ? 0 : world.Actors
				.Count(a => a.Owner == owner && !a.IsDead && a.IsInWorld && info.BoostActors.Contains(a.Info.Name));

			InCombat = inCombat;
			Reserve = reserve;
			Rate = inCombat ? 0 : 100 + System.Math.Min(info.MaxReserveBonus, reserve * info.ReserveBonusPerUnit) + boosts * info.BoostPercent;
		}
	}
}
