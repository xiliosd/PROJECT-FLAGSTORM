using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Rules
{
    [TestFixture]
    public class RobotAndCaptureTests
    {
        [Test]
        public void RobotLandsOnCoastAndCapturesFlag()
        {
            var match = MatchBuilder.NewPlaying();
            var deploy = match.Submit(
                Side.Blue, new DeployRobotCommand(PrototypeArsenal.JungleRobot, new HexCoord(0, 1)));
            var robotId = deploy.Events.OfType<RobotDeployed>().Single().RobotId;
            Assert.That(robotId, Is.EqualTo(1));
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(2));

            var move = match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { new HexCoord(1, 1) }));
            Assert.That(move.Events.OfType<FlagCaptured>().Single().TotalCaptured, Is.EqualTo(1));

            var view = match.ViewFor(Side.Blue);
            Assert.That(view.FlagsCapturedByViewer, Is.EqualTo(1));
            Assert.That(view.OwnRobots.Single().MovementPointsLeft, Is.EqualTo(2));
            var redView = match.ViewFor(Side.Red);
            Assert.That(redView.FlagsLostByViewer, Is.EqualTo(1));
            Assert.That(redView.OwnFlags, Does.Not.Contain(new HexCoord(1, 1)));
            Assert.That(redView.EnemyRobotsOnOwnIsland.Single().Position, Is.EqualTo(new HexCoord(1, 1)));
        }

        [Test]
        public void DeployOnInterior_IsRejected()
        {
            var match = MatchBuilder.NewPlaying();
            var result = match.Submit(
                Side.Blue, new DeployRobotCommand(PrototypeArsenal.TrackedRobot, new HexCoord(1, 1)));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.NotCoast));
        }

        [Test]
        public void DeployOnOccupiedCoastOrOutside_IsRejected()
        {
            var (match, _) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1));
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new EndTurnCommand());
            var robot = PrototypeArsenal.TrackedRobot;
            Assert.That(
                match.Submit(Side.Blue, new DeployRobotCommand(robot, new HexCoord(0, 1))).Reason,
                Is.EqualTo(RejectionReason.CellOccupied));
            Assert.That(
                match.Submit(Side.Blue, new DeployRobotCommand(robot, new HexCoord(10, 10))).Reason,
                Is.EqualTo(RejectionReason.OutOfIsland));
        }

        [Test]
        public void NonAdjacentPath_IsRejectedAtomically()
        {
            var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1));
            var result = match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { new HexCoord(2, 2) }));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.InvalidPath));
            Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().Position, Is.EqualTo(new HexCoord(0, 1)));
        }

        [Test]
        public void PathOverBudget_IsRejectedWithoutCapturing()
        {
            var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1));
            var path = new[] { new HexCoord(1, 1), new HexCoord(1, 2) };
            var result = match.Submit(Side.Blue, new MoveRobotCommand(robotId, path));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.InvalidPath));
            Assert.That(match.ViewFor(Side.Blue).FlagsCapturedByViewer, Is.EqualTo(0));
            Assert.That(match.ViewFor(Side.Red).OwnFlags, Has.Count.EqualTo(5));
        }

        [Test]
        public void EmptyPathOrUnknownRobot_IsRejected()
        {
            var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1));
            Assert.That(
                match.Submit(Side.Blue, new MoveRobotCommand(robotId, new HexCoord[0])).Reason,
                Is.EqualTo(RejectionReason.InvalidPath));
            Assert.That(
                match.Submit(Side.Blue, new MoveRobotCommand(99, new[] { new HexCoord(1, 1) })).Reason,
                Is.EqualTo(RejectionReason.UnknownRobot));
        }

        [Test]
        public void MovementPoints_RefreshAtStartOfOwnersTurn()
        {
            var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1));
            match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { new HexCoord(1, 0), new HexCoord(2, 0) }));
            Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().MovementPointsLeft, Is.EqualTo(1));
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new EndTurnCommand());
            Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().MovementPointsLeft, Is.EqualTo(3));
        }

        [Test]
        public void CapturingLastFlag_WinsAndFreezesMatch()
        {
            var rules = RulesConfig.Default with { FlagsPerSide = 1 };
            var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1), rules);
            var path = new[] { new HexCoord(1, 1), new HexCoord(2, 1) };
            var result = match.Submit(Side.Blue, new MoveRobotCommand(robotId, path));
            Assert.That(result.Events.OfType<MatchWon>().Single().Winner, Is.EqualTo(Side.Blue));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(match.Winner, Is.EqualTo(Side.Blue));
            Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().Position, Is.EqualTo(new HexCoord(1, 1)));
            Assert.That(
                match.Submit(Side.Blue, new EndTurnCommand()).Reason, Is.EqualTo(RejectionReason.MatchFinished));
        }
    }
}
