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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Wagon industriel ananthanien : les véhicules qui en descendent gardent un moment un bonus
	// (réparation, ravitaillement) donné par une ExternalCondition sur le passager.
	[Desc("Grants an external condition to each passenger leaving this transport, for a limited time.")]
	public class GrantConditionOnPassengerExitInfo : TraitInfo, Requires<CargoInfo>
	{
		[FieldLoader.Require]
		[Desc("External condition granted to the passenger (it needs a matching ExternalCondition).")]
		public readonly string Condition = null;

		[Desc("Duration of the condition, in ticks. 0 means forever.")]
		public readonly int Duration = 500;

		public override object Create(ActorInitializer init) { return new GrantConditionOnPassengerExit(this); }
	}

	public class GrantConditionOnPassengerExit : INotifyPassengerExited
	{
		readonly GrantConditionOnPassengerExitInfo info;

		public GrantConditionOnPassengerExit(GrantConditionOnPassengerExitInfo info) { this.info = info; }

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			passenger.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == info.Condition && t.CanGrantCondition(self))
				?.GrantCondition(passenger, self, info.Duration);
		}
	}
}
