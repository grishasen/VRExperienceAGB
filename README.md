# VRExperienceAGB

Current development: [simplified M3/M4 navigation](docs/m4-simple-navigation-2026-09-30.md) starts in an empty clearing with a case menu. M3 selects one tree from a list; M4 opens a pine garden with path markers, hover information, click-to-enter trees and saved return locations. The default is the user-selected 50-tree `export_Mobile_Click_Through_Rate_AGB_demo.json`. Nested AGB exports load directly for structure/manual preview; profile scoring remains unverified for these exports.
An immersive explanation of adaptive gradient boosted decision trees for Meta Quest 3.

Explore a miniature forest, enter a tree, choose branches yourself, or follow a prepared customer profile. Watch each selected leaf contribute to the ensemble score in a twilight forest with luminous paths inspired by the existing AGB Grid Drive prototype.

**Status: implementation underway.** M4 synthetic behavior is implemented on `codex/m4-ensemble`; **150 EditMode and 27 PlayMode tests pass**, and the Android candidate built successfully. Evidence is recorded in the [simplified navigation record](docs/m4-simple-navigation-2026-09-30.md). The earlier M3 appearance/posture/lantern iteration was approved by the user on Quest 3. M4 headset acceptance, remaining device checks, real AGB scoring verification and actual performance measurements remain pending. Open `BoostingExperience/Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity` for the mouse preview. The default demo uses manual exploration. Deterministic prepared-profile playback is retained with the separate verified synthetic fixtures.

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
| `docs/backlog.md` and `docs/backlog/` | Six delivery epics and 48 detailed user stories with visual flows |
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
