using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;
using CheckmateRoyale.Director;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Settings apply to the underlying systems (sound, motion, AI, time control).</summary>
    public class SettingsTests
    {
        [UnityTest]
        public IEnumerator Settings_ApplyToSystems()
        {
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Build();
            var ai = new GameObject("Ai").AddComponent<AiController>();
            ai.Context = ctx;
            var settings = new GameObject("Settings").AddComponent<SettingsPanel>();
            settings.Context = ctx;

            settings.SetSoundMuted(true);
            Assert.IsTrue(ctx.Sound.Muted);

            settings.SetReducedMotion(true);
            Assert.AreEqual(ModeDial.Battle, ctx.Dial, "reduced motion caps pacing at Battle");
            settings.SetReducedMotion(false);
            Assert.AreEqual(ModeDial.Cinema, ctx.Dial);

            settings.SetAiEnabled(false);
            Assert.IsFalse(ai.AiEnabled);
            settings.SetDifficulty(AiController.Difficulty.Hard);
            Assert.AreEqual(AiController.Difficulty.Hard, ai.Level);

            settings.SetTimeOption(SettingsPanel.TimeOption.Bullet); // 1+0 => 60000ms, new game
            Assert.IsTrue(ctx.Clock.Enabled);
            Assert.AreEqual(60000, ctx.Clock.DisplayMs(CC.Color.White));

            settings.SetTimeOption(SettingsPanel.TimeOption.Casual);
            Assert.IsFalse(ctx.Clock.Enabled, "casual disables the clock");

            yield return null;
            Object.Destroy(ctx.gameObject);
            Object.Destroy(ai.gameObject);
            Object.Destroy(settings.gameObject);
        }
    }
}
