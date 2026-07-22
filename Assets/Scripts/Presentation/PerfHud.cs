using System;
using UnityEngine;

namespace CheckmateRoyale.Presentation
{
    /// <summary>On-screen perf overlay for the slice: fps, frame ms, and GC allocated per frame.</summary>
    public sealed class PerfHud : MonoBehaviour
    {
        private float _fps;
        private float _ms;
        private long _lastAlloc;
        private long _allocPerFrame;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f)
            {
                _fps = Mathf.Lerp(_fps, 1f / dt, 0.1f);
                _ms = Mathf.Lerp(_ms, dt * 1000f, 0.1f);
            }

            long now = GC.GetAllocatedBytesForCurrentThread();
            _allocPerFrame = Math.Max(0, now - _lastAlloc);
            _lastAlloc = now;
        }

        private void OnGUI()
        {
            var style = new GUIStyle { fontSize = 18, normal = { textColor = Color.white } };
            var bg = new Rect(Screen.width - 250, 16, 234, 78);
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(bg, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(bg.x + 12, bg.y + 8, 220, 24), $"{_fps:F0} fps   {_ms:F1} ms", style);
            GUI.Label(new Rect(bg.x + 12, bg.y + 32, 220, 24), $"GC/frame: {_allocPerFrame / 1024f:F1} KB", style);
        }
    }
}
