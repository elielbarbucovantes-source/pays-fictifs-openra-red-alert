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

// Rétroportage du générateur de cartes aléatoires d'OpenRA (playtest-20260222)
// vers le moteur release-20231010. Ces interfaces vivaient dans OpenRA.Game et
// OpenRA.Mods.Common ; elles sont regroupées ici.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.MapGen
{
	public class MapGenerationException : Exception
	{
		public MapGenerationException(string message)
			: base(message) { }
	}

	public interface IMapGeneratorInfo : ITraitInfoInterface
	{
		string Type { get; }
		string Name { get; }
		string MapTitle { get; }

		Map Generate(ModData modData, MapGenerationArgs args);
	}

	public interface IEditorMapGeneratorInfo : IMapGeneratorInfo
	{
		string[] Tilesets { get; }
		IMapGeneratorSettings GetSettings();
	}

	public interface IMapGeneratorSettings
	{
		ImmutableArray<MapGeneratorOption> Options { get; }

		int PlayerCount { get; }

		void Randomize(MersenneTwister random);

		void Initialize(MapGenerationArgs args);

		MapGenerationArgs Compile(ITerrainInfo terrainInfo, Size size);
	}
}
