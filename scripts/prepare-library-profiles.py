#!/usr/bin/env python3
"""Create two synthetic profiles and independent leaf-sum references for an AGB export.

No customer data is read. Threshold/category-derived values are test inputs, not
realistic customer distributions. The original export is never modified.
"""
import argparse
import hashlib
import json
import math
import re
from pathlib import Path


def split(text):
    for kind, pattern in [('lt', r'(.+?) < (.+)'), ('in', r'(.+?) in \{ (.+) \}'), ('missing', r'(.+?) is Missing')]:
        match = re.fullmatch(pattern, text)
        if match:
            values = float(match[2]) if kind == 'lt' else [s.strip() for s in match[2].split(',')] if kind == 'in' else None
            return kind, match[1], values
    raise ValueError('Unsupported split: ' + text)


def prepare(source, output):
    raw = source.read_bytes()
    trees = json.loads(raw)['model']['booster']['trees']
    thresholds, categories, features = {}, {}, set()

    def inspect(node):
        if 'split' not in node:
            return
        kind, feature, value = split(node['split'])
        features.add(feature)
        if kind == 'lt':
            thresholds.setdefault(feature, set()).add(value)
        elif kind == 'in':
            categories.setdefault(feature, set()).update(value)
        inspect(node['left'])
        inspect(node['right'])

    for tree in trees:
        inspect(tree)
    if thresholds.keys() & categories.keys():
        raise ValueError('Mixed numeric/category predictors need explicit fixture values.')
    sha = hashlib.sha256(raw).hexdigest()
    model_id = 'agb-trees-' + sha
    output.mkdir(parents=True, exist_ok=True)
    results = []
    for index, label in enumerate(('A', 'B')):
        values = {}
        for feature in sorted(features):
            if feature in categories:
                options = sorted(categories[feature])
                values[feature] = options[0 if index == 0 else -1]
            elif feature in thresholds:
                options = sorted(thresholds[feature])
                values[feature] = options[len(options)//2] if index == 0 else options[-1] + max(1, abs(options[-1]) * .1)
            else:
                values[feature] = 1
        profile = dict(id='synthetic-' + label.lower(), displayName='Synthetic ' + label, values=values)
        envelope = dict(schemaVersion=1, modelId=model_id, profiles=[profile])
        filename = 'AGB_sample_synthetic_' + label + '.json'
        (output / filename).write_text(json.dumps(envelope, indent=2) + '\n')
        leaves = []
        for tree in trees:
            node, address = tree, 'root'
            while 'split' in node:
                kind, feature, value = split(node['split'])
                actual = values[feature]
                hit = actual < value if kind == 'lt' else actual in value if kind == 'in' else actual is None
                side = 'left' if hit else 'right'
                node, address = node[side], address + '/' + side
            leaves.append(dict(leaf=address, contribution=node['score']))
        results.append(dict(profile=profile['id'], file=filename, score=math.fsum(n['contribution'] for n in leaves), trees=leaves))
    reference = dict(source=source.name, sourceSha256=sha, modelId=model_id, treeCount=len(trees), featureCount=len(features), results=results)
    (output / 'expected.json').write_text(json.dumps(reference, indent=2) + '\n')
    print(json.dumps({k: v for k, v in reference.items() if k != 'results'}))
    print([(r['profile'], r['score']) for r in results])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    prepare(args.source, args.output)
