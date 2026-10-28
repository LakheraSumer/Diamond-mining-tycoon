"""Create UI-only circular thumbnails from the actual model face renders; sources untouched."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageOps

ROOT=Path(__file__).resolve().parents[2]
UI=ROOT/'Assets/_Project/Resources/Ui'
OUT=ROOT/'ArtSource/UI/Portraits'
OUT.mkdir(parents=True,exist_ok=True)
NAMES=('Tavi','Mira','Oren','Zela','Pavi','Nilo','Etta','Rumi','Kavi','Sola','Beni','Daro')
rows=[]
for name in NAMES:
    source=ROOT/f'ArtSource/Characters/{name}/07_Face.png'
    if not source.exists():raise FileNotFoundError(source)
    image=Image.open(source).convert('RGBA')
    # Work at 4x the UI export size for smooth circular alpha and an ivory rim.
    thumb=ImageOps.fit(image,(1024,1024),method=Image.Resampling.LANCZOS)
    mask=Image.new('L',(1024,1024),0);ImageDraw.Draw(mask).ellipse((16,16,1008,1008),fill=255)
    thumb.putalpha(mask)
    ImageDraw.Draw(thumb).ellipse((20,20,1004,1004),outline=(244,231,202,255),width=16)
    outputs=[]
    for size,target in [(256,UI/f'crew_avatar_{name.lower()}.png'),(128,OUT/f'crew_avatar_{name.lower()}-128.png')]:
        thumb.resize((size,size),Image.Resampling.LANCZOS).save(target)
        outputs.append({'file':target.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'size':[size,size]})
    rows.append({'id':name.lower(),'source':source.relative_to(ROOT).as_posix(),
                 'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
                 'kind':'Circular thumbnail crop of actual Blender model face render',
                 'modification':'UI thumbnail resize, circular alpha and narrow ivory rim only; original face render is unchanged.',
                 'outputs':outputs})
manifest=ROOT/'ArtSource/UI/crew-portrait-manifest.json'
manifest.write_text(json.dumps({'script_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
                                'notes':'Actual model face thumbnails. They do not certify likeness to the original character roster.',
                                'portraits':rows},indent=2),encoding='utf-8')
print(f'Created {len(rows)} runtime 256px portraits and {len(rows)} 128px previews; original face renders unchanged.')
