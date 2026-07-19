using NUnit.Framework;
using CheckmateRoyale.ChessCore;
using CheckmateRoyale.Director;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>Targeted checks that each drama tag fires on the intended situation.</summary>
    [TestFixture]
    public class DirectorScorerTests
    {
        private static DramaScore ScoreFocus(string fen, string[] setup, string focus,
            float? evalB = null, float? evalA = null, ClockState? clock = null)
        {
            var (input, _) = DirectorHarness.BuildScenario(
                fen, setup, focus, evalB, evalA, clock ?? ClockState.Untimed, ModeDial.Cinema, 0x5EEDUL);
            var facts = MoveFacts.From(input);
            var nf = input.Memory.Evaluate(input.Move, input.Before, input.Ply);
            return DramaScorer.Score(input, nf, facts);
        }

        [Test]
        public void FirstBlood_OnFirstCapture()
        {
            var s = ScoreFocus(Fen.StartPos, new[] { "e4", "d5" }, "exd5");
            Assert.That(s.Has(DramaTag.FirstBlood), Is.True);
        }

        [Test]
        public void Quiet_OnDevelopingMove()
        {
            var s = ScoreFocus(Fen.StartPos, null, "Nf3");
            Assert.That(s.Has(DramaTag.Quiet), Is.True);
        }

        [Test]
        public void Blunder_OnEvalCollapse()
        {
            var s = ScoreFocus(Fen.StartPos, null, "Nf3", evalB: 300f, evalA: -200f);
            Assert.That(s.Has(DramaTag.Blunder), Is.True); // signed swing -500 < -250
        }

        [Test]
        public void Brilliant_OnQueenSacWithEvalGain()
        {
            // Queen captures a defended rook (queen-for-rook sac) while eval jumps for the mover.
            var s = ScoreFocus("3rk3/8/8/8/8/8/8/3QK3 w - - 0 1", null, "Qxd8", evalB: 0f, evalA: 250f);
            Assert.That(s.Has(DramaTag.Brilliant), Is.True);
        }

        [Test]
        public void Revenge_OnRecapturingTheKiller()
        {
            // 1.exd5 Qxd5 (black queen kills white's pawn) 2.Nxd5 — the queen that killed our pawn falls.
            var s = ScoreFocus("rnbqkbnr/ppp1pppp/8/3p4/4P3/2N5/PPPP1PPP/R1BQKBNR w KQkq - 0 1",
                new[] { "exd5", "Qxd5" }, "Nxd5");
            Assert.That(s.Has(DramaTag.Revenge), Is.True);
        }

        [Test]
        public void Desperate_OnLowClock()
        {
            var s = ScoreFocus(Fen.StartPos, null, "e4", clock: new ClockState(20, true));
            Assert.That(s.Has(DramaTag.Desperate), Is.True);
        }

        [Test]
        public void Decisive_OnCheckmate()
        {
            var s = ScoreFocus("7k/5Q2/6K1/8/8/8/8/8 w - - 0 1", null, "Qg7");
            Assert.That(s.Has(DramaTag.Decisive), Is.True);
            Assert.That(s.Score, Is.GreaterThan(20));
        }
    }
}
