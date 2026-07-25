using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CheckmateRoyale.Presentation;

namespace CheckmateRoyale.Editor
{
    /// <summary>
    /// Authors Assets/Scenes/Demo_Board.unity: a GameContext (builds board + pieces at play),
    /// a hero camera, a directional light and the tap-to-move DemoController. Open it and press
    /// Play to play both sides by hand. Run headless with:
    ///   Unity -batchmode -quit -executeMethod CheckmateRoyale.Editor.BuildDemoScene.Build
    /// </summary>
    public static class BuildDemoScene
    {
        [MenuItem("Checkmate Royale/Build Demo Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.21f, 0.26f);

            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52f, -34f, 0f);

            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
            cam.fieldOfView = 42f;
            cam.transform.position = new Vector3(0f, 8.5f, -7.5f);
            cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);

            var ctxGo = new GameObject("GameContext");
            var ctx = ctxGo.AddComponent<GameContext>();

            var ctrlGo = new GameObject("DemoController");
            var ctrl = ctrlGo.AddComponent<DemoController>();
            ctrl.Context = ctx;
            ctrl.Cam = cam;

            // Play vs the built-in AI (you are White). Disable this component for hot-seat.
            var aiGo = new GameObject("AiController");
            var ai = aiGo.AddComponent<AiController>();
            ai.Context = ctx;
            ai.AiColor = CheckmateRoyale.ChessCore.Color.Black;
            ai.Depth = 3;

            const string path = "Assets/Scenes/Demo_Board.unity";
            bool ok = EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[BuildDemoScene] saved {path} = {ok}");
        }
    }
}
