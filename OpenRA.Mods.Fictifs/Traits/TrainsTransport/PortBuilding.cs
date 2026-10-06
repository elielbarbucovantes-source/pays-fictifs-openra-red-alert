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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Bâtiment à cheval sur la côte (gare portuaire australouisienne) : le bâtiment et la voie sont à terre,
	// le quai (cases « _ » de l'empreinte, non vérifiées par le moteur) doit être sur l'eau.
	// Le moteur ne vérifie qu'un seul jeu de TerrainTypes pour toute l'empreinte : la condition sur l'eau
	// passe par IsCloseEnoughToBase, que l'aperçu de placement, l'ordre de pose et l'IA consultent tous.
	[Desc("Building whose listed footprint cells must be on water (e.g. a port quay), the rest using TerrainTypes.")]
	public class PortBuildingInfo : BuildingInfo
	{
		[Desc("Cells (relative to the top-left) that must be water. Use Empty ('_') footprint cells for them.")]
		public readonly CVec[] WaterCells = Array.Empty<CVec>();

		[Desc("Terrain types counted as water.")]
		public readonly HashSet<string> WaterTerrainTypes = new() { "Water" };

		[Desc("Terrain types also accepted under the quay (ragged coasts).")]
		public readonly HashSet<string> ShoreTerrainTypes = new() { "Beach", "Clear", "Rough", "Road" };

		[Desc("Minimum number of quay cells that must be water.")]
		public readonly int MinWaterCells = 2;

		// Les côtes sont rarement droites : le quai accepte la plage et le sol nu, pourvu qu'au moins
		// MinWaterCells cases soient dans l'eau ; aucune case du quai ne doit déborder sur un bâtiment.
		public override bool IsCloseEnoughToBase(World world, Player p, ActorInfo ai, CPos topLeft)
		{
			var influence = world.WorldActor.TraitOrDefault<BuildingInfluence>();
			var water = 0;
			foreach (var v in WaterCells)
			{
				var cell = topLeft + v;
				if (!world.Map.Contains(cell) || (influence != null && influence.AnyBuildingAt(cell)))
					return false;

				var type = world.Map.GetTerrainInfo(cell).Type;
				if (WaterTerrainTypes.Contains(type))
					water++;
				else if (!ShoreTerrainTypes.Contains(type))
					return false;
			}

			return water >= Math.Min(MinWaterCells, WaterCells.Length) && base.IsCloseEnoughToBase(world, p, ai, topLeft);
		}
	}
}
