using Flagstorm.Core.Arsenal;
using UnityEngine;

namespace Flagstorm.Presentation
{
    // Mallas planas sobre el plano XZ, centradas en el origen y visibles desde arriba (Y+).
    public static class HexMeshFactory
    {
        private const int CircleSides = 20;

        // Hexágono "pointy-top": un vértice apunta hacia Z+, igual que HexLayout.
        public static Mesh CreateFlatHex(float size) => CreatePolygon("FlatHex", size, 6, -30f);

        public static Mesh CreateMarker(MarkerShape shape, float size)
        {
            switch (shape)
            {
                case MarkerShape.Diamond:
                    return CreatePolygon("Diamond", size, 4, 0f);
                case MarkerShape.Square:
                    return CreatePolygon("Square", size * 0.85f, 4, 45f);
                case MarkerShape.Flag:
                    return CreatePolygon("Flag", size, 3, 90f);
                case MarkerShape.Circle:
                    return CreatePolygon("Circle", size * 0.8f, CircleSides, 0f);
                case MarkerShape.HollowHexagon:
                    return CreateRing("HollowHexagon", size, size * 0.6f, 6, 0f);
                case MarkerShape.HollowTriangle:
                    return CreateRing("HollowTriangle", size, size * 0.5f, 3, 90f);
                case MarkerShape.Pentagon:
                    return CreatePolygon("Pentagon", size * 0.9f, 5, 90f);
                case MarkerShape.HollowDiamond:
                    return CreateRing("HollowDiamond", size, size * 0.6f, 4, 0f);
                default:
                    return CreateRing("Ring", size, size * 0.85f, CircleSides, 0f);
            }
        }

        public static Mesh CreatePolygon(string name, float radius, int sides, float rotationDegrees)
        {
            var vertices = new Vector3[sides + 1];
            vertices[0] = Vector3.zero;
            for (var i = 0; i < sides; i++)
            {
                vertices[i + 1] = PointOnCircle(radius, (360f * i / sides) + rotationDegrees);
            }

            var triangles = new int[sides * 3];
            for (var i = 0; i < sides; i++)
            {
                // Orden horario visto desde arriba: es la cara frontal en Unity.
                triangles[i * 3] = 0;
                triangles[(i * 3) + 1] = ((i + 1) % sides) + 1;
                triangles[(i * 3) + 2] = i + 1;
            }

            return Finish(name, vertices, triangles);
        }

        public static Mesh CreateRing(string name, float outer, float inner, int sides, float rotationDegrees)
        {
            var vertices = new Vector3[sides * 2];
            for (var i = 0; i < sides; i++)
            {
                var angle = (360f * i / sides) + rotationDegrees;
                vertices[i * 2] = PointOnCircle(outer, angle);
                vertices[(i * 2) + 1] = PointOnCircle(inner, angle);
            }

            var triangles = new int[sides * 6];
            for (var i = 0; i < sides; i++)
            {
                var outerNow = i * 2;
                var innerNow = outerNow + 1;
                var outerNext = ((i + 1) % sides) * 2;
                var innerNext = outerNext + 1;
                var t = i * 6;
                triangles[t] = outerNow;
                triangles[t + 1] = innerNow;
                triangles[t + 2] = innerNext;
                triangles[t + 3] = outerNow;
                triangles[t + 4] = innerNext;
                triangles[t + 5] = outerNext;
            }

            return Finish(name, vertices, triangles);
        }

        private static Vector3 PointOnCircle(float radius, float degrees)
        {
            var angle = Mathf.Deg2Rad * degrees;
            return new Vector3(radius * Mathf.Cos(angle), 0f, radius * Mathf.Sin(angle));
        }

        private static Mesh Finish(string name, Vector3[] vertices, int[] triangles)
        {
            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    // Cada elemento del tablero tiene una forma propia para no depender solo del color (WCAG 1.4.1).
    public enum MarkerShape
    {
        Diamond,
        Square,
        Flag,
        Circle,
        HollowDiamond,
        HollowHexagon,
        HollowTriangle,
        Pentagon,
        Ring,
    }

    // Forma de cada defensa en el tablero: escudo, antimisiles, torreta, señuelo y defensores se distinguen sin color.
    public static class MarkerShapes
    {
        public static MarkerShape ForWeapon(string weaponId)
        {
            if (weaponId == PrototypeArsenal.Decoy)
            {
                return MarkerShape.HollowDiamond;
            }

            var weapon = PrototypeArsenal.Get(weaponId);
            if (weapon.Category == WeaponCategory.Guard)
            {
                return MarkerShape.Pentagon;
            }

            switch (weapon.Intercepts)
            {
                case WeaponCategory.Reveal:
                    return MarkerShape.HollowHexagon;
                case WeaponCategory.Destroy:
                    return MarkerShape.HollowTriangle;
                default:
                    return MarkerShape.Square;
            }
        }

        // Escudos, antimisiles y defensores cubren una zona; se tiñe para mostrar hasta dónde llegan.
        public static bool HasZone(WeaponDefinition weapon) =>
            weapon.Intercepts.HasValue || weapon.Category == WeaponCategory.Guard;
    }
}
