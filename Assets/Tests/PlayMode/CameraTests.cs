using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Phase-4 gate: the camera director switches rigs across a directed sequence.</summary>
    public class CameraTests
    {
        [UnityTest]
        public IEnumerator CaptureSequence_SwitchesThroughRigs()
        {
            // A real main camera makes the CameraDirector active.
            var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
            camGo.AddComponent<Camera>();

            var ctxGo = new GameObject("GameContext");
            var ctx = ctxGo.AddComponent<GameContext>();
            ctx.Build();
            Assert.IsNotNull(ctx.Cameras, "camera director should exist");

            // Reach a capture: 1.e4 d5 2.exd5 (Cinema dial => full grammar incl. Impact beat).
            ctx.TryMakeMove(12, 28);
            ctx.TryMakeMove(51, 35);
            ctx.Player.FlushInstant();
            yield return null;

            Assert.AreEqual("CommanderRig", ctx.Cameras.ActiveRigName, "should rest on Commander");

            ctx.TryMakeMove(28, 35); // exd5

            // Drive time deterministically (headless Time.deltaTime is ~0), stepping through the sequence.
            for (int i = 0; i < 60 && ctx.Player.IsPlaying; i++)
                ctx.Player.Tick(0.1f);
            yield return null;

            // The director records every rig it activated across the sequence.
            var seen = ctx.Cameras.ActivatedRigs;
            Assert.That(seen, Has.Member("DollyTrackRig"), "Approach beat should use the dolly rig");
            Assert.That(seen, Has.Member("DuelOTSRig"), "Impact beat should use the duel OTS rig");

            Object.Destroy(ctxGo);
            Object.Destroy(camGo);
        }

        [UnityTest]
        public IEnumerator Skip_ReturnsToCommander()
        {
            var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
            camGo.AddComponent<Camera>();
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Build();

            ctx.TryMakeMove(12, 28); // e4
            yield return null;       // sequence starts (likely off Commander)
            ctx.Player.SkipCurrent();
            yield return null;

            Assert.AreEqual("CommanderRig", ctx.Cameras.ActiveRigName, "skip should hard-cut to Commander");

            Object.Destroy(ctx.gameObject);
            Object.Destroy(camGo);
        }
    }
}
