using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Phase-0 smoke test: the runtime boots and renders a frame without errors.</summary>
    public class SmokeTests
    {
        [UnityTest]
        public IEnumerator BootstrapScene_RendersOneFrame()
        {
            var cameraGo = new GameObject("SmokeCamera");
            cameraGo.AddComponent<Camera>();

            int startFrame = Time.frameCount;
            yield return null; // let one frame render

            Assert.That(Time.frameCount, Is.GreaterThan(startFrame), "no frame was rendered");
            Object.Destroy(cameraGo);
        }
    }
}
