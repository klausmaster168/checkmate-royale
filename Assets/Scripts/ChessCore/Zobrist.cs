using CheckmateRoyale.ChessCore.Util;

namespace CheckmateRoyale.ChessCore
{
    /// <summary>
    /// Zobrist hash keys, generated once from a fixed seed so every device produces
    /// identical position hashes (required for replay determinism and repetition).
    /// </summary>
    public static class Zobrist
    {
        /// <summary>[pieceIndex 0..11][square 0..63]</summary>
        public static readonly ulong[][] Piece = new ulong[12][];
        /// <summary>XORed in when it is Black to move.</summary>
        public static readonly ulong Side;
        /// <summary>Indexed by the 4-bit castling-rights nibble (0..15).</summary>
        public static readonly ulong[] Castling = new ulong[16];
        /// <summary>Indexed by en-passant file (0..7).</summary>
        public static readonly ulong[] EnPassantFile = new ulong[8];

        static Zobrist()
        {
            var rng = new Xoshiro256(0x9D3E_7F11_ABCD_0042UL);
            for (int p = 0; p < 12; p++)
            {
                Piece[p] = new ulong[64];
                for (int s = 0; s < 64; s++) Piece[p][s] = rng.NextULong();
            }
            Side = rng.NextULong();
            for (int i = 0; i < 16; i++) Castling[i] = rng.NextULong();
            for (int i = 0; i < 8; i++) EnPassantFile[i] = rng.NextULong();
        }
    }
}
