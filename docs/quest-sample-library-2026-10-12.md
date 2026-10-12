# Complete sample library and Quest controls — October 12, 2026

## Files and identity

The requested `export_Mobile_Click_Through_Rate_AGB_sample.json` contains **100 trees and 200 predictors**. It is distinct from the bundled 50-tree `export_Mobile_Click_Through_Rate_AGB_demo.json`. Neither export was edited or truncated.

The exact sample was copied to the Quest app's persistent `Models` folder. Two separate synthetic files, `AGB_sample_synthetic_A.json` and `AGB_sample_synthetic_B.json`, were copied to `Profiles`. The common directory is `/sdcard/Android/data/com.vrexperienceagb.prototype/files/`.

Open **Models / profiles → Saved models → export_Mobile_Click_Through_Rate_AGB_sample**. Compatible profiles load with the model; both files are also selectable under **Saved profile files**. The main menu now shows the active filename, tree count and profile count. The later [default-model revision](default-sample100-2026-10-12.md) starts with this same 100-tree sample and two profiles automatically. The earlier library candidate recorded below started with the 50-tree demo.

Source SHA-256: `6cd87c1839a0ab96fd2a973f5fa3256a0a6b391ebd75bc3c76edcb408485b363`.

The reproducible command below creates two threshold/category-derived synthetic inputs and independent Python leaf-sum references. These fixtures are not representative customer data. Generated files remain in ignored artifacts.

```sh
python3 scripts/prepare-library-profiles.py \
  data/examples/export_Mobile_Click_Through_Rate_AGB_sample.json \
  artifacts/quest-sample100-2026-10-12/fixtures
```

| Profile | Expected full score |
| --- | ---: |
| Synthetic A | -4.936703202173835 |
| Synthetic B | -3.6144993159119045 |

Propensity requires a separate calibration mapping. These are raw tree scores, with no inferred calibrated probability.

## Controls and sound

Stick walking had defaulted to off. It now starts enabled in the forest; Comfort still allows teleport-only and speed adjustment. Reading panels and menus block movement. Left stick walks; right stick snap-turns. Manual tree exploration and profile playback retain their existing scene navigation.

**Tools → Comfort and feedback → Hear wolves in forest** closes the menu, opens the forest, enables ambience and requests a spatial howl immediately. It honors application focus/pause. This exercises the same recorded clip and spatial source as ordinary scheduled wildlife; no audio replacement was needed.

Quest reported media volume 0/15 while asleep. Programmatic volume and volume-key attempts did not change the reported value. Physical volume and listening acceptance must be checked while wearing the headset.

## Verification

- Unity 6000.6.3f1 compilation passed.
- PlayMode: **105 passed, zero failed, one opt-in video test skipped**. New regression covers default forest walking, the normal-menu howl action, and active model identity. Existing checks cover the remaining application features.
- Imported the exact sample and both generated files in the normal Unity scene. Both complete scores and all **200 reached leaf addresses** match the independent Python reference. Traversal completed all 100 trees in 1,422 A/B playback state transitions.
- Inspected the rendered main menu with the full filename and `100 trees · 2 profiles`.
- Normal saved-file menu selection in the Editor imported the sample, automatically loaded both separate profile files, calculated the matching A/B scores and visited manual tree, forest and table views. All three device file SHA-256 fingerprints match the local originals.
- Device installation and runtime evidence are recorded below. Automated commands are diagnostics within the full application, not a separate restricted experience.

## Headset walkthrough

Record each item as Pass / Fail / Not tested. Programmatic activation does not establish controller usability or perceived audio quality.

1. Saved models: open the exact sample. Main menu must show the filename, **100 trees**, **2 profiles**; check both profile files are listed separately.
2. Explore the forest: move with the left stick, turn with the right stick, use teleport targets. Open the menu and confirm stick movement stops; close it and resume. Check seated/standing and recenter.
3. Raise headset media volume. Use **Hear wolves in forest** and listen for a positional call; wait for the next scheduled call. Test Ambience off/on and headset removal/resume.
4. Enter a tree, choose both manual branches in separate runs, inspect/pin node details, go back/restart and return to the forest. A manual route must remain a route score.
5. Compare profiles A / B: inspect exact values and differences, run Learn/Fast/Step, pause/resume, previous tree/restart, then Calculate whole model. Compare the full scores with the table above.
6. Show forest/table results; inspect contributions and first/last trees. Switch table groups/labels, scale/rotate, and return to the same result.
7. Exercise Learn, model passport, dictionary, glossary, verification, predictor search, boosting trail, recorded reviews and What-if edit B. Cancel and Apply must behave distinctly.
8. Take a screenshot with X; use Tools → Performance capture in forest, tree, A/B and table. Check sustained performance while wearing the headset; no performance acceptance is inferred from Editor tests.
9. Relaunch: verify the 100-tree sample and two profiles start automatically, the files persist, and reopening the sample produces the same calculation. Check the active filename/count.

See also [the full acceptance checklist](quest-acceptance-2026-10-11.md).

## Installed candidate

`artifacts/builds/VRExperienceAGB-sample-library.apk` — Android IL2CPP Development, 101,973,171 bytes, SHA-256 `54aa7c335ecc17bbc09b6b389697648bf0b5447dc93a3b1fb6656495a7f4729b`. Build `build_338d6009df38` succeeded with zero errors and eight warnings (Pipeline runtime configuration, deprecated diagnostic APIs/directive, TMP shader/compiler warnings). Installed with `adb install -r` successfully; existing user files were retained. Launched `com.vrexperienceagb.prototype`.

At the initial post-install check the headset was asleep. Unity logged that the surface was destroyed without focus and paused. The request to open the exact saved sample was queued for wake-up. On-device import/calculation, physical controller movement and perceived howl loudness remain pending until the headset is awake and worn. No runtime pass is inferred from a successful installation.

### Awake headset follow-up

After the user put on the headset, the exact sample imported successfully: 100 trees, two compatible synthetic profiles, expected model fingerprint. Two older incompatible profile files were skipped without deletion. Diagnostics showed active XR application focus, enabled stick walking, no modal movement block, one active audio listener and a playing wolf source. **The user confirmed that both stick movement and the howl work.** The earlier volume-zero readout was not sufficient to establish actual listening state; the user's in-headset observation is the acceptance evidence.

Full A/B calculation on Quest matched the independent reference within 1e-10. Ordinary automatic A/B playback, started through Compare → Start, reached tree **51/100** (zero-based index 50) and paused correctly when application focus was lost. A headset screenshot was inspected at tree 14 with both profile routes and exact input values. The user then requested that the current run be left for independent testing. No claim is made that the entire on-device animation completed; the full 100-tree calculation already matched, and the complete 1,422-transition traversal was verified separately in the Editor.

### Initial performance sample

A 60.002-second mixed Quest capture recorded 3,708 frames at **61.80 FPS average**, median **14.48 ms**, p95 **21.22 ms**, p99 **31.56 ms**, and a **494.53 ms maximum** during a run containing tree, result-menu, forest and playback transitions. The display reported 72 Hz; this capture does **not** meet sustained 72 FPS acceptance. Six interrupted frames were recorded; GPU timing was unavailable. Peak allocated Unity memory was approximately 176 MB. This is a mixed functional session, not a steady-state or thermal acceptance measurement. Further performance tuning and separate per-view captures remain open.

Evidence: `artifacts/quest-sample100-2026-10-12/Measurements/capture-20261012-072017-738.json`.

### Handoff

The user confirmed walking and howling, then chose to continue the remaining checks independently. The app was left with the imported 100-tree sample and both synthetic profiles in the current A/B session. A opens the playback menu; **End playback · forest** returns to exploration. All normal application menus remain available. No pending test request was added at handoff.

Artifacts for this revision are under `artifacts/quest-sample100-2026-10-12/`, including the APK build report, PlayMode results, Python references, exact leaf comparison, saved-file menu checks, device hashes, A/B calculation/progress reports, screenshot and frame measurement. The current device log contains the existing optional OpenXR `xrDiscoverSpacesMETA` lookup error; no null-reference or fatal exception was found in that captured app log.
