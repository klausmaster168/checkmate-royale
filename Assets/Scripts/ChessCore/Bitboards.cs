using System.Runtime.CompilerServices;

namespace CheckmateRoyale.ChessCore
{
    /// <summary>
    /// A bitboard is a <see cref="ulong"/> where bit <c>i</c> is square <c>i</c>
    /// (a1 = bit 0). These helpers are the vocabulary the whole engine speaks.
    /// </summary>
    public static class Bitboards
    {
        public const ulong FileA = 0x0101010101010101UL;
        public const ulong FileB = FileA << 1;
        public const ulong FileG = FileA << 6;
        public const ulong FileH = FileA << 7;
        public const ulong Rank1 = 0x00000000000000FFUL;
        public const ulong Rank2 = Rank1 << 8;
        public const ulong Rank4 = Rank1 << 24;
        public const ulong Rank5 = Rank1 << 32;
        public const ulong Rank7 = Rank1 << 48;
        public const ulong Rank8 = Rank1 << 56;
        public const ulong NotFileA = ~FileA;
        public const ulong NotFileH = ~FileH;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Bit(int sq) => 1UL << sq;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSet(ulong bb, int sq) => (bb & (1UL << sq)) != 0;

        /// <summary>Number of set bits (portable SWAR popcount; no BitOperations dependency).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(ulong bb)
        {
            bb -= (bb >> 1) & 0x5555555555555555UL;
            bb = (bb & 0x3333333333333333UL) + ((bb >> 2) & 0x3333333333333333UL);
            bb = (bb + (bb >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
            return (int)((bb * 0x0101010101010101UL) >> 56);
        }

        private const ulong DeBruijn64 = 0x03F79D71B4CB0A89UL;
        private static readonly int[] DeBruijnIndex =
        {
             0, 47,  1, 56, 48, 27,  2, 60,
            57, 49, 41, 37, 28, 16,  3, 61,
            54, 58, 35, 52, 50, 42, 21, 44,
            38, 32, 29, 23, 17, 11,  4, 62,
            46, 55, 26, 59, 40, 36, 15, 53,
            34, 51, 20, 43, 31, 22, 10, 45,
            25, 39, 14, 33, 19, 30,  9, 24,
            13, 18,  8, 12,  7,  6,  5, 63
        };

        /// <summary>Index of the least-significant set bit (0..63). <paramref name="bb"/> must be non-zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Lsb(ulong bb) => DeBruijnIndex[((bb ^ (bb - 1)) * DeBruijn64) >> 58];

        /// <summary>Return the LSB index and clear it from <paramref name="bb"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopLsb(ref ulong bb)
        {
            int sq = Lsb(bb);
            bb &= bb - 1;
            return sq;
        }

        // Single-step shifts that never wrap across the board edges.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong North(ulong bb) => bb << 8;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong South(ulong bb) => bb >> 8;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong East(ulong bb) => (bb & NotFileH) << 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong West(ulong bb) => (bb & NotFileA) >> 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong NorthEast(ulong bb) => (bb & NotFileH) << 9;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong NorthWest(ulong bb) => (bb & NotFileA) << 7;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SouthEast(ulong bb) => (bb & NotFileH) >> 7;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SouthWest(ulong bb) => (bb & NotFileA) >> 9;
    }
}
