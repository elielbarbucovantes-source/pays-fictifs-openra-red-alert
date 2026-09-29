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

// Dans le moteur récent, les collections de MultiBrush sont une section
// `MultiBrushCollections` des fichiers de tileset. Le moteur release-20231010 ne
// sait pas la lire : on la place dans des fichiers séparés, déclarés par le
// générateur (clé `MultiBrushFiles`), et chargés ici à la demande.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA.Mods.MapGen
{
	public static class MultiBrushCollections
	{
		static readonly object SyncRoot = new();
		static IReadOnlyFileSystem fileSystem;
		static readonly Dictionary<string, string> Files = new();
		static readonly Dictionary<string, Dictionary<string, ImmutableArray<MultiBrushInfo>>> Cache = new();

		public static void Register(ModData modData, IReadOnlyDictionary<string, string> filesByTileset)
		{
			lock (SyncRoot)
			{
				if (fileSystem != modData.DefaultFileSystem)
				{
					fileSystem = modData.DefaultFileSystem;
					Cache.Clear();
				}

				foreach (var kv in filesByTileset)
				{
					if (Files.TryGetValue(kv.Key, out var existing) && existing == kv.Value)
						continue;

					Files[kv.Key] = kv.Value;
					Cache.Remove(kv.Key);
				}
			}
		}

		public static ImmutableArray<MultiBrushInfo> Get(ITerrainInfo terrainInfo, string name)
		{
			lock (SyncRoot)
			{
				if (!Cache.TryGetValue(terrainInfo.Id, out var collections))
				{
					if (!Files.TryGetValue(terrainInfo.Id, out var file))
						throw new MapGenerationException($"Aucun fichier MultiBrush déclaré pour le tileset {terrainInfo.Id}");

					using (var stream = fileSystem.Open(file))
					{
						var root = MiniYaml.FromStream(stream, file);
						var node = root.FirstOrDefault(n => n.Key == "MultiBrushCollections")
							?? throw new YamlException($"{file} ne contient pas de section MultiBrushCollections");

						collections = node.Value.Nodes.ToDictionary(
							n => n.Key,
							n => MultiBrushInfo.ParseCollection(n.Value));
					}

					Cache[terrainInfo.Id] = collections;
				}

				if (!collections.TryGetValue(name, out var collection))
					throw new MapGenerationException($"Collection MultiBrush `{name}` absente pour le tileset {terrainInfo.Id}");

				return collection;
			}
		}
	}
}
