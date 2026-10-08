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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("PathFinder du moteur, qui abandonne tout de suite une recherche vers des cibles inatteignables.",
		"Remplace PathFinder sur l'acteur World.")]
	public class PathFinderFictifsInfo : PathFinderInfo
	{
		public override object Create(ActorInitializer init) { return new PathFinderFictifs(init.Self); }
	}

	// Une unité dont la case est encombrée (fantassins serrés dans une base) est considérée
	// comme partant d'une case inaccessible : le moteur fait alors une recherche locale sans le
	// pathfinder hiérarchique. Si la cible est sur une autre île, cette recherche explore tout le
	// continent de la cible, à chaque tick et pour chaque unité (escouades de l'IA visant une
	// cible de l'autre côté de l'eau : le jeu ralentit fortement).
	// On vérifie donc d'abord, en ignorant les acteurs, qu'au moins une cible est atteignable.
	public class PathFinderFictifs : PathFinder, IPathFinder
	{
		public PathFinderFictifs(Actor self)
			: base(self) { }

		List<CPos> IPathFinder.FindPathToTargetCells(
			Actor self, CPos source, IEnumerable<CPos> targets, BlockedByActor check,
			Func<CPos, int> customCost, Actor ignoreActor, bool laneBias)
		{
			if (self.OccupiesSpace is Mobile mobile)
			{
				var targetList = targets as IList<CPos> ?? targets.ToList();
				if (!targetList.Any(t => PathExistsForLocomotor(mobile.Locomotor, source, t)))
					return NoPath;

				targets = targetList;
			}

			return FindPathToTargetCells(self, source, targets, check, customCost, ignoreActor, laneBias);
		}
	}
}
