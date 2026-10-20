"""Install only this project's Android dependencies from official pinned archives.

No global environment, existing SDK, agent configuration, or license is changed.
"""
import base64
import hashlib
import json
import shutil
import subprocess
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / '.cache/android-tools'
CACHE.mkdir(parents=True, exist_ok=True)
EVIDENCE = ROOT / 'analysis/evidence'
release = json.loads((EVIDENCE / 'unity-release.json').read_text())
windows = next(x for x in release['results'][0]['downloads'] if x['platform'] == 'WINDOWS' and x['architecture'] == 'X86_64')
android = next(x for x in windows['modules'] if x['id'] == 'android')
modules = []
def visit(node):
    modules.append(node)
    for child in node.get('subModules', []): visit(child)
visit(android)
repository = ET.fromstring((EVIDENCE / 'android-repository.xml').read_bytes())

def checksum_for(url):
    filename = url.rsplit('/', 1)[-1]
    for archive in repository.iter('archive'):
        if archive.findtext('complete/url') == filename:
            checksum = archive.find('complete/checksum')
            return checksum.attrib['type'], checksum.text
    # Unity publishes its JDK archive's SHA-256 in the filename.
    tail = filename.rsplit('_', 1)[-1].removesuffix('.zip')
    if len(tail) == 64 and all(c in '0123456789abcdef' for c in tail): return 'sha256', tail
    raise ValueError('No official checksum: ' + filename)

def digest(path, algorithm):
    with path.open('rb') as stream: return hashlib.file_digest(stream, algorithm).hexdigest()

def download(url, partial, size):
    if size < 200 * 1024**2:
        subprocess.run(['curl.exe', '--silent', '--show-error', '--fail', '--location', '--retry', '1',
            '--connect-timeout', '20', '--max-time', '300', '--speed-time', '45', '--speed-limit', '1024',
            '--output', str(partial), url], check=True)
        return
    # Bounded HTTP ranges avoid losing an entire large SDK download on a slow
    # connection. The final archive is still checked against Google's digest.
    block = 16 * 1024**2
    parts = partial.with_suffix('.chunks')
    parts.mkdir(exist_ok=True)
    if partial.exists():
        with partial.open('rb') as old:
            for offset in range(0, (partial.stat().st_size // block) * block, block):
                target = parts / str(offset)
                data = old.read(block)
                if not target.exists(): target.write_bytes(data)
    def transfer(offset):
        end = min(size, offset + block) - 1
        target = parts / str(offset)
        if target.exists() and target.stat().st_size == end - offset + 1: return target
        subprocess.run(['curl.exe', '--silent', '--show-error', '--fail', '--location', '--retry', '1',
            '--connect-timeout', '20', '--max-time', '180', '--range', f'{offset}-{end}',
            '--output', str(target), url], check=True)
        assert target.stat().st_size == end - offset + 1, 'Server did not honor bounded range'
        return target
    with ThreadPoolExecutor(max_workers=4) as workers:
        completed = list(workers.map(transfer, range(0, size, block)))
    assembled = partial.with_suffix('.assembled')
    with assembled.open('wb') as output:
        for target in completed:
            with target.open('rb') as source: shutil.copyfileobj(source, output, length=1024*1024)
    assembled.replace(partial)

def install(node, relative):
    url = node['url']
    assert url.startswith(('https://download.unity3d.com/', 'https://dl.google.com/android/repository/'))
    algorithm, expected = checksum_for(url)
    archive = CACHE / url.rsplit('/', 1)[-1]
    destination = CACHE / relative
    if not archive.exists():
        print('Downloading ' + node['name'], flush=True)
        partial = archive.with_suffix('.part')
        download(url, partial, node['downloadSize']['value'])
        assert partial.stat().st_size == node['downloadSize']['value'], 'Download size mismatch'
        assert digest(partial, algorithm) == expected, 'Official checksum mismatch'
        partial.rename(archive)
    assert digest(archive, algorithm) == expected, 'Cached archive checksum mismatch'
    marker = destination / '.diamond-install-complete'
    if not marker.exists():
        print('Extracting ' + node['name'], flush=True)
        with zipfile.ZipFile(archive) as bundle:
            files = [x for x in bundle.infolist() if not x.is_dir()]
            assert sum(x.file_size for x in files) < 8 * 1024**3
            top = {PurePosixPath(x.filename).parts[0] for x in files}
            strip = len(top) == 1
            for member in files:
                parts = PurePosixPath(member.filename).parts
                assert '..' not in parts and not member.filename.startswith(('/', '\\'))
                target = destination.joinpath(*(parts[1:] if strip else parts))
                assert target.resolve().is_relative_to(destination.resolve()) and not target.is_symlink()
                target.parent.mkdir(parents=True, exist_ok=True)
                with bundle.open(member) as source, target.open('wb') as output:
                    shutil.copyfileobj(source, output, length=1024*1024)
        marker.write_text(algorithm + ':' + expected)
    return {'id': node['id'], 'path': str(destination), 'url': url, 'checksum': algorithm + ':' + expected, 'archive_sha256': digest(archive, 'sha256')}

selected = [
    ('android-open-jdk-17.0.18+8', 'OpenJDK'),
    ('android-ndk-r27c', 'NDK'),
    ('android-sdk-build-tools-36.0.0', 'SDK/build-tools/36.0.0'),
    ('android-sdk-platforms-36', 'SDK/platforms/android-36'),
    ('android-sdk-command-line-tools-16.0', 'SDK/cmdline-tools/16.0'),
    ('cmake-3.22.1', 'SDK/cmake/3.22.1'),
]
results = []
for module_id, relative in selected:
    node = next(x for x in modules if x['id'] == module_id)
    results.append(install(node, relative))
    (EVIDENCE / 'android-tools-installed.json').write_text(json.dumps(results, indent=2))
# Reuse already installed platform tools and previously accepted licenses.
existing = Path.home() / 'AppData/Local/Android/Sdk'
for folder in ['platform-tools', 'licenses']:
    if (existing / folder).exists(): shutil.copytree(existing / folder, CACHE / 'SDK' / folder, dirs_exist_ok=True)
print('Project-local Android prerequisites prepared. No new license acceptance was automated.', flush=True)
