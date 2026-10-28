"""Compare actual Unity captures with exact per-panel reference crops; no likeness scoring."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw, ImageFont, ImageChops, ImageEnhance

ROOT=Path(__file__).resolve().parents[2]
CAP=ROOT/'release/screenshots/ui-reference'
OUT=CAP/'comparisons'
OUT.mkdir(parents=True,exist_ok=True)
SCREENS=('explore','mine','factory','craft','shop','map','tasks','rank')
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',24)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
records=[]

def label(image,title,subtitle):
    board=Image.new('RGB',(image.width,image.height+86),'#fff8ed')
    board.paste(image,(0,86));draw=ImageDraw.Draw(board)
    draw.text((18,10),title,font=font,fill='#173149')
    draw.text((18,46),subtitle,font=small,fill='#4b4d4a')
    return board

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

for screen in SCREENS:
    source=ROOT/f'Docs/UI-Reference/{screen}/Source.png'
    capture=CAP/f'reference/{screen}.png'
    bounds=CAP/f'reference/{screen}-bounds.json'
    if not capture.exists():
        records.append({'screen':screen,'available':False,'reason':'Unity capture has not been produced.'})
        continue
    original=Image.open(source).convert('RGB')
    actual=Image.open(capture).convert('RGB')
    if abs(actual.width/actual.height-original.width/original.height)>.0005:
        raise ValueError(f'{screen}: unexpected Unity aspect {actual.size} vs source {original.size}')
    reference=original.resize(actual.size,Image.Resampling.LANCZOS)
    side=Image.new('RGB',(actual.width*2+20,actual.height),'#34342d')
    side.paste(reference,(0,0));side.paste(actual,(actual.width+20,0))
    label(side,f'{screen.upper()} — source reference (left), actual Main Play mode (right)',
          f'JPEG source {original.width}×{original.height}; enlarged for comparison. Actual {actual.width}×{actual.height}. No fidelity score assigned.').save(OUT/f'{screen}-side-by-side.png')
    overlay=Image.blend(reference,actual,.5)
    label(overlay,f'{screen.upper()} — 50% reference / 50% actual overlay',
          'Diagnostic composite. UI, camera, geometry, lighting, icons and typography can all contribute to differences.').save(OUT/f'{screen}-overlay50.png')
    difference=ImageEnhance.Contrast(ImageChops.difference(reference,actual)).enhance(2)
    label(difference,f'{screen.upper()} — absolute RGB difference, contrast ×2',
          'Diagnostic difference image; not an objective character or screen likeness percentage.').save(OUT/f'{screen}-difference.png')
    bound_report=json.loads(bounds.read_text(encoding='utf-8-sig'))
    edge=actual.copy();draw=ImageDraw.Draw(edge)
    for row in bound_report['elements']:
        for key,color in [('expected','#ee49bd'),('actual','#49f4ba')]:
            rect=row.get(key)
            if not rect:continue
            x,y,w,h=rect
            draw.rectangle((x*actual.width,y*actual.height,(x+w)*actual.width,(y+h)*actual.height),outline=color,width=2)
    label(edge,f'{screen.upper()} — measured UI rectangle check',
          'Magenta: recorded source bounds. Green: actual RectTransform bounds. This does not check artwork identity.').save(OUT/f'{screen}-bounds-overlay.png')
    errors=[max(row.get('error') or [1]) for row in bound_report['elements']]
    uncertain=[row['id'] for row in bound_report['elements']
               if row['measurementUncertaintyPixels']/min(original.size)>.005]
    records.append({'screen':screen,'available':True,'source_sha256':sha(source),
                    'actual_sha256':sha(capture),'source_dimensions':list(original.size),
                    'actual_dimensions':list(actual.size),'measured_element_count':len(errors),
                    'all_rectangles_present':all(row['present'] for row in bound_report['elements']),
                    'maximum_normalized_rectangle_error':max(errors),
                    'within_recorded_bounds_target':all(row['present'] and row['withinTarget'] for row in bound_report['elements']),
                    'source_uncertainty_exceeds_target_for':uncertain,
                    'original_png_pixel_identity_certified':False,
                    'total_visual_identity_score':None})

(CAP/'comparison-summary.json').write_text(json.dumps({
    'scope':'Actual Unity capture vs decoded JPEG per-panel crop. Rectangle error is against the measured manifest, not a visual fidelity score.',
    'source_limitation':'Named original PNG was unavailable. Manual source-edge uncertainty is 1.5 native pixels and can exceed the 0.5% acceptance target.',
    'screens':records},indent=2),encoding='utf-8')
manifest_path=CAP/'capture-manifest.json'
if manifest_path.exists():
    manifest=json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    manifest['images']=[row for row in manifest.get('images',[]) if '/comparisons/' not in row['file']]
    for path in sorted(OUT.glob('*.png')):
        manifest['images'].append({'file':path.relative_to(ROOT).as_posix(),'sha256':sha(path),
                                  'kind':'Diagnostic comparison composite',
                                  'note':'Labelled source + actual Unity comparison, overlay, bounds or absolute difference. Not a raw game screenshot; no likeness score.',
                                  'group':'UI diagnostic comparisons'})
    manifest_path.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(f'Compared {sum(r["available"] for r in records)} / {len(records)} Unity reference captures.')
