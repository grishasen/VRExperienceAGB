# Hidden node-details surface blocks branch selection — October 12, 2026

The user reported that the right branch showed a hover hint but the left branch could neither be hovered nor selected. Quest logs contained hover events for FalseBranch only. Both branch controls existed and were interactable.

## Reproduction and cause

`FullNameTooltip.Hide()` hid the child graphics but left `NodeReadingPanel`, including its Meta `RayInteractable` surface, active. After opening details while looking left from a lower head position, then closing them, rays from both controller positions toward TRUE hit the invisible panel before the learning console. Editor reproduction measured the hidden panel at approximately 1.95/2.10 m versus the intended console at 3.13/3.27 m. Rays toward FALSE reached the console unobstructed.

Earlier tests called pointer handlers directly and therefore did not exercise Meta surface selection. This reproduction establishes a concrete input obstruction matching the reported asymmetry; physical controller acceptance of the fix is separate.

## Changes

- Hide and reopen the entire node-details canvas together with its graphics, including the Meta ray surface. A newly created hidden details panel starts inactive.
- Remove branch-button hover tooltips. Keep the existing branch labels and selection behavior.
- Remove the redundant “Trigger to select” footer from other button hints while preserving their descriptive text.
- Add regression coverage that opens/closes details on either side, checks the nearest Meta surface from both controller positions, verifies the uGUI hit target, and dispatches pointer events through that hit to confirm both branches advance.

## Verification

Unity compilation passed. The full PlayMode suite passed **109 tests**, with zero failures and one opt-in video test skipped (84.3 seconds). This includes ray selection and branch execution on both sides, pinned evidence, ordinary tooltips, and full A/B playback. Android IL2CPP Development build `build_b39eeb60ff47` succeeded with zero errors and six Pipeline/TMP/development-symbol warnings. Installed with `adb install -r` (Success), then explicitly restarted. Physical controller acceptance remains pending.

APK: `artifacts/builds/VRExperienceAGB-branch-rays.apk`, 128918775 bytes, SHA-256 `86ff8cdd1f372e24bd385087ef65208fe171f1634cff3f245b1c36641e526039`. Evidence: `artifacts/quest-branch-rays-2026-10-12/`. The complete 100-tree startup model and both synthetic profiles are retained.

Headset acceptance: open a manual tree, hover a node to show its details, let the panel close, then select the left branch. Restart and select the right branch. Repeat with both controllers and after turning the head. Neither branch should show a generic hover tooltip. Pinned node details must still be readable and scrollable.
