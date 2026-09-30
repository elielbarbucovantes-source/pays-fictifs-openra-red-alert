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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Fictifs.Traits
{
	// Système pétrole : Oil Derrick capturé ── pipeline ── Oil Refinery du même joueur.
	// Le trait OilIncome (sur le joueur) recalcule les réseaux et paie chaque derrick relié.

	[Desc("Oil derrick that can be connected to an " + nameof(OilRefinery) + " by pipelines.")]
	public class OilDerrickInfo : TraitInfo
	{
		[GrantedConditionReference]
		[Desc("Condition granted while the derrick is connected to a refinery of its owner.")]
		public readonly string ConnectedCondition = null;

		public override object Create(ActorInitializer init) { return new OilDerrick(this); }
	}

	public class OilDerrick : INotifyOwnerChanged
	{
		readonly OilDerrickInfo info;
		int token = Actor.InvalidConditionToken;

		public OilDerrick(OilDerrickInfo info) { this.info = info; }

		public bool Connected { get; private set; }

		public void SetConnected(Actor self, bool connected)
		{
			if (Connected == connected)
				return;

			Connected = connected;
			if (string.IsNullOrEmpty(info.ConnectedCondition))
				return;

			if (connected && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.ConnectedCondition);
			else if (!connected && token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		// Capturé ou redevenu neutre : le lien est coupé tout de suite.
		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			SetConnected(self, false);
		}
	}

	[Desc("Pipeline segment: connects oil derricks to oil refineries of the same owner (4-neighbour adjacency).")]
	public class OilPipelineInfo : TraitInfo<OilPipeline> { }

	public class OilPipeline { }

	[Desc("Lets pipeline segments (WithWallSpriteBody) draw a connection towards this multi-cell building.")]
	public class OilPipelineConnectorInfo : TraitInfo
	{
		[Desc("Wall type the segments use (WithWallSpriteBody.Type).")]
		public readonly string Type = "pipeline";

		public override object Create(ActorInitializer init) { return new OilPipelineConnector(this); }
	}

	public class OilPipelineConnector : IWallConnector
	{
		readonly OilPipelineConnectorInfo info;

		public OilPipelineConnector(OilPipelineConnectorInfo info) { this.info = info; }

		bool IWallConnector.AdjacentWallCanConnect(Actor self, CPos wallLocation, string wallType, out CVec facing)
		{
			facing = CVec.Zero;
			if (wallType != info.Type)
				return false;

			foreach (var (cell, _) in self.OccupiesSpace.OccupiedCells())
			{
				var delta = wallLocation - cell;
				if (System.Math.Abs(delta.X) + System.Math.Abs(delta.Y) == 1)
				{
					facing = delta;
					return true;
				}
			}

			return false;
		}

		void IWallConnector.SetDirty() { }
	}

	[Desc("Oil refinery: every connected oil derrick earns money through the owner's " + nameof(OilIncome) + " trait.")]
	public class OilRefineryInfo : ConditionalTraitInfo
	{
		[Desc("Maximum number of derricks connected to this refinery.")]
		public readonly int MaxDerricks = 5;

		[Desc("Maximum distance between a derrick and this refinery.")]
		public readonly WDist MaxDistance = WDist.FromCells(30);

		[GrantedConditionReference]
		[Desc("Condition granted once per connected derrick (stacks).")]
		public readonly string DerrickCondition = null;

		public override object Create(ActorInitializer init) { return new OilRefinery(this); }
	}

	public class OilRefinery : ConditionalTrait<OilRefineryInfo>
	{
		readonly Stack<int> tokens = new();

		public OilRefinery(OilRefineryInfo info)
			: base(info) { }

		public int ConnectedDerricks => tokens.Count;

		public void SetConnectedCount(Actor self, int count)
		{
			if (string.IsNullOrEmpty(Info.DerrickCondition))
			{
				// Garde quand même le compte à jour.
				while (tokens.Count < count)
					tokens.Push(Actor.InvalidConditionToken);
				while (tokens.Count > count)
					tokens.Pop();
				return;
			}

			while (tokens.Count < count)
				tokens.Push(self.GrantCondition(Info.DerrickCondition));
			while (tokens.Count > count)
				self.RevokeCondition(tokens.Pop());
		}
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Pays the player for every oil derrick connected by pipelines to one of its oil refineries.",
		"Attach this to the player actor.")]
	public class OilIncomeInfo : TraitInfo
	{
		[Desc("Cash given per connected derrick every " + nameof(Interval) + " ticks.")]
		public readonly int AmountPerDerrick = 100;

		[Desc("Ticks between two payments.")]
		public readonly int Interval = 250;

		[Desc("Ticks between two network updates (connection indicators).")]
		public readonly int UpdateInterval = 25;

		[Desc("Show the cash tick above each refinery.")]
		public readonly bool ShowTicks = true;

		public readonly int DisplayDuration = 30;

		public override object Create(ActorInitializer init) { return new OilIncome(init.Self, this); }
	}

	public class OilIncome : ITick
	{
		static readonly CVec[] Directions = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };

		readonly OilIncomeInfo info;
		readonly Player player;
		readonly List<(Actor Actor, OilDerrick Derrick)> connected = new();
		readonly Dictionary<Actor, int> perRefinery = new();
		PlayerResources resources;
		int payTicks;
		int updateTicks;

		public OilIncome(Actor self, OilIncomeInfo info)
		{
			this.info = info;
			player = self.Owner;
			payTicks = info.Interval;
		}

		void ITick.Tick(Actor self)
		{
			if (--updateTicks <= 0)
			{
				updateTicks = info.UpdateInterval;
				UpdateNetwork(self.World);
			}

			if (--payTicks > 0)
				return;

			payTicks = info.Interval;

			// Vérification au moment du paiement : un derrick capturé ou détruit ne paie plus.
			UpdateNetwork(self.World);
			resources ??= self.Trait<PlayerResources>();
			foreach (var kv in perRefinery)
			{
				if (kv.Value == 0)
					continue;

				var amount = resources.ChangeCash(kv.Value * info.AmountPerDerrick);
				if (info.ShowTicks && amount != 0)
				{
					var refinery = kv.Key;
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(refinery.CenterPosition, player.Color,
						FloatingText.FormatCashTick(amount), info.DisplayDuration)));
				}
			}
		}

		static bool IsValid(Actor a, Player owner)
		{
			return a.Owner == owner && !a.IsDead && a.IsInWorld;
		}

		void UpdateNetwork(World world)
		{
			var pipes = new HashSet<CPos>();
			foreach (var p in world.ActorsWithTrait<OilPipeline>())
				if (IsValid(p.Actor, player))
					pipes.Add(p.Actor.Location);

			// Cases occupées par chaque derrick du joueur.
			var derrickAt = new Dictionary<CPos, (Actor Actor, OilDerrick Derrick)>();
			foreach (var d in world.ActorsWithTrait<OilDerrick>())
			{
				if (!IsValid(d.Actor, player))
					continue;

				foreach (var (cell, _) in d.Actor.OccupiesSpace.OccupiedCells())
					derrickAt[cell] = (d.Actor, d.Trait);
			}

			// Paires raffinerie ↔ derrick atteignable, triées par distance.
			var candidates = new List<(Actor Refinery, OilRefinery Trait, Actor Derrick, OilDerrick DerrickTrait, long Distance)>();
			var refineries = new List<(Actor Actor, OilRefinery Trait)>();
			foreach (var r in world.ActorsWithTrait<OilRefinery>())
			{
				if (!IsValid(r.Actor, player))
					continue;

				refineries.Add((r.Actor, r.Trait));
				if (r.Trait.IsTraitDisabled)
					continue;

				var maxDistSq = r.Trait.Info.MaxDistance.LengthSquared;
				foreach (var d in Reachable(r.Actor, pipes, derrickAt))
				{
					var distSq = (d.Actor.CenterPosition - r.Actor.CenterPosition).HorizontalLengthSquared;
					if (distSq <= maxDistSq)
						candidates.Add((r.Actor, r.Trait, d.Actor, d.Derrick, distSq));
				}
			}

			// Chaque derrick n'est compté qu'une fois, pour la raffinerie la plus proche qui a de la place.
			var assigned = new HashSet<Actor>();
			perRefinery.Clear();
			foreach (var r in refineries)
				perRefinery[r.Actor] = 0;

			foreach (var c in candidates.OrderBy(c => c.Distance).ThenBy(c => c.Derrick.ActorID).ThenBy(c => c.Refinery.ActorID))
			{
				if (assigned.Contains(c.Derrick) || perRefinery[c.Refinery] >= c.Trait.Info.MaxDerricks)
					continue;

				assigned.Add(c.Derrick);
				perRefinery[c.Refinery]++;
			}

			foreach (var r in refineries)
				r.Trait.SetConnectedCount(r.Actor, perRefinery[r.Actor]);

			// Indicateurs : on éteint les derricks qui ne sont plus reliés, on allume les autres.
			foreach (var c in connected)
				if (!assigned.Contains(c.Actor) && !c.Actor.IsDead)
					c.Derrick.SetConnected(c.Actor, false);

			connected.Clear();
			foreach (var d in derrickAt.Values.Distinct())
			{
				if (!assigned.Contains(d.Actor))
					continue;

				d.Derrick.SetConnected(d.Actor, true);
				connected.Add(d);
			}
		}

		// Parcours en largeur des pipelines à partir des cases de la raffinerie.
		static IEnumerable<(Actor Actor, OilDerrick Derrick)> Reachable(Actor refinery, HashSet<CPos> pipes,
			Dictionary<CPos, (Actor Actor, OilDerrick Derrick)> derrickAt)
		{
			var found = new HashSet<Actor>();
			var visited = new HashSet<CPos>();
			var queue = new Queue<CPos>();

			foreach (var (cell, _) in refinery.OccupiesSpace.OccupiedCells())
			{
				visited.Add(cell);
				queue.Enqueue(cell);
			}

			while (queue.Count > 0)
			{
				var cell = queue.Dequeue();
				foreach (var dir in Directions)
				{
					var next = cell + dir;
					if (!visited.Add(next))
						continue;

					if (derrickAt.TryGetValue(next, out var d))
					{
						// Un derrick termine la branche : on ne traverse pas un derrick.
						if (found.Add(d.Actor))
							yield return d;
					}
					else if (pipes.Contains(next))
						queue.Enqueue(next);
				}
			}
		}
	}
}
