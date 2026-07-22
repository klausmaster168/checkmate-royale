using System;
using System.Collections.Generic;
using UnityEngine;
using CheckmateRoyale.Director;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Plays committed moves as directed sequences. State is already committed before a
    /// sequence plays, so playback is pure presentation: it can lag, compress or be skipped
    /// and the board's logical truth never waits for it.
    ///
    /// Slow-motion is a LOCAL time scale — it never touches Time.timeScale, so UI and clocks
    /// keep ticking (OFF-CLOCK CINEMA). Any exception during playback hard-snaps to the
    /// committed layout so a presentation bug can never desync the board.
    /// </summary>
    public sealed class SequencePlayer : MonoBehaviour
    {
        public const int MaxBacklog = 2; // deeper queue => fast-forward to keep up

        public event Action<BeatType, int> BeatStarted;
        public event Action SequenceSkipped;

        private PieceViewRegistry _registry;

        private readonly Queue<MoveCommitted> _queue = new Queue<MoveCommitted>();
        private readonly List<Dying> _dying = new List<Dying>(8);

        private bool _playing;
        private MoveCommitted _cur;
        private float _elapsed, _realDuration, _impactAt;
        private int _beatIndex;
        private float[] _beatStartReal;
        private bool _capturedHandled;
        private Vector3 _moverStart, _moverEnd, _rookStart, _rookEnd;

        public bool IsPlaying => _playing || _queue.Count > 0;

        private struct Dying { public PieceView View; public float T, Dur; public Vector3 StartScale; }

        public void Init(PieceViewRegistry registry) => _registry = registry;

        public void Enqueue(in MoveCommitted mc) => _queue.Enqueue(mc);

        private void Update()
        {
            try
            {
                StepDying(Time.deltaTime);

                // Fast-forward if we're falling behind (premove / rapid play).
                while (_queue.Count > MaxBacklog)
                {
                    if (_playing) FinishCurrentInstant();
                    else StartNext();
                }

                if (!_playing && _queue.Count > 0) StartNext();
                if (_playing) Advance(Time.deltaTime);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SequencePlayer] playback error, snapping to committed state: {e}");
                HardSnap();
            }
        }

        private void StartNext()
        {
            _cur = _queue.Dequeue();
            _playing = true;
            _elapsed = 0f;
            _beatIndex = -1;
            _capturedHandled = _cur.Visual.Captured == null;

            var beats = _cur.Shot.Beats;
            _beatStartReal = new float[beats.Length];
            float real = 0f;
            float impact = -1f;
            for (int i = 0; i < beats.Length; i++)
            {
                _beatStartReal[i] = real;
                float slow = Mathf.Max(beats[i].SlowMoFactor, 0.05f);
                if (impact < 0f && (beats[i].Type == BeatType.Impact || beats[i].Type == BeatType.Finisher))
                    impact = real;
                real += beats[i].Duration / slow; // slow-mo stretches real playback time
            }
            _realDuration = Mathf.Max(real, 0.0001f);
            _impactAt = impact < 0f ? _realDuration * 0.55f : impact;

            _moverStart = _cur.Visual.Mover != null ? _cur.Visual.Mover.transform.position : Vector3.zero;
            _moverEnd = _cur.Visual.Mover != null ? _cur.Visual.Mover.StandWorld(_registry.Board) : Vector3.zero;
            if (_cur.Visual.CastleRook != null)
            {
                _rookStart = _cur.Visual.CastleRook.transform.position;
                _rookEnd = _cur.Visual.CastleRook.StandWorld(_registry.Board);
            }
        }

        private void Advance(float dt)
        {
            _elapsed += dt;
            float p = Mathf.Clamp01(_elapsed / _realDuration);

            // Beat lifecycle events (for camera/audio in later phases).
            if (_beatStartReal != null)
            {
                while (_beatIndex + 1 < _beatStartReal.Length && _elapsed >= _beatStartReal[_beatIndex + 1])
                {
                    _beatIndex++;
                    BeatStarted?.Invoke(_cur.Shot.Beats[_beatIndex].Type, _beatIndex);
                }
            }

            // Mover motion: eased glide with a hop arc + squash.
            if (_cur.Visual.Mover != null)
            {
                float e = Tweener.EaseInOut(p);
                Vector3 pos = Vector3.Lerp(_moverStart, _moverEnd, e);
                pos.y += Tweener.Arc(p) * 0.35f;
                _cur.Visual.Mover.transform.position = pos;
                _cur.Visual.Mover.transform.localScale = ScaleFor(_cur.Visual.Mover, Tweener.SquashStretch(p));
            }
            if (_cur.Visual.CastleRook != null)
            {
                Vector3 rp = Vector3.Lerp(_rookStart, _rookEnd, Tweener.EaseInOut(p));
                rp.y += Tweener.Arc(p) * 0.2f;
                _cur.Visual.CastleRook.transform.position = rp;
            }

            // Kill the captured piece at impact.
            if (!_capturedHandled && _elapsed >= _impactAt)
            {
                BeginDeath(_cur.Visual.Captured);
                _capturedHandled = true;
            }

            if (p >= 1f) FinishCurrentInstant();
        }

        /// <summary>User skip: finish the current sequence immediately, snap to committed.</summary>
        public void SkipCurrent()
        {
            if (!_playing) return;
            FinishCurrentInstant();
            SequenceSkipped?.Invoke();
        }

        /// <summary>Finish everything instantly (used by tests / new-game / focus loss).</summary>
        public void FlushInstant()
        {
            while (_playing || _queue.Count > 0)
            {
                if (!_playing && _queue.Count > 0) StartNext();
                if (_playing) FinishCurrentInstant();
            }
            HardSnap();
        }

        private void FinishCurrentInstant()
        {
            if (!_capturedHandled && _cur.Visual.Captured != null) { BeginDeath(_cur.Visual.Captured); _capturedHandled = true; }
            // Immediately resolve any dying pieces from this move.
            for (int i = _dying.Count - 1; i >= 0; i--) DestroyDying(i);

            if (_cur.Visual.Mover != null)
            {
                _cur.Visual.Mover.SnapTo(_registry.Board.SquareToWorld(_cur.Visual.Mover.Square));
                _cur.Visual.Mover.transform.localScale = ScaleFor(_cur.Visual.Mover, Vector3.one);
            }
            if (_cur.Visual.CastleRook != null)
                _cur.Visual.CastleRook.SnapTo(_registry.Board.SquareToWorld(_cur.Visual.CastleRook.Square));

            _playing = false;
        }

        private void HardSnap()
        {
            for (int i = _dying.Count - 1; i >= 0; i--) DestroyDying(i);
            _registry.SnapAllToBoard();
            _playing = false;
            _queue.Clear();
        }

        private void BeginDeath(PieceView view)
        {
            if (view == null) return;
            _dying.Add(new Dying { View = view, T = 0f, Dur = 0.4f, StartScale = view.transform.localScale });
        }

        private void StepDying(float dt)
        {
            for (int i = _dying.Count - 1; i >= 0; i--)
            {
                Dying d = _dying[i];
                d.T += dt;
                float k = Mathf.Clamp01(d.T / d.Dur);
                if (d.View != null)
                {
                    d.View.transform.localScale = d.StartScale * (1f - Tweener.EaseIn(k));
                    d.View.transform.position += Vector3.down * (dt * 0.6f);
                }
                if (k >= 1f) { DestroyDying(i); continue; }
                _dying[i] = d;
            }
        }

        private void DestroyDying(int index)
        {
            Dying d = _dying[index];
            if (d.View != null)
            {
                if (Application.isPlaying) Destroy(d.View.gameObject);
                else DestroyImmediate(d.View.gameObject);
            }
            _dying.RemoveAt(index);
        }

        // Apply squash-and-stretch on top of the piece's authored scale.
        private static Vector3 ScaleFor(PieceView view, Vector3 squash) => Vector3.Scale(view.BaseScale, squash);
    }
}
