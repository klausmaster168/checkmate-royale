using System.Collections.Generic;
using CheckmateRoyale.ChessCore.Util;

namespace CheckmateRoyale.Director
{
    /// <summary>
    /// Turns a scored move into a concrete <see cref="ShotList"/>. Beat structure and total
    /// duration follow the Mode Dial; per-beat camera/animation/VFX/audio/slow-mo choices are
    /// drawn from a seeded PRNG stable for (seed, ply) so variety never breaks determinism.
    /// </summary>
    public static class ShotPlanner
    {
        private const int AnimVariants = 3;
        private const int AudioVariants = 4;
        private static readonly float[] SlowMoChoices = { 0.5f, 0.4f, 0.35f };

        public static ShotList Plan(in DirectorInput input, in DramaScore drama, in MoveFacts facts, EscalationBudget budget, ModeDial dial, int slowMoThreshold = 50)
        {
            ModeDial eff = ResolveDial(dial, input.Clock);
            var rng = MoveRandom.For(input.DirectorSeed, input.Ply);
            int available = budget.PeekAvailable(input.Ply);

            List<Beat> beats;
            if (facts.IsMate) beats = FinisherBeats(eff, drama.Score, available, ref rng);
            else if (facts.IsCapture) beats = CaptureBeats(eff, drama.Score, available, ref rng, slowMoThreshold);
            else beats = QuietBeats(eff, ref rng);

            // A (non-mating) check gets a crane reveal — Cinema only.
            if (facts.IsCheck && !facts.IsMate && eff == ModeDial.Cinema)
            {
                Beat crane = Make(BeatType.CraneReveal, 0.6f, CameraRig.CraneReveal, VfxOf(drama.Score, false, available), 1.0f, ref rng);
                beats.Insert(beats.Count > 0 ? 1 : 0, crane);
            }

            return new ShotList
            {
                Ply = input.Ply,
                Dial = eff,
                DramaScoreValue = drama.Score,
                Tags = drama.Tags,
                Beats = beats.ToArray()
            };
        }

        /// <summary>Auto resolves by clock; a sub-30s clock forces Blitz pacing (fairness floor).</summary>
        public static ModeDial ResolveDial(ModeDial dial, ClockState clock)
        {
            if (clock.SecondsLeft < 30) return ModeDial.Blitz;
            if (dial != ModeDial.Auto) return dial;
            if (clock.SecondsLeft > 120) return ModeDial.Cinema;
            return ModeDial.Battle;
        }

        // ---- templates ----

        private static List<Beat> CaptureBeats(ModeDial dial, int score, int available, ref Xoshiro256 rng, int slowMoThreshold)
        {
            var b = new List<Beat>(6);
            bool slow = dial == ModeDial.Cinema && score >= slowMoThreshold && available > 0;

            switch (dial)
            {
                case ModeDial.Cinema:
                    b.Add(Make(BeatType.Confirm, 0.3f, CameraRig.Commander, 0, 1.0f, ref rng));
                    b.Add(Make(BeatType.Approach, 0.9f, CameraRig.DollyTrack, VfxStep(score, -2, available), 1.0f, ref rng));
                    b.Add(Make(BeatType.Impact, 0.5f, CameraRig.DuelOTS, VfxOf(score, true, available), slow ? SlowMoChoices[rng.NextInt(SlowMoChoices.Length)] : 1.0f, ref rng));
                    b.Add(Make(BeatType.Fall, 0.6f, CameraRig.Commander, VfxStep(score, -1, available), 1.0f, ref rng));
                    b.Add(Make(BeatType.Victor, 0.4f, CameraRig.Commander, 0, 1.0f, ref rng));
                    b.Add(Make(BeatType.Return, 0.5f, CameraRig.Commander, 0, 1.0f, ref rng));
                    break;
                case ModeDial.Battle:
                    b.Add(Make(BeatType.Approach, 0.4f, CameraRig.DollyTrack, VfxStep(score, -1, available), 1.0f, ref rng));
                    b.Add(Make(BeatType.Impact, 0.3f, CameraRig.DuelOTS, VfxOf(score, true, available), 1.0f, ref rng));
                    b.Add(Make(BeatType.Fall, 0.3f, CameraRig.Commander, 0, 1.0f, ref rng));
                    break;
                default: // Blitz
                    b.Add(Make(BeatType.Impact, 0.3f, CameraRig.DuelOTS, VfxOf(score, true, available), 1.0f, ref rng));
                    break;
            }
            return b;
        }

        private static List<Beat> QuietBeats(ModeDial dial, ref Xoshiro256 rng)
        {
            float dur = dial == ModeDial.Cinema ? 1.2f : (dial == ModeDial.Battle ? 0.5f : 0.2f);
            return new List<Beat> { Make(BeatType.March, dur, CameraRig.Commander, 0, 1.0f, ref rng) };
        }

        private static List<Beat> FinisherBeats(ModeDial dial, int score, int available, ref Xoshiro256 rng)
        {
            var b = new List<Beat>(5);
            bool slow = dial == ModeDial.Cinema && available > 0;
            float sf = slow ? SlowMoChoices[rng.NextInt(SlowMoChoices.Length)] : 1.0f;

            switch (dial)
            {
                case ModeDial.Cinema:
                    b.Add(Make(BeatType.Confirm, 0.3f, CameraRig.Commander, 0, 1.0f, ref rng));
                    b.Add(Make(BeatType.Approach, 0.6f, CameraRig.DollyTrack, VfxStep(score, -1, available), 1.0f, ref rng));
                    b.Add(Make(BeatType.Impact, 0.6f, CameraRig.OrbitalSloMo, 3, sf, ref rng));
                    b.Add(Make(BeatType.Finisher, 1.5f, CameraRig.OrbitalSloMo, 3, sf, ref rng));
                    b.Add(Make(BeatType.Return, 0.5f, CameraRig.Commander, 0, 1.0f, ref rng));
                    break;
                case ModeDial.Battle:
                    b.Add(Make(BeatType.Impact, 0.4f, CameraRig.DuelOTS, 3, 1.0f, ref rng));
                    b.Add(Make(BeatType.Finisher, 0.8f, CameraRig.OrbitalSloMo, 3, 1.0f, ref rng));
                    break;
                default: // Blitz
                    b.Add(Make(BeatType.Finisher, 0.5f, CameraRig.OrbitalSloMo, 2, 1.0f, ref rng));
                    break;
            }
            return b;
        }

        // ---- helpers ----

        private static Beat Make(BeatType type, float dur, CameraRig cam, byte vfx, float slowmo, ref Xoshiro256 rng)
        {
            // Draw order is fixed, so output is stable for a given (seed, ply).
            byte anim = (byte)rng.NextInt(AnimVariants);
            byte audio = (byte)rng.NextInt(AudioVariants);
            return new Beat
            {
                Type = type,
                Duration = dur,
                Camera = cam,
                AnimationIntentId = anim,
                VfxTier = vfx,
                SlowMoFactor = slowmo,
                AudioStingerId = audio
            };
        }

        // Base VFX tier from drama; the impact beat reads full tier, others step down.
        private static byte VfxOf(int score, bool isImpact, int available)
        {
            int tier = score >= 75 ? 3 : score >= 50 ? 2 : score >= 25 ? 1 : 0;
            if (available == 0 && isImpact && tier > 0) tier--; // exhausted budget => calmer peak
            return (byte)tier;
        }

        private static byte VfxStep(int score, int delta, int available)
        {
            int baseTier = VfxOf(score, false, available);
            int t = baseTier + delta;
            if (t < 0) t = 0; if (t > 3) t = 3;
            return (byte)t;
        }
    }
}
