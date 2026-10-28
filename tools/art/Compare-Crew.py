"""Lay out exact roster crops beside hash-verified actual Unity captures.

Only crop/resize/layout/text operations are performed. Camera and pose matching
remain approximate; these diagnostic composites do not assign likeness scores.
"""
from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw,ImageFont,ImageOps

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'release/screenshots/characters'
designs=json.loads((ROOT/'tools/art/character-designs.json').read_text(encoding='utf-8'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest_path=OUT/'evidence-manifest.json'
if not manifest_path.is_file():
    raise RuntimeError('Run the current Main crew review first; its capture evidence-manifest.json is required.')
manifest=json.loads(manifest_path.read_text(encoding='utf-8-sig'))
captured={row['file']:row for row in manifest.get('images',[])}
font_path=ROOT/'Assets/_Project/Resources/Fonts/Nunito.ttf'
if not font_path.is_file():font_path=Path('C:/Windows/Fonts/segoeuib.ttf')
font=ImageFont.truetype(str(font_path),30)
small=ImageFont.truetype(str(font_path),20)
inputs=[];outputs=[]

def record(path):return {'file':path.relative_to(ROOT).as_posix(),'sha256':sha(path)}
def fit(board,path,box):
    image=Image.open(path).convert('RGB')
    image=ImageOps.contain(image,(box[2],box[3]),Image.Resampling.LANCZOS)
    board.paste(image,(box[0]+(box[2]-image.width)//2,box[1]+(box[3]-image.height)//2))
def save(board,name):
    path=OUT/name;board.save(path);outputs.append(record(path))

for identifier,spec in designs.items():
    actual=OUT/(identifier+'-unity.png');relative=actual.relative_to(ROOT).as_posix()
    if relative not in captured or captured[relative].get('sha256')!=sha(actual):
        raise RuntimeError('Unity input has no matching capture hash: '+relative)
    folder=ROOT/'ArtSource/Characters'/spec['name']
    source=folder/'01_SourceCrop.png'
    reference=json.loads((folder/'reference.json').read_text(encoding='utf-8'))
    if reference.get('source_crop_sha256')!=sha(source):
        raise RuntimeError('Preserved source crop changed: '+str(source))
    inputs.extend([record(source),record(actual)])
    board=Image.new('RGB',(1600,1240),'#f8f2e6');draw=ImageDraw.Draw(board)
    draw.text((25,18),spec['name']+' · preserved roster crop, enlarged',font=font,fill='#23313b')
    draw.text((825,18),spec['name']+' · actual Unity model',font=font,fill='#23313b')
    fit(board,source,(20,80,760,1100));fit(board,actual,(820,80,760,1100))
    draw.text((25,1200),'Uniform image fit; source camera and pose are approximate. Reconstructed design; no likeness score.',font=small,fill='#45505a')
    save(board,identifier+'-reference-vs-unity.png')

sheet=Image.new('RGB',(1536,1024),'#e1dbca');draw=ImageDraw.Draw(sheet)
draw.text((35,18),'DIAMOND MINE TYCOON | Actual Unity crew',font=font,fill='#23313b')
draw.text((35,61),'Reference-directed reconstructions. Faces, poses and inferred surfaces remain different.',font=small,fill='#45505a')
for i,(identifier,spec) in enumerate(designs.items()):
    x=(i%6)*256;y=102+(i//6)*452
    fit(sheet,OUT/(identifier+'-unity.png'),(x,y,256,352))
    draw.text((x+18,y+365),spec['name'],font=font,fill='#23313b')
save(sheet,'crew-unity-lineup.png')

roster=ROOT/'Assets/_Project/Resources/Ui/crew-lineup.jpg';inputs.append(record(roster))
board=Image.new('RGB',(3072,1100),'#f8f2e6');draw=ImageDraw.Draw(board)
draw.text((25,12),'PRESERVED ROSTER REFERENCE · JPEG COPY',font=font,fill='#23313b')
draw.text((1561,12),'ACTUAL UNITY MODELS · likeness remains unaccepted',font=font,fill='#23313b')
fit(board,roster,(0,60,1536,1024));board.paste(sheet,(1536,60))
save(board,'reference-vs-unity-crew.png')

contact=Image.new('RGB',(2560,1870),'#f8f2e6');draw=ImageDraw.Draw(contact)
draw.text((25,15),'CREW REVIEW | preserved source crops (left) and actual Unity (right)',font=font,fill='#23313b')
draw.text((25,58),'Uniform fits at comparison size, not matched-camera renders. Source enlarged; no similarity percentage assigned.',font=small,fill='#45505a')
for i,(identifier,spec) in enumerate(designs.items()):
    x=(i%4)*640;y=110+(i//4)*580
    draw.text((x+20,y),spec['name'],font=font,fill='#23313b')
    fit(contact,ROOT/'ArtSource/Characters'/spec['name']/'01_SourceCrop.png',(x+8,y+45,300,500))
    fit(contact,OUT/(identifier+'-unity.png'),(x+324,y+45,300,500))
save(contact,'crew-reference-vs-unity-contact-sheet.png')

context={'reference':'Assets/_Project/Resources/Ui/crew-lineup.jpg','reference_sha256':sha(roster),
         'operations':'Exact preserved character crops, uniform resize, layout and labels only. No generated/painted replacement of Unity screenshots.',
         'camera_pose_alignment':'Approximate; composites do not certify matched camera, pose or lighting.',
         'source_limitation':'Available preserved JPEG roster copy; named original PNG was not found. Side/back geometry is reconstructed.',
         'tool_sha256':sha(Path(__file__)),'inputs':inputs,'outputs':outputs,'likeness_score':None}
(OUT/'comparison-context.json').write_text(json.dumps(context,indent=2),encoding='utf-8')
names={row['file'] for row in outputs}
manifest['images']=[row for row in manifest.get('images',[]) if row['file'] not in names]
for row in outputs:
    manifest['images'].append({**row,'kind':'Diagnostic source-versus-Unity comparison composite','group':'Comparisons',
                              'note':'Uniform layout of exact decoded roster crops and hash-verified actual Unity captures; approximate camera/pose; no likeness score.'})
manifest['comparison_inputs']=inputs
manifest['comparison_context_sha256']=sha(OUT/'comparison-context.json')
manifest_path.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Created 12 individual and 3 lineup comparison images; hashes appended to crew capture manifest.')
