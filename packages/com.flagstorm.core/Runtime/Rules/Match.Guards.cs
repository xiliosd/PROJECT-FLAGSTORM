using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Rules
{
    // Defensores: robots que cuidan la isla propia; los rivales los ven solo si están revelados o cerca.
    public sealed partial class Match
    {
        private bool IsGuardWeapon(string weaponId) =>
            weaponId != null
            && _arsenal.TryGetValue(weaponId, out var weapon)
            && weapon.Category == WeaponCategory.Guard;

        // Dos unidades nunca comparten celda: bandera, defensa, defensor o robot de ataque rival la ocupan.
        private bool IsCellFreeForBuilding(Side side, HexCoord cell)
        {
            var own = _sides[side];
            return !own.Flags.Contains(cell)
                && !own.Defenses.ContainsKey(cell)
                && !own.Guards.Any(g => g.Position == cell)
                && !_sides[side.Opponent()].Robots.Any(r => r.Position == cell);
        }

        private CommandResult ApplyPlaceGuard(Side side, BuildDefenseCommand command)
        {
            var own = _sides[side];
            if (!own.Island.Contains(command.Cell))
            {
                return CommandResult.Rejected(RejectionReason.OutOfIsland);
            }

            if (!IsCellFreeForBuilding(side, command.Cell))
            {
                return CommandResult.Rejected(RejectionReason.CellOccupied);
            }

            var reason = CheckWeapon(side, command.WeaponId, WeaponCategory.Guard, out var weapon);
            if (reason != RejectionReason.None)
            {
                return CommandResult.Rejected(reason);
            }

            own.Energy -= weapon.EnergyCost;
            var guard = new GuardState(_nextRobotId++, weapon.Id, command.Cell, weapon.HitPoints);
            own.Guards.Add(guard);
            return CommandResult.Accepted(new IMatchEvent[]
            {
                new GuardPlaced(side, guard.RobotId, weapon.Id, command.Cell),
                new EnergyChanged(side, own.Energy),
            });
        }

        // Fase de defensa tras las torretas: combates, robots contra defensas fijas y avance de defensores.
        private void ResolveGuardPhase(Side attackerSide, List<IMatchEvent> events)
        {
            var attacker = _sides[attackerSide];
            var defender = _sides[attackerSide.Opponent()];
            var clashed = ResolveClashes(attackerSide, attacker, defender, events);
            ResolveAttacksOnFixedDefenses(attackerSide, attacker, defender, clashed.RobotIds, events);
            AdvanceGuards(attackerSide.Opponent(), attacker, defender, clashed.GuardIds, events);
        }

        // Cada defensor y cada robot eligen al rival adyacente de menor id; todos los golpes se calculan
        // antes de aplicar ninguno, para que el orden de resolución no cambie el resultado.
        private (HashSet<int> RobotIds, HashSet<int> GuardIds) ResolveClashes(
            Side attackerSide, SideState attacker, SideState defender, List<IMatchEvent> events)
        {
            var defenderSide = attackerSide.Opponent();
            var guards = defender.Guards.OrderBy(g => g.RobotId).ToList();
            var robots = attacker.Robots.OrderBy(r => r.RobotId).ToList();
            var damageToGuard = new Dictionary<(int RobotId, int GuardId), int>();
            var damageToRobot = new Dictionary<(int RobotId, int GuardId), int>();
            foreach (var guard in guards)
            {
                var target = robots.FirstOrDefault(r => r.Position.DistanceTo(guard.Position) <= 1);
                if (target != null)
                {
                    damageToRobot[(target.RobotId, guard.RobotId)] =
                        PowerOnIsland(defender, guard.WeaponId, guard.Position);
                }
            }

            foreach (var robot in robots)
            {
                var target = guards.FirstOrDefault(g => g.Position.DistanceTo(robot.Position) <= 1);
                if (target != null)
                {
                    damageToGuard[(robot.RobotId, target.RobotId)] =
                        PowerOnIsland(defender, robot.WeaponId, robot.Position);
                }
            }

            var pairs = damageToGuard.Keys.Union(damageToRobot.Keys)
                .OrderBy(pair => pair.RobotId)
                .ThenBy(pair => pair.GuardId)
                .ToList();
            var clashedRobots = new HashSet<int>();
            var clashedGuards = new HashSet<int>();
            foreach (var pair in pairs)
            {
                damageToGuard.TryGetValue(pair, out var toGuard);
                damageToRobot.TryGetValue(pair, out var toRobot);
                clashedRobots.Add(pair.RobotId);
                clashedGuards.Add(pair.GuardId);
                events.Add(new RobotsClashed(attackerSide, pair.RobotId, pair.GuardId, toGuard, toRobot));
            }

            foreach (var guard in guards)
            {
                var received = damageToGuard.Where(p => p.Key.GuardId == guard.RobotId).Sum(p => p.Value);
                if (received == 0)
                {
                    continue;
                }

                guard.HitPoints -= received;
                if (guard.HitPoints <= 0)
                {
                    defender.Guards.Remove(guard);
                    events.Add(new GuardDestroyed(defenderSide, guard.RobotId, guard.Position));
                }
                else
                {
                    events.Add(new GuardDamaged(defenderSide, guard.RobotId, guard.HitPoints));
                }
            }

            foreach (var robot in robots)
            {
                var received = damageToRobot.Where(p => p.Key.RobotId == robot.RobotId).Sum(p => p.Value);
                robot.HitPoints -= received;
                if (received > 0 && robot.HitPoints <= 0)
                {
                    attacker.Robots.Remove(robot);
                    events.Add(new RobotDestroyed(attackerSide, robot.RobotId, robot.Position));
                }
            }

            return (clashedRobots, clashedGuards);
        }

        // El señuelo no se golpea: solo se delata al pisarlo. Un robot que ya combatió no ataca defensas.
        private void ResolveAttacksOnFixedDefenses(
            Side attackerSide,
            SideState attacker,
            SideState defender,
            HashSet<int> clashedRobots,
            List<IMatchEvent> events)
        {
            var defenderSide = attackerSide.Opponent();
            var idle = attacker.Robots.Where(r => !clashedRobots.Contains(r.RobotId)).OrderBy(r => r.RobotId);
            foreach (var robot in idle.ToList())
            {
                var cells = defender.Defenses
                    .Where(d => d.Value.WeaponId != PrototypeArsenal.Decoy && d.Key.DistanceTo(robot.Position) <= 1)
                    .Select(d => d.Key)
                    .OrderBy(cell => cell, CellOrder.Instance)
                    .ToList();
                if (cells.Count == 0)
                {
                    continue;
                }

                var cell = cells[0];
                var defense = defender.Defenses[cell];
                var damage = PowerOnIsland(defender, robot.WeaponId, robot.Position);
                defense.HitPoints -= damage;
                events.Add(new RobotHitDefense(attackerSide, robot.RobotId, cell, damage));
                if (defense.HitPoints <= 0)
                {
                    defender.Defenses.Remove(cell);
                    events.Add(new DefenseDestroyed(defenderSide, cell, defense.WeaponId));
                }
                else
                {
                    events.Add(new DefenseDamaged(defenderSide, cell, defense.WeaponId, defense.HitPoints));
                }
            }
        }

        private int PowerOnIsland(SideState island, string weaponId, HexCoord cell) =>
            _arsenal[weaponId].EffectivePowerOn(island.Island.TerrainAt(cell));

        // Un defensor libre persigue al intruso más cercano dentro de su zona o vuelve a su puesto.
        private void AdvanceGuards(
            Side defenderSide,
            SideState attacker,
            SideState defender,
            HashSet<int> clashedGuards,
            List<IMatchEvent> events)
        {
            foreach (var guard in defender.Guards.Where(g => !clashedGuards.Contains(g.RobotId))
                .OrderBy(g => g.RobotId).ToList())
            {
                var radius = _arsenal[guard.WeaponId].Radius;
                var intruder = attacker.Robots
                    .Where(r => r.Position.DistanceTo(guard.Post) <= radius)
                    .OrderBy(r => r.Position.DistanceTo(guard.Position))
                    .ThenBy(r => r.RobotId)
                    .FirstOrDefault();
                var goal = intruder != null ? intruder.Position : guard.Post;
                var step = ChooseGuardStep(guard, goal, radius, attacker, defender);
                if (step != null)
                {
                    events.Add(new GuardMoved(defenderSide, guard.RobotId, guard.Position, step.Value));
                    guard.Position = step.Value;
                }
            }
        }

        // Paso voraz: el vecino libre dentro de la zona que más acerca a la meta; si ninguno acerca, se queda.
        private static HexCoord? ChooseGuardStep(
            GuardState guard, HexCoord goal, int radius, SideState attacker, SideState defender)
        {
            var best = guard.Position.DistanceTo(goal);
            HexCoord? chosen = null;
            foreach (var cell in guard.Position.Neighbors().OrderBy(c => c, CellOrder.Instance))
            {
                var distance = cell.DistanceTo(goal);
                if (distance < best
                    && defender.Island.Contains(cell)
                    && cell.DistanceTo(guard.Post) <= radius
                    && !defender.Guards.Any(g => g.Position == cell)
                    && !defender.Defenses.ContainsKey(cell)
                    && !attacker.Robots.Any(r => r.Position == cell))
                {
                    best = distance;
                    chosen = cell;
                }
            }

            return chosen;
        }

        private static IReadOnlyList<GuardInfo> KnownGuardsOf(SideState viewer, SideState owner) =>
            owner.Guards
                .Where(g => viewer.RevealedEnemyCells.Contains(g.Position)
                    || viewer.Robots.Any(r => r.Position.DistanceTo(g.Position) <= 1))
                .OrderBy(g => g.RobotId)
                .Select(g => g.ToInfo())
                .ToList();
    }
}
