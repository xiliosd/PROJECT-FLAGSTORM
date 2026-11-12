using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Rules
{
    [TestFixture]
    public class GuardTests
    {
        private static readonly HexCoord GuardCell = new HexCoord(1, 3);

        [Test]
        public void BuildGuard_CostsEnergyAndAppearsInOwnView()
        {
            var match = MatchBuilder.NewPlaying();
            var result = match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, GuardCell));
            var placed = result.Events.OfType<GuardPlaced>().Single();
            Assert.That(placed.Cell, Is.EqualTo(GuardCell));
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(3));
            var guard = match.ViewFor(Side.Blue).OwnGuards.Single();
            Assert.That(guard.RobotId, Is.EqualTo(placed.RobotId));
            Assert.That(guard.Post, Is.EqualTo(GuardCell));
            Assert.That(guard.Position, Is.EqualTo(GuardCell));
            Assert.That(guard.HitPoints, Is.EqualTo(4));
        }

        [Test]
        public void BuildGuard_OnOccupiedCell_IsRejected()
        {
            var match = MatchBuilder.NewPlaying();
            var result = match.Submit(
                Side.Blue, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, MatchBuilder.DefaultFlags[0]));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.CellOccupied));
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(6));
        }

        [Test]
        public void BuildGuard_OnGuardCell_IsRejected()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, GuardCell));
            var result = match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, GuardCell));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.CellOccupied));
        }

        [Test]
        public void EnemyGuard_IsHiddenUntilRevealedOrAdjacent()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, GuardCell));
            Assert.That(match.ViewFor(Side.Blue).KnownEnemyGuards, Is.Empty);
            Assert.That(match.ViewFor(Side.Red).OwnGuards, Has.Count.EqualTo(1));
            match.Submit(Side.Red, new EndTurnCommand());
            match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, GuardCell));
            Assert.That(match.ViewFor(Side.Blue).KnownEnemyGuards.Single().Position, Is.EqualTo(GuardCell));
        }

        [Test]
        public void EnemyGuard_IsVisibleWhenAdjacentToOwnRobot()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, GuardCell));
            match.Submit(Side.Red, new EndTurnCommand());
            match.Submit(Side.Blue, new DeployRobotCommand(PrototypeArsenal.JungleRobot, new HexCoord(1, 4)));
            Assert.That(match.ViewFor(Side.Blue).KnownEnemyGuards, Has.Count.EqualTo(1));
        }

        [Test]
        public void AttackRobot_CannotWalkIntoGuardCell()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, GuardCell));
            match.Submit(Side.Red, new EndTurnCommand());
            var deploy = match.Submit(
                Side.Blue, new DeployRobotCommand(PrototypeArsenal.JungleRobot, new HexCoord(1, 4)));
            var robotId = deploy.Events.OfType<RobotDeployed>().Single().RobotId;
            var result = match.Submit(
                Side.Blue, new MoveRobotCommand(robotId, new[] { GuardCell, new HexCoord(1, 2) }));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.InvalidPath));
            Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().Position, Is.EqualTo(new HexCoord(1, 4)));
        }

        [Test]
        public void DeployOnEnemyGuardCell_IsRejected()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, new HexCoord(1, 4)));
            match.Submit(Side.Red, new EndTurnCommand());
            var result = match.Submit(
                Side.Blue, new DeployRobotCommand(PrototypeArsenal.JungleRobot, new HexCoord(1, 4)));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.CellOccupied));
        }

        [Test]
        public void BuildGuard_OnEnemyAttackRobotCell_IsRejected()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new DeployRobotCommand(PrototypeArsenal.JungleRobot, new HexCoord(1, 4)));
            match.Submit(Side.Blue, new EndTurnCommand());
            var energy = match.EnergyOf(Side.Red);
            var result = match.Submit(
                Side.Red, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, new HexCoord(1, 4)));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.CellOccupied));
            Assert.That(match.EnergyOf(Side.Red), Is.EqualTo(energy));
        }

        [Test]
        public void BuildDefense_OnOwnGuardCell_IsRejected()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, GuardCell));
            var result = match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.Turret, GuardCell));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.CellOccupied));
        }

        [Test]
        public void BuildDefense_OnEnemyAttackRobotCell_IsRejected()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new DeployRobotCommand(PrototypeArsenal.JungleRobot, new HexCoord(1, 4)));
            match.Submit(Side.Blue, new EndTurnCommand());
            var result = match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.Turret, new HexCoord(1, 4)));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.CellOccupied));
        }

        [Test]
        public void StateHash_ChangesWhenGuardIsPlaced()
        {
            var first = MatchBuilder.NewPlaying();
            var second = MatchBuilder.NewPlaying();
            first.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, GuardCell));
            second.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, new HexCoord(0, 4)));
            Assert.That(first.ComputeStateHash(), Is.Not.EqualTo(second.ComputeStateHash()));
        }
    }
}
