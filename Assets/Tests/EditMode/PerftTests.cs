using NUnit.Framework;
using CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>
    /// The move generator's correctness oracle. Node counts are the standard published
    /// perft values; any mismatch means the rules are wrong somewhere. SACRED RULES.
    /// </summary>
    [TestFixture]
    public class PerftTests
    {
        private const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -";
        private const string Pos3 = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - -";
        private const string Pos4 = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq -";
        private const string Pos5 = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ -";

        [TestCase(1, 20L)]
        [TestCase(2, 400L)]
        [TestCase(3, 8902L)]
        [TestCase(4, 197281L)]
        [TestCase(5, 4865609L)]
        public void StartPos(int depth, long expected)
        {
            var pos = Fen.Parse(Fen.StartPos);
            Assert.That(Perft.Run(pos, depth), Is.EqualTo(expected));
        }

        [Test, Explicit("Slow: 119M nodes, CI runs depths 1-5 by default.")]
        public void StartPos_Depth6()
        {
            var pos = Fen.Parse(Fen.StartPos);
            Assert.That(Perft.Run(pos, 6), Is.EqualTo(119060324L));
        }

        [TestCase(Kiwipete, 1, 48L)]
        [TestCase(Kiwipete, 2, 2039L)]
        [TestCase(Kiwipete, 3, 97862L)]
        [TestCase(Kiwipete, 4, 4085603L)]
        [TestCase(Pos3, 1, 14L)]
        [TestCase(Pos3, 2, 191L)]
        [TestCase(Pos3, 3, 2812L)]
        [TestCase(Pos3, 4, 43238L)]
        [TestCase(Pos4, 1, 6L)]
        [TestCase(Pos4, 2, 264L)]
        [TestCase(Pos4, 3, 9467L)]
        [TestCase(Pos4, 4, 422333L)]
        [TestCase(Pos5, 1, 44L)]
        [TestCase(Pos5, 2, 1486L)]
        [TestCase(Pos5, 3, 62379L)]
        [TestCase(Pos5, 4, 2103487L)]
        public void NamedPositions(string fen, int depth, long expected)
        {
            var pos = Fen.Parse(fen);
            Assert.That(Perft.Run(pos, depth), Is.EqualTo(expected));
        }
    }
}
