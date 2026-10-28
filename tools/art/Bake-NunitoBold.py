"""OFL-compatible static bold derivative, renamed to avoid upstream family ambiguity."""
import hashlib
import json
from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

ROOT=Path(__file__).resolve().parents[2]
source=ROOT/'Assets/_Project/Resources/Fonts/Nunito.ttf'
out=ROOT/'ArtSource/Fonts/DiamondMineRounded-Bold.ttf'
font=instantiateVariableFont(TTFont(source),{'wght':800},inplace=False)
names={1:'DiamondMine Rounded',2:'Bold',3:'DiamondMineRounded-Bold-800-1',
       4:'DiamondMine Rounded Bold',6:'DiamondMineRounded-Bold',16:'DiamondMine Rounded',17:'Bold'}
for record in font['name'].names:
    if record.nameID in names:
        record.string=names[record.nameID].encode(record.getEncoding(),errors='replace')
font['OS/2'].usWeightClass=800
font.save(out)
record={'file':out.relative_to(ROOT).as_posix(),'upstream_file':source.relative_to(ROOT).as_posix(),
        'license':'SIL Open Font License 1.1; upstream Nunito copyright and license retained.',
        'modifications':'FontTools static instance at wght=800; family renamed DiamondMine Rounded; naming only beyond weight instancing.',
        'sha256':hashlib.sha256(out.read_bytes()).hexdigest()}
(ROOT/'ArtSource/Fonts/bold-derivative.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
print(json.dumps(record,indent=2))
