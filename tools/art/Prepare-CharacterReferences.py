"""Preserve exact source pixels and reference-authority decisions for the crew.

This does not generate, repaint, sharpen, mask or upscale character artwork.
The named original PNG is not currently available; the preserved project JPEG
is used unless --source explicitly supplies a higher-authority roster file.
"""
from pathlib import Path
from PIL import Image,ImageStat
import argparse,hashlib,json,shutil

ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser()
parser.add_argument('--source',type=Path,default=ROOT/'Assets/_Project/Resources/Ui/crew-lineup.jpg')
args=parser.parse_args()
source=args.source.resolve();im=Image.open(source).convert('RGB')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
base_size=(1024,682)
# Inspected bounding rectangles in the available roster, top-left origin.
# These are character-isolating crops, not uniformly sized presentation tiles.
entries=[
 ('tavi','Tavi','a',(0,48,154,324)),('mira','Mira','c',(160,48,346,324)),
 ('oren','Oren','b',(342,57,500,324)),('zela','Zela','d',(516,57,679,324)),
 ('pavi','Pavi','f',(688,48,868,324)),('nilo','Nilo','e',(876,45,1024,325)),
 ('etta','Etta','h',(8,352,154,644)),('rumi','Rumi','g',(174,350,326,644)),
 ('kavi','Kavi','k',(334,350,499,644)),('sola','Sola','i',(512,353,671,644)),
 ('beni','Beni','l',(674,347,869,644)),('daro','Daro','j',(858,346,1024,644))]
decisions={
 'tavi':[
  'The original roster has goggles resting on the teal helmet. The auxiliary sheet omits those goggles; retain the roster goggles and circular lamp.',
  'Use the original asymmetrical map pouch, scarf wrapping, wavy hair and orange-brown boot silhouette. Extra backpack/harness complexity is guidance only where it does not alter the original visible silhouette.',
  'Side/back hair and scarf tails are reconstructed, not verified geometry.'],
 'mira':[
  'Retain the original forest-green rolled sleeves, rust bib overalls and bulky grey knee protection. Do not copy Tavi’s scarf or face.',
  'The small cart is part of the original identity. The auxiliary cart details are useful for unseen hardware; keep the original compact ore-cart scale and hand-on-cart stance.',
  'Rear overall strap routing and rear pouches are conservative reconstructions.'],
 'oren':[
  'Original plum outer shirt remains the dominant torso colour over the light inner shirt. The auxiliary front sheet supports this; a large brown vest that hides it is a mismatch.',
  'Retain the original beard outline, raised rock sample, magnifier, tubes and visible backpack straps. Match the original bulk before adding the auxiliary bedroll and extra buckles.',
  'Backpack rear construction and rolled blanket are inferred if not visible in the roster.'],
 'zela':[
  'Use the original red waves, cobalt protection, orange gauntlets and grey knee pads. The auxiliary sheet is consistent in broad colour regions.',
  'The drill and backpack hose follow the original visible arrangement. Added vents, hose sockets and underside detail are reconstructed.',
  'Avoid enlarging the backpack until it changes the original visible silhouette.'],
 'pavi':[
  'The roster’s teal waterproof apron is a long garment with visible volume; the auxiliary sheet supports the apron. Do not reduce it to a small bib.',
  'Retain the roster’s large round goggles, cream sleeves, coral gauntlets/boots and pan held by both hands.',
  'The auxiliary rear apron bow and hood construction are inferred; use only where consistent with the original cream neckline and apron straps.'],
 'nilo':[
  'Keep the original short swept hair, indigo vest/trousers, cream sleeves, dark gloves and divided sorting tray.',
  'The auxiliary front/back pocket seams and patch shapes may guide hidden construction, but must not change the original narrow youthful-adult silhouette.',
  'Tray underside and compartment depth are reconstructed.'],
 'etta':[
  'The original Etta has textured hair, a violet headband/lamp and protective glasses, not a miner helmet. The auxiliary sheet’s headband supports this.',
  'Keep the original hair gathered upward, apricot shirt, dark trousers and purple-laced boots. Do not replace the distinctive volume with a smooth cap.',
  'Retain the original raised gem and precision tool pose; added gem-case trays and rear hair geometry are reconstructions.'],
 'rumi':[
  'The original visible hair is a short bob/compact hair mass under the pale-blue helmet. The auxiliary sheet proposes a larger tied-back ponytail; prefer the original visible bob silhouette.',
  'Keep the original lavender workwear, pale-blue sleeves, grey gloves and cream cloth. Do not use Tavi’s face solely because both wear similar helmets.',
  'Back trouser pockets and hidden belt tools are reconstructed, not verified.'],
 'kavi':[
  'No helmet: retain the original dark wavy hair and beard.',
  'The original sand shirt and maroon leather apron, ring-shaped belt tools and raised ring determine identity. Auxiliary rear cross-straps and knots are inferred.',
  'Keep the original visible face and hairstyle rather than inventing a generic combed crown.'],
 'sola':[
  'The original has a short wavy blonde bob. The auxiliary sheet proposes an elaborate back bun; prefer a conservative bob reconstruction consistent with the roster.',
  'Retain the peach blouse, sage fitted waistcoat, diamond badge, tailored trousers, loafers and open jewel box. Do not add work boots or a helmet.',
  'The box hinge, lining and hidden hair surfaces are reconstructed details.'],
 'beni':[
  'The original Beni wears a blue delivery jacket and trousers, not a bib-overall chest. The auxiliary jacket front supports preserving that construction.',
  'Keep the original broad rounded silhouette, amber helmet, gloves and wheeled trolley with stacked protective cases. A floating single suitcase is a mismatch.',
  'Extra backpack hardware in the auxiliary sheet is lower authority and should not change the original visible silhouette.'],
 'daro':[
  'Retain the original mustard helmet, glasses, beard, olive overalls, dark shirt, pale rolled cuffs, turquoise blueprint and dark planning board.',
  'The auxiliary square frames and overall fittings are useful, but any hidden belt tool additions remain reconstructed.',
  'Back strap and rear pocket details cannot be verified from the roster.']}

# Inspected 5x5 sampling regions, relative to each crop at the available JPEG
# resolution. These preserve exposure/shadow differences; they are observations,
# not guessed flat paint colours or exact PBR base-colour declarations.
regions={
 'tavi':[('Teal helmet',65,14),('Skin',86,59),('Dark hair',53,61),('Teal jacket',126,114),('Cream scarf',86,79),('Tan trousers',63,178),('Orange-brown boots',127,251),('Leather pouch',124,154)],
 'mira':[('Green helmet',67,18),('Skin',90,51),('Tied dark hair',42,58),('Green shirt',42,92),('Rust bib',92,108),('Orange trousers',108,169),('Brown boots',108,250),('Grey knee protection',120,195)],
 'oren':[('Yellow helmet',56,23),('Skin',76,37),('Dark beard',82,50),('Plum shirt',35,94),('Cream inner shirt',84,113),('Brown trousers',57,181),('Work boots',113,244),('Backpack leather',25,65)],
 'zela':[('Cobalt helmet',72,16),('Skin',98,52),('Red hair',57,55),('Blue workwear',93,106),('Orange glove',54,135),('Grey knee pad',59,207),('Dark boot',52,248),('Backpack metal',27,96)],
 'pavi':[('Teal helmet',59,25),('Skin',81,59),('Dark hair',47,54),('Cream sleeve',35,96),('Turquoise apron',79,180),('Coral glove',60,134),('Coral rubber boot',52,243),('Dark washing pan',130,121)],
 'nilo':[('Mint helmet',53,19),('Skin',70,57),('Dark hair',56,37),('Indigo vest',80,105),('Cream sleeve',22,127),('Indigo trousers',86,180),('Dark boot',117,254),('Dark glove',52,148)],
 'etta':[('Textured dark hair',33,30),('Skin',68,52),('Violet glasses',64,42),('Apricot shirt',43,95),('Dark trousers',55,183),('Grey knee protection',42,203),('Dark boot',104,269),('Leather belt',90,132)],
 'rumi':[('Pale-blue helmet',66,17),('Skin',89,54),('Dark bob',34,63),('Lavender workwear',94,169),('Pale-blue sleeve',36,122),('Grey glove',72,111),('Cream cloth',114,118),('Brown boot',108,271)],
 'kavi':[('Dark hair',62,28),('Skin',84,36),('Dark beard',86,53),('Sand shirt',45,88),('Maroon apron',89,102),('Brown trousers',41,221),('Brown boot',36,266),('Gold tool loop',81,168)],
 'sola':[('Blonde hair',37,39),('Skin',64,53),('Peach blouse',35,94),('Sage waistcoat',75,105),('Dark trousers',81,177),('Brown loafer',103,273),('Leather pouch',30,158),('Jewellery-box lining',132,78)],
 'beni':[('Amber helmet',91,28),('Skin',94,56),('Dark hair',72,39),('Blue jacket',118,113),('Blue trousers',87,205),('Work glove',50,145),('Brown boot',43,268),('Delivery-case metal',150,200)],
 'daro':[('Mustard helmet',63,21),('Skin',91,44),('Facial hair',86,56),('Olive overalls',107,175),('Dark shirt',138,81),('Work glove',148,130),('Brown boot',131,275),('Turquoise blueprint',36,88)]}

refdir=ROOT/'ArtSource/Characters/References';refdir.mkdir(parents=True,exist_ok=True)
preserved=refdir/('OriginalRoster'+source.suffix.lower())
if source!=preserved.resolve():shutil.copyfile(source,preserved)
manifest={'authority_order':['Original roster visible identity','Original gameplay board composition','Auxiliary character sheets','Conservative unseen-surface reconstruction'],
 'requested_original_filename':'496f83af-1898-4619-aed4-5ced82552533.png',
 'source':str(source),'source_sha256':sha(source),'source_dimensions':list(im.size),
 'original_png_available':source.name.lower()=='496f83af-1898-4619-aed4-5ced82552533.png',
 'limitation':'The named original PNG was not found in the project, D:/games or Codex attachments. These crops preserve exact decoded pixels from the available 1024x682 JPEG copy; they cannot certify pixel equality to the missing original PNG.',
 'crop_operation':'Pillow RGB decode followed by rectangular crop; saved lossless PNG. No resampling, enhancement, masking or generated content.',
 'characters':{}}
sx,sy=im.width/base_size[0],im.height/base_size[1]
for identifier,name,letter,base_box in entries:
    folder=ROOT/'ArtSource/Characters'/name;folder.mkdir(parents=True,exist_ok=True)
    box=tuple(round(value*(sx if i%2==0 else sy)) for i,value in enumerate(base_box))
    cropped=im.crop(box);out=folder/'01_SourceCrop.png';cropped.save(out)
    assert Image.open(out).convert('RGB').tobytes()==cropped.tobytes()
    auxiliary=Path('D:/games/chartcter image')/(letter+'.png')
    if auxiliary.is_file():shutil.copyfile(auxiliary,folder/'AuxiliarySheet.png')
    # Quantised source colours are sampled evidence, not artist-approved PBR values.
    # Cream background and nearly-white margins are excluded from these samples.
    px=[p for p in cropped.get_flattened_data() if max(p)-min(p)>20 or max(p)<175]
    palette_image=Image.new('RGB',(max(1,len(px)),1));palette_image.putdata(px or [(0,0,0)])
    quantized=palette_image.quantize(colors=8);raw=quantized.getpalette()
    colors=sorted(quantized.getcolors(),reverse=True)
    palette=[{'rgb':raw[index*3:index*3+3],'pixels':count} for count,index in colors]
    labelled=[]
    for label,x,y in regions[identifier]:
        x=round(x*sx);y=round(y*sy);r=max(1,round(2*max(sx,sy)))
        patch_box=(max(0,x-r),max(0,y-r),min(cropped.width,x+r+1),min(cropped.height,y+r+1))
        rgb=[int(v) for v in ImageStat.Stat(cropped.crop(patch_box)).median]
        labelled.append({'label':label,'crop_pixel_rect_xyxy':list(patch_box),'rgb':rgb,'method':'Median RGB of inspected source pixel patch; source lighting and JPEG compression retained.'})
    entry={'id':identifier,'name':name,'source_crop_rect_xyxy':list(box),'source_crop_dimensions':list(cropped.size),
        'source_crop_sha256':sha(out),'decoded_pixel_copy_verified':True,
        'auxiliary_source':str(auxiliary),'auxiliary_sha256':sha(auxiliary) if auxiliary.is_file() else None,
        'source_palette_method':'8-colour median-cut quantisation of non-background source pixels; observational colours, not exact PBR parameters',
        'source_palette':palette,'labelled_source_samples':labelled,'decisions':decisions[identifier],
        'side_back_status':'Reconstructed details. The auxiliary views are proposed generated guidance, not verified technical drawings.'}
    (folder/'reference.json').write_text(json.dumps(entry,indent=2),encoding='utf-8')
    (folder/'README.md').write_text('# '+name+' reference and model review\n\n'+manifest['limitation']+'\n\n`01_SourceCrop.png` is the unchanged crop of that available roster copy. `AuxiliarySheet.png` is the supplied generated modelling guidance. The numbered 02–10 images are actual-model review outputs when present, not recovered original assets.\n\n'+ '\n'.join('- '+x for x in decisions[identifier])+'\n\nSide and back surfaces are reconstructed. See `reference.json` for crop coordinates, hashes and source palette samples, and `model-views.json` for actual render provenance.\n',encoding='utf-8')
    manifest['characters'][identifier]=entry
(refdir/'authority-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
doc=ROOT/'Docs/characters/reference-authority.md'
doc.write_text('# Character reference authority and discrepancy log\n\nOriginal roster identity takes priority over generated auxiliary sheets. Original gameplay-board composition is the UI authority. Hidden geometry is reconstructed; no similarity score is assigned by this preparation step.\n\n'+manifest['limitation']+'\n\nThe exact crop rectangles and SHA-256 values are recorded in `ArtSource/Characters/References/authority-manifest.json`. Source crops are not stretched or regenerated.\n\n'+ '\n\n'.join('## '+name+'\n\n'+'\n'.join('- '+d for d in decisions[identifier]) for identifier,name,_,_ in entries)+'\n',encoding='utf-8')
print(json.dumps({'source_dimensions':im.size,'source':str(source),'characters':len(entries),'manifest':str(refdir/'authority-manifest.json')},indent=2))
