using System;
using System.Collections.Generic;
using NUnit.Framework;
using CheckmateRoyale.ChessCore;
using CheckmateRoyale.Director;

namespace CheckmateRoyale.Tests.EditMode
{
    /// <summary>
    /// Golden ShotLists for 12 signature scenarios. Any change to scorer/planner/serialization
    /// that alters output fails here — intentional updates require regenerating with CMR_REGEN=1.
    /// </summary>
    [TestFixture]
    public class DirectorGoldenTests
    {
        private const ulong Seed = 0xC5EED_1234_5678UL;

        private sealed class Scenario
        {
            public string Name, Fen, Focus;
            public string[] Setup;
            public float? EvalB, EvalA;
            public ClockState Clock = ClockState.Untimed;
            public ModeDial Dial = ModeDial.Cinema;
            public int PreSpend;
        }

        private static readonly Scenario[] Scenarios =
        {
            new Scenario { Name = "first_blood",       Fen = Fen.StartPos, Setup = new[] { "e4", "d5" }, Focus = "exd5" },
            new Scenario { Name = "quiet_maneuver",    Fen = Fen.StartPos, Focus = "Nf3" },
            new Scenario { Name = "queen_sac_brilliant", Fen = "3rk3/8/8/8/8/8/8/3QK3 w - - 0 1", Focus = "Qxd8", EvalB = 0f, EvalA = 250f },
            new Scenario { Name = "revenge",           Fen = "rnbqkbnr/ppp1pppp/8/3p4/4P3/2N5/PPPP1PPP/R1BQKBNR w KQkq - 0 1", Setup = new[] { "exd5", "Qxd5" }, Focus = "Nxd5" },
            new Scenario { Name = "en_passant",        Fen = "rnbqkbnr/ppppp1pp/8/4Pp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 1", Focus = "exf6" },
            new Scenario { Name = "castling",          Fen = "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", Focus = "O-O" },
            new Scenario { Name = "promotion",         Fen = "8/P6k/8/8/8/8/8/7K w - - 0 1", Focus = "a8=Q" },
            new Scenario { Name = "mate_finisher",     Fen = "7k/5Q2/6K1/8/8/8/8/8 w - - 0 1", Focus = "Qg7" },
            new Scenario { Name = "blunder",           Fen = Fen.StartPos, Focus = "Nf3", EvalB = 300f, EvalA = -200f },
            new Scenario { Name = "low_clock_desperate", Fen = Fen.StartPos, Focus = "e4", Clock = new ClockState(20, true) },
            new Scenario { Name = "exhausted_budget",  Fen = "3rk3/8/8/8/8/8/8/3QK3 w - - 0 1", Focus = "Qxd8", EvalB = -800f, EvalA = 800f, PreSpend = EscalationBudget.Cap },
            new Scenario { Name = "blitz_dial",        Fen = "rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 1", Focus = "exd5", Dial = ModeDial.Blitz },
        };

        // Golden serialized ShotLists (base64). Regenerate deliberately with CMR_REGEN=1.
        private static readonly Dictionary<string, string> Golden = new Dictionary<string, string>
        {
            ["first_blood"] = "AQAAAAMAAAAAAgAAAAIFBwYAmpmZPgAAAAAAgD8BAmZmZj8BAQAAAIA/AwMAAAA/AgIAAACAPwIEmpkZPwAAAAAAgD8ABc3MzD4AAgAAAIA/AgYAAAA/AAEAAACAPwE=",
            ["quiet_maneuver"] = "AQAAAAEAAAAAAAAAAAEFAQGamZk/AAEAAACAPwM=",
            ["queen_sac_brilliant"] = "AQAAAAEAAAAAGgAAAAIBBwcAmpmZPgABAAAAgD8DB5qZGT8DAQEAAIA/AgJmZmY/AQAAAACAPwADAAAAPwIBAQAAgD8CBJqZGT8AAgAAAIA/AAXNzMw+AAAAAACAPwIGAAAAPwAAAAAAgD8B",
            ["revenge"] = "AQAAAAMAAAAAHAAAAAECBgCamZk+AAAAAACAPwECZmZmPwEBAAAAgD8DAwAAAD8CAgEAAIA/AgSamRk/AAAAAACAPwAFzczMPgACAAAAgD8CBgAAAD8AAQAAAIA/AQ==",
            ["en_passant"] = "AQAAAAEAAAAACgAAAAIFBwYAmpmZPgABAAAAgD8DAmZmZj8BAAAAAIA/AAMAAAA/AgEAAACAPwIEmpkZPwACAAAAgD8ABc3MzD4AAAAAAIA/AgYAAAA/AAAAAACAPwE=",
            ["castling"] = "AQAAAAEAAAAABwAAAAEFAQGamZk/AAEAAACAPwM=",
            ["promotion"] = "AQAAAAEAAAAAEgAAAAEFAQGamZk/AAEAAACAPwM=",
            ["mate_finisher"] = "AQAAAAEAAAAAIAAAAAEGBQCamZk+AAIAAACAPwACmpkZPwEAAAAAgD8CA5qZGT8EAgPNzMw+AggAAMA/BAADzczMPgEGAAAAPwAAAAAAgD8A",
            ["blunder"] = "AQAAAAEAAAAACwAAAAIABQEBmpmZPwABAAAAgD8D",
            ["low_clock_desperate"] = "AQAAAAEAAAACAwAAAAIEBQEBzcxMPgABAAAAgD8D",
            ["exhausted_budget"] = "AQAAAAEAAAAAOAAAAAIBBwcAmpmZPgABAAAAgD8DB5qZGT8DAQIAAIA/AgJmZmY/AQAAAACAPwADAAAAPwIBAQAAgD8CBJqZGT8AAgEAAIA/AAXNzMw+AAAAAACAPwIGAAAAPwAAAAAAgD8B",
            ["blitz_dial"] = "AQAAAAEAAAACAgAAAAIFBwEDmpmZPgIBAAAAgD8D",
        };

        private static string DirectBase64(Scenario s)
        {
            var built = DirectorHarness.BuildScenario(s.Fen, s.Setup, s.Focus, s.EvalB, s.EvalA, s.Clock, s.Dial, Seed, s.PreSpend);
            return Convert.ToBase64String(built.director.Direct(built.input).ToBytes());
        }

        [Test]
        public void Scenarios_MatchGolden()
        {
            if (Environment.GetEnvironmentVariable("CMR_REGEN") == "1")
            {
                foreach (var s in Scenarios)
                    TestContext.Out.WriteLine($"GOLDEN|{s.Name}|{DirectBase64(s)}");
                Assert.Ignore("Regenerated goldens (CMR_REGEN=1); paste them into the Golden map.");
            }

            Assert.That(Golden.Count, Is.EqualTo(Scenarios.Length), "golden map is not fully populated");
            foreach (var s in Scenarios)
            {
                Assert.That(Golden.ContainsKey(s.Name), Is.True, $"no golden for {s.Name}");
                Assert.That(DirectBase64(s), Is.EqualTo(Golden[s.Name]), $"ShotList changed for scenario '{s.Name}'");
            }
        }
    }
}
