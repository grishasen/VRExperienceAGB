# Windows desktop prototype

## Scope

The `codex/windows-desktop` branch starts from the macOS desktop implementation at `56ae3e1`. The Quest checkpoint remains `quest-before-macos-2026-10-25`. Windows uses the same ordinary observer, forest/table navigation, manual exploration, complete prepared-profile evaluation, and A/B comparison. Evaluation code and package versions are unchanged.

This is a Windows x64 development player using Unity 6000.6.3f1, Mono, Medium managed stripping, and Direct3D 11. The Mac host has the matching Windows Build Support (Mono) module. No headset or desktop XR runtime is required; Standalone XR initialization is disabled during the build.

## Run

Extract the entire `VRExperienceAGB-Windows-x64.zip` archive on the Windows PC, then open `VRExperienceAGB/VRExperienceAGB.exe`. Keep `UnityPlayer.dll`, `MonoBleedingEdge`, `VRExperienceAGB_Data`, and all other supplied files beside the executable. Do not run directly inside the ZIP or copy only the EXE. Unity Editor is not needed. This portable development package has no installer or Windows code-signing certificate.

Controls:

- WASD: walk in the forest.
- Hold the right mouse button and move, or hold Alt and move: look around.
- Left-click: select; mouse wheel or trackpad scrolling: scroll lists.
- Table View: A/D rotate, W/S change distance, Q/E resize.
- Esc: menu; R: center; F1: toggle hints; F12: screenshot.
- Text entry: type, Backspace, Enter to apply, Esc to cancel, Ctrl+V to paste.
- Alt+F4: quit.

Use Models / profiles to select a ZIP package or JSON file through the Windows file dialog; profile imports also accept JSONL. The picker supports Unicode paths, cancellation, and reports native dialog errors. It restores the working directory after closing. Existing import validation still applies. Native interop follows Microsoft's [OPENFILENAMEW documentation](https://learn.microsoft.com/en-us/windows/win32/api/commdlg/ns-commdlg-openfilenamew).

## Rebuild

Install Windows Build Support (Mono) for the pinned Editor, open `BoostingExperience`, and select Windows x64 in Build Profiles. Run **VRExperienceAGB > Desktop > Build Windows x64**. Output: `BoostingExperience/Builds/Windows/VRExperienceAGB/VRExperienceAGB.exe`. `WindowsDesktopBuild.Build` can also be invoked through Unity's batch `-executeMethod` option, with the Windows target selected. The build restores the shared player settings, Standalone scripting settings, graphics settings, preloaded assets, and previous XR initialization setting afterward.

Package the complete `VRExperienceAGB` output directory, excluding any Unity-generated `BackUpThisFolder_ButDontShipItWithYourGame` directory. Build outputs and local bundled model resources remain ignored by Git. Do not publish this local-data package without reviewing its bundled data.

## Verification

- Windows x64 build succeeded on macOS with zero errors and two warnings. The warnings concern disabled Pipeline tooling in the player and a deprecated debug-symbol pragma in the bundled TextMeshPro shader.
- The executable and UnityPlayer.dll are verified as Windows PE32+ x86-64 binaries. Required managed assemblies, data, and Mono runtime are present.
- Four desktop PlayMode tests and six library PlayMode tests passed in the Mac Editor with Windows selected. These are Editor checks, not Windows runtime acceptance.
- The native dialog structure has the expected 64-bit ABI size (152 bytes), with file pointer at offset 48 and flags at offset 96. Actual OS dialog interaction is still untested.
- `python3 scripts/verify-examples.py` passed. Package manifest/lock, shared project settings, and Quest rendering settings retain their prior tracked values.
- The first build after installing the module into a running Editor incorrectly reported success without producing the player, with missing platform settings and postprocess errors. Restarting the Editor loaded Windows support fully; the next build succeeded with actual output files. The builder now rejects reports with errors or missing executable/runtime files.
- Evidence: ignored `artifacts/windows-desktop-tests.json`, `artifacts/windows-library-tests.json`, `artifacts/windows-build-report.json`, and `artifacts/windows-editor.log` in the worktree.

The local package is `/Users/gregory/Documents/VRExperienceAGB/artifacts/windows-desktop/VRExperienceAGB-Windows-x64.zip` (111,678,672 bytes, 173 files). ZIP integrity and required entries passed verification; Unity's backup directory is excluded. SHA-256: `b499cb39a2a2d4cc06fe0bda1f0911c2d1e2ad6c1a7c329f281917c5c7e0e463`. An extracted copy and checksum file are alongside the ZIP.

Windows runtime rendering, native file selection/cancellation, and physical keyboard/mouse behavior require a Windows PC and have not been verified on this Mac. The existing delivered Mac app and preserved Quest APK were not rebuilt or replaced by this Windows task.

For the development player's opt-in acceptance probe, run the following from its extracted directory, using a new output directory on each run:

```powershell
.\VRExperienceAGB.exe -desktop-smoke "$env:TEMP\VRAGB-Windows-Smoke-01" -logFile "$env:TEMP\VRAGB-Windows-Smoke-01.log"
```

Keep the window focused until it exits. The probe records `result.json` and screenshots, checks complete model playback and A/B scores, and imports a synthetic package into the local library. This probe does not exercise the native file dialog; check both selection and cancellation separately, including a path containing spaces and non-ASCII characters.
