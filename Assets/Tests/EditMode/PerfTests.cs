using System;
using System.Diagnostics;
using NUnit.Framework;
using CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>Performance gates from the phase spec: zero-alloc movegen and fast perft.</summary>
    [TestFixture]
    public class PerfTests
    {
        private const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -";

        [Test]
        public void Movegen_IsZeroAlloc_AfterWarmup()
        {
            var pos = Fen.Parse(Kiwipete);
            var buf = new Move[MoveGenerator.MaxMoves];

            for (int i = 0; i < 2000; i++) MoveGenerator.GenerateLegal(pos, buf); // warm up JIT/tiering

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100_000; i++) MoveGenerator.GenerateLegal(pos, buf);
            long delta = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(delta, Is.LessThan(1024), $"movegen allocated {delta} bytes over 100k calls");
        }

        [Test]
        public void Perft5_UnderBudget()
        {
            var pos = Fen.Parse(Fen.StartPos);
            var sw = Stopwatch.StartNew();
            long nodes = Perft.Run(pos, 5);
            sw.Stop();
            TestContext.Out.WriteLine($"perft(5) = {nodes} in {sw.ElapsedMilliseconds} ms");
            Assert.That(nodes, Is.EqualTo(4_865_609L));
            // Debug/CI runs are slower than the Release target (<2.5s); keep a generous ceiling here.
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(15_000));
        }
    }
}
