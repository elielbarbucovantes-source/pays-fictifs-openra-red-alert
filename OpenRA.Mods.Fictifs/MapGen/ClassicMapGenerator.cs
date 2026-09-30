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
using System.Collections.Immutable;
using System.Linq;

using OpenRA.Mods.Common.Terrain;
using OpenRA.Support;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using static OpenRA.Mods.Common.Traits.ResourceLayerInfo;

namespace OpenRA.Mods.MapGen
{
	[TraitLocation(SystemActors.EditorWorld)]
	public sealed class ClassicMapGeneratorInfo : TraitInfo, IEditorMapGeneratorInfo
	{
		[FieldLoader.Require]
		public readonly string Type = null;

		[FieldLoader.Require]
		public readonly string Name = null;

		[FieldLoader.Require]
		[Desc("Tilesets that are compatible with this map generator.")]
		public readonly string[] Tilesets = null;

		[Desc("The title to use for generated maps.")]
		public readonly string MapTitle = "label-random-map";

		[Desc("The widget tree to open when the tool is selected.")]
		public readonly string PanelWidget = "MAP_GENERATOR_TOOL_PANEL";

		[FieldLoader.Require]
		[Desc("Fichiers contenant la section MultiBrushCollections de chaque tileset (clé = identifiant du tileset).")]
		public readonly Dictionary<string, string> MultiBrushFiles = null;

		[FieldLoader.LoadUsing(nameof(SettingsLoader))]
		public readonly MiniYaml Settings;

		[Desc("Folder holding the real-world region images (tools/monde.py), e.g. <mod>|mapgen/monde/.")]
		public readonly string WorldRegionFolder = null;

		// Altitudes réelles (m) des paliers de falaises, du plus bas au plus haut.
		static readonly int[] RealCliffAltitudes = [400, 900, 1500, 2200, 3000, 3900, 4900, 5900];
		const int RealMinimumLandSeaThickness = 3;

		// Pont réel : cases où le tablier peut s'arrêter.
		static readonly string[] BridgeLandingTerrain = ["Clear", "Road", "Rough", "Beach", "Ore", "Gems"];

		// Distance (en cases) jusqu'à laquelle un bout de pont cherche la terre au-delà du tracé.
		const int BridgeLandingSearch = 10;

		// Décalage latéral maximal (en cases) d'un pont dont le tracé ne relie pas deux terres.
		const int BridgeShiftSearch = 6;

		// Région réelle : toute zone praticable d'au moins cette surface (en cases) est reliée
		// à la plus grande, et chaque bout de pont doit déboucher sur une telle zone...
		const int LandAreaMinimumSize = 400;

		// ... quitte à dégager un chemin d'un coût au plus égal à celui-ci (case libre 1, encombrée 5).
		const int LandAreaMaximumPathCost = 200;
		const int LandAreaMaximumAttempts = 64;

		string IMapGeneratorInfo.Type => Type;
		string IMapGeneratorInfo.Name => Name;
		string IMapGeneratorInfo.MapTitle => MapTitle;
		string[] IEditorMapGeneratorInfo.Tilesets => Tilesets;

		static MiniYaml SettingsLoader(MiniYaml my)
		{
			return my.NodeWithKey("Settings").Value;
		}

		const int FractionMax = Terraformer.FractionMax;
		const int EntityBonusMax = 1000000;

		sealed class Parameters
		{
			[FieldLoader.Require]
			public readonly int Seed = default;
			[FieldLoader.Require]
			public readonly int Rotations = default;
			[FieldLoader.LoadUsing(nameof(MirrorLoader))]
			public readonly Symmetry.Mirror Mirror = default;
			[FieldLoader.Require]
			public readonly int Players = default;
			[FieldLoader.Require]
			public readonly int TerrainFeatureSize = default;
			[FieldLoader.Require]
			public readonly int ForestFeatureSize = default;
			[FieldLoader.Require]
			public readonly int ResourceFeatureSize = default;
			[FieldLoader.Require]
			public readonly int CivilianBuildingsFeatureSize = default;
			[FieldLoader.Require]
			public readonly int Water = default;
			[FieldLoader.Require]
			public readonly int Mountains = default;
			[FieldLoader.Require]
			public readonly int Forests = default;
			[FieldLoader.Require]
			public readonly int ForestCutout = default;
			[FieldLoader.Require]
			public readonly int MaximumCutoutSpacing = default;
			[FieldLoader.Require]
			public readonly int ExternalCircularBias = default;
			[FieldLoader.Require]
			public readonly int TerrainSmoothing = default;
			[FieldLoader.Require]
			public readonly int SmoothingThreshold = default;
			public readonly int MinimumCoastStraight = -1;
			[FieldLoader.Require]
			public readonly int MinimumLandSeaThickness = default;
			[FieldLoader.Require]
			public readonly int MinimumMountainThickness = default;
			[FieldLoader.Require]
			public readonly int MaximumAltitude = default;
			[FieldLoader.Require]
			public readonly int RoughnessRadius = default;
			[FieldLoader.Require]
			public readonly int Roughness = default;
			public readonly int WaterRoughness = 0;
			[FieldLoader.Require]
			public readonly int MinimumTerrainContourSpacing = default;
			public readonly int MinimumBeachLength = 0;
			public readonly int MinimumWaterCliffLength = 0;
			[FieldLoader.Require]
			public readonly int MinimumCliffLength = default;
			[FieldLoader.Require]
			public readonly int ForestClumpiness = default;
			[FieldLoader.Require]
			public readonly bool DenyWalledAreas = default;
			[FieldLoader.Require]
			public readonly int EnforceSymmetry = default;
			[FieldLoader.Require]
			public readonly bool Roads = default;
			[FieldLoader.Require]
			public readonly int RoadSpacing = default;
			[FieldLoader.Require]
			public readonly int RoadShrink = default;
			[FieldLoader.Require]
			public readonly bool CreateEntities = default;
			[FieldLoader.Require]
			public readonly int AreaEntityBonus = default;
			[FieldLoader.Require]
			public readonly int PlayerCountEntityBonus = default;
			[FieldLoader.Require]
			public readonly int CentralSpawnReservationFraction = default;
			[FieldLoader.Require]
			public readonly int ResourceSpawnReservation = default;
			[FieldLoader.Require]
			public readonly int SpawnRegionSize = default;
			[FieldLoader.Require]
			public readonly int SpawnBuildSize = default;
			[FieldLoader.Require]
			public readonly int MinimumSpawnRadius = default;
			[FieldLoader.Require]
			public readonly int SpawnResourceSpawns = default;
			[FieldLoader.Require]
			public readonly int SpawnReservation = default;
			[FieldLoader.Require]
			public readonly int SpawnResourceBias = default;
			[FieldLoader.Require]
			public readonly int ResourcesPerPlayer = default;
			[FieldLoader.Require]
			public readonly int OreUniformity = default;
			[FieldLoader.Require]
			public readonly int OreClumpiness = default;
			[FieldLoader.Require]
			public readonly int MaximumExpansionResourceSpawns = default;
			[FieldLoader.Require]
			public readonly int MaximumResourceSpawnsPerExpansion = default;
			[FieldLoader.Require]
			public readonly int MinimumExpansionSize = default;
			[FieldLoader.Require]
			public readonly int MaximumExpansionSize = default;
			[FieldLoader.Require]
			public readonly int ExpansionInner = default;
			[FieldLoader.Require]
			public readonly int ExpansionBorder = default;
			[FieldLoader.Require]
			public readonly int MinimumBuildings = default;
			[FieldLoader.Require]
			public readonly int MaximumBuildings = default;
			[FieldLoader.LoadUsing(nameof(BuildingWeightsLoader))]
			public readonly IReadOnlyDictionary<string, int> BuildingWeights = default;
			[FieldLoader.Require]
			public readonly int CivilianBuildings = default;
			[FieldLoader.Require]
			public readonly int CivilianBuildingDensity = default;
			[FieldLoader.Require]
			public readonly int MinimumCivilianBuildingDensity = default;
			[FieldLoader.Require]
			public readonly int CivilianBuildingDensityRadius = default;

			// Région du monde réel (vide = relief aléatoire)
			// et nombre exact de derricks (0 = selon « Bâtiments tech. »).
			public readonly string WorldRegion = null;
			public readonly int OilDerricks = 0;
			public readonly string OilDerrickActor = "oilb";
			public readonly int WorldRegionMapWidth = 0;
			public readonly string BridgeActorNS = "pont.ns";
			public readonly string BridgeActorEW = "pont.ew";

			[FieldLoader.Require]
			public readonly ushort LandTile = default;
			[FieldLoader.Require]
			public readonly ushort WaterTile = default;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> SegmentedBrushes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> ForestObstacles;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> UnplayableObstacles;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> CivilianBuildingsObstacles;
			[FieldLoader.Ignore]
			public readonly IReadOnlyDictionary<ushort, IReadOnlyList<MultiBrush>> RepaintTiles;

			[FieldLoader.Ignore]
			public readonly ResourceTypeInfo DefaultResource;
			[FieldLoader.Ignore]
			public readonly IReadOnlyDictionary<string, ResourceTypeInfo> ResourceSpawnSeeds;
			[FieldLoader.LoadUsing(nameof(ResourceSpawnWeightsLoader))]
			public readonly IReadOnlyDictionary<string, int> ResourceSpawnWeights = default;

			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> ClearTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> PlayableTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> DominantTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> ZoneableTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> ClearSegmentTypes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> BeachSegmentTypes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> WaterCliffSegmentTypes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> CliffSegmentTypes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> RoadSegmentTypes;

			public Parameters(Map map, MiniYaml my)
			{
				FieldLoader.Load(this, my);

				// Une vraie région n'est pas symétrique et garde sa forme rectangulaire.
				if (!string.IsNullOrEmpty(WorldRegion))
				{
					Rotations = 1;
					Mirror = Symmetry.Mirror.None;
					EnforceSymmetry = 0;
					ExternalCircularBias = 0;
				}

				var terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
				SegmentedBrushes = MultiBrush.LoadCollection(map, "Segmented");
				ForestObstacles = MultiBrush.LoadCollection(map, my.NodeWithKey("ForestObstacles").Value.Value);
				UnplayableObstacles = MultiBrush.LoadCollection(map, my.NodeWithKey("UnplayableObstacles").Value.Value);
				CivilianBuildingsObstacles = MultiBrush.LoadCollection(map, my.NodeWithKey("CivilianBuildingsObstacles").Value.Value);
				RepaintTiles = my.NodeWithKeyOrDefault("RepaintTiles")?.Value.ToDictionary(
					k =>
					{
						if (MapGenCompat.TryParseUshortInvariant(k, out var tile))
							return tile;
						else
							throw new YamlException($"RepaintTile {k} is not a ushort");
					},
					v => MultiBrush.LoadCollection(map, v.Value) as IReadOnlyList<MultiBrush>);
				RepaintTiles ??= ImmutableDictionary<ushort, IReadOnlyList<MultiBrush>>.Empty;

				var resourceTypes = map.Rules.Actors[SystemActors.World].TraitInfoOrDefault<ResourceLayerInfo>().ResourceTypes;
				if (!resourceTypes.TryGetValue(my.NodeWithKey("DefaultResource").Value.Value, out DefaultResource))
					throw new YamlException("DefaultResource is not valid");
				var playerResourcesInfo = map.Rules.Actors[SystemActors.Player].TraitInfoOrDefault<PlayerResourcesInfo>();
				try
				{
					ResourceSpawnSeeds = my.NodeWithKey("ResourceSpawnSeeds").Value
						.ToDictionary(subMy => subMy.Value)
						.ToDictionary(kv => kv.Key, kv => resourceTypes[kv.Value]);
				}
				catch (KeyNotFoundException e)
				{
					throw new YamlException("Bad ResourceSpawnSeeds resource: " + e);
				}

				switch (Rotations)
				{
					case 1:
					case 2:
					case 4:
						break;
					default:
						EnforceSymmetry = 0;
						break;
				}

				IReadOnlySet<byte> ParseTerrainIndexes(string key)
				{
					return my.NodeWithKey(key).Value.Value
						.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.Select(terrainInfo.GetTerrainIndex)
						.ToHashSet();
				}

				IReadOnlyList<string> ParseSegmentTypes(string key)
				{
					return my.NodeWithKey(key).Value.Value
						.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.ToImmutableArray();
				}

				ClearTerrain = ParseTerrainIndexes("ClearTerrain");
				PlayableTerrain = ParseTerrainIndexes("PlayableTerrain");
				DominantTerrain = ParseTerrainIndexes("DominantTerrain");
				ZoneableTerrain = ParseTerrainIndexes("ZoneableTerrain");

				ClearSegmentTypes = ParseSegmentTypes("ClearSegmentTypes");
				BeachSegmentTypes = ParseSegmentTypes("BeachSegmentTypes");
				if (WaterRoughness > 0)
					WaterCliffSegmentTypes = ParseSegmentTypes("WaterCliffSegmentTypes");

				CliffSegmentTypes = ParseSegmentTypes("CliffSegmentTypes");
				RoadSegmentTypes = ParseSegmentTypes("RoadSegmentTypes");

				Validate(terrainInfo);
			}

			static object MirrorLoader(MiniYaml my)
			{
				if (Symmetry.TryParseMirror(my.NodeWithKey("Mirror").Value.Value, out var mirror))
					return mirror;
				else
					throw new YamlException($"Invalid Mirror value `{my.NodeWithKey("Mirror").Value.Value}`");
			}

			static IReadOnlyDictionary<string, int> BuildingWeightsLoader(MiniYaml my)
			{
				return my.NodeWithKey("BuildingWeights").Value.ToDictionary(subMy =>
					{
						if (Exts.TryParseInt32Invariant(subMy.Value, out var f))
							return f;
						else
							throw new YamlException($"Invalid building weight `{subMy.Value}`");
					});
			}

			static IReadOnlyDictionary<string, int> ResourceSpawnWeightsLoader(MiniYaml my)
			{
				return my.NodeWithKey("ResourceSpawnWeights").Value.ToDictionary(subMy =>
					{
						if (Exts.TryParseInt32Invariant(subMy.Value, out var f))
							return f;
						else
							throw new YamlException($"Invalid resource spawn weight `{subMy.Value}`");
					});
			}

			public void Validate(ITemplatedTerrainInfo terrainInfo)
			{
				if (Rotations < 1)
					throw new MapGenerationException("Rotations must be >= 1");
				if (TerrainFeatureSize < 1)
					throw new MapGenerationException("TerrainFeatureSize must be >= 1");
				if (ForestFeatureSize < 1)
					throw new MapGenerationException("ForestFeatureSize must be >= 1");
				if (ResourceFeatureSize < 1)
					throw new MapGenerationException("ResourceFeatureSize must be >= 1");
				if (CivilianBuildingsFeatureSize < 1)
					throw new MapGenerationException("CivilianBuildingsFeatureSize must be >= 1");
				if (TerrainSmoothing < 0 || TerrainSmoothing > MatrixUtils.MaxBinomialKernelRadius)
					throw new MapGenerationException($"TerrainSmoothing must be between 0 and {MatrixUtils.MaxBinomialKernelRadius} inclusive");
				if (WaterRoughness > 0 && MinimumCoastStraight < 0)
					throw new MapGenerationException("MinimumCoastStraight must be >= 0");
				if (SmoothingThreshold < (FractionMax + 1) / 2 || SmoothingThreshold > FractionMax)
					throw new MapGenerationException($"SmoothingThreshold must be between {(FractionMax + 1) / 2} and {FractionMax} inclusive");
				if (MinimumLandSeaThickness < 1)
					throw new MapGenerationException("MinimumLandSeaThickness must be >= 1");
				if (MinimumMountainThickness < 1)
					throw new MapGenerationException("MinimumMountainThickness must be >= 1");
				if (Water < 0 || Water > FractionMax)
					throw new MapGenerationException($"Water must be between 0 and {FractionMax} inclusive");
				if (Forests < 0 || Forests > FractionMax)
					throw new MapGenerationException($"Forest must be between 0 and {FractionMax} inclusive");
				if (ForestCutout < 0)
					throw new MapGenerationException("ForestCutout must be >= 0");
				if (MaximumCutoutSpacing < 0)
					throw new MapGenerationException("TopologyAugmentationThreshold must be >= 0");
				if (ForestClumpiness < 0)
					throw new MapGenerationException("ForestClumpiness must be >= 0");
				if (Mountains < 0 || Mountains > FractionMax)
					throw new MapGenerationException($"Mountains must be between 0 and {FractionMax} inclusive");
				if (Roughness < 0 || Roughness > FractionMax)
					throw new MapGenerationException("Roughness must be between 0 and {FractionMax}");
				if (WaterRoughness < 0 || WaterRoughness > FractionMax)
					throw new MapGenerationException("WaterRoughness must be between 0 and {FractionMax}");
				if (RoughnessRadius < 1)
					throw new MapGenerationException("RoughnessRadius must be >= 1");
				if (MaximumAltitude < 0)
					throw new MapGenerationException("MaximumAltitude must be >= 0");
				if (MinimumTerrainContourSpacing < 0)
					throw new MapGenerationException("MinimumTerrainContourSpacing must be >= 0");
				if (WaterRoughness > 0 && MinimumBeachLength < 1)
					throw new MapGenerationException("MinimumBeachLength must be >= 1");
				if (WaterRoughness > 0 && MinimumCliffLength < 1)
					throw new MapGenerationException("MinimumWaterCliffLength must be >= 1");
				if (MinimumCliffLength < 1)
					throw new MapGenerationException("MinimumCliffLength must be >= 1");
				if (RoadSpacing < 0)
					throw new MapGenerationException("RoadSpacing must be >= 0");
				if (RoadShrink < 0)
					throw new MapGenerationException("RoadShrink must be >= 0");
				if (Players < 0)
					throw new MapGenerationException("Players must be >= 0");
				if (CentralSpawnReservationFraction < 0)
					throw new MapGenerationException("CentralSpawnReservationFraction must be >= 0");
				if (AreaEntityBonus < 0)
					throw new MapGenerationException("PlayableAreaDensityBonus must be >= 0");
				if (PlayerCountEntityBonus < 0)
					throw new MapGenerationException("PlayerCountDensityBonus must be >= 0");
				if (SpawnRegionSize < 1)
					throw new MapGenerationException("SpawnRegionSize must be >= 1");
				if (SpawnReservation < 1)
					throw new MapGenerationException("SpawnReservation must be >= 1");
				if (SpawnBuildSize < 1)
					throw new MapGenerationException("SpawnBuildSize must be >= 1");
				if (MinimumSpawnRadius < 1)
					throw new MapGenerationException("MinimumSpawnRadius must be >= 1");
				if (SpawnResourceSpawns < 0)
					throw new MapGenerationException("SpawnResourceSpawns must be >= 0");
				if (ResourceSpawnReservation < 1)
					throw new MapGenerationException("ResourceSpawnReservation must be >= 1");
				if (MaximumExpansionResourceSpawns < 0)
					throw new MapGenerationException("MaximumExpansionResourceSpawns must be >= 0");
				if (MinimumExpansionSize < 1)
					throw new MapGenerationException("MinimumExpansionSize must be >= 1");
				if (MaximumExpansionSize < 1)
					throw new MapGenerationException("MaximumExpansionSize must be >= 1");
				if (MinimumExpansionSize > MaximumExpansionSize)
					throw new MapGenerationException("MinimumExpansionSize must be <= maximumExpansionSize");
				if (ExpansionBorder < 1)
					throw new MapGenerationException("ExpansionBorder must be >= 1");
				if (ExpansionInner < 1)
					throw new MapGenerationException("ExpansionInner must be >= 1");
				if (MaximumResourceSpawnsPerExpansion < 1)
					throw new MapGenerationException("MaximumResourceSpawnsPerExpansion must be >= 1");
				if (MinimumBuildings < 0)
					throw new MapGenerationException("MinimumBuildings must be >= 0");
				if (MaximumBuildings < 0)
					throw new MapGenerationException("MaximumBuildings must be >= 0");
				if (MinimumBuildings > MaximumBuildings)
					throw new MapGenerationException("MinimumBuildings must be <= maximumBuildings");
				if (CivilianBuildings < 0 || CivilianBuildings > FractionMax)
					throw new MapGenerationException($"CivilianBuildings must be between 0 and {FractionMax} inclusive");
				if (CivilianBuildingDensity < 0 || CivilianBuildingDensity > FractionMax)
					throw new MapGenerationException($"CivilianBuildingDensity must be between 0 and {FractionMax} inclusive");
				if (MinimumCivilianBuildingDensity < 0 || MinimumCivilianBuildingDensity > FractionMax)
					throw new MapGenerationException($"MinimumCivilianBuildingDensity must be between 0 and {FractionMax} inclusive");
				if (OilDerricks < 0 || OilDerricks > 100)
					throw new MapGenerationException("OilDerricks must be between 0 and 100 inclusive");
				if (CivilianBuildingDensityRadius < 0)
					throw new MapGenerationException("CivilianBuildingDensityRadius must be >= 0");
				if (ResourcesPerPlayer < 0)
					throw new MapGenerationException("ResourcesPerPlayer must be >= 0");
				if (OreUniformity < 0)
					throw new MapGenerationException("OreUniformity must be >= 0");
				if (OreClumpiness < 0)
					throw new MapGenerationException("OreClumpiness must be >= 0");
				foreach (var kv in BuildingWeights)
					if (kv.Value < 0)
						throw new MapGenerationException("BuildingWeights.* must be >= 0");
				foreach (var kv in ResourceSpawnWeights)
					if (kv.Value < 0)
						throw new MapGenerationException("ResourceSpawnWeights.* must be >= 0");
				foreach (var kv in ResourceSpawnWeights)
					if (!ResourceSpawnSeeds.ContainsKey(kv.Key))
						throw new MapGenerationException($"ResourceSpawnSeeds does not contain possible resource spawn `{kv.Key}`");

				if (!(terrainInfo.Templates.TryGetValue(LandTile, out var landTemplate) && landTemplate.Contains(0)))
					throw new MapGenerationException("LandTile is not valid");
				if (!(terrainInfo.Templates.TryGetValue(LandTile, out var waterTemplate) && waterTemplate.Contains(0)))
					throw new MapGenerationException("WaterTile is not valid");

				if (Players > 32)
					throw new MapGenerationException("Total number of players must not exceed 32");

				var symmetryCount = Symmetry.RotateAndMirrorProjectionCount(Rotations, Mirror);
				if (Players % symmetryCount != 0)
					throw new MapGenerationException($"Total number of players must be a multiple of {symmetryCount}");
			}
		}

		public IMapGeneratorSettings GetSettings()
		{
			return new MapGeneratorSettings(this, Settings);
		}

		public Map Generate(ModData modData, MapGenerationArgs args)
		{
			// Région réelle : on tente d'abord des côtes plus précises ; si les tuiles
			// de rivage n'y arrivent pas, on reprend avec le lissage habituel.
			if (!string.IsNullOrEmpty(args.Settings?.NodeWithKeyOrDefault("WorldRegion")?.Value.Value))
			{
				foreach (var thickness in new[] { RealMinimumLandSeaThickness, RealMinimumLandSeaThickness + 1, 0 })
				{
					try
					{
						return Generate(modData, args, thickness);
					}
					catch (MapGenerationException e) when (e.Message.Contains("coast"))
					{
						Log.Write("debug", $"Région réelle : côtes impossibles avec l'épaisseur {thickness}, nouvel essai.");
					}
				}
			}

			return Generate(modData, args, -1);
		}

		/// <param name="realThickness">
		/// Région réelle : épaisseur minimale des terres et des mers (0 = celle des réglages).
		/// -1 : lissage d'origine du générateur (dernier recours).
		/// </param>
		Map Generate(ModData modData, MapGenerationArgs args, int realThickness)
		{
			var terrainInfo = modData.DefaultTerrainInfo[args.Tileset];
			var size = args.Size;
			MultiBrushCollections.Register(modData, MultiBrushFiles);

			var map = new Map(modData, terrainInfo, size.Width, size.Height);
			var actorPlans = new List<ActorPlan>();

			var param = new Parameters(map, args.Settings);

			var terraformer = new Terraformer(args, map, modData, actorPlans, param.Mirror, param.Rotations);

			var waterIsPlayable = param.PlayableTerrain.Contains(terrainInfo.GetTerrainIndex(new TerrainTile(param.WaterTile, 0)));

			var externalCircleRadius = CellLayerUtils.Radius(map) - new WDist((param.MinimumLandSeaThickness + param.MinimumMountainThickness) * 1024);
			if (param.ExternalCircularBias != 0 && externalCircleRadius.Length <= 0)
				throw new MapGenerationException("map is too small for circular shaping");

			CellLayer<MultiBrush.Replaceability> PlayableToReplaceable()
			{
				var playable = terraformer.CheckSpace(param.PlayableTerrain, true);
				var basicLand = terraformer.CheckSpace(param.LandTile);
				var replace = new CellLayer<MultiBrush.Replaceability>(map);
				foreach (var mpos in map.AllCells.MapCoords)
					if (playable[mpos])
					{
						if (basicLand[mpos])
							replace[mpos] = MultiBrush.Replaceability.Any;
						else
							replace[mpos] = MultiBrush.Replaceability.Actor;
					}
					else
					{
						replace[mpos] = MultiBrush.Replaceability.None;
					}

				return replace;
			}

			// Use `random` to derive separate independent random number generators.
			//
			// This prevents changes in one part of the algorithm from affecting randomness in
			// other parts and provides flexibility for future parallel processing.
			//
			// In order to maximize stability, additions should be appended only. Disused
			// derivatives may be deleted but should be replaced with their unused call to
			// random.Next(). All generators should be created unconditionally.
			var random = new MersenneTwister(param.Seed);

			var elevationRandom = new MersenneTwister(random.Next());
			var coastTilingRandom = new MersenneTwister(random.Next());
			var cliffTilingRandom = new MersenneTwister(random.Next());
			var forestRandom = new MersenneTwister(random.Next());
			var forestTilingRandom = new MersenneTwister(random.Next());
			var symmetryTilingRandom = new MersenneTwister(random.Next());
			var debrisTilingRandom = new MersenneTwister(random.Next());
			var resourceRandom = new MersenneTwister(random.Next());
			var roadTilingRandom = new MersenneTwister(random.Next());
			var playerRandom = new MersenneTwister(random.Next());
			var expansionRandom = new MersenneTwister(random.Next());
			var buildingRandom = new MersenneTwister(random.Next());
			var topologyRandom = new MersenneTwister(random.Next());
			var repaintRandom = new MersenneTwister(random.Next());
			var decorationRandom = new MersenneTwister(random.Next());
			var decorationTilingRandom = new MersenneTwister(random.Next());
			var pickAnyRandom = new MersenneTwister(random.Next());
			var oilRandom = new MersenneTwister(random.Next());

			Matrix<int> realAltitude = null;
			List<(int2 From, int2 To)> realBridges = null;
			if (!string.IsNullOrEmpty(param.WorldRegion))
			{
				if (WorldRegionFolder == null)
					throw new MapGenerationException("WorldRegionFolder is not set");

				var regionSize = CellLayerUtils.CellBounds(map).Size.ToInt2();
				realAltitude = WorldRegion.Load(modData, WorldRegionFolder, param.WorldRegion, regionSize);
				realBridges = WorldRegion.LoadBridges(modData, WorldRegionFolder, param.WorldRegion, regionSize);
			}

			terraformer.InitMap();

			foreach (var mpos in map.AllCells.MapCoords)
				map.Tiles[mpos] = terraformer.PickTile(pickAnyRandom, param.LandTile);

			var elevation = terraformer.ElevationNoiseMatrix(
				elevationRandom,
				param.TerrainFeatureSize,
				param.TerrainSmoothing);
			var roughnessMatrix = MatrixUtils.GridVariance(
				elevation,
				param.RoughnessRadius);

			Matrix<bool> mapShape;
			if (param.ExternalCircularBias == 0)
				mapShape = new Matrix<bool>(CellLayerUtils.CellBounds(map).Size.ToInt2()).Fill(true);
			else
				mapShape = CellLayerUtils.ToMatrix(terraformer.CenteredCircle(true, false, externalCircleRadius), false);

			var landPlan = realAltitude != null
				? realAltitude.Map(a => a >= 0)
				: terraformer.SliceElevation(elevation, mapShape, FractionMax - param.Water);

			if (param.ExternalCircularBias > 0)
			{
				for (var n = 0; n < landPlan.Data.Length; n++)
					landPlan[n] |= !mapShape[n];
				var ring = terraformer.CenteredCircle(false, true, externalCircleRadius + new WDist(param.MinimumMountainThickness * 1024));
				var path = TilingPath.QuickCreate(
					map,
					param.SegmentedBrushes,
					CellLayerUtils.BordersToPoints(ring)[0],
					(param.MinimumMountainThickness - 1) / 2,
					param.CliffSegmentTypes[0],
					param.CliffSegmentTypes[0]);
				var brush = path.Tile(cliffTilingRandom)
					?? throw new MapGenerationException("Could not fit tiles for exterior circle cliffs");
				terraformer.PaintTiling(pickAnyRandom, brush);
			}

			// Région réelle : on retire les détails trop fins au lieu de les épaissir,
			// pour garder la vraie forme des côtes, des détroits et des mers.
			var landSeaThickness = param.MinimumLandSeaThickness;
			if (realAltitude != null && realThickness >= 0)
			{
				if (realThickness > 0)
					landSeaThickness = realThickness;
				landPlan = RealBlotch(landPlan, landSeaThickness);
			}
			else
				landPlan = MatrixUtils.BooleanBlotch(
					landPlan,
					param.TerrainSmoothing,
					param.SmoothingThreshold, /*smoothingThresholdOutOf=*/FractionMax,
					param.MinimumLandSeaThickness,
					/*bias=*/param.Water <= FractionMax / 2);

			var coast = MatrixUtils.BordersToPoints(landPlan);
			List<TilingPath> coastPaths;
			if (param.WaterRoughness > 0)
			{
				var beachZone = new Terraformer.PathPartitionZone()
				{
					RequiredSomewhere = true,
					SegmentType = param.BeachSegmentTypes[0],
					MinimumLength = param.MinimumBeachLength,
					MaximumDeviation = Math.Max(landSeaThickness - 1, RealMinimumLandSeaThickness),
				};
				var waterCliffZone = new Terraformer.PathPartitionZone()
				{
					SegmentType = param.WaterCliffSegmentTypes[0],
					MinimumLength = param.MinimumCliffLength,
					MaximumDeviation = Math.Max(landSeaThickness - 1, RealMinimumLandSeaThickness),
				};

				var waterCliffMask = MatrixUtils.CalibratedBooleanThreshold(
					roughnessMatrix,
					param.WaterRoughness, FractionMax);
				var partitionMask = waterCliffMask.Map(masked => masked ? waterCliffZone : beachZone);
				coastPaths = terraformer.PartitionPaths(
					coast,
					[beachZone, waterCliffZone],
					partitionMask,
					param.SegmentedBrushes,
					param.MinimumCoastStraight);

				foreach (var coastPath in coastPaths)
					coastPath
						.OptimizeLoop()
						.ExtendEdge(4);
			}
			else
			{
				coastPaths = CellLayerUtils.FromMatrixPoints(coast, map.Tiles)
					.Select(beach =>
						TilingPath.QuickCreate(
								map,
								param.SegmentedBrushes,
								beach,
								landSeaThickness - 1,
								param.BeachSegmentTypes[0],
								param.BeachSegmentTypes[0])
									.ExtendEdge(4))
					.ToList();
			}

			var landCoastWater = terraformer.PaintLoopsAndFill(
				coastTilingRandom,
				coastPaths,
				landPlan[0] ? Terraformer.Side.In : Terraformer.Side.Out,
				[new MultiBrush().WithTemplate(map, param.WaterTile, CVec.Zero)],
				null)
					?? throw new MapGenerationException("Could not fit tiles for coast");

			if (param.Mountains > 0)
			{
				var cliffMask = MatrixUtils.CalibratedBooleanThreshold(
					roughnessMatrix,
					param.Roughness, FractionMax);
				var cliffPlan = Matrix<bool>.Zip(landPlan, mapShape, (a, b) => a && b);

				for (var altitude = 0; altitude < param.MaximumAltitude; altitude++)
				{
					if (realAltitude != null)
					{
						// Relief réel : chaque palier suit une vraie courbe de niveau.
						if (altitude >= RealCliffAltitudes.Length)
							break;

						var room = MatrixUtils.ChebyshevRoom(cliffPlan, true);
						var threshold = RealCliffAltitudes[altitude];
						var minimumRoom = param.MinimumTerrainContourSpacing + 1;
						var previous = cliffPlan;
						cliffPlan = new Matrix<bool>(previous.Size);
						for (var n = 0; n < cliffPlan.Data.Length; n++)
							cliffPlan[n] = previous[n] && realAltitude[n] >= threshold && room[n] >= minimumRoom;
					}
					else
						cliffPlan = terraformer.SliceElevation(
							elevation,
							cliffPlan,
							param.Mountains,
							param.MinimumTerrainContourSpacing);
					cliffPlan = MatrixUtils.BooleanBlotch(
						cliffPlan,
						param.TerrainSmoothing,
						param.SmoothingThreshold, /*smoothingThresholdOutOf=*/FractionMax,
						param.MinimumMountainThickness,
						/*bias=*/false);
					var unmaskedCliffs = MatrixUtils.BordersToPoints(cliffPlan);
					var maskedCliffs = MatrixUtils.MaskPathPoints(unmaskedCliffs, cliffMask);
					var cliffs = CellLayerUtils.FromMatrixPoints(maskedCliffs, map.Tiles)
						.Where(cliff => cliff.Length >= param.MinimumCliffLength).ToArray();
					if (cliffs.Length == 0)
						break;
					foreach (var cliff in cliffs)
					{
						var cliffPath = TilingPath.QuickCreate(
							map,
							param.SegmentedBrushes,
							cliff,
							(param.MinimumMountainThickness - 1) / 2,
							param.CliffSegmentTypes[0],
							param.ClearSegmentTypes[0])
								.ExtendEdge(4);
						var brush = cliffPath.Tile(cliffTilingRandom)
							?? throw new MapGenerationException("Could not fit tiles for cliffs");
						terraformer.PaintTiling(pickAnyRandom, brush);
					}
				}
			}

			if (param.Forests > 0)
			{
				var space = terraformer.CheckSpace(param.ClearTerrain);
				var passages = terraformer.PlanPassages(
					topologyRandom,
					terraformer.ImproveSymmetry(space, true, (a, b) => a && b),
					param.ForestCutout,
					param.MaximumCutoutSpacing);
				var forestNoise = terraformer.BooleanNoise(
					forestRandom,
					param.ForestFeatureSize,
					param.Forests,
					param.ForestClumpiness);
				var replace = PlayableToReplaceable();
				foreach (var mpos in map.AllCells.MapCoords)
					if (!forestNoise[mpos] || !space[mpos] || passages[mpos])
						replace[mpos] = MultiBrush.Replaceability.None;
				terraformer.PaintArea(forestTilingRandom, replace, param.ForestObstacles);
			}

			if (param.EnforceSymmetry != 0)
			{
				var asymmetries = terraformer.FindAsymmetries(param.DominantTerrain, true, param.EnforceSymmetry == 2);
				terraformer.PaintActors(symmetryTilingRandom, asymmetries, param.ForestObstacles);
			}

			CellLayer<bool> playable;
			{
				// For circle-in-mountains, the outside is unplayable and should never count as
				// the largest/preferred region.
				CellLayer<bool> poison = null;
				if (param.ExternalCircularBias > 0)
					poison = terraformer.CenteredCircle(
						false, true, CellLayerUtils.Radius(map.Tiles) - new WDist(1024));

				playable = terraformer.ChoosePlayableRegion(
					terraformer.CheckSpace(param.PlayableTerrain, true, false, true),
					poison)
						?? throw new MapGenerationException("could not find a playable region");

				var minimumPlayableSpace = (int)(param.Players * Math.PI * param.SpawnBuildSize * param.SpawnBuildSize);
				if (playable.Count(p => p) < minimumPlayableSpace)
					throw new MapGenerationException("playable space is too small");

				if (param.DenyWalledAreas)
				{
					// Coast tiles are particularly problematic. If they're for unplayable bodies
					// of water, they should be obliterated. If they're just surrounded by rocks,
					// trees, etc, they should be filled in with actors.
					// Région réelle : les mers intérieures (Méditerranée, golfe du Mexique...)
					// restent de l'eau même si leur détroit est trop fin pour les navires.
					if (waterIsPlayable && realAltitude == null)
					{
						var mask = CellLayerUtils.Clone(playable);
						terraformer.ZoneFromOutOfBounds(mask, true);
						terraformer.FillUnmaskedSideAndBorder(
							mask,
							landCoastWater,
							Terraformer.Side.Out,
							cpos => map.Tiles[cpos] = terraformer.PickTile(pickAnyRandom, param.LandTile));
					}

					var replace = PlayableToReplaceable();
					foreach (var mpos in map.AllCells.MapCoords)
						if (playable[mpos] || !map.Bounds.Contains(mpos.U, mpos.V))
							replace[mpos] = MultiBrush.Replaceability.None;

					terraformer.PaintArea(debrisTilingRandom, replace, param.UnplayableObstacles);
				}
			}

			if (param.Roads)
			{
				// TODO: Move or collapse into configuration
				const int RoadMinimumShrinkLength = 12;
				const int RoadStraightenShrink = 4;
				const int RoadStraightenGrow = 2;
				const int RoadInertialRange = 8;

				var roadPaths = terraformer.PlanRoads(
					terraformer.CheckSpace(param.ClearTerrain, true, false),
					param.RoadSpacing,
					RoadMinimumShrinkLength + 2 * (RoadStraightenShrink + param.RoadShrink));
				foreach (var roadPath in roadPaths)
				{
					var tilingPath = TilingPath.QuickCreate(
						map,
						param.SegmentedBrushes,
						roadPath,
						param.RoadSpacing - 1,
						param.RoadSegmentTypes[0],
						param.ClearSegmentTypes[0])
							.StraightenEnds(
								RoadStraightenShrink + param.RoadShrink,
								RoadStraightenGrow,
								RoadMinimumShrinkLength,
								RoadInertialRange)
							.RetainIfValid();
					if (tilingPath.Points == null)
						continue;

					// Une route impossible à poser est abandonnée au lieu de faire échouer la carte.
					var brush = tilingPath.Tile(roadTilingRandom);
					if (brush == null)
						continue;

					terraformer.PaintTiling(pickAnyRandom, brush);
				}
			}

			var bridgeLandings = realBridges != null && realBridges.Count > 0
				? PlaceBridges(map, (ITemplatedTerrainInfo)terrainInfo, actorPlans, realBridges, param.BridgeActorNS, param.BridgeActorEW)
				: [];

			if (param.CreateEntities)
			{
				var zoneable = terraformer.GetZoneable(param.ZoneableTerrain, playable);

				// Rien ne doit boucher l'accès aux ponts.
				foreach (var landing in bridgeLandings)
					for (var dy = -2; dy <= 2; dy++)
						for (var dx = -2; dx <= 2; dx++)
						{
							var c = landing + new CVec(dx, dy);
							if (zoneable.Contains(c))
								zoneable[c] = false;
						}

				var zoneableArea = zoneable.Count(v => v);
				var symmetryCount = Symmetry.RotateAndMirrorProjectionCount(param.Rotations, param.Mirror);
				var entityMultiplier =
					(long)zoneableArea * param.AreaEntityBonus +
					(long)param.Players * param.PlayerCountEntityBonus;
				var perSymmetryEntityMultiplier = entityMultiplier / symmetryCount;

				// Spawn generation
				var symmetryPlayers = param.Players / symmetryCount;
				for (var iteration = 0; iteration < symmetryPlayers; iteration++)
				{
					var chosenCPos = terraformer.ChooseSpawnInZoneable(
						playerRandom,
						zoneable,
						param.CentralSpawnReservationFraction,
						param.MinimumSpawnRadius,
						param.SpawnRegionSize,
						param.SpawnReservation)
							?? throw new MapGenerationException("Not enough room for player spawns");

					var spawn = new ActorPlan(map, "mpspawn")
					{
						Location = chosenCPos,
					};

					var resourceSpawnPreferences = terraformer.TargetWalkingDistance(
						terraformer.CheckSpace(param.PlayableTerrain, true),
						terraformer.ErodeZones(zoneable, 1),
						[chosenCPos],
						new WDist((param.SpawnBuildSize + param.SpawnRegionSize * 2) * 512),
						new WDist(param.SpawnRegionSize * 1024));
					terraformer.AddDistributedActors(
						playerRandom,
						zoneable,
						resourceSpawnPreferences,
						param.ResourceSpawnWeights,
						param.SpawnResourceSpawns,
						false,
						new WDist(param.ResourceSpawnReservation * 1024));

					terraformer.ProjectPlaceDezoneActor(spawn, zoneable, new WDist(param.SpawnReservation * 1024));
				}

				// Expansions
				{
					var resourceSpawnsRemaining = (int)(param.MaximumExpansionResourceSpawns * perSymmetryEntityMultiplier / EntityBonusMax);
					while (resourceSpawnsRemaining > 0)
					{
						var added = terraformer.AddActorCluster(
							expansionRandom,
							zoneable,
							param.ResourceSpawnWeights,
							Math.Min(resourceSpawnsRemaining, expansionRandom.Next(param.MaximumResourceSpawnsPerExpansion) + 1),
							param.ExpansionInner,
							param.MinimumExpansionSize,
							param.MaximumExpansionSize,
							param.ExpansionBorder,
							true,
							new WDist(param.ResourceSpawnReservation * 1024));
						resourceSpawnsRemaining -= added;
						if (added == 0)
							break;
					}
				}

				// Neutral buildings
				{
					// Nombre exact de derricks choisi : « Bâtiments tech. » n'en place plus.
					var neutralWeights = param.OilDerricks > 0
						? (IReadOnlyDictionary<string, int>)param.BuildingWeights.Where(kv => kv.Key != param.OilDerrickActor).ToDictionary(kv => kv.Key, kv => kv.Value)
						: param.BuildingWeights;
					var (buildingTypes, buildingWeights) = Terraformer.SplitDictionary(neutralWeights);
					var targetBuildingCount =
						(param.MaximumBuildings != 0)
							? buildingRandom.Next(
								(int)(param.MinimumBuildings * perSymmetryEntityMultiplier / EntityBonusMax),
								(int)(param.MaximumBuildings * perSymmetryEntityMultiplier / EntityBonusMax) + 1)
							: 0;
					if (buildingTypes.Length > 0)
						for (var i = 0; i < targetBuildingCount; i++)
							terraformer.AddActor(
								buildingRandom,
								zoneable,
								buildingTypes[buildingRandom.PickWeighted(buildingWeights)]);
				}

				// Oil derricks : nombre choisi (arrondi au multiple de la symétrie),
				// autant que la place le permet.
				if (param.OilDerricks > 0)
				{
					var perSymmetry = Math.Max(1, (param.OilDerricks + symmetryCount / 2) / symmetryCount);
					var placed = 0;

					// D'abord espacés de 2 cases, puis collés si la place manque.
					foreach (var spacing in new[] { new WDist(2048), WDist.Zero })
						while (placed < perSymmetry && terraformer.AddActor(oilRandom, zoneable, param.OilDerrickActor, spacing))
							placed++;
				}

				// Grow resources
				var targetResourceValue = param.ResourcesPerPlayer * entityMultiplier / EntityBonusMax;
				if (targetResourceValue > 0)
				{
					var resourcePattern = terraformer.ResourceNoise(
						resourceRandom,
						param.ResourceFeatureSize,
						param.OreClumpiness,
						param.OreUniformity * 1024 / FractionMax);

					var resourceBiases = new List<Terraformer.ResourceBias>();
					var wSpawnBuildSizeSq = (long)param.SpawnBuildSize * param.SpawnBuildSize * 1024 * 1024;

					// Bias towards resource spawns
					foreach (var (actorType, resourceType) in param.ResourceSpawnSeeds.OrderBy(kv => kv.Key))
					{
						resourceBiases.AddRange(
							terraformer.ActorsOfType(actorType)
								.Select(a => new Terraformer.ResourceBias(a)
								{
									BiasRadius = new WDist(16 * 1024),
									Bias = (value, rSq) => value + (int)(1024 * 1024 / (1024 + Exts.ISqrt(rSq))),
									ResourceType = resourceType,
								}));
					}

					// Bias towards player spawns, but also reserve an area for base building.
					resourceBiases.AddRange(
						terraformer.ActorsOfType("mpspawn")
							.Select(a => new Terraformer.ResourceBias(a)
							{
								ExclusionRadius = new WDist(param.SpawnBuildSize * 1024),
								BiasRadius = new WDist(param.SpawnRegionSize * 2 * 1024),
								Bias = (value, rSq) => value + (int)(value * param.SpawnResourceBias * wSpawnBuildSizeSq / Math.Max(rSq, 1024 * 1024) / FractionMax),
							}));

					var (plan, typePlan) = terraformer.PlanResources(
						resourcePattern,
						CellLayerUtils.Intersect([playable, terraformer.CheckSpace(null, true)]),
						param.DefaultResource,
						resourceBiases);
					terraformer.GrowResources(
						plan,
						typePlan,
						targetResourceValue);
					terraformer.ZoneFromResources(zoneable, false);
				}

				// CivilianBuildings
				if (param.CivilianBuildings > 0)
				{
					var decorationNoise = terraformer.DecorationPattern(
						decorationRandom,
						terraformer.CheckSpace(param.PlayableTerrain, true),
						CellLayerUtils.Intersect([zoneable, terraformer.CheckSpace(param.LandTile)]),
						param.CivilianBuildings,
						param.CivilianBuildingsFeatureSize,
						param.CivilianBuildingDensity,
						param.MinimumCivilianBuildingDensity,
						param.CivilianBuildingDensityRadius);
					terraformer.PaintActors(
						decorationTilingRandom,
						decorationNoise,
						param.CivilianBuildingsObstacles,
						alwaysPreferLargerBrushes: true);
				}
			}

			// Cosmetically repaint tiles
			terraformer.RepaintTiles(repaintRandom, param.RepaintTiles);

			if (realAltitude != null)
				ConnectLandAreas(map, terrainInfo, actorPlans, bridgeLandings, param.BridgeActorNS, param.BridgeActorEW,
					() => terraformer.PickTile(pickAnyRandom, param.LandTile));

			terraformer.ReorderPlayerSpawns();
			terraformer.BakeMap();

			return map;
		}

		/// <summary>
		/// Lissage des côtes réelles : un léger lissage majoritaire, puis on retire
		/// (sans jamais épaissir) les terres et mers plus fines que <paramref name="thickness"/>,
		/// et on supprime les contacts en diagonale terre/mer que les tuiles ne savent pas dessiner.
		/// </summary>
		static Matrix<bool> RealBlotch(Matrix<bool> input, int thickness)
		{
			var (matrix, _) = MatrixUtils.BooleanBlur(input, 1, 1, 2);
			var minimumArea = thickness * thickness * 2;
			for (var pass = 0; pass < 32; pass++)
			{
				int changes, total = 0;
				(matrix, changes) = MatrixUtils.RetainThickRegions(matrix, true, thickness);
				total += changes;
				(matrix, changes) = MatrixUtils.RetainThickRegions(matrix, false, thickness);
				total += changes;
				total += RemoveSmallRegions(matrix, minimumArea);

				// Motifs 10/01 ou 01/10 : on change une seule case.
				for (var y = 0; y + 1 < matrix.Size.Y; y++)
					for (var x = 0; x + 1 < matrix.Size.X; x++)
					{
						var a = matrix[x, y];
						if (a == matrix[x + 1, y + 1] && matrix[x + 1, y] == matrix[x, y + 1] && a != matrix[x + 1, y])
						{
							matrix[x + 1, y] = a;
							total++;
						}
					}

				if (total == 0)
					break;
			}

			return matrix;
		}

		/// <summary>Îles et lacs trop petits pour les tuiles de rivage : absorbés par leur entourage.</summary>
		static int RemoveSmallRegions(Matrix<bool> matrix, int minimumArea)
		{
			var seen = new Matrix<bool>(matrix.Size);
			var changes = 0;
			var queue = new Queue<int2>();
			var region = new List<int2>();
			var dirs = new[] { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1) };
			for (var y = 0; y < matrix.Size.Y; y++)
				for (var x = 0; x < matrix.Size.X; x++)
				{
					if (seen[x, y])
						continue;

					var value = matrix[x, y];
					region.Clear();
					queue.Enqueue(new int2(x, y));
					seen[x, y] = true;
					var touchesEdge = false;
					while (queue.Count > 0)
					{
						var c = queue.Dequeue();
						region.Add(c);
						touchesEdge |= matrix.IsEdge(c);
						foreach (var d in dirs)
						{
							var n = c + d;
							if (matrix.ContainsXY(n) && !seen[n] && matrix[n] == value)
							{
								seen[n] = true;
								queue.Enqueue(n);
							}
						}
					}

					if (region.Count < minimumArea && !touchesEdge)
					{
						foreach (var c in region)
							matrix[c] = !value;
						changes += region.Count;
					}
				}

			return changes;
		}

		/// <summary>
		/// Pose les ponts d'une région réelle : chaque pont (horizontal ou vertical, 2 cases
		/// de large) reçoit un tablier sur toute l'eau entre la première et la dernière
		/// terre ferme de son tracé. Renvoie les cases d'arrivée.
		/// </summary>
		static List<CPos> PlaceBridges(Map map, ITemplatedTerrainInfo terrainInfo, List<ActorPlan> actorPlans,
			List<(int2 From, int2 To)> bridges, string actorNS, string actorEW)
		{
			var landingTypes = BridgeLandingTerrain
				.Where(t => terrainInfo.TerrainTypes.Any(tt => tt.Type == t))
				.Select(terrainInfo.GetTerrainIndex)
				.ToHashSet();
			bool IsLanding(CPos c) => landingTypes.Contains(terrainInfo.GetTerrainIndex(map.Tiles[c]));

			var landings = new List<CPos>();
			var deck = new HashSet<CPos>();
			foreach (var (from, to) in bridges)
			{
				var eastWest = Math.Abs(to.X - from.X) >= Math.Abs(to.Y - from.Y);
				var type = eastWest ? actorEW : actorNS;
				var length = eastWest ? Math.Abs(to.X - from.X) : Math.Abs(to.Y - from.Y);
				var step = eastWest ? new CVec(Math.Sign(to.X - from.X), 0) : new CVec(0, Math.Sign(to.Y - from.Y));
				var side = eastWest ? new CVec(0, 1) : new CVec(1, 0);

				// La côte générée ne suit pas exactement la vraie : chaque bout du pont
				// s'accroche à la terre praticable la plus proche, un peu avant ou après,
				// et une rangée qui ne relie rien est décalée sur le côté.
				List<CPos> Lane(int offset)
				{
					var cells = Enumerable.Range(-BridgeLandingSearch, length + 1 + 2 * BridgeLandingSearch)
						.Select(i => new CPos(from.X, from.Y) + side * offset + step * i)
						.ToList();

					// Le départ reste dans la première moitié du tracé et l'arrivée dans la
					// seconde : un pont ne doit pas relier une terre à elle-même.
					int Nearest(int target, int min, int max)
					{
						for (var d = 0; d <= BridgeLandingSearch; d++)
							foreach (var i in new[] { target - d, target + d })
								if (i >= min && i <= max && map.Tiles.Contains(cells[i]) && IsLanding(cells[i]))
									return i;

						return -1;
					}

					var middle = BridgeLandingSearch + length / 2;
					var first = Nearest(BridgeLandingSearch, 0, middle);
					var last = Nearest(BridgeLandingSearch + length, middle + 1, cells.Count - 1);
					return first < 0 || last <= first + 1 ? null : cells.GetRange(first, last - first + 1);
				}

				var lanes = new List<List<CPos>>();
				for (var shift = 0; shift <= 2 * BridgeShiftSearch; shift++)
				{
					var offset = shift % 2 == 0 ? shift / 2 : -(shift + 1) / 2;
					var found = new[] { Lane(offset), Lane(offset + 1) }.Where(l => l != null).ToList();
					if (found.Count > lanes.Count)
						lanes = found;

					if (lanes.Count == 2)
						break;
				}

				foreach (var cells in lanes)
				{
					landings.Add(cells[0]);
					landings.Add(cells[^1]);
					foreach (var c in cells)
						if (!IsLanding(c) && deck.Add(c))
							actorPlans.Add(new ActorPlan(map, type) { Location = c });
				}
			}

			// Les arbres ou rochers qui bouchent l'accès au tablier sont retirés.
			var clear = new HashSet<CPos>(deck.SelectMany(c =>
				Enumerable.Range(-1, 3).SelectMany(dy => Enumerable.Range(-1, 3).Select(dx => c + new CVec(dx, dy)))));
			actorPlans.RemoveAll(a => a.Info.Name != actorNS && a.Info.Name != actorEW
				&& a.Info.Name != "mpspawn" && a.Footprint().Keys.Any(clear.Contains));

			return landings;
		}

		/// <summary>
		/// Région réelle : les forêts et les décors sont posés sans connaître les ponts ni la
		/// forme des continents, et peuvent enfermer un bout de pont ou couper une terre en deux.
		/// On relie alors chaque bout de pont, puis chaque grande zone praticable, à la plus
		/// grande zone en retirant les obstacles (arbres, rochers...) du chemin le plus court.
		/// Les mers et les falaises ne sont jamais franchies.
		/// </summary>
		static void ConnectLandAreas(Map map, ITerrainInfo terrainInfo, List<ActorPlan> actorPlans,
			List<CPos> landings, string actorNS, string actorEW, Func<TerrainTile> clearTile)
		{
			var passable = BridgeLandingTerrain
				.Where(t => terrainInfo.TerrainTypes.Any(tt => tt.Type == t))
				.Select(terrainInfo.GetTerrainIndex)
				.ToHashSet();
			bool IsKept(ActorPlan a) => a.Info.Name == actorNS || a.Info.Name == actorEW || a.Info.Name == "mpspawn";
			var deck = actorPlans
				.Where(a => a.Info.Name == actorNS || a.Info.Name == actorEW)
				.Select(a => a.Location)
				.ToHashSet();
			bool Walkable(CPos c) => map.Tiles.Contains(c) && (deck.Contains(c) || passable.Contains(terrainInfo.GetTerrainIndex(map.Tiles[c])));

			// Les forêts peintes en tuiles se dégagent aussi : la case redevient de la terre.
			var forestTerrain = terrainInfo.TerrainTypes.Any(tt => tt.Type == "Tree") ? terrainInfo.GetTerrainIndex("Tree") : byte.MaxValue;
			bool IsForestTile(CPos c) => map.Tiles.Contains(c) && terrainInfo.GetTerrainIndex(map.Tiles[c]) == forestTerrain;
			var directions = new[] { new CVec(1, 0), new CVec(-1, 0), new CVec(0, 1), new CVec(0, -1) };

			// Points de départ déjà essayés sans succès (bout de pont ou première case d'une zone).
			var hopeless = new HashSet<CPos>();

			for (var attempt = 0; attempt < LandAreaMaximumAttempts; attempt++)
			{
				var obstacles = new Dictionary<CPos, List<ActorPlan>>();
				foreach (var a in actorPlans)
				{
					if (IsKept(a))
						continue;

					foreach (var c in a.Footprint().Keys)
					{
						if (!obstacles.TryGetValue(c, out var list))
							obstacles[c] = list = [];
						list.Add(a);
					}
				}

				// Zones praticables en tenant compte des obstacles.
				var zone = new Dictionary<CPos, int>();
				var zoneCells = new List<List<CPos>>();
				foreach (var start in map.AllCells)
				{
					if (zone.ContainsKey(start) || !Walkable(start) || obstacles.ContainsKey(start))
						continue;

					var cells = new List<CPos> { start };
					zone[start] = zoneCells.Count;
					for (var i = 0; i < cells.Count; i++)
					{
						foreach (var d in directions)
						{
							var n = cells[i] + d;
							if (!zone.ContainsKey(n) && Walkable(n) && !obstacles.ContainsKey(n))
							{
								zone[n] = zoneCells.Count;
								cells.Add(n);
							}
						}
					}

					zoneCells.Add(cells);
				}

				if (zoneCells.Count == 0)
					return;

				var largest = Enumerable.Range(0, zoneCells.Count).MaxBy(i => zoneCells[i].Count);
				bool IsOpen(CPos c) => zone.TryGetValue(c, out var id) && zoneCells[id].Count >= LandAreaMinimumSize;

				// D'abord les bouts de pont enfermés, puis les grandes zones isolées.
				List<CPos> sources;
				Func<CPos, bool> isGoal;
				var closedLanding = landings.Where(l => !hopeless.Contains(l) && !IsOpen(l)).Select(l => (CPos?)l).FirstOrDefault();
				if (closedLanding != null)
				{
					sources = [closedLanding.Value];
					isGoal = IsOpen;
				}
				else
				{
					var isolated = zoneCells.FirstOrDefault(z => z.Count >= LandAreaMinimumSize && zone[z[0]] != largest && !hopeless.Contains(z[0]));
					if (isolated == null)
						return;

					sources = isolated;
					isGoal = c => zone.TryGetValue(c, out var id) && id == largest;
				}

				// Plus court chemin : une case encombrée coûte plus cher qu'une case libre.
				var cost = new Dictionary<CPos, int>();
				var from = new Dictionary<CPos, CPos>();
				var frontier = new PriorityQueue<CPos, int>();
				foreach (var s in sources)
				{
					cost[s] = 0;
					frontier.Enqueue(s, 0);
				}

				CPos? reached = null;
				while (frontier.TryDequeue(out var c, out var k))
				{
					if (k > cost[c])
						continue;

					if (isGoal(c))
					{
						reached = c;
						break;
					}

					if (k >= LandAreaMaximumPathCost)
						break;

					foreach (var d in directions)
					{
						var n = c + d;
						var forest = IsForestTile(n);
						if (!forest && !Walkable(n))
							continue;

						var nk = k + (forest || obstacles.ContainsKey(n) ? 5 : 1);
						if (!cost.TryGetValue(n, out var old) || nk < old)
						{
							cost[n] = nk;
							from[n] = c;
							frontier.Enqueue(n, nk);
						}
					}
				}

				var removed = new HashSet<ActorPlan>();
				var cleared = new List<CPos>();
				if (reached != null)
				{
					// Passage de 2 cases de large.
					for (var c = reached.Value; ; c = from[c])
					{
						foreach (var w in new[] { c, c + new CVec(1, 0), c + new CVec(0, 1), c + new CVec(1, 1) })
						{
							if (obstacles.TryGetValue(w, out var list))
								removed.UnionWith(list);

							if (IsForestTile(w))
								cleared.Add(w);
						}

						if (!from.ContainsKey(c))
							break;
					}
				}

				if (removed.Count == 0 && cleared.Count == 0)
				{
					hopeless.Add(sources[0]);
					continue;
				}

				actorPlans.RemoveAll(removed.Contains);
				foreach (var c in cleared)
					map.Tiles[c] = clearTile();
			}
		}

		public bool TryGenerateMetadata(ModData modData, MapGenerationArgs args, out MapPlayers players, out Dictionary<string, MiniYaml> ruleDefinitions)
		{
			try
			{
				var playerCount = FieldLoader.GetValue<int>("Players", args.Settings.NodeWithKey("Players").Value.Value);

				// Generated maps use the default ruleset
				ruleDefinitions = [];
				players = new MapPlayers(modData.DefaultRules, playerCount);

				return true;
			}
			catch
			{
				players = null;
				ruleDefinitions = null;
				return false;
			}
		}

		public override object Create(ActorInitializer init)
		{
			return new ClassicMapGenerator(init, this);
		}
	}

	public class ClassicMapGenerator
	{
		public ClassicMapGenerator(ActorInitializer init, ClassicMapGeneratorInfo info) { }
	}
}
