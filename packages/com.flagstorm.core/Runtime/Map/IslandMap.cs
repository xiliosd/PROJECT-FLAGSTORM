using System;
using System.Collections.Generic;
using System.Linq;

namespace Flagstorm.Core.Map
{
    public sealed class IslandMap
    {
        private readonly Dictionary<HexCoord, TerrainType> _cells;

        public IslandMap(IReadOnlyDictionary<HexCoord, TerrainType> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            _cells = cells.ToDictionary(pair => pair.Key, pair => pair.Value);
            var ordered = _cells.Keys.OrderBy(c => c.R).ThenBy(c => c.Q).ToList();
            CoastCells = ordered.Where(IsCoast).ToList();
            InteriorCells = ordered.Where(c => !IsCoast(c)).ToList();
        }

        public IReadOnlyDictionary<HexCoord, TerrainType> Cells => _cells;

        public IReadOnlyList<HexCoord> CoastCells { get; }

        public IReadOnlyList<HexCoord> InteriorCells { get; }

        public bool Contains(HexCoord cell) => _cells.ContainsKey(cell);

        public TerrainType TerrainAt(HexCoord cell)
        {
            if (!_cells.TryGetValue(cell, out var terrain))
            {
                throw new ArgumentOutOfRangeException(nameof(cell), cell, "La celda no es tierra de esta isla.");
            }

            return terrain;
        }

        // Costa = tierra con al menos un vecino de agua: es donde pueden desembarcar los robots.
        public bool IsCoast(HexCoord cell) => Contains(cell) && cell.Neighbors().Any(n => !Contains(n));
    }
}
