# Mac development setup

Checked against official documentation on 2026-09-27. Follow the linked release notes when a package's current requirements differ from this guide. The intended workflow is **Mac → Android APK → standalone Quest 3**.

## 1. Current machine and prerequisites

The initial read-only check found Apple Silicon (`arm64`), macOS 27.0, Git, Apple Command Line Tools, Homebrew, Python 3, and Visual Studio Code. Unity Hub, a Unity Editor, Meta Quest Developer Hub, and Meta XR Simulator were not found in the checked standard application locations. `adb` and `dotnet` were not on the command path. Nonstandard installations may exist.

This inventory does not certify Unity or Meta compatibility with the installed macOS version. Confirm that the chosen releases support the host OS, then prove the combination with M1's Editor and device smoke tests. Do not change macOS as part of routine setup.

Have a Quest 3, charged controllers, a USB data cable, a Unity account, and a Meta developer account available. Reserve sufficient SSD space for the Editor, Android modules, project cache, and builds; 40–60 GB free is a planning allowance rather than a vendor minimum. A Mac with 16 GB RAM is a reasonable starting point; 32 GB is more comfortable.

Start in the repository:

```sh
cd /Users/gregory/Documents/VRExperienceAGB
bash scripts/check-mac.sh
```

The check may fail until setup is complete. It does not install or alter anything. Optional `--devices` additionally invokes ADB device discovery, which may start its local daemon.

## 2. Install Unity Hub and the Editor

Install Unity Hub using the [official download page](https://unity.com/download), or the [Homebrew cask](https://formulae.brew.sh/cask/unity-hub) if you use Homebrew:

```sh
brew install --cask unity-hub
open -a 'Unity Hub'
```

Sign in and activate the Unity license appropriate to your use. Select a stable **Unity 6.3 LTS Apple Silicon** editor patch as the initial candidate. Record the exact patch in [Toolchain](toolchain.md). Unity 6.3 is an actively supported LTS line; this is a project choice, not a claim that every SDK combination has already been tested. [Unity release support](https://unity.com/releases/unity-6/support)

In Hub, add these modules to that same Editor installation:

- Android Build Support
- Android SDK & NDK Tools
- OpenJDK

Use the versions bundled with that Editor. In Unity Settings/Preferences → External Tools, keep the Android SDK, NDK, and JDK paths pointed to those installations. A separate Android Studio installation is not required for this workflow. [Unity Android dependencies](https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/android/getting-started/sdksetup/install-dependencies)

The reviewed Meta setup guide requires Unity 6000.0.66f2 or later and recommends 6.1 or later. The selected Editor must also satisfy the requirements of the exact Meta SDK release installed below. [Meta Unity setup](https://developers.meta.com/horizon/documentation/unity/unity-project-setup/)

## 3. Configure Visual Studio Code

VS Code is already present on the checked Mac. Install Microsoft's **Unity** extension through its Extensions view. Alternatively:

```sh
'/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code' \
  --install-extension visualstudiotoolsforunity.vstuc
```

The extension installs its declared dependencies, including C# Dev Kit. If it requests a .NET runtime/SDK for editor tooling, follow its supported setup. A separately installed .NET SDK does not replace Unity's compiler/runtime.

Inside Unity, use the **Visual Studio Editor** package version 2.0.20 or newer, even when VS Code is your editor. The old **Visual Studio Code Editor** package is legacy. Set Visual Studio Code as External Script Editor and generate project files if IntelliSense is missing. [Microsoft's Unity setup](https://code.visualstudio.com/docs/other/unity)

## 4. Prepare the headset

1. Create or join a Meta developer team and complete the account verification required for developer mode.
2. Pair the headset with the Meta Horizon mobile app using the appropriate account.
3. Enable Developer Mode for the headset.
4. Connect the Quest to the Mac with a USB data cable.
5. Put on the headset and accept the USB debugging prompt for this computer.

macOS does not need the Windows Oculus ADB driver. A charge-only cable will not work. [Meta device setup](https://developers.meta.com/horizon/documentation/native/android/mobile-device-setup/)

Install **Meta Quest Developer Hub (MQDH)** from Meta's [developer tools page](https://developers.meta.com/horizon/develop/). It is useful for device discovery, APK deployment, logs, and performance tooling. Confirm the connected headset appears in its Device Manager. Prefer a direct USB connection for the first build.

## 5. Create the Unity project in this repository

In Unity Hub, create a project using **Universal 3D**. Set:

| Setting | Value |
| --- | --- |
| Project name | `VRExperienceAGB` |
| Location (parent directory) | `/Users/gregory/Documents/VRExperienceAGB/unity` |
| Resulting project directory | `/Users/gregory/Documents/VRExperienceAGB/unity/VRExperienceAGB` |
| Editor | The candidate Apple Silicon Editor selected in step 2 |

Confirm the final path before creating it. The repository root is a documentation/scaffolding repository, not currently an Editor project. Do not ask Hub to open it as an existing Unity project.

Use Git already initialized at the repository root. Do not create another repository or enable an additional version-control service for the nested Unity directory.

When Unity opens, enable text asset serialization and visible `.meta` files in the appropriate Editor/version-control settings. Commit assets with their `.meta` files; keep `Library`, `Temp`, and local builds ignored.

## 6. Install Unity packages

Use Unity's Package Manager and Asset Store integration. These are Unity packages; do not install them with `pip`, `npm`, or Homebrew.

| Package | Installation / purpose |
| --- | --- |
| Universal RP | Supplied by the Universal 3D template; keep the Editor-compatible version |
| XR Plug-in Management | Install/enable through Project Settings → XR Plug-in Management |
| OpenXR Plugin | Unity Registry package `com.unity.xr.openxr` |
| Input System | Unity Registry package `com.unity.inputsystem`, if not already resolved by the selected packages |
| Meta XR Core SDK | Add the official Meta package through Asset Store / Package Manager |
| Meta XR Interaction SDK | Install the official Meta package and its declared dependencies |
| Unity Test Framework | Keep/install the compatible Unity Registry package for EditMode and PlayMode tests |
| Visual Studio Editor | IDE integration described in step 3 |

For Meta packages, sign in to the Asset Store, add the package to your assets, open it in Unity, and install through Package Manager. Accept the required package dependencies. The All-in-One wrapper is an alternative installation route, not an additional requirement; the initial project only needs Core and Interaction capabilities. [Meta package overview](https://developers.meta.com/horizon/documentation/unity/unity-sdks-overview/), [Interaction SDK installation](https://developers.meta.com/horizon/documentation/unity/unity-isdk-setup/)

Use matching compatible Meta SDK releases. Package numbering may differ between components, so follow declared dependencies rather than forcing every package to an identical version. Consult [Interaction SDK requirements](https://developers.meta.com/horizon/documentation/unity/unity-isdk-packages-and-requirements/).

Enable **OpenXR** for the Android/Meta Quest target. Enable its desktop target only when configuring a supported Simulator workflow. The Oculus XR Plugin is deprecated. Use the Meta XR feature group appropriate to the installed Core SDK and run the Meta Project Setup Tool for the Android target. Review required fixes and project validation results. [Meta Unity/OpenXR configuration](https://developers.meta.com/horizon/documentation/unity/unity-project-setup/)

Choose one rig and controller interaction stack from the matching Meta samples/Building Blocks. Import a minimal controller ray/select example before assembling the application scene. Hand tracking, MR Utility Kit, multiplayer, and the Meta Platform SDK are not required by the initial offline experience. Add platform services when the distribution workflow needs them.

After the first successful headset build, commit the generated `Packages/manifest.json`, `Packages/packages-lock.json`, and `ProjectSettings/ProjectVersion.txt`. They are the source of truth for exact versions. Update [Toolchain](toolchain.md) at the same time.

## 7. Configure the standalone build

Select File → Build Profiles → **Meta Quest**. If that profile is unavailable in your Editor, use **Android**. The output is an APK. [Meta build configuration](https://developers.meta.com/horizon/documentation/unity/unity-build/)

Initial project choices:

| Setting | Initial value / rule |
| --- | --- |
| Product name | `VRExperienceAGB` |
| Application identifier | `com.vrexperienceagb.prototype` for local development; confirm the final identifier before distribution |
| Scripting backend | IL2CPP |
| Architecture | ARM64; disable ARMv7 |
| Android minimum API | 32 as the initial Quest 3-only recommendation |
| Android target API | 34 for the currently documented new immersive-app requirement |
| Graphics | Use the Android configuration recommended by the installed Meta setup tools; record the resolved graphics API |
| Color space | Linear, subject to project validation |
| Initial frame target | 72 Hz with a matching sustained native rendering target |
| Debug build | Development Build for debugging; disable it for final performance acceptance |
| Script debugging | Enable only when needed |

IL2CPP/ARM64 follow Meta's [player configuration guidance](https://developers.meta.com/horizon/documentation/unity/unity-prepare-for-publish/). API recommendations are date-sensitive; recheck the [manifest requirements](https://developers.meta.com/horizon/resources/publish-mobile-manifest/) before release. Do not apply a desktop Simulator graphics setting to the Android build without checking it.

Do not add camera, microphone, storage, or network permissions unless a feature actually requires them. Keep signing keys and passwords outside Git. Local debug signing is sufficient for the first smoke test; distribution signing is a later task.

## 8. Build the first scene and run it

Create a scene containing the selected tracked rig, a floor, a readable world-space label, and a large selectable object. Add the controller selection components from the matching SDK sample. Save the scene and include it in the active build profile's scene list.

Select the connected Quest as the run device. Use **Build and Run** and save the APK under `artifacts/builds/`. The first build can take longer while Unity prepares IL2CPP and Android build caches. A successful compilation alone is not the acceptance result: the scene must appear in the headset and respond to tracked input. [Meta build workflow](https://developers.meta.com/horizon/documentation/unity/unity-build/)

Then disconnect the Mac, relaunch the installed app in the headset, and repeat the interaction. Record the tested editor/package versions, headset OS, and result in [Toolchain](toolchain.md).

## 9. Optional command-line deployment and logs

Unity includes ADB in its Android SDK. To use that copy, replace the placeholder with the exact installed Editor directory:

```sh
export VRAGB_EDITOR_DIR='/Applications/Unity/Hub/Editor/<exact-editor-version>'
export VRAGB_ADB="$VRAGB_EDITOR_DIR/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
"$VRAGB_ADB" version
"$VRAGB_ADB" devices -l
```

Alternatively, install standalone platform tools:

```sh
brew install --cask android-platform-tools
adb version
adb devices -l
```

This optional [Homebrew package](https://formulae.brew.sh/cask/android-platform-tools) provides command-line tools; keep Unity pointed at its own SDK/NDK/JDK for builds. Prefer one ADB installation during a session if tools report version conflicts.

With standalone `adb` on your path:

```sh
# Replace QUEST_SERIAL with the serial reported by adb devices -l.
adb -s QUEST_SERIAL install -r artifacts/builds/VRExperienceAGB-dev.apk

# Launch the installed application manually in the headset, then stream Unity logs.
adb -s QUEST_SERIAL logcat -v time 'Unity:I' '*:S'
```

`install -r` updates an installed package when its identity/signing are compatible. For startup crashes, inspect the unfiltered log because a Unity-only filter can hide native errors. Never uninstall automatically to solve a signature mismatch; uninstalling may remove local app data. [Android ADB reference](https://developer.android.com/tools/adb)

## 10. Simulator and the Mac iteration loop

Meta Horizon Link is Windows-only. On a Mac, use Editor-level tests, the optional **Meta XR Simulator**, and actual APK builds on Quest. [Meta development workflows](https://developers.meta.com/horizon/design/prototype-setup-hardware/)

The reviewed Simulator documentation supports Apple Silicon and requires Unity OpenXR Plugin 1.13.0 or later on macOS. Follow the current standalone Simulator installer and activation guide; do not assume an old simulator Unity package is interchangeable with the new standalone runtime. Check its supported desktop graphics setup separately from the Quest player. [Simulator setup](https://developers.meta.com/horizon/documentation/unity/unity-simulate-xrsim/), [Simulator capabilities](https://developers.meta.com/horizon/documentation/unity/xrsim-intro/)

The Simulator models XR behavior; it is not a Quest hardware or Android performance emulator. Keep it optional: it must not delay the first USB-deployed APK.

Suggested daily loop: edit C# → run relevant EditMode/PlayMode checks → inspect the scene → build to Quest after a meaningful interaction change → profile a release build when performance is affected.

## 11. Verification commands and troubleshooting

```sh
cd /Users/gregory/Documents/VRExperienceAGB
bash scripts/check-mac.sh
python3 scripts/verify-examples.py

# Optional, after enabling debugging and connecting the headset:
bash scripts/check-mac.sh --devices
```

| Symptom | First checks |
| --- | --- |
| Unity is not detected | Confirm its location; set `VRAGB_EDITOR_DIR` for a custom installation |
| Several Editors are installed | Open the version recorded by the project; the checker uses `ProjectVersion.txt` when present |
| Android target is missing | Add Android modules to the selected Editor through Hub |
| `adb` shows `unauthorized` | Put on the awake headset and accept its debugging prompt |
| No device appears | Check Developer Mode, data cable, direct connection, and MQDH |
| SDK/NDK/JDK build errors | Restore the selected Editor's bundled tool paths and read the first actual build error |
| App opens as a flat screen or fails to enter VR | Check Android OpenXR activation, supported features, scene list, rig, and generated manifest |
| Controllers do not select | Verify the matching SDK sample, controller profile, input setup, and colliders |
| VS Code has no IntelliSense | Check Microsoft's Unity extension, Visual Studio Editor package, and regenerated project files |
| Simulator cannot start | Check its current macOS/graphics requirements; continue device testing through USB |
| Editor is fast but Quest is slow | Profile the standalone release APK; desktop speed is not a mobile GPU measurement |

## 12. Setup completion checklist

- [ ] Exact candidate Editor and Meta packages installed and recorded.
- [ ] Android toolchain resolved from that Editor.
- [ ] OpenXR/project validation required issues resolved.
- [ ] Quest visible and authorized.
- [ ] First APK launches with tracked head and controller input.
- [ ] Standalone relaunch succeeds after disconnecting the Mac.
- [ ] Unity project files and package lock committed.
- [ ] Toolchain status updated with actual evidence.
