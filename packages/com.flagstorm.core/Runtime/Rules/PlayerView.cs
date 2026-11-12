using System.Collections.Generic;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Rules
{
    public sealed record RobotInfo(
        int RobotId, string WeaponId, HexCoord Position, int HitPoints, int MovementPointsLeft);

    public sealed record GuardInfo(int RobotId, string WeaponId, HexCoord Post, HexCoord Position, int HitPoints);

    public sealed record DefenseInfo(string WeaponId, int HitPoints);

    // Lo que un bando puede saber. La IA solo recibe esto, así no puede hacer trampa (spec §5.2).
    public sealed class PlayerView
    {
        public Side Viewer { get; internal set; }

        public MatchPhase Phase { get; internal set; }

        public Side CurrentSide { get; internal set; }

        public int TurnNumber { get; internal set; }

        public int Energy { get; internal set; }

        public RulesConfig Rules { get; internal set; }

        public IslandMap OwnIsland { get; internal set; }

        public IslandMap EnemyIsland { get; internal set; }

        public IReadOnlyCollection<HexCoord> OwnFlags { get; internal set; }

        public IReadOnlyList<string> Loadout { get; internal set; }

        // Todas las defensas propias (señuelos incluidos), ordenadas por CellOrder.
        public IReadOnlyDictionary<HexCoord, DefenseInfo> OwnDefenses { get; internal set; }

        public IReadOnlyList<GuardInfo> OwnGuards { get; internal set; }

        // Defensores rivales en celdas reveladas o a distancia 1 o menos de un robot propio.
        public IReadOnlyList<GuardInfo> KnownEnemyGuards { get; internal set; }

        public IReadOnlyCollection<HexCoord> OwnDecoys { get; internal set; }

        public IReadOnlyDictionary<HexCoord, int> OwnTurrets { get; internal set; }

        public IReadOnlyCollection<HexCoord> RevealedEnemyCells { get; internal set; }

        // Banderas y señuelos en celdas reveladas, sin distinguir cuál es cuál.
        public IReadOnlyCollection<HexCoord> EnemySignals { get; internal set; }

        public IReadOnlyDictionary<HexCoord, int> KnownEnemyTurrets { get; internal set; }

        public IReadOnlyList<RobotInfo> OwnRobots { get; internal set; }

        public IReadOnlyList<RobotInfo> EnemyRobotsOnOwnIsland { get; internal set; }

        public int FlagsCapturedByViewer { get; internal set; }

        public int FlagsLostByViewer { get; internal set; }
    }
}
