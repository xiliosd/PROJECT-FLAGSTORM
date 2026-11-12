using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Arsenal
{
    // Valores del prototipo; se ajustan con pruebas de juego (plan, tarea 12).
    public static class PrototypeArsenal
    {
        public const string Drone = "dron-explorador";
        public const string DroneShield = "escudo-antidrones";
        public const string BeachRobot = "robot-anfibio";
        public const string BeachGuard = "defensor-anfibio";
        public const string StandardMissile = "misil-estandar";
        public const string AntiMissile = "antimisiles";
        public const string JungleRobot = "robot-trepador";
        public const string JungleGuard = "defensor-trepador";
        public const string SnowRobot = "robot-nieve";
        public const string SnowGuard = "defensor-nieve";
        public const string TrackedRobot = "robot-orugas";
        public const string TrackedGuard = "defensor-orugas";
        public const string MagmaRobot = "robot-magma";
        public const string MagmaGuard = "defensor-magma";
        public const string IncendiaryMissile = "misil-incendiario";
        public const string Turret = "torreta";
        public const string Decoy = "senuelo";

        // El orden del catálogo es el orden en que se muestran y desbloquean las armas.
        public static readonly IReadOnlyList<WeaponDefinition> All = new[]
        {
            new WeaponDefinition(Drone, "Dron explorador", WeaponCategory.Reveal, 2, 0, 1, 0, 0, None(), None()),
            new WeaponDefinition(
                DroneShield, "Escudo antidrones", WeaponCategory.Defend, 3, 0, 1, 3, 0, None(), None())
            {
                Intercepts = WeaponCategory.Reveal,
            },
            Robot(BeachRobot, "Robot anfibio", TerrainType.Beach, TerrainType.Volcano, 0),
            Guard(BeachGuard, "Defensor anfibio", TerrainType.Beach, TerrainType.Volcano, 0),
            new WeaponDefinition(
                StandardMissile, "Misil estándar", WeaponCategory.Destroy, 3, 3, 1, 0, 0, None(), None())
            {
                UnlockLevel = 1,
            },
            new WeaponDefinition(AntiMissile, "Antimisiles", WeaponCategory.Defend, 4, 0, 1, 3, 0, None(), None())
            {
                Intercepts = WeaponCategory.Destroy,
                UnlockLevel = 1,
            },
            Robot(JungleRobot, "Robot trepador", TerrainType.Jungle, TerrainType.Rock, 2),
            Guard(JungleGuard, "Defensor trepador", TerrainType.Jungle, TerrainType.Rock, 2),
            Robot(SnowRobot, "Robot de nieve", TerrainType.Ice, TerrainType.Volcano, 3),
            Guard(SnowGuard, "Defensor de nieve", TerrainType.Ice, TerrainType.Volcano, 3),
            Robot(TrackedRobot, "Robot de orugas", TerrainType.Rock, TerrainType.Jungle, 4),
            Guard(TrackedGuard, "Defensor de orugas", TerrainType.Rock, TerrainType.Jungle, 4),
            Robot(MagmaRobot, "Robot de magma", TerrainType.Volcano, TerrainType.Ice, 5),
            Guard(MagmaGuard, "Defensor de magma", TerrainType.Volcano, TerrainType.Ice, 5),
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
                None())
            {
                UnlockLevel = 6,
            },
            new WeaponDefinition(Turret, "Torreta", WeaponCategory.Defend, 3, 2, 1, 4, 0, None(), None())
            {
                UnlockLevel = 6,
            },
            new WeaponDefinition(Decoy, "Señuelo", WeaponCategory.Defend, 2, 0, 0, 1, 0, None(), None())
            {
                UnlockLevel = 7,
            },
        };

        // Terreno natal de cada robot y defensor; las demás armas no pertenecen a ningún terreno.
        private static readonly IReadOnlyDictionary<string, TerrainType> HomeTerrains =
            new Dictionary<string, TerrainType>
            {
                [BeachRobot] = TerrainType.Beach,
                [BeachGuard] = TerrainType.Beach,
                [JungleRobot] = TerrainType.Jungle,
                [JungleGuard] = TerrainType.Jungle,
                [SnowRobot] = TerrainType.Ice,
                [SnowGuard] = TerrainType.Ice,
                [TrackedRobot] = TerrainType.Rock,
                [TrackedGuard] = TerrainType.Rock,
                [MagmaRobot] = TerrainType.Volcano,
                [MagmaGuard] = TerrainType.Volcano,
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

        public static TerrainType? HomeTerrainOf(string weaponId) =>
            HomeTerrains.TryGetValue(weaponId, out var terrain) ? terrain : (TerrainType?)null;

        // Fuerte (+50 %) en su terreno y débil (-50 %) en el opuesto: con poder 2 da 3, 2 y 1.
        private static Dictionary<TerrainType, int> PowerByTerrain(TerrainType home, TerrainType opposite) =>
            new Dictionary<TerrainType, int> { [home] = 50, [opposite] = -50 };

        // Los robots avanzan barato en su terreno y en Playa; solo el de magma cruza el volcán a costo 1.
        private static Dictionary<TerrainType, int> RobotMovement(TerrainType home)
        {
            var costs = new Dictionary<TerrainType, int>
            {
                [TerrainType.Beach] = 1,
                [TerrainType.Jungle] = 2,
                [TerrainType.Ice] = 2,
                [TerrainType.Volcano] = 3,
                [TerrainType.Rock] = 2,
            };
            costs[home] = 1;
            return costs;
        }

        private static WeaponDefinition Robot(
            string id, string name, TerrainType home, TerrainType opposite, int unlockLevel) =>
            new WeaponDefinition(
                id, name, WeaponCategory.Capture, 4, 2, 0, 4, 3, PowerByTerrain(home, opposite), RobotMovement(home))
            {
                UnlockLevel = unlockLevel,
            };

        private static WeaponDefinition Guard(
            string id, string name, TerrainType home, TerrainType opposite, int unlockLevel) =>
            new WeaponDefinition(
                id, name, WeaponCategory.Guard, 3, 2, 2, 4, 1, PowerByTerrain(home, opposite), None())
            {
                UnlockLevel = unlockLevel,
            };

        // Diccionario vacío por arma: los datos de cada arma quedan independientes entre sí.
        private static Dictionary<TerrainType, int> None() => new Dictionary<TerrainType, int>();
    }
}
