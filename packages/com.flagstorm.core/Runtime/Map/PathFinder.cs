using System;
using System.Collections.Generic;
using Flagstorm.Core.Arsenal;

namespace Flagstorm.Core.Map
{
    public static class PathFinder
    {
        // Dijkstra con desempate determinista: a igual costo gana la celda descubierta primero.
        public static IReadOnlyList<HexCoord> FindPath(
            IslandMap map, Func<TerrainType, int> movementCost, HexCoord from, HexCoord to) =>
            FindPath(map, movementCost, from, to, null);

        // Las celdas bloqueadas (p. ej. defensores rivales conocidos) no se pueden pisar, ni siquiera la meta.
        public static IReadOnlyList<HexCoord> FindPath(
            IslandMap map, Func<TerrainType, int> movementCost, HexCoord from, HexCoord to, ISet<HexCoord> blocked)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (movementCost == null)
            {
                throw new ArgumentNullException(nameof(movementCost));
            }

            if (!map.Contains(from) || !map.Contains(to) || (blocked != null && blocked.Contains(to)))
            {
                return null;
            }

            var best = new Dictionary<HexCoord, int> { [from] = 0 };
            var previous = new Dictionary<HexCoord, HexCoord>();
            var frontier = new SortedSet<FrontierEntry>(FrontierComparer.Instance);
            var order = 0;
            frontier.Add(new FrontierEntry(0, order++, from));

            while (frontier.Count > 0)
            {
                var current = frontier.Min;
                frontier.Remove(current);
                if (current.Cell == to)
                {
                    return Rebuild(previous, from, to);
                }

                if (current.Cost > best[current.Cell])
                {
                    continue;
                }

                foreach (var next in current.Cell.Neighbors())
                {
                    if (!map.Contains(next) || (blocked != null && blocked.Contains(next)))
                    {
                        continue;
                    }

                    var cost = current.Cost + movementCost(map.TerrainAt(next));
                    if (best.TryGetValue(next, out var known) && known <= cost)
                    {
                        continue;
                    }

                    best[next] = cost;
                    previous[next] = current.Cell;
                    frontier.Add(new FrontierEntry(cost, order++, next));
                }
            }

            return null;
        }

        public static int PathCost(
            IslandMap map, Func<TerrainType, int> movementCost, IReadOnlyList<HexCoord> path)
        {
            var total = 0;
            foreach (var cell in path)
            {
                total += movementCost(map.TerrainAt(cell));
            }

            return total;
        }

        public static IReadOnlyList<HexCoord> TruncateToBudget(
            IslandMap map, Func<TerrainType, int> movementCost, IReadOnlyList<HexCoord> path, int budget)
        {
            var result = new List<HexCoord>();
            var spent = 0;
            foreach (var cell in path)
            {
                spent += movementCost(map.TerrainAt(cell));
                if (spent > budget)
                {
                    break;
                }

                result.Add(cell);
            }

            return result;
        }

        public static IReadOnlyList<HexCoord> FindPath(
            IslandMap map, WeaponDefinition robot, HexCoord from, HexCoord to) =>
            FindPath(map, CostOf(robot), from, to);

        public static IReadOnlyList<HexCoord> FindPath(
            IslandMap map, WeaponDefinition robot, HexCoord from, HexCoord to, ISet<HexCoord> blocked) =>
            FindPath(map, CostOf(robot), from, to, blocked);

        public static int PathCost(IslandMap map, WeaponDefinition robot, IReadOnlyList<HexCoord> path) =>
            PathCost(map, CostOf(robot), path);

        public static IReadOnlyList<HexCoord> TruncateToBudget(
            IslandMap map, WeaponDefinition robot, IReadOnlyList<HexCoord> path, int budget) =>
            TruncateToBudget(map, CostOf(robot), path, budget);

        private static Func<TerrainType, int> CostOf(WeaponDefinition robot)
        {
            if (robot == null)
            {
                throw new ArgumentNullException(nameof(robot));
            }

            return robot.MovementCostOn;
        }

        private static IReadOnlyList<HexCoord> Rebuild(
            Dictionary<HexCoord, HexCoord> previous, HexCoord from, HexCoord to)
        {
            var path = new List<HexCoord>();
            for (var cell = to; cell != from; cell = previous[cell])
            {
                path.Add(cell);
            }

            path.Reverse();
            return path;
        }

        private readonly struct FrontierEntry
        {
            public FrontierEntry(int cost, int order, HexCoord cell)
            {
                Cost = cost;
                Order = order;
                Cell = cell;
            }

            public int Cost { get; }

            public int Order { get; }

            public HexCoord Cell { get; }
        }

        private sealed class FrontierComparer : IComparer<FrontierEntry>
        {
            public static readonly FrontierComparer Instance = new FrontierComparer();

            public int Compare(FrontierEntry x, FrontierEntry y)
            {
                var byCost = x.Cost.CompareTo(y.Cost);
                return byCost != 0 ? byCost : x.Order.CompareTo(y.Order);
            }
        }
    }
}
