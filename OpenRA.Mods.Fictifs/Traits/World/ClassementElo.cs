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
using System.Net;
using System.Text;
using System.Text.Json;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Enregistre le résultat de chaque escarmouche/partie en ligne et met à jour le classement Elo (fictifs-elo.json).")]
	public class ClassementEloInfo : TraitInfo
	{
		[Desc("Elo fixe de chaque type d'IA (Type du ModularBot). Les IA ne gagnent ni ne perdent de points.")]
		public readonly Dictionary<string, int> EloIa = new()
		{
			{ "turtle", 900 },
			{ "naval", 1000 },
			{ "normal", 1100 },
			{ "rush", 1200 },
		};

		[Desc("Elo d'une IA dont le type n'est pas listé.")]
		public readonly int EloIaParDefaut = 1000;

		public int EloDeIa(string type)
		{
			return type != null && EloIa.TryGetValue(type, out var elo) ? elo : EloIaParDefaut;
		}

		public override object Create(ActorInitializer init) { return new ClassementElo(this); }
	}

	public class ClassementElo : IGameOver
	{
		readonly ClassementEloInfo info;

		public ClassementElo(ClassementEloInfo info) { this.info = info; }

		void IGameOver.GameOver(World world)
		{
			try
			{
				Enregistrer(world);
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Classement Elo : enregistrement impossible : {e}");
			}
		}

		void Enregistrer(World world)
		{
			if (world.Type != WorldType.Regular || world.IsReplay || world.Map.Visibility.HasFlag(MapVisibility.MissionSelector))
				return;

			var joueurs = world.Players
				// Pas de test sur Spectating : il devient vrai dès qu'un joueur a gagné ou perdu.
				.Where(p => p.Playable && !p.NonCombatant)
				.Select(p =>
				{
					var client = world.LobbyInfo.ClientWithIndex(p.ClientIndex);
					return new ParticipantElo
					{
						Nom = p.PlayerName,
						Ia = p.IsBot ? p.BotType : null,
						EloIa = p.IsBot ? info.EloDeIa(p.BotType) : 0,
						Equipe = client?.Team ?? 0,
						Faction = p.Faction.InternalName,
						Resultat = p.WinState == WinState.Won ? "V" : p.WinState == WinState.Lost ? "D" : "N",
					};
				})
				.ToList();

			if (!joueurs.Any(j => j.Ia == null))
				return;

			// Même identifiant chez tous les participants d'une partie en ligne.
			var signature = string.Join(";", joueurs.Select(j => j.Ia ?? j.Nom).OrderBy(n => n, StringComparer.Ordinal));
			var partie = new PartieElo
			{
				Id = CryptoUtil.SHA1Hash($"{world.LobbyInfo.GlobalSettings.RandomSeed}|{world.Map.Uid}|{signature}")[..16],
				Date = DateTime.UtcNow,
				Carte = world.Map.Title,
				Joueurs = joueurs,
			};

			if (!ClassementEloStore.PartieCompte(partie))
				return;

			var humains = joueurs.Where(j => j.Ia == null).Select(j => j.Nom).Distinct().ToList();
			var avant = humains.ToDictionary(n => n, ClassementEloStore.Fiche);
			if (!ClassementEloStore.Ajouter(partie))
				return;

			foreach (var nom in humains)
			{
				var a = avant[nom];
				var b = ClassementEloStore.Fiche(nom);
				var ecart = b.Elo - a.Elo;
				var grade = ClassementEloStore.Grade(b.Elo);
				TextNotificationsManager.AddSystemLine("Classement",
					$"{nom} : {a.Elo} → {b.Elo} ({(ecart >= 0 ? "+" : "")}{ecart} Elo) — {grade.Nom}");

				var ancien = ClassementEloStore.Grade(a.Elo);
				if (ancien.Nom != grade.Nom)
					TextNotificationsManager.AddSystemLine("Classement",
						b.Elo > a.Elo ? $"{nom} est promu {grade.Nom} !" : $"{nom} redescend {grade.Nom}.");
			}
		}
	}

	public class ParticipantElo
	{
		public string Nom { get; set; }

		/// <summary>Type d'IA, null pour un humain.</summary>
		public string Ia { get; set; }
		public int EloIa { get; set; }
		public int Equipe { get; set; }
		public string Faction { get; set; }

		/// <summary>V = victoire, D = défaite, N = indécis.</summary>
		public string Resultat { get; set; }
	}

	public class PartieElo
	{
		public string Id { get; set; }
		public DateTime Date { get; set; }
		public string Carte { get; set; }
		public List<ParticipantElo> Joueurs { get; set; }
	}

	public class FicheElo
	{
		public string Nom;
		public int Elo = ClassementEloStore.EloDepart;
		public int Parties;
		public int Victoires;
		public int Defaites;

		public FicheElo Copie() { return (FicheElo)MemberwiseClone(); }
	}

	/// <summary>
	/// Historique des parties classées (SupportDir/fictifs-elo.json). L'Elo n'est pas stocké :
	/// il est recalculé en rejouant l'historique dans l'ordre, si bien que deux PC qui ont
	/// le même historique (fusionné via l'hôte) affichent exactement les mêmes chiffres.
	/// </summary>
	public static class ClassementEloStore
	{
		public const int EloDepart = 1000;
		public const string CheminHttp = "/fictifs-elo/";
		const string Fichier = "fictifs-elo.json";

		public static readonly (int Min, string Nom, string Image)[] Grades =
		{
			(int.MinValue, "Soldat", "soldat"),
			(1050, "Caporal", "caporal"),
			(1100, "Sergent", "sergent"),
			(1175, "Lieutenant", "lieutenant"),
			(1250, "Capitaine", "capitaine"),
			(1350, "Commandant", "commandant"),
			(1450, "Colonel", "colonel"),
			(1575, "Général", "general"),
			(1700, "Maréchal", "marechal"),
		};

		static readonly object Verrou = new();
		static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

		static List<PartieElo> parties;
		static Dictionary<string, FicheElo> fiches;

		/// <summary>Incrémenté à chaque changement, pour rafraîchir l'affichage.</summary>
		public static int Version { get; private set; }

		static string Chemin => Path.Combine(Platform.SupportDir, Fichier);

		public static (int Min, string Nom, string Image) Grade(int elo)
		{
			return Grades.Last(g => elo >= g.Min);
		}

		public static FicheElo Fiche(string nom)
		{
			lock (Verrou)
			{
				Charger();
				return fiches.TryGetValue(nom, out var f) ? f.Copie() : new FicheElo { Nom = nom };
			}
		}

		/// <summary>
		/// Elo d'un camp de plusieurs joueurs : leurs forces 10^(Elo/400) s'additionnent
		/// (et non une moyenne, qui rendait un camp de deux joueurs plus faible que le
		/// meilleur des deux seul). Deux joueurs à 1000 valent ainsi un joueur à 1120 :
		/// seul contre eux, on a une chance sur trois.
		/// </summary>
		public static double EloDuCamp(IEnumerable<double> elos)
		{
			return 400 * Math.Log10(elos.Sum(e => Math.Pow(10, e / 400)));
		}

		/// <summary>Part des points gagnés en plus par adversaire au-delà du premier.</summary>
		public const double BonusParAdversaire = 0.10;

		/// <summary>
		/// Chances de victoire du camp i parmi tous les camps (forces = EloDuCamp de chacun) :
		/// sa force divisée par la somme des forces. Le total fait 1 ; avec deux camps,
		/// c'est Attendu.
		/// </summary>
		public static double Chances(IList<double> forces, int i)
		{
			var max = forces.Max();
			return Math.Pow(10, (forces[i] - max) / 400) / forces.Sum(f => Math.Pow(10, (f - max) / 400));
		}

		/// <summary>Probabilité de victoire attendue d'un camp d'Elo a contre un camp d'Elo b.</summary>
		public static double Attendu(double a, double b)
		{
			return 1 / (1 + Math.Pow(10, (b - a) / 400));
		}

		public static bool Ajouter(PartieElo partie)
		{
			return Fusionner(new[] { partie }, false) > 0;
		}

		/// <summary>
		/// Ajoute les parties inconnues. Avec <paramref name="autorite"/> (données de l'hôte),
		/// les dates des parties déjà connues sont alignées sur celles de l'hôte pour que
		/// l'ordre de calcul soit le même partout.
		/// </summary>
		public static int Fusionner(IEnumerable<PartieElo> autres, bool autorite)
		{
			lock (Verrou)
			{
				Charger();
				var connues = parties.ToDictionary(p => p.Id);
				var ajoutees = 0;
				var modifie = false;
				foreach (var p in autres)
				{
					if (p?.Id == null || p.Joueurs == null || !PartieCompte(p))
						continue;

					if (connues.TryGetValue(p.Id, out var existante))
					{
						if (autorite && existante.Date != p.Date)
						{
							existante.Date = p.Date;
							modifie = true;
						}

						continue;
					}

					parties.Add(p);
					connues[p.Id] = p;
					ajoutees++;
				}

				if (ajoutees > 0 || modifie)
				{
					Recalculer();
					Sauver();
				}

				return ajoutees;
			}
		}

		public static string Exporter()
		{
			lock (Verrou)
			{
				Charger();
				return JsonSerializer.Serialize(parties, Options);
			}
		}

		public static List<PartieElo> Lire(string json)
		{
			return JsonSerializer.Deserialize<List<PartieElo>>(json) ?? new List<PartieElo>();
		}

		/// <summary>Une partie compte s'il y a au moins deux camps et qu'ils n'ont pas tous le même résultat.</summary>
		public static bool PartieCompte(PartieElo partie)
		{
			var camps = Camps(partie);
			return camps.Count >= 2 && camps.Select(c => Score(c)).Distinct().Count() > 1
				&& partie.Joueurs.Any(j => j.Ia == null);
		}

		// Équipe 0 = chacun pour soi.
		static List<List<ParticipantElo>> Camps(PartieElo partie)
		{
			return partie.Joueurs
				.Select((j, i) => (j, cle: j.Equipe > 0 ? "E" + j.Equipe : "J" + i))
				.GroupBy(x => x.cle)
				.Select(g => g.Select(x => x.j).ToList())
				.ToList();
		}

		static double Score(List<ParticipantElo> camp)
		{
			return camp.Any(j => j.Resultat == "V") ? 1 : camp.All(j => j.Resultat == "D") ? 0 : 0.5;
		}

		static void Recalculer()
		{
			fiches = new Dictionary<string, FicheElo>();
			FicheElo FicheDe(string nom)
			{
				if (!fiches.TryGetValue(nom, out var f))
					fiches[nom] = f = new FicheElo { Nom = nom };
				return f;
			}

			double EloDe(ParticipantElo j) => j.Ia != null ? j.EloIa : FicheDe(j.Nom).Elo;

			foreach (var partie in parties.OrderBy(p => p.Date).ThenBy(p => p.Id, StringComparer.Ordinal))
			{
				var camps = Camps(partie);
				var forces = camps.Select(c => EloDuCamp(c.Select(EloDe))).ToList();
				var scores = camps.Select(Score).ToList();

				// Chaque camp est comparé à tous les autres réunis : plus il y a
				// d'adversaires, plus ses chances sont faibles, plus une victoire
				// rapporte et moins une défaite coûte. En 1 contre 1, c'est l'Elo classique.
				var ecarts = new double[camps.Count];
				for (var i = 0; i < camps.Count; i++)
					ecarts[i] = scores[i] - Chances(forces, i);

				for (var i = 0; i < camps.Count; i++)
				{
					foreach (var j in camps[i].Where(j => j.Ia == null).DistinctBy(j => j.Nom))
					{
						var f = FicheDe(j.Nom);
						var k = f.Parties < 10 ? 40 : 24;
						var gain = k * ecarts[i];

						// Bonus des parties à plusieurs : +10 % des points gagnés par
						// adversaire au-delà du premier (les défaites ne coûtent pas plus).
						var adversaires = partie.Joueurs.Count - camps[i].Count;
						if (gain > 0 && adversaires > 1)
							gain *= 1 + BonusParAdversaire * (adversaires - 1);

						f.Elo += (int)Math.Round(gain);
						f.Parties++;
						if (scores[i] == 1)
							f.Victoires++;
						else if (scores[i] == 0)
							f.Defaites++;
					}
				}
			}

			Version++;
		}

		static void Charger()
		{
			if (parties != null)
				return;

			parties = new List<PartieElo>();
			try
			{
				if (File.Exists(Chemin))
					parties = Lire(File.ReadAllText(Chemin));
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Classement Elo illisible ({Chemin}) : {e.Message}");
				try
				{
					File.Copy(Chemin, Chemin + ".illisible", true);
				}
				catch (Exception)
				{
					// Tant pis : on repart d'un historique vide.
				}
			}

			Recalculer();
		}

		static void Sauver()
		{
			try
			{
				var temporaire = Chemin + ".tmp";
				File.WriteAllText(temporaire, JsonSerializer.Serialize(parties, Options));
				File.Move(temporaire, Chemin, true);
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Classement Elo : sauvegarde impossible : {e.Message}");
			}
		}

		/// <summary>Côté hôte : GET renvoie l'historique, POST y fusionne celui d'un joueur.</summary>
		public static void ServirHttp(HttpListenerContext context)
		{
			if (context.Request.HttpMethod == "POST")
			{
				using var lecteur = new StreamReader(context.Request.InputStream, Encoding.UTF8);
				var ajoutees = Fusionner(Lire(lecteur.ReadToEnd()), false);
				Log.Write("debug", $"Classement Elo : {ajoutees} partie(s) reçue(s) d'un joueur");
			}

			var octets = Encoding.UTF8.GetBytes(Exporter());
			context.Response.ContentType = "application/json; charset=utf-8";
			context.Response.ContentLength64 = octets.Length;
			context.Response.OutputStream.Write(octets, 0, octets.Length);
		}
	}
}
