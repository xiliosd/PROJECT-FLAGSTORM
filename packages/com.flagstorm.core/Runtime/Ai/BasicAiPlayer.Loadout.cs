using System;
using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Ai
{
    // Elección del arsenal de la partida.
    public sealed partial class BasicAiPlayer
    {
        // Elige hasta `slots` armas de las disponibles. Primero el robot del terreno más común de la isla rival
        // (se mueve barato y pega fuerte ahí), luego cada ataque con la defensa que lo intercepta, y al final
        // el resto por prioridad. Siempre lleva un robot de captura si hay alguno disponible, así que el
        // resultado cumple LoadoutRules.
        public IReadOnlyList<string> ChooseLoadout(
            IslandMap enemyIsland, IReadOnlyList<WeaponDefinition> available, int slots)
        {
            if (enemyIsland == null)
            {
                throw new ArgumentNullException(nameof(enemyIsland));
            }

            if (available == null)
            {
                throw new ArgumentNullException(nameof(available));
            }

            var terrainCounts = enemyIsland.Cells.Values
                .GroupBy(terrain => terrain)
                .ToDictionary(group => group.Key, group => group.Count());
            var robots = available
                .Where(w => w.Category == WeaponCategory.Capture)
                .OrderByDescending(w => HomeTerrainCount(terrainCounts, w))
                .ToList();

            var order = new List<WeaponDefinition>();
            void Add(WeaponDefinition weapon)
            {
                if (weapon != null && !order.Contains(weapon))
                {
                    order.Add(weapon);
                }
            }

            Add(robots.FirstOrDefault());
            foreach (var attackCategory in new[] { WeaponCategory.Reveal, WeaponCategory.Destroy })
            {
                var attack = available.FirstOrDefault(w => w.Category == attackCategory);
                if (attack != null)
                {
                    Add(attack);
                    Add(available.FirstOrDefault(w => w.Intercepts == attackCategory));
                }
            }

            Add(available.FirstOrDefault(w => w.Id == PrototypeArsenal.Turret));
            robots.ForEach(Add);
            foreach (var robot in robots)
            {
                var home = PrototypeArsenal.HomeTerrainOf(robot.Id);
                Add(available.FirstOrDefault(w => w.Category == WeaponCategory.Guard
                    && PrototypeArsenal.HomeTerrainOf(w.Id) == home));
            }

            available.ToList().ForEach(Add);
            var count = Math.Min(Math.Min(slots, LoadoutRules.MaxSlots), order.Count);
            return order.Take(count).Select(w => w.Id).ToList();
        }

        private static int HomeTerrainCount(Dictionary<TerrainType, int> terrainCounts, WeaponDefinition robot)
        {
            var home = PrototypeArsenal.HomeTerrainOf(robot.Id);
            return home.HasValue && terrainCounts.TryGetValue(home.Value, out var count) ? count : 0;
        }
    }
}
