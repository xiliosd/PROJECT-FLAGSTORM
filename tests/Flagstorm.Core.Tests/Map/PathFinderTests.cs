using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Map;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Map
{
    [TestFixture]
    public class PathFinderTests
    {
        [Test]
        public void FindPath_PrefersCheaperDetour()
        {
            var map = TestIslands.Small();
            var path = PathFinder.FindPath(map, Cost, new HexCoord(0, 2), new HexCoord(2, 2));
            Assert.That(path.Last(), Is.EqualTo(new HexCoord(2, 2)));
            Assert.That(path, Does.Not.Contain(new HexCoord(1, 2)));
            Assert.That(PathFinder.PathCost(map, Cost, path), Is.EqualTo(3));
        }

        [Test]
        public void FindPath_UnreachableOrOutside_ReturnsNull()
        {
            var map = TestIslands.Small();
            Assert.That(PathFinder.FindPath(map, Cost, new HexCoord(0, 2), new HexCoord(10, 10)), Is.Null);
        }

        [Test]
        public void FindPath_WithBlockedCells_GoesAroundThem()
        {
            var map = TestIslands.Small();
            var blocked = new HashSet<HexCoord> { new HexCoord(1, 2) };
            var path = PathFinder.FindPath(map, Cost, new HexCoord(0, 2), new HexCoord(2, 2), blocked);
            Assert.That(path.Last(), Is.EqualTo(new HexCoord(2, 2)));
            Assert.That(path, Does.Not.Contain(new HexCoord(1, 2)));
        }

        [Test]
        public void FindPath_WithBlockedTarget_ReturnsNull()
        {
            var map = TestIslands.Small();
            var blocked = new HashSet<HexCoord> { new HexCoord(2, 2) };
            Assert.That(PathFinder.FindPath(map, Cost, new HexCoord(0, 2), new HexCoord(2, 2), blocked), Is.Null);
        }

        [Test]
        public void TruncateToBudget_StopsBeforeExceeding()
        {
            var map = TestIslands.Small();
            var path = new[] { new HexCoord(1, 1), new HexCoord(1, 2) };
            Assert.That(PathFinder.TruncateToBudget(map, Cost, path, 3), Is.EqualTo(new[] { new HexCoord(1, 1) }));
        }

        private static int Cost(TerrainType terrain) => terrain switch
        {
            TerrainType.Volcano => 3,
            TerrainType.Jungle => 2,
            _ => 1,
        };
    }
}
