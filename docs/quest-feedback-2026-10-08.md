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


## Forest ground, playback visibility and foliage follow-up

The forest floor previously ended at x = ±21 m while decorative pines were placed around x = ±26–34 m, with some also beyond its rear edge. The floor now includes the actual bounds of every decorative pine, plus a 12 m margin, independent of the loaded ensemble size. This adds no new ground draw calls.

The playback explanation board moves from the central line of sight to the left of the stone route. It faces the observer at entry/recenter and stays world anchored while the head moves. The smaller board does not intercept pointer rays. Existing pause/resume and full-model calculation remain in the menu.

Model foliage uses a stronger green tint and a low green emission floor to retain its hue under blue moonlight. Decorative foliage uses a stronger blue emission floor. Table View now shares the model foliage material rather than using the original asset separately. Geometry, scoring and headset pose are unchanged.

All **13 ScenarioRevisionTests** passed, including coverage of scenery ground bounds for both the 100-tree sample and a small imported model, projected ball visibility in single/A/B playback, and shared forest/table foliage. Editor screenshots were visually inspected in `artifacts/visibility-fixes/forest.png` and `playback.png`; the new panel leaves the central route clear. These are Editor checks; final Quest appearance requires headset confirmation.


The follow-up Android Development APK built with zero errors and six warnings, installed successfully on the connected Quest, and was launched. File: `artifacts/builds/VRExperienceAGB-visibility-fixes.apk`, 102850453 bytes; SHA-256 `669fba70765649987af855d209258768f8521f5a469dfb962df271e1adc059e2`. Final headset visual acceptance remains open.


## Muted palette, hover hints and screenshots

Following feedback that the previous palette was too vivid, model trees now use muted forest green and scenery uses grey-blue. Emission floors are reduced from 0.22 green / 0.48 blue to 0.06 / 0.065; Table View retains the same model material.

Buttons in the unified menu and the shared forest/table/tool factories show an arrow and outline on hover. Command-specific explanations appear above the unified menu; other controls show their label and trigger instruction near the control. Hint graphics do not intercept pointer rays and disappear when the button is hidden or the pointer exits.

**X on the left controller / F12** requests an application screenshot. A and B keep their menu/recenter assignments. Captures render a 1600×900 mono view through URP using a temporary non-XR camera, unique filenames and a two-second cooldown. The tracked camera is untouched; confirmation appears after the asynchronous PNG write completes. The session-protected browser page includes screenshot downloads. See [sharing screenshots](json-library.md#sharing-screenshots).

All **15 focused ScenarioRevisionTests** passed, including an actual Editor PNG capture, hold-to-repeat prevention and hover/click coexistence. All **3 HTTP tests** passed, including screenshot authentication and path restrictions. The final tooltip position above the menu and muted forest palette were visually inspected in Editor. Evidence: `artifacts/feedback-tools/playmode.json`, `http-tests.json`, `hint.png`, `forest.png`. Android and physical controller evidence follow separately; these tests do not establish Quest image output.


The initial device ScreenCapture path needed an Android-relative filename and then produced a black PNG. The final capture uses a separate mono URP render instead. The focused capture test passed with an explicit non-black pixel check and 1600×900 dimensions (`artifacts/feedback-tools/capture-test.json`). Final Android Development build: zero errors, seven warnings; installed and launched on Quest. APK: `artifacts/builds/VRExperienceAGB-feedback-tools.apk`, 109356811 bytes, SHA-256 `308a177c7add863741fd8c159bac5ec0e992697dfb8cc4d91b20e7069e0e859b`. The capture method saved a visually inspected **1600×900 scene image** on Quest (`artifacts/feedback-tools/quest-screenshot.png`, 271183 bytes). The device HTTP listing and download were verified through USB forwarding; downloaded bytes matched exactly (`download-check.txt`). Direct Wi-Fi routing and physically pressing X remain user acceptance checks. The temporary server and forwarding were stopped; the failed black test PNG was removed.


## Collapsible forest summary

The large forest information card starts closed. A small **Forest info** button opens it, and **Hide forest info** or the card's close button hides it. The main menu also exposes **Forest info** so the card can be recalled after walking away from the entrance. Opening places it ahead of the current horizontal gaze without moving the tracked head. The open/closed choice survives scene navigation within the running session; the card is hidden during tree viewing, playback, menus and Table View. Model metadata and evaluation are unchanged.

The focused PlayMode regression passed (1/1), exercising actual pointer events, close-button raycasts, navigation persistence, menu reopening and unchanged model/score. The open panel was visually inspected in Editor. Evidence: `artifacts/forest-info/test.json` and `open.png`.

Android Development build succeeded with zero errors and nine warnings, then installed and launched on Quest. APK: `artifacts/builds/VRExperienceAGB-forest-info.apk`; SHA-256 `24ffa255bd477f18594bc5f91787023e550f0dd65c81cddaf4acd158e412ad70`. Physical headset interaction acceptance remains pending.

## Single-tree text readability

Following the four October 8 user captures, the segment and evidence side cards are hidden. Their complete evidence and path conditions remain in the node hover detail, including long category lists. Main questions, branch controls, node labels and route status use larger text. The detail panel uses fixed 32-point light text, an opaque background and an independent canvas placed 2.2 m ahead when opened. It stays anchored while reading; repeated entry to the same visible content preserves scrolling.

Editor inspection exposed background stone labels drawing over the detail panel. The detail canvas now sorts above these labels. Text is separated from its background plane, inertial scrolling is disabled, and content height is resolved once for each new source instead of using a live ContentSizeFitter. These changes address overlap and clipping instability; absence of the reported flicker on the physical headset still requires confirmation.

Visual evidence: `artifacts/text-readability/tree.png` and `detail.png`. The long-condition scroll regression passed. The new scenario regression verifies hidden side cards, fixed text size, sorting priority, stable position/scroll across delayed feature linking and an unchanged route revision.

Final validation: **17/17 ScenarioRevisionTests passed**, plus the long-condition scrolling test (1/1). Android Development build `artifacts/builds/VRExperienceAGB-readable-text.apk` succeeded with zero errors and six toolchain/Pipeline/TMP warnings. Installation on the connected Quest succeeded. Physical readability and flicker acceptance remain pending.

Menu hover follow-up: removed the redundant arrow and replaced the gold outline with a thin, muted blue-gray highlight. Compilation and Android build passed (zero errors); `artifacts/builds/VRExperienceAGB-subtle-hover.apk` was installed on Quest. Physical visual acceptance remains pending.
