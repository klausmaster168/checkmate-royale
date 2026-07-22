using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Phase-3 chunk-2 gates: committed moves animate, and presentation never desyncs.</summary>
    public class MovePipelineTests
    {
        private static GameContext NewContext()
        {
            var go = new GameObject("GameContext");
            var ctx = go.AddComponent<GameContext>();
            ctx.Build();
            return ctx;
        }

        private static bool PlayFirstLegal(GameContext ctx)
        {
            var buf = new CC.Move[CC.MoveGenerator.MaxMoves];
            int n = ctx.Game.LegalMoves(buf);
            if (n == 0) return false;
            CC.Move m = buf[0];
            return ctx.TryMakeMove(m.From, m.To, m.IsPromotion ? m.Promotion : CC.PieceType.Queen);
        }

        private static void AssertLayoutMatches(GameContext ctx)
        {
            int expected = 0;
            for (int sq = 0; sq < 64; sq++)
            {
                CC.Piece pc = ctx.Game.Position.Board[sq];
                PieceView view = ctx.Pieces.At(sq);
                if (pc == CC.Piece.None)
                {
                    Assert.IsNull(view, $"square {sq} should be empty");
                    continue;
                }
                expected++;
                Assert.IsNotNull(view, $"square {sq} should have a piece view");
                Assert.AreEqual(CC.Types.TypeOf(pc), view.Type, $"type mismatch at {sq}");
                Assert.AreEqual(CC.Types.ColorOf(pc), view.Side, $"colour mismatch at {sq}");
                float dist = Vector3.Distance(view.transform.position, view.StandWorld(ctx.Board));
                Assert.Less(dist, 0.06f, $"piece at {sq} not snapped to committed square (off by {dist})");
            }
            Assert.AreEqual(expected, ctx.Pieces.LiveCount, "live piece count != board occupancy");
        }

        [UnityTest]
        public IEnumerator RapidGame_FinalLayoutEqualsPosition()
        {
            var ctx = NewContext();

            // Commit ~16 plies fast (stressing premove/queue compression), yielding rarely.
            for (int i = 0; i < 16; i++)
            {
                if (!PlayFirstLegal(ctx)) break;
                if (i % 4 == 0) yield return null; // let Update fast-forward the backlog
            }

            ctx.Player.FlushInstant();
            yield return null;

            AssertLayoutMatches(ctx);
            Object.Destroy(ctx.gameObject);
        }

        [UnityTest]
        public IEnumerator Skip_SnapsToCommittedState()
        {
            var ctx = NewContext();

            // 1. e4 (e2->e4)  1... d5 (d7->d5)  — settle these.
            ctx.TryMakeMove(12, 28);
            ctx.TryMakeMove(51, 35);
            ctx.Player.FlushInstant();
            yield return null;

            // 2. exd5 — a capture. Start it, let it play briefly, then skip.
            Assert.IsTrue(ctx.TryMakeMove(28, 35), "exd5 should be legal");
            yield return null; // sequence starts + advances a little

            int liveBefore = ctx.Pieces.LiveCount;
            ctx.Player.SkipCurrent();
            yield return null;

            PieceView mover = ctx.Pieces.At(35);
            Assert.IsNotNull(mover, "captured square should now hold the mover");
            float dist = Vector3.Distance(mover.transform.position, mover.StandWorld(ctx.Board));
            Assert.Less(dist, 0.06f, "skip did not snap mover to committed square");
            Assert.AreEqual(liveBefore, ctx.Pieces.LiveCount, "captured piece should be gone after skip");

            Object.Destroy(ctx.gameObject);
        }

        [UnityTest]
        public IEnumerator LocalSlowMo_NeverTouchesTimeScale()
        {
            var ctx = NewContext();
            PlayFirstLegal(ctx);

            for (int frame = 0; frame < 12; frame++)
            {
                Assert.AreEqual(1.0f, Time.timeScale, 1e-6f, "Time.timeScale must stay 1 (off-clock cinema)");
                yield return null;
            }
            Object.Destroy(ctx.gameObject);
        }
    }
}
