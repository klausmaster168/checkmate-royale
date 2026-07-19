# Unity setup & merge (Phase 0 completion)

The ChessCore + Director code (Phases 1–2) was built and tested with the .NET SDK
before Unity was installed. To turn this repo into a runnable Unity project without
losing that tested code or the git history, we create a throwaway URP project and
merge its engine config in.

## 1. Install Unity
- Unity Hub (unity.com/download), sign in, activate a **free Personal** license.
- Install the latest **Unity 6 LTS** (`6000.x` LTS) with **iOS Build Support** and
  **Android Build Support** modules.

## 2. Create a throwaway URP project
Unity Hub → New project → template **Universal 3D** (URP) →
- Name: `CheckmateRoyale_UnitySrc`
- Location: `~/Claude/Projects/chess/`
- Create, let it fully import once, confirm zero console errors, then close.

(We use a separate name because our real repo folder `CheckmateRoyale/` is already
populated and Hub's "New project" needs an empty folder.)

## 3. Merge its engine config into this repo
```
cd ~/Claude/Projects/chess/CheckmateRoyale
Tools/merge_unity_config.sh ~/Claude/Projects/chess/CheckmateRoyale_UnitySrc
```
This copies `ProjectSettings/`, `Packages/`, and the URP render assets, and adds the
Phase-0 packages to `Packages/manifest.json`.

## 4. Open the merged project
Unity Hub → **Add project from disk** → select `CheckmateRoyale/`. Let it import.
Now Unity compiles our `Assets/Scripts/ChessCore` + `Director` via their asmdefs, and
the EditMode tests run in the Test Runner.

## Packages added (Phase 0 deliverable #3)
| Package | Version* | Why |
|---|---|---|
| com.unity.cinemachine | 3.1.x | Camera rigs (Phase 4) |
| com.unity.timeline | 1.8.x | Sequence/beat playback |
| com.unity.addressables | 2.3.x | Faction/arena content bundles (Phase 12) |
| com.unity.inputsystem | 1.11.x | New Input System (board input) |
| com.unity.test-framework | (template) | EditMode/PlayMode tests |
| URP (render-pipelines.universal) | 17.x (template) | Mobile rendering |

\* Package Manager pins to the versions compatible with your installed Unity 6 LTS.

## Then: remaining Phase 0 Unity bits + Phase 3
Once the project opens clean, Claude Code will: verify the asmdefs compile with no
UnityEngine leakage in ChessCore/Director, tune the URP asset for mobile (MSAA 4x,
HDR off, shadow distance 20, one cascade), set IL2CPP + Linear + ARM64, add the smoke
PlayMode test, then build Phase 3 (board view, pieces, sequence player) driven by the
ShotLists the Director already produces.
