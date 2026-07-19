using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using CheckmateRoyale.Director;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>Duration invariants, escalation-budget rationing and Direct() performance.</summary>
    [TestFixture]
    public class DirectorDurationBudgetTests
    {
        private static float Target(bool capture, ModeDial dial) => dial switch
        {
            ModeDial.Cinema => capture ? 3.2f : 1.2f,
            ModeDial.Battle => capture ? 1.0f : 0.5f,
            _ => capture ? 0.3f : 0.2f
        };

        [TestCase(ModeDial.Cinema)]
        [TestCase(ModeDial.Battle)]
        [TestCase(ModeDial.Blitz)]
        public void CaptureAndQuiet_TotalsMatchDialTarget(ModeDial dial)
        {
            for (ulong g = 1; g <= 15; g++)
            {
                var game = DirectorHarness.PlayAndDirect(g, 0xABC0UL, dial, 90);
                foreach (var d in game)
                {
                    var f = MoveFacts.From(d.Input);
                    if (f.IsCheck || f.IsMate) continue; // check/mate templates intentionally differ

                    if (f.IsCapture)
                        Assert.That(d.Shot.TotalDuration, Is.EqualTo(Target(true, dial)).Within(0.05f),
                            $"capture total off (game {g})");
                    else if (f.IsQuiet)
                        Assert.That(d.Shot.TotalDuration, Is.EqualTo(Target(false, dial)).Within(0.05f),
                            $"quiet total off (game {g})");
                }
            }
        }

        [Test]
        public void SlowMo_NeverExceedsTokensEarned()
        {
            const int plies = 200;
            var game = DirectorHarness.PlayAndDirect(999, 0xBEEFUL, ModeDial.Cinema, plies);

            int slowMoBeats = 0;
            foreach (var d in game)
                foreach (var b in d.Shot.Beats)
                    if (b.SlowMoFactor < 1.0f) slowMoBeats++;

            int earned = EscalationBudget.Cap + game.Count / 10; // starting pool + regens
            Assert.That(slowMoBeats, Is.LessThanOrEqualTo(earned),
                $"used {slowMoBeats} slow-mo beats but only {earned} tokens were ever available");
        }

        [Test]
        public void ExhaustedBudget_LowersImpactIntensity()
        {
            // Same high-drama Cinema capture, with and without a drained budget.
            // Big eval swing => high drama score (>=50) so a full budget would spend slow-mo.
            var full = DirectorHarness.BuildScenario("3rk3/8/8/8/8/8/8/3QK3 w - - 0 1",
                null, "Qxd8", -800f, 800f, ClockState.Untimed, ModeDial.Cinema, 0x1234UL, preSpend: 0);
            var empty = DirectorHarness.BuildScenario("3rk3/8/8/8/8/8/8/3QK3 w - - 0 1",
                null, "Qxd8", -800f, 800f, ClockState.Untimed, ModeDial.Cinema, 0x1234UL, preSpend: EscalationBudget.Cap);

            var shotFull = full.director.Direct(full.input);
            var shotEmpty = empty.director.Direct(empty.input);

            Assert.That(shotFull.UsesSlowMo(), Is.True, "full budget should allow slow-mo");
            Assert.That(shotEmpty.UsesSlowMo(), Is.False, "drained budget should forbid slow-mo");
        }

        [Test]
        public void Direct_P99_Under50ms()
        {
            var game = DirectorHarness.PlayAndDirect(2024, 0xF00DUL, ModeDial.Cinema, 80);
            Assert.That(game.Count, Is.GreaterThan(0));

            var timings = new List<double>(10_000);
            var sw = new Stopwatch();
            for (int i = 0; i < 10_000; i++)
            {
                var d = game[i % game.Count];
                sw.Restart();
                d.Input.Memory.Evaluate(d.Input.Move, d.Input.Before, d.Input.Ply); // realistic path
                var shot = new BattleDirector(d.Input.DirectorSeed).Direct(d.Input);
                sw.Stop();
                timings.Add(sw.Elapsed.TotalMilliseconds);
                GC.KeepAlive(shot);
            }
            timings.Sort();
            double p99 = timings[(int)(timings.Count * 0.99)];
            TestContext.Out.WriteLine($"Direct p50={timings[timings.Count / 2]:F4}ms p99={p99:F4}ms");
            Assert.That(p99, Is.LessThan(50.0));
        }
    }
}
