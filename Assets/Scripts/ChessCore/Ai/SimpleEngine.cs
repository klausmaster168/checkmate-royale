using System;

namespace CheckmateRoyale.ChessCore.Ai
{
    /// <summary>
    /// A lightweight, deterministic chess opponent: negamax with alpha-beta pruning over a
    /// material + piece-square-table evaluation. Not a grandmaster (that's Stockfish, later) —
    /// a solid casual bot so the game is playable solo. Pure C#, no allocation in the search.
    /// </summary>
    public sealed class SimpleEngine
    {
        public const int Mate = 30000;
        private const int Inf = 1_000_000;

        /// <summary>Pick the best move for the side to move, searching to <paramref name="depth"/> plies.</summary>
        public Move ChooseMove(Position pos, int depth = 3)
        {
            Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
            int n = MoveGenerator.GenerateLegal(pos, moves);
            if (n == 0) return Move.Null;
            OrderCapturesFirst(pos, moves, n);

            int best = -Inf;
            Move bestMove = moves[0];
            int alpha = -Inf;
            for (int i = 0; i < n; i++)
            {
                pos.MakeMove(moves[i], out StateUndo u);
                int v = -Negamax(pos, depth - 1, -Inf, -alpha, 1);
                pos.UnmakeMove(moves[i], u);
                if (v > best) { best = v; bestMove = moves[i]; }
                if (best > alpha) alpha = best;
            }
            return bestMove;
        }

        private int Negamax(Position pos, int depth, int alpha, int beta, int ply)
        {
            Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
            int n = MoveGenerator.GenerateLegal(pos, moves);
            if (n == 0)
                return pos.InCheck(pos.SideToMove) ? -(Mate - ply) : 0; // checkmate (prefer sooner) / stalemate
            if (depth == 0)
                return Evaluate(pos);

            OrderCapturesFirst(pos, moves, n);
            int best = -Inf;
            for (int i = 0; i < n; i++)
            {
                pos.MakeMove(moves[i], out StateUndo u);
                int v = -Negamax(pos, depth - 1, -beta, -alpha, ply + 1);
                pos.UnmakeMove(moves[i], u);
                if (v > best) best = v;
                if (best > alpha) alpha = best;
                if (alpha >= beta) break; // beta cutoff
            }
            return best;
        }

        /// <summary>Static evaluation from the side-to-move's perspective (centipawns).</summary>
        public static int Evaluate(Position pos)
        {
            int white = 0, black = 0;
            for (int sq = 0; sq < 64; sq++)
            {
                Piece p = pos.Board[sq];
                if (p == Piece.None) continue;
                PieceType t = p.TypeOf();
                int v = Value(t);
                if (p.ColorOf() == Color.White) white += v + Pst(t)[sq ^ 56];
                else black += v + Pst(t)[sq];
            }
            int score = white - black;
            return pos.SideToMove == Color.White ? score : -score;
        }

        private static int Value(PieceType t) => t switch
        {
            PieceType.Pawn => 100,
            PieceType.Knight => 320,
            PieceType.Bishop => 330,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            _ => 0
        };

        // Move MVV-LVA-ish: captures (and promotions) to the front for better pruning.
        private static void OrderCapturesFirst(Position pos, Span<Move> moves, int n)
        {
            int front = 0;
            for (int i = 0; i < n; i++)
            {
                if (moves[i].IsCapture || moves[i].IsPromotion)
                {
                    (moves[front], moves[i]) = (moves[i], moves[front]);
                    front++;
                }
            }
        }

        // ---- piece-square tables (white view, listed a8..h1; read with sq^56 for White, sq for Black) ----
        private static int[] Pst(PieceType t) => t switch
        {
            PieceType.Pawn => PawnPst,
            PieceType.Knight => KnightPst,
            PieceType.Bishop => BishopPst,
            PieceType.Rook => RookPst,
            PieceType.Queen => QueenPst,
            _ => KingPst
        };

        private static readonly int[] PawnPst =
        {
             0,  0,  0,  0,  0,  0,  0,  0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
             5,  5, 10, 25, 25, 10,  5,  5,
             0,  0,  0, 20, 20,  0,  0,  0,
             5, -5,-10,  0,  0,-10, -5,  5,
             5, 10, 10,-20,-20, 10, 10,  5,
             0,  0,  0,  0,  0,  0,  0,  0
        };
        private static readonly int[] KnightPst =
        {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50
        };
        private static readonly int[] BishopPst =
        {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20
        };
        private static readonly int[] RookPst =
        {
             0,  0,  0,  0,  0,  0,  0,  0,
             5, 10, 10, 10, 10, 10, 10,  5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
             0,  0,  0,  5,  5,  0,  0,  0
        };
        private static readonly int[] QueenPst =
        {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20
        };
        private static readonly int[] KingPst =
        {
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -20,-30,-30,-40,-40,-30,-30,-20,
            -10,-20,-20,-20,-20,-20,-20,-10,
             20, 20,  0,  0,  0,  0, 20, 20,
             20, 30, 10,  0,  0, 10, 30, 20
        };
    }
}
