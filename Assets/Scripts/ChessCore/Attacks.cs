using System.Runtime.CompilerServices;
using CheckmateRoyale.ChessCore.Util;
using static CheckmateRoyale.ChessCore.Bitboards;
using static CheckmateRoyale.ChessCore.Types;

namespace CheckmateRoyale.ChessCore
{
    /// <summary>
    /// Precomputed attack tables. Knight/king/pawn are simple lookups; sliding pieces
    /// use magic bitboards. Magic constants are found once at type-init with a fixed
    /// seed, so they are identical on every run and every platform, and all lookup
    /// tables are built up front — nothing is searched or allocated per move.
    /// </summary>
    public static class Attacks
    {
        private static readonly ulong[] Knight = new ulong[64];
        private static readonly ulong[] King = new ulong[64];
        private static readonly ulong[][] Pawn = { new ulong[64], new ulong[64] }; // [color][sq]

        private static readonly ulong[] RookMask = new ulong[64];
        private static readonly ulong[] BishopMask = new ulong[64];
        private static readonly ulong[] RookMagic = new ulong[64];
        private static readonly ulong[] BishopMagic = new ulong[64];
        private static readonly int[] RookShift = new int[64];
        private static readonly int[] BishopShift = new int[64];
        private static readonly ulong[][] RookTable = new ulong[64][];
        private static readonly ulong[][] BishopTable = new ulong[64][];

        private static readonly int[,] RookDirs = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
        private static readonly int[,] BishopDirs = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };

        static Attacks()
        {
            InitLeapers();
            var rng = new Xoshiro256(0xC4EC_3EAD_1234_5678UL); // fixed seed => deterministic magics
            InitMagics(RookDirs, RookMask, RookMagic, RookShift, RookTable, ref rng);
            InitMagics(BishopDirs, BishopMask, BishopMagic, BishopShift, BishopTable, ref rng);
        }

        // ---- public queries ----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong KnightAttacks(int sq) => Knight[sq];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong KingAttacks(int sq) => King[sq];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PawnAttacks(Color c, int sq) => Pawn[(int)c][sq];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RookAttacks(int sq, ulong occ)
        {
            ulong idx = ((occ & RookMask[sq]) * RookMagic[sq]) >> RookShift[sq];
            return RookTable[sq][idx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BishopAttacks(int sq, ulong occ)
        {
            ulong idx = ((occ & BishopMask[sq]) * BishopMagic[sq]) >> BishopShift[sq];
            return BishopTable[sq][idx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong QueenAttacks(int sq, ulong occ) => RookAttacks(sq, occ) | BishopAttacks(sq, occ);

        /// <summary>Attack set for any piece type on <paramref name="sq"/> given occupancy.</summary>
        public static ulong PieceAttacks(PieceType pt, Color c, int sq, ulong occ) => pt switch
        {
            PieceType.Pawn => PawnAttacks(c, sq),
            PieceType.Knight => Knight[sq],
            PieceType.Bishop => BishopAttacks(sq, occ),
            PieceType.Rook => RookAttacks(sq, occ),
            PieceType.Queen => QueenAttacks(sq, occ),
            PieceType.King => King[sq],
            _ => 0UL
        };

        // ---- init ----
        private static void InitLeapers()
        {
            for (int sq = 0; sq < 64; sq++)
            {
                ulong b = Bit(sq);

                ulong n = 0;
                n |= (b & NotFileH) << 17;               // NNE
                n |= (b & NotFileA) << 15;               // NNW
                n |= (b & (NotFileH & ~FileG)) << 10;    // ENE
                n |= (b & (NotFileA & ~FileB)) << 6;     // WNW
                n |= (b & (NotFileH & ~FileG)) >> 6;     // ESE
                n |= (b & (NotFileA & ~FileB)) >> 10;    // WSW
                n |= (b & NotFileH) >> 15;               // SSE
                n |= (b & NotFileA) >> 17;               // SSW
                Knight[sq] = n;

                ulong k = 0;
                k |= North(b) | South(b) | East(b) | West(b);
                k |= NorthEast(b) | NorthWest(b) | SouthEast(b) | SouthWest(b);
                King[sq] = k;

                Pawn[(int)Color.White][sq] = NorthEast(b) | NorthWest(b);
                Pawn[(int)Color.Black][sq] = SouthEast(b) | SouthWest(b);
            }
        }

        /// <summary>Ray attacks from a square through occupancy — the reference used to build magic tables.</summary>
        private static ulong SlideAttacks(int sq, ulong occ, int[,] dirs)
        {
            ulong att = 0;
            int f0 = FileOf(sq), r0 = RankOf(sq);
            for (int d = 0; d < 4; d++)
            {
                int df = dirs[d, 0], dr = dirs[d, 1];
                int f = f0 + df, r = r0 + dr;
                while (f >= 0 && f <= 7 && r >= 0 && r <= 7)
                {
                    int s = SquareOf(f, r);
                    att |= Bit(s);
                    if ((occ & Bit(s)) != 0) break;
                    f += df; r += dr;
                }
            }
            return att;
        }

        /// <summary>Relevant-occupancy mask: ray squares excluding the board edges.</summary>
        private static ulong SlideMask(int sq, int[,] dirs)
        {
            ulong mask = 0;
            int f0 = FileOf(sq), r0 = RankOf(sq);
            for (int d = 0; d < 4; d++)
            {
                int df = dirs[d, 0], dr = dirs[d, 1];
                int f = f0 + df, r = r0 + dr;
                while (f >= 0 && f <= 7 && r >= 0 && r <= 7)
                {
                    // Exclude the board-edge square in this direction: if the next step
                    // leaves the board, the current square is the edge, so stop before it.
                    int nf = f + df, nr = r + dr;
                    if (nf < 0 || nf > 7 || nr < 0 || nr > 7) break;
                    mask |= Bit(SquareOf(f, r));
                    f += df; r += dr;
                }
            }
            return mask;
        }

        private static void InitMagics(int[,] dirs, ulong[] masks, ulong[] magics, int[] shifts, ulong[][] tables, ref Xoshiro256 rng)
        {
            for (int sq = 0; sq < 64; sq++)
            {
                ulong mask = SlideMask(sq, dirs);
                masks[sq] = mask;
                int bits = PopCount(mask);
                int shift = 64 - bits;
                shifts[sq] = shift;
                int n = 1 << bits;

                // Enumerate every occupancy subset of the mask and its reference attacks.
                ulong[] occ = new ulong[n];
                ulong[] reference = new ulong[n];
                ulong b = 0;
                for (int i = 0; i < n; i++)
                {
                    occ[i] = b;
                    reference[i] = SlideAttacks(sq, b, dirs);
                    b = (b - mask) & mask; // Carry-Rippler subset enumeration
                }

                ulong[] table = new ulong[n];
                const ulong Empty = ulong.MaxValue; // sentinel: real attack sets are never all-ones
                while (true)
                {
                    ulong magic = rng.NextULong() & rng.NextULong() & rng.NextULong();
                    // Cheap reject: magic must scatter the mask's high byte a bit.
                    if (PopCount((mask * magic) & 0xFF00000000000000UL) < 6) continue;

                    for (int i = 0; i < n; i++) table[i] = Empty;
                    bool fail = false;
                    for (int i = 0; i < n; i++)
                    {
                        ulong idx = (occ[i] * magic) >> shift;
                        if (table[idx] == Empty) table[idx] = reference[i];
                        else if (table[idx] != reference[i]) { fail = true; break; }
                    }
                    if (fail) continue;

                    for (int i = 0; i < n; i++) if (table[i] == Empty) table[i] = 0UL;
                    magics[sq] = magic;
                    tables[sq] = table;
                    break;
                }
            }
        }
    }
}
