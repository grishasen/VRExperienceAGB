# Northern winter sky — October 5, 2026

The environment now uses a fixed winter star field for latitude **55 degrees north**, with local sidereal time **6 hours**. This represents approximately a mid-January late evening in local solar time. It is a seasonal scene, not a live clock, longitude lookup or exact dated ephemeris. World +Z is south, -X east, +X west and -Z north.

## Catalog and presentation

`Resources/WinterStars.csv` contains 5070 HYG v4.1 entries of visual magnitude 6.0 or brighter, with the Sun removed. Right ascension, declination, magnitude and color index come from the [HYG catalog](https://github.com/astronexus/HYG-Database/blob/main/hyg/README.md). Coordinates retain the source's J2000 mean equatorial reference; precession, proper motion, atmospheric refraction and solar-system ephemerides are not simulated.

The fixed orientation places Orion near the southern meridian, Sirius southeast, Procyon farther east, and Taurus/Pleiades west of Orion. Capella, Gemini and northern circumpolar stars are also present. Brightness and warm/cool color are mapped from catalog magnitude and B-V, with artistically enlarged star points for headset visibility. The angular pattern is preserved; sprite sizes are not physical stellar diameters.

The generated twilight panorama is replaced by a dark gradient. At the user's request the Moon remains visible: its original procedural surface is reused at a 0.65-degree apparent diameter, southwest of the winter landmarks. The old oversized mesh and its clones are hidden. Lunar placement/phase is artistic and is not a dated ephemeris. Existing ground lighting, wolves, controls and model evaluation are retained.

Stars use one shared mesh and a camera-relative stereo shader, with terrain occlusion. Neither locomotion nor the tracked head/controller transforms are modified. Shader resources are bundled explicitly for Android builds.

## Meteors

One original procedural streak at a time, lasting 0.85 seconds, high above the horizon. Starts are separated by 22–34 seconds, after a 26-second initial delay: at most three events in any rolling 60-second window. Timing does not catch up in bursts after a stall. Focus loss, application suspension, explicit pause and modal help/comparison/extensions panels suppress the effect. These are decorative sporadic meteors, not a forecast or a simulated named shower.

## Attribution

The HYG v4.1 data and adapted CSV are CC BY-SA 4.0, credited to David Nash / Astronomy Nexus. The bundled `Resources/WinterStars-Notice.txt` identifies the source, changes, license and source checksum. No external sky image or meteor footage is used. Original shaders and implementation are separate from the licensed catalog data.

## Verification

- Unity 6000.6.3f1 Editor compilation completed without errors.
- Full EditMode suite: 177 passed, zero failures (`artifacts/winter-sky/editmode.json`).
- Full PlayMode suite: 70 passed, zero failures (`artifacts/winter-sky/playmode.json`), including catalog landmarks, east/west orientation, Moon separation, unchanged tracked head pose, navigation/import survival and meteor focus suppression.
- Three actual 1800 × 1200 Editor captures use the same south-facing camera at 30 degrees elevation: `artifacts/winter-sky/variant-1.png` (natural/default), `variant-2.png` (blue moonlit background), and `variant-3.png` (stronger bright stars with subdued faint stars). They are visual proposals, not a new user-facing settings panel. These initial proposals are retained for comparison; the selected moonlit revision below supersedes variant 1.
- Quest stereo appearance, perceived brightness, meteor comfort and hardware performance remain pending. Editor captures are not headset acceptance evidence.

Android Development build succeeded with zero errors and five existing Pipeline/TMP warnings. APK: `artifacts/builds/VRExperienceAGB-winter-sky.apk` (101126290 bytes), SHA-256 `db72ac7b6754e4841d57f007f5af1809e306bbd310f0e3029e688bebacb82bcb`. Build report: `artifacts/winter-sky/build.json`. The APK has not been installed or checked on Quest during this update.

## Selected moonlit revision

The user selected the frosty moonlit direction with brighter stars and the faintest stars removed. The default now has a visibly blue gradient, a low horizon haze and a soft directional lunar aureole. Stars fainter than magnitude 5.0 are omitted from the rendered mesh (795 stars above the horizon); the source catalog remains intact. Retained stars use 1.8 times the previous intensity and slightly larger points. Catalog positions, lunar position and meteor cadence are unchanged.

Editor compilation and all three focused WinterSky PlayMode checks passed after this change (`artifacts/winter-sky/moonlit-tests.json`). The revised actual Editor capture was visually inspected: `artifacts/winter-sky/moonlit-selected.png`. The full 177/70 suites above describe the preceding revision; headset appearance remains unverified.

Revised Android Development APK: `artifacts/builds/VRExperienceAGB-moonlit-sky.apk`, 101932517 bytes, SHA-256 `813012df4e84394ba44fcdf927659a75f5bd8e17ba889f83608c6c1c6d623458`. Build succeeded with zero errors and five existing Pipeline/TMP warnings (`artifacts/winter-sky/moonlit-build.json`). Not installed on Quest during this revision.

## Headset installation — October 7, 2026

At the user's request, the moonlit APK identified above was installed as an update on the connected Quest 3 using `adb install -r`. ADB returned `Success`; the local APK checksum matched the recorded SHA-256. The app was not launched during this installation task. Visual, interaction and performance acceptance remain pending.
