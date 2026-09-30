# One-tree branch stones — September 30, 2026

This is the first incremental fix requested after headset feedback. The user accepted this change on Quest, then requested the [forest appearance update](m4-forest-appearance-2026-09-30.md).

## Behavior

TRUE and FALSE return to separate stones beside the current condition. Their transparent pointer targets align with the stones; the normal view has no menu panel or other action buttons. At a leaf, branch stones are hidden.

A opens and closes the tree submenu (desktop Tab also works). It contains Back, Restart tree, seated/standing layout, return to forest/tree list, and Close menu. Opening the submenu hides the stones and pauses a pending branch move; closing restores the previous pause state. A small read-only hint identifies the menu shortcut. The default 50-tree demo and scoring boundary are unchanged.

## Verification

All 30 PlayMode tests pass. Coverage includes exactly two normal-view action targets, transparent targets aligned with the stones, A-menu toggling through the shared handler, exactly five submenu actions, manual branch selection, and pointer reachability in standing and seated layouts. The prior 150 EditMode passes remain the model-layer baseline; no scoring code changed.

Two Editor captures were inspected: stones and open submenu. Evidence is in ignored `artifacts/m3-branch-stones-2026-09-30/`. Temporary desktop camera adjustments were not saved.

Android build succeeded with zero errors and four warnings. APK: `artifacts/builds/VRExperienceAGB-m3-branch-stones.apk`, 124,809,993 bytes; SHA-256 `38a6e9b3045340b4af2e22240d597c8f79dccfaf5c55809a54fbae2c7ffb531c`. It was installed successfully on the connected Quest 3 and cold-launched. Its process remained running and the captured startup log contained no fatal exceptions or Unity errors. The user subsequently accepted this first fix on Quest. Performance measurements remain separate.
