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
using OpenRA.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.MapGen
{
	/// <summary>
	/// Régions du monde réel pour le générateur de cartes.
	/// Les images viennent de tools/monde.py : PNG indexé, 0 = eau,
	/// 1 à 254 = terre d'altitude (valeur - 1) * 25 m. Ponts éventuels dans
	/// &lt;région&gt;.ponts : une ligne « x0 y0 x1 y1 » (pixels de l'image) par pont.
	/// </summary>
	public static class WorldRegion
	{
		public const int MetresPerStep = 25;

		static readonly Dictionary<string, Png> Cache = new();

		static Png Open(ModData modData, string folder, string region)
		{
			var path = folder + region + ".png";
			lock (Cache)
			{
				if (Cache.TryGetValue(path, out var cached))
					return cached;

				try
				{
					using (var s = modData.DefaultFileSystem.Open(path))
						return Cache[path] = new Png(s);
				}
				catch (Exception e)
				{
					throw new MapGenerationException($"Région du monde introuvable : {path} ({e.Message})");
				}
			}
		}

		/// <summary>
		/// Donne à la carte les proportions de la région (et la largeur imposée
		/// par la région, pour les cartes « Monde immense »).
		/// </summary>
		public static void AdjustSize(ModData modData, string folder, MapGenerationArgs args)
		{
			var region = args.Settings?.NodeWithKeyOrDefault("WorldRegion")?.Value.Value;
			if (string.IsNullOrEmpty(region) || folder == null)
				return;

			var png = Open(modData, folder, region);
			var width = args.Size.Width - 2;
			var forced = args.Settings.NodeWithKeyOrDefault("WorldRegionMapWidth")?.Value.Value;
			if (forced != null && int.TryParse(forced, out var w) && w > 0)
				width = w;

			var height = Math.Max(16, (int)Math.Round((double)width * png.Height / png.Width));
			args.Size = new Size(width + 2, height + 2);
		}

		// Recadrage « couvrant » : même échelle sur les deux axes, centré.
		static (double Scale, double X0, double Y0) Framing(Png png, int2 size)
		{
			var scale = Math.Max((double)size.X / png.Width, (double)size.Y / png.Height);
			return (scale, (png.Width - size.X / scale) / 2, (png.Height - size.Y / scale) / 2);
		}

		/// <summary>
		/// Altitude réelle de chaque cellule de la carte, en mètres (-1 pour l'eau).
		/// La région est centrée et recadrée pour remplir toute la carte.
		/// </summary>
		public static Matrix<int> Load(ModData modData, string folder, string region, int2 size)
		{
			var png = Open(modData, folder, region);
			var w = png.Width;
			var h = png.Height;
			var data = png.Data;
			var (scale, x0, y0) = Framing(png, size);

			const int Sub = 3;
			var altitude = new Matrix<int>(size);
			for (var y = 0; y < size.Y; y++)
			{
				for (var x = 0; x < size.X; x++)
				{
					int land = 0, sum = 0;
					for (var sy = 0; sy < Sub; sy++)
					{
						for (var sx = 0; sx < Sub; sx++)
						{
							var px = (int)(x0 + (x + (sx + 0.5) / Sub) / scale);
							var py = (int)(y0 + (y + (sy + 0.5) / Sub) / scale);
							var v = data[Math.Clamp(py, 0, h - 1) * w + Math.Clamp(px, 0, w - 1)];
							if (v == 0)
								continue;

							land++;
							sum += (v - 1) * MetresPerStep;
						}
					}

					altitude[x, y] = 2 * land > Sub * Sub ? sum / land : -1;
				}
			}

			return altitude;
		}

		/// <summary>Ponts de la région (s'il y en a), en cellules de la carte : départ et arrivée.</summary>
		public static List<(int2 From, int2 To)> LoadBridges(ModData modData, string folder, string region, int2 size)
		{
			var result = new List<(int2, int2)>();
			var path = folder + region + ".ponts";
			if (!modData.DefaultFileSystem.Exists(path))
				return result;

			var (scale, x0, y0) = Framing(Open(modData, folder, region), size);
			int2 ToCell(double px, double py) => new((int)((px - x0) * scale), (int)((py - y0) * scale));

			using (var s = modData.DefaultFileSystem.Open(path))
			using (var reader = new System.IO.StreamReader(s))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length != 4 || line.TrimStart().StartsWith('#'))
						continue;

					var v = parts.Select(p => double.Parse(p, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
					result.Add((ToCell(v[0], v[1]), ToCell(v[2], v[3])));
				}
			}

			return result;
		}
	}
}
