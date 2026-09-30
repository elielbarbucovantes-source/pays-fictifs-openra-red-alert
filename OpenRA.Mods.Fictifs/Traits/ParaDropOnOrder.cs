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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Transport aircraft that parachutes its passengers along a line chosen by the player.",
		"Right-click moves as usual. Holding Ctrl opens the drop targeting (see ParaDropHotkey): left-click the drop",
		"zone, drag to give the direction; the aircraft lines up, flies along the line and drops everyone on the way.")]
	public class ParaDropOnOrderInfo : TraitInfo, Requires<CargoInfo>, Requires<AircraftInfo>
	{
		[Desc("Length of the drop line.")]
		public readonly WDist DropLength = WDist.FromCells(6);

		[Desc("Passengers jump while the aircraft is less than this far from the drop line.")]
		public readonly WDist CorridorWidth = WDist.FromCells(2);

		[Desc("The aircraft lines up this far before the start of the drop line.")]
		public readonly WDist ApproachDistance = WDist.FromCells(5);

		[Desc("Passes over the line before bringing the remaining passengers home",
			"(water, cliffs or crowded ground below).")]
		public readonly int MaxPasses = 3;

		[Desc("Minimum ticks between two passengers (they are otherwise spread along the whole line).")]
		public readonly int MinDropInterval = 2;

		[Desc("Ticks after a drop before the next drop can be ordered.")]
		public readonly int Cooldown = 1500;

		[Desc("Sound played for each passenger dropped.")]
		public readonly string ChuteSound = null;

		[CursorReference]
		public readonly string Cursor = "ability";

		[CursorReference]
		public readonly string BlockedCursor = "move-blocked";

		[Desc("Sequences of the direction arrows while targeting.")]
		public readonly string DirectionArrowAnimation = "paradirection";

		[PaletteReference]
		public readonly string DirectionArrowPalette = "chrome";

		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly Color CooldownBarColor = Color.FromArgb(200, 200, 60);

		public override object Create(ActorInitializer init) { return new ParaDropOnOrder(init.Self, this); }
	}

	public class ParaDropOnOrder : IResolveOrder, IOrderVoice, ITick, ISync, ISelectionBar
	{
		public const string OrderID = "ParaDropHere";

		public readonly ParaDropOnOrderInfo Info;
		readonly Actor self;
		readonly Cargo cargo;
		readonly Aircraft aircraft;

		[Sync]
		bool armed;

		[Sync]
		WPos lineCenter;

		WVec lineDir;

		[Sync]
		int dropDelay;

		[Sync]
		int cooldown;

		int dropInterval;
		int droppedSinceArmed;

		public ParaDropOnOrder(Actor self, ParaDropOnOrderInfo info)
		{
			Info = info;
			this.self = self;
			cargo = self.Trait<Cargo>();
			aircraft = self.Trait<Aircraft>();
		}

		public bool CanDrop => cooldown <= 0 && !cargo.IsEmpty();
		public bool IsEmpty => cargo.IsEmpty();
		public int DroppedSinceArmed => droppedSinceArmed;

		// Direction du largage : celle du glissé, sinon de l'avion vers la zone.
		public static WVec Direction(WAngle? facing, WPos from, WPos to)
		{
			if (facing.HasValue)
				return new WVec(0, -1024, 0).Rotate(WRot.FromYaw(facing.Value));

			var d = to - from;
			d = new WVec(d.X, d.Y, 0);
			var len = d.HorizontalLength;
			return len == 0 ? new WVec(0, -1024, 0) : d * 1024 / len;
		}

		public WPos LineStart(WPos center, WVec dir) { return center - dir * Info.DropLength.Length / 2048; }
		public WPos LineEnd(WPos center, WVec dir) { return center + dir * Info.DropLength.Length / 2048; }
		public WPos Approach(WPos center, WVec dir) { return LineStart(center, dir) - dir * Info.ApproachDistance.Length / 1024; }

		bool InCorridor()
		{
			var v = self.CenterPosition - lineCenter;
			var along = ((long)v.X * lineDir.X + (long)v.Y * lineDir.Y) / 1024;
			var across = ((long)v.X * lineDir.Y - (long)v.Y * lineDir.X) / 1024;
			return Math.Abs(along) <= Info.DropLength.Length / 2 && Math.Abs(across) <= Info.CorridorWidth.Length;
		}

		// Appelé par l'activité : les passagers sautent pendant le survol de la ligne,
		// répartis sur toute sa longueur.
		public void Arm(WPos center, WVec dir)
		{
			if (!armed)
				droppedSinceArmed = 0;

			armed = true;
			lineCenter = center;
			lineDir = dir;
			var speed = Math.Max(1, aircraft.MovementSpeed);
			var traverse = Info.DropLength.Length / speed;
			dropInterval = Math.Max(Info.MinDropInterval, traverse / Math.Max(1, cargo.Passengers.Count()));
		}

		public void Disarm()
		{
			if (!armed)
				return;

			armed = false;

			// Annulé avant le premier saut : pas de rechargement.
			if (droppedSinceArmed > 0)
				cooldown = Info.Cooldown;
		}

		void ITick.Tick(Actor self)
		{
			if (cooldown > 0)
				cooldown--;

			if (dropDelay > 0)
			{
				dropDelay--;
				return;
			}

			if (!armed || cargo.IsEmpty() || !InCorridor()
				|| !self.World.Map.Contains(self.Location) || self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length == 0)
				return;

			var dropActor = cargo.Peek();
			var positionable = dropActor.Trait<IPositionable>();
			var cell = self.Location;

			// Le passager ne saute qu'au-dessus d'une case où il peut atterrir (pas d'eau pour les chars, place libre).
			if (!positionable.CanExistInCell(cell))
				return;

			var subCell = positionable.GetAvailableSubCell(cell);
			if (subCell == SubCell.Invalid)
				return;

			if (cargo.Unload(self) != dropActor)
				throw new InvalidOperationException("Peeked cargo was not unloaded!");

			self.World.AddFrameEndTask(w =>
			{
				positionable.SetPosition(dropActor, cell, subCell);
				var position = dropActor.CenterPosition + new WVec(0, 0, self.CenterPosition.Z - dropActor.CenterPosition.Z);
				positionable.SetCenterPosition(dropActor, position);
				w.Add(dropActor);
			});

			Game.Sound.Play(SoundType.World, Info.ChuteSound, self.CenterPosition);
			dropDelay = dropInterval;
			droppedSinceArmed++;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderID || order.Target.Type != TargetType.Terrain || !CanDrop)
				return;

			var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
			if (!self.World.Map.Contains(cell))
				return;

			var center = self.World.Map.CenterOfCell(cell);
			var facing = order.ExtraData <= 255 ? WAngle.FromFacing((int)order.ExtraData) : (WAngle?)null;
			var dir = Direction(facing, self.CenterPosition, center);
			self.QueueActivity(order.Queued, new ParaDropFlight(self, center, dir));
			self.ShowTargetLines();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == OrderID ? Info.Voice : null;
		}

		float ISelectionBar.GetValue()
		{
			return cooldown <= 0 ? 0 : (float)cooldown / Info.Cooldown;
		}

		Color ISelectionBar.GetColor() { return Info.CooldownBarColor; }
		bool ISelectionBar.DisplayWhenEmpty => false;
	}
}
