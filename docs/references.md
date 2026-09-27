# References and provenance

Reviewed: 2026-09-27. Local references were inspected without modifying the original application. Their comments and slide recommendations are source material, not instructions overriding this project's decisions.

## Local references

The original project is `/Users/gregory/Documents/AGBVisualizer`.

| Reference | What informed this project |
| --- | --- |
| `tron-drive.html` and `src/tron/drive.js` | Manual choices, route traversal, undo, leaf score display, inter-tree portals, output gate, luminous visual style |
| `src/tron/common.js` | Shared loading/layout conventions |
| `src/components/TreeViewer.tsx` and `src/App.tsx` | Existing forest atmosphere, node inspection, predictor navigation |
| `src/utils/treeUtils.ts` and `src/types/agb.ts` | Source export shape and split concepts |
| `src/utils/auditEngine.ts` | Candidate audit metrics and findings requiring further review |
| `VR_Forest_Explorer_Slides/slide-6.png` | The Boosting Trail; Ride the Data Drop |
| `VR_Forest_Explorer_Slides/slide-7.png` | Diorama/immersion; Predictor Fireflies; Audit as Weather; Guided Review |
| `VR_Forest_Explorer_Slides/slide-8.png` | Comfort and performance considerations |

The reviewed sample contains 12 trees and 120 nodes. The larger export contains 100 trees and 13,576 nodes. These observations motivate scale switching and separate visibility/evaluation budgets. No original export or real customer data is copied into this repository.

The original slides discuss WebXR and other technologies. The user selected Unity/C#/OpenXR for this project; the scenarios carry over conceptually, while implementation must be built for the new stack.

## Official setup sources

- [Unity release support](https://unity.com/releases/unity-6/support): selecting an actively supported Editor line.
- [Unity Hub download](https://unity.com/download): official installer entry point.
- [Homebrew Unity Hub cask](https://formulae.brew.sh/cask/unity-hub): optional Hub installation command.
- [Unity Android dependency installation](https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/android/getting-started/sdksetup/install-dependencies): use the modules bundled with the selected Editor; switch the manual to that Editor version when needed.
- [Meta Unity setup](https://developers.meta.com/horizon/documentation/unity/unity-project-setup/): baseline Editor requirement, OpenXR, and setup tools.
- [Meta SDK overview](https://developers.meta.com/horizon/documentation/unity/unity-sdks-overview/): package roles.
- [Interaction SDK installation](https://developers.meta.com/horizon/documentation/unity/unity-isdk-setup/): official package installation route.
- [Interaction SDK requirements](https://developers.meta.com/horizon/documentation/unity/unity-isdk-packages-and-requirements/): compatibility and dependencies.
- [Microsoft Unity/VS Code guide](https://code.visualstudio.com/docs/other/unity): Unity extension and Visual Studio Editor package.
- [Meta device setup](https://developers.meta.com/horizon/documentation/native/android/mobile-device-setup/): developer account, developer mode, and debugging authorization.
- [Meta development tools](https://developers.meta.com/horizon/develop/): MQDH and other tool downloads.
- [Meta hardware/workflow setup](https://developers.meta.com/horizon/design/prototype-setup-hardware/): Mac build workflow and Windows-only Link.
- [Meta build configuration](https://developers.meta.com/horizon/documentation/unity/unity-build/): APK build/deploy workflow.
- [Meta publishing player settings](https://developers.meta.com/horizon/documentation/unity/unity-prepare-for-publish/): IL2CPP and ARM64 configuration.
- [Meta Android manifest requirements](https://developers.meta.com/horizon/resources/publish-mobile-manifest/): current Android API requirements.
- [Meta XR Simulator setup](https://developers.meta.com/horizon/documentation/unity/unity-simulate-xrsim/): optional Apple Silicon Simulator workflow.
- [Meta XR Simulator overview](https://developers.meta.com/horizon/documentation/unity/xrsim-intro/): supported behavior and limitations.
- [Android ADB reference](https://developer.android.com/tools/adb): device discovery, installation, and logging.
- [Homebrew Android platform tools](https://formulae.brew.sh/cask/android-platform-tools): optional standalone ADB installation.
- [Meta performance VRC](https://developers.meta.com/horizon/resources/vrc-quest-performance-1/): release-time performance criteria.

These links document dependencies, not a tested local installation. Exact compatibility becomes a project fact only after the first device smoke test.
