using System.Collections.Generic;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Rules
{
    internal sealed class DefenseState
    {
        public DefenseState(string weaponId, int hitPoints)
        {
            WeaponId = weaponId;
            HitPoints = hitPoints;
        }

        public string WeaponId { get; }

        public int HitPoints { get; set; }
    }

    internal sealed class RobotState
    {
        public RobotState(int robotId, string weaponId, HexCoord position, int hitPoints, int movementPointsLeft)
        {
            RobotId = robotId;
            WeaponId = weaponId;
            Position = position;
            HitPoints = hitPoints;
            MovementPointsLeft = movementPointsLeft;
        }

        public int RobotId { get; }

        public string WeaponId { get; }

        public HexCoord Position { get; set; }

        public int HitPoints { get; set; }

        public int MovementPointsLeft { get; set; }

        public RobotInfo ToInfo() => new RobotInfo(RobotId, WeaponId, Position, HitPoints, MovementPointsLeft);
    }

    // Estado de un bando: su isla, lo que esconde y defiende, y lo que sabe y tiene en la isla rival.
    internal sealed class SideState
    {
        public SideState(IslandMap island)
        {
            Island = island;
        }

        public IslandMap Island { get; }

        public bool SetupDone { get; set; }

        public int Energy { get; set; }

        public HashSet<HexCoord> Flags { get; } = new HashSet<HexCoord>();

        public Dictionary<HexCoord, DefenseState> Defenses { get; } = new Dictionary<HexCoord, DefenseState>();

        public HashSet<HexCoord> RevealedEnemyCells { get; } = new HashSet<HexCoord>();

        // Robots de este bando, que siempre están en la isla rival.
        public List<RobotState> Robots { get; } = new List<RobotState>();

        public int FlagsCaptured { get; set; }
    }
}
