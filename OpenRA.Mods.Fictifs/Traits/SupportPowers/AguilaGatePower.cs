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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Opens two linked " + nameof(AguilaGate) + " actors: first click = entrance, second click = exit.")]
	public class AguilaGatePowerInfo : TwoCellPowerInfo
	{
		[ActorReference(typeof(AguilaGateInfo))]
		[FieldLoader.Require]
		[Desc("Actor spawned on the first cell.")]
		public readonly string EntranceActor = null;

		[ActorReference(typeof(AguilaGateInfo))]
		[FieldLoader.Require]
		[Desc("Actor spawned on the second cell.")]
		public readonly string ExitActor = null;

		[Desc("Number of units that may travel through the tunnel before it collapses. 0 means unlimited.")]
		public readonly int Capacity = 0;

		[Desc("Terrain types where the tunnel ends may be opened. Empty means any terrain.")]
		public readonly HashSet<string> ValidTerrainTypes = new();

		public override object Create(ActorInitializer init) { return new AguilaGatePower(init.Self, this); }
	}

	public class AguilaGatePower : TwoCellPower
	{
		readonly AguilaGatePowerInfo info;

		public AguilaGatePower(Actor self, AguilaGatePowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override bool IsValidFirstCell(CPos cell)
		{
			return base.IsValidFirstCell(cell) && IsValidTerrain(cell);
		}

		public override bool IsValidPair(CPos first, CPos second)
		{
			return base.IsValidPair(first, second) && IsValidTerrain(second);
		}

		bool IsValidTerrain(CPos cell)
		{
			return info.ValidTerrainTypes.Count == 0 || info.ValidTerrainTypes.Contains(Self.World.Map.GetTerrainInfo(cell).Type);
		}

		protected override void Deploy(Actor self, CPos first, CPos second)
		{
			self.World.AddFrameEndTask(w =>
			{
				var entrance = w.CreateActor(info.EntranceActor, new TypeDictionary
				{
					new LocationInit(first),
					new OwnerInit(self.Owner),
				});

				var exit = w.CreateActor(info.ExitActor, new TypeDictionary
				{
					new LocationInit(second),
					new OwnerInit(self.Owner),
				});

				var link = new AguilaGateLink(info.Capacity);
				entrance.Trait<AguilaGate>().Link(exit, link);
				exit.Trait<AguilaGate>().Link(entrance, link);
			});
		}
	}
}
