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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Fictifs.Orders;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	// Bouton « Débarquement » au-dessus de la barre de commandes : visible quand des transports
	// chargés ou une Base logistique amphibie sont sélectionnés (voir ExtendedLandingOrderGenerator).
	// Bouton « Navette amphibie » : programmer la navette des transports sélectionnés (2 escales) ;
	// si leur programme est arrêté, le bouton le relance.
	public class DebarquementButtonsLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public DebarquementButtonsLogic(Widget widget, World world)
		{
			var button = widget.Get<ButtonWidget>("DEBARQUEMENT");
			button.IsVisible = () => ExtendedLandingOrderGenerator.Available(world);
			button.IsDisabled = () => ExtendedLandingOrderGenerator.Transports(world).Count == 0;
			button.IsHighlighted = () => world.OrderGenerator is ExtendedLandingOrderGenerator;
			button.OnClick = () =>
			{
				if (world.OrderGenerator is ExtendedLandingOrderGenerator)
					world.CancelInputMode();
				else if (ExtendedLandingOrderGenerator.Transports(world).Count > 0)
					world.OrderGenerator = new ExtendedLandingOrderGenerator(world);
			};

			var navette = widget.Get<ButtonWidget>("NAVETTE_AMPHIBIE");
			navette.IsVisible = () => world.LocalPlayer != null && NavetteAmphibieOrderGenerator.Transports(world).Any();
			navette.IsHighlighted = () => world.OrderGenerator is NavetteAmphibieOrderGenerator;
			navette.OnClick = () =>
			{
				if (world.OrderGenerator is NavetteAmphibieOrderGenerator)
				{
					world.CancelInputMode();
					return;
				}

				var transports = NavetteAmphibieOrderGenerator.Transports(world).ToList();
				if (transports.Count == 0)
					return;

				if (transports.All(t => t.Trait.HasProgram && !t.Trait.Active))
				{
					foreach (var (a, _) in transports)
						world.IssueOrder(new Order(NavetteAmphibie.ResumeOrder, a, false));

					return;
				}

				world.OrderGenerator = new NavetteAmphibieOrderGenerator(world);
			};
		}
	}
}
