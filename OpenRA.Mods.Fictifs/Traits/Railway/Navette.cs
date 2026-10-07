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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Navette automatique d'une locomotive : charger le stock de la gare de chargement dans ses wagons
	// (départ quand le train est plein, ou après un délai sans nouvelle unité s'il y a quelqu'un à bord),
	// rouler jusqu'à la gare de déchargement, tout y débarquer (les unités vont au point de ralliement
	// de cette gare), puis revenir, en boucle. Tout ordre manuel donné au train arrête la navette ;
	// le programme reste en mémoire et le bouton « Navette » la relance.

	[Desc("Locomotive shuttle program between a loading and an unloading station (" + nameof(GareStock) + ").")]
	public class NavetteInfo : TraitInfo, Requires<RailCarInfo>
	{
		[Desc("Ticks between two units moved between the station and each wagon.")]
		public readonly int TransferInterval = 12;

		[Desc("Leave a partly loaded train after this many ticks without a new unit boarding.")]
		public readonly int MaxWait = 750;

		public readonly Color LoadColor = Color.FromArgb(255, 80, 220, 80);

		public readonly Color UnloadColor = Color.FromArgb(255, 240, 150, 40);

		public readonly Color InactiveColor = Color.FromArgb(200, 150, 150, 150);

		[VoiceReference]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new Navette(init.Self, this); }
	}

	public class Navette : IResolveOrder, IOrderVoice, IRenderAboveShroudWhenSelected
	{
		public const string ProgramOrder = "Navette";
		public const string ResumeOrder = "NavetteReprendre";

		public readonly NavetteInfo Info;
		public readonly RailCar Car;
		readonly Actor self;

		public Navette(Actor self, NavetteInfo info)
		{
			Info = info;
			this.self = self;
			Car = self.Trait<RailCar>();
		}

		public Actor LoadGare { get; private set; }

		public Actor UnloadGare { get; private set; }

		public bool Usable => Car.Info.Locomotive && !Car.IsTraitDisabled;

		public bool HasProgram => ValidGare(LoadGare) && ValidGare(UnloadGare);

		public bool Active => self.CurrentActivity is NavetteActivity a && !a.IsCanceling;

		public bool ValidGare(Actor gare)
		{
			return gare != null && !gare.IsDead && gare.IsInWorld && gare.Owner == self.Owner && gare.Info.HasTraitInfo<GareStockInfo>();
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (!Usable)
				return;

			if (order.OrderString == ProgramOrder)
			{
				if (order.Target.Type != TargetType.Actor)
					return;

				var load = order.Target.Actor;
				var unload = self.World.GetActorById(order.ExtraData);
				if (!ValidGare(load) || !ValidGare(unload) || load == unload)
					return;

				LoadGare = load;
				UnloadGare = unload;
				self.QueueActivity(order.Queued, new NavetteActivity(self));
			}
			else if (order.OrderString == ResumeOrder && HasProgram && !Active)
				self.QueueActivity(order.Queued, new NavetteActivity(self));
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == ProgramOrder || order.OrderString == ResumeOrder ? Info.Voice : null;
		}

		// Train sélectionné : ligne vers la gare de chargement (vert) et vers la gare de déchargement (orange).
		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (!HasProgram || self.World.RenderPlayer != null && self.Owner != self.World.RenderPlayer)
				yield break;

			var active = Active;
			var load = active ? Info.LoadColor : Info.InactiveColor;
			var unload = active ? Info.UnloadColor : Info.InactiveColor;
			yield return new TargetLineRenderable(new[] { self.CenterPosition, LoadGare.CenterPosition }, load, 1, 3);
			yield return new TargetLineRenderable(new[] { LoadGare.CenterPosition, UnloadGare.CenterPosition }, unload, 2, 4);
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable => false;

		// Wagons à voyageurs du train (ceux dont le Cargo prend de l'infanterie ou des véhicules stockables).
		public IEnumerable<(RailCar Car, Cargo Cargo)> Wagons(GareStock stock)
		{
			if (Car.Train == null)
				yield break;

			foreach (var car in Car.Train.Cars)
			{
				if (car.Self.IsDead || car.Self.TraitOrDefault<TrainCargo>() == null)
					continue;

				var cargo = car.Self.TraitOrDefault<Cargo>();
				if (cargo != null && (cargo.Info.Types.Overlaps(stock.Info.InfantryTypes) || cargo.Info.Types.Overlaps(stock.Info.VehicleTypes)))
					yield return (car, cargo);
			}
		}

		public static bool Accepts(Cargo cargo, Actor passenger)
		{
			var p = passenger.Info.TraitInfoOrDefault<PassengerInfo>();
			return p != null && cargo.Info.Types.Contains(p.CargoType) && cargo.HasSpace(p.Weight);
		}

		public static bool Full(Cargo cargo)
		{
			return !cargo.HasSpace(1);
		}
	}
}
