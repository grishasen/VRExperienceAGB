# Fictional AGB-like example

All names, inputs, scores, gains, counts and monitoring values are invented. This is a compact teaching model, not a customer export or a fitted model. It demonstrates AGB-shaped data and leaf-sum scoring without including private data.

| File | Purpose |
| --- | --- |
| `demo-agb-export.json` | Nested `AdaptiveBoostScoringModel`: monitoring fields, three ordered trees, textual splits, node scores/gains/counts |
| `demo-model.json` | Matching normalized runtime model with stable IDs, readable predictor labels, feature domains, zero baseline and unit weights |
| `demo-profiles.json` | Four customer-and-treatment inputs, including explicit missing data and strict threshold boundaries |
| `expected-predictions.json` | Independently specified paths, leaves, contributions and expected full outputs |

The first tree asks about previous clicks, recent digital visits and relationship duration. Its negative leaf scores encode the main response-rate adjustment. The second tree adjusts for a fictional treatment; the third adjusts for missing loyalty information. Numeric equality takes the false branch. Membership is exact and case-sensitive. The normalized fixture explicitly rejects unknown categories; production symbolic routing still needs verification.

| Profile | Leaf contributions | Raw score | Probability |
| --- | --- | ---: | ---: |
| New visitor | -3.8, -0.1, -0.2 | -4.1 | 0.0163024993714409 |
| Returning visitor | -3.2, +0.5, +0.1 | -2.6 | 0.0691384203433468 |
| Engaged visitor | -2.6, +0.5, +0.1 | -2.0 | 0.11920292202211755 |
| Exact threshold values | -2.6, -0.1, -0.2 | -2.9 | 0.05215356307841774 |

No separate intercept or extra learning-rate factor is added. Probabilities use the sigmoid of the complete leaf sum. Invented monitoring values illustrate metadata only; they are not calculated from these four profiles. Internal scores use child-count-weighted averages, all leaf gains are zero, root counts decrease, and child counts are deliberately nonconserving.

Run `python3 scripts/verify-examples.py` from the repository root. It checks predictions, rejection behavior, the nested-to-normalized mapping and Unity data-copy equality. Run `python3 scripts/inspect-agb-structure.py data/examples/demo-agb-export.json` for structural diagnostics. These checks do not constitute production importer or Pega scorer verification.

The [model contract](../../docs/model-contract.md) owns structure/scoring design; [M2](../../docs/backlog/m2-model-trust.md) owns unresolved implementation and verification work. The existing seven-node Unity scene retains its node identities and referenced asset metadata.
