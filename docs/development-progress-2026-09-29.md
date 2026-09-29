# Deep navigation and ensemble development — 2026-09-29

## Implemented in priority order

1. **Deep-tree navigation:** seven reusable node platforms show the current decision and at most two descendant generations. The complete tree remains available to the evaluator. Hidden-descendant counts, depth and TRUE/FALSE route history provide context. Back removes one accepted decision; root overview returns to the same saved decision. No camera animation is introduced. A separate deterministic synthetic example contains eight decisions per path, 511 nodes and 256 leaves, with four prepared profiles. The original example is selectable again.
2. **Scenery polish:** stone platforms use 48 segments and rocks 24, with irregular bevels and blended vertex normals. Pine cutouts use a lower alpha cutoff and alpha-to-coverage material setting to soften foliage edges where multisampling is active. Trees remain crossed foliage cards, not new volumetric tree models. Device appearance and rendering cost are unmeasured for this revision.
3. **Ensemble progression:** independent tree sessions retain accepted paths and leaf contributions across tree selection. A prepared profile is evaluated across the full model, independent of visible nodes. Visited-tree subtotals stay distinct from the complete synthetic probability. Final output includes the baseline and individual contributions. Arbitrary branch selection switches the entire ensemble to manual exploration and removes the profile prediction. Manual cross-tree consistency is explicitly unverified; no manual probability is shown.
4. **Profile editing:** controller buttons select a feature, increment/decrement numbers, cycle categories/missing values where permitted, restore the original and close the editor. Every accepted change validates and evaluates a copied profile before replacing sessions, resets all tree routes and displays the full updated prediction. Rejected edits preserve the last valid state. The original profile stays immutable. This is a simple step-based editor, not free numeric text entry.
5. **Forest overview:** shared platforms become a paged overview of up to seven model trees with selection, node count, depth and completed contribution. Previous/next select a tree; Enter tree restores its session. This is the initial functional overview, not the later rotatable/scalable miniature forest. Tree overview is a separate root-neighborhood view with collapsed descendants.
6. **Reliability preparation:** automated regressions, visual inspection and a non-development Android build prepare the next Quest check. No Quest was connected during this work; hardware acceptance and performance measurements remain open.

## Evidence and limits

- Unity 6000.6.3f1: **111 EditMode tests passed** (102 existing plus deep-navigation/ensemble tests).
- **Nine PlayMode tests passed**, including eight-level progression, both interaction modes, camera-pose independence, stale input, full three-tree playback, atomic profile reset/restore, forest return, selected-tree preservation and visible-button raycasts in manual/profile/editor modes.
- The final marker visibility and score-copy refinements followed that test run; Android compilation and subsequent screenshot inspection cover those presentation-only changes.
- Independent `scripts/verify-examples.py` checks passed.
- Screenshots and raw reports: `artifacts/navigation-2026-09-29/` (local ignored artifacts).
- Original synthetic fixtures, scoring formulas, tracked rig, package versions and the pre-existing Oculus runtime settings change were preserved.
- September 28: the previous polished build installed and launched on Quest 3; OpenXR reached FOCUSED and the user reported the scene and first interaction test worked well. That is user feedback on the previous build, not acceptance of the new controls, seated/standing coverage, recovery or performance.

Remaining work includes explicit manual-route constraint diagnosis, short-tour summaries, adjustable miniature scaling, richer volumetric scenery, real AGB source reconciliation, onboarding and final audience acceptance. None is marked complete by this iteration.

## Next Quest check

1. Install this revision, disconnect the Mac, and relaunch from the headset.
2. In Forest overview, select Tree 2 and enter it. Return and confirm progress survives switching trees.
3. Follow the New visitor profile across all three trees. Expect raw score **-4.1**, contributions **-3.8, -0.1, -0.2**, probability about **1.63%**.
4. Choose Try a change, adjust a feature, inspect the recomputed result, then Restore original. Confirm all previous routes reset and the original result returns.
5. Select Deep tree: 8 levels. Follow Example 0 through eight decisions: leaf contribution **-2**. Check that refocusing is understandable, Back restores the previous decision, and Tree overview returns without losing the route.
6. Repeat seated/standing; inspect upper control reach, text clarity, foliage edges and platform appearance. Recenter, open the system menu, and remove/re-wear the headset; verify tracking and safe resume.
7. Record actual CPU/GPU frame times, dropped frames, refresh rate and memory for the deep view and forest overview. Target remains sustained 72 FPS at 72 Hz; do not substitute Editor timing or subjective smoothness for measurements.

## Build identity

Non-development Android APK: `artifacts/builds/VRExperienceAGB-navigation-release.apk`.
Build `build_80e473944d44` succeeded with **zero errors and six warnings**: Pipeline runtime support disabled, one deprecated TMP shader pragma, three IL2CPP large-method compilation notices and a duplicate INTERNET manifest declaration. This pass did not change the Android manifest or package configuration.

Size: 79822402 bytes. SHA-256: `802d0acb2b2367993f21ec0c50009dab23e80deede9da4edd167c64506b8fe23`.
The build is for local headset testing, not a release-acceptance claim. It has not been installed or run on Quest. Full report, concise build summary and source hashes are saved alongside screenshots in `artifacts/navigation-2026-09-29/`.

## Compact interface and warm moon follow-up

The user's original moonlit concept and castle photograph guided this pass. True/False targets changed from 330 x 112 to 210 x 76 canvas units (about 57% less area), with a 12%-alpha warm background, subtle edge and cream text over smaller stone plaques. Categorical text can shrink within a bounded font-size range. The moon uses a muted peach-orange surface and a softer warm halo; the photograph was a color reference, not a copied texture.

Secondary navigation, example/profile selection, posture and editing commands now live in a collapsible Menu. Route details and profile metadata appear there instead of permanently covering the scene. Mode selection, Back, Restart, Pause and Menu remain immediately available; Step/Play are contextual to profile mode. Decisions are hidden in overview, at leaves and while the menu is open. Opening the menu pauses movement; closing it restores the prior pause state. The controller interaction canvas and scoring/session data are preserved.

Ten PlayMode tests passed, including menu pause preservation and pointer raycasts for every secondary control. The unchanged Domain/Application suite retains its prior 111-pass record. Closed/open screenshots are `artifacts/compact-ui-2026-09-29/manual.png` and `menu.png`. Smaller decision targets and text need a new physical-controller/readability check on Quest.

Compact UI APK: `artifacts/builds/VRExperienceAGB-compact-ui-release.apk`, 79822306 bytes, SHA-256 `d47505bfe7e30c9713a8ec54d799238877145feec0a3f50b808b10cf7f9a6072`. Android build succeeded with zero errors and five Pipeline/TMP/IL2CPP warnings. Not installed or tested on Quest.


## M3 completion pass for combined testing

Software work for the current M3 scope is ready for device verification. The menu includes stable read-only inspection of the current visible neighborhood: node ID, both actual branch conditions or leaf contribution, plus an inspection marker. Cycling inspection does not navigate, score or move the camera. The prepared profile's name and values are visible before Step/Play. Leaves show previous ensemble subtotal, signed contribution and resulting subtotal. Root/rejected-command feedback appears briefly without leaving secondary controls permanently visible.

Controller selection dispatch resolves the Meta canvas pointer's RayInteractor ID to its ControllerRef and sends a 40 ms, amplitude 0.25 Unity XR impulse only when the associated device reports impulse support. Desktop input stays silent. All button releases now require the same session revision captured on press. Physical haptic output and left/right parity remain unverified.

Menu editing keeps the current session paused even when edits replace all sessions; Done editing closes the menu and restores its prior pause state. An initial regression exposed the editor-exit pause mismatch, which was corrected before the final full rerun.

Final verification: **111/111 EditMode and 11/11 PlayMode tests passed**, and independent synthetic fixture verification passed. Profile-preview and inspection screenshots were captured and reviewed in `artifacts/m3-ready-2026-09-29/`. Reports are stored in the same directory. No headset results are inferred from these checks. Follow [the combined device checklist](m3-device-check-2026-09-29.md) before accepting M3.

Final combined APK: `artifacts/builds/VRExperienceAGB-m3-ready.apk`, 79824726 bytes, SHA-256 `4fe3800ac467fca60ce958a77200e13592073965010d3786968eab540456f785`. Build `build_8c953f8330e8` succeeded with zero errors and 4 Pipeline/IL2CPP warnings. The final rebuild also removed two deprecated Unity discovery API warnings; no behavior changed after the 122-test run. No headset was connected; this APK is not yet installed or device-tested.

## Geometry quality follow-up

The combined M3 scene now includes smoother 128-segment platforms, true volumetric luminous rims, smoother rock meshes, anisotropic slate filtering and layered radial foliage/trunks for the two foreground trees. Distant trees and render resolution remain unchanged. All 11 PlayMode regressions passed after regenerating the scene. See the [visual implementation record](design/moonlit-clearing.md). This adds visual improvements to the combined test; headset acceptance is still pending.

Updated combined APK: `artifacts/builds/VRExperienceAGB-geometry-polish.apk`, 80421812 bytes, SHA-256 `5b05f4cb128bdb23b8fbcb6db97c9626865c307d049ed388f5bed3bcb0ff2e6c`. Build `build_3515164c9281` succeeded with zero errors and 2 warnings. Not installed or measured on Quest.

## Updated geometry build installed on Quest — 2026-09-29

Verified the geometry-polish APK SHA-256, then update-installed it successfully on the connected Quest 3 (`2G97C5ZHBX02V4`). Android reported a successful cold launch of `com.vrexperienceagb.prototype/com.unity3d.player.UnityPlayerGameActivity`; process 8153 remained present at the follow-up check. The captured log subsequently shows XR focus loss, pause and session idle, so active viewing is not yet confirmed. No matching exception/fatal error appeared in this captured window. Log: `artifacts/geometry-polish/quest-launch.log`. Visual quality, interaction acceptance and performance remain pending user/device testing.

## Headset feedback: posture separation and trunk detail

The user reported visibly smoother geometry, but found the tree too high, foreground trunks unnaturally cylindrical and teaching parts separating when switching posture. Inspection found static flags on luminous rims and console/choice stones under the movable Presentation root. These objects must not participate in build-time static batching; all Presentation descendants now have static flags cleared, including when regenerated. Their existing common parent and the independent tracked camera are preserved.

The middle row is lowered from 2.8 to 2.2 m and leaf row from 3.9 to 2.85 m, with reduced label offsets; the root remains at 1.75 m so it clears the console. The forest overview is also lower. Initial tighter spacing was revised after screenshot inspection to avoid overlap. A new regression verifies three posture round trips for platforms, rims, labels, anchors, sample and console, checks that all teaching objects are non-static and confirms the camera stays put.

Foreground trunks now use an irregular tapered silhouette, flared base, mild bend, procedural bark grooves and 14 woody branches per tree. The bark shader supports stereo instancing and fog. It reuses procedural shading instead of adding large texture assets. This is stylized bark, not scanned photogrammetry.

Device reacceptance remains required: the earlier user feedback identified a real build-specific defect and does not qualify the corrected build. Performance remains unmeasured.

Corrected APK: `artifacts/builds/VRExperienceAGB-posture-fix.apk`, 80794447 bytes, SHA-256 `efa4e1e4ca386096ee2ffe1eaef3c0491bd7d7c9adb0519a7dfd296f2b20a521`. Build `build_7d51f939d27e` succeeded with zero errors and 5 warnings. Final PlayMode rerun: 12/12 passed.

The posture-fix APK was update-installed successfully on the connected Quest 3. Android reported a successful cold launch. User confirmation of alignment, comfort and trunk appearance is still pending; installation/startup is not M3 acceptance.

## Lantern placement follow-up

The user confirmed the posture/height correction looked good, then reported one floating lantern and the other embedded in rock. Both lanterns now use dedicated level plinths with their bottoms resting on the ground. Glow/frame/cap heights derive from the support surface and mesh bounds, and generated boulders intersecting the reserved lantern footprints are removed. The focused Editor repair leaves the teaching layout and interactions untouched. Final Editor screenshot reviewed: `artifacts/lantern-fix/scene.png`. This low-impact scenery correction did not rerun the unchanged twelve-test interaction suite; Android build and visual inspection verify this pass. Headset confirmation of the lantern placement remains pending.

Lantern APK: `artifacts/builds/VRExperienceAGB-lantern-fix.apk`, SHA-256 `5ede6b1fc600a9796e3bec5c67d3a63d09f6f0276a27ba01611302f90cf7416d`. Build `build_59e18389ae75` succeeded with zero errors and 2 warnings.

Lantern APK update installation succeeded on Quest 3 and Android reported a successful cold launch. User observation of this final placement is pending.


## End-of-day checkpoint and M4 handoff

The user approved the final lantern result and requested committing/merging this development checkpoint, with M4 as the next work session. User feedback confirms the improved appearance and the corrected posture layout; both lanterns were approved after the final update. The final installed APK is the lantern-fix build identified above.

Automated evidence remains 111 EditMode tests and 12 PlayMode tests; the final isolated lantern placement change was checked visually and compiled into Android without rerunning unrelated logic tests. The pre-existing `allowVisibilityMesh: 1` Oculus runtime setting is included to preserve the configuration used by the installed builds; it is not separately benchmarked.

This closes the requested visual iteration. It does not claim measured Quest performance, a recorded comprehension interview, both-controller parity, USB-disconnected relaunch, or every row of the full M3 device checklist. Those checks remain open and must be reconciled before formal milestone acceptance.

Next session: continue M4 from the existing EnsembleSession and profile editor. Prioritize a reconcilable contribution ledger, deliberate tree-to-tree result flow, manual-route consistency diagnostics, and equivalent short/detailed tours. Review M4 acceptance criteria against the existing implementation rather than rebuilding completed groundwork.
