using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Ai;
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
        public void Setup_FirstTapsMarkFlagsThenDefenses_AndTappingAgainUnmarks()
        {
            var session = MatchSession.CreateDefault(Seed);
            var cells = OwnCells(session).Take(7).ToList();

            foreach (var cell in cells)
            {
                session.TapOwnCell(cell);
            }

            Assert.That(session.PendingFlags.Count, Is.EqualTo(5));
            Assert.That(session.PendingDefenses.Count, Is.EqualTo(2));

            session.TapOwnCell(cells[0]);
            Assert.That(session.PendingFlags, Has.No.Member(cells[0]));
            Assert.That(session.PendingFlags.Count, Is.EqualTo(4));
        }

        [Test]
        public void Setup_PlacesSelectedFreeDefense()
        {
            var session = MatchSession.CreateDefault(Seed);
            var cells = OwnCells(session).Take(7).ToList();
            foreach (var cell in cells.Take(5))
            {
                session.TapOwnCell(cell);
            }

            session.SelectSetupDefense(PrototypeArsenal.DroneShield);
            session.TapOwnCell(cells[5]);
            session.SelectSetupDefense(PrototypeArsenal.BeachGuard);
            session.TapOwnCell(cells[6]);

            Assert.That(session.PendingDefenses.Select(p => p.WeaponId),
                Is.EqualTo(new[] { PrototypeArsenal.DroneShield, PrototypeArsenal.BeachGuard }));
            Assert.That(session.ConfirmSetup().IsAccepted, Is.True);
            Assert.That(session.View.OwnDefenses[cells[5]].WeaponId, Is.EqualTo(PrototypeArsenal.DroneShield));
            Assert.That(session.View.OwnGuards.Single().Post, Is.EqualTo(cells[6]));
            Assert.That(session.PendingDefenses, Is.Empty);
        }

        [Test]
        public void Setup_CannotSelectAttackWeaponAsDefense()
        {
            var session = MatchSession.CreateDefault(Seed);
            var initial = session.SelectedSetupDefenseId;

            Assert.That(session.SelectSetupDefense(PrototypeArsenal.Drone), Is.False);
            Assert.That(session.SelectSetupDefense(PrototypeArsenal.BeachRobot), Is.False);
            Assert.That(session.SelectSetupDefense(PrototypeArsenal.Turret), Is.False, "No está en el loadout");
            Assert.That(session.SelectedSetupDefenseId, Is.EqualTo(initial));
            Assert.That(session.SetupDefenseOptions.Select(w => w.Id),
                Is.EqualTo(new[] { PrototypeArsenal.DroneShield, PrototypeArsenal.BeachGuard }));
        }

        [Test]
        public void Create_RejectsAnInvalidPlayerLoadout()
        {
            var unlocked = ArsenalProgression.UnlockedAfter(0);

            Assert.Throws<System.ArgumentException>(
                () => MatchSession.Create(Seed, new[] { PrototypeArsenal.Drone }, unlocked));
        }

        [Test]
        public void Create_AiChoosesAValidLoadoutFromTheUnlockedWeapons()
        {
            var unlocked = ArsenalProgression.UnlockedAfter(3);
            var playerLoadout = unlocked.Take(4).Select(w => w.Id).ToList();

            var session = MatchSession.Create(Seed, playerLoadout, unlocked);

            var aiLoadout = session.Match.LoadoutOf(MatchSession.AiSide);
            Assert.That(LoadoutRules.Validate(aiLoadout, unlocked), Is.EqualTo(LoadoutError.None));
            Assert.That(session.Match.LoadoutOf(MatchSession.HumanSide), Is.EqualTo(playerLoadout));
        }

        [Test]
        public void TapOwnCell_WithGuardWeaponPlacesTheGuard()
        {
            var session = StartedSession();
            session.SelectWeapon(PrototypeArsenal.BeachGuard);
            var free = OwnCells(session).Skip(7).First();

            var result = session.TapOwnCell(free);

            Assert.That(result.IsAccepted, Is.True, result.Reason.ToString());
            Assert.That(session.View.OwnGuards.Single().Post, Is.EqualTo(free));
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
            session.SelectWeapon(PrototypeArsenal.DroneShield);

            var result = session.TapEnemyCell(EnemyCells(session).First());

            Assert.That(result.IsAccepted, Is.False);
        }

        [Test]
        public void TapOwnCell_WithDefenseWeaponBuildsIt()
        {
            var session = StartedSession();
            session.SelectWeapon(PrototypeArsenal.DroneShield);
            var free = OwnCells(session).Skip(7).First();

            var result = session.TapOwnCell(free);

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(session.View.OwnDefenses.ContainsKey(free), Is.True);
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
            session.SelectWeapon(PrototypeArsenal.BeachRobot);
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
            session.SelectWeapon(PrototypeArsenal.BeachRobot);
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
            session.SelectWeapon(PrototypeArsenal.BeachRobot);
            var coast = session.View.EnemyIsland.CoastCells;
            session.TapEnemyCell(coast[0]);

            // El arma sigue elegida tras usarla, para repetir la acción sin volver a la barra.
            Assert.That(session.SelectedWeaponId, Is.EqualTo(PrototypeArsenal.BeachRobot));
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

        [Test]
        public void Create_AiLoadoutHasTheSameSizeAsThePlayers()
        {
            var unlocked = ArsenalProgression.UnlockedAfter(3);
            var playerLoadout = unlocked.Take(4).Select(w => w.Id).ToList();

            var session = MatchSession.Create(Seed, playerLoadout, unlocked);

            Assert.That(session.Match.LoadoutOf(MatchSession.AiSide), Has.Count.EqualTo(playerLoadout.Count));
        }

        [Test]
        public void Robot_WalksAroundAKnownGuard()
        {
            var island = PrototypeIslands.CreateDefault();
            var landing = island.CoastCells.First();
            var guardPost = landing.Neighbors().First(island.InteriorCells.Contains);
            var session = StartedSessionWithRedGuard(island, guardPost);
            var definition = PrototypeArsenal.Get(PrototypeArsenal.BeachRobot);
            var blocked = new HashSet<HexCoord> { guardPost };
            var destination = island.Cells.Keys.First(cell =>
                cell != guardPost
                && PathFinder.FindPath(island, definition, landing, cell)?.Contains(guardPost) == true
                && PathFinder.FindPath(island, definition, landing, cell, blocked) != null);

            session.SelectWeapon(PrototypeArsenal.BeachRobot);
            Assert.That(session.TapEnemyCell(landing).IsAccepted, Is.True);
            Assert.That(session.View.KnownEnemyGuards.Single().Position, Is.EqualTo(guardPost));
            session.TapEnemyCell(landing);
            var result = session.TapEnemyCell(destination);

            Assert.That(result.IsAccepted, Is.True, result.Reason.ToString());
            Assert.That(result.Events.OfType<RobotMoved>().Single().Path, Has.No.Member(guardPost));
        }

        [Test]
        public void MissileOnARevealedGuard_ShowsItsDamageInTheLog()
        {
            var island = PrototypeIslands.CreateDefault();
            var guardPost = island.InteriorCells.First();
            var session = StartedSessionWithRedGuard(island, guardPost);
            session.SelectWeapon(PrototypeArsenal.Drone);
            session.TapEnemyCell(guardPost);
            Assert.That(session.View.KnownEnemyGuards, Has.Count.EqualTo(1));

            session.SelectWeapon(PrototypeArsenal.Drone);
            session.SelectWeapon(PrototypeArsenal.StandardMissile);
            var result = session.TapEnemyCell(guardPost);

            Assert.That(result.IsAccepted, Is.True, result.Reason.ToString());
            Assert.That(session.VisibleEventsOf(result).OfType<GuardDamaged>().Count(), Is.EqualTo(1));
        }

        // Rojo ya hizo su preparación con un defensor gratis en `guardPost`, así que la IA no la repite.
        private static MatchSession StartedSessionWithRedGuard(IslandMap island, HexCoord guardPost)
        {
            var loadout = ArsenalProgression.UnlockedAfter(1).Select(w => w.Id).ToList();
            var match = new Match(
                PrototypeIslands.CreateDefault(), island, RulesConfig.Default, PrototypeArsenal.All, loadout, loadout);
            var others = island.InteriorCells.Where(c => c != guardPost).ToList();
            var secondGuard = others.Last();
            var flags = others.Take(5).ToList();
            var placements = new[]
            {
                new DefensePlacement(PrototypeArsenal.BeachGuard, guardPost),
                new DefensePlacement(PrototypeArsenal.BeachGuard, secondGuard),
            };
            Assert.That(match.Submit(MatchSession.AiSide, new SetupCommand(flags, placements)).IsAccepted, Is.True);
            var session = new MatchSession(match, PrototypeArsenal.All, new BasicAiPlayer(new SeededRandom(Seed)));
            foreach (var cell in OwnCells(session).Take(5))
            {
                session.TapOwnCell(cell);
            }

            Assert.That(session.ConfirmSetup().IsAccepted, Is.True);
            return session;
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
