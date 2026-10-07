# Two-profile comparison — October 3, 2026

The user selected profile comparison as the next extension while leaving controller tuning and performance acceptance until the end. Real profiles, a model export and source results will be supplied separately; no production scoring verification is claimed.

## Delivered behavior

- **Compare synthetic profiles** is available beside the case menu, in the forest and in the tree menu. For the default structure-only export it explicitly opens the bundled synthetic model, retaining the original model, selected tree and accepted manual progress for **Exit comparison**.
- A starts from the current prepared profile, or the first bundled profile. B starts as an independent copy with its own identity. **Next profile A/B** selects other bundled profiles; **Copy A to B** replaces B with a fresh copy.
- Full-model results show both raw scores, both synthetic probabilities, B minus A, and the probability difference in percentage points. Every tree is evaluated, including trees outside the rendered neighborhood or tour.
- Browse every tree with **Previous tree / Next tree**, or use **Next difference**. The panel shows selected leaves, weighted contributions, contribution delta, the first diverging condition, the observed values and branch decisions, full node paths, and changed features. **More detail** pages long content without dropping values.
- **Edit B** stages numeric, categorical and explicit missing values. **Apply B** validates and recomputes the entire B ensemble, resetting its route; A stays intact. **Cancel edit** preserves the accepted B result. Invalid drafts remain editable without replacing either accepted result.
- **Follow A/B** opens the same selected tree using that profile's saved accepted route. Two labeled violet/blue drops identify the independent profile positions in the visible neighborhood. Node badges identify shared and distinct paths. Only the selected profile advances with Step or Play; this is not synchronized dual autoplay.
- **Inspect split** focuses the first divergence without committing branches or contributions. **Your current decision** returns to the selected profile's route. Returning to the forest keeps the comparison.
- **Exit comparison** restores the preceding experience in the forest, including its selected tree and accepted decisions. Deliberate profile/view transitions cancel unfinished movement tokens. Modal comparison inspection suspends animation without changing tracked head/controller poses.

The default real export remains available for manual exploration. Comparison never infers predictions from its structure, gain or counts. Hypothetical profile differences describe model sensitivity, not causal effects.

## Architecture

`ProfileComparisonSession` is plain C# in Application. It owns two independent `EnsembleSession` instances and comparison rows built from the complete evaluator outputs. It accepts only evaluable prepared profiles and keeps the existing structure-preview rejection. A failed replacement or edit preserves the accepted pair; non-finite score differences are rejected.

`OneTreeExperience` preserves the prior experience when entering comparison and binds the selected profile to the existing tree view. `ProfileComparisonPresentation` supplies world-space controls, paged evidence, draft editing and labeled drops through the existing Meta canvas and stale-pointer guards. Comparison badges supplement the existing route presentation. Existing signed contribution rings retain their meaning.

`ProfileComparisonSetup.Configure` assigns the two existing synthetic fixture assets through Unity's scene API. The saved scene keeps its original default export; only the two comparison references are added. New Unity-generated metadata is retained.

## Verification

- Unity 6000.6.3f1 batch compilation and scene configuration succeeded.
- 166 EditMode tests passed, including eight comparison tests: known independent deltas, strict threshold divergence, rejected drafts, cancel preservation, missing versus unsupplied, independent progress, retired movement tokens, and an untruncated 128-tree model including different paths with equal contributions.
- 54 PlayMode tests passed, including five new scene tests exercising pointer-driven entry, editing/cancel/apply, A/B switching, two-drop visibility, first-divergence inspection, retained export progress, stale-pointer rejection and paged details. The existing 49 scene regressions also passed.
- The captured comparison panel was visually inspected; modal ordering and background explanation visibility were corrected and the full PlayMode suite passed again. The screenshot is `artifacts/profile-comparison/comparison-panel.png`.
- No Quest acceptance or performance measurement is claimed. Those checks remain deferred at the user's request.

Detailed test reports and images are stored under ignored `artifacts/profile-comparison/`.

## Android candidate

The Android Development build succeeded in approximately 60.9 seconds with zero errors and five reported warnings. The APK is `artifacts/builds/VRExperienceAGB-profile-comparison.apk`, 127,292,377 bytes; SHA-256 `a19931207c570ce3494ccec1d7ff076269a1863244fa317a9340880d8aa4cd63`. It was not installed or run on Quest.

The source is the local working tree based on `f7a27a8746c5d92395c996e5377899c20610e5a7`, with this comparison implementation uncommitted. `artifacts/profile-comparison/build-manifest.json` records the APK identity and C#/scene source hashes. The detailed build log is alongside it. Package versions and project settings remain unchanged; temporary build resources were cleaned up by Unity and incidental TMP fallback-font serialization was excluded from the source changes.

## Real-data follow-up

Keep the model export, profiles and authoritative expected outputs under ignored `data/private/`. Each reference case should identify the exact model/version and profile, contain the actual prepared predictor values, and include the expected final output; visited leaves or decision paths are useful when available. M2-06–M2-08 must reconcile feature preparation, routing and scoring before this UI can label real-model predictions verified.

Predictor search, the full boosting trail and authored guided tours remain separate future extensions.
