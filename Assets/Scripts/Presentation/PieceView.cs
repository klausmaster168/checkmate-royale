using UnityEngine;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>A single piece in the scene, tracked by its stable Director PieceId.</summary>
    public sealed class PieceView : MonoBehaviour
    {
        public int PieceId;
        public CC.PieceType Type;
        public CC.Color Side;
        public int Square;
        public Vector3 BaseScale = Vector3.one; // authored (un-squashed) local scale

        /// <summary>Place the piece standing on the given board-surface world point.</summary>
        public void SnapTo(Vector3 surfaceWorld)
        {
            transform.position = StandPosition(surfaceWorld);
        }

        /// <summary>World position where this piece should stand on a given surface point.</summary>
        public Vector3 StandPosition(Vector3 surfaceWorld)
        {
            return surfaceWorld + new Vector3(0f, PlaceholderArt.Height(Type) * 0.5f, 0f);
        }

        /// <summary>World position where this piece should stand for its current logical square.</summary>
        public Vector3 StandWorld(BoardView board) => StandPosition(board.SquareToWorld(Square));
    }
}
