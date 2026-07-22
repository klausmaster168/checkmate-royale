using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Minimal hot-seat input: tap a piece to select (legal targets highlight), tap a target
    /// to move. Tapping while a sequence plays skips it. Promotion defaults to Queen for now.
    /// Selection logic lives in <see cref="HandleSquareTapped"/> so it is testable without a mouse.
    /// </summary>
    public sealed class DemoController : MonoBehaviour
    {
        public GameContext Context;
        public Camera Cam;

        private int _selected = -1;
        private readonly List<int> _targets = new List<int>(28);
        private readonly List<GameObject> _highlights = new List<GameObject>(28);
        private readonly CC.Move[] _moveBuffer = new CC.Move[CC.MoveGenerator.MaxMoves];

        private void Awake() { if (Cam == null) Cam = Camera.main; }
        private void Start() { if (Context == null) Context = FindFirstObjectByType<GameContext>(); }

        private void Update()
        {
            if (Context == null) return;

            if (!TryReadTap(out Vector2 screenPos)) return;

            if (Context.Player != null && Context.Player.IsPlaying)
            {
                Context.Player.SkipCurrent();
                return;
            }

            int sq = Pick(screenPos);
            if (sq >= 0) HandleSquareTapped(sq);
            else ClearSelection();
        }

        private static bool TryReadTap(out Vector2 pos)
        {
            pos = default;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pos = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            return false;
        }

        private int Pick(Vector2 screenPos)
        {
            if (Cam == null) return -1;
            Ray ray = Cam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return -1;
            var pv = hit.collider.GetComponentInParent<PieceView>();
            if (pv != null) return pv.Square;
            return Context.Board.WorldToSquare(hit.point);
        }

        /// <summary>Core selection/move logic. Public so tests can drive it directly.</summary>
        public void HandleSquareTapped(int square)
        {
            CC.Position pos = Context.Game.Position;
            CC.Piece here = pos.Board[square];
            bool ownPieceHere = here != CC.Piece.None && CC.Types.ColorOf(here) == pos.SideToMove;

            if (_selected < 0)
            {
                if (ownPieceHere) Select(square);
                return;
            }

            if (square == _selected) { ClearSelection(); return; }

            if (_targets.Contains(square))
            {
                Context.TryMakeMove(_selected, square);
                ClearSelection();
                return;
            }

            if (ownPieceHere) Select(square); // switch selection
            else ClearSelection();
        }

        private void Select(int square)
        {
            ClearSelection();
            _selected = square;

            int n = Context.Game.LegalMoves(_moveBuffer);
            for (int i = 0; i < n; i++)
                if (_moveBuffer[i].From == square && !_targets.Contains(_moveBuffer[i].To))
                    _targets.Add(_moveBuffer[i].To);

            ShowHighlights();
        }

        public int SelectedSquare => _selected;
        public IReadOnlyList<int> Targets => _targets;

        private void ShowHighlights()
        {
            foreach (int sq in _targets)
            {
                var hl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hl.name = "Highlight";
                var col = hl.GetComponent<Collider>();
                if (col != null) Destroy(col); // never block piece/board picking
                hl.transform.SetParent(transform, false);
                hl.transform.localScale = new Vector3(0.9f, 0.02f, 0.9f);
                hl.transform.position = Context.Board.SquareToWorld(sq) + new Vector3(0f, 0.02f, 0f);
                hl.GetComponent<MeshRenderer>().sharedMaterial = PlaceholderArt.Get(new Color(0.35f, 0.85f, 0.45f));
                _highlights.Add(hl);
            }
        }

        private void ClearSelection()
        {
            for (int i = 0; i < _highlights.Count; i++)
                if (_highlights[i] != null) Destroy(_highlights[i]);
            _highlights.Clear();
            _targets.Clear();
            _selected = -1;
        }
    }
}
