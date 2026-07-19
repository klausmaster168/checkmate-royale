using System;
using System.Collections.Generic;
using System.Threading;

namespace CheckmateRoyale.ChessCore
{
    /// <summary>
    /// Perft (performance test): counts leaf nodes of the legal move tree to a fixed
    /// depth. The canonical correctness oracle for a move generator.
    /// </summary>
    public static class Perft
    {
        /// <summary>Count leaf nodes at <paramref name="depth"/> plies from <paramref name="pos"/>.</summary>
        public static long Run(Position pos, int depth)
        {
            if (depth == 0) return 1;
            Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
            int n = MoveGenerator.GenerateLegal(pos, moves);
            if (depth == 1) return n;

            long nodes = 0;
            for (int i = 0; i < n; i++)
            {
                pos.MakeMove(moves[i], out StateUndo u);
                nodes += Run(pos, depth - 1);
                pos.UnmakeMove(moves[i], u);
            }
            return nodes;
        }

        /// <summary>Cancellable perft for long-running UI/tooling calls.</summary>
        public static long Run(Position pos, int depth, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (depth == 0) return 1;
            Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
            int n = MoveGenerator.GenerateLegal(pos, moves);
            if (depth == 1) return n;

            long nodes = 0;
            for (int i = 0; i < n; i++)
            {
                pos.MakeMove(moves[i], out StateUndo u);
                nodes += Run(pos, depth - 1, ct);
                pos.UnmakeMove(moves[i], u);
            }
            return nodes;
        }

        /// <summary>Per-root-move node counts — the standard tool for locating a movegen bug.</summary>
        public static Dictionary<string, long> Divide(Position pos, int depth)
        {
            var result = new Dictionary<string, long>();
            Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
            int n = MoveGenerator.GenerateLegal(pos, moves);
            for (int i = 0; i < n; i++)
            {
                Move m = moves[i];
                pos.MakeMove(m, out StateUndo u);
                long nodes = depth <= 1 ? 1 : Run(pos, depth - 1);
                pos.UnmakeMove(m, u);
                result[m.ToUci()] = nodes;
            }
            return result;
        }
    }
}
