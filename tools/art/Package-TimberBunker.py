"""Verify and package the reviewed art files for the existing Unity project."""
from pathlib import Path
import hashlib, json, re, zipfile
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'release/artwork'
EVIDENCE=ROOT/'release/screenshots/timber-bunker'
OUT.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
checks=(EVIDENCE/'checks.txt').read_text().splitlines()
errors=(EVIDENCE/'review-errors.txt').read_text().strip()
assert len(checks)>=20 and all(x.startswith('PASS:') for x in checks) and not errors, 'Play review incomplete'
for name in ('play-mode-main.png','play-mode-mine-interaction.png','play-mode-cart-moving.png','unity-closeup.png','reference-vs-unity.png'):
    assert (EVIDENCE/name).stat().st_size>10000, name

files=set()
for folder in ('Assets/_Project/Art/TimberBunker','Assets/_Project/Resources/Yard','ArtSource/TimberBunker','release/screenshots/timber-bunker'):
    for path in (ROOT/folder).rglob('*'):
        if path.is_file() and path.suffix not in ('.blend1','.blend2'):files.add(path)
for relative in (
    'Assets/_Project/Art/TimberBunker.meta','Assets/_Project/Resources/Yard.meta',
    'Assets/_Project/Art/Shaders/TimberSurface.shader',
    'Assets/_Project/Scripts/Runtime/TimberBunkerArea.cs','Assets/_Project/Scripts/Runtime/YardArt.cs',
    'Assets/_Project/Scripts/Runtime/YardWorld.cs','Assets/_Project/Scripts/Runtime/MinePresenter.cs',
    'Assets/_Project/Editor/TimberAssetPipeline.cs','Assets/_Project/Editor/TimberVisualReview.cs',
    'Assets/_Project/Editor/TimberManualPreview.cs',
    'tools/art/Create-TimberBunker.py','tools/art/Package-TimberBunker.py','tools/Run-TimberReview.ps1','Docs/Timber-Bunker.md'):
    path=ROOT/relative
    assert path.is_file(),relative
    files.add(path)
    if relative.startswith('Assets/') and path.suffix!='.meta':
        assert Path(str(path)+'.meta').is_file(),str(path)+'.meta'
        files.add(Path(str(path)+'.meta'))

guids={}
for meta in (ROOT/'Assets').rglob('*.meta'):
    found=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(errors='replace'),re.M)
    if found:guids[found.group(1)]=meta
for path in files:
    if path.suffix not in ('.mat','.prefab','.asset'):continue
    for guid in re.findall(r'guid: ([0-9a-f]{32})',path.read_text(errors='replace')):
        if guid.startswith('0000000000000000'):continue
        assert guid in guids,f'Unresolved Unity reference {guid}: {path}'
for path in files:
    if not path.is_relative_to(ROOT/'Assets'):continue
    reviewed=ROOT/'.cache/timber-workspace'/path.relative_to(ROOT)
    assert reviewed.is_file() and sha(reviewed)==sha(path),f'Root differs from reviewed input: {path}'

ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
trx=ET.parse(ROOT/'tests/results/simulation.trx').getroot()
counts=trx.find('t:ResultSummary/t:Counters',ns).attrib
assert counts['failed']=='0' and int(counts['passed'])>=85
manifest={
    'scope':'One authored timber bunker, terrain, track and cart in the existing Diamond Mine Tycoon project',
    'blender':json.loads((ROOT/'ArtSource/TimberBunker/mesh-audit.json').read_text()),
    'unity':'6000.3.26f1','main_play_mode_checks_passed':len(checks),'main_play_mode_errors':0,
    'simulation_tests':counts,'asset_references':'resolved against root Assets metadata',
    'root_assets_match_review_workspace':True,
    'limitations':['Other areas/characters/screens retain their existing presentation.',
        'Editor UI input is synthetic; no physical Android/iOS results.',
        'No new APK or iOS export was produced for this art pass.'],
    'files':{p.relative_to(ROOT).as_posix():{'bytes':p.stat().st_size,'sha256':sha(p)} for p in sorted(files)}
}
manifest_path=OUT/'timber-bunker-manifest.json'
manifest_path.write_text(json.dumps(manifest,indent=2))
archive=OUT/'TimberBunker-Source-and-Unity.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for path in sorted(files):z.write(path,path.relative_to(ROOT).as_posix())
    z.write(manifest_path,'timber-bunker-manifest.json')
with zipfile.ZipFile(archive) as z:assert z.testzip() is None
Path(str(archive)+'.sha256').write_text(sha(archive)+'  '+archive.name+'\n')
print(json.dumps({'archive':str(archive),'bytes':archive.stat().st_size,'sha256':sha(archive),
    'files':len(files),'play_checks':len(checks),'simulation_passed':counts['passed']},indent=2))
