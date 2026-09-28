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
	[Desc("Refills an ammo pool while the aircraft flies over water, like a water bomber scooping its load.",
		"Used by the Australouis seaplane (Cormoran).")]
	public class ScoopsWaterInfo : ConditionalTraitInfo, Requires<AircraftInfo>, Requires<AmmoPoolInfo>
	{
		[Desc("Name of the ammo pool to refill.")]
		public readonly string AmmoPool = "primary";

		[Desc("Terrain types the aircraft can scoop from.")]
		public readonly HashSet<string> TerrainTypes = new() { "Water" };

		[Desc("Ticks spent over water (not necessarily in a row) to gain one ammo.")]
		public readonly int Delay = 50;

		[GrantedConditionReference]
		[Desc("Condition granted while the aircraft is scooping.")]
		public readonly string ScoopingCondition = null;

		public override object Create(ActorInitializer init) { return new ScoopsWater(init.Self, this); }
	}

	public class ScoopsWater : ConditionalTrait<ScoopsWaterInfo>, ITick
	{
		readonly Aircraft aircraft;
		AmmoPool pool;
		int ticks;
		int token = Actor.InvalidConditionToken;

		public ScoopsWater(Actor self, ScoopsWaterInfo info)
			: base(info)
		{
			aircraft = self.Trait<Aircraft>();
		}

		protected override void Created(Actor self)
		{
			pool = self.TraitsImplementing<AmmoPool>().First(p => p.Info.Name == Info.AmmoPool);
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			var scooping = !IsTraitDisabled && !pool.HasFullAmmo && OverWater(self);
			if (!scooping)
			{
				SetCondition(self, false);
				return;
			}

			SetCondition(self, true);
			if (++ticks >= Info.Delay)
			{
				ticks = 0;
				pool.GiveAmmo(self, 1);
			}
		}

		bool OverWater(Actor self)
		{
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length <= 0 && !aircraft.Info.CanHover)
				return false;

			var cell = self.Location;
			return self.World.Map.Contains(cell) && Info.TerrainTypes.Contains(self.World.Map.GetTerrainInfo(cell).Type);
		}

		void SetCondition(Actor self, bool on)
		{
			if (string.IsNullOrEmpty(Info.ScoopingCondition))
				return;

			if (on && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(Info.ScoopingCondition);
			else if (!on && token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}
	}
}
