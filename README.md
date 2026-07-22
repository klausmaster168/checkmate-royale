# Checkmate Royale ♟️⚔️

> Chess, reimagined as cinematic war — a fully legal, rating-ready chess engine where every move is a directed 3D battle sequence, and the spectacle never costs a second of clock time.

**Unity 6 (URP) · C# · deterministic engine + real-time "Battle Director"**

Checkmate Royale renders each game as a living 3D battlefield: when a knight captures a pawn, the camera drops low, the charge lands in slow-motion, the pawn falls, and a scar stays on the board for the rest of the game. Underneath, nothing changes — 100% FIDE-legal chess. On top: a war movie, directed in real time.

---

## Status

Built and verified phase-by-phase, from an empty folder to a playable cinematic core.

| Phase | What | Status |
|---|---|---|
| 0 | Project scaffold, toolchain, deterministic PRNG | ✅ |
| 1 | **Chess rules engine** — bitboard movegen, make/unmake, FEN, PGN, clocks | ✅ **perft-verified (depths 1–6)** |
| 2 | **AI Battle Director** — deterministic drama scoring → timed shot lists | ✅ golden-locked |
| 3 | Board, pieces, animation, tap-to-move, VFX, battle scars | ✅ playable |
| 4 | Five-rig Cinemachine camera system, beat-driven | ✅ |
| 5 | "Knight takes pawn" vertical slice | ✅ assembled |

**72 automated tests green** (61 EditMode + 11 PlayMode), all run inside Unity's own test runner.

## Engineering highlights

- **Perft-verified move generator** — magic-bitboard sliding attacks, legal move generation validated against the standard published node counts (startpos through 119,060,324 nodes at depth 6; Kiwipete, Position 3/4/5). `perft(5)` in **94 ms**, zero-allocation movegen.
- **Deterministic Battle Director** — the same `(position, move, seed)` always produces a byte-identical `ShotList`, so replays re-render perfectly from a few kilobytes and every device sees the same cinema. 12 golden shot lists lock the behavior. `Direct()` p99 **0.0048 ms**.
- **Off-clock cinema** — moves commit to game state instantly; animations are local playback that can lag, compress or be skipped, and never touch the game clock or `Time.timeScale`.
- **Clean architecture** — the chess core and director are pure C# with **zero `UnityEngine` dependency** (enforced by assembly definitions), so they're testable, portable, and were built + verified with the .NET SDK before Unity was even involved.

## Architecture

```
Assets/Scripts/
  ChessCore/      pure C#, zero UnityEngine — bitboards, movegen, FEN/PGN, clocks, perft
  Director/       pure C# — DramaScorer, WarMemory, EscalationBudget, ShotPlanner, BattleDirector
  Presentation/   MonoBehaviours — board, pieces, SequencePlayer, VFX, scars, input, cameras
Assets/Tests/     EditMode (fast pure-C#) + PlayMode (scene-level)
```

## Run it

Open in **Unity 6000.x LTS**, then open `Assets/Scenes/Demo_Board.unity` (full hot-seat game) or `Assets/Scenes/Slice_KnightTakesPawn.unity` (the cinematic vertical slice) and press **Play**. Tap a piece to see legal moves, tap a square to move; tap during a battle to skip.

Run the pure-C# core tests without Unity:
```bash
dotnet test CheckmateRoyale.Core.slnx
```

## License

Personal project. All rights reserved (contact for reuse).
