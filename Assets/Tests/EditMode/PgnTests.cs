using System;
using System.Text;
using NUnit.Framework;
using CheckmateRoyale.ChessCore;
using CheckmateRoyale.ChessCore.Util;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>PGN/SAN import/export exactness. encode → decode → encode must be stable.</summary>
    [TestFixture]
    public class PgnTests
    {
        // Morphy's "Opera Game", Paris 1858 — ends in a clean mate, exercises O-O-O,
        // disambiguation (Nbd7), checks and promotion-free tactics.
        private const string OperaGame =
            "[Event \"Paris\"]\n[Site \"Paris FRA\"]\n[Date \"1858.??.??\"]\n[Round \"?\"]\n" +
            "[White \"Paul Morphy\"]\n[Black \"Duke Karl / Count Isouard\"]\n[Result \"1-0\"]\n\n" +
            "1. e4 e5 2. Nf3 d6 3. d4 Bg4 4. dxe5 Bxf3 5. Qxf3 dxe5 6. Bc4 Nf6 7. Qb3 Qe7 " +
            "8. Nc3 c6 9. Bg5 b5 10. Nxb5 cxb5 11. Bxb5+ Nbd7 12. O-O-O Rd8 13. Rxd7 Rxd7 " +
            "14. Rd1 Qe6 15. Bxd7+ Nxd7 16. Qb8+ Nxb8 17. Rd8# 1-0\n";

        [Test]
        public void OperaGame_RoundTripsExactly()
        {
            var game = Pgn.Parse(OperaGame);
            Assert.That(game.Moves.Count, Is.EqualTo(33)); // 17 White + 16 Black
            Assert.That(game.Result, Is.EqualTo("1-0"));

            // Final move is mate.
            var pos = Fen.Parse(game.StartFen);
            foreach (var m in game.Moves) pos.MakeMove(m, out _);
            Assert.That(pos.InCheck(pos.SideToMove), Is.True);
            Span<Move> buf = stackalloc Move[MoveGenerator.MaxMoves];
            Assert.That(MoveGenerator.GenerateLegal(pos, buf), Is.EqualTo(0), "position should be checkmate");

            // encode -> decode -> encode stability.
            string written = Pgn.Write(game);
            var reparsed = Pgn.Parse(written);
            Assert.That(Moves(reparsed), Is.EqualTo(Moves(game)));
            Assert.That(Pgn.Write(reparsed), Is.EqualTo(written));
        }

        [Test]
        public void RandomGames_RoundTripThroughPgn()
        {
            var rng = new Xoshiro256(0xA5A5_1234_9876_F00DUL);
            var buf = new Move[MoveGenerator.MaxMoves];

            for (int game = 0; game < 25; game++)
            {
                var pos = Fen.Parse(Fen.StartPos);
                var pgnGame = new PgnGame();
                for (int ply = 0; ply < 40; ply++)
                {
                    int n = MoveGenerator.GenerateLegal(pos, buf);
                    if (n == 0) break;
                    Move m = buf[rng.NextInt(n)];
                    pgnGame.Moves.Add(m);
                    pos.MakeMove(m, out _);
                }

                string pgn = Pgn.Write(pgnGame);
                var reparsed = Pgn.Parse(pgn);
                Assert.That(Moves(reparsed), Is.EqualTo(Moves(pgnGame)), $"game {game} did not round-trip");
            }
        }

        private static string Moves(PgnGame g)
        {
            var sb = new StringBuilder();
            foreach (var m in g.Moves) sb.Append(m.ToUci()).Append(' ');
            return sb.ToString();
        }
    }
}
