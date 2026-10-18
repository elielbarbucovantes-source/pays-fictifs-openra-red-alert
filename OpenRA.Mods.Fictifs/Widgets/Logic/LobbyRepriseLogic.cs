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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Fictifs.ServerTraits;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	/// <summary>
	/// Bouton « Reprendre une partie » du salon (hôte seulement) : liste les replays des
	/// dernières parties en ligne et charge celle choisie dans le salon (commande
	/// « reprise », voir ReprisePartieServeur).
	/// </summary>
	public class LobbyRepriseLogic : ChromeLogic
	{
		sealed class Choix
		{
			public string Fichier;
			public string Titre;
		}

		[ObjectCreator.UseCtor]
		public LobbyRepriseLogic(Widget widget, ModData modData, OrderManager orderManager)
		{
			var bouton = widget.Get<DropDownButtonWidget>("REPRISE_BUTTON");

			widget.IsVisible = () => Game.IsHost;
			bouton.IsDisabled = () => orderManager.LocalClient == null || orderManager.LocalClient.IsReady;

			bouton.OnMouseDown = _ =>
			{
				var choix = RepriseReplay.Lister(modData, 10).Select(r =>
				{
					var infos = r.Infos.GameInfo;
					var joueurs = string.Join(", ", infos.HumanPlayers.Select(p => p.Name));
					var duree = TimeSpan.FromSeconds(infos.FinalGameTick / 25);
					return new Choix
					{
						Fichier = r.Fichier,
						Titre = $"{infos.StartTimeUtc.ToLocalTime():dd/MM HH:mm} · {infos.MapTitle} · {(int)duree.TotalMinutes} min · {joueurs}"
					};
				}).ToList();

				if (choix.Count == 0)
					choix.Add(new Choix { Titre = "Aucune partie en ligne à reprendre" });

				ScrollItemWidget Element(Choix c, ScrollItemWidget modele)
				{
					var item = ScrollItemWidget.Setup(modele, () => false, () =>
					{
						if (c.Fichier != null)
							orderManager.IssueOrder(Order.Command("reprise " + c.Fichier));
					});

					item.Get<LabelWidget>("LABEL").GetText = () => c.Titre;
					return item;
				}

				bouton.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", Math.Min(choix.Count, 8) * 25 + 2, choix, Element);
			};
		}
	}
}
