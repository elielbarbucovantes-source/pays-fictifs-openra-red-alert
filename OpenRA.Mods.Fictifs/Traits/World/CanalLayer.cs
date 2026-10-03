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
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Canals dug by the Advanced Tunneller (DigsCanals).",
		"A dug cell is a dry trench until it touches the sea: then every connected trench floods",
		"(its tile becomes water for good) and ground units standing in it drown.")]
	public class CanalLayerInfo : TraitInfo
	{
		[Desc("Terrain types that can be dug.")]
		public readonly HashSet<string> DiggableTerrainTypes = new() { "Clear", "Rough", "Road", "Ore", "Gems", "Beach" };

		[Desc("Terrain types of the water that floods the trenches.")]
		public readonly HashSet<string> FloodTerrainTypes = new() { "Water" };

		[Desc("Terrain type of a dry trench.")]
		public readonly string TrenchTerrainType = "Rough";

		[Desc("Image holding the trench sprites: 16 frames, indexed by the trench neighbours (N=1, E=2, S=4, W=8).")]
		public readonly string Image = "canal";

		[SequenceReference(nameof(Image))]
		public readonly string Sequence = "tranchee";

		[PaletteReference]
		public readonly string Palette = "terrain";

		[Desc("Damage types used to kill the units that drown.")]
		public readonly BitSet<DamageType> DrownDamageTypes = default;

		public override object Create(ActorInitializer init) { return new CanalLayer(init.Self, this); }
	}

	public class CanalLayer : IWorldLoaded, IRenderOverlay, ITickRender, INotifyActorDisposing
	{
		static readonly CVec[] Neighbours = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };

		public readonly CanalLayerInfo Info;
		readonly World world;
		readonly Map map;
		readonly HashSet<CPos> trenches = new();
		readonly HashSet<CPos> dirty = new();
		readonly byte trenchType;
		readonly HashSet<byte> floodTypes;
		readonly HashSet<byte> diggableTypes;

		// Tuile d'eau 1×1 du jeu de tuiles (aucune sur « Interior » : on ne creuse pas).
		readonly ushort waterTemplate;
		readonly bool hasWater;

		IResourceLayer resources;
		TerrainSpriteLayer render;
		ISpriteSequence sequence;
		PaletteReference palette;
		bool disposed;

		public CanalLayer(Actor self, CanalLayerInfo info)
		{
			Info = info;
			world = self.World;
			map = world.Map;

			var terrain = map.Rules.TerrainInfo;
			var trench = Indices(terrain, new HashSet<string> { info.TrenchTerrainType });
			trenchType = trench.FirstOrDefault();
			floodTypes = Indices(terrain, info.FloodTerrainTypes);
			diggableTypes = Indices(terrain, info.DiggableTerrainTypes);

			if (terrain is ITemplatedTerrainInfo templated)
			{
				var water = templated.Templates.Values
					.Where(t => t.Size.X == 1 && t.Size.Y == 1 && t.TilesCount == 1 && t.Contains(0) && floodTypes.Contains(t[0].TerrainType))
					.OrderBy(t => t.Id)
					.FirstOrDefault();

				if (water != null && trench.Count > 0)
				{
					waterTemplate = water.Id;
					hasWater = true;
				}
			}
		}

		// Types de terrain présents dans ce jeu de tuiles (les autres sont ignorés).
		static HashSet<byte> Indices(ITerrainInfo terrain, HashSet<string> types)
		{
			var set = new HashSet<byte>();
			for (var i = 0; i < terrain.TerrainTypes.Length; i++)
				if (types.Contains(terrain.TerrainTypes[i].Type))
					set.Add((byte)i);

			return set;
		}

		public bool IsTrench(CPos cell) { return trenches.Contains(cell); }

		public bool IsWater(CPos cell)
		{
			return map.Contains(cell) && floodTypes.Contains(map.GetTerrainIndex(cell));
		}

		static bool Blocks(Actor a)
		{
			return a.Info.HasTraitInfo<BuildingInfo>();
		}

		public bool CanDig(CPos cell)
		{
			if (!hasWater || !map.Contains(cell) || trenches.Contains(cell))
				return false;

			// Cases déjà modifiées par autre chose (îles du Mothership) : on n'y touche pas.
			if (map.CustomTerrain[cell] != byte.MaxValue)
				return false;

			if (!diggableTypes.Contains(map.GetTerrainIndex(cell)))
				return false;

			return !world.ActorMap.GetActorsAt(cell).Any(Blocks);
		}

		/// <summary>Creuse une case. Renvoie le nombre de cases envahies par la mer.</summary>
		public int Dig(CPos cell, Actor digger)
		{
			if (!CanDig(cell))
				return 0;

			trenches.Add(cell);
			map.CustomTerrain[cell] = trenchType;
			resources?.ClearResources(cell);
			MarkDirty(cell);

			// Toute la tranchée reliée à cette case : inondée si elle touche l'eau quelque part.
			var connected = new List<CPos> { cell };
			var seen = new HashSet<CPos> { cell };
			for (var i = 0; i < connected.Count; i++)
				foreach (var v in Neighbours)
					if (trenches.Contains(connected[i] + v) && seen.Add(connected[i] + v))
						connected.Add(connected[i] + v);

			if (!connected.Any(c => Neighbours.Any(v => IsWater(c + v))))
				return 0;

			Flood(connected, digger);
			return connected.Count;
		}

		// La mer envahit la tranchée.
		void Flood(List<CPos> flooded, Actor digger)
		{
			trenches.ExceptWith(flooded);
			foreach (var c in flooded)
			{
				map.CustomTerrain[c] = byte.MaxValue;
				map.Tiles[c] = new TerrainTile(waterTemplate, 0);
				MarkDirty(c);
			}

			world.AddFrameEndTask(w => Drown(flooded, digger));
		}

		void Drown(List<CPos> cells, Actor digger)
		{
			var attacker = digger != null && !digger.IsDead && digger.IsInWorld ? digger : world.WorldActor;
			foreach (var c in cells)
			{
				foreach (var a in world.ActorMap.GetActorsAt(c).ToList())
				{
					if (a.IsDead || !a.IsInWorld || a.Info.HasTraitInfo<AircraftInfo>())
						continue;

					var mobile = a.TraitOrDefault<Mobile>();
					if (mobile != null)
					{
						if (mobile.Locomotor.MovementCostForCell(c) == PathGraph.MovementCostForUnreachableCell)
							a.Kill(attacker, Info.DrownDamageTypes);
					}
					else if (!Blocks(a))
					{
						// Épaves, caisses, mines : emportées par l'eau.
						if (a.Info.HasTraitInfo<IHealthInfo>())
							a.Kill(attacker, Info.DrownDamageTypes);
						else
							a.Dispose();
					}
				}
			}
		}

		void MarkDirty(CPos cell)
		{
			dirty.Add(cell);
			foreach (var v in Neighbours)
				dirty.Add(cell + v);
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			resources = w.WorldActor.TraitOrDefault<IResourceLayer>();
			sequence = map.Sequences.GetSequence(Info.Image, Info.Sequence);
			var first = sequence.GetSprite(0);
			var empty = new Sprite(first.Sheet, Rectangle.Empty, TextureChannel.Alpha);
			render = new TerrainSpriteLayer(w, wr, empty, first.BlendMode, w.Type != WorldType.Editor);
			palette = wr.Palette(Info.Palette);
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (dirty.Count == 0)
				return;

			var done = new List<CPos>();
			foreach (var c in dirty)
			{
				if (!map.Contains(c))
				{
					done.Add(c);
					continue;
				}

				// Comme les cratères : ce qui se passe sous le brouillard n'apparaît qu'une fois revu.
				if (world.FogObscures(c))
					continue;

				if (trenches.Contains(c))
				{
					var mask = 0;
					for (var i = 0; i < Neighbours.Length; i++)
						if (trenches.Contains(c + Neighbours[i]))
							mask |= 1 << i;

					render.Update(c, sequence, palette, mask);
				}
				else
					render.Clear(c);

				done.Add(c);
			}

			foreach (var c in done)
				dirty.Remove(c);
		}

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			render?.Draw(wr.Viewport);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			render?.Dispose();
			disposed = true;
		}
	}
}
