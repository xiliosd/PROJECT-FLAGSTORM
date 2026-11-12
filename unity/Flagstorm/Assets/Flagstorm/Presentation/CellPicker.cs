using System;
using Flagstorm.Core.Map;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Flagstorm.Presentation
{
    // Convierte el puntero (ratón o dedo) en casillas: un toque corto elige; pasar o mantener el dedo apunta.
    public sealed class CellPicker : MonoBehaviour
    {
        // Un toque que se desplaza más que esto (en píxeles) se ignora: el jugador estaba arrastrando.
        private const float MaxTapTravel = 40f;

        private static readonly Plane Board = new Plane(Vector3.up, Vector3.zero);

        private Camera _camera;
        private IslandRenderer[] _islands = new IslandRenderer[0];
        private Func<Vector2, bool> _isOverHud;
        private Vector2 _pressPosition;
        private bool _pressStartedOnBoard;
        private IslandRenderer _hoveredIsland;
        private HexCoord _hoveredCell;

        public event Action<IslandRenderer, HexCoord> CellTapped;

        // Island nula cuando el puntero no está sobre ninguna casilla.
        public event Action<IslandRenderer, HexCoord> HoverChanged;

        public void Configure(Camera worldCamera, Func<Vector2, bool> isOverHud, params IslandRenderer[] islands)
        {
            _camera = worldCamera;
            _isOverHud = isOverHud;
            _islands = islands;
        }

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || _camera == null)
            {
                return;
            }

            var position = pointer.position.ReadValue();
            var overHud = _isOverHud != null && _isOverHud(position);

            if (pointer.press.wasPressedThisFrame)
            {
                _pressPosition = position;
                _pressStartedOnBoard = !overHud;
            }

            // En pantallas táctiles solo hay "hover" mientras el dedo está apoyado.
            var canHover = !(pointer is Touchscreen) || pointer.press.isPressed;
            UpdateHover(canHover && !overHud ? Pick(position) : default);

            if (pointer.press.wasReleasedThisFrame && _pressStartedOnBoard && !overHud
                && Vector2.Distance(position, _pressPosition) <= MaxTapTravel)
            {
                var hit = Pick(position);
                if (hit.Island != null)
                {
                    CellTapped?.Invoke(hit.Island, hit.Cell);
                }
            }
        }

        private void UpdateHover((IslandRenderer Island, HexCoord Cell) hit)
        {
            if (hit.Island == _hoveredIsland && (hit.Island == null || hit.Cell == _hoveredCell))
            {
                return;
            }

            _hoveredIsland = hit.Island;
            _hoveredCell = hit.Cell;
            HoverChanged?.Invoke(hit.Island, hit.Cell);
        }

        private (IslandRenderer Island, HexCoord Cell) Pick(Vector2 screenPosition)
        {
            var ray = _camera.ScreenPointToRay(screenPosition);
            if (!Board.Raycast(ray, out var distance))
            {
                return default;
            }

            var world = ray.GetPoint(distance);
            foreach (var island in _islands)
            {
                if (island.TryGetCell(world, out var cell))
                {
                    return (island, cell);
                }
            }

            return default;
        }
    }
}
