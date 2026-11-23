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
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Porte-avions : il remplace le centre technique ou le dôme radar du joueur quand celui-ci
	// a été détruit (ou capturé, vendu). La condition accordée active, sur le navire, les
	// ProvidesPrerequisite / ProvidesRadar du bâtiment perdu. Tant que le joueur n'a jamais eu
	// le bâtiment, rien n'est remplacé : le porte-avions ne sert pas à sauter une étape technologique.

	[TraitLocation(SystemActors.Player)]
	[Desc("Remembers the building types the player has owned, for " + nameof(GrantConditionOnBuildingLost) + ".")]
	public class MemoireBatimentsInfo : TraitInfo
	{
		[Desc("Ticks between two scans of the player's buildings.")]
		public readonly int Interval = 25;

		public override object Create(ActorInitializer init) { return new MemoireBatiments(this); }
	}

	public class MemoireBatiments : ITick
	{
		readonly MemoireBatimentsInfo info;
		readonly HashSet<string> dejaPossedes = new();
		readonly HashSet<string> possedes = new();
		int ticks;

		public MemoireBatiments(MemoireBatimentsInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			if (--ticks > 0)
				return;

			ticks = info.Interval;
			possedes.Clear();
			foreach (var a in self.World.ActorsHavingTrait<Building>())
				if (a.Owner == self.Owner && a.IsInWorld && !a.IsDead)
					possedes.Add(a.Info.Name);

			dejaPossedes.UnionWith(possedes);
		}

		/// <summary>Le joueur a eu un bâtiment de ces types, et n'en a plus aucun.</summary>
		public bool APerdu(IEnumerable<string> types)
		{
			return types.Any(dejaPossedes.Contains) && !types.Any(possedes.Contains);
		}
	}

	[Desc("Grants a condition while the owner has lost every building of the given types",
		"(owned at least one of them before, owns none now). Needs " + nameof(MemoireBatiments) + " on the player.")]
	public class GrantConditionOnBuildingLostInfo : TraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Building actor types replaced by this actor.")]
		public readonly HashSet<string> Types = new();

		[GrantedConditionReference]
		[FieldLoader.Require]
		[Desc("Condition granted while they are lost.")]
		public readonly string Condition = null;

		[Desc("Ticks between two checks.")]
		public readonly int Interval = 25;

		public override object Create(ActorInitializer init) { return new GrantConditionOnBuildingLost(this); }
	}

	public class GrantConditionOnBuildingLost : ITick
	{
		readonly GrantConditionOnBuildingLostInfo info;
		int token = Actor.InvalidConditionToken;
		int ticks;

		public GrantConditionOnBuildingLost(GrantConditionOnBuildingLostInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			if (--ticks > 0)
				return;

			ticks = info.Interval;
			var memoire = self.Owner.PlayerActor.TraitOrDefault<MemoireBatiments>();
			var perdu = memoire != null && memoire.APerdu(info.Types);

			if (perdu && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);
			else if (!perdu && token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}
	}
}
