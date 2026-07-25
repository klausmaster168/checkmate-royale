using UnityEngine;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Procedurally-synthesized audio feedback (no asset files): a soft click for a move, a
    /// heavier thud for a capture, an alert tone for check, plus castle/promotion/game-over
    /// cues. Plays on each committed move. Needs an AudioListener in the scene to be heard.
    /// </summary>
    public sealed class SoundFx : MonoBehaviour
    {
        public bool Muted = false;

        private GameContext _ctx;
        private AudioSource _src;
        private AudioClip _move, _capture, _check, _castle, _promo, _end;

        // Exposed for tests (audio can't be heard headless, but selection logic is verifiable).
        public string LastPrimary { get; private set; }
        public bool PlayedCheck { get; private set; }
        public bool PlayedEnd { get; private set; }

        public void Init(GameContext ctx)
        {
            _ctx = ctx;
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;

            _move = Make(220f, 0.06f, 40f, 0.40f, "Move");
            _capture = Make(140f, 0.13f, 24f, 0.62f, "Capture");
            _check = Make(660f, 0.18f, 12f, 0.00f, "Check");
            _castle = Make(300f, 0.09f, 30f, 0.35f, "Castle");
            _promo = Make(540f, 0.25f, 8f, 0.00f, "Promotion");
            _end = Make(300f, 0.42f, 5f, 0.00f, "GameOver");

            _ctx.MoveCommittedEvent += OnMove;
        }

        private void OnMove(MoveCommitted mc)
        {
            PlayedCheck = false;
            PlayedEnd = false;

            AudioClip primary = mc.Move.IsCapture ? _capture
                              : mc.Move.IsCastle ? _castle
                              : mc.Move.IsPromotion ? _promo
                              : _move;
            LastPrimary = primary.name;
            Play(primary);

            if (_ctx.Game.InCheck) { PlayedCheck = true; Play(_check); }
            if (_ctx.Game.IsGameOver) { PlayedEnd = true; Play(_end); }
        }

        private void Play(AudioClip clip)
        {
            if (!Muted && clip != null) _src.PlayOneShot(clip);
        }

        // A short percussive/tonal one-shot: sine tone blended with noise, exponentially decayed.
        private static AudioClip Make(float freq, float dur, float decay, float noiseAmount, string name)
        {
            const int rate = 44100;
            int n = Mathf.Max(1, (int)(rate * dur));
            var data = new float[n];
            uint s = 2463534242u;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * decay);
                s = s * 1664525u + 1013904223u;
                float noise = ((s >> 8) & 0xFFFF) / 32768f - 1f;
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                data[i] = (tone * (1f - noiseAmount) + noise * noiseAmount) * env * 0.6f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
