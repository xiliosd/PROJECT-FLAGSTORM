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
}
