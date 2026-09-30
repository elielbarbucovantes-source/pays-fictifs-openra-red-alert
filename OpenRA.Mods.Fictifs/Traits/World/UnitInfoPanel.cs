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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Fictifs.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Ajoute l'encadré d'informations (survol / sélection) en bas à droite de l'interface en jeu.")]
	public class UnitInfoPanelInfo : TraitInfo, Requires<LoadWidgetAtGameStartInfo>
	{
		[Desc("Conteneur de l'interface dans lequel l'encadré est ajouté.")]
		public readonly string Container = "WORLD_ROOT";

		public override object Create(ActorInitializer init) { return new UnitInfoPanel(this); }
	}

	public class UnitInfoPanel : IWorldLoaded
	{
		readonly UnitInfoPanelInfo info;

		public UnitInfoPanel(UnitInfoPanelInfo info) { this.info = info; }

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			if (w.Type != WorldType.Regular)
				return;

			// LoadWidgetAtGameStart (requis) a déjà créé INGAME_ROOT à ce stade.
			var parent = Ui.Root.GetOrNull(info.Container);
			parent?.AddChild(new UnitInfoPanelWidget(w, wr));
		}
	}
}
