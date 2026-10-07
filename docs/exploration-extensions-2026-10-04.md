# Exploration extensions — October 4, 2026

Implemented in the current working tree, following the agreed order: predictor search, boosting progression, then recorded guided review. Controller tuning and hardware performance acceptance remain deferred. Real AGB profile/scorer reconciliation is still pending.

## Entry and workflows

Open **Extensions** beside the case/forest menu, or from the tree menu. The three tabs share a world-space panel and the existing controller/mouse pointer stack.

### Predictor fireflies

- Choose an exact feature identifier, or use **Find feature** to filter the picker by a case-insensitive part of its name. The actual query always uses the selected exact, case-sensitive identifier.
- Switch **Entire model / path**. Path scope includes all evaluated trees for the active prepared profile, including trees not yet visited. Manual and structure-only sessions explicitly report that a prepared profile is required.
- Matching trees receive distant markers and matching visible split nodes receive LINKED badges. **Previous/Next match** and **Inspect match** expose every result, including nodes outside the bounded visible neighborhood.
- Full paged detail includes tree/iteration, original condition, gain and sample count when supplied. Missing evidence stays explicitly unavailable. Search never edits the route or commits a contribution.

### Boosting trail

- Choose the baseline, previous/next iteration, any iteration through **Go to iteration**, or the full ensemble.
- **Reveal trees** displays the prefix in canonical boosting order with an ordered ground trail. With a prepared profile, the rings show the corresponding signed weighted contributions.
- Intermediate raw score and sigmoid probability are calculated from the complete accepted evaluation, starting at its baseline. The full-model result remains visible and unchanged. A structure/manual preview never shows a fabricated probability.
- **Inspect tree** opens the selected iteration. Existing return navigation restores the forest location. **Clear overlays** restores all trees.
- Gain, geometry and profile contribution remain separately labelled. This view is ensemble progression, not a training-loss history.

### Recorded guided review

- Navigate to a forest, tree, inspected node, prepared profile or A/B comparison, then **Capture stop**. Add an English explanation using the controller-accessible character keyboard, up to 240 characters.
- Select, reorder or delete stops. **Save locally** writes the current review slot; **Load saved** validates it before replacing the draft.
- Replay visits stops in order, with a minimum 12-second dwell (profile playback finishes before transition), captions and Next/Pause/End controls. Profile stops use deterministic playback of the selected tree; comparison stops retain both saved profiles and replay the selected A/B route. Pause allows inspection, and End restores the preceding exploration session.
- Every saved review contains stable tree/node targets, model identity and a SHA-256 fingerprint of scoring/tree/feature semantics. Profile snapshots preserve hypothetical edits. A changed model, missing target or invalid profile produces an explanation instead of silently navigating to another target.
- Saved files stay local: ignored `data/private/guided-review.json` in the Editor; `Application.persistentDataPath/private/guided-review.json` on the device. There is one explicit save slot, no cloud upload or multi-user synchronization.

## Boundaries

`PredictorSearch`, `BoostingProgression` and `GuidedReview` belong to the Unity-independent Application assembly. Presentation supplies UI, scoped markers, visibility, local storage and transitions. Replay uses isolated sessions; it does not commit decisions to the visitor's previous manual route. All trees remain available to model evaluation, regardless of visual revelation or focus.

The default 50-tree nested export stays a structure preview. Use **Compare synthetic profiles** to exercise profile-path search and intermediate probabilities now. These results are teaching-fixture results, not verification of the real AGB scorer.

## Verification

- Unity 6000.6.3f1 compiled the final runtime and tests without compilation errors.
- **172 EditMode tests passed**, including exact feature identity, full-profile path scope, independently expected prefix results, review round-trip and changed/missing-target rejection.
- **67 PlayMode tests passed**, including all previous 59 scene tests and eight new extension tests. The new tests exercise actual pointer down/click handling, both picker keyboards, model replacement, reveal/evaluation separation, local save/load, stop reordering, edited B snapshots, pause and restoration of manual exploration.
- `python3 scripts/verify-examples.py` passed the independent synthetic fixture, missing-value, rejection, weight, stability and Unity data-copy checks.
- Actual Editor renders were inspected for search, trail, tour authoring and the character keyboard. Captures live in ignored `artifacts/extensions/`. An initial overlapping button row and keyboard background bleed were fixed before the final capture.
- Detailed CLI reports: `artifacts/extensions-editmode-status.json` and `artifacts/extensions-playmode-status.json`.
- Following the final search-label refinement, all **8 focused extension PlayMode tests passed again** (`artifacts/extensions-focused-status.json`).
- Android Development APK succeeded with **0 errors and 5 warnings** via the connected Editor's asynchronous build command. The warnings concern disabled Pipeline runtime tooling, a TMP shader pragma and three TMP IL2CPP compilation partitions.
- APK: `artifacts/builds/VRExperienceAGB-extensions.apk`, 101930887 bytes; SHA-256 `b4351e9699d4268a239cc0e58869b2ca0cb43efa941b14acb1bfc30505f6370b`. This includes A/B comparison and horizon wolves. It was not installed or run on Quest.
- Build evidence: `artifacts/extensions-build-summary.json` and the detailed `artifacts/extensions-build-status.json`. The earlier direct CLI evaluation timed out during a successful first build; the final asynchronous build completed without that error.

 Actual Quest readability, controller ergonomics, comfort, audio and performance remain untested for this extension build.
