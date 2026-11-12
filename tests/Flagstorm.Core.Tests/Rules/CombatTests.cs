using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Rules
{
    // Isla roja 5×5 (ver TestIslands): (2,3) es Hielo y (1,3) Roca; (3,3), (-1,4) y (-2,4) son costa.
    [TestFixture]
    public class CombatTests
    {
        private static readonly HexCoord IceCell = new HexCoord(2, 3);
        private static readonly HexCoord RockCell = new HexCoord(1, 3);
        private static readonly HexCoord CoastNextToIce = new HexCoord(3, 3);
        private static readonly HexCoord CoastNextToRock = new HexCoord(-1, 4);
        private static readonly HexCoord FarCoast = new HexCoord(-2, 4);

        // Rojo coloca sus defensas y azul desembarca un robot trepador; queda listo el cierre del turno azul.
        private static (Match Match, int RobotId) Stage(
            string redDefense, HexCoord defenseCell, HexCoord landing, string robot = PrototypeArsenal.JungleRobot)
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            AssertAccepted(match.Submit(Side.Red, new BuildDefenseCommand(redDefense, defenseCell)));
            match.Submit(Side.Red, new EndTurnCommand());
            var deploy = match.Submit(Side.Blue, new DeployRobotCommand(robot, landing));
            AssertAccepted(deploy);
            return (match, deploy.Events.OfType<RobotDeployed>().Single().RobotId);
        }

        private static void AssertAccepted(CommandResult result) =>
            Assert.That(result.IsAccepted, Is.True, $"rechazado: {result.Reason}");

        private static GuardInfo RedGuard(Match match) => match.ViewFor(Side.Red).OwnGuards.Single();

        [Test]
        public void AdjacentRobots_ClashOncePerAttackerTurn()
        {
            var (match, robotId) = Stage(PrototypeArsenal.BeachGuard, IceCell, CoastNextToIce);
            var end = match.Submit(Side.Blue, new EndTurnCommand());
            var clash = end.Events.OfType<RobotsClashed>().Single();
            Assert.That(clash.AttackerId, Is.EqualTo(robotId));
            Assert.That(clash.DamageToGuard, Is.EqualTo(2));
            Assert.That(clash.DamageToAttacker, Is.EqualTo(2));
            Assert.That(RedGuard(match).HitPoints, Is.EqualTo(2));
            Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().HitPoints, Is.EqualTo(2));
        }

        [Test]
        public void HomeTerrain_GivesAdvantageInClash()
        {
            var (match, _) = Stage(PrototypeArsenal.SnowGuard, IceCell, CoastNextToIce);
            var clash = match.Submit(Side.Blue, new EndTurnCommand()).Events.OfType<RobotsClashed>().Single();
            Assert.That(clash.DamageToAttacker, Is.EqualTo(3));
            Assert.That(clash.DamageToGuard, Is.EqualTo(2));
            Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().HitPoints, Is.EqualTo(1));
        }

        [Test]
        public void Clash_DestroysRobotAtZeroHitPoints()
        {
            var (match, robotId) = Stage(PrototypeArsenal.SnowGuard, IceCell, CoastNextToIce);
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new EndTurnCommand());
            var second = match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(second.Events.OfType<RobotDestroyed>().Single().RobotId, Is.EqualTo(robotId));
            Assert.That(match.ViewFor(Side.Blue).OwnRobots, Is.Empty);
        }

        [Test]
        public void TwoGuardsAgainstOneRobot_IsDeterministic()
        {
            var forward = PlayTwoGuardsAgainstOneRobot(new[] { IceCell, RockCell });
            var reversed = PlayTwoGuardsAgainstOneRobot(new[] { RockCell, IceCell });
            Assert.That(reversed.EventNames, Is.EqualTo(forward.EventNames));
            Assert.That(reversed.GuardHitPoints, Is.EqualTo(forward.GuardHitPoints));
            Assert.That(reversed.RobotAlive, Is.EqualTo(forward.RobotAlive));
            Assert.That(forward.RobotAlive, Is.False);
            Assert.That(forward.GuardHitPoints, Is.EqualTo(new[] { 2, 4 }));
        }

        [Test]
        public void TwoGuardsAgainstOneRobot_SameOrderTwice_ReachesSameHash()
        {
            var first = PlayTwoGuardsAgainstOneRobot(new[] { IceCell, RockCell });
            var second = PlayTwoGuardsAgainstOneRobot(new[] { IceCell, RockCell });
            Assert.That(second.Hash, Is.EqualTo(first.Hash));
        }

        private static (List<string> EventNames, List<int> GuardHitPoints, bool RobotAlive, string Hash)
            PlayTwoGuardsAgainstOneRobot(HexCoord[] placementOrder)
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            foreach (var cell in placementOrder)
            {
                AssertAccepted(match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, cell)));
            }

            match.Submit(Side.Red, new EndTurnCommand());
            AssertAccepted(match.Submit(
                Side.Blue, new DeployRobotCommand(PrototypeArsenal.JungleRobot, new HexCoord(1, 4))));
            var end = match.Submit(Side.Blue, new EndTurnCommand());
            var names = end.Events.Select(e => e.GetType().Name).ToList();
            var hitPoints = match.ViewFor(Side.Red).OwnGuards.Select(g => g.HitPoints).OrderBy(h => h).ToList();
            return (names, hitPoints, match.ViewFor(Side.Blue).OwnRobots.Any(), match.ComputeStateHash());
        }

        [Test]
        public void Guard_AdvancesOneCellTowardIntruder()
        {
            var (match, _) = Stage(PrototypeArsenal.BeachGuard, RockCell, CoastNextToRock);
            var end = match.Submit(Side.Blue, new EndTurnCommand());
            var moved = end.Events.OfType<GuardMoved>().Single();
            Assert.That(moved.From, Is.EqualTo(RockCell));
            Assert.That(moved.To.DistanceTo(RockCell), Is.EqualTo(1));
            Assert.That(moved.To.DistanceTo(CoastNextToRock), Is.EqualTo(1));
            Assert.That(RedGuard(match).Position, Is.EqualTo(moved.To));
            Assert.That(RedGuard(match).Post, Is.EqualTo(RockCell));
        }

        [Test]
        public void Guard_NeverLeavesItsZone()
        {
            var (match, robotId) = Stage(PrototypeArsenal.BeachGuard, RockCell, FarCoast);
            for (var round = 0; round < 10; round++)
            {
                match.Submit(Side.Blue, new EndTurnCommand());
                Assert.That(RedGuard(match).Position.DistanceTo(RockCell), Is.LessThanOrEqualTo(2));
                match.Submit(Side.Red, new EndTurnCommand());
            }

            Assert.That(RedGuard(match).Position, Is.EqualTo(RockCell), "sin intrusos en la zona no se mueve");

            // El atacante entra y sale de la zona: el defensor persigue y regresa sin pasarse del radio.
            for (var round = 0; round < 6; round++)
            {
                var inside = round % 2 == 0;
                var from = inside ? FarCoast : CoastNextToRock;
                var to = inside ? CoastNextToRock : FarCoast;
                var stillAlive = match.ViewFor(Side.Blue).OwnRobots.Any(r => r.RobotId == robotId);
                if (stillAlive)
                {
                    match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { to }));
                }

                match.Submit(Side.Blue, new EndTurnCommand());
                Assert.That(RedGuard(match).Position.DistanceTo(RockCell), Is.LessThanOrEqualTo(2), $"{from}");
                match.Submit(Side.Red, new EndTurnCommand());
            }
        }

        [Test]
        public void Guard_ReturnsTowardPostWhenZoneIsEmpty()
        {
            var (match, robotId) = Stage(PrototypeArsenal.BeachGuard, RockCell, CoastNextToRock);
            match.Submit(Side.Blue, new EndTurnCommand());
            var advanced = RedGuard(match).Position;
            Assert.That(advanced, Is.Not.EqualTo(RockCell));
            match.Submit(Side.Red, new EndTurnCommand());
            match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { FarCoast }));
            var end = match.Submit(Side.Blue, new EndTurnCommand());
            var moved = end.Events.OfType<GuardMoved>().Single();
            Assert.That(moved.From, Is.EqualTo(advanced));
            Assert.That(moved.To, Is.EqualTo(RockCell));
            Assert.That(RedGuard(match).Position, Is.EqualTo(RockCell));
        }

        [Test]
        public void RobotNextToShield_DamagesItEachTurn_UntilDestroyed()
        {
            var (match, robotId) = Stage(PrototypeArsenal.DroneShield, IceCell, CoastNextToIce);
            var first = match.Submit(Side.Blue, new EndTurnCommand());
            var hit = first.Events.OfType<RobotHitDefense>().Single();
            Assert.That(hit.RobotId, Is.EqualTo(robotId));
            Assert.That(hit.Defense, Is.EqualTo(IceCell));
            Assert.That(hit.Damage, Is.EqualTo(2));
            Assert.That(first.Events.OfType<DefenseDamaged>().Single().RemainingHitPoints, Is.EqualTo(1));
            match.Submit(Side.Red, new EndTurnCommand());
            var second = match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(second.Events.OfType<DefenseDestroyed>().Single().Cell, Is.EqualTo(IceCell));
            Assert.That(match.ViewFor(Side.Red).OwnDefenses.ContainsKey(IceCell), Is.False);
            Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().HitPoints, Is.EqualTo(4));
        }

        [Test]
        public void RobotNextToDecoy_DoesNotDamageIt()
        {
            var (match, _) = Stage(PrototypeArsenal.Decoy, IceCell, CoastNextToIce);
            var end = match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(end.Events.OfType<RobotHitDefense>(), Is.Empty);
            Assert.That(match.ViewFor(Side.Red).OwnDefenses.ContainsKey(IceCell), Is.True);
        }

        [Test]
        public void FlagCaptured_ReportsTheCapturingWeapon()
        {
            var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1));
            var move = match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { new HexCoord(1, 1) }));
            var captured = move.Events.OfType<FlagCaptured>().Single();
            Assert.That(captured.WeaponId, Is.EqualTo(PrototypeArsenal.JungleRobot));
        }

        [Test]
        public void Replay_WithCombat_ReachesSameHash()
        {
            var (original, _) = Stage(PrototypeArsenal.SnowGuard, IceCell, CoastNextToIce);
            original.Submit(Side.Blue, new EndTurnCommand());
            original.Submit(Side.Red, new EndTurnCommand());
            original.Submit(Side.Blue, new EndTurnCommand());
            var replayed = MatchReplayer.Replay(() => MatchBuilder.NewSetup(), original.History);
            Assert.That(replayed.ComputeStateHash(), Is.EqualTo(original.ComputeStateHash()));
        }
    }
}
