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
	[ScriptPropertyGroup("Transports")]
	public class ExtendedLandingProperties : ScriptActorProperties, Requires<ExtendedLandingInfo>
	{
		public ExtendedLandingProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[Desc("Extended landing over the rectangle start-end (start == end: concentrated landing around the cell).",
			"Transports given the same zone in the same tick share it.")]
		public void LandInZone(CPos start, CPos end)
		{
			Self.World.IssueOrder(ExtendedLanding.MakeOrder(Self.World, Self, start, end, false, null));
		}
	}

	[ScriptPropertyGroup("Transports")]
	public class NavetteAmphibieProperties : ScriptActorProperties, Requires<NavetteAmphibieInfo>
	{
		readonly NavetteAmphibie navette;

		public NavetteAmphibieProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			navette = self.Trait<NavetteAmphibie>();
		}

		[Desc("Amphibious shuttle between two amphibious bases.")]
		public void ShuttleBetweenBases(Actor loadBase, Actor unloadBase)
		{
			Self.World.IssueOrder(NavetteAmphibie.MakeOrder(Self, new Escale(loadBase), new Escale(unloadBase), false));
		}

		[Desc("Amphibious shuttle from an amphibious base to a beach (rectangle start-end).")]
		public void ShuttleToBeach(Actor loadBase, CPos start, CPos end)
		{
			var (tl, br) = Self.World.WorldActor.Trait<ExtendedLandingManager>().Zone(start, end);
			Self.World.IssueOrder(NavetteAmphibie.MakeOrder(Self, new Escale(loadBase), new Escale(tl, br), false));
		}

		[Desc("Amphibious shuttle from a beach (rectangle start-end) to an amphibious base.")]
		public void ShuttleFromBeach(CPos start, CPos end, Actor unloadBase)
		{
			var (tl, br) = Self.World.WorldActor.Trait<ExtendedLandingManager>().Zone(start, end);
			Self.World.IssueOrder(NavetteAmphibie.MakeOrder(Self, new Escale(tl, br), new Escale(unloadBase), false));
		}

		[Desc("Is the amphibious shuttle running?")]
		public bool AmphibiousShuttleActive => navette.Active;
	}

	[ScriptPropertyGroup("Transports")]
	public class AmphibiousBaseProperties : ScriptActorProperties, Requires<AmphibiousBaseInfo>
	{
		readonly AmphibiousBase amphibiousBase;

		public AmphibiousBaseProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			amphibiousBase = self.Trait<AmphibiousBase>();
		}

		[Desc("Send every loaded transport of this base to land over start-end; they come back to the quay afterwards.")]
		public void LaunchOperation(CPos start, CPos end)
		{
			foreach (var t in amphibiousBase.LoadedTransports().ToList())
				Self.World.IssueOrder(ExtendedLanding.MakeOrder(Self.World, t, start, end, false, Self));
		}

		[Desc("Number of units in the base stock.")]
		public int StockedUnits => amphibiousBase.StockCount;

		[Desc("Number of reserved berths at the quay.")]
		public int DockedCount => amphibiousBase.DockedCount;

		[Desc("Transports docked at this base.")]
		public Actor[] DockedTransports => amphibiousBase.Transports().ToArray();
	}
}
