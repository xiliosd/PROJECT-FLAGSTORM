using Flagstorm.Core.Ai;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using NUnit.Framework;

namespace Flagstorm.Presentation.Tests
{
    // Comprueba que el núcleo compila y corre con el C# y el runtime de Unity (records, shim IsExternalInit).
    public class CoreSmokeTests
    {
        [Test]
        public void CoreRunsInsideUnity()
        {
            var match = new Match(
                PrototypeIslands.CreateDefault(),
                PrototypeIslands.CreateDefault(),
                RulesConfig.Default,
                PrototypeArsenal.All);
            var ai = new BasicAiPlayer(new SeededRandom(7));
            Assert.That(match.Submit(Side.Blue, ai.ChooseSetup(match.ViewFor(Side.Blue))).IsAccepted, Is.True);
        }

        [Test]
        public void StateHash_MatchesBetweenTwoIdenticalMatches()
        {
            Assert.That(NewPlayed().ComputeStateHash(), Is.EqualTo(NewPlayed().ComputeStateHash()));
        }

        private static Match NewPlayed()
        {
            var match = new Match(
                PrototypeIslands.CreateDefault(),
                PrototypeIslands.CreateDefault(),
                RulesConfig.Default,
                PrototypeArsenal.All);
            var blue = new BasicAiPlayer(new SeededRandom(11));
            var red = new BasicAiPlayer(new SeededRandom(12));
            match.Submit(Side.Blue, blue.ChooseSetup(match.ViewFor(Side.Blue)));
            match.Submit(Side.Red, red.ChooseSetup(match.ViewFor(Side.Red)));
            for (var i = 0; i < 30 && match.Phase == MatchPhase.Playing; i++)
            {
                var side = match.CurrentSide;
                var player = side == Side.Blue ? blue : red;
                match.Submit(side, player.NextCommand(match.ViewFor(side), PrototypeArsenal.All));
            }

            return match;
        }
    }
}
