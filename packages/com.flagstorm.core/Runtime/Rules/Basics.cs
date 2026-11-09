using System.Collections.Generic;

namespace Flagstorm.Core.Rules
{
    // Blue es el jugador humano en la campaña; Red es la IA.
    public enum Side
    {
        Blue,
        Red,
    }

    public enum MatchPhase
    {
        Setup,
        Playing,
        Finished,
    }

    public enum RejectionReason
    {
        None,
        WrongPhase,
        NotYourTurn,
        MatchFinished,
        InvalidSetup,
        OutOfIsland,
        NotEnoughEnergy,
        UnknownWeapon,
        WrongCategory,
        NotCoast,
        InvalidPath,
        UnknownRobot,
        CellOccupied,
    }

    public static class SideExtensions
    {
        public static Side Opponent(this Side side) => side == Side.Blue ? Side.Red : Side.Blue;
    }

    // Valores de equilibrio en datos, para ajustarlos con pruebas de juego sin tocar las reglas.
    public sealed record RulesConfig(int FlagsPerSide, int EnergyPerTurn, int MaxEnergy, int InitialFreeTurrets)
    {
        public static RulesConfig Default => new RulesConfig(5, 6, 10, 2);
    }

    public sealed record CommandResult(bool IsAccepted, RejectionReason Reason, IReadOnlyList<IMatchEvent> Events)
    {
        private static readonly IReadOnlyList<IMatchEvent> NoEvents = new IMatchEvent[0];

        public static CommandResult Accepted(IReadOnlyList<IMatchEvent> events) =>
            new CommandResult(true, RejectionReason.None, events ?? NoEvents);

        public static CommandResult Rejected(RejectionReason reason) => new CommandResult(false, reason, NoEvents);
    }

    public sealed record RecordedCommand(Side Side, IMatchCommand Command);
}
