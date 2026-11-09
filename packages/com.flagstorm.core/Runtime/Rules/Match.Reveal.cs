using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Rules
{
    // Drones y misiles: siempre apuntan a la isla rival.
    public sealed partial class Match
    {
        private CommandResult ApplyDrone(Side side, LaunchDroneCommand command)
        {
            var enemy = _sides[side.Opponent()];
            if (!enemy.Island.Contains(command.Target))
            {
                return CommandResult.Rejected(RejectionReason.OutOfIsland);
            }

            var reason = CheckWeapon(side, command.WeaponId, WeaponCategory.Reveal, out var drone);
            if (reason != RejectionReason.None)
            {
                return CommandResult.Rejected(reason);
            }

            var own = _sides[side];
            own.Energy -= drone.EnergyCost;
            var cells = LandWithin(enemy.Island, command.Target, drone.Radius);
            own.RevealedEnemyCells.UnionWith(cells);
            return CommandResult.Accepted(new IMatchEvent[]
            {
                new CellsRevealed(side, cells),
                new EnergyChanged(side, own.Energy),
            });
        }

        private CommandResult ApplyMissile(Side side, FireMissileCommand command)
        {
            var enemySide = side.Opponent();
            var enemy = _sides[enemySide];
            if (!enemy.Island.Contains(command.Target))
            {
                return CommandResult.Rejected(RejectionReason.OutOfIsland);
            }

            var reason = CheckWeapon(side, command.WeaponId, WeaponCategory.Destroy, out var missile);
            if (reason != RejectionReason.None)
            {
                return CommandResult.Rejected(reason);
            }

            var own = _sides[side];
            own.Energy -= missile.EnergyCost;
            var cells = LandWithin(enemy.Island, command.Target, missile.Radius);
            var events = new List<IMatchEvent> { new MissileImpact(side, missile.Id, command.Target, cells) };

            // Las banderas son indestructibles y los señuelos también lo son para los misiles: si un misil
            // pudiera destruirlos, bastaría un disparo para distinguir señuelo de bandera.
            // Solo un robot los desenmascara.
            foreach (var cell in cells)
            {
                if (!enemy.Defenses.TryGetValue(cell, out var defense) || defense.WeaponId == PrototypeArsenal.Decoy)
                {
                    continue;
                }

                defense.HitPoints -= missile.EffectivePowerOn(enemy.Island.TerrainAt(cell));
                if (defense.HitPoints <= 0)
                {
                    enemy.Defenses.Remove(cell);
                    events.Add(new DefenseDestroyed(enemySide, cell, defense.WeaponId));
                }
                else
                {
                    events.Add(new DefenseDamaged(enemySide, cell, defense.WeaponId, defense.HitPoints));
                }
            }

            events.Add(new EnergyChanged(side, own.Energy));
            return CommandResult.Accepted(events);
        }

        private static IReadOnlyList<HexCoord> LandWithin(IslandMap island, HexCoord center, int radius) =>
            HexCoord.WithinRadius(center, radius).Where(island.Contains).ToList();
    }
}
