using System;

namespace Flagstorm.Core.Ai
{
    public interface IRandomSource
    {
        int Next(int minInclusive, int maxExclusive);
    }

    // xorshift32 propio: System.Random no garantiza la misma secuencia entre .NET y el runtime de Unity.
    public sealed class SeededRandom : IRandomSource
    {
        private uint _state;

        public SeededRandom(uint seed)
        {
            _state = seed == 0 ? 1u : seed;
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "El rango está vacío.");
            }

            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            var range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(_state % range);
        }
    }
}
