# Inter menus and shared source colors — October 17, 2026

The [inline split and legend follow-up](inline-split-and-legend-2026-10-17.md) supersedes the always-visible legend board described below and moves split Details into its nearby card.

## Interaction

The forest card now opens **Details** inside the same world-space panel. The panel expands without changing its position, selecting a different tree, hiding the forest or opening the main menu. Previous/Next page through the complete tree description; **Summary**, **Explore tree** and close remain available. Pointer presses are invalidated when the card changes mode, page or tree.

Each fresh opening of the main menu places it 1.6 m along the viewer's current gaze, with the configured height offset. Page changes and ordinary head motion leave the panel anchored. Closing and reopening it places it in front of the current pose again. Search and comparison panels also use the current gaze when opened. Nothing moves the tracked head.

## Typography and information density

All text created by the shared runtime UI factory uses the existing project-owned Inter assets. Main-menu buttons use fixed 27-point labels; body and detail text retain readable fixed sizes. Long identifiers and evidence remain in paged details instead of shrinking labels to fit.

- Home, profile selection, positioning, help and settings have shorter labels and summaries.
- **Details** in the main-menu footer retains the full model name, identifier, calculation capability and relevant help or performance information.
- Import instructions are paged. File names, upload address/code, validation failures and essential input states remain available.
- Predictor search shows the selected feature, scope, match counts and condition. **Details** reveals the full condition, gain and source evidence.
- Boosting trail shows iteration and current/final score; structural evidence and explanatory material are in **Details**.
- Profile comparison starts with contributions and the first divergence. **Details** retains full paths, exact values, changed inputs and ranked contribution changes. The details control advances through pages, then returns to Summary.
- Calibration and source-scoring limitations remain attached to results. No calculation or model data is removed.

## Shared source palette and legend

`PredictorPalette` is the common source for predictor labels, inspector feature/identifier text, input rows, base composition strips and the in-scene **Sources** legend.

Customer cyan, py violet, IH amber and Score pink retain the concept palette. Audience uses mint, Param coral and unqualified identifiers rose. Other source names receive a deterministic, saturated color derived from their name. The assignment is independent of model order and does not fall back to white. Customer.Scores remains in Customer; the existing secondary Score tag is a lexical marker, not a separate source or an inferred business role.

The legend is visible in the scene, positioned to the left when the view opens, and stays world-anchored while reading. It lists the source groups actually used by the model, with pagination beyond six groups. It hides with modal menus and returns afterward.

Base-strip widths still represent stored-gain shares when complete positive gain evidence is available. The presentation combines `py*` identifiers into the same group as their text labels. Without usable gain evidence, equal-width source swatches identify the groups and are labeled **Sources**; they do not claim gain shares. Trees without predictors show **No predictors**. Full numerical evidence remains in Details.

## Verification

- Unity 6000.6.3f1 compiled the changes. The complete PlayMode suite passed **125 tests**, with zero failures and one optional video case skipped (101.16 seconds).
- Seven focused card/menu checks also passed. They exercise inline pagination and text bounds, preserved manual routes and complete A/B results, fixed Inter typography across 25 menu pages, gaze-relative opening with anchored reading, and matching source colors after replacing the model.
- A final page-bound guard prevents an old detail-page index from showing a blank page after changing the boosting iteration. All **8 extension PlayMode tests** passed after that fix (3.01 seconds), including the new assertion for this transition. The legend target's runtime-only reference is explicitly excluded from serialization.
- A live synthetic nine-source model verified legend pages 1/2 and 2/2 through the actual pointer handler, including the disabled Next button on the final page. All nine trees remained loaded.
- Inspected 1600 × 900 Editor captures of the main menu, inline tree Details and legend, predictor search and profile comparison. Search received a final opaque background so scenery cannot show through the text area. These are Editor captures, not headset images.
- Local evidence is in `artifacts/inter-menus-2026-10-17/`: `playmode-tests.json`, `focused-tests.json`, `legend-pagination.json` and the four `inter-*.png` previews.

- Final Android IL2CPP Development build succeeded in 22.00 seconds, with zero errors and five warnings: optional Pipeline configuration, the existing performance-probe directive and generated TMP C++ compilation units. Report: `build-status.json`.
- Installed on the connected Quest 3 using `adb install -r` (`Success`), then launched the activity and confirmed the app process. This verifies installation and startup, not physical readability or interaction.
- The captured startup sample (`quest-startup.log`) contains no fatal exception, null/missing reference exception or segmentation fault. It still reports the OpenXR `xrDiscoverSpacesMETA` lookup error and an OVR metrics number-format error; it is not clean-log acceptance.
- APK: `BoostingExperience/Builds/Android/VRExperienceAGB-inter-menus-2026-10-17.apk` (119,582,578 bytes).
- SHA-256: `8263d03d595550f770e246a0dcb6f3d550680b72a7717d6dca6ee8b1991e7d19`.

Physical Quest reading, controller interaction and performance acceptance remain pending.
