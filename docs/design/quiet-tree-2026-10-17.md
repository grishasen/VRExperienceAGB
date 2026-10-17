# Quiet tree implementation — October 17, 2026

Follow-up: [Inter menus and shared source colors](inter-menus-2026-10-17.md) keeps tree Details inside the nearby card, places menus at the viewer's current gaze when opened, extends Inter to the remaining menus, and replaces the separate base-strip palette with a shared source legend. The verification below records the preceding Quiet tree build.

## Approved direction

The user selected concept 2, **Quiet tree**, from `artifacts/ui-concepts-2026-10-17/02-quiet-tree.png`, with a smaller split inspector and transparent backgrounds on the left/right choice stones. The environment and model remain the existing Unity experience. The reference uses synthetic data; its illustrative typography is replaced by the project's actual Inter assets.

## Forest selection

Hovering a crown or planter name highlights the target without opening or changing a card. The first trigger/click opens **Explore tree / Details** and preserves the selected session, route, score and viewer pose. A subsequent **Explore tree** click enters that tree. The card stays open when the pointer leaves; **×** dismisses it. Clicking another tree replaces the choice card; an earlier held press cannot activate the replacement. Tree-list and tabletop navigation keep their existing actions.

This supersedes the hover-to-open behavior of the [initial Inter pilot](tree-description-card.md).

## Split presentation

- Short Inter labels above nodes use source colors: Customer cyan (`#67D5F5`), `py*` violet (`#BAADFF`), IH amber (`#ECC383`), other sources neutral. Namespace text in the inspector also identifies the source. A separate pink Score tag is a lexical identifier marker, not a score calculation or business-role inference.
- The compact inspector is 580 × 400 layout units at scale 0.0016, versus the prior full panel's 1000 × 600. It opens to the viewer's right and remains anchored while reading. It supports pin/unpin.
- It shows the feature, exact identifier (ellipsis only in the compact display), predicate and exploration mode. **Model details** opens a paged Inter view containing the full identifier, complete categorical set, original export condition, source evidence, gain, samples and segment description.
- Root/nearby labels replace generic “Next question” prose with the actual predictor. Redundant edge cards are hidden in the main experience. Subtree controls remain available with shorter labels, and every model node remains in evaluation.
- The branch buttons retain their hit targets on the stones with fully transparent UI backgrounds. Numeric operators remain strict `<` / complementary `≥`; missing/present and exact categorical membership are unchanged. Long category sets are explicitly summarized with a count and remain complete in details.

The legacy teaching harness retains its full scrollable evidence panel. Manual exploration, prepared-profile evaluation and A/B playback use the same model/session logic as before. Playback node labels use the same source palette; its existing explanatory transport remains available.

## Verification

- Unity 6000.6.3f1 compiled the implementation. The complete PlayMode suite passed **123 tests**, with zero failures and one optional video case skipped (99.48 seconds). After the final layout adjustments, all **5 focused card tests** passed again (8.1 seconds).
- The focused tests cover first-click forest choices, preserved manual routes and A/B results, stale presses, both-controller ray surfaces, transparent branch hit targets, numeric/categorical/missing conditions, source and Score colors, pinning, and complete paged model details.
- Inspected the final runtime layout in the Editor at 1600 × 900. The preview uses synthetic Customer/py/IH identifiers; it does not replace the bundled models. The inspector is offset to avoid the adjacent node label. This is an Editor capture, not a headset screenshot.
- Android IL2CPP Development build succeeded in 39.16 seconds, with zero errors. Six warnings concern optional Pipeline configuration, the existing performance-probe compilation directive, a TMP shader pragma, and generated TMP C++ compilation units.
- Installed with `adb install -r` on the connected Quest 3; installation returned `Success`. The app activity launched and its process was present. The startup sample contains no fatal crash or managed UI exception. It includes OpenXR spatial-function lookup errors, an Android Play asset-pack class lookup exception and an OVR metrics parse error; it is not clean-log acceptance.
- Local evidence: `artifacts/quiet-tree-2026-10-17/playmode-final.json`, `focused-final.json`, `quiet-tree-editor.png`, `build-status.json`, and `quest-startup.log`.
- APK: `BoostingExperience/Builds/Android/VRExperienceAGB-quiet-tree-2026-10-17.apk`.
- APK SHA-256: `0415646e26449eadf257bc35f1752525619f7549af9c012646bd7a0d6552b055`.

Fresh headset readability, physical controller interaction and performance acceptance are pending.

## Quest check

1. Point at several trees: no card should appear. Click a tree: choices open. Point elsewhere: those choices stay on the clicked tree. Choose **Explore tree**.
2. Read both conditions directly on the stones. Confirm there is no dark UI patch behind either condition, including hover/press states.
3. Point at a node label, pin the smaller inspector, then open **Model details**. Page through it and return; route and score must remain unchanged.
4. Try both controllers, numeric/categorical/missing conditions, manual backtracking and full A/B playback. Judge text size and contrast seated and standing.
