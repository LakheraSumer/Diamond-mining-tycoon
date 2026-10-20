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
    'unity_player_build': 'not built', 'device_qa': {'android':'not run','ios':'not run'},
    'limitations': ['API compiler check is not Unity import or IL2CPP', 'Windows save behavior is not a mobile filesystem result', 'No device FPS, thermal or memory measurement']}
(RESULTS/'verification-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
(ROOT/'Docs/Economy-Results.md').write_text('# Economy results\n\nMeasured by the original guided bot on the current source/config, without ads and without manually collecting order bonuses. This is simulated progression, not a child playtest or a device measurement.\n\n'+ '\n\n'.join('## '+x['test']+'\n\n```text\n'+x['output']+'\n```' for x in pacing)+'\n\nEvidence: tests/results/simulation.trx and verification-summary.json. Order bonuses can accelerate progression; they do not gate production.\n',encoding='utf-8')
print(json.dumps({'passed':counter['passed'],'failed':counter['failed'],'pacing':pacing},indent=2))
