#!/bin/bash
# Ad-hoc signing for local testing; this does not notarize a distribution release.
set -euo pipefail
VRAGB_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VRAGB_APP="${1:-$VRAGB_ROOT/BoostingExperience/Builds/macOS/VRExperienceAGB.app}"
test -d "$VRAGB_APP/Contents"
# Unity can thin bundled native plugins without refreshing their signatures.
codesign --force --deep --sign - "$VRAGB_APP"
codesign --verify --deep --strict "$VRAGB_APP"
