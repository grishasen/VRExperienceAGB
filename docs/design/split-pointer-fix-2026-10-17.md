# Split-card controller pointer fix — October 17, 2026

## Reproduction

Aiming at a split label did not open its description on Quest. In the live Editor, rays from both controller positions hit the old **LearningConsole** plane before reaching the visible node labels. Its rectangle was 1600 × 2400 canvas units, inherited from the earlier multi-section controls, even though the current view only uses the lower branch controls. Hiding graphics did not remove this Meta ray surface. The current controller rays already reach 30 metres; the inspected labels are approximately 4.5–7.5 metres away. Increasing controller reach would not fix the obstruction.

The source line of the new three-line node label also extended above its original 400 × 100 canvas rectangle. Aiming at that visible top line could miss the Meta surface even after removing the foreground obstruction.

The earlier tests invoked `ShowNode` or `OnPointerEnter` directly. They checked the card's content and layout but did not reproduce the nearer Meta ray-surface selection. The regression now exercises that selection and the SDK canvas event dispatch.

## Changes

- Fit the simplified learning console to its current lower controls (1000 × 400), keeping branch buttons, positions and transparent backgrounds intact.
- Fit node-label canvas surfaces to the displayed label, including the source heading.
- Give the split-label background the same hover/click behavior as its text, so the full visible label is a usable target.
- Preserve the existing interaction: hover opens Summary, clicking a label pins it, Model details expands the same card, Summary returns and close removes its ray surface.
- Preserve model evaluation, manual choices, prepared playback and the tracked head pose.

## Verification

The reproduction is recorded in `artifacts/split-pointer-2026-10-17/before-rays.json`. Compilation succeeded. All **9 focused card/menu tests passed** (14.59 seconds). The new test finds the nearest active Meta ray surface using the configured controller reach, then routes Hover/Select/Unselect through `PointableCanvasModule`. It covers both controller origins; current and next split labels; the top source line and label margins; reopening the menu; Details, Summary and close; and both branch targets while Details is open. The inspected node, manual route and head pose remain unchanged. Evidence: `focused-tests.json`.

The full PlayMode suite passed **127 tests**, with zero failures and one optional video case skipped (111.71 seconds). Evidence: `playmode-tests.json`.

## Android candidate

- Android IL2CPP Development build `build_832418cc8603` succeeded in 24.20 seconds, with zero errors and five warnings (optional Pipeline configuration, the existing performance-probe directive and generated TMP C++ compilation units).
- Installed on the connected Quest 3 with `adb install -r` (`Success`), preserving app data. Launched the main activity and confirmed the app process (PID 28010).
- APK: `BoostingExperience/Builds/Android/VRExperienceAGB-split-pointer-2026-10-17.apk` (119,590,983 bytes).
- SHA-256: `3ed5788a8f46c9cecbacd1c67be66b632c36b67366db031845e196cd5e1f4a98`.
- Build report: `artifacts/split-pointer-2026-10-17/build-status.json`.
- The process-filtered startup sample (`quest-startup.log`) has no fatal, null/missing-reference or segmentation-fault entries. It retains the existing OVR metrics number-format error and OpenXR `xrDiscoverSpacesMETA` lookup messages; this is not clean-log or physical interaction acceptance.

The user subsequently confirmed the split-card interaction works on Quest and requested the copy cleanup below. Performance acceptance remains pending.

## Split-card copy follow-up

The node-side Details card omits the SHA-256 line and abbreviates source-path segments `left` / `right` to `L` / `R` (for example, `tree[0]/root/L/R`). Exact node IDs, immutable source hashes and full audit descriptions remain available in the underlying model. Feature identifiers, category values, original conditions and segment evidence are unchanged.

Editor compilation succeeded and all nine focused card/menu PlayMode checks passed (14.20 seconds). A live Editor check covered all 13 nodes in the synthetic AGB export, including both branch directions, and confirmed that the detail body and source metadata are preserved. Synthetic category values `left` and `right` remain unabbreviated. Evidence is in `artifacts/split-card-copy-2026-10-17/`.

Android build `build_bc5aa97c8e6f` succeeded in 26.60 seconds with zero errors and six warnings (optional Pipeline configuration, the existing performance-probe directive, the TMP shader debug-symbol pragma and generated TMP compilation units). `VRExperienceAGB-split-card-copy-2026-10-17.apk` was installed on the connected Quest 3 with `adb install -r` (`Success`) and the main activity was launched. APK size: 119,593,722 bytes; SHA-256: `170aaaf3af162dff660afb28f44e8d80fb350a5c11b1e5dea449d623f94a1eae`. Physical readability of this copy revision remains to be checked.
