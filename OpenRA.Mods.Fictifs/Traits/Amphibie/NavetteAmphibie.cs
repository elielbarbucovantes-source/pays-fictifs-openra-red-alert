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
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Escale d'une navette amphibie : une Base logistique amphibie du joueur, ou une plage (rectangle).
	public class Escale
	{
		public readonly Actor Base;
		public readonly CPos TopLeft, BottomRight;

		public Escale(Actor amphibiousBase)
		{
			Base = amphibiousBase;
		}

		public Escale(CPos topLeft, CPos bottomRight)
		{
			TopLeft = topLeft;
			BottomRight = bottomRight;
		}

		public bool IsBase => Base != null;

		public bool Valid(Player owner)
		{
			return !IsBase || (!Base.IsDead && Base.IsInWorld && Base.Owner == owner && Base.Info.HasTraitInfo<AmphibiousBaseInfo>());
		}

		public bool Contains(CPos c)
		{
			return c.X >= TopLeft.X && c.X <= BottomRight.X && c.Y >= TopLeft.Y && c.Y <= BottomRight.Y;
		}

		public WPos Center(World world)
		{
			if (IsBase)
				return Base.CenterPosition;

			var a = world.Map.CenterOfCell(TopLeft);
			var b = world.Map.CenterOfCell(BottomRight);
			return new WPos((a.X + b.X) / 2, (a.Y + b.Y) / 2, 0);
		}

		public string Serialize()
		{
			return IsBase ? "b" + Base.ActorID.ToString(CultureInfo.InvariantCulture)
				: FormattableString.Invariant($"z{TopLeft.X},{TopLeft.Y},{BottomRight.X},{BottomRight.Y}");
		}

		public static Escale Parse(World world, string s)
		{
			if (string.IsNullOrEmpty(s))
				return null;

			if (s[0] == 'b' && uint.TryParse(s[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
			{
				var a = world.GetActorById(id);
				return a != null ? new Escale(a) : null;
			}

			if (s[0] != 'z')
				return null;

			var p = s[1..].Split(',');
			if (p.Length != 4 || !p.All(v => int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
				return null;

			var v = p.Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();
			return new Escale(new CPos(v[0], v[1]), new CPos(v[2], v[3]));
		}
	}

	// Navette automatique d'un transport amphibie (bouton « Navette amphibie ») : se remplir à l'escale de
	// chargement (le stock d'une base, ou les unités du joueur présentes sur la plage), partir quand il est
	// plein ou après MaxWait sans nouvelle unité s'il y a quelqu'un à bord, décharger à l'escale de
	// déchargement (dans le stock de la base, ou débarquement sur la plage), puis revenir, en boucle.
	// Tout ordre manuel arrête la navette ; le programme reste en mémoire et le bouton la relance.
	[Desc("Amphibious transport shuttle between two stops: amphibious bases (stock) or beaches (zones).")]
	public class NavetteAmphibieInfo : TraitInfo, Requires<ExtendedLandingInfo>
	{
		[Desc("Ticks between two units moved between a base stock and the transport.")]
		public readonly int TransferInterval = 8;

		[Desc("Leave a partly loaded transport after this many ticks without a new unit boarding.")]
		public readonly int MaxWait = 750;

		[Desc("Locomotor whose cells define the land of a beach used as loading stop.")]
		public readonly string BeachLocomotor = "foot";

		public readonly Color LoadColor = Color.FromArgb(255, 80, 220, 80);

		public readonly Color UnloadColor = Color.FromArgb(255, 240, 150, 40);

		public readonly Color InactiveColor = Color.FromArgb(200, 150, 150, 150);

		[VoiceReference]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new NavetteAmphibie(init.Self, this); }
	}

	public class NavetteAmphibie : IResolveOrder, IOrderVoice, IRenderAboveShroudWhenSelected, IRenderAnnotationsWhenSelected
	{
		public const string ProgramOrder = "NavetteAmphibie";
		public const string ResumeOrder = "NavetteAmphibieReprendre";

		public readonly NavetteAmphibieInfo Info;
		readonly Actor self;
		readonly ExtendedLanding landing;

		public NavetteAmphibie(Actor self, NavetteAmphibieInfo info)
		{
			Info = info;
			this.self = self;
			landing = self.Trait<ExtendedLanding>();
		}

		public Escale Load { get; private set; }

		public Escale Unload { get; private set; }

		public bool Usable => !landing.IsTraitDisabled;

		public bool HasProgram => Load != null && Unload != null && Load.Valid(self.Owner) && Unload.Valid(self.Owner);

		public bool Active => self.CurrentActivity is NavetteAmphibieActivity a && !a.IsCanceling;

		public static Order MakeOrder(Actor transport, Escale load, Escale unload, bool queued)
		{
			return new Order(ProgramOrder, transport, queued) { TargetString = load.Serialize() + "|" + unload.Serialize() };
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (!Usable)
				return;

			if (order.OrderString == ProgramOrder)
			{
				var parts = order.TargetString?.Split('|');
				if (parts == null || parts.Length != 2)
					return;

				var load = Escale.Parse(self.World, parts[0]);
				var unload = Escale.Parse(self.World, parts[1]);
				if (load == null || unload == null || !load.Valid(self.Owner) || !unload.Valid(self.Owner)
					|| (load.IsBase && load.Base == unload.Base))
					return;

				Load = load;
				Unload = unload;
				self.QueueActivity(order.Queued, new NavetteAmphibieActivity(self));
				self.ShowTargetLines();
			}
			else if (order.OrderString == ResumeOrder && HasProgram && !Active)
				self.QueueActivity(order.Queued, new NavetteAmphibieActivity(self));
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == ProgramOrder || order.OrderString == ResumeOrder ? landing.Info.Voice : null;
		}

		// Transport sélectionné : ligne vers l'escale de chargement (vert), puis vers celle de déchargement (orange).
		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (!HasProgram || (self.World.RenderPlayer != null && self.Owner != self.World.RenderPlayer))
				yield break;

			var active = Active;
			var load = Load.Center(self.World);
			var unload = Unload.Center(self.World);
			yield return new TargetLineRenderable(new[] { self.CenterPosition, load }, active ? Info.LoadColor : Info.InactiveColor, 1, 3);
			yield return new TargetLineRenderable(new[] { load, unload }, active ? Info.UnloadColor : Info.InactiveColor, 2, 4);
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable => false;

		// Plages de la navette : rectangle autour de la zone.
		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!HasProgram || (self.World.RenderPlayer != null && self.Owner != self.World.RenderPlayer))
				yield break;

			var active = Active;
			foreach (var (e, color) in new[] { (Load, Info.LoadColor), (Unload, Info.UnloadColor) })
			{
				if (e.IsBase)
					continue;

				foreach (var r in ZoneOutline(self.World, e.TopLeft, e.BottomRight, active ? color : Info.InactiveColor))
					yield return r;
			}
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;

		public static IEnumerable<IRenderable> ZoneOutline(World world, CPos tl, CPos br, Color color)
		{
			var a = world.Map.CenterOfCell(tl) - new WVec(512, 512, 0);
			var b = world.Map.CenterOfCell(br) + new WVec(512, 512, 0);
			var corners = new[] { a, new WPos(b.X, a.Y, 0), b, new WPos(a.X, b.Y, 0) };
			for (var i = 0; i < 4; i++)
				yield return new LineAnnotationRenderable(corners[i], corners[(i + 1) % 4], 2, color);
		}
	}
}
