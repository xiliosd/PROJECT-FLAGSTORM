using System.Collections.Generic;

namespace Flagstorm.Core.Map
{
    public static class PrototypeIslands
    {
        public static readonly IReadOnlyList<string> DefaultLayout = new[]
        {
            ".BBBBBB..",
            "BBJJJRBB.",
            "BJJVVRRIB",
            "BJVVVRIIB",
            "BBJJRRIIB",
            ".BBRRIIB.",
            "..BBBBB..",
        };

        public static IslandMap CreateDefault() => IslandLayoutParser.Parse(DefaultLayout);
    }
}
