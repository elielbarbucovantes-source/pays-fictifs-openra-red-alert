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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Network;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	/// <summary>
	/// Partage des cartes absentes du Resource Center (cartes générées, cartes perso) :
	/// l'hôte sert ses cartes en HTTP sur le port 1235, et un joueur qui n'a pas la
	/// carte du salon la télécharge automatiquement chez l'hôte.
	/// Le même serveur sert aussi le classement Elo (voir ClassementEloStore).
	/// Placé dans MAP_STATUS_UNAVAILABLE, qui n'est visible (donc ne « tick »)
	/// que lorsque la carte est introuvable.
	/// </summary>
	public class CarteHoteLogic : ChromeLogic
	{
		public const int Port = 1235;
		const string Prefix = "/fictifs-carte/";

		static HttpListener listener;

		readonly ModData modData;
		readonly OrderManager orderManager;
		readonly Func<(MapPreview Map, Session.MapStatus Status)> getMap;
		readonly IPAddress host;

		string attemptedUid;
		volatile string status;

		[ObjectCreator.UseCtor]
		public CarteHoteLogic(Widget widget, ModData modData, OrderManager orderManager, Func<(MapPreview Map, Session.MapStatus Status)> getMap)
		{
			this.modData = modData;
			this.orderManager = orderManager;
			this.getMap = getMap;

			// Partie en réseau hébergée ici : on partage nos cartes.
			// Serveur distant : on retient son adresse pour y chercher la carte.
			// (En escarmouche, la connexion n'est pas une NetworkConnection.)
			var endPoint = (orderManager?.Connection as NetworkConnection)?.EndPoint;
			if (endPoint != null && IPAddress.IsLoopback(endPoint.Address))
				StartServer(modData);
			else if (endPoint != null)
				host = endPoint.Address.IsIPv4MappedToIPv6 ? endPoint.Address.MapToIPv4() : endPoint.Address;

			var a = widget.Get<LabelWidget>("a");
			var b = widget.Get<LabelWidget>("b");
			var textA = a.Text;
			var textB = b.Text;
			a.GetText = () => status ?? textA;
			b.GetText = () => status != null ? "" : textB;
		}

		public override void Tick()
		{
			if (host == null)
				return;

			var map = getMap().Map;
			if (map == MapCache.UnknownMap || map.Status != MapStatus.Unavailable || map.Uid == attemptedUid)
				return;

			attemptedUid = map.Uid;
			status = "Téléchargement chez l'hôte…";
			Task.Run(() => Download(map));
		}

		async Task Download(MapPreview map)
		{
			try
			{
				var installLocation = modData.MapCache.MapLocations.FirstOrDefault(p => p.Value == MapClassification.User);
				if (installLocation.Key is not IReadWritePackage mapInstallPackage)
					throw new InvalidOperationException("Dossier des cartes utilisateur introuvable");

				var client = HttpClientFactory.Create();
				var response = await client.GetAsync($"http://{FormatHost(host)}:{Port}{Prefix}{map.Uid}");
				if (!response.IsSuccessStatusCode)
				{
					status = "Carte absente chez l'hôte";
					return;
				}

				var filename = Path.GetFileName(response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "");
				if (string.IsNullOrEmpty(filename) || !filename.EndsWith(".oramap", StringComparison.OrdinalIgnoreCase))
					filename = map.Uid + ".oramap";

				mapInstallPackage.Update(filename, await response.Content.ReadAsByteArrayAsync());
				var package = mapInstallPackage.OpenPackage(filename, modData.ModFiles);
				if (package == null)
				{
					status = "Carte reçue illisible";
					return;
				}

				map.UpdateFromMap(package, mapInstallPackage, MapClassification.User, null, map.GridType, null);
				Log.Write("debug", $"Carte {map.Uid} téléchargée chez l'hôte dans {filename}");
				status = null;

				// Même suite que le bouton « Install » du moteur : on signale qu'on a la carte.
				Game.RunAfterTick(() => orderManager.IssueOrder(Order.Command($"state {Session.ClientState.NotReady}")));
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Téléchargement de la carte chez l'hôte impossible : {e}");
				status = "L'hôte n'a pas envoyé la carte";
			}
		}

		static string FormatHost(IPAddress address)
		{
			return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? $"[{address}]" : address.ToString();
		}

		// Ne sert que les cartes .oramap de l'utilisateur, demandées par leur UID.
		internal static void StartServer(ModData modData)
		{
			if (listener != null)
				return;

			try
			{
				listener = new HttpListener();
				listener.Prefixes.Add($"http://*:{Port}{Prefix}");
				listener.Prefixes.Add($"http://*:{Port}{ClassementEloStore.CheminHttp}");
				listener.Start();
				Log.Write("debug", $"Partage des cartes ouvert sur le port {Port}");
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Partage des cartes impossible : {e.Message}");
				listener = null;
				return;
			}

			Task.Run(async () =>
			{
				while (listener.IsListening)
				{
					HttpListenerContext context;
					try
					{
						context = await listener.GetContextAsync();
					}
					catch (Exception)
					{
						return;
					}

					try
					{
						if (context.Request.Url.AbsolutePath.StartsWith(ClassementEloStore.CheminHttp, StringComparison.Ordinal))
							ClassementEloStore.ServirHttp(context);
						else
							Serve(modData, context);
					}
					catch (Exception e)
					{
						Log.Write("debug", $"Envoi de carte échoué : {e.Message}");
					}
					finally
					{
						context.Response.Close();
					}
				}
			});
		}

		static void Serve(ModData modData, HttpListenerContext context)
		{
			var uid = context.Request.Url.AbsolutePath[Prefix.Length..].Trim('/');
			if (uid.Length != 40 || !uid.All(Uri.IsHexDigit))
			{
				context.Response.StatusCode = 404;
				return;
			}

			var map = modData.MapCache[uid];
			var path = map.Package?.Name;
			if (map.Status != MapStatus.Available || map.Class != MapClassification.User
				|| path == null || !File.Exists(path) || !path.EndsWith(".oramap", StringComparison.OrdinalIgnoreCase))
			{
				context.Response.StatusCode = 404;
				return;
			}

			var bytes = File.ReadAllBytes(path);
			context.Response.ContentType = "application/zip";
			context.Response.AddHeader("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(path)}\"");
			context.Response.ContentLength64 = bytes.Length;
			context.Response.OutputStream.Write(bytes, 0, bytes.Length);
		}
	}
}
