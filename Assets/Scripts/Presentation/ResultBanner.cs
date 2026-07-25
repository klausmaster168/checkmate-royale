using UnityEngine;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Detects terminal game states after each committed move and shows a result banner
    /// ("Checkmate — White wins", "Stalemate — Draw", …) with a New Game button.
    /// </summary>
    public sealed class ResultBanner : MonoBehaviour
    {
        private GameContext _ctx;

        public CC.GameResult Result { get; private set; } = CC.GameResult.Ongoing;
        public CC.GameEndReason Reason { get; private set; } = CC.GameEndReason.None;
        public string Message { get; private set; } = "";

        public bool IsGameOver => Result != CC.GameResult.Ongoing;

        public void Init(GameContext ctx)
        {
            _ctx = ctx;
            _ctx.MoveCommittedEvent += OnMove;
        }

        private void OnMove(MoveCommitted mc)
        {
            var (result, reason) = _ctx.Game.GetResult();
            Result = result;
            Reason = reason;
            Message = result == CC.GameResult.Ongoing ? "" : Describe(result, reason);
        }

        public void Clear()
        {
            Result = CC.GameResult.Ongoing;
            Reason = CC.GameEndReason.None;
            Message = "";
        }

        public static string Describe(CC.GameResult result, CC.GameEndReason reason)
        {
            string outcome = result switch
            {
                CC.GameResult.WhiteWins => "White wins",
                CC.GameResult.BlackWins => "Black wins",
                CC.GameResult.Draw => "Draw",
                _ => ""
            };
            return reason switch
            {
                CC.GameEndReason.Checkmate => $"Checkmate — {outcome}",
                CC.GameEndReason.Stalemate => "Stalemate — Draw",
                CC.GameEndReason.FiftyMove => "Draw — 50-move rule",
                CC.GameEndReason.ThreefoldRepetition => "Draw — threefold repetition",
                CC.GameEndReason.InsufficientMaterial => "Draw — insufficient material",
                _ => outcome
            };
        }

        private void OnGUI()
        {
            if (!IsGameOver) return;

            const float w = 470, h = 156;
            var rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2.4f, w, h);

            Color prev = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.78f, 0.28f, 1f); // gold rule
            GUI.DrawTexture(new Rect(rect.x, rect.y, w, 4f), Texture2D.whiteTexture);
            GUI.color = prev;

            var title = new GUIStyle { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            GUI.Label(new Rect(rect.x, rect.y + 24, w, 42), Message, title);

            var sub = new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.78f, 0.80f, 0.86f) } };
            GUI.Label(new Rect(rect.x, rect.y + 66, w, 22), "Game over", sub);

            var btn = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            if (GUI.Button(new Rect(rect.x + w / 2f - 85, rect.y + 98, 170, 42), "New Game", btn))
                _ctx.NewGame();
        }
    }
}
