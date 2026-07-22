using System.Collections.Generic;
using UnityEngine;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Persistent capture-square decals — by late game the board is a map of the war. Pooled,
    /// capped at 32 (oldest recycled). Cleared on a new game. Placeholder = a dark flat quad.
    /// </summary>
    public sealed class BattleScars : MonoBehaviour
    {
        public const int Max = 32;

        private readonly Stack<GameObject> _pool = new Stack<GameObject>(Max);
        private readonly List<GameObject> _active = new List<GameObject>(Max + 1);
        private BoardView _board;
        private Material _mat;

        public int Count => _active.Count;

        public void Init(BoardView board) => _board = board;

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++) _pool.Push(CreateOne());
        }

        public void AddScar(int square, int intensity)
        {
            if (square < 0 || _board == null) return;
            GameObject go = _pool.Count > 0 ? _pool.Pop() : CreateOne();
            go.transform.position = _board.SquareToWorld(square) + new Vector3(0f, 0.03f, 0f);
            float s = 0.55f + Mathf.Clamp(intensity, 0, 3) * 0.1f;
            go.transform.localScale = new Vector3(s, 0.02f, s);
            go.SetActive(true);
            _active.Add(go);

            if (_active.Count > Max)
            {
                GameObject oldest = _active[0];
                _active.RemoveAt(0);
                oldest.SetActive(false);
                _pool.Push(oldest);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].SetActive(false);
                _pool.Push(_active[i]);
            }
            _active.Clear();
        }

        private GameObject CreateOne()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Scar";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(transform, false);
            if (_mat == null) _mat = PlaceholderArt.Get(new Color(0.08f, 0.06f, 0.07f));
            go.GetComponent<MeshRenderer>().sharedMaterial = _mat;
            go.SetActive(false);
            return go;
        }
    }
}
