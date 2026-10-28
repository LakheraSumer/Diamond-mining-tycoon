"""Fetch the unmodified OFL font from the official Google Fonts repository."""
import datetime
import hashlib
import json
import pathlib
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[2]
out = ROOT / 'Assets/_Project/Resources/Fonts'
out.mkdir(parents=True, exist_ok=True)
files = {
    'Nunito.ttf': 'https://raw.githubusercontent.com/google/fonts/main/ofl/nunito/Nunito%5Bwght%5D.ttf',
    'Nunito-OFL.txt': 'https://raw.githubusercontent.com/google/fonts/main/ofl/nunito/OFL.txt',
}
rows = []
for name, url in files.items():
    data = urllib.request.urlopen(url, timeout=20).read()
    if name.endswith('.ttf') and data[:4] not in (b'\0\1\0\0', b'OTTO'):
        raise ValueError('Invalid font response')
    (out / name).write_bytes(data)
    rows.append({'file': str((out/name).relative_to(ROOT)), 'source': url,
                 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()})
source = ROOT / 'ArtSource/Fonts'
source.mkdir(parents=True, exist_ok=True)
(source/'source-manifest.json').write_text(json.dumps({
    'family': 'Nunito', 'license': 'SIL Open Font License 1.1',
    'upstream': 'https://github.com/google/fonts/tree/main/ofl/nunito',
    'retrieved_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'modifications': 'None. Original variable TrueType bytes; Unity Font style selection remains live.',
    'files': rows}, indent=2), encoding='utf-8')
print(json.dumps(rows, indent=2))
