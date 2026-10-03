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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.MapGen;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	/// <summary>
	/// Bouton « Aléatoire » du salon : ouvre le générateur de cartes, et la carte
	/// retenue (« Jouer cette carte ») devient aussitôt la carte du salon, sans
	/// passer par le sélecteur de cartes. Les autres joueurs la récupèrent chez
	/// l'hôte (CarteHoteLogic).
	/// </summary>
	public class LobbyCarteAleatoireLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public LobbyCarteAleatoireLogic(Widget widget, ModData modData, OrderManager orderManager)
		{
			var button = widget.Get<ButtonWidget>("CARTE_ALEATOIRE_BUTTON");

			var hasGenerator = modData.DefaultRules.Actors[SystemActors.EditorWorld].HasTraitInfo<IEditorMapGeneratorInfo>();
			widget.IsVisible = () => hasGenerator;

			// Mêmes conditions que « Changer », mais réservé à l'hôte : lui seul choisit la carte.
			button.IsDisabled = () => !Game.IsHost || orderManager.LocalClient == null || orderManager.LocalClient.IsReady;

			button.OnClick = () =>
			{
				Ui.OpenWindow("RANDOM_MAP_GENERATOR_PANEL", new WidgetArgs()
				{
					{ "onExit", () => { } },
					{
						"onSelect", (Action<string>)(uid =>
						{
							if (uid == orderManager.LobbyInfo.GlobalSettings.Map || modData.MapCache[uid].Status != MapStatus.Available)
								return;

							orderManager.IssueOrder(Order.Command("map " + uid));
							Game.Settings.Server.Map = uid;
							Game.Settings.Save();
						})
					},
				});
			};
		}
	}
}
