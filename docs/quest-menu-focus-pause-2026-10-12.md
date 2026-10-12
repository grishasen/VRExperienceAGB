# Branch input after menu/focus changes — October 12, 2026

The latest Quest screenshot (`forest-20261012-100331-466-e914ed.png`) still showed a `LINKED` badge in the node diagram. Quest input logs showed real hover/down/click events reaching both branch controls, with `enabled=False`, `paused=True`, no pending decision and no leaf/browsing state. In these recorded events, the buttons were disabled by session pause; an overlapping ray surface was not preventing delivery.

Menu pause and application suspension previously saved/restored the same boolean independently. Closing a menu during suspension could unpause too soon and then restore the menu's pause on focus recovery. Opening a menu while suspended could similarly capture the temporary suspension as the intended pause state. The director now reads/writes the intended pause separately from application suspension. Closing an already closed menu is a no-op, so a stale menu snapshot cannot pause a later session.

The regression exercises both orders of menu opening versus focus loss and both orders of focus recovery versus menu closing, then selects both branches on sample tree 9. It also preserves explicit pause and checks repeated menu closing. Existing focus tests and ray-surface tests remain in the suite.

Remaining `LINKED` copy in the story and node diagram is replaced by `Predictor:` and `Same predictor`; it indicates a highlighted predictor match, not a score or probability.

## Validation

Compilation passed. Full PlayMode regression: 113 passed, zero failures, one optional video test skipped. Android Development build `build_65a7a49ad6f1` succeeded with zero errors and six warnings. `adb install -r` returned Success and the app was relaunched. APK: `artifacts/builds/VRExperienceAGB-menu-focus-fix.apk` (128,926,500 bytes; SHA-256 `ed544d13995b343644037fa4a42eac548417bfe9f77275d628f6c74f03a919bb`). The headset was asleep when live state was requested; no live headset success is claimed. Evidence is in `artifacts/branch-regression-2026-10-12/`.

## Selected predictor marker follow-up

Above-tree labels, hover details and the forest story now use `Selected predictor:`. Matching forest trees also show a small green pointed diamond above the crown. It uses one shared eight-triangle mesh/material, no colliders, no ray surfaces and no shadow casting; the marker remains associated with its tree beyond the text visibility distance. Changing/clearing the selection updates all markers. The focused matching/clearing regression passed and an Editor render was inspected (`artifacts/selected-predictor-2026-10-12/preview.png`). Android Development build `build_d7daf82c6bab` succeeded with zero errors and six warnings. APK: `artifacts/builds/VRExperienceAGB-selected-predictor.apk`. After the user reconnected Quest device `2G97C5ZHBX02V4`, `adb install -r` returned Success and the app was relaunched. Headset visual acceptance remains pending.

## Table View marker follow-up

Table View now shares the green predictor diamond geometry and matching rules with the forest, at one tenth of the forest marker scale. Markers follow table rotation/scale and are recreated correctly for grouped views. A focused test checks all trees, both 50-tree groups and selection clearing; it passed. The test also exposed and fixed a null-selection exception in `LinkFeature(null)`. An Editor render was inspected in `artifacts/table-predictor-2026-10-12/preview.png`.

To select a predictor without entering a tree, open A → Tools → Search predictors, use Find feature or feature navigation, choose Entire model for all occurrences, then close the search panel. Selection persists in both forest and Table View. Android Development build `build_5bb890f25abb` succeeded with zero errors and five warnings; `VRExperienceAGB-table-predictor.apk` was installed successfully and the app relaunched. Headset visual acceptance remains pending.

## Main-menu search fix

`Search predictors` is now on the home menu beside `Explore the forest`; `About model` remains in Tools. Direct search/trail/review commands were previously rejected whenever the extension panel was closed, because the closed-panel guard ran before tab-opening commands. Those open commands now run before the guard.

A pointer-event regression clicked the home search button from Table View, verified the visible canvas and enabled Meta ray surface, selected a predictor, closed search, and checked retained table markers. It also reopened Search, Trail and Review from closed state. The test passed. Android Development build `build_bb9c50591dc3` succeeded with zero errors and five warnings; `VRExperienceAGB-main-menu-search.apk` was installed successfully and the app relaunched. Physical controller acceptance remains pending. The current user path is A → main menu → Search predictors → Find feature; close the search panel to retain the selected predictor.

## Search keyboard input follow-up

The latest Quest screenshot (`forest-20261012-103929-576-67c08c.png`) showed the search text-entry keyboard. Its nested Meta ray surface was only 1 mm in front of the parent panel surface. During text entry, the parent panel now releases both its Meta ray surface and its GraphicRaycaster; the keyboard is 2 cm in front of it. Finishing entry restores the parent input. Refreshing the panel no longer repositions it with the head; placement happens when opening it. These changes remove competing panel input and moving hit targets. Physical controller confirmation is still required.

The regression checks every keyboard key, Backspace and Done against both simulated controller-ray origins and the first uGUI click target, verifies a stationary panel after head movement, and types/deletes/finishes text through pointer events. The full PlayMode run passed 116 tests with one optional skip; the new test initially failed because the fixture camera is not tagged MainCamera. After correcting the test to use the rig camera, its focused rerun passed. No runtime code changed between those runs. Evidence: `artifacts/search-input-2026-10-12/`.

Android Development build `build_1b45875d94b2` succeeded with zero errors and six warnings. `VRExperienceAGB-search-keyboard-fix.apk` was installed successfully on Quest and the application relaunched. Physical keyboard/controller acceptance remains pending.

## Confirmed Meta keyboard routing defect

The user reported the previous candidate still did not accept keyboard input. The prior ray/graphic tests missed the actual SDK filter. `PointableCanvasModule.FindFirstRaycastWithinCanvas` accepts a graphic only when its `rootCanvas` equals the canvas carried by the Meta pointer. The keyboard had its own nested Canvas and PointableCanvas, so every key was rejected. A regression invoking the installed SDK filter reproduced this failure (`Meta canvas routing rejected ReviewKey-0`) before the runtime change.

The keyboard is now a RectTransform/Image group under the panel's existing root Canvas, with no separate Canvas, GraphicRaycaster or Meta surface. Its buttons share the panel input path. The previous parent-ray suppression is removed. The same regression now passes for every key, Backspace and Done, alongside both simulated ray origins, text entry/deletion/completion and stable placement after head movement. Before/after evidence: `meta-before.json` and `meta-after.json` in `artifacts/search-input-2026-10-12/`.

The focused routing regression and all eight ExplorationExtensions PlayMode tests passed. Android Development build `build_511d79d95ece` succeeded with zero errors and six warnings. `VRExperienceAGB-keyboard-root-canvas.apk` was installed successfully and relaunched on Quest. Physical controller acceptance remains pending.
