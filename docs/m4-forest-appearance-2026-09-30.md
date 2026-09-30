# Forest appearance — September 30, 2026

Following user acceptance of the restored branch stones, this iteration addresses the forest overview.

Tree numbers are small, fixed labels on the front of the planters, with no large opaque name buttons. Pointing at a pine or its name reveals a compact details panel beside the crown; it disappears on pointer exit. Clicking still enters the selected tree. Names no longer rotate as floating billboards. Planters are 0.36 m tall to provide a readable front face.

Pine height uses structural depth: 0.65 m plus 0.45 m per level through depth 4, then 0.075 m per level through depth 14 (maximum 3.20 m). Crown radius uses leaf count, ranging from 0.32 m for one leaf to 0.90 m at 16 leaves. Trunk width follows crown radius. These are fixed visual scales, not scores or probabilities. Identical structural metrics intentionally produce identical sizes.

The unchanged 50-tree demo has five depth-2/four-leaf trees, forty depth-3/eight-leaf trees, and five depth-4/sixteen-leaf trees. Their heights are 1.55, 2.00 and 2.45 m. Garden paths, complete model, manual routes, return locations and the accepted A-button one-tree menu are preserved.

## Verification

Evidence is stored under ignored `artifacts/m4-forest-appearance-2026-09-30/`. 151 EditMode and 31 PlayMode tests pass. The new coverage checks shallow-tree size separation, rendered crown width, visible height differences in the exact demo, compact transparent name targets at planter height, and hover show/hide behavior. Existing path travel, tree entry, saved return pose, manual routes and A-menu tests also pass. Two Editor captures were visually inspected: the overview and a close-range hover card. Temporary preview camera poses were not saved. User visual acceptance and device performance measurements remain pending.


Android build succeeded with zero errors and four warnings. APK: `artifacts/builds/VRExperienceAGB-m4-forest-appearance.apk`, 124,815,201 bytes; SHA-256 `3aeca9d2067d16a76d6ed0e4c37d54c38683c3fe3a62067a03be2744fd3a2b4a`. Installation on the connected Quest 3 succeeded, followed by a successful cold launch.
