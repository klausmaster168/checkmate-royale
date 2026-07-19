using System.Collections.Generic;
using NUnit.Framework;
using CheckmateRoyale.ChessCore;
using CheckmateRoyale.ChessCore.Util;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>Hash integrity and FEN exactness under heavy random play.</summary>
    [TestFixture]
    public class ZobristFenTests
    {
        [Test]
        public void IncrementalHash_MatchesRecompute_Over10000Moves()
        {
            var rng = new Xoshiro256(0xBADC0FFEE0DDF00DUL);
            var pos = Fen.Parse(Fen.StartPos);
            var buf = new Move[MoveGenerator.MaxMoves];
            int made = 0;

            while (made < 10_000)
            {
                int n = MoveGenerator.GenerateLegal(pos, buf);
                if (n == 0 || pos.HalfmoveClock >= 100) { pos = Fen.Parse(Fen.StartPos); continue; }
                Move m = buf[rng.NextInt(n)];
                pos.MakeMove(m, out _);
                Assert.That(pos.Hash, Is.EqualTo(pos.ComputeHash()), "incremental hash drifted from scratch recompute");
                made++;
            }
        }

        [Test]
        public void MakeUnmake_RestoresHashAndFen()
        {
            var rng = new Xoshiro256(0x0123456789ABCDEFUL);
            var buf = new Move[MoveGenerator.MaxMoves];

            for (int game = 0; game < 300; game++)
            {
                var pos = Fen.Parse(Fen.StartPos);
                ulong startHash = pos.Hash;
                string startFen = Fen.ToFen(pos);

                var moves = new List<Move>();
                var undos = new List<StateUndo>();
                for (int ply = 0; ply < 30; ply++)
                {
                    int n = MoveGenerator.GenerateLegal(pos, buf);
                    if (n == 0) break;
                    Move m = buf[rng.NextInt(n)];
                    pos.MakeMove(m, out StateUndo u);
                    moves.Add(m); undos.Add(u);
                }
                for (int i = moves.Count - 1; i >= 0; i--) pos.UnmakeMove(moves[i], undos[i]);

                Assert.That(pos.Hash, Is.EqualTo(startHash), "hash not restored after unmake");
                Assert.That(Fen.ToFen(pos), Is.EqualTo(startFen), "FEN not restored after unmake");
            }
        }

        [Test]
        public void Fen_RoundTrips_On50RandomPositions()
        {
            var rng = new Xoshiro256(0xFEEDFACECAFEBEEFUL);
            var buf = new Move[MoveGenerator.MaxMoves];
            int verified = 0;
            var pos = Fen.Parse(Fen.StartPos);

            while (verified < 50)
            {
                int n = MoveGenerator.GenerateLegal(pos, buf);
                if (n == 0 || pos.HalfmoveClock >= 100) { pos = Fen.Parse(Fen.StartPos); continue; }
                pos.MakeMove(buf[rng.NextInt(n)], out _);

                string fen = Fen.ToFen(pos);
                var reparsed = Fen.Parse(fen);
                Assert.That(Fen.ToFen(reparsed), Is.EqualTo(fen), "FEN did not round-trip exactly");
                Assert.That(reparsed.Hash, Is.EqualTo(pos.Hash), "hash differs after FEN round-trip");
                verified++;
            }
        }
    }
}
