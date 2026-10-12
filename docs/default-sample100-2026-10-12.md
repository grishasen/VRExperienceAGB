# Default 100-tree sample — October 12, 2026

The user-selected `export_Mobile_Click_Through_Rate_AGB_sample.json` is now the startup model. `scripts/prepare-local-sample.py` bundles all **100 trees**, **200 predictors** and exactly **two synthetic profiles**, A and B. No manual file selection is required after launching the prepared build. The saved Models/Profiles library remains available.

The bundle uses the same source fingerprint/model ID and exact profile IDs, names and values as the separate synthetic files already stored on Quest. Python comparison confirmed equality and matching full scores:

- A: `-4.936703202173835`
- B: `-3.6144993159119045`

The model remains unchanged; no trees are omitted. The UI reports raw score and the existing calibration notice. Propensity is not reconstructed from these tree scores.

Preparation reads the ignored local source at `data/examples/export_Mobile_Click_Through_Rate_AGB_sample.json` and writes ignored `Resources/LocalModel` assets. Existing asset metadata is preserved. Missing source data causes preparation to fail; it does not silently choose the 50-tree demo. Rebuild after running:

```sh
python3 scripts/prepare-local-sample.py
```

Startup tests now require the correct filename, 100 trees and two profiles, and compare every reached leaf and both scores with the generated independent traversal. Opt-in video captions also identify the 100-tree sample. Validation and installed build identity follow below.

The larger startup run exposed an ambiguous test lookup: several buttons can own inactive sibling objects named `ButtonHint`. The assertion now selects the active hint and reads the hovered button's own hint text. No tooltip product behavior was changed for this test correction.

## Verification

- Unity 6000.6.3f1 compilation passed.
- Complete PlayMode rerun: **108 passed, zero failed, one opt-in video test skipped** (81.96 seconds), including full 100-tree A/B playback and per-leaf reference comparison.
- Fresh Editor scene startup, with no library import: bundled model active, correct filename and source fingerprint, 100 trees, 200 predictors, two profiles, matching A/B scores and bound export metadata.
- Original export bytes and both profile value sets match the existing saved-file fixture. Prior forest and branch-control fixes remain included.

Evidence is stored under `artifacts/quest-default100-2026-10-12/`.

## Installed build

`artifacts/builds/VRExperienceAGB-default100.apk`, 108498555 bytes; SHA-256 `3ddd87364f105e3559f0c72a1eaa79dc3434441569ee092e67922a402788f96f`. Android IL2CPP Development build `build_5302ffd0184f` succeeded with zero errors and 2 warnings. Installed with `adb install -r`, then explicitly restarted without a library import. The previous queued diagnostic import was removed before deployment so it could not mask the startup default.

The headset was asleep at installation. Fresh Editor startup is verified; physical headset acceptance of this revision remains pending. Saved model/profile files are retained.
