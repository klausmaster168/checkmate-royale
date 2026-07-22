using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CheckmateRoyale.Presentation;

namespace CheckmateRoyale.Editor
{
    /// <summary>
    /// Authors Assets/Scenes/Slice_KnightTakesPawn.unity — the Phase-5 Proof of Magic slice.
    /// Open it, press Play, hit ATTACK. Run headless:
    ///   Unity -batchmode -quit -executeMethod CheckmateRoyale.Editor.BuildSliceScene.Build
    /// </summary>
    public static class BuildSliceScene
    {
        [MenuItem("Checkmate Royale/Build Slice Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.19f, 0.24f);

            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -32f, 0f);

            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
            cam.fieldOfView = 42f;
            cam.transform.position = new Vector3(0f, 8.5f, -7.5f);
            cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            camGo.AddComponent<PerfHud>();

            var ctxGo = new GameObject("GameContext");
            var ctx = ctxGo.AddComponent<GameContext>();
            var slice = ctxGo.AddComponent<SliceController>();
            slice.Context = ctx;

            const string path = "Assets/Scenes/Slice_KnightTakesPawn.unity";
            bool ok = EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[BuildSliceScene] saved {path} = {ok}");
        }
    }
}
