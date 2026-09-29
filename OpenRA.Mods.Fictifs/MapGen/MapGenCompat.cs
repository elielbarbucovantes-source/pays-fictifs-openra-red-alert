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

// Petites fonctions du moteur playtest-20260222 absentes de release-20231010,
// nécessaires au générateur de cartes rétroporté.
using System.Globalization;
using System.IO;
using OpenRA.Primitives;

namespace OpenRA.Mods.MapGen
{
	public static class MapGenCompat
	{
		public static MiniYamlNode NodeWithKey(this MiniYaml yaml, string key)
		{
			var result = yaml.NodeWithKeyOrDefault(key);
			if (result == null)
				throw new InvalidDataException($"No node with key '{key}'");
			return result;
		}

		public static MiniYamlNode NodeWithKeyOrDefault(this MiniYaml yaml, string key)
		{
			MiniYamlNode result = null;
			foreach (var node in yaml.Nodes)
			{
				if (node.Key != key)
					continue;

				if (result != null)
					throw new InvalidDataException($"Duplicate key '{node.Key}' in {node.Location}");

				result = node;
			}

			return result;
		}

		/// <summary>Traduit une clé Fluent si elle existe, sinon renvoie le texte tel quel.</summary>
		public static string Tr(string keyOrText)
		{
			if (keyOrText == null)
				return null;

			return TranslationProvider.TryGetString(keyOrText, out var message) ? message : keyOrText;
		}

		public static bool TryParseUshortInvariant(string s, out ushort i)
		{
			return ushort.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i);
		}

		public static bool TryParseByteInvariant(string s, out byte i)
		{
			return byte.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i);
		}

		public static int ParseInt32Invariant(string s)
		{
			return int.Parse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
		}

		public static bool TryParseTerrainTile(string s, out TerrainTile tt)
		{
			var split = s.Split(',');
			if (split.Length == 2 &&
				TryParseUshortInvariant(split[0], out var type) &&
				TryParseByteInvariant(split[1], out var index))
			{
				tt = new TerrainTile(type, index);
				return true;
			}

			tt = default;
			return false;
		}

		public static int2 ToInt2(this Size size)
		{
			return new int2(size.Width, size.Height);
		}

		/// <summary>Map.MapSize est un int2 dans release-20231010, une Size ensuite.</summary>
		public static Size MapSizeAsSize(this Map map)
		{
			return new Size(map.MapSize.X, map.MapSize.Y);
		}

		public static CellRegion CellRegion<T>(this CellLayer<T> layer)
		{
			return new CellRegion(layer.GridType, new MPos(0, 0), new MPos(layer.Size.Width - 1, layer.Size.Height - 1));
		}
	}
}
