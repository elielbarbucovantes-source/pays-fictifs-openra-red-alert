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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Scripting
{
	[ScriptPropertyGroup("Player")]
	public class BlitzGaugeProperties : ScriptPlayerProperties
	{
		public BlitzGaugeProperties(ScriptContext context, Player player)
			: base(context, player) { }

		[Desc("State of the Blitz 7 offensive gauge: \"remaining ticks / charge speed % / reserve / in combat\", or an empty string.")]
		public string BlitzGauge()
		{
			var manager = Player.PlayerActor.TraitOrDefault<SupportPowerManager>();
			var gauge = manager?.Powers.Values.OfType<BlitzGaugeInstance>().FirstOrDefault();
			if (gauge == null)
				return "";

			return $"{gauge.RemainingTicks}/{gauge.Rate}%/{gauge.Reserve}/{gauge.InCombat}";
		}
	}

	[ScriptPropertyGroup("Ability")]
	public class InterceptsMissilesProperties : ScriptActorProperties, Requires<InterceptsMissilesInfo>
	{
		public InterceptsMissilesProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[Desc("Number of enemy missiles destroyed in flight by this actor.")]
		public int MissilesIntercepted => Self.Trait<InterceptsMissiles>().Intercepted;
	}
}
