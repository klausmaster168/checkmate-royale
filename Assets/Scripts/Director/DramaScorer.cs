using System;
using System.Collections.Generic;
using CheckmateRoyale.ChessCore;
using static CheckmateRoyale.ChessCore.Types;
using static CheckmateRoyale.ChessCore.Attacks;
using static CheckmateRoyale.ChessCore.Bitboards;

namespace CheckmateRoyale.Director
{
    /// <summary>Tunable weights for the drama components (sum need not be 1; defaults per spec).</summary>
    public struct DramaScorerConfig
    {
        public double EvalSwing, MaterialEvent, KingDanger, MateDistance, NarrativeMemory, ClockPressure;

        public static DramaScorerConfig Default => new DramaScorerConfig
        {
            EvalSwing = 0.35,
            MaterialEvent = 0.20,
            KingDanger = 0.15,
            MateDistance = 0.15,
            NarrativeMemory = 0.10,
            ClockPressure = 0.05
        };
    }

    /// <summary>The Director's judgement of a move: a 0-100 score plus narrative tags.</summary>
    public readonly struct DramaScore
    {
        public readonly int Score;          // 0..100
        public readonly DramaTag[] Tags;

        public DramaScore(int score, DramaTag[] tags) { Score = score; Tags = tags; }
        public bool Has(DramaTag t) { foreach (var x in Tags) if (x == t) return true; return false; }
    }

    /// <summary>
    /// Pure, deterministic scoring of a committed move's dramatic weight. Every component
    /// is a pure function of the inputs; no randomness, no wall-clock, no engine calls.
    /// </summary>
    public static class DramaScorer
    {
        public static DramaScore Score(in DirectorInput input, in NarrativeFacts nf, in MoveFacts facts)
            => Score(input, nf, facts, DramaScorerConfig.Default);

        public static DramaScore Score(in DirectorInput input, in NarrativeFacts nf, in MoveFacts facts, DramaScorerConfig cfg)
        {
            Move m = input.Move;
            Position before = input.Before;
            Position after = input.After;
            Color mover = input.Mover;

            int signedSwing = SignedSwingCp(input);
            double evalSwing = Math.Abs(signedSwing) / 1600.0 * 100.0;

            double material = MaterialEvent(input, facts, mover);
            double kingDanger = KingDanger(after);
            double mateDistance = MateDistance(input, facts);
            double narrative = nf.Bonus;
            double clockPressure = input.Clock.SecondsLeft < 15 ? 100 : (input.Clock.SecondsLeft < 30 ? 60 : 0);

            double raw = cfg.EvalSwing * evalSwing
                       + cfg.MaterialEvent * material
                       + cfg.KingDanger * kingDanger
                       + cfg.MateDistance * mateDistance
                       + cfg.NarrativeMemory * narrative
                       + cfg.ClockPressure * clockPressure;

            int score = (int)Math.Round(Clamp(raw, 0, 100), MidpointRounding.AwayFromZero);

            var tags = BuildTags(input, nf, facts, mover, signedSwing, mateDistance, clockPressure, score);
            return new DramaScore(score, tags);
        }

        // ---- components ----

        private static int SignedSwingCp(in DirectorInput input)
        {
            if (!input.EvalBefore.HasValue || !input.EvalAfter.HasValue) return 0;
            int a = (int)Clamp(input.EvalAfter.Value, -800, 800);
            int b = (int)Clamp(input.EvalBefore.Value, -800, 800);
            return a - b;
        }

        private static double MaterialEvent(in DirectorInput input, in MoveFacts facts, Color mover)
        {
            Move m = input.Move;
            Position before = input.Before;
            int me = 0;

            if (facts.IsCapture)
            {
                int capSq = m.IsEnPassant ? (mover == Color.White ? m.To - 8 : m.To + 8) : m.To;
                PieceType victim = before.Board[capSq].TypeOf();
                PieceType capper = before.Board[m.From].TypeOf();
                int v = Values.Spice(victim);
                if (input.Memory.IsRecapture(m, input.Ply)) v += 15;
                if (Values.Cp(capper) < Values.Cp(victim)) v += 20; // "punching up"
                me = Math.Max(me, v);
            }
            if (facts.IsPromotion) me = Math.Max(me, 70);
            if (facts.IsCastle) me = Math.Max(me, 18);
            if (facts.IsCheck) me = Math.Max(me, 25);
            if (facts.IsEnPassant) me = Math.Max(me, 40);

            return Math.Min(me, 100);
        }

        private static double KingDanger(Position after)
        {
            Color defender = after.SideToMove;         // the side whose king is now under pressure
            Color attacker = defender.Opposite();
            int ksq = after.KingSquare(defender);
            ulong zone = Bit(ksq) | KingAttacks(ksq);

            int count = 0;
            ulong z = zone;
            while (z != 0)
            {
                int s = PopLsb(ref z);
                if (after.IsAttacked(s, attacker, after.OccAll)) count++;
            }
            double kd = count * 12.0;
            if (after.InCheck(defender)) kd += 30;
            return Math.Min(kd, 100);
        }

        private static double MateDistance(in DirectorInput input, in MoveFacts facts)
        {
            if (facts.IsMate) return 100;
            if (input.EvalAfter.HasValue)
            {
                float ev = input.EvalAfter.Value;
                if (ev >= 31000) return 80; // mate very soon for the mover
                if (ev >= 29000) return 50; // forced mate line known
            }
            return 0;
        }

        // ---- tags ----

        private static DramaTag[] BuildTags(in DirectorInput input, in NarrativeFacts nf, in MoveFacts facts,
                                            Color mover, int signedSwing, double mateDistance, double clockPressure, int score)
        {
            var tags = new List<DramaTag>(4);
            bool haveEvals = input.EvalBefore.HasValue && input.EvalAfter.HasValue;

            if (haveEvals && signedSwing < -250) tags.Add(DramaTag.Blunder);
            if (haveEvals && signedSwing >= 150 && IsStaticSacrifice(input, facts, mover)) tags.Add(DramaTag.Brilliant);
            if (nf.Revenge) tags.Add(DramaTag.Revenge);
            if (nf.Rampage) tags.Add(DramaTag.Rampage);
            if (clockPressure >= 60) tags.Add(DramaTag.Desperate);
            if (score < 20) tags.Add(DramaTag.Quiet);
            if (mateDistance >= 80) tags.Add(DramaTag.Decisive);
            if (nf.FirstBlood) tags.Add(DramaTag.FirstBlood);

            return tags.ToArray();
        }

        // A move that leaves >= 300cp of the mover's own material en prise on the destination square.
        private static bool IsStaticSacrifice(in DirectorInput input, in MoveFacts facts, Color mover)
        {
            Move m = input.Move;
            Position before = input.Before;
            Position after = input.After;

            PieceType moverType = before.Board[m.From].TypeOf();
            if (Values.Cp(moverType) < 300) return false;

            int gained = 0;
            if (facts.IsCapture)
            {
                int capSq = m.IsEnPassant ? (mover == Color.White ? m.To - 8 : m.To + 8) : m.To;
                gained = Values.Cp(before.Board[capSq].TypeOf());
            }
            int exposed = Values.Cp(moverType) - gained;
            if (exposed < 300) return false;

            return after.IsAttacked(m.To, mover.Opposite(), after.OccAll);
        }

        private static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
