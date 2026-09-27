#!/usr/bin/env python3
"""Check the synthetic fixture contract. This is not a production AGB importer."""

import copy
import json
import math
from pathlib import Path
import sys


def require(condition, message):
    if not condition:
        raise ValueError(message)


def number(value):
    return type(value) in (int, float) and math.isfinite(value)


def validate_model(model):
    require(model.get("schemaVersion") == 1, "Unsupported schema version")
    require(model.get("objective") == "binary_logistic", "Unsupported objective")
    require(number(model.get("baseScore")), "Invalid baseline")
    features = model["features"]
    require(isinstance(features, dict) and features, "Features must be defined")
    for name, spec in features.items():
        require(spec.get("type") in ("number", "category"), f"Invalid type: {name}")
        require(type(spec.get("allowMissing")) is bool, f"Missing policy required: {name}")
        if spec["type"] == "category":
            values = spec.get("values")
            require(isinstance(values, list) and values, f"Invalid category domain: {name}")
            require(all(isinstance(v, str) for v in values), f"Non-string category: {name}")
            require(len(values) == len(set(values)), f"Duplicate category: {name}")
    trees = model["trees"]
    require(isinstance(trees, list) and trees, "No trees")
    require(len({t["id"] for t in trees}) == len(trees), "Duplicate tree ID")
    for tree in trees:
        require(number(tree.get("weight")), "Invalid tree weight")
        nodes = {n["id"]: n for n in tree["nodes"]}
        require(len(nodes) == len(tree["nodes"]), "Duplicate node ID")
        require(tree["rootId"] in nodes, "Invalid root")
        visited, active = set(), set()

        def visit(node_id):
            require(node_id in nodes, "Missing child reference")
            require(node_id not in active, "Cycle")
            require(node_id not in visited, "Node has multiple parents")
            active.add(node_id)
            node = nodes[node_id]
            if node["kind"] == "leaf":
                require(number(node.get("score")), "Invalid leaf score")
            else:
                require(node["kind"] == "split", "Unknown node kind")
                feature = node["feature"]
                require(feature in features, "Unknown split feature")
                spec = features[feature]
                operator = node["operator"]
                require(operator in ("lt", "in", "is_missing"), "Unknown operator")
                if operator == "lt":
                    require(spec["type"] == "number" and number(node.get("threshold")), "Invalid numeric split")
                elif operator == "in":
                    values = node.get("values")
                    require(spec["type"] == "category" and isinstance(values, list) and values, "Invalid membership split")
                    require(all(v in spec["values"] for v in values), "Unknown split category")
                else:
                    require(spec["allowMissing"], "Missing predicate requires a missing-value policy")
                visit(node["trueChild"])
                visit(node["falseChild"])
            active.remove(node_id)
            visited.add(node_id)

        visit(tree["rootId"])
        require(visited == set(nodes), "Unreachable node")


def validate_values(model, values):
    require(isinstance(values, dict), "Profile values must be an object")
    require(set(values) <= set(model["features"]), "Unknown profile feature")
    for name, spec in model["features"].items():
        value = values.get(name)
        if value is None:
            require(spec["allowMissing"], f"Missing required feature: {name}")
        elif spec["type"] == "number":
            require(number(value), f"Invalid numeric value: {name}")
            require(not spec.get("integer") or float(value).is_integer(), f"Integer required: {name}")
            require("minimum" not in spec or value >= spec["minimum"], f"Below minimum: {name}")
        else:
            require(isinstance(value, str) and value in spec["values"], f"Unknown category: {name}")


def sigmoid(value):
    if value >= 0:
        return 1 / (1 + math.exp(-value))
    exp_value = math.exp(value)
    return exp_value / (1 + exp_value)


def evaluate(model, values):
    validate_values(model, values)
    paths, leaves, contributions = [], [], []
    for tree in model["trees"]:
        nodes = {n["id"]: n for n in tree["nodes"]}
        current, path = tree["rootId"], []
        while True:
            node = nodes[current]
            path.append(current)
            if node["kind"] == "leaf":
                leaves.append(current)
                contributions.append(tree["weight"] * node["score"])
                paths.append(path)
                break
            value = values.get(node["feature"])
            operator = node["operator"]
            if operator == "is_missing":
                choice = value is None
            elif operator == "lt":
                require(value is not None, "Numeric missing value has no route")
                choice = value < node["threshold"]
            elif operator == "in":
                require(value is not None, "Categorical missing value has no route")
                choice = value in node["values"]
            else:
                raise ValueError("Unknown operator")
            current = node["trueChild"] if choice else node["falseChild"]
    raw = model["baseScore"] + math.fsum(contributions)
    require(number(raw), "Non-finite accumulated score")
    return dict(paths=paths, leaves=leaves, contributions=contributions,
                rawScore=raw, probability=sigmoid(raw))


def expect_rejection(label, operation):
    try:
        operation()
    except ValueError:
        return
    raise ValueError(f"Invalid fixture accepted: {label}")


def main():
    directory = Path(__file__).resolve().parents[1] / "data" / "examples"
    def read(name):
        return json.loads((directory / name).read_text(encoding="utf-8"))
    model = read("demo-model.json")
    profile_file = read("demo-profiles.json")
    expected_file = read("expected-predictions.json")
    validate_model(model)
    for document in (profile_file, expected_file):
        require(document["schemaVersion"] == 1, "Fixture schema mismatch")
        require(document["modelId"] == model["modelId"], "Fixture model mismatch")
    profiles = {p["id"]: p["values"] for p in profile_file["profiles"]}
    require(len(profiles) == len(profile_file["profiles"]), "Duplicate profile ID")
    cases = expected_file["cases"]
    require(len(cases) == len(profiles), "Expected case count mismatch")
    require({c["profileId"] for c in cases} == set(profiles), "Expected profile coverage mismatch")
    for case in cases:
        actual = evaluate(model, profiles[case["profileId"]])
        for key in ("paths", "leaves", "contributions"):
            require(actual[key] == case[key], f"{case['profileId']}: {key} mismatch")
        for key in ("rawScore", "probability"):
            require(math.isclose(actual[key], case[key], rel_tol=1e-12, abs_tol=1e-12),
                    f"{case['profileId']}: {key} mismatch")
        print(f"PASS {case['profileId']}: score={actual['rawScore']:.3f}, probability={actual['probability']:.9f}")

    invalid_operator = copy.deepcopy(model)
    invalid_operator["trees"][0]["nodes"][0]["operator"] = "guess"
    expect_rejection("unknown operator", lambda: validate_model(invalid_operator))
    cycle = copy.deepcopy(model)
    cycle["trees"][0]["nodes"][0]["trueChild"] = "t1-root"
    expect_rejection("cycle", lambda: validate_model(cycle))
    nonfinite = copy.deepcopy(model)
    nonfinite["trees"][0]["nodes"][3]["score"] = float("nan")
    expect_rejection("non-finite score", lambda: validate_model(nonfinite))
    missing = dict(profiles["new-visitor"])
    del missing["visits"]
    expect_rejection("missing numeric input", lambda: evaluate(model, missing))
    unknown = dict(profiles["new-visitor"], placement="unknown")
    expect_rejection("unknown category", lambda: evaluate(model, unknown))
    boolean = dict(profiles["new-visitor"], previousResponses=True)
    expect_rejection("boolean as numeric input", lambda: evaluate(model, boolean))
    absent = dict(profiles["new-visitor"])
    del absent["loyaltyTier"]
    require(evaluate(model, absent) == evaluate(model, profiles["new-visitor"]), "Absent/null missing mismatch")
    weighted = copy.deepcopy(model)
    weighted["trees"][1]["weight"] = 0.5
    require(math.isclose(evaluate(weighted, profiles["returning-visitor"])["rawScore"], -0.95, abs_tol=1e-12),
            "Weighted contribution mismatch")
    require(sigmoid(-1000) == 0 and sigmoid(1000) == 1, "Unstable sigmoid")
    print("PASS rejection, missing-value, weight, and numeric-stability checks")
    print("Synthetic contract verified. Unity and real AGB scoring remain unverified.")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, KeyError, TypeError, OSError, json.JSONDecodeError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        sys.exit(1)
