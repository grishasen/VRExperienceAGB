# Scenery visible immediately — October 18, 2026

Status: all six scenery PlayMode tests pass. The non-Development APK is built, signature-verified and installed on Quest 3 following the user's subsequent installation request. The application launched successfully; renewed visual acceptance and performance measurements remain pending.

## Behavior

The startup scene enables full scenery detail. Grass and rock batches, model-tree crowns, decorative fir branches/trunks and backdrop tree cards select their highest existing detail level immediately. They no longer switch or disappear as the observer moves away. The same policy applies to the single-tree clearing, inactive views before they open, and newly rebuilt forests after a model change.

`ForestGardenView` disables automatic LOD selection after scenery construction and explicitly enables only the highest-detail renderers. Lower levels stay disabled, avoiding duplicate geometry. This also survives global quality/LOD-bias changes, which reset a simple `ForceLOD(0)` override in the rendering diagnostic. Native camera frustum and depth culling, texture mipmaps and alpha-tested needle edges remain in use. Very small distant features still occupy fewer pixels. All model trees and their metric dimensions remain intact; the change does not alter evaluation or head tracking.

The existing automatic LOD configuration is retained for an explicit code/Editor comparison through `SetFullSceneryDetail(false)`. No user action is needed to enable full detail in this APK. Performance reports now identify full detail, automatic LOD or a mixed capture.

## Validation

- Unity 6000.6.3f1 compiled the implementation without C# errors.
- All six `NearSceneryTests` pass in one run (7.77 seconds). The rendering regression compares full detail against automatic LOD for grass, rocks, model crowns, interior firs, backdrop trees and clearing vegetation. It verifies restored rendering, view changes, a model rebuild and unchanged tracked local pose. Global LOD bias is reduced for the render comparison without moving the tracked head; this is a visibility regression, not a Quest performance test.
- Android build `build_b4cce96c389b` succeeded in 33.452 seconds, with zero errors and six existing Pipeline/development-define/TextMeshPro warnings. ARM64, IL2CPP, non-Development, debugging and profiler connection disabled. All 134 project C#/shader/HLSL/scene files retained their pre-build checksums.
- Quest installation: `adb install -r` succeeded on device `2G97C5ZHBX02V4`; cold launch returned `Status: ok`. The installed APK checksum matches the distribution file. Process 7093 remained running after startup.
- Startup log: 1,524 lines from the new process contain no matched fatal exception/signal, NullReference/MissingReference exception or shader failure. Two entries report that OVRMrcLib could not be loaded; these are retained in the evidence, and the application continued running.
- Quest visual inspection and performance comparison: pending.

Evidence is stored in ignored `artifacts/scenery-always-visible-2026-10-18/`.

APK: `BoostingExperience/Builds/Android/VRExperienceAGB-Quest3-all-scenery-2026-10-18.apk`, **92,354,592 bytes**, SHA-256 `cd6d3c9cada0e245f6eb3c85da37a41e004b4b98537f96bfe47560daf7a9fd8b`. Package `com.vrexperienceagb.prototype`, version 1.0 / code 1. The existing Android Debug certificate and APK v2 signature verify; this is a direct-install APK, not a store-signed release.

## Distribution rebuild

Build `build_28302301a21d` succeeded in **6.049 seconds**, with zero errors and one existing warning about absent optional Pipeline runtime configuration. The saved startup scene has full scenery detail enabled. The ARM64 IL2CPP APK is non-Development, with debugging and profiler connection disabled. All 134 project C#/shader/HLSL/scene files match the tested and installed version; the six focused tests were not rerun solely for this unchanged rebuild.

Distribution file: `BoostingExperience/Builds/Android/VRExperienceAGB-Quest3-all-scenery-2026-10-18-distribution.apk`, **92,354,592 bytes**. Its SHA-256 remains `cd6d3c9cada0e245f6eb3c85da37a41e004b4b98537f96bfe47560daf7a9fd8b`, byte-identical to the installed APK. APK v2 signature and manifest checks pass. Build evidence is under ignored `artifacts/scenery-distribution-2026-10-18/`; a checksum file is saved beside the APK.

## Later Quest comparison

The previous non-Development APK remains available as `BoostingExperience/Builds/Android/VRExperienceAGB-Quest3-2026-10-18-0851.apk`, SHA-256 `9fd5be03896b093ad8d5dd350fd02551d83dfd982932a5b4a6887fca457c009a`. Use the same full model, viewpoint, refresh rate, resolution and audio state for both builds. In the application, open **Performance capture**, start the 60-second capture, and close the menu. The capture includes a three-second warm-up. Keep the headset active and compare median/p95/p99 frame times, available CPU/GPU timings and thermal state; capped FPS alone does not establish equal cost. Measurement files are saved in the application's `Measurements` directory.
