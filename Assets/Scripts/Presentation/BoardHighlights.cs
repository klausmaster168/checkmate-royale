using UnityEngine;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Chess-app affordances: highlights the last move's from/to squares (amber) and the
    /// checked king's square (crimson — the design bible's colour for threat). Updates on each
    /// committed move; cleared on a new game.
    /// </summary>
    public sealed class BoardHighlights : MonoBehaviour
    {
        private BoardView _board;
        private GameContext _ctx;
        private GameObject _from, _to, _check;

        public int LastFrom { get; private set; } = -1;
        public int LastTo { get; private set; } = -1;
        public int CheckSquare { get; private set; } = -1;

        public void Init(BoardView board, GameContext ctx)
        {
            _board = board;
            _ctx = ctx;
            _from = MakeTile(new Color(0.95f, 0.78f, 0.28f));   // amber
            _to = MakeTile(new Color(0.95f, 0.78f, 0.28f));
            _check = MakeTile(new Color(0.86f, 0.18f, 0.18f));  // crimson
            HideAll();
            _ctx.MoveCommittedEvent += OnMove;
        }

        private void OnMove(MoveCommitted mc) => ShowLast(mc.Move.From, mc.Move.To);

        /// <summary>Show the last-move tiles (from &lt; 0 hides them) and refresh the check tile.</summary>
        public void ShowLast(int from, int to)
        {
            LastFrom = from;
            LastTo = to;
            if (from < 0)
            {
                _from.SetActive(false);
                _to.SetActive(false);
            }
            else
            {
                Place(_from, from, 0.012f);
                Place(_to, to, 0.012f);
            }
            UpdateCheck();
        }

        private void UpdateCheck()
        {
            if (_ctx.Game.InCheck)
            {
                CheckSquare = _ctx.Game.Position.KingSquare(_ctx.Game.SideToMove);
                Place(_check, CheckSquare, 0.02f);
            }
            else
            {
                CheckSquare = -1;
                _check.SetActive(false);
            }
        }

        /// <summary>Clear all highlights (call on a new game).</summary>
        public void Clear()
        {
            LastFrom = LastTo = CheckSquare = -1;
            HideAll();
        }

        private void Place(GameObject tile, int square, float y)
        {
            tile.transform.position = _board.SquareToWorld(square) + new Vector3(0f, y, 0f);
            tile.SetActive(true);
        }

        private void HideAll()
        {
            if (_from != null) _from.SetActive(false);
            if (_to != null) _to.SetActive(false);
            if (_check != null) _check.SetActive(false);
        }

        private GameObject MakeTile(Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Highlight";
            var col = go.GetComponent<Collider>();
            if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); } // never block picking
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(0.94f, 0.03f, 0.94f);
            go.GetComponent<MeshRenderer>().sharedMaterial = PlaceholderArt.Get(color);
            return go;
        }
    }
}
