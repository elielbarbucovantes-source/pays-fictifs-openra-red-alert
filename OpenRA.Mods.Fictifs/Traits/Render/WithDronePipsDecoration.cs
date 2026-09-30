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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits.Render
{
	[Desc("Displays one pip per drone of a " + nameof(DroneCarrier) + " batch: green = drone alive, empty = lost.")]
	public class WithDronePipsDecorationInfo : WithDecorationBaseInfo, Requires<DroneCarrierInfo>
	{
		[Desc("If non-zero, override the spacing between adjacent pips.")]
		public readonly int2 PipStride = int2.Zero;

		[Desc("Image that defines the pip sequences.")]
		public readonly string Image = "pips";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for lost drones.")]
		public readonly string EmptySequence = "pip-empty";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for living drones.")]
		public readonly string AliveSequence = "pip-green";

		[PaletteReference]
		public readonly string Palette = "chrome";

		public override object Create(ActorInitializer init) { return new WithDronePipsDecoration(init.Self, this); }
	}

	public class WithDronePipsDecoration : WithDecorationBase<WithDronePipsDecorationInfo>
	{
		readonly DroneCarrier carrier;
		readonly Animation pips;

		public WithDronePipsDecoration(Actor self, WithDronePipsDecorationInfo info)
			: base(self, info)
		{
			carrier = self.Trait<DroneCarrier>();
			pips = new Animation(self.World, info.Image);
		}

		protected override IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 screenPos)
		{
			pips.PlayRepeating(Info.EmptySequence);

			var palette = wr.Palette(Info.Palette);
			var pipSize = pips.Image.Size.XY.ToInt2();
			var pipStride = Info.PipStride != int2.Zero ? Info.PipStride : new int2(pipSize.X, 0);
			var alive = carrier.Alive;

			screenPos -= pipSize / 2;
			for (var i = 0; i < carrier.Info.BatchSize; i++)
			{
				pips.PlayRepeating(i < alive ? Info.AliveSequence : Info.EmptySequence);
				yield return new UISpriteRenderable(pips.Image, self.CenterPosition, screenPos, 0, palette);

				screenPos += pipStride;
			}
		}
	}
}
