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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Widgets
{
	/// <summary>
	/// Encadré en bas à droite (à gauche de la barre latérale) :
	/// - survol d'un acteur visible : nom, points de vie et description ;
	/// - sinon, une seule unité sélectionnée : sa fiche ;
	/// - sinon, plusieurs unités sélectionnées : le nombre d'unités de chaque type.
	/// </summary>
	public class UnitInfoPanelWidget : Widget
	{
		const int PanelWidth = 300;
		const int Padding = 8;
		const int RightMargin = 5;
		const int BottomMargin = 5;
		const int HealthBarHeight = 5;
		const int MaxTypeLines = 14;

		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly SpriteFont titleFont;
		readonly SpriteFont textFont;

		// Retrouve l'acteur qui porte un ITooltipInfo (utile pour les espions déguisés).
		readonly Dictionary<ITooltipInfo, ActorInfo> tooltipOwners = new();

		int selectionHash = -1;
		int selectionRefresh;
		string[] selectionLines = Array.Empty<string>();
		int selectionTotal;

		public UnitInfoPanelWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			titleFont = Game.Renderer.Fonts["Bold"];
			textFont = Game.Renderer.Fonts["Regular"];
			IgnoreMouseOver = true;
			IgnoreChildMouseOver = true;

			foreach (var ai in world.Map.Rules.Actors.Values)
				foreach (var ti in ai.TraitInfos<ITooltipInfo>())
					tooltipOwners.TryAdd(ti, ai);
		}

		public override bool HandleMouseInput(MouseInput mi) { return false; }

		public override void Draw()
		{
			if (world.IsLoadingGameSave || world.Type != WorldType.Regular)
				return;

			var hovered = Ui.MouseOverWidget is ViewportControllerWidget ? ActorUnderCursor() : null;
			if (hovered != null)
			{
				DrawActor(hovered);
				return;
			}

			var selected = world.Selection.Actors.Where(IsShown).ToList();
			if (selected.Count == 1)
				DrawActor(selected[0]);
			else if (selected.Count > 1)
				DrawSelection(selected);
		}

		bool IsShown(Actor a)
		{
			return a.IsInWorld && !a.IsDead && !world.FogObscures(a) && Tooltip(a) != null;
		}

		static ITooltip Tooltip(Actor a)
		{
			return a.TraitsImplementing<ITooltip>().FirstEnabledTraitOrDefault();
		}

		Actor ActorUnderCursor()
		{
			var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (!world.Map.Contains(cell) || world.ShroudObscures(cell))
				return null;

			var worldPixel = worldRenderer.Viewport.ViewToWorldPx(Viewport.LastMousePos);
			return world.ScreenMap.ActorsAtMouse(worldPixel)
				.Where(a => a.Actor.Info.HasTraitInfo<ITooltipInfo>() && !world.FogObscures(a.Actor))
				.WithHighestSelectionPriority(worldPixel, Game.GetModifierKeys());
		}

		string DisplayName(ITooltip tooltip)
		{
			var o = tooltip.Owner;
			var stance = o == null || world.RenderPlayer == null ? PlayerRelationship.None : o.RelationshipWith(world.RenderPlayer);
			return tooltip.TooltipInfo.TooltipForPlayerStance(stance);
		}

		string Description(ITooltip tooltip, Actor a)
		{
			// Un espion déguisé montre la description de l'unité qu'il imite.
			if (!tooltipOwners.TryGetValue(tooltip.TooltipInfo, out var info))
				info = a.Info;

			var desc = info.TraitInfos<BuildableInfo>().Select(b => b.Description).FirstOrDefault(d => !string.IsNullOrEmpty(d));
			desc ??= info.TraitInfos<TooltipDescriptionInfo>()
				.Select(t => t.Description).FirstOrDefault(d => !string.IsNullOrEmpty(d));

			return desc?.Replace("\\n", "\n") ?? "";
		}

		void DrawActor(Actor a)
		{
			var tooltip = Tooltip(a);
			if (tooltip == null)
				return;

			var textWidth = PanelWidth - 2 * Padding;
			var name = WidgetUtils.TruncateText(DisplayName(tooltip), textWidth, titleFont);
			var health = a.TraitOrDefault<IHealth>();
			var hpText = health != null && health.MaxHP > 0 ? $"PV : {health.HP} / {health.MaxHP}" : null;
			var desc = Description(tooltip, a);
			desc = desc.Length > 0 ? WidgetUtils.WrapText(desc, textWidth, textFont) : null;

			var titleHeight = titleFont.Measure(name).Y;
			var lineHeight = textFont.Measure("Ag").Y;
			var height = Padding + titleHeight + 4;
			if (hpText != null)
				height += lineHeight + HealthBarHeight + 6;
			if (desc != null)
				height += 4 + textFont.Measure(desc).Y;
			height += Padding;

			var rect = PanelRect(height);
			WidgetUtils.DrawPanel("dialog4", rect);

			var x = rect.X + Padding;
			var y = rect.Y + Padding;
			titleFont.DrawTextWithShadow(name, new float2(x, y), Color.White, Color.Black, 1);
			y += titleHeight + 4;

			if (hpText != null)
			{
				var ratio = (float)health.HP / health.MaxHP;
				var color = ratio > 0.5f ? Color.LimeGreen : ratio > 0.25f ? Color.Gold : Color.Red;
				textFont.DrawTextWithShadow(hpText, new float2(x, y), color, Color.Black, 1);
				y += lineHeight + 2;

				WidgetUtils.FillRectWithColor(new Rectangle(x, y, textWidth, HealthBarHeight), Color.FromArgb(160, 0, 0, 0));
				WidgetUtils.FillRectWithColor(new Rectangle(x, y, (int)(textWidth * ratio), HealthBarHeight), color);
				y += HealthBarHeight + 4;
			}

			if (desc != null)
				textFont.DrawTextWithShadow(desc, new float2(x, y + 4), Color.LightGray, Color.Black, 1);
		}

		void DrawSelection(List<Actor> selected)
		{
			// Le décompte est recalculé quand la sélection change (ou de temps en temps, pour les déguisements).
			if (selectionHash != world.Selection.Hash || --selectionRefresh <= 0)
			{
				selectionHash = world.Selection.Hash;
				selectionRefresh = 30;
				selectionTotal = selected.Count;

				var groups = selected
					.GroupBy(a => DisplayName(Tooltip(a)))
					.Select(g => (Name: g.Key, Count: g.Count()))
					.OrderByDescending(g => g.Count)
					.ThenBy(g => g.Name)
					.ToList();

				var textWidth = PanelWidth - 2 * Padding;
				var lines = groups.Take(MaxTypeLines)
					.Select(g => WidgetUtils.TruncateText($"{g.Name} ×{g.Count}", textWidth, textFont))
					.ToList();

				if (groups.Count > MaxTypeLines)
				{
					var rest = groups.Skip(MaxTypeLines).Sum(g => g.Count);
					lines.Add($"… et {rest} autre{(rest > 1 ? "s" : "")}");
				}

				selectionLines = lines.ToArray();
			}

			var title = $"Sélection : {selectionTotal} unités";
			var titleHeight = titleFont.Measure(title).Y;
			var lineHeight = textFont.Measure("Ag").Y + 2;
			var height = Padding + titleHeight + 6 + selectionLines.Length * lineHeight + Padding;

			var rect = PanelRect(height);
			WidgetUtils.DrawPanel("dialog4", rect);

			var x = rect.X + Padding;
			var y = rect.Y + Padding;
			titleFont.DrawTextWithShadow(title, new float2(x, y), Color.White, Color.Black, 1);
			y += titleHeight + 6;

			foreach (var line in selectionLines)
			{
				textFont.DrawTextWithShadow(line, new float2(x, y), Color.LightGray, Color.Black, 1);
				y += lineHeight;
			}
		}

		static Rectangle PanelRect(int height)
		{
			var screen = Game.Renderer.Resolution;
			var x = Math.Max(0, screen.Width - RightMargin - PanelWidth);
			var y = Math.Max(0, screen.Height - BottomMargin - height);
			return new Rectangle(x, y, PanelWidth, height);
		}
	}
}
