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
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Activities
{
	// Creuse les cases d'une ligne, une par une, depuis une case voisine.
	// Le tunnelier ne se tient jamais dans une tranchée : si elle était inondée, il se noierait.
	public class DigCanal : Activity
	{
		readonly DigsCanals digger;
		readonly CanalLayer layer;
		readonly Mobile mobile;
		readonly List<CPos> cells;
		readonly HashSet<CPos> failed = new();

		CPos? stand;
		CPos target;
		bool digging;
		int token = Actor.InvalidConditionToken;

		public DigCanal(Actor self, List<CPos> cells)
		{
			digger = self.Trait<DigsCanals>();
			layer = self.World.WorldActor.TraitOrDefault<CanalLayer>();
			mobile = self.Trait<Mobile>();
			this.cells = cells;
		}

		static bool Adjacent(CPos a, CPos b)
		{
			var d = a - b;
			return a != b && System.Math.Abs(d.X) <= 1 && System.Math.Abs(d.Y) <= 1;
		}

		bool CanDigFrom(CPos from, CPos cell)
		{
			return Adjacent(from, cell) && !layer.IsTrench(from);
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || layer == null || digger.IsTraitDisabled)
			{
				StopDigging(self);
				return true;
			}

			if (digging)
			{
				if (!layer.CanDig(target) || !CanDigFrom(self.Location, target))
				{
					StopDigging(self);
					return false;
				}

				if (++digger.Progress < digger.Info.DigDelay)
					return false;

				StopDigging(self);
				cells.Remove(target);
				if (layer.Dig(target, self) > 0)
					TextNotificationsManager.AddTransientLine(digger.Info.FloodedTextNotification, self.Owner);

				return false;
			}

			cells.RemoveAll(c => !layer.CanDig(c));
			if (cells.Count == 0)
				return true;

			target = cells[0];
			if (CanDigFrom(self.Location, target))
			{
				stand = null;
				failed.Clear();
				digging = true;
				digger.Progress = 0;
				if (!string.IsNullOrEmpty(digger.Info.DiggingCondition) && token == Actor.InvalidConditionToken)
					token = self.GrantCondition(digger.Info.DiggingCondition);

				var facing = (self.World.Map.CenterOfCell(target) - self.CenterPosition).Yaw;
				QueueChild(new Turn(self, facing));
				return false;
			}

			// Le déplacement précédent n'a pas abouti : on essaie une autre case.
			if (stand.HasValue && self.Location != stand.Value)
				failed.Add(stand.Value);

			stand = PickStand(self);
			if (stand == null)
			{
				// Inaccessible : on passe à la case suivante.
				cells.RemoveAt(0);
				failed.Clear();
				return false;
			}

			QueueChild(mobile.MoveTo(stand.Value, 0));
			return false;
		}

		CPos? PickStand(Actor self)
		{
			var next = cells.Count > 1 ? cells[1] : (CPos?)null;
			var best = (CPos?)null;
			var bestScore = int.MaxValue;
			for (var dy = -1; dy <= 1; dy++)
			{
				for (var dx = -1; dx <= 1; dx++)
				{
					var c = target + new CVec(dx, dy);
					if (c == target || failed.Contains(c) || !self.World.Map.Contains(c) || layer.IsTrench(c))
						continue;

					if (!mobile.CanEnterCell(c, self, BlockedByActor.Immovable))
						continue;

					// Sur la ligne (sauf la prochaine case), il faudrait bouger de nouveau aussitôt.
					var score = (c - self.Location).LengthSquared;
					if (c != next && cells.Contains(c))
						score += 10000;

					if (score < bestScore)
					{
						bestScore = score;
						best = c;
					}
				}
			}

			return best;
		}

		void StopDigging(Actor self)
		{
			digging = false;
			digger.Progress = -1;
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (cells.Count == 0)
				yield break;

			var color = digger.Info.TargetLineColor;
			yield return new TargetLineNode(Target.FromCell(self.World, cells[0]), color);
			foreach (var c in cells.Skip(1))
				yield return new TargetLineNode(Target.FromCell(self.World, c), color, tile: digger.Tile);
		}
	}
}
