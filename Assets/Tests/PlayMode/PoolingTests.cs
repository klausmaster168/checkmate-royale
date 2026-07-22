using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Phase-3 pooling gate: VFX + scars allocate ~nothing per capture after warmup.</summary>
    public class PoolingTests
    {
        [UnityTest]
        public IEnumerator VfxAndScars_NearZeroAllocAfterWarmup()
        {
            var boardGo = new GameObject("Board");
            var board = boardGo.AddComponent<BoardView>();
            board.Build();

            var fxGo = new GameObject("FX");
            var vfx = fxGo.AddComponent<VFXSpawner>();
            var scars = fxGo.AddComponent<BattleScars>();
            scars.Init(board);

            vfx.Prewarm(256);
            scars.Prewarm(64);

            for (int i = 0; i < 8; i++) { vfx.Burst(Vector3.zero, i % 4); scars.AddScar(i, 1); }
            yield return null; // let Update tick once (JIT warm)

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 200; i++)
            {
                vfx.Burst(new Vector3(i % 8, 0f, 0f), i % 4);
                scars.AddScar(i % 64, 1);
            }
            long delta = GC.GetAllocatedBytesForCurrentThread() - before;
            Debug.Log($"[Pooling] 200 captures allocated {delta} bytes ({delta / 200} per capture)");

            Assert.Less(delta, 200 * 1024, $"pooling allocated too much: {delta} bytes over 200 captures");
            Assert.LessOrEqual(scars.Count, BattleScars.Max, "scar cap exceeded");

            UnityEngine.Object.Destroy(boardGo);
            UnityEngine.Object.Destroy(fxGo);
        }
    }
}
