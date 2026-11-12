using System.Collections.Generic;
using System.Linq;

namespace Flagstorm.Core.Arsenal
{
    public enum LoadoutError
    {
        None,
        TooMany,
        Duplicate,
        NotUnlocked,
        NoCaptureWeapon,
    }

    // Reglas del arsenal que cada jugador lleva a la partida (spec de defensas §7).
    public static class LoadoutRules
    {
        public const int MaxSlots = 6;

        public static LoadoutError Validate(IReadOnlyList<string> selection, IReadOnlyList<WeaponDefinition> unlocked)
        {
            if (selection.Count > MaxSlots)
            {
                return LoadoutError.TooMany;
            }

            if (selection.Distinct().Count() != selection.Count)
            {
                return LoadoutError.Duplicate;
            }

            var byId = unlocked.ToDictionary(w => w.Id);
            if (selection.Any(id => !byId.ContainsKey(id)))
            {
                return LoadoutError.NotUnlocked;
            }

            // Sin un robot de captura no hay forma de ganar, así que el arsenal no sirve.
            return selection.Any(id => byId[id].Category == WeaponCategory.Capture)
                ? LoadoutError.None
                : LoadoutError.NoCaptureWeapon;
        }
    }
}
