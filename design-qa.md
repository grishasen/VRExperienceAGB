# Moonlit clearing visual QA

Source visual truth: `docs/design/moonlit-clearing-reference.png` (1672 × 941).
Implementation: `artifacts/moonlit-clearing/manual-final.png` (1920 × 1080).
Additional states: `profile-final.png` and `leaf-final.png` in the same folder.

Scope: adaptation of the selected art direction to the existing Unity Quest scene, not a pixel-identical rendering of the generated illustration. Both comparison images show the same seven-node tree and manual mode at the root. They were opened together in one comparison input at their native aspect ratios, both approximately 16:9. Unity world-space UI has no CSS size or browser density. The reference is a concept illustration; all implementation evidence is an actual Editor camera capture.

## Findings and comparison history

- Resolved P1: stone console obscured buttons in `first-pass.png`. Moved the stone backing behind the interactive canvas and raised the controls. The final capture shows every active button exposed; the screen-position raycast test passes in both modes.
- Resolved P2: duplicated root question in `first-pass.png`, and root title behind the leaf explanation in the first leaf capture. The current question occupies a dedicated sightline; the redundant root label remains hidden. The final leaf capture shows the explanation unobstructed.
- Resolved P2: original flat oval scenery and the first pass's uniform cone trees did not support the selected forest direction. Added generated pine foliage with alpha-tested crossed cards, shared slate texture, layered panorama, moon and warm local lantern lights. Final capture includes these assets.
- Resolved P2: profile controls were all visible in manual mode. Step/Play now appear in prepared-profile mode; the selected mode is highlighted and named in text. Back, pause, restart, overview and posture remain available.

## Required fidelity surfaces

- Typography: retained the project's Liberation Sans SDF for Unity/TMP consistency. The main question and mode actions are larger than status and profile details. No truncation or overlap is visible in the final root/profile/leaf captures. Small profile text still requires headset readability qualification. The reference's exact font is not a supplied font asset.
- Spacing: seven-node hierarchy and full-tree framing are retained; current decisions flank the root and the compact stone console sits below. Additional recovery, posture and profile controls are retained because the product requires them, although the concept omits them.
- Colors: blue twilight, cyan routes/rims, amber sample and warm lantern accents implemented. Current/visited text supplements material changes. Lighting is restrained and less cinematic than the generated target; this is an explicit implementation difference, not an exact-fidelity claim.
- Assets: real generated slate, pine and panorama textures are in the project. Stone structures are actual 3D meshes; decorative trees use crossed cutout cards. There is no lake, volumetric fog, dense animated foliage or bloom in this pass. These are art-direction simplifications, and performance is not yet measured.
- Copy: English labels retain actual thresholds and signed scores. Manual routes and prepared one-tree subtotals remain distinct and explicitly say they are not full predictions. Generated decorative slogans were intentionally omitted.

The full-view comparison also allowed direct inspection of the question, branch choices, score console, foliage edges and sky regions; separate crops were unnecessary. Additional profile and leaf screenshots were inspected for changing text and controls. Desktop pointer raycasts, session behavior and deterministic playback are covered by six passing PlayMode tests. The independent model/session suite passed 102 EditMode tests.

## Remaining qualification

This visual pass does not establish Quest stereo appearance, foliage-card quality from arbitrary viewpoints, physical controller pointing, text readability, comfort or performance. The user has now requested headset testing after the follow-up polish; connection and testing remain pending. The APK is a development build.

## Follow-up polish

P3: richer stone-edge detail, more foliage variations, a subtler lunar texture and further atmospheric lighting can improve the match to the concept after on-device constraints are measured.

final result: passed

## Follow-up polish

Latest preview: `artifacts/moonlit-polish/final.png`. The moon now has a luminous lunar surface and soft halo, clear of leaf labels. Stone edges have irregular bevels; cyan rims were moved outside them after visual inspection revealed clipping. Sky/fill are darker and lantern pools stronger. This remains a stylized adaptation; headset appearance and performance are not yet qualified. Six scene tests passed before the final decorative rim adjustment; that adjustment was visually rechecked.

## September 29 navigation revision

Current visual evidence is under `artifacts/navigation-2026-09-29/`: forest overview, deep root/fifth decision, profile editor and ensemble result. Seven platforms are reused for deep navigation. Additional controls occupy a separate upper row; an initial overlap with node labels was corrected, then the row shifted clear of the moon. A current-node marker intersecting the sample was hidden; the sample and active platform still identify that node. Smoother stone shading and softer pine alpha edges are implemented. Desktop button-center raycasts pass in all three UI modes. This revision has not been viewed or measured on Quest; the user approved only the previous revision's first headset test.

## Compact interface follow-up

The user's original concept guided warm cream decision text, compact stone plaques and near-transparent button fills. The castle photograph guided a muted orange lunar tint; no image extraction or literal texture match is claimed. Secondary actions and route metadata now appear only in Menu. Closed/open views were visually inspected at 1920 x 1080 in `artifacts/compact-ui-2026-09-29/`. The closed view has no upper command strip; the expanded view places commands on a dark translucent panel and suppresses the overlapping central question. Ten PlayMode tests pass. Physical controller accuracy for the smaller targets remains untested on Quest.


## M3 candidate — September 29

Reviewed actual 1920x1080 Editor Game captures `artifacts/m3-ready-2026-09-29/profile.png` and `inspection.png`. Profile details appear before playback; the stable inspection panel fits both branch conditions above its two controls. Compact decision plaques and warm moon remain present. Inspection is a temporary menu overlay, not a permanent scene label. This is an adaptation of the concept, not pixel parity. Physical headset text size, reach and haptics remain pending.
