# Combined M3 and navigation device check — 2026-09-29

Status: partial device verification completed. User approved the final visuals, corrected posture layout and lantern placement. Formal M3 acceptance remains pending the unrecorded checks below.

## Build under test

Use `artifacts/builds/VRExperienceAGB-lantern-fix.apk`. SHA-256: `5ede6b1fc600a9796e3bec5c67d3a63d09f6f0276a27ba01611302f90cf7416d`. Record headset OS, refresh rate, posture, controller hand and profile. Do not transfer results from the September 28 build to this revision.

## Check sequence

| Check | Expected result | Actual result |
| --- | --- | --- |
| Cold launch, then disconnect USB and relaunch | Scene opens standalone; orange moon, small cream True/False plaques, secondary controls only in Menu | Pending |
| Manual: TRUE → TRUE | First-tree leaf -3.8; previous total 0, contribution -3.8, new total -3.8; no profile probability | Pending |
| Back, FALSE | Alternative leaf -3.2; abandoned -3.8 contribution removed exactly once | Pending |
| Repeated Back at root | No score change; readable root feedback | Pending |
| Inspect nodes | Menu pauses movement; both actual branch conditions and stable node ID readable; Next visible node does not change route or score | Pending |
| New visitor profile | Name and values visible before Step/Play; first root value follows TRUE; replay gives the same leaf | Pending |
| Play/Pause/Resume, Back/Restart mid-move | Drop and route stay consistent; viewpoint remains under head tracking; repeated trigger does not skip nodes | Pending |
| Open/close Menu while moving and while explicitly paused | Menu freezes movement; closing preserves the earlier pause state | Pending |
| Complete three trees with New visitor | Contributions -3.8, -0.1, -0.2; complete raw score -4.1; full probability 1.63%; partial totals never presented as full probability | Pending |
| Forest overview, previous/next tree, enter | Selected tree and accepted route preserved; the complete evaluator remains independent of seven visible slots | Pending |
| Try a change, adjust, Restore original, Done editing | Full result recomputed; all old routes reset; original returns to -4.1; editor closes cleanly | Pending |
| Deep tree: 8 levels, Example 0 | Eight decisions; leaf -2; at most seven node platforms; Back and Tree overview preserve understandable context | Pending |
| Both controllers, seated and standing | All core targets reachable and labels readable; pointer hover/press clear; short haptic on selecting controller where supported | Pending |
| System menu, recenter, remove/re-wear | Safe pause; no scripted tracking-space movement; user can resume | Pending |
| Explain the result | Record visitor's own explanation of why the branch was chosen and why the raw total changed | Pending |

## Performance evidence

Capture actual device refresh rate, CPU/GPU frame times, dropped frames and memory in the original, deep and forest views. Target: sustained 72 FPS at 72 Hz. Record measurement method and duration. Editor timing and subjective smoothness do not establish acceptance.

## Decision

Detailed scripted checks below remain pending unless explicitly recorded; the user's visual and posture approval is recorded separately. Record blockers and fix them before marking M3-08 or the M3 epic accepted. This combined check does not claim the later M4 constraint engine, M5 adjustable miniature or M6 multi-person study is complete.

## Reported defect after geometry build

User confirmed smoother, prettier shapes but reported an overly high tree, cylindrical trunks and separated stones/rims/labels during posture switching. Software correction clears static flags on movable teaching geometry, lowers upper rows and adds bark/woody branches. Posture checks must be repeated on the replacement build before acceptance.

## End-of-day user feedback

User approved the smoother visuals, confirmed the corrected posture/height layout, and approved both lanterns after their placement repair. This is qualitative device evidence for those fixes only. The user requested merging the checkpoint and continuing M4 next session.
