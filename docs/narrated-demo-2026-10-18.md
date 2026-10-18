# Narrated application demo

The English demo follows the live application from model selection through manual tree exploration, deterministic profile playback, whole-model scoring, Table View, and comparison of two synthetic profiles.

## Capture and delivery

- Duration: 4 minutes 16 seconds, with 28 shots and eight navigation chapters.
- Picture: 2560 × 1440, 30 frames per second, H.264 MP4.
- Camera: desktop preview camera, 65° vertical field of view (approximately 97° horizontal at 16:9), eased movement and stationary reading shots.
- Sound: English synthetic narration using the installed macOS Daniel voice, mixed with the application's recorded stereo ambience.
- Text: English chapter headings, burned-in subtitles, a separate SRT, and brief explanations of the first A/B split.
- Local output: `artifacts/narrated-demo-2026-10-18/VRExperienceAGB-narrated-demo-English.mp4`.

This is a Unity Editor recording. It requires no headset and provides no evidence of Quest frame rate, controller tracking, or headset performance. Scripted poses apply only to the desktop preview camera.

## Storyboard

| Time | Content |
| --- | --- |
| 00:00 | Opening forest view |
| 00:12 | Saved model selection and ZIP package |
| 00:26 | Forest walk, lake, mountains, stars, and wolf |
| 01:14 | Tree 17, selected split, detailed condition, manual branch and leaf |
| 01:56 | Prepared profile, deterministic route, pause and resume, complete calculation |
| 02:44 | Whole forest on a table and rotation |
| 03:07 | A/B selection, diverging routes, complete scores, and table rings |
| 03:55 | Final forest walk and closing lake view |

## Data and scoring

The capture uses the local model resource containing 100 trees and two synthetic profiles. The recorder imports them through the application's ZIP library into an isolated artifact directory. It does not change the user's saved library. The local export and generated ZIP remain ignored artifacts.

Tree 17 is the focused example. Its first condition is `IH.Mobile.Inbound.Clicked.HistoricalOutcomeCount < 18`. Synthetic A supplies 7 and follows the left branch; Synthetic B supplies 19.8 and follows the right branch. Shortening the tour does not truncate the scoring ensemble: the application's **Calculate whole model** command evaluates all 100 trees.

The accepted comparison shows A = -4.936703, B = -3.614499, and B − A = +1.322204. These are ensemble scores. No calibration mapping is loaded, so the video does not present them as calibrated propensity. Manual branch choices are explicitly distinguished from a customer prediction.

## Reproduction

Use the installed Unity 6000.6.3f1 project and the local model/profile resources. The capture is opt-in; ordinary test runs skip it.

1. Write `preview` or `full` to `artifacts/narrated-demo-2026-10-18/record-request.txt`.
2. Run the PlayMode test `NarratedDemoVideoTests.RecordNarratedDemo`. Preview mode writes three framing images per shot. Full mode writes `scene.mp4`, `ambience.f32`, `shots.tsv`, review images, and `completed.txt`.
3. Run `python3 scripts/compose-narrated-demo.py prepare` with access to the macOS speech service. Empty speech output is rejected.
4. Run `python3 scripts/compose-narrated-demo.py compose` to mix narration and ambience, burn captions, and add chapters.

The recording uses `/opt/homebrew/bin/ffmpeg`; composition requires FFmpeg, Python, Pillow, and the macOS Helvetica font. Caption graphics are rendered with Pillow, so FFmpeg does not need libass. The SRT contains dialogue; the ASS sidecar also includes chapter and evidence text. Sentence timings follow the measured clip duration and word count, rather than forced speech alignment.

## Verification status

The full opt-in PlayMode capture passed: one test, zero failures, 7,680 frames, and 256 seconds. It asserted both profile availability and completion of all 100 trees. Capture execution took approximately 432 seconds on the recording Mac; this is offline capture time, not a headset performance measurement.

The final MP4 was decoded completely by FFmpeg without errors. FFprobe confirms 2560 × 1440 H.264 at 30 fps, 7,680 frames, stereo AAC at 48 kHz, matching 256-second video/audio durations, and eight chapters. The encoder reported zero duplicated and zero dropped frames. The file is 627,133,622 bytes.

All 27 voiced clips contain audio and fit their scene durations; the wolf interval has no narration. The final mixed audio has a measured peak of -3.8 dBFS and mean level of -20.1 dBFS. Fifty-one subtitle cues use at most two lines. Twelve frames extracted from the final MP4 were inspected, including full-size split and A/B result frames; subtitles remain below the key application content. This verification did not include a real-time listening review of the entire narration or headset testing.

Evidence is saved beside the video in `capture-test-result.json`, `final-probe.json`, `audio-verification.log`, `compose-progress.txt`, and `final-contact.jpg`. The narration script, clean scene master, and SRT are retained for revisions.

## Revision 2: Orion and female narration

The requested revision removes the wolf reveal and its howl from the demo. The recorder disables wildlife sound and visibility only in the filming session; the application's wildlife feature is unchanged. Shots 06 and 07 now look toward the opposite side of the sky, center on Alnilam, and move from a wide view to Orion. The second shot enables the application's constellation guides. All seven principal Orion stars are checked against the camera viewport.

Three female English voice samples are available in `artifacts/narrated-demo-2026-10-18/voice-options/`: Samantha (US), Karen (Australia), and Moira (Ireland). They use the same 56-word passage at 155 words per minute, normalized to the same loudness target. Their durations are approximately 21–22 seconds. The user rejected these system voices as too synthetic and selected Nova with the Patient Teacher delivery from OpenAI.fm; the original delivered MP4 remains intact.

For a sky-only capture, write `sky-preview` or `sky` to the request marker; output goes into `orion-revision/`. For a complete revised capture, use `revision-preview` or `revision-full`; output goes into `revision-2/`. The composition script now accepts `--voice NAME` and `--output-dir PATH`, so the revised narration and video can be prepared separately from version 1. The storyboard remains 4:16; the former wolf interval becomes the Orion detail shot with English narration.

The 16-second Orion replacement capture passed (480 frames, one PlayMode test, zero failures). Framing images were inspected, and the H.264/AAC preview is saved as `orion-revision/Orion-preview.mp4`. The complete revised capture also passed: one PlayMode test, zero failures, 7,680 frames and 256 seconds. Capture execution took 835.42 seconds. The clean master and test evidence are retained in `revision-2/`.

### Selected Nova voice and narration handoff

The supplied `openai-fm-nova-patient-teacher.mp3` is a 31.56-second reference. It is retained as `nova-narration/selected-voice-reference.mp3` beside the artifacts. No OpenAI API key or connected Nova synthesis tool was available, and the user chose to generate the narration on OpenAI.fm.

`nova-narration/full-narration.txt` contains the complete revised English script: 554 words and 3,437 characters. Four separate `part-01.txt` through `part-04.txt` files each remain below 1,000 characters and preserve shot boundaries. They contain only spoken text. `parts.json` maps those blocks to the recorded shots for the final edit, and `delivery-notes.txt` records the selected voice and handoff instructions.

### Supplied recordings and review cut

The user supplied four further Nova MP3 files. Local Whisper transcription confirmed that suffixes `-2` and `-3` cover shots 01–16, and `-5` covers shots 23–28. The latter ends after “Compare the outcomes”; the optional final sentence is omitted from the spoken closing. File `-4` contains an unrelated painting lesson and is excluded. The missing section was requested in `nova-narration/replacement-missing-section.txt`. The user then supplied a 38.352-second replacement covering deterministic scoring, full calculation, score interpretation, Table View, and table rotation.

The completed version uses the four matching recordings without changing speech speed or pitch. Edits occur inside detected pauses; measured word timings drive sentence captions. Nine shots are extended slightly to fit the natural delivery, yielding 265.667 seconds. Video timing changes only in those shots, mostly stationary reading views. The complete 100-tree scoring, wide forest views, and Orion framing are preserved.

The replacement ends after the table-rotation instruction. The redundant following explanation is omitted, and profile selection is a deliberate seven-second quiet transition with its chapter heading before the spoken A/B example. The result has 27 voiced shots, 521 spoken words, English captions, and eight chapters. No narration placeholder remains. The earlier incomplete review is superseded.

The final output is `artifacts/narrated-demo-2026-10-18/nova-final/VRExperienceAGB-narrated-demo-English.mp4`, with `captions.srt` and `final-narration-script.txt` alongside it. Original recordings, local recognition results, safe edit boundaries, and the assembly script are retained under `nova-narration/`; the final narration master and verification evidence are under `nova-final/`.

Final verification passed: complete MP4 decoding, 2560 × 1440 H.264 at 30 fps, 7,970 frames, stereo 48 kHz AAC, matching 265.667-second audio/video durations, and eight chapters. The file is 611,445,831 bytes. Retiming adds 290 duplicate frames across the extended shots; no frames were dropped. Measured audio peak is -3.8 dBFS and mean level is -20.8 dBFS. All measured sentence cues remain inside their shots; captions fit at most two lines. Thirteen frames from the encoded final were extracted, with full-size score and A/B result frames inspected. This is file, timing, and visual verification, not a full real-time listening review or a headset test.

## Revision 3: quiet sky look-around

The user requested a natural look at the sky without commentary about Orion or constellations. The replacement lifts the camera from the preceding lake view, then looks across the sky with a fixed 65° vertical field of view. Constellation lines and labels are disabled during capture. From 00:50.600 to 01:06.600 the final edit contains only the environment and its ambience: no narration, subtitles, chapter labels, or capture-context overlay.

The sky-only capture uses `sky-look` (or `sky-look-preview`) in `record-request.txt`, with output under `sky-look/`. Compilation and the capture test passed: one test, zero failures, 480 frames, 16 seconds. The capture-only assertions check disabled guides and the fixed field of view. The new footage replaces exactly frames 1,500–1,979 of the clean 256-second scene master. The existing timing adjustments then retain the 4:25.667 final duration. The narration master is exactly silent during the sky sequence and byte-identical to the preceding version outside it; no caption event overlaps that interval.

The revised output is `artifacts/narrated-demo-2026-10-18/nova-quiet-sky/VRExperienceAGB-narrated-demo-English.mp4`. The local `revise-quiet-sky.py` assembly script, SRT, narration master, and verification evidence are retained alongside the earlier versions.

The revised final passed complete decoding and stream checks: 7,970 frames, 2560 × 1440 at 30 fps, stereo 48 kHz AAC, 265.667 seconds, and eight chapters. The file is 587,797,288 bytes. Peak audio is -3.9 dBFS; the encoder reports no dropped frames. Six frames extracted from the final film were inspected, including the lake-to-sky sequence and subsequent return to tree inspection.
