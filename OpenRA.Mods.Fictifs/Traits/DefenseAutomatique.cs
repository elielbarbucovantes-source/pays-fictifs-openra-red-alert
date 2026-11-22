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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Système de défense automatique : les armements listés tirent seuls sur les ennemis à portée,",
		"sans ordre du joueur et quelle que soit la posture de l'unité (même « Ne pas tirer »).",
		"L'unité ne change pas de trajectoire : ces armes ne font que réagir à ce qui passe à portée.",
		"Utilisé par le B-2 Spirit (roquettes antiaériennes, canons de défense).")]
	public class DefenseAutomatiqueInfo : ConditionalTraitInfo, Requires<ArmamentInfo>
	{
		[FieldLoader.Require]
		[Desc("Noms des armements (Armament Name) commandés par ce système.")]
		public readonly HashSet<string> Armaments = new();

		[Desc("Cibles traitées en priorité quand plusieurs sont à portée (types Targetable).")]
		public readonly BitSet<TargetableType> PriorityTargets = default;

		[Desc("Relations des cibles visées.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Enemy;

		[Desc("Intervalle entre deux recherches de cible (ticks).")]
		public readonly int ScanInterval = 5;

		public override object Create(ActorInitializer init) { return new DefenseAutomatique(this); }
	}

	public class DefenseAutomatique : ConditionalTrait<DefenseAutomatiqueInfo>, ITick
	{
		Armament[] armaments;
		Target[] cibles;
		IFacing facing;
		int ticks;

		public DefenseAutomatique(DefenseAutomatiqueInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			armaments = self.TraitsImplementing<Armament>().Where(a => Info.Armaments.Contains(a.Info.Name)).ToArray();
			cibles = armaments.Select(_ => Target.Invalid).ToArray();
			facing = self.TraitOrDefault<IFacing>();
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !self.IsInWorld || self.IsDead)
				return;

			var scan = --ticks <= 0;
			if (scan)
				ticks = Info.ScanInterval;

			for (var i = 0; i < armaments.Length; i++)
			{
				var armament = armaments[i];
				if (armament.IsTraitDisabled)
					continue;

				// Garde sa cible tant qu'elle reste valide, sinon en cherche une autre.
				if (!CibleValide(self, armament, cibles[i]))
				{
					cibles[i] = Target.Invalid;
					if (scan)
						cibles[i] = ChoisirCible(self, armament);
				}

				if (cibles[i].Type != TargetType.Invalid)
					armament.CheckFire(self, facing, cibles[i]);
			}
		}

		bool CibleValide(Actor self, Armament armament, in Target cible)
		{
			if (cible.Type != TargetType.Actor)
				return false;

			var a = cible.Actor;
			if (a.IsDead || !a.IsInWorld || !cible.IsValidFor(self)
				|| !Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(a.Owner))
				|| !a.CanBeViewedByPlayer(self.Owner))
				return false;

			var weapon = armament.Weapon;
			return weapon.IsValidAgainst(cible, self.World, self)
				&& cible.IsInRange(self.CenterPosition, armament.MaxRange())
				&& (weapon.MinRange == WDist.Zero || !cible.IsInRange(self.CenterPosition, weapon.MinRange));
		}

		Target ChoisirCible(Actor self, Armament armament)
		{
			var meilleure = Target.Invalid;
			var meilleurRang = int.MaxValue;
			var meilleureDistance = long.MaxValue;
			var meilleurId = uint.MaxValue;

			foreach (var a in self.World.FindActorsInCircle(self.CenterPosition, armament.MaxRange()))
			{
				if (a == self)
					continue;

				var cible = Target.FromActor(a);
				if (!CibleValide(self, armament, cible))
					continue;

				// Ordre déterministe (indispensable en multijoueur) : priorité, distance, identifiant.
				var rang = !Info.PriorityTargets.IsEmpty && a.GetEnabledTargetTypes().Overlaps(Info.PriorityTargets) ? 0 : 1;
				var distance = (a.CenterPosition - self.CenterPosition).HorizontalLengthSquared;
				if (rang < meilleurRang || (rang == meilleurRang && (distance < meilleureDistance
					|| (distance == meilleureDistance && a.ActorID < meilleurId))))
				{
					meilleure = cible;
					meilleurRang = rang;
					meilleureDistance = distance;
					meilleurId = a.ActorID;
				}
			}

			return meilleure;
		}
	}
}
