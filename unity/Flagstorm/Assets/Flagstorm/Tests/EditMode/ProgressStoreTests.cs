using NUnit.Framework;

namespace Flagstorm.Presentation.Tests
{
    public class ProgressStoreTests
    {
        [SetUp]
        public void ResetBefore() => new ProgressStore().Reset();

        [TearDown]
        public void ResetAfter() => new ProgressStore().Reset();

        [Test]
        public void StartsAtZero_AfterReset()
        {
            var store = new ProgressStore();
            store.RecordWin();
            store.Reset();

            Assert.That(store.Wins, Is.EqualTo(0));
        }

        [Test]
        public void RecordWin_Increments()
        {
            var store = new ProgressStore();

            store.RecordWin();
            store.RecordWin();

            Assert.That(store.Wins, Is.EqualTo(2));
        }

        [Test]
        public void Persists_AcrossInstances()
        {
            new ProgressStore().RecordWin();

            Assert.That(new ProgressStore().Wins, Is.EqualTo(1));
        }
    }
}
