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
using OpenRA.GameRules;
using OpenRA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Salve Agni (Ananthanie) : le bâtiment qui porte le pouvoir tire une salve de missiles de
	// croisière (projectiles de l'arme Weapon) sur la zone choisie, chacun sur un point au hasard.

	[Desc("Fires a salvo of projectiles of Weapon from the power's owner at random points around the target.")]
	public class ProjectileSalvoPowerInfo : SupportPowerInfo, IRulesetLoaded
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Weapon whose projectile and warheads are used.")]
		public readonly string Weapon = null;

		[Desc("Number of projectiles.")]
		public readonly int Count = 6;

		[Desc("Ticks between two launches.")]
		public readonly int Interval = 8;

		[Desc("Impacts are scattered within this distance of the target.")]
		public readonly WDist Scatter = WDist.FromCells(2);

		[Desc("Launch position relative to the power's owner.")]
		public readonly WVec LaunchOffset = new(0, 0, 1024);

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new ProjectileSalvoPower(init.Self, this); }

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out var w))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{Weapon.ToLowerInvariant()}'");

			WeaponInfo = w;
		}
	}

	public class ProjectileSalvoPower : SupportPower
	{
		readonly ProjectileSalvoPowerInfo info;

		public ProjectileSalvoPower(Actor self, ProjectileSalvoPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var target = order.Target.CenterPosition;
			self.World.AddFrameEndTask(w =>
			{
				for (var i = 0; i < info.Count; i++)
				{
					var r = w.SharedRandom.Next(info.Scatter.Length + 1);
					var a = new WAngle(w.SharedRandom.Next(1024));
					var impact = target + new WVec(0, -r, 0).Rotate(WRot.FromYaw(a));
					w.Add(new DelayedAction(1 + i * info.Interval, () => Launch(self, impact)));
				}
			});
		}

		void Launch(Actor self, WPos impact)
		{
			if (self.IsDead || !self.IsInWorld)
				return;

			var source = self.CenterPosition + info.LaunchOffset;
			var facing = (impact - source).Yaw;
			var args = new ProjectileArgs
			{
				Weapon = info.WeaponInfo,
				Facing = facing,
				CurrentMuzzleFacing = () => facing,
				DamageModifiers = Array.Empty<int>(),
				InaccuracyModifiers = Array.Empty<int>(),
				RangeModifiers = Array.Empty<int>(),
				Source = source,
				CurrentSource = () => source,
				SourceActor = self,
				PassiveTarget = impact,
				GuidedTarget = Target.FromPos(impact)
			};

			if (info.WeaponInfo.Report != null && info.WeaponInfo.Report.Length > 0)
				Game.Sound.Play(SoundType.World, info.WeaponInfo.Report.Random(self.World.LocalRandom), source);

			var projectile = info.WeaponInfo.Projectile?.Create(args);
			if (projectile != null)
				self.World.Add(projectile);
		}
	}
}
