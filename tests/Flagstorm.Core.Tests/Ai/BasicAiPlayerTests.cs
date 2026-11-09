using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Ai;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Ai
{
    [TestFixture]
    public class BasicAiPlayerTests
    {
        private static IEnumerable<uint> Seeds => Enumerable.Range(1, 20).Select(i => (uint)i);

        [Test]
        public void SeededRandom_IsDeterministicAndInRange()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);
            for (var i = 0; i < 100; i++)
            {
                var x = a.Next(3, 9);
                Assert.That(x, Is.EqualTo(b.Next(3, 9)));
                Assert.That(x, Is.InRange(3, 8));
            }
        }

        [Test]
        public void SeededRandom_ZeroSeedStillProducesValues()
        {
            var random = new SeededRandom(0);
            var values = Enumerable.Range(0, 20).Select(_ => random.Next(0, 1000)).Distinct().Count();
            Assert.That(values, Is.GreaterThan(1));
        }

        [TestCaseSource(nameof(Seeds))]
        public void ChooseSetup_IsAlwaysAccepted(uint seed)
        {
            var match = NewPrototypeMatch();
            var ai = new BasicAiPlayer(new SeededRandom(seed));
            var result = match.Submit(Side.Blue, ai.ChooseSetup(match.ViewFor(Side.Blue)));
            Assert.That(result.IsAccepted, Is.True, result.Reason.ToString());
        }

        [Test]
        public void ChooseSetup_VariesWithSeed()
        {
            var view = NewPrototypeMatch().ViewFor(Side.Blue);
            var first = new BasicAiPlayer(new SeededRandom(1)).ChooseSetup(view).Flags;
            var different = Seeds.Skip(1).Any(
                seed => !new BasicAiPlayer(new SeededRandom(seed)).ChooseSetup(view).Flags.SequenceEqual(first));
            Assert.That(different, Is.True);
        }

        [Test]
        public void AttacksKnownSignalWithRobot()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 1)));
            var ai = new BasicAiPlayer(new SeededRandom(3));
            var command = ai.NextCommand(match.ViewFor(Side.Red), PrototypeArsenal.All);
            Assert.That(command, Is.TypeOf<DeployRobotCommand>());
        }

        [TestCaseSource(nameof(Seeds))]
        public void AiVsAi_OnPrototypeIsland_FinishesWithoutRejectedCommands(uint seed)
        {
            var match = NewPrototypeMatch();
            var players = new Dictionary<Side, BasicAiPlayer>
            {
                [Side.Blue] = new BasicAiPlayer(new SeededRandom(seed)),
                [Side.Red] = new BasicAiPlayer(new SeededRandom(seed + 1000)),
            };
            foreach (var side in new[] { Side.Blue, Side.Red })
            {
                Assert.That(match.Submit(side, players[side].ChooseSetup(match.ViewFor(side))).IsAccepted, Is.True);
            }

            var guard = 0;
            while (match.Phase == MatchPhase.Playing && guard++ < 5000)
            {
                var side = match.CurrentSide;
                var command = players[side].NextCommand(match.ViewFor(side), PrototypeArsenal.All);
                var result = match.Submit(side, command);
                Assert.That(result.IsAccepted, Is.True, $"turno {match.TurnNumber}: {command} → {result.Reason}");
            }

            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(match.TurnNumber, Is.LessThanOrEqualTo(200));
            TestContext.Out.WriteLine($"semilla {seed}: {match.TurnNumber} turnos, ganó {match.Winner}");
        }

        private static Match NewPrototypeMatch() =>
            new Match(
                PrototypeIslands.CreateDefault(),
                PrototypeIslands.CreateDefault(),
                RulesConfig.Default,
                PrototypeArsenal.All);
    }
}
