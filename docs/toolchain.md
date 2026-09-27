# Toolchain and verification status

Updated: 2026-09-27. “Proposed” is not “installed”, and “installed” is not “verified on Quest”.

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

## Proposed baseline

| Component | Initial policy | Exact verified version |
| --- | --- | --- |
| Unity Editor | Stable Unity 6.3 LTS Apple Silicon patch compatible with selected SDKs | Pending M1 |
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

After M1, the generated `unity/VRExperienceAGB/ProjectSettings/ProjectVersion.txt` and `Packages/packages-lock.json` define exact versions. Keep them in Git. Change dependencies as an explicit maintenance task, then repeat the affected validation.

## First verified configuration record

Complete this after a real device run:

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
