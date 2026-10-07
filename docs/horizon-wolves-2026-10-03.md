# Horizon wolves and distant howling — October 3, 2026

Implements the requested M5-05 atmosphere addition. Three original low-poly wolves stand on two distant rocky ridges, visible from the forest and individual-tree experience. Pointed ears, raised muzzles, bushy tails and bent rear hocks form a howling silhouette. The head lifts slightly during a call. The wildlife has no interaction targets or colliders and is independent of tree evaluation, profile routes and tracked head/controller poses.

## Original sound and lifecycle (superseded October 7)

`HorizonWolves` generates one original six-second mono howl at 22,050 Hz. A rising/falling fundamental, formant-shaped harmonics, breath noise and gentle vibrato produce a stylized synthesized call. No animal recording, downloaded sample or external media service is used.

Each wolf has a fully spatial AudioSource at its muzzle, with linear distance attenuation, no Doppler shift, a low background volume and a slightly different pitch. Only one wolf calls at a time. The first call occurs after nine seconds of eligible ambience; subsequent calls start 32–54 seconds apart and rotate between wolves.

The existing **Sound on / off** control stops and mutes every wolf immediately. Unmuting schedules a new call after a delay rather than resuming in the middle of a howl. Help, comparison panels, explicit session pause and application focus/pause loss suppress calls. Wildlife remains visually present while silent. The scene's original breeze and selection cue are retained.

The wildlife root is outside the teaching presentation and garden rebuild roots. A model reload or profile comparison does not duplicate it. For longer gardens the ridges are placed farther beyond the last beds. The complete wildlife layer contains 3,236 rendered triangles. This geometry count is not a device performance result. Shared body/head/ridge meshes and two materials are created once, and generated meshes, materials and PCM are released when the component is destroyed. No package, project-setting or tracked scene change is required for this addition.

## Verification

Unity 6000.6.3f1 compiled the implementation. Five focused PlayMode tests passed for spatial configuration, mute/resume and interruption behavior, independence from model and tracking state, model/comparison reloads, bounded mono PCM and silent clip boundaries. Horizon and detail captures were inspected; ridge geometry was adjusted to support the wolves' feet.

The complete PlayMode suite passed **59 tests**, including the existing 54 scene/comparison regressions. The ridge seam was subsequently refined and all five focused wildlife tests passed again before the candidate build. The domain/evaluator was unchanged; its prior 166-test EditMode result was not rerun for this scenery addition. Detailed reports, PNG captures, geometry counts and a WAV preview are stored under ignored `artifacts/horizon-wolves/`. No headset appearance, perceived loudness, sound realism or performance acceptance is claimed; those checks remain deferred to the final Quest pass as requested.

## Android candidate

The Android Development build succeeded with zero errors. APK: `artifacts/builds/VRExperienceAGB-horizon-wolves.apk`, 127,299,329 bytes; SHA-256 `130b65d54ab41bf9df4c3c189a1cc82b6fdd70d56f49f780bae11aebca957ffd`. It includes the preceding A/B comparison feature and was not installed or run on Quest.

The build is based on `f7a27a8746c5d92395c996e5377899c20610e5a7` plus the local uncommitted implementation. `artifacts/horizon-wolves/build-manifest.json` records the exact APK identity and C#/scene hashes; the detailed build log is alongside it. Package versions and project settings are unchanged. Unity cleaned up its temporary build resources; incidental TMP fallback-font serialization was excluded.

## Recorded howl replacement — October 7, 2026

The user rejected the synthesized call as unrealistic after headset use. `HorizonWolves` now loads `Resources/WolfHowl.wav`, an excerpt of the U.S. Fish and Wildlife Service recording [Wolf howls](https://commons.wikimedia.org/wiki/File:Wolf_howls.ogg). Commons marks the official government recording public domain (PD-USGov-FWS). Source identity and adaptation details are bundled in `Resources/WolfHowl-Notice.txt`.

The 13.35–17.55 second excerpt is converted to mono 22,050 Hz PCM, amplified 4x (peak below 0.5), and faded over 150 ms at the start and 600 ms at the end. Playback keeps the original pitch. Positional attenuation, sound toggle, modal/focus interruption and 32–54 second start intervals are retained. The generated waveform code was removed; the imported shared clip is no longer destroyed by an individual wildlife component.

All five focused wildlife PlayMode tests passed (`artifacts/horizon-wolves/recorded-tests.json`), including resource identity, decoded mono PCM boundaries, unchanged pitch and mute/focus behavior. Listening acceptance on Quest remains pending.

The user accepted the audio preview. Android Development build succeeded with zero errors and five existing Pipeline/TMP warnings. APK: `artifacts/builds/VRExperienceAGB-recorded-wolves.apk`, 102680024 bytes; SHA-256 `6e3091feed00a45ad982a9779d4bdd105570894afecc07e80f03d987079d55e8`. Report: `artifacts/horizon-wolves/recorded-build.json`. This revision was not installed on the headset during the sound replacement task.
