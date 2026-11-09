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
                // Sin robot volcánico hasta la región 3 de la campaña (spec §5).
                if (terrain == TerrainType.Volcano)
                {
                    continue;
                }

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
        public void Get_UnknownId_Throws() =>
            Assert.Throws<KeyNotFoundException>(() => PrototypeArsenal.Get("no-existe"));

        [Test]
        public void PathFinder_AcceptsRobotDefinition()
        {
            var robot = PrototypeArsenal.Get(PrototypeArsenal.TrackedRobot);
            var map = TestIslands.Small();
            var path = PathFinder.FindPath(map, robot, new HexCoord(0, 1), new HexCoord(1, 1));
            Assert.That(PathFinder.PathCost(map, robot, path), Is.EqualTo(1));
        }
    }
}
