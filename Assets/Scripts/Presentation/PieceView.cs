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

        /// <summary>Place the piece standing on the given board-surface world point.</summary>
        public void SnapTo(Vector3 surfaceWorld)
        {
            float halfHeight = PlaceholderArt.Height(Type) * 0.5f;
            transform.position = surfaceWorld + new Vector3(0f, halfHeight, 0f);
        }
    }
}
