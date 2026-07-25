using System.Collections.Generic;
using UnityEngine;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Turned (lathe) chess-piece meshes generated at runtime — a recognizable chess set with
    /// no external art. Each profile is revolved around Y into a mesh centred at the origin,
    /// baked to the piece's Silhouette-Law height. Cached per piece type.
    /// </summary>
    public static class ProceduralChessMesh
    {
        private const int Segments = 24;
        private static readonly Dictionary<CC.PieceType, Mesh> _cache = new Dictionary<CC.PieceType, Mesh>();

        public static Mesh For(CC.PieceType type)
        {
            if (_cache.TryGetValue(type, out Mesh m) && m != null) return m;
            m = Lathe(Profile(type), Segments, type.ToString());
            _cache[type] = m;
            return m;
        }

        /// <summary>Revolve a (radius, height) profile around Y into a solid, vertically centred mesh.</summary>
        private static Mesh Lathe(Vector2[] profile, int seg, string name)
        {
            int rings = profile.Length;
            float minY = profile[0].y, maxY = profile[rings - 1].y;
            float mid = (minY + maxY) * 0.5f;

            var verts = new List<Vector3>(rings * (seg + 1) + 2);
            var tris = new List<int>();
            int stride = seg + 1;

            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s <= seg; s++)
                {
                    float a = (float)s / seg * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a) * profile[r].x, profile[r].y - mid, Mathf.Sin(a) * profile[r].x));
                }
            }

            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < seg; s++)
                {
                    int i0 = r * stride + s, i1 = i0 + 1, i2 = (r + 1) * stride + s, i3 = i2 + 1;
                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            // Bottom cap (faces down) and top cap (faces up).
            int bottom = verts.Count; verts.Add(new Vector3(0, profile[0].y - mid, 0));
            for (int s = 0; s < seg; s++) { tris.Add(bottom); tris.Add(s + 1); tris.Add(s); }
            int top = verts.Count; verts.Add(new Vector3(0, profile[rings - 1].y - mid, 0));
            int topRing = (rings - 1) * stride;
            for (int s = 0; s < seg; s++) { tris.Add(top); tris.Add(topRing + s); tris.Add(topRing + s + 1); }

            var mesh = new Mesh { name = "Chess_" + name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Profiles: (radius, height) bottom-to-top, total height = Silhouette-Law height for the type.
        private static Vector2[] Profile(CC.PieceType type) => type switch
        {
            CC.PieceType.Pawn => new[]
            {
                new Vector2(0.00f,0.00f), new Vector2(0.24f,0.00f), new Vector2(0.24f,0.05f), new Vector2(0.14f,0.09f),
                new Vector2(0.095f,0.16f), new Vector2(0.115f,0.22f), new Vector2(0.085f,0.26f),
                new Vector2(0.145f,0.34f), new Vector2(0.145f,0.40f), new Vector2(0.10f,0.47f), new Vector2(0.00f,0.50f),
            },
            CC.PieceType.Knight => new[] // turned base/neck to full height; a muzzle block is added in PlaceholderArt
            {
                new Vector2(0.00f,0.00f), new Vector2(0.25f,0.00f), new Vector2(0.25f,0.05f), new Vector2(0.15f,0.10f),
                new Vector2(0.11f,0.24f), new Vector2(0.10f,0.36f), new Vector2(0.13f,0.44f), new Vector2(0.12f,0.50f),
                new Vector2(0.16f,0.56f), new Vector2(0.115f,0.62f), new Vector2(0.00f,0.65f),
            },
            CC.PieceType.Bishop => new[]
            {
                new Vector2(0.00f,0.00f), new Vector2(0.25f,0.00f), new Vector2(0.25f,0.05f), new Vector2(0.13f,0.10f),
                new Vector2(0.085f,0.22f), new Vector2(0.105f,0.32f), new Vector2(0.085f,0.42f),
                new Vector2(0.135f,0.54f), new Vector2(0.14f,0.62f), new Vector2(0.09f,0.70f), new Vector2(0.045f,0.735f), new Vector2(0.00f,0.75f),
            },
            CC.PieceType.Rook => new[] // battlement ring added on top in PlaceholderArt
            {
                new Vector2(0.00f,0.00f), new Vector2(0.27f,0.00f), new Vector2(0.27f,0.06f), new Vector2(0.16f,0.12f),
                new Vector2(0.15f,0.40f), new Vector2(0.21f,0.46f), new Vector2(0.21f,0.60f), new Vector2(0.00f,0.60f),
            },
            CC.PieceType.Queen => new[]
            {
                new Vector2(0.00f,0.00f), new Vector2(0.29f,0.00f), new Vector2(0.29f,0.06f), new Vector2(0.15f,0.13f),
                new Vector2(0.105f,0.32f), new Vector2(0.13f,0.48f), new Vector2(0.17f,0.62f), new Vector2(0.135f,0.70f),
                new Vector2(0.185f,0.78f), new Vector2(0.12f,0.84f), new Vector2(0.10f,0.88f), new Vector2(0.00f,0.90f),
            },
            CC.PieceType.King => new[]
            {
                new Vector2(0.00f,0.00f), new Vector2(0.29f,0.00f), new Vector2(0.29f,0.06f), new Vector2(0.15f,0.13f),
                new Vector2(0.105f,0.34f), new Vector2(0.13f,0.52f), new Vector2(0.17f,0.68f), new Vector2(0.14f,0.76f),
                new Vector2(0.18f,0.83f), new Vector2(0.12f,0.90f), new Vector2(0.10f,0.95f), new Vector2(0.00f,1.00f),
            },
            _ => new[] { new Vector2(0.00f,0.00f), new Vector2(0.2f,0.00f), new Vector2(0.2f,0.4f), new Vector2(0.0f,0.5f) }
        };
    }
}
