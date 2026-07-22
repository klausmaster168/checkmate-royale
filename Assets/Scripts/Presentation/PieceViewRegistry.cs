using System.Collections.Generic;
using UnityEngine;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Spawns and tracks one <see cref="PieceView"/> per piece, keyed by the same stable
    /// PieceId scheme as <see cref="CC.Position"/>/WarMemory init (ascending square order).
    /// </summary>
    public sealed class PieceViewRegistry
    {
        private readonly Dictionary<int, PieceView> _byId = new Dictionary<int, PieceView>(32);
        private readonly BoardView _board;
        private readonly Transform _parent;

        public PieceViewRegistry(BoardView board, Transform parent)
        {
            _board = board;
            _parent = parent;
        }

        public IReadOnlyDictionary<int, PieceView> ById => _byId;
        public PieceView Get(int pieceId) => _byId.TryGetValue(pieceId, out PieceView v) ? v : null;

        public void SpawnFromPosition(CC.Position pos)
        {
            Clear();
            int nextId = 1;
            for (int sq = 0; sq < 64; sq++)
            {
                CC.Piece pc = pos.Board[sq];
                if (pc == CC.Piece.None) continue;
                _byId[nextId] = CreateView(nextId, CC.Types.TypeOf(pc), CC.Types.ColorOf(pc), sq);
                nextId++;
            }
        }

        private PieceView CreateView(int id, CC.PieceType type, CC.Color side, int square)
        {
            Color team = side == CC.Color.White ? PlaceholderArt.SteelArmy : PlaceholderArt.ObsidianArmy;
            GameObject go = PlaceholderArt.CreatePiece(type, team, $"{side}_{type}_{id}");
            go.transform.SetParent(_parent, false);

            var view = go.AddComponent<PieceView>();
            view.PieceId = id;
            view.Type = type;
            view.Side = side;
            view.Square = square;
            view.SnapTo(_board.SquareToWorld(square));
            return view;
        }

        public void Clear()
        {
            foreach (PieceView v in _byId.Values)
            {
                if (v == null) continue;
                if (Application.isPlaying) Object.Destroy(v.gameObject);
                else Object.DestroyImmediate(v.gameObject);
            }
            _byId.Clear();
        }
    }
}
