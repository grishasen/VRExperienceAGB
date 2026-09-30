# Asset provenance register

Updated: September 30, 2026. Scope: the teaching scene and M5 runtime views. This is an inventory of observed sources and bundled notices; it is not a legal determination or a distribution approval. No new third-party art, font, audio or package was introduced for M5.

| Included content | Source / version | Evidence and distribution follow-up |
| --- | --- | --- |
| Pine, trunk, rock, stone and ring meshes; materials; sky, bark and moon shaders | Original project-generated geometry and authored Unity materials under `Assets/VRExperienceAGB/Art/Moonlit/` | Generation code in `Editor/MoonlitClearingStyler.cs`; implementation history in [moonlit design record](design/moonlit-clearing.md). M5 reuses these shared meshes and materials. The project has not selected a public source license. |
| `SlateAlbedo.png`, `PineFoliage.png`, `TwilightPanorama.png` | Generated with built-in ImageGen for this project | Source descriptions and actual use recorded in [moonlit design record](design/moonlit-clearing.md#art-assets). These are generated content, not downloaded stock assets. The concept PNG in `docs/design/` is reference material, not a runtime texture. |
| M5 breeze and selection cue | Original deterministic procedural audio in `Runtime/Presentation/ForestAtmosphere.cs` | Runtime PCM generation, with no external audio file or sample. One eight-second mono loop and one short positional cue; both can be muted. |
| Liberation Sans font and SDF assets | TextMesh Pro Essential Resources; source font under `Assets/TextMesh Pro/Fonts/` | The tracked `LiberationSans - OFL.txt` includes Google (2010), Red Hat (2012), reserved names and SIL OFL 1.1. Retain that complete notice with any distribution containing the font; do not substitute a bare attribution. |
| TextMesh Pro shaders/resources and uGUI | Installed `com.unity.ugui` 2.0.0 and imported Essential Resources | Installed `com.unity.ugui` `LICENSE.md` states Unity Companion License. Exact dependency is pinned in `Packages/packages-lock.json`. Keep upstream notices and review distribution scope during M6. |
| Universal Render Pipeline rendering | Installed `com.unity.render-pipelines.universal` 17.6.0 | Pinned manifest/lock; retain installed package license/third-party notices in the release evidence. No M5 package update. |
| Controller rig, canvas pointer surfaces, haptics | Meta XR Core, Interaction and Interaction OVR 207.0.0 | Installed packages contain `LICENSE.md` pointing to the Oculus SDK License Agreement. The rig/pointer assets are SDK content, not original project art. Preserve upstream notices; verify the final release's distribution terms in M6. |
| OpenXR integration | Unity OpenXR 1.18.0; Meta OpenXR 2.6.1 | Existing pinned dependencies and upstream notices; unchanged by M5. |
| Verification-only Pipeline tools | `com.unity.pipeline` 0.8.0-exp.1 | Editor tooling; no new runtime service added. Build dependency evidence must distinguish tooling from shipped content. |
| Model data | Existing user-selected 50-tree structure preview; separate fictional teaching fixtures | Source identity and explicit preview limits are in [simplified navigation](m4-simple-navigation-2026-09-30.md) and [model contract](model-contract.md). Model approval does not establish production scoring fidelity. M5 adds no customer profile or model export. |

## Release credits and evidence

The M6 presenter package must retain the complete bundled font notice and relevant SDK/package notices, record the actual build's used assets, and distinguish generated art, original code/geometry, and package content. The project/source license and external distribution remain separate decisions. No unverified rights are asserted here.

The M5 source changes introduce no downloaded media or external asset requiring a new purchase or account. Runtime scenery keeps the existing moon, distant conifers and restrained materials; the diorama shares those assets. Detailed logs, screenshots and APKs stay under ignored `artifacts/`.
