# Main merge and Quest / Mac validation builds — October 26, 2026

## Source

`codex/windows-desktop` (including the Mac port, Option look, Windows port, and shared desktop icon) was merged into local `main` without conflicts. Merge commit: `2904129cf7ee3f9c7ae7f3b87d4e4a04b770eeb4`. Both builds below use that source in `/Users/gregory/Documents/VRExperienceAGB`. No remote push was performed.

The Quest checkpoint tag and previous APK remain preserved. Android uses IL2CPP, ARM64, Vulkan, and its enabled OpenXR startup; desktop input/observer activation remains restricted to desktop players and Editor preview. The Mac native picker importer is restricted to macOS, with its tracked GUID retained. Package versions and model evaluation code are unchanged.

## Checks

- 218/218 EditMode tests passed in the merged project.
- Full PlayMode run: 147 passed, zero failed, two optional video captures skipped (149 total).
- `python3 scripts/verify-examples.py` passed.
- Build-generated font caches and the URP shader-prefilter setting were restored to their tracked values. No unintended shared configuration changes remain.

## Quest APK

- Output: `BoostingExperience/Builds/Android/VRExperienceAGB-Quest3-merged-2026-10-26.apk`.
- Non-Development build, `BuildOptions.None`, OneTreeLearning scene; succeeded with zero errors and seven warnings.
- Actual APK size: 92,460,964 bytes. Unity's aggregate build-report byte count includes other outputs and is not the APK size.
- SHA-256: `c86e053d81aab576ac0df8e6ff28b4b194b4ae519a05a9e9ff0a41f270693ce3`.
- APK signature verification passed with Unity's bundled `apksigner`; signing certificate matches the preserved October 19 Quest APK.
- Package: `com.vrexperienceagb.prototype`, version name `1.0`, version code `1`, minimum SDK 32, target SDK 34. The manifest is not debuggable.
- Native libraries contain only `arm64-v8a`; the Mac dylib is absent.
- Warnings concern disabled Pipeline tooling, deprecated development/shader directives, large TextMeshPro IL2CPP methods, and an already-declared INTERNET permission.

This APK was not installed or launched on the headset in this task. Physical head tracking, controller selection, forest walking, table manipulation, prepared-profile/A-B playback, and sustained performance still require the user's headset check. No new headset performance claim is made.

## Mac application

- Built from the same merge commit as an Apple Silicon Mono/Metal development player; succeeded with zero errors and three warnings.
- Output: `BoostingExperience/Builds/macOS/VRExperienceAGB.app`.
- Delivered copy: `artifacts/macos-desktop/VRExperienceAGB.app`.
- Ad-hoc signing and recursive signature verification passed for the delivered bundle. This is local signing, not notarization.
- Executable is Mach-O arm64; SHA-256: `5451ed52648e1781bbc8ab9e6a5d161e2d056882dba926ab9ef7b90bc8e61aea`.
- The icon referenced by Info.plist exists; ICNS SHA-256: `dca468d88a8cfb5acd95daa80d4fa7cd2682d1d354c6fcb7c3c3fb669566f742`.
- The previous delivered app is retained as `artifacts/macos-desktop/VRExperienceAGB-before-main-2026-10-26.app`. The installed copy in Applications was not replaced.

The rebuilt Mac player was not given a new standalone interaction acceptance run in this task. Earlier desktop checks remain documented in the Mac and icon records. Launch the new `.app` directly or replace the installed copy after quitting it.

## Evidence

Ignored `artifacts/merged-builds-2026-10-26/` contains full test results, build reports, APK verification, warnings, and Mac verification. The Editor log is `artifacts/merged-builds-2026-10-26.log`. A checksum file is alongside the APK. All build outputs remain local and ignored by Git.
