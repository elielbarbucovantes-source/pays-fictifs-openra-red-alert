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

using System.Linq;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Scripting
{
	[ScriptPropertyGroup("Carrier")]
	public class AircraftCarrierProperties : ScriptActorProperties, Requires<AircraftCarrierInfo>
	{
		readonly AircraftCarrier carrier;

		public AircraftCarrierProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			carrier = self.Trait<AircraftCarrier>();
		}

		[Desc("Number of aircraft on board (stowed or being stowed).")]
		public int AircraftOnBoard => carrier.Count;

		[Desc("Number of aircraft on board that are rearmed and ready to launch.")]
		public int ReadyAircraft => carrier.Readiness.Count(r => r);

		[Desc("Launch every ready aircraft.")]
		public void LaunchAircraft()
		{
			carrier.LaunchAll();
		}
	}
}
