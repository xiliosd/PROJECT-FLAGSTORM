using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;

namespace Flagstorm.Core.Rules
{
    // Defensores: robots que cuidan la isla propia; los rivales los ven solo si están revelados o cerca.
    public sealed partial class Match
    {
        private bool IsGuardWeapon(string weaponId) =>
            weaponId != null
            && _arsenal.TryGetValue(weaponId, out var weapon)
            && weapon.Category == WeaponCategory.Guard;

        private CommandResult ApplyPlaceGuard(Side side, BuildDefenseCommand command)
        {
            var own = _sides[side];
            if (!own.Island.Contains(command.Cell))
            {
                return CommandResult.Rejected(RejectionReason.OutOfIsland);
            }

            if (own.Flags.Contains(command.Cell) || own.Defenses.ContainsKey(command.Cell)
                || own.Guards.Any(g => g.Position == command.Cell))
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

        private static IReadOnlyList<GuardInfo> KnownGuardsOf(SideState viewer, SideState owner) =>
            owner.Guards
                .Where(g => viewer.RevealedEnemyCells.Contains(g.Position)
                    || viewer.Robots.Any(r => r.Position.DistanceTo(g.Position) <= 1))
                .OrderBy(g => g.RobotId)
                .Select(g => g.ToInfo())
                .ToList();
    }
}
