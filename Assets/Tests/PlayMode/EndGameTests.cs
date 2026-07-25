using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Terminal-state detection surfaces the right result and New Game resets it.</summary>
    public class EndGameTests
    {
        [UnityTest]
        public IEnumerator FoolsMate_ShowsCheckmate_ThenNewGameResets()
        {
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Build();

            // 1.f3 e5 2.g4 Qh4#
            ctx.TryMakeMove(13, 21); // f2-f3
            ctx.TryMakeMove(52, 36); // e7-e5
            ctx.TryMakeMove(14, 30); // g2-g4
            ctx.TryMakeMove(59, 31); // Qd8-h4#

            Assert.AreEqual(CC.GameResult.BlackWins, ctx.EndBanner.Result);
            Assert.AreEqual(CC.GameEndReason.Checkmate, ctx.EndBanner.Reason);
            StringAssert.Contains("Checkmate", ctx.EndBanner.Message);
            Assert.IsTrue(ctx.EndBanner.IsGameOver);

            // New Game clears the banner and returns to the start position.
            ctx.NewGame();
            Assert.IsFalse(ctx.EndBanner.IsGameOver, "banner should clear on new game");
            Assert.AreEqual(0, ctx.Game.PlyCount);

            yield return null;
            Object.Destroy(ctx.gameObject);
        }

        [UnityTest]
        public IEnumerator StalemateMove_ShowsDraw()
        {
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Configure("7k/8/6K1/8/8/8/8/5Q2 w - - 0 1", 42UL); // Qf1, Kg6 vs Kh8
            ctx.Build();

            ctx.TryMakeMove(5, 53); // Qf1-f7 => black (h8) is stalemated, not in check

            Assert.AreEqual(CC.GameResult.Draw, ctx.EndBanner.Result);
            Assert.AreEqual(CC.GameEndReason.Stalemate, ctx.EndBanner.Reason);
            StringAssert.Contains("Stalemate", ctx.EndBanner.Message);

            yield return null;
            Object.Destroy(ctx.gameObject);
        }
    }
}
