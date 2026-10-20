"""Restore only the version-matched native files documented by REA."""
import hashlib
import json
import tarfile
from pathlib import Path

root = Path(__file__).resolve().parents[1]
rea = Path(r"D:\games\rea-main\rea-main")
package = root / ".cache/rea-agents-6.7.0.tgz"
# npm's registry integrity for this exact tarball, retained in evidence.
import base64
expected = base64.b64decode("yhIx2ioJYYT1C/tONfYeitC6LhnKmCh+ZwRUOgYuhSc733lGhkkt73zhug9WmLgxoMdKuqIYGCoBfD4fXkOhFw==").hex()
assert hashlib.sha512(package.read_bytes()).hexdigest() == expected
assert json.loads((rea / "package.json").read_text())["version"] == "6.7.0"
out = rea / "native/windows/build"
out.mkdir(parents=True, exist_ok=True)
with tarfile.open(package) as archive:
    for name in ["manifest.json", "rea-windows-x64.node"]:
        entry = archive.getmember("package/native/windows/build/" + name)
        assert entry.isfile() and entry.size < 16 * 1024 * 1024
        target = out / name
        assert not target.exists(), "Preserve any pre-existing native artifact"
        target.write_bytes(archive.extractfile(entry).read())
print("Restored two REA 6.7.0 native files from the SHA-512-verified registry tarball.")
