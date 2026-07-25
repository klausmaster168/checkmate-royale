using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Board affordances: last-move squares and the checked-king square are tracked.</summary>
    public class HighlightTests
    {
        [UnityTest]
        public IEnumerator LastMoveAndCheck_AreHighlighted()
        {
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Build();

            // A quiet opener: last move tracked, no check.
            ctx.TryMakeMove(12, 28); // e2-e4
            Assert.AreEqual(12, ctx.Highlights.LastFrom);
            Assert.AreEqual(28, ctx.Highlights.LastTo);
            Assert.AreEqual(-1, ctx.Highlights.CheckSquare, "no check yet");

            // Reach a check: 1...e5 2.Bc4 Nc6 3.Bxf7+ (bishop c4 x f7, checks the black king on e8).
            ctx.TryMakeMove(52, 36); // e7-e5
            ctx.TryMakeMove(5, 26);  // Bf1-c4
            ctx.TryMakeMove(57, 42); // Nb8-c6
            ctx.TryMakeMove(26, 53); // Bxf7+

            Assert.AreEqual(26, ctx.Highlights.LastFrom);
            Assert.AreEqual(53, ctx.Highlights.LastTo);
            Assert.AreEqual(60, ctx.Highlights.CheckSquare, "black king (e8=60) should be flagged in check");

            yield return null;
            Object.Destroy(ctx.gameObject);
        }
    }
}
