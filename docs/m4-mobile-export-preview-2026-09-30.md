# M4 nested AGB export preview — September 30, 2026

Branch: `codex/m4-ensemble`. Unity Editor: 6000.6.3f1, Apple Silicon.

The user selected structure and manual traversal testing before supplying a prepared profile and source prediction. The incoming format is the nested AGB JSON. The application imports that format directly; the normalized teaching fixture remains an internal representation.

## Input and structural evidence

Local input: `data/examples/export_Mobile_Click_Through_Rate_AGB_sample.json`. The user excluded this file from Git; it is not bundled or committed by this change. SHA256: `6cd87c1839a0ab96fd2a973f5fa3256a0a6b391ebd75bc3c76edcb408485b363` (3,074,083 bytes).

| Check | Observed result |
| --- | --- |
| Complete ensemble | 100 trees, 13,576 nodes, 6,738 splits, 6,838 leaves |
| Predictor inventory | 200 predictors |
| Condition inventory | 6,240 numeric `<`, 475 membership, 23 missing predicates |
| Maximum leaf depth | 7 |
| Independent structural gates | All pass; no unparsed conditions |
| Import metadata | All 13,576 source addresses, conditions, scores, gains and counts match the original file |

Parent/child count gaps are retained, not interpreted as corruption or traffic probabilities. Internal estimates, gain and sample counts remain inspection metadata; only reached leaf scores contribute to manual totals.

## Manual traversal evidence

An independent Python walk of the original nested objects produced three route sets. The Unity application sessions reached the same 100 leaves for each set and reproduced the totals within `1e-12`. These choices are arbitrary manual routes, not customer profiles or reference predictions.

| Manual choices in every tree | Unity route total | Independent total |
| --- | --- | --- |
| TRUE at every split | -2.8281422741772433 | -2.8281422741772424 |
| FALSE at every split | -3.813080254322283 | -3.8130802543222826 |
| Alternate TRUE/FALSE by depth | -2.4965708686926273 | -2.4965708686926278 |

Every route set includes all 100 exported trees. Seven visible platforms and four ledger rows per page limit presentation only. No scoring trees or categories are removed.

## Runtime and UI

`modelFile` automatically accepts either a normalized teaching fixture or a nested AGB export. `LoadAgbStructure` can replace an active session with a complete export preview; failed imports preserve the accepted session. The importer rejects unknown node fields, malformed children, duplicate JSON fields, unsupported operators and ambiguous category syntax with diagnostics.

Preview models cannot produce profile probabilities, claim verified manual consistency or be serialized as a verified normalized feature contract. Observed category sets do not become production feature domains; missing-only predictors keep an unknown type. The original synthetic prepared-profile experience remains available.

Compact labels wrap long names and explicitly summarize large sets. Hover reveals the exact predictor identifier and original split in a scrollable read-only panel. Mouse hover, scrolling, exit, invalid-import preservation, manual undo, camera independence and existing M4 flows are covered by scene tests. Meta canvas pointer surfaces are wired, but this iteration has not been checked on Quest.

## Automated and Editor validation

147 EditMode and 18 PlayMode tests passed. The synthetic fixture checks still pass. Scene actions traversed all 100 trees with 523 TRUE choices, matched every independently selected leaf, completed all contributions, and preserved the camera position/rotation. Undoing the final decision reduced the completed count to 99; replay restored the exact total within `1e-12`. Page 25 of the ledger lists tree indices 96–99, including the final tree.

The actual predictor label was reachable through Unity UI raycasting. Its pointer-enter event displayed the complete source identifier and condition. A synthetic 108-category condition verified complete tooltip text, vertical scrolling, pointer exit, and unchanged route revision/total. The screenshots were inspected after capture.

## Local evidence

Ignored folder: `artifacts/m4-mobile-export-2026-09-30/`.

- `structure.json`: independent structural inventory and gates.
- `manual-validation.json`: complete node comparison and 300 selected leaf comparisons.
- `scene-validation.json`: runtime action traversal, undo/replay, camera and final-ledger checks.
- `editmode.json`, `playmode.json`: Unity test results.
- `01-overview.png`, `02-focus.png`, `03-ledger.png`: actual Unity Game-view screenshots of the supplied export.

No prepared source profile, trusted production prediction, new Android build, simulator run, headset interaction or performance acceptance is claimed by this record. The earlier M4 APK predates this importer/hover iteration.
