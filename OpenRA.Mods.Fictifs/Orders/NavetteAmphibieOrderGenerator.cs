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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Orders
{
	// Programmation d'une navette amphibie (bouton « Navette amphibie ») : escale de CHARGEMENT puis
	// escale de DÉCHARGEMENT. Pour chacune : clic sur une Base logistique amphibie du joueur, ou rectangle
	// glissé sur une plage (simple clic ailleurs : carré autour du point). Clic droit : annuler.
	public class NavetteAmphibieOrderGenerator : IOrderGenerator
	{
		const string Cursor = "ability";
		const string BlockedCursor = "move-blocked";

		readonly ExtendedLandingManager manager;
		readonly Locomotor foot;
		CPos? start;
		Escale load;

		public NavetteAmphibieOrderGenerator(World world)
		{
			manager = world.WorldActor.Trait<ExtendedLandingManager>();
			foot = manager.LocomotorByName("foot");
			TextNotificationsManager.AddTransientLine(
				"Navette amphibie : escale de CHARGEMENT (clic sur une base logistique, ou rectangle sur une plage).", world.LocalPlayer);
		}

		public static IEnumerable<(Actor Actor, NavetteAmphibie Trait)> Transports(World world)
		{
			return world.Selection.Actors
				.Where(a => a.IsInWorld && !a.IsDead && a.Owner == world.LocalPlayer)
				.Select(a => (a, a.TraitOrDefault<NavetteAmphibie>()))
				.Where(p => p.Item2 != null && p.Item2.Usable);
		}

		static Actor BaseAt(World world, CPos cell)
		{
			return world.ActorMap.GetActorsAt(cell)
				.FirstOrDefault(a => !a.IsDead && a.Owner == world.LocalPlayer && a.Info.HasTraitInfo<AmphibiousBaseInfo>());
		}

		bool HasLand(CPos tl, CPos br)
		{
			return foot == null || manager.LandCells(tl, br, new[] { foot }).Count > 0;
		}

		Escale EscaleFor(World world, CPos a, CPos b)
		{
			if (a == b)
			{
				var ab = BaseAt(world, a);
				if (ab != null)
					return load != null && load.Base == ab ? null : new Escale(ab);
			}

			var (tl, br) = manager.Zone(a, b);
			return HasLand(tl, br) ? new Escale(tl, br) : null;
		}

		IEnumerable<Order> IOrderGenerator.Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
			{
				world.CancelInputMode();
				yield break;
			}

			if (mi.Button != MouseButton.Left)
				yield break;

			if (mi.Event == MouseInputEvent.Down)
			{
				start = world.Map.Clamp(cell);
				yield break;
			}

			if (mi.Event != MouseInputEvent.Up || start == null)
				yield break;

			var escale = EscaleFor(world, start.Value, world.Map.Clamp(cell));
			start = null;
			if (escale == null)
				yield break;

			if (load == null)
			{
				load = escale;
				TextNotificationsManager.AddTransientLine(
					"Navette amphibie : escale de DÉCHARGEMENT (clic sur une base logistique, ou rectangle sur une plage).", world.LocalPlayer);
				yield break;
			}

			var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
			foreach (var (a, _) in Transports(world))
				yield return NavetteAmphibie.MakeOrder(a, load, escale, queued);

			world.CancelInputMode();
		}

		void IOrderGenerator.Tick(World world)
		{
			if (!Transports(world).Any() || (load != null && !load.Valid(world.LocalPlayer)))
				world.CancelInputMode();
		}

		void IOrderGenerator.SelectionChanged(World world, IEnumerable<Actor> selected) { }

		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world) { yield break; }

		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world)
		{
			if (load == null)
				yield break;

			var color = Color.FromArgb(255, 80, 220, 80);
			var to = load.Center(world);
			foreach (var (a, _) in Transports(world))
				yield return new TargetLineRenderable(new[] { a.CenterPosition, to }, color, 2, 4);
		}

		IEnumerable<IRenderable> IOrderGenerator.RenderAnnotations(WorldRenderer wr, World world)
		{
			var loadColor = Color.FromArgb(255, 80, 220, 80);
			if (load != null && !load.IsBase)
				foreach (var r in NavetteAmphibie.ZoneOutline(world, load.TopLeft, load.BottomRight, loadColor))
					yield return r;

			var mouse = world.Map.Clamp(wr.Viewport.ViewToWorld(Viewport.LastMousePos));
			if (start == null && BaseAt(world, mouse) != null)
				yield break;

			var (tl, br) = manager.Zone(start ?? mouse, mouse);
			var color = load == null ? loadColor : Color.FromArgb(255, 240, 150, 40);
			foreach (var r in NavetteAmphibie.ZoneOutline(world, tl, br, HasLand(tl, br) ? color : Color.Red))
				yield return r;
		}

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (!world.Map.Contains(cell))
				return BlockedCursor;

			var ab = BaseAt(world, cell);
			if (ab != null)
				return load != null && load.Base == ab ? BlockedCursor : Cursor;

			return Cursor;
		}

		bool IOrderGenerator.HandleKeyPress(KeyInput e) { return false; }

		void IOrderGenerator.Deactivate() { }
	}
}
