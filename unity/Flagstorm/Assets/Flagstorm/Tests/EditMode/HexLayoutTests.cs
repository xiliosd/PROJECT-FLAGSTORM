using Flagstorm.Core.Map;
using NUnit.Framework;
using UnityEngine;

namespace Flagstorm.Presentation.Tests
{
    public class HexLayoutTests
    {
        [Test]
        public void NeighborsAreEquidistant()
        {
            var center = HexLayout.ToWorld(new HexCoord(0, 0), 1f, Vector3.zero);
            foreach (var neighbor in new HexCoord(0, 0).Neighbors())
            {
                var distance = Vector3.Distance(center, HexLayout.ToWorld(neighbor, 1f, Vector3.zero));
                Assert.That(distance, Is.EqualTo(Mathf.Sqrt(3f)).Within(1e-4));
            }
        }

        [Test]
        public void FromWorld_InvertsToWorld_IncludingNearEdges()
        {
            var origin = new Vector3(2f, 0f, -5f);
            for (var q = -3; q <= 3; q++)
            {
                for (var r = -3; r <= 3; r++)
                {
                    var cell = new HexCoord(q, r);
                    var world = HexLayout.ToWorld(cell, 0.5f, origin) + new Vector3(0.2f, 0f, -0.15f);
                    Assert.That(HexLayout.FromWorld(world, 0.5f, origin), Is.EqualTo(cell), cell.ToString());
                }
            }
        }
    }
}
