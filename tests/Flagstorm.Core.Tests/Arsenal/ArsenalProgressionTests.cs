using System.Linq;
using Flagstorm.Core.Arsenal;

namespace Flagstorm.Core.Tests.Arsenal
{
    [TestFixture]
    public class ArsenalProgressionTests
    {
        [Test]
        public void Progression_StartsWithDroneShieldAndBeachPair() =>
            Assert.That(
                ArsenalProgression.UnlockedAfter(0).Select(w => w.Id),
                Is.EquivalentTo(new[] { "dron-explorador", "escudo-antidrones", "robot-anfibio", "defensor-anfibio" }));

        [Test]
        public void Progression_EachWinAddsTheNextLevel_AndEndsWithAll()
        {
            Assert.That(ArsenalProgression.UnlockedAfter(1).Select(w => w.Id), Does.Contain("antimisiles"));
            Assert.That(ArsenalProgression.UnlockedAfter(ArsenalProgression.MaxLevel).Count, Is.EqualTo(17));
            Assert.That(ArsenalProgression.UnlockedAfter(-3).Count, Is.EqualTo(4));
        }

        [TestCase("dron-explorador", 0)]
        [TestCase("escudo-antidrones", 0)]
        [TestCase("robot-anfibio", 0)]
        [TestCase("defensor-anfibio", 0)]
        [TestCase("misil-estandar", 1)]
        [TestCase("antimisiles", 1)]
        [TestCase("robot-trepador", 2)]
        [TestCase("defensor-trepador", 2)]
        [TestCase("robot-nieve", 3)]
        [TestCase("defensor-nieve", 3)]
        [TestCase("robot-orugas", 4)]
        [TestCase("defensor-orugas", 4)]
        [TestCase("robot-magma", 5)]
        [TestCase("defensor-magma", 5)]
        [TestCase("misil-incendiario", 6)]
        [TestCase("torreta", 6)]
        [TestCase("senuelo", 7)]
        public void UnlockLevel_MatchesTheTable(string weaponId, int expectedLevel) =>
            Assert.That(PrototypeArsenal.Get(weaponId).UnlockLevel, Is.EqualTo(expectedLevel));
    }
}
