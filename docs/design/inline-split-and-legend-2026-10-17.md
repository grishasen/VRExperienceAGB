# Inline split details and collapsible source legend — October 17, 2026

The subsequent [controller pointer fix](split-pointer-fix-2026-10-17.md) corrects an invisible foreground canvas that intercepted rays aimed at node labels. Earlier direct-handler card tests did not cover that path.

## Reference and interaction

Reviewed the latest two app captures pulled from the connected Quest 3, recorded at 15:23 and 15:24 UTC on October 17, and concept 2 (`artifacts/ui-concepts-2026-10-17/02-quiet-tree.png`). The old source key appeared as a tall, separate board near the tree. Local reference captures are retained under `artifacts/inline-split-2026-10-17/quest-before/`.

**Model details** now expands the inspected node's existing card, without opening the unified menu, changing its world position, moving the viewer or changing a route. The width remains 580 canvas units; height grows from 400 to 640. Inter remains fixed-size. **Summary** restores the compact source/feature/predicate view; Previous/Next expose the complete unmodified node evidence. Close disables the entire reading canvas and its ray surface. Explicit Details stays open when the pointer leaves, without requiring Pin. Hovering another node cannot replace the expanded reading. Changing a route or model retains the existing dismissal behavior.

The source key starts collapsed as **Sources ›** at the lower left of a freshly opened scene view. The arrow reveals a single, 64-unit-high strip of colored dots and source names, using the same deterministic palette as predictor labels and tree base strips. **‹** folds it back to a 180-unit-wide control, including its interaction surface. The strip expands from its anchored left edge and stays still while reading or paging. It remembers the expanded/collapsed choice across view and model changes, hides with modal menus and shows only groups used by the active model. More than four source groups use Previous/Next arrows in the same row; no model trees or source groups are removed.

## Verification

- Unity 6000.6.3f1 compiled the runtime changes. The full PlayMode run completed in 107.69 seconds: 125 passed, one failed and one optional video test skipped. All eight card/menu/legend cases passed, including complete categorical and missing predicates, same-card pagination and Summary, unchanged routes and head pose, collapsed ray bounds, nine-source pagination, matching colors and clear branch targets with the legend expanded.
- The failed older distance-culling test allowed the live desktop cursor to re-enter a crown after its scripted hover exit. The test now disables `DesktopTreePreview` while driving hover explicitly; the runtime culling code is unchanged. All **24 scenario tests** then passed (30.61 seconds), giving passing results for all **126 enabled cases** across the full run and corrected scenario rerun. One optional video test remains skipped.
- Editor preview inspection moved the legend farther down and left to keep the left branch stone unobstructed. Inspected the final 1600 × 900 Editor captures of Summary, inline Details and the collapsed legend. These use the synthetic teaching fixture and are not headset acceptance images.

Evidence is saved in `artifacts/inline-split-2026-10-17/`: `playmode-tests.json`, `scenario-tests.json`, `focused-tests.json`, `legend-tests.json`, `split-summary.png`, `split-details.png` and `legend-collapsed.png`.

## Android candidate

- Android IL2CPP Development build `build_18cadda3a1db` succeeded in 36.27 seconds, with zero errors and six warnings (optional Pipeline config, an existing development-build directive, a TMP shader directive and generated TMP C++ compilation units).
- Installed on the connected Quest 3 with `adb install -r` (`Success`), preserving app data. Launched the main activity and confirmed the running app process (PID 24323).
- APK: `BoostingExperience/Builds/Android/VRExperienceAGB-inline-split-2026-10-17.apk`, 119,589,679 bytes.
- SHA-256: `3c92288e2d48826d1f74dd677b2c036fe9e9acfba936506969f0dedddd45ce9c`.
- Build report: `artifacts/inline-split-2026-10-17/build-status.json`.
- The process-filtered startup sample (`quest-startup.log`) contains no fatal exception, null/missing reference exception or segmentation fault. It still includes the existing OVR metrics number-format error and OpenXR `xrDiscoverSpacesMETA` lookup messages; startup is not clean-log or physical headset acceptance.

Physical Quest readability, controller usability and performance acceptance remain pending.
