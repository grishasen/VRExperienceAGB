# Quest feedback follow-up — October 8, 2026

## Changes

- Single-tree explanations no longer rebuild every frame. Ensemble consistency is cached against all tree-session revisions and session-array identity; branch changes and undo invalidate the report.
- The unified menu remains at its initial world placement. Explicit scene/menu placement commands can reposition it. Entering a tree aligns the stones with the horizontal gaze at that moment, preserving tracked local pose.
- Decorative backdrop pines use pale snowy foliage, distinct from green model pines.
- Table height represents relative tree depth and width represents relative leaf count across the complete loaded model. Pointing at a tree opens its passport without selecting or changing its route.
- Profile playback uses the existing stone platforms and branches. A ball traverses each evaluated route, shows the leaf contribution and accumulated score, then advances to the next tree. A/B uses two simultaneous balls; divergent paths occupy adjacent stone groups. Seven platforms are reused for deep trees; evaluation always includes the complete ensemble.
- Both playback modes offer **Calculate whole model**. It skips animation, commits all evaluated leaf results and opens the final probability prompt with forest/table results available; it never truncates the ensemble.
- Playback defaults to 0.45 seconds per decision, 0.3 seconds per movement and 1.2 seconds per tree result. The playback menu provides pause/resume, step, previous tree, speed and restart. Opening the menu also suspends advancement.

## Performance evidence

The reported bottleneck was reproduced on the local 100-tree sample in the Editor. Twenty explicit explanation refreshes averaged **43.113915 ms** before the fix and **0.16942 ms** after consistency caching. The frame loop now only updates visibility; it does not call this content refresh. Reports are in `artifacts/tree-performance/before-editor.json` and `after-editor.json`.

These are Editor CPU timings for one operation, not headset FPS or total frame times. The allocation counter was not usable and provides no allocation evidence. The short device measurement below supplements these Editor timings; full interactive and sustained performance acceptance remains open.

## Device sampling

Run `bash scripts/measure-single-tree.sh` with the corrected Development APK installed and the Quest connected via ADB.

Development builds include an opt-in probe. Create `tree-performance-request.txt` in the application's persistent data directory using ADB. The probe waits for application focus and a running XR display, enters the first tree, warms up for three seconds and collects 15 seconds of Unity frame intervals. Any focus loss resets the sample. It never writes the tracked head pose.

The resulting `tree-performance.json` contains frame count, elapsed seconds, mean FPS, median and p95 frame intervals, device identity and focus/XR state. This measures the application frame cadence, not GPU execution time or compositor dropped frames. It is one single-tree sample and does not replace interactive acceptance across the forest, playback and menus.

## Automated verification

The final ordinary suites passed **180/180 EditMode** and **79/79 PlayMode** tests; the opt-in video was skipped in the ordinary suite. Reports: `artifacts/tree-performance/editmode.json` and `playmode-final.json`. New coverage verifies consistency-cache invalidation, idle-frame behavior, gaze entry and anchored menu, actual stone-anchor interpolation, pause/resume, tabletop size/hover behavior, and immediate full-model results for both modes. Full A/B traversal still visits all 100 trees.

The tabletop regression also checks every number target and the table/help controls via actual EventSystem raycasts. Number selection remains above the crown; a child hit area supports pointing at the foliage.

The separate video test passed **1/1**: 1,495 frames, 99.67 seconds, H.264 1280×720 at 15 FPS with stereo AAC. Reviewed captures show actual stones and balls, pause/fast-calculation controls, varied tabletop trees and final A/B results. Output: `artifacts/scenario-revision/video/scenario.mp4`. This is an Editor recording, not headset performance evidence.

## Installed build and actual Quest sample

The Android Development APK built successfully with zero errors and eight warnings, then installed with `adb install -r` and launched on the connected Quest 3. File: `artifacts/builds/VRExperienceAGB-tree-fixes.apk`, 109,331,956 bytes, SHA-256 `cb96d792959ecb13ec2a0c34c4b0a0e02ccf7afcd7b9f84d1b22e45b7ce68b90`. The build warnings include existing audio/pipeline/TMP messages and the new probe's deprecated `DEVELOPMENT_BUILD` directive; compilation succeeded.

With the user wearing the headset, the probe captured **1,030 frames over 15.004 seconds**, with application focus and a running XR display. Mean **68.65 FPS**, median **13.889 ms**, p95 **15.511 ms**. The 100-tree model was loaded and one tree was shown. The captured log also includes manual branch interactions during this sample. Report: `artifacts/tree-performance/quest-sample.json`; process log: `quest-log.txt`. No Unity exception or Android fatal exception appeared in the captured process log. Two later OpenXR Meta messages reported that `xrDiscoverSpacesMETA` could not be looked up; spatial discovery is not part of this scenario and was not tested.

This confirms a working on-device sample close to the 72 Hz cadence, with some slower frames; it does not demonstrate a sustained 72 FPS lock or establish a numerical before/after headset comparison. No baseline headset timing was recorded. Broader forest/playback performance and interaction acceptance remain pending.

## Follow-up: scenery contrast and controller recenter

The user reported that the first snowy tint did not distinguish the large scenery trees sufficiently. The replacement uses an explicit blue base tint and blue emission visible under the night lighting. A shared runtime material is applied to decorative branches in both the original stone-scene environment and the forest backdrop. Model pines retain their green material.

Right-controller **B** now centers the current scene and unified menu on a fresh button press. **R** provides the desktop equivalent. The table is placed ahead when in Table View; stones are centered in tree/playback views. Holding B does not continuously follow the head. Tracked local pose, playback progress, pause and evaluated scores are preserved. The menu's existing recenter action uses the same implementation.

All **11 focused ScenarioRevisionTests** pass, including the new tests for shared scenery materials and button press/release behavior across tree and table views. Physical B-button acceptance and the new scenery color on Quest remain to be checked by the user.

The follow-up recording passed **1/1** and was visually inspected for blue scenery versus green model pines (`artifacts/tree-performance/blue-frost-forest.jpg`). The follow-up Android Development build succeeded with zero errors and six warnings, and was installed on Quest with `adb install -r`. APK: `artifacts/builds/VRExperienceAGB-blue-frost-recenter.apk`, 109,331,857 bytes, SHA-256 `c5d22c8e1e0337e12146c88ca8fdde5d128c5e1a409fea09a8d9ea874e6b43a7`. Physical button/color acceptance remains pending; the earlier performance sample belongs to the preceding APK.
