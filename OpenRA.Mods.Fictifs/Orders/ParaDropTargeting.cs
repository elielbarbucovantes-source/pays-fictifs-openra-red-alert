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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Orders
{
	// Ciblage du largage, actif tant que Ctrl est enfoncé (voir ParaDropHotkey) : comme les pouvoirs
	// aériens de Red Alert (curseur « ability », flèches bleues), clic gauche sur la zone, glisser pour
	// choisir l'axe, relâcher. Sans glisser, l'axe va des avions vers la zone. Clic droit : annuler.
	public class ParaDropTargeting : IOrderGenerator
	{
		const int MinDragThreshold = 20;
		const int MaxDragThreshold = 75;
		static readonly string[] ArrowNames = { "arrow-t", "arrow-tl", "arrow-l", "arrow-bl", "arrow-b", "arrow-br", "arrow-r", "arrow-tr" };
		static readonly Color Valide = Color.FromArgb(230, 90, 220, 90);
		static readonly Color Bloque = Color.FromArgb(230, 230, 60, 40);

		readonly ParaDropOnOrderInfo info;
		readonly (Sprite Sprite, double EndAngle, WAngle Direction)[] arrows;
		readonly MouseAttachmentWidget mouseAttachment;

		CPos targetCell;
		int2 targetLocation;
		float2 dragDirection;
		bool activated;
		bool dragStarted;
		WAngle currentDirection;

		public ParaDropTargeting(World world, ParaDropOnOrderInfo info)
		{
			this.info = info;
			var part = 360 / ArrowNames.Length;
			arrows = new (Sprite, double, WAngle)[ArrowNames.Length];
			for (var i = 0; i < ArrowNames.Length; i++)
			{
				var sprite = world.Map.Sequences.GetSequence(info.DirectionArrowAnimation, ArrowNames[i]).GetSprite(0);
				arrows[i] = (sprite, i * part + part / 2d, WAngle.FromDegrees(i * part));
			}

			mouseAttachment = Ui.Root.Get<MouseAttachmentWidget>("MOUSE_ATTATCHMENT");
		}

		static IEnumerable<(Actor Actor, ParaDropOnOrder Trait)> Avions(World world)
		{
			return world.Selection.Actors
				.Where(a => a.IsInWorld && !a.IsDead && a.Owner == world.LocalPlayer)
				.Select(a => (a, a.TraitOrDefault<ParaDropOnOrder>()))
				.Where(p => p.Item2 != null && !p.Item2.IsEmpty);
		}

		bool Dragged => activated && dragStarted && dragDirection.Length > MinDragThreshold;

		IEnumerable<Order> IOrderGenerator.Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
			{
				world.CancelInputMode();
				yield break;
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				if (!activated && world.Map.Contains(cell))
				{
					targetCell = cell;
					targetLocation = mi.Location;
					activated = true;
					dragStarted = false;
					dragDirection = float2.Zero;
					Game.Cursor.Lock();
				}

				yield break;
			}

			if (!activated)
				yield break;

			if (mi.Event == MouseInputEvent.Move)
			{
				dragDirection += mi.Delta;
				var angle = AngleOf(dragDirection);
				if (dragDirection.Length > MaxDragThreshold)
					dragDirection = -MaxDragThreshold * float2.FromAngle((float)(angle * (Math.PI / 180)));

				var arrow = arrows.FirstOrDefault(a => a.EndAngle >= angle);
				if (arrow.Sprite == null)
					arrow = arrows[0];

				currentDirection = arrow.Direction;
				mouseAttachment.SetAttachment(targetLocation, arrow.Sprite, info.DirectionArrowPalette);
				dragStarted = true;
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
				var dragged = Dragged;
				var extra = dragged ? (uint)currentDirection.Facing : uint.MaxValue;
				foreach (var (a, t) in Avions(world))
				{
					if (!t.CanDrop)
						continue;

					yield return new Order(ParaDropOnOrder.OrderID, a, Target.FromCell(world, targetCell), queued) { ExtraData = extra };
				}

				world.CancelInputMode();
			}
		}

		void IOrderGenerator.Tick(World world)
		{
			if (!Avions(world).Any() || (!activated && !Game.GetModifierKeys().HasModifier(Modifiers.Ctrl)))
				world.CancelInputMode();
		}

		void IOrderGenerator.SelectionChanged(World world, IEnumerable<Actor> selected) { }

		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world) { yield break; }

		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		IEnumerable<IRenderable> IOrderGenerator.RenderAnnotations(WorldRenderer wr, World world) { yield break; }

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var ok = world.Map.Contains(cell) && Avions(world).Any(p => p.Trait.CanDrop);
			return ok ? info.Cursor : info.BlockedCursor;
		}

		bool IOrderGenerator.HandleKeyPress(KeyInput e) { return false; }

		void IOrderGenerator.Deactivate()
		{
			if (activated)
			{
				mouseAttachment.Reset();
				Game.Cursor.Unlock();
			}
		}

		static double AngleOf(float2 delta)
		{
			var d = Math.Atan2(delta.Y, delta.X) * (180 / Math.PI);
			if (d < 0.0)
				d += 360.0;

			var angle = 270.0 - d;
			if (angle < 0)
				angle += 360.0;

			return angle;
		}
	}
}
