# Model and profile contract

Status: the normalized fictional AGB-like example is implemented. Nested production-shaped AGB structure import and manual preview are implemented; feature-policy and source-scorer reconciliation remain pending under M2-06–M2-08. The scoring and structural requirements below are solution-design decisions, not a claim of production verification.

## Separate the model from its presentation

The same model must support manual exploration, deterministic profile evaluation, and several visual layouts. Node positions, decorative colors, camera paths, and hidden geometry are not part of the prediction.

Stable tree and node IDs connect model data to views, history, and recorded tours. A collapsed subtree still exists in the evaluator. Reordering for display must preserve the canonical boosting order.

## Normalized contract v1

The fixture under `data/examples/` uses:

- `schemaVersion`: currently `1`.
- `modelId`: stable model identity.
- `provenance`: explicitly marks synthetic data.
- `objective`: `binary_logistic` for this example.
- `outcomeLabel`: names the event whose probability is shown.
- `baseScore`: baseline in raw score / log-odds space, not a probability.
- `features`: feature definitions with numeric or categorical types, allowed values/ranges, and missing-value policy.
- `trees`: an ordered list of trees, each with `id`, `rootId`, `weight`, and `nodes`.
- Split nodes: `kind`, `id`, `feature`, `operator`, parameters, `trueChild`, and `falseChild`.
- Leaf nodes: `kind`, `id`, and `score`.

Supported normalized operators are `lt`, `in`, and `is_missing`. Numeric comparison is strictly less-than. Equality follows the false branch. Categorical values use exact, case-sensitive membership. A missing predicate matches an absent value or explicit JSON `null` only for a feature that permits missing values. Missing numeric values do not silently become zero or false.

An evaluator returns the visited node IDs, selected leaf, effective contribution per tree, raw score, probability, and warnings/errors. Unsupported operators, unknown categories, non-finite values, invalid roots/children, cycles, and incompatible feature types produce explicit errors. A malformed tree is not partially evaluated.

The normalized evaluator supports an explicit baseline and weights for general testing. The bundled AGB-like example uses baseline zero and unit weights:

```text
rawScore = baseScore + sum(tree.weight * reachedLeaf.score)
probability = sigmoid(rawScore)
sigmoid(z) = 1 / (1 + exp(-z))
```

Use a numerically stable sigmoid implementation and define numeric tolerances. The example is invented and is not a fitted business model. Production support requires the verification gates below.

## Direct nested-export preview

`OneTreeExperience.modelFile` accepts the nested `AdaptiveBoostScoringModel` / `GRADIENT_BOOST` JSON as well as the normalized teaching fixture. A normalized file is an internal runtime representation, not a format the export producer must supply. `AgbStructurePreview.Read` imports every tree and node without a display cap, generates stable tree/path addresses, and retains each original split, node estimate, gain and sample count in companion metadata. `LoadAgbStructure` can replace the active preview from JSON; rejected imports preserve the accepted session.

The current adapter accepts strict numeric `<`, plain comma-separated categorical membership, and `is Missing`. Unsupported or ambiguous conditions reject the complete import with a source address. Category lists observed in splits are not complete feature domains; predictors used only in missing predicates retain an unknown type. The model carries `StructureOnlyPreview`, so profile evaluation and serialization as a verified normalized model are refused, and manual consistency remains unverified. Manual totals use reached leaves, neutral baseline and unit weights as the design target, without asserting source prediction agreement.

Compact tree labels summarize large category sets. Hover over a predictor label or the current condition to read the complete identifier and original condition in a scrollable panel. Hover and scrolling do not change decisions, scores or camera pose. Mouse and Meta canvas pointer event wiring are implemented; headset pointer behavior still requires a device check.

See [the export preview test record](m4-mobile-export-preview-2026-09-30.md) for the tested sample and evidence.

## Prepared profiles

A profile declares `modelId` and a map from feature identifiers to values. Validate it against the model before playback. Show a readable display name while preserving the exact identifier internally. The same profile is passed to every tree. The accumulated ensemble score is not inserted as a new feature unless the actual model explicitly requires it.

A profile edit creates a copy and a new evaluation result. Reset or recompute affected presentation state consistently; never retain stale leaf contributions from the original profile. Explicitly distinguish a missing value from a feature that the user has not supplied in a partially constructed manual profile.

## Manual exploration

Keep an ordered history of branch decisions and committed leaf events. Manual input chooses a path even when no complete profile exists. A raw route total can be displayed, but a profile prediction requires a consistent profile and verified scoring semantics.

Track accumulated constraints per feature across all chosen trees. For numeric splits, intersect intervals with correct open/closed endpoints. For categorical splits, intersect the allowed set or its complement within the declared feature domain. For missing predicates, track present/missing states separately.

When constraints become inconsistent, show the conflicting decisions. Offer “Revise earlier choice” and “Continue free exploration”. In free exploration, label the result “Route score”; do not assert that a real profile could produce it. If the constraint solver cannot decide a case, report “Consistency not verified”.

Undo reverts decisions and score events exactly once. Merely entering, looking at, or re-rendering a leaf must not add its contribution.

## AGB structure and scoring design

An AGB model is an ordered ensemble of binary decision trees. A scoring input describes a customer and a candidate action/treatment; the same customer may receive different propensities for different candidates. Customer attributes, upstream model scores, interaction-history summaries, and action/context fields are predictor families, not independent models. Their presence alone is not an audit finding.

```text
AdaptiveBoostScoringModel
  algorithm: GRADIENT_BOOST
  modelVersion, factoryUpdateTime
  trainingStats: positiveCount, negativeCount, totalCount
  successRate, auc
  model.booster.trees[]
    score, gain, sampleCount
    split, left, right  (internal nodes only)
```

Children are nested objects; leaves have no `split`. Preserve canonical zero-based tree order. When native node IDs are absent, assign deterministic tree/path addresses such as `tree[0]/left/right` and retain them alongside presentation IDs. These are generated addresses, not export-provided identities.

The adapter's target scoring contract is:

```text
rawScore = sum(reachedLeaf.score for every exported tree)
probability = sigmoid(rawScore)
normalized baseScore = 0
normalized tree.weight = 1
```

Leaf scores are treated as finished contributions with learning-rate scaling already applied. Do not multiply by eta again or add a separate intercept, root score, or logit(successRate). Learning the baseline into trees is compatible with a neutral initial score of zero. Every tree contributes even if a tour shows only a subset. A single unsplit root is a valid leaf tree.

| Field | Design meaning and limits |
| --- | --- |
| Leaf `score` | One raw contribution, not a probability or importance value |
| Internal `score` | Node estimate for inspection; test child-count-weighted averaging as a structural diagnostic, never use it instead of reached leaves |
| `gain` | Structural split usefulness; separate from score and SHAP-based influence |
| `sampleCount` | Evidence seen by that node; children need not sum to the parent. It is not a current traffic probability |
| `split` | Strict numeric `<`, exact categorical membership `in { ... }`, or explicit `is Missing` |
| Monitoring fields | Aggregate response statistics, distinct from structural node evidence; not per-profile outputs |

Retain raw split text and audit fields in an adapter metadata layer independent of Unity objects. The normalized runtime schema currently carries only evaluation data; the companion `demo-agb-export.json` carries nested structure and invented audit metadata. These are two representations of the same fictional model, checked for matching paths and leaves, not two independent examples.

A scoring export need not include hyperparameters, a predictor dictionary, an intercept, or default branch directions. Do not flag their absence as corruption. Infer predictor *usage* from conditions, but obtain complete feature domains, units, preparation rules and missing policies separately. An observed membership set is not a full category dictionary. Never guess how unseen or missing symbolic values are routed.

The normalized fixture uses true → left and false → right in its nested representation. Its absent/null and unknown-category policies are explicit fixture rules. A production adapter must demonstrate source agreement on those behaviors before claiming support.

## Structural diagnostics and interpretation

Before any audit, parse the complete file and record file identity, tree/node/leaf counts, condition inventory, predictor count, gain sum, depth distribution, and monitoring counts. Check binary-tree identity, finite values, valid children, zero leaf gains, count arithmetic, and unparsed conditions. A failed gate requires parser/source investigation; never silently drop conditions or reinterpret parse failures as drift.

Check root-count order, child-count conservation, and internal-score averaging explicitly. Incremental learning and pruning can make counts and gains depend on evidence age as well as routing. Root count gaps may support stream-order comparisons; they are not elapsed time. Do not derive deeper-node creation times from total responses minus local counts, or estimate responses per day from one snapshot timestamp.

Use these conservative audit rules:

- Root-score sum minus logit(successRate) is a weak diagnostic, not a contract violation or correction factor.
- Node count times global positive fraction is only a heuristic expected-positive count, not observed branch positives or traffic share.
- Gain shares normalized to 100 are not Prediction Studio SHAP importance. Compare their rankings only with the difference in meaning made explicit.
- Independent per-tree leaf minima/maxima give outer score bounds; cross-tree constraints can make those extremes unreachable.
- Reuse, missingness, low evidence, and contextual dependence may suggest hypotheses. Structure alone cannot prove leakage, memorization, causality, interactions, calibration, fairness, or generalization.
- Actions sharing membership patterns have the same route only when all other model inputs are also identical.
- AUC is in [0, 1], with 0.5 representing random ranking. Lifetime monitoring counts alone do not establish a valid uncertainty interval for an adaptive dependent stream.

Planning priors for configuration are eta 0.3, lambda 1, gamma 0, minimum child weight 1, maximum trees 50 (range 1–500), and maximum depth 9 (range 1–14). Treat these as assumed defaults until the target platform version and adaptive model rule are checked. Observed tree count/depth does not reveal configured caps. Pruning and incremental growth must be considered when comparing snapshots; disappearance alone does not establish a dated drift event.

## Production verification questions

The formula is the design target; agreement with the official scorer is a separate acceptance gate.

| Question | Required evidence | Backlog owner |
| --- | --- | --- |
| Do zero baseline, unit weights, and sigmoid reconstruct the served propensity? | Approved records and full predictions; document any downstream calibration/processing and agreed tolerance | M2-07/M2-08, Platform team |
| Which child handles strict equality, membership, absent/null and unseen categories? | Known paths covering every supported operator and edge case | M2-06/M2-07, Model developer |
| How are symbolic commas, spaces, slashes and escaping represented? | Unambiguous grammar examples; reject unsupported/ambiguous syntax with source addresses | M2-06, Import developer |
| What are the outcomes and feature preparation rules? | Configured labels, units, transformations and provenance of upstream model scores | M2-07, Model owner / Data engineering |
| What are the actual tree/depth limits and platform mechanics? | Versioned platform documentation and adaptive model rule settings | M2-07, Prediction Studio |
| Does the model generalize, calibrate and behave fairly? | Labelled time-based evaluation and appropriate subgroup checks | Model owner; outside structural certification |

## Example and private-data strategy

The default structure-preview scene now bundles the user-selected `export_Mobile_Click_Through_Rate_AGB_demo.json` (50 trees); see the [simplified navigation record](m4-simple-navigation-2026-09-30.md). It has no supplied profile/reference prediction and remains explicitly unverified for scoring.

The separate verified scoring fixture has three trees and four prepared fictional profiles. The first tree has seven nodes; other trees demonstrate treatment membership and an explicit missing predicate. Scores, gains, counts, treatment names and monitoring values are invented. The compact ensemble teaches scoring mechanics; its score distribution is not a claim about trained-model performance.

`demo-model.json` is the normalized runtime representation; `demo-agb-export.json` is its nested export-shaped counterpart. A declared feature dictionary and readable labels live in the normalized file, since the export shape does not provide them. The runtime also accepts nested AGB exports directly for structure and manual preview. The fixture checker only verifies this known example's mapping.

For production verification, collect an approved export and representative source-model profiles with full predictions and preferably visited leaves. Keep sensitive material under ignored `data/private/`. Track only fictional or separately approved anonymized fixtures. Passing example checks does not certify production scoring.
