using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;
using CheckmateRoyale.Director;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Phase-5 slice: ATTACK commits Nxe5 as a directed capture; Replay is identical, Variation differs.</summary>
    public class SliceTests
    {
        [UnityTest]
        public IEnumerator Attack_CommitsNxe5_AndIsDeterministic()
        {
            var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
            camGo.AddComponent<Camera>();

            var ctxGo = new GameObject("GameContext");
            var ctx = ctxGo.AddComponent<GameContext>();          // no Awake
            var slice = ctxGo.AddComponent<SliceController>();     // Awake configures ctx to the slice FEN
            slice.Context = ctx;
            ctx.Build();

            ShotList captured = null;
            ctx.MoveCommittedEvent += mc => captured = mc.Shot;

            slice.Attack();
            yield return null;

            // The only legal showcase move happened: knight now on e5, black pawn gone.
            Assert.AreEqual(1, ctx.Game.PlyCount, "Nxe5 should have committed");
            Assert.AreEqual(CC.Piece.WN, ctx.Game.Position.Board[36], "white knight should be on e5");
            Assert.IsNotNull(captured, "a ShotList should have been produced");

            bool hasImpact = false, hasApproach = false, impactSlowMo = false;
            foreach (Beat b in captured.Beats)
            {
                if (b.Type == BeatType.Approach) hasApproach = true;
                if (b.Type == BeatType.Impact) { hasImpact = true; impactSlowMo = b.SlowMoFactor < 1f; }
            }
            Assert.IsTrue(hasApproach && hasImpact, "Cinema capture should have Approach + Impact beats");
            Assert.IsTrue(impactSlowMo, "slice impact should be in slow-mo (lowered threshold)");
            Assert.That(captured.TotalDuration, Is.EqualTo(3.2f).Within(0.05f), "Cinema capture ~3.2s");

            string firstShot = Convert.ToBase64String(captured.ToBytes());

            // Replay with the same seed => byte-identical shot.
            slice.Replay();
            captured = null;
            slice.Attack();
            yield return null;
            Assert.AreEqual(firstShot, Convert.ToBase64String(captured.ToBytes()), "replay must be identical");

            // Variation bumps the seed => different variant selection.
            slice.Variation();
            captured = null;
            slice.Attack();
            yield return null;
            Assert.AreNotEqual(firstShot, Convert.ToBase64String(captured.ToBytes()), "variation must differ");

            UnityEngine.Object.Destroy(ctxGo);
            UnityEngine.Object.Destroy(camGo);
        }
    }
}
