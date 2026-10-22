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
using System.Linq;
using OpenRA.Mods.Fictifs.ServerTraits;

namespace OpenRA.Mods.Fictifs.UtilityCommands
{
	/// <summary>
	/// Diagnostic de la reprise : ./utility.sh --test-reprise &lt;replay&gt; affiche ce que le
	/// serveur rejouerait (salon, dernière image complète, ordres de chaque joueur).
	/// </summary>
	sealed class TestRepriseCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--test-reprise";
		bool IUtilityCommand.ValidateArguments(string[] args) { return args.Length >= 2; }

		[Desc("REPLAY", "Analyse un replay pour la reprise.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var r = RepriseReplay.Lire(args[1]);
			Console.WriteLine($"NetFrameInterval {r.Salon.GlobalSettings.NetFrameInterval}");
			Console.WriteLine($"Carte {r.Salon.GlobalSettings.Map} graine {r.Salon.GlobalSettings.RandomSeed}");
			Console.WriteLine($"Derniere image {r.DerniereImage}, sync {r.ImageSync} ({r.Sync.Length} o)");
			if (r.ImageDesynchro > 0)
				Console.WriteLine($"Desynchronisation a l'image {r.ImageDesynchro}");
			foreach (var c in r.Salon.Clients)
				Console.WriteLine($"  client {c.Index} {c.Name} slot={c.Slot} bot={c.Bot} admin={c.IsAdmin}");
			foreach (var c in r.Participants)
			{
				var pleines = Enumerable.Range(1, r.DerniereImage).Count(i => r.Ordres(c.Index, i).Length > 0);
				var octets = Enumerable.Range(1, r.DerniereImage).Sum(i => r.Ordres(c.Index, i).Length);
				r.Deconnexions.TryGetValue(c.Index, out var d);
				Console.WriteLine($"  participant {c.Index} {c.Name}: {pleines} images avec ordres, {octets} o, derniere image {r.DerniereImageDe(c.Index)}, deconnexion {d}");
			}
		}
	}
}
