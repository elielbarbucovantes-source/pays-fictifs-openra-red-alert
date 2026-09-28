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

using System.Reflection;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Warheads
{
	[Desc("Makes the affected players forget what they had explored around the impact:",
		"the cells go back to black shroud, except those they currently see.",
		"Used by the Australouis seaplane (Cormoran) and its fog of sprayed water.")]
	public class ReshroudWarhead : Warhead
	{
		[Desc("Radius of the area that goes back to shroud.")]
		public readonly WDist Range = new(6144);

		[Desc("Players affected, relative to the firing player.")]
		public readonly PlayerRelationship AffectedPlayers = PlayerRelationship.Enemy;

		// Shroud has no public way to forget part of the map (only ResetExploration, for the whole map).
		static readonly FieldInfo ExploredField = typeof(Shroud).GetField("explored", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly FieldInfo TouchedField = typeof(Shroud).GetField("touched", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly FieldInfo AnyCellTouchedField = typeof(Shroud).GetField("anyCellTouched", BindingFlags.Instance | BindingFlags.NonPublic);

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (firedBy == null)
				return;

			var world = firedBy.World;
			var cells = Shroud.ProjectedCellsInRange(world.Map, args.ImpactPosition, WDist.Zero, Range);

			foreach (var player in world.Players)
			{
				if (player.NonCombatant || !AffectedPlayers.HasRelationship(firedBy.Owner.RelationshipWith(player)))
					continue;

				Forget(player.Shroud, world.Map, cells);
			}
		}

		static void Forget(Shroud shroud, Map map, System.Collections.Generic.IEnumerable<PPos> cells)
		{
			if (shroud == null || shroud.Disabled || shroud.ExploreMapEnabled)
				return;

			var explored = (ProjectedCellLayer<bool>)ExploredField.GetValue(shroud);
			var touched = (ProjectedCellLayer<bool>)TouchedField.GetValue(shroud);
			var any = false;
			foreach (var puv in cells)
			{
				if (!map.Contains(puv) || shroud.IsVisible(puv))
					continue;

				var index = touched.Index(puv);
				if (!explored[index])
					continue;

				explored[index] = false;
				touched[index] = true;
				any = true;
			}

			if (any)
				AnyCellTouchedField.SetValue(shroud, true);
		}
	}
}
