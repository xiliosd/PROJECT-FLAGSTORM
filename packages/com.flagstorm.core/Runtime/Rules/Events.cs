using System.Collections.Generic;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Rules
{
    // Los eventos alimentan la presentación (animaciones, registro) y las repeticiones.
    public interface IMatchEvent
    {
    }

    public sealed record SetupCompleted(Side Side) : IMatchEvent;

    public sealed record MatchStarted(Side FirstSide) : IMatchEvent;

    public sealed record TurnEnded(Side NextSide, int TurnNumber) : IMatchEvent;

    public sealed record EnergyChanged(Side Side, int Energy) : IMatchEvent;

    public sealed record CellsRevealed(Side By, IReadOnlyList<HexCoord> Cells) : IMatchEvent;

    public sealed record MissileImpact(Side By, string WeaponId, HexCoord Target, IReadOnlyList<HexCoord> Cells)
        : IMatchEvent;

    public sealed record DefenseDamaged(Side Owner, HexCoord Cell, string WeaponId, int RemainingHitPoints)
        : IMatchEvent;

    // Revelar el tipo de defensa destruida es intencional: destruir algo te dice qué era.
    public sealed record DefenseDestroyed(Side Owner, HexCoord Cell, string WeaponId) : IMatchEvent;
}
