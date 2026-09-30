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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Engineered Tsunami (Australouis) : une vague part du large et balaie une bande
	// de terre de Width cases sur Depth cases, dans la direction choisie par le joueur.
	// Dégâts en pourcentage des PV max, pleins au rivage et décroissants vers l'intérieur.
	// La vague ne modifie jamais le terrain : les îles du Mothership n'ont rien à craindre.

	[Desc("Directional support power: a wave born at sea sweeps a band of land (Width x Depth cells).",
		"The target must be a coast: the cells just before the band must be mostly water.")]
	public class TsunamiPowerInfo : SupportPowerInfo
	{
		[Desc("Width of the band, in cells (across the wave direction).")]
		public readonly int Width = 10;

		[Desc("Depth of the band on land, in cells (along the wave direction).")]
		public readonly int Depth = 8;

		[Desc("Distance at sea from which the wave comes, in cells.")]
		public readonly int Offshore = 12;

		[Desc("Ticks between the launch and the moment the wave reaches the coast.")]
		public readonly int ImpactDelay = 250;

		[Desc("Wave speed.")]
		public readonly WDist Speed = new(96);

		[Desc("Terrain types counted as sea.")]
		public readonly HashSet<string> WaterTypes = new() { "Water" };

		[Desc("Depth of the strip checked in front of the coast, in cells.")]
		public readonly int CoastCheck = 4;

		[Desc("Minimum share of water (percent) in the strip in front of the band.")]
		public readonly int MinWaterPercent = 50;

		[Desc("Minimum share of land (percent) in the band itself.")]
		public readonly int MinLandPercent = 25;

		[Desc("Damage (percent of max HP) at the coast for units, per armor type.")]
		public readonly Dictionary<string, int> UnitDamage = new() { { "None", 100 }, { "Light", 75 }, { "Heavy", 55 } };

		[Desc("Damage for units whose armor type is not listed.")]
		public readonly int DefaultUnitDamage = 75;

		[Desc("Damage (percent of max HP) at the coast for buildings, per armor type.")]
		public readonly Dictionary<string, int> BuildingDamage = new() { { "Wood", 90 }, { "Concrete", 60 }, { "Heavy", 60 } };

		[Desc("Damage for buildings whose armor type is not listed.")]
		public readonly int DefaultBuildingDamage = 90;

		[Desc("Damage (percent of max HP) for ships and anything else floating on water.")]
		public readonly int ShipDamage = 5;

		[Desc("Cells from the coast where the wave keeps its full strength.")]
		public readonly int FullDamageDepth = 2;

		[Desc("Damage multiplier (percent) at the far end of the band; linear from 100 after FullDamageDepth.")]
		public readonly int FarEndDamage = 40;

		[Desc("Actors with one of these target types are destroyed outright (trees, walls).")]
		public readonly BitSet<TargetableType> DestroyTargetTypes = new("Trees", "Wall");

		[Desc("...unless they also have one of these target types.")]
		public readonly BitSet<TargetableType> SpareTargetTypes = new("Pipeline");

		[Desc("Ore and gems are washed away: share (percent) of the density removed at the coast.")]
		public readonly int ResourceRemoval = 100;

		public readonly BitSet<DamageType> DamageTypes = new("DefaultDeath");

		[Desc("Image of the wave crest.")]
		public readonly string WaveImage = "tsunami";

		[SequenceReference(nameof(WaveImage))]
		public readonly string WaveSequence = "crest";

		[PaletteReference]
		public readonly string WavePalette = "effect";

		[Desc("Sound played where the wave reaches the coast.")]
		public readonly string ImpactSound = null;

		[Desc("Sequences of the direction arrows while targeting.")]
		public readonly string DirectionArrowAnimation = "paradirection";

		[PaletteReference]
		public readonly string DirectionArrowPalette = "chrome";

		[Desc("Message shown to the caster when the target is not a coast.")]
		public readonly string NotCoastTextNotification = null;

		public override object Create(ActorInitializer init) { return new TsunamiPower(init.Self, this); }
	}

	public class TsunamiPower : SupportPower
	{
		public readonly TsunamiPowerInfo TsunamiInfo;

		public TsunamiPower(Actor self, TsunamiPowerInfo info)
			: base(self, info)
		{
			TsunamiInfo = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectTsunamiTarget(self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
			var facing = order.ExtraData <= 255 ? WAngle.FromFacing((int)order.ExtraData) : BestFacing(self.World, cell) ?? WAngle.Zero;
			Launch(self, cell, facing);
		}

		public void Launch(Actor self, CPos cell, WAngle facing)
		{
			var band = new TsunamiBand(self.World.Map, cell, facing, TsunamiInfo);
			self.World.AddFrameEndTask(w => w.Add(new TsunamiWave(self, band, TsunamiInfo)));
		}

		// Directions testées quand le joueur clique sans glisser : les 8 flèches.
		public WAngle? BestFacing(World world, CPos cell)
		{
			WAngle? best = null;
			var bestScore = -1;
			for (var i = 0; i < 8; i++)
			{
				var facing = new WAngle(i * 128);
				var band = new TsunamiBand(world.Map, cell, facing, TsunamiInfo);
				if (!band.IsValid(out var water, out var land))
					continue;

				if (water + land > bestScore)
				{
					bestScore = water + land;
					best = facing;
				}
			}

			return best;
		}
	}

	// Bande touchée : repère centré sur la case visée, « Along » dans le sens de la vague
	// (négatif en mer, 0 au rivage), « Across » en travers.
	public readonly struct TsunamiBand
	{
		public readonly WPos Origin;
		public readonly WVec Direction;
		public readonly WVec Side;
		public readonly WAngle Facing;
		readonly Map map;
		readonly TsunamiPowerInfo info;

		public TsunamiBand(Map map, CPos cell, WAngle facing, TsunamiPowerInfo info)
		{
			this.map = map;
			this.info = info;
			Facing = facing;
			Origin = map.CenterOfCell(cell);
			Direction = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(facing));
			Side = new WVec(1024, 0, 0).Rotate(WRot.FromYaw(facing));
		}

		public int HalfWidth => info.Width * 512;
		public int LandDepth => info.Depth * 1024;
		public int SeaDepth => info.Offshore * 1024;

		public int Along(WPos pos)
		{
			var v = pos - Origin;
			return (int)(((long)v.X * Direction.X + (long)v.Y * Direction.Y) / 1024);
		}

		public int Across(WPos pos)
		{
			var v = pos - Origin;
			return (int)(((long)v.X * Side.X + (long)v.Y * Side.Y) / 1024);
		}

		public bool InWidth(WPos pos) { return Math.Abs(Across(pos)) <= HalfWidth; }

		public WPos At(int along, int across)
		{
			return Origin + Direction * along / 1024 + Side * across / 1024;
		}

		public bool IsWater(CPos cell)
		{
			if (!map.Contains(cell))
				return false;

			return info.WaterTypes.Contains(map.Rules.TerrainInfo.TerrainTypes[map.GetTerrainIndex(cell)].Type);
		}

		// Toutes les cases dont le centre est dans l'intervalle [minAlong, maxAlong] de la bande.
		public IEnumerable<CPos> Cells(int minAlong, int maxAlong)
		{
			var reach = (Math.Max(Math.Abs(minAlong), Math.Abs(maxAlong)) + HalfWidth) / 1024 + 2;
			var center = map.CellContaining(Origin);
			for (var y = center.Y - reach; y <= center.Y + reach; y++)
			{
				for (var x = center.X - reach; x <= center.X + reach; x++)
				{
					var c = new CPos(x, y);
					if (!map.Contains(c))
						continue;

					var p = map.CenterOfCell(c);
					var a = Along(p);
					if (a >= minAlong && a <= maxAlong && InWidth(p))
						yield return c;
				}
			}
		}

		// Côte valide : surtout de l'eau juste avant la bande, et assez de terre dans la bande.
		public bool IsValid(out int waterPercent, out int landPercent)
		{
			waterPercent = landPercent = 0;
			if (!map.Contains(map.CellContaining(Origin)))
				return false;

			var front = Cells(-info.CoastCheck * 1024, -513).ToList();
			var band = Cells(-512, LandDepth).ToList();
			if (front.Count == 0 || band.Count == 0)
				return false;

			var self = this;
			waterPercent = 100 * front.Count(self.IsWater) / front.Count;
			landPercent = 100 * band.Count(c => !self.IsWater(c)) / band.Count;
			return waterPercent >= info.MinWaterPercent && landPercent >= info.MinLandPercent;
		}

		public WPos[] Outline(int minAlong, int maxAlong)
		{
			return new[]
			{
				At(minAlong, -HalfWidth), At(maxAlong, -HalfWidth),
				At(maxAlong, HalfWidth), At(minAlong, HalfWidth)
			};
		}
	}

	// La vague elle-même : invisible pendant l'alerte, puis une crête d'écume qui avance
	// et frappe tout ce qu'elle dépasse (une seule fois par acteur).
	public class TsunamiWave : IEffect
	{
		readonly Actor source;
		readonly Player owner;
		readonly TsunamiBand band;
		readonly TsunamiPowerInfo info;
		readonly Animation crest;
		readonly HashSet<Actor> hit = new();
		readonly IResourceLayer resources;
		readonly int spawnTick;
		int tick;
		int front;
		bool landed;

		public TsunamiWave(Actor source, TsunamiBand band, TsunamiPowerInfo info)
		{
			this.source = source;
			owner = source.Owner;
			this.band = band;
			this.info = info;
			resources = source.World.WorldActor.TraitOrDefault<IResourceLayer>();
			spawnTick = Math.Max(0, info.ImpactDelay - info.Offshore * 1024 / info.Speed.Length);
			front = -band.SeaDepth;
			crest = new Animation(source.World, info.WaveImage);
			crest.PlayRepeating(info.WaveSequence);
		}

		void IEffect.Tick(World world)
		{
			if (++tick < spawnTick)
				return;

			crest.Tick();
			var previous = front;
			front += info.Speed.Length;
			if (front > band.LandDepth)
			{
				world.AddFrameEndTask(w => w.Remove(this));
				return;
			}

			if (!landed && front >= 0)
			{
				landed = true;
				if (info.ImpactSound != null)
					Game.Sound.Play(SoundType.World, info.ImpactSound, band.Origin);
			}

			WashCells(previous, front);
			StrikeActors(world, previous, front);
		}

		int Falloff(int along)
		{
			var full = info.FullDamageDepth * 1024;
			if (along <= full)
				return 100;

			return 100 - (100 - info.FarEndDamage) * (Math.Min(along, band.LandDepth) - full) / Math.Max(1, band.LandDepth - full);
		}

		void WashCells(int from, int to)
		{
			if (resources == null || info.ResourceRemoval <= 0 || to < -512)
				return;

			foreach (var c in band.Cells(Math.Max(from + 1, -512), to))
			{
				var r = resources.GetResource(c);
				if (r.Type == null || r.Density <= 0)
					continue;

				var amount = (r.Density * info.ResourceRemoval * Falloff(band.Along(source.World.Map.CenterOfCell(c))) + 9999) / 10000;
				resources.RemoveResource(r.Type, c, amount);
			}
		}

		// Position de l'acteur pour la vague : son centre, ou pour un bâtiment la case
		// occupée la plus proche du large (un grand bâtiment est touché dès son bord).
		bool Reached(Actor a, int to, out int along)
		{
			along = int.MaxValue;
			if (a.OccupiesSpace is Building building)
			{
				foreach (var c in building.OccupiedCells())
				{
					var p = a.World.Map.CenterOfCell(c.Item1);
					if (band.InWidth(p))
						along = Math.Min(along, band.Along(p));
				}
			}
			else if (band.InWidth(a.CenterPosition))
				along = band.Along(a.CenterPosition);

			return along <= to && along >= -band.SeaDepth;
		}

		void StrikeActors(World world, int from, int to)
		{
			var mid = band.At((from + to) / 2, 0);
			var radius = new WDist(band.HalfWidth + 3 * 1024);
			var attacker = source.IsInWorld && !source.IsDead ? source : owner.PlayerActor;
			foreach (var a in world.FindActorsInCircle(mid, radius).ToList())
			{
				if (a.IsDead || !a.IsInWorld || hit.Contains(a) || !Reached(a, to, out var along))
					continue;

				// Aviation en vol : la vague passe dessous.
				if (world.Map.DistanceAboveTerrain(a.CenterPosition).Length > 128)
					continue;

				var health = a.TraitOrDefault<IHealth>();
				if (health == null)
					continue;

				hit.Add(a);
				var types = a.GetEnabledTargetTypes();
				if (types.Overlaps(info.DestroyTargetTypes) && !types.Overlaps(info.SpareTargetTypes))
				{
					a.Kill(attacker, info.DamageTypes);
					continue;
				}

				var percent = DamagePercent(a) * Falloff(along) / 100;
				var damage = (int)((long)health.MaxHP * percent / 100);
				if (damage > 0)
					a.InflictDamage(attacker, new Damage(damage, info.DamageTypes));
			}
		}

		int DamagePercent(Actor a)
		{
			if (band.IsWater(a.Location))
				return info.ShipDamage;

			var armor = a.TraitsImplementing<Armor>().FirstOrDefault(t => !t.IsTraitDisabled)?.Info.Type;
			if (a.OccupiesSpace is Building)
				return armor != null && info.BuildingDamage.TryGetValue(armor, out var b) ? b : info.DefaultBuildingDamage;

			return armor != null && info.UnitDamage.TryGetValue(armor, out var u) ? u : info.DefaultUnitDamage;
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr)
		{
			if (tick < spawnTick)
				yield break;

			var world = wr.World;
			for (var row = 0; row < 2; row++)
			{
				var along = front - row * 512;
				if (row > 0 && along < -band.SeaDepth)
					continue;

				for (var across = -band.HalfWidth; across <= band.HalfWidth; across += 512)
				{
					var pos = band.At(along, across + (row * 256));
					if (!world.Map.Contains(world.Map.CellContaining(pos)) || world.FogObscures(pos))
						continue;

					foreach (var r in crest.Render(pos, wr.Palette(info.WavePalette)))
						yield return r;
				}
			}
		}
	}
}
