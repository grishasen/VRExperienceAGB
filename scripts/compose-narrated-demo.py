#!/usr/bin/env python3
"""Prepare English narration/subtitles, then mix the opt-in Unity demo recording."""
import argparse, json, re, subprocess, textwrap, wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'artifacts/narrated-demo-2026-10-18'
# Shot order and durations match NarratedDemoVideoTests. No production predictions are invented.
SCRIPT = [
('01-opening',12,'A model you can explore','Step inside a forest of decisions. This application turns a complete tree ensemble into a place you can explore, inspect, and understand.'),
('02-select-model',8,'01 / Choose a model','Start by choosing a saved model. Each tree in the forest represents one tree in the ensemble.'),
('03-package-loaded',6,'One ZIP. One model. Its profiles.','A ZIP package keeps the model and its prepared profiles together, in an isolated collection.'),
('04-forest-walk',14,'02 / Explore the forest','Walk between the trees and see the structure at human scale. Height reflects tree depth, while crown width reflects the number of leaves. Every tree remains available to inspect.'),
('05-lake',10,'A place to look around','The forest sits beside a lake, surrounded by mountain scenery. Broad views make it easy to step back and see the whole setting.'),
('06-stars',8,'Look toward Orion','Look to the other side of the forest, where Orion stands above the trees.'),
('07-orion',8,"Orion and its three-star belt","Three stars mark Orion's belt. Constellation guides help you trace the shape across the winter sky."),
('08-tree-card',8,'Inspect a tree','Select a tree to see its depth and leaf count, then open it for a closer look.'),
('09-tree-overview',8,'03 / Follow the decisions','Inside the tree, branching paths show how a sequence of questions leads toward a leaf.'),
('10-read-split',10,'Read the split','Here, tree seventeen tests a historical outcome count against eighteen. The two branches represent the exact alternatives: below the threshold, or at least the threshold.'),
('11-split-details',9,'Inspect the evidence','Open the split details to inspect its full predictor identifier and condition. The compact label and detailed evidence describe the same decision.'),
('12-manual-branch',8,'Choose a branch manually','In manual exploration, you choose the branch. The view advances to the next question along that route.'),
('13-manual-leaf',7,'A leaf contributes to the score','A leaf supplies one signed contribution. A manually chosen route is not a prediction for a customer.'),
('14-choose-profile',7,'04 / Play a prepared profile','Now choose a prepared profile. These demonstration inputs are synthetic, and their values determine the route.'),
('15-follow-profile',13,'Profile A / Tree 17','We focus again on tree seventeen. Profile A supplies a value of seven, so it follows the branch below eighteen. The animated marker makes the sequence of decisions visible.'),
('16-playback-controls',7,'Pause. Inspect. Continue.','Pause the playback whenever you need to read a decision. You can step through it or resume.'),
('17-resume',7,'A deterministic route','The same inputs produce the same route. Each reached leaf contributes to the running ensemble score.'),
('18-calculate-all',5,'Calculate the whole model','Calculate whole model skips the remaining animation, while evaluating every tree.'),
('19-score',9,'All 100 trees evaluated','The result combines all one hundred tree contributions. This is an ensemble score; calibrated propensity requires a separate calibration mapping.'),
('20-table',13,'05 / The entire forest on a table','Table View brings the entire model into one compact view. You can inspect all one hundred trees together, while their rings show the signed contributions for the selected profile.'),
('21-table-orbit',10,'Change perspective','Rotate the table to compare the trees from another angle. This changes the presentation, while the model and calculated result stay the same.'),
('22-compare',7,'06 / Compare profiles A and B','Choose two profiles independently to compare how their inputs change the route and final score.'),
('23-ab-route',17,'Two inputs. Two routes.','At the same split, profile A has a value of seven, while profile B has nineteen point eight. Their paths separate at eighteen. The gold and purple markers make both routes visible as they continue through the tree.'),
('24-ab-result',9,'Compare the complete results','Calculate the complete ensemble for both profiles. The result shows each score and the difference between them, using all one hundred trees.'),
('25-ab-table',9,'Both profiles on the same table','On the table, the outer ring represents profile A and the inner ring represents profile B.'),
('26-forest-results',6,'Keep the results in view','Return to the forest with both completed results available for inspection.'),
('27-final-walk',13,'From individual trees to the whole model','Walk among the trees again, now with the contributions of both profiles visible. Move between individual decisions and the whole ensemble to connect the route with the result.'),
('28-closing',8,'Explore. Inspect. Compare.','Explore the structure. Follow a profile. Compare the outcomes. A complete model, made spatial.'),
]


def run(args):
    return subprocess.run([str(x) for x in args], check=True, capture_output=True, text=True)


def prepare(voice_name='Daniel'):
    OUT.mkdir(parents=True, exist_ok=True)
    voice=OUT/'voice';voice.mkdir(exist_ok=True)
    timeline=0;manifest=[]
    for shot,duration,title,text in SCRIPT:
        speech=voice/(shot+'.txt');speech.write_text(text+'\n')
        wav=voice/(shot+'.wav')
        if text:
            aiff=voice/(shot+'.aiff')
            run(['say','-v',voice_name,'-r','160','-f',speech,'-o',aiff])
            run(['ffmpeg','-y','-v','error','-i',aiff,'-ar','48000','-ac','1','-c:a','pcm_s16le',wav])
            with wave.open(str(wav)) as w: spoken=w.getnframes()/w.getframerate()
            if spoken < .5:
                raise RuntimeError(f'{shot}: the speech service returned empty audio. Run with access to the macOS speech service.')
            maximum=duration-1.0
            if spoken>maximum:
                speed=spoken/maximum
                adjusted=voice/(shot+'-fit.wav')
                run(['ffmpeg','-y','-v','error','-i',wav,'-af',f'atempo={speed:.6f}','-c:a','pcm_s16le',adjusted])
                adjusted.replace(wav)
                with wave.open(str(wav)) as w: spoken=w.getnframes()/w.getframerate()
        else: spoken=0
        manifest.append(dict(id=shot,start=timeline,duration=duration,title=title,text=text,speech=spoken,voice=voice_name))
        timeline+=duration
    (OUT/'narration.json').write_text(json.dumps(manifest,indent=2)+'\n')
    # A mono narration master leaves clear space for the stereo environment.
    samples=bytearray(round(timeline*48000)*2)
    for item in manifest:
        if not item['text']:continue
        with wave.open(str(voice/(item['id']+'.wav'))) as w:data=w.readframes(w.getnframes())
        start=round((item['start']+.45)*48000)*2;samples[start:start+len(data)]=data
    with wave.open(str(OUT/'narration.wav'),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(48000);w.writeframes(samples)
    subtitles(manifest)
    (OUT/'narration-script.txt').write_text('\n\n'.join(f"{x['start']:05.1f}s  {x['title']}\n{x['text']}" for x in manifest)+'\n')
    print(json.dumps({'seconds':timeline,'words':sum(len(x['text'].split()) for x in manifest),'clips':len(manifest)}))


def stamp(t,ass=False):
    scale=100 if ass else 1000
    ticks=round(t*scale);h,ticks=divmod(ticks,3600*scale);m,ticks=divmod(ticks,60*scale);s,fraction=divmod(ticks,scale)
    return f'{h}:{m:02d}:{s:02d}.{fraction:02d}' if ass else f'{h:02d}:{m:02d}:{s:02d},{fraction:03d}'


def subtitles(items):
    header='''[Script Info]
ScriptType: v4.00+
PlayResX: 2560
PlayResY: 1440
WrapStyle: 0

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Subtitle,Helvetica,46,&H00FFFFFF,&H00FFFFFF,&H00101820,&H80101820,0,0,0,0,100,100,0,0,3,10,0,2,160,160,68,1
Style: Chapter,Helvetica,42,&H00E9F3F2,&H00FFFFFF,&H00202931,&H80202931,1,0,0,0,100,100,0,0,3,10,0,7,70,70,55,1
Style: Note,Helvetica,25,&H00C2CCC9,&H00FFFFFF,&H00101820,&H80101820,0,0,0,0,100,100,0,0,1,1,0,9,70,70,60,1
Style: Evidence,Helvetica,34,&H00FFFFFF,&H00FFFFFF,&H00101820,&H80101820,0,0,0,0,100,100,0,0,3,10,0,7,70,70,155,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
    ass=[header];srt=[];n=0
    for x in items:
        ass.append(f"Dialogue: 1,{stamp(x['start'],True)},{stamp(x['start']+min(x['duration'],5),True)},Chapter,,0,0,0,,{{\\fad(250,250)}}{x['title']}\n")
        evidence = {
            '15-follow-profile': 'FIRST SPLIT / TREE 17\\NHistoricalOutcomeCount < 18\\NProfile A: 7 / left branch',
            '23-ab-route': 'FIRST SPLIT / TREE 17\\NHistoricalOutcomeCount < 18\\NProfile A: 7 / left branch\\NProfile B: 19.8 / right branch',
        }.get(x['id'])
        if evidence:
            ass.append(f"Dialogue: 1,{stamp(x['start'],True)},{stamp(x['start']+7,True)},Evidence,,0,0,0,,{{\\fad(250,400)}}{evidence}\n")
        sentences=re.split(r'(?<=[.!?])\s+',x['text'].strip()) if x['text'] else []
        words=sum(len(s.split()) for s in sentences);offset=x['start']+.45
        for index,sentence in enumerate(sentences):
            duration=x['speech']*len(sentence.split())/max(1,words)
            end=offset+duration
            if 'sentence_cues' in x:
                cue=x['sentence_cues'][index]
                offset=x['start']+cue['start'];end=x['start']+cue['end']
            lines=textwrap.wrap(sentence,width=91)
            if len(lines)==2:
                tokens=sentence.split()
                candidates=[(' '.join(tokens[:i]),' '.join(tokens[i:])) for i in range(1,len(tokens))]
                lines=list(min((pair for pair in candidates if max(map(len,pair))<=91),key=lambda pair:abs(len(pair[0])-len(pair[1]))))
            body='\\N'.join(lines)
            ass.append(f'Dialogue: 2,{stamp(offset,True)},{stamp(end,True)},Subtitle,,0,0,0,,{body}\n')
            n+=1;srt.append(f'{n}\n{stamp(offset)} --> {stamp(end)}\n'+ '\n'.join(lines)+'\n')
            offset=end
    total=sum(x['duration'] for x in items)
    ass.append(f'Dialogue: 0,0:00:00.00,{stamp(total,True)},Note,,0,0,0,,UNITY EDITOR CAPTURE  /  SYNTHETIC PROFILES\n')
    (OUT/'captions.ass').write_text(''.join(ass));(OUT/'captions.srt').write_text('\n'.join(srt))


def compose():
    rate=(OUT/'audio-rate.txt').read_text().strip()
    items=json.loads((OUT/'narration.json').read_text());duration=sum(x['duration'] for x in items)
    recorded=[line.split('\t') for line in (OUT/'shots.tsv').read_text().splitlines()]
    assert [(x[0],float(x[2])) for x in recorded]==[(x['id'],x.get('source_duration',x['duration'])) for x in items], 'Storyboard and recording timings differ.'
    output=OUT/'VRExperienceAGB-narrated-demo-English.mp4'
    chapter_starts=[items[i]['start'] for i in [0,1,3,8,13,19,21,26]]
    chapter_titles=['A model you can explore','Choose a model','Explore the forest','Inspect a tree','Play a profile','Table view','Compare profiles','Final forest walk']
    metadata=[';FFMETADATA1','title=VRExperienceAGB — Explore, Inspect, Compare','language=eng']
    for i,start in enumerate(chapter_starts):
        end=chapter_starts[i+1] if i+1<len(chapter_starts) else duration
        metadata.extend(['[CHAPTER]','TIMEBASE=1/1000',f'START={round(start*1000)}',f'END={round(end*1000)}',f'title={chapter_titles[i]}'])
    chapters=OUT/'chapters.ffmetadata';chapters.write_text('\n'.join(metadata)+'\n')
    # Keep a clean scene master and duck ambience under narration.
    overlays=render_overlays()
    source_time=0;retime=[]
    for item in items:
        source_duration=item.get('source_duration',item['duration'])
        extra=item['duration']-source_duration
        if abs(extra)>0.0001:
            retime.append(f"{extra:.8f}*clip((PTS*TB-{source_time})/{source_duration},0,1)")
        source_time+=source_duration
    picture="[0:v]setpts='(PTS*TB+"+'+'.join(retime)+")/TB'[timed];[timed]" if retime else '[0:v]'
    filters=(picture+f"[4:v]overlay=eof_action=repeat,fade=t=in:st=0:d=0.7,fade=t=out:st={duration-.9}:d=0.9[v];"
        f"[1:a]atempo={source_time/duration:.8f},apad,volume=1.50[amb];[2:a]loudnorm=I=-17:TP=-2:LRA=7,aresample=48000,asplit=2[voice][side];"
        "[amb][side]sidechaincompress=threshold=0.015:ratio=5:attack=15:release=400[duck];"
        "[duck][voice]amix=inputs=2:duration=longest:normalize=0,alimiter=limit=0.891[a]")
    args=['ffmpeg','-y','-hide_banner','-loglevel','warning','-i',OUT/'scene.mp4','-f','f32le','-ar',rate,'-ac','2','-i',OUT/'ambience.f32','-i',OUT/'narration.wav','-f','ffmetadata','-i',chapters,'-f','concat','-safe','0','-i',overlays,
          '-filter_complex',filters,'-map','[v]','-map','[a]','-map_metadata','3','-map_chapters','3','-metadata:s:a:0','language=eng','-c:v','libx264','-preset','medium','-crf','18','-pix_fmt','yuv420p','-r','30','-c:a','aac','-ar','48000','-b:a','192k','-t',str(duration),'-movflags','+faststart','-progress',OUT/'compose-progress.txt','-stats_period','5',output]
    subprocess.run([str(a) for a in args],check=True)
    print(output)


def render_overlays():
    """Render sparse transparent caption cards without requiring an FFmpeg libass build."""
    from PIL import Image, ImageDraw, ImageFont
    folder=OUT/'overlays';folder.mkdir(exist_ok=True)
    events=[]
    def seconds(stamp):
        h,m,s=stamp.split(':');return int(h)*3600+int(m)*60+float(s)
    for line in (OUT/'captions.ass').read_text().splitlines():
        if not line.startswith('Dialogue: '):continue
        x=line[10:].split(',',9)
        events.append(dict(layer=int(x[0]),start=seconds(x[1]),end=seconds(x[2]),style=x[3],text=re.sub(r'\{[^}]*\}','',x[9]).replace('\\N','\n')))
    boundaries=sorted({e[t] for e in events for t in ('start','end')})
    fonts={style:ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc',size) for style,size in [('Subtitle',46),('Chapter',42),('Note',25),('Evidence',34)]}
    listing=['ffconcat version 1.0'];last=None
    for i,(start,end) in enumerate(zip(boundaries,boundaries[1:])):
        canvas=Image.new('RGBA',(2560,1440));draw=ImageDraw.Draw(canvas)
        for e in sorted(events,key=lambda e:e['layer']):
            if not e['start']<=start+0.0001<e['end']:continue
            font=fonts[e['style']];lines=e['text'].splitlines();size=font.size;line_height=round(size*1.27)
            widths=[draw.textlength(t,font=font) for t in lines];width=max(widths);height=line_height*len(lines)
            if e['style']=='Subtitle':x=(2560-width)/2;y=1440-68-height
            elif e['style']=='Chapter':x=70;y=55
            elif e['style']=='Evidence':x=70;y=155
            else:x=2560-70-width;y=60
            draw.rounded_rectangle((x-14,y-9,x+width+14,y+height+9),radius=9,fill=(5,17,26,225))
            for n,t in enumerate(lines):
                tx=x+(width-widths[n])/2 if e['style']=='Subtitle' else x
                draw.text((tx,y+n*line_height),t,font=font,fill=(245,249,248,255),anchor='lt')
        last=folder/f'{i:03d}.png';canvas.save(last)
        listing.extend([f"file '{last.name}'",'option framerate 1000',f'duration {end-start:.3f}'])
    listing.extend([f"file '{last.name}'",'option framerate 1000'])
    result=folder/'frames.ffconcat';result.write_text('\n'.join(listing)+'\n')
    print(f'Rendered {len(boundaries)-1} caption states.')
    return result

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('action',choices=['prepare','compose','overlays'])
    parser.add_argument('--voice',default='Daniel');parser.add_argument('--output-dir',type=Path)
    args=parser.parse_args()
    if args.output_dir:OUT=args.output_dir.resolve()
    if args.action=='prepare':prepare(args.voice)
    else:{'compose':compose,'overlays':render_overlays}[args.action]()
