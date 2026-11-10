using Flagstorm.Core.Map;
using UnityEngine;

namespace Flagstorm.Presentation
{
    // Conversión entre coordenadas axiales "pointy-top" y el plano XZ del mundo (Y arriba).
    public static class HexLayout
    {
        private static readonly float Sqrt3 = Mathf.Sqrt(3f);

        public static Vector3 ToWorld(HexCoord cell, float size, Vector3 origin)
        {
            var x = size * Sqrt3 * (cell.Q + (cell.R / 2f));
            var z = -size * 1.5f * cell.R;
            return origin + new Vector3(x, 0f, z);
        }

        public static HexCoord FromWorld(Vector3 world, float size, Vector3 origin)
        {
            var local = world - origin;
            var r = -local.z / (1.5f * size);
            var q = (local.x / (Sqrt3 * size)) - (r / 2f);
            return Round(q, r);
        }

        // Redondeo en coordenadas cúbicas: corrige la componente con mayor error.
        private static HexCoord Round(float q, float r)
        {
            var s = -q - r;
            var rq = Mathf.Round(q);
            var rr = Mathf.Round(r);
            var rs = Mathf.Round(s);
            var dq = Mathf.Abs(rq - q);
            var dr = Mathf.Abs(rr - r);
            var ds = Mathf.Abs(rs - s);
            if (dq > dr && dq > ds)
            {
                rq = -rr - rs;
            }
            else if (dr > ds)
            {
                rr = -rq - rs;
            }

            return new HexCoord((int)rq, (int)rr);
        }
    }
}
