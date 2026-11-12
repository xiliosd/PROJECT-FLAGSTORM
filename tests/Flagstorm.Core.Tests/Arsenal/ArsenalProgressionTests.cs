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
    }
}
