using System;
using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;

namespace Flagstorm.Core.Ai
{
    // IA del prototipo. Solo recibe PlayerView, nunca Match: no puede ver lo oculto (spec §5.2).
    // Pide un comando a la vez para decidir siempre con la vista actualizada.
    public sealed class BasicAiPlayer
    {
        private const int MaxRobots = 3;
        private const int MaxTurrets = 4;
        private const int MaxDecoys = 2;

        private readonly IRandomSource _random;

        // Celdas que pisaron nuestros robots: los robots no revelan, así que la IA recuerda dónde ya buscó.
        private readonly HashSet<HexCoord> _visited = new HashSet<HexCoord>();

        public BasicAiPlayer(IRandomSource random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public SetupCommand ChooseSetup(PlayerView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            var rules = view.Rules;
            var candidates = view.OwnIsland.InteriorCells.Count >= rules.FlagsPerSide
                ? view.OwnIsland.InteriorCells.ToList()
                : view.OwnIsland.Cells.Keys.OrderBy(c => c, CellOrder.Instance).ToList();

            var flags = new List<HexCoord>();
            while (flags.Count < rules.FlagsPerSide)
            {
                // Preferir banderas separadas para que un solo dron no las revele todas.
                var spread = candidates.Where(c => flags.All(f => f.DistanceTo(c) >= 2)).ToList();
                var pool = spread.Count > 0 ? spread : candidates;
                var pick = pool[_random.Next(0, pool.Count)];
                flags.Add(pick);
                candidates.Remove(pick);
            }

            var defenseCells = candidates
                .Where(c => flags.Any(f => f.DistanceTo(c) == 1))
                .ToList();
            var defenses = new List<DefensePlacement>();
            foreach (var weaponId in FreeDefenseWeaponIds(view.Loadout, rules.InitialFreeDefenses))
            {
                if (defenseCells.Count == 0)
                {
                    break;
                }

                var pick = defenseCells[_random.Next(0, defenseCells.Count)];
                defenses.Add(new DefensePlacement(weaponId, pick));
                defenseCells.Remove(pick);
            }

            return new SetupCommand(flags, defenses);
        }

        public IMatchCommand NextCommand(PlayerView view, IReadOnlyList<WeaponDefinition> arsenal)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (arsenal == null)
            {
                throw new ArgumentNullException(nameof(arsenal));
            }

            if (view.Phase != MatchPhase.Playing || view.CurrentSide != view.Viewer)
            {
                return new EndTurnCommand();
            }

            return TryMoveRobot(view, arsenal)
                ?? TryDeploy(view, arsenal)
                ?? TryMissile(view, arsenal)
                ?? TryDrone(view, arsenal)
                ?? TryDefend(view, arsenal)
                ?? (IMatchCommand)new EndTurnCommand();
        }

        // Torretas como siempre si el arsenal las trae; si no, las primeras defensas del arsenal por orden,
        // para que la IA siga siendo determinista.
        private static IReadOnlyList<string> FreeDefenseWeaponIds(IReadOnlyList<string> loadout, int count)
        {
            var ids = loadout ?? new string[0];
            if (ids.Contains(PrototypeArsenal.Turret))
            {
                return Enumerable.Repeat(PrototypeArsenal.Turret, count).ToList();
            }

            return ids
                .Where(id => PrototypeArsenal.All.Any(w => w.Id == id && w.Category == WeaponCategory.Defend))
                .Take(count)
                .ToList();
        }

        private IMatchCommand TryMoveRobot(PlayerView view, IReadOnlyList<WeaponDefinition> arsenal)
        {
            foreach (var robot in view.OwnRobots.Where(r => r.MovementPointsLeft > 0))
            {
                _visited.Add(robot.Position);
                var definition = arsenal.First(w => w.Id == robot.WeaponId);
                var targets = view.EnemySignals.Count > 0
                    ? view.EnemySignals
                    : Unexplored(view).ToList();
                var path = CheapestPath(view.EnemyIsland, definition, robot.Position, targets);
                if (path == null)
                {
                    continue;
                }

                var step = PathFinder.TruncateToBudget(view.EnemyIsland, definition, path, robot.MovementPointsLeft);
                if (step.Count == 0)
                {
                    continue;
                }

                foreach (var cell in step)
                {
                    _visited.Add(cell);
                }

                return new MoveRobotCommand(robot.RobotId, step);
            }

            return null;
        }

        private IMatchCommand TryMissile(PlayerView view, IReadOnlyList<WeaponDefinition> arsenal)
        {
            var threats = view.KnownEnemyTurrets.Keys
                .Where(t => view.OwnRobots.Any(r => r.Position.DistanceTo(t) <= 2)
                    || view.EnemySignals.Any(s => s.DistanceTo(t) <= 1))
                .OrderBy(t => t, CellOrder.Instance)
                .ToList();
            foreach (var turret in threats)
            {
                var terrain = view.EnemyIsland.TerrainAt(turret);

                // Elegir el misil que mejor rinde en ese terreno: aquí se nota la mecánica de terrenos.
                var missile = arsenal
                    .Where(w => w.Category == WeaponCategory.Destroy && w.EnergyCost <= view.Energy)
                    .OrderByDescending(w => w.EffectivePowerOn(terrain))
                    .FirstOrDefault();
                if (missile != null)
                {
                    return new FireMissileCommand(missile.Id, turret);
                }
            }

            return null;
        }

        private IMatchCommand TryDeploy(PlayerView view, IReadOnlyList<WeaponDefinition> arsenal)
        {
            var hasWork = view.EnemySignals.Count > view.OwnRobots.Count
                || (view.OwnRobots.Count == 0 && Unrevealed(view).Count() * 3 < view.EnemyIsland.Cells.Count);
            if (!hasWork || view.OwnRobots.Count >= MaxRobots)
            {
                return null;
            }

            var targets = view.EnemySignals.Count > 0 ? view.EnemySignals : Unexplored(view).ToList();
            var occupied = new HashSet<HexCoord>(view.OwnRobots.Select(r => r.Position));
            DeployRobotCommand best = null;
            var bestCost = int.MaxValue;
            var robots = arsenal.Where(w => w.Category == WeaponCategory.Capture && w.EnergyCost <= view.Energy);
            foreach (var robot in robots)
            {
                foreach (var coast in view.EnemyIsland.CoastCells.Where(c => !occupied.Contains(c)))
                {
                    var path = CheapestPath(view.EnemyIsland, robot, coast, targets);
                    if (path == null)
                    {
                        continue;
                    }

                    var cost = PathFinder.PathCost(view.EnemyIsland, robot, path);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        best = new DeployRobotCommand(robot.Id, coast);
                    }
                }
            }

            return best;
        }

        private IMatchCommand TryDrone(PlayerView view, IReadOnlyList<WeaponDefinition> arsenal)
        {
            var remainingFlags = view.Rules.FlagsPerSide - view.FlagsCapturedByViewer;
            if (view.EnemySignals.Count >= remainingFlags + 1)
            {
                return null;
            }

            var drone = arsenal.FirstOrDefault(w => w.Category == WeaponCategory.Reveal && w.EnergyCost <= view.Energy);
            if (drone == null)
            {
                return null;
            }

            var revealed = new HashSet<HexCoord>(view.RevealedEnemyCells);
            var scored = view.EnemyIsland.Cells.Keys
                .OrderBy(c => c, CellOrder.Instance)
                .Select(c => (Cell: c, Gain: HexCoord.WithinRadius(c, drone.Radius)
                    .Count(n => view.EnemyIsland.Contains(n) && !revealed.Contains(n))))
                .Where(x => x.Gain > 0)
                .ToList();
            if (scored.Count == 0)
            {
                return null;
            }

            var bestGain = scored.Max(x => x.Gain);
            var best = scored.Where(x => x.Gain == bestGain).ToList();
            return new LaunchDroneCommand(drone.Id, best[_random.Next(0, best.Count)].Cell);
        }

        private IMatchCommand TryDefend(PlayerView view, IReadOnlyList<WeaponDefinition> arsenal)
        {
            var occupied = new HashSet<HexCoord>(view.OwnFlags.Concat(view.OwnDecoys).Concat(view.OwnTurrets.Keys));
            var free = view.OwnIsland.Cells.Keys
                .Where(c => !occupied.Contains(c))
                .OrderBy(c => c, CellOrder.Instance)
                .ToList();

            var turret = arsenal.FirstOrDefault(
                w => w.Id == PrototypeArsenal.Turret && w.EnergyCost <= view.Energy);
            if (turret != null && view.OwnTurrets.Count < MaxTurrets)
            {
                // Primero frenar robots invasores; si no hay, proteger banderas.
                var near = free
                    .Where(c => view.EnemyRobotsOnOwnIsland.Any(r => r.Position.DistanceTo(c) <= 1))
                    .ToList();
                if (near.Count == 0 && view.EnemyRobotsOnOwnIsland.Count == 0)
                {
                    near = free.Where(c => view.OwnFlags.Any(f => f.DistanceTo(c) == 1)).ToList();
                }

                if (near.Count > 0)
                {
                    return new BuildDefenseCommand(turret.Id, near[_random.Next(0, near.Count)]);
                }
            }

            var decoy = arsenal.FirstOrDefault(w => w.Id == PrototypeArsenal.Decoy && w.EnergyCost <= view.Energy);
            var interior = free.Where(view.OwnIsland.InteriorCells.Contains).ToList();
            if (decoy != null && view.OwnDecoys.Count < MaxDecoys && interior.Count > 0)
            {
                return new BuildDefenseCommand(decoy.Id, interior[_random.Next(0, interior.Count)]);
            }

            return null;
        }

        private IEnumerable<HexCoord> Unrevealed(PlayerView view)
        {
            var revealed = new HashSet<HexCoord>(view.RevealedEnemyCells);
            return view.EnemyIsland.Cells.Keys.Where(c => !revealed.Contains(c)).OrderBy(c => c, CellOrder.Instance);
        }

        // Celdas sin revelar ni pisar; primero el interior, porque las banderas suelen estar tierra adentro.
        private IEnumerable<HexCoord> Unexplored(PlayerView view)
        {
            var pending = Unrevealed(view).Where(c => !_visited.Contains(c)).ToList();
            var interior = pending.Where(c => !view.EnemyIsland.IsCoast(c)).ToList();
            return interior.Count > 0 ? interior : pending;
        }

        private static IReadOnlyList<HexCoord> CheapestPath(
            IslandMap island, WeaponDefinition robot, HexCoord from, IEnumerable<HexCoord> targets)
        {
            IReadOnlyList<HexCoord> best = null;
            var bestCost = int.MaxValue;
            foreach (var target in targets)
            {
                if (target == from)
                {
                    continue;
                }

                var path = PathFinder.FindPath(island, robot, from, target);
                if (path == null)
                {
                    continue;
                }

                var cost = PathFinder.PathCost(island, robot, path);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = path;
                }
            }

            return best;
        }
    }
}
