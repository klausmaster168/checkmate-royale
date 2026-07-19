using System;
using System.Collections.Generic;
using static CheckmateRoyale.ChessCore.Types;

namespace CheckmateRoyale.ChessCore
{
    public enum GameResult { Ongoing, WhiteWins, BlackWins, Draw }

    public enum GameEndReason
    {
        None, Checkmate, Stalemate, FiftyMove, ThreefoldRepetition, InsufficientMaterial
    }

    /// <summary>
    /// A game in progress: the authoritative <see cref="Position"/> plus the history
    /// needed for repetition detection and unmake. Surfaces FIDE terminal states
    /// (mate, stalemate, 50-move, threefold, insufficient material).
    /// </summary>
    public sealed class GameState
    {
        public Position Position { get; }

        private readonly List<ulong> _hashHistory = new List<ulong>(128);
        private readonly List<Move> _moveHistory = new List<Move>(128);
        private readonly List<StateUndo> _undoHistory = new List<StateUndo>(128);

        public GameState(string fen = null)
        {
            Position = Fen.Parse(fen ?? Fen.StartPos);
            _hashHistory.Add(Position.Hash);
        }

        public Color SideToMove => Position.SideToMove;
        public bool InCheck => Position.InCheck(Position.SideToMove);
        public int PlyCount => _moveHistory.Count;
        public IReadOnlyList<Move> MoveHistory => _moveHistory;

        /// <summary>Fill <paramref name="buffer"/> with legal moves; returns the count.</summary>
        public int LegalMoves(Span<Move> buffer) => MoveGenerator.GenerateLegal(Position, buffer);

        public bool IsLegal(Move m)
        {
            Span<Move> buf = stackalloc Move[MoveGenerator.MaxMoves];
            int n = MoveGenerator.GenerateLegal(Position, buf);
            for (int i = 0; i < n; i++) if (buf[i] == m) return true;
            return false;
        }

        /// <summary>Apply a move and record it for repetition tracking and unmake.</summary>
        public void MakeMove(Move m)
        {
            Position.MakeMove(m, out StateUndo u);
            _moveHistory.Add(m);
            _undoHistory.Add(u);
            _hashHistory.Add(Position.Hash);
        }

        /// <summary>Reverse the most recent move.</summary>
        public void UnmakeLast()
        {
            if (_moveHistory.Count == 0) return;
            int last = _moveHistory.Count - 1;
            Position.UnmakeMove(_moveHistory[last], _undoHistory[last]);
            _moveHistory.RemoveAt(last);
            _undoHistory.RemoveAt(last);
            _hashHistory.RemoveAt(_hashHistory.Count - 1);
        }

        /// <summary>How many times the current position has occurred in this game (>=1).</summary>
        public int RepetitionCount()
        {
            ulong h = Position.Hash;
            int count = 0;
            for (int i = 0; i < _hashHistory.Count; i++)
                if (_hashHistory[i] == h) count++;
            return count;
        }

        public bool IsThreefoldRepetition => RepetitionCount() >= 3;
        public bool IsFiftyMoveRule => Position.HalfmoveClock >= 100;

        /// <summary>Conservative FIDE "no mate possible by any legal sequence" material check.</summary>
        public bool IsInsufficientMaterial()
        {
            // Any pawn, rook or queen means mate is possible.
            if (Position.PieceBB(Color.White, PieceType.Pawn) != 0 || Position.PieceBB(Color.Black, PieceType.Pawn) != 0) return false;
            if (Position.PieceBB(Color.White, PieceType.Rook) != 0 || Position.PieceBB(Color.Black, PieceType.Rook) != 0) return false;
            if (Position.PieceBB(Color.White, PieceType.Queen) != 0 || Position.PieceBB(Color.Black, PieceType.Queen) != 0) return false;

            int wn = Bitboards.PopCount(Position.PieceBB(Color.White, PieceType.Knight));
            int bn = Bitboards.PopCount(Position.PieceBB(Color.Black, PieceType.Knight));
            ulong wb = Position.PieceBB(Color.White, PieceType.Bishop);
            ulong bb = Position.PieceBB(Color.Black, PieceType.Bishop);
            int wbc = Bitboards.PopCount(wb);
            int bbc = Bitboards.PopCount(bb);

            int minorTotal = wn + bn + wbc + bbc;
            if (minorTotal == 0) return true;                       // K v K
            if (minorTotal == 1) return true;                       // K+minor v K
            if (minorTotal == 2 && wn == 0 && bn == 0 && wbc == 1 && bbc == 1)
            {
                // K+B v K+B is a dead draw only when both bishops are on the same colour.
                bool wLight = ((Bitboards.Lsb(wb) & 1) ^ ((Bitboards.Lsb(wb) >> 3) & 1)) == 0;
                bool bLight = ((Bitboards.Lsb(bb) & 1) ^ ((Bitboards.Lsb(bb) >> 3) & 1)) == 0;
                return wLight == bLight;
            }
            return false;
        }

        /// <summary>Current result and the reason. Terminal-move states take priority over the draw rules.</summary>
        public (GameResult result, GameEndReason reason) GetResult()
        {
            Span<Move> buf = stackalloc Move[MoveGenerator.MaxMoves];
            int n = MoveGenerator.GenerateLegal(Position, buf);
            if (n == 0)
            {
                if (Position.InCheck(Position.SideToMove))
                    return (Position.SideToMove == Color.White ? GameResult.BlackWins : GameResult.WhiteWins, GameEndReason.Checkmate);
                return (GameResult.Draw, GameEndReason.Stalemate);
            }
            if (IsFiftyMoveRule) return (GameResult.Draw, GameEndReason.FiftyMove);
            if (IsThreefoldRepetition) return (GameResult.Draw, GameEndReason.ThreefoldRepetition);
            if (IsInsufficientMaterial()) return (GameResult.Draw, GameEndReason.InsufficientMaterial);
            return (GameResult.Ongoing, GameEndReason.None);
        }

        public bool IsGameOver => GetResult().result != GameResult.Ongoing;
    }
}
