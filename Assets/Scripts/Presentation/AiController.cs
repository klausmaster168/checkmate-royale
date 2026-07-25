using UnityEngine;
using CheckmateRoyale.ChessCore.Ai;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Plays the built-in <see cref="SimpleEngine"/> for one colour, so the game is playable
    /// solo. Moves only when it's the AI's turn, the game isn't over, and the current sequence
    /// has finished animating (so the human's move plays out first).
    /// </summary>
    public sealed class AiController : MonoBehaviour
    {
        public GameContext Context;
        public CC.Color AiColor = CC.Color.Black;
        [Range(1, 4)] public int Depth = 3;
        public bool AiEnabled = true;

        private readonly SimpleEngine _engine = new SimpleEngine();

        private void Start()
        {
            if (Context == null) Context = FindFirstObjectByType<GameContext>();
        }

        private void Update()
        {
            if (!AiEnabled || Context == null || Context.Game == null) return;
            if (Context.EndBanner != null && Context.EndBanner.IsGameOver) return;
            if (Context.Game.SideToMove != AiColor) return;
            if (Context.Player != null && Context.Player.IsPlaying) return; // wait for the human's animation
            ThinkAndMove();
        }

        /// <summary>Compute and play the AI's move (if it is the AI's turn). Public for tests.</summary>
        public void ThinkAndMove()
        {
            if (Context == null || Context.Game == null || Context.Game.SideToMove != AiColor) return;
            if (Context.Game.IsGameOver) return;

            CC.Move move = _engine.ChooseMove(Context.Game.Position, Depth);
            if (move.IsNull) return;

            CC.PieceType promo = move.IsPromotion ? move.Promotion : CC.PieceType.Queen;
            Context.TryMakeMove(move.From, move.To, promo);
        }
    }
}
