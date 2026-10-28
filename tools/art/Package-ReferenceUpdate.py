"""Package the reference-directed update after checking current evidence.

This is an update for the existing project, not a claim of visual acceptance.
Use --validate-only to inspect requirements without producing/replacing a ZIP.
No missing/stale evidence, failed state checks or unresolved GUIDs are waived.
"""
from pathlib import Path
from datetime import datetime,timezone
import argparse,hashlib,json,re,sys,zipfile
import xml.etree.ElementTree as ET
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--qa-workspace',type=Path,default=ROOT/'.cache/timber-workspace')
parser.add_argument('--crew-manifest',type=Path,default=ROOT/'release/screenshots/characters/evidence-manifest.json')
parser.add_argument('--state-report',type=Path,default=ROOT/'release/screenshots/characters/state-review.json')
parser.add_argument('--ui-manifest',type=Path,default=ROOT/'release/screenshots/ui-reference/capture-manifest.json')
parser.add_argument('--validate-only',action='store_true')
args=parser.parse_args()
QA=args.qa_workspace.resolve()
for name in ('crew_manifest','state_report','ui_manifest'):
    path=getattr(args,name)
    setattr(args,name,(path if path.is_absolute() else ROOT/path).resolve())
OUT=ROOT/'release/artwork'
STATES={'Idle','Walk','Run','Mine','Collect','Carry','Wave','Celebrate','Machine','Craft'}
SCREENS={'explore','mine','factory','craft','shop','map','tasks','rank'}
errors=[];files=set();validated_manifests=[]

def sha(path):
    digest=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''):digest.update(block)
    return digest.hexdigest()

def fail(message):errors.append(message)
def read_json(path):
    try:return json.loads(path.read_text(encoding='utf-8-sig'))
    except (OSError,ValueError) as ex:fail(str(path)+': '+str(ex));return {}

def local(relative):
    """Evidence manifests may name only files in this project."""
    relative=str(relative).replace('\\','/')
    path=(ROOT/relative).resolve()
    if not path.is_relative_to(ROOT):
        fail('Evidence path escapes project: '+relative);return None
    return path

def add(path,required=True):
    path=Path(path)
    if not path.is_absolute():path=ROOT/path
    if not path.is_file():
        if required:fail('Required file missing: '+str(path))
        return
    if not path.resolve().is_relative_to(ROOT):fail('Package input escapes project: '+str(path));return
    files.add(path)

def add_tree(relative):
    path=ROOT/relative
    if not path.is_dir():fail('Required folder missing: '+relative);return
    for item in path.rglob('*'):
        if not item.is_file():continue
        if any(part in ('__pycache__','_Recovery','bin','obj') for part in item.parts):continue
        if item.suffix.lower() in ('.blend1','.blend2','.pyc'):continue
        files.add(item)

def verify_record(record,label,compare_qa=False):
    path=local(record.get('file',''))
    if path is None:return None
    if not path.is_file():fail(label+' file missing: '+str(path));return None
    expected=str(record.get('sha256','')).lower()
    if len(expected)!=64 or sha(path)!=expected:fail(label+' hash mismatch: '+str(path))
    if compare_qa:
        other=QA/path.relative_to(ROOT)
        if not other.is_file() or sha(other)!=sha(path):fail(label+' root differs from QA workspace: '+str(path))
    add(path)
    return path

designs=read_json(ROOT/'tools/art/character-designs.json')
audit=read_json(ROOT/'ArtSource/Characters/mesh-audit.json')
view_results={}
for identifier,design in designs.items():
    folder=ROOT/'ArtSource/Characters'/design['name']
    manifest=read_json(folder/'model-views.json');source=ROOT/'ArtSource/Characters'/(identifier+'.blend')
    if not source.is_file() or manifest.get('model_source_sha256')!=sha(source):fail(identifier+': rendered views are stale for current .blend')
    if manifest.get('renderer_sha256')!=sha(ROOT/'tools/art/Render-CharacterViews.py'):fail(identifier+': renderer revision mismatch')
    if manifest.get('assembler_sha256')!=sha(ROOT/'tools/art/Assemble-CharacterViews.py'):fail(identifier+': assembler revision mismatch')
    reference=read_json(folder/'reference.json')
    source_crop=folder/'01_SourceCrop.png'
    if not source_crop.is_file() or reference.get('source_crop_sha256')!=sha(source_crop):fail(identifier+': source crop changed')
    names=['SourceCrop','Beauty','Front','Left','Right','Back','Face','Accessories','Materials','Turnaround','ReferenceVsModel']
    for index,name in enumerate(names,1):
        file=f'{index:02d}_{name}.png';path=folder/file
        expected=manifest.get('outputs',{}).get(file,{})
        if not path.is_file():fail(identifier+': numbered view missing '+file);continue
        with Image.open(path) as image:dimensions=list(image.size);image.verify()
        if expected.get('sha256')!=sha(path) or expected.get('dimensions')!=dimensions:fail(identifier+': image/hash/dimensions mismatch '+file)
    raw=manifest.get('raw_render_outputs',{})
    if len(raw)<8:fail(identifier+': raw render provenance incomplete')
    for file,entry in raw.items():verify_record({'file':(folder/file).relative_to(ROOT).as_posix(),'sha256':entry.get('sha256')},identifier+' raw render')
    model=audit.get(identifier,{})
    if model.get('bones')!=47 or model.get('skinned_meshes')!=4 or set(model.get('clips',[]))!=STATES:fail(identifier+': exporter audit does not report 47 bones/four groups/ten required clips')
    for relative in ('ArtSource/Characters/'+identifier+'.blend','Assets/_Project/Art/Crew/Models/'+identifier+'.fbx','Assets/_Project/Resources/Characters/'+identifier+'.prefab'):add(relative)
    view_results[identifier]={'source_sha256':manifest.get('model_source_sha256'),'numbered_views':11,'exporter_report_not_independent_unity_validation':model}

crew=ROOT/'release/screenshots/characters'
try:
    checks=(crew/'checks.txt').read_text(encoding='utf-8-sig').splitlines()
    if not checks or any(not row.startswith('PASS:') for row in checks if row.strip()):fail('Crew checks contain a failure or no checks')
    for identifier in designs:
        for state in STATES:
            if 'PASS: '+identifier+' animator state '+state not in checks:fail(identifier+': missing actual Play animator-state check '+state)
    if (crew/'review-errors.txt').read_text(encoding='utf-8-sig').strip():fail('Crew Main Play review reports errors')
except OSError as ex:fail('Crew review incomplete: '+str(ex));checks=[]

def capture_manifest(path,label):
    manifest=read_json(path);add(path)
    images=manifest.get('images',[]);inputs=manifest.get('inputs',[])
    if not images or not inputs:fail(label+': capture manifest needs images and source inputs')
    for record in images:verify_record(record,label+' image')
    for record in inputs:verify_record(record,label+' input',compare_qa=str(record.get('file','')).replace('\\','/').startswith(('Assets/','Packages/','ProjectSettings/')))
    if 'qa-saves' not in str(manifest.get('saveRoot','')):fail(label+': isolated QA save provenance missing')
    if 'Main.unity' not in str(manifest.get('scene','')):fail(label+': Main scene provenance missing')
    paths={str(row.get('file','')).replace('\\','/') for row in inputs}
    for identifier in designs:
        if 'Assets/_Project/Art/Crew/Models/'+identifier+'.fbx' not in paths:fail(label+': captured character model hash missing '+identifier)
    for context in ('Explore','Mine','Factory','Workshop','Boutique','RegionMap'):
        if 'Assets/_Project/Art/GameplayEnvironments/Models/'+context+'.fbx' not in paths:fail(label+': captured environment model hash missing '+context)
    validated_manifests.append({'file':path.relative_to(ROOT).as_posix(),'sha256':sha(path) if path.is_file() else None,
                                'images':len(images),'inputs':len(inputs),'utc':manifest.get('utc'),'unity':manifest.get('unity')})
    return manifest

crew_manifest=capture_manifest(args.crew_manifest,'Crew')
ui_manifest=capture_manifest(args.ui_manifest,'UI')
env_audit=read_json(ROOT/'ArtSource/GameplayEnvironments/mesh-audit.json')
for context in ('Explore','Mine','Factory','Workshop','Boutique','RegionMap'):
    add('ArtSource/GameplayEnvironments/'+context+'.blend')
    add('Assets/_Project/Art/GameplayEnvironments/Models/'+context+'.fbx')
    entry=env_audit.get(context,{})
    if not entry.get('meshes') or entry.get('uv_meshes')!=entry.get('meshes'):fail(context+': environment exporter audit lacks UV coverage')
add('Assets/_Project/Resources/Environments/GameplayEnvironmentSet.prefab')
icon_manifest=read_json(ROOT/'ArtSource/UI/Icons/render-manifest.json')
if len(icon_manifest.get('icons',[]))<11:fail('Rendered UI prop icon manifest is incomplete')
if icon_manifest.get('script_sha256')!=sha(ROOT/'tools/art/Render-UiIcons.py'):fail('Rendered UI icon tool revision mismatch')
for icon in icon_manifest.get('icons',[]):
    verify_record(icon,'UI icon')
    source=local(icon.get('source',''))
    if source is None or not source.is_file() or icon.get('blend_sha256')!=sha(source):fail('UI icon source hash mismatch '+str(icon.get('id')))
    if icon.get('mesh_count')!=icon.get('uv_mesh_count'):fail('UI icon UV coverage incomplete '+str(icon.get('id')))
portrait_manifest=read_json(ROOT/'ArtSource/UI/crew-portrait-manifest.json')
if {row.get('id') for row in portrait_manifest.get('portraits',[])}!=set(designs):fail('Actual model-derived portrait manifest must cover twelve characters')
if portrait_manifest.get('script_sha256')!=sha(ROOT/'tools/art/Build-CrewPortraits.py'):fail('UI portrait tool revision mismatch')
for row in portrait_manifest.get('portraits',[]):
    verify_record({'file':row.get('source',''),'sha256':row.get('source_sha256')},'Actual model portrait source')
    for record in row.get('outputs',[]):verify_record(record,'UI portrait output')
crew_paths={str(row.get('file','')).replace('\\','/') for row in crew_manifest.get('images',[])}
for identifier in designs:
    for suffix in ('-unity.png','-face-unity.png'):
        if 'release/screenshots/characters/'+identifier+suffix not in crew_paths:fail('Final Unity crew capture is not in manifest: '+identifier+suffix)
for name in ('play-mode-character.png','play-mode-character-front.png','play-mode-character-mining.png','play-mode-character-cart.png','crew-unity-lineup.png','reference-vs-unity-crew.png','tavi-reference-vs-unity.png'):
    if 'release/screenshots/characters/'+name not in crew_paths:fail('Final crew comparison/gameplay capture not in manifest: '+name)
for identifier in designs:
    name='release/screenshots/characters/'+identifier+'-reference-vs-unity.png'
    if name not in crew_paths:fail('Individual source-versus-Unity comparison not in manifest: '+name)
contact='release/screenshots/characters/crew-reference-vs-unity-contact-sheet.png'
if contact not in crew_paths:fail('Crew comparison contact sheet is not in manifest')
context_path=crew/'comparison-context.json';comparison=read_json(context_path);add(context_path)
if not context_path.is_file() or crew_manifest.get('comparison_context_sha256')!=sha(context_path):
    fail('Crew comparison context hash is not pinned in capture manifest')
for record in comparison.get('inputs',[]):verify_record(record,'Crew comparison input')
for record in comparison.get('outputs',[]):verify_record(record,'Crew comparison output')

state_report=read_json(args.state_report);add(args.state_report)
state_rows=state_report.get('states',[]);state_pairs=set()
for row in state_rows:
    identifier=row.get('character_id',row.get('character',''));state=row.get('state','')
    pair=(identifier,state)
    if pair in state_pairs:fail('Duplicate per-state evidence '+str(pair))
    state_pairs.add(pair)
    if row.get('passed') is not True:fail('Failed per-state review '+str(pair))
    if not row.get('outputs'):fail('Per-state output-hash evidence missing '+str(pair))
    for record in row.get('outputs',[]):
        verify_record(record,'State '+str(pair))
        if str(record.get('file','')).replace('\\','/') not in crew_paths:fail('State output absent from crew capture manifest '+str(pair))
expected_pairs={(identifier,state) for identifier in designs for state in STATES}
if state_pairs!=expected_pairs:fail('Per-state evidence must cover exactly twelve characters × ten required states; missing '+str(sorted(expected_pairs-state_pairs)))

ui=ROOT/'release/screenshots/ui-reference'
try:
    if (ui/'errors.txt').read_text(encoding='utf-8-sig').strip():fail('UI Main Play review reports errors')
    ui_checks=(ui/'checks.txt').read_text(encoding='utf-8-sig').splitlines()
    if len(ui_checks)<9:fail('UI Play checks incomplete')
except OSError as ex:fail('UI review incomplete: '+str(ex));ui_checks=[]
ui_paths={str(row.get('file','')).replace('\\','/') for row in ui_manifest.get('images',[])}
summary=read_json(ui/'comparison-summary.json');summary_rows={row.get('screen'):row for row in summary.get('screens',[])}
if set(summary_rows)!=SCREENS:fail('UI comparison summary must cover all eight screens')
for screen in SCREENS:
    source=ROOT/'Docs/UI-Reference'/screen/'Source.png';actual=ui/'reference'/(screen+'.png')
    for mode in ('reference','live'):
        capture=ui/mode/(screen+'.png');relative=capture.relative_to(ROOT).as_posix()
        if relative not in ui_paths:fail('UI capture is not manifest-verified: '+relative)
        bounds=read_json(ui/mode/(screen+'-bounds.json'));add(ui/mode/(screen+'-bounds.json'));add(ui/mode/(screen+'.txt'))
        if capture.is_file():
            with Image.open(capture) as image:dimensions=list(image.size)
            if dimensions!=[bounds.get('viewportWidth'),bounds.get('viewportHeight')]:fail(screen+' '+mode+': capture/bounds viewport mismatch')
            if mode=='live' and dimensions!=[720,1280]:fail(screen+': live capture is not the recorded 720x1280 adaptive portrait viewport')
        if mode=='reference':
            if not bounds.get('referenceReview') or not bounds.get('elements'):fail(screen+': reference bounds report incomplete')
            if any(not row.get('present') or not row.get('withinTarget') or max(row.get('error') or [1])>.005001 for row in bounds.get('elements',[])):fail(screen+': rectangle implementation regression')
            if source.is_file():
                with Image.open(source) as image:source_size=list(image.size)
                if source_size!=[bounds.get('sourceWidth'),bounds.get('sourceHeight')]:fail(screen+': bounds report source crop dimensions changed')
                if capture.is_file() and dimensions!=[value*4 for value in source_size]:fail(screen+': reference screenshot is not native source-crop aspect at 4x')
            board=ROOT/'Docs/design-boards/gameplay-board.jpg'
            if not board.is_file() or bounds.get('sourceHash')!=sha(board):fail(screen+': source-board hash differs from measured bounds report')
    row=summary_rows.get(screen,{})
    if not row.get('available') or not row.get('within_recorded_bounds_target'):fail(screen+': comparison data missing/rectangle regression')
    if not source.is_file() or row.get('source_sha256')!=sha(source):fail(screen+': source crop/summary hash mismatch')
    if not actual.is_file() or row.get('actual_sha256')!=sha(actual):fail(screen+': actual capture/summary hash mismatch')
    for suffix in ('side-by-side','overlay50','difference','bounds-overlay'):
        relative='release/screenshots/ui-reference/comparisons/'+screen+'-'+suffix+'.png'
        if relative not in ui_paths:fail('UI diagnostic composite is not in manifest: '+relative)

# Keep a complete, reviewable patch of project-owned assets. Imported package code
# and editor caches remain dependencies named by Packages, not copied software.
for relative in ('Assets/_Project','ArtSource/Characters','ArtSource/GameplayEnvironments','ArtSource/TimberBunker','ArtSource/Fonts','ArtSource/UI',
                 'Packages','ProjectSettings','Docs/characters','Docs/UI-Reference','Docs/design-boards','tests/DiamondMine.Simulation.Tests'):
    add_tree(relative)
add('Assets/_Project.meta',required=False)
for relative in ('README.md','PROGRESS.md','Docs/PROJECT-STATUS.md','Docs/UI-MEASUREMENTS.md','Docs/Timber-Bunker.md',
                 'RUN_FULL_GAME_TEST.cmd','BUILD_ANDROID_TEST.cmd','PLAY_GAME.cmd','TEST_GAME.cmd',
                 'tools/Resume-ReferenceValidation.ps1','tools/Inspect-Build.py',
                 'Docs/Gameplay-Environments.md','Docs/Licenses.md','Docs/TESTING.md','Docs/Build.md',
                 'tests/results/simulation.trx','tests/results/verification-summary.json',
                 'tools/Run-ReferenceReview.ps1','tools/Run-ReferenceUiReview.ps1','tools/Run-CrewReview.ps1',
                 'tools/Run-TimberReview.ps1','tools/Run-GalleryCapture.ps1','tools/Run-SimulationTests.ps1',
                 'tools/Build-Player.ps1','tools/Compile-UnitySource.ps1','tools/Record-Verification.py'):
    add(relative)
tool_names=['Create-Crew.py','CrewRefinements.py','CrewRoleRefinements.py','CrewFaceRefinements.py','character-designs.json',
            'Create-TimberBunker.py','Create-GameplayEnvironments.py','Prepare-CharacterReferences.py','Render-CharacterViews.py',
            'Assemble-CharacterViews.py','Verify-CharacterViews.py','Compare-Crew.py','Compare-ReferenceUi.py',
            'Measure-ReferenceUi.py','Sync-ReferenceUiLayout.py','Generate-UiSprites.py','Acquire-Nunito.py',
            'Render-UiIcons.py','Build-CrewPortraits.py',
            'Build-VisualGallery.py','VisualGalleryCapture.cs','Package-ReferenceUpdate.py']
for name in tool_names:add('tools/art/'+name)
for folder in (crew,ui):
    for path in folder.glob('*.txt'):add(path)
    for path in folder.glob('*context.json'):add(path)
add(ui/'comparison-summary.json')
for relative in ('release/reference-build-results.json','Docs/REFERENCE-UPDATE.md','Docs/Reference-Update.md'):
    add(relative,required=False)

# Assets being delivered must equal those in the editor workspace used for the
# captures. Detect a runtime, material or prefab edit made after evidence capture.
qa_mismatches=[]
captured_inputs={str(row.get('file','')).replace('\\','/'):str(row.get('sha256','')).lower()
                 for manifest in (crew_manifest,ui_manifest) for row in manifest.get('inputs',[])}
uncaptured_assets=[]
for path in sorted(files):
    relative=path.relative_to(ROOT)
    if relative.parts[0] not in ('Assets','Packages','ProjectSettings'):continue
    if captured_inputs.get(relative.as_posix())!=sha(path):uncaptured_assets.append(relative.as_posix())
    other=QA/relative
    if not other.is_file() or sha(path)!=sha(other):qa_mismatches.append(relative.as_posix())
if qa_mismatches:fail('Root/QA assets differ: '+', '.join(qa_mismatches[:30])+(' …' if len(qa_mismatches)>30 else ''))
if uncaptured_assets:fail('Delivered assets/config lack matching capture-time input hashes: '+', '.join(uncaptured_assets[:30])+(' …' if len(uncaptured_assets)>30 else ''))

# Resolve every serialized project GUID, not merely references in the selected
# art folders. Package GUIDs may resolve through the installed package cache.
guid_re=re.compile(r'^guid:\s*([0-9a-f]{32})\s*$',re.M)
reference_re=re.compile(r'\bguid:\s*([0-9a-f]{32})\b')
guids={};duplicates=[]
for base in (ROOT/'Assets',ROOT/'Packages'):
    for meta in base.rglob('*.meta'):
        found=guid_re.search(meta.read_text(encoding='utf-8',errors='replace'))
        if not found:continue
        guid=found.group(1)
        if guid in guids and guids[guid]!=meta:duplicates.append([str(guids[guid]),str(meta)])
        guids[guid]=meta
for base in (ROOT/'Library/PackageCache',QA/'Library/PackageCache'):
    if not base.is_dir():continue
    for meta in base.rglob('*.meta'):
        found=guid_re.search(meta.read_text(encoding='utf-8',errors='replace'))
        if found:guids.setdefault(found.group(1),meta)
unresolved=[]
extensions={'.meta','.unity','.prefab','.mat','.controller','.overridecontroller','.asset','.anim','.mask','.playable','.rendertexture','.terrainlayer'}
for base in (ROOT/'Assets',ROOT/'ProjectSettings'):
    for path in base.rglob('*'):
        if not path.is_file() or path.suffix.lower() not in extensions:continue
        raw=path.read_bytes()
        if b'\x00' in raw[:2048]:continue
        for guid in set(reference_re.findall(raw.decode('utf-8',errors='replace'))):
            if not guid.startswith('0000000000000000') and guid not in guids:unresolved.append({'file':path.relative_to(ROOT).as_posix(),'guid':guid})
if duplicates:fail('Duplicate project/package GUIDs: '+str(duplicates[:10]))
if unresolved:fail('Unresolved global project GUIDs: '+str(unresolved[:20]))

simulation={}
try:
    ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    simulation=ET.parse(ROOT/'tests/results/simulation.trx').getroot().find('t:ResultSummary/t:Counters',ns).attrib
    if int(simulation.get('passed','0'))<85 or any(int(simulation.get(key,'0')) for key in ('failed','error','aborted','timeout')):fail('Simulation TRX did not pass expected 85 tests')
    verification=read_json(ROOT/'tests/results/verification-summary.json')
    if verification.get('trx_sha256')!=sha(ROOT/'tests/results/simulation.trx'):fail('Simulation verification summary uses a different TRX')
    simulation_sources=list((ROOT/'Assets/_Project/Scripts/Simulation').rglob('*.cs'))+[
        ROOT/'Assets/_Project/Scripts/Runtime/DevRewardedAds.cs',ROOT/'Assets/_Project/Resources/economy.json']
    for path in simulation_sources:
        if verification.get('source_sha256',{}).get(path.relative_to(ROOT).as_posix())!=sha(path):fail('Simulation verification source hash is stale: '+str(path))
except (OSError,ET.ParseError,AttributeError,ValueError) as ex:fail('Simulation evidence invalid: '+str(ex))

report={'scope':'Reference-directed update for the existing Diamond Mine Tycoon Unity project; source, exports, materials, prefabs and exact-hash evidence.',
        'visual_status':'Character95/100 target and exact visual reference match are not certified. Side/back geometry is reconstructed. UI rectangle checks are against manually measured JPEG bounds, not full visual identity.',
        'created_utc':datetime.now(timezone.utc).isoformat(),'passed':not errors,'errors':errors,
        'qa_workspace':str(QA),'root_assets_match_qa':not qa_mismatches,
        'delivered_assets_have_capture_input_hashes':not uncaptured_assets,
        'global_guid_audit':{'unresolved':unresolved,'duplicate_project_guids':duplicates},
        'characters':view_results,'crew_play_checks':len(checks),'per_state_records':len(state_rows),'ui_checks':len(ui_checks),
        'capture_manifests':validated_manifests,'simulation':simulation,
        'limits':['Original named roster/gameplay PNG files unavailable; preserved JPEG copies are identified in authority records.',
                  'No original 3D character meshes were supplied. Unseen surfaces are reconstructed.',
                  'No automatic likeness score, physical-device FPS, thermal, touch or memory certification.',
                  'Finger grips and ten action clips are present; no facial-expression rig or lip sync.',
                  'No mobile LOD/material atlas yet; supported player builds, if performed, have a separate build-results record.'],
        'files':{path.relative_to(ROOT).as_posix():{'bytes':path.stat().st_size,'sha256':sha(path)} for path in sorted(files)}}
OUT.mkdir(parents=True,exist_ok=True)
report_path=OUT/'reference-update-validation.json'
report_path.write_text(json.dumps(report,indent=2),encoding='utf-8')
if errors:
    for error in errors:print('FAIL: '+error)
    print('Validation report: '+str(report_path));sys.exit(1)
if args.validate_only:
    print(json.dumps({'passed':True,'files':len(files),'report':str(report_path),'archive_updated':False},indent=2));sys.exit(0)

manifest_path=OUT/'reference-update-manifest.json'
manifest_path.write_text(json.dumps(report,indent=2),encoding='utf-8')
archive=OUT/'DiamondMineTycoon-Reference-Update.zip'
temporary=OUT/'DiamondMineTycoon-Reference-Update.zip.tmp'
with zipfile.ZipFile(temporary,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as bundle:
    for path in sorted(files):bundle.write(path,path.relative_to(ROOT).as_posix())
    bundle.write(manifest_path,'reference-update-manifest.json')
with zipfile.ZipFile(temporary) as bundle:
    corrupt=bundle.testzip()
    if corrupt:raise RuntimeError('ZIP CRC verification failed: '+corrupt)
temporary.replace(archive)
digest=sha(archive)
Path(str(archive)+'.sha256').write_text(digest+'  '+archive.name+'\n',encoding='utf-8')
print(json.dumps({'passed':True,'archive':str(archive),'bytes':archive.stat().st_size,'files':len(files),
                  'sha256':digest,'manifest':str(manifest_path)},indent=2))
