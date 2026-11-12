using System;
using System.Collections.Generic;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Arsenal
{
    public sealed record WeaponDefinition(
        string Id,
        string DisplayName,
        WeaponCategory Category,
        int EnergyCost,
        int Power,
        int Radius,
        int HitPoints,
        int MovementPoints,
        IReadOnlyDictionary<TerrainType, int> PowerModifierPercent,
        IReadOnlyDictionary<TerrainType, int> MovementCost)
    {
        // Categoría de ataque que esta defensa detiene (escudo contra Reveal, antimisiles contra Destroy).
        public WeaponCategory? Intercepts { get; init; }

        // Victorias necesarias para tener el arma disponible en el arsenal (ArsenalProgression).
        public int UnlockLevel { get; init; }

        public int ModifierPercentOn(TerrainType terrain) =>
            PowerModifierPercent != null && PowerModifierPercent.TryGetValue(terrain, out var percent) ? percent : 0;

        // Redondeo "lejos del cero" para que un -50 % sobre 3 deje 2 y no 1: el arma débil sigue sirviendo.
        public int EffectivePowerOn(TerrainType terrain)
        {
            var scaled = Power * (100 + ModifierPercentOn(terrain)) / 100.0;
            return Math.Max(0, (int)Math.Round(scaled, MidpointRounding.AwayFromZero));
        }

        public int MovementCostOn(TerrainType terrain) =>
            MovementCost != null && MovementCost.TryGetValue(terrain, out var cost) ? cost : 1;
    }
}
