# VRExperienceAGB

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
