using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>The live clock ticks down, presses on a move, and flags to a timeout result.</summary>
    public class ClockTests
    {
        [UnityTest]
        public IEnumerator TicksDown_PressesOnMove_AndFlags()
        {
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Build(); // default 3+2 blitz (180000ms + 2000 increment)

            Assert.AreEqual(180000, ctx.Clock.DisplayMs(CC.Color.White));

            ctx.Clock.Tick(5f); // White thinks 5s
            Assert.AreEqual(175000, ctx.Clock.DisplayMs(CC.Color.White));

            ctx.TryMakeMove(12, 28); // e4 => press: White -5s +2s inc, Black now on move
            Assert.AreEqual(177000, ctx.Clock.DisplayMs(CC.Color.White));
            Assert.AreEqual(180000, ctx.Clock.DisplayMs(CC.Color.Black));

            // Black flags.
            ctx.Clock.Tick(181f);
            Assert.IsTrue(ctx.Clock.Flagged);
            Assert.AreEqual(CC.Color.Black, ctx.Clock.FlaggedSide);
            Assert.AreEqual(CC.GameResult.WhiteWins, ctx.EndBanner.Result);
            Assert.AreEqual(CC.GameEndReason.Timeout, ctx.EndBanner.Reason);

            // No moves accepted after the flag.
            Assert.IsFalse(ctx.TryMakeMove(51, 35));

            yield return null;
            Object.Destroy(ctx.gameObject);
        }
    }
}
