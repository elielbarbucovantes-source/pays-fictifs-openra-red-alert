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
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Base for support powers targeted with two clicks: a first cell, then a second cell.")]
	public abstract class TwoCellPowerInfo : SupportPowerInfo
	{
		[Desc("Minimum distance between the two cells, in cells.")]
		public readonly int MinDistance = 0;

		[Desc("Maximum distance between the two cells, in cells. 0 means unlimited.")]
		public readonly int MaxDistance = 0;

		[PaletteReference]
		public readonly string TargetOverlayPalette = TileSet.TerrainPaletteInternalName;

		public readonly string FootprintImage = "overlay";

		[SequenceReference(nameof(FootprintImage))]
		public readonly string ValidFootprintSequence = "target-valid";

		[SequenceReference(nameof(FootprintImage))]
		public readonly string InvalidFootprintSequence = "target-invalid";

		[SequenceReference(nameof(FootprintImage))]
		public readonly string SourceFootprintSequence = "target-select";

		[CursorReference]
		[Desc("Cursor to display when selecting the first cell.")]
		public readonly string SelectionCursor = "chrono-select";

		[CursorReference]
		[Desc("Cursor to display when selecting the second cell.")]
		public readonly string TargetCursor = "chrono-target";

		[CursorReference]
		[Desc("Cursor to display when the second cell is not valid.")]
		public readonly string TargetBlockedCursor = "move-blocked";
	}

	public abstract class TwoCellPower : SupportPower
	{
		readonly TwoCellPowerInfo info;

		protected TwoCellPower(Actor self, TwoCellPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectFirstCell(order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			var first = order.ExtraLocation;
			var second = self.World.Map.CellContaining(order.Target.CenterPosition);
			if (!IsValidFirstCell(first) || !IsValidPair(first, second))
				return;

			base.Activate(self, order, manager);
			PlayLaunchSounds();
			Deploy(self, first, second);
		}

		protected abstract void Deploy(Actor self, CPos first, CPos second);

		public virtual bool IsValidFirstCell(CPos cell)
		{
			return Self.World.Map.Contains(cell) && Self.Owner.Shroud.IsExplored(cell);
		}

		public virtual bool IsValidPair(CPos first, CPos second)
		{
			if (!Self.World.Map.Contains(second) || !Self.Owner.Shroud.IsExplored(second))
				return false;

			var distSq = (second - first).LengthSquared;
			if (distSq < info.MinDistance * info.MinDistance)
				return false;

			return info.MaxDistance <= 0 || distSq <= info.MaxDistance * info.MaxDistance;
		}

		// Cells highlighted while choosing the second cell.
		public virtual IEnumerable<CPos> PreviewCells(CPos first, CPos second)
		{
			yield return first;
			yield return second;
		}

		sealed class SelectFirstCell : OrderGenerator
		{
			readonly TwoCellPower power;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectFirstCell(string order, SupportPowerManager manager, TwoCellPower power)
			{
				// Clear selection if using Left-Click Orders
				if (Game.Settings.Game.UseClassicMouseStyle)
					manager.Self.World.Selection.Clear();

				this.manager = manager;
				this.order = order;
				this.power = power;
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left && power.IsValidFirstCell(cell))
					world.OrderGenerator = new SelectSecondCell(world, order, manager, power, cell);

				yield break;
			}

			protected override void Tick(World world)
			{
				if (!manager.Powers.TryGetValue(order, out var p) || !p.Active || !p.Ready)
					world.CancelInputMode();
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return power.IsValidFirstCell(cell) ? power.info.SelectionCursor : power.info.TargetBlockedCursor;
			}
		}

		sealed class SelectSecondCell : OrderGenerator
		{
			readonly TwoCellPower power;
			readonly SupportPowerManager manager;
			readonly string order;
			readonly CPos first;
			readonly Sprite validTile, invalidTile, sourceTile;
			readonly float validAlpha, invalidAlpha, sourceAlpha;

			public SelectSecondCell(World world, string order, SupportPowerManager manager, TwoCellPower power, CPos first)
			{
				this.manager = manager;
				this.order = order;
				this.power = power;
				this.first = first;

				var info = power.info;
				var sequences = world.Map.Sequences;
				var valid = sequences.GetSequence(info.FootprintImage, info.ValidFootprintSequence);
				validTile = valid.GetSprite(0);
				validAlpha = valid.GetAlpha(0);

				var invalid = sequences.GetSequence(info.FootprintImage, info.InvalidFootprintSequence);
				invalidTile = invalid.GetSprite(0);
				invalidAlpha = invalid.GetAlpha(0);

				var source = sequences.GetSequence(info.FootprintImage, info.SourceFootprintSequence);
				sourceTile = source.GetSprite(0);
				sourceAlpha = source.GetAlpha(0);
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
				{
					world.CancelInputMode();
					yield break;
				}

				if (!power.IsValidPair(first, cell))
					yield break;

				world.CancelInputMode();
				yield return new Order(order, manager.Self, Target.FromCell(world, cell), false)
				{
					ExtraLocation = first,
					SuppressVisualFeedback = true
				};
			}

			protected override void Tick(World world)
			{
				if (!manager.Powers.TryGetValue(order, out var p) || !p.Active || !p.Ready)
					world.CancelInputMode();
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var palette = wr.Palette(power.info.TargetOverlayPalette);
				var cell = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var isValid = power.IsValidPair(first, cell);
				var tile = isValid ? validTile : invalidTile;
				var alpha = isValid ? validAlpha : invalidAlpha;

				yield return new SpriteRenderable(sourceTile, wr.World.Map.CenterOfCell(first), WVec.Zero, -511, palette, 1f, sourceAlpha,
					float3.Ones, TintModifiers.IgnoreWorldTint, true);

				foreach (var c in power.PreviewCells(first, cell).Where(c => c != first))
					yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(c), WVec.Zero, -511, palette, 1f, alpha,
						float3.Ones, TintModifiers.IgnoreWorldTint, true);
			}

			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return power.IsValidPair(first, cell) ? power.info.TargetCursor : power.info.TargetBlockedCursor;
			}
		}
	}
}
