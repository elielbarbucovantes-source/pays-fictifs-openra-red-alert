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
using OpenRA.Mods.Common.LoadScreens;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	/// <summary>
	/// Écran de chargement du mod (celui de Red Alert, inchangé). Il sert surtout à fixer
	/// la version du mod (ModVersion) dès le démarrage du jeu, avant tout menu et toute
	/// connexion : la version annoncée au serveur ne dépend plus de l'affichage du menu
	/// principal.
	/// </summary>
	public sealed class FictifsLoadScreen : ILoadScreen
	{
		readonly LogoStripeLoadScreen inner = new();

		public void Init(ModData m, Dictionary<string, string> info)
		{
			ModVersion.Apply(m);
			inner.Init(m, info);
		}

		public void Display() { inner.Display(); }

		public bool BeforeLoad() { return inner.BeforeLoad(); }

		public void StartGame(Arguments args) { inner.StartGame(args); }

		public void Dispose() { inner.Dispose(); }
	}
}
