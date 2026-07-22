# Art pass: dropping in the free faction models

The game runs on primitive placeholders. Real models are a **data-only** swap — no code
changes — via the `FactionArt` seam. Any piece slot left empty keeps its placeholder, so
you can upgrade one piece at a time.

## 1. Get the free packs (all CC0 / free-commercial)
- **KayKit** — https://kaylousberg.itch.io (Skeletons = Necropolis; Adventurers = Iron Crown)
- **Quaternius** — https://quaternius.com (Ultimate Animated Character Pack, Universal Animation Library)
- **Mixamo** — https://mixamo.com (free rigged animations; export FBX, Unity Humanoid rig)

Download, unzip, and drag the model folders into `Assets/Art/<pack>/` in Unity.

## 2. Make a piece prefab (per piece type)
1. Drag a model into an empty scene.
2. Set its material(s); add an `Animator` later for real clips.
3. Drag it into `Assets/Art/Prefabs/` to make a prefab. Pivot/scale don't matter — the
   registry auto-fits each piece to board height and centres it.

## 3. Create a FactionArt asset
`Assets ▸ Create ▸ Checkmate Royale ▸ Faction Art`. Assign a prefab per piece type
(Pawn/Knight/Bishop/Rook/Queen/King). Leave any slot empty to keep its placeholder.
Optionally set **Team Tint** + tick **Apply Tint** to colour a shared model per side.

Suggested faction → pack mapping:
- **Iron Crown** (white) → KayKit Adventurers / Quaternius knights
- **Obsidian Horde** (black) → Quaternius / KayKit, Apply Tint = dark
- **Necropolis** → KayKit Skeletons

## 4. Assign to the scene
Select the **GameContext** object in `Demo_Board.unity` / `Slice_KnightTakesPawn.unity`
and drop your FactionArt assets into **White Art** and **Black Art**. Press Play — the
armies are now real models, animated by the same ShotLists, framed by the same cameras.

## Later: real animation clips
The `AnimationIntentResolver` (Phase 3/12) will map ShotList animation-intent ids to
Mixamo/pack `AnimationClip`s per piece type. Until then, pieces use the procedural
placeholder tweens (march/attack/death) regardless of mesh — so imported models already
move; swapping in real clips is the next data-only upgrade.
