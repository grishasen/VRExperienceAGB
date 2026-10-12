# Synthetic 50-tree score demo — October 11, 2026

Historical record: superseded by the [100-tree default model](default-sample100-2026-10-12.md). The preparation script now bundles that larger sample.

Status: implementation, Unity verification and Android Development build complete. October 12: installed on Quest 3; full A/B calculation matched the reference, and a device capture shows playback at tree 2/50. Full headset acceptance remains pending.

## Behavior

- Prepare the entire tracked `data/examples/export_Mobile_Click_Through_Rate_AGB_demo.json` using `python3 scripts/prepare-local-sample.py`. The source contains 50 trees and is not truncated. The separate ignored 100-tree source is unchanged.
- The generated LocalModel resources contain the normalized 50-tree model, original synthetic export, three synthetic profiles and an independent Python traversal reference for every reached leaf.
- All 200 used predictors are supplied. Expected raw scores: baseline 2.1433, alternative 3.2506, boundary case -3.4563. A/B defaults to the first two profiles; B minus A is 1.1073.
- Forest and Table View results, A/B comparison, accepted edits, contribution ledger, node evidence and boosting progression show raw scores. Completed results include all 50 trees; intermediate subtotals remain explicitly labeled.
- UI copy: **Propensity comes from calibration. No calibration table is loaded; only score is shown.** A/B differences use score units. Sigmoid percentages are no longer presented as prediction results.
- Existing evaluator sigmoid properties remain internal synthetic reference calculations for compatibility and numerical regression checks. Recorded CDH propensity remains separately labeled source evidence.
- Ordinary AGB/CDH imports and manual branch exploration remain available. No real profiles or production exports are bundled by this change.

## Verification

- Unity 6000.6.3f1 compiled the changed source without errors.
- EditMode: 207 passed, zero failures.
- PlayMode: 104 passed, zero failures; one opt-in video recording test skipped. Initial failures were stale expected copy/counts and were updated to score-only/50-tree expectations before the passing rerun.
- Every leaf and all three scores matched the independent Python reference. Full A/B playback and immediate calculation cover all 50 trees. Ordinary imported AGB/CDH result copy was also checked.
- Live scene: 50 trees, three synthetic profiles, A score 2.1433, B score 3.2506. Table result text has no TMP overflow (font size 33.65) and was visually inspected at 1600x900. No headset was used.
- The existing independent normalized-fixture verifier passed.
- Evidence: ignored `artifacts/sample50-score/` (test reports, live scene JSON, screenshot and build report).
- Android Development APK: `artifacts/builds/VRExperienceAGB-sample50-score.apk`; succeeded with 0 errors and 6 warnings in 33.419 seconds. APK file size: 102,870,172 bytes.
- SHA-256: `fcfca5a786ca4aa2dccaffe58de5cd92d09f95c6c6d1807a58e32bec1dac5d46`.
- October 12: this exact APK was installed with `adb install -r` on Quest 3 and launched. See the device check below.

## Headset checks

1. Launch the new APK and confirm the home menu reports 50 trees and synthetic profiles.
2. Open the forest and Table View; confirm all 50 trees are available.
3. Run profiles A/B, pause, step and calculate the whole model. Expect A score 2.1433, B score 3.2506 and B minus A 1.1073 (subject to display rounding).
4. Check the result above the forest/table and result-details page: score and calibration notice are readable in stereo; no calculated probability percentage appears.
5. Select the boundary profile and expect score -3.4563. Inspect leaf contributions and intermediate totals.
6. Enter a tree manually, choose branches, undo and return; manual route totals must remain separate from prepared-profile results.
7. Check controller targets, seated/standing placement, recentering and sustained device performance. Editor results do not establish headset acceptance.

## Quest 3 check — October 12, 2026

The APK SHA-256 above was rechecked before installation. Installation succeeded without clearing application data. The startup report confirmed model `local-sample-ac5fcb27e992`, 50 trees and three synthetic profiles.

The on-device development command `compare` evaluated the complete A/B pair using the application's Calculate whole model path. It returned `complete: true`, A score 2.1433 and B score 3.2505999999999995; both match the independent reference within 1e-10. The difference is 1.1073. Diagnostic JSON retains internal sigmoid reference fields; these are not displayed as calibrated propensity.

The `ab` command opened paused playback. A subsequent application-rendered device screenshot shows both profile markers, their condition values, tree 2/50, one completed tree and the Learn playback estimate. This establishes visible progression beyond the first tree on the device; it does not establish animated completion of all 50 trees or stereo readability/comfort acceptance. Playback was left available to the user.

Startup logs include OpenXR `xrDiscoverSpacesMETA` lookup warnings, an optional Play AssetPackManager class lookup failure, and an OVRMetricsTool initialization failure (`0-stub`). Calculation and captured rendering succeeded despite these messages. No sustained performance acceptance is claimed. The application process restarted once during the initial check; the cause was not established from this check.

Local evidence: `artifacts/quest-sample50-2026-10-12/` contains startup and A/B reports, device logs and `ab-headset.png`. Full animated traversal, pause/resume/controller acceptance, stereo readability and sustained performance remain pending user confirmation or further device checks.
