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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Traits.BotModules.Squads;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Attaques coordonnées (Desperate AI) : quand une escouade d'assaut part à l'attaque, ou qu'un débarquement
	// a des troupes en mer, son objectif devient l'objectif commun. Les escadrilles (hors chasseurs, qui gardent
	// la supériorité aérienne), la marine et les autres escouades d'assaut en marche y convergent, pour frapper
	// en même temps par la terre, l'air et la mer. L'objectif est gardé DureeObjectif ticks pour ne pas osciller.
	// Les escouades gardent leur propre jugement : fuite devant une DCA trop forte ou un combat perdu.
	[TraitLocation(SystemActors.Player)]
	[Desc("Coordinates the AI attacks: air, naval and other assault squads converge on the target of the latest assault or landing.")]
	public class OffensiveCoordonneeBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Ticks between two checks.")]
		public readonly int ScanInterval = 100;

		[Desc("Ticks during which a common objective is kept, unless it is destroyed or a landing starts.")]
		public readonly int DureeObjectif = 1500;

		[Desc("Naval squads attack the enemy actors within this many cells of the objective.")]
		public readonly int RayonNaval = 15;

		[ActorReference]
		[Desc("Air units left to air superiority (fighters): never sent to the objective.")]
		public readonly HashSet<string> ChasseursExclus = new();

		[Desc("Also send the other marching assault squads to the objective.")]
		public readonly bool RegrouperAssauts = true;

		public override object Create(ActorInitializer init) { return new OffensiveCoordonneeBotModule(init.Self, this); }
	}

	public class OffensiveCoordonneeBotModule : ConditionalTrait<OffensiveCoordonneeBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		readonly HashSet<SquadCA> assautsConnus = new();

		SquadManagerBotModuleCA squads;
		DebarquementBotModule debarquement;
		Actor objectif;
		int finObjectif, ticks;

		public OffensiveCoordonneeBotModule(Actor self, OffensiveCoordonneeBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		void IBotTick.BotTick(IBot bot)
		{
			AIUtilsCA.Securise(bot, "Offensive coordonnée", () => BotTickProtege(bot));
		}

		void BotTickProtege(IBot bot)
		{
			if (IsTraitDisabled || --ticks > 0)
				return;

			ticks = Info.ScanInterval;
			squads ??= player.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>().FirstOrDefault(s => !s.IsTraitDisabled);
			debarquement ??= player.PlayerActor.TraitsImplementing<DebarquementBotModule>().FirstOrDefault(d => !d.IsTraitDisabled);
			if (squads == null)
				return;

			assautsConnus.RemoveWhere(s => !squads.Squads.Contains(s));

			// Un débarquement avec des troupes en mer passe avant tout : l'appui doit arriver avec elles.
			Actor nouveau = null;
			var debarquementEnCours = debarquement != null && debarquement.TroupesEnMer && debarquement.Cible != null;
			if (debarquementEnCours)
				nouveau = debarquement.Cible;

			// Nouvelle escouade d'assaut partie à l'attaque (sa cible est choisie à son premier passage).
			SquadCA assautMeneur = null;
			foreach (var s in squads.Squads.Where(s => s.Type == SquadCAType.Assault && s.IsValid))
			{
				if (!Valide(s.TargetActor) || !assautsConnus.Add(s))
					continue;

				if (nouveau == null)
				{
					nouveau = s.TargetActor;
					assautMeneur = s;
				}
			}

			if (nouveau != null && nouveau != objectif && (!Valide(objectif) || debarquementEnCours || world.WorldTick >= finObjectif))
			{
				objectif = nouveau;
				finObjectif = world.WorldTick + Info.DureeObjectif;
				AIUtilsCA.BotDebug("{0}: attaque coordonnée sur {1} de {2} ({3})", player, objectif.Info.Name, objectif.Owner,
					debarquementEnCours ? "débarquement" : "assaut");
			}

			if (!Valide(objectif))
			{
				objectif = null;
				return;
			}

			foreach (var s in squads.Squads.ToList())
			{
				if (!s.IsValid || s == assautMeneur || s.TargetActor == objectif)
					continue;

				switch (s.Type)
				{
					case SquadCAType.Air:
						EnvoyerAviation(s);
						break;
					case SquadCAType.Naval:
						EnvoyerMarine(s);
						break;
					case SquadCAType.Assault:
						if (Info.RegrouperAssauts && s.FuzzyStateMachine.CurrentState is GroundUnitsAttackMoveStateCA)
							Viser(s, objectif, s.GetLeader());

						break;
				}
			}
		}

		bool Valide(Actor a)
		{
			return a != null && !a.IsDead && a.IsInWorld && player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy;
		}

		// Vise la cible si l'escouade peut l'atteindre (à portée de tir), sinon ne change rien.
		bool Viser(SquadCA s, Actor cible, Actor meneur)
		{
			if (meneur == null)
				return false;

			var t = squads.FindEnemies(new[] { cible }, meneur).FirstOrDefault();
			if (t.Actor == null)
				return false;

			s.SetActorToTarget(t);
			return true;
		}

		void EnvoyerAviation(SquadCA s)
		{
			// Chasseurs : ils gardent la supériorité aérienne. Escadrille en fuite (DCA, réarmement) : on la laisse.
			if (s.Units.All(u => Info.ChasseursExclus.Contains(u.Info.Name)))
				return;

			var etat = s.FuzzyStateMachine.CurrentState;
			if (etat is not AirIdleStateCA && etat is not AirAttackStateCA)
				return;

			// En attaque contre des avions ennemis : la chasse passe d'abord.
			if (etat is AirAttackStateCA && Valide(s.TargetActor) && s.TargetActor.Info.HasTraitInfo<AircraftInfo>())
				return;

			if (!Viser(s, objectif, s.Units.FirstOrDefault(u => !u.IsDead && u.IsInWorld)))
				return;

			if (etat is AirIdleStateCA)
				s.FuzzyStateMachine.ChangeState(s, new AirAttackStateCA(), true);
		}

		void EnvoyerMarine(SquadCA s)
		{
			var meneur = s.Units.FirstOrDefault(u => !u.IsDead && u.IsInWorld);
			if (meneur == null)
				return;

			// Le bâtiment ou l'unité ennemie la plus proche de l'objectif que les navires peuvent atteindre
			// (à portée de tir depuis l'eau).
			var proches = world.FindActorsInCircle(objectif.CenterPosition, WDist.FromCells(Info.RayonNaval))
				.Where(Valide)
				.OrderBy(a => (a.CenterPosition - objectif.CenterPosition).LengthSquared)
				.Take(8)
				.ToList();

			if (Valide(s.TargetActor) && proches.Contains(s.TargetActor))
				return;

			var t = squads.FindEnemies(proches, meneur).FirstOrDefault();
			if (t.Actor != null)
				s.SetActorToTarget(t);
		}
	}
}
