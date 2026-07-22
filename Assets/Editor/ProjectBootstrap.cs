using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CheckmateRoyale.Editor
{
    /// <summary>
    /// Applies the Phase-0 mobile project settings from CLAUDE.md. Run headless via:
    ///   Unity -batchmode -quit -executeMethod CheckmateRoyale.Editor.ProjectBootstrap.ApplyMobileSettings
    /// or from the menu. Idempotent.
    /// </summary>
    public static class ProjectBootstrap
    {
        [MenuItem("Checkmate Royale/Apply Mobile Settings")]
        public static void ApplyMobileSettings()
        {
            // Rendering + memory.
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.gcIncremental = true;

            // IL2CPP on both mobile targets; ARM64-only on Android; .NET Standard 2.1.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.iOS, ApiCompatibilityLevel.NET_Standard);

            int tuned = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (urp == null) continue;

                // URP mobile tuning: MSAA 4x, HDR off, short single-cascade shadows.
                var so = new SerializedObject(urp);
                SetInt(so, "m_MSAA", 4);
                SetBool(so, "m_SupportsHDR", false);
                SetFloat(so, "m_ShadowDistance", 20f);
                SetInt(so, "m_ShadowCascadeCount", 1);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(urp);
                tuned++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ProjectBootstrap] Mobile settings applied. URP assets tuned: {tuned}. " +
                      $"ColorSpace={PlayerSettings.colorSpace}, IncrementalGC={PlayerSettings.gcIncremental}.");
        }

        private static void SetInt(SerializedObject so, string prop, int v)
        {
            var p = so.FindProperty(prop); if (p != null) p.intValue = v;
        }
        private static void SetBool(SerializedObject so, string prop, bool v)
        {
            var p = so.FindProperty(prop); if (p != null) p.boolValue = v;
        }
        private static void SetFloat(SerializedObject so, string prop, float v)
        {
            var p = so.FindProperty(prop); if (p != null) p.floatValue = v;
        }
    }
}
