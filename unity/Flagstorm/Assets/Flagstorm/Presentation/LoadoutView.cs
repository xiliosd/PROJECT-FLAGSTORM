using System;
using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flagstorm.Presentation
{
    // Pantalla previa a la partida: el jugador marca hasta 6 de las armas desbloqueadas (UI Toolkit en código).
    public sealed class LoadoutView
    {
        private const float ButtonMinHeight = 96f;

        private readonly IReadOnlyList<WeaponDefinition> _unlocked;
        private readonly VisualElement _panel;
        private readonly Label _reason;
        private readonly Button _play;
        private readonly Dictionary<string, Button> _weaponButtons = new Dictionary<string, Button>();
        private readonly HashSet<string> _selected = new HashSet<string>();

        public LoadoutView(VisualElement root, IReadOnlyList<WeaponDefinition> unlocked, int wins, bool allowReset)
        {
            _unlocked = unlocked;
            _panel = new VisualElement();
            _panel.style.position = Position.Absolute;
            _panel.style.left = 0;
            _panel.style.right = 0;
            _panel.style.top = 0;
            _panel.style.bottom = 0;
            _panel.style.backgroundColor = new Color(0.02f, 0.04f, 0.09f, 0.97f);
            _panel.style.paddingLeft = 24;
            _panel.style.paddingRight = 24;
            _panel.style.paddingTop = 48;
            _panel.style.paddingBottom = 24;

            Text($"Elige tu arsenal ({LoadoutRules.MaxSlots} armas como máximo)", 40, HoloPalette.OwnColor);
            Text($"Victorias: {wins}", 28, HoloPalette.TextColor);

            var list = new ScrollView();
            list.style.flexGrow = 1;
            list.style.flexShrink = 1;
            foreach (var weapon in unlocked)
            {
                var id = weapon.Id;
                _weaponButtons[id] = AddButton(list, string.Empty, () => Toggle(id));
            }

            _panel.Add(list);
            _reason = Text(string.Empty, 28, HoloPalette.EnemyColor);
            _play = AddButton(_panel, "Jugar", Click);
            if (allowReset)
            {
                AddButton(_panel, "Reiniciar progreso", () => ResetProgressClicked?.Invoke());
            }

            root.Add(_panel);
            Refresh();
        }

        public event Action<IReadOnlyList<string>> PlayClicked;

        public event Action ResetProgressClicked;

        // Armas marcadas, en el orden del catálogo.
        public IReadOnlyList<string> SelectedIds =>
            _unlocked.Where(w => _selected.Contains(w.Id)).Select(w => w.Id).ToList();

        public bool CanPlay => LoadoutRules.Validate(SelectedIds, _unlocked) == LoadoutError.None;

        public string ReasonText => _reason.text;

        public void Toggle(string weaponId)
        {
            if (!_selected.Remove(weaponId))
            {
                if (_selected.Count >= LoadoutRules.MaxSlots)
                {
                    Refresh();
                    _reason.text = ReasonFor(LoadoutError.TooMany);
                    return;
                }

                _selected.Add(weaponId);
            }

            Refresh();
        }

        // Equivale a pulsar "Jugar": no hace nada si el arsenal no es válido.
        public void Click()
        {
            if (CanPlay)
            {
                PlayClicked?.Invoke(SelectedIds);
            }
        }

        public void Remove() => _panel.RemoveFromHierarchy();

        internal static string ReasonFor(LoadoutError error)
        {
            switch (error)
            {
                case LoadoutError.TooMany: return $"Máximo {LoadoutRules.MaxSlots} armas.";
                case LoadoutError.Duplicate: return "Hay un arma repetida.";
                case LoadoutError.NotUnlocked: return "Hay un arma que todavía no desbloqueas.";
                case LoadoutError.NoCaptureWeapon: return "Incluye al menos un robot de ataque para poder ganar.";
                default: return string.Empty;
            }
        }

        private static string Caption(WeaponDefinition weapon, bool marked)
        {
            var home = PrototypeArsenal.HomeTerrainOf(weapon.Id);
            var terrain = home.HasValue ? HoloPalette.TerrainLabel(home.Value) : "cualquiera";
            var mark = marked ? "[X]" : "[  ]";
            return $"{mark} {weapon.DisplayName}\n{weapon.EnergyCost} energía · terreno: {terrain}";
        }

        private void Refresh()
        {
            foreach (var weapon in _unlocked)
            {
                var marked = _selected.Contains(weapon.Id);
                var button = _weaponButtons[weapon.Id];
                button.text = Caption(weapon, marked);
                var width = marked ? 6f : 2f;
                var color = marked ? HoloPalette.OwnColor : Color.gray;
                button.style.borderTopWidth = width;
                button.style.borderBottomWidth = width;
                button.style.borderLeftWidth = width;
                button.style.borderRightWidth = width;
                button.style.borderTopColor = color;
                button.style.borderBottomColor = color;
                button.style.borderLeftColor = color;
                button.style.borderRightColor = color;
            }

            var error = LoadoutRules.Validate(SelectedIds, _unlocked);
            _reason.text = error == LoadoutError.None
                ? $"Arsenal listo: {_selected.Count}/{LoadoutRules.MaxSlots} armas."
                : ReasonFor(error);
            _play.SetEnabled(error == LoadoutError.None);
            _play.style.opacity = error == LoadoutError.None ? 1f : 0.35f;
        }

        private Label Text(string text, int size, Color color)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.style.fontSize = size;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 8;
            _panel.Add(label);
            return label;
        }

        private static Button AddButton(VisualElement parent, string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.minHeight = ButtonMinHeight;
            button.style.fontSize = 28;
            button.style.color = Color.white;
            button.style.backgroundColor = new Color(0.06f, 0.15f, 0.3f);
            button.style.marginBottom = 8;
            button.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(button);
            return button;
        }
    }
}
