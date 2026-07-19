using NUnit.Framework;
using CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>Terminal-state and draw-rule detection — the outcomes players trust.</summary>
    [TestFixture]
    public class RulesTests
    {
        private static void Play(GameState g, params string[] sans)
        {
            foreach (string san in sans) g.MakeMove(Pgn.FromSan(g.Position, san));
        }

        [Test]
        public void FoolsMate_IsCheckmate()
        {
            var g = new GameState();
            Play(g, "f3", "e5", "g4", "Qh4");
            var (result, reason) = g.GetResult();
            Assert.That(reason, Is.EqualTo(GameEndReason.Checkmate));
            Assert.That(result, Is.EqualTo(GameResult.BlackWins));
        }

        [Test]
        public void Stalemate_IsDraw()
        {
            var g = new GameState("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1");
            var (result, reason) = g.GetResult();
            Assert.That(reason, Is.EqualTo(GameEndReason.Stalemate));
            Assert.That(result, Is.EqualTo(GameResult.Draw));
        }

        [Test]
        public void FiftyMoveRule_IsDraw()
        {
            var g = new GameState("8/8/8/4k3/8/8/4N3/4K3 w - - 100 1");
            var (result, reason) = g.GetResult();
            Assert.That(reason, Is.EqualTo(GameEndReason.FiftyMove));
            Assert.That(result, Is.EqualTo(GameResult.Draw));
        }

        [TestCase("8/8/8/4k3/8/8/8/4K3 w - - 0 1")]        // K v K
        [TestCase("8/8/8/4k3/8/5N2/8/4K3 w - - 0 1")]      // K+N v K
        [TestCase("8/8/8/4k3/8/6B1/8/4K3 w - - 0 1")]      // K+B v K
        [TestCase("8/8/8/3bk3/8/5B2/8/4K3 w - - 0 1")]     // K+B v K+B, same-colour bishops
        public void InsufficientMaterial_IsDraw(string fen)
        {
            var g = new GameState(fen);
            var (result, reason) = g.GetResult();
            Assert.That(reason, Is.EqualTo(GameEndReason.InsufficientMaterial));
            Assert.That(result, Is.EqualTo(GameResult.Draw));
        }

        [Test]
        public void OppositeColourBishops_NotAutoDraw()
        {
            // K+B v K+B with bishops on opposite colours is NOT an automatic draw.
            var g = new GameState("8/8/8/3bk3/8/6B1/8/4K3 w - - 0 1");
            Assert.That(g.IsInsufficientMaterial(), Is.False);
        }

        [Test]
        public void ThreefoldRepetition_IsDraw()
        {
            var g = new GameState();
            Play(g, "Nf3", "Nf6", "Ng1", "Ng8", "Nf3", "Nf6", "Ng1", "Ng8");
            Assert.That(g.RepetitionCount(), Is.GreaterThanOrEqualTo(3));
            var (result, reason) = g.GetResult();
            Assert.That(reason, Is.EqualTo(GameEndReason.ThreefoldRepetition));
            Assert.That(result, Is.EqualTo(GameResult.Draw));
        }

        [Test]
        public void Clock_IncrementAndFlag()
        {
            var clock = new Clock(TimeControl.Blitz3_2); // 3+2
            Assert.That(clock.Remaining(Color.White), Is.EqualTo(180_000));
            bool flagged = clock.Press(Color.White, 5_000); // spent 5s
            Assert.That(flagged, Is.False);
            Assert.That(clock.Remaining(Color.White), Is.EqualTo(180_000 - 5_000 + 2_000));

            var bullet = new Clock(TimeControl.Bullet1_0);
            Assert.That(bullet.Press(Color.Black, 70_000), Is.True); // over the 60s base => flag
            Assert.That(bullet.HasFlagged(Color.Black), Is.True);
        }
    }
}
