# Ordinary AGB export and CDH profile playback — October 11, 2026

**Subsequent presentation change:** the [50-tree score build](sample50-score-2026-10-11.md) replaces calculated sigmoid percentages in the UI with raw score and a calibration notice. The behavior and APK below are the preceding import revision.

Status: implementation complete; automated regression and Android candidate build passed. A subsequent Web check found a sigmoid/CDH mismatch; treatment-specific calibration now reproduces all five executions for one supplied treatment. Other contexts remain unresolved. Headset acceptance is deferred. The local sample, sample generator and generated sample resources are unchanged.

## Implemented behavior

- Import an ordinary nested AGB export directly through the local JSON library. Retain all trees, original order, exact leaf values, source metadata and SHA-256. Preserve its model name on save/reopen.
- Calculate the sum of the reached leaf scores, then apply sigmoid. No extra learning-rate multiplier, gain/count contribution or PAVA adjustment. Label the output **Tree-score probability** and explicitly defer CDH reconciliation.
- Read CDH JSON objects, arrays and JSONL, including embedded JSON strings. Resolve record-local common references, merge execution-specific parameters, retain action/treatment context, and create one selectable profile per matching ADM execution.
- Match by model/rule/component name, including the component's terminal plural `s`. Record modelVersion and technique as evidence, without filtering by version or treating logged output as the model calculation.
- Select A and B independently, inspect their inputs and CDH source, follow their deterministic routes, pause/step, calculate every tree, and view complete forest/table results. Edited profiles detach original recorded outputs.
- Accept constant-leaf models with no predictors and equal A/B scores. Prevent an empty feature editor from opening. Long selector labels fit or truncate; complete execution details remain available on the source page.
- Reject unsupported conditions, malformed references, non-finite inputs and ambiguous empty used values. Rejected imports preserve the accepted model, playback, pair and stored files. Exact membership handles unseen category strings without inventing a complete domain.

## Scope and boundaries

The evaluator remains independent of Unity scene objects. Namespace families still derive from actual identifiers; no fixed family set or separate Customer.Scores family was added. The explicit structure-only API is preserved. Existing normalized synthetic files continue to work.

Keep the original export filename. If it is renamed so its model name no longer matches the CDH rule/component, CDH import reports no matching execution. Only predictors used by the imported trees are materialized. Common attributes that do not affect those trees remain unused.

Explicit null and absent values remain distinguishable in the trace, but both match an explicit `is Missing` condition. A missing operand at a numeric or membership comparison is an error. A used empty string is an error because its source missing semantics have not been established.

## Verification evidence

Artifacts are local and ignored under `artifacts/agb-cdh-import-2026-10-11/`. Real files used for checks are under ignored `data/private/agb-cdh-check/`; tracked tests use fictional inputs only.

- Unity 6000.6.3f1: successful Editor C# compilation.
- EditMode: 207/207 tests passed, including 14 new cases for tree sums, thresholds, constant leaves, CDH references/types, record isolation, parameter precedence, malformed inputs, membership semantics and filename-preserving reload.
- PlayMode: 104/104 ordinary tests passed; one opt-in video test skipped. New scene tests cover full A/B calculation and reload, constant-leaf input/editor behavior, and rejection preserving an accepted pair.
- Supplied private Mobile files: one tree, zero predictors, 27 matching GradientBoost execution profiles; all evaluated successfully and A/B session creation succeeded. Raw score -0.18350515463917527; tree-score probability 0.45425201635544527 for every execution. This is not a CDH propensity comparison.
- Android Development APK built successfully in 43.222 seconds: zero errors, six warnings. No install or device launch was performed.
- APK: `artifacts/builds/VRExperienceAGB-agb-cdh-import.apk` (102,870,172 bytes). SHA-256: `a02517828aa7e5510c234ac396e0899bf7ab391c457a9ab70372b36e38b0ea43`.
- Build warnings concern optional runtime Pipeline configuration, the existing development-build directive, and TMP shader/IL2CPP code generation. Generated TMP fallback cache changes were restored after the build.
- No headset session or source propensity reconciliation has been performed for this change.

## Subsequent Web validation and propensity check

The newly supplied CDH file contains 31 records. Name/component matching imports 210 Web executions: 44 report GradientBoost across five records; 166 report NaiveBayes for the older candidate component. No modelVersion filtering was applied. The propensity comparison below uses only the 44 GradientBoost executions.

All 210 imported profiles passed the C# evaluator and matched an independent Python traversal of the original JSON for reached leaves, raw score and sigmoid within 1e-14. Local save/reopen restored all 210. A pair of GradientBoost profiles on opposite branches completed deterministic playback through both trees in the actual Unity scene; Step, Calculate whole model, Table View and the profile-source page succeeded without a headset.

The exported model has two trees: one strict numeric split and one constant leaf. Among the GradientBoost executions, 35 take the left branch and nine take the right branch. The resulting tree-score probabilities are 0.416789202246394 and 0.43065324741611843 respectively.

**CDH agreement failed: 0/44 match within 1e-12.** Recorded execution-level pyPropensity ranges from 0.016666666666666666 to 0.13636363636363635. Calculated values exceed recorded values by 29.428961 to 41.398658 percentage points. The recorded modelVersion equals the export's modelVersion for all 44 executions. The export timestamp is October 7; these execution timestamps are October 11. Equal version strings do not resolve the observed disagreement.

For the same value of the only predictor used by these exported trees, different executions have different recorded propensities. These two trees alone therefore cannot reproduce all logged results. The specific cause has not been established; no calibration, model-context mapping or output correction was invented. Recorded outputs remain separate from calculated tree-score probabilities.

This check changes the status from comparison deferred to **comparison performed, mismatch unresolved** for the supplied Web data. It does not establish correctness of the CDH serving formula or source-model completeness. Local evidence: `artifacts/web-cdh-check/independent.json`, `csharp-result.json`, and `scene-result.json`. Real input files and the temporary saved-file library remain under ignored `data/private/agb-cdh-check/`. No source code, sample or APK changed for this validation.

## Treatment-specific calibration confirmed for one supplied context

The user identified the second score-distribution screenshot as NewHomeownerWebHub. Its totals (92 responses, six positives) and AUC (74.03%) agree with that treatment's logged model evidence. Using the displayed score intervals and counts, the smoothed mapping `(binPositives + 0.5) / (binPositives + binNegatives + 1)` reproduces all five GradientBoost executions for this treatment exactly in double precision, including both exported-tree routes.

The lower raw score selects the first bin (zero positives, 18 negatives), yielding 0.02631578947368421. The higher raw score selects the third bin (one positive, nine negatives), yielding 0.13636363636363635. These equal the recorded execution outputs. The screenshot's displayed bin propensity uses the unsmoothed response rate, so it shows 0% and 10% for those bins instead.

This resolves the earlier sigmoid mismatch for this treatment: raw tree evaluation was reproduced, but final CDH propensity requires the context-specific calibrated mapping and smoothing. The other treatments still require their own matching distributions. Exact boundary behavior beyond the observed points is not certified by rounded screenshot labels. The application has not yet gained a calibration-table importer; its sigmoid output remains explicitly unverified against CDH.

The smoothing formula is documented in [Pega's ADM explanation](https://pegasystems.github.io/pega-datascientist-tools/articles/ADMExplained.html#from-log-odds-score-to-final-propensity). AGB's use of PAVA and the absence of its bins from standalone exports are described in [AGB Explained](https://pegasystems.github.io/pega-datascientist-tools/5.1.0/articles/AGBExplained.html#model-auc-and-calibration). Private evidence: `artifacts/web-cdh-check/newhomeowner-calibration-check.json`.

## Quest checks

1. Install the new candidate and open **Models / profiles**. Import the ordinary AGB export, then CDH JSON/JSONL through local upload or an available file picker.
2. Check the forest tree count, source name/version and **Tree-score probability / CDH reconciliation deferred** labels. Check the import status reports the expected number of execution profiles.
3. In **Compare profiles A / B**, select two executions independently. Inspect **Inputs → Profile source** and confirm action/treatment, rule/component and recorded propensity belong to the selected execution.
4. Play A/B, pause and resume, use Step, inspect each condition and reached leaf, then **Calculate whole model**. Confirm all trees contribute and the same complete result appears in forest and Table View.
5. With the supplied one-leaf Mobile export, expect no predictors and identical A/B results. Inputs must explain constant leaves; Edit B must be unavailable without an exception.
6. Import a malformed or incompatible profile. Confirm a useful error, preserved current result and no invalid saved profile. Return to the accepted pair.
7. Restart the app, reopen the saved model, and confirm the compatible CDH profiles and source details return. Repeat A/B calculation.
8. Check controller pointing, scrolling of long names/source details, stereo readability, seated/standing placement and pause/recenter. Complete sustained device performance measurements using the [combined acceptance checklist](quest-acceptance-2026-10-11.md).

CDH propensity agreement is not a passed acceptance criterion. The supplied Web executions have now been compared and do not match the exported-tree result; the discrepancy requires investigation before claiming source-scorer equivalence.
