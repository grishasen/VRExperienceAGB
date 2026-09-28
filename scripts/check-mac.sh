#!/bin/bash
# Read-only prerequisite discovery. --devices may start the local ADB daemon.
set -u
shopt -s nullglob

check_devices=false
case "${1:-}" in
    '') ;;
    --devices) check_devices=true ;;
    --help|-h)
        printf 'Usage: bash scripts/check-mac.sh [--devices]\n'
        printf 'Optional VRAGB_EDITOR_DIR selects a custom Unity Hub editor directory.\n'
        printf 'Optional VRAGB_PROJECT_DIR selects an explicit project; defaults to BoostingExperience.\n'
        exit 0 ;;
    *) printf 'Unknown argument: %s\n' "$1" >&2; exit 2 ;;
esac
if [ "$#" -gt 1 ]; then
    printf 'Expected at most one argument.\n' >&2
    exit 2
fi

repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
missing=0
ok() { printf '[OK] %s\n' "$1"; }
info() { printf '[INFO] %s\n' "$1"; }
need() { printf '[MISSING] %s\n' "$1"; missing=$((missing + 1)); }

if [ "$(uname -s)" != Darwin ]; then
    printf 'This environment check targets macOS.\n' >&2
    exit 2
fi
info "Host: $(uname -m), macOS $(sw_vers -productVersion)"
for tool in git python3; do
    if command -v "$tool" >/dev/null 2>&1; then
        ok "$tool is available"
    else
        need "$tool is not on PATH"
    fi
done
if xcode-select -p >/dev/null 2>&1; then
    ok 'Apple developer tools selected'
else
    need 'Apple Command Line Tools or Xcode must be selected'
fi

hub_found=false
for app in '/Applications/Unity Hub.app' "$HOME/Applications/Unity Hub.app"; do
    if [ -d "$app" ]; then hub_found=true; fi
done
if "$hub_found"; then ok 'Unity Hub found'; else need 'Unity Hub not found in standard locations'; fi

project_dir="${VRAGB_PROJECT_DIR:-$repo_dir/BoostingExperience}"
version_file="$project_dir/ProjectSettings/ProjectVersion.txt"
editor_dir="${VRAGB_EDITOR_DIR:-}"
if [ -z "$editor_dir" ] && [ -f "$version_file" ]; then
    editor_version="$(awk '/^m_EditorVersion: / {print $2; exit}' "$version_file" | tr -d '\r')"
    if [ -n "$editor_version" ]; then
        editor_dir="/Applications/Unity/Hub/Editor/$editor_version"
    fi
fi
if [ -z "$editor_dir" ]; then
    candidates=(/Applications/Unity/Hub/Editor/*/Unity.app "$HOME"/Applications/Unity/Hub/Editor/*/Unity.app)
    if [ "${#candidates[@]}" -eq 1 ]; then
        editor_dir="$(dirname "${candidates[0]}")"
    elif [ "${#candidates[@]}" -gt 1 ]; then
        info 'Several Editors found; set VRAGB_EDITOR_DIR or create a pinned Unity project'
    fi
fi

android_dir=''
if [ -n "$editor_dir" ] && [ -x "$editor_dir/Unity.app/Contents/MacOS/Unity" ]; then
    ok "Unity Editor: $editor_dir"
    for candidate in "$editor_dir/PlaybackEngines/AndroidPlayer" "$editor_dir/Unity.app/Contents/PlaybackEngines/AndroidPlayer"; do
        if [ -d "$candidate" ]; then android_dir="$candidate"; break; fi
    done
else
    need 'Unity Editor not found or not selected'
fi

adb_bin=''
if [ -n "$android_dir" ]; then
    ok 'Android Build Support found'
    for part in SDK NDK OpenJDK; do
        if [ -d "$android_dir/$part" ]; then ok "Bundled $part found"; else need "Bundled $part missing"; fi
    done
    if [ -x "$android_dir/SDK/platform-tools/adb" ]; then
        adb_bin="$android_dir/SDK/platform-tools/adb"
    fi
else
    need 'Android modules not found for the selected Editor'
fi
if [ -z "$adb_bin" ] && command -v adb >/dev/null 2>&1; then
    adb_bin="$(command -v adb)"
fi
if [ -n "$adb_bin" ]; then ok "ADB: $adb_bin"; else need 'ADB unavailable'; fi

if [ -f "$version_file" ]; then
    ok "Unity project: $project_dir"
    for file in manifest.json packages-lock.json; do
        if [ -f "$project_dir/Packages/$file" ]; then
            ok "Unity package file: $file"
        else
            need "Unity package file missing: $file"
        fi
    done
else
    need "Unity project version file missing: $version_file; see docs/macos-setup.md"
fi

for name in 'Visual Studio Code' 'Meta Quest Developer Hub' 'Meta XR Simulator'; do
    if [ -d "/Applications/$name.app" ] || [ -d "$HOME/Applications/$name.app" ]; then
        info "$name found"
    else
        info "$name not found in standard locations; see setup guide for its role"
    fi
done
if "$check_devices"; then
    if [ -n "$adb_bin" ]; then
        info 'ADB discovery (device authorization is completed in the headset)'
        if ! "$adb_bin" devices -l; then need 'ADB device discovery failed'; fi
    else
        info 'Device discovery skipped because ADB is unavailable'
    fi
fi

info 'This check does not verify SDK compatibility, account state, or headset behavior.'
if [ "$missing" -gt 0 ]; then
    printf '\nSetup incomplete: %s required checks unresolved.\n' "$missing"
    exit 1
fi
printf '\nLocal prerequisite paths found. Complete the Quest smoke test next.\n'
