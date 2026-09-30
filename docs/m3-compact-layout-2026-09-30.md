# Compact one-tree view — September 30, 2026

The user reported that the one-tree view floated too high and that large text/buttons overlapped the objects. This iteration applies to the simplified M3/M4 demo flow.

The [branch-stone follow-up](m3-branch-stones-2026-09-30.md) supersedes the normal-view controls described below: TRUE/FALSE are stones again, and the remaining buttons open with A.

## Changes

- Tree platforms use lower, flatter standing heights: 1.30 m, 1.50 m and 1.85 m for the three displayed generations. Node positions still belong only to presentation.
- The console stone sits near the floor instead of floating. Seated layout lowers the presentation by 0.40 m and reduces the console stone's vertical size to keep its base above the floor. Tracking transforms are not moved.
- The normal tree view has TRUE, FALSE, Back and Menu. Menu opens a compact submenu with Restart tree, Use seated/standing layout, Back to forest/list and Close menu. A / desktop Tab also toggles it.
- Opening the submenu pauses a pending branch transition; closing restores its previous pause state. Restart affects the current tree. Returning preserves the existing forest/list destination and accepted route.
- Node labels are narrower with smaller type and a smaller world-space scale. Their height above the platforms separates words from the objects. The current condition moves onto the console; duplicated branch text and floating choice stones are removed from this view.
- Controls use smaller typography on solid backgrounds. Full-name hover remains available in a smaller scrollable panel.

The default 50-tree nested demo, scoring boundary, existing prepared-profile fixtures, forest navigation and complete model remain unchanged.

## Verification

Interaction, visual and Android build evidence is stored under ignored `artifacts/m3-compact-layout-2026-09-30/`. 29 PlayMode tests pass. New tests verify submenu pause/resume during a pending branch move, posture changes without tracked-head changes, the console base staying above the floor, and raycast reachability of every visible normal/submenu button in both layouts. The existing 150 EditMode passes remain the model-layer baseline; this iteration changes presentation only.

Three Editor captures were visually inspected: standing tree, standing submenu and seated submenu. The seated screenshot uses a desktop-only 0.40 m eye-height reduction; neither this preview camera adjustment nor its look angle was saved. Headset tracking remains independent. Earlier headset feedback does not establish acceptance of this layout.


Android build succeeded with zero errors and four warnings. APK: `artifacts/builds/VRExperienceAGB-m3-compact-layout.apk`, 124,817,346 bytes; SHA-256 `a238b1553bcb77ea69223197982b41e5fb6b6f92659a61dc9e685b66b3a2d277`. It was installed successfully on the connected Quest 3 and cold-launched. The process remained running and its captured launch log contained no fatal exceptions or Unity errors. Installation/startup is verified; user acceptance of the new height, text size and posture layouts remains pending.
