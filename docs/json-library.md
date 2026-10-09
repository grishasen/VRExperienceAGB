# Importing models and profiles

The main menu's **Models / profiles** page imports JSON documents and opens saved files without rebuilding the APK. Android uses a system document picker when one is installed; the Editor uses a desktop file dialog. The connected Quest has no handler for `ACTION_OPEN_DOCUMENT`, so the import action opens the Wi-Fi receiver instead. Reading, parsing and profile validation run off the main thread. The current model is replaced only after successful validation.

## User flow

1. On Quest, open **Models / profiles → From computer / Wi-Fi → Start receiving**. Open the displayed address on a computer on the same Wi-Fi and enter the eight-digit code. Upload the model first, then its profile JSON files. Up to eight received files can wait in order while the headset is off your head; put it on again to validate and open them.
2. Alternatively, if a document picker is installed, use **Import model JSON** to choose a local document such as a file in Downloads. Select a normalized v1 scoring model or supported nested AGB export. The imported model opens as a forest and remains available under **Saved models**.
3. For a scoring model, use **Import profile JSON** for each profile file. A file may contain one or several profiles. Compatible saved profile files are combined automatically; files belonging to other models remain saved but are excluded.
4. Return to the main menu and select **Play a prepared profile** or **Compare profiles A / B**. Cycle the A and B selectors independently. Pause, Calculate whole model, final forest and Table View results work with imported data.
5. After restarting the application, open the saved model to restore its compatible profile collection. The bundled demonstration remains the startup scene.

The library stores copies in separate `Models` and `Profiles` directories below `Application.persistentDataPath`. The in-app **Formats / storage folders** page shows the exact device paths. On this Android package they are normally:

```text
/sdcard/Android/data/com.vrexperienceagb.prototype/files/Models/
/sdcard/Android/data/com.vrexperienceagb.prototype/files/Profiles/
```

The system picker grants access only to the selected document; broad storage permission is not requested. A temporary cache copy is validated before being saved. Files stay on the device and survive application restarts and in-place APK updates; uninstalling the application can remove application-owned data. The Wi-Fi receiver is local, starts only on request, requires a per-session code and stops on demand or after 15 minutes. It does not upload to a cloud service. Network access is enabled in the Android manifest for this receiver.

Imported filenames contain a content hash so different files do not overwrite each other. Reimporting the same name and contents is idempotent. Profile IDs are internally qualified by their source filename, allowing A and B files to use the same original ID. The selectors show the source name and profile display name. Invalid imports preserve the active model; malformed or incompatible saved profile files are counted and skipped. JSON larger than 32 MiB is explicitly rejected rather than truncated.

## Supported formats

A normalized model uses the existing [model contract](model-contract.md), including `schemaVersion`, `modelId`, objective, base score, features and ordered trees. `data/examples/demo-model.json` is a complete fictional example.

A profile file uses this envelope (feature names and values must match the selected model):

```json
{
  "schemaVersion": 1,
  "modelId": "the-exact-model-id",
  "profiles": [
    {
      "id": "profile-a",
      "displayName": "Profile A",
      "values": {
        "a_numeric_feature": 12,
        "a_categorical_feature": "an_allowed_category"
      }
    }
  ]
}
```

Use `data/examples/demo-profiles.json` with the demonstration model. `artifacts/json-import/example-library.zip` contains the same synthetic model and two separate A/B profile files in their respective directories. Unzip it before importing individual JSON files.

Supported nested `AdaptiveBoostScoringModel` exports open for forest/tree structure exploration. They do **not** gain scoring support merely by importing profiles: production feature policy, baseline/weights and source-score agreement remain subject to the model contract. The UI rejects profile playback on a structure-only model explicitly.

## Developer USB workflow

With ADB configured and the application started once:

```sh
adb shell mkdir -p /sdcard/Android/data/com.vrexperienceagb.prototype/files/Models /sdcard/Android/data/com.vrexperienceagb.prototype/files/Profiles
adb push data/examples/demo-model.json /sdcard/Android/data/com.vrexperienceagb.prototype/files/Models/
adb push data/examples/demo-profiles.json /sdcard/Android/data/com.vrexperienceagb.prototype/files/Profiles/
```

Choose **Refresh files**, then open the model. Opening a saved profile file validates it against the current model and reloads the compatible collection. Real customer files remain outside tracked fixtures; on the development machine use ignored `data/private/`.

## Verification

The connected Quest returned `No activity found` for `ACTION_OPEN_DOCUMENT` with CATEGORY_OPENABLE and any MIME type. This is why Wi-Fi upload is included rather than relying solely on the system picker. Full baseline suites passed 184 EditMode and 84 ordinary PlayMode tests before the Wi-Fi addition; two HTTP protocol tests also passed. Four final library PlayMode integration checks passed, including a real TCP upload into the scene. Android compilation alone does not confirm availability of a document picker on a particular Quest OS version.

The Android Development APK built successfully with zero errors and seven warnings (Pipeline runtime configuration, the existing development-build conditional, TMP shader/code-generation notices, and a duplicate INTERNET declaration). It was installed on Quest 3. The actual device HTTP endpoint accepted the synthetic demo model and separate A/B files, saved them to the correct folders, and completed all three trees with raw scores **-4.1 / -2.6** and probabilities **0.01630249937144095 / 0.06913842034334682**. After force-stopping and relaunching the app, reopening the saved demo model restored all three trees and both profiles. The receiver was off after restart. The result was captured from the headset. The browser upload page was visually checked in Chrome.

Direct access to the headset LAN address from the development host failed with `No route to host`. The same device HTTP endpoint was therefore tested through `adb forward`; this confirms Android import/evaluation but does not certify the direct Wi-Fi route. Native picker interaction is untested because this Quest has no document picker.

Build: `artifacts/builds/VRExperienceAGB-json-library.apk`, 109342602 bytes, SHA-256 `d6b20b8a6c6dad34e0b16b8d154bc95794230d24a62c6a4f8f405bb3192f55f8`. Evidence lives in ignored `artifacts/json-import/`: `editmode.json`, `playmode-before-wifi.json`, `http-tests.json`, `playmode-library.json`, `build.json`, `device-compare.json`, `device-reopened.json`, `device-result.png`, and `browser-upload.png`.


## Sharing screenshots

Press **X on the left controller** (or **F12** in the desktop preview) to save the current application view. The capture renders one ordinary 1600×900 image from the current head position and direction using a temporary non-XR camera; it does not move the tracked camera. PNG writing runs off the main thread. A short confirmation appears after the PNG is complete, so it does not cover the image. Holding the button does not repeat the capture; a two-second cooldown prevents accidental rapid captures.

Files are stored separately under `Application.persistentDataPath/Screenshots/`. Open **Models / profiles → From computer / Wi-Fi → Start receiving**, connect with the session code, then choose **Download screenshots** in the browser. The page lists the latest 100 saved captures; refresh it after taking another picture. It does not expose models, profiles or arbitrary device paths. The receiver must be explicitly enabled to download. USB alternative:

```sh
adb pull /sdcard/Android/data/com.vrexperienceagb.prototype/files/Screenshots/ artifacts/headset-screenshots/
```

The first native ScreenCapture attempt on Quest returned a black image. The final implementation renders a separate mono image using URP SingleCameraRequest, with XR disabled for the temporary camera. Editor checks verify actual scene pixels, dimensions and unchanged head pose. Device validation is recorded in the Quest feedback/testing documents.

The final APK produced a visually inspected 1600×900 image on Quest. Screenshot listing and download from the actual device were verified through USB forwarding with exact byte agreement. Physical X-button acceptance and direct Wi-Fi routing remain unverified.
