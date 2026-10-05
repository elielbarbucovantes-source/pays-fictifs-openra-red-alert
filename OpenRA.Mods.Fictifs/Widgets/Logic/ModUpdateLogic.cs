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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	/// <summary>
	/// Bouton « Mettre à jour » du menu principal : compare la version installée
	/// au dernier commit publié sur GitHub, puis ferme le jeu et lance le script
	/// update.sh / update.ps1 qui télécharge, recompile et relance le jeu.
	/// </summary>
	public class ModUpdateLogic : ChromeLogic
	{
		const string ResultFile = "update-result.txt";

		readonly string root;
		readonly string repository;
		readonly string branch;
		readonly string localCommit;

		volatile string status = "Recherche de mises à jour…";
		volatile bool updateAvailable;

		[ObjectCreator.UseCtor]
		public ModUpdateLogic(Widget widget, ModData modData, Dictionary<string, MiniYaml> logicArgs)
		{
			repository = logicArgs.TryGetValue("Repository", out var repo) ? repo.Value : null;
			branch = logicArgs.TryGetValue("Branch", out var br) ? br.Value : "main";

			root = ModVersion.Root(modData);
			localCommit = ModVersion.ReadLocalCommit(root);
			ModVersion.Apply(modData);

			var button = widget.Get<ButtonWidget>("UPDATE_BUTTON");
			var label = widget.Get<LabelWidget>("UPDATE_STATUS");
			label.GetText = () => status;
			label.GetColor = () => updateAvailable ? Color.Yellow : label.TextColor;
			button.GetText = () => updateAvailable ? "Mettre à jour !" : "Mettre à jour";
			button.IsDisabled = () => repository == null;

			button.OnClick = () => ConfirmationDialogs.ButtonPrompt(modData,
				title: "Mise à jour",
				text: "Le jeu va se fermer, télécharger la dernière version\n"
					+ "publiée sur GitHub, se recompiler (une à deux minutes)\n"
					+ "puis redémarrer tout seul.",
				onConfirm: StartUpdater,
				confirmText: "Mettre à jour",
				cancelText: "Annuler");

			// Résultat de la dernière mise à jour, affiché une fois au démarrage.
			var resultPath = Path.Combine(root, ResultFile);
			string lastResult = null;
			try
			{
				if (File.Exists(resultPath))
				{
					lastResult = File.ReadAllLines(resultPath).FirstOrDefault();
					File.Delete(resultPath);
				}
			}
			catch (IOException) { }

			if (!string.IsNullOrEmpty(lastResult))
				status = lastResult;

			if (repository != null)
				Task.Run(() => CheckRemote(lastResult != null));
		}

		async Task CheckRemote(bool keepResultMessage)
		{
			try
			{
				var client = HttpClientFactory.Create();
				var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repository}/commits/{branch}");
				request.Headers.Add("User-Agent", "OpenRA-mod-updater");
				request.Headers.Add("Accept", "application/vnd.github.sha");

				var response = await client.SendAsync(request);
				response.EnsureSuccessStatusCode();
				var remoteCommit = (await response.Content.ReadAsStringAsync()).Trim();

				if (localCommit == null)
					status = "Version installée inconnue";
				else if (remoteCommit.StartsWith(localCommit, StringComparison.OrdinalIgnoreCase)
						|| localCommit.StartsWith(remoteCommit, StringComparison.OrdinalIgnoreCase))
				{
					if (!keepResultMessage)
						status = "Le jeu est à jour";
				}
				else if (LocalContains(remoteCommit))
				{
					// PC de développement : des commits locaux pas encore publiés.
					if (!keepResultMessage)
						status = "Version locale en avance sur GitHub";
				}
				else
				{
					updateAvailable = true;
					status = "Nouvelle version disponible";
				}
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Vérification des mises à jour impossible : {e.Message}");
				if (!keepResultMessage)
					status = "GitHub injoignable";
			}
		}

		// Installation git : le commit publié est-il déjà dans l'historique local ?
		// Sans git (ZIP), ou si git ne connaît pas ce commit, on répond non.
		bool LocalContains(string commit)
		{
			if (!Directory.Exists(Path.Combine(root, ".git")))
				return false;

			try
			{
				var psi = new ProcessStartInfo("git") { UseShellExecute = false, WorkingDirectory = root, RedirectStandardError = true };
				foreach (var a in new[] { "merge-base", "--is-ancestor", commit, "HEAD" })
					psi.ArgumentList.Add(a);

				using var git = Process.Start(psi);
				if (!git.WaitForExit(5000))
				{
					git.Kill();
					return false;
				}

				return git.ExitCode == 0;
			}
			catch (Exception e)
			{
				Log.Write("debug", $"git merge-base impossible : {e.Message}");
				return false;
			}
		}

		void StartUpdater()
		{
			var pid = Environment.ProcessId.ToString();
			try
			{
				ProcessStartInfo psi;
				if (Platform.CurrentPlatform == PlatformType.Windows)
				{
					// Fenêtre de console visible : on suit la mise à jour.
					psi = new ProcessStartInfo("cmd.exe") { UseShellExecute = false };
					foreach (var a in new[] { "/c", "start", "Mise à jour", "powershell", "-NoProfile", "-ExecutionPolicy", "Bypass",
						"-File", Path.Combine(root, "update.ps1"), pid, repository, branch })
						psi.ArgumentList.Add(a);
				}
				else
				{
					// nohup : le script survit à la fermeture du jeu.
					psi = new ProcessStartInfo("/bin/sh") { UseShellExecute = false };
					psi.ArgumentList.Add("-c");
					psi.ArgumentList.Add("nohup /bin/bash ./update.sh \"$0\" \"$1\" \"$2\" > update.log 2>&1 &");
					psi.ArgumentList.Add(pid);
					psi.ArgumentList.Add(repository);
					psi.ArgumentList.Add(branch);
				}

				psi.WorkingDirectory = root;
				Process.Start(psi);
				Game.Exit();
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Lancement de la mise à jour impossible : {e}");
				status = "Échec du lancement de la mise à jour";
			}
		}
	}
}
