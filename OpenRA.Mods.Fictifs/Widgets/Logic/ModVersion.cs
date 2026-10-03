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
using System.Security.Cryptography;
using System.Text;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	/// <summary>
	/// Version du mod = version du moteur + commit installé + empreinte du contenu
	/// (ex. release-20231010+8f4e95b.3fa91c). Le serveur OpenRA refuse les joueurs
	/// dont la version diffère : deux joueurs qui n'ont pas exactement les mêmes
	/// règles et le même code sont arrêtés à la connexion au lieu de se
	/// désynchroniser dès la première image (« out of sync frame 1 »).
	/// L'empreinte couvre aussi les modifications locales pas encore publiées,
	/// que le seul numéro de commit ne voit pas.
	/// </summary>
	public static class ModVersion
	{
		public const string VersionFile = ".version-commit";

		/// <summary>Racine du dépôt (mods/&lt;id&gt;/ → ../..).</summary>
		public static string Root(ModData modData)
		{
			return Path.GetFullPath(Path.Combine(modData.Manifest.Package.Name, "..", ".."));
		}

		// Dossiers dont le contenu change le déroulement d'une partie.
		static readonly string[] FingerprintDirs = { "mods/fictifs", "OpenRA.Mods.Fictifs" };
		static readonly string[] TextExtensions = { ".yaml", ".cs", ".lua", ".ftl", ".txt", ".md", ".csproj" };

		static string suffix;

		public static void Apply(ModData modData)
		{
			if (suffix == null)
			{
				var root = Root(modData);
				var commit = ReadLocalCommit(root);
				var fingerprint = ContentFingerprint(root);
				suffix = "+" + (string.IsNullOrEmpty(commit) ? "local" : commit[..Math.Min(7, commit.Length)])
					+ (fingerprint != null ? "." + fingerprint : "");
			}

			foreach (var metadata in new[] { modData.Manifest.Metadata, Game.Mods[modData.Manifest.Id].Metadata })
			{
				var baseVersion = metadata.Version.Split('+')[0];
				metadata.Version = baseVersion + suffix;
			}
		}

		/// <summary>
		/// Empreinte (6 caractères hexadécimaux) des fichiers du mod et de son code source,
		/// indépendante de l'ordre du disque et des fins de ligne Windows.
		/// Les dossiers de compilation (bin/obj) et les fichiers cachés ou de sauvegarde sont ignorés.
		/// </summary>
		public static string ContentFingerprint(string root)
		{
			try
			{
				var files = FingerprintDirs
					.Select(d => Path.Combine(root, d))
					.Where(Directory.Exists)
					.SelectMany(d => Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories))
					.Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
					.Where(Included)
					.OrderBy(f => f, StringComparer.Ordinal);

				using var sha = SHA1.Create();
				foreach (var file in files)
				{
					var bytes = File.ReadAllBytes(Path.Combine(root, file));
					if (TextExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
						bytes = bytes.Where(b => b != (byte)'\r').ToArray();

					var name = Encoding.UTF8.GetBytes(file + "\n" + bytes.Length + "\n");
					sha.TransformBlock(name, 0, name.Length, null, 0);
					sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
				}

				sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
				return Convert.ToHexString(sha.Hash)[..6].ToLowerInvariant();
			}
			catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
			{
				return null;
			}
		}

		static bool Included(string path)
		{
			foreach (var part in path.Split('/'))
				if (part.StartsWith('.') || part == "bin" || part == "obj" || part == "__pycache__")
					return false;

			return !path.EndsWith('~') && !path.EndsWith(".bak", StringComparison.Ordinal)
				&& !path.EndsWith(".save", StringComparison.Ordinal) && !path.EndsWith(".orig", StringComparison.Ordinal);
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
