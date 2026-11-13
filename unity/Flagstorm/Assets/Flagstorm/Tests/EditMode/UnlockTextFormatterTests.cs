using NUnit.Framework;

namespace Flagstorm.Presentation.Tests
{
    public class UnlockTextFormatterTests
    {
        [Test]
        public void FirstWin_UnlocksStandardMissileAndAntiMissile() =>
            Assert.That(UnlockTextFormatter.Format(0), Is.EqualTo("Nuevo: misil estándar y antimisiles"));

        [Test]
        public void SecondWin_UnlocksJungleRobotAndGuard() =>
            Assert.That(UnlockTextFormatter.Format(1), Is.EqualTo("Nuevo: robot trepador y defensor trepador"));

        [Test]
        public void AfterEverythingIsUnlocked_ReturnsEmpty() =>
            Assert.That(UnlockTextFormatter.Format(50), Is.Empty);
    }
}
