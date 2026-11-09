using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Rules
{
    [TestFixture]
    public class DefenseTests
    {
        [Test]
        public void EnemyTurret_ShootsRobotThatEndsTurnInRange()
        {
            var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1));
            var result = match.Submit(Side.Blue, new EndTurnCommand());
            var shot = result.Events.OfType<TurretFired>().Single();
            Assert.That(shot.Turret, Is.EqualTo(new HexCoord(0, 2)));
            Assert.That(shot.TargetRobotId, Is.EqualTo(robotId));
            Assert.That(shot.Damage, Is.EqualTo(2));
            Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().HitPoints, Is.EqualTo(2));
        }

        [Test]
        public void RobotOutOfRange_IsNotShot()
        {
            var (match, _) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(-2, 4));
            var result = match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(result.Events.OfType<TurretFired>(), Is.Empty);
        }

        [Test]
        public void TwoTurnsInRange_DestroyRobot()
        {
            var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1));
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new EndTurnCommand());
            var result = match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(result.Events.OfType<RobotDestroyed>().Single().RobotId, Is.EqualTo(robotId));
            Assert.That(match.ViewFor(Side.Blue).OwnRobots, Is.Empty);
            Assert.That(match.ViewFor(Side.Red).EnemyRobotsOnOwnIsland, Is.Empty);
        }

        [Test]
        public void BuildTurret_CostsEnergyAndAppearsInOwnView()
        {
            var match = MatchBuilder.NewPlaying();
            var result = match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.Turret, new HexCoord(1, 3)));
            Assert.That(result.Events.OfType<DefenseBuilt>().Single().Cell, Is.EqualTo(new HexCoord(1, 3)));
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(3));
            Assert.That(match.ViewFor(Side.Blue).OwnTurrets[new HexCoord(1, 3)], Is.EqualTo(4));
        }

        [Test]
        public void Decoy_LooksLikeAFlagUntilARobotStepsOnIt()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.Decoy, new HexCoord(1, 3)));
            Assert.That(match.ViewFor(Side.Red).OwnDecoys, Is.EquivalentTo(new[] { new HexCoord(1, 3) }));
            match.Submit(Side.Red, new EndTurnCommand());

            match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 3)));
            var signals = match.ViewFor(Side.Blue).EnemySignals;
            Assert.That(signals, Does.Contain(new HexCoord(1, 3)));
            Assert.That(signals, Does.Contain(new HexCoord(0, 3)));

            var deploy = match.Submit(
                Side.Blue, new DeployRobotCommand(PrototypeArsenal.TrackedRobot, new HexCoord(1, 4)));
            var robotId = deploy.Events.OfType<RobotDeployed>().Single().RobotId;
            var move = match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { new HexCoord(1, 3) }));
            Assert.That(move.Events.OfType<DecoyExposed>().Single().Cell, Is.EqualTo(new HexCoord(1, 3)));
            Assert.That(move.Events.OfType<FlagCaptured>(), Is.Empty);
            Assert.That(match.ViewFor(Side.Blue).EnemySignals, Does.Not.Contain(new HexCoord(1, 3)));
        }

        [Test]
        public void BuildOnEnemyIslandOrOccupiedCell_IsRejected()
        {
            var match = MatchBuilder.NewPlaying();
            Assert.That(
                match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.Turret, new HexCoord(10, 10))).Reason,
                Is.EqualTo(RejectionReason.OutOfIsland));
            Assert.That(
                match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.Turret, new HexCoord(1, 1))).Reason,
                Is.EqualTo(RejectionReason.CellOccupied));
            Assert.That(
                match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.Turret, new HexCoord(3, 1))).Reason,
                Is.EqualTo(RejectionReason.CellOccupied));
            Assert.That(
                match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.Drone, new HexCoord(1, 3))).Reason,
                Is.EqualTo(RejectionReason.WrongCategory));
        }
    }
}
