using System;
using System.Collections.Generic;
using System.Globalization;

namespace Flagstorm.Core.Map
{
    // Coordenadas axiales para hexágonos "pointy-top". Struct escrito a mano (no `record struct`)
    // porque Unity 6 compila C# 9.
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        // El orden fijo de vecinos hace deterministas las búsquedas de ruta y la IA.
        private static readonly HexCoord[] NeighborOffsets =
        {
            new HexCoord(1, 0), new HexCoord(1, -1), new HexCoord(0, -1),
            new HexCoord(-1, 0), new HexCoord(-1, 1), new HexCoord(0, 1),
        };

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q { get; }

        public int R { get; }

        public static bool operator ==(HexCoord left, HexCoord right) => left.Equals(right);

        public static bool operator !=(HexCoord left, HexCoord right) => !left.Equals(right);

        public static IEnumerable<HexCoord> WithinRadius(HexCoord center, int radius)
        {
            for (var dr = -radius; dr <= radius; dr++)
            {
                var minDq = Math.Max(-radius, -dr - radius);
                var maxDq = Math.Min(radius, -dr + radius);
                for (var dq = minDq; dq <= maxDq; dq++)
                {
                    yield return new HexCoord(center.Q + dq, center.R + dr);
                }
            }
        }

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;

        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);

        public override int GetHashCode() => unchecked((Q * 397) ^ R);

        // Cultura invariante: la huella del estado debe ser igual en cualquier dispositivo (spec §11).
        public override string ToString() =>
            "(" + Q.ToString(CultureInfo.InvariantCulture) + "," + R.ToString(CultureInfo.InvariantCulture) + ")";

        public int DistanceTo(HexCoord other)
        {
            var dq = Q - other.Q;
            var dr = R - other.R;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
        }

        public IReadOnlyList<HexCoord> Neighbors()
        {
            var result = new HexCoord[NeighborOffsets.Length];
            for (var i = 0; i < NeighborOffsets.Length; i++)
            {
                result[i] = new HexCoord(Q + NeighborOffsets[i].Q, R + NeighborOffsets[i].R);
            }

            return result;
        }
    }
}
