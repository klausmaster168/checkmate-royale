using UnityEngine;

namespace CheckmateRoyale.Presentation
{
    /// <summary>Tiny DOTween-free easing helpers used by the placeholder animation system.</summary>
    public static class Tweener
    {
        public static float EaseInOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t); // smoothstep
        }

        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        public static float EaseIn(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        /// <summary>A hop arc height factor (0 at ends, 1 in the middle).</summary>
        public static float Arc(float t)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Sin(t * Mathf.PI);
        }

        /// <summary>Squash-and-stretch scale for a value in [0,1] progress (1 at ends, dip in middle).</summary>
        public static Vector3 SquashStretch(float t, float amount = 0.12f)
        {
            float s = Mathf.Sin(t * Mathf.PI);
            return new Vector3(1f + s * amount, 1f - s * amount, 1f + s * amount);
        }
    }
}
