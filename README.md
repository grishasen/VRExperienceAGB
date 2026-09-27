# VRExperienceAGB

An immersive explanation of adaptive gradient boosted decision trees for Meta Quest 3.

Explore a miniature forest, enter a tree, choose branches yourself, or follow a prepared customer profile. Watch each selected leaf contribute to the ensemble score in a twilight forest with luminous paths inspired by the existing AGB Grid Drive prototype.

**Status: project foundation.** The repository contains the experience specification, delivery plan, Mac setup guide, a synthetic model fixture, and local checks. A Unity Editor project and a runnable Quest application have not been created yet. No Unity or Meta tools were installed as part of this foundation.

All project documentation, code, comments, identifiers, commit messages, and initial application copy are in English.

## Start here

1. Read [Experience scenarios](docs/experience-scenarios.md) for the product behavior.
2. Follow [Mac setup](docs/macos-setup.md) to install the tools and create the Unity project.
3. Work through [Development plan](docs/development-plan.md), starting with the headset smoke test.
4. Use [Testing](docs/testing.md) to decide whether a milestone is complete.

## Local checks available now

Run from the repository root:

```sh
bash scripts/check-mac.sh
python3 scripts/verify-examples.py
```

The first command reports local prerequisites and returns a nonzero status while required setup is missing. It does not install software or change configuration. The second checks the synthetic example predictions using only the Python standard library. It does not verify Pega scoring or Unity code.

## Repository map

| Path | Purpose |
| --- | --- |
| `docs/experience-scenarios.md` | User journeys, interaction rules, visual direction |
| `docs/development-plan.md` | Milestones, dependencies, acceptance criteria, deferred features |
| `docs/macos-setup.md` | Mac installation, packages, device setup, first APK, troubleshooting |
| `docs/toolchain.md` | Proposed dependencies and actual verification status |
| `docs/architecture.md` | Planned Unity modules and state ownership |
| `docs/model-contract.md` | Data semantics and the boundary between exploration and prediction |
| `docs/testing.md` | Automated, Editor, headset, and demo validation |
| `docs/references.md` | Reviewed local references and official setup sources |
| `data/examples/` | Synthetic model, profiles, and expected predictions |
| `data/private/` | Local-only real exports; ignored by Git |
| `scripts/` | Environment and example checks |
| `unity/` | Parent directory for the future Unity project |

## Agreed technology

Unity 6, C#, URP, Unity OpenXR Plugin, and Meta XR Core/Interaction SDK. The target is a standalone Android ARM64 application on Quest 3. Controllers are the initial input method. The first release works offline with bundled demonstration data.

Unity 6.3 LTS is the proposed starting line, subject to the first verified SDK/editor/headset combination. Exact versions must be recorded in [Toolchain](docs/toolchain.md) after that check. This repository intentionally contains no guessed Unity package manifest or project-version file.

## First deliverable

A Quest user can inspect one seven-node tree, choose a branch, reach a leaf, go back without double-counting its contribution, and replay the same tree with a prepared profile. The scene includes a small but coherent piece of the final forest environment.

The existing `AGBVisualizer` is a reference implementation. This repository does not modify it or assume that its display calculations are a verified production scorer.

The repository is local. Creating a remote, publishing a build, and selecting an open-source license are separate decisions.
