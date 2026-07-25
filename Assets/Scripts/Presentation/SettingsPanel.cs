using UnityEngine;
using CheckmateRoyale.Director;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// One panel to tune the game: sound on/off, reduced-motion (caps pacing at Battle),
    /// play-vs-AI + difficulty, and time control (bullet/blitz/rapid/casual). Apply methods
    /// are public so the settings are testable without the UI.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        public enum TimeOption { Bullet, Blitz, Rapid, Casual }

        public GameContext Context;
        public bool ReducedMotion { get; private set; }
        public TimeOption Time { get; private set; } = TimeOption.Blitz;

        private AiController _ai;
        private bool _open;

        private AiController Ai => _ai != null ? _ai : (_ai = FindFirstObjectByType<AiController>());

        private void Start() { if (Context == null) Context = FindFirstObjectByType<GameContext>(); }

        // ---- apply methods (public for tests) ----
        public void SetSoundMuted(bool muted) { if (Context != null && Context.Sound != null) Context.Sound.Muted = muted; }

        public void SetReducedMotion(bool reduced)
        {
            ReducedMotion = reduced;
            if (Context != null) Context.Dial = reduced ? ModeDial.Battle : ModeDial.Cinema;
        }

        public void SetAiEnabled(bool enabled) { if (Ai != null) Ai.AiEnabled = enabled; }
        public void SetDifficulty(AiController.Difficulty d) { if (Ai != null) Ai.Level = d; }

        public void SetTimeOption(TimeOption t)
        {
            Time = t;
            if (Context == null || Context.Clock == null) return;
            if (t == TimeOption.Casual) Context.Clock.Enabled = false;
            else { Context.Clock.Enabled = true; Context.Clock.SetTimeControl(TcFor(t)); }
            Context.NewGame();
        }

        private static CC.TimeControl TcFor(TimeOption t) => t switch
        {
            TimeOption.Bullet => CC.TimeControl.Bullet1_0,
            TimeOption.Rapid => CC.TimeControl.Rapid10_5,
            _ => CC.TimeControl.Blitz3_2
        };

        private void OnGUI()
        {
            if (GUI.Button(new Rect(Screen.width - 150, 62, 130, 30), _open ? "✕ Close" : "⚙ Settings"))
                _open = !_open;
            if (!_open) return;

            const float w = 230f, h = 262f;
            float x = Screen.width - w - 16f, y = 98f;
            GUI.color = new Color(0.05f, 0.06f, 0.09f, 0.96f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(x + 12, y + 10, w - 24, h - 20));

            bool muted = Context != null && Context.Sound != null && Context.Sound.Muted;
            bool wantSound = GUILayout.Toggle(!muted, " Sound");
            if (wantSound == muted) SetSoundMuted(!wantSound);

            bool rm = GUILayout.Toggle(ReducedMotion, " Reduced motion");
            if (rm != ReducedMotion) SetReducedMotion(rm);

            bool aiOn = Ai != null && Ai.AiEnabled;
            bool wantAi = GUILayout.Toggle(aiOn, " Play vs AI");
            if (wantAi != aiOn) SetAiEnabled(wantAi);

            GUILayout.Space(6);
            GUILayout.Label("Difficulty");
            GUILayout.BeginHorizontal();
            foreach (AiController.Difficulty d in new[] { AiController.Difficulty.Easy, AiController.Difficulty.Medium, AiController.Difficulty.Hard })
                if (GUILayout.Toggle(Ai != null && Ai.Level == d, d.ToString(), "Button") && (Ai == null || Ai.Level != d))
                    SetDifficulty(d);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Time control");
            GUILayout.BeginHorizontal();
            foreach (TimeOption t in new[] { TimeOption.Bullet, TimeOption.Blitz, TimeOption.Rapid, TimeOption.Casual })
                if (GUILayout.Toggle(Time == t, Label(t), "Button") && Time != t)
                    SetTimeOption(t);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private static string Label(TimeOption t) => t switch
        {
            TimeOption.Bullet => "1+0",
            TimeOption.Blitz => "3+2",
            TimeOption.Rapid => "10+5",
            _ => "Casual"
        };
    }
}
