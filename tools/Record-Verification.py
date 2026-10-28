"""Record completed local checks; never invent device passes."""
import hashlib
import json
import platform
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RESULTS = ROOT / 'tests/results'
ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
trx = ET.parse(RESULTS / 'simulation.trx').getroot()
counter = trx.find('t:ResultSummary/t:Counters', ns).attrib
tests = []
pacing = []
for result in trx.findall('t:Results/t:UnitTestResult', ns):
    item = {k: result.attrib.get(k) for k in ['testName', 'outcome', 'duration']}
    tests.append(item)
    if item['testName'].startswith('Pacing_'):
        pacing.append({'test': item['testName'], 'output': result.findtext('t:Output/t:StdOut', '', ns).strip()})
sources = {}
for folder in ['Assets', 'Packages', 'ProjectSettings']:
    for path in sorted((ROOT / folder).rglob('*')):
        if path.is_file(): sources[path.relative_to(ROOT).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
summary = {'test_counters': counter, 'test_run_times': trx.find('t:Times', ns).attrib,
    'host': platform.platform(), 'pacing': pacing, 'tests': tests, 'source_sha256': sources,
    'trx_sha256': hashlib.sha256((RESULTS/'simulation.trx').read_bytes()).hexdigest(),
    'unity_player_build': 'not invoked by this verification script; existing Builds artifacts may be from older source', 'device_qa': {'android':'not run','ios':'not run'},
    'limitations': ['API compiler check is not Unity import or IL2CPP', 'Windows save behavior is not a mobile filesystem result', 'No device FPS, thermal or memory measurement']}
timber_review = ROOT/'release/screenshots/timber-bunker'
if (timber_review/'checks.txt').exists() and (timber_review/'review-errors.txt').exists():
    checks = (timber_review/'checks.txt').read_text().splitlines()
    errors = (timber_review/'review-errors.txt').read_text().strip()
    summary['timber_editor_play_review'] = {'passed_checks':sum(x.startswith('PASS:') for x in checks),
        'errors':errors, 'evidence':'release/screenshots/timber-bunker/interaction-results.txt',
        'scope':'Main Play mode, isolated save, synthetic UI input. No device results.'}
crew_review = ROOT/'release/screenshots/characters'
if (crew_review/'checks.txt').exists() and (crew_review/'review-errors.txt').exists():
    checks = (crew_review/'checks.txt').read_text().splitlines()
    errors = (crew_review/'review-errors.txt').read_text().strip()
    manifest_path=crew_review/'evidence-manifest.json'
    capture_current=False
    if manifest_path.exists():
        manifest=json.loads(manifest_path.read_text(encoding='utf-8-sig'))
        inputs=manifest.get('inputs',[])
        capture_current=bool(inputs) and all((ROOT/row['file']).is_file() and hashlib.sha256((ROOT/row['file']).read_bytes()).hexdigest()==row['sha256'] for row in inputs)
    summary['crew_editor_play_review'] = {'passed_checks':sum(x.startswith('PASS:') for x in checks),
        'errors':errors, 'evidence':'release/screenshots/characters/review-context.txt',
        'scope':'All twelve imported characters and Main gameplay; isolated save; automated input; no device results.',
        'capture_inputs_match_current_files':capture_current,
        'freshness_note':'Later source changes require a new Play review unless capture inputs match. Check count alone is not current-source validation.',
        'visual_likeness':'Requested 100% reference match not achieved.'}
(RESULTS/'verification-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
(ROOT/'Docs/Economy-Results.md').write_text('# Economy results\n\nMeasured by the original guided bot on the current source/config, without ads and without manually collecting order bonuses. This is simulated progression, not a child playtest or a device measurement.\n\n'+ '\n\n'.join('## '+x['test']+'\n\n```text\n'+x['output']+'\n```' for x in pacing)+'\n\nEvidence: tests/results/simulation.trx and verification-summary.json. Order bonuses can accelerate progression; they do not gate production.\n',encoding='utf-8')
print(json.dumps({'passed':counter['passed'],'failed':counter['failed'],'pacing':pacing},indent=2))
