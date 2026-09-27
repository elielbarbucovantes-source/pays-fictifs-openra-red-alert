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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("When the listed armaments fire, an aircraft flies over the target (purely visual: the weapon deals the damage).",
		"Used by units that call an airstrike on what they designate.")]
	public class FlybyOnAttackInfo : ConditionalTraitInfo
	{
		[Desc("Armaments that summon the aircraft.")]
		public readonly HashSet<string> Armaments = new() { "primary" };

		[ActorReference(typeof(AircraftInfo))]
		[FieldLoader.Require]
		[Desc("Aircraft actor that flies over the target.")]
		public readonly string UnitType = null;

		[Desc("The aircraft appears this far before the target, on the caller's side, and leaves as far beyond it.")]
		public readonly WDist Distance = WDist.FromCells(10);

		public override object Create(ActorInitializer init) { return new FlybyOnAttack(this); }
	}

	public class FlybyOnAttack : ConditionalTrait<FlybyOnAttackInfo>, INotifyAttack
	{
		public FlybyOnAttack(FlybyOnAttackInfo info)
			: base(info) { }

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || !Info.Armaments.Contains(a.Info.Name))
				return;

			var altitude = self.World.Map.Rules.Actors[Info.UnitType].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
			var targetPos = target.CenterPosition;
			var delta = targetPos - self.CenterPosition;
			var facing = delta.HorizontalLengthSquared != 0 ? delta.Yaw : WAngle.Zero;

			// Unit vector (length 1024) pointing from the caller towards the target.
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(facing));
			var above = new WVec(0, 0, altitude);
			var start = targetPos + above - dir * Info.Distance.Length / 1024;
			var finish = targetPos + above + dir * Info.Distance.Length / 1024;

			self.World.AddFrameEndTask(w =>
			{
				var plane = w.CreateActor(Info.UnitType, new TypeDictionary
				{
					new CenterPositionInit(start),
					new OwnerInit(self.Owner),
					new FacingInit(facing),
				});

				plane.QueueActivity(new Fly(plane, Target.FromPos(finish)));
				plane.QueueActivity(new RemoveSelf());
			});
		}
	}
}
