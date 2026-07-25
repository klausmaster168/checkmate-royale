using UnityEngine;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// A live chess clock built on the pure <see cref="CC.Clock"/> logic: the side to move
    /// ticks down in real time; committing a move presses the clock (switch + increment); a
    /// flag fall ends the game as a timeout. Time is driven via <see cref="Tick"/> so tests
    /// can advance it deterministically.
    /// </summary>
    public sealed class ChessClock : MonoBehaviour
    {
        private GameContext _ctx;
        private CC.TimeControl _tc;
        private CC.Clock _clock;
        private float _turnElapsed;
        private bool _running;

        public bool Enabled = true; // false => casual, no clock
        public bool Flagged { get; private set; }
        public CC.Color FlaggedSide { get; private set; }

        public void Init(GameContext ctx, CC.TimeControl tc)
        {
            _ctx = ctx;
            _tc = tc;
            ResetClock();
            _ctx.MoveCommittedEvent += OnMove;
        }

        /// <summary>Switch time control and restart the clock.</summary>
        public void SetTimeControl(CC.TimeControl tc)
        {
            _tc = tc;
            ResetClock();
        }

        public void ResetClock()
        {
            _clock = new CC.Clock(_tc);
            _turnElapsed = 0f;
            _running = true;
            Flagged = false;
        }

        /// <summary>Displayed milliseconds for a colour (the active side's live-counts down).</summary>
        public long DisplayMs(CC.Color c)
        {
            long rem = _clock.Remaining(c);
            if (_running && !GameOver() && c == _ctx.Game.SideToMove)
                rem -= (long)(_turnElapsed * 1000f);
            return rem < 0 ? 0 : rem;
        }

        private bool GameOver() =>
            _ctx.Game.IsGameOver || (_ctx.EndBanner != null && _ctx.EndBanner.IsGameOver);

        private void OnMove(MoveCommitted mc)
        {
            CC.Color mover = CC.Types.Opposite(_ctx.Game.SideToMove); // side flipped after the move
            _clock.Press(mover, (long)(_turnElapsed * 1000f));
            _turnElapsed = 0f;
            if (GameOver()) _running = false;
        }

        private void Update() => Tick(Time.unscaledDeltaTime);

        /// <summary>Advance the active side's clock by <paramref name="dt"/> seconds; flag if it runs out.</summary>
        public void Tick(float dt)
        {
            if (!Enabled || !_running || _ctx == null || _ctx.Game == null || GameOver()) return;
            _turnElapsed += dt;

            CC.Color active = _ctx.Game.SideToMove;
            if (DisplayMs(active) <= 0)
            {
                Flagged = true;
                FlaggedSide = active;
                _running = false;
                CC.GameResult winner = active == CC.Color.White ? CC.GameResult.BlackWins : CC.GameResult.WhiteWins;
                _ctx.EndBanner?.ForceResult(winner, CC.GameEndReason.Timeout);
            }
        }

        private void OnGUI()
        {
            if (!Enabled) return;
            DrawClock(CC.Color.Black, 60f);
            DrawClock(CC.Color.White, 100f);
        }

        private void DrawClock(CC.Color c, float y)
        {
            bool active = _running && !GameOver() && _ctx.Game.SideToMove == c;
            long ms = DisplayMs(c);
            bool low = ms <= 20000;

            var box = new Rect(16f, y, 96f, 34f);
            GUI.color = active ? new Color(0.12f, 0.14f, 0.2f, 0.95f) : new Color(0.05f, 0.06f, 0.09f, 0.8f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            if (active)
            {
                GUI.color = new Color(0.95f, 0.78f, 0.28f, 1f);
                GUI.DrawTexture(new Rect(box.x, box.y, box.width, 3f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            var style = new GUIStyle
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = low ? new Color(0.95f, 0.35f, 0.32f) : Color.white }
            };
            long total = ms / 1000;
            GUI.Label(box, $"{(c == CC.Color.White ? "White" : "Black")}  {total / 60:0}:{total % 60:00}", style);
        }
    }
}
