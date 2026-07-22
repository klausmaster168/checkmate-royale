using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace CheckmateRoyale.Editor
{
    /// <summary>
    /// Adds packages by name so the Package Manager resolves the version compatible with the
    /// installed editor (avoids hard-coding versions that break on a specific Unity build).
    /// Run headless WITHOUT -quit:
    ///   Unity -batchmode -executeMethod CheckmateRoyale.Editor.AddPackages.AddCinemachine
    /// </summary>
    public static class AddPackages
    {
        private static AddRequest _request;

        public static void AddCinemachine()
        {
            _request = Client.Add("com.unity.cinemachine");
            EditorApplication.update += Progress;
        }

        private static void Progress()
        {
            if (_request == null || !_request.IsCompleted) return;
            EditorApplication.update -= Progress;

            if (_request.Status == StatusCode.Success)
                Debug.Log($"[AddPackages] added {_request.Result.name} @ {_request.Result.version}");
            else
                Debug.LogError($"[AddPackages] failed: {_request.Error?.message}");

            EditorApplication.Exit(_request.Status == StatusCode.Success ? 0 : 1);
        }
    }
}
