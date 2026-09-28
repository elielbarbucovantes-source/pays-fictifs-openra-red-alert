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

using OpenRA.Scripting;

namespace OpenRA.Mods.Fictifs.Scripting
{
	[ScriptPropertyGroup("Player")]
	public class ShroudProperties : ScriptPlayerProperties
	{
		public ShroudProperties(ScriptContext context, Player player)
			: base(context, player) { }

		[Desc("Returns true if the player has explored the given cell (it is not black shroud).")]
		public bool IsExplored(CPos cell)
		{
			return Player.Shroud.IsExplored(cell);
		}
	}
}
