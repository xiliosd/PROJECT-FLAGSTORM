using System.Collections;
using System.Linq;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Flagstorm.Presentation
{
    // Orquesta la escena de prototipo: crea las islas, refleja el estado del núcleo y pasa los toques a la sesión.
    [RequireComponent(typeof(UIDocument), typeof(CellPicker))]
    public sealed class MatchController : MonoBehaviour
    {
        // Pausa entre jugadas de la IA para que el jugador pueda seguir lo que hace.
        private const float AiStepSeconds = 0.6f;

        // Las celdas rivales sin explorar se ven tenues (25 %) sobre el océano.
        private const float FogVisibility = 0.25f;

        // Fracción de pantalla que ocupan los paneles del HUD arriba y abajo.
        private const float HudTopFraction = 0.2f;
        private const float HudBottomFraction = 0.34f;

        private const float CameraTiltDegrees = 80f;
        private const float CameraDistance = 60f;
        private const float IslandGap = 2.5f;

        [SerializeField]
        private Camera worldCamera;

        [SerializeField]
        private Material cellMaterial;

        [SerializeField]
        private float cellSize = 1f;

        [Tooltip("0 = semilla distinta en cada partida.")]
        [SerializeField]
        private uint seed;

        private MatchSession _session;
        private UIDocument _document;
        private HudView _hud;
        private BoardLabels _labels;
        private IslandRenderer _ownIsland;
        private IslandRenderer _enemyIsland;
        private HexCoord? _hoveredEnemyCell;
        private bool _aiRunning;
        private float _fittedAspect;

        private void Start()
        {
            var matchSeed = seed != 0 ? seed : (uint)System.Environment.TickCount | 1u;
            _session = MatchSession.CreateDefault(matchSeed);

            var view = _session.View;
            var islandDepth = IslandDepth(view.OwnIsland);
            var offset = (islandDepth + IslandGap) / 2f;
            _ownIsland = CreateIsland(
                "Isla propia", view.OwnIsland, new Vector3(0f, 0f, -offset), HoloPalette.OwnColor);
            _enemyIsland = CreateIsland(
                "Isla rival", view.EnemyIsland, new Vector3(0f, 0f, offset), HoloPalette.EnemyColor);

            _document = GetComponent<UIDocument>();
            _hud = new HudView(_document.rootVisualElement, _session.Arsenal);
            _hud.WeaponSelected += OnWeaponSelected;
            _hud.ConfirmSetupClicked += ConfirmSetup;
            _hud.EndTurnClicked += EndTurn;
            _hud.RestartClicked += () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            _labels = new BoardLabels(_hud.Board);
            _hud.LabelsToggled += _labels.ToggleTerrain;
            AddTerrainLabels(_ownIsland);
            AddTerrainLabels(_enemyIsland);

            var picker = GetComponent<CellPicker>();
            picker.Configure(worldCamera, IsOverHud, _ownIsland, _enemyIsland);
            picker.CellTapped += OnCellTapped;
            picker.HoverChanged += OnHoverChanged;

            _hud.AddLog("El rival escondió sus banderas");
            Refresh();
        }

        // Accesos internos para la prueba de humo en modo de juego (InternalsVisibleTo).
        internal MatchSession Session => _session;

        internal IslandRenderer OwnIsland => _ownIsland;

        internal IslandRenderer EnemyIsland => _enemyIsland;

        internal void ConfirmSetup() => Apply(_session.ConfirmSetup());

        internal void EndTurn() => Apply(_session.EndTurn());

        internal void OnWeaponSelected(string weaponId)
        {
            _session.SelectWeapon(weaponId);
            Refresh();
        }

        internal void OnCellTapped(IslandRenderer island, HexCoord cell)
        {
            var result = island == _ownIsland ? _session.TapOwnCell(cell) : _session.TapEnemyCell(cell);
            Apply(result);
        }

        private void Update()
        {
            _hud?.Tick();
            // Reajustar si cambia la proporción: rotación del teléfono o ventana del editor.
            if (worldCamera != null && !Mathf.Approximately(worldCamera.aspect, _fittedAspect))
            {
                _fittedAspect = worldCamera.aspect;
                FitCamera();
            }

            if (_session != null && _session.IsAiTurn && !_aiRunning)
            {
                StartCoroutine(PlayAiTurn());
            }
        }

        private void LateUpdate() => _labels?.Reposition(worldCamera);

        private IslandRenderer CreateIsland(string label, IslandMap island, Vector3 center, Color sideColor)
        {
            var islandRenderer = new GameObject(label).AddComponent<IslandRenderer>();
            islandRenderer.transform.SetParent(transform, false);
            islandRenderer.Build(island, center, cellSize, sideColor, cellMaterial);
            return islandRenderer;
        }

        private float IslandDepth(IslandMap island)
        {
            var rows = island.Cells.Keys.Select(c => c.R).ToList();
            return ((rows.Max() - rows.Min()) * 1.5f * cellSize) + (2f * cellSize);
        }

        private void AddTerrainLabels(IslandRenderer island)
        {
            foreach (var cell in island.Island.Cells)
            {
                _labels.AddTerrainLabel(island.WorldOf(cell.Key), HoloPalette.TerrainLabel(cell.Value));
            }
        }

        // Ajusta la cámara ortográfica para que ambas islas quepan entre los paneles del HUD.
        private void FitCamera()
        {
            if (worldCamera == null || _ownIsland == null)
            {
                return;
            }

            var own = _ownIsland.Footprint;
            var enemy = _enemyIsland.Footprint;
            var width = Mathf.Max(own.width, enemy.width) * 1.08f;
            var tilt = Mathf.Sin(CameraTiltDegrees * Mathf.Deg2Rad);
            var depth = (enemy.yMax - own.yMin) * tilt * 1.05f;
            var freeHeight = 1f - HudTopFraction - HudBottomFraction;
            var aspect = Mathf.Max(0.1f, worldCamera.aspect);
            var size = Mathf.Max(width / (2f * aspect), depth / (2f * freeHeight));
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = size;

            // Desplaza el centro de la vista para que las islas queden en el hueco libre entre los paneles.
            var freeCenter = HudBottomFraction + (freeHeight / 2f);
            var shift = (freeCenter - 0.5f) * 2f * size / tilt;
            var focus = new Vector3(0f, 0f, ((own.yMin + enemy.yMax) / 2f) - shift);
            var rotation = Quaternion.Euler(CameraTiltDegrees, 0f, 0f);
            var position = focus - (rotation * Vector3.forward * CameraDistance);
            worldCamera.transform.SetPositionAndRotation(position, rotation);
            worldCamera.backgroundColor = HoloPalette.Ocean;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private bool IsOverHud(Vector2 screenPosition)
        {
            var panel = _document.rootVisualElement.panel;
            if (panel == null)
            {
                return false;
            }

            var flipped = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return _hud.IsOverControl(RuntimePanelUtils.ScreenToPanel(panel, flipped));
        }

        private void OnHoverChanged(IslandRenderer island, HexCoord cell)
        {
            _hoveredEnemyCell = island == _enemyIsland ? cell : (HexCoord?)null;
            RefreshWeapons(_session.View);
        }

        private IEnumerator PlayAiTurn()
        {
            _aiRunning = true;
            Refresh();
            while (_session.IsAiTurn)
            {
                yield return new WaitForSeconds(AiStepSeconds);
                Apply(_session.StepAi());
            }

            _aiRunning = false;
            Refresh();
        }

        // Muestra los eventos o el motivo del rechazo, y redibuja; null significa que solo cambió la selección.
        private void Apply(CommandResult result)
        {
            if (result != null)
            {
                if (!result.IsAccepted)
                {
                    _hud.ShowToast(RejectionTextFormatter.Format(result.Reason));
                }

                foreach (var matchEvent in result.Events)
                {
                    _hud.AddLog(EventTextFormatter.Format(
                        matchEvent, MatchSession.HumanSide, _session.Match.Rules.FlagsPerSide));
                }
            }

            Refresh();
        }

        private void Refresh()
        {
            var view = _session.View;
            var match = _session.Match;
            var rules = match.Rules;
            var setup = match.Phase == MatchPhase.Setup;

            _hud.SetSetupMode(setup);
            _hud.SetInteractive(_session.IsHumanTurn);
            _hud.SetEnergy(view.Energy, rules.MaxEnergy);
            var flagsLeft = rules.FlagsPerSide - view.FlagsLostByViewer;
            _hud.SetFlags(view.FlagsCapturedByViewer, flagsLeft, rules.FlagsPerSide);
            _hud.SetStatus(StatusText(match));
            _hud.SetHint(HintTextFormatter.Format(_session));
            RefreshWeapons(view);

            _labels.ClearValues();
            DrawOwnIsland(view, setup);
            DrawEnemyIsland(view);

            if (match.Phase == MatchPhase.Finished)
            {
                _hud.ShowEnd(match.Winner == MatchSession.HumanSide ? "¡Victoria!" : "Derrota");
            }
        }

        private string StatusText(Match match)
        {
            switch (match.Phase)
            {
                case MatchPhase.Setup:
                    return "Preparación";
                case MatchPhase.Finished:
                    return "Partida terminada";
                default:
                    var whose = _session.IsHumanTurn ? "Tu turno" : "Turno del rival";
                    return $"Turno {match.TurnNumber} · {whose}";
            }
        }

        private void RefreshWeapons(PlayerView view)
        {
            var hovered = _hoveredEnemyCell.HasValue && view.EnemyIsland.Contains(_hoveredEnemyCell.Value)
                ? view.EnemyIsland.TerrainAt(_hoveredEnemyCell.Value)
                : (TerrainType?)null;
            foreach (var weapon in _session.Arsenal)
            {
                var modifier = hovered.HasValue
                    ? ModifierLabelFormatter.Format(weapon.ModifierPercentOn(hovered.Value))
                    : string.Empty;
                var affordable = _session.IsHumanTurn && weapon.EnergyCost <= view.Energy;
                _hud.SetWeaponState(weapon, weapon.Id == _session.SelectedWeaponId, affordable, modifier);
            }
        }

        private void DrawOwnIsland(PlayerView view, bool setup)
        {
            var island = _ownIsland;
            island.ClearMarkers();
            foreach (var cell in view.OwnIsland.Cells)
            {
                island.SetCellColor(cell.Key, HoloPalette.TerrainColor(cell.Value));
            }

            var own = HoloPalette.OwnColor;
            var flags = setup ? _session.PendingFlags : view.OwnFlags;
            foreach (var flag in flags)
            {
                island.AddMarker(flag, MarkerShape.Flag, own);
            }

            if (setup)
            {
                foreach (var turret in _session.PendingTurrets)
                {
                    island.AddMarker(turret, MarkerShape.Square, own);
                }

                return;
            }

            foreach (var turret in view.OwnTurrets)
            {
                island.AddMarker(turret.Key, MarkerShape.Square, own);
                _labels.AddValueLabel(island.WorldOf(turret.Key), $"{turret.Value} PV", own);
            }

            foreach (var decoy in view.OwnDecoys)
            {
                island.AddMarker(decoy, MarkerShape.HollowDiamond, own);
            }

            foreach (var robot in view.EnemyRobotsOnOwnIsland)
            {
                island.AddMarker(robot.Position, MarkerShape.Circle, HoloPalette.EnemyColor);
                _labels.AddValueLabel(
                    island.WorldOf(robot.Position), $"{robot.HitPoints} PV", HoloPalette.EnemyColor);
            }
        }

        private void DrawEnemyIsland(PlayerView view)
        {
            var island = _enemyIsland;
            island.ClearMarkers();
            var revealed = view.RevealedEnemyCells;
            foreach (var cell in view.EnemyIsland.Cells)
            {
                var terrain = HoloPalette.TerrainColor(cell.Value);
                var visible = revealed.Contains(cell.Key);
                var fogged = Color.Lerp(HoloPalette.Ocean, terrain, FogVisibility);
                island.SetCellColor(cell.Key, visible ? terrain : fogged);
            }

            var enemy = HoloPalette.EnemyColor;
            foreach (var signal in view.EnemySignals)
            {
                island.AddMarker(signal, MarkerShape.Diamond, enemy);
            }

            foreach (var turret in view.KnownEnemyTurrets)
            {
                island.AddMarker(turret.Key, MarkerShape.Square, enemy);
                _labels.AddValueLabel(island.WorldOf(turret.Key), $"{turret.Value} PV", enemy);
            }

            foreach (var robot in view.OwnRobots)
            {
                island.AddMarker(robot.Position, MarkerShape.Circle, HoloPalette.OwnColor);
                _labels.AddValueLabel(
                    island.WorldOf(robot.Position), $"{robot.HitPoints} PV", HoloPalette.OwnColor);
                if (robot.RobotId == _session.SelectedRobotId)
                {
                    island.AddMarker(robot.Position, MarkerShape.Ring, HoloPalette.TextColor);
                }
            }
        }
    }
}
