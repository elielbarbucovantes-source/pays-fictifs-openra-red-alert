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
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("Deploy ability with a long charge: every actor of the given relationships around the deployer",
		"(units and buildings) switches to the deployer's owner. Used by the Australouis Révolutionnaire.")]
	public class RevolutionOnDeployInfo : ConditionalTraitInfo
	{
		[Desc("Radius of the revolution.")]
		public readonly WDist Range = WDist.FromCells(5);

		[Desc("Ticks needed to charge the ability.")]
		public readonly int ChargeDelay = 7500;

		[Desc("Start with a full charge.")]
		public readonly bool StartCharged = false;

		[Desc("Relationships of the actors that change sides.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Enemy;

		[Desc("Actors with one of these target types are not converted.")]
		public readonly BitSet<TargetableType> InvalidTargets = default;

		[Desc("Image of the effect played on the deployer.")]
		public readonly string EffectImage = null;

		[SequenceReference(nameof(EffectImage), allowNullImage: true)]
		public readonly string EffectSequence = "idle";

		[PaletteReference]
		public readonly string EffectPalette = "effect";

		[Desc("Effect played on each converted actor.")]
		public readonly string ConvertedImage = null;

		[SequenceReference(nameof(ConvertedImage), allowNullImage: true)]
		public readonly string ConvertedSequence = "idle";

		public readonly string Sound = null;

		[NotificationReference("Speech")]
		public readonly string ReadyNotification = null;

		[Desc("Text shown to the owner when the ability is ready.")]
		public readonly string ReadyTextNotification = null;

		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly string DeployCursor = "deploy";
		public readonly string DeployBlockedCursor = "deploy-blocked";

		public readonly Color ChargeColor = Color.Magenta;

		public override object Create(ActorInitializer init) { return new RevolutionOnDeploy(this); }
	}

	public class RevolutionOnDeploy : ConditionalTrait<RevolutionOnDeployInfo>, ITick, IIssueOrder, IResolveOrder,
		IOrderVoice, IIssueDeployOrder, ISelectionBar
	{
		const string OrderID = "Revolution";
		int charge;

		public RevolutionOnDeploy(RevolutionOnDeployInfo info)
			: base(info)
		{
			charge = info.StartCharged ? info.ChargeDelay : 0;
		}

		bool Ready => !IsTraitDisabled && charge >= Info.ChargeDelay;

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || charge >= Info.ChargeDelay)
				return;

			if (++charge == Info.ChargeDelay)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.ReadyNotification, self.Owner.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(Info.ReadyTextNotification, self.Owner);
			}
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new DeployOrderTargeter(OrderID, 5, () => Ready ? Info.DeployCursor : Info.DeployBlockedCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == OrderID ? new Order(OrderID, self, queued) : null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued) { return new Order(OrderID, self, queued); }

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return Ready; }

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == OrderID && Ready ? Info.Voice : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderID || !Ready)
				return;

			charge = 0;
			var owner = self.Owner;
			var world = self.World;

			if (Info.EffectImage != null)
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(self.CenterPosition, w, Info.EffectImage, Info.EffectSequence, Info.EffectPalette)));

			if (Info.Sound != null)
				Game.Sound.Play(SoundType.World, Info.Sound, self.CenterPosition);

			var converted = world.FindActorsInCircle(self.CenterPosition, Info.Range)
				.Where(a => a != self && !a.IsDead && a.IsInWorld && a.Owner != owner
					&& !a.Owner.NonCombatant && Info.ValidRelationships.HasRelationship(owner.RelationshipWith(a.Owner))
					&& a.Info.HasTraitInfo<IHealthInfo>() && !a.Info.HasTraitInfo<HuskInfo>()
					&& !a.GetEnabledTargetTypes().Overlaps(Info.InvalidTargets))
				.ToList();

			world.AddFrameEndTask(w =>
			{
				foreach (var a in converted)
				{
					if (a.IsDead || !a.IsInWorld)
						continue;

					a.ChangeOwner(owner);
					if (Info.ConvertedImage != null)
						w.Add(new SpriteEffect(a.CenterPosition, w, Info.ConvertedImage, Info.ConvertedSequence, Info.EffectPalette));
				}
			});
		}

		float ISelectionBar.GetValue()
		{
			return IsTraitDisabled ? 0 : (float)charge / Info.ChargeDelay;
		}

		Color ISelectionBar.GetColor() { return Info.ChargeColor; }

		bool ISelectionBar.DisplayWhenEmpty => !IsTraitDisabled;
	}
}
