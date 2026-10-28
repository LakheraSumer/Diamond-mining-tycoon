"""Snapshot current project/source files without certifying unrun Unity reviews."""
from pathlib import Path
import hashlib,json,zipfile
from datetime import datetime,timezone

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'release/artwork';OUT.mkdir(parents=True,exist_ok=True)
files=set()
for directory in ('Assets','Packages','ProjectSettings','ArtSource','Docs','tools','tests'):
    for path in (ROOT/directory).rglob('*'):
        if not path.is_file() or any(part in ('__pycache__','_Recovery','bin','obj') for part in path.relative_to(ROOT).parts):continue
        if path.suffix.lower() in ('.blend1','.blend2','.pyc','.zip'):continue
        files.add(path)
for name in ('README.md','PROGRESS.md','PLAY_GAME.cmd','TEST_GAME.cmd'):
    files.add(ROOT/name)

def sha(path):
    digest=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''):digest.update(block)
    return digest.hexdigest()

view=json.loads((ROOT/'ArtSource/Characters/view-validation.json').read_text())
if not view.get('passed'):raise RuntimeError('Current numbered model-view validation has not passed')
manifest={
    'created_utc':datetime.now(timezone.utc).isoformat(),
    'status':'SOURCE DRAFT — final combined Unity Play review and visual acceptance pending',
    'integrity_scope':'File hashes and ZIP CRC only. Does not certify Unity gameplay, visual likeness or player builds.',
    'final_execution_blocker':'Automatic approval review failed with an account usage limit; final Unity action was not executed.',
    'current_model_views_verified':True,
    'limits':['No 95/100 character or exact UI acceptance.','Side/back geometry reconstructed.','Historical Unity captures are delivered separately in the image gallery.','Use Docs/REFERENCE-UPDATE.md for current test/build scope.'],
    'files':{p.relative_to(ROOT).as_posix():{'bytes':p.stat().st_size,'sha256':sha(p)} for p in sorted(files)}
}
description='''SOURCE DRAFT — DIAMOND MINE TYCOON

Existing Unity project snapshot, editable Blender models, UVs/materials/textures,
numbered reference/model views, measured UI and reproduction tools.

The final combined Unity Play-mode recapture was NOT executed because the
agent automatic approval reviewer hit an account usage limit. Earlier passing
checks do not certify the latest revision. Character/UI reference match is
unfinished. Read Docs/REFERENCE-UPDATE.md before treating this as a release.

Open the existing project in Unity 6000.3.26f1. Do not copy Library/cache files.
Use the isolated crew preview for laptop testing. Normal save data is not
included in this archive. No signing keys or external account data is included.
'''
target=OUT/'DiamondMineTycoon-Source-Draft.zip'
temporary=OUT/'DiamondMineTycoon-Source-Draft.zip.tmp'
with zipfile.ZipFile(temporary,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
    archive.writestr('READ-ME-FIRST.txt',description)
    archive.writestr('source-draft-manifest.json',json.dumps(manifest,indent=2))
    for path in sorted(files):archive.write(path,path.relative_to(ROOT).as_posix())
with zipfile.ZipFile(temporary) as archive:
    failure=archive.testzip()
    if failure:raise RuntimeError('ZIP CRC failure: '+failure)
temporary.replace(target)
(OUT/'source-draft-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
Path(str(target)+'.sha256').write_text(sha(target)+'  '+target.name+'\n',encoding='utf-8')
print(json.dumps({'archive':str(target),'files':len(files),'bytes':target.stat().st_size,'status':manifest['status'],'sha256':sha(target)},indent=2))
