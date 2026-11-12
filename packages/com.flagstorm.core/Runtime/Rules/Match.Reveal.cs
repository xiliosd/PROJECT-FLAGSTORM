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
            var shield = FindInterceptor(enemy, command.Target, drone.Category);
            if (shield != null)
            {
                // El escudo derriba el dron antes de que revele nada, pero delata su propia celda.
                own.RevealedEnemyCells.Add(shield.Value.Key);
                return CommandResult.Accepted(new IMatchEvent[]
                {
                    new DroneShotDown(side, command.Target, shield.Value.Key),
                    new EnergyChanged(side, own.Energy),
                });
            }

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
            var antiMissile = FindInterceptor(enemy, command.Target, missile.Category);
            if (antiMissile != null)
            {
                return CommandResult.Accepted(
                    InterceptMissile(side, enemySide, missile, command.Target, antiMissile.Value));
            }

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

            // Los defensores en el área también reciben el daño; los robots del atacante nunca.
            var guardsInArea = enemy.Guards.Where(g => cells.Contains(g.Position)).OrderBy(g => g.RobotId).ToList();
            foreach (var guard in guardsInArea)
            {
                guard.HitPoints -= missile.EffectivePowerOn(enemy.Island.TerrainAt(guard.Position));
                if (guard.HitPoints <= 0)
                {
                    enemy.Guards.Remove(guard);
                    events.Add(new GuardDestroyed(enemySide, guard.RobotId, guard.Position));
                }
                else
                {
                    events.Add(new GuardDamaged(enemySide, guard.RobotId, guard.HitPoints));
                }
            }

            events.Add(new EnergyChanged(side, own.Energy));
            return CommandResult.Accepted(events);
        }

        private List<IMatchEvent> InterceptMissile(
            Side side,
            Side enemySide,
            WeaponDefinition missile,
            HexCoord target,
            KeyValuePair<HexCoord, DefenseState> antiMissile)
        {
            var own = _sides[side];
            var defense = antiMissile.Value;
            defense.HitPoints -= 1;
            own.RevealedEnemyCells.Add(antiMissile.Key);
            var events = new List<IMatchEvent>
            {
                new MissileIntercepted(side, missile.Id, target, antiMissile.Key, defense.HitPoints),
            };
            if (defense.HitPoints <= 0)
            {
                _sides[enemySide].Defenses.Remove(antiMissile.Key);
                events.Add(new DefenseDestroyed(enemySide, antiMissile.Key, defense.WeaponId));
            }

            events.Add(new EnergyChanged(side, own.Energy));
            return events;
        }

        // Gana la defensa más cercana al objetivo y, a igual distancia, la primera en CellOrder.
        private KeyValuePair<HexCoord, DefenseState>? FindInterceptor(
            SideState enemy, HexCoord target, WeaponCategory incoming)
        {
            KeyValuePair<HexCoord, DefenseState>? best = null;
            foreach (var defense in enemy.Defenses.OrderBy(d => d.Key, CellOrder.Instance))
            {
                var weapon = _arsenal[defense.Value.WeaponId];
                var distance = defense.Key.DistanceTo(target);
                if (weapon.Intercepts != incoming || distance > weapon.Radius)
                {
                    continue;
                }

                if (best == null || distance < best.Value.Key.DistanceTo(target))
                {
                    best = defense;
                }
            }

            return best;
        }

        private static IReadOnlyList<HexCoord> LandWithin(IslandMap island, HexCoord center, int radius) =>
            HexCoord.WithinRadius(center, radius).Where(island.Contains).ToList();
    }
}
