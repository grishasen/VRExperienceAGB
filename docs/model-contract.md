# Model and profile contract

Status: proposed normalized contract with executable synthetic examples. Real AGB import and scoring are not implemented or verified.

## Separate the model from its presentation

The same model must support manual exploration, deterministic profile evaluation, and several visual layouts. Node positions, decorative colors, camera paths, and hidden geometry are not part of the prediction.

Stable tree and node IDs connect model data to views, history, and recorded tours. A collapsed subtree still exists in the evaluator. Reordering for display must preserve the canonical boosting order.

## Synthetic contract v1

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

For the synthetic logistic model:

```text
rawScore = baseScore + sum(tree.weight * reachedLeaf.score)
probability = sigmoid(rawScore)
sigmoid(z) = 1 / (1 + exp(-z))
```

Use a numerically stable sigmoid implementation and define numeric tolerances. This formula describes the synthetic contract; applying it to a real export requires the checks below.

## Prepared profiles

A profile declares `modelId` and a map from feature identifiers to values. Validate it against the model before playback. Show a readable display name while preserving the exact identifier internally. The same profile is passed to every tree. The accumulated ensemble score is not inserted as a new feature unless the actual model explicitly requires it.

A profile edit creates a copy and a new evaluation result. Reset or recompute affected presentation state consistently; never retain stale leaf contributions from the original profile. Explicitly distinguish a missing value from a feature that the user has not supplied in a partially constructed manual profile.

## Manual exploration

Keep an ordered history of branch decisions and committed leaf events. Manual input chooses a path even when no complete profile exists. A raw route total can be displayed, but a profile prediction requires a consistent profile and verified scoring semantics.

Track accumulated constraints per feature across all chosen trees. For numeric splits, intersect intervals with correct open/closed endpoints. For categorical splits, intersect the allowed set or its complement within the declared feature domain. For missing predicates, track present/missing states separately.

When constraints become inconsistent, show the conflicting decisions. Offer “Revise earlier choice” and “Continue free exploration”. In free exploration, label the result “Route score”; do not assert that a real profile could produce it. If the constraint solver cannot decide a case, report “Consistency not verified”.

Undo reverts decisions and score events exactly once. Merely entering, looking at, or re-rendering a leaf must not add its contribution.

## Real AGB adapter verification

The reviewed exports contain `model.booster.trees` with `score`, `gain`, `sampleCount`, and textual `split` fields. The reference code includes numeric, membership, and missing-value concepts. This is enough to design an adapter, not enough to certify inference behavior.

Before labeling an imported result as the source model's probability, verify:

| Question | Required evidence |
| --- | --- |
| Does the export include an initial score, or encode it in a tree? | Source documentation or trusted predictions that distinguish the alternatives |
| Are leaf scores already scaled by learning rate? | Verified contribution semantics; avoid applying a factor twice |
| Does the model use the assumed output link or calibration? | Objective and post-processing definition |
| What is the exact branch convention? | Known paths for strict boundaries, membership, and missing values |
| How are categorical values escaped and represented? | Representative exports with expected parsing |
| Are derived features precomputed or transformed? | Feature preparation contract matching the source scorer |
| Do all trees contribute, and in what order? | Full-ensemble reference results |

Do not derive the baseline from observed success rate unless the source contract confirms that relationship. Do not use root `score`, `gain`, sample shares, or a sum of arbitrary branch choices as an individual prediction. Do not assume every textual split can be parsed; retain the original text and reject unsupported grammar.

The reference `tron-drive` begins its display sum at zero and visualizes up to 16 trees. Those are reference-demo behaviors to review, not production scoring requirements. The new implementation must separate presentation limits from evaluation and verify the correct initial score.

## Reference data strategy

Start with the three-tree synthetic fixture and four profiles in this repository. It includes a seven-node tree for the first scene, categorical membership, an explicit missing predicate, and exact-boundary inputs.

For real verification, collect a small approved set of source-model profiles with expected full predictions and, ideally, visited leaves. Store sensitive material under ignored `data/private/`. Track only synthetic or explicitly approved anonymized fixtures. A successful synthetic check cannot certify the real adapter.
