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
	// L'unité rejoint la gare et y entre. Gare pleine : elle attend devant, jusqu'à ce qu'une place se libère.
	public class EntrerGare : Activity
	{
		const int RetryDelay = 25;

		readonly Actor gare;
		readonly GareStock stock;
		readonly IMove move;
		int attempts;

		public EntrerGare(Actor self, Actor gare)
		{
			this.gare = gare;
			stock = gare.Trait<GareStock>();
			move = self.Trait<IMove>();
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || gare.IsDead || !gare.IsInWorld || gare.Owner != self.Owner || !stock.CanStore(self))
				return true;

			if (stock.InReach(self))
			{
				attempts = 0;
				if (stock.HasRoomFor(self))
				{
					stock.Store(self);
					return true;
				}

				// Gare pleine : on attend devant.
				QueueChild(new Wait(RetryDelay));
				return false;
			}

			// Chemin bloqué : on réessaie de temps en temps, sans fin (comme une unité qui attend son tour).
			if (attempts++ > 0)
				QueueChild(new Wait(RetryDelay));

			QueueChild(move.MoveToTarget(self, Target.FromActor(gare), targetLineColor: Color.Green));
			return false;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(gare), Color.Green);
		}
	}
}
