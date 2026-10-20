"""Create stable missing Unity metadata without changing any existing GUID."""
from pathlib import Path
import uuid

root = Path(__file__).resolve().parents[1]
assets = root / 'Assets'
created = 0
for path in sorted(assets.rglob('*')):
    if path.suffix == '.meta':
        continue
    meta = Path(str(path) + '.meta')
    if meta.exists():
        continue
    relative = path.relative_to(root).as_posix()
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'diamond-mine-tycoon:' + relative).hex
    content = 'fileFormatVersion: 2\nguid: ' + guid + '\n'
    if path.is_dir():
        content += 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix == '.cs':
        content += 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix == '.png':
        content += 'TextureImporter:\n  externalObjects: {}\n  serializedVersion: 13\n  mipmaps:\n    enableMipMap: 0\n  isReadable: 0\n  textureType: 0\n  sRGBTexture: 1\n  maxTextureSize: 1024\n  alphaIsTransparency: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    else:
        importer = {'.json': 'TextScriptImporter', '.asmdef': 'AssemblyDefinitionImporter'}.get(path.suffix, 'DefaultImporter')
        content += importer + ':\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    meta.write_text(content, encoding='utf-8', newline='\n')
    created += 1
print(f'Created {created} missing metadata files; existing metadata preserved.')
