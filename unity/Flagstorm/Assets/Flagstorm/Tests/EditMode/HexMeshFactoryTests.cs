using System;
using System.Linq;
using Flagstorm.Core.Arsenal;
using NUnit.Framework;
using UnityEngine;

namespace Flagstorm.Presentation.Tests
{
    public class HexMeshFactoryTests
    {
        [Test]
        public void EveryShapeFacesUp()
        {
            AssertFacesUp(HexMeshFactory.CreateFlatHex(1f));
            foreach (MarkerShape shape in Enum.GetValues(typeof(MarkerShape)))
            {
                AssertFacesUp(HexMeshFactory.CreateMarker(shape, 1f));
            }
        }

        [Test]
        public void EveryDefenseKindHasItsOwnShape()
        {
            var shapes = PrototypeArsenal.All
                .Where(w => w.Category == WeaponCategory.Defend)
                .Select(w => MarkerShapes.ForWeapon(w.Id))
                .ToList();

            Assert.That(shapes, Is.Unique);
            Assert.That(MarkerShapes.ForWeapon(PrototypeArsenal.DroneShield), Is.EqualTo(MarkerShape.HollowHexagon));
            Assert.That(MarkerShapes.ForWeapon(PrototypeArsenal.AntiMissile), Is.EqualTo(MarkerShape.HollowTriangle));
            Assert.That(MarkerShapes.ForWeapon(PrototypeArsenal.BeachGuard), Is.EqualTo(MarkerShape.Pentagon));
            Assert.That(MarkerShapes.ForWeapon(PrototypeArsenal.MagmaGuard), Is.EqualTo(MarkerShape.Pentagon));
        }

        [Test]
        public void FlatHex_HasAVertexPointingToPositiveZ()
        {
            var mesh = HexMeshFactory.CreateFlatHex(1f);

            Assert.That(mesh.bounds.max.z, Is.EqualTo(1f).Within(1e-4));
            Assert.That(mesh.bounds.max.x, Is.EqualTo(Mathf.Sqrt(3f) / 2f).Within(1e-4));
        }

        // Unity muestra la cara frontal cuando los triángulos van en sentido horario vistos desde la cámara.
        private static void AssertFacesUp(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a);
                Assert.That(normal.y, Is.GreaterThan(0f), $"{mesh.name}, triángulo {i / 3}");
            }
        }
    }
}
