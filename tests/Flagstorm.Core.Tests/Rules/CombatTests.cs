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
            Assert.That(clash.AttackerHitPointsLeft, Is.EqualTo(2));
            Assert.That(clash.GuardHitPointsLeft, Is.EqualTo(2));
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
            // Defensores asimétricos: si el objetivo dependiera del orden, el daño recibido cambiaría.
            foreach (var order in new[]
            {
                new[] { PrototypeArsenal.SnowGuard, PrototypeArsenal.BeachGuard },
                new[] { PrototypeArsenal.BeachGuard, PrototypeArsenal.SnowGuard },
            })
            {
                var match = MatchBuilder.NewPlaying();
                match.Submit(Side.Blue, new EndTurnCommand());
                foreach (var weapon in order)
                {
                    var cell = weapon == PrototypeArsenal.SnowGuard ? IceCell : RockCell;
                    AssertAccepted(match.Submit(Side.Red, new BuildDefenseCommand(weapon, cell)));
                }

                match.Submit(Side.Red, new EndTurnCommand());
                var robotId = DeployBlue(match, PrototypeArsenal.JungleRobot, new HexCoord(1, 4));
                var guardIds = match.ViewFor(Side.Red).OwnGuards.Select(g => g.RobotId).ToList();
                var end = match.Submit(Side.Blue, new EndTurnCommand());
                var clashes = end.Events.OfType<RobotsClashed>().ToList();
                Assert.That(clashes, Has.Count.EqualTo(2), "ambos defensores golpean al robot");
                Assert.That(clashes.Single(c => c.DamageToGuard > 0).GuardId, Is.EqualTo(guardIds.Min()));
                Assert.That(clashes.Sum(c => c.DamageToAttacker), Is.EqualTo(5), "3 en Hielo + 2 en Roca");
                Assert.That(end.Events.OfType<RobotDestroyed>().Single().RobotId, Is.EqualTo(robotId));
            }
        }

        private static int DeployBlue(Match match, string weapon, HexCoord cell)
        {
            var result = match.Submit(Side.Blue, new DeployRobotCommand(weapon, cell));
            AssertAccepted(result);
            return result.Events.OfType<RobotDeployed>().Single().RobotId;
        }

        // Azul termina un turno sin robots, rojo construye y azul queda listo para desembarcar.
        private static Match NewBlueToDeploy(params (string Weapon, HexCoord Cell)[] redBuilds)
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            foreach (var build in redBuilds)
            {
                AssertAccepted(match.Submit(Side.Red, new BuildDefenseCommand(build.Weapon, build.Cell)));
            }

            match.Submit(Side.Red, new EndTurnCommand());
            return match;
        }

        [Test]
        public void GuardBetweenTwoRobots_HitsLowerIdAndBothRobotsHitGuard()
        {
            var match = NewBlueToDeploy((PrototypeArsenal.BeachGuard, RockCell));
            var first = DeployBlue(match, PrototypeArsenal.JungleRobot, new HexCoord(0, 4));
            var second = DeployBlue(match, PrototypeArsenal.JungleRobot, new HexCoord(1, 4));
            var clashes = match.Submit(Side.Blue, new EndTurnCommand()).Events.OfType<RobotsClashed>().ToList();
            Assert.That(clashes.Select(c => c.AttackerId), Is.EqualTo(new[] { first, second }));
            Assert.That(clashes[0].DamageToAttacker, Is.EqualTo(2), "el defensor golpea al de menor id");
            Assert.That(clashes[1].DamageToAttacker, Is.EqualTo(0));
            Assert.That(clashes.Select(c => c.DamageToGuard), Is.EqualTo(new[] { 2, 2 }));
            Assert.That(clashes[1].GuardHitPointsLeft, Is.EqualTo(0));
        }

        [Test]
        public void RobotThatClashed_DoesNotAlsoHitAdjacentFixedDefense()
        {
            var match = NewBlueToDeploy(
                (PrototypeArsenal.BeachGuard, RockCell), (PrototypeArsenal.DroneShield, IceCell));
            DeployBlue(match, PrototypeArsenal.JungleRobot, new HexCoord(1, 4));
            var end = match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(end.Events.OfType<RobotsClashed>(), Is.Not.Empty);
            Assert.That(end.Events.OfType<RobotHitDefense>(), Is.Empty);
            Assert.That(match.ViewFor(Side.Red).OwnDefenses[IceCell].HitPoints, Is.EqualTo(3));
        }

        [Test]
        public void GuardThatClashed_DoesNotMove()
        {
            // Dos robots en la zona: A (junto al defensor) muere en el choque; B queda a 2 casillas.
            var match = NewBlueToDeploy((PrototypeArsenal.SnowGuard, IceCell));
            var near = DeployBlue(match, PrototypeArsenal.JungleRobot, new HexCoord(3, 2));
            DeployBlue(match, PrototypeArsenal.JungleRobot, new HexCoord(4, 1));
            var end = match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(end.Events.OfType<RobotDestroyed>().Select(e => e.RobotId), Is.EqualTo(new[] { near }));
            Assert.That(end.Events.OfType<GuardMoved>(), Is.Empty);
            Assert.That(RedGuard(match).Position, Is.EqualTo(IceCell));
        }

        [Test]
        public void Clash_CanDestroyBothSidesInTheSameExchange()
        {
            var (match, robotId) = Stage(PrototypeArsenal.BeachGuard, IceCell, CoastNextToIce);
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new EndTurnCommand());
            var end = match.Submit(Side.Blue, new EndTurnCommand());
            var clash = end.Events.OfType<RobotsClashed>().Single();
            Assert.That(clash.AttackerHitPointsLeft, Is.EqualTo(0));
            Assert.That(clash.GuardHitPointsLeft, Is.EqualTo(0));
            Assert.That(end.Events.OfType<RobotDestroyed>().Single().RobotId, Is.EqualTo(robotId));
            Assert.That(end.Events.OfType<GuardDestroyed>().Single().Cell, Is.EqualTo(IceCell));
            Assert.That(match.ViewFor(Side.Red).OwnGuards, Is.Empty);
        }

        [Test]
        public void Guard_StaysPut_WhenItsOnlyImprovingCellIsOccupied()
        {
            // Intruso en (4,1): la única casilla que acerca al defensor de (2,3) es (3,2), ocupada por un escudo.
            var match = NewBlueToDeploy(
                (PrototypeArsenal.SnowGuard, IceCell), (PrototypeArsenal.DroneShield, new HexCoord(3, 2)));
            DeployBlue(match, PrototypeArsenal.JungleRobot, new HexCoord(4, 1));
            var end = match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(end.Events.OfType<GuardMoved>(), Is.Empty);
            Assert.That(RedGuard(match).Position, Is.EqualTo(IceCell));
        }

        [Test]
        public void RobotDestroyedByTurret_DoesNotClash()
        {
            var match = NewBlueToDeploy();
            DeployBlue(match, PrototypeArsenal.JungleRobot, new HexCoord(3, 2));
            match.Submit(Side.Blue, new EndTurnCommand());
            AssertAccepted(match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.SnowGuard, IceCell)));
            match.Submit(Side.Red, new EndTurnCommand());
            var end = match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(end.Events.OfType<TurretFired>(), Is.Not.Empty);
            Assert.That(end.Events.OfType<RobotDestroyed>(), Has.Exactly(1).Items);
            Assert.That(end.Events.OfType<RobotsClashed>(), Is.Empty);
            Assert.That(RedGuard(match).HitPoints, Is.EqualTo(4));
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
                var to = inside ? CoastNextToRock : FarCoast;
                var stillAlive = match.ViewFor(Side.Blue).OwnRobots.Any(r => r.RobotId == robotId);
                Assert.That(stillAlive, Is.True);
                AssertAccepted(match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { to })));

                match.Submit(Side.Blue, new EndTurnCommand());
                Assert.That(RedGuard(match).Position.DistanceTo(RockCell), Is.LessThanOrEqualTo(2));
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
