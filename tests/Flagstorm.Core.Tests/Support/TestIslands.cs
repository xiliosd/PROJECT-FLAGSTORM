using System.Collections.Generic;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Tests.Support
{
    // Isla 5×5: 9 celdas interiores (1,1)J (2,1)J (3,1)I (0,2)J (1,2)V (2,2)I (0,3)R (1,3)R (2,3)I y 16 de costa.
    public static class TestIslands
    {
        public static readonly IReadOnlyList<string> SmallLayout = new[]
        {
            "BBBBB",
            "BJJIB",
            "BJVIB",
            "BRRIB",
            "BBBBB",
        };

        public static IslandMap Small() => IslandLayoutParser.Parse(SmallLayout);
    }
}
