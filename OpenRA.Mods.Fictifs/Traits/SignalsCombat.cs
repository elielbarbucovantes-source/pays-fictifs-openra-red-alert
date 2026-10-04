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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Jauge d'offensive « Blitz 7 » (Ananthanie) : chaque unité retient le dernier moment où elle a
	// tiré ou été touchée par un ennemi. La jauge (BlitzGaugePower) ne se remplit pas tant qu'une
	// unité du joueur est au combat, et va plus vite quand beaucoup d'unités attendent en réserve.

	[Desc("Remembers when this actor last fired or was hit by an enemy (used by " + nameof(BlitzGaugePower) + ").")]
	public class SignalsCombatInfo : TraitInfo<SignalsCombat> { }

	public class SignalsCombat : INotifyAttack, INotifyDamage
	{
		public int LastCombatTick { get; private set; } = int.MinValue / 2;

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			LastCombatTick = self.World.WorldTick;
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value <= 0 || e.Attacker == null || e.Attacker.Owner == null)
				return;

			if (self.Owner.RelationshipWith(e.Attacker.Owner) == PlayerRelationship.Enemy)
				LastCombatTick = self.World.WorldTick;
		}
	}
}
