# VRExperienceAGB

An immersive explanation of adaptive gradient boosted decision trees for Meta Quest 3.

Explore a miniature forest, enter a tree, choose branches yourself, or follow a prepared customer profile. Watch each selected leaf contribute to the ensemble score in a twilight forest with luminous paths inspired by the existing AGB Grid Drive prototype.

**Status: Unity setup in progress.** A project now exists at `BoostingExperience/` and opens in Unity 6000.6.3f1. The refreshed [setup review](docs/setup-review-2026-09-27.md) confirms Android tools, active URP, Meta SDKs, Android OpenXR, and a saved smoke-test scene. A development APK was built and installed on Quest 3 on 2026-09-27; launch, tracking, interaction, and standalone relaunch remain unverified. The user accepted the existing Editor baseline and project location on 2026-09-28. Warnings and optional feature scope still need review. The specification, synthetic fixture, and local checks remain available.

All project documentation, code, comments, identifiers, commit messages, and initial application copy are in English.

## Start here

1. Read [Experience scenarios](docs/experience-scenarios.md) for the product behavior.
2. Follow [Mac setup](docs/macos-setup.md) to install the tools and create the Unity project.
3. Work through [Development plan](docs/development-plan.md), starting with the headset smoke test.
4. Use [Testing](docs/testing.md) to decide whether a milestone is complete.
5. Use the [M1–M6 delivery backlog](docs/backlog.md) for six epics, detailed user stories, acceptance criteria, dependencies, and required evidence.

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
