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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Fictifs.Orders;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	// Boutons au-dessus de la barre de commandes, visibles selon la sélection :
	//  - « Navette » (locomotive) : programmer la navette (2 clics) ; si les trains sélectionnés ont
	//    un programme arrêté, le bouton le relance ;
	//  - « Vider la gare » (gare) : tout le stock sort vers le point de ralliement de la gare.
	public class NavetteButtonsLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public NavetteButtonsLogic(Widget widget, World world)
		{
			var navette = widget.Get<ButtonWidget>("NAVETTE");
			navette.IsVisible = () => world.LocalPlayer != null && NavetteOrderGenerator.Trains(world).Any();
			navette.IsHighlighted = () => world.OrderGenerator is NavetteOrderGenerator;
			navette.OnClick = () =>
			{
				if (world.OrderGenerator is NavetteOrderGenerator)
				{
					world.CancelInputMode();
					return;
				}

				var trains = NavetteOrderGenerator.Trains(world).ToList();
				if (trains.Count == 0)
					return;

				if (trains.All(t => t.Trait.HasProgram && !t.Trait.Active))
				{
					foreach (var (a, _) in trains)
						world.IssueOrder(new Order(Navette.ResumeOrder, a, false));

					return;
				}

				world.OrderGenerator = new NavetteOrderGenerator(world);
			};

			var vider = widget.Get<ButtonWidget>("VIDER_GARE");
			vider.IsVisible = () => world.LocalPlayer != null && Gares(world).Any();
			vider.IsDisabled = () => !Gares(world).Any(g => !g.Trait<GareStock>().IsEmpty);
			vider.OnClick = () =>
			{
				foreach (var g in Gares(world))
					world.IssueOrder(new Order(GareStock.EmptyOrder, g, false));
			};
		}

		static IEnumerable<Actor> Gares(World world)
		{
			return world.Selection.Actors.Where(a => a.IsInWorld && !a.IsDead && a.Owner == world.LocalPlayer
				&& a.Info.HasTraitInfo<GareStockInfo>());
		}
	}
}
