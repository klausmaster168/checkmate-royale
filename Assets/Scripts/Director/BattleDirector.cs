using CheckmateRoyale.ChessCore;
using static CheckmateRoyale.ChessCore.Types;

namespace CheckmateRoyale.Director
{
    /// <summary>
    /// The heart of the app. Converts each committed move into a directed <see cref="ShotList"/>,
    /// deterministically and in well under 50ms. Per-game state (escalation budget, mode dial)
    /// lives here; per-game <see cref="WarMemory"/> is passed in via <see cref="DirectorInput"/>.
    ///
    /// Usage per move: call <see cref="Direct"/> (pure — safe to call repeatedly) to get the
    /// sequence, then <see cref="Commit"/> exactly once to advance war memory and the budget.
    /// <see cref="DirectAndCommit"/> does both.
    /// </summary>
    public sealed class BattleDirector
    {
        public ulong Seed { get; }
        public ModeDial Dial { get; set; }
        public EscalationBudget Budget { get; }

        /// <summary>Drama score at/above which a Cinema capture spends a slow-mo token. Default 50.</summary>
        public int SlowMoThreshold { get; set; } = 50;

        public BattleDirector(ulong seed, ModeDial dial = ModeDial.Cinema)
        {
            Seed = seed;
            Dial = dial;
            Budget = new EscalationBudget();
        }

        /// <summary>Plan the sequence for a move. Pure: does not mutate memory or budget.</summary>
        public ShotList Direct(in DirectorInput input)
        {
            MoveFacts facts = MoveFacts.From(input);
            NarrativeFacts nf = input.Memory.Evaluate(input.Move, input.Before, input.Ply);
            DramaScore drama = DramaScorer.Score(input, nf, facts);
            return ShotPlanner.Plan(input, drama, facts, Budget, Dial, SlowMoThreshold);
        }

        /// <summary>Advance war memory and the escalation budget for a directed move. Call once.</summary>
        public void Commit(in DirectorInput input, ShotList shot)
        {
            MoveFacts facts = MoveFacts.From(input);
            int swing = SignedSwingCp(input);
            input.Memory.RecordMove(input.Move, input.Before, input.Ply, facts.IsCheck, swing);
            Budget.Commit(input.Ply, shot.UsesSlowMo());
        }

        public ShotList DirectAndCommit(in DirectorInput input)
        {
            ShotList shot = Direct(input);
            Commit(input, shot);
            return shot;
        }

        internal static int SignedSwingCp(in DirectorInput input)
        {
            if (!input.EvalBefore.HasValue || !input.EvalAfter.HasValue) return 0;
            int a = Clamp((int)input.EvalAfter.Value, -800, 800);
            int b = Clamp((int)input.EvalBefore.Value, -800, 800);
            return a - b;
        }

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
