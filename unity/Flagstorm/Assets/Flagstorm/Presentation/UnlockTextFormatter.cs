using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;

namespace Flagstorm.Presentation
{
    // Texto del panel final que avisa qué armas se desbloquean con la victoria recién lograda.
    public static class UnlockTextFormatter
    {
        // `winsBefore` son las victorias antes de registrar la actual; vacío si ya no queda nada por desbloquear.
        public static string Format(int winsBefore)
        {
            var before = ArsenalProgression.UnlockedAfter(winsBefore).Select(w => w.Id).ToHashSet();
            var added = ArsenalProgression.UnlockedAfter(winsBefore + 1)
                .Where(w => !before.Contains(w.Id))
                .Select(w => w.DisplayName.ToLowerInvariant())
                .ToList();
            return added.Count == 0 ? string.Empty : $"Nuevo: {JoinWithAnd(added)}";
        }

        private static string JoinWithAnd(IReadOnlyList<string> items) =>
            items.Count == 1
                ? items[0]
                : $"{string.Join(", ", items.Take(items.Count - 1))} y {items[items.Count - 1]}";
    }
}
