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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	/// <summary>
	/// Encadré « Classement Elo » du salon : grade et Elo de chaque joueur, chances de victoire.
	/// Un joueur qui rejoint une partie en ligne récupère l'historique de l'hôte et lui envoie
	/// le sien, pour que les deux PC aient le même classement.
	/// </summary>
	public class ClassementSalonLogic : ChromeLogic
	{
		static readonly Color Gris = Color.FromArgb(170, 170, 170);

		readonly OrderManager orderManager;
		readonly ClassementEloInfo info;
		readonly ScrollPanelWidget liste;
		readonly Widget titre;
		readonly Widget modeleLigne;
		readonly LabelWidget modeleTexte;

		bool lobbyChange = true;
		int versionAffichee = -1;
		volatile string etat;
		string etatAffiche;

		[ObjectCreator.UseCtor]
		public ClassementSalonLogic(Widget widget, ModData modData, OrderManager orderManager)
		{
			this.orderManager = orderManager;
			info = modData.DefaultRules.Actors[SystemActors.World].TraitInfoOrDefault<ClassementEloInfo>();
			widget.IsVisible = () => info != null;

			liste = widget.Get<ScrollPanelWidget>("CLASSEMENT_LISTE");
			titre = liste.Get("TITRE");
			modeleLigne = liste.Get("TEMPLATE_LIGNE");
			modeleTexte = liste.Get<LabelWidget>("TEMPLATE_TEXTE");
			liste.RemoveChildren();

			Game.LobbyInfoChanged += SurChangement;

			var endPoint = (orderManager?.Connection as NetworkConnection)?.EndPoint;
			if (endPoint != null && IPAddress.IsLoopback(endPoint.Address))
				CarteHoteLogic.StartServer(modData);
			else if (endPoint != null)
			{
				var hote = endPoint.Address.IsIPv4MappedToIPv6 ? endPoint.Address.MapToIPv4() : endPoint.Address;
				etat = "Synchronisation avec l'hôte…";
				Task.Run(() => Synchroniser(hote));
			}
		}

		void SurChangement() { lobbyChange = true; }

		async Task Synchroniser(IPAddress hote)
		{
			try
			{
				var adresse = hote.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? $"[{hote}]" : hote.ToString();
				var url = $"http://{adresse}:{CarteHoteLogic.Port}{ClassementEloStore.CheminHttp}";
				var client = HttpClientFactory.Create();

				using (var delai = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
				{
					var json = await client.GetStringAsync(url, delai.Token);
					ClassementEloStore.Fusionner(ClassementEloStore.Lire(json), true);
				}

				// On envoie notre historique (fusionné) ; l'hôte répond avec le sien, complété.
				using (var delai = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
				{
					var contenu = new StringContent(ClassementEloStore.Exporter(), Encoding.UTF8, "application/json");
					var reponse = await client.PostAsync(url, contenu, delai.Token);
					reponse.EnsureSuccessStatusCode();
					var json = await reponse.Content.ReadAsStringAsync();
					ClassementEloStore.Fusionner(ClassementEloStore.Lire(json), true);
				}

				etat = "Synchronisé avec l'hôte";
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Classement Elo : synchronisation avec l'hôte impossible : {e.Message}");
				etat = "Hôte injoignable : classement local";
			}
		}

		public override void Tick()
		{
			if (info != null && (lobbyChange || versionAffichee != ClassementEloStore.Version || etat != etatAffiche))
				Reconstruire();
		}

		void Reconstruire()
		{
			lobbyChange = false;
			versionAffichee = ClassementEloStore.Version;
			etatAffiche = etat;

			liste.RemoveChildren();
			liste.AddChild(titre);

			var clients = orderManager.LobbyInfo.Clients
				.Where(c => c.Slot != null)
				.OrderBy(c => c.Team == 0 ? int.MaxValue : c.Team)
				.ThenBy(c => c.Index)
				.ToList();

			foreach (var c in clients)
			{
				var fiche = c.IsBot ? null : ClassementEloStore.Fiche(c.Name);
				var elo = c.IsBot ? info.EloDeIa(c.Bot) : fiche.Elo;
				var grade = ClassementEloStore.Grade(elo);

				var ligne = modeleLigne.Clone();
				ligne.Get<ImageWidget>("INSIGNE").GetImageName = () => grade.Image;

				var nom = ligne.Get<LabelWidget>("NOM");
				var nomCourt = WidgetUtils.TruncateText(c.Name, nom.Bounds.Width, Game.Renderer.Fonts[nom.Font]);
				nom.GetText = () => nomCourt;
				nom.GetColor = () => c.Color;

				var texteElo = elo.ToString();
				ligne.Get<LabelWidget>("ELO").GetText = () => texteElo;

				var detail = c.IsBot ? $"{grade.Nom} · IA, Elo fixe"
					: fiche.Parties == 0 ? $"{grade.Nom} · aucune partie"
					: $"{grade.Nom} · {fiche.Victoires} V / {fiche.Defaites} D";
				if (c.Team > 0)
					detail += $" · Équipe {c.Team}";

				var labelDetail = ligne.Get<LabelWidget>("DETAIL");
				labelDetail.GetText = () => detail;
				labelDetail.GetColor = () => Gris;

				ligne.IsVisible = () => true;
				liste.AddChild(ligne);
			}

			// Chances de victoire de chaque camp (total 100 %), dès qu'il y a deux camps.
			var camps = clients
				.Select((c, i) => (c, cle: c.Team > 0 ? "E" + c.Team : "J" + i))
				.GroupBy(x => x.cle)
				.Select(g => g.Select(x => x.c).ToList())
				.ToList();

			if (camps.Count >= 2)
			{
				var forces = camps.Select(camp => ClassementEloStore.EloDuCamp(
					camp.Select(c => (double)(c.IsBot ? info.EloDeIa(c.Bot) : ClassementEloStore.Fiche(c.Name).Elo)))).ToList();

				string Nom(System.Collections.Generic.List<Session.Client> camp) =>
					camp.Count == 1 ? camp[0].Name : $"Équipe {camp[0].Team}";

				AjouterTexte("Chances de victoire :", null);
				for (var i = 0; i < camps.Count; i++)
					AjouterTexte($"{Nom(camps[i])} : {Math.Round(ClassementEloStore.Chances(forces, i) * 100)} %", null);
			}

			if (etatAffiche != null)
				AjouterTexte(etatAffiche, Gris);
		}

		void AjouterTexte(string texte, Color? couleur)
		{
			var label = (LabelWidget)modeleTexte.Clone();
			var court = WidgetUtils.TruncateText(texte, label.Bounds.Width, Game.Renderer.Fonts[label.Font]);
			label.GetText = () => court;
			if (couleur != null)
				label.GetColor = () => couleur.Value;

			label.IsVisible = () => true;
			liste.AddChild(label);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Game.LobbyInfoChanged -= SurChangement;

			base.Dispose(disposing);
		}
	}
}
