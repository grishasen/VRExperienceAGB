# Central hill wolf — October 12, 2026

The user confirmed that increasing the Quest's physical volume restored the howl. A live device report had already confirmed that the audio source was playing, but the screenshot taken during that event did not show a wolf among the restored large pines.

At the user's request, one wolf now stands on a central rocky hill at the end of the forest's main aisle. The hill shares the existing ridge mesh. The central wolf is larger (scale 1.8) to make its distant silhouette legible. The two side wolves remain. Wildlife stays beyond the model beds as tree count grows; the 100-tree sample puts the central wolf at (0, 7.7, 75).

The first automatic call comes from the central wolf. **Hear wolves in forest** always starts that wolf, including after a side wolf has called. Later automatic calls still rotate. Existing sound/mute, menu and application-focus rules apply; the wolf appears during the call and then leaves.

Unity compilation passed. The 100-tree entrance view was rendered and inspected: the central wolf and hill are visible through the main aisle, with the large pines retained. Image: `artifacts/quest-central-wolf-2026-10-12/editor.png`. Full PlayMode regression passed **111 tests**, zero failed and one opt-in video test skipped (85.83 seconds). This includes explicit central-wolf selection after another wolf calls, menu/input restoration, and A/B playback. Android Development build `build_8c1b0c18a46d` succeeded with zero errors and five warnings. Installed successfully with `adb install -r` and explicitly restarted. APK: `artifacts/builds/VRExperienceAGB-central-wolf.apk`, 128920902 bytes, SHA-256 `115a9db4306a1bb0507ba40e017df9d3eb16775022bfbd25d110770c5dd9acd7`. Physical headset visibility acceptance remains pending; the Editor image does not establish it.

## Lower hill and brighter fur follow-up

At the user's request, the central ridge's vertical scale is now 0.5: its summit and the wolf's resting height are **3.85 m instead of 7.7 m**. Width, depth and wolf size are unchanged. Only the central wolf uses a separate silver fur material with a restrained emission contribution, improving contrast in the existing night lighting; body, head, tail and fur tufts share it. The material is released with the wildlife layer.

Compilation and a fresh 100-tree Editor render passed visual inspection (`editor-lower-brighter.png`). The silhouette remains unobstructed and is brighter. The prior 111-test result above predates this appearance-only adjustment; the suite was not rerun for material and placement values. Build `build_6ee13d5c5ce7` succeeded with zero errors and six warnings; installed successfully and restarted on Quest. APK: `artifacts/builds/VRExperienceAGB-wolf-contrast.apk`, 128919419 bytes, SHA-256 `2722a8309b22512b81312df0bbda2423d3ed51673786199df399b2ffd8437171`. The user accepted the lower hill and brighter wolf on Quest before requesting commit, push and a final build.

## One-minute arrival delay — October 17

Automatic howling now waits for 60 seconds of eligible exploration after application startup and after each successful entry into a tree. Entering a tree stops any current howl and starts a fresh minute, including when revisiting the same tree. Ordinary tree refreshes do not reset the timer. Menus, mute and application focus/pause interruptions suspend the initial countdown without replacing it with the shorter resume delay. The explicit **Hear wolves in forest** command still plays immediately; later automatic calls retain their 32–54 second intervals and rotating callers.

The accompanying tree-card cleanup removes SHA-256 from the root-condition section of tree Details, matching the split card. Full source hashes remain in the model and audit descriptions.

Compilation succeeded. The full PlayMode suite passed **128 tests**, with zero failures and one optional video case skipped (109.40 seconds). The arrival-delay regression verifies the first call, mute/focus/pause/help interruptions, repeated entries into different and previously visited trees, silence before 60 seconds, and continued normal call intervals. Existing tests cover immediate manual howling, source-card content and navigation, manual exploration and prepared A/B playback. Report: `artifacts/tree-card-wolf-delay-2026-10-17/playmode-tests.json`. Real-time headset listening and performance acceptance remain pending.

Android Development build `build_1b3a21c6b99e` succeeded in 28.09 seconds with zero errors and six existing Pipeline/TMP/performance-probe warnings. Installed with `adb install -r` (`Success`) on the connected Quest 3, preserving app data, and launched the main activity. APK: `BoostingExperience/Builds/Android/VRExperienceAGB-tree-card-wolf-delay-2026-10-17.apk`, 119,589,656 bytes; SHA-256: `7fdf2ce788797d01201d584db03721ff5747a53bb362884d0caaf95c3dfb09c6`. Build evidence: `artifacts/tree-card-wolf-delay-2026-10-17/build-status.json`.
