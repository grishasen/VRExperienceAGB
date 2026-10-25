# macOS desktop prototype

## Baseline and plan

- Quest baseline commit: `07bd682`, annotated tag `quest-before-macos-2026-10-25`.
- Desktop branch: `codex/macos-desktop`, in an isolated managed worktree.
- Preserved Quest APK SHA-256: `fcef86987f52da4cfead4db981db41bed217140d3fb1e20adc1361a9802a42c8`.
- Keep Unity 6000.6.3f1 and the existing package manifest/lock.
- Enable the ordinary observer camera only for the Mac player and Editor preview.
- Add keyboard navigation, mouse selection/look, table controls and text entry.
- Add an AppKit file picker for the existing JSON/ZIP importer.
- Build an Apple Silicon `.app`, verify standalone behavior, run regression tests, and record actual outcomes.

## Controls

WASD walks in the forest. In Table View, A/D rotates and W/S changes distance; Q/E changes size. Right-drag looks around, left-click selects, Esc opens/closes the menu, R centers the scene, F12 captures a screenshot, and F1 toggles the screen hints. Text entry supports typing, backspace, Enter, Escape and Cmd+V. Existing on-screen controls remain available.

## Build

Run `bash scripts/build-macos-native.sh` on macOS with Apple Command Line Tools. It compiles the native picker for Apple Silicon and Intel; its generated dylib is ignored, while Unity importer metadata is tracked. In Unity, select macOS and run **VRExperienceAGB > Desktop > Build macOS**. The builder selects Apple Silicon, Mono, Medium managed stripping and Metal, disables XR startup for Standalone, and writes `BoostingExperience/Builds/macOS/VRExperienceAGB.app`. Shared window/product settings and preloaded assets are restored after the build. Android settings and evaluation code are not changed.

After building, run `bash scripts/sign-macos-local.sh`. Unity can thin packaged native plugins without refreshing their existing signatures; this script ad-hoc signs the completed local bundle and verifies it recursively. This is not Developer ID signing or notarization.

The initial unstripped build failed because unused Meta modules referenced Microphone. Medium stripping removed those unused references and the next build succeeded without adding microphone permission copy. Automatic desktop keyboard controls are installed only in the macOS player; the existing Editor mouse preview is retained, and keyboard controls can be explicitly added for testing.

The development app is for local testing. Distribution signing/notarization and Intel execution are outside this first prototype. Local model resources remain ignored and must not be published inadvertently.

## Verification

- 218/218 EditMode tests passed.
- Full PlayMode rerun: 147 passed, zero failed, two opt-in video tests skipped (149 total). This includes four new Desktop integration tests. The first full run exposed automatic desktop activation in legacy fixtures; restricting activation to the player and guarding navigation availability resolved it.
- Apple Silicon development player built and launched with Metal, outside the Editor, with all 100 default trees.
- The final standalone acceptance probe passed 13 checks with no captured errors: ordinary camera/disabled tracked rig, installed desktop input, advancing frames, real Input System mouse selection and keyboard walking, menu blocking, manual decisions, complete prepared-profile and A/B evaluation, table rotation without observer motion, preserved final scores, keyboard text/Enter search, and synthetic ZIP model/profile import.
- Menu, forest, manual tree, table results and library screenshots were inspected. Desktop hints were moved to the upper-left corner to avoid covering the source legend.
- Recursive local code-signature verification passed after ad-hoc signing.
- Android/Quest regression APK built with IL2CPP and the existing Android configuration: `Succeeded`, zero errors and two warnings. It was not installed or launched on a headset. A first build exposed an OpenXR validator exception because Unity's selected build-target group remained Standalone after the active target changed to Android; selecting the matching Android group resolved it on the next build.
- The regression APK signature verifies with Unity's bundled `apksigner`; SHA-256: `4d2e3723490124266a53aabd946e6df097f2e528985f069e9303a42be194fc82`. It is stored at `artifacts/builds/VRExperienceAGB-extensions.apk` in the worktree, separately from the preserved baseline.
- `python3 scripts/verify-examples.py` passed. Package manifest and lock retain their original versions.
- Evidence: ignored `artifacts/editmode.json`, `artifacts/playmode-final.json`, `artifacts/mac-smoke-final/` and `artifacts/mac-editor.log` in the desktop worktree.

Run the opt-in player check with `-desktop-smoke <new-empty-output-directory>`. It writes `result.json` and screenshots, imports a small synthetic package into this desktop app's local library, then exits. The app must remain focused for the mouse/keyboard checks. The probe is inert in the Editor, on Quest, and in non-development players.

The local deliverable is copied to `/Users/gregory/Documents/VRExperienceAGB/artifacts/macos-desktop/VRExperienceAGB.app`. It is an Apple Silicon development build. The copied bundle passes recursive signature verification. Its main executable SHA-256 is `ac07cbafd57b0f45a1ba561d8123df3cb41a4718cfcbc55ba38d5383a1146312`.

Native dialog selection/cancellation, Intel Macs, other macOS versions, distribution notarization and physical headset regression have not been accepted. Existing documented scoring/calibration limitations remain unchanged.
