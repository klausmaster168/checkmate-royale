using System;

namespace CheckmateRoyale.ChessCore
{
    /// <summary>Four-bit move classification packed into bits 16-19 of a <see cref="Move"/>.</summary>
    public enum MoveFlag : uint
    {
        Quiet = 0,
        DoublePush = 1,
        KingCastle = 2,
        QueenCastle = 3,
        Capture = 4,
        EnPassant = 5,
        PromoKnight = 8,
        PromoBishop = 9,
        PromoRook = 10,
        PromoQueen = 11,
        PromoCapKnight = 12,
        PromoCapBishop = 13,
        PromoCapRook = 14,
        PromoCapQueen = 15
    }

    /// <summary>
    /// An immutable move packed into a single <see cref="uint"/>:
    /// bits 0-5 from, 6-11 to, 12-15 promotion <see cref="PieceType"/>, 16-19 <see cref="MoveFlag"/>.
    /// A default/zero value is the null move (<see cref="IsNull"/>).
    /// </summary>
    public readonly struct Move : IEquatable<Move>
    {
        private readonly uint _data;

        private Move(uint data) => _data = data;

        /// <summary>Build a move; the promotion-piece bits are derived from the flag.</summary>
        public static Move Create(int from, int to, MoveFlag flag)
        {
            uint promo = 0;
            if (((uint)flag & 8u) != 0) // any promotion flag
                promo = (uint)(PieceType.Knight) + ((uint)flag & 3u); // Knight..Queen
            uint data = (uint)(from & 63)
                        | ((uint)(to & 63) << 6)
                        | (promo << 12)
                        | (((uint)flag & 15u) << 16);
            return new Move(data);
        }

        public static readonly Move Null = new Move(0);

        public int From => (int)(_data & 63);
        public int To => (int)((_data >> 6) & 63);
        public PieceType Promotion => (PieceType)((_data >> 12) & 15);
        public MoveFlag Flag => (MoveFlag)((_data >> 16) & 15);
        public uint Raw => _data;

        public bool IsNull => _data == 0;
        public bool IsCapture => ((uint)Flag & 4u) != 0;      // Capture, EnPassant, promo-captures
        public bool IsPromotion => ((uint)Flag & 8u) != 0;
        public bool IsEnPassant => Flag == MoveFlag.EnPassant;
        public bool IsDoublePush => Flag == MoveFlag.DoublePush;
        public bool IsCastle => Flag == MoveFlag.KingCastle || Flag == MoveFlag.QueenCastle;

        /// <summary>Long algebraic (UCI) form, e.g. "e2e4", "e7e8q".</summary>
        public string ToUci()
        {
            if (IsNull) return "0000";
            string s = Types.SquareName(From) + Types.SquareName(To);
            if (IsPromotion)
            {
                char c = Promotion switch
                {
                    PieceType.Knight => 'n',
                    PieceType.Bishop => 'b',
                    PieceType.Rook => 'r',
                    _ => 'q'
                };
                s += c;
            }
            return s;
        }

        public bool Equals(Move other) => _data == other._data;
        public override bool Equals(object obj) => obj is Move m && Equals(m);
        public override int GetHashCode() => (int)_data;
        public override string ToString() => ToUci();

        public static bool operator ==(Move a, Move b) => a._data == b._data;
        public static bool operator !=(Move a, Move b) => a._data != b._data;
    }
}
