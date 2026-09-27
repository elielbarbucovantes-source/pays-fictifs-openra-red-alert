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

using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Limits how many priority orders a player can give during each term (quinquennat).",
		"Attach this to the player actor.")]
	public class PresidentialMandateInfo : TraitInfo
	{
		[Desc("Priority orders available in each term.")]
		public readonly int OrdersPerTerm = 10;

		[Desc("Length of a term, in ticks.")]
		public readonly int TermLength = 22500;

		[Desc("Text shown to the player when a new term starts. {0} is replaced by OrdersPerTerm.")]
		public readonly string NewTermTextNotification = null;

		[Desc("Speech notification played when a new term starts.")]
		public readonly string NewTermSpeechNotification = null;

		public override object Create(ActorInitializer init) { return new PresidentialMandate(this); }
	}

	public class PresidentialMandate : ITick
	{
		public readonly PresidentialMandateInfo Info;
		int ticksLeft;

		public int Used { get; private set; }
		public int Remaining => Info.OrdersPerTerm - Used;
		public int Term { get; private set; } = 1;

		public PresidentialMandate(PresidentialMandateInfo info)
		{
			Info = info;
			ticksLeft = info.TermLength;
		}

		public bool TryConsume()
		{
			if (Remaining <= 0)
				return false;

			Used++;
			return true;
		}

		void ITick.Tick(Actor self)
		{
			if (--ticksLeft > 0)
				return;

			ticksLeft = Info.TermLength;
			Term++;
			var hadUsedOrders = Used > 0;
			Used = 0;

			if (!hadUsedOrders)
				return;

			var player = self.Owner;
			if (!string.IsNullOrEmpty(Info.NewTermTextNotification))
				TextNotificationsManager.AddTransientLine(string.Format(Info.NewTermTextNotification, Info.OrdersPerTerm), player);

			Game.Sound.PlayNotification(self.World.Map.Rules, player, "Speech", Info.NewTermSpeechNotification, player.Faction.InternalName);
		}
	}
}
