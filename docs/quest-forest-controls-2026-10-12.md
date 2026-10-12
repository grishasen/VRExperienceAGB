# Forest scenery and manual branch regressions — October 12, 2026

## Reproduced failures

Loading a model from the open library menu cloned the large forest backdrop pines from `MoonlitEnvironment` while `MenuReadingSpace` had temporarily disabled their renderers. The newly instantiated renderers inherited `enabled=false`; closing the menu restored that false value. Reproduced with the exact 100-tree sample.

Ending A/B playback or choosing Explore the forest stopped presentation but retained the comparison/prepared-profile session. Reentering a tree therefore kept manual branch choices unavailable. In the same reproduction, a freshly imported manual tree accepted a branch, whereas entering after A/B → End playback did not.

## Changes

- Before rebuilding the garden, restore the authored renderer/canvas/collider state. The still-open menu suppresses the rebuilt scene again before rendering. Closing it restores visible large pines, including across repeated imports.
- Explore the forest and End playback return prepared-profile/comparison sessions to manual exploration. Existing manual sessions retain their routes.
- The tree menu offers **Explore branches manually** to deliberately switch the selected result/profile tree to a fresh manual route. A/B result inspection remains available until that explicit choice.

## Checks

Regression tests cover repeated imports while the menu is open, menu suppression/restoration, actual pointer-down/click events for both branch buttons after ending A/B, and switching the selected result tree into manual mode. Unity compilation passed. The complete PlayMode suite passed **108 tests**, with zero failures and one opt-in video test skipped. On the exact imported sample, all **112 backdrop renderers** were enabled after closing the menu; the previously failing A/B → End playback → tree sequence returned Manual mode with no retained comparison and accepted a branch. The restored forest screenshot was inspected. Device status follows below.

Headset checks:

1. Open the saved 100-tree sample from Models / profiles, then Explore the forest. Confirm the large snowy backdrop pines surround the model garden.
2. Open and close the menu, reload the sample, and confirm the large trees remain visible.
3. Run A/B, choose End playback · forest, enter a model tree, select TRUE, restart, and select FALSE. Both choices must advance their respective branches.
4. Calculate A/B, open a tree from the results, then A → Explore branches manually. Confirm the same tree resets to a manual route and both choices work.
5. Recheck ordinary A/B playback and the two complete reference scores. Scene visibility and manual mode changes must not change evaluation.

## Installed build

`artifacts/builds/VRExperienceAGB-forest-controls.apk`, 108,498,555 bytes; SHA-256 `be4c16f54b53055beb06edd21706ad15c86d897b492c859e91749b8df9b444a6`. Android IL2CPP Development build `build_9f302e9f9cae` succeeded with zero errors and five existing Pipeline/TMP warnings. Installed successfully with `adb install -r` and launched. The exact sample model and both synthetic profile files remain in their persistent folders.

The headset was asleep at deployment. The updated build's physical controller and visual acceptance are pending the user's independent check; Editor passes above are not headset acceptance. A request to reopen the saved sample was issued after launch.

Evidence: `artifacts/quest-forest-controls-2026-10-12/` contains the test/build reports, exact-sample reproduction after fixes, and the inspected restored-forest image.
