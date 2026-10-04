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

using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Pacte de Fraternité (Ananthanie) : pouvoir de coalition. Comme GrantExternalConditionPower
	// (la condition touche les unités du joueur ET de ses alliés dans la zone), plus une caméra
	// qui révèle la zone, et une recharge plus rapide pour chaque allié encore en jeu.

	[Desc("GrantExternalConditionPower that also reveals the area (CameraActor) and charges",
		"AllyChargeBonus percent faster for every allied player still in the game.")]
	public class FraternitePowerInfo : GrantExternalConditionPowerInfo
	{
		[ActorReference]
		[Desc("Actor spawned at the target to reveal the area.")]
		public readonly string CameraActor = null;

		[Desc("Ticks before the camera is removed.")]
		public readonly int CameraDuration = 1125;

		[Desc("Extra charge speed (percent) per allied player still in the game.")]
		public readonly int AllyChargeBonus = 20;

		public override object Create(ActorInitializer init) { return new FraternitePower(init.Self, this); }
	}

	public class FraternitePower : GrantExternalConditionPower
	{
		readonly FraternitePowerInfo info;

		public FraternitePower(Actor self, FraternitePowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			if (string.IsNullOrEmpty(info.CameraActor))
				return;

			var pos = order.Target.CenterPosition;
			self.World.AddFrameEndTask(w =>
			{
				var camera = w.CreateActor(info.CameraActor, new TypeDictionary
				{
					new LocationInit(w.Map.CellContaining(pos)),
					new OwnerInit(self.Owner),
				});

				w.Add(new DelayedAction(info.CameraDuration, () =>
				{
					if (!camera.Disposed)
						camera.Dispose();
				}));
			});
		}

		public override SupportPowerInstance CreateInstance(string key, SupportPowerManager manager)
		{
			return new FraterniteInstance(key, info, manager);
		}
	}

	public class FraterniteInstance : SupportPowerInstance
	{
		readonly FraternitePowerInfo info;
		int scanTicks;
		int allies;

		public FraterniteInstance(string key, FraternitePowerInfo info, SupportPowerManager manager)
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
				scanTicks = 25;
				var owner = Manager.Self.Owner;
				allies = owner.World.Players.Count(p => p != owner && !p.NonCombatant && !p.Spectating
					&& p.WinState != WinState.Lost && p.IsAlliedWith(owner));
			}

			remainingSubTicks = (remainingSubTicks - allies * info.AllyChargeBonus).Clamp(0, TotalTicks * 100);
		}
	}
}
