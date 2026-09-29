# Windows development setup

Prepared on 2026-09-28 for **Windows → Android APK → standalone Meta Quest 3**. This guide uses the existing project and its pinned dependencies. **Windows installation, Editor compilation, APK builds, and headset behavior have not been verified on a Windows machine.** Existing Mac results in [Toolchain](toolchain.md) do not establish Windows compatibility.

## 1. Install the host tools

Use a Windows 11 x64 PC as the initial setup target and check the selected Editor's release requirements before installation. Allow roughly 40–60 GB of free SSD space for the Editor, Android modules, project cache, and builds; this is a planning allowance, not a vendor minimum. Have a Quest 3, controllers, a USB data cable, a Unity account, and a Meta developer account ready.

Install:

- [Git for Windows](https://git-scm.com/downloads/win).
- [Unity Hub](https://unity.com/download), then sign in and activate the appropriate Unity license.
- [Visual Studio Code](https://code.visualstudio.com/).
- [Python 3 for Windows](https://www.python.org/downloads/windows/) for the synthetic fixture verifier.
- [Meta Quest Developer Hub (MQDH)](https://developers.meta.com/horizon/develop/) for device management, deployment, and logs.

Use native Windows tools and PowerShell for this guide. WSL and Android Studio are not required. Keep the checkout in a short local path such as `C:\Dev\VRExperienceAGB`, outside OneDrive or other synchronized folders.

## 2. Obtain the existing repository

Use an available repository copy or an explicitly configured remote. The project currently documents a local repository; no public clone URL is assumed. When transferring from the Mac, close Unity and preserve the repository, `Assets`, `.meta` files, `Packages`, and `ProjectSettings`. Do not transfer generated `Library`, `Temp`, or build caches. A Git clone transfers committed content only, so account for any work that has not yet been committed before changing machines.

In PowerShell, substitute your actual checkout path:

```powershell
Set-Location 'C:\Dev\VRExperienceAGB'
git status --short
Get-Content '.\BoostingExperience\ProjectSettings\ProjectVersion.txt'
py -3 --version
py -3 .\scripts\verify-examples.py
```

If `py` is unavailable but Python 3 is installed as `python`, use `python --version` and `python .\scripts\verify-examples.py`. The verifier uses the Python standard library; no packages are required. It checks synthetic fixtures, not Unity compilation or real AGB scoring.

`scripts/check-mac.sh` is macOS-specific. Running it through Git Bash or WSL does not validate a Windows installation; use the checks and completion checklist below.

## 3. Install the pinned Unity Editor and Android modules

The current `ProjectVersion.txt` pins **6000.6.3f1**. Install the **Windows Editor for that exact version** through Hub or the [Unity download archive](https://unity.com/releases/editor/archive). Apple Silicon is the existing Mac host architecture, not a Windows installation requirement. Do not upgrade or downgrade the project just because Hub suggests another release. If the exact Windows installer is unavailable, record that blocker before choosing a different baseline.

In Hub, add these modules to the selected Editor:

- Android Build Support
- Android SDK & NDK Tools
- OpenJDK

In Unity, open **Edit → Preferences → External Tools** and use that Editor's bundled Android SDK, NDK, JDK, and Gradle. Do not substitute unrelated system installations to resolve setup errors. See [Unity's Android dependency instructions](https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/android/getting-started/sdksetup/install-dependencies); select the manual matching the installed Editor when available.

## 4. Open the project and configure the code editor

In Unity Hub, add/open `C:\Dev\VRExperienceAGB\BoostingExperience`. The parent repository and the `unity` placeholder directory are not Editor projects. Do not create another project or nested Git repository.

Allow package resolution and the initial asset import to finish. Preserve `Packages/manifest.json` and `Packages/packages-lock.json`; these already define the Unity and Meta dependencies, including the Meta registry. Do not install a second SDK bundle or replace packages with the latest releases as part of host setup. Resolve authentication or registry connectivity errors before editing dependencies.

Current reference versions are Unity OpenXR 1.18.0, URP 17.6.0, and Meta XR Core/Interaction 207.0.0. The tracked manifest and lock take precedence if the project changes. These versions are recorded project dependencies, not a claim of Windows qualification.

Install Microsoft's **Unity** extension in VS Code. Its dependencies provide C# tooling. In Unity's External Tools preferences, select Visual Studio Code as the script editor and regenerate project files if needed. The project already pins Visual Studio Editor 2.0.28, above the 2.0.20 minimum described in [Microsoft's Unity guide](https://code.visualstudio.com/docs/other/unity). Do not add the legacy Visual Studio Code Editor package. Follow any supported .NET tooling installation prompt; that runtime does not replace Unity's compiler.

Keep text asset serialization and visible `.meta` files enabled. Review `git status` after import: host setup should not silently upgrade dependencies or rewrite shared configuration. Preserve existing work when reviewing changes.

## 5. Connect and authorize Quest 3

1. Create or join a Meta developer team and complete the required account verification.
2. Pair the headset with the Meta Horizon mobile app and enable Developer Mode.
3. Install the official [Oculus ADB Drivers for Windows](https://developers.meta.com/horizon/downloads/package/oculus-adb-drivers/) following the package instructions. Windows needs this device-driver setup; the Mac guide omits it.
4. Connect the awake headset directly to the PC with a USB data cable.
5. Put on the headset and accept USB debugging authorization for this PC. Authorization of the Mac does not authorize Windows.
6. Check that MQDH recognizes the connected headset.

Follow [Meta's device setup guide](https://developers.meta.com/horizon/documentation/native/android/mobile-device-setup/) for current account and driver steps. File-transfer access alone does not establish ADB authorization.

Use Unity's bundled ADB in PowerShell. Adjust the installation path if Hub uses a custom location:

```powershell
$VragbEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor'
$VragbAdb = Join-Path $VragbEditor 'Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
Test-Path $VragbAdb
& $VragbAdb version
& $VragbAdb devices -l
```

`Test-Path` must return `True`. The headset must appear with status `device`, not `unauthorized` or `offline`. Device discovery may start the local ADB daemon. Avoid committing device serial numbers or account identifiers.

## 6. Check the Android build configuration

In **File → Build Profiles**, select the project's Android/Meta Quest build target. Install missing modules through Hub if the target is unavailable. Check the existing settings against the [configuration checkpoint](configuration-checkpoint-2026-09-28.md) and current tracked project settings:

| Setting | Recorded baseline |
| --- | --- |
| Product / application ID | `VRExperienceAGB` / `com.vrexperienceagb.prototype` |
| Scripting backend / architecture | IL2CPP / ARM64 |
| Minimum / target Android API | 32 / 34 |
| Graphics / color space | Vulkan / Linear |
| Input handling | Input System Package (New) |
| XR provider | OpenXR enabled for Android, with the installed Meta features |
| Initial debug build | Development Build; script debugging off unless needed |

These are the project's recorded settings, not a statement of current store submission requirements. Recheck release requirements before distribution. Run Android OpenXR validation and inspect the Meta Project Setup Tool; review proposed changes before applying fixes that affect shared project settings.

Use the existing smoke scene at `Assets/VRExperienceAGB/Scenes/SmokeTest.unity` for the first Windows build. Confirm the intended scene is enabled in the active build scene list; later M3 work may change that list. The [M3 progress record](m3-progress-2026-09-28.md) describes the separate one-tree experience. Do not recreate the rig or change the application features just to configure another host.

Keep the controller-only smoke baseline and its permission configuration. Keep signing keys and passwords outside Git. Local development signing is sufficient for a first local install, but builds signed on different computers may not be compatible updates.

## 7. Run checks, build, and deploy

Open **Window → General → Test Runner** and run the EditMode suite. Record actual results from this Windows machine. The historical Mac result is not an expected fixed test count: tests may be added during development.

Select the connected Quest as the run device, then use **Build and Run**. Save the APK to `artifacts/builds/VRExperienceAGB-dev.apk` under the repository root. Generated builds and logs belong in ignored `artifacts/`. See [Meta's build workflow](https://developers.meta.com/horizon/documentation/unity/unity-build/).

For an existing APK, optional PowerShell deployment and Unity logs are:

```powershell
# Replace this value with the serial reported by devices -l.
$VragbSerial = 'QUEST_SERIAL'
& $VragbAdb -s $VragbSerial install -r '.\artifacts\builds\VRExperienceAGB-dev.apk'

# Launch the app inside the headset, then stream Unity logs. Stop with Ctrl+C.
& $VragbAdb -s $VragbSerial logcat -v time 'Unity:I' '*:S'
```

If startup fails, inspect unfiltered `logcat` as well; the Unity filter can hide native errors. See the [ADB reference](https://developer.android.com/tools/adb).

An update requires compatible application identity and signing. If a Windows debug build conflicts with the installed Mac-signed build, decide whether to use the appropriate existing signing material securely or remove the old installation with informed acceptance of local data loss. Do not automatically uninstall to work around the mismatch.

Inside the headset, verify launch, tracked head movement, both controllers, and selection. Disconnect USB and relaunch independently. Compilation and installation alone do not pass M1. Follow [Testing](testing.md) for behavior and performance acceptance; measure final performance on Quest with a release build.

## 8. Optional desktop iteration

The initial workflow needs only native Windows Unity, Android tools, and USB deployment. Meta XR Simulator or a supported Meta Horizon Link setup can be configured separately for desktop iteration. Check their current OS/GPU requirements and the installed SDK's instructions before changing the desktop OpenXR runtime. See [Meta's workflow guide](https://developers.meta.com/horizon/design/prototype-setup-hardware/) and [Simulator setup](https://developers.meta.com/horizon/documentation/unity/unity-simulate-xrsim/).

Record the chosen desktop runtime and graphics configuration separately from Android settings. Desktop playback does not verify standalone rendering, permissions, offline relaunch, or Quest performance. No Simulator or Link session has been validated by this Windows guide.

## 9. Troubleshooting

| Symptom | Action |
| --- | --- |
| Hub requests a different Editor | Check `ProjectVersion.txt`; install the exact Windows version before opening |
| Android target or ADB missing | Add Android modules to that exact Editor; verify its install path |
| No headset in ADB | Check the Windows ADB driver, Developer Mode, direct USB connection, and data cable |
| `unauthorized` | Put on the awake headset and accept this PC's debugging prompt |
| `offline` | Reconnect the headset and check authorization; avoid competing ADB installations |
| Android SDK/NDK/JDK error | Restore bundled paths; inspect the first build error rather than replacing tool versions |
| Registry/package resolution failure | Check connectivity, proxy, account access, and the existing Meta scoped registry |
| Python opens Microsoft Store | Check the Python installation and use the working `py -3` or `python` command |
| Long-path or locked-file errors | Use a short local checkout outside sync folders and close processes holding the files |
| APK signature mismatch | Resolve signing deliberately; uninstalling can delete local application data |
| Flat-screen launch or missing input | Check Android OpenXR, scene list, rig, and controller configuration |
| IntelliSense missing | Check Microsoft's Unity extension, Visual Studio Editor package, and regenerated project files |

## 10. Windows setup completion record

- [ ] Windows version, CPU architecture, and GPU/driver recorded.
- [ ] Exact pinned Windows Editor and its Android modules installed.
- [ ] Existing project imports and compiles without errors.
- [ ] Package resolution completes without unintended dependency changes.
- [ ] Python fixture verifier and Unity EditMode results recorded.
- [ ] Android validation reviewed and required issues resolved.
- [ ] Quest visible and authorized through ADB.
- [ ] APK builds and installs from Windows.
- [ ] Headset launch, tracking, and controller interaction verified.
- [ ] Standalone relaunch verified after USB disconnect.
- [ ] Actual results and remaining failures added to [Toolchain](toolchain.md).

Record the date, source commit, Windows version, Editor/package versions, Android tool versions, headset OS, build type, and each check's pass/fail/not-tested result. Preserve package manifest, lock, project settings, assets, and metadata together when intentional changes have been generated and verified. Keep real profiles and exports in ignored `data/private/`; use synthetic fixtures for shared setup checks.
