using System;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Rules
{
    [TestFixture]
    public class ReplayTests
    {
        [Test]
        public void ReplayingHistory_ReproducesExactState()
        {
            var original = PlayMixedTurns();
            var replayed = MatchReplayer.Replay(() => MatchBuilder.NewSetup(), original.History);
            Assert.That(replayed.ComputeStateHash(), Is.EqualTo(original.ComputeStateHash()));
            Assert.That(replayed.TurnNumber, Is.EqualTo(original.TurnNumber));
        }

        [Test]
        public void SameState_SameHash_AcrossInstances()
        {
            Assert.That(
                MatchBuilder.NewPlaying().ComputeStateHash(),
                Is.EqualTo(MatchBuilder.NewPlaying().ComputeStateHash()));
        }

        [Test]
        public void DifferentCommands_ProduceDifferentHash()
        {
            var a = MatchBuilder.NewPlaying();
            var b = MatchBuilder.NewPlaying();
            b.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 1)));
            Assert.That(b.ComputeStateHash(), Is.Not.EqualTo(a.ComputeStateHash()));
        }

        [Test]
        public void Replay_WithInvalidHistory_Throws()
        {
            var bad = new[] { new RecordedCommand(Side.Red, new EndTurnCommand()) };
            Assert.Throws<InvalidOperationException>(() => MatchReplayer.Replay(() => MatchBuilder.NewSetup(), bad));
        }

        // Dron, misil, robot, defensa y varios cambios de turno.
        private static Match PlayMixedTurns()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 1)));
            var deploy = match.Submit(
                Side.Blue, new DeployRobotCommand(PrototypeArsenal.TrackedRobot, new HexCoord(0, 1)));
            var robotId = deploy.Events.OfType<RobotDeployed>().Single().RobotId;
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.Decoy, new HexCoord(1, 3)));
            match.Submit(Side.Red, new FireMissileCommand(PrototypeArsenal.StandardMissile, new HexCoord(3, 1)));
            match.Submit(Side.Red, new EndTurnCommand());
            match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { new HexCoord(1, 1) }));
            Assert.That(match.History.Count, Is.EqualTo(9));
            return match;
        }
    }
}
