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

        /// <summary>Build a placeholder piece object (unparented) for a piece type + colour.</summary>
        public static GameObject CreatePiece(CC.PieceType type, Color teamColor, string name)
        {
            float h = Height(type);
            GameObject go;

            switch (type)
            {
                case CC.PieceType.Knight: // cube body
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = new Vector3(0.4f, h, 0.4f);
                    break;
                case CC.PieceType.Rook: // squat cylinder tower
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.localScale = new Vector3(0.42f, h * 0.5f, 0.42f);
                    break;
                case CC.PieceType.Bishop: // slim cylinder
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.localScale = new Vector3(0.3f, h * 0.5f, 0.3f);
                    break;
                default: // pawn/queen/king = capsule of varying height
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.transform.localScale = new Vector3(0.36f, h * 0.5f, 0.36f);
                    break;
            }

            go.name = name;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Get(teamColor);

            if (type == CC.PieceType.King) // cross on top
            {
                var cross = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cross.name = "Cross";
                cross.transform.SetParent(go.transform, false);
                cross.transform.localScale = new Vector3(0.25f, 0.25f, 0.9f);
                cross.transform.localPosition = new Vector3(0, 1.05f, 0);
                cross.GetComponent<MeshRenderer>().sharedMaterial = Get(teamColor);
            }
            return go;
        }
    }
}
