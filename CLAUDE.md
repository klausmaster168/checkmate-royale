# CLAUDE.md — Checkmate Royale

## What this project is
A mobile chess game (iOS + Android, Unity 6 URP) where every move plays out as a
directed cinematic battle. Chess rules are 100% FIDE-legal and sacred. The war layer
is PRESENTATION ONLY: it never changes rules, timing, or fairness.

## The 3 unbreakable laws
1. SACRED RULES — The chess core must be exact, deterministic, and perft-verified.
   No feature may ever alter legality, clocks, or outcomes.
2. OFF-CLOCK CINEMA — Animations never consume player clock time. Moves are committed
   to game state instantly; battle sequences are local playback of committed facts.
3. DETERMINISTIC DIRECTION — Same position + same director seed => identical sequence
   on every device, forever. (Replays are PGN + seed, re-rendered locally.)

## Tech stack (do not substitute without asking)
- Client: Unity 6 (6000.x LTS), URP, C# 12 / .NET profile as per Unity
- Camera: Cinemachine 3.x   | Sequencing: Timeline + Playables API
- Content: Addressables     | Engine AI: Stockfish (NNUE) via UCI, native plugin
- Server (Phase 10+): Go + Nakama-style realtime, PostgreSQL, Redis
- Tests: Unity Test Framework (EditMode + PlayMode), NUnit

## Repository layout
Assets/Scripts/ChessCore/      pure C#, ZERO UnityEngine references (testable, portable)
Assets/Scripts/Director/       DramaScorer, ShotPlanner, BattleDirector (pure C# too)
Assets/Scripts/Presentation/   MonoBehaviours: animation, camera, VFX, audio, board view
Assets/Scripts/Meta/           UI shell, settings, profiles, economy (later phases)
Assets/Scripts/Net/            networking client (later phases)
Assets/Tests/EditMode/         fast pure-C# tests (perft, scorer determinism)
Assets/Tests/PlayMode/         scene-level tests (sequence timing, skip behavior)
Server/                        Go services (later phases)
Tools/                         content pipeline scripts

## Hard architecture rules
- ChessCore and Director assemblies must compile with no UnityEngine dependency.
  Enforce with asmdef files: ChessCore.asmdef, Director.asmdef (noEngineReferences=true).
- All randomness flows through a single seeded PRNG (xoshiro256**) injected explicitly.
  System.Random and UnityEngine.Random are BANNED outside that wrapper.
- Game state is authoritative and separate from presentation. Presentation subscribes
  to a MoveCommitted event stream; it can lag or be skipped, state never waits for it.
- No singletons except a single composition root (GameContext). Use constructor
  injection for pure C#, serialized references for MonoBehaviours.
- Every public API in ChessCore/Director gets XML doc comments and a unit test.
- Frame budget on baseline device (iPhone 12 / Pixel 6): 16.6ms. Zero GC allocs
  per frame in steady state during battle playback (use pooling, Spans, structs).

## Coding conventions
- C#: PascalCase types/methods, _camelCase private fields, readonly where possible.
- Prefer structs for board/move types; Move is an immutable readonly struct.
- No LINQ in per-frame or per-move hot paths. No async/await in ChessCore.
- Commit style: conventional commits (feat/fix/test/perf/refactor(scope): message).

## Definition of done for ANY task
1. Code compiles with zero warnings.  2. New/changed logic has tests and they pass.
3. No rule of the '3 unbreakable laws' is violated.  4. CLAUDE.md updated if
   architecture changed.  5. Short summary of what changed and how to verify it.

## When unsure
Ask before: adding packages, changing Unity/package versions, altering asmdef
structure, introducing threads, or touching anything in ChessCore after Phase 1.

---

## Build notes (this repo, pre-Unity)
Unity is not yet installed on the dev machine, so Phases 1–2 (pure C#, zero Unity) are
built and verified as a standalone .NET solution under `build/`. Unity only imports the
`Assets/` folder, so `build/` and the root `.sln` are invisible to Unity. The exact same
`.cs` source under `Assets/Scripts/ChessCore` and `Assets/Scripts/Director` is compiled
both by `dotnet` (now) and by Unity's asmdefs (later) — no code changes needed to migrate.

Run the core test suite:
```
export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$PATH"
dotnet test CheckmateRoyale.Core.slnx
```
