using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using NUnit.Framework;

namespace Flagstorm.Presentation.Tests
{
    public class MatchSessionTests
    {
        private const uint Seed = 7;

        [Test]
        public void Setup_FirstTapsMarkFlagsThenTurrets_AndTappingAgainUnmarks()
        {
            var session = MatchSession.CreateDefault(Seed);
            var cells = OwnCells(session).Take(7).ToList();

            foreach (var cell in cells)
            {
                session.TapOwnCell(cell);
            }

            Assert.That(session.PendingFlags.Count, Is.EqualTo(5));
            Assert.That(session.PendingTurrets.Count, Is.EqualTo(2));

            session.TapOwnCell(cells[0]);
            Assert.That(session.PendingFlags, Has.No.Member(cells[0]));
            Assert.That(session.PendingFlags.Count, Is.EqualTo(4));
        }

        [Test]
        public void Setup_TappingBeyondTheLimitIsRejected()
        {
            var session = MatchSession.CreateDefault(Seed);
            var cells = OwnCells(session).Take(8).ToList();
            CommandResult last = null;
            foreach (var cell in cells)
            {
                last = session.TapOwnCell(cell);
            }

            Assert.That(last, Is.Not.Null);
            Assert.That(last.Reason, Is.EqualTo(RejectionReason.InvalidSetup));
        }

        [Test]
        public void ConfirmSetup_StartsTheMatchOnTheHumanTurn()
        {
            var session = StartedSession();

            Assert.That(session.Match.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(session.Match.CurrentSide, Is.EqualTo(MatchSession.HumanSide));
            Assert.That(session.IsAiTurn, Is.False);
        }

        [Test]
        public void ConfirmSetup_WithTooFewFlagsIsRejected()
        {
            var session = MatchSession.CreateDefault(Seed);
            session.TapOwnCell(OwnCells(session).First());

            Assert.That(session.ConfirmSetup().Reason, Is.EqualTo(RejectionReason.InvalidSetup));
        }

        [Test]
        public void TapEnemyCell_WithoutSelectionDoesNothing()
        {
            var session = StartedSession();

            Assert.That(session.TapEnemyCell(EnemyCells(session).First()), Is.Null);
        }

        [Test]
        public void TapEnemyCell_WithDroneRevealsCells()
        {
            var session = StartedSession();
            session.SelectWeapon(PrototypeArsenal.Drone);

            var result = session.TapEnemyCell(EnemyCells(session).First());

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.Events.OfType<CellsRevealed>(), Is.Not.Empty);
        }

        [Test]
        public void TapEnemyCell_WithDefenseWeaponIsRejected()
        {
            var session = StartedSession();
            session.SelectWeapon(PrototypeArsenal.Turret);

            var result = session.TapEnemyCell(EnemyCells(session).First());

            Assert.That(result.IsAccepted, Is.False);
        }

        [Test]
        public void TapOwnCell_WithDefenseWeaponBuildsIt()
        {
            var session = StartedSession();
            session.SelectWeapon(PrototypeArsenal.Decoy);
            var free = OwnCells(session).Skip(7).First();

            var result = session.TapOwnCell(free);

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(session.View.OwnDecoys, Has.Member(free));
        }

        [Test]
        public void SelectWeapon_Twice_ClearsSelection()
        {
            var session = StartedSession();
            session.SelectWeapon(PrototypeArsenal.Drone);
            session.SelectWeapon(PrototypeArsenal.Drone);

            Assert.That(session.SelectedWeaponId, Is.Null);
        }

        [Test]
        public void Robot_DeployThenSelectThenMove()
        {
            var session = StartedSession();
            var landing = session.View.EnemyIsland.CoastCells.First();
            session.SelectWeapon(PrototypeArsenal.TrackedRobot);
            Assert.That(session.TapEnemyCell(landing).IsAccepted, Is.True);
            var robot = session.View.OwnRobots.Single();

            Assert.That(session.TapEnemyCell(landing), Is.Null);
            Assert.That(session.SelectedRobotId, Is.EqualTo(robot.RobotId));
            Assert.That(session.SelectedWeaponId, Is.Null);

            var destination = landing.Neighbors().First(c => session.View.EnemyIsland.Contains(c));
            var result = session.TapEnemyCell(destination);

            Assert.That(result.IsAccepted, Is.True, result.Reason.ToString());
            Assert.That(result.Events.OfType<RobotMoved>(), Is.Not.Empty);
        }

        [Test]
        public void Robot_FarDestinationIsTruncatedToItsMovementBudget()
        {
            var session = StartedSession();
            var island = session.View.EnemyIsland;
            var landing = island.CoastCells.First();
            session.SelectWeapon(PrototypeArsenal.TrackedRobot);
            session.TapEnemyCell(landing);
            session.TapEnemyCell(landing);

            var farthest = island.Cells.Keys.OrderByDescending(c => c.DistanceTo(landing)).First();
            var result = session.TapEnemyCell(farthest);

            Assert.That(result.IsAccepted, Is.True, result.Reason.ToString());
            var robot = session.View.OwnRobots.Single();
            Assert.That(robot.Position, Is.Not.EqualTo(farthest));
            Assert.That(robot.Position, Is.Not.EqualTo(landing));
        }

        [Test]
        public void TapEnemyCell_WithoutEnoughEnergyIsRejected()
        {
            var session = StartedSession();
            session.SelectWeapon(PrototypeArsenal.TrackedRobot);
            var coast = session.View.EnemyIsland.CoastCells;
            session.TapEnemyCell(coast[0]);

            // El arma sigue elegida tras usarla, para repetir la acción sin volver a la barra.
            Assert.That(session.SelectedWeaponId, Is.EqualTo(PrototypeArsenal.TrackedRobot));
            var result = session.TapEnemyCell(coast[1]);

            Assert.That(result.Reason, Is.EqualTo(RejectionReason.NotEnoughEnergy));
        }

        [Test]
        public void StepAi_PlaysUntilItEndsItsTurn()
        {
            var session = StartedSession();
            session.EndTurn();
            Assert.That(session.IsAiTurn, Is.True);

            for (var step = 0; step < 50 && session.IsAiTurn; step++)
            {
                Assert.That(session.StepAi().IsAccepted, Is.True);
            }

            Assert.That(session.IsAiTurn, Is.False);
            Assert.That(session.Match.CurrentSide, Is.EqualTo(MatchSession.HumanSide));
        }

        private static MatchSession StartedSession()
        {
            var session = MatchSession.CreateDefault(Seed);
            foreach (var cell in OwnCells(session).Take(5))
            {
                session.TapOwnCell(cell);
            }

            Assert.That(session.ConfirmSetup().IsAccepted, Is.True);
            return session;
        }

        private static System.Collections.Generic.IEnumerable<HexCoord> OwnCells(MatchSession session) =>
            session.View.OwnIsland.InteriorCells;

        private static System.Collections.Generic.IEnumerable<HexCoord> EnemyCells(MatchSession session) =>
            session.View.EnemyIsland.InteriorCells;
    }
}
