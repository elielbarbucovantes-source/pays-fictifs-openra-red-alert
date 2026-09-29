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

// Panneau « Carte aléatoire », adapté du MapGeneratorLogic d'OpenRA playtest-20260222.
// Différence majeure avec le playtest : la carte retenue n'est pas une carte
// « générée » éphémère. Elle est enregistrée comme .oramap dans le dossier des
// cartes de l'utilisateur, et apparaît donc ensuite dans « Custom Maps »,
// jouable comme n'importe quelle autre carte.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.MapGen
{
	public class MapGeneratorLogic : ChromeLogic
	{
		public static readonly IReadOnlyDictionary<string, int2> MapSizes = new Dictionary<string, int2>()
		{
			{ "Petite", new int2(48, 60) },
			{ "Moyenne", new int2(60, 90) },
			{ "Grande", new int2(90, 120) },
			{ "Immense", new int2(120, 160) },
		};

		static readonly IReadOnlyDictionary<string, string> TilesetNames = new Dictionary<string, string>()
		{
			{ "TEMPERAT", "Tempéré" },
			{ "SNOW", "Neige" },
			{ "DESERT", "Désert" },
			{ "INTERIOR", "Intérieur" },
		};

		static readonly MersenneTwister Random = new();

		readonly ModData modData;
		readonly IEditorMapGeneratorInfo generator;
		readonly IMapGeneratorSettings settings;

		readonly GeneratedMapPreviewWidget preview;
		readonly ScrollPanelWidget settingsPanel;
		readonly Widget checkboxSettingTemplate;
		readonly Widget textSettingTemplate;
		readonly Widget dropdownSettingTemplate;
		readonly Widget tilesetSetting;
		readonly Widget sizeSetting;

		ITerrainInfo selectedTerrain;
		string selectedSize;
		Size size;

		// Dernière carte générée avec succès (accès depuis le fil principal uniquement).
		Map generatedMap;
		MapGenerationArgs generatedArgs;

		volatile bool failed;
		volatile uint generationCounter = 0;
		volatile uint lastGeneration = 0;

		bool IsGenerating => lastGeneration != generationCounter;

		[ObjectCreator.UseCtor]
		internal MapGeneratorLogic(Widget widget, ModData modData, Action onExit, Action<string> onSelect)
		{
			this.modData = modData;

			generator = modData.DefaultRules.Actors[SystemActors.EditorWorld].TraitInfos<IEditorMapGeneratorInfo>().First();
			settings = generator.GetSettings();
			preview = widget.Get<GeneratedMapPreviewWidget>("PREVIEW");

			widget.Get("ERROR").IsVisible = () => failed && !IsGenerating;

			var previewTitleLabel = widget.Get<LabelWidget>("TITLE");
			previewTitleLabel.GetText = () => IsGenerating ? "Génération en cours..."
				: failed ? "Échec de la génération" : generatedArgs?.Title ?? "Carte aléatoire";

			var previewDetailsLabel = widget.GetOrNull<LabelWidget>("DETAILS");
			if (previewDetailsLabel != null)
			{
				var playersOption = settings.Options.FirstOrDefault(o => o.Id == "Players") as MapGeneratorMultiIntegerChoiceOption;
				previewDetailsLabel.GetText = () => $"Conquête — {playersOption?.Value ?? 0} joueurs";
				previewDetailsLabel.IsVisible = () => !failed;
			}

			var previewSizeLabel = widget.GetOrNull<LabelWidget>("SIZE");
			if (previewSizeLabel != null)
			{
				previewSizeLabel.IsVisible = () => !failed;
				previewSizeLabel.GetText = () => $"Taille : {size.Width - 2}x{size.Height - 2}";
			}

			settingsPanel = widget.Get<ScrollPanelWidget>("SETTINGS_PANEL");
			checkboxSettingTemplate = settingsPanel.Get<Widget>("CHECKBOX_TEMPLATE");
			textSettingTemplate = settingsPanel.Get<Widget>("TEXT_TEMPLATE");
			dropdownSettingTemplate = settingsPanel.Get<Widget>("DROPDOWN_TEMPLATE");
			settingsPanel.Layout = new GridLayout(settingsPanel);

			// Le tileset et la taille ne font pas partie des réglages du générateur :
			// on les ajoute à la main.
			var validTerrainInfos = generator.Tilesets
				.Where(modData.DefaultTerrainInfo.ContainsKey)
				.Select(t => modData.DefaultTerrainInfo[t])
				.ToList();

			tilesetSetting = dropdownSettingTemplate.Clone();
			tilesetSetting.Get<LabelWidget>("LABEL").GetText = () => "Climat";

			var tilesetDropdown = tilesetSetting.Get<DropDownButtonWidget>("DROPDOWN");
			tilesetDropdown.GetText = () => TilesetName(selectedTerrain);
			tilesetDropdown.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupItem(ITerrainInfo terrainInfo, ScrollItemWidget template)
				{
					bool IsSelected() => terrainInfo == selectedTerrain;
					void OnClick()
					{
						selectedTerrain = terrainInfo;
						RefreshSettings();
						GenerateMap();
					}

					var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
					var itemLabel = TilesetName(terrainInfo);
					item.Get<LabelWidget>("LABEL").GetText = () => itemLabel;
					return item;
				}

				tilesetDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", validTerrainInfos.Count * 30, validTerrainInfos, SetupItem);
			};

			sizeSetting = dropdownSettingTemplate.Clone();
			sizeSetting.Get<LabelWidget>("LABEL").GetText = () => "Taille";

			var sizeDropdown = sizeSetting.Get<DropDownButtonWidget>("DROPDOWN");
			sizeDropdown.GetText = () => selectedSize;
			sizeDropdown.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupItem(string size, ScrollItemWidget template)
				{
					bool IsSelected() => size == selectedSize;
					void OnClick()
					{
						selectedSize = size;
						RandomizeSize();
						GenerateMap();
					}

					var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => size;
					return item;
				}

				sizeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", MapSizes.Count * 30, MapSizes.Keys, SetupItem);
			};

			var generateButton = widget.Get<ButtonWidget>("BUTTON_GENERATE");
			generateButton.IsDisabled = () => IsGenerating;
			generateButton.OnClick = () =>
			{
				settings.Randomize(Random);
				RandomizeSize();
				GenerateMap();
			};

			var useButton = widget.Get<ButtonWidget>("BUTTON_USE");
			useButton.IsDisabled = () => IsGenerating || failed || generatedMap == null;
			useButton.OnClick = () =>
			{
				var uid = SaveGeneratedMap();
				if (uid == null)
				{
					failed = true;
					return;
				}

				Ui.CloseWindow();
				onSelect(uid);
			};

			widget.Get<ButtonWidget>("BUTTON_BACK").OnClick = () =>
			{
				Ui.CloseWindow();
				onExit();
			};

			selectedSize = MapSizes.Keys.Skip(1).First();
			selectedTerrain = validTerrainInfos[0];
			settings.Randomize(Random);
			RandomizeSize();
			RefreshSettings();
			GenerateMap();
		}

		static string TilesetName(ITerrainInfo terrainInfo)
		{
			if (terrainInfo == null)
				return "";

			return TilesetNames.TryGetValue(terrainInfo.Id, out var name) ? name : terrainInfo.Id;
		}

		void RandomizeSize()
		{
			var mapGrid = modData.Manifest.Get<MapGrid>();
			var sizeRange = MapSizes[selectedSize];
			var width = Random.Next(sizeRange.X, sizeRange.Y);
			var height =
				mapGrid.Type == MapGridType.RectangularIsometric
					? width * 2
					: width;

			size = new Size(width + 2, height + mapGrid.MaximumTerrainHeight * 2 + 2);
		}

		void RefreshSettings()
		{
			settingsPanel.RemoveChildren();
			tilesetSetting.Bounds = sizeSetting.Bounds = dropdownSettingTemplate.Bounds;
			settingsPanel.AddChild(tilesetSetting);
			settingsPanel.AddChild(sizeSetting);

			var playerCount = settings.PlayerCount;
			foreach (var o in settings.Options)
			{
				if (o.Id == "Seed")
					continue;

				Widget settingWidget = null;
				switch (o)
				{
					case MapGeneratorBooleanOption bo:
					{
						settingWidget = checkboxSettingTemplate.Clone();
						var checkboxWidget = settingWidget.Get<CheckboxWidget>("CHECKBOX");
						var label = MapGenCompat.Tr(bo.Label);
						checkboxWidget.GetText = () => label;
						checkboxWidget.IsChecked = () => bo.Value;
						checkboxWidget.OnClick = () =>
						{
							bo.Value ^= true;
							GenerateMap();
						};
						break;
					}

					case MapGeneratorIntegerOption io:
					{
						settingWidget = textSettingTemplate.Clone();
						var labelWidget = settingWidget.Get<LabelWidget>("LABEL");
						var label = MapGenCompat.Tr(io.Label);
						labelWidget.GetText = () => label;
						var textFieldWidget = settingWidget.Get<TextFieldWidget>("INPUT");
						textFieldWidget.Type = TextFieldType.Integer;
						textFieldWidget.Text = FieldSaver.FormatValue(io.Value);
						textFieldWidget.OnTextEdited = () =>
						{
							var valid = int.TryParse(textFieldWidget.Text, out io.Value);
							textFieldWidget.IsValid = () => valid;
						};

						textFieldWidget.OnEscKey = _ => { textFieldWidget.YieldKeyboardFocus(); return true; };
						textFieldWidget.OnEnterKey = _ => { textFieldWidget.YieldKeyboardFocus(); return true; };
						textFieldWidget.OnLoseFocus = GenerateMap;
						break;
					}

					case MapGeneratorMultiIntegerChoiceOption mio:
					{
						settingWidget = dropdownSettingTemplate.Clone();
						var labelWidget = settingWidget.Get<LabelWidget>("LABEL");
						var label = MapGenCompat.Tr(mio.Label);
						labelWidget.GetText = () => label;

						var dropDownWidget = settingWidget.Get<DropDownButtonWidget>("DROPDOWN");
						dropDownWidget.GetText = () => FieldSaver.FormatValue(mio.Value);
						dropDownWidget.OnMouseDown = _ =>
						{
							ScrollItemWidget SetupItem(int choice, ScrollItemWidget template)
							{
								bool IsSelected() => choice == mio.Value;
								void OnClick()
								{
									mio.Value = choice;
									if (o.Id == "Players")
										RefreshSettings();
									GenerateMap();
								}

								var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
								var itemLabel = FieldSaver.FormatValue(choice);
								item.Get<LabelWidget>("LABEL").GetText = () => itemLabel;
								return item;
							}

							dropDownWidget.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", mio.Choices.Length * 30, mio.Choices, SetupItem);
						};
						break;
					}

					case MapGeneratorMultiChoiceOption mo:
					{
						var validChoices = mo.ValidChoices(selectedTerrain, playerCount);
						if (!validChoices.Contains(mo.Value))
						{
							if (mo.Default != null)
								mo.Value = mo.Default.FirstOrDefault(validChoices.Contains);
							mo.Value ??= validChoices.FirstOrDefault();
						}

						if (mo.Label != null && validChoices.Count > 0)
						{
							settingWidget = dropdownSettingTemplate.Clone();
							var labelWidget = settingWidget.Get<LabelWidget>("LABEL");
							var label = MapGenCompat.Tr(mo.Label);
							labelWidget.GetText = () => label;

							var dropDownWidget = settingWidget.Get<DropDownButtonWidget>("DROPDOWN");
							dropDownWidget.GetText = () => mo.Value != null ? MapGenCompat.Tr(mo.Choices[mo.Value].Label) : "";
							dropDownWidget.OnMouseDown = _ =>
							{
								ScrollItemWidget SetupItem(string choice, ScrollItemWidget template)
								{
									bool IsSelected() => choice == mo.Value;
									void OnClick()
									{
										mo.Value = choice;
										GenerateMap();
									}

									var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
									var itemLabel = MapGenCompat.Tr(mo.Choices[choice].Label);
									item.Get<LabelWidget>("LABEL").GetText = () => itemLabel;
									return item;
								}

								dropDownWidget.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", validChoices.Count * 30, validChoices, SetupItem);
							};
						}

						break;
					}

					default:
						throw new NotImplementedException($"Unhandled MapGeneratorOption type {o.GetType().Name}");
				}

				if (settingWidget == null)
					continue;

				settingWidget.IsVisible = () => true;
				settingsPanel.AddChild(settingWidget);
			}
		}

		void GenerateMap()
		{
			var currentGeneration = Interlocked.Increment(ref generationCounter);

			failed = false;
			generatedMap = null;
			generatedArgs = null;
			preview.Clear();

			var terrain = selectedTerrain;
			var mapSize = size;
			MapGenerationArgs args;
			try
			{
				args = settings.Compile(terrain, mapSize);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Réglages du générateur de cartes invalides :");
				Log.Write("debug", e);
				lastGeneration = currentGeneration;
				failed = true;
				return;
			}

			var seed = args.Settings.NodeWithKeyOrDefault("Seed")?.Value.Value ?? "0";
			args.Title = $"{MapGenCompat.Tr(generator.MapTitle)} {seed.TrimStart('-')}";

			Task.Run(() =>
			{
				// Les tâches ne tournent pas en parallèle : on peut sauter les demandes périmées.
				if (currentGeneration != generationCounter)
					return;

				Map map;
				try
				{
					map = generator.Generate(modData, args);
				}
				catch (Exception e)
				{
					if (e is not MapGenerationException)
					{
						Log.Write("debug", "Le générateur de cartes a planté :");
						Log.Write("debug", e);
					}

					// Nous sommes la demande la plus récente : on signale l'échec.
					if (currentGeneration == generationCounter)
					{
						lastGeneration = currentGeneration;
						failed = true;
					}

					return;
				}

				// Les widgets ne se manipulent que depuis le fil principal.
				Game.RunAfterTick(() =>
				{
					// Une génération plus récente a été lancée entre-temps : on jette celle-ci.
					if (currentGeneration != generationCounter)
						return;

					generatedMap = map;
					generatedArgs = args;
					preview.Update(map);
					lastGeneration = currentGeneration;
				});
			});
		}

		/// <summary>
		/// Enregistre la carte générée dans le dossier des cartes de l'utilisateur et
		/// l'ajoute au cache des cartes. Renvoie l'UID de la carte, ou null en cas d'échec.
		/// </summary>
		string SaveGeneratedMap()
		{
			try
			{
				var folder = modData.MapCache.MapLocations
					.Where(kv => kv.Value == MapClassification.User)
					.Select(kv => kv.Key)
					.OfType<Folder>()
					.FirstOrDefault();

				if (folder == null)
					throw new InvalidOperationException("Aucun dossier de cartes utilisateur accessible en écriture.");

				var playerCount = generatedMap.PlayerDefinitions.Count(p => p.Key.StartsWith("PlayerReference@Multi", StringComparison.Ordinal));
				var seed = generatedArgs.Settings.NodeWithKeyOrDefault("Seed")?.Value.Value ?? "0";
				var baseName = $"aleatoire-{generatedArgs.Tileset.ToLowerInvariant()}-{playerCount}j-{seed.Replace('-', 'm')}";

				// Deux cartes différentes ne doivent pas s'écraser (mêmes réglages, autre taille...).
				var fileName = baseName + ".oramap";
				for (var i = 2; File.Exists(Path.Combine(folder.Name, fileName)); i++)
					fileName = $"{baseName}-{i}.oramap";

				var path = Path.Combine(folder.Name, fileName);
				using (var package = ZipFileLoader.Create(path))
					generatedMap.Save(package);

				// Attention : lire MapCache[uid] traite aussi les changements du dossier des
				// cartes, et peut donc avoir déjà chargé le fichier que l'on vient d'écrire.
				var uid = generatedMap.Uid;
				var existing = modData.MapCache[uid];
				if (existing.Status == MapStatus.Available && existing.Package?.Name != path)
				{
					// Cette carte exacte (même UID) est déjà installée ailleurs : on garde l'existante.
					File.Delete(path);
					return uid;
				}

				if (existing.Status != MapStatus.Available)
					modData.MapCache.LoadMap(fileName, folder, MapClassification.User, modData.Manifest.Get<MapGrid>(), null);

				if (modData.MapCache[uid].Status != MapStatus.Available)
					throw new InvalidOperationException($"La carte {path} n'a pas pu être rechargée.");

				Log.Write("debug", $"Carte aléatoire enregistrée : {path} ({uid})");
				return uid;
			}
			catch (Exception e)
			{
				Log.Write("debug", "Impossible d'enregistrer la carte aléatoire :");
				Log.Write("debug", e);
				return null;
			}
		}
	}

	/// <summary>
	/// Ajoute au sélecteur de cartes un bouton qui ouvre le générateur de cartes aléatoires.
	/// </summary>
	public class RandomMapGeneratorButtonLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		internal RandomMapGeneratorButtonLogic(Widget widget, ModData modData, Action<string> onSelect)
		{
			var button = widget.GetOrNull<ButtonWidget>("BUTTON_RANDOM_MAP_GENERATOR");
			if (button == null)
				return;

			var hasGenerator = modData.DefaultRules.Actors[SystemActors.EditorWorld].HasTraitInfo<IEditorMapGeneratorInfo>();
			button.IsVisible = () => hasGenerator;
			button.IsDisabled = () => onSelect == null;
			button.OnClick = () =>
			{
				Ui.OpenWindow("RANDOM_MAP_GENERATOR_PANEL", new WidgetArgs()
				{
					{ "onExit", () => { } },
					{
						"onSelect", (Action<string>)(uid =>
						{
							// Ferme aussi le sélecteur de cartes, puis choisit la nouvelle carte.
							Ui.CloseWindow();
							onSelect(uid);
						})
					},
				});
			};
		}
	}
}
