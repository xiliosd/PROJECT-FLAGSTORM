using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Rules
{
    [TestFixture]
    public class InterceptionTests
    {
        private static readonly HexCoord ShieldCell = new HexCoord(2, 3);
        private static readonly HexCoord GuardCell = new HexCoord(1, 3);

        // Azul termina su turno y Rojo construye lo indicado; queda jugando Azul.
        private static Match MatchWithRedBuilt(params (string WeaponId, HexCoord Cell)[] builds) =>
            MatchWithRedBuilt(RulesConfig.Default, builds);

        // Con una sola bandera quedan libres celdas de selva, hielo y volcán para construir.
        private static Match MatchWithRedBuilt(RulesConfig rules, params (string WeaponId, HexCoord Cell)[] builds)
        {
            var match = MatchBuilder.NewPlaying(rules);
            match.Submit(Side.Blue, new EndTurnCommand());
            foreach (var (weaponId, cell) in builds)
            {
                var result = match.Submit(Side.Red, new BuildDefenseCommand(weaponId, cell));
                Assert.That(result.IsAccepted, Is.True, $"construir {weaponId}: {result.Reason}");
            }

            match.Submit(Side.Red, new EndTurnCommand());
            return match;
        }

        [Test]
        public void Drone_OverShield_IsShotDown_AndRevealsOnlyTheShield()
        {
            var match = MatchWithRedBuilt((PrototypeArsenal.DroneShield, ShieldCell));
            var energyBefore = match.EnergyOf(Side.Blue);
            var result = match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(2, 2)));

            Assert.That(result.IsAccepted, Is.True);
            var shot = result.Events.OfType<DroneShotDown>().Single();
            Assert.That(shot.Shield, Is.EqualTo(ShieldCell));
            Assert.That(shot.Target, Is.EqualTo(new HexCoord(2, 2)));
            Assert.That(result.Events.OfType<CellsRevealed>(), Is.Empty);
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(energyBefore - 2));
            var view = match.ViewFor(Side.Blue);
            Assert.That(view.RevealedEnemyCells, Is.EquivalentTo(new[] { ShieldCell }));
            Assert.That(view.KnownEnemyDefenses[ShieldCell].WeaponId, Is.EqualTo(PrototypeArsenal.DroneShield));
        }

        [Test]
        public void Drone_OutsideShieldRange_RevealsNormally()
        {
            var match = MatchWithRedBuilt((PrototypeArsenal.DroneShield, ShieldCell));
            var result = match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 1)));

            Assert.That(result.Events.OfType<DroneShotDown>(), Is.Empty);
            Assert.That(result.Events.OfType<CellsRevealed>().Single().Cells, Has.Count.EqualTo(7));
            Assert.That(match.ViewFor(Side.Blue).RevealedEnemyCells, Does.Not.Contain(ShieldCell));
        }

        [Test]
        public void Missile_OverAntiMissile_IsIntercepted_AndAntiMissileLosesOnePoint()
        {
            var match = MatchWithRedBuilt((PrototypeArsenal.AntiMissile, ShieldCell));
            var energyBefore = match.EnergyOf(Side.Blue);
            var result = match.Submit(
                Side.Blue, new FireMissileCommand(PrototypeArsenal.StandardMissile, new HexCoord(2, 2)));

            Assert.That(result.IsAccepted, Is.True);
            var intercepted = result.Events.OfType<MissileIntercepted>().Single();
            Assert.That(intercepted.AntiMissile, Is.EqualTo(ShieldCell));
            Assert.That(intercepted.RemainingHitPoints, Is.EqualTo(2));
            Assert.That(result.Events.OfType<MissileImpact>(), Is.Empty);
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(energyBefore - 3));
            Assert.That(match.ViewFor(Side.Red).OwnDefenses[ShieldCell].HitPoints, Is.EqualTo(2));
            Assert.That(match.ViewFor(Side.Blue).KnownEnemyDefenses.Keys, Does.Contain(ShieldCell));
        }

        [Test]
        public void AntiMissile_DestroyedAfterThreeInterceptions()
        {
            var match = MatchWithRedBuilt((PrototypeArsenal.AntiMissile, ShieldCell));
            var missile = new FireMissileCommand(PrototypeArsenal.StandardMissile, new HexCoord(2, 2));
            match.Submit(Side.Blue, missile);
            match.Submit(Side.Blue, missile);
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new EndTurnCommand());
            var third = match.Submit(Side.Blue, missile);

            Assert.That(third.Events.OfType<MissileIntercepted>().Single().RemainingHitPoints, Is.EqualTo(0));
            Assert.That(match.ViewFor(Side.Red).OwnDefenses.Keys, Does.Not.Contain(ShieldCell));

            var fourth = match.Submit(Side.Blue, missile);
            Assert.That(fourth.Events.OfType<MissileImpact>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Missile_DamagesShieldAndGuardInArea_ButNotOwnRobot()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.DroneShield, ShieldCell));
            match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.BeachGuard, GuardCell));
            match.Submit(Side.Red, new EndTurnCommand());
            var deploy = match.Submit(
                Side.Blue, new DeployRobotCommand(PrototypeArsenal.JungleRobot, new HexCoord(2, 4)));
            Assert.That(deploy.IsAccepted, Is.True, $"{deploy.Reason}");
            match.Submit(Side.Blue, new EndTurnCommand());
            match.Submit(Side.Red, new EndTurnCommand());

            var result = match.Submit(
                Side.Blue, new FireMissileCommand(PrototypeArsenal.StandardMissile, ShieldCell));

            Assert.That(result.Events.OfType<DefenseDestroyed>().Single().Cell, Is.EqualTo(ShieldCell));
            Assert.That(result.Events.OfType<GuardDamaged>().Single().RemainingHitPoints, Is.EqualTo(1));
            Assert.That(match.ViewFor(Side.Red).OwnGuards.Single().HitPoints, Is.EqualTo(1));
            Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().HitPoints, Is.EqualTo(4));
        }

        [Test]
        public void Missile_PowerOnGuardDependsOnTerrain()
        {
            var jungleCell = new HexCoord(2, 1);
            var rules = RulesConfig.Default with { FlagsPerSide = 1 };
            var standard = MatchWithRedBuilt(rules, (PrototypeArsenal.BeachGuard, jungleCell));
            var normal = standard.Submit(
                Side.Blue, new FireMissileCommand(PrototypeArsenal.StandardMissile, jungleCell));
            Assert.That(normal.Events.OfType<GuardDamaged>().Single().RemainingHitPoints, Is.EqualTo(1));

            var incendiary = MatchWithRedBuilt(rules, (PrototypeArsenal.BeachGuard, jungleCell));
            var burned = incendiary.Submit(
                Side.Blue, new FireMissileCommand(PrototypeArsenal.IncendiaryMissile, jungleCell));
            Assert.That(burned.Events.OfType<GuardDestroyed>().Single().Cell, Is.EqualTo(jungleCell));
            Assert.That(incendiary.ViewFor(Side.Red).OwnGuards, Is.Empty);
        }

        [Test]
        public void KnownEnemyDefenses_NeverIncludesDecoys()
        {
            var decoyCell = new HexCoord(2, 2);
            var rules = RulesConfig.Default with { FlagsPerSide = 1 };
            var match = MatchWithRedBuilt(rules, (PrototypeArsenal.Decoy, decoyCell));
            match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(2, 2)));

            var view = match.ViewFor(Side.Blue);
            Assert.That(view.EnemySignals, Does.Contain(decoyCell));
            Assert.That(view.KnownEnemyDefenses.Keys, Does.Not.Contain(decoyCell));
            Assert.That(view.KnownEnemyDefenses.Keys, Does.Contain(new HexCoord(3, 1)));
        }
    }
}
