using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Takeback reverts moves and re-syncs the board view + highlights.</summary>
    public class UndoTests
    {
        [UnityTest]
        public IEnumerator Undo_RevertsMoves_AndResyncsBoard()
        {
            var ctx = new GameObject("GameContext").AddComponent<GameContext>();
            ctx.Build();

            ctx.TryMakeMove(12, 28); // e4
            ctx.TryMakeMove(52, 36); // e5
            Assert.AreEqual(2, ctx.Game.PlyCount);

            // Undo the black pawn's move.
            Assert.AreEqual(1, ctx.Undo(1));
            Assert.AreEqual(1, ctx.Game.PlyCount);
            Assert.IsNull(ctx.Pieces.At(36), "e5 should be empty again");
            PieceView backHome = ctx.Pieces.At(52);
            Assert.IsNotNull(backHome, "black pawn should be back on e7");
            Assert.AreEqual(CC.PieceType.Pawn, backHome.Type);
            Assert.AreEqual(CC.Color.Black, backHome.Side);
            Assert.AreEqual(12, ctx.Highlights.LastFrom, "last-move highlight now shows e4");
            Assert.AreEqual(28, ctx.Highlights.LastTo);

            // Undo white's move too => start position.
            Assert.AreEqual(1, ctx.Undo(1));
            Assert.AreEqual(0, ctx.Game.PlyCount);
            Assert.IsNull(ctx.Pieces.At(28), "e4 should be empty");
            Assert.IsNotNull(ctx.Pieces.At(12), "white pawn back on e2");
            Assert.AreEqual(-1, ctx.Highlights.LastFrom, "no last move to highlight");

            // Undo with empty history is a no-op.
            Assert.AreEqual(0, ctx.Undo(1));

            yield return null;
            Object.Destroy(ctx.gameObject);
        }
    }
}
