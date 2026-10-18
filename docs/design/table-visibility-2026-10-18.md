# Table miniature visibility — October 18, 2026

**Superseded visual design:** the user confirmed that these trees were visible but disliked the cone appearance. They selected natural-fir concept A for the [next revision](table-natural-fir-2026-10-18.md). The evidence below describes the earlier cone build.

The latest application screenshot on Quest, `forest-20261017-224904-670-09a134.png`, shows bright contribution rings but almost transparent tree crowns. Table View reused the full-size model's alpha-tested needle texture on a small branch mesh. At miniature scale the needles lose coverage and the crown becomes a sparse, dark lattice.

## Change

Table View now uses a closed, opaque three-tier fir with a short trunk, shared by all miniatures and the enlarged hover preview. Faceted green tiers have bounded presentation lighting so their silhouette remains readable in the night scene. The material does not sample textures, discard alpha, or require additional scene lights. It supports instancing and stereo rendering. Each tree has 80 triangles instead of 144; this is a geometry reduction, not a measured frame-rate claim. The table owns and releases its generated mesh and material.

The existing metric dimensions remain unchanged: height represents maximum depth and width represents leaf count. All model trees remain present, with the same layout, scale limits, selection targets, hover passports, predictor markers, contribution rings and source strips. Manual exploration and deterministic playback retain the complete scoring ensemble. Full-size forest trees and the approved sky are unchanged by this adjustment.

## Verification

Evidence is saved under ignored `artifacts/table-visibility-2026-10-18/`:

- `quest-before.png`: the latest actual device screenshot.
- `editor-before.png` and `editor-after.png`: matching 1600 × 900 Editor views of the completed 100-tree A/B table, at scale 1 and yaw 0.
- `editor-minimum-rotated.png`: scale 0.7, yaw 45°, with the enlarged hover tree and passport visible.
- Preview inspection confirms 100 miniatures, one shared mesh and one shared material; the simplified silhouettes remain visible at minimum scale.
- A rendered regression checks the smallest miniature at minimum scale from three rotations, requiring visible coverage over 30% of its projected bounding rectangle. Existing table tests cover metric variation, pointer hover without selection, grouping and model-state preservation.

Compilation passed. All 26 scenario cases have passing results across the initial run and one focused rerun. The new silhouette test initially measured the projected world-axis bounding box, which expands into empty space when rotated; it now projects the actual mesh vertices. The corrected test passes at 0°, 45° and 180° without changing the rendering or coverage threshold. Existing 25 scenario cases passed on the first run. Evidence: `scenario-tests-initial.json` and `silhouette-tests.json`.

All nine M5 forest PlayMode cases also pass (`forest-tests.json`, 9.03 seconds), giving 35 passing relevant cases across these runs. No shader messages were reported and the shader is supported in the Editor. Android build settings retain non-Development APK, no script debugging and no profiler connection.

## Android build and installation

Build `build_fdecc70d656e` succeeded in 39.454 seconds with zero errors and six existing notices: missing optional Pipeline runtime config, the performance probe's deprecated Development directive, a TextMeshPro shader pragma, and three TextMeshPro IL2CPP compilation notices. The new table shader is included in the build report. Verified sources were unchanged during the build.

Distribution file: `BoostingExperience/Builds/Android/VRExperienceAGB-Quest3-table-visibility-2026-10-18.apk`, 91,737,946 bytes. SHA-256: `64ce36f760b4cf9777d409777dc5857d34647f8f7b6e402b53d675f9e194bd9e`. A checksum and installation notes accompany the APK. Package `com.vrexperienceagb.prototype`, version 1.0 / code 1, ARM64 IL2CPP, non-Development. Signature verification passes with the existing Android Debug certificate for compatible sideload updates; this is not store release signing.

`adb install -r` returned Success on Quest 3 `2G97C5ZHBX02V4`. The activity launched successfully (PID 7275), and the installed APK SHA-256 matches the distribution file. The captured 945-line process startup log contains no matched fatal exception, fatal signal, null/missing-reference exception or shader failure. This is a scoped startup check, not a claim that every log subsystem is warning-free.

Editor images are not headset visual acceptance; renewed device readability and performance remain unverified. The geometry reduction alone does not establish a measured performance improvement.
