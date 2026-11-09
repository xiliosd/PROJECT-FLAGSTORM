using System;
using System.Linq;
using Flagstorm.Core.Map;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Map
{
    [TestFixture]
    public class IslandLayoutParserTests
    {
        [Test]
        public void Parse_SmallIsland_MapsOddRowOffsetToAxial()
        {
            var map = IslandLayoutParser.Parse(TestIslands.SmallLayout);
            Assert.That(map.Cells, Has.Count.EqualTo(25));
            Assert.That(map.TerrainAt(new HexCoord(1, 1)), Is.EqualTo(TerrainType.Jungle));
            Assert.That(map.TerrainAt(new HexCoord(1, 2)), Is.EqualTo(TerrainType.Volcano));
            Assert.That(map.TerrainAt(new HexCoord(-2, 4)), Is.EqualTo(TerrainType.Beach));
            Assert.That(map.InteriorCells, Has.Count.EqualTo(9));
            Assert.That(map.IsCoast(new HexCoord(0, 1)), Is.True);
            Assert.That(map.IsCoast(new HexCoord(2, 2)), Is.False);
        }

        [Test]
        public void Parse_UnknownChar_ThrowsWithPosition()
        {
            var ex = Assert.Throws<FormatException>(() => IslandLayoutParser.Parse(new[] { "BB", "BX" }));
            Assert.That(ex.Message, Does.Contain("fila 1").And.Contain("columna 1"));
        }

        [Test]
        public void Parse_WaterIsNotLand()
        {
            var map = IslandLayoutParser.Parse(new[] { ".B", "B." });
            Assert.That(map.Contains(new HexCoord(0, 0)), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => map.TerrainAt(new HexCoord(0, 0)));
        }

        [Test]
        public void DefaultPrototypeIsland_HasAllTerrainsAndRoomToHide()
        {
            var map = PrototypeIslands.CreateDefault();
            Assert.That(map.Cells.Values.Distinct(), Is.EquivalentTo(Enum.GetValues(typeof(TerrainType))));
            Assert.That(map.InteriorCells, Has.Count.GreaterThanOrEqualTo(10));
        }
    }
}
