using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Map;
using UnityEngine;

namespace Flagstorm.Presentation
{
    // Dibuja una isla como hexágonos planos: un borde del color del bando y un relleno del color del terreno.
    public sealed class IslandRenderer : MonoBehaviour
    {
        private const float FillScale = 0.86f;
        private const float OutlineScale = 0.98f;
        private const float MarkerScale = 0.42f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly Dictionary<HexCoord, MeshRenderer> _fills = new Dictionary<HexCoord, MeshRenderer>();
        private readonly Dictionary<MarkerShape, Mesh> _markerMeshes = new Dictionary<MarkerShape, Mesh>();
        private readonly List<GameObject> _markers = new List<GameObject>();
        private MaterialPropertyBlock _block;
        private Material _material;

        public IslandMap Island { get; private set; }

        public Vector3 Origin { get; private set; }

        public float CellSize { get; private set; }

        // Rectángulo que ocupa la isla en el plano XZ (x = ancho, y = profundidad en Z).
        public Rect Footprint { get; private set; }

        public void Build(IslandMap island, Vector3 center, float cellSize, Color sideColor, Material material)
        {
            Island = island;
            CellSize = cellSize;
            _material = material;
            _block = new MaterialPropertyBlock();

            // Centrar la isla: el origen axial no coincide con el centro visual.
            var average = island.Cells.Keys
                .Select(c => HexLayout.ToWorld(c, cellSize, Vector3.zero))
                .Aggregate(Vector3.zero, (sum, p) => sum + p) / island.Cells.Count;
            Origin = center - average;
            Footprint = ComputeFootprint();

            var outlineColor = Color.Lerp(HoloPalette.Ocean, sideColor, 0.8f);
            var outlineMesh = HexMeshFactory.CreateFlatHex(cellSize * OutlineScale);
            var fillMesh = HexMeshFactory.CreateFlatHex(cellSize * FillScale);
            foreach (var cell in island.Cells.Keys.OrderBy(c => c, CellOrder.Instance))
            {
                var position = WorldOf(cell);
                CreatePiece($"Borde {cell}", outlineMesh, position, outlineColor);
                var fill = CreatePiece($"Celda {cell}", fillMesh, position + (Vector3.up * 0.01f), Color.black);
                _fills[cell] = fill;
            }
        }

        public void SetCellColor(HexCoord cell, Color color)
        {
            if (_fills.TryGetValue(cell, out var fill))
            {
                _block.SetColor(BaseColorId, color);
                fill.SetPropertyBlock(_block);
            }
        }

        public void ClearMarkers()
        {
            foreach (var marker in _markers)
            {
                Destroy(marker);
            }

            _markers.Clear();
        }

        public void AddMarker(HexCoord cell, MarkerShape shape, Color color)
        {
            if (!_markerMeshes.TryGetValue(shape, out var mesh))
            {
                mesh = HexMeshFactory.CreateMarker(shape, CellSize * MarkerScale);
                _markerMeshes[shape] = mesh;
            }

            // La altura según la forma evita que un anillo de selección quede tapado por el ícono.
            var lift = 0.02f + (0.01f * (int)shape);
            var marker = CreatePiece($"{shape} {cell}", mesh, WorldOf(cell) + (Vector3.up * lift), color);
            _markers.Add(marker.gameObject);
        }

        public Vector3 WorldOf(HexCoord cell) => HexLayout.ToWorld(cell, CellSize, Origin);

        public bool TryGetCell(Vector3 world, out HexCoord cell)
        {
            cell = HexLayout.FromWorld(world, CellSize, Origin);
            return Island != null && Island.Contains(cell);
        }

        private Rect ComputeFootprint()
        {
            var points = Island.Cells.Keys.Select(WorldOf).ToList();
            var halfWidth = CellSize * Mathf.Sqrt(3f) / 2f;
            var minX = points.Min(p => p.x) - halfWidth;
            var maxX = points.Max(p => p.x) + halfWidth;
            var minZ = points.Min(p => p.z) - CellSize;
            var maxZ = points.Max(p => p.z) + CellSize;
            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        private MeshRenderer CreatePiece(string label, Mesh mesh, Vector3 position, Color color)
        {
            var piece = new GameObject(label);
            piece.transform.SetParent(transform, false);
            piece.transform.position = position;
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = piece.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            _block.SetColor(BaseColorId, color);
            meshRenderer.SetPropertyBlock(_block);
            return meshRenderer;
        }
    }
}
