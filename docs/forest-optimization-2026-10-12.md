# Forest UI cost, perimeter scenery and distance readability — October 12, 2026

## Changes

The 100-tree forest retains every tree and its full scoring ensemble. Predictor-composition strips are now shown within 8 m, and purely visual composition/link canvases no longer instantiate Meta ray surfaces or uGUI raycasters. Crown selection remains available within its existing 12 m range. Names are displayed within 8 m with distance-adaptive 32–50 point type, a dark backing, and consistent pale text; signed contribution remains indicated by signs and colored planter rings. Hover details use a larger layout and 24–26 point type, placed approximately 2.2 m along the viewing direction with a lateral offset instead of remaining beside a distant crown. Moving away and back restores detail visibility. Hovering a crown between 8 and 12 m temporarily restores its plaque/card, preserving the full selection range.

Side and rear scenery adds shrubs and distant mountains outside the tree beds, without filling the front central aisle or moving the wolf. The scenery consists of two combined opaque meshes (two renderers), with no colliders, transparency or shadow casting. Meshes and materials are owned and released on garden replacement. The shrubs use rounded low-poly clusters and the mountain silhouettes use irregular faceted peaks. Shared leaf cutout settings are explicitly disabled for these solid meshes.

## Measured scene cost

At the same 100-tree entrance in Editor:

| Active components | Before | After |
| --- | ---: | ---: |
| Canvases | 141 | 37 |
| Enabled Meta ray interactables | 140 | 34 |
| Enabled renderers | 629 | 631 |

Counts measure scene/component reduction, not frame-time improvement. Evidence: `artifacts/forest-optimization-2026-10-12/editor-before.json` and `editor-after.json`. Display-only canvases never need a controller surface; reducing them also avoids invisible targets interfering with selection.

## Text assessment

The existing font is LiberationSans SDF, 86-point sampling, scale 1, 1024 atlas, Mobile Distance Field shader. URP uses 4x MSAA and render scale 1. No blanket render-resolution or atlas-memory increase was made. The limiting factor for the old distant labels is their angular size: font size 25 at world scale .0024 gives a nominal 6 cm em, only about 0.29 degrees at 12 m; actual glyph height is smaller. At the new maximum readable name distance of 8 m, 50-point type yields a 12 cm em (about 0.86 degrees). These are geometric estimates, not measured Quest pixels or a universal legibility threshold. Detailed cards are brought closer rather than trying to show long paragraphs at 8–12 m.

Front/side/rear Editor renders were inspected. Final headset text acceptance requires checking central and peripheral clarity at 3–8 m, long identifiers in the hover card, menu text after repositioning, and selection from both controllers. Keep exact identifier/detail views available; do not infer data from shortened labels.

## Validation status

Compilation passed. The full PlayMode run passed 111 tests, skipped one opt-in video test, and found one obsolete assertion expecting transparent name backing. That assertion was updated for the intentional contrast backing and its focused rerun passed. The final focused distance/hover regression also passed, including hover at 10 m and deferred hide on the next frame. Across the full run and focused reruns, all 112 enabled PlayMode cases have passed; the optional video test remains skipped. Android Development build `build_c3a9bdcbfc4b` succeeded with zero errors and seven warnings (Pipeline runtime configuration, deprecated directives, TMP IL2CPP compilation notices and duplicate Internet permission). APK `artifacts/builds/VRExperienceAGB-forest-optimized.apk` (128,921,635 bytes; SHA-256 `34b358820de7dc84b7b30732932763bad7e8637b92973962850b95fd5bd05b3d`) was installed with `adb install -r` returning Success. This is an uncommitted candidate based on `175b0d0`. A new Quest baseline was requested after the user said ready, but the headset repeatedly reported Asleep and never processed the queued command; no new measurement file appeared. The earlier 61.80 FPS mixed session is historical and is not a comparable baseline for these changes. No FPS speedup or sustained 72 Hz acceptance is claimed.

## Headset checks

1. Walk down the full 100-tree forest and back. Nearby composition strips and tree labels should return; every tree must remain selectable.
2. Read tree numbers at 3, 5 and 8 m. Aim at a crown between 8 and 12 m and confirm its detail card appears near the viewer, with long identifiers intact. Test both controllers.
3. Look left, right and behind: shrubs and mountains should frame the clearing. Check that locomotion, central wolf/howl and the main aisle remain usable.
4. Open and close Tools, select both branches of a tree, and complete synthetic A/B playback.
5. Capture 60 seconds in the forest with the headset worn and app focused. Record frame times and interruptions. A speedup claim requires a comparable capture from the previous build with the same model, pose, refresh rate and conditions.
