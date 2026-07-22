# Building Checkmate Royale to a device (Phase 5)

The vertical slice lives in **`Assets/Scenes/Slice_KnightTakesPawn.unity`**.
Open it, press **Play**, hit **ATTACK** — the knight charges e5 as a directed Cinema capture.
Use **Replay** (identical) and **Variation** (new seed) to compare cuts. The perf HUD
(top-right) shows fps / frame-ms / GC-per-frame.

Project settings are already mobile-ready (from `Checkmate Royale ▸ Apply Mobile Settings`):
Linear color space, IL2CPP, Android ARM64, .NET Standard 2.1, incremental GC, URP tuned
(MSAA 4×, HDR off, shadow distance 20, single cascade).

## Android APK
1. `File ▸ Build Profiles` (or Build Settings) → add **Slice_KnightTakesPawn** as scene 0.
2. Platform **Android** → *Switch Platform*.
3. Player Settings → Other Settings:
   - Scripting Backend **IL2CPP**, Target Architectures **ARM64** only.
   - Minimum API Level 24+, Target API Level = highest installed.
4. Build → produces an `.apk`. Install: `adb install -r CheckmateRoyale.apk`.
5. Target: **< 150 MB**, 60 fps median / p95 < 20 ms on a Pixel-6-class device.

Headless CI build (optional):
```
"$UNITY" -batchmode -quit -projectPath . -buildTarget Android \
  -executeMethod <YourBuildScript.BuildAndroid> -logFile build.log
```
(Requires the Android SDK/NDK that shipped with the Android module.)

## iOS Xcode project (macOS)
1. Platform **iOS** → *Switch Platform*.
2. Player Settings → IL2CPP, target minimum iOS 13+.
3. Build → produces an **Xcode project** (not an `.ipa` directly).
4. Open in Xcode, set your signing Team, select your iPhone, **Run**.
5. Target device: iPhone-12-class, same fps budget.

## The real gate (not code)
Put the build on a phone and show it to **10 people**. Ask: *"Did you feel that?"*
Target **8/10 yes**. That result — not any test — decides whether the whole plan proceeds.

## Recording a share clip
`Window ▸ General ▸ Recorder` (add the Unity Recorder package) → record ~15 s of the
slice at 1080×1920 portrait → MP4. This is the marketing artifact the design bible's
launch plan is built around.
