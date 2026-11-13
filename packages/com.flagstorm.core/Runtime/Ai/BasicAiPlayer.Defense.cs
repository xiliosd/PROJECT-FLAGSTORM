using System;
using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;

namespace Flagstorm.Core.Ai
{
    // Construcción de defensas en la isla propia.
    public sealed partial class BasicAiPlayer
    {
        private const int MaxTurrets = 4;
        private const int MaxDecoys = 2;
        private const int MaxShields = 3;
        private const int MaxGuards = 3;
        private const int MaxAntiMissiles = 2;

        // Orden de prioridad: frenar invasores, tapar las banderas de los drones, poner defensores,
        // proteger las torretas de los misiles, reforzar las banderas y, por último, señuelos.
        private IMatchCommand TryDefend(
            PlayerView view, IReadOnlyList<WeaponDefinition> available, IReadOnlyList<WeaponDefinition> arsenal)
        {
            var occupied = new HashSet<HexCoord>(view.OwnFlags.Concat(view.OwnDefenses.Keys)
                .Concat(view.OwnGuards.Select(g => g.Position))
                .Concat(view.EnemyRobotsOnOwnIsland.Select(r => r.Position)));
            var free = view.OwnIsland.Cells.Keys
                .Where(c => !occupied.Contains(c))
                .OrderBy(c => c, CellOrder.Instance)
                .ToList();

            return BuildTurretAgainstInvaders(view, available, free)
                ?? BuildShield(view, available, arsenal, free)
                ?? BuildGuard(view, available, free)
                ?? BuildAntiMissile(view, available, arsenal, free)
                ?? BuildTurretNearFlags(view, available, free)
                ?? BuildDecoy(view, available, free);
        }

        private IMatchCommand BuildTurretAgainstInvaders(
            PlayerView view, IReadOnlyList<WeaponDefinition> available, List<HexCoord> free)
        {
            var turret = Affordable(view, available, w => w.Id == PrototypeArsenal.Turret);
            if (turret == null || CountOwn(view, PrototypeArsenal.Turret) >= MaxTurrets)
            {
                return null;
            }

            var near = free
                .Where(c => view.EnemyRobotsOnOwnIsland.Any(r => r.Position.DistanceTo(c) <= 1))
                .ToList();
            return Build(turret, near);
        }

        // Un escudo cubre las banderas a su alcance: se construye junto a la primera bandera sin cobertura.
        private IMatchCommand BuildShield(
            PlayerView view,
            IReadOnlyList<WeaponDefinition> available,
            IReadOnlyList<WeaponDefinition> arsenal,
            List<HexCoord> free)
        {
            var shield = Affordable(view, available, w => w.Intercepts == WeaponCategory.Reveal);
            if (shield == null || CountIntercepting(view, arsenal, WeaponCategory.Reveal) >= MaxShields)
            {
                return null;
            }

            var uncovered = view.OwnFlags
                .Where(flag => !IsCovered(view, arsenal, WeaponCategory.Reveal, flag))
                .Select(flag => (HexCoord?)flag)
                .FirstOrDefault();
            if (uncovered == null)
            {
                return null;
            }

            var cells = free.Where(c => c.DistanceTo(uncovered.Value) <= shield.Radius);
            return Build(shield, PreferInterior(view, cells));
        }

        // El defensor con más poder en el terreno de la celda elegida, junto a una bandera.
        private IMatchCommand BuildGuard(
            PlayerView view, IReadOnlyList<WeaponDefinition> available, List<HexCoord> free)
        {
            var guards = available
                .Where(w => w.Category == WeaponCategory.Guard && w.EnergyCost <= view.Energy)
                .ToList();
            if (guards.Count == 0 || view.OwnGuards.Count >= MaxGuards)
            {
                return null;
            }

            var cells = PreferInterior(view, free.Where(c => view.OwnFlags.Any(f => f.DistanceTo(c) == 1)));
            if (cells.Count == 0)
            {
                return null;
            }

            var cell = cells[_random.Next(0, cells.Count)];
            var terrain = view.OwnIsland.TerrainAt(cell);
            var guard = guards.OrderByDescending(w => w.EffectivePowerOn(terrain)).First();
            return Propose(BuildKey(cell), new BuildDefenseCommand(guard.Id, cell));
        }

        // Cada torreta propia sin antimisiles cerca recibe uno, porque son lo primero que el rival apunta.
        private IMatchCommand BuildAntiMissile(
            PlayerView view,
            IReadOnlyList<WeaponDefinition> available,
            IReadOnlyList<WeaponDefinition> arsenal,
            List<HexCoord> free)
        {
            var antiMissile = Affordable(view, available, w => w.Intercepts == WeaponCategory.Destroy);
            if (antiMissile == null || CountIntercepting(view, arsenal, WeaponCategory.Destroy) >= MaxAntiMissiles)
            {
                return null;
            }

            var exposed = view.OwnDefenses
                .Where(d => d.Value.WeaponId == PrototypeArsenal.Turret)
                .Select(d => (HexCoord?)d.Key)
                .FirstOrDefault(turret => !IsCovered(view, arsenal, WeaponCategory.Destroy, turret.Value));
            if (exposed == null)
            {
                return null;
            }

            return Build(antiMissile, free.Where(c => c.DistanceTo(exposed.Value) <= antiMissile.Radius).ToList());
        }

        private IMatchCommand BuildTurretNearFlags(
            PlayerView view, IReadOnlyList<WeaponDefinition> available, List<HexCoord> free)
        {
            var turret = Affordable(view, available, w => w.Id == PrototypeArsenal.Turret);
            if (turret == null
                || CountOwn(view, PrototypeArsenal.Turret) >= MaxTurrets
                || view.EnemyRobotsOnOwnIsland.Count > 0)
            {
                return null;
            }

            return Build(turret, free.Where(c => view.OwnFlags.Any(f => f.DistanceTo(c) == 1)).ToList());
        }

        private IMatchCommand BuildDecoy(
            PlayerView view, IReadOnlyList<WeaponDefinition> available, List<HexCoord> free)
        {
            var decoy = Affordable(view, available, w => w.Id == PrototypeArsenal.Decoy);
            if (decoy == null || CountOwn(view, PrototypeArsenal.Decoy) >= MaxDecoys)
            {
                return null;
            }

            return Build(decoy, free.Where(view.OwnIsland.InteriorCells.Contains).ToList());
        }

        private static WeaponDefinition Affordable(
            PlayerView view, IReadOnlyList<WeaponDefinition> available, Func<WeaponDefinition, bool> match) =>
            available.FirstOrDefault(w => match(w) && w.EnergyCost <= view.Energy);

        private IMatchCommand Build(WeaponDefinition weapon, IReadOnlyList<HexCoord> cells)
        {
            if (cells.Count == 0)
            {
                return null;
            }

            var cell = cells[_random.Next(0, cells.Count)];
            return Propose(BuildKey(cell), new BuildDefenseCommand(weapon.Id, cell));
        }

        private static string BuildKey(HexCoord cell) => $"build:{cell.Q},{cell.R}";

        private static IReadOnlyList<HexCoord> PreferInterior(PlayerView view, IEnumerable<HexCoord> cells)
        {
            var all = cells.ToList();
            var interior = all.Where(view.OwnIsland.InteriorCells.Contains).ToList();
            return interior.Count > 0 ? interior : all;
        }

        private static int CountOwn(PlayerView view, string weaponId) =>
            view.OwnDefenses.Values.Count(d => d.WeaponId == weaponId);

        private static int CountIntercepting(
            PlayerView view, IReadOnlyList<WeaponDefinition> arsenal, WeaponCategory category) =>
            view.OwnDefenses.Values.Count(d => arsenal.Any(w => w.Id == d.WeaponId && w.Intercepts == category));

        // Una celda está cubierta si alguna defensa propia que intercepta esa categoría la alcanza.
        private static bool IsCovered(
            PlayerView view, IReadOnlyList<WeaponDefinition> arsenal, WeaponCategory category, HexCoord cell) =>
            view.OwnDefenses.Any(defense => arsenal.Any(w => w.Id == defense.Value.WeaponId
                && w.Intercepts == category
                && defense.Key.DistanceTo(cell) <= w.Radius));
    }
}
