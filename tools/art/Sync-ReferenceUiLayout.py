"""Generate Unity-serializable arrays from the editable source measurement manifest."""
import argparse
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
source=ROOT/'Docs/UI-Reference/layout-manifest.json'
data=json.loads(source.read_text(encoding='utf-8-sig'))
runtime={'source':data['source'],'sourceHash':data['source_sha256'],'screens':[]}
for screen,entry in data['screens'].items():
    runtime['screens'].append({'id':screen,'width':entry['crop_size_px'][0],
                              'height':entry['crop_size_px'][1],
                              'elements':[{'id':e['id'],'rect':e['normalized_rect'],
                                           'fontSize':e['font']['size_px'],
                                           'uncertaintyPixels':e['measurement_uncertainty_px']}
                                          for e in entry['elements']]})
target=ROOT/'Assets/_Project/Resources/Ui/reference-layout-runtime.json'
full=ROOT/'Assets/_Project/Resources/Ui/reference-layout.json'
parser=argparse.ArgumentParser();parser.add_argument('--check',action='store_true');args=parser.parse_args()
if args.check:
    if json.loads(target.read_text(encoding='utf-8-sig'))!=runtime:
        raise SystemExit('Runtime measurement arrays differ; run this tool without --check.')
    if json.loads(full.read_text(encoding='utf-8-sig'))!=data:
        raise SystemExit('Runtime full manifest differs; run this tool without --check.')
    print('Editable source and both Unity measurement manifests agree.')
else:
    target.write_text(json.dumps(runtime,indent=2),encoding='utf-8')
    full.write_text(json.dumps(data,indent=2),encoding='utf-8')
    print(f'Wrote {len(runtime["screens"])} screens from measured source manifest.')
