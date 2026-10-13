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
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Orders
{
	// Bouton « Débarquement » (touche K) : appuyer, glisser, relâcher = rectangle de débarquement étendu ;
	// simple clic = débarquement concentré autour du point. Clic droit : annuler.
	// Commande les transports chargés sélectionnés, ou ceux des bases logistiques sélectionnées.
	public class ExtendedLandingOrderGenerator : IOrderGenerator
	{
		const string Cursor = "ability";
		const string BlockedCursor = "move-blocked";

		readonly ExtendedLandingManager manager;
		readonly (Sprite Sprite, float Alpha) valid, invalid;

		CPos? start;

		public ExtendedLandingOrderGenerator(World world)
		{
			manager = world.WorldActor.Trait<ExtendedLandingManager>();
			valid = DigsCanals.OverlaySprite(world, "build-valid");
			invalid = DigsCanals.OverlaySprite(world, "build-invalid");
			TextNotificationsManager.AddTransientLine(
				"Débarquement : glissez un rectangle sur la côte (étendu) ou cliquez un point (concentré).", world.LocalPlayer);
		}

		// Transports commandés : (transport, base qui lance l'opération ou null).
		public static List<(Actor Transport, Actor Base)> Transports(World world)
		{
			var selected = world.Selection.Actors.Where(a => a.IsInWorld && !a.IsDead && a.Owner == world.LocalPlayer).ToList();
			var list = selected
				.Where(a => a.TraitOrDefault<ExtendedLanding>()?.CanLand(a) == true)
				.Select(a => (a, (Actor)null))
				.ToList();

			foreach (var b in selected.Where(a => a.Info.HasTraitInfo<AmphibiousBaseInfo>()))
				foreach (var t in b.Trait<AmphibiousBase>().LoadedTransports())
					if (!list.Any(p => p.Item1 == t))
						list.Add((t, b));

			return list;
		}

		static List<Actor> Bases(World world)
		{
			return world.Selection.Actors.Where(a => a.IsInWorld && !a.IsDead && a.Owner == world.LocalPlayer
				&& a.Info.HasTraitInfo<AmphibiousBaseInfo>()).ToList();
		}

		public static bool Available(World world)
		{
			return world.LocalPlayer != null && (Transports(world).Count > 0 || Bases(world).Count > 0);
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

			var end = world.Map.Clamp(cell);
			var transports = Transports(world);
			var (tl, br) = manager.Zone(start.Value, end);
			if (transports.Count == 0 || manager.LandCells(tl, br, ExtendedLandingManager.PassengerLocomotors(transports.Select(t => t.Transport))).Count == 0)
			{
				start = null;
				yield break;
			}

			var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
			foreach (var (t, b) in transports)
				yield return ExtendedLanding.MakeOrder(world, t, start.Value, end, queued, b);

			world.CancelInputMode();
		}

		void IOrderGenerator.Tick(World world)
		{
			if (!Available(world))
				world.CancelInputMode();
		}

		void IOrderGenerator.SelectionChanged(World world, IEnumerable<Actor> selected) { }

		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world) { yield break; }

		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world)
		{
			var mouse = world.Map.Clamp(wr.Viewport.ViewToWorld(Viewport.LastMousePos));
			var (tl, br) = manager.Zone(start ?? mouse, mouse);
			var land = manager.LandCells(tl, br, ExtendedLandingManager.PassengerLocomotors(Transports(world).Select(t => t.Transport))).ToHashSet();
			var pal = wr.Palette(TileSet.TerrainPaletteInternalName);
			foreach (var c in ExtendedLandingManager.Cells(tl, br))
			{
				if (world.ShroudObscures(c))
					continue;

				var t = land.Contains(c) ? valid : invalid;
				yield return new SpriteRenderable(t.Sprite, world.Map.CenterOfCell(c), WVec.Zero, -511, pal, 1f, t.Alpha * 0.6f,
					float3.Ones, TintModifiers.IgnoreWorldTint, true);
			}
		}

		IEnumerable<IRenderable> IOrderGenerator.RenderAnnotations(WorldRenderer wr, World world)
		{
			var mouse = world.Map.Clamp(wr.Viewport.ViewToWorld(Viewport.LastMousePos));
			var (tl, br) = manager.Zone(start ?? mouse, mouse);
			var a = world.Map.CenterOfCell(tl) - new WVec(512, 512, 0);
			var b = world.Map.CenterOfCell(br) + new WVec(512, 512, 0);
			var corners = new[] { a, new WPos(b.X, a.Y, 0), b, new WPos(a.X, b.Y, 0) };
			for (var i = 0; i < 4; i++)
				yield return new LineAnnotationRenderable(corners[i], corners[(i + 1) % 4], 2, Color.Orange);
		}

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) && Transports(world).Count > 0 ? Cursor : BlockedCursor;
		}

		bool IOrderGenerator.HandleKeyPress(KeyInput e) { return false; }

		void IOrderGenerator.Deactivate() { }
	}
}
