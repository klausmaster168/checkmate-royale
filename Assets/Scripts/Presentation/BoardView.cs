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

        /// <summary>Nearest square to a world point, or -1 if it falls outside the board.</summary>
        public int WorldToSquare(Vector3 world)
        {
            Vector3 local = world - transform.position;
            int file = Mathf.RoundToInt(local.x / SquareSize + Offset);
            int rank = Mathf.RoundToInt(local.z / SquareSize + Offset);
            if (file < 0 || file > 7 || rank < 0 || rank > 7) return -1;
            return SquareOf(file, rank);
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

            AddCoordinates();
        }

        private void AddCoordinates()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 48);
            var labelColor = new Color(0.72f, 0.74f, 0.80f);

            for (int file = 0; file < 8; file++)
                Label(((char)('a' + file)).ToString(), new Vector3((file - Offset) * SquareSize, 0.02f, -Offset * SquareSize - 0.7f), font, labelColor);
            for (int rank = 0; rank < 8; rank++)
                Label((rank + 1).ToString(), new Vector3(-Offset * SquareSize - 0.7f, 0.02f, (rank - Offset) * SquareSize), font, labelColor);
        }

        private void Label(string text, Vector3 localPos, Font font, Color color)
        {
            var go = new GameObject("Lbl_" + text);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lie flat, readable from above
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = font;
            tm.fontSize = 64;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }
}
