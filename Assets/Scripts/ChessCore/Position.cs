using System.Runtime.CompilerServices;
using static CheckmateRoyale.ChessCore.Bitboards;
using static CheckmateRoyale.ChessCore.Types;
using static CheckmateRoyale.ChessCore.Attacks;

namespace CheckmateRoyale.ChessCore
{
    /// <summary>Castling-right bit flags packed into a 4-bit nibble.</summary>
    public static class CastleRight
    {
        public const int WhiteKing = 1;
        public const int WhiteQueen = 2;
        public const int BlackKing = 4;
        public const int BlackQueen = 8;
        public const int All = 15;
    }

    /// <summary>Everything needed to exactly reverse a <see cref="Position.MakeMove"/>.</summary>
    public struct StateUndo
    {
        public Piece Captured;
        public int Castling;
        public int EnPassant;
        public int HalfmoveClock;
        public ulong Hash;
    }

    /// <summary>
    /// Authoritative board state: twelve piece bitboards, a mailbox for fast lookup,
    /// side to move, castling rights, en-passant square, clocks and an incrementally
    /// maintained Zobrist hash. Make/unmake restores the prior state exactly.
    /// </summary>
    public sealed class Position
    {
        public readonly ulong[] Pieces = new ulong[12];
        public readonly ulong[] Occ = new ulong[2];
        public ulong OccAll;
        public readonly Piece[] Board = new Piece[64];

        public Color SideToMove;
        public int Castling;      // CastleRight nibble
        public int EnPassant;     // target square (0..63) or -1
        public int HalfmoveClock;
        public int FullmoveNumber;
        public ulong Hash;

        // When a piece leaves/enters one of these squares, castling rights are cleared.
        private static readonly int[] CastleMask = BuildCastleMask();

        private static int[] BuildCastleMask()
        {
            var m = new int[64];
            for (int i = 0; i < 64; i++) m[i] = CastleRight.All;
            m[4] = CastleRight.All & ~(CastleRight.WhiteKing | CastleRight.WhiteQueen);  // e1
            m[0] = CastleRight.All & ~CastleRight.WhiteQueen;                            // a1
            m[7] = CastleRight.All & ~CastleRight.WhiteKing;                             // h1
            m[60] = CastleRight.All & ~(CastleRight.BlackKing | CastleRight.BlackQueen); // e8
            m[56] = CastleRight.All & ~CastleRight.BlackQueen;                           // a8
            m[63] = CastleRight.All & ~CastleRight.BlackKing;                            // h8
            return m;
        }

        public Position() { Clear(); }

        public void Clear()
        {
            for (int i = 0; i < 12; i++) Pieces[i] = 0;
            Occ[0] = Occ[1] = 0; OccAll = 0;
            for (int i = 0; i < 64; i++) Board[i] = Piece.None;
            SideToMove = Color.White;
            Castling = 0; EnPassant = -1; HalfmoveClock = 0; FullmoveNumber = 1; Hash = 0;
        }

        public Position Clone()
        {
            var p = new Position();
            System.Array.Copy(Pieces, p.Pieces, 12);
            System.Array.Copy(Occ, p.Occ, 2);
            System.Array.Copy(Board, p.Board, 64);
            p.OccAll = OccAll;
            p.SideToMove = SideToMove;
            p.Castling = Castling; p.EnPassant = EnPassant;
            p.HalfmoveClock = HalfmoveClock; p.FullmoveNumber = FullmoveNumber;
            p.Hash = Hash;
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Piece PieceAt(int sq) => Board[sq];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong PieceBB(Color c, PieceType t) => Pieces[MakePiece(c, t).Index()];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int KingSquare(Color c) => Lsb(Pieces[MakePiece(c, PieceType.King).Index()]);

        // ---- placement primitives (also maintain the hash) ----
        public void AddPiece(int sq, Piece p)
        {
            int idx = p.Index(); ulong b = Bit(sq);
            Pieces[idx] |= b; Occ[(int)p.ColorOf()] |= b; OccAll |= b; Board[sq] = p;
            Hash ^= Zobrist.Piece[idx][sq];
        }

        public void RemovePiece(int sq)
        {
            Piece p = Board[sq]; int idx = p.Index(); ulong b = Bit(sq);
            Pieces[idx] &= ~b; Occ[(int)p.ColorOf()] &= ~b; OccAll &= ~b; Board[sq] = Piece.None;
            Hash ^= Zobrist.Piece[idx][sq];
        }

        private void MovePiece(int from, int to)
        {
            Piece p = Board[from];
            RemovePiece(from);
            AddPiece(to, p);
        }

        /// <summary>Recompute the Zobrist hash from scratch (used by FEN load and DEBUG asserts).</summary>
        public ulong ComputeHash()
        {
            ulong h = 0;
            for (int sq = 0; sq < 64; sq++)
            {
                Piece p = Board[sq];
                if (p != Piece.None) h ^= Zobrist.Piece[p.Index()][sq];
            }
            if (SideToMove == Color.Black) h ^= Zobrist.Side;
            h ^= Zobrist.Castling[Castling];
            if (EnPassant != -1) h ^= Zobrist.EnPassantFile[FileOf(EnPassant)];
            return h;
        }

        /// <summary>Is <paramref name="sq"/> attacked by any piece of <paramref name="by"/> given <paramref name="occ"/>?</summary>
        public bool IsAttacked(int sq, Color by, ulong occ)
        {
            ulong pawns = Pieces[MakePiece(by, PieceType.Pawn).Index()];
            if ((PawnAttacks(by.Opposite(), sq) & pawns) != 0) return true;
            if ((KnightAttacks(sq) & Pieces[MakePiece(by, PieceType.Knight).Index()]) != 0) return true;
            if ((KingAttacks(sq) & Pieces[MakePiece(by, PieceType.King).Index()]) != 0) return true;

            ulong bishopsQueens = Pieces[MakePiece(by, PieceType.Bishop).Index()] | Pieces[MakePiece(by, PieceType.Queen).Index()];
            if ((BishopAttacks(sq, occ) & bishopsQueens) != 0) return true;
            ulong rooksQueens = Pieces[MakePiece(by, PieceType.Rook).Index()] | Pieces[MakePiece(by, PieceType.Queen).Index()];
            if ((RookAttacks(sq, occ) & rooksQueens) != 0) return true;
            return false;
        }

        public bool InCheck(Color c) => IsAttacked(KingSquare(c), c.Opposite(), OccAll);

        /// <summary>Apply a legal or pseudo-legal move, filling <paramref name="undo"/> for reversal.</summary>
        public void MakeMove(in Move m, out StateUndo undo)
        {
            undo.Captured = Piece.None;
            undo.Castling = Castling;
            undo.EnPassant = EnPassant;
            undo.HalfmoveClock = HalfmoveClock;
            undo.Hash = Hash;

            int from = m.From, to = m.To;
            MoveFlag flag = m.Flag;
            Color us = SideToMove, them = us.Opposite();
            Piece pc = Board[from];
            bool pawnMove = pc.TypeOf() == PieceType.Pawn;

            if (EnPassant != -1) Hash ^= Zobrist.EnPassantFile[FileOf(EnPassant)];

            // Captures.
            if (flag == MoveFlag.EnPassant)
            {
                int capSq = us == Color.White ? to - 8 : to + 8;
                undo.Captured = Board[capSq];
                RemovePiece(capSq);
            }
            else if (m.IsCapture)
            {
                undo.Captured = Board[to];
                RemovePiece(to);
            }

            // Mover.
            RemovePiece(from);
            if (m.IsPromotion) AddPiece(to, MakePiece(us, m.Promotion));
            else AddPiece(to, pc);

            // Castling rook hop.
            if (flag == MoveFlag.KingCastle) MovePiece(us == Color.White ? 7 : 63, us == Color.White ? 5 : 61);
            else if (flag == MoveFlag.QueenCastle) MovePiece(us == Color.White ? 0 : 56, us == Color.White ? 3 : 59);

            // Castling rights.
            int newCastling = Castling & CastleMask[from] & CastleMask[to];
            if (newCastling != Castling)
            {
                Hash ^= Zobrist.Castling[Castling];
                Hash ^= Zobrist.Castling[newCastling];
                Castling = newCastling;
            }

            // En-passant target.
            EnPassant = flag == MoveFlag.DoublePush ? (us == Color.White ? from + 8 : from - 8) : -1;
            if (EnPassant != -1) Hash ^= Zobrist.EnPassantFile[FileOf(EnPassant)];

            // Clocks.
            HalfmoveClock = (pawnMove || m.IsCapture) ? 0 : HalfmoveClock + 1;
            if (us == Color.Black) FullmoveNumber++;

            SideToMove = them;
            Hash ^= Zobrist.Side;
        }

        /// <summary>Exactly reverse a <see cref="MakeMove"/>.</summary>
        public void UnmakeMove(in Move m, in StateUndo undo)
        {
            SideToMove = SideToMove.Opposite();
            Color us = SideToMove;
            int from = m.From, to = m.To;
            MoveFlag flag = m.Flag;

            if (us == Color.Black) FullmoveNumber--;

            // Move the (possibly promoted) piece back.
            if (m.IsPromotion)
            {
                RemovePiece(to);
                AddPiece(from, MakePiece(us, PieceType.Pawn));
            }
            else
            {
                MovePiece(to, from);
            }

            // Restore captured material.
            if (flag == MoveFlag.EnPassant)
            {
                int capSq = us == Color.White ? to - 8 : to + 8;
                AddPiece(capSq, undo.Captured);
            }
            else if (m.IsCapture)
            {
                AddPiece(to, undo.Captured);
            }

            // Restore the castling rook.
            if (flag == MoveFlag.KingCastle) MovePiece(us == Color.White ? 5 : 61, us == Color.White ? 7 : 63);
            else if (flag == MoveFlag.QueenCastle) MovePiece(us == Color.White ? 3 : 59, us == Color.White ? 0 : 56);

            Castling = undo.Castling;
            EnPassant = undo.EnPassant;
            HalfmoveClock = undo.HalfmoveClock;
            Hash = undo.Hash; // overrides the incremental changes above — exact restoration
        }
    }
}
