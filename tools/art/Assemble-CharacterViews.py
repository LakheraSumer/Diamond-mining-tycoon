"""Label actual mesh/material renders and lay out turnarounds without repainting."""
from pathlib import Path
from PIL import Image,ImageOps,ImageDraw,ImageFont
import os,json,hashlib
ROOT=Path(__file__).resolve().parents[2]
designs=json.loads((ROOT/'tools/art/character-designs.json').read_text())
ids=[s.strip().lower() for s in os.environ.get('DMT_VIEW_IDS',','.join(designs)).split(',') if s.strip()]
font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',28)
small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',21)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
for identifier in ids:
    folder=ROOT/'ArtSource/Characters'/designs[identifier]['name']
    manifest=json.loads((folder/'model-views.json').read_text())
    reference=json.loads((folder/'reference.json').read_text())
    source=ROOT/manifest['model_source']
    if manifest['model_source_sha256']!=sha(source):
        raise RuntimeError(identifier+': rendered views are stale for the current .blend; render them again before assembling.')
    if reference['source_crop_sha256']!=sha(folder/'01_SourceCrop.png'):
        raise RuntimeError(identifier+': preserved source crop hash does not match reference.json.')
    for file,entry in manifest.get('raw_render_outputs',{}).items():
        if sha(folder/file)!=entry['sha256']:
            raise RuntimeError(identifier+': raw render changed: '+file)
    # Labels describe actual extracted assemblies; no invented equipment is added.
    # Always start with the unlabelled raw render, so rerunning is idempotent.
    accessories=Image.open(folder/'_08_Accessories.png').convert('RGB');draw=ImageDraw.Draw(accessories)
    for assembly in manifest['views']['08_Accessories.png']['assemblies']:
        u,v=assembly['label_anchor_normalized'];x=u*accessories.width;y=v*accessories.height+170
        text=assembly['label'];box=draw.textbbox((0,0),text,font=small)
        draw.text((x-(box[2]-box[0])/2,min(accessories.height-35,y)),text,font=small,fill='#29343c')
    accessories.save(folder/'08_Accessories.png')
    raw=Image.open(folder/'_09_MaterialPreview.png').convert('RGB')
    palette=Image.new('RGB',(1600,1320),'#efede5');palette.paste(raw,(0,85));draw=ImageDraw.Draw(palette)
    draw.text((25,16),designs[identifier]['name']+' · actual model materials and observed source palette',font=font,fill='#29343c')
    for material in manifest['material_samples']:
        u,v=material['label_anchor_normalized'];text=material['key'];box=draw.textbbox((0,0),text,font=small)
        draw.text((u*1200-(box[2]-box[0])/2,v*1200+85+107),text,font=small,fill='#29343c')
    draw.text((1240,115),'Roster pixel samples',font=small,fill='#29343c')
    for i,sample in enumerate(reference.get('labelled_source_samples',reference['source_palette'])):
        y=165+i*128;rgb=tuple(sample['rgb']);draw.rounded_rectangle((1240,y,1545,y+64),radius=7,fill=rgb)
        draw.text((1240,y+66),sample.get('label','Source sample '+str(i+1)),font=small,fill='#29343c')
        draw.text((1240,y+93),'#'+''.join(f'{v:02X}' for v in rgb),font=small,fill='#29343c')
    draw.text((25,1280),'Left: neutral PBR previews. Right: sampled image colours, not verified PBR values.',font=small,fill='#59666c')
    palette.save(folder/'09_Materials.png')
    sheet=Image.new('RGB',(2400,1540),'#efede5');draw=ImageDraw.Draw(sheet)
    draw.text((24,20),designs[identifier]['name'].upper()+' · ACTUAL MODEL REVIEW',font=font,fill='#29343c')
    draw.text((24,62),'Source revision '+manifest['model_source_sha256'][:16]+' · Side/back reconstructed · This contact sheet is a layout of native renders, not native 4K.',font=small,fill='#59666c')
    for i,(file,title) in enumerate([('02_Beauty.png','Beauty / authored idle'),('03_Front.png','Front / neutral'),('04_Left.png','Left / reconstructed'),('05_Right.png','Right / reconstructed'),('06_Back.png','Back / reconstructed')]):
        im=Image.open(folder/file).convert('RGB');im=ImageOps.contain(im,(460,660),Image.Resampling.LANCZOS)
        x=10+i*480;y=115;sheet.paste(im,(x+(460-im.width)//2,y));draw.text((x+12,785),title,font=small,fill='#29343c')
    for (file,title,x,width) in [('01_SourceCrop.png','Original roster copy crop',0,360),('07_Face.png','Actual model face',360,600),('08_Accessories.png','Actual extracted accessories',960,720),('09_Materials.png','Actual materials / source colours',1680,720)]:
        im=Image.open(folder/file).convert('RGB');im=ImageOps.contain(im,(width-24,610),Image.Resampling.LANCZOS)
        sheet.paste(im,(x+(width-im.width)//2,875));draw.text((x+12,830),title,font=small,fill='#29343c')
    sheet.save(folder/'10_Turnaround.png')
    compare=Image.new('RGB',(1600,1210),'#efede5');draw=ImageDraw.Draw(compare)
    draw.text((20,20),'Original roster copy · enlarged',font=font,fill='#29343c');draw.text((820,20),'Actual current model · Blender',font=font,fill='#29343c')
    for file,x in [('01_SourceCrop.png',0),('02_Beauty.png',800)]:
        im=Image.open(folder/file).convert('RGB');im=ImageOps.contain(im,(780,1110),Image.Resampling.LANCZOS);compare.paste(im,(x+(800-im.width)//2,80))
    compare.save(folder/'11_ReferenceVsModel.png')
    manifest['compositing']='Labels, uniform image fitting and contact-sheet layout only. No generated/painted replacement of actual model renders.'
    manifest['assembler_sha256']=sha(Path(__file__))
    manifest['outputs']={f'{i:02d}_{name}.png':{'sha256':sha(folder/f'{i:02d}_{name}.png'),'dimensions':list(Image.open(folder/f'{i:02d}_{name}.png').size)} for i,name in enumerate(['SourceCrop','Beauty','Front','Left','Right','Back','Face','Accessories','Materials','Turnaround'],1)}
    manifest['outputs']['11_ReferenceVsModel.png']={'sha256':sha(folder/'11_ReferenceVsModel.png'),'dimensions':list(compare.size)}
    (folder/'model-views.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print('ASSEMBLED-CHARACTER-VIEWS',identifier,flush=True)
