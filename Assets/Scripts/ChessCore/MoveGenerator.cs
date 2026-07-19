using System;
using static CheckmateRoyale.ChessCore.Bitboards;
using static CheckmateRoyale.ChessCore.Types;
using static CheckmateRoyale.ChessCore.Attacks;

namespace CheckmateRoyale.ChessCore
{
    /// <summary>
    /// Legal move generation. Pseudo-legal moves are generated into a caller-supplied
    /// buffer (zero allocation), then filtered with make/unmake so the moving side is
    /// never left in check. Castling legality (through/into/out of check) and the
    /// en-passant discovered-check trap are handled correctly.
    /// </summary>
    public static class MoveGenerator
    {
        /// <summary>Recommended buffer size; no legal position exceeds this.</summary>
        public const int MaxMoves = 256;

        private ref struct Buf
        {
            public Span<Move> M;
            public int N;
            public void Add(Move m) => M[N++] = m;
        }

        /// <summary>Fill <paramref name="moves"/> with fully legal moves; returns the count.</summary>
        public static int GenerateLegal(Position pos, Span<Move> moves)
        {
            Span<Move> pseudo = stackalloc Move[MaxMoves];
            int n = GeneratePseudoLegal(pos, pseudo);
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                Move m = pseudo[i];
                pos.MakeMove(m, out StateUndo u);
                Color mover = pos.SideToMove.Opposite();
                if (!pos.IsAttacked(pos.KingSquare(mover), pos.SideToMove, pos.OccAll))
                    moves[count++] = m;
                pos.UnmakeMove(m, u);
            }
            return count;
        }

        /// <summary>Generate pseudo-legal moves (own-king safety not yet verified, except castling).</summary>
        public static int GeneratePseudoLegal(Position pos, Span<Move> moves)
        {
            var buf = new Buf { M = moves, N = 0 };
            Color us = pos.SideToMove, them = us.Opposite();
            ulong own = pos.Occ[(int)us];
            ulong enemy = pos.Occ[(int)them];
            ulong occ = pos.OccAll;
            ulong empty = ~occ;

            GeneratePawns(pos, us, them, enemy, empty, ref buf);

            GenerateLeaper(pos.PieceBB(us, PieceType.Knight), own, enemy, KnightMode.Knight, occ, ref buf);
            GenerateSlider(pos.PieceBB(us, PieceType.Bishop), own, enemy, occ, SliderKind.Bishop, ref buf);
            GenerateSlider(pos.PieceBB(us, PieceType.Rook), own, enemy, occ, SliderKind.Rook, ref buf);
            GenerateSlider(pos.PieceBB(us, PieceType.Queen), own, enemy, occ, SliderKind.Queen, ref buf);
            GenerateLeaper(pos.PieceBB(us, PieceType.King), own, enemy, KnightMode.King, occ, ref buf);

            GenerateCastling(pos, us, them, ref buf);
            return buf.N;
        }

        private enum KnightMode { Knight, King }
        private enum SliderKind { Bishop, Rook, Queen }

        private static void GenerateLeaper(ulong from, ulong own, ulong enemy, KnightMode mode, ulong occ, ref Buf buf)
        {
            while (from != 0)
            {
                int sq = PopLsb(ref from);
                ulong att = (mode == KnightMode.Knight ? KnightAttacks(sq) : KingAttacks(sq)) & ~own;
                ulong quiets = att & ~enemy;
                ulong caps = att & enemy;
                while (quiets != 0) buf.Add(Move.Create(sq, PopLsb(ref quiets), MoveFlag.Quiet));
                while (caps != 0) buf.Add(Move.Create(sq, PopLsb(ref caps), MoveFlag.Capture));
            }
        }

        private static void GenerateSlider(ulong from, ulong own, ulong enemy, ulong occ, SliderKind kind, ref Buf buf)
        {
            while (from != 0)
            {
                int sq = PopLsb(ref from);
                ulong att = kind switch
                {
                    SliderKind.Bishop => BishopAttacks(sq, occ),
                    SliderKind.Rook => RookAttacks(sq, occ),
                    _ => QueenAttacks(sq, occ)
                } & ~own;
                ulong quiets = att & ~enemy;
                ulong caps = att & enemy;
                while (quiets != 0) buf.Add(Move.Create(sq, PopLsb(ref quiets), MoveFlag.Quiet));
                while (caps != 0) buf.Add(Move.Create(sq, PopLsb(ref caps), MoveFlag.Capture));
            }
        }

        private static void GeneratePawns(Position pos, Color us, Color them, ulong enemy, ulong empty, ref Buf buf)
        {
            ulong pawns = pos.PieceBB(us, PieceType.Pawn);
            int up = us == Color.White ? 8 : -8;
            ulong startRank = us == Color.White ? Rank2 : Rank7;
            ulong promoRank = us == Color.White ? Rank8 : Rank1;

            while (pawns != 0)
            {
                int from = PopLsb(ref pawns);
                int to = from + up;

                // Pushes.
                if ((empty & Bit(to)) != 0)
                {
                    if ((Bit(to) & promoRank) != 0) AddPromotions(from, to, capture: false, ref buf);
                    else
                    {
                        buf.Add(Move.Create(from, to, MoveFlag.Quiet));
                        if ((Bit(from) & startRank) != 0)
                        {
                            int to2 = from + 2 * up;
                            if ((empty & Bit(to2)) != 0) buf.Add(Move.Create(from, to2, MoveFlag.DoublePush));
                        }
                    }
                }

                // Captures.
                ulong caps = PawnAttacks(us, from) & enemy;
                while (caps != 0)
                {
                    int t = PopLsb(ref caps);
                    if ((Bit(t) & promoRank) != 0) AddPromotions(from, t, capture: true, ref buf);
                    else buf.Add(Move.Create(from, t, MoveFlag.Capture));
                }

                // En passant.
                if (pos.EnPassant != -1 && (PawnAttacks(us, from) & Bit(pos.EnPassant)) != 0)
                    buf.Add(Move.Create(from, pos.EnPassant, MoveFlag.EnPassant));
            }
        }

        private static void AddPromotions(int from, int to, bool capture, ref Buf buf)
        {
            if (capture)
            {
                buf.Add(Move.Create(from, to, MoveFlag.PromoCapQueen));
                buf.Add(Move.Create(from, to, MoveFlag.PromoCapRook));
                buf.Add(Move.Create(from, to, MoveFlag.PromoCapBishop));
                buf.Add(Move.Create(from, to, MoveFlag.PromoCapKnight));
            }
            else
            {
                buf.Add(Move.Create(from, to, MoveFlag.PromoQueen));
                buf.Add(Move.Create(from, to, MoveFlag.PromoRook));
                buf.Add(Move.Create(from, to, MoveFlag.PromoBishop));
                buf.Add(Move.Create(from, to, MoveFlag.PromoKnight));
            }
        }

        private static void GenerateCastling(Position pos, Color us, Color them, ref Buf buf)
        {
            ulong occ = pos.OccAll;
            if (us == Color.White)
            {
                if (pos.IsAttacked(4, them, occ)) return; // king in check: no castling either side
                if ((pos.Castling & CastleRight.WhiteKing) != 0 &&
                    (occ & (Bit(5) | Bit(6))) == 0 &&
                    !pos.IsAttacked(5, them, occ) && !pos.IsAttacked(6, them, occ))
                    buf.Add(Move.Create(4, 6, MoveFlag.KingCastle));
                if ((pos.Castling & CastleRight.WhiteQueen) != 0 &&
                    (occ & (Bit(1) | Bit(2) | Bit(3))) == 0 &&
                    !pos.IsAttacked(3, them, occ) && !pos.IsAttacked(2, them, occ))
                    buf.Add(Move.Create(4, 2, MoveFlag.QueenCastle));
            }
            else
            {
                if (pos.IsAttacked(60, them, occ)) return;
                if ((pos.Castling & CastleRight.BlackKing) != 0 &&
                    (occ & (Bit(61) | Bit(62))) == 0 &&
                    !pos.IsAttacked(61, them, occ) && !pos.IsAttacked(62, them, occ))
                    buf.Add(Move.Create(60, 62, MoveFlag.KingCastle));
                if ((pos.Castling & CastleRight.BlackQueen) != 0 &&
                    (occ & (Bit(57) | Bit(58) | Bit(59))) == 0 &&
                    !pos.IsAttacked(59, them, occ) && !pos.IsAttacked(58, them, occ))
                    buf.Add(Move.Create(60, 58, MoveFlag.QueenCastle));
            }
        }
    }
}
