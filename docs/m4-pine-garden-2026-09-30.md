# M4 pine garden — September 30, 2026

Branch: `codex/m4-ensemble`. Unity 6000.6.3f1, Apple Silicon.

The user approved replacing stone-only forest cards with a separate walkable garden of miniature pines and preserving the existing decision walkthrough. The earlier “Depth 0” label was route progress, not structural depth.

## Implemented behavior

- Every model tree has a persistent planter. Numbered beds contain up to ten trees in canonical model order. Distant plaques are hidden for readability; model trees and evaluation are never truncated.
- Height represents maximum root-to-leaf depth; crown width represents leaf count. Both use bounded, monotonic scales, and plaques report exact values. A valid unsplit tree has depth zero and one leaf.
- Amber rings identify selection; bright cyan and explicit text identify reached leaves. Unvisited and partially explored routes have separate text. Gain is not currently mapped onto trunk thickness or presented as prediction importance.
- Pointing at a pine or its plaque highlights the planter and shows its structure in the field guide. Selecting the pine, plaque or a numbered button changes the selected tree without choosing a branch or clearing contributions.
- Enter/resume opens the selected tree's existing walkthrough. Returning restores the garden origin and preserves selection, decisions and route score.
- Teleport targets are explicit standing points beside planters or on bed paths. Snap turns rotate the observer origin in 30-degree steps around the current head position. View changes and teleports use a short fade; no scripted movement writes the tracked eye/controller transforms.
- The field guide can be hidden, reopened with its handle, or recalled with right-controller A / desktop Tab. Right thumbstick snaps in the garden; explicit turn buttons provide another route. In the desktop preview, right-drag changes the view direction. Physical headset look remains tracked.
- Existing pine meshes, textures and planter artwork are reused. A separate serialized evergreen material makes model-bearing pines distinguishable from background scenery and retains its shader variant in player builds.

The evaluator, prepared-profile results, manual consistency rules and scoring ensemble are unchanged. The nested-export structure preview remains separate from verified production scoring.

## Verification

150 EditMode and 23 PlayMode tests pass. New coverage checks true structural depth, leaf counts, stump handling, direct pine selection and pointer reachability for field-guide buttons, selection without route changes, all-tree retention across beds, stale pointer rejection, guide visibility, origin teleports/snap turns, preserved tracked-local poses, and garden/walkthrough round trips with accepted leaf contributions. Existing manual, prepared-profile, hover, editing and tour tests continue to pass.

All 100 trees in the local sample matched independently calculated depth, leaf and node counts (13,576 nodes total). The last tree was reached, manually traversed to a leaf, and returned to the same garden position with its contribution preserved. Tracked local head pose remained unchanged. It is not added to the project assets or bundled in the APK. The APK uses the existing synthetic examples; the larger synthetic ensemble remains available through the walkthrough menu.

Local evidence is stored under ignored `artifacts/m4-garden-2026-09-30/`: test reports, independent expected metrics, actual sample checks, screenshots and build evidence.

The final Android development build succeeded with zero errors and four warnings. The APK is `artifacts/builds/VRExperienceAGB-m4-garden.apk` (104,704,566 bytes; SHA-256 `ab17ccf3ec55a8f40998fe1475cd456983d2a93cf6f46ff246369b9b572a0a01`). It has not been installed or tested on a headset.

Three Editor captures show the actual sample: `01-garden-wide.png`, `02-pine-field-guide.png`, and `03-tree-walkthrough.png` in the evidence folder. The desktop camera was manually aimed for these captures; the authored camera and headset tracking were unchanged.

## Device acceptance still required

Controller ray selection, A-button behavior, thumbstick dead zones, seated readability, comfort fades and performance require a Quest 3 check. Editor tests and an Android build do not establish headset acceptance. Physical room movement uses the existing tracking setup; free continuous joystick walking has not been added.
