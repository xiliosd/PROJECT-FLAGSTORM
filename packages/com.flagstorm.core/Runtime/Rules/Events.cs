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

    public sealed record RobotDeployed(Side Owner, int RobotId, string WeaponId, HexCoord Cell) : IMatchEvent;

    public sealed record RobotMoved(Side Owner, int RobotId, IReadOnlyList<HexCoord> Path) : IMatchEvent;

    public sealed record FlagCaptured(Side By, HexCoord Cell, int TotalCaptured, string WeaponId) : IMatchEvent;

    public sealed record DecoyExposed(Side By, HexCoord Cell) : IMatchEvent;

    public sealed record MatchWon(Side Winner) : IMatchEvent;

    public sealed record DefenseBuilt(Side Owner, HexCoord Cell, string WeaponId) : IMatchEvent;

    public sealed record GuardPlaced(Side Owner, int RobotId, string WeaponId, HexCoord Cell) : IMatchEvent;

    public sealed record TurretFired(Side Owner, HexCoord Turret, int TargetRobotId, int Damage) : IMatchEvent;

    public sealed record DroneShotDown(Side By, HexCoord Target, HexCoord Shield) : IMatchEvent;

    public sealed record MissileIntercepted(
        Side By, string WeaponId, HexCoord Target, HexCoord AntiMissile, int RemainingHitPoints) : IMatchEvent;

    public sealed record GuardDamaged(Side Owner, int RobotId, int RemainingHitPoints) : IMatchEvent;

    public sealed record GuardDestroyed(Side Owner, int RobotId, HexCoord Cell) : IMatchEvent;

    public sealed record RobotDestroyed(Side Owner, int RobotId, HexCoord Cell) : IMatchEvent;

    // Golpes simultáneos entre un robot de ataque y un defensor; cada daño es el que recibe el otro bando.
    public sealed record RobotsClashed(
        Side Attacker,
        int AttackerId,
        int GuardId,
        int DamageToGuard,
        int DamageToAttacker,
        int AttackerHitPointsLeft,
        int GuardHitPointsLeft) : IMatchEvent;

    public sealed record GuardMoved(Side Owner, int RobotId, HexCoord From, HexCoord To) : IMatchEvent;

    public sealed record RobotHitDefense(Side Attacker, int RobotId, HexCoord Defense, int Damage) : IMatchEvent;
}
