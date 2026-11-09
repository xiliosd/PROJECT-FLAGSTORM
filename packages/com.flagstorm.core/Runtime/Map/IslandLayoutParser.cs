using System;
using System.Collections.Generic;

namespace Flagstorm.Core.Map
{
    // Las islas se definen como datos (spec §5) para agregar islas nuevas sin reprogramar.
    public static class IslandLayoutParser
    {
        public static IslandMap Parse(IReadOnlyList<string> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            var cells = new Dictionary<HexCoord, TerrainType>();
            for (var row = 0; row < rows.Count; row++)
            {
                var line = rows[row] ?? string.Empty;
                for (var col = 0; col < line.Length; col++)
                {
                    var symbol = line[col];
                    if (symbol == '.')
                    {
                        continue;
                    }

                    // Offset "odd-r" a axial.
                    var q = col - ((row - (row & 1)) / 2);
                    cells[new HexCoord(q, row)] = ToTerrain(symbol, row, col);
                }
            }

            return new IslandMap(cells);
        }

        private static TerrainType ToTerrain(char symbol, int row, int col)
        {
            switch (symbol)
            {
                case 'B': return TerrainType.Beach;
                case 'J': return TerrainType.Jungle;
                case 'I': return TerrainType.Ice;
                case 'V': return TerrainType.Volcano;
                case 'R': return TerrainType.Rock;
                default:
                    throw new FormatException(
                        $"Carácter de terreno desconocido '{symbol}' en fila {row}, columna {col}.");
            }
        }
    }
}
