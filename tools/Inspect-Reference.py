"""Bounded, static APK inventory; never executes or installs APK contents."""
import hashlib
import json
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(r"D:\games\uptodown-com.lumber.inc.apk")
OUT = ROOT / "analysis" / "evidence"
OUT.mkdir(parents=True, exist_ok=True)
COPY = OUT / "reference-working-copy.apk"

def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()

source_hash = digest(SOURCE)
if not COPY.exists():
    shutil.copyfile(SOURCE, COPY)
assert digest(COPY) == source_hash, "Working copy does not match original"
sdk = Path.home() / "AppData/Local/Android/Sdk"
build_tools = sdk / "build-tools/36.1.0"
jdk = Path(r"C:\Program Files\Android\Android Studio\jbr")
commands = {
    "badging.txt": [str(build_tools / "aapt.exe"), "dump", "badging", str(COPY)],
    "manifest-tree.txt": [str(build_tools / "aapt.exe"), "dump", "xmltree", str(COPY), "AndroidManifest.xml"],
    "signing.txt": [str(jdk / "bin/java.exe"), "-jar", str(build_tools / "lib/apksigner.jar"), "verify", "--verbose", "--print-certs", str(COPY)],
}
results = {}
for name, argv in commands.items():
    result = subprocess.run(argv, capture_output=True, timeout=45)
    (OUT / name).write_bytes(result.stdout + b"\nSTDERR:\n" + result.stderr)
    results[name] = {"argv": argv, "exit_code": result.returncode}

with zipfile.ZipFile(COPY) as archive:
    entries = archive.infolist()
    assert len(entries) < 100000
    assert sum(x.file_size for x in entries) < 1024**3
    bad_crc_entry = archive.testzip()
    names = [e.filename for e in entries]
    inventory = [{"path": e.filename, "size": e.file_size, "compressed_size": e.compress_size, "crc32": f"{e.CRC:08x}"} for e in entries]
    (OUT / "zip-inventory.json").write_text(json.dumps(inventory, indent=2), encoding="utf-8")
    summary = {
        "source": str(SOURCE), "working_copy": str(COPY), "sha256": source_hash,
        "source_preserved": digest(SOURCE) == source_hash, "bytes": SOURCE.stat().st_size,
        "entries": len(entries), "uncompressed_bytes": sum(e.file_size for e in entries),
        "bad_crc_entry": bad_crc_entry, "duplicate_names": sorted({n for n in names if names.count(n) > 1}),
        "abis": sorted({n.split('/')[1] for n in names if n.startswith('lib/') and n.endswith('.so')}),
        "native_libraries": [n for n in names if n.startswith('lib/') and n.endswith('.so')],
        "dex_files": [n for n in names if n.endswith('.dex')],
        "unity_indicators": [n for n in names if 'il2cpp' in n.lower() or 'global-metadata.dat' in n.lower() or n.startswith('assets/bin/Data/') or n.endswith('libunity.so')],
        "embedded_apks_or_obb": [n for n in names if n.endswith(('.apk', '.obb'))],
        "commands": results,
        "limitations": ["No APK execution", "Signing certificate not compared with an independently trusted publisher identity", "ZIP integrity is not publisher authenticity", "No network or private services contacted"],
    }
(OUT / "apk-summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
print(json.dumps(summary, indent=2))
