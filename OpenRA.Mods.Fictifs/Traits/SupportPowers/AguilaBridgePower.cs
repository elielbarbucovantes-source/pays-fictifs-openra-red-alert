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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Lays a line of bridge segments between two cells: first click = start, second click = end.",
		"Segments are ordinary actors (usually with ChangesTerrain and KillsSelf).")]
	public class AguilaBridgePowerInfo : TwoCellPowerInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Segment spawned where the line runs east-west.")]
		public readonly string HorizontalSegment = null;

		[ActorReference]
		[FieldLoader.Require]
		[Desc("Segment spawned where the line runs north-south.")]
		public readonly string VerticalSegment = null;

		public override object Create(ActorInitializer init) { return new AguilaBridgePower(init.Self, this); }
	}

	public class AguilaBridgePower : TwoCellPower
	{
		readonly AguilaBridgePowerInfo info;

		public AguilaBridgePower(Actor self, AguilaBridgePowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		// Cells from first to second where each step moves along a single axis,
		// so ground units never have to cut a corner to follow the bridge.
		public static List<CPos> Line(CPos first, CPos second)
		{
			var cells = new List<CPos> { first };
			int dx = second.X - first.X, dy = second.Y - first.Y;
			int sx = Math.Sign(dx), sy = Math.Sign(dy);
			int x = first.X, y = first.Y;

			while (x != second.X || y != second.Y)
			{
				// Take the step that stays closest to the straight line.
				var errX = Math.Abs((x + sx - first.X) * dy - (y - first.Y) * dx);
				var errY = Math.Abs((x - first.X) * dy - (y + sy - first.Y) * dx);
				if (y == second.Y || (x != second.X && errX <= errY))
					x += sx;
				else
					y += sy;

				cells.Add(new CPos(x, y));
			}

			return cells;
		}

		public override IEnumerable<CPos> PreviewCells(CPos first, CPos second)
		{
			return Line(first, second);
		}

		public override bool IsValidPair(CPos first, CPos second)
		{
			return first != second && base.IsValidPair(first, second);
		}

		protected override void Deploy(Actor self, CPos first, CPos second)
		{
			var cells = Line(first, second);
			self.World.AddFrameEndTask(w =>
			{
				var delta = second - first;
				var segment = Math.Abs(delta.X) >= Math.Abs(delta.Y) ? info.HorizontalSegment : info.VerticalSegment;
				foreach (var cell in cells)
					w.CreateActor(segment, new TypeDictionary
					{
						new LocationInit(cell),
						new OwnerInit(self.Owner),
					});
			});
		}
	}
}
