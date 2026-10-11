# Review follow-up — 11 October 2026

Status: implementation, desktop verification and Android candidate build complete. No Quest acceptance or production scorer agreement is claimed. The current sample and its generator/resources are deliberately unchanged at the user's request.

## Scope and implementation

| Review tasks | Implementation |
| --- | --- |
| 1 — atomic replacement | Pure candidate preparation checks manual session construction and complete prepared-profile evaluation before retiring active state; library rejects invalid incoming scoring data before saving. |
| 2 — export evidence | Metadata can bind to a separate reconstruction only with the expected SHA-256 and an exact complete tree/node/condition/leaf mapping. Same-source preview retains its evidence. |
| 3 — unique labels | Runtime display labels disambiguate duplicate names using exact feature IDs. Source identifiers and sample files remain unchanged. |
| 4 — A/B inputs | Named A/B columns, differences filter, controller keyboard search, page counts, exact full-value detail pages, clamping and explicit Missing / Not supplied states. |
| 5 — reading space | Menu suppresses competing model renderers/canvases/colliders and restores their saved state on close. |
| 6 — result placement | Result anchors on entry/recenter or view transition, with profile/outcome and source-verification copy retained. |
| 7 — decision readability | Compact future-node previews, current-branch emphasis, pinned scrollable evidence and clearer profile markers. |
| 8, 15 — comfort/accessibility | Teleport-only default; optional stick walking/speed; seated layout and transition fade; separate ambience, selection sound and haptics; modal movement guards. |
| 9 — measurement preparation | Explicit 60-second capture with warm-up, frame distribution, optional CPU/GPU timings, memory, build/device identity and view transitions. Actual Quest measurement remains pending. |
| 10 — namespaces | Groups come from the actual first identifier segment. Unqualified IDs have a structural Unqualified group. No semantic aliases, required family list or dedicated Customer.Scores.* group. |
| 11–13 — learning and navigation | Four-step learning guide, distinct manual entry/resume, pace presets, remaining-animation estimates, retained complete calculation and controller tree-number entry. |
| 14 — table | Optional sparse labels, explicit groups of 50/all trees, bounded row elevation, zoom/rotation and magnified inspection. Display groups do not affect evaluation. |
| 16 — what-if | Existing copy/edit B and divergence inspection exposed in the unified menu; changed inputs and largest contribution deltas remain inspectable. |
| 17 — library | Capability/recovery copy and scoring-disabled paths; atomic import checks. Direct Wi-Fi still needs device verification. |
| 18 — replace sample | Deferred explicitly by the user; no source or generated sample data changed. |
| 19–24 — model explanation | Paginated passport, dictionary, gain concentration/namespace overview, glossary and provenance/verification pages; actual source nodes reachable through existing predictor search. Production reconciliation still needs approved external reference data. |
| 25–26 — tests and acceptance | Regression coverage for failed replacement, bound metadata, grouping, UI restoration, A/B, keyboard, comfort, pacing and pinned details; headset checklist prepared. Final results below. |
| 27 — maintainability | Pure activation and metadata-binding helpers; typed internal command dispatch with string compatibility boundary; menu, input review and learning UI separated from the playback coordinator. |

## Verification evidence

- First defect-focused run: 5/5 EditMode and 4/4 PlayMode passed.
- Full EditMode after current changes: 193/193 passed.
- Final full PlayMode after visual layout refinement: 101 passed, zero failed, one opt-in video test skipped (102 total; 76.49 seconds). Metadata binding, exact input details, posture changes during playback and the other focused review checks are included.
- Python synthetic fixture verification passed. This verifies the teaching contract, not source-platform predictions.
- Original and generated local sample checksums matched after implementation.
- Fresh Editor visual walkthrough completed: input review/exact values, learning, comfort, structure/dictionary, manual tree, A/B playback, complete result, table and library. Fixed long-reading text overlapping pagination controls; enlarged the playback explanation board. Preview camera framing was adjusted only at runtime for captures.
- A 60-second Editor performance report was saved successfully with Editor-only status and no active XR. This validates capture plumbing, not device speed.
- Bound local-source metadata was confirmed independently. The unchanged sample contains one negative stored gain; affected aggregate gain remains unavailable rather than being silently discarded.
- Android Development build succeeded in 71.9 seconds: zero errors, six warnings. No installation or headset launch was performed.
- Current headset measurements: not performed.
- `git diff --check` passed. Unity-generated source `.meta` files are retained; the temporary TMP fallback cache was restored. Original SmokeTest scene is open and clean.

Machine-readable evidence is kept under ignored `artifacts/implementation-2026-10-11/`. The prior review captures remain historical evidence of the earlier revision.

## Acceptance boundaries

Follow [Quest acceptance checklist](quest-acceptance-2026-10-11.md). Code compilation, automated tests and desktop images do not establish binocular readability, controller ergonomics, fatigue, real device frame budgets or direct Wi-Fi connectivity. The sample's incorrect business naming is not used as a platform contract.

## Android candidate

- APK: `artifacts/builds/VRExperienceAGB-review-follow-up.apk` (109,371,622 bytes).
- Build GUID: `63e396f1d7a7484892c1440b7320dde2`.
- SHA-256: `14d4248f1753640acfed69174a58015d2894b19942ab53f87fe2622abd64f0bd`.
- Unity 6000.6.3f1; Android IL2CPP Development; OneTreeLearning scene.
- Source: uncommitted current worktree over `89a6aa26c17b2722b26fefae2ea1ad20029f5e16`. No package/lock/project-setting changes.
- Six build warnings: no optional RuntimePipelineConfig (remote Pipeline commands disabled in Player); an existing DEVELOPMENT_BUILD directive in TreePerformanceProbe; a deprecated TMP shader pragma; three TMP large-method IL2CPP code-generation notices. The in-app performance capture does not require runtime Pipeline.
- This candidate is for functional/device diagnosis. Final sustained performance acceptance requires a release build without script debugging, as specified in testing.md.

Build report/identity, test results, screenshots and the Editor diagnostic capture are retained in `artifacts/implementation-2026-10-11/`. No production-scorer agreement, direct Wi-Fi success or Quest performance acceptance is implied.
