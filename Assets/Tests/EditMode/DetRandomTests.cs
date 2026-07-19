using NUnit.Framework;
using CheckmateRoyale.ChessCore.Util;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>
    /// The PRNG underpins the DETERMINISTIC DIRECTION law: same seed must yield the same
    /// stream on every platform, forever. Golden constants are checked in below.
    /// </summary>
    [TestFixture]
    public class DetRandomTests
    {
        private const ulong Seed = 0x1234567890ABCDEFUL;

        // First 10 NextULong() outputs for Seed — regenerate deliberately if the algorithm changes.
        private static readonly ulong[] Golden =
        {
            0xF83D908D86AAC154UL, 0x293F15AE5365AA94UL, 0x8E7B24A84FDB3E3EUL, 0x74268D987564D9ADUL,
            0x6BA36875206D23E4UL, 0x2091C67CA5D304DCUL, 0x56D0C46CCA3952BBUL, 0x3DBFFC6B8D700073UL,
            0xC1FF7053C4E0CC6CUL, 0xC3480806490988DAUL
        };

        // XOR of the first 1000 outputs — a compact fingerprint of the whole stream.
        private const ulong GoldenXor1000 = 0x8274E7F38C34A91CUL;

        [Test]
        public void FirstOutputsMatchGolden()
        {
            var rng = new Xoshiro256(Seed);
            for (int i = 0; i < Golden.Length; i++)
                Assert.That(rng.NextULong(), Is.EqualTo(Golden[i]), $"mismatch at index {i}");
        }

        [Test]
        public void Thousand_Fingerprint()
        {
            var rng = new Xoshiro256(Seed);
            ulong acc = 0;
            for (int i = 0; i < 1000; i++) acc ^= rng.NextULong();
            Assert.That(acc, Is.EqualTo(GoldenXor1000));
        }

        [Test]
        public void SameSeedSameSequence()
        {
            var a = new Xoshiro256(Seed);
            var b = new Xoshiro256(Seed);
            for (int i = 0; i < 1000; i++)
                Assert.That(a.NextULong(), Is.EqualTo(b.NextULong()));
        }

        [Test]
        public void DifferentSeedDiffers()
        {
            var a = new Xoshiro256(Seed);
            var b = new Xoshiro256(Seed + 1);
            int same = 0;
            for (int i = 0; i < 1000; i++) if (a.NextULong() == b.NextULong()) same++;
            Assert.That(same, Is.LessThan(5)); // effectively zero collisions expected
        }

        [Test]
        public void NextInt_InRangeAndDeterministic()
        {
            var rng = new Xoshiro256(42UL);
            int[] expected = { 42, 2, 9, 93, 76, 84, 54, 7 };
            for (int i = 0; i < expected.Length; i++)
            {
                int v = rng.NextInt(100);
                Assert.That(v, Is.EqualTo(expected[i]));
                Assert.That(v, Is.InRange(0, 99));
            }
        }
    }
}
