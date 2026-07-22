using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>The art seam: assigned faction prefabs replace primitives; unassigned fall back.</summary>
    public class ArtSeamTests
    {
        [UnityTest]
        public IEnumerator FactionPrefab_ReplacesPrimitive_WithFallback()
        {
            // A stand-in "model" prefab (a cube) for pawns only.
            var template = GameObject.CreatePrimitive(PrimitiveType.Cube);
            template.name = "TplPawn";

            var art = ScriptableObject.CreateInstance<FactionArt>();
            art.Pawn = template;          // pawns use the prefab
            art.Knight = null;            // knights fall back to the primitive

            var ctxGo = new GameObject("GameContext");
            var ctx = ctxGo.AddComponent<GameContext>();
            ctx.WhiteArt = art;           // white uses the faction art; black stays primitive
            ctx.Build();

            // White pawn on a2 (square 8) => built from the prefab wrapper (has a child model + BoxCollider).
            PieceView whitePawn = ctx.Pieces.At(8);
            Assert.IsNotNull(whitePawn);
            Assert.Greater(whitePawn.transform.childCount, 0, "prefab-built pawn should wrap a model child");
            Assert.IsNotNull(whitePawn.GetComponent<BoxCollider>(), "prefab wrapper should have a picking collider");

            // White knight on b1 (square 1) => no prefab assigned => primitive (no child).
            PieceView whiteKnight = ctx.Pieces.At(1);
            Assert.IsNotNull(whiteKnight);
            Assert.AreEqual(0, whiteKnight.transform.childCount, "unassigned knight should stay a primitive");

            // Black pawn on a7 (square 48) => no black art => primitive.
            PieceView blackPawn = ctx.Pieces.At(48);
            Assert.IsNotNull(blackPawn);
            Assert.AreEqual(0, blackPawn.transform.childCount, "black pawn should stay a primitive");

            yield return null;
            Object.Destroy(ctxGo);
            Object.Destroy(template);
            Object.Destroy(art);
        }
    }
}
