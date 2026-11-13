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
    // Las partes parciales separan el arsenal (Loadout) y la construcción de defensas (Defense).
    public sealed partial class BasicAiPlayer
    {
        private const int MaxRobots = 3;

        // Un robot propio a esta distancia de un defensor o torreta rival ya está en su alcance.
        private const int ThreatDistance = 2;

        private readonly IRandomSource _random;

        // Celdas que pisaron nuestros robots: los robots no revelan, así que la IA recuerda dónde ya buscó.
        private readonly HashSet<HexCoord> _visited = new HashSet<HexCoord>();

        // Comandos propuestos en el turno en curso. La partida no avisa a la IA de los rechazos, pero un comando
        // válido nunca se repite igual en el mismo turno (cambia el estado): si reaparece es porque se rechazó.
        // Se descarta y se elige otra jugada para no quedarse atascada.
        private readonly HashSet<string> _proposed = new HashSet<string>();
        private int _proposedTurn = -1;

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

            if (view.TurnNumber != _proposedTurn)
            {
                _proposed.Clear();
                _proposedTurn = view.TurnNumber;
            }

            // Solo se usan armas del loadout: el catálogo completo sirve únicamente para conocer las armas rivales.
            var available = arsenal.Where(w => view.Loadout == null || view.Loadout.Contains(w.Id)).ToList();
            return TryMissile(view, available, arsenal, view.KnownEnemyGuards.Select(g => g.Position))
                ?? TryMoveRobot(view, arsenal)
                ?? TryDeploy(view, available)
                ?? TryMissile(view, available, arsenal, TurretCells(view))
                ?? TryDrone(view, available)
                ?? TryDefend(view, available, arsenal)
                ?? (IMatchCommand)new EndTurnCommand();
        }

        private bool IsFresh(string key) => !_proposed.Contains(key);

        private T Propose<T>(string key, T command)
            where T : IMatchCommand
        {
            _proposed.Add(key);
            return command;
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
                .Where(id => PrototypeArsenal.All.Any(
                    w => w.Id == id && (w.Category == WeaponCategory.Defend || w.Category == WeaponCategory.Guard)))
                .Take(count)
                .ToList();
        }

        // Avanza de a una casilla: los defensores a distancia 1 siempre son conocidos, así que una ruta de un
        // solo paso nunca se topa con un defensor oculto y es rechazada.
        private IMatchCommand TryMoveRobot(PlayerView view, IReadOnlyList<WeaponDefinition> arsenal)
        {
            var blocked = KnownGuardCells(view);
            foreach (var robot in view.OwnRobots.Where(r => r.MovementPointsLeft > 0))
            {
                _visited.Add(robot.Position);
                var definition = arsenal.First(w => w.Id == robot.WeaponId);
                var targets = view.EnemySignals.Count > 0
                    ? view.EnemySignals
                    : Unexplored(view).ToList();
                var path = CheapestPath(view.EnemyIsland, definition, robot.Position, targets, blocked);
                if (path == null)
                {
                    continue;
                }

                var step = PathFinder.TruncateToBudget(view.EnemyIsland, definition, path, robot.MovementPointsLeft);
                if (step.Count == 0)
                {
                    continue;
                }

                var key = $"move:{robot.RobotId}:{step[0].Q},{step[0].R}";
                if (!IsFresh(key))
                {
                    continue;
                }

                _visited.Add(step[0]);
                return Propose(key, new MoveRobotCommand(robot.RobotId, new[] { step[0] }));
            }

            return null;
        }

        private static HashSet<HexCoord> KnownGuardCells(PlayerView view) =>
            new HashSet<HexCoord>(view.KnownEnemyGuards.Select(g => g.Position));

        private static IEnumerable<HexCoord> TurretCells(PlayerView view) =>
            view.KnownEnemyDefenses.Where(d => d.Value.WeaponId == PrototypeArsenal.Turret).Select(d => d.Key);

        // Dispara a los objetivos rivales conocidos que estorban a un robot o a una señal propia, con el misil
        // que más daño hace en ese terreno. Los defensores bloquean el paso, así que se atacan antes de mover;
        // las torretas solo después de desembarcar, para no gastar en misiles la energía de los robots.
        private IMatchCommand TryMissile(
            PlayerView view,
            IReadOnlyList<WeaponDefinition> available,
            IReadOnlyList<WeaponDefinition> arsenal,
            IEnumerable<HexCoord> candidates)
        {
            var threats = candidates
                .Where(cell => IsObstacle(view, cell) && !IsCoveredByAntiMissile(view, arsenal, cell))
                .Distinct()
                .OrderBy(cell => cell, CellOrder.Instance);
            foreach (var target in threats)
            {
                var terrain = view.EnemyIsland.TerrainAt(target);

                // Elegir el misil que mejor rinde en ese terreno: aquí se nota la mecánica de terrenos.
                var missile = available
                    .Where(w => w.Category == WeaponCategory.Destroy && w.EnergyCost <= view.Energy)
                    .OrderByDescending(w => w.EffectivePowerOn(terrain))
                    .FirstOrDefault();
                if (missile != null)
                {
                    return new FireMissileCommand(missile.Id, target);
                }
            }

            return null;
        }

        private static bool IsObstacle(PlayerView view, HexCoord cell) =>
            view.OwnRobots.Any(r => r.Position.DistanceTo(cell) <= ThreatDistance)
            || view.EnemySignals.Any(s => s.DistanceTo(cell) <= 1);

        // Un misil dentro del radio de un antimisiles conocido solo desgasta a este: no vale la energía.
        private static bool IsCoveredByAntiMissile(
            PlayerView view, IReadOnlyList<WeaponDefinition> arsenal, HexCoord cell) =>
            view.KnownEnemyDefenses.Any(defense =>
            {
                var weapon = arsenal.FirstOrDefault(w => w.Id == defense.Value.WeaponId);
                return weapon != null
                    && weapon.Intercepts == WeaponCategory.Destroy
                    && defense.Key.DistanceTo(cell) <= weapon.Radius;
            });

        // El robot que desembarca es el de menor costo de ruta hacia el objetivo: así se elige por terreno.
        private IMatchCommand TryDeploy(PlayerView view, IReadOnlyList<WeaponDefinition> available)
        {
            var hasWork = view.EnemySignals.Count > view.OwnRobots.Count
                || (view.OwnRobots.Count == 0 && Unrevealed(view).Count() * 3 < view.EnemyIsland.Cells.Count);
            if (!hasWork || view.OwnRobots.Count >= MaxRobots)
            {
                return null;
            }

            var targets = view.EnemySignals.Count > 0 ? view.EnemySignals : Unexplored(view).ToList();
            var blocked = KnownGuardCells(view);
            var occupied = new HashSet<HexCoord>(view.OwnRobots.Select(r => r.Position).Concat(blocked));
            DeployRobotCommand best = null;
            var bestCost = int.MaxValue;
            var robots = available.Where(w => w.Category == WeaponCategory.Capture && w.EnergyCost <= view.Energy);
            foreach (var robot in robots)
            {
                foreach (var coast in view.EnemyIsland.CoastCells.Where(c => !occupied.Contains(c)))
                {
                    var path = CheapestPath(view.EnemyIsland, robot, coast, targets, blocked);
                    if (path == null || !IsFresh(DeployKey(robot.Id, coast)))
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

            return best == null ? null : Propose(DeployKey(best.WeaponId, best.LandingCell), best);
        }

        private static string DeployKey(string weaponId, HexCoord cell) => $"deploy:{weaponId}:{cell.Q},{cell.R}";

        private IMatchCommand TryDrone(PlayerView view, IReadOnlyList<WeaponDefinition> available)
        {
            var remainingFlags = view.Rules.FlagsPerSide - view.FlagsCapturedByViewer;
            if (view.EnemySignals.Count >= remainingFlags + 1)
            {
                return null;
            }

            var drone = available.FirstOrDefault(
                w => w.Category == WeaponCategory.Reveal && w.EnergyCost <= view.Energy);
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
            var target = best[_random.Next(0, best.Count)].Cell;
            return new LaunchDroneCommand(drone.Id, target);
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
            IslandMap island,
            WeaponDefinition robot,
            HexCoord from,
            IEnumerable<HexCoord> targets,
            ISet<HexCoord> blocked)
        {
            IReadOnlyList<HexCoord> best = null;
            var bestCost = int.MaxValue;
            foreach (var target in targets)
            {
                if (target == from)
                {
                    continue;
                }

                var path = PathFinder.FindPath(island, robot, from, target, blocked);
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
