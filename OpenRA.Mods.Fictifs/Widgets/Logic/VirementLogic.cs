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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Fictifs.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Fictifs.Widgets.Logic
{
	// Icône « Virement » en haut à gauche : ouvre un petit panneau avec les alliés
	// à qui l'on peut envoyer de l'argent et le montant (taxe : Virements.TaxPercent).
	public class VirementLogic : ChromeLogic
	{
		const int RowSpacing = 4;

		readonly World world;
		readonly Widget panneau;
		readonly Widget liste;
		readonly ButtonWidget modele;
		readonly Widget bas;
		readonly int listeY;
		readonly int panneauBas;

		bool ouvert;
		Player destinataire;
		List<Player> affiches = new();

		Player Moi => world.LocalPlayer;

		Virements Trait => Moi?.PlayerActor.TraitOrDefault<Virements>();

		bool Utilisable => Moi != null && !Moi.Spectating && Moi.WinState == WinState.Undefined && Trait != null;

		IEnumerable<Player> Destinataires => Utilisable ? Virements.Recipients(Moi) : Enumerable.Empty<Player>();

		int Disponible
		{
			get
			{
				var r = Moi?.PlayerActor.TraitOrDefault<PlayerResources>();
				return r != null ? r.Cash + r.Resources : 0;
			}
		}

		[ObjectCreator.UseCtor]
		public VirementLogic(Widget widget, World world)
		{
			this.world = world;

			var bouton = widget.Get<ButtonWidget>("VIREMENT_BOUTON");
			panneau = widget.Get("VIREMENT_PANNEAU");
			liste = panneau.Get("JOUEURS");
			modele = liste.Get<ButtonWidget>("JOUEUR_MODELE");
			liste.RemoveChild(modele);
			bas = panneau.Get("BAS");
			listeY = liste.Bounds.Y;
			panneauBas = panneau.Bounds.Height - bas.Bounds.Y;

			var montant = panneau.Get<TextFieldWidget>("MONTANT");
			var recu = bas.Get<LabelWidget>("RECU");
			var envoyer = bas.Get<ButtonWidget>("ENVOYER");

			widget.IsVisible = () => Utilisable;

			bouton.IsDisabled = () => !Destinataires.Any();
			bouton.IsHighlighted = () => ouvert;
			bouton.GetTooltipDesc = () => Destinataires.Any()
				? $"Envoyer de l'argent à un allié.\nTaxe de {Trait.Info.TaxPercent} % : l'allié reçoit {100 - Trait.Info.TaxPercent} % du montant."
				: "Aucun allié à qui faire un virement.";
			bouton.OnClick = () =>
			{
				ouvert = !ouvert;
				if (ouvert)
				{
					if (destinataire != null && !Destinataires.Contains(destinataire))
						destinataire = null;

					destinataire ??= Destinataires.FirstOrDefault();
					Reconstruire();
				}
				else
					montant.YieldKeyboardFocus();
			};

			var icone = bouton.GetOrNull<ImageWidget>("ICONE");
			if (icone != null)
				icone.GetImageName = () => bouton.IsDisabled() ? "virement-disabled" : "virement";

			panneau.IsVisible = () => ouvert && Utilisable;

			int Montant() => int.TryParse(montant.Text, out var m) ? m : 0;

			void Ajouter(string nom, Func<int, int> f)
			{
				var b = panneau.GetOrNull<ButtonWidget>(nom);
				if (b == null)
					return;

				b.OnClick = () => montant.Text = Math.Max(0, Math.Min(f(Montant()), Disponible)).ToString();
			}

			Ajouter("PLUS_100", m => m + 100);
			Ajouter("PLUS_500", m => m + 500);
			Ajouter("PLUS_1000", m => m + 1000);
			Ajouter("TOUT", _ => Disponible);
			Ajouter("ZERO", _ => 0);

			bool Valide()
			{
				var m = Montant();
				return destinataire != null && Destinataires.Contains(destinataire)
					&& m >= Trait.Info.MinAmount && m <= Disponible;
			}

			recu.GetText = () =>
			{
				if (destinataire == null)
					return "Choisissez un allié.";

				var m = Montant();
				if (m < Trait.Info.MinAmount)
					return $"Montant minimum : {Trait.Info.MinAmount} $.";

				if (m > Disponible)
					return $"Fonds insuffisants ({Disponible} $).";

				var r = Trait.Received(m);
				return $"{destinataire.PlayerName} recevra {r} $ (taxe {m - r} $).";
			};

			envoyer.IsDisabled = () => !Valide();
			envoyer.OnClick = () =>
			{
				if (!Valide())
					return;

				world.IssueOrder(Virements.Order(Moi, destinataire, Montant()));
				montant.Text = "";
				montant.YieldKeyboardFocus();
				ouvert = false;
			};

			montant.OnEnterKey = _ =>
			{
				envoyer.OnClick();
				return true;
			};

			var fermer = bas.GetOrNull<ButtonWidget>("FERMER");
			if (fermer != null)
				fermer.OnClick = () =>
				{
					montant.YieldKeyboardFocus();
					ouvert = false;
				};
		}

		public override void Tick()
		{
			// Un allié vaincu ou qui change d'alliance : la liste suit.
			if (ouvert && !affiches.SequenceEqual(Destinataires))
				Reconstruire();
		}

		void Reconstruire()
		{
			affiches = Destinataires.ToList();
			if (destinataire != null && !affiches.Contains(destinataire))
				destinataire = affiches.FirstOrDefault();

			liste.RemoveChildren();
			var y = 0;
			foreach (var p in affiches)
			{
				var joueur = p;
				var b = (ButtonWidget)modele.Clone();
				b.Bounds = new Rectangle(b.Bounds.X, y, b.Bounds.Width, b.Bounds.Height);
				b.GetText = () => joueur.PlayerName;
				b.GetColor = () => joueur.Color;
				b.IsHighlighted = () => destinataire == joueur;
				b.OnClick = () => destinataire = joueur;
				b.IsVisible = () => true;
				liste.AddChild(b);
				y += b.Bounds.Height + RowSpacing;
			}

			var hauteurListe = Math.Max(0, y - RowSpacing);
			liste.Bounds = new Rectangle(liste.Bounds.X, listeY, liste.Bounds.Width, hauteurListe);
			bas.Bounds = new Rectangle(bas.Bounds.X, listeY + hauteurListe + 6, bas.Bounds.Width, bas.Bounds.Height);
			panneau.Bounds = new Rectangle(panneau.Bounds.X, panneau.Bounds.Y, panneau.Bounds.Width, bas.Bounds.Y + panneauBas);
		}
	}
}
