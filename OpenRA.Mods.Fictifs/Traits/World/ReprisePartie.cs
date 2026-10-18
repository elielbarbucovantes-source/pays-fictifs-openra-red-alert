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
using OpenRA.Mods.Fictifs.ServerTraits;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Joueur déconnecté en cours de partie : une IA de l'hôte prend le relais (voir RepriseIA).",
		"La partie peut ensuite être reprise depuis le salon (ReprisePartieServeur).")]
	public class ReprisePartieInfo : TraitInfo<ReprisePartie> { }

	public class ReprisePartie : INotifyPlayerDisconnected
	{
		// Appelé chez tous les joueurs à la même image de jeu.
		void INotifyPlayerDisconnected.PlayerDisconnected(Actor self, Player p)
		{
			var world = self.World;
			if (p.IsBot || p.NonCombatant || p.WinState != WinState.Undefined || world.IsReplay)
				return;

			var hote = world.Players.FirstOrDefault(j => j.Playable && !j.IsBot
				&& world.LobbyInfo.ClientWithIndex(j.ClientIndex)?.IsAdmin == true);

			if (hote == null)
			{
				TextNotificationsManager.AddSystemLine("Reprise",
					$"{p.PlayerName} est déconnecté. L'hôte est spectateur : personne ne peut prendre le relais.");
				return;
			}

			TextNotificationsManager.AddSystemLine("Reprise",
				$"{p.PlayerName} est déconnecté : une IA prend le relais. Pour qu'il revienne, quittez la partie " +
				"puis, dans un nouveau salon, l'hôte choisit « Reprendre une partie ».");

			// L'ordre passe par le flux de la partie : il est appliqué partout au même
			// moment et rejoué à l'identique lors d'une reprise.
			if (world.LocalPlayer == hote)
				world.IssueOrder(new Order(RepriseIA.OrdrePriseEnMain, hote.PlayerActor, false) { ExtraData = p.PlayerActor.ActorID });
		}
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Permet à l'hôte de faire jouer une IA à la place de ce joueur s'il se déconnecte.")]
	public class RepriseIAInfo : TraitInfo
	{
		[Desc("Type du ModularBot qui prend le relais.")]
		public readonly string Bot = "normal";

		[GrantedConditionReference]
		[Desc("Condition qui active les modules de cette IA (celle de GrantConditionOnBotOwner).")]
		public readonly string Condition = "enable-normal-ai";

		public override object Create(ActorInitializer init) { return new RepriseIA(init.Self, this); }
	}

	public class RepriseIA : IResolveOrder, ITick
	{
		public const string OrdrePriseEnMain = "FictifsPriseEnMainIA";

		readonly Actor self;
		readonly RepriseIAInfo info;
		int jeton = Actor.InvalidConditionToken;
		ModularBot ia;
		bool chargement;

		/// <summary>Joueur dont le client donne les ordres à la place de celui-ci (l'hôte), ou null.</summary>
		public Player Controleur { get; private set; }

		public RepriseIA(Actor self, RepriseIAInfo info)
		{
			this.self = self;
			this.info = info;
		}

		// Reçu par le joueur de l'hôte ; ExtraData désigne le joueur à reprendre.
		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrdrePriseEnMain)
				return;

			var cible = self.World.GetActorById(order.ExtraData)?.Owner;
			if (cible == null || cible == self.Owner || cible.PlayerActor != self.World.GetActorById(order.ExtraData))
				return;

			cible.PlayerActor.TraitOrDefault<RepriseIA>()?.PrendreEnMain(self.Owner);
		}

		void PrendreEnMain(Player controleur)
		{
			if (Controleur != null)
				return;

			Controleur = controleur;
			jeton = self.GrantCondition(info.Condition);

			// L'IA ne réfléchit que chez l'hôte ; ses ordres passent par le réseau.
			if (Game.IsHost && controleur == self.World.LocalPlayer)
			{
				ia = self.TraitsImplementing<ModularBot>().FirstOrDefault(b => ((IBot)b).Info.Type == info.Bot);
				ia?.Activate(self.Owner);
			}
		}

		void Liberer()
		{
			Controleur = null;
			if (jeton != Actor.InvalidConditionToken)
				jeton = self.RevokeCondition(jeton);

			if (ia != null)
			{
				ia.IsEnabled = false;
				ia = null;
			}
		}

		void ITick.Tick(Actor self)
		{
			// Fin du rejeu d'une reprise (même image chez tous) : le joueur revenu
			// reprend la main ; un absent (fantôme) reste joué par l'IA.
			var enChargement = self.World.IsLoadingGameSave;
			if (chargement && !enChargement && Controleur != null
				&& !ReprisePartieServeur.EstAbsent(self.World.LobbyInfo.ClientWithIndex(self.Owner.ClientIndex)))
			{
				Liberer();
				TextNotificationsManager.AddSystemLine("Reprise", $"{self.Owner.PlayerName} reprend le commandement.");
			}

			chargement = enChargement;
		}
	}

	[TraitLocation(SystemActors.World)]
	[Desc("ValidateOrder du moteur, qui accepte en plus les ordres de l'hôte pour un joueur repris par l'IA.")]
	public class ValidateOrderFictifsInfo : TraitInfo<ValidateOrderFictifs> { }

	public class ValidateOrderFictifs : IValidateOrder
	{
		public bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order)
		{
			if (order.Subject == null || order.Subject.Owner == null)
				return true;

			var owner = order.Subject.Owner;
			var controleur = owner.PlayerActor.TraitOrDefault<RepriseIA>()?.Controleur;
			if (controleur != null && controleur.ClientIndex == clientId)
				return order.Subject.AcceptsOrder(order.OrderString);

			// Reste : identique à ValidateOrder.
			var subjectClientId = owner.ClientIndex;
			var subjectClient = orderManager.LobbyInfo.ClientWithIndex(subjectClientId);

			if (subjectClient == null)
			{
				Log.Write("debug", $"Tick {world.WorldTick}: Order sent to {owner.PlayerName}: resolved ClientIndex `{subjectClientId}` doesn't exist");
				return false;
			}

			var isBotOrder = subjectClient.Bot != null && clientId == subjectClient.BotControllerClientIndex;
			if (subjectClientId != clientId && !isBotOrder)
				return false;

			return order.Subject.AcceptsOrder(order.OrderString);
		}
	}
}
