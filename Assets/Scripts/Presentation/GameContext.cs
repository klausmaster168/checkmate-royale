using System;
using UnityEngine;
using CheckmateRoyale.ChessCore;
using CheckmateRoyale.Director;
using CheckmateRoyale.Presentation.Cameras;

namespace CheckmateRoyale.Presentation
{
    /// <summary>A move that has been committed to game state, with its directed sequence.</summary>
    public readonly struct MoveCommitted
    {
        public readonly Move Move;
        public readonly ShotList Shot;
        public readonly string Fen;
        public readonly PieceViewRegistry.MoveVisual Visual;

        public MoveCommitted(Move move, ShotList shot, string fen, PieceViewRegistry.MoveVisual visual)
        {
            Move = move; Shot = shot; Fen = fen; Visual = visual;
        }
    }

    /// <summary>
    /// The single composition root per scene. Owns the authoritative <see cref="GameState"/>,
    /// the <see cref="BattleDirector"/> and <see cref="WarMemory"/>, builds the board + pieces,
    /// and is the ONLY way presentation mutates the game: <see cref="TryMakeMove"/> validates,
    /// commits instantly, directs the move, and emits <see cref="MoveCommitted"/>.
    /// </summary>
    public sealed class GameContext : MonoBehaviour
    {
        [SerializeField] private ulong _directorSeed = 0xC5EED12345678UL;

        public GameState Game { get; private set; }
        public BattleDirector Director { get; private set; }
        public WarMemory Memory { get; private set; }
        public BoardView Board { get; private set; }
        public PieceViewRegistry Pieces { get; private set; }
        public SequencePlayer Player { get; private set; }
        public VFXSpawner Vfx { get; private set; }
        public BattleScars Scars { get; private set; }
        public CameraDirector Cameras { get; private set; }

        public event Action<MoveCommitted> MoveCommittedEvent;

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

            var playerGo = new GameObject("SequencePlayer");
            playerGo.transform.SetParent(transform, false);
            Player = playerGo.AddComponent<SequencePlayer>();
            Player.Init(Pieces);

            var fxGo = new GameObject("Battlefield");
            fxGo.transform.SetParent(transform, false);
            Vfx = fxGo.AddComponent<VFXSpawner>();
            Scars = fxGo.AddComponent<BattleScars>();
            Scars.Init(Board);
            Vfx.Prewarm(16);
            Scars.Prewarm(8);

            Player.CaptureImpact += OnCaptureImpact;

            var camGo = new GameObject("CameraDirector");
            camGo.transform.SetParent(transform, false);
            Cameras = camGo.AddComponent<CameraDirector>();
            Cameras.Init(Camera.main, Pieces, Board, Player); // inert if there is no main camera
        }

        private void OnCaptureImpact(Vector3 worldPos, int tier)
        {
            Vfx.Burst(worldPos, tier);
            Scars.AddScar(Board.WorldToSquare(worldPos), tier);
        }

        public ModeDial Dial
        {
            get => Director.Dial;
            set => Director.Dial = value;
        }

        /// <summary>
        /// Validate a move, commit it to game state INSTANTLY, direct it, and enqueue the
        /// sequence for playback. Returns false if the move is illegal.
        /// </summary>
        public bool TryMakeMove(int from, int to, PieceType promotion = PieceType.Queen)
        {
            if (!_built || Game.IsGameOver) return false;

            Move move = FindLegal(from, to, promotion);
            if (move.IsNull) return false;

            CheckmateRoyale.ChessCore.Color mover = Game.Position.SideToMove;
            Position before = Game.Position.Clone();
            Game.MakeMove(move);
            Position after = Game.Position.Clone();

            var input = new DirectorInput(move, before, after, null, null,
                                          ClockState.Untimed, Memory, Director.Seed, Game.PlyCount);
            ShotList shot = Director.Direct(input);
            Director.Commit(input, shot);

            PieceViewRegistry.MoveVisual visual = Pieces.ApplyMove(move, mover);
            var committed = new MoveCommitted(move, shot, Fen.ToFen(after), visual);

            MoveCommittedEvent?.Invoke(committed);
            Player.Enqueue(committed);
            return true;
        }

        private Move FindLegal(int from, int to, PieceType promotion)
        {
            Span<Move> buf = stackalloc Move[MoveGenerator.MaxMoves];
            int n = Game.LegalMoves(buf);
            for (int i = 0; i < n; i++)
            {
                Move m = buf[i];
                if (m.From != from || m.To != to) continue;
                if (m.IsPromotion && m.Promotion != promotion) continue;
                return m;
            }
            return Move.Null;
        }

        /// <summary>Restart from the initial position.</summary>
        public void NewGame()
        {
            Player.FlushInstant();
            Game = new GameState();
            Memory = new WarMemory();
            Memory.Init(Game.Position);
            Director = new BattleDirector(_directorSeed, Director?.Dial ?? ModeDial.Cinema);
            Pieces.SpawnFromPosition(Game.Position);
            Scars.Clear();
        }
    }
}
