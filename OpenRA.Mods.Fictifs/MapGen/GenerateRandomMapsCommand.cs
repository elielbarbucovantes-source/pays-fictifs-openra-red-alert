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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.MapGen
{
	/// <summary>
	/// Outil de test : génère des cartes aléatoires sans lancer le jeu.
	/// ./utility.sh --generate-random-maps NOMBRE [DOSSIER]
	/// </summary>
	sealed class GenerateRandomMapsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--generate-random-maps";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2 && int.TryParse(args[1], out _);
		}

		[Desc("NOMBRE [DOSSIER]", "Génère NOMBRE cartes aléatoires avec des réglages au hasard et les enregistre éventuellement dans DOSSIER.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			TranslationProvider.Initialize(modData, modData.DefaultFileSystem);
			var count = int.Parse(args[1]);
			var outDir = args.Length > 2 ? args[2] : null;
			if (outDir != null)
				Directory.CreateDirectory(outDir);

			var generator = modData.DefaultRules.Actors[SystemActors.EditorWorld].TraitInfos<IEditorMapGeneratorInfo>().First();
			var mapGrid = modData.Manifest.Get<MapGrid>();
			var random = new MersenneTwister(12345);
			var sizes = MapGeneratorLogic.MapSizes.Values.ToArray();

			int ok = 0, rejected = 0, crashed = 0;
			for (var i = 0; i < count; i++)
			{
				var settings = generator.GetSettings();
				settings.Randomize(random);
				var tileset = generator.Tilesets[random.Next(generator.Tilesets.Length)];
				var range = sizes[random.Next(sizes.Length)];
				var width = random.Next(range.X, range.Y);
				var size = new Size(width + 2, width + mapGrid.MaximumTerrainHeight * 2 + 2);

				// Réglages au hasard parmi les choix valides (joueurs d'abord : ils filtrent les symétries).
				foreach (var o in settings.Options.OfType<MapGeneratorMultiIntegerChoiceOption>())
					o.Value = o.Choices[random.Next(o.Choices.Length)];
				foreach (var o in settings.Options.OfType<MapGeneratorMultiChoiceOption>())
				{
					var valid = o.ValidChoices(modData.DefaultTerrainInfo[tileset], settings.PlayerCount);
					if (valid.Count > 0)
						o.Value = valid[random.Next(valid.Count)];
				}

				foreach (var o in settings.Options.OfType<MapGeneratorBooleanOption>())
					o.Value = random.Next(2) == 0;

				var args2 = settings.Compile(modData.DefaultTerrainInfo[tileset], size);
				var summary = string.Join(" ", settings.Options.Select(o => o switch
				{
					MapGeneratorMultiChoiceOption mo => $"{o.Id}={mo.Value}",
					MapGeneratorMultiIntegerChoiceOption mio => $"{o.Id}={mio.Value}",
					MapGeneratorBooleanOption bo => $"{o.Id}={bo.Value}",
					MapGeneratorIntegerOption io => $"{o.Id}={io.Value}",
					_ => o.Id
				}));

				var start = DateTime.UtcNow;
				try
				{
					var map = generator.Generate(modData, args2);
					if (outDir != null)
					{
						var path = Path.Combine(outDir, $"carte-{i}.oramap");
						File.Delete(path);
						File.WriteAllBytes(Path.Combine(outDir, $"carte-{i}.png"), map.SavePreview());
						using (var package = ZipFileLoader.Create(path))
							map.Save(package);

						// Relecture : la carte doit se charger comme une carte ordinaire.
						using (var package = new Folder(outDir).OpenPackage($"carte-{i}.oramap", modData.ModFiles))
						{
							var reloaded = new Map(modData, package);
							if (reloaded.Uid != map.Uid)
								throw new InvalidDataException("UID différent après relecture");
						}
					}

					ok++;
					Console.WriteLine($"OK      #{i} {tileset} {size} {(DateTime.UtcNow - start).TotalSeconds:F1}s {summary}");
				}
				catch (MapGenerationException e)
				{
					rejected++;
					Console.WriteLine($"REFUSÉE #{i} {tileset} {size} ({e.Message}) {summary}");
				}
				catch (Exception e)
				{
					crashed++;
					Console.WriteLine($"PLANTÉE #{i} {tileset} {size} {summary}");
					Console.WriteLine(e);
				}
			}

			Console.WriteLine($"Résultat : {ok} réussies, {rejected} refusées (réglages impossibles), {crashed} plantages.");
		}
	}
}
