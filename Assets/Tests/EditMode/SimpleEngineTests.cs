using NUnit.Framework;
using CheckmateRoyale.ChessCore;
using CheckmateRoyale.ChessCore.Ai;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>The starter AI opponent: legal, finds simple tactics, deterministic.</summary>
    [TestFixture]
    public class SimpleEngineTests
    {
        private readonly SimpleEngine _engine = new SimpleEngine();

        [Test]
        public void ChoosesALegalMove_FromStart()
        {
            var pos = Fen.Parse(Fen.StartPos);
            Move m = _engine.ChooseMove(pos, 3);
            Assert.That(m.IsNull, Is.False);

            System.Span<Move> buf = stackalloc Move[MoveGenerator.MaxMoves];
            int n = MoveGenerator.GenerateLegal(pos, buf);
            bool legal = false;
            for (int i = 0; i < n; i++) if (buf[i] == m) legal = true;
            Assert.That(legal, Is.True, "engine move must be legal");
        }

        [Test]
        public void FindsMateInOne()
        {
            // 6k1/5ppp/8/8/8/8/5PPP/R5K1 w — Ra1-a8 is mate.
            var pos = Fen.Parse("6k1/5ppp/8/8/8/8/5PPP/R5K1 w - - 0 1");
            Move m = _engine.ChooseMove(pos, 3);
            Assert.That(m.From, Is.EqualTo(0), "should move the a1 rook");
            Assert.That(m.To, Is.EqualTo(56), "Ra8# is the mate");
        }

        [Test]
        public void GrabsAHangingQueen()
        {
            // White Qd1 can take an undefended black queen on d4.
            var pos = Fen.Parse("4k3/8/8/8/3q4/8/8/3QK3 w - - 0 1");
            Move m = _engine.ChooseMove(pos, 3);
            Assert.That(m.From, Is.EqualTo(3), "should move the d1 queen");
            Assert.That(m.To, Is.EqualTo(27), "Qxd4 wins the free queen");
        }

        [Test]
        public void IsDeterministic()
        {
            var pos = Fen.Parse("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 0 1");
            Move a = _engine.ChooseMove(pos, 3);
            Move b = _engine.ChooseMove(pos, 3);
            Assert.That(b, Is.EqualTo(a), "same position must yield the same move");
        }

        [Test]
        public void Varied_TopN1_EqualsBest()
        {
            var pos = Fen.Parse(Fen.StartPos);
            Assert.That(_engine.ChooseMoveVaried(pos, 3, 1), Is.EqualTo(_engine.ChooseMove(pos, 3)));
        }

        [Test]
        public void Varied_ReturnsALegalMove()
        {
            var pos = Fen.Parse(Fen.StartPos);
            Move m = _engine.ChooseMoveVaried(pos, 2, 3);
            System.Span<Move> buf = stackalloc Move[MoveGenerator.MaxMoves];
            int n = MoveGenerator.GenerateLegal(pos, buf);
            bool legal = false;
            for (int i = 0; i < n; i++) if (buf[i] == m) legal = true;
            Assert.That(legal, Is.True);
        }
    }
}
