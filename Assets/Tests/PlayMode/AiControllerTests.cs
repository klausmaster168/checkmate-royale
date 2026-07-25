using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>The AI opponent replies with a legal move on its turn.</summary>
    public class AiControllerTests
    {
        [UnityTest]
        public IEnumerator Ai_RepliesOnItsTurn()
        {
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Build();

            var ai = new GameObject("Ai").AddComponent<AiController>();
            ai.Context = ctx;
            ai.AiColor = CC.Color.Black;
            ai.AiEnabled = false; // drive manually for a deterministic test

            ctx.TryMakeMove(12, 28); // human White plays e4 => Black (AI) to move
            Assert.AreEqual(CC.Color.Black, ctx.Game.SideToMove);

            ai.ThinkAndMove();

            Assert.AreEqual(2, ctx.Game.PlyCount, "AI should have replied");
            Assert.AreEqual(CC.Color.White, ctx.Game.SideToMove, "turn should return to the human");

            yield return null;
            Object.Destroy(ctx.gameObject);
            Object.Destroy(ai.gameObject);
        }
    }
}
