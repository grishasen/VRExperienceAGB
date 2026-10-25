#!/bin/bash
set -euo pipefail
VRAGB_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VRAGB_PLUGIN="$VRAGB_ROOT/BoostingExperience/Assets/Plugins/macOS/libVRAGBDesktop.dylib"
mkdir -p "$(dirname "$VRAGB_PLUGIN")"
xcrun clang++ -dynamiclib -fobjc-arc -framework AppKit -arch arm64 -arch x86_64 \
  -mmacosx-version-min=11.0 -Wno-deprecated-declarations \
  "$VRAGB_ROOT/native/macos/FilePicker.mm" -o "$VRAGB_PLUGIN"
codesign --force --sign - "$VRAGB_PLUGIN"
