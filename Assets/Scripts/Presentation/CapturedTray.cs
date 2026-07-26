using System.Text;
using UnityEngine;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Shows captured pieces and the material advantage for each side — derived directly from
    /// the current position (starting material minus what's on the board), so it stays correct
    /// through undo/new-game with no event bookkeeping.
    /// </summary>
    public sealed class CapturedTray : MonoBehaviour
    {
        public GameContext Context;

        private static readonly CC.PieceType[] Order =
            { CC.PieceType.Queen, CC.PieceType.Rook, CC.PieceType.Bishop, CC.PieceType.Knight, CC.PieceType.Pawn };

        private static int Start(CC.PieceType t) => t switch
        {
            CC.PieceType.Pawn => 8,
            CC.PieceType.Knight => 2,
            CC.PieceType.Bishop => 2,
            CC.PieceType.Rook => 2,
            CC.PieceType.Queen => 1,
            _ => 0
        };

        private static int Value(CC.PieceType t) => t switch
        {
            CC.PieceType.Pawn => 1,
            CC.PieceType.Knight => 3,
            CC.PieceType.Bishop => 3,
            CC.PieceType.Rook => 5,
            CC.PieceType.Queen => 9,
            _ => 0
        };

        private static char Letter(CC.PieceType t) => t switch
        {
            CC.PieceType.Queen => 'Q',
            CC.PieceType.Rook => 'R',
            CC.PieceType.Bishop => 'B',
            CC.PieceType.Knight => 'N',
            _ => 'P'
        };

        private int OnBoard(CC.Color c, CC.PieceType t) => CC.Bitboards.PopCount(Context.Game.Position.PieceBB(c, t));

        /// <summary>How many pieces of <paramref name="t"/> the given side has captured.</summary>
        public int CapturedCount(CC.Color capturer, CC.PieceType t)
        {
            CC.Color victimSide = capturer == CC.Color.White ? CC.Color.Black : CC.Color.White;
            return Start(t) - OnBoard(victimSide, t);
        }

        /// <summary>Material advantage in points, positive = White ahead.</summary>
        public int MaterialAdvantage()
        {
            int adv = 0;
            foreach (CC.PieceType t in Order)
                adv += Value(t) * (OnBoard(CC.Color.White, t) - OnBoard(CC.Color.Black, t));
            return adv;
        }

        private string CapturedString(CC.Color capturer)
        {
            var sb = new StringBuilder();
            foreach (CC.PieceType t in Order)
                for (int i = 0; i < CapturedCount(capturer, t); i++) sb.Append(Letter(t));
            return sb.ToString();
        }

        private void OnGUI()
        {
            if (Context == null || Context.Game == null) return;

            int adv = MaterialAdvantage();
            DrawRow(CC.Color.Black, 140f, adv < 0 ? $"+{-adv}" : "");  // Black's captures (top)
            DrawRow(CC.Color.White, 168f, adv > 0 ? $"+{adv}" : "");   // White's captures
        }

        private void DrawRow(CC.Color capturer, float y, string advText)
        {
            var style = new GUIStyle { fontSize = 13, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.85f, 0.87f, 0.92f) } };
            string label = $"{(capturer == CC.Color.White ? "W" : "B")}: {CapturedString(capturer)}";
            if (advText.Length > 0) label += $"  {advText}";
            GUI.Label(new Rect(16f, y, 260f, 20f), label, style);
        }
    }
}
