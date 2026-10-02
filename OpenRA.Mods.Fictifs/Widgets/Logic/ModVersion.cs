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

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	/// <summary>
	/// Version du mod = version du moteur + commit installé (ex. release-20231010+8f4e95b).
	/// Le serveur OpenRA refuse les joueurs dont la version diffère : deux joueurs
	/// qui n'ont pas le même commit sont arrêtés à la connexion au lieu de se
	/// désynchroniser en pleine partie.
	/// </summary>
	public static class ModVersion
	{
		public const string VersionFile = ".version-commit";

		/// <summary>Racine du dépôt (mods/&lt;id&gt;/ → ../..).</summary>
		public static string Root(ModData modData)
		{
			return Path.GetFullPath(Path.Combine(modData.Manifest.Package.Name, "..", ".."));
		}

		public static void Apply(ModData modData)
		{
			var commit = ReadLocalCommit(Root(modData));
			if (string.IsNullOrEmpty(commit))
				return;

			var suffix = "+" + commit[..Math.Min(7, commit.Length)];
			foreach (var metadata in new[] { modData.Manifest.Metadata, Game.Mods[modData.Manifest.Id].Metadata })
			{
				var baseVersion = metadata.Version.Split('+')[0];
				metadata.Version = baseVersion + suffix;
			}
		}

		// Lit le commit installé sans lancer git : .git/refs, sinon packed-refs,
		// sinon le fichier laissé par une installation depuis le ZIP.
		public static string ReadLocalCommit(string root)
		{
			try
			{
				var git = Path.Combine(root, ".git");
				if (Directory.Exists(git))
				{
					var head = File.ReadAllText(Path.Combine(git, "HEAD")).Trim();
					if (!head.StartsWith("ref: ", StringComparison.Ordinal))
						return head;

					var reference = head[5..];
					var refPath = Path.Combine(git, reference);
					if (File.Exists(refPath))
						return File.ReadAllText(refPath).Trim();

					var packed = Path.Combine(git, "packed-refs");
					if (File.Exists(packed))
						foreach (var line in File.ReadAllLines(packed))
							if (line.EndsWith(" " + reference, StringComparison.Ordinal))
								return line.Split(' ')[0];

					return null;
				}

				var version = Path.Combine(root, VersionFile);
				return File.Exists(version) ? File.ReadAllText(version).Trim() : null;
			}
			catch (IOException)
			{
				return null;
			}
		}
	}
}
