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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Deploy key: draw a line (press, drag, release; or two clicks) and the unit digs every cell of it.",
		"Dug cells are dry trenches until the canal reaches the sea (see CanalLayer on the World).",
		"Used by the Advanced Tunneller.")]
	public class DigsCanalsInfo : ConditionalTraitInfo, Requires<MobileInfo>
	{
		[Desc("Ticks needed to dig one cell.")]
		public readonly int DigDelay = 200;

		[Desc("Longest line that can be ordered, in cells.")]
		public readonly int MaxLength = 24;

		[GrantedConditionReference]
		[Desc("Condition granted while digging.")]
		public readonly string DiggingCondition = null;

		[Desc("Text shown to the owner when the sea floods a canal.")]
		public readonly string FloodedTextNotification = null;

		[VoiceReference]
		public readonly string Voice = "Action";

		[CursorReference]
		public readonly string Cursor = "ability";

		[CursorReference]
		public readonly string BlockedCursor = "generic-blocked";

		public readonly Color TargetLineColor = Color.SandyBrown;

		public readonly Color ProgressColor = Color.SandyBrown;

		[Desc("Overlay sprites (image \"overlay\") used to preview the line.")]
		public readonly string TileValidName = "build-valid";

		public readonly string TileInvalidName = "build-invalid";

		public readonly string TileUnknownName = "build-valid";

		public override object Create(ActorInitializer init) { return new DigsCanals(init.Self, this); }
	}

	public class DigsCanals : ConditionalTrait<DigsCanalsInfo>, IIssueOrder, IResolveOrder, IIssueDeployOrder, IOrderVoice, ISelectionBar
	{
		public const string OrderID = "PlaceCanal";

		public readonly Sprite Tile;

		// Avancement du creusage de la case en cours (barre de sélection), -1 hors creusage.
		public int Progress = -1;

		public DigsCanals(Actor self, DigsCanalsInfo info)
			: base(info)
		{
			Tile = OverlaySprite(self.World, info.TileValidName).Sprite;
		}

		internal static (Sprite Sprite, float Alpha) OverlaySprite(World world, string name)
		{
			var sequences = world.Map.Sequences;
			var tileset = world.Map.Tileset.ToLowerInvariant();
			// Repli sur build-valid si l'image n'existe pas (Red Alert n'a pas de build-unknown).
			if (sequences.HasSequence("overlay", $"{name}-{tileset}"))
				name = $"{name}-{tileset}";
			else if (!sequences.HasSequence("overlay", name))
				name = "build-valid";

			var seq = sequences.GetSequence("overlay", name);

			return (seq.GetSprite(0), seq.GetAlpha(0));
		}

		// Ligne de cases reliées par les côtés (pas en diagonale), pour que l'eau puisse passer.
		public static List<CPos> Line(CPos a, CPos b, int max)
		{
			var cells = new List<CPos> { a };
			var dx = Math.Abs(b.X - a.X);
			var dy = Math.Abs(b.Y - a.Y);
			var sx = Math.Sign(b.X - a.X);
			var sy = Math.Sign(b.Y - a.Y);
			int x = a.X, y = a.Y, ix = 0, iy = 0;
			while ((ix < dx || iy < dy) && cells.Count < max)
			{
				if (iy >= dy || (ix < dx && (1 + 2 * ix) * dy < (1 + 2 * iy) * dx))
				{
					ix++;
					x += sx;
				}
				else
				{
					iy++;
					y += sy;
				}

				cells.Add(new CPos(x, y));
			}

			return cells;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders { get { yield break; } }

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued) { return null; }

		// La touche de déploiement ouvre le tracé ; l'ordre part au relâchement du clic.
		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			var world = self.World;
			if (self.Owner == world.LocalPlayer && world.OrderGenerator is not CanalOrderGenerator)
				world.OrderGenerator = new CanalOrderGenerator(world, Info);

			return null;
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !IsTraitDisabled; }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderID || IsTraitDisabled)
				return;

			var end = self.World.Map.CellContaining(order.Target.CenterPosition);
			var cells = Line(order.ExtraLocation, end, Info.MaxLength);
			self.QueueActivity(order.Queued, new DigCanal(self, cells));
			self.ShowTargetLines();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == OrderID ? Info.Voice : null;
		}

		float ISelectionBar.GetValue()
		{
			return Progress < 0 ? 0 : (float)Progress / Info.DigDelay;
		}

		Color ISelectionBar.GetColor() { return Info.ProgressColor; }

		bool ISelectionBar.DisplayWhenEmpty => false;
	}

	// Tracé du canal : appuyer, glisser, relâcher (ou deux clics). Clic droit : annuler.
	public class CanalOrderGenerator : IOrderGenerator
	{
		readonly DigsCanalsInfo info;
		readonly CanalLayer layer;
		readonly (Sprite Sprite, float Alpha) valid, invalid, unknown;

		CPos? start;
		bool pressed;

		public CanalOrderGenerator(World world, DigsCanalsInfo info)
		{
			this.info = info;
			layer = world.WorldActor.TraitOrDefault<CanalLayer>();
			valid = DigsCanals.OverlaySprite(world, info.TileValidName);
			invalid = DigsCanals.OverlaySprite(world, info.TileInvalidName);
			unknown = DigsCanals.OverlaySprite(world, info.TileUnknownName);
		}

		static IEnumerable<Actor> Diggers(World world)
		{
			return world.Selection.Actors.Where(a => a.IsInWorld && !a.IsDead && a.Owner == world.LocalPlayer
				&& a.TraitsImplementing<DigsCanals>().Any(t => !t.IsTraitDisabled));
		}

		IEnumerable<Order> IOrderGenerator.Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
			{
				world.CancelInputMode();
				yield break;
			}

			if (mi.Button != MouseButton.Left || !world.Map.Contains(cell))
				yield break;

			if (mi.Event == MouseInputEvent.Down)
			{
				if (start == null)
				{
					start = cell;
					pressed = true;
					yield break;
				}

				// Deuxième clic.
				foreach (var o in Finish(world, cell, mi))
					yield return o;

				yield break;
			}

			if (mi.Event == MouseInputEvent.Up && pressed)
			{
				pressed = false;

				// Relâché sur la case de départ : on attend un second clic.
				if (cell == start)
					yield break;

				foreach (var o in Finish(world, cell, mi))
					yield return o;
			}
		}

		IEnumerable<Order> Finish(World world, CPos end, MouseInput mi)
		{
			var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
			var orders = Diggers(world)
				.Select(a => new Order(DigsCanals.OrderID, a, Target.FromCell(world, end), queued) { ExtraLocation = start.Value })
				.ToList();

			world.CancelInputMode();
			return orders;
		}

		void IOrderGenerator.Tick(World world)
		{
			if (!Diggers(world).Any())
				world.CancelInputMode();
		}

		void IOrderGenerator.SelectionChanged(World world, IEnumerable<Actor> selected) { }

		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world) { yield break; }

		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world)
		{
			var mouse = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
			var cells = DigsCanals.Line(start ?? mouse, mouse, info.MaxLength);
			var pal = wr.Palette(TileSet.TerrainPaletteInternalName);
			foreach (var c in cells)
			{
				var t = valid;
				if (!world.Map.Contains(c) || world.ShroudObscures(c))
					t = invalid;
				else if (world.FogObscures(c))
					t = unknown;
				else if (layer == null || !layer.CanDig(c))
					t = invalid;

				yield return new SpriteRenderable(t.Sprite, world.Map.CenterOfCell(c), WVec.Zero, -511, pal, 1f, t.Alpha,
					float3.Ones, TintModifiers.IgnoreWorldTint, true);
			}
		}

		IEnumerable<IRenderable> IOrderGenerator.RenderAnnotations(WorldRenderer wr, World world) { yield break; }

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) ? info.Cursor : info.BlockedCursor;
		}

		bool IOrderGenerator.HandleKeyPress(KeyInput e) { return false; }

		void IOrderGenerator.Deactivate() { }
	}
}
