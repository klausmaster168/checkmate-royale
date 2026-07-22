using UnityEngine;
using CheckmateRoyale.ChessCore;
using CheckmateRoyale.Director;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// The single composition root per scene. Owns the authoritative <see cref="GameState"/>,
    /// the <see cref="BattleDirector"/> and <see cref="WarMemory"/>, and builds the board + pieces.
    /// Presentation reads state ONLY through here and never mutates the game itself.
    /// (Move commit + ShotList emission arrive in the next chunk.)
    /// </summary>
    public sealed class GameContext : MonoBehaviour
    {
        [SerializeField] private ulong _directorSeed = 0xC5EED12345678UL;

        public GameState Game { get; private set; }
        public BattleDirector Director { get; private set; }
        public WarMemory Memory { get; private set; }
        public BoardView Board { get; private set; }
        public PieceViewRegistry Pieces { get; private set; }

        private bool _built;

        private void Start() => Build();

        /// <summary>Idempotent build — safe to call from Start() or from editor tooling.</summary>
        public void Build()
        {
            if (_built) return;
            _built = true;

            Game = new GameState();
            Memory = new WarMemory();
            Memory.Init(Game.Position);
            Director = new BattleDirector(_directorSeed, ModeDial.Cinema);

            var boardGo = new GameObject("Board");
            boardGo.transform.SetParent(transform, false);
            Board = boardGo.AddComponent<BoardView>();
            Board.Build();

            var piecesGo = new GameObject("Pieces");
            piecesGo.transform.SetParent(transform, false);
            Pieces = new PieceViewRegistry(Board, piecesGo.transform);
            Pieces.SpawnFromPosition(Game.Position);
        }
    }
}
