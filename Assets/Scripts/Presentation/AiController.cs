using UnityEngine;
using CheckmateRoyale.ChessCore.Ai;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Plays the built-in <see cref="SimpleEngine"/> for one colour, so the game is playable
    /// solo. Moves only when it's the AI's turn, the game isn't over, and the current sequence
    /// has finished animating. Shows a one-frame "thinking…" indicator before it searches, and
    /// supports Easy / Medium / Hard difficulty.
    /// </summary>
    public sealed class AiController : MonoBehaviour
    {
        public enum Difficulty { Easy, Medium, Hard }

        public GameContext Context;
        public CC.Color AiColor = CC.Color.Black;
        public Difficulty Level = Difficulty.Medium;
        public bool AiEnabled = true;

        public bool IsThinking { get; private set; }

        private readonly SimpleEngine _engine = new SimpleEngine();
        private bool _armed; // one frame of "thinking…" shown before the (blocking) search

        private void Start()
        {
            if (Context == null) Context = FindFirstObjectByType<GameContext>();
        }

        private void Update()
        {
            if (!AiEnabled || Context == null || Context.Game == null || !IsAiTurnReady())
            {
                IsThinking = false;
                _armed = false;
                return;
            }

            if (!_armed)
            {
                IsThinking = true; // render one frame with the indicator first
                _armed = true;
                return;
            }

            ThinkAndMove();
            IsThinking = false;
            _armed = false;
        }

        private bool IsAiTurnReady()
        {
            if (Context.EndBanner != null && Context.EndBanner.IsGameOver) return false;
            if (Context.Game.SideToMove != AiColor) return false;
            if (Context.Player != null && Context.Player.IsPlaying) return false;
            return true;
        }

        public int DepthFor(Difficulty d) => d == Difficulty.Easy ? 2 : d == Difficulty.Medium ? 3 : 4;
        private int TopNFor(Difficulty d) => d == Difficulty.Easy ? 3 : 1;

        /// <summary>Compute and play the AI's move (if it is the AI's turn). Public for tests.</summary>
        public void ThinkAndMove()
        {
            if (Context == null || Context.Game == null || Context.Game.SideToMove != AiColor) return;
            if (Context.Game.IsGameOver) return;

            CC.Position pos = Context.Game.Position;
            int depth = DepthFor(Level);
            int topN = TopNFor(Level);
            CC.Move move = topN > 1 ? _engine.ChooseMoveVaried(pos, depth, topN) : _engine.ChooseMove(pos, depth);
            if (move.IsNull) return;

            CC.PieceType promo = move.IsPromotion ? move.Promotion : CC.PieceType.Queen;
            Context.TryMakeMove(move.From, move.To, promo);
        }

        private void OnGUI()
        {
            if (!AiEnabled) return;

            // Difficulty cycle button (top-right, above the perf area / clear of the board).
            var btn = new GUIStyle(GUI.skin.button) { fontSize = 14 };
            if (GUI.Button(new Rect(Screen.width - 150, Screen.height - 44, 130, 32), $"AI: {Level}", btn))
                Level = (Difficulty)(((int)Level + 1) % 3);

            if (IsThinking)
            {
                var style = new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.95f, 0.85f, 0.4f) } };
                GUI.Label(new Rect(0, 24, Screen.width, 30), "AI is thinking…", style);
            }
        }
    }
}
