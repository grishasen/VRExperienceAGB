# Validation and acceptance

Status: test strategy. The synthetic fixture verifier passes. The [setup review](setup-review-2026-09-27.md) inspected the live Unity Editor, which reported no project compilation failure and zero discoverable tests. No Unity test suite, Simulator session, or Quest acceptance run was performed during that review; The subsequent development APK build and update installation succeeded on 2026-09-27; launch, tracking, interaction, and independent relaunch remain untested. Device acceptance remains pending.

## Current repository checks

```sh
bash scripts/check-mac.sh
python3 scripts/verify-examples.py
```

The environment check reports missing setup as expected until installation is complete. The example verifier validates the bundled model/profile structure and compares paths, leaves, contributions, raw scores, and probabilities with the expected fixtures. It also checks representative malformed-input rejection. It uses no third-party Python packages.

## Planned C# EditMode tests

Test user-visible semantics independently of rendering:

- Numeric values below, equal to, and above a threshold.
- Categorical membership, unsupported categories, explicit missing values, and absent required values.
- Baseline plus weighted leaf contributions and numerically stable output transformation.
- Multiple trees and profiles; complete evaluation even when most geometry is hidden.
- Invalid IDs, child references, cycles, unsupported operators, and malformed imports.
- Manual constraint intersections and contradictions across different trees.
- Branch/leaf event commitment, undo, restart, repeated input, and mode changes.
- Re-evaluation after a profile edit, including paths in previously visited trees.

Use fixed independently expected cases. Tests that merely repeat the production implementation do not establish scoring correctness. Add trusted source-model comparisons before approving the AGB adapter.

## Planned PlayMode checks

Validate the application state and its visual consequences: selected node maps to the correct model ID; hidden branches retain their data; returning to the diorama preserves progress; playback stops on pause; back/forward does not duplicate contributions; changing profiles clears stale highlights.

Use a small deterministic scene. Most logic tests should not require a headset or a Meta runtime.

## Quest 3 acceptance matrix

| Area | Scenario | Pass condition |
| --- | --- | --- |
| Installation | Fresh install and update of a compatible signed build | App launches and reports the intended version |
| Tracking | Look around and point with each controller | Stable tracked pose and reliable target selection |
| Manual route | Choose a path, reach a leaf, undo, choose another | Correct route and exactly one contribution per reached leaf |
| Prepared profile | Step and replay a full profile | All visible decisions match the reference route |
| Takeover | Pause profile playback and edit a feature | New result and routes are internally consistent |
| Ensemble | Compare short and detailed tour | Identical full-model score and output |
| Scale | Enter/exit a tree from the diorama | Selection/progress retained without disorientation |
| Text | Read conditions in the intended seated and standing positions | No need to lean into labels or infer text from glow |
| Recovery | Open system menu, remove headset, lose/restore tracking | Safe pause/resume with no score corruption |
| Recenter | Recenter or change seated position | Controls and return point remain usable |
| Offline use | Disconnect network and restart bundled demo | All core scenarios remain available |
| Duration | Full tour and a 30-minute soak | No crash, accumulating state error, or persistent performance collapse |
| Casting | Repeat the demo with casting if required | Presentation remains usable; measurements recorded separately |

## Performance

The initial project target is sustained native rendering at 72 FPS with a 72 Hz display rate. That corresponds to approximately 13.9 ms per frame; target useful headroom rather than treating the entire interval as available application time. Assess 90 Hz only after the baseline is stable. This is a project acceptance target, not a restatement of store minimums.

Measure CPU/GPU times, dropped frames, and memory on the actual Quest using Meta device performance tools and targeted Unity profiling. Final measurements use a release build without script debugging. Simulator or Editor FPS cannot stand in for device measurements.

Agree a worst visible scene with documented counts of active trees, nodes, text labels, and effects. Include a large-model case. Budget active detail explicitly rather than assuming that all 13,576 nodes from the reviewed large export must render with full labels simultaneously.

For later distribution, recheck [Meta's current performance VRC](https://developers.meta.com/horizon/resources/vrc-quest-performance-1/), which distinguishes display refresh rate, rendering rate, and exceptions.

## User comprehension

Ask at least three new users to complete the first-session route without keyboard assistance. Observe whether they can select a branch, undo, identify the current tree, and distinguish a leaf contribution from final probability. Ask them to explain why the prepared profile took one branch. Record confusion and navigation failures as implementation work, not just subjective ratings.

## Test record

Keep detailed logs/videos in ignored `artifacts/`. Summarize useful results in the relevant milestone or toolchain record.

```text
Date / source commit / app version:
Editor and package versions:
Quest OS / display rate / build type:
Fixture or approved model identifier:
Visible scene size:
Tested scenario:
Expected / actual:
Performance observations:
Pass / fail / not tested:
Remaining issues:
```

Do not mark “not tested” as a pass. A release candidate requires successful headset acceptance, correct full-model results for every supported input format, and a reproducible build configuration.
