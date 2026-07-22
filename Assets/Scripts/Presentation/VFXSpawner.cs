using System.Collections.Generic;
using UnityEngine;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Pooled placeholder impact VFX keyed by intensity tier 0-3. Everything is pooled, so
    /// after warmup a burst allocates no managed memory. Real particle prefabs replace the
    /// placeholder pop later with no call-site changes.
    /// </summary>
    public sealed class VFXSpawner : MonoBehaviour
    {
        private struct Active { public GameObject Go; public float T, Dur, Size; }

        private readonly Stack<GameObject> _pool = new Stack<GameObject>(256);
        private readonly List<Active> _active = new List<Active>(256);
        private Material _mat;

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++) _pool.Push(CreateOne());
        }

        /// <summary>Spawn a pooled burst at a world position; bigger for higher tiers.</summary>
        public void Burst(Vector3 worldPos, int tier)
        {
            GameObject go = _pool.Count > 0 ? _pool.Pop() : CreateOne();
            go.transform.position = worldPos + Vector3.up * 0.2f;
            go.transform.localScale = Vector3.one * 0.05f;
            go.SetActive(true);
            _active.Add(new Active { Go = go, T = 0f, Dur = 0.25f, Size = 0.2f + tier * 0.15f });
        }

        private GameObject CreateOne()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "VFX";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(transform, false);
            if (_mat == null) _mat = PlaceholderArt.Get(new Color(1f, 0.85f, 0.4f));
            go.GetComponent<MeshRenderer>().sharedMaterial = _mat;
            go.SetActive(false);
            return go;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Active a = _active[i];
                a.T += dt;
                float k = Mathf.Clamp01(a.T / a.Dur);
                if (a.Go != null)
                {
                    float s = Mathf.Lerp(0.05f, a.Size, Tweener.EaseOut(k)) * (1f - k * 0.5f);
                    a.Go.transform.localScale = Vector3.one * s;
                }
                if (k >= 1f)
                {
                    if (a.Go != null) { a.Go.SetActive(false); _pool.Push(a.Go); }
                    _active.RemoveAt(i);
                }
                else _active[i] = a;
            }
        }
    }
}
