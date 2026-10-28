"""Embed texture maps in the editable crew files without changing exported models."""
import bpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
for path in sorted((ROOT/'ArtSource/Characters').glob('*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    bpy.context.preferences.filepaths.save_version=0
    for im in bpy.data.images:
        if im.source=='FILE' and im.filepath:
            source=Path(bpy.path.abspath(im.filepath))
            if not source.is_file():source=ROOT/'Assets/_Project/Art/Crew/Textures'/source.name
            if source.is_file():
                im.filepath=str(source);im.pack()
                im.filepath='//../../Assets/_Project/Art/Crew/Textures/'+source.name
    bpy.ops.wm.save_as_mainfile(filepath=str(path))
    print('PACKED-SOURCE',path.name,flush=True)
