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
	// Option de partie « Files de production », comme le multi-queue de Combined Arms :
	//  - Normale : une file par type et par joueur (RA classique, les usines en plus accélèrent) ;
	//  - Multi-file : chaque usine a sa propre file et produit en parallèle des autres.
	// Les deux jeux de files existent toujours et utilisent les mêmes types (Infantry, Vehicle...),
	// seul celui du mode choisi est actif : pas besoin de toucher aux Buildable des unités.

	[TraitLocation(SystemActors.Player)]
	[Desc("Lobby dropdown choosing between classic (per player) and multi-queue (per factory) unit production.")]
	public class ProductionModeOptionInfo : TraitInfo, ILobbyOptions, ITechTreePrerequisiteInfo
	{
		public const string OptionId = "queuetype";
		public const string Normal = "normal";
		public const string Multi = "multi";

		public readonly string Label = "Files de production";
		public readonly string Description = "Normale : une file par type, les usines en plus l'accélèrent.\nMulti-file : chaque usine a sa propre file (comme Combined Arms).";
		public readonly int DisplayOrder = 7;
		public readonly string Default = Normal;
		public readonly bool Locked = false;

		[Desc("Prerequisites granted to every player in multi-queue mode.")]
		public readonly HashSet<string> MultiQueuePrerequisites = new() { "global.multiqueue" };

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info) { return MultiQueuePrerequisites; }

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			var values = new Dictionary<string, string>
			{
				{ Normal, "Normale" },
				{ Multi, "Multi-file" },
			};

			yield return new LobbyOption(map, OptionId, Label, Description, true, DisplayOrder, values, Default, Locked);
		}

		public static bool IsMultiQueue(World world)
		{
			var info = world.Map.Rules.Actors[SystemActors.Player].TraitInfoOrDefault<ProductionModeOptionInfo>();
			if (info == null)
				return false;

			return world.LobbyInfo.GlobalSettings.OptionOrDefault(OptionId, info.Default) == Multi;
		}

		public override object Create(ActorInitializer init) { return new ProductionModeOption(init.World, this); }
	}

	public class ProductionModeOption : ITechTreePrerequisite
	{
		readonly IEnumerable<string> prerequisites;

		public ProductionModeOption(World world, ProductionModeOptionInfo info)
		{
			prerequisites = ProductionModeOptionInfo.IsMultiQueue(world) ? info.MultiQueuePrerequisites : Enumerable.Empty<string>();
		}

		IEnumerable<string> ITechTreePrerequisite.ProvidesPrerequisites => prerequisites;
	}

	[Desc("Per-player classic production queue, active only in the \"normal\" queue mode.")]
	public class NormalModeProductionQueueInfo : ClassicProductionQueueInfo
	{
		[Desc("Production types (Buildable.BuildAtProductionType) also served by this queue, e.g. Train in the Vehicle queue.")]
		public readonly HashSet<string> ExtraProductionTypes = new();

		public override object Create(ActorInitializer init) { return new NormalModeProductionQueue(init, this); }
	}

	public class NormalModeProductionQueue : ClassicProductionQueue
	{
		readonly bool active;
		readonly HashSet<string> extraTypes;

		public NormalModeProductionQueue(ActorInitializer init, NormalModeProductionQueueInfo info)
			: base(init, info)
		{
			active = !ProductionModeOptionInfo.IsMultiQueue(init.World);
			extraTypes = info.ExtraProductionTypes;

			// Désactivée avant Created : la file ne s'inscrit pas dans l'arbre technologique.
			if (!active)
				Enabled = false;
		}

		protected override void Tick(Actor self)
		{
			if (!active)
				return;

			if (extraTypes.Count == 0)
			{
				base.Tick(self);
				return;
			}

			// Comme ClassicProductionQueue.Tick, mais un chantier ferroviaire seul (type Train) active aussi la file.
			Enabled = false;
			var isActive = false;
			foreach (var x in self.World.ActorsWithTrait<Production>())
			{
				if (x.Trait.IsTraitDisabled || x.Actor.Owner != self.Owner)
					continue;

				if (!x.Trait.Info.Produces.Contains(Info.Type) && !x.Trait.Info.Produces.Any(extraTypes.Contains))
					continue;

				Enabled |= IsValidFaction;
				isActive |= !x.Trait.IsTraitPaused;
			}

			if (!Enabled)
				ClearQueue();

			TickInner(self, !isActive);
		}
	}

	[Desc("Per-factory production queue, active only in the \"multi\" queue mode",
		"and only on actors with a Production trait producing this Type. Safe to put on every building.")]
	public class MultiModeProductionQueueInfo : ProductionQueueInfo
	{
		[Desc("Production types (Buildable.BuildAtProductionType) also served by this queue, e.g. Train in the Vehicle queue.")]
		public readonly HashSet<string> ExtraProductionTypes = new();

		public override object Create(ActorInitializer init) { return new MultiModeProductionQueue(init, this); }
	}

	public class MultiModeProductionQueue : ProductionQueue
	{
		readonly bool active;
		readonly MultiModeProductionQueueInfo info;
		bool extraTraitsAdded;

		public MultiModeProductionQueue(ActorInitializer init, MultiModeProductionQueueInfo info)
			: base(init, info)
		{
			this.info = info;
			active = ProductionModeOptionInfo.IsMultiQueue(init.World)
				&& init.Self.Info.TraitInfos<ProductionInfo>().Any(p => Serves(p, info));

			if (!active)
				Enabled = false;
		}

		static bool Serves(ProductionInfo p, MultiModeProductionQueueInfo info)
		{
			return p.Produces.Contains(info.Type) || p.Produces.Any(info.ExtraProductionTypes.Contains);
		}

		protected override void Tick(Actor self)
		{
			if (!active)
				return;

			// ProductionQueue ne garde que les Production du type de la file : on ajoute celles des types en plus
			// (chantier ferroviaire : Train dans la file Véhicules).
			if (!extraTraitsAdded)
			{
				extraTraitsAdded = true;
				if (info.ExtraProductionTypes.Count > 0)
					productionTraits = self.TraitsImplementing<Production>().Where(p => Serves(p.Info, info)).ToArray();
			}

			base.Tick(self);
		}

		public override TraitPair<Production> MostLikelyProducer()
		{
			if (info.ExtraProductionTypes.Count == 0)
				return base.MostLikelyProducer();

			var traits = productionTraits.Where(p => !p.IsTraitDisabled && Serves(p.Info, info));
			var unpaused = traits.FirstOrDefault(a => !a.IsTraitPaused);
			return new TraitPair<Production>(Actor, unpaused ?? traits.FirstOrDefault());
		}

		// Une file d'usine ne montre que ce que cette usine sait sortir :
		// pas de MiG à l'héliport, pas de chien à la caserne, pas d'ICBM au chantier naval simple.
		bool CanProduceHere(ActorInfo actor)
		{
			if (developerMode.AllTech)
				return true;

			var type = actor.TraitInfo<BuildableInfo>().BuildAtProductionType ?? Info.Type;
			return productionTraits.Any(p => !p.IsTraitDisabled && p.Info.Produces.Contains(type));
		}

		public override IEnumerable<ActorInfo> AllItems()
		{
			return active ? base.AllItems().Where(CanProduceHere) : Array.Empty<ActorInfo>();
		}

		public override IEnumerable<ActorInfo> BuildableItems()
		{
			return active ? base.BuildableItems().Where(CanProduceHere) : Array.Empty<ActorInfo>();
		}
	}
}
