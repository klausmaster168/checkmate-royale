using System.Collections.Generic;
using UnityEngine;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Procedural placeholder art: URP materials and primitive piece meshes. Real faction
    /// prefabs replace these later with no code changes elsewhere (the resolver keys by
    /// piece type + intent). Kept tiny and pooled-friendly.
    /// </summary>
    public static class PlaceholderArt
    {
        private static readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();
        private static Shader _litShader;

        public static readonly Color LightSquare = new Color(0.82f, 0.78f, 0.66f);
        public static readonly Color DarkSquare = new Color(0.30f, 0.34f, 0.42f);
        public static readonly Color FrameColor = new Color(0.12f, 0.12f, 0.14f);
        public static readonly Color SteelArmy = new Color(0.80f, 0.82f, 0.88f);  // white / Iron Crown
        public static readonly Color ObsidianArmy = new Color(0.16f, 0.16f, 0.20f); // black / Obsidian Horde

        public static Material Get(Color color)
        {
            if (_materials.TryGetValue(color, out Material m) && m != null) return m;
            if (_litShader == null) _litShader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(_litShader != null ? _litShader : Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
            _materials[color] = mat;
            return mat;
        }

        /// <summary>Height (world units) of each piece placeholder — encodes the Silhouette Law ladder.</summary>
        public static float Height(CC.PieceType t) => t switch
        {
            CC.PieceType.Pawn => 0.5f,
            CC.PieceType.Knight => 0.65f,
            CC.PieceType.Bishop => 0.75f,
            CC.PieceType.Rook => 0.6f,
            CC.PieceType.Queen => 0.9f,
            CC.PieceType.King => 1.0f,
            _ => 0.5f
        };

        /// <summary>Build a turned (lathe) placeholder chess piece for a type + colour, with decorative features.</summary>
        public static GameObject CreatePiece(CC.PieceType type, Color teamColor, string name)
        {
            var go = new GameObject(name);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralChessMesh.For(type);
            go.AddComponent<MeshRenderer>().sharedMaterial = Get(teamColor);

            var col = go.AddComponent<BoxCollider>();
            Bounds b = mf.sharedMesh.bounds;
            col.center = b.center;
            col.size = new Vector3(Mathf.Max(b.size.x, 0.4f), b.size.y, Mathf.Max(b.size.z, 0.4f));

            AddFeatures(go, type, teamColor);
            return go;
        }

        // Small decorative pieces on top of the turned body (may protrude above the piece height).
        private static void AddFeatures(GameObject parent, CC.PieceType type, Color color)
        {
            float top = Height(type) * 0.5f;
            switch (type)
            {
                case CC.PieceType.King: // cross
                    Feature(parent, color, new Vector3(0.055f, 0.24f, 0.055f), new Vector3(0, top + 0.10f, 0), Quaternion.identity);
                    Feature(parent, color, new Vector3(0.17f, 0.055f, 0.055f), new Vector3(0, top + 0.13f, 0), Quaternion.identity);
                    break;
                case CC.PieceType.Queen: // crown of spikes
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i / 6f * Mathf.PI * 2f;
                        Feature(parent, color, new Vector3(0.05f, 0.11f, 0.05f),
                            new Vector3(Mathf.Cos(a) * 0.135f, top + 0.03f, Mathf.Sin(a) * 0.135f), Quaternion.identity);
                    }
                    break;
                case CC.PieceType.Rook: // battlements
                    for (int i = 0; i < 4; i++)
                    {
                        float a = (i + 0.5f) / 4f * Mathf.PI * 2f;
                        Feature(parent, color, new Vector3(0.10f, 0.12f, 0.10f),
                            new Vector3(Mathf.Cos(a) * 0.15f, top + 0.02f, Mathf.Sin(a) * 0.15f), Quaternion.identity);
                    }
                    break;
                case CC.PieceType.Knight: // forward-leaning muzzle for a horse hint
                    Feature(parent, color, new Vector3(0.15f, 0.16f, 0.28f),
                        new Vector3(0f, top - 0.02f, 0.10f), Quaternion.Euler(35f, 0f, 0f));
                    break;
            }
        }

        private static void Feature(GameObject parent, Color color, Vector3 scale, Vector3 pos, Quaternion rot)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "Feature";
            c.transform.SetParent(parent.transform, false);
            c.transform.localScale = scale;
            c.transform.localPosition = pos;
            c.transform.localRotation = rot;
            c.GetComponent<MeshRenderer>().sharedMaterial = Get(color);
        }
    }
}
