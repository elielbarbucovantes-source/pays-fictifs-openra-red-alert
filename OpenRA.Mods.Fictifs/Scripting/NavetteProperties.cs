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

using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Scripting
{
	[ScriptPropertyGroup("Movement")]
	public class NavetteProperties : ScriptActorProperties, Requires<NavetteInfo>
	{
		readonly Navette navette;

		public NavetteProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			navette = self.Trait<Navette>();
		}

		[Desc("Locomotive: start a shuttle between a loading and an unloading station (same order as the « Navette » button).")]
		public void StartShuttle(Actor loadStation, Actor unloadStation)
		{
			Self.World.IssueOrder(new Order(Navette.ProgramOrder, Self, Target.FromActor(loadStation), false) { ExtraData = unloadStation.ActorID });
		}

		[Desc("Locomotive: resume the stored shuttle program.")]
		public void ResumeShuttle()
		{
			Self.World.IssueOrder(new Order(Navette.ResumeOrder, Self, false));
		}

		[Desc("Is the shuttle running?")]
		public bool ShuttleActive => navette.Active;
	}

	[ScriptPropertyGroup("Transports")]
	public class GareStockProperties : ScriptActorProperties, Requires<GareStockInfo>
	{
		readonly GareStock stock;

		public GareStockProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			stock = self.Trait<GareStock>();
		}

		[Desc("Number of units stored in the station.")]
		public int StoredUnits => stock.Units.Count;

		[Desc("Empty the station (same as the « Vider la gare » button).")]
		public void EmptyStation()
		{
			Self.World.IssueOrder(new Order(GareStock.EmptyOrder, Self, false));
		}
	}

	[ScriptPropertyGroup("Movement")]
	public class EntreEnGareProperties : ScriptActorProperties, Requires<EntreEnGareInfo>
	{
		public EntreEnGareProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[Desc("Go and garrison in the station (same order as a right click on it).")]
		public void EnterStation(Actor station)
		{
			Self.World.IssueOrder(new Order(EntreEnGare.OrderID, Self, Target.FromActor(station), false));
		}
	}
}
