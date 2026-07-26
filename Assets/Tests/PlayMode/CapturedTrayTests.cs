using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Captured-material tray tracks captures + advantage, derived from the position.</summary>
    public class CapturedTrayTests
    {
        [UnityTest]
        public IEnumerator TracksCaptures_AndAdvantage_ThroughUndo()
        {
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Build();

            Assert.AreEqual(0, ctx.Captured.MaterialAdvantage(), "even at the start");

            ctx.TryMakeMove(12, 28); // e4
            ctx.TryMakeMove(51, 35); // d5
            ctx.TryMakeMove(28, 35); // exd5 — White captures a pawn

            Assert.AreEqual(1, ctx.Captured.CapturedCount(CC.Color.White, CC.PieceType.Pawn));
            Assert.AreEqual(0, ctx.Captured.CapturedCount(CC.Color.Black, CC.PieceType.Pawn));
            Assert.AreEqual(1, ctx.Captured.MaterialAdvantage(), "White +1");

            ctx.Undo(1); // take back exd5
            Assert.AreEqual(0, ctx.Captured.MaterialAdvantage(), "back to even after undo");
            Assert.AreEqual(0, ctx.Captured.CapturedCount(CC.Color.White, CC.PieceType.Pawn));

            yield return null;
            Object.Destroy(ctx.gameObject);
        }
    }
}
