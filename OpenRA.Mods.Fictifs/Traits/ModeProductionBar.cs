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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Barre bleue de production sur l'usine. Celle de RA (ProductionBar) se lie à la
	// première file du bon type, même désactivée : avec les files « Normale » et
	// « Multi-file » (MultiQueue.cs), chaque bâtiment porte une file d'usine inerte en
	// mode Normal et le joueur garde les files RA inertes ; la barre restait vide.
	// Ici, on prend d'abord une file active (usine, puis joueur).

	[Desc("Visualizes the remaining build time of actor produced here.",
		"Binds to an enabled queue first (production mode option aware).")]
	public class ModeProductionBarInfo : ConditionalTraitInfo, Requires<ProductionInfo>
	{
		[FieldLoader.Require]
		[Desc("Production queue type, for actors with multiple queues.")]
		public readonly string ProductionType = null;

		public readonly Color Color = Color.SkyBlue;

		public override object Create(ActorInitializer init) { return new ModeProductionBar(init.Self, this); }
	}

	public class ModeProductionBar : ConditionalTrait<ModeProductionBarInfo>, ISelectionBar, ITick, INotifyOwnerChanged
	{
		readonly Actor self;
		ProductionQueue queue;
		float value;

		public ModeProductionBar(Actor self, ModeProductionBarInfo info)
			: base(info)
		{
			this.self = self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			FindQueue();
		}

		void FindQueue()
		{
			var own = self.TraitsImplementing<ProductionQueue>().Where(q => q.Info.Type == Info.ProductionType).ToList();
			var player = self.Owner.PlayerActor.TraitsImplementing<ProductionQueue>().Where(q => q.Info.Type == Info.ProductionType).ToList();

			queue = FirstEnabled(own) ?? FirstEnabled(player) ?? own.FirstOrDefault() ?? player.FirstOrDefault();
		}

		static ProductionQueue FirstEnabled(List<ProductionQueue> queues)
		{
			return queues.FirstOrDefault(q => q.Enabled);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || queue == null)
				return;

			var current = queue.AllQueued().Where(i => i.Started).MinByOrDefault(i => i.RemainingTime);
			value = current != null ? 1 - (float)current.RemainingCost / current.TotalCost : 0;
		}

		float ISelectionBar.GetValue()
		{
			// Seuls les alliés voient la production.
			if (IsTraitDisabled || !self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0;

			return value;
		}

		Color ISelectionBar.GetColor() { return Info.Color; }
		bool ISelectionBar.DisplayWhenEmpty => false;

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			FindQueue();
		}
	}
}
