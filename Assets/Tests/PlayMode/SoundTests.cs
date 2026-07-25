using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Audio cue selection: capture vs move, and check / game-over layers.</summary>
    public class SoundTests
    {
        [UnityTest]
        public IEnumerator Capture_SelectsCaptureCue()
        {
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Build();

            ctx.TryMakeMove(12, 28); // e4
            ctx.TryMakeMove(51, 35); // d5
            ctx.TryMakeMove(28, 35); // exd5 (capture)

            Assert.AreEqual("Capture", ctx.Sound.LastPrimary);
            Assert.IsFalse(ctx.Sound.PlayedCheck);

            yield return null;
            Object.Destroy(ctx.gameObject);
        }

        [UnityTest]
        public IEnumerator CheckmatingMove_PlaysCheckAndGameOverCues()
        {
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Build();

            // Fool's mate: 1.f3 e5 2.g4 Qh4#
            ctx.TryMakeMove(13, 21);
            ctx.TryMakeMove(52, 36);
            ctx.TryMakeMove(14, 30);
            ctx.TryMakeMove(59, 31);

            Assert.IsTrue(ctx.Sound.PlayedCheck, "checkmate is a check");
            Assert.IsTrue(ctx.Sound.PlayedEnd, "game over should play the end cue");

            yield return null;
            Object.Destroy(ctx.gameObject);
        }
    }
}
