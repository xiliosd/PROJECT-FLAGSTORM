using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;

namespace Flagstorm.Core.Tests.Support
{
    public static class MatchBuilder
    {
        public static readonly IReadOnlyList<HexCoord> DefaultFlags = new[]
        {
            new HexCoord(1, 1), new HexCoord(2, 1), new HexCoord(1, 2), new HexCoord(2, 2), new HexCoord(0, 3),
        };

        public static readonly IReadOnlyList<HexCoord> DefaultTurrets = new[] { new HexCoord(3, 1), new HexCoord(0, 2) };

        public static Match NewSetup(RulesConfig rules = null) =>
            new Match(TestIslands.Small(), TestIslands.Small(), rules ?? RulesConfig.Default, PrototypeArsenal.All);

        public static Match NewPlaying(RulesConfig rules = null)
        {
            var match = NewSetup(rules);
            var setup = DefaultSetupFor(match.Rules);
            Assert.That(match.Submit(Side.Blue, setup).IsAccepted, Is.True, "setup azul");
            Assert.That(match.Submit(Side.Red, setup).IsAccepted, Is.True, "setup rojo");
            return match;
        }

        public static (Match Match, int RobotId) NewPlayingWithBlueRobotAt(HexCoord coast, RulesConfig rules = null)
        {
            var match = NewPlaying(rules);
            var result = match.Submit(Side.Blue, new DeployRobotCommand(PrototypeArsenal.TrackedRobot, coast));
            Assert.That(result.IsAccepted, Is.True, $"despliegue: {result.Reason}");
            return (match, result.Events.OfType<RobotDeployed>().Single().RobotId);
        }

        public static SetupCommand DefaultSetupFor(RulesConfig rules) =>
            new SetupCommand(DefaultFlags.Take(rules.FlagsPerSide).ToList(), DefaultTurrets);
    }
}
