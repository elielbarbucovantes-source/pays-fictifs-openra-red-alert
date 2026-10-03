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
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	// Remplace ClassicProductionLogic : mêmes boutons de type, mais les files sont relues à
	// chaque image (files du joueur en mode normal, une file par usine en multi-file).
	// En multi-file, une rangée d'onglets numérotés sous la palette choisit l'usine ;
	// recliquer sur le bouton de type passe à l'usine suivante (Maj : précédente).
	public class MultiQueueProductionLogic : ChromeLogic
	{
		static readonly ProductionQueue[] NoQueues = Array.Empty<ProductionQueue>();

		readonly ProductionPaletteWidget palette;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly Dictionary<string, ProductionQueue[]> queuesByGroup = new();
		string currentGroup;

		static string GroupOf(ProductionQueue q) => q.Info.Group ?? q.Info.Type;

		ProductionQueue[] QueuesFor(string group)
		{
			return queuesByGroup.TryGetValue(group, out var queues) ? queues : NoQueues;
		}

		void UpdateQueues()
		{
			queuesByGroup.Clear();
			if (world.LocalPlayer == null)
				return;

			var player = world.LocalPlayer;
			var all = world.ActorsWithTrait<ProductionQueue>()
				.Where(x => x.Actor.Owner == player && x.Actor.IsInWorld && x.Trait.Enabled)
				.OrderBy(x => x.Actor == player.PlayerActor ? 0 : 1)
				.ThenBy(x => x.Actor.ActorID)
				.Select(x => x.Trait);

			foreach (var g in all.GroupBy(GroupOf))
				queuesByGroup[g.Key] = g.ToArray();
		}

		void SelectQueue(ProductionQueue queue)
		{
			palette.CurrentQueue = queue;
			currentGroup = queue != null ? GroupOf(queue) : null;

			// When a tab is selected, scroll to the top because the current row position may be invalid for the new tab
			palette.ScrollToTop();

			// Attempt to pick up a completed building (if there is one) so it can be placed
			palette.PickUpCompletedBuilding();
		}

		void SetupProductionGroupButton(ProductionTypeButtonWidget button)
		{
			if (button == null)
				return;

			var group = button.ProductionGroup;

			void SelectTab(bool reverse)
			{
				var queues = QueuesFor(group).Where(q => q.BuildableItems().Any()).ToList();
				if (queues.Count == 0)
					queues = QueuesFor(group).ToList();

				if (queues.Count == 0)
					return;

				// Déjà sur ce type : usine suivante (ou précédente avec Maj)
				var index = queues.IndexOf(palette.CurrentQueue);
				if (index < 0)
					SelectQueue(queues[0]);
				else
					SelectQueue(queues[(index + (reverse ? queues.Count - 1 : 1)) % queues.Count]);
			}

			button.IsDisabled = () => !QueuesFor(group).Any(q => q.BuildableItems().Any());
			button.OnMouseUp = mi => SelectTab(mi.Modifiers.HasModifier(Modifiers.Shift));
			button.OnKeyPress = e => SelectTab(e.Modifiers.HasModifier(Modifiers.Shift));
			button.OnClick = () => SelectTab(false);
			button.IsHighlighted = () => palette.CurrentQueue != null && GroupOf(palette.CurrentQueue) == group;

			var chromeName = group.ToLowerInvariant();
			var icon = button.Get<ImageWidget>("ICON");
			icon.GetImageName = () => button.IsDisabled() ? chromeName + "-disabled" :
				QueuesFor(group).Any(q => q.AllQueued().Any(i => i.Done)) ? chromeName + "-alert" : chromeName;
		}

		[ObjectCreator.UseCtor]
		public MultiQueueProductionLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			palette = widget.Get<ProductionPaletteWidget>("PRODUCTION_PALETTE");
			UpdateQueues();

			var factoryTabs = widget.GetOrNull("FACTORY_TABS");
			var background = widget.GetOrNull("PALETTE_BACKGROUND");
			var foreground = widget.GetOrNull("PALETTE_FOREGROUND");
			if (background != null || foreground != null)
			{
				Widget backgroundTemplate = null;
				Widget backgroundBottom = null;
				Widget foregroundTemplate = null;

				if (background != null)
				{
					backgroundTemplate = background.Get("ROW_TEMPLATE");
					backgroundBottom = background.GetOrNull("BOTTOM_CAP");
				}

				if (foreground != null)
					foregroundTemplate = foreground.Get("ROW_TEMPLATE");

				void UpdateBackground(int _, int icons)
				{
					var rows = Math.Max(palette.MinimumRows, (icons + palette.Columns - 1) / palette.Columns);
					rows = Math.Min(rows, palette.MaximumRows);

					if (background != null)
					{
						background.RemoveChildren();

						var rowHeight = backgroundTemplate.Bounds.Height;
						for (var i = 0; i < rows; i++)
						{
							var row = backgroundTemplate.Clone();
							row.Bounds.Y = i * rowHeight;
							background.AddChild(row);
						}

						// Les onglets d'usine suivent le bas de la palette
						if (factoryTabs != null)
							factoryTabs.Bounds.Y = rows * rowHeight + (backgroundBottom?.Bounds.Height ?? 0) + 2;

						if (backgroundBottom == null)
							return;

						backgroundBottom.Bounds.Y = rows * rowHeight;
						background.AddChild(backgroundBottom);
					}

					if (foreground != null)
					{
						foreground.RemoveChildren();

						var rowHeight = foregroundTemplate.Bounds.Height;
						for (var i = 0; i < rows; i++)
						{
							var row = foregroundTemplate.Clone();
							row.Bounds.Y = i * rowHeight;
							foreground.AddChild(row);
						}
					}
				}

				palette.OnIconCountChanged += UpdateBackground;

				// Set the initial palette state
				UpdateBackground(0, 0);
			}

			if (factoryTabs != null)
				SetupFactoryTabs(factoryTabs);

			var typesContainer = widget.Get("PRODUCTION_TYPES");
			foreach (var i in typesContainer.Children)
				SetupProductionGroupButton(i as ProductionTypeButtonWidget);

			var ticker = widget.Get<LogicTickerWidget>("PRODUCTION_TICKER");
			ticker.OnTick = () =>
			{
				UpdateQueues();

				if (palette.CurrentQueue != null && !palette.CurrentQueue.Enabled)
					palette.CurrentQueue = null;

				if (palette.CurrentQueue == null || palette.DisplayedIconCount == 0)
				{
					// Usine détruite ou vendue : rester sur le même type si possible
					var sameGroup = currentGroup != null ? QueuesFor(currentGroup).FirstOrDefault(q => q.BuildableItems().Any()) : null;
					if (sameGroup != null && sameGroup != palette.CurrentQueue)
					{
						SelectQueue(sameGroup);
						return;
					}

					// Select the first active tab
					foreach (var b in typesContainer.Children)
					{
						if (b is not ProductionTypeButtonWidget button || button.IsDisabled())
							continue;

						button.OnClick();
						break;
					}
				}
			};

			// Hook up scroll up and down buttons on the palette
			var scrollDown = widget.GetOrNull<ButtonWidget>("SCROLL_DOWN_BUTTON");

			if (scrollDown != null)
			{
				scrollDown.OnClick = palette.ScrollDown;
				scrollDown.IsVisible = () => palette.TotalIconCount > palette.MaxIconRowOffset * palette.Columns;
				scrollDown.IsDisabled = () => !palette.CanScrollDown;
			}

			var scrollUp = widget.GetOrNull<ButtonWidget>("SCROLL_UP_BUTTON");

			if (scrollUp != null)
			{
				scrollUp.OnClick = palette.ScrollUp;
				scrollUp.IsVisible = () => palette.TotalIconCount > palette.MaxIconRowOffset * palette.Columns;
				scrollUp.IsDisabled = () => !palette.CanScrollUp;
			}

			SetMaximumVisibleRows(palette);
		}

		// Onglets numérotés, un par usine du type affiché. Ne s'affichent qu'à partir de deux files
		// (donc jamais en mode normal, où il n'y a qu'une file par type).
		void SetupFactoryTabs(Widget tabs)
		{
			var template = tabs.Get<ButtonWidget>("TAB_TEMPLATE");
			tabs.RemoveChildren();

			var columns = Math.Max(1, (tabs.Bounds.Width + 2) / (template.Bounds.Width + 2));
			var readyColor = Color.FromArgb(255, 220, 60);
			var busyColor = Color.White;
			var idleColor = Color.FromArgb(150, 150, 150);

			ProductionQueue[] CurrentTabs()
			{
				var queues = palette.CurrentQueue != null ? QueuesFor(GroupOf(palette.CurrentQueue)) : NoQueues;
				return queues.Length > 1 ? queues : NoQueues;
			}

			tabs.IsVisible = () => CurrentTabs().Length > 0;

			// Assez d'onglets pour 4 rangées ; au-delà, l'usine reste accessible en la sélectionnant
			for (var i = 0; i < columns * 4; i++)
			{
				var index = i;
				var tab = (ButtonWidget)template.Clone();
				tab.Id = "TAB_" + (i + 1);
				tab.Bounds.X = (i % columns) * (template.Bounds.Width + 2);
				tab.Bounds.Y = (i / columns) * (template.Bounds.Height + 2);

				ProductionQueue Queue()
				{
					var queues = CurrentTabs();
					return index < queues.Length ? queues[index] : null;
				}

				tab.IsVisible = () => Queue() != null;
				tab.GetText = () => (index + 1).ToString();
				tab.IsHighlighted = () => Queue() != null && Queue() == palette.CurrentQueue;
				tab.GetColor = () =>
				{
					var q = Queue();
					if (q == null)
						return idleColor;

					var item = q.CurrentItem();
					return item == null ? idleColor : item.Done ? readyColor : busyColor;
				};

				tab.GetTooltipText = () =>
				{
					var q = Queue();
					if (q == null)
						return "";

					var name = q.Actor.TraitsImplementing<Tooltip>().FirstOrDefault(t => !t.IsTraitDisabled)?.Info.Name ?? q.Actor.Info.Name;
					var item = q.CurrentItem();
					var queued = q.AllQueued().Count();
					var status = item == null ? "inactive" : item.Done ? "prête" : $"{queued} en file";
					return $"{name} n°{index + 1} — {status}\nRecliquer : centrer la vue sur l'usine";
				};

				tab.OnClick = () =>
				{
					var q = Queue();
					if (q == null)
						return;

					if (q == palette.CurrentQueue)
						worldRenderer.Viewport.Center(q.Actor.CenterPosition);
					else
						SelectQueue(q);
				};

				tabs.AddChild(tab);
			}
		}

		static void SetMaximumVisibleRows(ProductionPaletteWidget productionPalette)
		{
			var screenHeight = Game.Renderer.Resolution.Height;

			// Get height of currently displayed icons
			var containerWidget = Ui.Root.GetOrNull<ContainerWidget>("SIDEBAR_PRODUCTION");

			if (containerWidget == null)
				return;

			var sidebarProductionHeight = containerWidget.Bounds.Y;

			// Check if icon heights exceed y resolution
			var maxItemsHeight = screenHeight - sidebarProductionHeight;

			var maxIconRowOffest = maxItemsHeight / productionPalette.IconSize.Y - 1;
			productionPalette.MaxIconRowOffset = Math.Min(maxIconRowOffest, productionPalette.MaximumRows);
		}
	}
}
