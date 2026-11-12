using System;
using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Arsenal
{
    [TestFixture]
    public class WeaponDefinitionTests
    {
        [Test]
        public void IncendiaryMissile_UsesSpecTerrainModifiers()
        {
            var missile = PrototypeArsenal.Get(PrototypeArsenal.IncendiaryMissile);
            Assert.That(missile.ModifierPercentOn(TerrainType.Jungle), Is.EqualTo(40));
            Assert.That(missile.ModifierPercentOn(TerrainType.Ice), Is.EqualTo(-50));
            Assert.That(missile.EffectivePowerOn(TerrainType.Jungle), Is.EqualTo(4));
            Assert.That(missile.EffectivePowerOn(TerrainType.Ice), Is.EqualTo(2));
            Assert.That(missile.EffectivePowerOn(TerrainType.Rock), Is.EqualTo(3));
        }

        [Test]
        public void EveryTerrainHasAGoodRobot()
        {
            var robots = PrototypeArsenal.All.Where(w => w.Category == WeaponCategory.Capture).ToList();
            foreach (TerrainType terrain in Enum.GetValues(typeof(TerrainType)))
            {
                Assert.That(robots.Any(r => r.MovementCostOn(terrain) == 1), terrain.ToString());
            }
        }

        [Test]
        public void NoWeaponIsBestEverywhere()
        {
            var terrains = Enum.GetValues(typeof(TerrainType)).Cast<TerrainType>().ToList();
            var destroyers = PrototypeArsenal.All.Where(w => w.Category == WeaponCategory.Destroy).ToList();
            foreach (var weapon in destroyers)
            {
                var bestEverywhere = terrains.All(
                    t => destroyers.All(o => weapon.EffectivePowerOn(t) >= o.EffectivePowerOn(t)));
                Assert.That(bestEverywhere, Is.False, weapon.Id);
            }

            var robots = PrototypeArsenal.All.Where(w => w.Category == WeaponCategory.Capture).ToList();
            foreach (var robot in robots)
            {
                var bestEverywhere = terrains.All(
                    t => robots.All(o => robot.MovementCostOn(t) <= o.MovementCostOn(t)));
                Assert.That(bestEverywhere, Is.False, robot.Id);
            }
        }

        [Test]
        public void Catalog_HasSeventeenWeapons_WithUniqueIds()
        {
            Assert.That(PrototypeArsenal.All.Count, Is.EqualTo(17));
            Assert.That(PrototypeArsenal.All.Select(w => w.Id).Distinct().Count(), Is.EqualTo(17));
        }

        [Test]
        public void EveryAttackHasItsDefense()
        {
            Assert.That(PrototypeArsenal.Get(PrototypeArsenal.DroneShield).Intercepts, Is.EqualTo(WeaponCategory.Reveal));
            Assert.That(PrototypeArsenal.Get(PrototypeArsenal.AntiMissile).Intercepts, Is.EqualTo(WeaponCategory.Destroy));
            foreach (TerrainType terrain in Enum.GetValues(typeof(TerrainType)))
            {
                Assert.That(
                    PrototypeArsenal.All.Count(
                        w => w.Category == WeaponCategory.Guard && PrototypeArsenal.HomeTerrainOf(w.Id) == terrain),
                    Is.EqualTo(1),
                    terrain.ToString());
            }
        }

        [Test]
        public void Robot_IsStrongAtHomeAndWeakOnOpposite()
        {
            var snow = PrototypeArsenal.Get(PrototypeArsenal.SnowRobot);
            Assert.That(snow.EffectivePowerOn(TerrainType.Ice), Is.EqualTo(3));
            Assert.That(snow.EffectivePowerOn(TerrainType.Rock), Is.EqualTo(2));
            Assert.That(snow.EffectivePowerOn(TerrainType.Volcano), Is.EqualTo(1));
        }

        [Test]
        public void RobotsAndGuards_FollowTheTable()
        {
            var magma = PrototypeArsenal.Get(PrototypeArsenal.MagmaRobot);
            Assert.That(magma.MovementCostOn(TerrainType.Volcano), Is.EqualTo(1));
            Assert.That(magma.MovementCostOn(TerrainType.Ice), Is.EqualTo(2));
            var tracked = PrototypeArsenal.Get(PrototypeArsenal.TrackedRobot);
            Assert.That(tracked.MovementCostOn(TerrainType.Volcano), Is.EqualTo(3));
            Assert.That(tracked.EffectivePowerOn(TerrainType.Jungle), Is.EqualTo(1));
            var guard = PrototypeArsenal.Get(PrototypeArsenal.BeachGuard);
            Assert.That(guard.Radius, Is.EqualTo(2));
            Assert.That(guard.MovementPoints, Is.EqualTo(1));
            Assert.That(PrototypeArsenal.HomeTerrainOf(PrototypeArsenal.Turret), Is.Null);
        }

        [Test]
        public void Get_UnknownId_Throws() =>
            Assert.Throws<KeyNotFoundException>(() => PrototypeArsenal.Get("no-existe"));

        [Test]
        public void PathFinder_AcceptsRobotDefinition()
        {
            var robot = PrototypeArsenal.Get(PrototypeArsenal.TrackedRobot);
            var map = TestIslands.Small();
            var path = PathFinder.FindPath(map, robot, new HexCoord(0, 1), new HexCoord(1, 1));
            Assert.That(PathFinder.PathCost(map, robot, path), Is.EqualTo(2));
        }
    }
}
