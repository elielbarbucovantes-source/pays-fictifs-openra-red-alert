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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	[Desc("A " + nameof(GrantExternalConditionPower) + " that spends one of the player's " + nameof(PresidentialMandate) + " orders.")]
	public class PriorityOrderPowerInfo : GrantExternalConditionPowerInfo
	{
		[Desc("Text shown after each use. {0} is replaced by the orders left in the current term.")]
		public readonly string OrdersLeftTextNotification = null;

		[Desc("Text shown when no order is left in the current term.")]
		public readonly string NoOrdersLeftTextNotification = null;

		public override object Create(ActorInitializer init) { return new PriorityOrderPower(init.Self, this); }
	}

	public class PriorityOrderPower : GrantExternalConditionPower
	{
		readonly PriorityOrderPowerInfo info;

		public PriorityOrderPower(Actor self, PriorityOrderPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			var mandate = self.Owner.PlayerActor.TraitOrDefault<PresidentialMandate>();
			if (mandate != null && !mandate.TryConsume())
			{
				TextNotificationsManager.AddTransientLine(info.NoOrdersLeftTextNotification, self.Owner);
				return;
			}

			base.Activate(self, order, manager);

			if (mandate != null && !string.IsNullOrEmpty(info.OrdersLeftTextNotification))
				TextNotificationsManager.AddTransientLine(string.Format(info.OrdersLeftTextNotification, mandate.Remaining), self.Owner);
		}
	}

	[Desc("Grants a condition while the owner still has priority orders left in the current term.")]
	public class GrantConditionWhilePriorityOrdersRemainInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionWhilePriorityOrdersRemain(this); }
	}

	public class GrantConditionWhilePriorityOrdersRemain : ITick, INotifyOwnerChanged
	{
		readonly GrantConditionWhilePriorityOrdersRemainInfo info;
		PresidentialMandate mandate;
		int token = Actor.InvalidConditionToken;

		public GrantConditionWhilePriorityOrdersRemain(GrantConditionWhilePriorityOrdersRemainInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			mandate ??= self.Owner.PlayerActor.TraitOrDefault<PresidentialMandate>();
			var available = mandate == null || mandate.Remaining > 0;

			if (available && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);
			else if (!available && token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			mandate = null;
		}
	}
}
