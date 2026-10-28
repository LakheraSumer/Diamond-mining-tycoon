"""Validate numbered review files and their exact source/render provenance.

This checks completeness and freshness, not visual similarity or animation quality.
Run after Render-CharacterViews.py and Assemble-CharacterViews.py. A nonzero exit
means at least one requested character has missing, stale or altered evidence.
"""
from pathlib import Path
from PIL import Image
import argparse,hashlib,json,sys

ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser()
parser.add_argument('--ids',help='Comma-separated character IDs; default all twelve')
parser.add_argument('--output',type=Path,default=ROOT/'ArtSource/Characters/view-validation.json')
args=parser.parse_args()
designs=json.loads((ROOT/'tools/art/character-designs.json').read_text(encoding='utf-8'))
identifiers=[i.strip().lower() for i in args.ids.split(',') if i.strip()] if args.ids else list(designs)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
names=['SourceCrop','Beauty','Front','Left','Right','Back','Face','Accessories','Materials','Turnaround']
audit=json.loads((ROOT/'ArtSource/Characters/mesh-audit.json').read_text(encoding='utf-8'))
required_clips={'Idle','Walk','Run','Mine','Collect','Carry','Wave','Celebrate','Machine','Craft'}
report={'scope':'File completeness, source SHA and render/composite provenance only. Does not establish visual acceptance, actual Unity deformation or mobile performance.',
        'characters':{},'passed':True}
for identifier in identifiers:
    folder=ROOT/'ArtSource/Characters'/designs[identifier]['name'];problems=[];outputs={}
    source=ROOT/'ArtSource/Characters'/(identifier+'.blend')
    model_hash=sha(source) if source.is_file() else None
    if model_hash is None:problems.append('Model source missing')
    reference=json.loads((folder/'reference.json').read_text(encoding='utf-8'))
    path=folder/'model-views.json'
    manifest=json.loads(path.read_text(encoding='utf-8')) if path.is_file() else {}
    if not manifest:problems.append('model-views.json missing')
    elif manifest.get('model_source_sha256')!=model_hash:problems.append('Rendered views use an older model source')
    if manifest.get('renderer_sha256')!=sha(ROOT/'tools/art/Render-CharacterViews.py'):
        problems.append('Renderer revision does not match current tool')
    if manifest.get('assembler_sha256')!=sha(ROOT/'tools/art/Assemble-CharacterViews.py'):
        problems.append('Composite revision does not match current tool')
    for i,name in enumerate(names,1):
        file=f'{i:02d}_{name}.png';path=folder/file
        if not path.is_file():problems.append(file+' missing');continue
        try:
            with Image.open(path) as im:dimensions=list(im.size);im.verify()
        except (OSError,ValueError):problems.append(file+' is not a valid complete image');continue
        digest=sha(path);outputs[file]={'sha256':digest,'dimensions':dimensions}
        expected=manifest.get('outputs',{}).get(file,{})
        if expected.get('sha256')!=digest or expected.get('dimensions')!=dimensions:
            problems.append(file+' lacks a matching final output hash/dimensions')
        if i==1 and reference['source_crop_sha256']!=digest:
            problems.append('Preserved source crop hash changed')
    for file,entry in manifest.get('raw_render_outputs',{}).items():
        path=folder/file
        if not path.is_file() or sha(path)!=entry['sha256']:problems.append(file+' raw render missing or altered')
    exporter=audit.get(identifier,{})
    if exporter.get('bones')!=47 or set(exporter.get('clips',[]))!=required_clips:
        problems.append('Exporter audit does not report 47 bones and ten required clips')
    passed=not problems;report['passed']&=passed
    report['characters'][identifier]={'passed':passed,'model_source_sha256':model_hash,
        'exporter_report_not_independent_unity_validation':exporter,'outputs':outputs,'problems':problems}
args.output.parent.mkdir(parents=True,exist_ok=True)
args.output.write_text(json.dumps(report,indent=2),encoding='utf-8')
for identifier,item in report['characters'].items():
    print(('PASS ' if item['passed'] else 'FAIL ')+identifier+': '+str(len(item['outputs']))+'/10 files, '+('; '.join(item['problems']) or 'fresh source and matching output hashes'))
print('Report: '+str(args.output))
sys.exit(0 if report['passed'] else 1)
