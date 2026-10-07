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
using OpenRA.Graphics;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Orders
{
	// Programmation d'une navette (bouton « Navette ») : clic gauche sur la gare de chargement,
	// puis sur la gare de déchargement. Clic droit : annuler.
	public class NavetteOrderGenerator : IOrderGenerator
	{
		const string Cursor = "ability";
		const string BlockedCursor = "move-blocked";

		Actor loadGare;

		public NavetteOrderGenerator(World world)
		{
			TextNotificationsManager.AddTransientLine("Navette : cliquez sur la gare de CHARGEMENT.", world.LocalPlayer);
		}

		public static IEnumerable<(Actor Actor, Navette Trait)> Trains(World world)
		{
			return world.Selection.Actors
				.Where(a => a.IsInWorld && !a.IsDead && a.Owner == world.LocalPlayer)
				.Select(a => (a, a.TraitOrDefault<Navette>()))
				.Where(p => p.Item2 != null && p.Item2.Usable);
		}

		static Actor GareAt(World world, CPos cell)
		{
			return world.ActorMap.GetActorsAt(cell)
				.FirstOrDefault(a => !a.IsDead && a.Owner == world.LocalPlayer && a.Info.HasTraitInfo<GareStockInfo>());
		}

		IEnumerable<Order> IOrderGenerator.Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
			{
				world.CancelInputMode();
				yield break;
			}

			if (mi.Button != MouseButton.Left || mi.Event != MouseInputEvent.Down)
				yield break;

			var gare = GareAt(world, cell);
			if (gare == null || gare == loadGare)
				yield break;

			if (loadGare == null)
			{
				loadGare = gare;
				TextNotificationsManager.AddTransientLine("Navette : cliquez sur la gare de DÉCHARGEMENT.", world.LocalPlayer);
				yield break;
			}

			var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
			foreach (var (a, _) in Trains(world))
				yield return new Order(Navette.ProgramOrder, a, Target.FromActor(loadGare), queued) { ExtraData = gare.ActorID };

			world.CancelInputMode();
		}

		void IOrderGenerator.Tick(World world)
		{
			if (!Trains(world).Any() || (loadGare != null && (loadGare.IsDead || !loadGare.IsInWorld)))
				world.CancelInputMode();
		}

		void IOrderGenerator.SelectionChanged(World world, IEnumerable<Actor> selected) { }

		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world) { yield break; }

		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world)
		{
			if (loadGare == null || !loadGare.IsInWorld)
				yield break;

			foreach (var (a, t) in Trains(world))
				yield return new TargetLineRenderable(new[] { a.CenterPosition, loadGare.CenterPosition }, t.Info.LoadColor, 2, 4);
		}

		IEnumerable<IRenderable> IOrderGenerator.RenderAnnotations(WorldRenderer wr, World world) { yield break; }

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var gare = GareAt(world, cell);
			return gare != null && gare != loadGare ? Cursor : BlockedCursor;
		}

		bool IOrderGenerator.HandleKeyPress(KeyInput e) { return false; }

		void IOrderGenerator.Deactivate() { }
	}
}
