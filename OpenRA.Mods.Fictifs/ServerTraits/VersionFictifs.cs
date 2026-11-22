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

using OpenRA.Mods.Fictifs.Widgets.Logic;
using OpenRA.Server;

namespace OpenRA.Mods.Fictifs.ServerTraits
{
	/// <summary>
	/// Fixe la version du mod (ModVersion) à la création du serveur, avant la première
	/// connexion. Indispensable pour le serveur dédié (launch-dedicated), qui n'affiche
	/// ni écran de chargement ni menu : sans cela il annoncerait la version brute
	/// (release-20231010) et refuserait tous les joueurs.
	/// </summary>
	public class VersionFictifs : ServerTrait
	{
		public VersionFictifs()
		{
			if (Game.ModData != null)
				ModVersion.Apply(Game.ModData);
		}
	}
}
