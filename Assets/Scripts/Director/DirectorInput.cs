using CheckmateRoyale.ChessCore;
using static CheckmateRoyale.ChessCore.Types;

namespace CheckmateRoyale.Director
{
    /// <summary>
    /// Everything the Director needs to direct one committed move. Engine evals are
    /// optional (null in early phases before Stockfish is wired). All fields are data;
    /// the Director derives its output purely from them plus the seed.
    /// </summary>
    public readonly struct DirectorInput
    {
        public readonly Move Move;
        public readonly Position Before;
        public readonly Position After;
        public readonly float? EvalBefore;   // centipawns from the mover's POV
        public readonly float? EvalAfter;    // centipawns from the mover's POV
        public readonly GamePhase Phase;
        public readonly ClockState Clock;
        public readonly WarMemory Memory;
        public readonly ulong DirectorSeed;
        public readonly int Ply;

        public DirectorInput(Move move, Position before, Position after,
                             float? evalBefore, float? evalAfter,
                             ClockState clock, WarMemory memory, ulong directorSeed, int ply)
        {
            Move = move;
            Before = before;
            After = after;
            EvalBefore = evalBefore;
            EvalAfter = evalAfter;
            Phase = PhaseOf(after);
            Clock = clock;
            Memory = memory;
            DirectorSeed = directorSeed;
            Ply = ply;
        }

        /// <summary>The mover's colour (the side that just moved) is the side NOT to move in <see cref="After"/>.</summary>
        public Color Mover => After.SideToMove.Opposite();

        /// <summary>Classify game phase by remaining non-pawn material on both sides.</summary>
        public static GamePhase PhaseOf(Position p)
        {
            int cp = 0;
            for (int c = 0; c < 2; c++)
            {
                Color col = (Color)c;
                cp += Bitboards.PopCount(p.PieceBB(col, PieceType.Knight)) * Values.Cp(PieceType.Knight);
                cp += Bitboards.PopCount(p.PieceBB(col, PieceType.Bishop)) * Values.Cp(PieceType.Bishop);
                cp += Bitboards.PopCount(p.PieceBB(col, PieceType.Rook)) * Values.Cp(PieceType.Rook);
                cp += Bitboards.PopCount(p.PieceBB(col, PieceType.Queen)) * Values.Cp(PieceType.Queen);
            }
            if (cp >= 5200) return GamePhase.Opening;
            if (cp <= 2600) return GamePhase.Endgame;
            return GamePhase.Middlegame;
        }
    }
}
