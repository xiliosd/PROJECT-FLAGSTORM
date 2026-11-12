using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Rules
{
    // Defensas: torretas y señuelos en la propia isla.
    public sealed partial class Match
    {
        private CommandResult ApplyBuildDefense(Side side, BuildDefenseCommand command)
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

            var reason = CheckWeapon(side, command.WeaponId, WeaponCategory.Defend, out var defense);
            if (reason != RejectionReason.None)
            {
                return CommandResult.Rejected(reason);
            }

            own.Energy -= defense.EnergyCost;
            own.Defenses[command.Cell] = new DefenseState(defense.Id, defense.HitPoints);
            return CommandResult.Accepted(new IMatchEvent[]
            {
                new DefenseBuilt(side, command.Cell, defense.Id),
                new EnergyChanged(side, own.Energy),
            });
        }

        // Al cerrar su turno, los robots del atacante que quedaron al alcance reciben el fuego de las torretas.
        partial void OnTurnEnding(Side side, List<IMatchEvent> events)
        {
            var defenderSide = side.Opponent();
            var defender = _sides[defenderSide];
            var attacker = _sides[side];
            var turrets = defender.Defenses
                .Where(d => d.Value.WeaponId == PrototypeArsenal.Turret)
                .OrderBy(d => d.Key, CellOrder.Instance)
                .ToList();

            foreach (var turret in turrets)
            {
                var definition = _arsenal[turret.Value.WeaponId];
                var target = attacker.Robots
                    .Where(r => r.Position.DistanceTo(turret.Key) <= definition.Radius)
                    .OrderBy(r => r.Position.DistanceTo(turret.Key))
                    .ThenBy(r => r.RobotId)
                    .FirstOrDefault();
                if (target == null)
                {
                    continue;
                }

                target.HitPoints -= definition.Power;
                events.Add(new TurretFired(defenderSide, turret.Key, target.RobotId, definition.Power));
                if (target.HitPoints <= 0)
                {
                    attacker.Robots.Remove(target);
                    events.Add(new RobotDestroyed(side, target.RobotId, target.Position));
                }
            }
        }
    }
}
