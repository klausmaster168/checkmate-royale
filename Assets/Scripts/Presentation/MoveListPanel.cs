using System.Collections.Generic;
using UnityEngine;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// A running SAN move list beside the board (1. e4 e5  2. Nf3 Nc6 …), with the latest move
    /// highlighted. Appends on each committed move; trimmed on takeback; cleared on a new game.
    /// </summary>
    public sealed class MoveListPanel : MonoBehaviour
    {
        private readonly List<string> _sans = new List<string>(64);
        private Vector2 _scroll;

        public IReadOnlyList<string> Sans => _sans;

        public void Init(GameContext ctx) => ctx.MoveCommittedEvent += OnMove;

        private void OnMove(MoveCommitted mc)
        {
            _sans.Add(mc.San);
            _scroll.y = float.MaxValue; // keep the latest move in view
        }

        public void TrimTo(int count)
        {
            while (_sans.Count > count) _sans.RemoveAt(_sans.Count - 1);
        }

        public void Clear() => _sans.Clear();

        private void OnGUI()
        {
            const float w = 196f;
            float h = Mathf.Min(Screen.height * 0.58f, 460f);
            float x = Screen.width - w - 16f;
            float y = 60f;

            GUI.color = new Color(0.05f, 0.06f, 0.09f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var header = new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.95f, 0.85f, 0.4f) } };
            GUI.Label(new Rect(x, y + 6, w, 20), "Moves", header);

            var num = new GUIStyle { fontSize = 13, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(0.6f, 0.63f, 0.7f) } };
            var cell = new GUIStyle { fontSize = 13, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.88f, 0.9f, 0.95f) }, padding = new RectOffset(6, 0, 0, 0) };
            var cur = new GUIStyle(cell) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.98f, 0.82f, 0.3f) } };

            GUILayout.BeginArea(new Rect(x + 6, y + 28, w - 12, h - 34));
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

            int pairs = (_sans.Count + 1) / 2;
            int last = _sans.Count - 1;
            for (int i = 0; i < pairs; i++)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(18));
                GUILayout.Label($"{i + 1}.", num, GUILayout.Width(26));
                int wi = i * 2, bi = i * 2 + 1;
                GUILayout.Label(_sans[wi], wi == last ? cur : cell, GUILayout.Width(66));
                GUILayout.Label(bi < _sans.Count ? _sans[bi] : "", bi == last ? cur : cell, GUILayout.Width(66));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
