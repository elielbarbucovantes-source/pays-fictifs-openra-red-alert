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
	// Se place dans l'axe, survole la ligne de largage (les passagers sautent pendant le survol),
	// recommence si certains n'ont pas pu sauter, puis rentre à la base.
	public class ParaDropFlight : Activity
	{
		readonly ParaDropOnOrder paraDrop;
		readonly WPos center;
		readonly WVec dir;
		readonly WPos approach, lineStart, exit;
		bool returning;
		bool onLine;
		int passes;

		public ParaDropFlight(Actor self, WPos center, WVec dir)
		{
			paraDrop = self.Trait<ParaDropOnOrder>();
			this.center = center;
			this.dir = dir;
			approach = paraDrop.Approach(center, dir);
			lineStart = paraDrop.LineStart(center, dir);
			exit = paraDrop.LineEnd(center, dir) + dir * 2;
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

			if (paraDrop.IsEmpty || passes >= paraDrop.Info.MaxPasses)
			{
				paraDrop.Disarm();
				returning = true;
				QueueChild(new ReturnToBase(self, null, true));
				return false;
			}

			// Alternance : se placer au point d'approche, puis survoler toute la ligne.
			if (!onLine)
			{
				onLine = true;
				QueueChild(new Fly(self, Target.FromPos(approach), WDist.FromCells(1), targetLineColor: Color.Green));
			}
			else
			{
				onLine = false;
				passes++;
				paraDrop.Arm(center, dir);
				QueueChild(new Fly(self, Target.FromPos(lineStart), WDist.FromCells(1), targetLineColor: Color.Green));
				QueueChild(new Fly(self, Target.FromPos(exit), WDist.FromCells(1), targetLineColor: Color.Green));
			}

			return false;
		}

		protected override void OnLastRun(Actor self)
		{
			paraDrop.Disarm();
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (returning)
				yield break;

			yield return new TargetLineNode(Target.FromPos(approach), Color.Green);
			yield return new TargetLineNode(Target.FromPos(lineStart), Color.Green);
			yield return new TargetLineNode(Target.FromPos(exit), Color.Green);
		}
	}
}
