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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	/// <summary>Écran Créer un serveur : seulement l'adresse ZeroTier de ce PC.</summary>
	public class AdresseZeroTierLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public AdresseZeroTierLogic(Widget widget)
		{
			AmiZeroTierLogic.SetupAddress(widget);
		}
	}

	/// <summary>
	/// Bloc « Jouer avec un ami » des écrans Multijoueur et Créer un serveur :
	/// affiche (et copie) l'adresse ZeroTier de ce PC, mémorise l'adresse de
	/// l'ami et le rejoint en un clic. Tous les widgets sont facultatifs.
	/// </summary>
	public class AmiZeroTierLogic : ChromeLogic
	{
		const string FriendFile = "fictifs-ami.txt";
		const int DefaultPort = 1234;

		static readonly Action DoNothing = () => { };

		[ObjectCreator.UseCtor]
		public AmiZeroTierLogic(Widget widget, Action onStart, Action onExit)
		{
			SetupAddress(widget);

			var friendField = widget.GetOrNull<TextFieldWidget>("FRIEND_ADDRESS");
			var joinButton = widget.GetOrNull<ButtonWidget>("JOIN_FRIEND");
			if (friendField == null || joinButton == null)
				return;

			friendField.Text = LoadFriend();
			joinButton.IsDisabled = () => string.IsNullOrWhiteSpace(friendField.Text);
			joinButton.OnClick = () =>
			{
				var text = friendField.Text.Trim();
				SaveFriend(text);
				friendField.YieldKeyboardFocus();
				ConnectionLogic.Connect(ParseTarget(text), "", () => OpenLobby(onStart, onExit), DoNothing);
			};
			friendField.OnEnterKey = _ => { joinButton.OnClick(); return true; };
		}

		/// <summary>Label MY_ADDRESS et bouton COPY_ADDRESS, si présents.</summary>
		public static void SetupAddress(Widget widget)
		{
			var address = FindZeroTierAddress();
			var copied = false;

			var myAddress = widget.GetOrNull<LabelWidget>("MY_ADDRESS");
			if (myAddress != null)
			{
				myAddress.GetText = () => address != null ? "Ton adresse : " + address : "ZeroTier non détecté";
				myAddress.GetColor = () => address != null ? myAddress.TextColor : Color.Orange;
			}

			var copyButton = widget.GetOrNull<ButtonWidget>("COPY_ADDRESS");
			if (copyButton != null)
			{
				copyButton.IsDisabled = () => address == null;
				copyButton.GetText = () => copied ? "Adresse copiée !" : "Copier mon adresse";
				copyButton.OnClick = () => copied = Game.SetClipboardText(address);
			}
		}

		// Même enchaînement que MultiplayerLogic.OpenLobby : on ferme le
		// navigateur de parties et on le rouvre en quittant le salon.
		static void OpenLobby(Action onStart, Action onExit)
		{
			Ui.CloseWindow();

			void OnLobbyExit()
			{
				Ui.OpenWindow("MULTIPLAYER_PANEL", new WidgetArgs
				{
					{ "onStart", onStart },
					{ "onExit", onExit },
					{ "directConnectEndPoint", null },
				});

				Game.Disconnect();
				DiscordService.UpdateStatus(DiscordState.InMenu);
			}

			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onStart", onStart },
				{ "onExit", OnLobbyExit },
				{ "skirmishMode", false }
			});
		}

		static ConnectionTarget ParseTarget(string text)
		{
			// « 10.1.2.3 » ou « 10.1.2.3:1234 » (une adresse IPv6 contient plusieurs « : »).
			var colon = text.LastIndexOf(':');
			if (colon > 0 && text.IndexOf(':') == colon && Exts.TryParseIntegerInvariant(text[(colon + 1)..], out var port))
				return new ConnectionTarget(text[..colon], port);

			return new ConnectionTarget(text, DefaultPort);
		}

		/// <summary>
		/// Adresse IPv4 de l'interface ZeroTier : « zt… » sous Linux, « feth… »
		/// sous macOS, « ZeroTier One » dans la description sous Windows.
		/// </summary>
		public static string FindZeroTierAddress()
		{
			try
			{
				return NetworkInterface.GetAllNetworkInterfaces()
					.Where(n => n.OperationalStatus != OperationalStatus.Down
						&& (n.Name.StartsWith("zt", StringComparison.Ordinal)
							|| n.Name.StartsWith("feth", StringComparison.Ordinal)
							|| n.Description.Contains("ZeroTier", StringComparison.OrdinalIgnoreCase)))
					.SelectMany(n => n.GetIPProperties().UnicastAddresses)
					.Select(a => a.Address)
					.Where(a => a.AddressFamily == AddressFamily.InterNetwork)
					.Select(a => a.ToString())
					.FirstOrDefault();
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Interfaces réseau illisibles : {e.Message}");
				return null;
			}
		}

		static string FriendPath => Path.Combine(Platform.SupportDir, FriendFile);

		static string LoadFriend()
		{
			try
			{
				return File.Exists(FriendPath) ? File.ReadAllText(FriendPath).Trim() : "";
			}
			catch (IOException)
			{
				return "";
			}
		}

		static void SaveFriend(string text)
		{
			try
			{
				File.WriteAllText(FriendPath, text);
			}
			catch (IOException e)
			{
				Log.Write("debug", $"Adresse de l'ami non enregistrée : {e.Message}");
			}
		}
	}
}
