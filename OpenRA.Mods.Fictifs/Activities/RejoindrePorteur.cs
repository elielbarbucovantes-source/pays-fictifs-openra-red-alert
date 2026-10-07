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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Activities
{
	// L'appareil poursuit un porteur mobile (wagon-plateforme du train aérien) et passe au-dessus de son pont :
	// AircraftCarrier le récupère dès qu'il est assez près. Contrairement à ReturnToBase, l'approche suit
	// la position actuelle du wagon au lieu de viser l'endroit où il était au moment de l'ordre.
	// Pas de réservation (Reservable n'en tient qu'une) : le wagon accueille plusieurs appareils et compte ses places.
	public class RejoindrePorteur : Activity
	{
		public readonly Actor Carrier;
		readonly AircraftCarrier carrier;

		public RejoindrePorteur(Actor self, Actor carrierActor)
		{
			Carrier = carrierActor;
			carrier = carrierActor.Trait<AircraftCarrier>();
		}

		bool Valid(Actor self)
		{
			return !Carrier.IsDead && Carrier.IsInWorld && Carrier.Owner == self.Owner && carrier.Accepts(self);
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || !Valid(self) || !carrier.HasSpace)
				return true;

			// Vol vers la position actuelle du wagon, recommencé tant qu'il n'a pas été récupéré.
			QueueChild(new Fly(self, Target.FromActor(Carrier), WDist.Zero, targetLineColor: Color.Green));
			return false;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(Carrier), Color.Green);
		}
	}
}
