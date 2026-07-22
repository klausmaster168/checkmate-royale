using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using CheckmateRoyale.Presentation;

namespace CheckmateRoyale.Editor
{
    /// <summary>
    /// Headless visual proof: builds a GameContext, renders the board from a hero angle
    /// via a URP render request, and writes a PNG. Run with:
    ///   Unity -batchmode -quit -executeMethod CheckmateRoyale.Editor.CaptureBoard.Capture
    /// </summary>
    public static class CaptureBoard
    {
        [MenuItem("Checkmate Royale/Capture Board PNG")]
        public static void Capture()
        {
            const int W = 1280, H = 800;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.19f, 0.24f);

            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.96f, 0.9f);
            sun.transform.rotation = Quaternion.Euler(52f, -34f, 0f);

            var ctxGo = new GameObject("GameContext");
            var ctx = ctxGo.AddComponent<GameContext>();
            ctx.Build();

            var camGo = new GameObject("CaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
            cam.fieldOfView = 42f;
            cam.transform.position = new Vector3(0f, 8.5f, -7.5f);
            cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);

            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 };

            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
            {
                // Submit twice: the first pass uploads materials/registers lights, the second renders them.
                RenderPipeline.SubmitRenderRequest(cam, request);
                RenderPipeline.SubmitRenderRequest(cam, request);
            }
            else
            {
                cam.targetTexture = rt; // fallback (non-SRP)
                cam.Render();
            }

            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            string path = Path.Combine(Application.dataPath, "..", "Tools", "board_capture.png");
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Debug.Log($"[CaptureBoard] wrote {Path.GetFullPath(path)} ({W}x{H})");
        }
    }
}
