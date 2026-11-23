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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Virements entre alliés : le joueur envoie un montant à un allié, qui en reçoit
	// (100 - TaxPercent) %. L'interface est VirementLogic (icône en haut à gauche).
	// Ordre : sujet = acteur du joueur qui paie, ExtraData = ActorID de l'acteur du joueur
	// destinataire, TargetString = montant débité.

	[TraitLocation(SystemActors.Player)]
	[Desc("Lets the player transfer money to an ally, minus a tax.")]
	public class VirementsInfo : TraitInfo
	{
		[Desc("Percentage of the amount lost in the transfer.")]
		public readonly int TaxPercent = 5;

		[Desc("Smallest amount that can be sent.")]
		public readonly int MinAmount = 10;

		public override object Create(ActorInitializer init) { return new Virements(this); }
	}

	public class Virements : IResolveOrder
	{
		public const string OrderName = "FictifsVirement";

		public readonly VirementsInfo Info;

		public Virements(VirementsInfo info)
		{
			Info = info;
		}

		public int Received(int amount)
		{
			return (int)((long)amount * (100 - Info.TaxPercent) / 100);
		}

		public static IEnumerable<Player> Recipients(Player player)
		{
			return player.World.Players
				.Where(p => p != player && !p.NonCombatant && p.Playable && p.WinState != WinState.Lost
					&& p.IsAlliedWith(player) && player.IsAlliedWith(p)
					&& p.PlayerActor.TraitOrDefault<PlayerResources>() != null);
		}

		public static Order Order(Player from, Player to, int amount)
		{
			return new Order(OrderName, from.PlayerActor, false)
			{
				ExtraData = to.PlayerActor.ActorID,
				TargetString = amount.ToString(),
			};
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderName)
				return;

			if (!int.TryParse(order.TargetString, out var amount) || amount < Info.MinAmount)
				return;

			var from = self.Owner;
			var to = Recipients(from).FirstOrDefault(p => p.PlayerActor.ActorID == order.ExtraData);
			if (to == null)
				return;

			var fromResources = self.Trait<PlayerResources>();
			var toResources = to.PlayerActor.Trait<PlayerResources>();

			amount = Math.Min(amount, fromResources.Cash + fromResources.Resources);
			if (amount < Info.MinAmount || !fromResources.TakeCash(amount))
				return;

			var received = Received(amount);
			toResources.GiveCash(received);

			TextNotificationsManager.AddTransientLine($"Virement de {amount} $ à {to.PlayerName} : {received} $ reçus (taxe {amount - received} $).", from);
			TextNotificationsManager.AddTransientLine($"{from.PlayerName} vous a viré {received} $.", to);
		}
	}
}
