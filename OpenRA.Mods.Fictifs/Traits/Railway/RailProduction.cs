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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Chantier ferroviaire : ne sort que des voitures de train, sur une voie libre de ses sorties.
	// (Production ne vérifie la sortie que pour les unités Mobile.)
	[Desc("Production of train cars (" + nameof(RailCar) + "): an exit is used only if its cell is a free track.")]
	public class RailProductionInfo : ProductionInfo
	{
		public override object Create(ActorInitializer init) { return new RailProduction(init, this); }
	}

	public class RailProduction : Production
	{
		public RailProduction(ActorInitializer init, RailProductionInfo info)
			: base(init, info) { }

		public override bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits, int refundableValue)
		{
			var car = producee.TraitInfoOrDefault<RailCarInfo>();
			if (car == null || IsTraitDisabled || IsTraitPaused || Reservable.IsReserved(self))
				return false;

			// Une voiture de plusieurs cases (ou un train blindé et sa composition) doit trouver ses cases
			// libres sur la voie derrière la sortie.
			var consist = producee.TraitInfoOrDefault<SpawnsConsistInfo>();
			var length = consist != null ? consist.TotalCells(producee, self.World.Map.Rules) : Math.Max(1, car.Cells);
			var network = self.World.WorldActor.Trait<RailNetwork>();
			var exit = SelectExit(self, producee, productionType, e => car.CanEnterCell(self.World, null, self.Location + e.Info.ExitCell)
				&& network.TryChain(self.Location + e.Info.ExitCell, length, e.Info.Facing ?? car.InitialFacing, out _));
			if (exit == null)
				return false;

			DoProduction(self, producee, exit.Info, productionType, inits);
			return true;
		}
	}
}
