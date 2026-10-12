# VRExperienceAGB

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
