# Tree description card — October 17, 2026

Follow-up: the user accepted the typography and requested explicit click-to-open choices. The [Quiet tree follow-up](quiet-tree-2026-10-17.md) supersedes the hover behavior below and implements the selected split concept. The verification below records the earlier pilot build.

## Scope

This pilot updates the forest's hover description and its new paged detail view. Other menus, node labels and Table View typography retain their existing appearance, allowing a focused headset comparison. It does not implement any of the three earlier split-card concepts.

The compact card shows the tree number, maximum depth, leaf count, and reached contribution (including A/B when available). Unvisited trees have no invented contribution. **Explore tree** preserves direct entry; **Details** opens the existing modal reading menu without selecting a tree, changing a route, or recalculating a score.

Stored gain, namespace gain shares, source index, node count, leaf bounds, root condition, evidence and complete predictor identifiers remain available in the paged details. No model trees or scoring inputs are removed. Manual route contributions remain distinct from prepared-profile contributions.

## Typography and layout

- Official Inter 4.1 static Medium and SemiBold source files, under the included SIL Open Font License.
- Project-owned TextMeshPro assets; the shared Liberation Sans assets and global TMP settings remain unchanged.
- Two static 1024 × 1024 SDF16 atlases, baked at 80 points with 8-pixel padding. The mobile distance-field material has no outline, glow or synthetic weight. Kerning is retained only for baked glyphs.
- A separate dynamic Inter fallback handles identifiers outside the baked Latin and punctuation set. It uses SDFAA and clears its dynamic data on build.
- Fixed type sizes, left-aligned content, deliberate spacing and an opaque navy background. No automatic reduction to fit long text.
- One reusable card for the forest, with its own root Canvas and Meta ray surface. It stays anchored while reading; it does not animate the tracked head.
- A short pointer-transfer grace period lets the user move from the crown to either button. Hovering the card retains it. Closing it disables its complete interaction surface.
- A press started for one tree cannot activate a newly hovered tree after the card is rebound.

The intended card size is 720 × 320 layout units at world scale 0.0016. It opens 2 m forward, 0.62 m to the right, and 0.12 m below the viewer. These are candidate dimensions, not Quest readability acceptance.

## Font provenance and reproduction

Source: [Inter 4.1 release](https://github.com/rsms/inter/releases/tag/v4.1), `extras/ttf/Inter-Medium.ttf`, `extras/ttf/Inter-SemiBold.ttf`, and `LICENSE.txt` from `Inter-4.1.zip`. No system-wide font installation is required.

Run **VRExperienceAGB → Build tree card fonts** in the Editor to create missing assets. Existing atlases are preserved; the builder relinks the fallback and removes feature records for glyphs outside each static atlas. Keep all generated `.meta` files. The builder belongs to the Editor assembly and is not included in the player.

## Verification

- Unity 6000.6.3f1 compiled the changes and generated the project-owned font assets.
- The final complete PlayMode run passed **121 tests**, with zero failures and one optional video case skipped (92.96 seconds).
- The three new card tests cover pointer transfer and world anchoring, both-controller ray surfaces, stale presses after tree changes, preserved manual routes/head pose, paged text bounds, and complete A/B contributions. Existing crown-hover tests now address the shared card and its transfer grace period.
- Inspected the compact card and paged details in the Editor at 1600 × 900. The reading area has a separate gap above pagination controls. This is an Editor preview, not a headset capture.
- Local evidence: `artifacts/tree-card-2026-10-17/full-playmode-final.json`, `tree-card-editor.png`, and `tree-details-editor.png`.

- Android IL2CPP Development build succeeded in 47.62 seconds, with zero errors. Six warnings concern disabled optional Pipeline tooling, an existing performance-probe compilation directive, a TMP shader pragma and generated TMP C++ compilation units. The build report is saved as `artifacts/tree-card-2026-10-17/build-status.json`.
- Installed with `adb install -r` on the connected Quest 3; installation returned `Success`. The activity launched and the app process was present. These checks do not establish visual or performance acceptance.
- The captured startup log has no managed or fatal exception, but repeats the previously recorded optional OpenXR `xrDiscoverSpacesMETA` lookup error. It is not a clean-log acceptance. The app subsequently paused on focus loss; headset interaction was not exercised.
- APK: `BoostingExperience/Builds/Android/VRExperienceAGB-tree-card-inter-2026-10-17.apk` (119,564,253 bytes).
- APK SHA-256: `09d8d607a47d709f818e45a278ea413fcd507930ba15cdaaec94d3d3acacaa20`.

Quest appearance, physical pointer transfer and performance acceptance remain pending.

## Headset reading check

1. Aim at a tree crown, then move the pointer onto **Details**. Confirm that the card remains available and both controls work with either controller.
2. Read a short and a deep-tree description from seated and standing positions. Compare Inter against unchanged menu text; check small numerals, punctuation and glyph edges during gentle head motion.
3. Page through the details and return to the scene. Confirm that the same tree/route remains selected and the hidden card no longer intercepts rays.
4. Inspect reached manual contributions and completed A/B results. Opening details must preserve all scores and all trees.
