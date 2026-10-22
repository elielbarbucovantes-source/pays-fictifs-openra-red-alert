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
using OpenRA.FileFormats;
using OpenRA.Network;

namespace OpenRA.Mods.Fictifs.ServerTraits
{
	/// <summary>
	/// Une partie en ligne relue depuis le replay de l'hôte, pour être reprise.
	///
	/// Le replay contient tout ce que l'hôte a reçu : le salon au lancement, puis pour
	/// chaque image réseau le paquet d'ordres de chaque joueur (pause comprise, c'est un
	/// ordre comme un autre), les déconnexions et les empreintes de synchronisation.
	/// On garde les images complètes (celles que l'hôte a pu jouer) et la dernière
	/// empreinte, qui vérifie à la fin du rejeu que la partie reprise est identique.
	///
	/// Après une désynchronisation, les joueurs ne voyaient plus la même partie : on
	/// reprend à la dernière empreinte sur laquelle tous étaient d'accord.
	/// </summary>
	public sealed class RepriseReplay
	{
		static readonly byte[] Vide = Array.Empty<byte>();

		/// <summary>Le salon au lancement de la partie.</summary>
		public Session Salon { get; private set; }

		/// <summary>Clients qui envoyaient des ordres (joueurs humains et spectateurs), dans l'ordre où le jeu les traite.</summary>
		public List<Session.Client> Participants { get; private set; }

		/// <summary>Dernière image complète.</summary>
		public int DerniereImage { get; private set; }

		public int ImageSync { get; private set; }

		/// <summary>Première image où les empreintes des joueurs diffèrent (désynchronisation), ou 0.</summary>
		public int ImageDesynchro { get; private set; }

		/// <summary>Client d'origine → image où le serveur l'a déclaré déconnecté.</summary>
		public Dictionary<int, int> Deconnexions { get; } = new();

		public int DerniereImageDe(int client)
		{
			return ordres.TryGetValue(client, out var parImage) && parImage.Count > 0 ? parImage.Keys.Max() : 0;
		}
		public byte[] Sync { get; private set; }

		// client d'origine → image → ordres (sans le numéro d'image)
		readonly Dictionary<int, Dictionary<int, byte[]>> ordres = new();

		public byte[] Ordres(int client, int image)
		{
			return ordres.TryGetValue(client, out var parImage) && parImage.TryGetValue(image, out var o) ? o : Vide;
		}

		public static string Dossier(ModData modData)
		{
			return Path.Combine(Platform.SupportDir, "Replays", modData.Manifest.Id, modData.Manifest.Metadata.Version);
		}

		/// <summary>Replays repris possibles : parties en ligne (au moins 2 humains), la plus récente d'abord.</summary>
		public static List<(string Fichier, ReplayMetadata Infos)> Lister(ModData modData, int max)
		{
			var resultat = new List<(string, ReplayMetadata)>();
			var dossier = new DirectoryInfo(Dossier(modData));
			if (!dossier.Exists)
				return resultat;

			foreach (var f in dossier.GetFiles("*.orarep").OrderByDescending(f => f.LastWriteTimeUtc))
			{
				if (resultat.Count >= max)
					break;

				try
				{
					var infos = ReplayMetadata.Read(f.FullName);
					if (infos?.GameInfo != null && infos.GameInfo.HumanPlayers.Count() >= 2)
						resultat.Add((f.Name, infos));
				}
				catch (Exception e)
				{
					Log.Write("debug", $"Reprise : replay {f.Name} illisible : {e.Message}");
				}
			}

			return resultat;
		}

		public static RepriseReplay Lire(string chemin)
		{
			var r = new RepriseReplay();
			var deconnexions = r.Deconnexions;
			// image → empreintes reçues (celle du joueur local comprise), avec leur client
			var syncs = new SortedDictionary<int, List<(int Client, byte[] Sync)>>();
			var lancee = false;
			Session salon = null;

			using (var rs = File.OpenRead(chemin))
			{
				while (rs.Length - rs.Position >= 8)
				{
					var client = rs.ReadInt32();
					if (client == ReplayMetadata.MetaStartMarker)
						break;

					var longueur = rs.ReadInt32();
					if (longueur < 4 || rs.Length - rs.Position < longueur)
						break;

					var paquet = rs.ReadBytes(longueur);
					var image = BitConverter.ToInt32(paquet, 0);

					if (OrderIO.TryParseDisconnect((client, paquet), out var deco))
					{
						deconnexions.TryAdd(deco.ClientId, deco.Frame);
						continue;
					}

					if (OrderIO.TryParseSync(paquet, out var sync))
					{
						if (!syncs.TryGetValue(sync.Frame, out var recues))
							syncs[sync.Frame] = recues = new List<(int, byte[])>();

						recues.Add((client, paquet[4..]));
						continue;
					}

					if (image == 0)
					{
						// Ordres immédiats : seul le salon d'avant le lancement nous intéresse.
						if (lancee || !OrderIO.TryParseOrderPacket(paquet, out var immediats))
							continue;

						foreach (var o in immediats.Orders.GetOrders(null))
						{
							switch (o.OrderString)
							{
								case "StartGame":
									lancee = true;
									break;
								case "SyncInfo":
									salon = Session.Deserialize(o.TargetString);
									break;
								case "SyncLobbyClients" when salon != null:
									salon.Clients = MiniYaml.FromString(o.TargetString)
										.Where(n => n.Key.Split('@')[0] == "Client")
										.Select(n => Session.Client.Deserialize(n.Value))
										.ToList();
									break;
								case "SyncLobbySlots" when salon != null:
									salon.Slots = MiniYaml.FromString(o.TargetString)
										.Where(n => n.Key.Split('@')[0] == "Slot")
										.Select(n => Session.Slot.Deserialize(n.Value))
										.ToDictionary(s => s.PlayerReference);
									break;
								case "SyncLobbyGlobalSettings" when salon != null:
									foreach (var n in MiniYaml.FromString(o.TargetString))
										if (n.Key.Split('@')[0] == "GlobalSettings")
											salon.GlobalSettings = Session.Global.Deserialize(n.Value);
									break;
							}
						}

						continue;
					}

					if (!lancee)
						continue;

					if (!r.ordres.TryGetValue(client, out var parImage))
						r.ordres[client] = parImage = new Dictionary<int, byte[]>();

					var donnees = paquet[4..];
					parImage[image] = parImage.TryGetValue(image, out var deja) ? deja.Concat(donnees).ToArray() : donnees;
				}
			}

			if (salon == null || !lancee)
				throw new InvalidDataException("pas de début de partie dans ce replay");

			r.Salon = salon;

			// Le jeu attend un paquet par image de chaque client non-IA, dans l'ordre du salon.
			r.Participants = salon.Clients.Where(c => !c.IsBot).ToList();

			// Désynchronisation : on s'arrête à la dernière image dont l'empreinte a été
			// envoyée, identique, par chaque participant encore connecté.
			var limite = int.MaxValue;
			var desynchro = syncs.FirstOrDefault(kv => kv.Value.Any(e => !e.Sync.AsSpan().SequenceEqual(kv.Value[0].Sync)));
			if (desynchro.Value != null)
			{
				r.ImageDesynchro = desynchro.Key;
				limite = syncs.Keys.LastOrDefault(i => i < desynchro.Key && r.Participants.All(c =>
					(deconnexions.TryGetValue(c.Index, out var d) && d <= i) || syncs[i].Any(e => e.Client == c.Index)));

				// Partie elle-même reprise, désynchronisée pendant le rejeu : rien de sûr ici.
				if (limite <= 0)
					throw new InvalidDataException($"désynchronisation dès l'image {desynchro.Key}, avant tout accord entre les joueurs " +
						"(si c'était déjà une reprise, reprenez depuis le replay d'origine)");
			}

			// Image complète : chaque participant encore connecté y a envoyé ses ordres.
			var derniere = 0;
			while (derniere < limite && r.Participants.All(c =>
				(deconnexions.TryGetValue(c.Index, out var d) && derniere + 1 >= d)
				|| (r.ordres.TryGetValue(c.Index, out var p) && p.ContainsKey(derniere + 1))))
			{
				derniere++;
				if (r.Participants.All(c => deconnexions.TryGetValue(c.Index, out var d) && derniere >= d))
					break;
			}

			var derniereSync = syncs.Keys.LastOrDefault(i => i <= derniere);
			if (derniereSync <= 0)
				throw new InvalidDataException("partie trop courte pour être reprise");

			r.DerniereImage = derniere;
			r.ImageSync = derniereSync;
			r.Sync = syncs[derniereSync][0].Sync;
			return r;
		}

		RepriseReplay() { }
	}
}
