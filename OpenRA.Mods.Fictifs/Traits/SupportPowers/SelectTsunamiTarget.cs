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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Ciblage du tsunami : comme l'attaque aérienne directionnelle du moteur (clic sur la
	// côte, glisser pour choisir le sens de la vague), avec en plus l'aperçu de la bande
	// touchée (bleu si c'est bien une côte, rouge sinon). Un clic sans glisser prend la
	// meilleure direction depuis la mer. Une cible qui n'est pas une côte est refusée.
	public class SelectTsunamiTarget : IOrderGenerator
	{
		const int MinDragThreshold = 20;
		const int MaxDragThreshold = 75;

		static readonly string[] ArrowNames = { "arrow-t", "arrow-tl", "arrow-l", "arrow-bl", "arrow-b", "arrow-br", "arrow-r", "arrow-tr" };

		static readonly Color ValidColor = Color.FromArgb(220, 80, 180, 255);
		static readonly Color InvalidColor = Color.FromArgb(220, 230, 40, 40);

		readonly string order;
		readonly SupportPowerManager manager;
		readonly TsunamiPower power;
		readonly (Sprite Sprite, double EndAngle, WAngle Direction)[] arrows;
		readonly MouseAttachmentWidget mouseAttachment;

		CPos targetCell;
		int2 targetLocation;
		float2 dragDirection;
		bool activated;
		bool dragStarted;
		WAngle currentDirection;

		public SelectTsunamiTarget(World world, string order, SupportPowerManager manager, TsunamiPower power)
		{
			this.order = order;
			this.manager = manager;
			this.power = power;

			var partAngle = 360 / ArrowNames.Length;
			arrows = new (Sprite, double, WAngle)[ArrowNames.Length];
			for (var i = 0; i < ArrowNames.Length; i++)
			{
				var sprite = world.Map.Sequences.GetSequence(power.TsunamiInfo.DirectionArrowAnimation, ArrowNames[i]).GetSprite(0);
				arrows[i] = (sprite, i * partAngle + partAngle / 2d, WAngle.FromDegrees(i * partAngle));
			}

			mouseAttachment = Ui.Root.Get<MouseAttachmentWidget>("MOUSE_ATTATCHMENT");
		}

		bool IsOutsideDragZone => dragStarted && dragDirection.Length > MinDragThreshold;

		// Direction retenue pour une case : celle de la flèche si le joueur a glissé, sinon la meilleure.
		WAngle? Facing(World world, CPos cell, bool dragged)
		{
			return dragged ? currentDirection : power.BestFacing(world, cell);
		}

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
				mouseAttachment.SetAttachment(targetLocation, arrow.Sprite, power.TsunamiInfo.DirectionArrowPalette);
				dragStarted = true;
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				var facing = Facing(world, targetCell, IsOutsideDragZone);
				var valid = facing.HasValue && new TsunamiBand(world.Map, targetCell, facing.Value, power.TsunamiInfo).IsValid(out _, out _);
				if (!valid)
				{
					TextNotificationsManager.AddTransientLine(power.TsunamiInfo.NotCoastTextNotification, world.LocalPlayer);
					Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", "AlertBleep", null);
					Reset();
					yield break;
				}

				yield return new Order(order, manager.Self, Target.FromCell(world, targetCell), false)
				{
					SuppressVisualFeedback = true,
					ExtraData = (uint)facing.Value.Facing
				};

				world.CancelInputMode();
			}
		}

		void Reset()
		{
			if (activated)
			{
				mouseAttachment.Reset();
				Game.Cursor.Unlock();
			}

			activated = dragStarted = false;
			dragDirection = float2.Zero;
		}

		void IOrderGenerator.Tick(World world)
		{
			if (!manager.Powers.TryGetValue(order, out var p) || !p.Active || !p.Ready)
				world.CancelInputMode();
		}

		void IOrderGenerator.SelectionChanged(World world, IEnumerable<Actor> selected) { }

		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world) { yield break; }

		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		IEnumerable<IRenderable> IOrderGenerator.RenderAnnotations(WorldRenderer wr, World world)
		{
			var cell = activated ? targetCell : wr.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (!world.Map.Contains(cell))
				yield break;

			var facing = Facing(world, cell, activated && IsOutsideDragZone);
			var band = new TsunamiBand(world.Map, cell, facing ?? WAngle.Zero, power.TsunamiInfo);
			var color = facing.HasValue && band.IsValid(out _, out _) ? ValidColor : InvalidColor;

			var land = band.Outline(-512, band.LandDepth);
			yield return new PolygonAnnotationRenderable(land, band.Origin, 2, color);

			// Flèche du large vers la côte.
			var start = band.At(-band.SeaDepth / 2, 0);
			var tip = band.At(-512, 0);
			yield return new LineAnnotationRenderable(start, tip, 2, color);
			yield return new LineAnnotationRenderable(tip, band.At(-1024, -384), 2, color);
			yield return new LineAnnotationRenderable(tip, band.At(-1024, 384), 2, color);
		}

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) ? power.Info.Cursor : "generic-blocked";
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

		// Même convention que le moteur : 0 = vers le haut, sens antihoraire.
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
