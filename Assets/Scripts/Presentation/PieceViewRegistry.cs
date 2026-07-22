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
        private readonly PieceView[] _atSquare = new PieceView[64];
        private readonly BoardView _board;
        private readonly Transform _parent;

        public PieceViewRegistry(BoardView board, Transform parent)
        {
            _board = board;
            _parent = parent;
        }

        public BoardView Board => _board;
        public IReadOnlyDictionary<int, PieceView> ById => _byId;
        public PieceView Get(int pieceId) => _byId.TryGetValue(pieceId, out PieceView v) ? v : null;
        public PieceView At(int square) => (uint)square < 64 ? _atSquare[square] : null;
        public int LiveCount => _byId.Count;

        /// <summary>Describes the visual consequences of a committed move (for the animator).</summary>
        public struct MoveVisual
        {
            public PieceView Mover;
            public int FromSquare, ToSquare;
            public PieceView Captured;   // detached from the board maps; animator disposes it
            public PieceView CastleRook;
            public int RookFrom, RookTo;
        }

        public void SpawnFromPosition(CC.Position pos)
        {
            Clear();
            int nextId = 1;
            for (int sq = 0; sq < 64; sq++)
            {
                CC.Piece pc = pos.Board[sq];
                if (pc == CC.Piece.None) continue;
                PieceView v = CreateView(nextId, CC.Types.TypeOf(pc), CC.Types.ColorOf(pc), sq);
                _byId[nextId] = v;
                _atSquare[sq] = v;
                nextId++;
            }
        }

        /// <summary>
        /// Update the logical board maps for a committed move and return what changed.
        /// Transforms are NOT moved here — the SequencePlayer animates and then snaps.
        /// </summary>
        public MoveVisual ApplyMove(in CC.Move m, CC.Color mover)
        {
            int from = m.From, to = m.To;
            var visual = new MoveVisual { FromSquare = from, ToSquare = to, RookFrom = -1, RookTo = -1 };

            PieceView movingView = _atSquare[from];
            visual.Mover = movingView;

            // Capture (incl. en passant) — detach the captured view from the maps.
            if (m.IsCapture)
            {
                int capSq = m.IsEnPassant ? (mover == CC.Color.White ? to - 8 : to + 8) : to;
                PieceView cap = _atSquare[capSq];
                if (cap != null)
                {
                    _atSquare[capSq] = null;
                    _byId.Remove(cap.PieceId);
                    visual.Captured = cap;
                }
            }

            // Move the piece in the maps.
            _atSquare[from] = null;
            _atSquare[to] = movingView;
            if (movingView != null) movingView.Square = to;

            // Castling: relocate the rook too.
            switch (m.Flag)
            {
                case CC.MoveFlag.KingCastle: MoveRook(mover == CC.Color.White ? 7 : 63, mover == CC.Color.White ? 5 : 61, ref visual); break;
                case CC.MoveFlag.QueenCastle: MoveRook(mover == CC.Color.White ? 0 : 56, mover == CC.Color.White ? 3 : 59, ref visual); break;
            }

            // Promotion: change type (and placeholder height) in place, id preserved.
            if (m.IsPromotion && movingView != null)
            {
                movingView.Type = m.Promotion;
                float h = PlaceholderArt.Height(m.Promotion);
                var s = movingView.transform.localScale;
                var newScale = new Vector3(s.x, h * 0.5f, s.z);
                movingView.transform.localScale = newScale;
                movingView.BaseScale = newScale;
            }

            return visual;
        }

        private void MoveRook(int rookFrom, int rookTo, ref MoveVisual visual)
        {
            PieceView rook = _atSquare[rookFrom];
            _atSquare[rookFrom] = null;
            _atSquare[rookTo] = rook;
            if (rook != null) rook.Square = rookTo;
            visual.CastleRook = rook;
            visual.RookFrom = rookFrom;
            visual.RookTo = rookTo;
        }

        /// <summary>Snap every live piece to its committed square (used on skip / queue flush).</summary>
        public void SnapAllToBoard()
        {
            foreach (PieceView v in _byId.Values)
                if (v != null) v.SnapTo(_board.SquareToWorld(v.Square));
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
            view.BaseScale = go.transform.localScale;
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
            for (int i = 0; i < 64; i++) _atSquare[i] = null;
        }
    }
}
