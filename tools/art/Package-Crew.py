"""Package the reviewed reconstruction assets; fail on stale input or broken GUIDs."""
from pathlib import Path
import hashlib,json,re,zipfile
import xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'release/artwork';OUT.mkdir(parents=True,exist_ok=True)
EV=ROOT/'release/screenshots/characters';REVIEW=ROOT/'.cache/timber-workspace'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
checks=(EV/'checks.txt').read_text().splitlines()
assert len(checks)>=242 and all(c.startswith('PASS:') for c in checks)
assert not (EV/'review-errors.txt').read_text().strip(),'Play review reported errors'
designs=json.loads((ROOT/'tools/art/character-designs.json').read_text())
for identifier in designs:
    for relative in ('ArtSource/Characters/'+identifier+'.blend',
                     'Assets/_Project/Art/Crew/Models/'+identifier+'.fbx',
                     'Assets/_Project/Resources/Characters/'+identifier+'.prefab',
                     'release/screenshots/characters/'+identifier+'-unity.png',
                     'release/screenshots/characters/'+identifier+'-face-unity.png'):
        assert (ROOT/relative).is_file(),relative
for name in ('play-mode-character.png','play-mode-character-front.png','play-mode-character-mining.png',
             'play-mode-character-cart.png','crew-unity-lineup.png','reference-vs-unity-crew.png','tavi-reference-vs-unity.png'):
    assert (EV/name).stat().st_size>10000,name
files=set()
for relative in ('ArtSource/Characters','Assets/_Project/Art/Crew','Assets/_Project/Resources/Characters','release/screenshots/characters'):
    for p in (ROOT/relative).rglob('*'):
        if p.is_file() and p.suffix not in ('.blend1','.blend2') and not p.name.endswith('-blender.png'):files.add(p)
for relative in ('Assets/_Project/Art/Crew.meta','Assets/_Project/Resources/Characters.meta',
    'Assets/_Project/Art/Shaders/TimberSurface.shader','Assets/_Project/Scripts/Runtime/CrewActor.cs',
    'Assets/_Project/Scripts/Runtime/YardArt.cs','Assets/_Project/Scripts/Runtime/YardWorld.cs',
    'Assets/_Project/Scripts/Runtime/YardControls.cs','Assets/_Project/Editor/CrewAssetPipeline.cs',
    'Assets/_Project/Editor/CrewVisualReview.cs','Assets/_Project/Editor/CrewManualPreview.cs',
    'Assets/_Project/Editor/TimberManualPreview.cs','Assets/_Project/Resources/Ui/crew-lineup.jpg',
    'Packages/manifest.json','Packages/packages-lock.json','Docs/characters/README.md',
    'tools/art/Create-Crew.py','tools/art/character-designs.json','tools/art/Pack-CrewSource.py',
    'tools/art/Compare-Crew.py','tools/art/Package-Crew.py','tools/Run-CrewReview.ps1'):
    p=ROOT/relative;assert p.is_file(),relative;files.add(p)
    if relative.startswith('Assets/') and p.suffix!='.meta':
        meta=Path(str(p)+'.meta');assert meta.is_file(),str(meta);files.add(meta)
guids={}
for meta in (ROOT/'Assets').rglob('*.meta'):
    found=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(errors='replace'),re.M)
    if found:
        assert found.group(1) not in guids,'Duplicate GUID: '+str(meta)
        guids[found.group(1)]=meta
for p in files:
    if p.suffix in ('.mat','.prefab','.controller','.asset'):
        for guid in re.findall(r'guid: ([0-9a-f]{32})',p.read_text(errors='replace')):
            if not guid.startswith('0000000000000000'):assert guid in guids,f'Unresolved GUID {guid}: {p}'
    if p.is_relative_to(ROOT/'Assets') or p.is_relative_to(ROOT/'Packages'):
        reviewed=REVIEW/p.relative_to(ROOT)
        assert reviewed.is_file() and sha(p)==sha(reviewed),f'Root differs from reviewed asset: {p}'
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
counts=ET.parse(ROOT/'tests/results/simulation.trx').getroot().find('t:ResultSummary/t:Counters',ns).attrib
assert counts['failed']=='0' and int(counts['passed'])>=85
manifest={'scope':'Twelve reference-directed 3D crew reconstructions for the existing Diamond Mine Tycoon project',
    'visual_status':'Requested 100% illustration match NOT achieved; faces, hair, garments, proportions and posing still differ.',
    'unity':'6000.3.26f1','blender':'4.5.9 LTS','play_checks':len(checks),'play_errors':0,'simulation':counts,
    'root_assets_match_review_workspace':True,'unity_guid_references':'resolved',
    'model_audit':json.loads((ROOT/'ArtSource/Characters/mesh-audit.json').read_text()),
    'limits':['Single supplied image; unseen views reconstructed.','No facial or individual-finger animation.',
              'No mobile LOD/material atlas or physical-device performance validation.',
              'Existing APK predates this work. No new mobile build or store submission.'],
    'files':{p.relative_to(ROOT).as_posix():{'bytes':p.stat().st_size,'sha256':sha(p)} for p in sorted(files)}}
mp=OUT/'crew-reconstruction-manifest.json';mp.write_text(json.dumps(manifest,indent=2))
archive=OUT/'Crew-Reconstructions-Source-and-Unity.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for p in sorted(files):z.write(p,p.relative_to(ROOT).as_posix())
    z.write(mp,mp.name)
with zipfile.ZipFile(archive) as z:assert z.testzip() is None
Path(str(archive)+'.sha256').write_text(sha(archive)+'  '+archive.name+'\n')
print(json.dumps({'archive':str(archive),'bytes':archive.stat().st_size,'sha256':sha(archive),
                  'files':len(files),'play_checks':len(checks),'simulation_passed':counts['passed']},indent=2))
