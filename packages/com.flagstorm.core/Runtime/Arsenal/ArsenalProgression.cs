using System;
using System.Collections.Generic;
using System.Linq;

namespace Flagstorm.Core.Arsenal
{
    // Cada victoria de campaña desbloquea el siguiente nivel del catálogo (spec de defensas §7).
    public static class ArsenalProgression
    {
        public static int MaxLevel => PrototypeArsenal.All.Max(w => w.UnlockLevel);

        public static IReadOnlyList<WeaponDefinition> UnlockedAfter(int wins)
        {
            var level = Math.Max(0, wins);
            return PrototypeArsenal.All.Where(w => w.UnlockLevel <= level).ToList();
        }
    }
}
