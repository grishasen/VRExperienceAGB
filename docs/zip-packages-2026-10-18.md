# ZIP model and profile packages — October 18, 2026

Status: implemented and compiled in Unity 6000.6.3f1. All 218 EditMode tests and all six JSON-library PlayMode tests pass. The non-Development ARM64 Android APK is built and signature-verified; installation and headset acceptance are pending.

## Behavior

Import one ZIP with exactly one supported model and one or more profile JSON/JSONL files through **Import model / ZIP** or the Wi-Fi browser upload page. The same action validates, saves and opens the complete package. Subfolders and repeated profile basenames in different subfolders are supported; Finder metadata is ignored.

Package membership is the explicit association: normalized profile modelId and CDH rule/component names do not need to match the model. CDH files contribute their ADM executions, preserving their original source fields. Complete input validation and evaluation still run before publication; incorrect values, unresolved references and unsupported model conditions reject the package. Recorded CDH outputs never become calculated results.

Storage uses `Packages/<archive-name>-<content-hash>/Models/` and `Profiles/`. The active model's package folder determines its profile collection. Separate archives cannot share profiles even when their model JSON is identical. Loose imports retain their existing identifier/name matching and cannot join a package. Reopening a saved model restores only that package's profiles. Reimporting identical bytes under the same archive name reuses the folder; changed archives get a new folder.

The importer bounds archive size, expanded file size, total expansion and entry count, rejects unsafe/duplicate paths and links, and validates in an invisible staging folder before one directory move publishes the package. Failed imports remove staging and preserve the accepted library/session. Temporary upload files remain subject to the existing cache cleanup.

## Verification

- Editor compilation completed with no C# errors.
- **218/218 EditMode tests passed**, including existing model, evaluator, CDH and HTTP regressions. New checks cover package identity, different profile collections for identical models, loose-file separation, storage reconstruction, reused profile IDs/basenames, idempotent import, renamed exports, normalized modelId rebinding, retained CDH provenance, invalid inputs, multiple/missing models, unsupported files, traversal/absolute paths, duplicate paths, symlinks, entry limits and expanded per-file limits.
- A final **14/14 ModelFileLibraryTests** run passed after requiring the model entry to use `.json` so saved-model discovery works. The run also covered corrupt ZIP input and the 64 MiB aggregate expansion limit.
- **6/6 JsonLibraryPlayModeTests passed**. Real TCP binary ZIP upload reached the main-thread queue and activated model/profiles together. Complete A/B evaluation, a second package with identical model bytes, library recreation/reopening, selecting a saved package profile, and failed imports preserving the accepted model/session/playback were verified. Existing separate-JSON and structure/manual-exploration checks also passed.
- A synthetic example ZIP was generated from the tracked `demo-model.json` and `demo-profiles.json`; no customer data was added to tracked fixtures.
- Evidence: ignored `artifacts/zip-import-2026-10-18/editmode.json`, `playmode-library.json`, `editmode-packages-final.json`, build reports and `example-package.zip`.

## Android artifact

Final build `build_47d70234d317` succeeded in 25.750 seconds with zero errors and four existing warnings: optional Pipeline runtime configuration and three TextMeshPro code-generation notices. The APK is ARM64 / IL2CPP, non-Development, with no debuggable manifest flag. APK Signature Scheme v2 verification passes.

File: `BoostingExperience/Builds/Android/VRExperienceAGB-Quest3-zip-import-2026-10-18.apk`.

Size: **92,436,244 bytes**. SHA-256: `0a34915bebb98efc77970b4cea5b5a9817c865249045b71b6ae6080a852785aa`.

Package: `com.vrexperienceagb.prototype`, version 1.0 / code 1. A checksum file is saved alongside the APK. The artifact uses the existing sideload signing configuration and has not been installed on the headset for this change.

## Device acceptance still required

On Quest, upload the synthetic example ZIP through the browser, select and complete a prepared profile or A/B run, restart the app and reopen the saved package. Import a second ZIP containing the same model and different profiles; confirm the selectors stay isolated. Check invalid-package feedback preserves the active run. Check menu readability and direct LAN routing; Editor TCP checks do not certify the headset Wi-Fi path. Native document-picker use remains unverified on Quest. No new performance acceptance is claimed.

See [the import guide](json-library.md#zip-packages) for the file layout and limits.
