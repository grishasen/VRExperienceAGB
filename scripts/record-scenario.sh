#!/usr/bin/env bash
# Opt-in Editor scenario recording. Run from any directory; requires an open Unity Editor.
set -euo pipefail
scenario_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$scenario_root"
scenario_output="artifacts/scenario-revision/video"
mkdir -p "$scenario_output"
# Remove only this recorder's previous frames; keep the preceding finished MP4 until replacement succeeds.
python3 - <<'PY'
from pathlib import Path
p=Path('artifacts/scenario-revision/video')
Path('BoostingExperience/Temp/pipeline_test_status.json').unlink(missing_ok=True)
for f in p.glob('frame-*.jpg'): f.unlink()
for name in ['completed.txt','audio.f32']:
 (p/name).unlink(missing_ok=True)
(p/'record-request.txt').write_text('Record the requested deterministic desktop scenario.\n')
PY
unity command run_tests --caller plugin --skill unity-cli --mode playmode --filter ScenarioVideoTests --async_tests true --format json > "$scenario_output/start.json"
scenario_deadline=$((SECONDS + 1200))
while true; do
  if (( SECONDS > scenario_deadline )); then echo "Recording timed out." >&2; exit 1; fi
  # Read the runner's persisted report: the live CLI callback can retain a stale running state after reload.
  python3 - <<'REPORT'
import json
from pathlib import Path
p=Path('BoostingExperience/Temp/pipeline_test_status.json')
r=json.loads(p.read_text()) if p.exists() else {'status':'running'}
Path('artifacts/scenario-revision/video/test-result.json').write_text(json.dumps({'data':{'result':r}},indent=2))
REPORT
  scenario_status="$(python3 -c 'import json; r=json.load(open("artifacts/scenario-revision/video/test-result.json")); print(r.get("data",{}).get("result",{}).get("status","error"))')"
  if [ "$scenario_status" = "completed" ]; then break; fi
  if [ "$scenario_status" != "running" ]; then cat "$scenario_output/test-result.json"; exit 1; fi
  sleep 5
done
python3 - <<'PY'
import json
from pathlib import Path
r=json.load(open('artifacts/scenario-revision/video/test-result.json'))['data']['result']
if r.get('summary',{}).get('passed')!=1 or r.get('summary',{}).get('failed')!=0 or not Path('artifacts/scenario-revision/video/completed.txt').exists():
 print(r);raise SystemExit(1)
PY
scenario_rate="$(cat "$scenario_output/audio-rate.txt")"
ffmpeg -hide_banner -loglevel warning -y -framerate 15 -i "$scenario_output/frame-%05d.jpg" -f f32le -ar "$scenario_rate" -ac 2 -i "$scenario_output/audio.f32" -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p -c:a aac -b:a 160k -shortest -movflags +faststart "$scenario_output/scenario-new.mp4"
mv "$scenario_output/scenario-new.mp4" "$scenario_output/scenario.mp4"
printf '%s\n' "$scenario_root/$scenario_output/scenario.mp4"
