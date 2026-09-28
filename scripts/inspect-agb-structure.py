#!/usr/bin/env python3
"""Read-only structural evidence for the model contract; not an importer or scorer."""
import argparse
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import re


def inspect(path):
    raw = path.read_bytes()
    def reject_constant(value):
        raise ValueError(f"Non-finite JSON constant: {value}")
    data = json.loads(raw, parse_constant=reject_constant)
    assert data['type'] == 'AdaptiveBoostScoringModel'
    assert data['algorithm'] == 'GRADIENT_BOOST'
    trees = data['model']['booster']['trees']
    counts, kinds, depths = Counter(), Counter(), Counter()
    predictors, unparsed, gains, mean_errors, deficits = set(), [], [], [], []
    namespace_gains = {}
    for index, root in enumerate(trees):
        stack = [(root, 0, 'root')]
        while stack:
            node, depth, address = stack.pop()
            counts['nodes'] += 1
            for key in ('score', 'gain', 'sampleCount'):
                assert type(node[key]) in (int, float) and math.isfinite(node[key])
            assert node['sampleCount'] >= 0
            if 'split' not in node:
                assert 'left' not in node and 'right' not in node
                counts['leaves'] += 1
                counts['nonzero_leaf_gain'] += node['gain'] != 0
                depths[depth] += 1
                continue
            counts['internal'] += 1
            split = node['split']
            feature = None
            for kind, pattern in (
                ('lt', r'(.+?) < ([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)'),
                ('in', r'(.+?) in \{ (.+) \}'),
                ('is_missing', r'(.+?) is Missing'),
            ):
                match = re.fullmatch(pattern, split)
                if match:
                    kinds[kind] += 1
                    feature = match[1]
                    predictors.add(feature)
                    break
            if feature is None:
                unparsed.append({'tree': index, 'address': address, 'split': split})
            else:
                namespace = next((p for p in ('Customer.Scores.', 'Customer.', 'IH.', 'Param.') if feature.startswith(p)), 'py*' if feature.startswith('py') else 'other')
                namespace_gains.setdefault(namespace, []).append(node['gain'])
            gains.append(node['gain'])
            left, right = node['left'], node['right']
            child_count = left['sampleCount'] + right['sampleCount']
            deficits.append(node['sampleCount'] - child_count)
            if child_count:
                mean = (left['score'] * left['sampleCount'] + right['score'] * right['sampleCount']) / child_count
                mean_errors.append(abs(node['score'] - mean))
            for side in ('right', 'left'):
                stack.append((node[side], depth + 1, address + '.' + side))
    stats = data['trainingStats']
    roots = [t['sampleCount'] for t in trees]
    base = stats['positiveCount'] / stats['totalCount']
    root_sum = math.fsum(t['score'] for t in trees)
    logit = math.log(base / (1 - base))
    gates = {
        'success_rate_exact': data['successRate'] == base,
        'training_counts_sum': stats['positiveCount'] + stats['negativeCount'] == stats['totalCount'],
        'first_root_equals_total': roots[0] == stats['totalCount'],
        'roots_strictly_decrease': all(a > b for a, b in zip(roots, roots[1:])),
        'leaf_gain_zero': counts['nonzero_leaf_gain'] == 0,
        'binary_identity': counts['leaves'] == counts['internal'] + len(trees),
        'tree_count_range': 1 <= len(trees) <= 500,
        'leaf_depth_range': max(depths) <= 14,
        'conditions_recognized': not unparsed,
    }
    return dict(file_bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest(), tree_count=len(trees),
                counts=dict(counts), condition_counts=dict(kinds), unparsed_conditions=unparsed,
                predictor_count=len(predictors), total_internal_gain=math.fsum(gains),
                namespace_gain={k: math.fsum(v) for k, v in namespace_gains.items()},
                leaf_depth_distribution=dict(sorted(depths.items())), first_root_count=roots[0],
                last_root_count=roots[-1], training_stats=stats, success_rate=base, auc=data['auc'],
                stump_count=sum('split' not in t for t in trees),
                parent_count_equal_children=sum(d == 0 for d in deficits),
                parent_count_exceeds_children=sum(d > 0 for d in deficits),
                parent_count_below_children=sum(d < 0 for d in deficits),
                weighted_mean_tested=len(mean_errors), weighted_mean_tolerance=1e-12,
                weighted_mean_matches=sum(e <= 1e-12 for e in mean_errors),
                weighted_mean_max_error=max(mean_errors, default=0),
                root_score_sum=root_sum, base_rate_logit=logit, root_margin_residual=root_sum-logit,
                gates=gates, all_gates_pass=all(gates.values()))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('export', type=Path)
    args = parser.parse_args()
    result = inspect(args.export)
    print(json.dumps(result, indent=2, allow_nan=False))
    if not result['all_gates_pass']:
        raise SystemExit('STRUCTURAL GATE FAILURE: investigate parser and source evidence before proceeding')


if __name__ == '__main__':
    main()
