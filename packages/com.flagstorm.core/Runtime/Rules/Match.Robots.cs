using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Rules
{
    // Robots: desembarcan en la costa rival y son lo único que puede capturar una bandera.
    public sealed partial class Match
    {
        private int _nextRobotId = 1;

        private CommandResult ApplyDeploy(Side side, DeployRobotCommand command)
        {
            var enemy = _sides[side.Opponent()];
            var own = _sides[side];
            if (!enemy.Island.Contains(command.LandingCell))
            {
                return CommandResult.Rejected(RejectionReason.OutOfIsland);
            }

            if (!enemy.Island.IsCoast(command.LandingCell))
            {
                return CommandResult.Rejected(RejectionReason.NotCoast);
            }

            if (own.Robots.Any(r => r.Position == command.LandingCell))
            {
                return CommandResult.Rejected(RejectionReason.CellOccupied);
            }

            var reason = CheckWeapon(side, command.WeaponId, WeaponCategory.Capture, out var robot);
            if (reason != RejectionReason.None)
            {
                return CommandResult.Rejected(reason);
            }

            own.Energy -= robot.EnergyCost;
            var state = new RobotState(
                _nextRobotId++, robot.Id, command.LandingCell, robot.HitPoints, robot.MovementPoints);
            own.Robots.Add(state);
            var events = new List<IMatchEvent>
            {
                new RobotDeployed(side, state.RobotId, robot.Id, command.LandingCell),
                new EnergyChanged(side, own.Energy),
            };

            // Desembarcar justo sobre una bandera o un señuelo cuenta como llegar a esa casilla.
            ResolveArrival(side, own, enemy, command.LandingCell, events);
            return CommandResult.Accepted(events);
        }

        private CommandResult ApplyMove(Side side, MoveRobotCommand command)
        {
            var own = _sides[side];
            var enemy = _sides[side.Opponent()];
            var robot = own.Robots.FirstOrDefault(r => r.RobotId == command.RobotId);
            if (robot == null)
            {
                return CommandResult.Rejected(RejectionReason.UnknownRobot);
            }

            var definition = _arsenal[robot.WeaponId];
            if (!IsWalkable(enemy.Island, robot.Position, command.Path)
                || PathFinder.PathCost(enemy.Island, definition, command.Path) > robot.MovementPointsLeft)
            {
                return CommandResult.Rejected(RejectionReason.InvalidPath);
            }

            var events = new List<IMatchEvent>();
            var walked = new List<HexCoord>();
            foreach (var cell in command.Path)
            {
                robot.MovementPointsLeft -= definition.MovementCostOn(enemy.Island.TerrainAt(cell));
                robot.Position = cell;
                walked.Add(cell);
                if (ResolveArrival(side, own, enemy, cell, events))
                {
                    break;
                }
            }

            events.Insert(0, new RobotMoved(side, robot.RobotId, walked));
            return CommandResult.Accepted(events);
        }

        // Devuelve true si la partida terminó: el resto de la ruta ya no importa.
        private bool ResolveArrival(Side side, SideState own, SideState enemy, HexCoord cell, List<IMatchEvent> events)
        {
            if (enemy.Flags.Remove(cell))
            {
                own.FlagsCaptured++;
                events.Add(new FlagCaptured(side, cell, own.FlagsCaptured));
                if (own.FlagsCaptured >= Rules.FlagsPerSide)
                {
                    Phase = MatchPhase.Finished;
                    Winner = side;
                    events.Add(new MatchWon(side));
                    return true;
                }
            }
            else if (enemy.Defenses.TryGetValue(cell, out var defense) && defense.WeaponId == PrototypeArsenal.Decoy)
            {
                enemy.Defenses.Remove(cell);
                events.Add(new DecoyExposed(side, cell));
            }

            return false;
        }

        private static bool IsWalkable(IslandMap island, HexCoord start, IReadOnlyList<HexCoord> path)
        {
            if (path == null || path.Count == 0)
            {
                return false;
            }

            var previous = start;
            foreach (var cell in path)
            {
                if (!island.Contains(cell) || previous.DistanceTo(cell) != 1)
                {
                    return false;
                }

                previous = cell;
            }

            return true;
        }

        partial void OnTurnStarting(Side side)
        {
            foreach (var robot in _sides[side].Robots)
            {
                robot.MovementPointsLeft = _arsenal[robot.WeaponId].MovementPoints;
            }
        }
    }
}
