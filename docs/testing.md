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
