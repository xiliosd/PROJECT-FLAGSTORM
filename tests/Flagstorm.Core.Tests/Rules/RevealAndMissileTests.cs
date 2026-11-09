using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Rules
{
    [TestFixture]
    public class RevealAndMissileTests
    {
        [Test]
        public void NewMatch_ViewHidesEverythingOfTheEnemy()
        {
            var view = MatchBuilder.NewPlaying().ViewFor(Side.Blue);
            Assert.That(view.RevealedEnemyCells, Is.Empty);
            Assert.That(view.EnemySignals, Is.Empty);
            Assert.That(view.KnownEnemyTurrets, Is.Empty);
            Assert.That(view.OwnFlags, Is.EquivalentTo(MatchBuilder.DefaultFlags));
        }

        [Test]
        public void Drone_RevealsRadiusAndShowsSignalsAndTurrets()
        {
            var match = MatchBuilder.NewPlaying();
            var result = match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 1)));
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.Events.OfType<CellsRevealed>().Single().Cells, Has.Count.EqualTo(7));

            var view = match.ViewFor(Side.Blue);
            Assert.That(view.RevealedEnemyCells, Has.Count.EqualTo(7));
            Assert.That(
                view.EnemySignals,
                Is.EquivalentTo(new[] { new HexCoord(1, 1), new HexCoord(2, 1), new HexCoord(1, 2) }));
            Assert.That(view.KnownEnemyTurrets.Keys, Is.EquivalentTo(new[] { new HexCoord(0, 2) }));
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(4));
        }

        [Test]
        public void Drone_DoesNotRevealOwnSideToEnemy()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 1)));
            Assert.That(match.ViewFor(Side.Red).RevealedEnemyCells, Is.Empty);
        }

        [Test]
        public void IncendiaryMissile_DamageDependsOnTerrain()
        {
            var match = MatchBuilder.NewPlaying();
            var onIce = match.Submit(
                Side.Blue, new FireMissileCommand(PrototypeArsenal.IncendiaryMissile, new HexCoord(3, 1)));
            Assert.That(onIce.Events.OfType<DefenseDamaged>().Single().RemainingHitPoints, Is.EqualTo(2));

            var onJungle = match.Submit(
                Side.Blue, new FireMissileCommand(PrototypeArsenal.IncendiaryMissile, new HexCoord(0, 2)));
            Assert.That(onJungle.Events.OfType<DefenseDestroyed>().Single().Cell, Is.EqualTo(new HexCoord(0, 2)));
            Assert.That(match.ViewFor(Side.Red).OwnTurrets.Keys, Is.EquivalentTo(new[] { new HexCoord(3, 1) }));
        }

        [Test]
        public void Missile_NeverDestroysFlags()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new FireMissileCommand(PrototypeArsenal.StandardMissile, new HexCoord(1, 1)));
            Assert.That(match.ViewFor(Side.Red).OwnFlags, Has.Count.EqualTo(5));
        }

        [Test]
        public void NotEnoughEnergy_IsRejectedAndEnergyUnchanged()
        {
            var match = MatchBuilder.NewPlaying();
            var missile = new FireMissileCommand(PrototypeArsenal.StandardMissile, new HexCoord(1, 1));
            match.Submit(Side.Blue, missile);
            match.Submit(Side.Blue, missile);
            var third = match.Submit(Side.Blue, missile);
            Assert.That(third.Reason, Is.EqualTo(RejectionReason.NotEnoughEnergy));
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(0));
        }

        [Test]
        public void TargetOutsideEnemyIsland_IsRejectedWithoutCost()
        {
            var match = MatchBuilder.NewPlaying();
            var drone = match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(10, 10)));
            var missile = match.Submit(
                Side.Blue, new FireMissileCommand(PrototypeArsenal.StandardMissile, new HexCoord(10, 10)));
            Assert.That(drone.Reason, Is.EqualTo(RejectionReason.OutOfIsland));
            Assert.That(missile.Reason, Is.EqualTo(RejectionReason.OutOfIsland));
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(6));
        }

        [Test]
        public void WrongCategoryAndUnknownWeapon_AreRejected()
        {
            var match = MatchBuilder.NewPlaying();
            Assert.That(
                match.Submit(Side.Blue, new FireMissileCommand(PrototypeArsenal.Drone, new HexCoord(1, 1))).Reason,
                Is.EqualTo(RejectionReason.WrongCategory));
            Assert.That(
                match.Submit(Side.Blue, new LaunchDroneCommand("no-existe", new HexCoord(1, 1))).Reason,
                Is.EqualTo(RejectionReason.UnknownWeapon));
        }
    }
}
