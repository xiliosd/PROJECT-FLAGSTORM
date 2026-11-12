using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flagstorm.Presentation
{
    // Textos anclados a puntos del mundo (terreno y PV), dibujados en la capa del HUD para que se lean nítidos.
    public sealed class BoardLabels
    {
        private const int TerrainFontSize = 13;
        private const int ValueFontSize = 18;

        // Separación en unidades de panel: el terreno arriba del ícono y los PV debajo, sin taparlo.
        private const float TerrainOffset = 27f;
        private const float ValueOffset = 24f;

        private readonly VisualElement _layer;
        private readonly List<Anchored> _terrain = new List<Anchored>();
        private readonly List<Anchored> _values = new List<Anchored>();
        private bool _terrainVisible = true;

        public BoardLabels(VisualElement layer)
        {
            _layer = layer;
        }

        public void AddTerrainLabel(Vector3 world, string text)
        {
            var label = Create(text, TerrainFontSize, HoloPalette.TextColor);
            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            label.style.display = _terrainVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _terrain.Add(new Anchored(label, world));
        }

        public void ClearValues()
        {
            foreach (var value in _values)
            {
                value.Label.RemoveFromHierarchy();
            }

            _values.Clear();
        }

        // Todas las etiquetas llevan fondo oscuro para mantener el contraste sobre cualquier terreno.
        public void AddValueLabel(Vector3 world, string text, Color color)
        {
            var label = Create(text, ValueFontSize, color);
            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
            label.style.paddingLeft = 4;
            label.style.paddingRight = 4;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            _values.Add(new Anchored(label, world));
        }

        public void ToggleTerrain()
        {
            _terrainVisible = !_terrainVisible;
            foreach (var terrain in _terrain)
            {
                terrain.Label.style.display = _terrainVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void Reposition(Camera camera)
        {
            if (_layer.panel == null || camera == null)
            {
                return;
            }

            Place(_terrain, camera, -TerrainOffset);
            Place(_values, camera, ValueOffset);
        }

        private void Place(List<Anchored> items, Camera camera, float verticalOffset)
        {
            // Proyección por viewport: vale igual en pantalla y al dibujar en una textura (capturas).
            var area = _layer.layout.size;
            if (float.IsNaN(area.x) || area.x <= 0f)
            {
                return;
            }

            foreach (var item in items)
            {
                var viewport = camera.WorldToViewportPoint(item.World);
                var point = new Vector2(viewport.x * area.x, (1f - viewport.y) * area.y);

                var size = item.Label.layout.size;
                var width = float.IsNaN(size.x) ? 0f : size.x;
                var height = float.IsNaN(size.y) ? 0f : size.y;
                item.Label.style.left = point.x - (width / 2f);
                item.Label.style.top = point.y - (height / 2f) + verticalOffset;
            }
        }

        private Label Create(string text, int fontSize, Color color)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            label.style.fontSize = fontSize;
            label.style.color = color;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _layer.Add(label);
            return label;
        }

        private readonly struct Anchored
        {
            public Anchored(Label label, Vector3 world)
            {
                Label = label;
                World = world;
            }

            public Label Label { get; }

            public Vector3 World { get; }
        }
    }
}
