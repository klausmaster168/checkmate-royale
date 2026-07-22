using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheckmateRoyale.Presentation;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Tests.PlayMode
{
    /// <summary>Tap-to-move selection logic (driven directly, no real pointer needed).</summary>
    public class DemoControllerTests
    {
        [UnityTest]
        public IEnumerator TapSelectThenMove_CommitsLegalMove()
        {
            var ctxGo = new GameObject("GameContext");
            var ctx = ctxGo.AddComponent<GameContext>();
            ctx.Build();

            var ctrlGo = new GameObject("DemoController");
            var ctrl = ctrlGo.AddComponent<DemoController>();
            ctrl.Context = ctx;

            // Tap e2 (square 12) — a white pawn — should select and list legal targets.
            ctrl.HandleSquareTapped(12);
            Assert.AreEqual(12, ctrl.SelectedSquare, "e2 should be selected");
            Assert.That(ctrl.Targets, Has.Member(28), "e4 should be a legal target");
            Assert.That(ctrl.Targets, Has.Member(20), "e3 should be a legal target");

            // Tap e4 (square 28) — a legal target — should commit e2e4 and clear selection.
            ctrl.HandleSquareTapped(28);
            Assert.AreEqual(-1, ctrl.SelectedSquare, "selection should clear after moving");
            yield return null;

            Assert.AreEqual(1, ctx.Game.PlyCount, "a move should have been committed");
            Assert.AreEqual(CC.Piece.WP, ctx.Game.Position.Board[28], "white pawn should be on e4");
            Assert.AreEqual(CC.Color.Black, ctx.Game.Position.SideToMove, "turn should pass to Black");

            Object.Destroy(ctrlGo);
            Object.Destroy(ctxGo);
        }

        [UnityTest]
        public IEnumerator TappingEmptyOrEnemyFirst_DoesNothing()
        {
            var ctxGo = new GameObject("GameContext");
            var ctx = ctxGo.AddComponent<GameContext>();
            ctx.Build();
            var ctrl = new GameObject("DemoController").AddComponent<DemoController>();
            ctrl.Context = ctx;

            ctrl.HandleSquareTapped(28); // empty e4
            Assert.AreEqual(-1, ctrl.SelectedSquare);
            ctrl.HandleSquareTapped(52); // black pawn e7 — not side to move
            Assert.AreEqual(-1, ctrl.SelectedSquare);

            yield return null;
            Assert.AreEqual(0, ctx.Game.PlyCount);
            Object.Destroy(ctrl.gameObject);
            Object.Destroy(ctxGo);
        }
    }
}
