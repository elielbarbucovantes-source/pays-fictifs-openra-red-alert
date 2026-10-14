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

namespace OpenRA.Mods.Fictifs
{
	// PAYS FICTIFS — fonctions du moteur de Combined Arms (plus récent que le
	// nôtre) dont les modules d'IA portés de CA ont besoin.
	public static class BotCompat
	{
		public static int GetCashAndResources(this PlayerResources pr)
		{
			return pr.Cash + pr.Resources;
		}

		public static MiniYamlNode NodeWithKeyOrDefault(this MiniYaml yaml, string key)
		{
			return yaml.Nodes.FirstOrDefault(n => n.Key == key);
		}

		// Notre moteur ne publie pas le chercheur « bloqué par l'immobile » :
		// on se contente du test de terrain seul.
		public static bool PathMightExistForLocomotorBlockedByImmovable(this IPathFinder pf, Locomotor locomotor, CPos source, CPos target)
		{
			return pf.PathExistsForLocomotor(locomotor, source, target);
		}

		public static Actor ClosestToIgnoringPath(this IEnumerable<Actor> actors, Actor actor)
		{
			return actors.ClosestToIgnoringPath(actor.CenterPosition);
		}

		public static Actor ClosestToIgnoringPath(this IEnumerable<Actor> actors, WPos position)
		{
			return actors.MinByOrDefault(a => (a.CenterPosition - position).LengthSquared);
		}

		public static T ClosestToIgnoringPath<T>(IEnumerable<T> items, Func<T, Actor> selector, Actor actor)
		{
			return ClosestToIgnoringPath(items, selector, actor.CenterPosition);
		}

		public static T ClosestToIgnoringPath<T>(IEnumerable<T> items, Func<T, Actor> selector, WPos position)
		{
			return items.MinByOrDefault(x => (selector(x).CenterPosition - position).LengthSquared);
		}

		public static int MultiplyBySqrtTwoOverTwo(int number)
		{
			return (int)(number * 23170L / 32768L);
		}

		public static IEnumerable<(Actor Actor, WVec[] ReachableOffsets)> WithPathFrom(
			this IEnumerable<Actor> actors, Actor sourceActor, Func<Actor, WVec[]> targetOffsets)
		{
			if (sourceActor.Info.HasTraitInfo<AircraftInfo>())
				return actors.Select(a => (a, targetOffsets(a)));

			var mobile = sourceActor.TraitOrDefault<Mobile>();
			if (mobile == null)
				return Enumerable.Empty<(Actor, WVec[])>();

			var pathFinder = sourceActor.World.WorldActor.Trait<IPathFinder>();
			var map = sourceActor.World.Map;
			return actors
				.Select(a => (a, targetOffsets(a).Where(offset =>
					pathFinder.PathExistsForLocomotor(
						mobile.Locomotor,
						map.CellContaining(sourceActor.CenterPosition),
						map.CellContaining(a.CenterPosition + offset)))
					.ToArray()))
				.Where(x => x.Item2.Length > 0);
		}

		public static IEnumerable<Actor> WithPathFrom(this IEnumerable<Actor> actors, Actor sourceActor)
		{
			if (sourceActor.Info.HasTraitInfo<AircraftInfo>())
				return actors;

			var mobile = sourceActor.TraitOrDefault<Mobile>();
			if (mobile == null)
				return Enumerable.Empty<Actor>();

			var pathFinder = sourceActor.World.WorldActor.Trait<IPathFinder>();
			var map = sourceActor.World.Map;
			return actors.Where(a => pathFinder.PathExistsForLocomotor(
				mobile.Locomotor,
				map.CellContaining(sourceActor.CenterPosition),
				map.CellContaining(a.CenterPosition)));
		}

		public static IEnumerable<Actor> WithPathTo(this IEnumerable<Actor> actors, World world, WPos targetPosition)
		{
			var pathFinder = world.WorldActor.Trait<IPathFinder>();
			var map = world.Map;
			return actors.Where(a =>
			{
				if (a.Info.HasTraitInfo<AircraftInfo>())
					return true;

				var mobile = a.TraitOrDefault<Mobile>();
				if (mobile == null)
					return false;

				return pathFinder.PathExistsForLocomotor(
					mobile.Locomotor,
					map.CellContaining(targetPosition),
					map.CellContaining(a.CenterPosition));
			});
		}

		public static IEnumerable<Actor> WithPathTo(this IEnumerable<Actor> actors, Actor targetActor)
		{
			return actors.WithPathTo(targetActor.World, targetActor.CenterPosition);
		}

		public static IEnumerable<(Actor Actor, WPos[] ReachablePositions)> WithPathToAny(
			this IEnumerable<Actor> actors, World world, Func<Actor, WPos[]> targetPositions)
		{
			var pathFinder = world.WorldActor.Trait<IPathFinder>();
			var map = world.Map;
			return actors
				.Select(a =>
				{
					if (a.Info.HasTraitInfo<AircraftInfo>())
						return (a, targetPositions(a).ToArray());

					var mobile = a.TraitOrDefault<Mobile>();
					if (mobile == null)
						return (a, Array.Empty<WPos>());

					return (a, targetPositions(a).Where(targetPosition =>
						pathFinder.PathExistsForLocomotor(
							mobile.Locomotor,
							map.CellContaining(targetPosition),
							map.CellContaining(a.CenterPosition)))
						.ToArray());
				})
				.Where(x => x.Item2.Length > 0);
		}

		public static Actor ClosestToWithPathToAny(
			this IEnumerable<Actor> actors, World world, Func<Actor, WPos[]> targetPositions)
		{
			return actors
				.WithPathToAny(world, targetPositions)
				.MinByOrDefault(x => x.ReachablePositions.Min(pos => (x.Actor.CenterPosition - pos).LengthSquared))
				.Actor;
		}
	}
}
