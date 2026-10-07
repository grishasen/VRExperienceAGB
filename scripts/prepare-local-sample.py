#!/usr/bin/env python3
"""Prepare local-only sample playback fixtures. Never certifies source scoring."""
import hashlib, json, math, re
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
source = ROOT / 'data/examples/export_Mobile_Click_Through_Rate_AGB_sample.json'
raw = source.read_text()
export = json.loads(raw)
out = ROOT / 'BoostingExperience/Assets/VRExperienceAGB/Resources/LocalModel'
out.mkdir(parents=True, exist_ok=True)
features, trees, thresholds = {}, [], {}
for i, tree in enumerate(export['model']['booster']['trees']):
    nodes = []
    def visit(n, address):
        if 'split' not in n:
            nodes.append(dict(id=address, kind='leaf', score=n['score']))
            return
        s=n['split']; numeric=re.fullmatch(r'(.+?) < (.+)',s); member=re.fullmatch(r'(.+?) in \{ (.+) \}',s); missing=re.fullmatch(r'(.+?) is Missing',s)
        node=dict(id=address,kind='split',trueChild=address+'/left',falseChild=address+'/right')
        if numeric:
            f,t=numeric.groups();t=float(t);node.update(feature=f,operator='lt',threshold=t)
            features.setdefault(f,dict(type='number',allowMissing=False,displayName=f.split('.')[-1]))
            thresholds.setdefault(f,[]).append(t)
        elif member:
            f,c=member.groups();values=[x.strip() for x in c.split(',')];node.update(feature=f,operator='in',values=values)
            feature=features.setdefault(f,dict(type='category',allowMissing=False,displayName=f.split('.')[-1],values=[]))
            feature['values']=sorted(set(feature['values']+values))
        elif missing:
            f=missing.group(1);node.update(feature=f,operator='is_missing')
        else: raise ValueError('Unsupported split: '+s)
        nodes.append(node);visit(n['left'],address+'/left');visit(n['right'],address+'/right')
    visit(tree,'root');trees.append(dict(id=f'tree[{i}]',rootId='root',weight=1,nodes=nodes))
# Missing-only predictors receive an explicit present numeric value in these synthetic fixtures.
for tree in trees:
    for node in tree['nodes']:
        if node.get('operator')=='is_missing':
            features.setdefault(node['feature'],dict(type='number',allowMissing=True,displayName=node['feature'].split('.')[-1]))
            features[node['feature']]['allowMissing']=True
sha=hashlib.sha256(raw.encode()).hexdigest();mid='local-sample-'+sha[:12]
model=dict(schemaVersion=1,modelId=mid,provenance='Local export reconstruction with synthetic fully supplied profiles. Left=true, zero baseline, unit weights. Source scoring not verified. Observed category sets define fixture values only, not production domains.',objective='binary_logistic',outcomeLabel='Click probability (source scoring unverified)',baseScore=0,features=features,trees=trees)
profiles=[]
for index,name in enumerate(['Synthetic baseline','Synthetic alternative','Synthetic boundary case']):
    values={}
    for f,spec in features.items():
        if spec['type']=='category':values[f]=spec['values'][min(index,len(spec['values'])-1)]
        elif f in thresholds:
            ts=sorted(set(thresholds[f]));values[f]=ts[len(ts)//2] if index==0 else ts[-1]+max(1,abs(ts[-1])*.1) if index==1 else ts[0]-max(1,abs(ts[0])*.1)
        else:values[f]=1
    profiles.append(dict(id=f'sample-{index}',displayName=name,values=values))
expected=[]
for profile in profiles:
    leaves=[]
    for tree in export['model']['booster']['trees']:
        n=tree;path='root'
        while 'split' in n:
            s=n['split'];num=re.fullmatch(r'(.+?) < (.+)',s);mem=re.fullmatch(r'(.+?) in \{ (.+) \}',s)
            hit=profile['values'][num[1]]<float(num[2]) if num else profile['values'][mem[1]] in [v.strip() for v in mem[2].split(',')] if mem else False
            side='left' if hit else 'right';n=n[side];path+='/'+side
        leaves.append(dict(leaf=path,contribution=n['score']))
    score=math.fsum(x['contribution'] for x in leaves);p=1/(1+math.exp(-score)) if score>=0 else math.exp(score)/(1+math.exp(score))
    expected.append(dict(profile=profile['id'],score=score,probability=p,trees=leaves))
for filename,value in [('model',model),('profiles',dict(schemaVersion=1,modelId=mid,profiles=profiles)),('expected',dict(sourceSha256=sha,results=expected))]:
    (out/(filename+'.json')).write_text(json.dumps(value,indent=2)+'\n')
(out/'export.json').write_text(raw)
print(f'Prepared {len(trees)} trees, {len(features)} predictors and {len(profiles)} synthetic profiles in ignored LocalModel resources.')
print([(v['profile'],v['score'],v['probability']) for v in expected])
