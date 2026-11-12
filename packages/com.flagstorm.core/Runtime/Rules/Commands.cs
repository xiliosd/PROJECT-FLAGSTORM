using System.Collections.Generic;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Rules
{
    // Cada jugada es un comando serializable: permite repeticiones y, en el futuro, multijugador (spec §11).
    public interface IMatchCommand
    {
    }

    public sealed record DefensePlacement(string WeaponId, HexCoord Cell);

    public sealed record SetupCommand(
        IReadOnlyList<HexCoord> Flags, IReadOnlyList<DefensePlacement> FreeDefenses) : IMatchCommand;

    public sealed record LaunchDroneCommand(string WeaponId, HexCoord Target) : IMatchCommand;

    public sealed record FireMissileCommand(string WeaponId, HexCoord Target) : IMatchCommand;

    public sealed record DeployRobotCommand(string WeaponId, HexCoord LandingCell) : IMatchCommand;

    public sealed record MoveRobotCommand(int RobotId, IReadOnlyList<HexCoord> Path) : IMatchCommand;

    public sealed record BuildDefenseCommand(string WeaponId, HexCoord Cell) : IMatchCommand;

    public sealed record EndTurnCommand() : IMatchCommand;
}
