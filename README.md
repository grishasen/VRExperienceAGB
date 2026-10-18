# VRExperienceAGB

**Natural Table View fir, concept A — October 18:** after rejecting the plain cone appearance, the user selected the dense natural fir. Miniatures now use irregular drooping boughs, opaque needle-covered volumes and textured outer sprigs. All 100 trees retain their metric dimensions and interactions. Close, complete-table and minimum-scale rotated Editor views are verified; all 26 scenario tests pass. The non-Development APK is installed and launched on Quest 3, with its installed checksum verified. Headset visual and performance acceptance remain pending. See [implementation, artwork and verification](docs/design/table-natural-fir-2026-10-18.md).

**Readable Table View trees — October 18:** replaced disappearing miniature needle textures with solid, faceted three-tier firs. Height/depth and width/leaf-count mappings, all trees, A/B rings and pointer behavior are preserved. Matching Editor views and minimum-scale rotation are verified; 35 relevant PlayMode cases have passing results. The non-Development distribution APK is installed and launched on Quest 3, with its installed checksum verified. Renewed headset readability remains pending. See [changes and verification](docs/design/table-visibility-2026-10-18.md).

**Orion and Sirius brightness — October 18:** restored three faint sword components and replaced the nearly uniform star-brightness mapping with a compressed logarithmic magnitude curve. Sirius and Orion's principal stars now stand out more clearly while retaining crisp points and visible independent twinkle. All five focused sky tests pass, including rendered brightness and sword-position checks. The non-Development APK is installed and launched on Quest 3; renewed headset appearance remains pending. See [measurements and verification](docs/winter-sky-2026-10-05.md#orion-and-sirius-brightness-correction--october-18-2026).

**Clear sky and distribution build — October 18:** the user confirmed the crisper stars look much better on Quest and requested complete removal of the Milky Way. The baked galactic layer is removed; the approved star shape and twinkle are retained. All five focused sky tests pass, and the clear-sky Editor capture is verified. The 91.7 MB non-Development APK for direct installation is built, installed and launched on Quest 3; its installed checksum matches the distribution file. See [changes and verification](docs/winter-sky-2026-10-05.md#clear-sky-and-distribution-build--october-18-2026).

**Crisper sky after Quest feedback — October 18:** narrowed the Milky Way about threefold, reduced star halos and made independent twinkle faster and stronger. Ambient stars now keep animating during model pause and menus; application suspension still freezes them. All five focused sky tests pass, including a rendered-brightness check. The Android update is installed and launched on Quest 3; renewed headset appearance and performance acceptance remain pending. See [changes and evidence](docs/winter-sky-2026-10-05.md#headset-feedback-narrower-band-and-crisp-visible-twinkle--october-18-2026).

**Compact playback status — October 17:** removed the opaque lower playback board and reduced its text to two Inter lines; full conditions and observed values are in playback-menu Details. All 25 scenario regression tests pass, and one-profile/A-B Editor previews are verified. The update is installed and launched on Quest 3; physical readability remains pending. See [implementation and evidence](docs/design/inter-menus-2026-10-17.md#compact-transparent-playback-status--october-17).

**Detailed winter sky — October 17:** a baked Milky Way haze, finer star cores and gentle independent scintillation enrich the existing catalog sky. All four focused sky PlayMode tests pass, including pause/focus recovery and unchanged catalog geometry; Editor shader checks report no messages. The Android update was built, installed and launched on Quest 3 on October 18; visual and performance acceptance remain pending. See [implementation and evidence](docs/winter-sky-2026-10-05.md#detailed-sky-and-gentle-scintillation--october-17-2026).

**Automatic tree-entry centering — October 17:** entering a tree centers the whole clearing like B while preserving the forest return position and rotation. All 135 enabled PlayMode tests pass (one optional video case skipped). The Android update is installed and launched on Quest 3; physical acceptance remains pending. See [behavior and evidence](docs/quest-feedback-2026-10-08.md#automatic-tree-entry-recenter--october-17).

**Fuller firs and forest understory — October 17:** model crowns now have 90 nearby branches instead of 64; small grass/slate groups echo the single-tree clearing. The 100-tree garden has 334 interior grass tufts and 56 additional rocks in eight shared batches. All 134 enabled PlayMode tests pass (one optional video case skipped); the update is installed on Quest 3. The one-minute pair recorded 71.85 → 71.52 FPS and unchanged p95, but worse p99 (14.41 → 16.98 ms). Different head directions and temperatures prevent a controlled attribution. See [implementation and evidence](docs/design/near-scenery-quality-plan-2026-10-17.md#fuller-model-firs-and-forest-understory--october-17).

**Floor markers removed — October 17:** removed all GO HERE / GO TO GROUP squares and their ray targets, and updated navigation help. All 13 navigation cases have passing results after a test-setup correction and focused rerun; all nine M5 forest tests pass. The 100-tree Editor view is verified and the update is installed on Quest 3. See [verification](docs/design/near-scenery-quality-plan-2026-10-17.md#floor-navigation-markers-removed--october-17).

**Forest paths removed — October 17:** removed central and cross-row paving, exposing the forest ground. All 13 focused navigation PlayMode tests pass; the 100-tree Editor view is verified and the update is installed on Quest 3. See [verification](docs/design/near-scenery-quality-plan-2026-10-17.md#forest-paths-removed--october-17).

**Quest scenery feedback — October 17:** corrected stretched side-panorama trees and matching lake reflections, removed empty crown-hover hints, extended full branch detail about fourfold and made the far crown denser. All 134 enabled PlayMode tests pass (one optional video case skipped); the update is installed on Quest 3. A fresh 60-second 100-tree capture recorded 71.37 FPS, p95 14.92 ms, p99 17.11 ms and a 27.78 ms maximum, with no interruptions. Position changed during capture, so this is operational evidence rather than a controlled same-view comparison. See [fixes and verification](docs/design/near-scenery-quality-plan-2026-10-17.md#quest-visual-feedback-follow-up--october-17).

**Forest detail completion — October 17:** refined mountain artwork, textured bark, sparse grass, shared decorative-tree LODs and textured model crowns are installed on Quest 3. All 132 enabled PlayMode tests pass (one optional video case skipped). A fresh 60-second pair recorded 70.33 → 71.72 FPS and p95 16.51 → 13.89 ms; head-direction differences make this preliminary, not a controlled speedup claim. A three-minute forest/tree exploration recorded 70.88 FPS and p95 14.87 ms, with a 208 ms peak whose cause is unresolved. GPU/compositor counters remain unavailable. See the [implementation, evidence and limitations](docs/design/near-scenery-quality-plan-2026-10-17.md).

**Near scenery and restored legend — October 17:** the source legend starts as a slim lower-left row and collapses with its arrow. Ground and slate artwork are refreshed; ground uses one texture projection, and three nearby decorative firs use shared branch textures and three detail levels. All 130 enabled PlayMode tests pass (one optional video case skipped). The Android candidate is installed on Quest 3. Initial 60-second forest captures show 67.40 → 69.23 FPS and p95 18.86 → 17.26 ms, with lower memory but a worse single-frame maximum. GPU timing, exact head-angle matching and sustained acceptance remain unverified. See the [plan, comparison and limitations](docs/design/near-scenery-quality-plan-2026-10-17.md).

**Tree-card cleanup and wolf delay — October 17:** tree Details now omits SHA-256, matching the split card. Automatic howling waits one minute after startup and each tree entry; menus, mute and focus interruptions preserve that countdown. All 128 enabled PlayMode tests pass (one optional video case skipped). The Android update is installed and launched on Quest 3; physical listening and performance acceptance remain pending. See [behavior and verification](docs/quest-central-wolf-2026-10-12.md#one-minute-arrival-delay--october-17).

**Split pointer fix — October 17:** the old learning-console ray surface was blocking node labels. The simplified console now fits its lower controls, and the full split label accepts hover/click, including the colored source heading and backing. The regression exercises actual Meta canvas routing through Details, Summary and close. All 127 enabled PlayMode tests pass (one optional video case skipped). The Android fix is installed and launched on Quest 3; the user confirmed the interaction works. The subsequent card copy cleanup hides SHA-256 and abbreviates path segments to L/R, with all nine focused card checks passing. Performance acceptance remains pending. See [reproduction and verification](docs/design/split-pointer-fix-2026-10-17.md).

**Inline split Details and collapsible legend — October 17:** split **Model details** now expands the same node-side card, with paged evidence and **Summary** to return. The source legend starts collapsed and opens into a slim lower-left row of matching colored markers. All 126 enabled PlayMode cases have passing results across the full run and corrected scenario rerun (one optional video case skipped). The Android update is built, installed and launched on Quest 3; physical readability and controller acceptance remain pending. See [implementation and verification](docs/design/inline-split-and-legend-2026-10-17.md).

**Inter menus and shared colors — October 17:** tree **Details** expands inside the nearby card. Menus reopen in front of the current gaze, use Inter and shorter summaries, with full evidence behind **Details**. Predictor labels, base strips and the visible Sources legend share one stable palette, including generated colors for other sources. All 125 enabled PlayMode tests pass (one optional video case skipped); eight extension checks pass after the final pagination guard. The Android update is installed and launched on Quest 3; physical readability and performance acceptance remain pending. See [implementation and verification](docs/design/inter-menus-2026-10-17.md).

**Quiet tree follow-up — October 17:** forest descriptions now open on the first click, followed by **Explore tree / Details**. The selected second concept adds smaller Inter split cards, source-colored predictor labels, transparent branch-button backgrounds and paged **Model details**. All 123 enabled PlayMode tests pass (one optional video case skipped), followed by five passing focused checks after the final layout adjustment. The Android candidate is installed and launched on Quest 3; fresh headset readability and performance acceptance remain pending. See [implementation and verification](docs/design/quiet-tree-2026-10-17.md).

**Tree description Inter pilot — October 17:** the forest hover card now uses Inter Medium/SemiBold with fixed sizes, an opaque background and a shorter summary. Technical structure, stored gain, root evidence and predictor identifiers move into paged **Details**. All 121 enabled PlayMode tests pass (one optional video case skipped); the Android candidate is built, installed and launched on Quest 3. Headset readability and performance acceptance remain pending. See [implementation and verification](docs/design/tree-description-card.md).

**Bundled Web model — October 13:** the local APK includes the supplied ten-tree Web export and two selected CDH profiles in Models / profiles. The pair differs in eight tree routes; complete A/B playback and independent leaf-sum agreement passed in the Editor. Private bundle resources stay ignored. See [selection and verification](docs/bundled-web-model-2026-10-13.md).

**Carezza tree refinement — October 12:** grounded shoreline spruce cards and moved water-overlapping trees onto the bank. Six larger interior firs now have open crowns, visible trunks and woody branches based on the one-tree clearing. See [implementation and verification](docs/design/moonlit-carezza.md#ground-contact-and-open-interior-crowns-follow-up).

**Carezza side refinement — October 12:** side ridges are smaller and softened with local atmospheric haze; six original decorative firs return between the model rows. The rear mountains retain their approved appearance. All 118 enabled PlayMode tests pass in a full run (one optional video case skipped). See [follow-up verification](docs/design/moonlit-carezza.md#side-atmosphere-and-interior-firs-follow-up).

**Carezza environment — October 12:** the selected second concept adds a lake, textured spruce groups and cream/rose Dolomite cliffs around the forest sides and rear. The lake and four-direction Editor views are verified; all 118 enabled PlayMode cases pass across the full run and focused rerun. The Android candidate is built and installed; visual and performance acceptance on Quest remain pending. See [implementation and current verification status](docs/design/moonlit-carezza.md).

**Predictor search and keyboard — October 12:** Search predictors is on the main menu; matching trees show green markers in forest and Table View. The keyboard now shares its panel's root Canvas, fixing Meta SDK rejection of nested-canvas pointer hits. The SDK-routing regression and eight extension tests pass. The latest Android APK was built and installed; physical controller acceptance remains pending. See [verification](docs/quest-menu-focus-pause-2026-10-12.md).

**Menu/focus pause fix — October 12:** branch input logs showed clicks reaching disabled controls while the session remained paused. Menu pause now composes with application suspension without restoring stale pause state. **113 PlayMode tests pass** (one optional video test skipped). Remaining `LINKED` labels use explicit predictor wording. See [evidence and verification](docs/quest-menu-focus-pause-2026-10-12.md).

**Rear clearing revision — October 12:** rear shrubs now reuse the one-tree scene's rocks and fern sprouts. Seated layout is the startup default. The unsupported diamond marker is removed and `LINK` is renamed to `Predictor:`. See [revision and validation](docs/forest-revision-2026-10-12.md).

**Forest optimization — October 12:** distance-aware details reduce active entrance canvases from 141 to 37 and enabled ray surfaces from 140 to 34 while retaining all 100 trees. Added side/rear shrubs and mountains, larger contrast-backed tree names and closer hover cards. All 112 enabled PlayMode cases passed across the full run and focused reruns (one optional video test skipped). Quest FPS and text acceptance remain pending. See [verification and headset checks](docs/forest-optimization-2026-10-12.md).

**Central wolf — October 12:** one wolf now appears on a hill at the end of the main aisle. The first automatic howl and the manual howl command use this central wolf. **111 PlayMode tests pass**; the 100-tree entrance view was inspected. See [verification](docs/quest-central-wolf-2026-10-12.md).

**Menu ray fix — October 12:** modal menus now disable hidden scene ray interactables and restore their prior state on close. **110 PlayMode tests pass**, including the Dictionary footer with an overlapping hidden console. See [menu input evidence](docs/quest-menu-rays-2026-10-12.md).

**Branch ray fix — October 12:** closing node details now disables their Meta ray surface, preventing an invisible panel from blocking branch buttons. Branch hover hints are removed. **109 PlayMode tests pass**, including surface and UI ray selection on both sides. See [reproduction and validation](docs/quest-branch-rays-2026-10-12.md).

**Default model — October 12:** `scripts/prepare-local-sample.py` now bundles the complete `export_Mobile_Click_Through_Rate_AGB_sample.json`: **100 trees, 200 predictors and two synthetic A/B profiles**. This supersedes the earlier 50-tree startup demo. The original sample and generated resources remain ignored; preparation requires the local sample file and never falls back to a smaller model. See [default-model verification](docs/default-sample100-2026-10-12.md).

**Forest/control fixes — October 12:** restored large backdrop pines after imports from the menu and manual branch selection after ending A/B. Added an explicit manual-entry action for result trees. **108 PlayMode tests pass**, with one opt-in video test skipped. See [regression evidence](docs/quest-forest-controls-2026-10-12.md).

**Quest sample library — October 12:** default stick walking restored, active model filename/counts shown on the main menu, and an immediate wolf-audio check added to Comfort. The exact 100-tree sample and two matching synthetic profile files are saved on Quest. **105 PlayMode tests pass** (one opt-in video test skipped); complete scores and all A/B reached leaves agree with an independent reference. See [device evidence and full-feature walkthrough](docs/quest-sample-library-2026-10-12.md).

**50-tree score demo — October 11:** the complete synthetic 50-tree demo and three synthetic profiles are selected by `python3 scripts/prepare-local-sample.py`. Results, A/B comparisons and the boosting trail show score, with a notice that propensity comes from calibration. No calibration table is loaded. **207 EditMode and 104 PlayMode tests pass** (one opt-in video test skipped). See [build and verification status](docs/sample50-score-2026-10-11.md). Earlier dated entries below describe historical behavior.

**AGB/CDH import — October 11:** ordinary AGB export and CDH JSON/JSONL profile import, complete tree calculation and A/B playback are implemented. **207 EditMode and 104 PlayMode tests pass** (one opt-in video test skipped). The supplied Mobile files produce 27 usable execution profiles. A subsequent check imported 210 Web executions and exercised both branches in Unity; the 44 GradientBoost executions do not match recorded CDH propensity despite matching modelVersion. Source-scorer agreement remains unresolved; fresh headset acceptance is pending. See [the import validation record](docs/agb-cdh-import-2026-10-11.md).

**Review follow-up — October 11:** defects 1–6 and the agreed UX/model-explanation follow-up are implemented. **193 EditMode and 101 PlayMode tests pass** (one opt-in video test skipped). Namespace groups derive from actual identifiers (`Party.*` → Party; `Customer.Scores.*` → Customer); the sample is unchanged. See the [implementation and verification record](docs/review-follow-up-2026-10-11.md) and [current Quest acceptance checklist](docs/quest-acceptance-2026-10-11.md). Fresh device acceptance and production scorer reconciliation remain open. Earlier dated entries below describe historical builds.

**JSON library — October 8:** import models and A/B profile files into separate persistent folders from **Models / profiles**. Quest receives files through a temporary local browser upload session; a system picker is also supported when available. See [formats, upload instructions and device evidence](docs/json-library.md). The Android candidate is installed; imported synthetic A/B scores match on Quest. Direct Wi-Fi access from the development host remains unverified; the device HTTP endpoint was tested through USB forwarding.

**October 8 Quest feedback:** removed per-frame model validation in single-tree viewing; anchored menu and gaze-aligned tree entry; snowy decorative pines; variable-size tabletop trees with hover passports; fast profile/A/B playback on the actual stones, pause and **Calculate whole model**. **180 EditMode and 79 PlayMode tests pass.** See [changes and performance evidence](docs/quest-feedback-2026-10-08.md). A short Quest 3 single-tree sample measured **68.65 FPS average** (median 13.889 ms, p95 15.511 ms); sustained 72 FPS and broader device acceptance remain pending.

**October 7 scenario revision:** one front-facing menu and scene placement, complete 100-tree local sample playback, simultaneous A/B, final sigmoid results in forest/Table View, quieter star field with optional constellation guides, and wolves synchronized with the recorded howl. Run `python3 scripts/prepare-local-sample.py` before building the local sample. Generated model resources are ignored; clean checkouts retain the previous demo. See [current scenarios](docs/experience-scenarios.md) and [repeatable video instructions](docs/testing.md#repeatable-scenario-recording--october-7-2026). Final validation evidence appears in that testing record; Quest acceptance and source-scorer agreement remain pending. Earlier records below describe historical builds.


**October 4 update:** [Predictor search, boosting trail and recorded guided review](docs/exploration-extensions-2026-10-04.md) are implemented, with name filtering, exact model/path queries, intermediate profile results, progressive tree reveal and locally saved tours. **172 EditMode and 67 PlayMode tests pass.** Open **Extensions** in the case/forest tools or tree menu. Quest acceptance and real AGB scoring reconciliation remain pending.

**Horizon wildlife — October 3:** [Three distant wolves and spatialized howling](docs/horizon-wolves-2026-10-03.md) are implemented. The existing sound toggle controls the howl. The October 7 revision replaces the initial synthesis with a public-domain U.S. Fish and Wildlife Service recording; all five focused wildlife checks pass. **59 PlayMode tests pass**; Quest appearance, loudness and performance acceptance remain deferred.

**October 3 update:** [Two-profile comparison](docs/profile-comparison-2026-10-03.md) adds independent A/B routes, full-model deltas, first-divergence inspection and an editable B copy using synthetic fixtures. **166 EditMode and 54 PlayMode tests pass** for this extension; the comparison panel was visually inspected in the Editor. Controller tuning and performance acceptance are deferred to final verification at the user's request. Real AGB reference data and scorer reconciliation remain pending.

M6 now includes all twelve approved [in-world explanation features](docs/m6-in-world-explanations-2026-09-30.md): node/branch explanations, segment portraits, evidence cards, crown passports, composition strips, pointer-driven predictor links, threshold spectra and an entrance story. Implemented in the current feature branch; **158 EditMode and 45 PlayMode tests pass**. The Android M6 candidate was installed and launched. The first Quest walkthrough reported an open second-tree interaction issue; see the [interaction follow-up](docs/m6-in-world-explanations-2026-09-30.md#first-quest-walkthrough-and-interaction-follow-up).

Current development: [M5 forest experience](docs/m5-progress-2026-09-30.md) is implemented on `codex/m5-forest-experience`: left-stick walking, direct crown selection, a bounded tabletop overview, selectable subtree summaries, visitor guidance and procedural spatial audio. The user reports that the preceding navigation works; combined Quest acceptance is deferred. Earlier iterations: [signed contribution lighting](docs/m4-contribution-glow-2026-09-30.md) colors reached-tree planter rings and shows signed route values on hover. Also, the [forest appearance update](docs/m4-forest-appearance-2026-09-30.md) puts small names on planters and makes structural size differences visible. Previously, [branch stones and an A-button tree menu](docs/m3-branch-stones-2026-09-30.md) restore the requested one-tree controls. The [compact one-tree view](docs/m3-compact-layout-2026-09-30.md) lowers the diagram and table, reduces labels/buttons, and restores seated/standing controls in a tree submenu. [Simplified M3/M4 navigation](docs/m4-simple-navigation-2026-09-30.md) starts in an empty clearing with a case menu. M3 selects one tree from a list; M4 opens a pine garden with path markers, hover information, click-to-enter trees and saved return locations. The default is the user-selected 50-tree `export_Mobile_Click_Through_Rate_AGB_demo.json`. Nested AGB exports load directly for structure/manual preview; profile scoring remains unverified for these exports.
An immersive explanation of adaptive gradient boosted decision trees for Meta Quest 3.

Explore a miniature forest, enter a tree, choose branches yourself, or follow a prepared customer profile. Watch each selected leaf contribute to the ensemble score in a twilight forest with luminous paths inspired by the existing AGB Grid Drive prototype.

**Status: implementation underway.** M5 and the twelve M6 explanation additions are implemented on `codex/m5-forest-experience`; **158 EditMode and 45 PlayMode tests pass**. See the [M5 implementation record](docs/m5-progress-2026-09-30.md) for automated, visual and build evidence. The user reports that the preceding navigation works on Quest 3 and requested a combined headset check later. M5 device acceptance, remaining device checks, real AGB scoring verification and actual performance measurements remain pending. Open `BoostingExperience/Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity` for the mouse preview. The default demo uses manual exploration. Deterministic prepared-profile playback is retained with the separate verified synthetic fixtures.

The [winter sky update](docs/winter-sky-2026-10-05.md) adds catalog-based stars for latitude 55° N, a retained Moon and rare meteors. The previous full suites passed **177 EditMode and 70 PlayMode tests**; the selected moonlit revision with brighter stars and faint-star filtering passes all **3 focused sky PlayMode checks**. Headset validation remains pending.

All project documentation, code, comments, identifiers, commit messages, and initial application copy are in English.

## Start here

1. Read [Experience scenarios](docs/experience-scenarios.md) for the product behavior.
2. Follow [Mac setup](docs/macos-setup.md) or [Windows setup](docs/windows-setup.md) to install the tools and open the existing Unity project.
3. Work through [Development plan](docs/development-plan.md), starting with the headset smoke test.
4. Use [Testing](docs/testing.md) to decide whether a milestone is complete.
5. Use the [M1–M6 delivery backlog](docs/backlog.md) for six epics, detailed user stories, acceptance criteria, dependencies, and required evidence.

The [model contract and solution design](docs/model-contract.md) explains AGB structure, scoring semantics, the fictional example, and the production verification backlog.

## Local checks available now

Run from the repository root:

```sh
bash scripts/check-mac.sh
python3 scripts/verify-examples.py
```

The first command reports local prerequisites and returns a nonzero status while required setup is missing. It does not install software or change configuration. The second checks the synthetic example predictions using only the Python standard library. It does not verify Pega scoring or Unity code.

The environment checker uses `BoostingExperience/` and its pinned Editor version. Set `VRAGB_PROJECT_DIR` only when intentionally checking another project. Local prerequisite checks pass; headset behavior still requires the smoke test.

## Repository map

| Path | Purpose |
| --- | --- |
| `docs/experience-scenarios.md` | User journeys, interaction rules, visual direction |
| `docs/development-plan.md` | Milestones, dependencies, acceptance criteria, deferred features |
| `docs/backlog.md` and `docs/backlog/` | Six delivery epics and 60 detailed user stories with visual flows |
| `docs/setup-review-2026-09-27.md` | Observed project setup, discrepancies, missing items, and verification limits |
| `docs/macos-setup.md` | Mac installation, packages, device setup, first APK, troubleshooting |
| `docs/windows-setup.md` | Windows installation, PowerShell checks, Quest drivers, APK deployment, troubleshooting |
| `docs/toolchain.md` | Proposed dependencies and actual verification status |
| `docs/architecture.md` | Planned Unity modules and state ownership |
| `docs/model-contract.md` | Data semantics and the boundary between exploration and prediction |
| `docs/testing.md` | Automated, Editor, headset, and demo validation |
| `docs/references.md` | Reviewed local references and official setup sources |
| `data/examples/` | Synthetic model, profiles, and expected predictions |
| `data/private/` | Local-only real exports; ignored by Git |
| `scripts/` | Environment and example checks |
| `unity/` | Redirect to the canonical Unity workspace instructions |
| `BoostingExperience/` | Canonical Unity Editor project, versioned by this parent repository |

## Agreed technology

Unity 6, C#, URP, Unity OpenXR Plugin, and Meta XR Core/Interaction SDK. The target is a standalone Android ARM64 application on Quest 3. Controllers are the initial input method. The first release works offline with bundled demonstration data.

The user selected the installed **Unity 6000.6.3f1 Apple Silicon** baseline on 2026-09-28. Keep that version; no Editor migration is planned. Open `BoostingExperience/` in Unity Hub and manage Git from this repository root. Package manifest, lock, settings, assets, and their metadata belong to this repository. See [Toolchain](docs/toolchain.md) for tested limits and [repository consolidation](docs/repository-consolidation.md) for preserved history and recovery.

## First deliverable

A Quest user can inspect one seven-node tree, choose a branch, reach a leaf, go back without double-counting its contribution, and replay the same tree with a prepared profile. The scene includes a small but coherent piece of the final forest environment.

The existing `AGBVisualizer` is a reference implementation. This repository does not modify it or assume that its display calculations are a verified production scorer.

The repository is local. Creating a remote, publishing a build, and selecting an open-source license are separate decisions.
