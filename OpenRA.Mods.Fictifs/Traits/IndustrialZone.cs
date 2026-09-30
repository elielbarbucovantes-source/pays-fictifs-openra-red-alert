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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Zone industrielle : objectif militaire secondaire. Elle appartient au camp dont les
	// unités sont seules dans son rayon ; son propriétaire reçoit un prérequis (bonus de
	// production des véhicules, voir zones.yaml) sauf si un camp ennemi est aussi présent.

	[Desc("Neutral objective controlled by presence: ground units of a single side inside the radius",
		"take it after CaptureDelay; units of two hostile sides make it contested. An empty zone keeps its controller.")]
	public class IndustrialZoneInfo : TraitInfo
	{
		[Desc("Control radius.")]
		public readonly WDist Radius = new(5120);

		[Desc("Ticks a side must stay alone in the zone to take it.")]
		public readonly int CaptureDelay = 125;

		[Desc("Ticks between two scans of the zone.")]
		public readonly int ScanInterval = 5;

		[GrantedConditionReference]
		[Desc("Condition granted while units of two hostile sides are in the zone.")]
		public readonly string ContestedCondition = "zone-contestee";

		[GrantedConditionReference]
		[Desc("Condition granted while a side is taking the zone.")]
		public readonly string CapturingCondition = "zone-capture";

		public readonly string CapturedTextNotification = null;
		public readonly string LostTextNotification = null;
		public readonly string ContestedTextNotification = null;

		public override object Create(ActorInitializer init) { return new IndustrialZone(this); }
	}

	public class IndustrialZone : ITick, INotifyOwnerChanged, ISelectionBar
	{
		readonly IndustrialZoneInfo info;
		int scan;
		int progress;
		Player capturer;
		bool contested;
		int contestedToken = Actor.InvalidConditionToken;
		int capturingToken = Actor.InvalidConditionToken;

		public IndustrialZone(IndustrialZoneInfo info) { this.info = info; }

		public bool Contested => contested;

		static bool Counts(Actor a)
		{
			// Unités au sol ou navires d'un joueur combattant (IA de mission comprise) ;
			// ni avions, ni bâtiments, ni épaves.
			if (a.IsDead || !a.IsInWorld || a.Owner.NonCombatant)
				return false;

			return a.Info.HasTraitInfo<MobileInfo>() && !a.Info.HasTraitInfo<AircraftInfo>() && !a.Info.HasTraitInfo<HuskInfo>();
		}

		void ITick.Tick(Actor self)
		{
			if (--scan > 0)
				return;

			scan = info.ScanInterval;

			var present = self.World.FindActorsInCircle(self.CenterPosition, info.Radius)
				.Where(Counts)
				.GroupBy(a => a.Owner)
				.Select(g => (Player: g.Key, Count: g.Count()))
				.ToList();

			var nowContested = present.Any(p => present.Any(q => p.Player.RelationshipWith(q.Player) == PlayerRelationship.Enemy));
			SetContested(self, nowContested);

			Player candidate = null;
			if (!nowContested && present.Count > 0)
			{
				// Un seul camp : l'allié le plus nombreux prend la zone.
				candidate = present.OrderByDescending(p => p.Count).ThenBy(p => p.Player.ClientIndex).First().Player;
				if (candidate == self.Owner || (!self.Owner.NonCombatant && self.Owner.IsAlliedWith(candidate)))
					candidate = null;
			}

			if (candidate == null)
			{
				capturer = null;
				progress = 0;
			}
			else
			{
				if (capturer != candidate)
				{
					capturer = candidate;
					progress = 0;
				}

				progress += info.ScanInterval;
				if (progress >= info.CaptureDelay)
				{
					var taker = capturer;
					capturer = null;
					progress = 0;
					self.ChangeOwner(taker);
				}
			}

			SetCapturing(self, capturer != null);
		}

		void SetContested(Actor self, bool value)
		{
			if (contested == value)
				return;

			contested = value;
			if (value)
			{
				contestedToken = self.GrantCondition(info.ContestedCondition);
				if (!string.IsNullOrEmpty(info.ContestedTextNotification) && !self.Owner.NonCombatant)
					TextNotificationsManager.AddTransientLine(info.ContestedTextNotification, self.Owner);
			}
			else if (contestedToken != Actor.InvalidConditionToken)
				contestedToken = self.RevokeCondition(contestedToken);
		}

		void SetCapturing(Actor self, bool value)
		{
			if (value && capturingToken == Actor.InvalidConditionToken)
				capturingToken = self.GrantCondition(info.CapturingCondition);
			else if (!value && capturingToken != Actor.InvalidConditionToken)
				capturingToken = self.RevokeCondition(capturingToken);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (!string.IsNullOrEmpty(info.LostTextNotification) && !oldOwner.NonCombatant)
				TextNotificationsManager.AddTransientLine(info.LostTextNotification, oldOwner);

			if (!string.IsNullOrEmpty(info.CapturedTextNotification) && !newOwner.NonCombatant)
				TextNotificationsManager.AddTransientLine(info.CapturedTextNotification, newOwner);
		}

		float ISelectionBar.GetValue()
		{
			return capturer == null ? 0 : (float)progress / info.CaptureDelay;
		}

		Color ISelectionBar.GetColor() { return capturer?.Color ?? Color.White; }

		bool ISelectionBar.DisplayWhenEmpty => false;
	}

	[TraitLocation(SystemActors.World)]
	[Desc("Places industrial zones at the start of the game, between the players' bases (lobby option).",
		"Zones already on the map are kept in \"auto\" mode and removed with \"none\".")]
	public class IndustrialZoneSpawnerInfo : TraitInfo, ILobbyOptions
	{
		[ActorReference]
		[Desc("Zone actor.")]
		public readonly string Actor = "zone.industrielle";

		public readonly string Label = "Zones industrielles";
		public readonly string Description = "Zones à tenir : +15 % de vitesse de production des véhicules.\nAuto = celles de la carte, sinon une par joueur.";
		public readonly int DisplayOrder = 6;
		public readonly string Default = "auto";

		[Desc("Minimum distance between a zone and a player's starting location.")]
		public readonly WDist MinHomeDistance = new(16384);

		[Desc("Minimum distance between two zones.")]
		public readonly WDist MinSpacing = new(12288);

		[Desc("Cells between two candidate locations.")]
		public readonly int SearchStep = 2;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			var values = new Dictionary<string, string>
			{
				{ "auto", "Auto" },
				{ "none", "Aucune" },
			};

			for (var i = 1; i <= 6; i++)
				values.Add(i.ToString(), i.ToString());

			yield return new LobbyOption(map, "industrialzones", Label, Description, true, DisplayOrder, values, Default, false);
		}

		public override object Create(ActorInitializer init) { return new IndustrialZoneSpawner(this); }
	}

	public class IndustrialZoneSpawner : ITick
	{
		readonly IndustrialZoneSpawnerInfo info;
		bool done;

		public IndustrialZoneSpawner(IndustrialZoneSpawnerInfo info) { this.info = info; }

		// Au premier tick et pas au chargement : le pathfinder n'est pas encore prêt dans WorldLoaded.
		void ITick.Tick(Actor self)
		{
			if (done)
				return;

			done = true;
			self.World.AddFrameEndTask(Spawn);
		}

		void Spawn(World w)
		{
			var option = w.LobbyInfo.GlobalSettings.OptionOrDefault("industrialzones", info.Default);
			var existing = w.Actors.Where(a => a.Info.Name == info.Actor).ToList();
			var homes = w.Players.Where(p => p.Playable && !p.NonCombatant).Select(p => p.HomeLocation).Distinct().ToList();

			int count;
			if (option == "none")
			{
				foreach (var a in existing)
					a.Dispose();

				return;
			}
			else if (option == "auto")
			{
				if (existing.Count > 0 || homes.Count < 2)
					return;

				count = homes.Count;
			}
			else if (!int.TryParse(option, out count))
				return;

			count -= existing.Count;
			if (count <= 0)
				return;

			var ai = w.Map.Rules.Actors[info.Actor];
			var bi = ai.TraitInfo<BuildingInfo>();
			var neutral = w.Players.First(p => p.InternalName == "Neutral");
			var placed = existing.Select(a => a.CenterPosition).ToList();

			// Une zone doit être accessible à pied depuis au moins une base.
			var locomotor = w.WorldActor.TraitsImplementing<Locomotor>().FirstOrDefault(l => l.Info.Name == "wheeled")
				?? w.WorldActor.TraitsImplementing<Locomotor>().FirstOrDefault();
			var pathFinder = w.WorldActor.TraitOrDefault<PathFinder>();

			var candidates = new List<(CPos Cell, WPos Pos, long Score)>();
			var bounds = w.Map.Bounds;
			var center = w.Map.CenterOfCell(new MPos((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2).ToCPos(w.Map));
			for (var v = bounds.Top + 2; v < bounds.Bottom - 2; v += info.SearchStep)
			{
				for (var u = bounds.Left + 2; u < bounds.Right - 2; u += info.SearchStep)
				{
					var cell = new MPos(u, v).ToCPos(w.Map);
					if (!w.CanPlaceBuilding(cell, ai, bi, null))
						continue;

					var pos = w.Map.CenterOfCell(cell) + bi.CenterOffset(w);
					var dists = homes.Select(h => (pos - w.Map.CenterOfCell(h)).Length).ToList();
					if (dists.Min() < info.MinHomeDistance.Length)
						continue;

					if (locomotor != null && pathFinder != null && !homes.Any(h => pathFinder.PathExistsForLocomotor(locomotor, h, cell)))
						continue;

					// Équitable (même distance de chaque base) et pas trop loin de l'action.
					var score = 3L * (dists.Max() - dists.Min()) + dists.Sum() / dists.Count;
					candidates.Add((cell, pos, score));
				}
			}

			foreach (var c in candidates.OrderBy(c => c.Score).ThenBy(c => (c.Pos - center).LengthSquared))
			{
				if (count == 0)
					break;

				if (placed.Any(p => (p - c.Pos).Length < info.MinSpacing.Length))
					continue;

				// Deux zones posées au même tour : la seconde ne doit pas recouvrir la première.
				if (!w.CanPlaceBuilding(c.Cell, ai, bi, null))
					continue;

				w.CreateActor(info.Actor, new TypeDictionary
				{
					new LocationInit(c.Cell),
					new OwnerInit(neutral),
				});

				placed.Add(c.Pos);
				count--;
			}
		}
	}
}
