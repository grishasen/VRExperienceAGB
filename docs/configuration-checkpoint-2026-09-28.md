# Controller-first configuration checkpoint — 2026-09-28

**Purpose:** preserve the D07 scope cleanup and D10 build/source record so main development can start from a known configuration. The user requested postponing the other checks until main development. This is a configuration checkpoint, not accepted headset behavior or M1 completion.

## Product scope and the current smoke scene

| Capability | Current smoke scene | Planned experience |
| --- | --- | --- |
| Head tracking and physical looking/leaning | Enabled; position and rotation tracking preserved | Always independent of scripted animation |
| Controller pointing and selection | Left and right controller ray interactors retained | Initial interaction method |
| Navigation between trees, overview/detail, and scale | No application navigation implemented in the cube test | Required by M3–M5; deliberate user-controlled transitions |
| Sample movement, turning, teleport and locomotor | Disabled in the comprehensive Meta sample rig | Choose and integrate the appropriate locomotion when the actual scene exists |
| Hand tracking | Disabled in the smoke scene and Android support configuration | Deferred extension in the original plan; may be deliberately added later |
| Mixed reality, room scanning, anchors, colocation | Disabled | Outside the initial offline VR scope |

The SDK packages and sample objects were retained. No hand or locomotion asset was deleted. Fourteen rig branches are inactive, and 63 locomotion behaviours are disabled, so the prototype has no active sample locomotor. Keeping prefab structure/data avoids discarding references that may be useful during development. This is not a decision to remove product navigation. Runtime interaction and comfort checks are deferred.

## Exact source and build identity

| Field | Recorded value |
| --- | --- |
| Source commit | `76defad516a5e15d0a41d12727df1b72e3580001` |
| Previous foundation checkpoint | `e98950d3db19d31e81748f9eb0b0b322153f6802` |
| Canonical Unity project | `BoostingExperience/` in the parent repository |
| Scene | `Assets/VRExperienceAGB/Scenes/SmokeTest.unity` |
| Application | `com.vrexperienceagb.prototype` |
| Product / version | `VRExperienceAGB`, version 1.0 / Android code 1 |
| Build type | Development APK; script debugging disabled |
| Build ID | `build_a9d677c2f693` |
| Build result | Succeeded; zero errors, two warnings |
| Build time | 2026-09-28T06:24:23.2339660Z to 2026-09-28T06:24:34.4915510Z; 11.257 seconds |
| APK | `artifacts/builds/VRExperienceAGB-d07-20260928.apk` |
| APK size | 91,946,442 bytes (about 92 MB) |
| SHA-256 | `d31396e853c08458f6ee3008c7d2e961cf1a6c85ad91d67d202b2c2daba1efc8` |
| Device installation | Not performed for this checkpoint, at the user's request to defer other checks |

The version number was not incremented for this local configuration build. Identify it by file name, SHA-256, and source commit, rather than version 1.0 alone. The previous September 27 APK was installed on Quest; that installation is not evidence for this changed build.

All 97 tracked Unity files match the source checkpoint, including manifest, lock, settings, scene and metadata. Their SHA-256 values are recorded locally in `artifacts/d07-d10-2026-09-28/source-provenance.json`. Dependencies were not changed by D07; package files remain pinned together in Git. Local development credentials remain excluded from Git.

## Toolchain and rendering

| Component | Configuration / observation |
| --- | --- |
| Host | Apple Silicon, macOS 27.0; full host qualification deferred |
| Editor | Unity 6000.6.3f1 Apple Silicon, accepted by the user |
| URP | 17.6.0; `Quest_URP`, Linear color space |
| OpenXR / XR Management / Input System | 1.18.0 / 4.7.0 / 1.20.0 |
| Input handling | Input System Package (New) |
| Unity OpenXR: Meta | 2.6.1 |
| Meta Core / Interaction / OVR integration | 207.0.0 / 207.0.0 / 207.0.0 |
| Test Framework / Visual Studio Editor | 1.8.0 / 2.0.28 |
| Android player | IL2CPP, ARM64; min API 32, target/compile API 34 |
| Graphics / stereo | Vulkan; OpenXR SinglePassInstanced |
| Android tools | Editor-bundled SDK; NDK 27.2.12479018, JDK 17.0.18+8, build tools 36.0.0 (recorded installation) |
| Supported device declaration | Quest 3 only (`quest3`) |
| Last recorded headset | Quest 3, Android 14/API 34, build `UP1A.231005.007.A1`, incremental `52433670048800520` on September 27 |
| Performance target | 72 Hz/native 72 FPS remains a target; no measurement or display-rate verification claimed |

No Horizon OS marketing version, device serial, or account identifier is inferred or recorded.

## D07 changes and retained infrastructure

Disabled Android OpenXR features: AR raycast, session, camera, colocation discovery, anchor, mesh, bounding box, boundary visibility, plane, occlusion, and SpaceWarp. Meta's separate SpaceWarp feature was already disabled. OVR project settings now specify ControllersOnly and Quest3; Unity's Meta Quest feature targets only Quest 3 as well.

Both controller rays, OVR head tracking, the grabbable cube, floor and label remain configured. Mixed-reality capture is disabled and startup permission requests are off. The comprehensive prefab's inactive branches are retained for later deliberate reuse.

Retained Android features are Meta Quest support, Meta XR, Oculus Touch interaction/proximity, OpenXR lifecycle, display utilities, composition-layer infrastructure, Meta foveation and subsampled layout. These support the existing XR/rendering setup; retaining them does not enable the disabled AR subsystems or claim measured performance. No package removal or upgrade was needed.

The merged APK manifest was inspected, not just the source XML. It declares only Quest 3 and has no hand-tracking feature or permissions for hands, scene data, anchors, map import/export, boundary suppression, colocation, Internet, or foreground/media-projection services. Explicit manifest merger removals prevent SDK/library defaults from adding the unused Internet and foreground-service permissions.

The only remaining requested permission is `com.vrexperienceagb.prototype.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`, an application-specific permission declared with signature protection for the Android receiver infrastructure. It grants none of the removed device capabilities.

## Checks performed and remaining warnings

- Android build succeeded. OpenXR configuration validation returned zero issues and Meta evaluation had no Required failures.
- Live saved scene inspection found two active controller ray interactors, zero active OVRHand tracking components, zero active locomotion behaviours, position/rotation head tracking enabled, and zero missing MonoBehaviour scripts.
- The scene is saved. All tracked `.meta` files remained unchanged.
- APK identity, architecture, API levels, supported device declaration and final permissions were verified.

The build warnings are explicit: Pipeline has no runtime configuration (Editor control remains available); Unity attempted to add Internet permission, which the source manifest intentionally removes. The final APK confirms Internet permission is absent.

Meta's advisory checks include optional SpaceWarp, controller-only mode without hands, texture compression, optional Platform services, and a difference from its default manifest template. Do not re-enable hands, SpaceWarp or permissions merely to clear these recommendations. Texture-compression tuning and other remaining diagnostics are deferred. No warning was hidden by marking it ignored.

D06's duplicate simulation settings remain unchanged. No Play Mode, Simulator session, headset launch, interaction, or performance test was run for this checkpoint.

## Reproduce the configuration later

1. Use the source commit above in a separate checkout/worktree when reproduction is needed; preserve active work before switching revisions.
2. Open `BoostingExperience/` with Unity 6000.6.3f1 and allow the pinned package manifest/lock to resolve. Install that Editor's Android support, SDK/NDK and JDK if absent.
3. Run `bash scripts/check-mac.sh` from the repository root. The optional `VRAGB_PROJECT_DIR` must explicitly identify any alternate checkout.
4. Select Android, the saved SmokeTest scene, Development Build, and no script debugging. Build the APK under ignored `artifacts/builds/`.
5. Verify the new APK's manifest/identity before deployment, then execute the deferred headset checklist. Generated build bytes need not be bit-for-bit identical across environments; record the new artifact's own hash.

A fresh checkout/package restore has not been exercised. The checkpoint is recoverable source plus local build evidence, not a claim of clean-machine reproducibility.

## Deferred checks requested by the user

| Finding / work | Revisit when | Required evidence |
| --- | --- | --- |
| D02 and D08: new build deployment, immersive launch, head/controllers, pointing/grab, readability, seated/standing comfort, relaunch/offline | Beginning of main Unity scene development | Dated Quest results using the changed APK |
| D06: duplicate simulation settings and historical warnings | Beginning of main development / before a Simulator workflow | Deliberate consolidation and affected validation |
| M1 remaining toolchain/controller/MQDH checks and fresh-checkout reproduction | Beginning of main development | Explicit pass/fail/not-tested evidence |
| D11: Unity evaluator/application tests | As M2 and subsequent stories are implemented | Meaningful C# and PlayMode acceptance |
| D12: native frame rate, GPU/CPU/memory, recovery, soak and comprehension | Start measurement planning during main development; execute on representative scenes in M6 | Actual Quest measurements and user observations |

The deferral changes scheduling, not acceptance criteria. M1 remains unaccepted and no headset or performance result is marked passed.
