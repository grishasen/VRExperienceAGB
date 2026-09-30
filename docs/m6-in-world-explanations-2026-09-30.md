# M6 in-world explanation implementation — September 30, 2026

Branch: `codex/m5-forest-experience`. The user approved all twelve proposed additions to M6. They extend the milestone's existing Quest acceptance work; hardware measurements, comfort/readability acceptance, the soak session and the new-user study are still open. No new APK is installed on Quest by this task.

## Delivered scope

| Story | In-world behavior | Evidence boundary |
| --- | --- | --- |
| M6-09 | Readable split questions; supplied units and profile values/results | Export labels are explicitly inferred from identifiers; units are never guessed |
| M6-10 | Both branch alternatives beside their actual edges and on choice stones; accepted/profile path marker | Pointing reveals complete category sets and the exact original source condition |
| M6-11 | Segment portrait beside the tree; repeated numeric predicates merge into lower-inclusive/upper-exclusive bounds | Full path remains available by pointing; accepted-route contradictions are explicit; structure-preview consistency stays unverified |
| M6-12 | Stored gain, share of tree gain, predictor reuse and source address | Gain is structural evidence, not SHAP, a probability or causality; unavailable/negative gain cannot yield shares |
| M6-13 | Exported sampleCount and a neutral review marker below 100 observations | This threshold is a UI review convention, not a statistical sufficiency verdict; no traffic shares, branch positives or node ages are inferred |
| M6-14 | Leaf contribution, accepted before/after subtotal, segment and full supported-profile result | An inspected leaf does not commit a score; manual routes have no profile probability |
| M6-15 | Crown passport with stable export order/index, depth, leaves, frequent predictors, gain share, leaf bounds and root question | Bounds describe weighted leaf contributions of that tree; no independent-leaf combination is claimed reachable across the ensemble |
| M6-16 | Separate family-composition strips on garden and tabletop bases | Shares sum stored split gain by identifier family; zero/unavailable gain is explicit; geometry and signed contribution rings retain their meanings |
| M6-17 | Hold a pointer on a node label for 0.45 seconds to link exact predictor occurrences, visible nodes and matching crowns | The link remains on forest return and resets on model replacement; no route/evaluation mutation |
| M6-18 | Local numeric threshold spectrum with white current-cut marker | Up to 64 visual bins; the full distinct, sorted cutpoint list remains in the pointer detail; no memory-horizon claim |
| M6-19 | Explicit MISSING/PRESENT edge labels and missing-node badges | Only actual IsMissing conditions qualify; absent history does not establish a new customer |
| M6-20 | Entrance story with optional version/update, counts, monitoring values and factual predictor-use summary | Missing/invalid monitoring is unavailable, not zero; no automatic quality rating, causal role or dated drift event |

The same accepted model backs all descriptions. The display can summarize long copy, but the exact conditions and full segment remain scrollable through the existing pointer tooltip. Text is English. No menu action is required to see the new explanations.

## Implementation boundaries

`ForestExplanation` is a cached plain-C# read-only description index in the Import assembly, separate from the evaluator and Unity objects. Optional `ExportMonitoring` retains supplied monitoring fields. Normalized feature dictionaries can now supply an optional `unit`; no scoring behavior changes. The default nested export remains structure/manual preview only.

`M6ExplanationPresentation` creates bounded runtime cards around the existing scene, uses the existing pointer/EventSystem infrastructure and follows the shared session/focus. It never changes the head/controller transforms or commands route movement. Forest passports and composition strips reuse the existing garden and tabletop. Transitions clear stale hovered node details before the reusable seven platforms are rebound.

Identifier families are explicitly inferred: `IH.*` → History; `Customer.*` → Customer; `py*` and `Param.*` → Context; everything else → Other. These are descriptive groupings, not an assertion about business semantics. A missing metadata layer displays “not supplied”. A zero denominator displays an unavailable share. Negative source gain disables dependent positive-share visualizations.

## Verification

Unity 6000.6.3f1: **158 EditMode tests passed and 45 PlayMode tests passed** on the final implementation, including the existing M5 navigation suite. Seven new pure-data tests cover gain reconciliation, exact feature identities, zero/negative/missing evidence, numeric segment bounds, full membership and missingness, optional monitoring, units and model isolation. Four new scene tests cover pointer linking with unchanged route state, focused segment/edge updates and help visibility, prepared-leaf reconciliation/undo, and stale-link removal on model replacement.

`python3 scripts/verify-examples.py` also passed. Final test reports are in ignored `artifacts/m6-2026-09-30/editmode-final.json` and `playmode-final.json`. Game-view captures were inspected for the tree, entrance and crown passport. The final side cards sit in front of the console and below the branch labels; the passport was reduced and brought closer to the crown to fit the preview. Temporary preview poses were not saved to the teaching scene.

Editor screenshots and tests supplement the deferred Quest walkthrough; they do not establish headset readability, comfort or performance. The local Editor reports its existing unavailable desktop OpenXR runtime diagnostics; no clean headset runtime log is claimed.

## Deferred device acceptance

Include the two side cards, six visible edge labels, long membership tooltip, distant predictor markers, composition strips, entrance story and dense focused subtree in the M6 worst-visible-scene workload. Verify trigger reachability from both controllers, seated/standing reading positions, pointer dwell behavior, full-condition scrolling and forest return. Preserve the original 72 Hz/72 FPS target, actual CPU/GPU and memory measurement, 30-minute soak and three-new-user checks. Do not tag a demo release until the existing acceptance gates pass.

## Android candidate

Development Android build `build_4f4baeeffa6e` succeeded from runtime source commit `6f33a0c18d76e3ead0c0c2396fa5056de835b734` in 70.968 seconds, with zero errors and five warnings. APK: `artifacts/builds/VRExperienceAGB-m6-in-world-explanations.apk`, **98,883,871 bytes**; SHA-256 `a23422fe1ba0fc4e2ef7fe7db08643e4bcdbe67951b2b8666167bc6c842356b1`. The APK was not installed or run on Quest. Subsequent commits record documentation only.

Warnings concern the intentionally absent Player Pipeline configuration, an existing TextMesh Pro shader debug-symbol pragma and three TextMesh Pro IL2CPP large-method diagnostics. Package versions and project settings were not migrated. Incidental font/material serialization by the Editor is excluded from the source change. Build details and the APK identity manifest are retained in ignored `artifacts/m6-2026-09-30/`.
