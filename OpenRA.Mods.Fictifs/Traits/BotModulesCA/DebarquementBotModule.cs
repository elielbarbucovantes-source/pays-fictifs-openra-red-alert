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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Débarquements de l'IA : seulement quand aucun ennemi n'est joignable par la terre (île, autre rive).
	// L'IA construit alors des transports amphibies de son pays (au besoin la Base logistique amphibie qui
	// les produit), choisit une plage chez elle et une plage près de l'ennemi, met ses transports en navette
	// amphibie entre les deux (NavetteAmphibie : chargement des unités au repos sur la plage, débarquement
	// étendu en face) et y envoie ses troupes au repos. Débarquées, elles attaquent ce qu'elles peuvent
	// atteindre à pied, puis reviennent au gestionnaire d'escouades.
	[TraitLocation(SystemActors.Player)]
	[Desc("AI amphibious landings when no enemy structure can be reached by land, using the amphibious shuttle.")]
	public class DebarquementBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Ticks between two checks.")]
		public readonly int ScanInterval = 250;

		[Desc("Number of transports the AI wants while it is cut off.")]
		public readonly int MaxTransports = 2;

		[Desc("Building that produces the amphibious transports of the countries without another shipyard for them.")]
		public readonly string AmphibiousBaseType = "base.amphibie";

		[Desc("Locomotor used to decide whether an enemy can be reached by land.")]
		public readonly string GroundLocomotor = "foot";

		[Desc("Never carried.")]
		public readonly HashSet<string> ExcludedTypes = new() { "harv", "mcv" };

		[Desc("Only idle units within this many cells of the base are sent to the beach.")]
		public readonly int HomeRadius = 30;

		[Desc("Half-size of the loading and landing beaches (cells).")]
		public readonly int BeachHalfSize = 2;

		[Desc("Search radius for the beaches (cells).")]
		public readonly int CoastSearchRadius = 40;

		[Desc("Preferred minimum distance between the landing beach and the targeted structure (cells).")]
		public readonly int LandingStandOff = 6;

		[Desc("Enemy defenses within this many cells of a landing beach make it less attractive.")]
		public readonly int DefenseRadius = 9;

		[Desc("Units waiting on the beach, as a multiple of the transport capacity.")]
		public readonly int WaitingFactor = 2;

		public override object Create(ActorInitializer init) { return new DebarquementBotModule(init.Self, this); }
	}

	public class DebarquementBotModule : ConditionalTrait<DebarquementBotModuleInfo>, IBotTick
	{
		enum Etat { VersPlage, ABord, Debarque }

		sealed class Suivi
		{
			public Etat Etat;
			public int Essais;
		}

		readonly World world;
		readonly Player player;
		readonly Dictionary<Actor, Suivi> unites = new();
		readonly HashSet<Actor> transports = new();

		IPathFinder pathFinder;
		Locomotor pied;
		SquadManagerBotModuleCA squads;
		ActorInfo[] typesTransport;
		int ticks, scans, demandesBase, attenteBase;
		Escale chargement, dechargement;
		Actor cible;

		public DebarquementBotModule(Actor self, DebarquementBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		protected override void Created(Actor self)
		{
			pathFinder = world.WorldActor.Trait<IPathFinder>();
			pied = world.WorldActor.TraitsImplementing<Locomotor>().FirstOrDefault(l => l.Info.Name == Info.GroundLocomotor);
			typesTransport = world.Map.Rules.Actors.Values
				.Where(a => a.HasTraitInfo<NavetteAmphibieInfo>() && a.HasTraitInfo<BuildableInfo>() && a.HasTraitInfo<MobileInfo>())
				.ToArray();

			base.Created(self);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (IsTraitDisabled || pied == null || --ticks > 0)
				return;

			ticks = Info.ScanInterval;
			scans++;
			squads ??= player.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>().FirstOrDefault(s => !s.IsTraitDisabled);

			Nettoyer();
			SuivreUnites(bot);

			var maison = Maison();
			var ennemis = world.ActorsHavingTrait<Building>()
				.Where(a => !a.IsDead && a.IsInWorld && player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy
					&& !a.Owner.NonCombatant && !a.Info.HasTraitInfo<LineBuildInfo>())
				.ToList();

			if (maison == null || ennemis.Count == 0 || ennemis.Any(e => pathFinder.PathExistsForLocomotor(pied, maison.Value, e.Location)))
			{
				Arreter(bot);
				return;
			}

			// Coupé de tous les ennemis : la cible est la base ennemie la plus proche.
			var proche = ennemis.MinBy(e => (e.Location - maison.Value).LengthSquared);
			if (cible == null || cible.IsDead || !cible.IsInWorld || cible != proche || scans % 8 == 0)
			{
				cible = proche;
				chargement = dechargement = null;
			}

			foreach (var t in world.ActorsHavingTrait<NavetteAmphibie>().Where(t => t.Owner == player && !t.IsDead && t.IsInWorld))
			{
				if (transports.Add(t))
					squads?.Reserve(t);
			}

			Produire(bot);
			if (transports.Count == 0)
				return;

			var navette = transports.First();
			var loco = navette.Trait<Mobile>().Locomotor;
			if (chargement == null && !Plages(maison.Value, loco))
				return;

			foreach (var t in transports)
			{
				var n = t.Trait<NavetteAmphibie>();
				if (n.Usable && !(n.Active && n.Load?.Serialize() == chargement.Serialize() && n.Unload?.Serialize() == dechargement.Serialize()))
					bot.QueueOrder(NavetteAmphibie.MakeOrder(t, chargement, dechargement, false));
			}

			EnvoyerTroupes(bot, maison.Value);
		}

		void Nettoyer()
		{
			transports.RemoveWhere(t => t.IsDead || t.Owner != player);
			foreach (var a in unites.Keys.Where(a => a.IsDead || a.Owner != player).ToList())
				unites.Remove(a);
		}

		// Centre de la base : le plus ancien chantier de construction, sinon le plus ancien bâtiment.
		CPos? Maison()
		{
			var batiments = world.ActorsHavingTrait<Building>()
				.Where(a => a.Owner == player && !a.IsDead && a.IsInWorld && !a.Info.HasTraitInfo<LineBuildInfo>())
				.OrderBy(a => a.ActorID)
				.ToList();

			var chantier = batiments.FirstOrDefault(a => a.Info.HasTraitInfo<BaseBuildingInfo>()) ?? batiments.FirstOrDefault();
			return chantier?.Location;
		}

		// Plus d'ennemi coupé de nous : les navettes vides s'arrêtent et rendent la main ; celles qui ont des
		// troupes à bord finissent leur traversée.
		void Arreter(IBot bot)
		{
			cible = null;
			chargement = dechargement = null;
			foreach (var t in transports.ToList())
			{
				if (!t.Trait<Cargo>().IsEmpty())
					continue;

				if (t.Trait<NavetteAmphibie>().Active)
					bot.QueueOrder(new Order("Stop", t, false));

				squads?.Release(t);
				transports.Remove(t);
			}

			foreach (var (a, s) in unites.Where(kv => kv.Value.Etat == Etat.VersPlage).ToList())
			{
				squads?.Release(a);
				unites.Remove(a);
			}
		}

		void Produire(IBot bot)
		{
			if (transports.Count >= Info.MaxTransports)
				return;

			var possibles = new List<(ProductionQueue Queue, ActorInfo Type)>();
			foreach (var type in typesTransport)
			{
				foreach (var file in AIUtils.FindQueues(player, type.TraitInfo<BuildableInfo>().Queue.FirstOrDefault() ?? ""))
				{
					if (file.AllQueued().Any(i => typesTransport.Any(t => t.Name == i.Item)))
						return;

					if (file.BuildableItems().Any(b => b.Name == type.Name))
						possibles.Add((file, type));
				}
			}

			if (possibles.Count > 0)
			{
				// Sans attendre d'avoir toute la somme : la production se paie au fur et à mesure (l'IA
				// dépense tout ce qu'elle gagne, elle n'aurait jamais le prix d'avance).
				var (q, t) = possibles.MinBy(p => p.Type.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0);
				AIUtilsCA.BotDebug("{0}: coupé de l'ennemi, construit un transport {1}", player, t.Name);
				bot.QueueOrder(Order.StartProduction(q.Actor, t.Name, 1));
				return;
			}

			// Aucun transport constructible : la Base logistique amphibie les produit (le gestionnaire de
			// construction la pose, sur la côte). Si elle n'a pas pu être posée, on attend avant de réessayer.
			if (world.Actors.Any(a => a.Owner == player && !a.IsDead && a.Info.Name.StartsWith(Info.AmphibiousBaseType, System.StringComparison.Ordinal)))
				return;

			if (attenteBase > 0)
			{
				attenteBase--;
				return;
			}

			foreach (var file in AIUtils.FindQueues(player, "Building"))
			{
				if (file.AllQueued().Any())
					continue;

				if (!file.BuildableItems().Any(b => b.Name == Info.AmphibiousBaseType))
					continue;

				if (++demandesBase % 3 == 0)
					attenteBase = 8;

				AIUtilsCA.BotDebug("{0}: coupé de l'ennemi, construit {1}", player, Info.AmphibiousBaseType);
				bot.QueueOrder(Order.StartProduction(file.Actor, Info.AmphibiousBaseType, 1));
				return;
			}
		}

		bool Praticable(Locomotor l, CPos c)
		{
			return world.Map.Contains(c) && l.MovementCostForCell(c) != PathGraph.MovementCostForUnreachableCell;
		}

		// Case d'eau voisine d'une case de terre, où le transport peut aller.
		CPos? EauVoisine(Locomotor loco, CPos c)
		{
			foreach (var v in CVec.Directions)
			{
				var w = c + v;
				if (Praticable(loco, w) && !Praticable(pied, w))
					return w;
			}

			return null;
		}

		// Plage de chargement près de la base, plage de débarquement près de la cible, reliées par l'eau.
		bool Plages(CPos maison, Locomotor loco)
		{
			var portee = System.Math.Min(Info.CoastSearchRadius, world.Map.Grid.MaximumTileSearchRange);
			var echecs = new List<CPos>();
			foreach (var c in world.Map.FindTilesInAnnulus(maison, 0, portee))
			{
				if (echecs.Count >= 6)
					break;

				if (!Praticable(pied, c) || !pathFinder.PathExistsForLocomotor(pied, c, maison))
					continue;

				var eau = EauVoisine(loco, c);
				if (eau == null || echecs.Any(e => pathFinder.PathExistsForLocomotor(loco, e, eau.Value)))
					continue;

				var arrivee = Debarquement(loco, eau.Value);
				if (arrivee == null)
				{
					echecs.Add(eau.Value);
					continue;
				}

				chargement = Zone(c);
				dechargement = Zone(arrivee.Value);
				AIUtilsCA.BotDebug("{0}: débarquement de {1} vers {2} ({3} de {4})", player, c, arrivee.Value, cible.Info.Name, cible.Owner);
				return true;
			}

			return false;
		}

		// Plage la moins défendue (défenses ennemies à portée de la côte), à distance de la cible, la plus
		// proche d'elle à égalité.
		CPos? Debarquement(Locomotor loco, CPos eauDepart)
		{
			var portee = System.Math.Min(Info.CoastSearchRadius, world.Map.Grid.MaximumTileSearchRange);
			var defenses = world.ActorsHavingTrait<AttackBase>()
				.Where(d => !d.IsDead && d.IsInWorld && d.Info.HasTraitInfo<BuildingInfo>() && player.RelationshipWith(d.Owner) == PlayerRelationship.Enemy)
				.Select(d => d.Location)
				.ToList();

			CPos? meilleure = null, repli = null;
			var meilleurScore = int.MaxValue;
			var vues = 0;
			foreach (var c in world.Map.FindTilesInAnnulus(cible.Location, 1, portee))
			{
				if (!Praticable(pied, c) || !pathFinder.PathExistsForLocomotor(pied, c, cible.Location))
					continue;

				var eau = EauVoisine(loco, c);
				if (eau == null || !pathFinder.PathExistsForLocomotor(loco, eau.Value, eauDepart))
					continue;

				if ((c - cible.Location).LengthSquared < Info.LandingStandOff * Info.LandingStandOff)
				{
					repli ??= c;
					continue;
				}

				var score = defenses.Count(d => (d - c).LengthSquared <= Info.DefenseRadius * Info.DefenseRadius);
				if (score < meilleurScore)
				{
					meilleurScore = score;
					meilleure = c;
					if (score == 0)
						break;
				}

				if (++vues >= 300)
					break;
			}

			return meilleure ?? repli;
		}

		Escale Zone(CPos c)
		{
			var s = Info.BeachHalfSize;
			var b = world.Map.Bounds;
			return new Escale(
				new CPos((c.X - s).Clamp(b.Left, b.Right - 1), (c.Y - s).Clamp(b.Top, b.Bottom - 1)),
				new CPos((c.X + s).Clamp(b.Left, b.Right - 1), (c.Y + s).Clamp(b.Top, b.Bottom - 1)));
		}

		// Troupes au repos près de la base, dans la limite de ce que les transports peuvent emporter.
		void EnvoyerTroupes(IBot bot, CPos maison)
		{
			var cargos = transports.Select(t => t.Info.TraitInfo<CargoInfo>()).ToList();
			var capacite = cargos.Sum(c => c.MaxWeight) * Info.WaitingFactor;
			var enAttente = unites.Where(kv => kv.Value.Etat == Etat.VersPlage).Sum(kv => kv.Key.Info.TraitInfo<PassengerInfo>().Weight);
			if (enAttente >= capacite)
				return;

			var plage = CasesPlage();
			if (plage.Count == 0)
				return;

			var candidats = world.ActorsHavingTrait<Passenger>()
				.Where(a => a.Owner == player && !a.IsDead && a.IsInWorld && a.IsIdle && !unites.ContainsKey(a)
					&& !transports.Contains(a) && !Info.ExcludedTypes.Contains(a.Info.Name)
					&& a.Info.HasTraitInfo<MobileInfo>() && a.Info.HasTraitInfo<AttackBaseInfo>()
					&& !a.Info.HasTraitInfo<AircraftInfo>() && !a.Info.HasTraitInfo<HarvesterInfo>()
					&& (a.Location - maison).LengthSquared <= Info.HomeRadius * Info.HomeRadius)
				.OrderBy(a => a.ActorID)
				.ToList();

			foreach (var a in candidats)
			{
				var pi = a.Info.TraitInfo<PassengerInfo>();
				if (!cargos.Any(c => c.Types.Contains(pi.CargoType) && c.MaxWeight >= pi.Weight))
					continue;

				var mobile = a.Trait<Mobile>();
				if (!pathFinder.PathExistsForLocomotor(mobile.Locomotor, a.Location, plage[0]))
					continue;

				squads?.Reserve(a);
				unites[a] = new Suivi { Etat = Etat.VersPlage };
				AllerALaPlage(bot, a, plage);

				enAttente += pi.Weight;
				if (enAttente >= capacite)
					break;
			}
		}

		List<CPos> CasesPlage()
		{
			if (chargement == null)
				return new List<CPos>();

			var cases = new List<CPos>();
			for (var y = chargement.TopLeft.Y; y <= chargement.BottomRight.Y; y++)
				for (var x = chargement.TopLeft.X; x <= chargement.BottomRight.X; x++)
				{
					var c = new CPos(x, y);
					if (Praticable(pied, c))
						cases.Add(c);
				}

			return cases;
		}

		void AllerALaPlage(IBot bot, Actor a, List<CPos> plage)
		{
			var c = plage[world.LocalRandom.Next(plage.Count)];
			bot.QueueOrder(new Order("Move", a, Target.FromCell(world, c), false));
		}

		void SuivreUnites(IBot bot)
		{
			var plage = CasesPlage();
			foreach (var (a, s) in unites.ToList())
			{
				switch (s.Etat)
				{
					case Etat.VersPlage:
						if (!a.IsInWorld)
							s.Etat = Etat.ABord;
						else if (a.IsIdle && (chargement == null || !chargement.Contains(a.Location)))
						{
							if (plage.Count == 0 || ++s.Essais > 3)
							{
								squads?.Release(a);
								unites.Remove(a);
							}
							else
								AllerALaPlage(bot, a, plage);
						}

						break;

					case Etat.ABord:
						if (!a.IsInWorld)
							break;

						if (chargement != null && chargement.Contains(a.Location))
						{
							// Laissée sur la plage au départ du transport : elle attend le suivant.
							s.Etat = Etat.VersPlage;
							s.Essais = 0;
							break;
						}

						s.Etat = Etat.Debarque;
						s.Essais = 0;
						Attaquer(bot, a, s);
						break;

					case Etat.Debarque:
						if (a.IsIdle)
							Attaquer(bot, a, s);

						break;
				}
			}
		}

		// Débarquée : attaque du bâtiment ennemi le plus proche qu'elle peut atteindre ; plus rien à
		// portée de pied, elle revient au gestionnaire d'escouades.
		void Attaquer(IBot bot, Actor a, Suivi s)
		{
			var loco = a.Trait<Mobile>().Locomotor;
			var proie = world.ActorsHavingTrait<Building>()
				.Where(e => !e.IsDead && e.IsInWorld && player.RelationshipWith(e.Owner) == PlayerRelationship.Enemy
					&& !e.Owner.NonCombatant && !e.Info.HasTraitInfo<LineBuildInfo>())
				.OrderBy(e => (e.Location - a.Location).LengthSquared)
				.Take(10)
				.FirstOrDefault(e => pathFinder.PathExistsForLocomotor(loco, a.Location, e.Location));

			if (proie == null || ++s.Essais > 20)
			{
				squads?.Release(a);
				unites.Remove(a);
				return;
			}

			bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(world, proie.Location), false));
		}
	}
}
