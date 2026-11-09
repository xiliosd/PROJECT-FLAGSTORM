using System.Collections.Generic;

namespace Flagstorm.Core.Map
{
    // Orden canónico de celdas (por R y luego por Q) para que toda salida sea determinista.
    public sealed class CellOrder : IComparer<HexCoord>
    {
        public static readonly CellOrder Instance = new CellOrder();

        public int Compare(HexCoord x, HexCoord y)
        {
            var byRow = x.R.CompareTo(y.R);
            return byRow != 0 ? byRow : x.Q.CompareTo(y.Q);
        }
    }
}
