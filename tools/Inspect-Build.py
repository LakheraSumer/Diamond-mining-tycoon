"""Inspect our built APK using installed Android tools; never launches the APK."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import struct
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parent.parent
TOOLS = ROOT / '.cache/android-tools'
BUILD_TOOLS = TOOLS / 'SDK/build-tools/36.0.0'


def elf_segments(data):
    if data[:6] != b'\x7fELF\x02\x01':
        raise ValueError('Expected a 64-bit little-endian ELF')
    phoff = struct.unpack_from('<Q', data, 32)[0]
    entry_size, count = struct.unpack_from('<HH', data, 54)
    segments = []
    relro = []
    for index in range(count):
        pos = phoff + entry_size * index
        kind = struct.unpack_from('<I', data, pos)[0]
        offset, address, _, file_size, memory_size, alignment = struct.unpack_from('<QQQQQQ', data, pos + 8)
        if kind == 1:
            segments.append({'offset': offset, 'address': address, 'alignment': alignment,
                             'aligned_16kb': alignment >= 16384 and offset % 16384 == address % 16384})
        if kind == 0x6474e552:
            relro.append({'address': address, 'memory_size': memory_size,
                          'end_aligned_16kb': (address + memory_size) % 16384 == 0})
    return {'load_segments': segments, 'relro': relro,
            'elf_16kb_pass': bool(segments) and all(s['aligned_16kb'] for s in segments)
            and all(s['end_aligned_16kb'] for s in relro)}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('apk', type=Path)
    args = parser.parse_args()
    apk = args.apk.resolve(strict=True)
    destination = ROOT / 'Builds/inspection' / apk.stem
    destination.mkdir(parents=True, exist_ok=True)
    env = dict(os.environ, JAVA_HOME=str(TOOLS / 'OpenJDK'))
    results = {}

    def run(name, command):
        result = subprocess.run([str(x) for x in command], capture_output=True, text=True,
                                encoding='utf-8', errors='replace', env=env, timeout=120)
        (destination / f'{name}.txt').write_text(result.stdout + result.stderr, encoding='utf-8')
        results[name] = {'exit_code': result.returncode, 'evidence': str(destination / f'{name}.txt')}
        return result.stdout + result.stderr

    aapt = BUILD_TOOLS / 'aapt.exe'
    badging = run('badging', [aapt, 'dump', 'badging', apk])
    permissions = run('permissions', [aapt, 'dump', 'permissions', apk])
    manifest = run('manifest', [aapt, 'dump', 'xmltree', apk, 'AndroidManifest.xml'])
    run('signing', [TOOLS / 'OpenJDK/bin/java.exe', '-jar', BUILD_TOOLS / 'lib/apksigner.jar',
                    'verify', '--verbose', '--print-certs', apk])
    run('zipalign-16kb', [BUILD_TOOLS / 'zipalign.exe', '-v', '-c', '-P', '16', '4', apk])
    sha = hashlib.file_digest(apk.open('rb'), 'sha256').hexdigest()
    apk.with_suffix(apk.suffix + '.sha256').write_text(f'{sha}  {apk.name}\n', encoding='ascii')
    libraries = []
    sdk_markers = []
    markers = [b'com/google/android/gms/ads', b'com/android/billingclient', b'com/google/firebase/analytics']
    with zipfile.ZipFile(apk) as archive:
        inventory = [{'name': i.filename, 'bytes': i.file_size, 'compression': i.compress_type} for i in archive.infolist()]
        (destination / 'package-contents.json').write_text(json.dumps(inventory, indent=2), encoding='utf-8')
        for info in archive.infolist():
            if info.filename.startswith('lib/') and info.filename.endswith('.so'):
                data = archive.read(info)
                libraries.append({'name': info.filename, 'bytes': len(data), **elf_segments(data)})
            if info.filename.endswith('.dex'):
                data = archive.read(info)
                sdk_markers.extend(marker.decode() for marker in markers if marker in data)
    package = re.search(r"package: name='([^']+)' versionCode='([^']+)' versionName='([^']+)'", badging)
    summary = {
        'apk': str(apk), 'bytes': apk.stat().st_size, 'sha256': sha,
        'package': package.group(1) if package else None,
        'version_code': package.group(2) if package else None,
        'version_name': package.group(3) if package else None,
        'permissions': re.findall(r"uses-permission:\s*name='([^']+)'", permissions),
        'debuggable': bool(re.search(r'debuggable[^\n]*0xffffffff', manifest)),
        'tools': results, 'native_libraries': libraries,
        'all_native_elf_16kb_pass': bool(libraries) and all(i['elf_16kb_pass'] for i in libraries),
        'dex_ad_purchase_analytics_marker_matches': sorted(set(sdk_markers)),
        'limitations': ['Static packaging checks; no device launch, network traffic or physical 16 KB test.',
                        'DEX marker scan checks the listed namespaces only; release feature policy is inspected separately.']
    }
    (destination / 'summary.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
    print(json.dumps({k: summary[k] for k in ['apk', 'bytes', 'sha256', 'package', 'version_code', 'version_name',
                                             'permissions', 'debuggable', 'all_native_elf_16kb_pass']}, indent=2))
    if any(value['exit_code'] != 0 for value in results.values()) or not summary['all_native_elf_16kb_pass']:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
