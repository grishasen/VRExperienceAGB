# Development plan

**October 7 revision:** the requested scenario/menu/placement, full-forest profile and A/B playback, result overview, wildlife and star-field corrections are implemented. On-demand desktop recording is available through `scripts/record-scenario.sh`. See [current scenario behavior](experience-scenarios.md) and [testing evidence](testing.md). Real-profile/source scoring reconciliation and Quest acceptance remain open. This revision does not claim measured device performance improvements.

**Current update — 2026-09-30:** M5 forest presentation is implemented on `codex/m5-forest-experience`: left-stick walking, direct crown selection, bounded tabletop scale/rotation, shared-state return, selectable hidden-subtree summaries, guidance/recovery and original spatial audio. See the [M5 record](m5-progress-2026-09-30.md). The user reports that the preceding navigation works and requested deferring the combined headset check. Formal Quest acceptance, actual performance measurements and production AGB scoring verification remain open. Earlier dated records below are historical.

Status: source/configuration foundation recorded; M1 headset acceptance deferred; M2-01–M2-05 implemented and verified by 84 Unity EditMode tests; scene implementation and real-model verification remain open. Updated: 2026-09-28. See the [setup review](setup-review-2026-09-27.md) for the existing `BoostingExperience/` project's configuration and outstanding work.

The [delivery backlog](backlog.md) expands M1–M6 into six business-facing epics and 60 user stories. Each epic maps every checklist item below to its stories and defines acceptance evidence. These are planned requirements, not completed test results.

## Delivery approach

The Android build is proven. On 2026-09-28 the user requested completing D07/D10 and revisiting the remaining checks when main development begins. Use the [configuration checkpoint](configuration-checkpoint-2026-09-28.md), then develop model semantics and the complete interaction loop while collecting the deferred acceptance evidence. Add environmental detail while keeping every milestone runnable on Quest 3. A desktop preview alone does not complete a VR milestone.

Estimates below are working days for one developer familiar with Unity. They exclude account approval delays, learning Unity from scratch, procuring assets, store review, and resolving undocumented source-model behavior. Expect roughly 5–8 working weeks for the demonstration MVP, including integration contingency. Real AGB scoring may have an independent dependency on reference predictions.

## M0 — Repository foundation

**Status:** delivered by the initial repository creation.

- [x] English scenario specification and visual direction.
- [x] Mac installation and first-device-build instructions.
- [x] Architecture, model contract, test strategy, and dependency status.
- [x] Synthetic model, profiles, expected predictions, and local checks.
- [x] Initial local Git repository.

These checkboxes do not imply a Unity project or VR build exists.

## M1 — Mac and Quest smoke test

**Estimate:** 1–2 days. **Depends on:** M0, Unity/Meta accounts, Quest 3 and a data cable.

- [x] Install Unity Hub, Unity 6000.6.3f1 Apple Silicon (user-accepted baseline), and its Android modules.
- [ ] Install and configure OpenXR plus Meta XR Core/Interaction SDK.
- [x] Establish the Universal 3D project at the user-selected `BoostingExperience/` location under the parent repository.
- [x] Run Android configuration validation and configure the ARM64 player; D07 controller-only offline APK built and merged permissions verified on 2026-09-28.
- [ ] Create a scene with a tracked rig, floor, readable label, and selectable object.
- [ ] Build, install, and run an APK on Quest 3. Build and update installation succeeded on 2026-09-27; launch and interaction remain untested.
- [ ] Verify headset/controller tracking and a controller selection.
- [ ] Disconnect the Mac and relaunch the application on the headset.
- [x] Record exact Editor, package and Android toolchain versions, last observed headset Android build, and source/APK identity. Runtime qualification is deferred.
- [x] Record generated project settings, package lock, assets, and `.meta` files together in the parent repository. This is a configuration snapshot; headset acceptance remains pending.

**Exit:** the standalone scene works on Quest 3. Capture the result in `artifacts/` locally and summarize the tested configuration in `docs/toolchain.md`. A simulator-only result does not pass this milestone.

## M2 — Model evaluation and source-model verification

**Estimate:** 3–5 days for the internal contract; source-model verification is dependency-driven. **Depends on:** M0; C# validation requires M1.

- [x] Implement the normalized model and profile types as plain C#.
- [x] Validate IDs, roots, child references, acyclicity, operators, feature types, and finite values.
- [x] Implement deterministic paths, leaf contributions, baseline handling, and output transformation.
- [x] Match the synthetic fixtures in `data/examples/`.
- [x] Support numeric `<`, categorical membership, and explicit missing-value predicates.
- [ ] Implement a fail-closed AGB import adapter that rejects unsupported conditions.
- [ ] Obtain trusted AGB predictions and leaf paths for representative profiles.
- [ ] Confirm baseline, leaf-score scale, learning-rate treatment, missing behavior, and any calibration with the source model owner.
- [ ] Resolve discrepancies before claiming real-model prediction fidelity.

**Evidence (2026-09-28):** [M2 foundation record](m2-foundation-2026-09-28.md), 84 passing EditMode tests in 6000.6.3f1. No new evaluator APK/device run is claimed.

**Exit:** internal fixtures pass. Real exports have a separate verified/unsupported status; synthetic work can continue while source-model evidence is pending. No silent fallback parses an unknown split as a numeric condition.

## M3 — One-tree vertical slice

**Current implementation:** the saved one-tree scene and session logic are playable in the Editor. 111 EditMode and 11 PlayMode tests pass; see the [current record](development-progress-2026-09-29.md). Inspection, profile preview, score breakdown and haptic dispatch are implemented. Controller hardware, haptic sensation, comfort, readability and M3-08 acceptance remain open in the [combined checklist](m3-device-check-2026-09-29.md).

**Estimate:** 4–6 days. **Depends on:** M1 and the internal part of M2.

- [ ] Build the seven-node synthetic tree in a small forest clearing.
- [ ] Create split platforms, branch labels, leaf stations, and a data-drop actor.
- [ ] Add controller pointing, selection feedback, and stable node inspection.
- [ ] Implement manual exploration, leaf commitment, undo, restart, and pause.
- [ ] Implement prepared-profile stepping and automatic playback on the same tree.
- [ ] Keep user tracking independent of the drop's animation.
- [ ] Display mode, tree, current condition, and score without clutter.
- [ ] Verify the full loop on the headset in seated and standing use.

**Exit:** a visitor can choose two different routes, undo one, replay a prepared profile, and explain the displayed leaf contribution. No duplicate score changes occur after repeated input.

## M4 — Ensemble and profile takeover

**Implementation status:** synthetic M4 implemented; [evidence](m4-progress-2026-09-30.md). Checkmarks below indicate implemented behavior, not formal Quest acceptance.

**Estimate:** 4–6 days. **Depends on:** M3.

- [x] Evaluate the complete synthetic ensemble and show baseline plus per-tree contributions.
- [x] Connect trees with deliberate transitions and a final output gate.
- [x] Add prepared-profile selection and editable profile copies.
- [x] Recompute all affected trees after an input edit.
- [x] Track manual route constraints; flag contradictions without removing free exploration.
- [x] Implement short and detailed tours with identical full-model results.
- [x] Add a persistent score display and an alternative to wrist-only UI.

**Exit:** both interaction modes work end to end. Backtracking and switching modes preserve the documented semantics. A shortened tour never becomes a truncated prediction.

## M5 — Diorama, depth, and environment

**Implementation status:** base scope implemented; [evidence and device limits](m5-progress-2026-09-30.md). The [October 3 addition of horizon wolves and howling](horizon-wolves-2026-10-03.md) is implemented; Quest acceptance remains pending. Checkmarks indicate implementation, not Quest acceptance. Headset checks are deferred at the user's request.

**Estimate:** 4–6 days. **Depends on:** M4; visual work can start during M3.

- [x] Add forest overview, tree selection, bounded scaling, and immersive entry/return.
- [x] Preserve selected profile, path, and node when switching scale.
- [x] Lay out branches in readable depth with controlled occlusion.
- [x] Add collapsed-subtree summaries and a focused local view.
- [x] Integrate coherent materials, restrained lighting, distant scenery, and spatial audio.
- [x] Track licenses and provenance for any introduced external assets.
- [x] Add a short onboarding sequence and visible recovery controls.
- [x] Add wolves on the distant horizon and occasional spatialized howling, respecting the sound toggle (M5-05; requested October 3, 2026).

**Exit:** the full demonstration is understandable without the presenter operating a keyboard. Foreground text stays legible; decorative scenery does not obscure decisions.

## M6 — In-world explanations, Quest acceptance and demonstration package

**Scope update — 2026-09-30:** the user approved twelve in-world explanation additions (M6-09–M6-20): readable questions, branch reasons, segment portraits, split significance, observation evidence, leaf explanations, crown passports, family composition strips, pointer-driven predictor links, threshold spectra, explicit missing-data routes, and an entrance model story. Implementation is delivered on `codex/m5-forest-experience`; see [the M6 backlog](backlog/m6-demo-readiness.md). Existing hardware acceptance remains deferred. The original estimate below covered acceptance only; this added implementation scope is additional.

**Estimate:** 4–6 days. **Depends on:** M5.

- [ ] Profile CPU/GPU frame time and memory on a standalone release build.
- [ ] Meet the project target of sustained 72 FPS at 72 Hz in the agreed worst visible scene; assess 90 Hz after there is margin.
- [ ] Test the full presentation duration and at least one 30-minute soak session.
- [ ] Check headset removal, pause/resume, controller tracking loss, recentering, and fresh launch.
- [ ] Test with at least three people unfamiliar with the controls; record comprehension and navigation issues.
- [ ] Test casting separately if it is part of the live demonstration.
- [ ] Build a clearly named APK, record its version and source commit, and write a short presenter guide.
- [ ] Create a demo tag only after acceptance; keep APKs outside ordinary Git history.

**Exit:** all blocking acceptance items in [Testing](testing.md) pass on Quest 3. The presenter can install and run the demo using the documented steps.

## Extensions after the demonstration MVP

| Feature | Prerequisite | Main work |
| --- | --- | --- |
| Predictor fireflies | Implemented; October 4 record | Exact-ID search, name filter, entire-model/profile-path scopes, markers and occurrence navigation; Quest acceptance pending |
| Full boosting trail | Implemented; October 4 record | Iteration selection, progressive tree reveal, ordered trail, distinct gain/contribution labels and intermediate verified-profile results; Quest acceptance pending |
| Two-profile comparison | Implemented for synthetic profiles; [October 3 record](profile-comparison-2026-10-03.md) | Independent A/B sessions, full-model deltas, first-divergence inspection and B editing; Quest acceptance deferred |
| Audit environment | Reviewed audit definitions | Evidence-linked findings, severity, missing-evidence state |
| Recorded guided review | Implemented; October 4 record | Stop capture/reorder, local save/load, explanations, validated replay and pause/restore; Quest acceptance pending |
| Live guided review | Recorded review and network design | Multi-user state, roles, avatars, pointers, voice |
| Hand tracking | Controller interactions validated | Input parity and accessibility testing |
| Continuous riding | Core comfort testing | User-controlled speed, stopping, additional comfort options |
| Learning-history terrain | Actual per-iteration metrics | Metric import and honest mapping to terrain |

Do not expand the MVP by treating every slide idea as a launch requirement.

## Dependencies that need explicit resolution

| Decision or dependency | Default / next action | Blocks |
| --- | --- | --- |
| Exact Unity/SDK combination | Unity 6000.6.3f1 accepted; current packages recorded after successful development APK build/install; runtime qualification remains in M1 | Reliable headset behavior |
| Real AGB scoring semantics | Verify with authoritative predictions and paths | Claims about real customer probabilities |
| Profile availability | Start with synthetic bundled profiles | Real-data demonstration only |
| Outcome wording | “Response probability” for synthetic examples | Domain-specific copy |
| Final package identifier | Use local prototype ID initially | Distribution configuration |
| Artifact distribution | USB install initially | External testers/store release |

## Working conventions

Use short feature branches after the baseline. Make each implementation change reviewable with relevant acceptance evidence. Update documentation when behavior changes. Keep a runnable scene at each milestone boundary. Use actual failures or changed behavior to decide when to expand testing.

See [October 4 extension implementation and verification](exploration-extensions-2026-10-04.md) for the current scope and evidence.
