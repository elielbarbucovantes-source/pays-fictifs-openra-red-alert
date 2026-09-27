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
	[Desc("Displays one pip per aircraft slot of an " + nameof(AircraftCarrier) + ".")]
	public class WithCarrierPipsDecorationInfo : WithDecorationBaseInfo, Requires<AircraftCarrierInfo>
	{
		[Desc("If non-zero, override the spacing between adjacent pips.")]
		public readonly int2 PipStride = int2.Zero;

		[Desc("Image that defines the pip sequences.")]
		public readonly string Image = "pips";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for free slots.")]
		public readonly string EmptySequence = "pip-empty";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for aircraft ready to launch.")]
		public readonly string ReadySequence = "pip-green";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for aircraft still rearming.")]
		public readonly string RearmingSequence = "pip-yellow";

		[PaletteReference]
		public readonly string Palette = "chrome";

		public override object Create(ActorInitializer init) { return new WithCarrierPipsDecoration(init.Self, this); }
	}

	public class WithCarrierPipsDecoration : WithDecorationBase<WithCarrierPipsDecorationInfo>
	{
		readonly AircraftCarrier carrier;
		readonly Animation pips;

		public WithCarrierPipsDecoration(Actor self, WithCarrierPipsDecorationInfo info)
			: base(self, info)
		{
			carrier = self.Trait<AircraftCarrier>();
			pips = new Animation(self.World, info.Image);
		}

		protected override IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 screenPos)
		{
			pips.PlayRepeating(Info.EmptySequence);

			var palette = wr.Palette(Info.Palette);
			var pipSize = pips.Image.Size.XY.ToInt2();
			var pipStride = Info.PipStride != int2.Zero ? Info.PipStride : new int2(pipSize.X, 0);
			var readiness = carrier.Readiness.ToArray();

			screenPos -= pipSize / 2;
			for (var i = 0; i < carrier.Info.MaxAircraft; i++)
			{
				var sequence = i < readiness.Length ? (readiness[i] ? Info.ReadySequence : Info.RearmingSequence) : Info.EmptySequence;
				pips.PlayRepeating(sequence);
				yield return new UISpriteRenderable(pips.Image, self.CenterPosition, screenPos, 0, palette);

				screenPos += pipStride;
			}
		}
	}
}
