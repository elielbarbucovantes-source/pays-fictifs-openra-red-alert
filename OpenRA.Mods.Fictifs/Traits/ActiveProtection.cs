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
using System.Reflection;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Projectiles;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Protection active (Karnvasha, Ananthanie) : réduit les dégâts des missiles et des roquettes.
	// Les dégâts ne disent pas quelle arme les a causés : au chargement des règles, on ajoute
	// le type de dégâts DamageType à toutes les armes à projectile Missile, et aux obus (Bullet)
	// dont l'image est une roquette. Le modificateur ne regarde ensuite que ce type.

	[Desc("Reduces the damage taken from missiles and rockets.",
		"On ruleset load, adds DamageType to every weapon with a Missile projectile,",
		"or a Bullet projectile whose image is listed in RocketImages.")]
	public class ActiveProtectionInfo : ConditionalTraitInfo, IRulesetLoaded
	{
		[Desc("Percentage of missile and rocket damage actually taken.")]
		public readonly int Modifier = 65;

		[Desc("Damage type added to missile and rocket weapons.")]
		public readonly string DamageType = "Missile";

		[Desc("Bullet images that count as rockets (lower case).")]
		public readonly HashSet<string> RocketImages = new() { "dragon", "missile", "missile2", "v2" };

		public override object Create(ActorInitializer init) { return new ActiveProtection(this); }

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var field = typeof(DamageWarhead).GetField(nameof(DamageWarhead.DamageTypes), BindingFlags.Instance | BindingFlags.Public);
			foreach (var weapon in rules.Weapons.Values)
			{
				var rocket = weapon.Projectile is MissileInfo
					|| (weapon.Projectile is BulletInfo b && b.Image != null && RocketImages.Contains(b.Image.ToLowerInvariant()));
				if (!rocket)
					continue;

				foreach (var w in weapon.Warheads.OfType<DamageWarhead>())
				{
					if (w.DamageTypes.Contains(DamageType))
						continue;

					var types = w.DamageTypes.Append(DamageType).ToArray();
					field.SetValue(w, new BitSet<DamageType>(types));
				}
			}
		}
	}

	public class ActiveProtection : ConditionalTrait<ActiveProtectionInfo>, IDamageModifier
	{
		public ActiveProtection(ActiveProtectionInfo info)
			: base(info) { }

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			if (IsTraitDisabled || damage.DamageTypes.IsEmpty || !damage.DamageTypes.Contains(Info.DamageType))
				return 100;

			return Info.Modifier;
		}
	}
}
