namespace CheckmateRoyale.ChessCore
{
    /// <summary>Side to move / piece owner.</summary>
    public enum Color : byte { White = 0, Black = 1 }

    /// <summary>Piece kind independent of colour.</summary>
    public enum PieceType : byte { None = 0, Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, Queen = 5, King = 6 }

    /// <summary>
    /// Coloured piece. Values are laid out so <c>(int)Piece - 1</c> indexes the 12
    /// piece bitboards (WP..BK = 0..11) and the two colour blocks are contiguous.
    /// </summary>
    public enum Piece : byte
    {
        None = 0,
        WP = 1, WN = 2, WB = 3, WR = 4, WQ = 5, WK = 6,
        BP = 7, BN = 8, BB = 9, BR = 10, BQ = 11, BK = 12
    }

    /// <summary>Static helpers over <see cref="Color"/>, <see cref="Piece"/> and squares.</summary>
    public static class Types
    {
        public const int BoardSquares = 64;

        /// <summary>Bitboard index (0..11) for a coloured piece.</summary>
        public static int Index(this Piece p) => (int)p - 1;

        public static Color ColorOf(this Piece p) => (int)p <= (int)Piece.WK ? Color.White : Color.Black;

        public static PieceType TypeOf(this Piece p)
        {
            if (p == Piece.None) return PieceType.None;
            int v = (int)p;
            int t = v <= (int)Piece.WK ? v : v - 6; // fold black onto white 1..6
            return (PieceType)t;
        }

        public static Piece MakePiece(Color c, PieceType t)
        {
            if (t == PieceType.None) return Piece.None;
            return (Piece)((int)t + (c == Color.White ? 0 : 6));
        }

        public static Color Opposite(this Color c) => (Color)(1 - (int)c);

        // ---- square arithmetic (a1 = 0, h1 = 7, a8 = 56, h8 = 63) ----
        public static int FileOf(int sq) => sq & 7;
        public static int RankOf(int sq) => sq >> 3;
        public static int SquareOf(int file, int rank) => (rank << 3) | file;

        /// <summary>Parse an algebraic square like "e4"; returns -1 on failure.</summary>
        public static int ParseSquare(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length != 2) return -1;
            int f = s[0] - 'a';
            int r = s[1] - '1';
            if (f < 0 || f > 7 || r < 0 || r > 7) return -1;
            return SquareOf(f, r);
        }

        /// <summary>Algebraic name of a square, e.g. 28 => "e4".</summary>
        public static string SquareName(int sq)
        {
            if (sq < 0 || sq > 63) return "-";
            return $"{(char)('a' + FileOf(sq))}{(char)('1' + RankOf(sq))}";
        }
    }
}
