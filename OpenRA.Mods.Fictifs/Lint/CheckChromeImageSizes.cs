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
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Lint;

namespace OpenRA.Mods.Fictifs.Lint
{
	// Les images de chrome (Image, Image2x, Image3x des collections de chrome.yaml) sont envoyées
	// telles quelles à la carte graphique : une image dont la largeur ou la hauteur n'est pas une
	// puissance de deux fait planter le jeu dès qu'elle s'affiche (« Non-power-of-two array »).
	// Ce contrôle (./utility.sh --check-yaml) l'attrape avant qu'on la publie.
	sealed class CheckChromeImageSizes : ILintPass
	{
		static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
		static readonly string[] ImageKeys = { "Image", "Image2x", "Image3x" };

		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			var vus = new HashSet<string>();
			foreach (var chrome in modData.Manifest.Chrome)
			{
				var nodes = MiniYaml.FromStream(modData.DefaultFileSystem.Open(chrome), chrome);
				foreach (var collection in nodes)
				{
					if (collection.Value?.Nodes == null)
						continue;

					foreach (var n in collection.Value.Nodes.Where(n => ImageKeys.Contains(n.Key)))
					{
						var image = n.Value.Value;
						if (string.IsNullOrEmpty(image) || !vus.Add(image))
							continue;

						if (!modData.DefaultFileSystem.TryOpen(image, out var s))
						{
							emitError($"{chrome}: collection `{collection.Key}` refers to missing image `{image}`.");
							continue;
						}

						using (s)
						{
							if (!TryReadPngSize(s, out var w, out var h))
								continue;

							if (!IsPowerOfTwo(w) || !IsPowerOfTwo(h))
								emitError($"{chrome}: chrome image `{image}` (collection `{collection.Key}`) is {w}x{h}; " +
									"both dimensions must be powers of two or the game crashes when it is drawn.");
						}
					}
				}
			}
		}

		static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;

		static bool TryReadPngSize(Stream s, out int width, out int height)
		{
			width = height = 0;
			var header = new byte[24];
			if (s.Read(header, 0, 24) != 24 || !header.Take(8).SequenceEqual(PngSignature))
				return false;

			width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
			height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
			return true;
		}
	}
}
