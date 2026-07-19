using System.Collections.Generic;
using NUnit.Framework;
using CheckmateRoyale.ChessCore;
using CheckmateRoyale.ChessCore.Util;
using CheckmateRoyale.Director;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>The DETERMINISTIC DIRECTION law: same inputs + seed => byte-identical output.</summary>
    [TestFixture]
    public class DirectorDeterminismTests
    {
        [Test]
        public void FullGame_SameSeed_ByteIdentical()
        {
            for (ulong g = 1; g <= 20; g++)
            {
                var a = DirectorHarness.PlayAndDirect(g, 0xD1_1EC7_0000_0000UL, ModeDial.Cinema, 80);
                var b = DirectorHarness.PlayAndDirect(g, 0xD1_1EC7_0000_0000UL, ModeDial.Cinema, 80);
                Assert.That(a.Count, Is.EqualTo(b.Count));
                for (int i = 0; i < a.Count; i++)
                    Assert.That(DirectorHarness.BytesEqual(a[i].Shot.ToBytes(), b[i].Shot.ToBytes()), Is.True,
                        $"game {g} ply {i} diverged");
            }
        }

        [Test]
        public void DirectTwice_WithoutCommit_IsIdentical()
        {
            // Direct() must be pure: calling it repeatedly on the same state yields the same bytes.
            var rng = new Xoshiro256(0x9999UL);
            var pos = Fen.Parse(Fen.StartPos);
            var memory = new WarMemory(); memory.Init(pos);
            var director = new BattleDirector(0xABCDEFUL, ModeDial.Cinema);
            var buf = new Move[MoveGenerator.MaxMoves];

            for (int ply = 1; ply <= 60; ply++)
            {
                int n = MoveGenerator.GenerateLegal(pos, buf);
                if (n == 0) break;
                Move m = buf[rng.NextInt(n)];
                var before = pos.Clone();
                pos.MakeMove(m, out _);
                var after = pos.Clone();
                var input = new DirectorInput(m, before, after, null, null, ClockState.Untimed, memory, 0xABCDEFUL, ply);

                byte[] first = director.Direct(input).ToBytes();
                byte[] second = director.Direct(input).ToBytes();
                Assert.That(DirectorHarness.BytesEqual(first, second), Is.True, $"Direct not pure at ply {ply}");

                director.Commit(input, director.Direct(input));
            }
        }

        [Test]
        public void DifferentSeed_DiffersOver30Percent()
        {
            const ulong game = 12345;
            var a = DirectorHarness.PlayAndDirect(game, 0x1111_0000UL, ModeDial.Cinema, 80);
            var b = DirectorHarness.PlayAndDirect(game, 0x2222_0000UL, ModeDial.Cinema, 80);

            int n = System.Math.Min(a.Count, b.Count);
            int diff = 0;
            for (int i = 0; i < n; i++)
                if (!DirectorHarness.BytesEqual(a[i].Shot.ToBytes(), b[i].Shot.ToBytes())) diff++;

            Assert.That(diff / (double)n, Is.GreaterThan(0.30), $"only {diff}/{n} shotlists differ across seeds");
        }

        [Test]
        public void WarMemory_SerializationRoundTrips()
        {
            var directed = DirectorHarness.PlayAndDirect(777, 0xFEE1UL, ModeDial.Cinema, 60);
            var mem = directed[directed.Count - 1].Input.Memory;
            byte[] bytes = mem.ToBytes();
            Assert.That(bytes.Length, Is.LessThanOrEqualTo(2048), "WarMemory must fit in <=2KB");
            var restored = WarMemory.FromBytes(bytes);
            Assert.That(DirectorHarness.BytesEqual(bytes, restored.ToBytes()), Is.True);
        }
    }
}
