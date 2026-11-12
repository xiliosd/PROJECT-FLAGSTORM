using System.Globalization;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Rules
{
    // Pruebas de los hallazgos de la revisión final del núcleo.
    [TestFixture]
    public class ReviewFixTests
    {
        [Test]
        public void StateHash_DoesNotDependOnCurrentCulture()
        {
            var (match, _) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(-2, 4));
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                var invariant = match.ComputeStateHash();
                CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
                Assert.That(match.ComputeStateHash(), Is.EqualTo(invariant));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void DeployingOntoAFlag_CapturesIt()
        {
            var match = MatchBuilder.NewSetup();
            match.Submit(Side.Blue, MatchBuilder.DefaultSetupFor(match.Rules));
            var coastFlags = new[]
            {
                new HexCoord(0, 1), new HexCoord(2, 1), new HexCoord(1, 2), new HexCoord(2, 2), new HexCoord(0, 3),
            };
            match.Submit(Side.Red, new SetupCommand(coastFlags, MatchBuilder.DefaultFreeDefenses));

            var result = match.Submit(
                Side.Blue, new DeployRobotCommand(PrototypeArsenal.TrackedRobot, new HexCoord(0, 1)));
            Assert.That(result.Events.OfType<FlagCaptured>().Single().Cell, Is.EqualTo(new HexCoord(0, 1)));
            Assert.That(match.ViewFor(Side.Blue).FlagsCapturedByViewer, Is.EqualTo(1));
        }

        [Test]
        public void Missile_CannotTellDecoyFromFlag()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.Decoy, new HexCoord(1, 3)));
            match.Submit(Side.Red, new EndTurnCommand());
            match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 3)));

            var missile = match.Submit(
                Side.Blue, new FireMissileCommand(PrototypeArsenal.StandardMissile, new HexCoord(1, 3)));
            Assert.That(missile.Events.OfType<DefenseDestroyed>(), Is.Empty);
            Assert.That(missile.Events.OfType<DefenseDamaged>(), Is.Empty);
            Assert.That(match.ViewFor(Side.Blue).EnemySignals, Does.Contain(new HexCoord(1, 3)));
            Assert.That(match.ViewFor(Side.Red).OwnDefenses.Keys, Does.Contain(new HexCoord(1, 3)));
        }
    }
}
