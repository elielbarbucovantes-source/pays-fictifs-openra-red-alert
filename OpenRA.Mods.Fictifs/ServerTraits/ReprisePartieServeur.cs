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
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRA.Network;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Fictifs.ServerTraits
{
	/// <summary>
	/// Reprise d'une partie en ligne interrompue (un joueur déconnecté).
	///
	/// Le moteur ne sait pas sauvegarder une partie à plusieurs : on repart du replay que
	/// l'hôte enregistre de toute façon. Dans un nouveau salon, l'hôte choisit le replay
	/// (« Reprendre une partie », commande « reprise &lt;fichier&gt; ») :
	///  - le salon reprend la carte, les options et la graine de la partie ;
	///  - chaque joueur retrouve son emplacement d'après son nom ; un absent garde le sien
	///    sous forme de « fantôme » (client sans connexion), qu'il remplace s'il arrive ;
	///  - les réglages sont verrouillés.
	/// Au lancement, le serveur renvoie à tous les ordres de la partie d'origine, image par
	/// image, et le jeu les rejoue en accéléré (mécanisme de chargement des sauvegardes du
	/// moteur, vérifié par la dernière empreinte de synchronisation). Puis les fantômes sont
	/// déclarés déconnectés : l'IA de l'hôte les joue (ReprisePartie), comme avant.
	/// </summary>
	public class ReprisePartieServeur : ServerTrait, IInterpretCommand, IClientJoined, IStartGame, ITick
	{
		/// <summary>Marque (champ Location, partagé par le salon) d'un joueur absent tenu par un fantôme.</summary>
		public const string MarqueAbsent = "Absent : joué par l'IA de l'hôte";

		// Commandes du salon qui changeraient la partie reprise : refusées.
		static readonly HashSet<string> Verrouillees = new()
		{
			"slot", "spectate", "slot_close", "slot_open", "slot_bot", "option", "assignteams",
			"make_spectator", "faction", "team", "handicap", "spawn", "clear_spawn", "color"
		};

		static readonly MethodInfo EnvoyerImage = typeof(S).GetMethod("DispatchOrdersToClient", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly FieldInfo PaquetSync = typeof(GameSave).GetField("lastSyncPacket", BindingFlags.Instance | BindingFlags.NonPublic);

		RepriseReplay reprise;

		public static bool EstAbsent(Session.Client c)
		{
			return c != null && c.Bot == null && c.Location == MarqueAbsent;
		}

		bool IInterpretCommand.InterpretCommand(S server, Connection conn, Session.Client client, string cmd)
		{
			if (server.State != ServerState.WaitingPlayers)
				return false;

			var espace = cmd.IndexOf(' ');
			var verbe = espace < 0 ? cmd : cmd[..espace];

			if (verbe == "reprise")
			{
				if (!client.IsAdmin)
					server.SendOrderTo(conn, "Message", "Seul l'hôte peut reprendre une partie.");
				else
					Charger(server, conn, espace < 0 ? "" : cmd[(espace + 1)..]);

				return true;
			}

			if (reprise == null)
				return false;

			// Changer de carte annule la reprise (la commande suit son cours normal).
			if (verbe == "map")
			{
				Annuler(server);
				server.SendMessage("Reprise annulée.");
				return false;
			}

			if (Verrouillees.Contains(verbe))
			{
				server.SendOrderTo(conn, "Message", "Partie reprise : emplacements et réglages verrouillés (changez de carte pour annuler la reprise).");
				return true;
			}

			return false;
		}

		void Annuler(S server)
		{
			reprise = null;
			server.GameSave = null;
			server.LobbyInfo.Clients.RemoveAll(EstAbsent);
		}

		void Charger(S server, Connection conn, string nom)
		{
			// Uniquement un replay du dossier des replays de cette version du mod.
			nom = Path.GetFileName(nom);
			var chemin = Path.Combine(RepriseReplay.Dossier(server.ModData), nom);
			if (!File.Exists(chemin))
			{
				server.SendOrderTo(conn, "Message", $"Replay introuvable : {nom}");
				return;
			}

			RepriseReplay r;
			try
			{
				r = RepriseReplay.Lire(chemin);
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Reprise : {chemin} : {e}");
				server.SendOrderTo(conn, "Message", $"Impossible de reprendre cette partie : {e.Message}");
				return;
			}

			var carte = server.ModData.MapCache[r.Salon.GlobalSettings.Map];
			if (carte.Status != MapStatus.Available)
			{
				server.SendOrderTo(conn, "Message", "La carte de cette partie n'est plus disponible chez l'hôte.");
				return;
			}

			// Sauvegarde « vide » : elle ne porte que la dernière image et l'empreinte de
			// synchronisation ; les ordres sont envoyés par GameStarted.
			var save = new GameSave();
			typeof(GameSave).GetProperty(nameof(GameSave.LastOrdersFrame)).SetValue(save, r.DerniereImage);
			typeof(GameSave).GetProperty(nameof(GameSave.LastSyncFrame)).SetValue(save, r.ImageSync);
			PaquetSync.SetValue(save, r.Sync);

			lock (server.LobbyInfo)
			{
				if (reprise != null)
					Annuler(server);

				reprise = r;
				server.GameSave = save;
				server.Map = carte;

				var nomServeur = server.LobbyInfo.GlobalSettings.ServerName;
				server.LobbyInfo.GlobalSettings = Session.Global.Deserialize(r.Salon.GlobalSettings.Serialize().Value);
				server.LobbyInfo.GlobalSettings.ServerName = nomServeur;
				server.LobbyInfo.GlobalSettings.MapStatus = server.MapStatusCache[carte];
				server.LobbyInfo.Slots = r.Salon.Slots.ToDictionary(kv => kv.Key, kv => Session.Slot.Deserialize(kv.Value.Serialize().Value));

				// Les emplacements de départ désactivés changent le tirage des positions de départ.
				server.LobbyInfo.DisabledSpawnPoints = new HashSet<int>(r.Salon.DisabledSpawnPoints);

				// Tout le monde en spectateur, IA retirées, puis chaque joueur de la partie
				// d'origine retrouve son emplacement (même nom) ; sinon, un fantôme le tient.
				foreach (var c in server.LobbyInfo.Clients.ToList())
				{
					if (c.Bot != null)
						server.LobbyInfo.Clients.Remove(c);
					else
					{
						c.Slot = null;
						c.State = Session.ClientState.NotReady;
					}
				}

				var admin = server.LobbyInfo.Clients.First(c => c.IsAdmin).Index;
				foreach (var origine in r.Salon.Clients.Where(c => c.Slot != null))
				{
					if (origine.Bot != null)
					{
						var ia = new Session.Client
						{
							Index = server.ChooseFreePlayerIndex(),
							Bot = origine.Bot,
							Name = origine.Name,
							State = Session.ClientState.NotReady,
							BotControllerClientIndex = admin
						};

						Copier(origine, ia);
						server.LobbyInfo.Clients.Add(ia);
						continue;
					}

					var humain = server.LobbyInfo.Clients.FirstOrDefault(c => c.Bot == null && c.Slot == null && c.Name == origine.Name);
					if (humain != null)
						Copier(origine, humain);
				}

				Completer(server);
				Trier(server);
				server.SyncLobbyInfo();
			}

			var absents = server.LobbyInfo.Clients.Where(EstAbsent).Select(c => c.Name).ToList();
			server.SendMessage($"Partie reprise : {carte.Title}, image {r.DerniereImage}." +
				(absents.Count > 0 ? $" Absents : {string.Join(", ", absents)} — l'IA de l'hôte les jouera, sauf s'ils rejoignent le salon avec le même nom." : ""));
		}

		static void Copier(Session.Client origine, Session.Client client)
		{
			client.Slot = origine.Slot;
			client.Faction = origine.Faction;
			client.SpawnPoint = origine.SpawnPoint;
			client.Team = origine.Team;
			client.Handicap = origine.Handicap;
			client.Color = origine.Color;
		}

		/// <summary>Un fantôme dans chaque emplacement humain d'origine resté vide.</summary>
		bool Completer(S server)
		{
			var change = false;
			foreach (var origine in reprise.Participants.Where(c => c.Slot != null))
			{
				if (server.LobbyInfo.ClientInSlot(origine.Slot) != null)
					continue;

				var fantome = new Session.Client
				{
					Index = server.ChooseFreePlayerIndex(),
					Name = origine.Name,
					Location = MarqueAbsent,
					State = Session.ClientState.Ready,
					PreferredColor = origine.Color
				};

				Copier(origine, fantome);
				server.LobbyInfo.Clients.Add(fantome);
				change = true;
			}

			return change;
		}

		/// <summary>
		/// Range les clients comme dans la partie d'origine : le jeu traite les ordres d'une
		/// même image dans cet ordre, il doit être identique pour rejouer la partie à l'identique.
		/// </summary>
		void Trier(S server)
		{
			var rang = reprise.Participants.Where(c => c.Slot != null)
				.Select((c, i) => (c.Slot, i))
				.ToDictionary(x => x.Slot, x => x.i);

			var tries = server.LobbyInfo.Clients
				.OrderBy(c => c.Bot == null && c.Slot != null && rang.TryGetValue(c.Slot, out var i) ? i : int.MaxValue)
				.ToList();

			server.LobbyInfo.Clients.Clear();
			server.LobbyInfo.Clients.AddRange(tries);
		}

		void IClientJoined.ClientJoined(S server, Connection conn)
		{
			if (reprise == null)
				return;

			lock (server.LobbyInfo)
			{
				var client = server.GetClient(conn);
				if (client == null)
					return;

				// Le moteur l'a mis dans le premier emplacement libre : il ne peut reprendre que
				// le sien, tenu par son fantôme.
				client.Slot = null;
				var fantome = server.LobbyInfo.Clients.FirstOrDefault(c => EstAbsent(c) && c.Name == client.Name);
				if (fantome != null)
				{
					server.LobbyInfo.Clients.Remove(fantome);
					client.Slot = fantome.Slot;
					client.Faction = fantome.Faction;
					client.SpawnPoint = fantome.SpawnPoint;
					client.Team = fantome.Team;
					client.Handicap = fantome.Handicap;
					client.Color = fantome.Color;
					server.SendMessage($"{client.Name} reprend sa place.");
				}

				Trier(server);
			}
		}

		void ITick.Tick(S server)
		{
			// Un emplacement vidé (départ, exclusion) est aussitôt repris par un fantôme :
			// sans lui, la partie ne pourrait pas être rejouée.
			if (reprise == null || server.State != ServerState.WaitingPlayers)
				return;

			lock (server.LobbyInfo)
			{
				if (Completer(server))
				{
					Trier(server);
					server.SyncLobbyInfo();
				}
			}
		}

		void IStartGame.GameStarted(S server)
		{
			if (reprise == null)
				return;

			var r = reprise;
			reprise = null;

			// Client d'origine → client actuel, par emplacement. Les ordres des spectateurs
			// d'origine (il n'y en a pas) sont laissés de côté.
			var correspondance = new Dictionary<int, int>();
			foreach (var origine in r.Participants.Where(c => c.Slot != null))
			{
				var actuel = server.LobbyInfo.ClientInSlot(origine.Slot);
				if (actuel != null && actuel.Bot == null)
					correspondance[origine.Index] = actuel.Index;
			}

			// Le jeu attend, à chaque image, un paquet de chaque client non-IA (spectateurs
			// compris), dans l'ordre du salon.
			var clients = server.LobbyInfo.Clients.Where(c => !c.IsBot).Select(c => c.Index).ToList();
			var sources = clients.ToDictionary(c => c, c => r.Participants
				.Where(o => correspondance.TryGetValue(o.Index, out var a) && a == c)
				.Select(o => o.Index)
				.ToList());

			var destinataires = server.Conns.Where(c => c.Validated).ToList();
			var ms = new MemoryStream();
			for (var image = 1; image <= r.DerniereImage; image++)
			{
				foreach (var c in clients)
				{
					ms.SetLength(0);
					foreach (var o in sources[c])
						ms.Write(r.Ordres(o, image));

					var donnees = ms.ToArray();
					foreach (var dest in destinataires)
						EnvoyerImage.Invoke(server, new object[] { dest, c, image, donnees });
				}
			}

			// Les fantômes se « déconnectent » juste après le rejeu : leur IA prend le relais.
			foreach (var fantome in server.LobbyInfo.Clients.Where(EstAbsent).ToList())
			{
				var paquet = new MemoryStream(5);
				paquet.WriteByte((byte)OrderType.Disconnect);
				paquet.Write(fantome.Index);
				server.DispatchServerOrdersToClients(paquet.ToArray(), r.DerniereImage + 1);
			}

			Log.Write("server", $"Reprise : {r.DerniereImage} images renvoyées à {destinataires.Count} joueurs.");
		}
	}
}
