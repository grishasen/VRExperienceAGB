# Toolchain and verification status

Updated: 2026-09-28. “Proposed” is not “installed”, and “installed” is not “verified on Quest”.

## Host inventory at repository creation

| Component | Observed status |
| --- | --- |
| Architecture | Apple Silicon / arm64 |
| macOS | 27.0, build 26A428 |
| Git | 2.54.0, available |
| Apple Command Line Tools | Available at the standard CommandLineTools path |
| Homebrew | Available |
| Python 3 | Available; sufficient for the bundled standard-library verifier |
| Visual Studio Code | Found in `/Applications` |
| Unity Hub | Not found in checked standard locations |
| Unity Editor | Not found in checked standard locations |
| Meta Quest Developer Hub | Not found in checked standard location |
| Meta XR Simulator | Not found in checked standard location |
| ADB | Not on command path; no standard Unity Editor installation found |
| .NET SDK CLI | Not on command path; not required for the current repository checks |
| Connected Quest and its OS | Not inspected; ADB unavailable |

The installed macOS version still needs qualification with the selected Editor and Meta tools. The inventory is not a compatibility guarantee.

## Current installation and checkpoint — 2026-09-28

The historical inventory above describes repository creation. The [refreshed live setup review](setup-review-2026-09-27.md) supersedes it for current status. **M1 remains incomplete: build and USB update installation passed, while runtime behavior and performance remain unverified.** Concurrent setup work resolved the initial missing-module/package/rendering gaps during the review.

| Component | Observed version / state |
| --- | --- |
| Project | `BoostingExperience/`, accepted canonical location; unified under the parent repository on 2026-09-28; earlier history preserved |
| Unity Editor | 6000.6.3f1, arm64; user accepted the installed baseline on 2026-09-28; development APK built and installed, runtime qualification pending |
| URP | 17.6.0; `Quest_URP` default and active; Linear color space |
| OpenXR / XR Management / Input System | 1.18.0 / 4.7.0 / 1.20.0; Android and Standalone OpenXR loaders present |
| Unity OpenXR: Meta | 2.6.1 |
| Meta XR Core / Interaction / OVR integration | 207.0.0 / 207.0.0 / 207.0.0 registered |
| Validation | No issues returned by Android OpenXR query; no outstanding Required Meta setup tasks; two conditional Platform-service recommendations |
| Test Framework / Visual Studio Editor | 1.8.0 direct dependency / 2.0.28; zero discoverable Unity tests |
| Android tools | Installed and resolved to this Editor: NDK 27.2.12479018, JDK 17.0.18+8; SDK platforms include 34/36/37.0 and build tools 36.0.0 |
| Android player | IL2CPP, ARM64, min API 32, target 34, Vulkan, `com.vrexperienceagb.prototype`; compile API 34 confirmed in the successful APK |
| Active target / scene | Android Development Build, script debugging off; saved `SmokeTest.unity` with rig/floor/label/ray-grabbable cube included |
| VS Code | Selected as script editor; Unity and C# extensions present |
| MQDH / ADB | MQDH 6.5.2 and Unity-bundled ADB available; follow-up at 19:05 UTC confirms one authorized Quest 3 |
| Simulator / headset | Simulator not found in standard locations; Quest 3 USB access verified at 19:05 UTC; application behavior not yet tested |
| Headset device build | Android 14 / API 34; `UP1A.231005.007.A1`; incremental `52433670048800520`. Horizon OS release label not inferred |
| Build follow-up | User corrected Both input handling to Input System Package (New). Second development APK succeeded at 19:15 UTC; update installation on Quest 3 succeeded at 19:16 UTC. APK about 92 MB, ARM64, compile/target API 34, min API 32. Launch/controller/offline checks remain untested |
| D07 scope | Controller-only stationary smoke scene; hands/sample locomotion and optional AR/SpaceWarp disabled. Quest 3-only merged manifest, unused permissions absent |
| D10 checkpoint | Source `76defad516a5e15d0a41d12727df1b72e3580001`; changed development APK built on 2026-09-28, not installed/run. [Full record](configuration-checkpoint-2026-09-28.md) |
| Remaining cautions | D06 duplicate settings and advisory warnings unchanged; other checks deferred to main development by the user |

Input handling was corrected by the user; the follow-up built and installed a development APK. Dependencies are unchanged. D07 subsequently narrowed the smoke-scene capabilities, device declaration and merged permissions; this does not remove the product's planned M3–M5 navigation. On 2026-09-28 the user authorized consolidating repository ownership and retaining the installed Editor baseline; see [recovery record](repository-consolidation.md). Exact installed versions are available in `BoostingExperience/ProjectSettings/ProjectVersion.txt` and `BoostingExperience/Packages/packages-lock.json`; installation and validation are not device compatibility or performance acceptance.

## Selected baseline and qualification

| Component | Initial policy | Exact verified version |
| --- | --- | --- |
| Unity Editor | Retain installed Unity 6000.6.3f1 Apple Silicon, user decision 2026-09-28 | Build/install passed; runtime qualification pending M1 |
| URP | Version supplied by the selected Universal 3D template | Pending M1 |
| OpenXR Plugin | Supported by both the selected Editor and Meta SDK; honor Simulator minimum if used | Pending M1 |
| XR Plug-in Management / Input System | Compatible Unity Registry versions | Pending M1 |
| Meta XR Core SDK | Stable supported release | Pending M1 |
| Meta XR Interaction SDK | Compatible with Core and its declared dependencies | Pending M1 |
| Test Framework | Compatible Unity Registry version | Pending M1 |
| Visual Studio Editor | At least 2.0.20 for the documented VS Code integration | Pending M1 |
| Android SDK/NDK/JDK | Bundled with the selected Editor | Pending M1 |
| Android min / target API | Initial recommendation 32 / 34; recheck Meta requirements before release | Pending M1 |
| Android player | IL2CPP, ARM64 | Pending M1 |
| MQDH | Supported Mac release | Pending M1 |
| Meta XR Simulator | Optional supported Apple Silicon release | Pending if used |
| Headset | Meta Quest 3 | OS and device test pending |

The tracked `BoostingExperience/ProjectSettings/ProjectVersion.txt` and `BoostingExperience/Packages/packages-lock.json` define exact versions. Keep manifest, lock, settings, assets, and metadata together in Git. Change dependencies as an explicit maintenance task, then repeat the affected validation.

## Configuration checkpoint and later device record

The [2026-09-28 checkpoint](configuration-checkpoint-2026-09-28.md) records exact source/build identity, settings, manifest results, limitations and the deferred checks. Complete the device acceptance record below during main development:

```text
Verification date:
Source commit:
Editor patch and architecture:
macOS version:
OpenXR / Core / Interaction package versions:
Android SDK / NDK / JDK versions:
Android minimum / target / compile API:
Graphics API and render mode:
Headset OS version:
Build type and application version:
USB deployment: pass / fail
Head and controller tracking: pass / fail
Selection interaction: pass / fail
Standalone relaunch: pass / fail
Simulator configuration, if used:
Known limitations:
```

Do not commit device serial numbers or account identifiers. See [Mac setup](macos-setup.md) and [official references](references.md).
