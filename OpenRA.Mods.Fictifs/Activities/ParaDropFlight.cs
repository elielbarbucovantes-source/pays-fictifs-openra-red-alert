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
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Activities
{
	// Fly to the drop zone, circle above it until every passenger has jumped, then return to base.
	public class ParaDropFlight : Activity
	{
		readonly ParaDropOnOrder paraDrop;
		readonly Target dropZone;
		bool returning;
		int lastDropped;
		int stalledSince = -1;

		public ParaDropFlight(Actor self, in Target dropZone)
		{
			paraDrop = self.Trait<ParaDropOnOrder>();
			this.dropZone = dropZone;
		}

		protected override void OnFirstRun(Actor self)
		{
			paraDrop.Arm(dropZone);
		}

		public override bool Tick(Actor self)
		{
			if (returning)
				return true;

			if (IsCanceling)
			{
				paraDrop.Disarm();
				return true;
			}

			// Nobody could jump for a while: bring the others back home.
			// (Children run between two ticks of this activity, so count in world ticks.)
			var now = self.World.WorldTick;
			if (paraDrop.DroppedSinceArmed != lastDropped)
			{
				lastDropped = paraDrop.DroppedSinceArmed;
				stalledSince = now;
			}
			else if (stalledSince < 0 && paraDrop.InDropRange(dropZone))
				stalledSince = now;

			if (paraDrop.IsEmpty || (stalledSince >= 0 && now - stalledSince > paraDrop.Info.GiveUpDelay))
			{
				paraDrop.Disarm();
				returning = true;
				QueueChild(new ReturnToBase(self, null, true));
				return false;
			}

			// Circle over the zone while the passengers jump; the trait does the dropping.
			if (paraDrop.InDropRange(dropZone))
				QueueChild(new FlyIdle(self, 10));
			else
				QueueChild(new Fly(self, dropZone, WDist.FromCells(1), targetLineColor: Color.Green));

			return false;
		}

		protected override void OnLastRun(Actor self)
		{
			paraDrop.Disarm();
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (!returning)
				yield return new TargetLineNode(dropZone, Color.Green);
		}
	}
}
