# Bundled Web model and CDH pair — October 13, 2026

The local Android build includes the supplied October 12 Web AGB export and two selected CDH execution profiles in addition to the existing 100-tree startup sample. Open **Models / profiles → Models** and choose the Web export; both compatible profiles load automatically for A/B selection.

The export contains ten trees, 35 split nodes and four used predictors. The source CDH JSONL contains 100 records and four executions matching the model name. One has empty/non-finite used inputs and is excluded. The selected pair shares action and treatment context and follows different leaves in eight of ten trees. Both supply all four used inputs. Selection does not filter on modelVersion or reinterpret the recorded technique (`0.0`). These executions predate the supplied model snapshot; this is deterministic replay of their inputs against that snapshot, not source propensity reproduction.

The APK contains the unchanged model export and two minimized CDH files. Each profile file retains its selected execution, required common inputs, action/treatment context and recorded outputs. Unrelated executions and unused common customer attributes are omitted. Original files, selection mapping and independent references are kept under ignored `data/private/web-20261012/`. Generated APK inputs are under ignored `Resources/LocalLibrary/Models` and `Resources/LocalLibrary/Profiles`; these private resources are not committed.

At startup, bundled JSON TextAssets are copied into the existing local model/profile library through its content-addressed save method. Repeated startup does not duplicate unchanged files or overwrite existing files. The existing import path validates and activates the chosen model and its compatible profiles. Scores remain explicitly separate from calibrated CDH propensity.

## Verification

Editor compilation passed. Both selected profiles matched an independent Python traversal of the original export for all ten reached leaves, every contribution and the complete sum (tolerance 1e-12). Evidence: `artifacts/web-bundle-2026-10-13/scoring.json`. Build and playback verification are recorded below when completed.

## Reproducing this private bundle

Keep the original model filename when copying the approved export from `data/private/` into `BoostingExperience/Assets/VRExperienceAGB/Resources/LocalLibrary/Models/`. Copy only the selected, validated profile JSON files into the sibling `Profiles/` directory. Unity imports them as TextAssets and retains their generated metadata locally. Build Android normally. A clean checkout without these ignored files does not include the private bundle.

## Scene verification

All four local-library and three AGB/CDH PlayMode tests passed. In a separate live Editor scene, the bundled files were seeded twice into an empty library, yielding one model and two profile files without duplicates. Opening the saved model automatically loaded the pair. A/B playback visited and completed every one of the ten trees, preserved the independently verified scores, retained the calibration notice and displayed Table View results. Evidence: `scene.json`, `library-tests.json` and `cdh-tests.json` in the evidence folder. Quest was not connected during these checks; this is Editor verification.

## Android build

Development build `build_a521c9215661` succeeded with zero errors and six warnings. The build report explicitly lists the Web export and both selected profile files in its packed assets. APK: `artifacts/builds/VRExperienceAGB-web-model-profiles.apk`, 119,558,644 bytes, SHA-256 `0edb8e1f5185c6cff7b39ccbbb5f6fb55a53283cc9df2c815ad926bcc3e03d81`. The current Carezza environment changes already present in the workspace are included. No Quest device was connected, so this APK has not been installed or tested on the headset. Generated font cache changes were restored; `git diff --check` passed.

## Subsequent Quest installation

After Quest was reconnected, `adb install -r` returned `Success` and the application was relaunched on device `2G97C5ZHBX02V4`. A device directory listing confirmed the bundled Web export in `Models` and both selected CDH profile files in `Profiles`. Physical A/B playback acceptance on Quest remains pending.
