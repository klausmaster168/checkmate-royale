#!/usr/bin/env bash
# Merge a freshly-created Unity URP project's engine config into THIS repo,
# preserving our existing Assets/Scripts (ChessCore + Director) and git history.
#
# Usage:  Tools/merge_unity_config.sh /path/to/FreshUnityURPProject
#
# It copies the Unity-generated pieces we can't hand-author reliably
# (Packages/, ProjectSettings/, the URP render-pipeline assets, the sample scene),
# then adds the Phase-0 packages to Packages/manifest.json.
set -euo pipefail

SRC="${1:-}"
REPO="$(cd "$(dirname "$0")/.." && pwd)"

if [[ -z "$SRC" || ! -d "$SRC" ]]; then
  echo "ERROR: pass the path to the freshly-created Unity URP project." >&2
  echo "  e.g. Tools/merge_unity_config.sh ~/Claude/Projects/chess/CheckmateRoyale_UnitySrc" >&2
  exit 1
fi
if [[ ! -d "$SRC/ProjectSettings" || ! -f "$SRC/Packages/manifest.json" ]]; then
  echo "ERROR: '$SRC' does not look like a Unity project (no ProjectSettings/ or Packages/manifest.json)." >&2
  exit 1
fi

echo "Merging Unity config from: $SRC"
echo "                     into: $REPO"

# 1) Engine config Unity owns.
cp -R "$SRC/ProjectSettings" "$REPO/"
cp -R "$SRC/Packages" "$REPO/"

# 2) URP render-pipeline assets + sample scene the template generates under Assets/.
for item in Settings Scenes InputSystem_Actions.inputactions DefaultVolumeProfile.asset UniversalRenderPipelineGlobalSettings.asset; do
  if [[ -e "$SRC/Assets/$item" ]]; then
    cp -R "$SRC/Assets/$item" "$REPO/Assets/"
    [[ -e "$SRC/Assets/$item.meta" ]] && cp "$SRC/Assets/$item.meta" "$REPO/Assets/"
    echo "  + Assets/$item"
  fi
done

# 3) Add the Phase-0 packages (idempotent). Versions resolve against the editor's Unity 6 LTS.
python3 - "$REPO/Packages/manifest.json" <<'PY'
import json, sys
path = sys.argv[1]
with open(path) as f: m = json.load(f)
deps = m.setdefault("dependencies", {})
wanted = {
    "com.unity.cinemachine":  "3.1.2",   # Cinemachine 3.x — camera rigs (Phase 4)
    "com.unity.timeline":     "1.8.7",   # sequence playback
    "com.unity.addressables": "2.3.1",   # faction/arena content delivery (Phase 12)
    "com.unity.inputsystem":  "1.11.2",  # new Input System (Active Input Handling)
    # com.unity.test-framework and URP are already added by the URP template.
}
for pkg, ver in wanted.items():
    deps.setdefault(pkg, ver)
with open(path, "w") as f:
    json.dump(m, f, indent=2); f.write("\n")
print("  manifest.json packages:", ", ".join(sorted(deps)))
PY

echo
echo "Done. Next: open $REPO in Unity Hub via 'Add project from disk' and let it import."
echo "Then tell Claude Code the import finished and any console output."
