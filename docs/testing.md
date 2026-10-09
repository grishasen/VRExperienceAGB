# Validation and acceptance

**Current update — 2026-09-30:** M4 ensemble ledger, final result review, staged hypothetical edits, manual consistency diagnostics and equivalent short/detailed tours are implemented on `codex/m4-ensemble`. See the [M4 implementation record](m4-progress-2026-09-30.md) for current automated/build evidence. Quest M4 acceptance, outstanding M1/M3 device checks, actual performance measurements and production AGB verification remain open. Earlier dated records below are historical.

Current fixture revision: 102 EditMode tests and the independent Python checks now pass against the current AGB-like fixtures. See the [moonlit update](design/moonlit-clearing.md) for the current scene verification. Earlier M2/M3 reports remain historical records.
Status: The combined M2/M3 suite has **102 passing Unity EditMode tests and 6 passing PlayMode tests** in 6000.6.3f1, with no compilation errors. The independent Python fixture verifier also passes. See the [M2 evidence record](m2-foundation-2026-09-28.md) and [M3 evidence and reproduction record](m3-progress-2026-09-28.md). The earlier setup review found zero tests; this implementation adds the first project suite. An APK containing the evaluator and one-tree scene was built; no Simulator session or Quest acceptance run was performed. Earlier APK build/install success does not establish launch, tracking, interaction, or independent relaunch; device acceptance remains pending.

## Scheduling decision — 2026-09-28

The user requested completing D07 and D10 and postponing all other outstanding checks until main development. The [configuration checkpoint](configuration-checkpoint-2026-09-28.md) records the successful changed APK build, final permissions and exact source identity. The changed APK has not been installed or run. D02/D08 headset checks, D06 diagnostics, fresh-checkout reproduction and later M6 acceptance stay open; begin revisiting them with main development. No check is waived or marked passed by this scheduling decision.

## Current repository checks

```sh
bash scripts/check-mac.sh
python3 scripts/verify-examples.py
python3 scripts/inspect-agb-structure.py data/examples/demo-agb-export.json
```

The environment check reports missing setup as expected until installation is complete. The example verifier validates the bundled model/profile structure and compares paths, leaves, contributions, raw scores, and probabilities with the expected fixtures. It also checks representative malformed-input rejection. It also checks nested-to-normalized equivalence and bundled Unity data-copy equality. The structural inspector checks the fictional export metadata. Both use only the Python standard library.

## C# EditMode tests

The current `VRExperienceAGB.Domain.Tests` assembly covers synthetic evaluation, validation, immutable copies, import round trips, large/deep structures, and assembly independence. One-tree session/undo tests and six scene integration tests now pass; cross-tree manual constraints and ensemble transitions are now covered by the M4 suite. Test user-visible semantics independently of rendering:

- Numeric values below, equal to, and above a threshold.
- Categorical membership, unsupported categories, explicit missing values, and absent required values.
- Baseline plus weighted leaf contributions and numerically stable output transformation.
- Multiple trees and profiles; complete evaluation even when most geometry is hidden.
- Invalid IDs, child references, cycles, unsupported operators, and malformed imports.
- Manual constraint intersections and contradictions across different trees.
- Branch/leaf event commitment, undo, restart, repeated input, and mode changes.
- Re-evaluation after a profile edit, including paths in previously visited trees.

Use fixed independently expected cases. Tests that merely repeat the production implementation do not establish scoring correctness. Add trusted source-model comparisons before approving the AGB adapter.

## PlayMode checks

The saved M3 scene has six passing integration tests; see the [M3 record](m3-progress-2026-09-28.md). Broader integration checks remain planned. Validate the application state and its visual consequences: selected node maps to the correct model ID; hidden branches retain their data; returning to the diorama preserves progress; playback stops on pause; back/forward does not duplicate contributions; changing profiles clears stale highlights.

Use a small deterministic scene. Most logic tests should not require a headset or a Meta runtime.

## Quest 3 acceptance matrix

| Area | Scenario | Pass condition |
| --- | --- | --- |
| Installation | Fresh install and update of a compatible signed build | App launches and reports the intended version |
| Tracking | Look around and point with each controller | Stable tracked pose and reliable target selection |
| Manual route | Choose a path, reach a leaf, undo, choose another | Correct route and exactly one contribution per reached leaf |
| Prepared profile | Step and replay a full profile | All visible decisions match the reference route |
| Takeover | Pause profile playback and edit a feature | New result and routes are internally consistent |
| Ensemble | Compare short and detailed tour | Identical full-model score and output |
| Scale | Enter/exit a tree from the diorama | Selection/progress retained without disorientation |
| Text | Read conditions in the intended seated and standing positions | No need to lean into labels or infer text from glow |
| Recovery | Open system menu, remove headset, lose/restore tracking | Safe pause/resume with no score corruption |
| Recenter | Recenter or change seated position | Controls and return point remain usable |
| Offline use | Disconnect network and restart bundled demo | All core scenarios remain available |
| Duration | Full tour and a 30-minute soak | No crash, accumulating state error, or persistent performance collapse |
| Casting | Repeat the demo with casting if required | Presentation remains usable; measurements recorded separately |

## Performance

The initial project target is sustained native rendering at 72 FPS with a 72 Hz display rate. That corresponds to approximately 13.9 ms per frame; target useful headroom rather than treating the entire interval as available application time. Assess 90 Hz only after the baseline is stable. This is a project acceptance target, not a restatement of store minimums.

Measure CPU/GPU times, dropped frames, and memory on the actual Quest using Meta device performance tools and targeted Unity profiling. Final measurements use a release build without script debugging. Simulator or Editor FPS cannot stand in for device measurements.

Agree a worst visible scene with documented counts of active trees, nodes, text labels, and effects. Include a large-model case. Budget active detail explicitly and use generated large ensembles to verify that rendering limits never truncate scoring.

For later distribution, recheck [Meta's current performance VRC](https://developers.meta.com/horizon/resources/vrc-quest-performance-1/), which distinguishes display refresh rate, rendering rate, and exceptions.

## User comprehension

Ask at least three new users to complete the first-session route without keyboard assistance. Observe whether they can select a branch, undo, identify the current tree, and distinguish a leaf contribution from final probability. Ask them to explain why the prepared profile took one branch. Record confusion and navigation failures as implementation work, not just subjective ratings.

## Test record

Keep detailed logs/videos in ignored `artifacts/`. Summarize useful results in the relevant milestone or toolchain record.

```text
Date / source commit / app version:
Editor and package versions:
Quest OS / display rate / build type:
Fixture or approved model identifier:
Visible scene size:
Tested scenario:
Expected / actual:
Performance observations:
Pass / fail / not tested:
Remaining issues:
```

Do not mark “not tested” as a pass. A release candidate requires successful headset acceptance, correct full-model results for every supported input format, and a reproducible build configuration.

## Moonlit polish test preparation — 2026-09-28

The user requested testing after visual polish, superseding the earlier headset deferral for this scene. Six scene integration tests and independent fixture checks passed; the updated development APK built successfully. See [the polish record](design/moonlit-clearing.md#follow-up-polish--2026-09-28). The polished APK subsequently installed and cold-launched on Quest 3; OpenXR reached FOCUSED. User-observed interaction and device acceptance remain pending.

## Compact interface follow-up

Ten PlayMode tests pass after compact decision plaques and the collapsible Menu were added. The new test verifies hidden secondary controls, all menu button raycasts, movement pause/resume and preservation of an explicit pause. Smaller True/False targets require fresh Quest pointing/readability checks. See the [current development record](development-progress-2026-09-29.md#compact-interface-and-warm-moon-follow-up).


## M3 combined candidate — September 29

All 111 EditMode and 11 PlayMode tests passed after the final M3 changes. New coverage verifies pre-play profile values, stable node inspection without route/score mutation, explicit previous/new leaf totals and root feedback. Existing full-ensemble, deep-tree, pause, guarded-pointer and UI raycast checks remain green. Independent synthetic example verification passed. Raw reports and reviewed screenshots: `artifacts/m3-ready-2026-09-29/`. Physical controller haptics, readability, seated/standing comfort and performance remain pending in [the combined device checklist](m3-device-check-2026-09-29.md).

## Posture regression — September 29

The user found parts separating in the Android build when changing posture. Movable Presentation geometry had static flags; these are now cleared in the saved scene and generators. All 12 PlayMode tests pass after the correction. The added test checks three seated/standing round trips, equal movement of node meshes/rims/labels/anchors/sample/console, non-static teaching objects and unchanged preview camera pose. The lower tree layout and foreground bark/branches require renewed device observation. Reports: `artifacts/posture-fix/`.

## M4 ensemble candidate — September 30

The complete final suite passed: **135/135 EditMode and 16/16 PlayMode** in Unity 6000.6.3f1. Independent synthetic fixture checks also passed. The Android Development APK built with zero errors and six tool/library warnings; it has not been installed or device-tested for M4. The [M4 record](m4-progress-2026-09-30.md) identifies source/build/hash and covers large ensembles, ledger/tour equality, drafts, late events, profile/mode switches and manual contradictions. Complete the [M4 Quest walkthrough](m4-device-check-2026-09-30.md) before formal acceptance. Performance and production AGB verification remain open.


## M5 verification and deferred device acceptance — September 30

The user reports that the preceding navigation works and requested implementing M5 in a separate branch before a combined headset check. The new left-stick walking/crown entry, tabletop scale/rotation, summaries, guide and spatial audio require that later device check. See [M5 implementation and automated evidence](m5-progress-2026-09-30.md). An Editor pass or APK build does not accept comfort, pointing on hardware, stereo readability, audio or performance.

For that combined walkthrough, check both controllers and both postures: left-stick walking and dead zone, right-stick turning, front/side/rear crown hover and trigger entry, optional teleports, repeated entry/return with a paused profile, tabletop scale/reset/rotation and all tree identities, nested summaries and current-route recovery, Help skip/reopen, sound mute, and system-menu/tracking interruption. Record actual reading positions and label/target readability. Keep performance measurement and the new-user observation open until actually performed.

## M6 explanation additions

The September 30 implementation adds M6-09–M6-20. Include these in the deferred combined Quest walkthrough: node questions and both alternatives; full membership scrolling; portrait and repeated numeric bounds; explicit missing routing; hover gain/counts/source; crown passports from multiple approaches; composition legend; held-pointer predictor matches and return to the forest; threshold/current-cut distinction; supported-profile leaf subtotal versus full result; entrance monitoring provenance. Check both seated and standing layouts and long labels. The [implementation record](m6-in-world-explanations-2026-09-30.md) keeps automated evidence separate from hardware acceptance.

## Repeatable scenario recording — October 7, 2026

Prepare the locally available sample, open the project in Unity 6000.6.3f1, then run:

```sh
python3 scripts/prepare-local-sample.py
bash scripts/record-scenario.sh
```

The opt-in `ScenarioVideoTests` runs actual scene commands and records 1280×720 at 15 FPS with the Unity audio mix. It covers the main menu, placement, manual decisions, a complete prepared-profile traversal, final result prompt, forest/table results, simultaneous A/B, constellation guides, wolf appearance/howl/departure and mute. Long ensemble sections are explicitly captioned as accelerated; every tree is evaluated and visited. `ScenarioRevisionTests` independently checks all reference leaves, full traversal, result persistence, tracked local pose and wildlife timing.

Output: `artifacts/scenario-revision/video/scenario.mp4`, `test-result.json`, `completed.txt`, frames and audio. The script replaces the finished MP4 only after recording and encoding succeed. No headset is required. Ordinary test runs skip the video unless the request marker exists.

The recorder moves only the Editor preview camera for sky/wildlife shots. This is not a headset recording, physical-controller test, comfort assessment or performance measurement. Device acceptance still requires menu pointing, stereo readability, recenter/placement, both postures, full tour interruption/resume, spatial howl and the complete 100-tree scene on Quest 3.

Validation on October 7: **179/179 EditMode and 75/75 ordinary PlayMode tests passed**; the opt-in recording test was skipped in that ordinary suite. Reports: `artifacts/scenario-revision/editmode.json` and `playmode-final.json`. The historical component suites explicitly select their original fixture/interface before scene startup; five new default-scenario tests exercise the 100-tree sample, actual menu raycasts/guarded clicks, scene placement, completed A/B results and wolf timing.

The three local synthetic reference results are:

| Profile | Raw Score | Sigmoid probability | Trees checked |
| --- | ---: | ---: | ---: |
| Synthetic baseline | -4.936703202173835 | 0.007127064888432 | 100 |
| Synthetic alternative | -3.343727525310490 | 0.034101165982097 | 100 |
| Synthetic boundary case | -3.393406053010961 | 0.032502178573973 | 100 |

All 300 reached leaf identities match the independent nested-source traversal. The source SHA-256 is `6cd87c1839a0ab96fd2a973f5fa3256a0a6b391ebd75bc3c76edcb408485b363`. These are reconstruction checks under the documented scoring assumptions, not source-platform certification.

The opt-in video test subsequently passed separately: **1/1**, 1,367 frames at 15 FPS (91.13 seconds), H.264 1280×720 with 48 kHz stereo AAC. Reviewed frames cover menu, profile explanation, A/B paths, complete forest, table probabilities and wolf visibility. The recorder explicitly supplies desktop lifecycle state and releases audio/capture resources in test teardown, including after assertion failures.

Android Development build: **Succeeded**, zero errors and five warnings, `artifacts/builds/VRExperienceAGB-scenarios.apk` (102,837,216 bytes), SHA-256 `849ffe39a9fc1d7440dd50f303bb8f105216ae493adee2eee48d5ba740d30144`. The build report is `artifacts/scenario-revision/build.json`. This APK has not been installed or accepted on Quest 3 in this revision.

## Quest feedback follow-up — October 8, 2026

See [current fixes, test evidence and device-sampling procedure](quest-feedback-2026-10-08.md). The updated suites pass **180 EditMode and 79 PlayMode** tests. The opt-in recording passes separately and produces a 99.67-second video demonstrating stone playback, pause/resume and Calculate whole model. The previous October 7 APK was installed and launched before the user reported the issues addressed here.

The corrected APK was built, installed and launched on Quest 3. A focused 15-second sample with the user wearing the headset measured **68.65 FPS average**, median **13.889 ms** and p95 **15.511 ms**. This is a short single-tree sample with manual interactions, not a sustained frame-rate guarantee. Full details, APK identity and limitations are in the linked follow-up record.


## JSON library — October 8, 2026

See [the import guide and verification record](json-library.md). Full pre-Wi-Fi regression: 184 EditMode and 84 ordinary PlayMode passes. Additional final checks: two TCP protocol tests and four library PlayMode integration tests. The Android candidate was installed and the device HTTP endpoint imported a model plus separate A/B profile files; full three-tree scores matched the fixture (-4.1 / -2.6). A force-stop/relaunch followed by reopening the saved model restored its three trees and both profiles. Direct Wi-Fi routing remains unverified because the host reported no route; the device endpoint was exercised through USB forwarding.

Repeatable manual acceptance scenario:

1. Open Models / profiles → From computer / Wi-Fi → Start receiving. Open the address on the same network, enter the code, then upload `demo-model.json`, `profile-A.json`, and `profile-B.json` from the example ZIP in that order.
2. Put on the headset if it was removed. Confirm three trees, two profiles, and separate saved Models / Profiles listings.
3. Open a tree for manual exploration; return to the forest. Choose Compare profiles A / B, start, pause/resume, then Calculate whole model. Expect -4.1 and -2.6, and sigmoid probabilities approximately 1.63025% and 6.91384%. Show the final forest and Table View.
4. Restart the app. Open Saved models → demo-model. Confirm both profiles are restored.
5. Import malformed JSON and a profile for a different modelId. Expect a readable error and the existing model to remain usable. Open a supported nested AGB export; confirm structure viewing works and profile scoring is explicitly unavailable.
6. Stop receiving. Confirm the browser can no longer upload.

The Development build also accepts `start`, `status`, `compare`, `open-demo`, and `stop` in `json-library-request.txt` under the app persistent folder. It writes `json-library-report.json`; this opt-in probe supports automated device checks and requires the application to be ticking. `open-demo` only targets a previously saved file named demo-model. The regular user flow does not use these commands.


## Forest/playback visibility follow-up — October 8, 2026

All 13 focused ScenarioRevisionTests passed after extending the ground below scenery, moving the playback explanation board to the side, and separating model/scenery foliage under moonlight. See [the change record](quest-feedback-2026-10-08.md#forest-ground-playback-visibility-and-foliage-follow-up). Reports and reviewed Editor captures: `artifacts/visibility-fixes/`.

For repeatable Development-build captures, the device probe also accepts `forest`, `playback`, and `ab`; the latter two start a paused route for visual inspection without moving the tracked head. Resume using the playback menu. The rendering tests do not establish headset performance or user acceptance.


## Hover hints and feedback capture — October 8, 2026

Focused verification passed 15 ScenarioRevisionTests and 3 HTTP server tests. This includes saving a complete PNG in Editor, preventing repeated captures while X remains held, preserving local head pose and evaluation state, and selecting a button while its tooltip is visible. The authenticated screenshot listing/download rejects traversal and non-screenshot filenames. Final tooltip and muted palette captures were visually reviewed; see the [feedback record](quest-feedback-2026-10-08.md#muted-palette-hover-hints-and-screenshots).

Device check: hover a menu button, verify its outline/arrow and explanation; close the menu, press X on the left controller, wait for confirmation, then connect through Models / profiles → From computer / Wi-Fi and download the resulting PNG. Check both forest and paused A/B views. The Development probe accepts `screenshot` to exercise the same capture method; this does not substitute for physically pressing X.

The initial device ScreenCapture path needed an Android-relative filename and then produced a black PNG. The final capture uses a separate mono URP render instead. The focused capture test passed with an explicit non-black pixel check and 1600×900 dimensions (`artifacts/feedback-tools/capture-test.json`). Final Android Development build: zero errors, seven warnings; installed and launched on Quest. APK: `artifacts/builds/VRExperienceAGB-feedback-tools.apk`, 109356811 bytes, SHA-256 `308a177c7add863741fd8c159bac5ec0e992697dfb8cc4d91b20e7069e0e859b`. The capture method saved a visually inspected **1600×900 scene image** on Quest (`artifacts/feedback-tools/quest-screenshot.png`, 271183 bytes). The device HTTP listing and download were verified through USB forwarding; downloaded bytes matched exactly (`download-check.txt`). Direct Wi-Fi routing and physically pressing X remain user acceptance checks. The temporary server and forwarding were stopped; the failed black test PNG was removed.
