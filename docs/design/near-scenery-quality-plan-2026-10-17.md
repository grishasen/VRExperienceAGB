# Near scenery quality plan — October 17, 2026

Status: the first pass and completion pass are installed and measured on Quest 3. A feedback follow-up fixes stretched side mountains, an empty tree-hover hint and overly short branch-detail distances; its verification is recorded below. Earlier device measurements do not establish acceptance of the increased detail distances. Exact view-matched performance acceptance and the cause of a 208 ms exploration-frame spike remain open.

## Goal and scope

Improve the ground, rocks and three nearby large decorative firs while preserving the approved night atmosphere, clear model cards and standalone Quest responsiveness. Preserve all model trees, manual exploration, complete deterministic playback and tracked head/controller poses.

## Current iteration

- [x] Preserve the installed baseline APK and record its identity.
- [x] Capture matching forest entrance and nearby-fir references before changes.
- [x] Collect a 60-second Quest baseline with the app focused and headset active. Record model, view, refresh rate, timing availability and interruptions.
- [x] Improve ground artwork with soil, fine gravel, needles and restrained moss; retain mipmaps and bounded Android texture sizes. Use a cheaper planar ground path where triplanar projection is unnecessary.
- [x] Improve rock surface artwork and a small set of nearby silhouettes, retaining shared materials and avoiding new real-time shadows.
- [x] Improve three nearby decorative firs with irregular branch clusters and distance-based detail. Retain existing bark and model-bearing trees.
- [x] Inspect matching Editor images, check resource ownership and run relevant automated regressions.
- [x] Build and install the candidate, then repeat the Quest measurement with the same model, forest view, refresh and build settings.
- [x] Record before/after frame time, CPU/GPU timing when available, memory and scene geometry counts. Reject interrupted or mixed-view captures as performance evidence.
- [x] Review appearance on Quest with the user before expanding the treatment. The user reported no visible regressions and authorized the remaining improvements on October 17.

## Comparison rules

Use the same complete model, view/origin, display refresh, resolution, quality settings, audio state and Development-build configuration. Use a warm-up before each measured interval. Do not substitute Editor timing or raw triangle counts for Quest acceptance. Compare median/p95/p99 application frame time, available CPU/GPU timings and memory; equal capped FPS alone does not prove equal cost. Record thermal/focus limitations and distinguish measured results from estimates. If the headset is unavailable, keep the original APK and reproducible capture steps and explicitly leave the device comparison pending.

## Later iterations

1. Higher-detail mountain source panorama, preserving the accepted silhouette and color treatment.
2. Carefully bounded grass clumps and transitions around paths and stones.
3. Shared LOD and spatial culling refinements for farther trees, only after profiling.
4. Model-tree silhouette improvements that preserve depth/leaf-count encodings and interaction targets.

Do not add dense foliage layers, global post-processing, animated grass or per-tree real-time shadows as part of this iteration.

## Completion pass — authorized October 17

- [x] Refine mountain texture detail while preserving the approved silhouette, palette and side haze.
- [x] Add restrained grass near paths and rocks; preserve clear walking and label areas.
- [x] Replace procedural bark shading with a shared textured material and bounded trunk meshes.
- [x] Extend branch detail and LODs to all interior decorative firs and the single-tree clearing.
- [x] Improve distant forest frustum culling and shared detail levels without removing trees.
- [x] Improve model-tree crowns in the forest and table while preserving structural size encodings, all model trees, pointer targets and complete playback.
- [x] Check matching views and automated regressions; build and install on Quest.
- [x] Record the requested one-minute before/after comparison and a three-minute moving-headset capture. Query XR-provider GPU/compositor counters and record availability explicitly. The one-minute candidate included head turns, so controlled stationary acceptance remains unverified.

Accepted first-pass APK and evidence remain preserved. Completion-pass evidence is under ignored `artifacts/scenery-completion-2026-10-17/`. The device evidence below is preliminary; changes in head direction prevent a controlled attribution to scenery alone.

## Evidence

Working evidence: ignored `artifacts/near-scenery-2026-10-17/`. Baseline source commit: `4a25f8ad37a7f2da5284c29243a5366b158c72c4`. Update this record with actual image prompts, asset provenance, test results, build identities and device results as they become available.

Before the baseline, the source legend was restored to an initially expanded lower-left row, with an arrow to collapse/reopen it. Both comparison builds include this change. All nine focused card/legend checks passed before the baseline build.

Baseline APK: `VRExperienceAGB-near-scenery-baseline-2026-10-17.apk`, 119,589,645 bytes, SHA-256 `3d4b6659a404c3c413db8d59bffaf4d2523df66d1845577acc9ad774d3610aa2`. Development build, Unity 6000.6.3f1. Quest capture `2026-10-17T19:47:22.516Z`: 100 trees, Forest only, active XR, 60.0008 seconds, 4,044 frames, no interruptions. Average 67.399 FPS; median 13.889 ms; p95 18.856 ms; p99 22.334 ms; mean CPU 14.836 ms; peak Unity allocated memory 244,256,743 bytes; refresh 72 Hz. GPU timing unavailable. Earlier captures involving model switches are excluded.

Generated artwork and exact prompts: `BoostingExperience/Assets/VRExperienceAGB/Art/NearScenery/`. All three sources import at 1024 pixels with ASTC 6×6 and mipmaps. Ground sampling falls from three projections to one. Rocks use three deterministic silhouettes. Each selected fir's maximum geometry falls from 19,520 to 1,780 triangles by replacing oversized wooden spokes with textured branches; medium and far levels use 1,440 and 72 triangles. This is a geometry comparison, not a GPU timing measurement.

Matching Editor cameras: entrance `(0, 1.65, -0.2)`, Euler `(6, 0, 0)`; nearby fir `(2.5, 1.65, 4)`, Euler `(-12, 42, 0)`; 1600 × 900. The existing bark texture is retained. Tree-view ground/rocks have a candidate screenshot, not a matched before/after capture. Editor images do not establish Quest readability or performance acceptance.

Full PlayMode suite: 131 cases, 130 passed, zero failed, one optional video case skipped, 116.42 seconds. The new tests check the three-tree detail cap, shared material, geometry budgets, all detail-level references, preservation of model count and tracked pose, model rebuild cleanup and the forest/tree-view boundary. Both Android builds succeeded with zero errors and the same six existing build warnings (Pipeline runtime config absent, development-define deprecation and TMP compilation/shader notices).

Candidate APK: `VRExperienceAGB-near-scenery-detail-2026-10-17.apk`, 119,591,970 bytes, SHA-256 `5e46197ac4a203cf15c1b6c133709be7fb9c064fd7c5f310ee1d6d97481b5e8e`; installed and launched on Quest 3. Source changes remain uncommitted for review.

## Initial Quest comparison

Both completed captures used the same full 100-tree model, Forest only, active XR, approximately 72 Hz, Development configuration and enabled ambience. Neither capture had interrupted frames. The user confirmed the headset was worn and controllers set aside. Candidate capture finished at `2026-10-17T19:57:12.048Z`, with 4,154 frames over 60.0009 seconds.

| Metric | Baseline | Candidate | Change |
| --- | ---: | ---: | ---: |
| Average application FPS | 67.40 | 69.23 | +2.72% |
| Median frame time | 13.889 ms | 13.889 ms | unchanged |
| p95 frame time | 18.856 ms | 17.260 ms | −8.46% |
| p99 frame time | 22.334 ms | 18.760 ms | −16.00% |
| Mean Unity CPU frame time | 14.836 ms | 14.443 ms | −2.64% |
| Maximum frame time | 30.108 ms | 40.244 ms | worse single-frame peak |
| Peak Unity allocated memory | 232.94 MiB | 224.38 MiB | −8.57 MiB |
| APK size | 119,589,645 bytes | 119,591,970 bytes | +2,325 bytes |

Average and percentile frame times improved in this pair; the worst single frame worsened. This is preliminary evidence, not proof of universally unchanged performance. GPU timing and compositor dropped-frame counters were unavailable. The frame-over-budget estimate is sensitive to timing jitter around the 72 Hz threshold and is not a compositor metric.

The Android thermal service reported status 0 around both captures. Battery temperature was 35–38 °C around the baseline session and 37–38 °C around the candidate. The earlier baseline temperature sample predates the final clean baseline start; CPU/GPU thermal frequencies were not captured. There is one clean run per build, not a sustained thermal soak or repeated statistical sample.

Tracked head pose was not recorded by the existing probe. The tracking-origin translation differs across application launches, which can compensate for a different physical tracking origin and cannot by itself establish a movement through the scene. Candidate origin was stable throughout its capture. A fresh post-capture Quest screenshot confirms the entrance view; exact head-angle matching across builds is unverified. Only Editor comparison cameras were fixed exactly, without modifying headset tracking.

Editor scene inventory for the 100-tree forest: 847,964 → 778,400 triangles attached to active/enabled mesh renderers; 579 → 582 renderer components; 12 → 13 unique shared materials. Candidate inventory includes all enabled LOD renderer components, not only the LOD/frustum subset drawn by a particular eye. These are scene inventory counts, not GPU draw calls or rendered-triangle measurements.

Evidence files in `artifacts/near-scenery-2026-10-17/`: `before/quest-capture.json`, `after/quest-capture.json`, APK identities, build reports, thermal snapshots, test results, matching `entrance.png` / `close.png` pairs, and `after/quest-forest.png`. The last image is an actual on-device application screenshot; the matching pairs are Editor previews.


## Completion-pass implementation and evidence

The mountain artwork was refined without changing its source pixel dimensions (2172 × 724), silhouette or palette; its reflection uses the same source. New bark and grass textures use mipmaps and bounded ASTC imports. `Art/ForestDetail/README.md` describes provenance and the adjacent `generation-prompts.json` retains exact built-in ImageGen prompts.

All six garden interior firs and 42 clearing firs now use shared crown/trunk LOD pairs: 604 / 168 / 96 triangles. The trunk shader samples bark once with vertex lighting. Forty-two distant backdrop trees use six/four-triangle LODs, and the 72 shore trees are partitioned into four spatial batches for native frustum culling. Garden grass is capped at 240 six-triangle tufts across at most four batches, outside walking strips and planter bounds; clearing rocks receive restrained grass clusters. All added vegetation is static and collider-free.

The complete set of model trees remains represented. Crown geometry now uses the branch texture with 256 / 72-triangle LODs; the last model LOD is never distance-culled. Both levels fit the same metric bounds, preserving depth/leaf-count encodings. Crown pointer targets and planter controls remain separate. The miniature table uses the smaller shared crown mesh.

Full PlayMode suite: 133 cases, 132 passed, zero failed, one optional video case skipped, 120.22 seconds. Regression coverage includes model bounds and count, all crown targets, decorative LODs, grass clearance and geometry budget, shared resources, rebuild cleanup, manual branching and deterministic playback. A separate 10-second Editor capture confirmed configurable duration, head-pose reporting and explicit unavailable compositor flags; Editor timing is not headset evidence.

Android build `build_1339779e7e96` succeeded in 38.76 seconds with zero errors and six existing warnings. Installed APK: `VRExperienceAGB-scenery-complete-2026-10-17.apk`, 119,603,618 bytes, SHA-256 `5ebfed5e827f3cc0a2f664d6976de3580ad0a4296691dbfd69622ae7ecebfc30`, build GUID `96f8bcef2b844e6c9af8daab747a153d`. The accepted first-pass APK remains available for rollback. Source changes are uncommitted.

Matching Editor views are saved for entrance, close firs, rear mountains and the tree clearing; an additional miniature-table preview and actual Quest screenshot are saved under `artifacts/scenery-completion-2026-10-17/after/`.

## Completion-pass Quest comparison

The accepted first-pass build was measured again immediately before installation. Both captures used the complete same 100-tree model, Forest view, active XR, approximately 72 Hz and Development settings. No interruptions or view changes were recorded. Standard recenter was requested before the warm-up; the tracked head was never animated.

| Metric | Accepted first pass, fresh run | Completion pass |
| --- | ---: | ---: |
| Average application FPS | 70.33 | 71.72 |
| Median frame time | 13.889 ms | 13.889 ms |
| p95 frame time | 16.511 ms | 13.889 ms |
| p99 frame time | 18.452 ms | 16.020 ms |
| Maximum frame time | 26.229 ms | 23.782 ms |
| Mean Unity CPU frame time | 14.218 ms | 13.942 ms |
| Peak Unity allocated memory | 236.14 MiB | 224.02 MiB |
| APK size | 119,591,970 bytes | 119,603,618 bytes |

Baseline finished at `2026-10-17T20:26:20.274Z` (4,220 frames / 60.001 seconds); candidate finished at `2026-10-17T20:28:36.320Z` (4,303 frames / 60.001 seconds). Thermal status was 0 in all four snapshots. Battery temperature was 35–36 °C before and 37 °C after installation.

The candidate records head yaw approximately 130° at the start and 357° at the end, despite the intended stationary protocol; the prior binary does not record head pose. Therefore this pair does not establish a controlled same-view speedup or universal absence of regression. Head position stayed within a roughly 0.26 × 0.13 × 0.24 m box near the entrance. CPU and frame-time results are encouraging preliminary observations. GPU, compositor GPU and compositor dropped/presented-frame counters remained unavailable even after querying the XR provider; zero GPU time is not zero GPU work.

Evidence: `before-repeat/quest-capture.json`, `after/quest-capture.json`, corresponding fresh state and thermal reports, `after/build-report.json`, `after/apk-identity.json`, and `after/playmode-results.json` under the completion-pass artifact directory. The longer exploration results below are recorded separately and must not be compared directly to a stationary entrance capture.

Runtime log review still shows the previously documented optional OpenXR Meta `xrDiscoverSpacesMETA` lookup errors. No Unity managed exception or Android fatal exception appeared in the captured current-process error log. This is not clean-log acceptance; spatial discovery is outside this scenery work.


## Three-minute Quest exploration

The user walked through the forest, entered a tree, opened its menu and returned to the forest. Capture `2026-10-17T20:33:35.188Z` completed 180.004 seconds / 12,759 frames with active XR, the same complete 100-tree model, 72 Hz and zero interrupted frames. View transitions were Forest → Tree at 40.15 s → Menu/tree at 78.91 s → Forest at 80.23 s.

Average application FPS: 70.88; median: 13.889 ms; p95: 14.869 ms; p99: 21.717 ms; maximum: 208.333 ms; mean CPU: 14.109 ms; peak Unity allocated memory: 236.52 MiB. GPU and compositor counters remained unavailable. The high maximum is a real recorded spike, not discarded as an outlier. This report does not retain per-frame timestamps, so it cannot assign the spike to a particular view transition, garbage collection or rendering operation. There is no matching moving baseline; the spike cannot be attributed to or ruled out as a consequence of this change.

The current three-minute result supports normal short-session use near the refresh cap, but does not establish long thermal-soak acceptance or absence of hitches. A focused spike investigation and a controlled same-view repeated comparison remain useful follow-ups. The implemented visual scope, full automated suite, APK installation and both requested measurement types are complete. Visual acceptance of the expanded scenery remains with the user.

Android thermal status remained 0; battery temperature rose from 38 to 40 °C during this capture. These snapshots do not measure CPU/GPU clock throttling. Evidence is saved in `artifacts/scenery-completion-2026-10-17/moving/`, including the capture, start/end states and thermal snapshots.

## Quest visual feedback follow-up — October 17

The two latest device screenshots (`forest-20261017-203359-093-adfc25.png` and `forest-20261017-203325-920-e25154.png`) show distorted side-ridge trees and an empty dark tooltip above a selected crown. Local copies and follow-up evidence are under ignored `artifacts/scenery-feedback-2026-10-17/`.

- Side mountain sampling formerly spread 28% of the panorama over each 85-degree side while compressing its height to 48%. The shared `CarezzaPanorama.hlsl` now integrates matched horizontal/vertical sampling density and mirrors the full artwork. The central rear section, source texture, side haze and mesh dimensions are retained. Lake reflections use the same coordinates and ridge fade.
- Crown and planter tree-selection targets no longer create generic hover hints. Clicking still opens the nearby **Explore tree / Details** choices. Other control hints remain available. The empty hint was reproduced with a pointer-enter event before the fix and absent after it.
- Full decorative branches transition at 5.5% screen height instead of 22%, and medium branches at 2.1% instead of 8.5%, extending their distances approximately fourfold. Full model crowns transition at 1.8% instead of 7.5%, approximately 4.17 times farther. The far model crown now has 64 branch planes / 128 triangles instead of 36 / 72, also shared by decorative far crowns and the table. No model trees are culled at distance. Decorative geometry is now 604 / 168 / 152 triangles; near model geometry remains 256 triangles. Global LOD bias, resolution, tracked poses and scoring are unchanged.

Matching left-side Editor images use camera position `(0, 1.65, -0.2)`, Euler `(-3, 270, 0)` at 1600 × 900. The corrected left, right, rear and entrance views have been inspected; both shaders compile without reported shader errors. The full PlayMode suite passed: 135 cases, 134 passed, zero failed, one optional video case skipped, 122.67 seconds. Added regressions cover the branch-detail distance and denser far-crown budget, plus both pointer IDs hovering and clicking a crown without creating an empty hint. Android build `build_53dfe6b9b910` succeeded in 28.54 seconds with zero errors and the same six existing warnings. APK `VRExperienceAGB-scenery-feedback-2026-10-17.apk` is installed and launched on Quest 3: 119,603,801 bytes, SHA-256 `bf8e9217ed32c87ce14e07e816661be73baa36f98174b9a58faeebd703f65ad5`, build GUID `b0b3c3f06dc44943b3be1dd4d59bd762`. Previous performance captures apply to the previous APK, not to these longer detail distances.

The user confirmed readiness for a fresh one-minute device check. Capture `2026-10-17T20:52:09.134Z` completed 60.001 seconds / 4,282 frames, with all 100 trees, Forest only, active XR, 72 Hz and no interruptions. Average application FPS was 71.37, median 13.889 ms, p95 14.916 ms, p99 17.114 ms, maximum 27.778 ms, mean CPU 14.012 ms and peak Unity allocated memory 230.52 MiB. GPU/compositor timings and compositor frame counts remain unavailable. Thermal status was 0 before/after; battery temperature was 36–37 °C.

This capture includes movement: the recorded world-space head bounds span approximately 14.76 × 0.07 × 25.32 m, and the tracking-origin position changed. Consequently it is an operational check of the denser scene, not a stationary view-matched comparison or proof of unchanged rendering cost. No scripted head pose was applied. It does not resolve the earlier 208 ms peak or provide a sustained thermal soak. A fresh on-device screenshot was saved after the capture as `after/quest-forest.png`; all evidence is under `artifacts/scenery-feedback-2026-10-17/`. The scoped process error log retains the existing optional `xrDiscoverSpacesMETA` lookup errors, with no captured managed or Android fatal exception. User visual acceptance remains pending.

## Forest paths removed — October 17

The user accepted the mountain, hover-hint and fir-detail feedback fixes, then requested removal of the visible paths. The garden no longer creates the central walkway or the cross-row paving strips; the existing textured forest ground fills these areas. This removes eleven decorative mesh renderers in the 100-tree layout. The paving had no colliders and did not drive locomotion, teleport destinations, tree selection or scoring. All 13 existing navigation PlayMode tests passed in 18.08 seconds. The 100-tree entrance was visually checked at the previous comparison camera, and live scene inspection confirmed zero paving objects with the ground intact. Android build `build_df84a8d88c33` succeeded in 26.27 seconds with zero errors and six existing warnings. APK `VRExperienceAGB-no-paths-2026-10-17.apk` (119,600,187 bytes; SHA-256 `7312d7d67c9d8dfa1cfead022bb287c2687095858e79b60b73bfa6cdef10c117`) is installed and launched on Quest 3. No new headset performance claim is made for this mesh removal; physical visual acceptance remains pending. Evidence is under ignored `artifacts/forest-path-removal-2026-10-17/`.

## Floor navigation markers removed — October 17

After accepting the path removal, the user requested removal of the remaining floor squares. The garden no longer creates the **GO HERE** or **GO TO GROUP** canvases, their ray-interaction targets, or the distance-refresh list. Help text no longer mentions path-marker teleportation. Stick walking, tree selection and deliberate navigation used by other features remain available. The existing forest round-trip test now positions its observer directly instead of clicking controls that have intentionally been removed. The navigation suite passed 12 cases initially; the round-trip case needed a 0.2-second wait after direct observer placement for the existing 0.15-second target-visibility refresh. Its focused rerun passed, giving passing results for all 13 navigation cases. All nine M5 forest checks also passed, including stick walking, crown selection and complete guided traversal. A matching 100-tree Editor entrance image was inspected; live scene inspection found zero floor navigation targets and zero GO HERE / GO TO GROUP labels. Android build `build_7e115a7d2c11` succeeded in 24.04 seconds with zero errors and five existing warnings. APK `VRExperienceAGB-no-floor-markers-2026-10-17.apk` (119,600,064 bytes; SHA-256 `af5bfa9fec6589702d7dd095659d5d0a7b8a1a12ee9bc7160fa62707efd44c5d`) is installed and launched on Quest 3. Physical visual acceptance remains pending; no new device performance measurement was requested for this removal. Evidence is under ignored `artifacts/forest-marker-removal-2026-10-17/`.

## Fuller model firs and forest understory — October 17

The user accepted the removal of floor markers and requested slightly fuller small firs plus grass and rocks resembling the single-tree clearing. Model crowns now use 90 bent branches / 360 triangles nearby (previously 64 / 256), and 72 flat branches / 144 triangles at distance (previously 64 / 128). The same metric bounding box is retained for both model LODs. Decorative far crowns share the latter mesh, giving 604 / 168 / 168 triangles including trunks. Detail distances are retained.

Forest ground cover now adds small deterministic rock/grass groups at the entrance and between model rows, using the clearing slate shapes, slate material and grass artwork. Grass has at most 624 six-triangle tufts; rocks have at most 86 sixty-triangle shapes, each distributed across at most four spatial batches. Geometry avoids the central walking space, cross-row standing space and planter/label clearance. Shared assets, collider-free decoration and distance/frustum culling are retained. Temporary combined meshes are owned and destroyed by the ground-cover component when a model rebuilds. No new artwork, real-time shadows or animated foliage were added.

Baseline reference images: the accepted no-marker forest entrance, a fresh close view at `(1.2, 1.65, 2.4)`, Euler `(8, -40, 0)`, and the single-tree clearing as an appearance reference. Evidence is under ignored `artifacts/forest-understory-2026-10-17/`. Matching entrance and close Editor views of the final candidate were inspected. The 100-tree scene contains 334 interior grass tufts and 56 additional small rocks, distributed across eight renderer batches. The full PlayMode suite passed: 135 cases, 134 passed, zero failed, one optional video case skipped, 118.88 seconds. Checks include model LOD bounds/count, ground-cover clearance, geometry budgets and owned-mesh cleanup, manual exploration and complete playback. Android build `build_f26b882e6d7d` succeeded in 37.28 seconds with zero errors and six existing warnings. Installed and launched APK: `VRExperienceAGB-forest-understory-2026-10-17.apk`, 119,607,679 bytes, SHA-256 `4d066daa6ad0e976a28e660863dc8418a5ab160389615586eed44052a76079a7`. Both user-approved one-minute device captures are complete. Candidate build GUID: `ff179d59c5994c86a16a1219208a42a3`.


### Understory Quest comparison

Both captures used the same complete 100-tree model, Forest only, active XR, Development settings, 72 Hz and no interrupted frames. Baseline (the accepted no-floor-marker build) finished at `2026-10-17T21:19:46.855Z`, with 4,311 frames / 60.001 seconds. The candidate finished at `2026-10-17T21:26:12.957Z`, with 4,291 frames / 60.001 seconds.

| Metric | Before | Fuller firs and understory |
| --- | ---: | ---: |
| Average application FPS | 71.85 | 71.52 |
| Median frame time | 13.889 ms | 13.889 ms |
| p95 frame time | 13.889 ms | 13.889 ms |
| p99 frame time | 14.407 ms | 16.981 ms |
| Maximum frame time | 23.370 ms | 33.808 ms |
| Mean Unity CPU frame time | 13.917 ms | 13.981 ms |
| Peak Unity allocated memory | 227.47 MiB | 226.41 MiB |

The average and p95 remain near the refresh cap, while p99 and the worst frame are worse in this pair. These are observations, not a controlled attribution: baseline head yaw changed approximately 325° → 3°, and candidate yaw changed 164° → 1°. The candidate initially faced away from the forest before returning to the rows. Head position stayed near the entrance in both runs, but view directions were not fixed. Battery temperature was 36–37 °C in the baseline and 38–40 °C in the candidate; Android thermal status was 0 in all snapshots. GPU/compositor timings and compositor dropped/presented-frame counts were unavailable. There is one run per build and no sustained thermal soak. The earlier 208 ms exploration spike remains a separate unresolved observation.

Evidence includes the two `quest-capture.json` reports, fresh start/end states, thermal snapshots, APK identities, build/test reports, matching Editor images, and a post-capture Quest screenshot (`after/quest-forest.png`). That screenshot is an inverted ground-facing view and is excluded from visual comparison; the matching images are Editor previews. The scoped device error log retains the previously documented optional OpenXR `xrDiscoverSpacesMETA` lookup errors. User visual acceptance and exact view-matched performance acceptance remain open.
