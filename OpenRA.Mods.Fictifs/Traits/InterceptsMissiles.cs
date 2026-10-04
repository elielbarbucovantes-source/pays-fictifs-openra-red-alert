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
using System.Linq;
using System.Reflection;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Projectiles;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Ananta (Ananthanie) : le laser abat les missiles ennemis en vol. Les projectiles Missile
	// n'exposent ni leur position ni leur tireur : on les lit par réflexion. Le missile intercepté
	// est retiré du monde (aucun dégât) et l'arme Weapon (un laser) est tirée vers lui pour l'effet.

	[Desc("Destroys enemy Missile projectiles in flight within Range, one every ReloadDelay ticks.")]
	public class InterceptsMissilesInfo : ConditionalTraitInfo, IRulesetLoaded
	{
		[Desc("Interception range.")]
		public readonly WDist Range = WDist.FromCells(6);

		[Desc("Ticks between two interceptions.")]
		public readonly int ReloadDelay = 15;

		[WeaponReference]
		[Desc("Weapon fired at the intercepted missile (visual only: its projectile, e.g. LaserZap, and warheads at the missile position).")]
		public readonly string Weapon = null;

		[Desc("Firing position relative to the actor.")]
		public readonly WVec LocalOffset = new(0, 0, 512);

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new InterceptsMissiles(this); }

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (Weapon == null)
				return;

			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out var w))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{Weapon.ToLowerInvariant()}'");

			WeaponInfo = w;
		}
	}

	public class InterceptsMissiles : ConditionalTrait<InterceptsMissilesInfo>, ITick
	{
		static readonly FieldInfo PosField = typeof(Missile).GetField("pos", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly FieldInfo ArgsField = typeof(Missile).GetField("args", BindingFlags.Instance | BindingFlags.NonPublic);

		int cooldown;

		public int Intercepted { get; private set; }

		public InterceptsMissiles(InterceptsMissilesInfo info)
			: base(info) { }

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || PosField == null || ArgsField == null)
				return;

			if (cooldown > 0)
			{
				cooldown--;
				return;
			}

			var range = Info.Range.LengthSquared;
			Missile best = null;
			var bestPos = WPos.Zero;
			var bestDist = long.MaxValue;
			foreach (var m in self.World.Effects.OfType<Missile>())
			{
				var args = (ProjectileArgs)ArgsField.GetValue(m);
				var shooter = args?.SourceActor?.Owner;
				if (shooter == null || self.Owner.RelationshipWith(shooter) != PlayerRelationship.Enemy)
					continue;

				var pos = (WPos)PosField.GetValue(m);
				var d = (pos - self.CenterPosition).LengthSquared;
				if (d <= range && d < bestDist)
				{
					best = m;
					bestPos = pos;
					bestDist = d;
				}
			}

			if (best == null)
				return;

			cooldown = Info.ReloadDelay;
			Intercepted++;
			var missile = best;
			self.World.AddFrameEndTask(w => w.Remove(missile));
			Zap(self, bestPos);
		}

		void Zap(Actor self, WPos target)
		{
			var weapon = Info.WeaponInfo;
			if (weapon?.Projectile == null)
				return;

			var source = self.CenterPosition + Info.LocalOffset;
			var facing = (target - source).Yaw;
			var args = new ProjectileArgs
			{
				Weapon = weapon,
				Facing = facing,
				CurrentMuzzleFacing = () => facing,
				DamageModifiers = Array.Empty<int>(),
				InaccuracyModifiers = Array.Empty<int>(),
				RangeModifiers = Array.Empty<int>(),
				Source = source,
				CurrentSource = () => source,
				SourceActor = self,
				PassiveTarget = target,
				GuidedTarget = Target.FromPos(target)
			};

			if (weapon.Report != null && weapon.Report.Length > 0)
				Game.Sound.Play(SoundType.World, weapon.Report.Random(self.World.LocalRandom), source);

			self.World.AddFrameEndTask(w => w.Add(weapon.Projectile.Create(args)));
		}
	}
}
