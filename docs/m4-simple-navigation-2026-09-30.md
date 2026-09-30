# Simplified M3/M4 navigation — September 30, 2026

The user requested a simpler headset flow after trying the pine garden. This supersedes the field-guide navigation in the earlier garden record.

## Default demonstration

The exact user-selected `data/examples/export_Mobile_Click_Through_Rate_AGB_demo.json` is bundled as a Unity TextAsset and assigned to the teaching scene. It contains 50 trees, 790 nodes and 420 leaves, with maximum depth 4. SHA-256: `ac5fcb27e992f935058d9002516474280c1e15aa81c30475e2610a762089736e`.

It uses the nested AGB export format directly. The source and bundled copy are checked for equality. The export remains a structure/manual preview: there is no prepared-profile probability or claim of source-scorer agreement. The verified synthetic model/profile fixtures and their regression coverage are retained independently of this startup demonstration.

## User flow

- Startup shows an empty forest clearing with two choices: **M3 · One tree** and **M4 · Whole forest**. Model pines and decision platforms are hidden until a case is chosen.
- M3 opens a paginated list of all 50 trees. Each row shows number, structural depth and leaf count. Selecting a row opens the existing individual-tree walkthrough. Return goes to the same list page.
- M4 opens all 50 model pines. Select explicit markers along the central and cross-paths to approach groups or individual planters. Right-stick snap turns and physical tracking remain available.
- Point at a pine to reveal structural information. Click it to open that tree immediately. Return restores the forest location and preserves the selected tree and route.
- The forest has a small Menu button and brief movement instructions. A / desktop Tab recalls the case menu. There is no field-guide dashboard, numbered selector, profile picker or ledger in this default flow.
- Individual-tree controls are branch choices, Back, Restart tree, Back to forest/list and Cases. Restart affects the current tree. Full-name hover remains available.

All initial UI copy is English. Navigation does not infer a prediction from arbitrary branch choices, change the scoring ensemble or write tracked head/controller local transforms. Movement between path markers is a deliberate teleport with a fade, not continuous joystick movement.

## Verification

150 EditMode and 27 PlayMode tests pass. The interaction suite covers the new two-choice startup, exact bundled-source equality, paging through tree 50, direct pine selection, hover without decisions, path-marker travel, saved forest return position, unchanged tracked-local head pose, current-tree Back behavior, pointer reachability and stale presses across pages. Existing synthetic profile, tour, hover and traversal regressions remain covered.

An independent walk of the original demo matched all 50 trees' depth, leaf and node counts. Manual all-TRUE traversal reached the same 50 leaves and contributions, for a route total of `-0.82529999999999981`. This is an arbitrary manual route, not a customer prediction. Evidence is under ignored `artifacts/m4-simple-navigation-2026-09-30/`.

Earlier garden headset installation does not establish acceptance of this new navigation.


The Android development APK built successfully with zero errors and five warnings. Artifact: `artifacts/builds/VRExperienceAGB-m4-simple-navigation.apk` (124,811,601 bytes; SHA-256 `6111afd1c96eb4ebe697d3769ef2544f7e3226d5fd50ad4f3dbd39babddd7c2f`). It was installed on the connected Quest 3 (`Success`) and cold-launched (`Status: ok`). The process remained running; the captured launch log contained no fatal exceptions or Unity errors. This confirms installation/startup, not controller comfort, readability or performance acceptance.

Editor screenshots were visually inspected: `01-home.png`, `02-tree-list.png`, `03-tree.png`, and `04-forest.png` in the evidence folder. The final forest menu sits beside the central path marker rather than covering it. The final tree return button is separated from the score text. All visible tree controls passed UI raycast reachability checks.
