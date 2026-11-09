using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Arsenal
{
    // Valores del prototipo; se ajustan con pruebas de juego (plan, tarea 12).
    public static class PrototypeArsenal
    {
        public const string Drone = "dron-explorador";
        public const string StandardMissile = "misil-estandar";
        public const string IncendiaryMissile = "misil-incendiario";
        public const string TrackedRobot = "robot-orugas";
        public const string SnowRobot = "robot-nieve";
        public const string Turret = "torreta";
        public const string Decoy = "senuelo";

        public static readonly IReadOnlyList<WeaponDefinition> All = new[]
        {
            new WeaponDefinition(Drone, "Dron explorador", WeaponCategory.Reveal, 2, 0, 1, 0, 0, None(), None()),
            new WeaponDefinition(
                StandardMissile, "Misil estándar", WeaponCategory.Destroy, 3, 3, 1, 0, 0, None(), None()),
            new WeaponDefinition(
                IncendiaryMissile,
                "Misil incendiario",
                WeaponCategory.Destroy,
                3,
                3,
                1,
                0,
                0,
                new Dictionary<TerrainType, int> { [TerrainType.Jungle] = 40, [TerrainType.Ice] = -50 },
                None()),
            new WeaponDefinition(
                TrackedRobot,
                "Robot de orugas",
                WeaponCategory.Capture,
                4,
                0,
                0,
                4,
                3,
                None(),
                new Dictionary<TerrainType, int>
                {
                    [TerrainType.Beach] = 1,
                    [TerrainType.Rock] = 1,
                    [TerrainType.Jungle] = 1,
                    [TerrainType.Ice] = 2,
                    [TerrainType.Volcano] = 3,
                }),
            new WeaponDefinition(
                SnowRobot,
                "Robot de nieve",
                WeaponCategory.Capture,
                4,
                0,
                0,
                3,
                3,
                None(),
                new Dictionary<TerrainType, int>
                {
                    [TerrainType.Ice] = 1,
                    [TerrainType.Beach] = 1,
                    [TerrainType.Rock] = 2,
                    [TerrainType.Jungle] = 2,
                    [TerrainType.Volcano] = 3,
                }),
            new WeaponDefinition(Turret, "Torreta", WeaponCategory.Defend, 3, 2, 1, 4, 0, None(), None()),
            new WeaponDefinition(Decoy, "Señuelo", WeaponCategory.Defend, 2, 0, 0, 1, 0, None(), None()),
        };

        public static WeaponDefinition Get(string id)
        {
            var weapon = All.FirstOrDefault(w => w.Id == id);
            if (weapon == null)
            {
                throw new KeyNotFoundException($"No existe el arma '{id}'.");
            }

            return weapon;
        }

        // Diccionario vacío por arma: los datos de cada arma quedan independientes entre sí.
        private static Dictionary<TerrainType, int> None() => new Dictionary<TerrainType, int>();
    }
}
