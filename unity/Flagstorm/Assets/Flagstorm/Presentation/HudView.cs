using System;
using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flagstorm.Presentation
{
    // HUD construido en código con UI Toolkit (sin UXML): textos visibles, botones grandes y contraste alto.
    public sealed class HudView
    {
        private const float ButtonMinHeight = 120f;
        private const int MaxLogLines = 6;

        private readonly VisualElement _root;
        private readonly Label _energy;
        private readonly Label _flags;
        private readonly Label _status;
        private readonly Label _hint;
        private readonly Label _toast;
        private readonly Label _log;
        private readonly VisualElement _weapons;
        private readonly VisualElement _setupDefenses;
        private readonly Dictionary<string, Button> _setupDefenseButtons = new Dictionary<string, Button>();
        private readonly Button _confirmSetup;
        private readonly Button _endTurn;
        private readonly VisualElement _endPanel;
        private readonly Label _endTitle;
        private readonly Dictionary<string, Button> _weaponButtons = new Dictionary<string, Button>();
        private readonly List<string> _logLines = new List<string>();
        private float _toastUntil;

        // `arsenal` es el loadout del jugador: solo esas armas tienen botón. `allowProgressReset` muestra en el
        // panel final el botón de desarrollo para borrar las victorias.
        public HudView(
            VisualElement root, IReadOnlyList<WeaponDefinition> arsenal, bool allowProgressReset = false)
        {
            _root = root;
            root.style.flexDirection = FlexDirection.Column;
            root.pickingMode = PickingMode.Ignore;

            var top = Panel(root);
            _energy = Text(top, 40, HoloPalette.OwnColor);
            _flags = Text(top, 32, HoloPalette.TextColor);
            _status = Text(top, 32, HoloPalette.TextColor);
            var toggles = Row(top);
            Board = new VisualElement { pickingMode = PickingMode.Ignore };
            Board.style.position = Position.Absolute;
            Board.style.left = 0;
            Board.style.top = 0;
            Board.style.right = 0;
            Board.style.bottom = 0;
            root.Insert(0, Board);
            AddButton(toggles, "Etiquetas", () => LabelsToggled?.Invoke());

            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.style.flexGrow = 1;
            root.Add(spacer);

            _toast = Text(root, 34, HoloPalette.EnemyColor);
            _toast.style.alignSelf = Align.Center;
            _toast.style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
            _toast.style.display = DisplayStyle.None;

            var bottom = Panel(root);
            _log = Text(bottom, 26, new Color(0.85f, 0.9f, 1f));
            _hint = Text(bottom, 28, HoloPalette.OwnColor);
            _weapons = Row(bottom);
            _weapons.style.flexWrap = Wrap.Wrap;
            foreach (var weapon in arsenal)
            {
                var id = weapon.Id;
                var caption = WeaponCaptionFormatter.Format(weapon, string.Empty, false);
                var button = AddButton(_weapons, caption, () => WeaponSelected?.Invoke(id));
                _weaponButtons[id] = button;
            }

            _setupDefenses = Row(bottom);
            _setupDefenses.style.flexWrap = Wrap.Wrap;
            foreach (var weapon in arsenal.Where(IsSetupDefense))
            {
                var id = weapon.Id;
                _setupDefenseButtons[id] = AddButton(
                    _setupDefenses,
                    WeaponCaptionFormatter.Format(weapon, string.Empty, true),
                    () => SetupDefenseSelected?.Invoke(id));
            }

            var actions = Row(bottom);
            _confirmSetup = AddButton(actions, "Confirmar banderas", () => ConfirmSetupClicked?.Invoke());
            _endTurn = AddButton(actions, "Terminar turno", () => EndTurnClicked?.Invoke());

            _endPanel = new VisualElement();
            _endPanel.style.position = Position.Absolute;
            _endPanel.style.left = 0;
            _endPanel.style.right = 0;
            _endPanel.style.top = 0;
            _endPanel.style.bottom = 0;
            _endPanel.style.backgroundColor = new Color(0.02f, 0.04f, 0.09f, 0.9f);
            _endPanel.style.justifyContent = Justify.Center;
            _endPanel.style.alignItems = Align.Center;
            _endPanel.style.display = DisplayStyle.None;
            _endTitle = Text(_endPanel, 56, HoloPalette.OwnColor);
            _endTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            _endTitle.style.marginLeft = 24;
            _endTitle.style.marginRight = 24;
            AddButton(_endPanel, "Jugar de nuevo", () => RestartClicked?.Invoke());
            if (allowProgressReset)
            {
                ResetProgressButton = AddButton(_endPanel, "Reiniciar progreso", RaiseResetProgress);
            }

            root.Add(_endPanel);
        }

        public event Action<string> WeaponSelected;

        public event Action<string> SetupDefenseSelected;

        public event Action ConfirmSetupClicked;

        public event Action EndTurnClicked;

        public event Action RestartClicked;

        public event Action LabelsToggled;

        public event Action ResetProgressClicked;

        // Botón de desarrollo del panel final; null si la compilación no es de desarrollo.
        internal Button ResetProgressButton { get; }

        internal void RaiseResetProgress() => ResetProgressClicked?.Invoke();

        // Capa donde se dibujan las etiquetas sobre las casillas.
        public VisualElement Board { get; }

        public void SetEnergy(int energy, int max) => _energy.text = $"Energía {energy}/{max}";

        public void SetFlags(int captured, int remaining, int total) =>
            _flags.text = $"Banderas rivales capturadas: {captured}/{total}   ·   Tus banderas: {remaining}/{total}";

        public void SetStatus(string text) => _status.text = text;

        public void SetHint(string text) => _hint.text = text;

        public void SetSetupMode(bool setup)
        {
            _confirmSetup.style.display = setup ? DisplayStyle.Flex : DisplayStyle.None;
            _weapons.style.display = setup ? DisplayStyle.None : DisplayStyle.Flex;
            _setupDefenses.style.display = setup ? DisplayStyle.Flex : DisplayStyle.None;
            _endTurn.style.display = setup ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void SetInteractive(bool interactive)
        {
            _weapons.SetEnabled(interactive);
            _endTurn.SetEnabled(interactive);
        }

        public void SetWeaponState(WeaponDefinition weapon, bool selected, bool affordable, string modifier)
        {
            var button = _weaponButtons[weapon.Id];
            button.text = WeaponCaptionFormatter.Format(weapon, modifier, false);
            Highlight(button, selected, affordable);
        }

        public void SetSetupDefenseState(string selectedId)
        {
            foreach (var pair in _setupDefenseButtons)
            {
                Highlight(pair.Value, pair.Key == selectedId, true);
            }
        }

        private static bool IsSetupDefense(WeaponDefinition weapon) =>
            weapon.Category == WeaponCategory.Defend || weapon.Category == WeaponCategory.Guard;

        private static void Highlight(Button button, bool selected, bool affordable)
        {
            button.SetEnabled(affordable);

            // Los estilos en línea anulan el aspecto "deshabilitado" del tema; se atenúa a mano.
            button.style.opacity = affordable ? 1f : 0.35f;
            button.style.borderTopColor = selected ? HoloPalette.OwnColor : Color.gray;
            button.style.borderBottomColor = button.style.borderTopColor;
            button.style.borderLeftColor = button.style.borderTopColor;
            button.style.borderRightColor = button.style.borderTopColor;
            var width = selected ? 6f : 2f;
            button.style.borderTopWidth = width;
            button.style.borderBottomWidth = width;
            button.style.borderLeftWidth = width;
            button.style.borderRightWidth = width;
        }

        public void AddLog(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            _logLines.Add(line);
            if (_logLines.Count > MaxLogLines)
            {
                _logLines.RemoveAt(0);
            }

            _log.text = string.Join("\n", _logLines);
        }

        public void ShowToast(string text)
        {
            _toast.text = text;
            _toast.style.display = DisplayStyle.Flex;
            _toastUntil = Time.unscaledTime + 2f;
        }

        public void ShowEnd(string reason, string unlockText = "")
        {
            _endTitle.text = string.IsNullOrEmpty(unlockText) ? reason : reason + "\n\n" + unlockText;
            _endPanel.style.display = DisplayStyle.Flex;
        }

        public void Tick()
        {
            if (_toast.style.display == DisplayStyle.Flex && Time.unscaledTime > _toastUntil)
            {
                _toast.style.display = DisplayStyle.None;
            }

            // Respetar muescas y bordes redondeados del teléfono.
            var safe = Screen.safeArea;
            var scale = _root.panel == null ? 1f : _root.worldBound.height / Mathf.Max(1f, Screen.height);
            _root.style.paddingTop = (Screen.height - safe.yMax) * scale;
            _root.style.paddingBottom = safe.yMin * scale;
        }

        // Verdadero si la posición (coordenadas de panel) cae sobre un control del HUD.
        public bool IsOverControl(Vector2 panelPosition)
        {
            var picked = _root.panel?.Pick(panelPosition);
            return picked != null && picked.pickingMode == PickingMode.Position && picked != _root;
        }

        private static VisualElement Panel(VisualElement parent)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = new Color(0.02f, 0.04f, 0.09f, 0.85f);
            panel.style.paddingLeft = 24;
            panel.style.paddingRight = 24;
            panel.style.paddingTop = 16;
            panel.style.paddingBottom = 16;
            parent.Add(panel);
            return panel;
        }

        private static VisualElement Row(VisualElement parent)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            parent.Add(row);
            return row;
        }

        private static Label Text(VisualElement parent, int size, Color color)
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.style.fontSize = size;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(label);
            return label;
        }

        private static Button AddButton(VisualElement parent, string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.minHeight = ButtonMinHeight;
            button.style.fontSize = 28;
            button.style.color = Color.white;
            button.style.backgroundColor = new Color(0.06f, 0.15f, 0.3f);
            button.style.marginRight = 8;
            button.style.marginBottom = 8;
            button.style.paddingLeft = 16;
            button.style.paddingRight = 16;
            button.style.whiteSpace = WhiteSpace.Normal;
            button.tooltip = text.Replace("\n", " ");
            parent.Add(button);
            return button;
        }
    }
}
