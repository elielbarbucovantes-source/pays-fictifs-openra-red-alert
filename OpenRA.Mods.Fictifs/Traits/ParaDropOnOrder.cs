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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Transport aircraft that parachutes its passengers where the player orders:",
		"right-click on the ground while loaded, the aircraft flies there, drops everything, then returns to base.",
		"Force-move (Alt) keeps the regular move order.")]
	public class ParaDropOnOrderInfo : TraitInfo, Requires<CargoInfo>, Requires<AircraftInfo>
	{
		[Desc("Passengers are dropped while the aircraft is closer than this to the drop point.")]
		public readonly WDist DropRange = WDist.FromCells(3);

		[Desc("Give up and return to base with the remaining passengers after circling this long",
			"without anyone being able to jump (water, cliffs or crowded ground below).")]
		public readonly int GiveUpDelay = 250;

		[Desc("Ticks between two passengers.")]
		public readonly int DropInterval = 5;

		[Desc("Ticks after a drop before the next drop can be ordered.")]
		public readonly int Cooldown = 1500;

		[Desc("Sound played for each passenger dropped.")]
		public readonly string ChuteSound = null;

		[CursorReference]
		public readonly string Cursor = "ability";

		[CursorReference]
		public readonly string BlockedCursor = "move-blocked";

		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly Color CooldownBarColor = Color.FromArgb(200, 200, 60);

		public override object Create(ActorInitializer init) { return new ParaDropOnOrder(init.Self, this); }
	}

	public class ParaDropOnOrder : IIssueOrder, IResolveOrder, IOrderVoice, ITick, ISync, ISelectionBar
	{
		public readonly ParaDropOnOrderInfo Info;
		readonly Actor self;
		readonly Cargo cargo;

		[Sync]
		Target dropZone = Target.Invalid;

		[Sync]
		int dropDelay;

		[Sync]
		int cooldown;

		int droppedSinceArmed;

		public ParaDropOnOrder(Actor self, ParaDropOnOrderInfo info)
		{
			Info = info;
			this.self = self;
			cargo = self.Trait<Cargo>();
		}

		public bool CanDrop => cooldown <= 0 && !cargo.IsEmpty();
		public bool IsEmpty => cargo.IsEmpty();
		public int DroppedSinceArmed => droppedSinceArmed;

		public bool InDropRange(in Target target)
		{
			return target.IsInRange(self.CenterPosition, Info.DropRange);
		}

		// Called by the activity: passengers leave while the aircraft is over the drop zone.
		public void Arm(in Target target)
		{
			dropZone = target;
			droppedSinceArmed = 0;
		}

		public void Disarm()
		{
			if (dropZone.Type == TargetType.Invalid)
				return;

			dropZone = Target.Invalid;

			// Cancelled before anyone jumped: no reloading time.
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

			if (dropZone.Type == TargetType.Invalid || cargo.IsEmpty() || !InDropRange(dropZone)
				|| !self.World.Map.Contains(self.Location) || self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length == 0)
				return;

			var dropActor = cargo.Peek();
			var positionable = dropActor.Trait<IPositionable>();
			var cell = self.Location;

			// Wait until the aircraft flies over a cell where the passenger can land (no water for tanks, free space).
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
			dropDelay = Info.DropInterval;
			droppedSinceArmed++;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get { yield return new ParaDropOrderTargeter(this); }
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == ParaDropOrderTargeter.Id)
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != ParaDropOrderTargeter.Id || order.Target.Type != TargetType.Terrain || !CanDrop)
				return;

			var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
			if (!self.World.Map.Contains(cell))
				return;

			self.QueueActivity(order.Queued, new ParaDropFlight(self, Target.FromCell(self.World, cell)));
			self.ShowTargetLines();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == ParaDropOrderTargeter.Id ? Info.Voice : null;
		}

		float ISelectionBar.GetValue()
		{
			return cooldown <= 0 ? 0 : (float)cooldown / Info.Cooldown;
		}

		Color ISelectionBar.GetColor() { return Info.CooldownBarColor; }
		bool ISelectionBar.DisplayWhenEmpty => false;

		sealed class ParaDropOrderTargeter : IOrderTargeter
		{
			public const string Id = "ParaDropHere";
			readonly ParaDropOnOrder trait;

			public ParaDropOrderTargeter(ParaDropOnOrder trait) { this.trait = trait; }

			public string OrderID => Id;

			// Above the aircraft's move order (4): a loaded transport drops instead of moving.
			public int OrderPriority => 6;
			public bool IsQueued { get; private set; }

			public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
			{
				if (target.Type != TargetType.Terrain || modifiers.HasModifier(TargetModifiers.ForceMove) || trait.IsEmpty)
					return false;

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
				var cell = self.World.Map.CellContaining(target.CenterPosition);
				cursor = trait.CanDrop && self.World.Map.Contains(cell) ? trait.Info.Cursor : trait.Info.BlockedCursor;
				return true;
			}

			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers)
			{
				return false;
			}
		}
	}
}
