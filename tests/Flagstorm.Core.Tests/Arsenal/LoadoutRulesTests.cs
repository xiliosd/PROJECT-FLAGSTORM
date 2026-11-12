using System.Linq;
using Flagstorm.Core.Arsenal;

namespace Flagstorm.Core.Tests.Arsenal
{
    [TestFixture]
    public class LoadoutRulesTests
    {
        [Test]
        public void Validate_RejectsSevenWeapons()
        {
            var seven = ArsenalProgression.UnlockedAfter(ArsenalProgression.MaxLevel).Take(7).Select(w => w.Id).ToList();
            Assert.That(
                LoadoutRules.Validate(seven, ArsenalProgression.UnlockedAfter(ArsenalProgression.MaxLevel)),
                Is.EqualTo(LoadoutError.TooMany));
        }

        [Test]
        public void Validate_RejectsDuplicates() =>
            Assert.That(
                LoadoutRules.Validate(
                    new[] { PrototypeArsenal.BeachRobot, PrototypeArsenal.BeachRobot },
                    ArsenalProgression.UnlockedAfter(0)),
                Is.EqualTo(LoadoutError.Duplicate));

        [Test]
        public void Validate_RejectsLoadoutWithoutCaptureWeapon() =>
            Assert.That(
                LoadoutRules.Validate(
                    new[] { PrototypeArsenal.Drone, PrototypeArsenal.DroneShield }, ArsenalProgression.UnlockedAfter(0)),
                Is.EqualTo(LoadoutError.NoCaptureWeapon));

        [Test]
        public void Validate_RejectsLockedWeapon() =>
            Assert.That(
                LoadoutRules.Validate(
                    new[] { PrototypeArsenal.BeachRobot, PrototypeArsenal.AntiMissile },
                    ArsenalProgression.UnlockedAfter(0)),
                Is.EqualTo(LoadoutError.NotUnlocked));

        [Test]
        public void Validate_AcceptsStarterSet()
        {
            var starter = ArsenalProgression.UnlockedAfter(0);
            Assert.That(
                LoadoutRules.Validate(starter.Select(w => w.Id).ToList(), starter), Is.EqualTo(LoadoutError.None));
        }
    }
}
