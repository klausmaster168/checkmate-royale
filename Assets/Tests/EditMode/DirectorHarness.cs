using System.Collections.Generic;
using CheckmateRoyale.ChessCore;
using CheckmateRoyale.ChessCore.Util;
using CheckmateRoyale.Director;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>Shared helpers for driving the Director over seeded random games.</summary>
    internal static class DirectorHarness
    {
        public struct Directed
        {
            public DirectorInput Input;
            public ShotList Shot;
            public bool WasCapture;
        }

        /// <summary>Play a seeded random game, directing (and committing) every move.</summary>
        public static List<Directed> PlayAndDirect(ulong gameSeed, ulong directorSeed, ModeDial dial, int maxPlies)
        {
            var rng = new Xoshiro256(gameSeed);
            var pos = Fen.Parse(Fen.StartPos);
            var memory = new WarMemory();
            memory.Init(pos);
            var director = new BattleDirector(directorSeed, dial);
            var buf = new Move[MoveGenerator.MaxMoves];
            var result = new List<Directed>(maxPlies);

            int ply = 1;
            while (result.Count < maxPlies)
            {
                int n = MoveGenerator.GenerateLegal(pos, buf);
                if (n == 0 || pos.HalfmoveClock >= 100) break;
                Move m = buf[rng.NextInt(n)];

                var before = pos.Clone();
                pos.MakeMove(m, out _);
                var after = pos.Clone();

                var input = new DirectorInput(m, before, after, null, null, ClockState.Untimed, memory, directorSeed, ply);
                var shot = director.DirectAndCommit(input);
                result.Add(new Directed { Input = input, Shot = shot, WasCapture = m.IsCapture });
                ply++;
            }
            return result;
        }

        public static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        /// <summary>
        /// Build a single focus move in context: parse <paramref name="fen"/>, replay
        /// <paramref name="setup"/> SAN moves (advancing war memory), optionally drain the
        /// budget, then return the DirectorInput + director for the <paramref name="focusSan"/> move.
        /// </summary>
        public static (DirectorInput input, BattleDirector director) BuildScenario(
            string fen, string[] setup, string focusSan,
            float? evalBefore, float? evalAfter, ClockState clock, ModeDial dial, ulong seed, int preSpend = 0)
        {
            var pos = Fen.Parse(fen);
            var memory = new WarMemory();
            memory.Init(pos);
            var director = new BattleDirector(seed, dial);
            for (int i = 0; i < preSpend; i++) director.Budget.Commit(0, true);

            int ply = 1;
            if (setup != null)
            {
                foreach (string san in setup)
                {
                    Move sm = Pgn.FromSan(pos, san);
                    var sb = pos.Clone();
                    pos.MakeMove(sm, out _);
                    var sa = pos.Clone();
                    var si = new DirectorInput(sm, sb, sa, null, null, ClockState.Untimed, memory, seed, ply);
                    director.DirectAndCommit(si);
                    ply++;
                }
            }

            Move fm = Pgn.FromSan(pos, focusSan);
            var before = pos.Clone();
            pos.MakeMove(fm, out _);
            var after = pos.Clone();
            var input = new DirectorInput(fm, before, after, evalBefore, evalAfter, clock, memory, seed, ply);
            return (input, director);
        }
    }
}
