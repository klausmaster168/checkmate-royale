using UnityEngine;
using CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// The Phase-5 "Proof of Magic" vertical slice: a fixed position whose only affordance is
    /// Nf3xe5. ATTACK triggers the directed capture (with injected high-drama evals so it plays
    /// as a FIRST_BLOOD showcase). Replay re-runs identically (same seed); Variation bumps the
    /// seed for different animation/camera variants. Everything is offline and deterministic.
    /// </summary>
    public sealed class SliceController : MonoBehaviour
    {
        // r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq -
        public const string SliceFen = "r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq -";
        public const int KnightFrom = 21; // f3
        public const int PawnTarget = 36; // e5

        /// <summary>Every knob the slice's feel depends on — iterate these in the Inspector.</summary>
        [System.Serializable]
        public sealed class SlicePolish
        {
            [Tooltip("Injected eval (cp, mover POV) before the move.")] public float EvalBefore = -600f;
            [Tooltip("Injected eval (cp, mover POV) after the move — big swing = max drama.")] public float EvalAfter = 600f;
            public ulong Seed = 0xF00DF00DUL;
        }

        public GameContext Context;
        public SlicePolish Polish = new SlicePolish();

        private ulong _seed;
        private bool _fired;

        private void Awake()
        {
            if (Context == null) Context = FindFirstObjectByType<GameContext>();
            _seed = Polish.Seed;
            if (Context != null) Context.Configure(SliceFen, _seed); // before GameContext.Build (its Start)
        }

        public void Attack()
        {
            if (_fired || Context == null) return;
            _fired = Context.TryMakeMove(KnightFrom, PawnTarget, PieceType.Queen, Polish.EvalBefore, Polish.EvalAfter);
        }

        public void Replay()
        {
            if (Context == null) return;
            _fired = false;
            Context.ResetGame(_seed);
        }

        public void Variation()
        {
            if (Context == null) return;
            _seed += 1;
            _fired = false;
            Context.ResetGame(_seed);
        }

        private void OnGUI()
        {
            const int w = 190, h = 54;
            var style = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };

            if (GUI.Button(new Rect(20, Screen.height - h - 20, w, h), "⚔  ATTACK", style)) Attack();
            if (GUI.Button(new Rect(20 + w + 12, Screen.height - h - 20, 130, h), "Replay")) Replay();
            if (GUI.Button(new Rect(20 + w + 12 + 142, Screen.height - h - 20, 140, h), "Variation")) Variation();

            GUI.Label(new Rect(20, Screen.height - h - 48, 600, 24),
                $"Knight takes pawn — seed {_seed:X}   (tap sequence to skip)");
        }
    }
}
