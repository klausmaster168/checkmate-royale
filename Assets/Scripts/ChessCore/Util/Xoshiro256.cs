using System.Runtime.CompilerServices;

namespace CheckmateRoyale.ChessCore.Util
{
    /// <summary>
    /// Deterministic, seedable PRNG (xoshiro256** by Blackman &amp; Vigna).
    /// State transitions use only integer ops, so output is bit-identical across
    /// platforms and architectures — the backbone of the game's determinism law.
    /// Seeded from a single <see cref="ulong"/> via SplitMix64.
    /// </summary>
    public struct Xoshiro256
    {
        private ulong _s0, _s1, _s2, _s3;

        /// <summary>Create a PRNG seeded deterministically from <paramref name="seed"/>.</summary>
        public Xoshiro256(ulong seed)
        {
            // SplitMix64 expands one seed word into the four state words.
            _s0 = SplitMix64(ref seed);
            _s1 = SplitMix64(ref seed);
            _s2 = SplitMix64(ref seed);
            _s3 = SplitMix64(ref seed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

        /// <summary>One step of SplitMix64; advances <paramref name="state"/> in place.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SplitMix64(ref ulong state)
        {
            ulong z = unchecked(state += 0x9E3779B97F4A7C15UL);
            z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
            z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
            return z ^ (z >> 31);
        }

        /// <summary>Next 64-bit unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong NextULong()
        {
            ulong result = unchecked(Rotl(_s1 * 5UL, 7) * 9UL);
            ulong t = _s1 << 17;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = Rotl(_s3, 45);
            return result;
        }

        /// <summary>
        /// Uniform integer in [0, <paramref name="maxExclusive"/>) via rejection
        /// sampling on 64-bit values (unbiased, portable, deterministic given the state).
        /// </summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(maxExclusive));

            ulong range = (ulong)maxExclusive;
            // Largest multiple of range that fits in 64 bits; reject above it to avoid modulo bias.
            ulong limit = ulong.MaxValue - (ulong.MaxValue % range);
            ulong x;
            do { x = NextULong(); } while (x >= limit);
            return (int)(x % range);
        }

        /// <summary>Double in [0, 1). Float appears only in output, never in state.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);
    }
}
