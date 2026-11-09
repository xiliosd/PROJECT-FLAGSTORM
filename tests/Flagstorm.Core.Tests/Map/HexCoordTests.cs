using System.Linq;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Tests.Map
{
    [TestFixture]
    public class HexCoordTests
    {
        [Test]
        public void DistanceTo_IsHexDistance()
        {
            Assert.That(new HexCoord(0, 0).DistanceTo(new HexCoord(2, -1)), Is.EqualTo(2));
            Assert.That(new HexCoord(1, 1).DistanceTo(new HexCoord(-1, 3)), Is.EqualTo(2));
            Assert.That(new HexCoord(3, 0).DistanceTo(new HexCoord(3, 0)), Is.EqualTo(0));
        }

        [Test]
        public void Neighbors_AreSixInFixedOrder()
        {
            Assert.That(new HexCoord(1, 1).Neighbors(), Is.EqualTo(new[]
            {
                new HexCoord(2, 1), new HexCoord(2, 0), new HexCoord(1, 0),
                new HexCoord(0, 1), new HexCoord(0, 2), new HexCoord(1, 2),
            }));
        }

        [Test]
        public void WithinRadius_OneReturnsCenterAndNeighborsSorted()
        {
            var cells = HexCoord.WithinRadius(new HexCoord(0, 0), 1).ToList();
            Assert.That(cells, Has.Count.EqualTo(7));
            Assert.That(cells, Is.Ordered.By(nameof(HexCoord.R)).Then.By(nameof(HexCoord.Q)));
            Assert.That(HexCoord.WithinRadius(new HexCoord(5, 5), 0), Is.EqualTo(new[] { new HexCoord(5, 5) }));
        }
    }
}
