using Flagstorm.Core.Map;
using UnityEngine;

namespace Flagstorm.Presentation
{
    // Colores de la vista holográfica (maquetas aprobadas). Cada terreno lleva además una etiqueta:
    // la información nunca depende solo del color (WCAG, spec §6.3).
    public static class HoloPalette
    {
        public static readonly Color Ocean = FromHex(0x040916);
        public static readonly Color OwnColor = FromHex(0x2DE2FF);
        public static readonly Color EnemyColor = FromHex(0xFF3D7F);
        public static readonly Color TextColor = Color.white;

        public static Color TerrainColor(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Jungle: return FromHex(0x3DFF8A);
                case TerrainType.Ice: return FromHex(0x8AD8FF);
                case TerrainType.Volcano: return FromHex(0xFF7A3C);
                case TerrainType.Beach: return FromHex(0xF2D48A);
                default: return FromHex(0xB8B8C8);
            }
        }

        public static string TerrainLabel(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Jungle: return "SELVA";
                case TerrainType.Ice: return "HIELO";
                case TerrainType.Volcano: return "VOLCÁN";
                case TerrainType.Beach: return "PLAYA";
                default: return "ROCA";
            }
        }

        private static Color FromHex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }
}
