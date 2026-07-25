using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>The notation panel records correct SAN, trims on undo, clears on new game.</summary>
    public class MoveListTests
    {
        [UnityTest]
        public IEnumerator RecordsSan_TrimsOnUndo_ClearsOnNewGame()
        {
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Build();

            ctx.TryMakeMove(12, 28); // e4
            ctx.TryMakeMove(52, 36); // e5
            ctx.TryMakeMove(6, 21);  // Nf3

            Assert.AreEqual(3, ctx.MoveList.Sans.Count);
            Assert.AreEqual("e4", ctx.MoveList.Sans[0]);
            Assert.AreEqual("e5", ctx.MoveList.Sans[1]);
            Assert.AreEqual("Nf3", ctx.MoveList.Sans[2]);

            ctx.Undo(1);
            Assert.AreEqual(2, ctx.MoveList.Sans.Count, "undo should drop the last SAN");
            Assert.AreEqual("e5", ctx.MoveList.Sans[ctx.MoveList.Sans.Count - 1]);

            ctx.NewGame();
            Assert.AreEqual(0, ctx.MoveList.Sans.Count, "new game clears the list");

            yield return null;
            Object.Destroy(ctx.gameObject);
        }
    }
}
