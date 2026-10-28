"""Build an offline visual review of actual captures, references and earlier work."""
from pathlib import Path
from datetime import datetime,timezone,timedelta
import argparse,base64,hashlib,html,io,json,zipfile
from PIL import Image,ImageOps,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'release/visual-review';OUT.mkdir(parents=True,exist_ok=True)
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--capture-manifest',type=Path,action='append',default=[],help='Explicit current-capture provenance JSON; entries under images need repository-relative file and sha256.')
parser.add_argument('--skip-archive',action='store_true',help='Refresh the offline page/index without rebuilding the image ZIP.')
args=parser.parse_args()
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def read_json(path):
    try:return json.loads(path.read_text(encoding='utf-8-sig'))
    except (OSError,ValueError):return {}
capture_entries={}
manifest_paths=[ROOT/'release/screenshots/current-evidence.json',
                ROOT/'release/screenshots/ui-reference/capture-manifest.json',
                ROOT/'release/screenshots/characters/evidence-manifest.json',
                ROOT/'release/screenshots/gameplay-current/evidence-manifest.json']+args.capture_manifest
for manifest_path in manifest_paths:
    data=read_json(manifest_path)
    entries=data.get('images',[]) if isinstance(data,dict) else []
    for entry in entries:
        relative=entry.get('file','').replace('\\','/')
        if relative and entry.get('sha256'):
            entry['capture_manifest']=str(manifest_path)
            capture_entries[relative]=entry
def current_capture(relative,path):
    entry=capture_entries.get(relative,{})
    return entry if str(entry.get('sha256','')).lower()==sha(path) else {}
current=ROOT/'release/screenshots/gameplay-current'
screens=[('01-explore','EXPLORE'),('02-mine','MINE'),('03-process','PROCESS'),('04-craft','CRAFT'),('05-sell','SELL'),('06-expand','EXPAND'),('07-progress','PROGRESS'),('08-rankings','RANKINGS')]
if all((current/(file+'.png')).is_file() for file,_ in screens):
    sheet=Image.new('RGB',(1600,1510),'#eae7dc');draw=ImageDraw.Draw(sheet)
    font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',31);small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
    draw.text((22,20),'DIAMOND MINE TYCOON · GAMEPLAY CAPTURE OVERVIEW',font=font,fill='#20313c')
    draw.text((22,65),'Layout of existing Unity captures. Check each image record for its revision and capture context.',font=small,fill='#53656b')
    for i,(file,title) in enumerate(screens):
        x=20+(i%4)*400;y=120+(i//4)*690
        draw.text((x,y),str(i+1)+' · '+title,font=small,fill='#20313c')
        image=Image.open(current/(file+'.png')).convert('RGB').resize((360,640),Image.Resampling.LANCZOS)
        sheet.paste(image,(x,y+34))
    sheet.save(current/'00-eight-screens-overview.jpg',quality=93)
roots=['release/screenshots','Docs/design-boards','Docs/UI-Reference','ArtSource/Characters','ArtSource/UI',
       'Assets/_Project/Resources/Ui','Assets/_Project/Art/Crew/Textures',
       'Assets/_Project/Art/TimberBunker/Textures','Assets/_Project/Art/GameplayEnvironments/Textures']
names={k:v['name'] for k,v in json.loads((ROOT/'tools/art/character-designs.json').read_text()).items()}
view_manifests={name:read_json(ROOT/'ArtSource/Characters'/name/'model-views.json') for name in names.values()}
fresh_models={}
for identifier,name in names.items():
    model=ROOT/'ArtSource/Characters'/(identifier+'.blend')
    fresh_models[name]=model.is_file() and view_manifests[name].get('model_source_sha256')==sha(model)
image_assets={}
for row in read_json(ROOT/'ArtSource/UI/Icons/render-manifest.json').get('icons',[]):
    source=ROOT/row.get('source','');path=ROOT/row.get('file','')
    fresh=source.is_file() and path.is_file() and row.get('blend_sha256')==sha(source) and row.get('sha256')==sha(path)
    image_assets[row.get('file','')]={'fresh':fresh,'kind':'Actual Blender prop render',
        'note':'Native 512px RGBA render of an authored, editable 3D prop. Reconstructed UI artwork, not recovered original reference pixels.',
        'provenance':'Exact .blend and PNG hashes match ArtSource/UI/Icons/render-manifest.json.'}
for row in read_json(ROOT/'ArtSource/UI/crew-portrait-manifest.json').get('portraits',[]):
    source=ROOT/row.get('source','');name=names.get(row.get('id',''),'')
    fresh=source.is_file() and row.get('source_sha256')==sha(source) and fresh_models.get(name,False)
    for output in row.get('outputs',[]):
        path=ROOT/output.get('file','')
        image_assets[output.get('file','')]={'fresh':fresh and path.is_file() and output.get('sha256')==sha(path),
            'kind':'Actual-model face thumbnail','note':'Circular thumbnail cropped/resized from the actual Blender character face render; alpha and narrow rim added. Source face render is unchanged; likeness is unaccepted.',
            'provenance':'Source face PNG and thumbnail hashes match ArtSource/UI/crew-portrait-manifest.json; the face view uses the current saved model.'}
friendly={'01-explore':'1 · Explore','02-mine':'2 · Mine','03-process':'3 · Process','04-craft':'4 · Craft','05-sell':'5 · Sell','06-expand':'6 · Expand','07-progress':'7 · Progress','08-rankings':'8 · Rankings','09-character-selection':'Character selection','10-inventory':'Inventory','11-more':'More menu','12-entire-yard':'Entire current yard',
 'play-mode-character-front':'Character beside timber bunker','play-mode-character':'Explore beside timber bunker','play-mode-character-mining':'Character mining interaction','play-mode-character-cart':'Ore cart in motion','crew-unity-lineup':'Twelve-character Unity lineup','reference-vs-unity-crew':'Character reference versus Unity','tavi-reference-vs-unity':'Tavi reference versus Unity','reference-vs-unity':'Environment reference versus Unity','unity-closeup':'Bunker, terrain, rails and cart close-up','yard-board':'World reference board','gameplay-board':'Eight-step gameplay reference','crew-lineup':'Original character reference sheet'}
friendly['00-eight-screens-overview']='Eight gameplay captures · overview'
items=[]
for root in roots:
    for p in sorted((ROOT/root).rglob('*')):
        if p.suffix.lower() not in ('.png','.jpg','.jpeg'):continue
        relative=p.relative_to(ROOT).as_posix();stem=p.stem
        # Raw renderer intermediates are retained on disk. The gallery shows
        # their labelled final equivalents rather than duplicate internal views.
        if relative.startswith('ArtSource/Characters/') and (p.name.startswith('_') or '/third_party/' in relative):continue
        title=friendly.get(stem,stem.replace('-',' ').replace('_',' ').title())
        group='Archive';kind='Earlier capture';note='Historical image. It may show assets or UI that have since changed.';current=False
        provenance='No matching current-capture manifest. File timestamps alone do not establish the current source revision.'
        if relative.startswith('ArtSource/Characters/'):
            name=p.parent.name;manifest=view_manifests.get(name,{})
            if p.name=='01_SourceCrop.png' or name=='References':
                group='References';kind='Preserved roster reference';note='Decoded pixels of the available JPEG roster copy, not a current game capture. The named original PNG was not found.'
            elif p.name=='AuxiliarySheet.png':
                group='References';kind='Supplied modelling guidance';note='Secondary generated character guidance. Original roster identity takes priority; hidden surfaces are reconstructed.';title=name+' · auxiliary modelling sheet'
            else:
                group='Characters';kind='Actual Blender model view'
                fresh=fresh_models.get(name,False) and manifest.get('outputs',{}).get(p.name,{}).get('sha256')==sha(p)
                current=bool(fresh)
                note='Actual saved mesh render. Reconstructed design; visual fidelity is unscored. Side/back surfaces are inferred.'
                if not fresh:kind='Unverified or older model view';note+=' Source/output hashes are not verified against the current model.'
                else:provenance='Current .blend SHA and exact image SHA match model-views.json. This is not a Unity gameplay validation.'
                view_name=stem.split('_',1)[-1].replace('Vs',' versus ').replace('SourceCrop','source crop')
                title=name+' · '+view_name
                if p.name=='11_ReferenceVsModel.png':group='Comparisons';kind='Source versus actual Blender model' if fresh else 'Older or unverified model comparison'
                if p.name=='10_Turnaround.png':note+=' This is a labelled contact-sheet layout of separate native views, not a native 4K render.'
        elif relative.startswith('Docs/UI-Reference/'):
            group='References';kind='Preserved UI reference crop';note='Exact rectangular crop of the decoded JPEG gameplay-board copy. It is target artwork, not a Unity screenshot.';title=p.parent.name.title()+' · '+stem
        elif '/ui-reference/' in relative:
            screen=stem.split('-')[0].title()
            if '/comparisons/' in relative:
                group='Comparisons';kind='Labelled UI diagnostic composite'
                note='Reference crop versus actual Unity capture. Uniform placement/overlay/difference only; no repainted game image. Source uncertainty and remaining mismatches must be read with the bounds report.'
                title=screen+' · '+('50% overlay' if 'overlay50' in stem else 'image difference' if 'difference' in stem else 'side-by-side comparison')
            else:
                group='UI review';kind='Unity fixed-aspect reference review' if '/reference/' in relative else 'Unity adaptive live UI review'
                note='Actual Main Game-view capture. Reference-review fixtures are isolated from player saves.' if '/reference/' in relative else 'Actual Main Game-view capture using live state and adaptive 720 × 1280 layout.'
                title=screen+' · '+('fixed reference aspect' if '/reference/' in relative else 'live portrait layout')
        elif '/gameplay-current/' in relative:
            group='Gameplay';kind='Unity Play-mode capture';note='Existing starter QA capture. Check provenance before treating it as the latest source revision.'
            if stem=='12-entire-yard':group='Environment';kind='Unity yard camera render';note='Full yard camera render during Main Play mode. Detailed environments and surrounding gameplay remain development art.'
        elif relative.startswith('release/screenshots/characters/'):
            if 'reference-vs-' in stem or '-reference-vs-' in stem:
                group='Comparisons';kind='Labelled Unity comparison';note='Supplied reference and actual Unity models from this capture revision. The requested 100% visual match is not achieved.'
            elif stem.startswith('play-mode-'):
                group='Gameplay';kind='Unity Play-mode capture';note='Actual Main Game-view capture using imported crew and environment assets from this capture revision.'
            elif '-blender' not in stem:
                group='Characters';kind='Unity character camera render';note='Imported, rigged reconstruction from this capture revision. Facial likeness, hair, clothing and posing differ from the reference.'
                for identifier,name in names.items():
                    if stem.startswith(identifier+'-'):
                        view='face close-up' if '-face-' in stem else ('side view' if '-side-' in stem else ('back view' if '-back-' in stem else 'full character'))
                        title=name+' · '+view;break
            else:kind='Earlier Blender look development'
        elif '/timber-bunker/' in relative:
            if stem=='reference-vs-unity':group='Comparisons';kind='Labelled comparison';note='Reference crop versus the authored environment from this capture revision. It is not a pixel-identical match.'
            elif stem=='unity-closeup':group='Environment';kind='Unity environment render';note='Timber bunker, rocky terrain, shaped rails and ore cart close-up. UI and surrounding plots are excluded.'
            elif stem.startswith('play-mode-'):group='Environment';kind='Earlier Unity Play capture';note='Environment review made before the crew replacement. The character shown here is the older placeholder.'
            else:kind='Blender environment look development'
        elif '/portrait-after/' in relative:
            group='Earlier UI';kind='Earlier interface QA capture';note='Earlier works-mine UI review, before the current yard and crew update.';title+=' · '+p.parent.name
        elif root in ('Docs/design-boards','Assets/_Project/Resources/Ui'):
            group='References';kind='Supplied visual reference';note='Target artwork, not a screenshot of the current game.'
            if root=='Assets/_Project/Resources/Ui' and stem!='crew-lineup':group='UI assets';kind='UI image asset';note='Project UI icon, illustration or surface asset. It is not a complete game screenshot.'
        elif root.endswith('/Textures'):
            group='Textures';kind='Authored texture map';note='Asset texture map, not a game screenshot.';title=('Crew · ' if '/Crew/' in relative else 'Environment · ')+title
        asset=image_assets.get(relative)
        if asset:
            group='UI assets';kind=asset['kind'];note=asset['note'];current=bool(asset['fresh'])
            provenance=asset['provenance'] if current else 'Asset source/output hashes are not current; retained for inspection.'
        captured=current_capture(relative,p)
        if captured and group not in ('References','Textures','UI assets'):
            current=True;provenance='Exact image SHA matches '+captured.get('capture_manifest','the supplied current-capture manifest')+'.'
            kind=captured.get('kind',kind);note=captured.get('note',note)
            capture_group=captured.get('group',group)
            aliases={'Reference review screens':'UI review','Live gameplay screens':'Gameplay','UI diagnostic comparisons':'Comparisons'}
            capture_group=aliases.get(capture_group,capture_group)
            if capture_group in ('Gameplay','UI review','Environment','Characters','Comparisons','Earlier UI','Archive'):group=capture_group
        im=Image.open(p);im.verify()
        im=Image.open(p);width,height=im.size
        # Thumbnail background only. Original transparent PNG remains untouched.
        if 'A' in im.getbands():
            background=Image.new('RGBA',im.size,(238,236,228,255));background.alpha_composite(im.convert('RGBA'));im=background.convert('RGB')
        else:im=im.convert('RGB')
        thumb=ImageOps.contain(im,(440,420),Image.Resampling.LANCZOS);buffer=io.BytesIO();thumb.save(buffer,format='JPEG',quality=82)
        url='../../'+relative
        items.append({'id':len(items),'title':title,'group':group,'kind':kind,'note':note,'provenance':provenance,'current':current,'src':url,'file':relative,
            'width':width,'height':height,'updated':datetime.fromtimestamp(p.stat().st_mtime,timezone(timedelta(hours=5,minutes=30))).strftime('%d %b %Y, %H:%M IST'),
            'thumb':'data:image/jpeg;base64,'+base64.b64encode(buffer.getvalue()).decode()})

priority=['ArtSource/Characters/Tavi/10_Turnaround.png','ArtSource/Characters/Mira/10_Turnaround.png',
          'release/screenshots/ui-reference/comparisons/explore-side-by-side.png',
          'release/screenshots/ui-reference/reference/explore.png','release/screenshots/characters/play-mode-character-front.png',
          'release/screenshots/gameplay-current/12-entire-yard.png','release/screenshots/characters/crew-unity-lineup.png',
          'release/screenshots/timber-bunker/unity-closeup.png','release/screenshots/characters/reference-vs-unity-crew.png',
          'release/screenshots/gameplay-current/00-eight-screens-overview.jpg']
items.sort(key=lambda x:(priority.index(x['file']) if x['file'] in priority else 100,x['group'],x['file']))
for i,item in enumerate(items):item['id']=i;item['highlight']=item['file'] in priority and item['current']
data=json.dumps(items,ensure_ascii=False).replace('</','<\\/')
template=r'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Diamond Mine Tycoon — Visual Review</title>
<style>
:root{color-scheme:light;--ink:#20313c;--muted:#68777f;--paper:#f4f2eb;--teal:#116a6e;--line:#dce1dc}*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.5 system-ui,Segoe UI,sans-serif}button,input{font:inherit}button,a{touch-action:manipulation}button{cursor:pointer}header{background:#18333d;color:white;padding:28px max(5vw,20px) 26px}header .eyebrow{color:#9fe1df;font-size:12px;letter-spacing:.15em;font-weight:700}h1{font-size:clamp(25px,4vw,38px);margin:5px 0 8px;letter-spacing:-.03em}header p{max-width:850px;margin:0;color:#d4e2e5}.summary{display:flex;gap:25px;flex-wrap:wrap;margin:18px 0 0;font-size:14px;color:#a9dcda}.toolbar{position:sticky;top:0;z-index:3;background:#f4f2ebf5;backdrop-filter:blur(15px);padding:16px max(5vw,20px);border-bottom:1px solid var(--line)}.searchrow{display:flex;gap:16px;align-items:center;margin-bottom:13px}input{width:min(420px,70%);border:1px solid #bdc9c9;border-radius:8px;background:white;padding:10px 13px;outline-offset:3px}#count{margin-left:auto;font-size:13px;color:var(--muted)}nav{display:flex;gap:7px;flex-wrap:wrap}nav button{background:transparent;color:var(--ink);border:1px solid #bdc9c9;padding:7px 13px;border-radius:100px;font-size:13px}nav button[aria-pressed=true]{background:var(--teal);color:white;border-color:var(--teal)}.intro{margin:22px 0;color:var(--muted);font-size:14px}main{padding:0 max(5vw,20px) 50px;max-width:2100px;margin:auto}#grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:20px}.card{overflow:hidden;border:1px solid var(--line);border-radius:13px;background:white;box-shadow:0 3px 12px #14212b04;display:flex;flex-direction:column}.picture{display:block;border:0;background:#e7e8e1;width:100%;height:270px;padding:12px}.picture img{height:100%;width:100%;object-fit:contain}.body{padding:16px;flex:1}.tag{display:inline-block;color:#0e6266;font-size:11px;line-height:1.3;font-weight:700;text-transform:uppercase;letter-spacing:.045em}.ref{color:#936623}.older{color:#8a6563}h2{font-size:16px;line-height:1.35;margin:7px 0}.body p{font-size:12px;margin:7px 0 0;color:var(--muted)}.meta{color:#879196;font-size:11px;margin-top:11px}.body a{display:inline-block;font-size:12px;color:var(--teal);margin-top:10px;text-decoration:none}.body a:hover{text-decoration:underline}button:focus-visible,a:focus-visible{outline:3px solid #d09a29;outline-offset:4px}#empty{display:none;padding:30px 0}footer{margin-top:35px;color:var(--muted);font-size:12px}footer a{color:var(--teal)}dialog{border:0;border-radius:12px;padding:0;width:96vw;max-width:1700px;max-height:96vh;background:#16232c;color:white}dialog::backdrop{background:#071218e8}.viewerbar{display:flex;align-items:center;gap:8px;padding:13px 16px;border-bottom:1px solid #3b4b52}.viewerbar strong{flex:1;font-size:15px}.viewerbar button,.viewerbar a{background:#334951;border:1px solid #4f656d;color:white;padding:7px 12px;border-radius:6px;text-decoration:none;font-size:13px}.stage{height:calc(90vh - 140px);overflow:auto;background:#0e1820;display:grid;place-items:center;padding:12px}.stage img{max-width:100%;max-height:100%;object-fit:contain;display:block}.stage.zoom{display:block}.stage.zoom img{max-width:none;max-height:none}.caption{padding:11px 16px;color:#c1d0d5;font-size:12px}.caption p{margin:0}#position{font-variant-numeric:tabular-nums;color:#b5cccd;font-size:12px;margin-right:8px}@media(max-width:600px){.viewerbar{flex-wrap:wrap}.viewerbar strong{flex-basis:70%}.picture{height:320px}.summary{gap:10px 20px}.stage{height:65vh}.toolbar{position:static}}
</style></head><body>
<header><div class="eyebrow">PROJECT VISUAL REVIEW · OFFLINE GALLERY</div><h1>Diamond Mine Tycoon</h1><p>Inspect environment and character assets, actual model renders, Unity captures and reference artwork. Click any image to enlarge it; use the arrow keys to browse.</p><div class="summary"><span id="total"></span><span>12 crew designs · separate modelling views</span><span>Source freshness and capture provenance labelled</span></div></header>
<div class="toolbar"><div class="searchrow"><input id="search" type="search" placeholder="Search: Tavi, terrain, craft, cart…" aria-label="Search images"><span id="count" aria-live="polite"></span></div><nav id="filters" aria-label="Image categories"></nav></div>
<main><p class="intro" id="intro"></p><div id="grid"></div><p id="empty">No matching images. Try another name or choose All images.</p><footer>Reference boards show the target appearance. Actual Blender renders and Unity captures remain different from those references; no 95% character acceptance or pixel-identical UI match is asserted here. “Current” requires matching model/image hashes or an explicit capture manifest. File dates alone do not prove freshness. Earlier captures remain available for comparison.<br><a href="manifest.json" target="_blank" rel="noopener">Image index, provenance and source paths</a><details><summary>Laptop play instructions</summary>Open the project in Unity 6000.3.26f1, stop Play mode, then choose Diamond Mine → Characters → Play crew with isolated save. Use the Crew preview window to select a character. The Game view lets you move, mine and send the cart. Stop Play to leave the separate test save. The image ZIP contains pictures only; the Unity project is needed to play.</details></footer></main>
<dialog id="viewer" aria-label="Enlarged project image"><div class="viewerbar"><strong id="viewtitle"></strong><span id="position"></span><button id="previous" aria-label="Previous image">←</button><button id="next" aria-label="Next image">→</button><button id="zoom">100% size</button><a id="original" target="_blank" rel="noopener">Open original</a><button id="close">Close ✕</button></div><div class="stage" id="stage"><img id="large" alt=""></div><div class="caption"><p id="viewnote"></p><p id="viewmeta"></p></div></dialog>
<script id="gallery-data" type="application/json">__DATA__</script>
<script>
'use strict';
const all=JSON.parse(document.getElementById('gallery-data').textContent);
const groups=['Highlights','All current visuals','Gameplay','UI review','Environment','Characters','Comparisons','References','UI assets','Textures','Earlier UI','Archive','All images'];
const descriptions={'Highlights':'Start with source-verified Tavi/Mira turnarounds and capture-manifest-verified Unity evidence where available. A current file is still development art, not an accepted likeness.','All current visuals':'Views whose model/image hashes match the saved source, plus captures verified by an explicit manifest. Blender renders and Unity game screenshots are labelled separately.','Gameplay':'Actual Main Play-mode captures. Check each record for revision provenance. Reference fixtures, live gameplay and earlier screenshots have different purposes.','UI review':'Actual Unity screenshots at fixed reference-crop aspect or the adaptive live portrait layout. Fixed review values are isolated fixtures; live values come from gameplay.','Environment':'Unity environment views, terrain, timber portals, cart and track. Capture records identify source freshness; existing historical views may predate the new six-context environment pass.','Characters':'Separate source, beauty, front, left, right, back, face, accessory, material and turnaround views where rendered. Native mesh renders and imported Unity camera renders are identified. Hidden surfaces are reconstructed.','Comparisons':'Labelled source-versus-model or source-versus-Unity comparisons, 50% overlays and image-difference diagnostics. These do not automatically establish visual acceptance.','References':'Original roster/gameplay copies, exact decoded JPEG crops and lower-authority auxiliary modelling sheets. These are target artwork, not screenshots of the current game.','UI assets':'Individual project icons, illustrations and surface assets used by UI. These are not complete game screenshots.','Textures':'Authored maps used by crew, timber bunker and six gameplay-environment material sets. These are asset images, not game screenshots.','Earlier UI':'Earlier works-mine interface QA images, retained as historical evidence.','Archive':'Earlier or unverified capture/model revisions. They may contain replaced characters or unfinished assets.','All images':'All indexed project screenshots, numbered model-review images, supplied references, UI assets and texture images, including historical versions.'};
let category='Highlights',visible=[],selected=0;
const $=id=>document.getElementById(id);
function matches(item,group){return group==='All images'||(group==='Highlights'?item.highlight:group==='All current visuals'?item.current:group==='Archive'?!item.current&&!['References','Textures','UI assets'].includes(item.group):item.group===group)}
function create(tag,className,text){const e=document.createElement(tag);if(className)e.className=className;if(text!==undefined)e.textContent=text;return e}
function render(){
  const q=$('search').value.toLowerCase().trim();
  visible=all.filter(i=>matches(i,category)&&(!q||[i.title,i.group,i.kind,i.file].join(' ').toLowerCase().includes(q)));
  $('count').textContent=visible.length+' of '+all.length+' images';$('intro').textContent=descriptions[category];$('grid').replaceChildren();$('empty').style.display=visible.length?'none':'block';
  for(const [index,item] of visible.entries()){
    const article=create('article','card'),button=create('button','picture');button.setAttribute('aria-label','Enlarge '+item.title);
    const img=create('img');img.src=item.thumb;img.alt=item.title;img.loading='lazy';button.append(img);button.onclick=()=>show(index);
    const body=create('div','body');body.append(create('span','tag '+(item.group==='References'?'ref':!item.current&&item.group!=='Textures'?'older':''),item.kind),create('h2','',item.title),create('p','',item.note),create('div','meta',item.width+' × '+item.height+' · File updated '+item.updated));
    const link=create('a','','Open full-resolution file ↗');link.href=item.src;link.target='_blank';link.rel='noopener';body.append(link);article.append(button,body);$('grid').append(article);
  }
  for(const button of $('filters').children)button.setAttribute('aria-pressed',String(button.dataset.category===category));
}
function show(index){selected=(index+visible.length)%visible.length;const item=visible[selected];$('viewtitle').textContent=item.title;$('position').textContent=(selected+1)+' / '+visible.length;$('large').src=item.src;$('large').alt=item.title;$('original').href=item.src;$('viewnote').textContent=item.kind+' · '+item.note+' '+item.provenance;$('viewmeta').textContent=item.width+' × '+item.height+' · '+item.file;$('stage').classList.remove('zoom');$('zoom').textContent='100% size';if(!$('viewer').open)$('viewer').showModal()}
for(const group of groups){const n=all.filter(i=>matches(i,group)).length;const b=create('button','',group+' · '+n);b.dataset.category=group;b.onclick=()=>{category=group;render()};$('filters').append(b)}
$('total').textContent=all.length+' images available';$('search').addEventListener('input',render);$('close').onclick=()=>$('viewer').close();$('previous').onclick=()=>show(selected-1);$('next').onclick=()=>show(selected+1);$('zoom').onclick=()=>{$('stage').classList.toggle('zoom');$('zoom').textContent=$('stage').classList.contains('zoom')?'Fit image':'100% size'};
$('viewer').addEventListener('keydown',e=>{if(e.key==='ArrowRight'){e.preventDefault();show(selected+1)}if(e.key==='ArrowLeft'){e.preventDefault();show(selected-1)}});render();
</script></body></html>'''
(OUT/'index.html').write_text(template.replace('__DATA__',data),encoding='utf-8')
manifest={'created':datetime.now(timezone.utc).isoformat(),'image_count':len(items),
          'current_definition':'Exact current model/source and output hashes for model views and derived UI assets, or exact image hash in an explicit capture manifest. File timestamps alone are insufficient. No automatic visual acceptance.',
          'capture_manifest_schema':{'images':[{'file':'release/screenshots/path/image.png','sha256':'SHA-256 of completed capture','kind':'Optional capture label','note':'Optional factual context','group':'Optional gallery category'}]},
          'images':[{k:v for k,v in item.items() if k!='thumb'} for item in items]}
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
launcher='<!doctype html><html lang="en"><meta charset="utf-8"><meta http-equiv="refresh" content="0;url=release/visual-review/index.html"><title>Diamond Mine Tycoon Visual Gallery</title><a href="release/visual-review/index.html">Open all project images</a></html>'
(ROOT/'VIEW_ALL_IMAGES.html').write_text(launcher,encoding='utf-8')
(OUT/'README.txt').write_text('Open VIEW_ALL_IMAGES.html in the project root, or this folder\'s index.html.\nThe gallery is offline. Click an image to enlarge; use arrows, 100% size and Open original.\nReference artwork and older captures are explicitly labelled.\nFor the ZIP: extract all files first, then open VIEW_ALL_IMAGES.html.\n',encoding='utf-8')
archive=ROOT/'release/artwork/DiamondMineTycoon-All-Images.zip'
if not args.skip_archive:
    archive.parent.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
        for p in (OUT/'index.html',OUT/'manifest.json',OUT/'README.txt',ROOT/'VIEW_ALL_IMAGES.html'):
            z.write(p,p.relative_to(ROOT).as_posix())
        for item in items:
            p=ROOT/item['file'];assert p.is_file();z.write(p,item['file'])
    with zipfile.ZipFile(archive) as z:assert z.testzip() is None
print(json.dumps({'gallery':str(OUT/'index.html'),'images':len(items),'current_images':sum(i['current'] for i in items),
    'groups':{g:sum(i['group']==g for i in items) for g in sorted(set(i['group'] for i in items))},
    'archive_updated':not args.skip_archive,'archive':str(archive) if not args.skip_archive else None,
    'archive_bytes':archive.stat().st_size if not args.skip_archive else None},indent=2))
