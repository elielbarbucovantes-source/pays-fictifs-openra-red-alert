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

using OpenRA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Œil d'Ambar (Ananthanie) : pose une caméra (acteur qui révèle) sur la zone choisie pour une durée limitée.

	[Desc("Spawns a camera actor at the target cell, removed after Duration ticks.")]
	public class RevealAreaPowerInfo : SupportPowerInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Camera actor to spawn.")]
		public readonly string CameraActor = null;

		[Desc("Ticks before the camera is removed.")]
		public readonly int Duration = 750;

		public override object Create(ActorInitializer init) { return new RevealAreaPower(init.Self, this); }
	}

	public class RevealAreaPower : SupportPower
	{
		readonly RevealAreaPowerInfo info;

		public RevealAreaPower(Actor self, RevealAreaPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
			self.World.AddFrameEndTask(w =>
			{
				var camera = w.CreateActor(info.CameraActor, new TypeDictionary
				{
					new LocationInit(cell),
					new OwnerInit(self.Owner),
				});

				w.Add(new DelayedAction(info.Duration, () =>
				{
					if (!camera.Disposed)
						camera.Dispose();
				}));
			});
		}
	}
}
