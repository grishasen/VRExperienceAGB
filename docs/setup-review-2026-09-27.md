# Unity setup review — 2026-09-27

**Result: the development APK built successfully and was installed as an update on Quest 3 on 2026-09-27. M1 remains unaccepted because launch, tracking, interaction, and independent relaunch have not been verified.**

Final live inspection: approximately **18:42–18:46 UTC on 2026-09-27**. Earlier gaps were resolved by concurrent project work while the backlog was being prepared. This report uses the refreshed snapshot, not the original missing-package snapshot. The initial review changed documentation only. A later user-authorized follow-up built and installed the APK; the user corrected input handling. On 2026-09-28 the user authorized unifying repository ownership while preserving the inner history and all current project files, and accepted the installed Editor baseline.

Reviewed `BoostingExperience/` against the [Mac setup guide](macos-setup.md), [development plan](development-plan.md), project instructions, and live Editor state. The [delivery backlog](backlog.md) maps remaining work into six epics and 48 user stories. Detailed snapshots are in ignored `artifacts/setup-review-2026-09-27/`; files ending in `-final.json` represent the refresh.

## Findings and resolution status

| ID / priority | Finding and impact | Required next step / story |
| --- | --- | --- |
| D01 / Resolved | At 19:05 UTC, ADB reports one authorized Quest 3 in `device` state and read-only device queries succeed. Android 14 / API 34, build `UP1A.231005.007.A1`, incremental `52433670048800520`. Developer Mode was confirmed by the user. | USB detection/authorization is resolved. Actual tracking, selection, deployment, and relaunch remain under D02/D08; remaining M1-07 checks are not automatically passed. |
| D02 / Deferred; build/install partly proven | September 27 APK built and installed. Changed D07 APK built successfully September 28 but was not installed/run. | User requested revisiting deployment, immersive launch, tracking, interaction and offline relaunch at the start of main development. M1-08 remains unaccepted. |
| D03 / Resolved | User retained `BoostingExperience/` and selected one parent repository. Original inner history is imported as `codex/boostingexperience-history`; original Git metadata and 107 project files are backed up and verified. The Editor scene/package references remain intact. | Use the updated opening instructions and [recovery record](repository-consolidation.md). Device acceptance and reproducibility remain in M1-09. |
| D04 / Decision resolved | User explicitly accepted installed **6000.6.3f1** on 2026-09-28. Development build and update installation passed; no Editor migration is planned. | Keep this baseline and complete runtime compatibility/performance checks. Acceptance of the version is not evidence of headset behavior. M1-02, M1-08. |
| D05 / Build blocker resolved | The first Android build rejected **Both** input handling. The user changed it to **Input System Package (New)** and restarted Unity. A live check confirmed `activeInputHandler = 1`; the second build succeeded. | Runtime controller checks remain under D02/D08; successful compilation/build does not prove interaction behavior. M1-04, M1-05, M1-08. |
| D06 / Deferred | Two identical loaded simulation runtime settings assets remain; EditorBuildSettings references the Settings copy. No new duplicate warning appeared after restart. | User deferred diagnosis/consolidation until main development. No D06 assets changed in D07 cleanup. |
| D07 / Configuration resolved | Controller-only Quest 3 smoke scene: sample hand and locomotion branches disabled, 11 optional XR features disabled, head tracking and both controller rays retained. New APK has no unused MR/hand/network/foreground-service permissions. Future M3–M5 navigation remains planned. | Runtime behavior and comfort checks deferred by the user. [Exact scope/build record](configuration-checkpoint-2026-09-28.md). |
| D08 / Deferred | Smoke scene remains saved with controller rays, cube, floor and label; inactive sample hand/locomotion structure is retained. Headset selection, readability, controller parity, comfort and recovery are untested. | User deferred these checks until main development. Use device evidence to decide later rig simplification. M1-06, M1-08. |
| D09 / Resolved | The checker now defaults to canonical `BoostingExperience/`, reads its pinned Editor, and supports explicit `VRAGB_PROJECT_DIR`. The actual project passes local checks; a deliberately missing path fails with its exact missing version-file path. | Keep this read-only prerequisite check separate from device acceptance. M1-01. |
| D10 / Configuration checkpoint resolved | Exact source `76defad516a5e15d0a41d12727df1b72e3580001`, toolchain, APK hash and merged manifest are recorded; source assets/settings/metadata and package files are in the parent repository. | New APK deployment, headset qualification and fresh-checkout reproduction are explicitly deferred, not passed. [Checkpoint](configuration-checkpoint-2026-09-28.md). |
| D11 / P2 | Zero Unity tests are discoverable; there is no project-owned application/evaluator implementation. Existing scene/package components do not deliver manual tree exploration or deterministic profile playback. | Implement and validate M2–M5; the passing Python fixture checker is only a reference contract. |
| D12 / Deferred | 72 Hz/native 72 FPS, release performance, soak, recovery and comprehension have no accepted device evidence. | Start planning/measurement during main development and complete M6 on representative headset scenes. No desktop substitution or inferred pass. |

P1 items prevent the first accepted headset demonstration. P2 items require resolution or explicit qualification as part of the relevant milestone. These priorities do not imply that every project-scope decision blocks a local experimental APK.

## Confirmed current configuration

| Area | Live/saved evidence |
| --- | --- |
| Editor / host | Unity 6000.6.3f1, Apple Silicon; macOS 27.0. Editor ready, Play Mode stopped. |
| Compilation / Console | `compilationFailed: false`, not compiling, zero current Console errors, six captured/current warnings at the refresh. This describes the initial refresh; the subsequent build attempt and restart are recorded below. |
| Active build | Android; Development Build enabled; script debugging disabled; `SmokeTest.unity` enabled in the build list. |
| Android player | IL2CPP, ARM64, minimum API 32, target API 34, identifier `com.vrexperienceagb.prototype`, product name `VRExperienceAGB`. |
| Rendering | URP 17.6.0; `Quest_URP` is the live default and active pipeline. Linear color space; explicit Vulkan Android API. |
| XR packages | OpenXR 1.18.0, XR Plug-in Management 4.7.0, Input System 1.20.0, Unity OpenXR: Meta 2.6.1. |
| Meta packages | Core 207.0.0, Interaction 207.0.0, Interaction OVR integration 207.0.0 registered in the Editor. Installation does not certify device compatibility. |
| Android XR | OpenXR loader present; Meta XR and Oculus Touch controller features enabled. Standalone OpenXR is also enabled. |
| Android validation | OpenXR query returns zero issues with an active Android loader. Read-only evaluation of Meta setup tasks finds no outstanding Required task. Two outstanding Recommended tasks concern optional Platform SDK services; see below. |
| Android tool paths | SDK, NDK, and JDK resolve to the selected Editor's `PlaybackEngines/AndroidPlayer/` installation. Android build support reports available. |
| Tool versions | NDK 27.2.12479018 (r27c), JDK Temurin 17.0.18+8; installed SDK platform folders include Android 34, 36, and 37.0; build tools 36.0.0. Follow-up APK metadata confirms compile SDK 34; the second development build succeeded. |
| Smoke scene | Saved at `Assets/VRExperienceAGB/Scenes/SmokeTest.unity`; one `OVRCameraRig`, Meta comprehensive interaction rig, floor, ray-grabbable cube, and English instruction label. No missing MonoBehaviour scripts detected in the open scene. |
| Test/IDE tooling | Test Framework 1.8.0 is now a direct dependency; zero tests found. Visual Studio Editor 2.0.28, VS Code selected, Unity/C# extensions installed. IntelliSense/debugging not exercised. |
| Serialization / metadata | Force Text and Visible Meta Files configured. Initial asset scan found no missing metadata companions; final new scene/pipeline assets have paired `.meta` files. |
| Device tools | MQDH 6.5.2 and Unity-bundled ADB available. Follow-up at 19:05 UTC: one authorized Quest 3 detected; device model/build queries succeed. MQDH device UI was not checked. |
| Synthetic contract | Four profile expectations plus rejection/missing-value/weight/numeric-stability checks pass in the Python verifier. No Unity or real AGB scoring acceptance is implied. |

The distinction between Unity OpenXR: Meta and Meta's own SDK remains relevant: the former provides Unity's Meta/AR Foundation integration; it does not replace Core/Interaction. Both are now installed. [Unity package documentation](https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@2.6/manual/index.html), [Meta SDK overview](https://developers.meta.com/horizon/documentation/unity/unity-sdks-overview/).

## Recommendations that are conditional, not current blockers

Meta's read-only task checks report two outstanding Recommended items: complete a Data Use Checkup, and configure a Platform application ID/package name. Both task messages explicitly say to ignore them when Platform SDK APIs are not used. The initial offline experience does not require Platform services, and no Meta Platform SDK appears in the registered package list. Do not add account/platform integration just to clear these conditional recommendations. Revisit them if that scope changes.

Meta XR Simulator was not found in standard application locations. It is optional; desktop OpenXR activation alone does not prove a Simulator workflow. It must not delay the USB-deployed APK.

## Setup-guide coverage and unknowns

| Guide area | Review conclusion |
| --- | --- |
| Machine prerequisites | Apple Silicon, Git, Python, Apple developer tools, Hub and VS Code found. Free disk/memory capacity and formal host support were not qualified. |
| Accounts/license | Editor opens; account entitlement/license suitability, Meta team verification were not audited. Developer Mode was subsequently confirmed by the user and USB debugging authorized. |
| Editor/modules | Installed, including Android SDK/NDK/JDK; exact Editor baseline differs from the guide. |
| Project creation/version control | Project exists and renders with URP. The user accepted the existing location and parent-repository consolidation on 2026-09-28. |
| Packages/validation | Required SDKs are present and current read-only Android checks pass; scope excess and warnings require review. |
| Player/build profile | Main recommended prototype values are now configured. Follow-up development build succeeded and merged manifest was inspected; runtime behavior remains unverified. |
| Scene | Saved, included in build, and structurally complete for smoke testing; device behavior/readability not tested. |
| Device/deployment/relaunch | Quest 3 USB connection authorized at 19:05 UTC; APK build/update installation succeeded at 19:15–19:16 UTC. Launch and independent relaunch remain untested. |
| Permissions/signing | Source manifest inspected: VR launch category, hand-tracking permission, broad device support. Merged APK confirms ARM64, compile/target API 34, min API 32, VR launch category, broad Quest-family devices and additional permissions. Update installation succeeded; release signing qualification remains pending. |
| Simulator | Optional; no installed/runtime session verified. |
| Version control/evidence | Installed versions recorded; verified checkpoint and completed device acceptance still missing. |

The checker's earlier false project-path failure was corrected on 2026-09-28. It now passes for the canonical project and correctly rejects a missing explicit project. Local prerequisite checks are not headset acceptance.

## Changes since the first inspection

| Earlier finding | Refreshed state |
| --- | --- |
| Android modules absent | Installed; tool paths and Android support confirmed |
| URP installed but inactive | `Quest_URP` active; Linear configured |
| Meta Core/Interaction absent | 207.0.0 packages registered |
| Desktop-only XR | Android OpenXR now configured |
| macOS/default player settings | Android/IL2CPP/ARM64/API 32–34/Vulkan/prototype identifier configured |
| Unsaved camera/light scene; empty build list | Saved smoke scene with rig/floor/label/cube included |
| Test Framework indirect | Direct dependency now; still zero tests |

These changes were observed during the refresh; they are not attributed to this review. Earlier snapshots remain local evidence of the first inspection.

## Current next steps

D07 and D10 are recorded in the [2026-09-28 configuration checkpoint](configuration-checkpoint-2026-09-28.md). The user requested postponing other checks until main development. Start that work from the recorded source; revisit D02/D08 headset behavior, D06 diagnostics, reproducibility and later M6 acceptance at the documented triggers. This scheduling decision does not mark M1 accepted.

## D01 follow-up — 19:05 UTC

USB hardware detection first found Quest 3 while ADB was empty. After the user confirmed Developer Mode, ADB reported `unauthorized`; after the user accepted the in-headset prompt, ADB reported `device` and device-property queries succeeded. Sanitized evidence: `artifacts/setup-review-2026-09-27/device-authorized-check.json`. Device serial numbers were not saved. The human-facing Horizon OS release label was not inferred from the Android build number.

## D02 / D05 follow-up — Android build attempt

At 19:07:58–19:08:44 UTC, the approved development APK build failed with one error: Android rejected Active Input Handling = Both. No APK was produced and no installation was changed. A separate warning stated that Pipeline runtime control was disabled because no RuntimePipelineConfig asset exists; this was not the reported build blocker. The first build report is preserved at `artifacts/smoke-test-2026-09-27/build-attempt-01.json`.

The user corrected input handling and requested continuation. At approximately 19:14 UTC, live inspection confirmed Input System Package (New), a restarted Editor, no compilation failure, and a saved smoke scene. A second development build was started. An existing `com.vrexperienceagb.prototype` version 1.0 / code 1 installation was detected before deployment; preserve its data when updating it. Installation presence alone does not establish smoke-test acceptance.

## Successful build and installation — 2026-09-27, 19:15–19:16 UTC

The second build succeeded in 40.9 seconds with zero errors and four warnings. The warnings concern disabled Pipeline runtime control and three IL2CPP/TextMeshPro compilation notices; none prevented the APK build. The installable APK is **91,946,442 bytes (about 92 MB)**. Unity's broader report size of 1,228,526,761 bytes is not the APK size.

- APK: `artifacts/builds/VRExperienceAGB-smoke-20260927-1914.apk`.
- SHA-256: `b0179814753105d26c22fc51a0d42bd25103be1331f591237b2fa79c832e5fd6`.
- Application: `com.vrexperienceagb.prototype`, version 1.0 / code 1, ARM64.
- Minimum API 32; target and compile API 34. VR launch category is present.
- Update installation returned Success at 19:16:53 UTC, preserving the existing app's data. No uninstall was performed.
- Evidence: `artifacts/smoke-test-2026-09-27/build-attempt-02-summary.json`, `apk-manifest.txt`, and `installation.json`.

The September 27 APK manifest requested hand tracking, Internet, foreground/media-projection services, scene/anchor access, boundary visibility, map import/export, and colocation discovery, with support broader than Quest 3. This historical evidence was superseded by the September 28 D07 APK: those permissions are absent and the device declaration is Quest 3 only.

The attempted launch command was **not executed**: automatic approval review could not complete because of a usage limit, not because the action was judged unsafe. Launch, head tracking, controller interaction, readability, comfort, independent relaunch, and offline behavior remain **not tested**. On 2026-09-28 the user requested moving to the next finding; this does not mark those checks passed.

## D03 decision preparation — 2026-09-28, before consolidation

A read-only recheck confirms two independent repositories: the parent has initial commit `8e7d711`; `BoostingExperience/` has initial commit `ca8dccf` on `main`, plus modified/deleted tracked files and untracked Unity assets. The parent tracks none of the nested project's files. The guide names `unity/VRExperienceAGB/`.

Recommended decision: retain the working `BoostingExperience/` location, establish one parent repository for documentation and Unity assets, and align the guide/checker with that location. Before changing ownership, preserve the inner repository's history and all current work in a recoverable backup; review generated/private/development-only files before staging. Alternative: keep separate repositories and document that ownership explicitly. This was the pre-decision state. The user subsequently approved the recommendation; completed work is recorded below.

## D03 / D04 / D09 resolution — 2026-09-28

The user selected the recommended consolidation and explicitly accepted the installed Editor. The canonical project remains `BoostingExperience/` in Unity 6000.6.3f1. Git now resolves that directory to the parent repository. Original history remains available as `codex/boostingexperience-history`; backup and recovery details are in [Repository consolidation](repository-consolidation.md). All 107 saved files were verified against the recovery archive; only the intended Git attribute policy changed during consolidation. Scene, package, player setting, and metadata hashes remained unchanged. The live Editor still reports the saved smoke scene with zero missing MonoBehaviour scripts and the expected package versions.

The prerequisite checker passes for the canonical location and correctly fails for a missing explicit location. D09 is therefore resolved together with D03. D04 is resolved as a baseline decision; headset behavior and performance are still not accepted.

D06 remains unresolved: two equal simulation runtime settings assets are still loaded, even though the warning has not recurred after restart. No simulation assets were changed during consolidation. The user subsequently deferred this check to main development.

## D07 / D10 completion and deferral — 2026-09-28

The user authorized D07/D10 fixes and requested leaving other checks for main development. The controller-only offline APK built successfully, its final manifest was inspected, and source commit `76defad516a5e15d0a41d12727df1b72e3580001` records the changed configuration. The [checkpoint](configuration-checkpoint-2026-09-28.md) contains artifact identity, retained/disabled capabilities, warnings and an explicit deferred-check table.

The hand and locomotion components are preserved but inactive in this initial smoke scene. Required M3–M5 navigation is still in scope; hand tracking remains a deferred extension under the original plan. New APK installation and runtime testing were not performed.
