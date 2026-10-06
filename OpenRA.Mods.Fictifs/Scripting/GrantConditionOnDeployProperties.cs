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
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Scripting
{
	[ScriptPropertyGroup("General")]
	public class GrantConditionOnDeployProperties : ScriptActorProperties, Requires<GrantConditionOnDeployInfo>
	{
		readonly GrantConditionOnDeploy deploy;

		public GrantConditionOnDeployProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			deploy = self.Trait<GrantConditionOnDeploy>();
		}

		[Desc("Deploy or undeploy (like the deploy key), e.g. the Schwerer Gustav battery setup.")]
		public void ToggleDeploy()
		{
			((IResolveOrder)deploy).ResolveOrder(Self, new Order("GrantConditionOnDeploy", Self, false));
		}

		[Desc("Is the actor deployed?")]
		public bool IsDeployed => deploy.DeployState == DeployState.Deployed;
	}
}
