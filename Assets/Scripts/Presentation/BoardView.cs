using UnityEngine;
using static CheckmateRoyale.ChessCore.Types;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Builds the 8x8 play surface from primitives and maps squares to world positions.
    /// Board is centred at this transform; square size = 1 unit; top surface at local y = 0.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        public const float SquareSize = 1.0f;
        private const float Offset = 3.5f; // centre the 8x8 grid on the origin
        private bool _built;

        /// <summary>World position of the top-centre of a square (where a piece stands).</summary>
        public Vector3 SquareToWorld(int square)
        {
            float x = (FileOf(square) - Offset) * SquareSize;
            float z = (RankOf(square) - Offset) * SquareSize;
            return transform.position + new Vector3(x, 0f, z);
        }

        public void Build()
        {
            if (_built) return;
            _built = true;

            // Frame slab beneath the squares.
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(transform, false);
            frame.transform.localScale = new Vector3(8.6f, 0.2f, 8.6f);
            frame.transform.localPosition = new Vector3(0, -0.16f, 0);
            frame.GetComponent<MeshRenderer>().sharedMaterial = PlaceholderArt.Get(PlaceholderArt.FrameColor);

            for (int sq = 0; sq < 64; sq++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Sq_{SquareName(sq)}";
                tile.transform.SetParent(transform, false);
                tile.transform.localScale = new Vector3(SquareSize, 0.1f, SquareSize);
                Vector3 p = SquareToWorld(sq) - transform.position;
                tile.transform.localPosition = new Vector3(p.x, -0.05f, p.z);

                bool light = ((FileOf(sq) + RankOf(sq)) & 1) == 1;
                tile.GetComponent<MeshRenderer>().sharedMaterial =
                    PlaceholderArt.Get(light ? PlaceholderArt.LightSquare : PlaceholderArt.DarkSquare);
            }
        }
    }
}
