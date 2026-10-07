#!/usr/bin/env bash
# Requires the corrected Development APK and an awake, worn Quest connected via ADB.
set -euo pipefail
probe_root="$(cd "$(dirname "$0")/.." && pwd)"
probe_package="com.vrexperienceagb.prototype"
probe_storage="/sdcard/Android/data/$probe_package/files"
probe_output="$probe_root/artifacts/tree-performance"
mkdir -p "$probe_output"
adb shell am start -n "$probe_package/com.unity3d.player.UnityPlayerGameActivity" >/dev/null
adb shell mkdir -p "$probe_storage"
adb shell rm -f "$probe_storage/tree-performance.json"
printf 'single\n' > "$probe_output/tree-performance-request.txt"
adb push "$probe_output/tree-performance-request.txt" "$probe_storage/tree-performance-request.txt" >/dev/null
printf 'Wear the headset. Waiting for a focused 15-second sample after warmup.\n'
probe_deadline=$((SECONDS + 180))
while (( SECONDS < probe_deadline )); do
  if adb shell cat "$probe_storage/tree-performance.json" > "$probe_output/quest-sample.json" 2>/dev/null; then
    if python3 -c 'import json,sys; sys.exit(json.load(open(sys.argv[1])).get("status")!="Completed")' "$probe_output/quest-sample.json"; then
      cat "$probe_output/quest-sample.json"
      exit 0
    fi
  fi
  sleep 3
done
printf 'No completed active-headset sample. No performance acceptance recorded.\n' >&2
exit 1
