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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Salve Agni (Ananthanie) : le bâtiment qui porte le pouvoir tire une salve de missiles de
	// croisière (projectiles de l'arme Weapon) sur la zone choisie, chacun sur un point au hasard.
	// Avec MaxRange (bombe atomique miniature du porte-avions rubénien) : la cible doit être à portée
	// d'un des acteurs qui portent le pouvoir ; le plus proche tire. Hors de portée, le clic est
	// refusé et la charge est gardée.

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

		[Desc("The target must be within this distance of one of the actors granting the power. Zero means unlimited.")]
		public readonly WDist MaxRange = WDist.Zero;

		[Desc("Colour of the range circles drawn around the launchers while targeting (with MaxRange).")]
		public readonly Color RangeCircleColor = Color.FromArgb(128, Color.OrangeRed);

		[CursorReference]
		[Desc("Cursor shown out of range (with MaxRange).")]
		public readonly string BlockedCursor = "generic-blocked";

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

		public override SupportPowerInstance CreateInstance(string key, SupportPowerManager manager)
		{
			return info.MaxRange.Length > 0 ? new RangedSalvoInstance(key, info, manager) : base.CreateInstance(key, manager);
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			if (info.MaxRange.Length > 0)
				self.World.OrderGenerator = new SelectRangedSalvoTarget(order, manager, info);
			else
				base.SelectTarget(self, order, manager);
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

	// Instance à portée limitée : un ordre hors de portée de tous les lanceurs est ignoré
	// (sans remettre le chrono à zéro) ; sinon le lanceur le plus proche tire (SupportPowerInstance).
	public class RangedSalvoInstance : SupportPowerInstance
	{
		readonly ProjectileSalvoPowerInfo info;

		public RangedSalvoInstance(string key, ProjectileSalvoPowerInfo info, SupportPowerManager manager)
			: base(key, info, manager)
		{
			this.info = info;
		}

		public IEnumerable<Actor> Launchers => Instances
			.Where(i => !i.IsTraitPaused && !i.IsTraitDisabled && i.Self.IsInWorld && !i.Self.IsDead)
			.Select(i => i.Self);

		public bool InRange(WPos target)
		{
			return Launchers.Any(a => (a.CenterPosition - target).HorizontalLengthSquared <= info.MaxRange.LengthSquared);
		}

		public override void Activate(Order order)
		{
			if (order.Target.Type == TargetType.Invalid || !InRange(order.Target.CenterPosition))
				return;

			base.Activate(order);
		}
	}

	public class SelectRangedSalvoTarget : SelectGenericPowerTarget
	{
		readonly SupportPowerManager manager;
		readonly ProjectileSalvoPowerInfo info;

		public SelectRangedSalvoTarget(string order, SupportPowerManager manager, ProjectileSalvoPowerInfo info)
			: base(order, manager, info.Cursor, MouseButton.Left)
		{
			this.manager = manager;
			this.info = info;
		}

		RangedSalvoInstance Instance => manager.Powers.TryGetValue(OrderKey, out var p) ? p as RangedSalvoInstance : null;

		bool InRange(World world, CPos cell)
		{
			var instance = Instance;
			return instance != null && world.Map.Contains(cell) && instance.InRange(world.Map.CenterOfCell(cell));
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			// Clic gauche hors de portée : on reste en mode ciblage.
			if (mi.Button == MouseButton.Left && !InRange(world, cell))
				return Enumerable.Empty<Order>();

			return base.OrderInner(world, cell, worldPixel, mi);
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return InRange(world, cell) ? info.Cursor : info.BlockedCursor;
		}

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			var instance = Instance;
			if (instance == null)
				yield break;

			foreach (var a in instance.Launchers)
				yield return new RangeCircleAnnotationRenderable(a.CenterPosition, info.MaxRange, 0,
					info.RangeCircleColor, 1, Color.FromArgb(96, Color.Black), 3);
		}
	}
}
