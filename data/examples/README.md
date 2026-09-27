# Synthetic examples

All values in this directory are invented for development and demonstration. They are not a Pega export, customer records, or a trained business model.

| File | Purpose |
| --- | --- |
| `demo-model.json` | Three trees using numeric, categorical, and missing-value splits; tree one has seven nodes |
| `demo-profiles.json` | Four prepared profiles, including exact numeric boundaries |
| `expected-predictions.json` | Independently specified paths, leaves, contributions, and outputs |

Run from the repository root:

```sh
python3 scripts/verify-examples.py
```

The baseline is `-1.5` in raw score space. The fixture's weights are all `1`. The new visitor receives `-0.4 - 0.1 - 0.2`, producing a raw score of `-2.2`. The returning visitor receives `+0.2 +0.5 +0.1`, producing `-0.7`. The engaged visitor receives `+0.8 +0.5 +0.1`, producing `-0.1`. The boundary profile uses `previousResponses = 5` and `lifetimeValue = 100`, taking the false branch of both strict `<` tests; its final raw score is `-1.0`.

The expected outputs use the logistic sigmoid. They verify the internal synthetic contract only. Real AGB scoring needs the reference checks in [Model contract](../../docs/model-contract.md).
