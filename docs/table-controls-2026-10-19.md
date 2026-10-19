# Table controls and duplicate tooltip removal — October 19, 2026

## Requested behavior

In Table View, the left thumbstick rotates the miniature model horizontally. Forward moves the table closer; backward moves it farther away. The table moves horizontally at its existing height, retaining its size. This input changes the content, not the tracking origin, headset pose, model inputs, or evaluation. Forest walking keeps its existing left-stick control.

Both axes use a 0.2 dead zone and proportional motion. Maximum rates are 45 degrees per second and 0.65 metres per second. Table-center distance is bounded to 1.25–3.2 metres; B/recenter returns placement to 1.6 metres. The walking comfort toggle does not disable table manipulation. Menu/help panels, application suspension, and invalid inputs block manipulation. Moving the table invalidates pending pointer clicks, keeps number targets facing the observer, and carries the complete-result panel with it.

The latest supplied Quest screenshot (`forest-20261019-102314-363-4f5a94.png`) shows a generic black ButtonHint containing “10” over the enlarged tree. Table selection targets now suppress that duplicate hint. The numbered targets, full passport, enlarged tree, and ordinary menu-button hints remain available.

## Verification

Compilation passed without errors. The full PlayMode run reported 142 passes, one failure, and two intentionally skipped opt-in video captures. The failure was an outdated import/menu assertion requiring all scenery LOD renderers to be enabled, contrary to the existing full-detail policy. The test now checks that the highest-detail trees return and lower-detail copies stay hidden. All 16 tests in that suite passed on the focused rerun. Across the full run and correction, all 143 runnable cases have passing results.

New checks cover both stick axes, dead zones, distance limits, recentering, menu/help/focus blocking, invalid input, stale pointer releases, unchanged headset/origin transforms, complete A/B scores, and the result panel following the table. The existing hover test also verifies that the duplicate number hint stays hidden while the enlarged tree remains visible.

Editor renders verify tree 10 on a rotated, closer table without the black number hint, and readable English instructions on the Table Display page. These are Editor previews, not headset acceptance evidence.

The non-Development ARM64 IL2CPP APK built successfully with zero errors and six warnings (Pipeline disabled in Player, a deprecated development-build directive, and TextMesh Pro shader/compiler notices). APK v2 signature verification passed. The update was installed without clearing application data and launched on the connected Quest 3. Its installed SHA-256 matches the local APK. Hardware stick behavior and comfort require user confirmation; no new Quest performance result is claimed.

The startup snapshot contains the previously documented optional Google Play `AssetPackManager` class lookup failure. The application process remains running; no fatal crash or C# exception was found in that snapshot.

- APK: `BoostingExperience/Builds/Android/VRExperienceAGB-Quest3-table-controls-2026-10-19.apk` (92,555,230 bytes).
- SHA-256: `fcef86987f52da4cfead4db981db41bed217140d3fb1e20adc1361a9802a42c8`.
- Distribution tag: `quest3-table-controls-2026-10-19` on [GitHub Releases](https://github.com/grishasen/VRExperienceAGB/releases/tag/quest3-table-controls-2026-10-19). This is a sideload prerelease; headset comfort and sustained performance acceptance remain pending.

Evidence directory: ignored `artifacts/table-controls-2026-10-19/`.
