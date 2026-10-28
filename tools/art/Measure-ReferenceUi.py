"""Editable, manually inspected measurements of the supplied gameplay board.

Coordinates are native source pixels, not guesses from verbal descriptions.
The original named PNG was not located; this measures the preserved 1024x683
JPEG copy. JPEG edge ambiguity is documented instead of claiming pixel identity.
"""
from pathlib import Path
import hashlib,json
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Docs/design-boards/gameplay-board.jpg'
OUT=ROOT/'Docs/UI-Reference';OUT.mkdir(parents=True,exist_ok=True)
source=Image.open(SOURCE)
SCREENS={
 'explore':([7,31,247,314],[7,60,247,284]),
 'mine':([262,31,247,314],[262,60,247,284]),
 'factory':([516,31,270,314],[516,60,270,284]),
 'craft':([793,31,225,314],[793,60,225,284]),
 'shop':([7,351,247,324],[7,380,247,295]),
 'map':([262,351,247,324],[262,380,247,295]),
 'tasks':([516,351,246,324],[516,380,246,295]),
 'rank':([775,351,243,324],[775,380,243,295])}
all_elements={k:[] for k in SCREENS}
def add(screen,id,rect,role='card',action='',font=8,layer=10,radius=None,uncertainty=1.5):
    crop=SCREENS[screen][1];x,y,w,h=rect;cx,cy,cw,ch=crop
    normalized=[(x-cx)/cw,(y-cy)/ch,w/cw,h/ch]
    all_elements[screen].append({'id':id,'source_rect_px':rect,'crop_rect_px':[x-cx,y-cy,w,h],
      'normalized_rect':normalized,'anchor_min':[normalized[0],1-normalized[1]-normalized[3]],
      'anchor_max':[normalized[0]+normalized[2],1-normalized[1]],'pivot':[.5,.5],
      'layer_order':layer,'role':role,'padding_px':[3,3,3,3],'corner_radius_px':radius if radius is not None else (6 if role in ('card','button') else 0),
      'font':{'requested_family':'rounded bold illustrated display / compact sans','fallback':'Unity LegacyRuntime.ttf (appearance differs)','size_px':font,'size_relative_to_crop_height':font/ch,'weight':'bold'},
      'icon_bounds_px':rect if role=='icon' else None,'fill_token':{'card':'cream','button':'green','pill':'dark','circle':'dark','icon':'artwork','text':'none'}.get(role,'cream'),
      'border':'warm outline; cyan on selected recipe','shadow':'soft warm 1px downward at native resolution','interaction':action or 'display only',
      'measurement_uncertainty_px':uncertainty,'method':'Manual inspection at 3x nearest source crop; rounded JPEG antialias edge midpoint.'})
def hud(screen,coin,gem,gear):
    add(screen,'coin',coin,'pill','Live GameState.Coins',9,radius=9)
    add(screen,'gem',gem,'pill','Live lifetime diamonds; secondary + opens resource information',9,radius=9)
    add(screen,'settings',gear,'button','Open existing settings',radius=5)
hud('explore',[97,62,66,19],[163,62,54,19],[227,62,19,19])
add('explore','joystick',[22,250,60,61],'circle','Existing joystick movement')
add('explore','joystick_knob',[43,271,19,19],'icon')
add('explore','home',[14,313,78,24],'button','GameSession.GoHome',8)
add('explore','home_icon',[20,316,13,17],'icon')
add('explore','home_label',[36,318,52,13],'text',font=7)
add('explore','interact',[191,276,52,58],'circle','Nearest existing bunker/factory/shop/gate interaction')
add('explore','interact_icon',[202,285,30,29],'icon')
add('explore','interact_label',[197,316,40,13],'text',font=9)
hud('mine',[359,62,58,19],[417,62,58,19],[481,62,19,19])
add('mine','inventory',[421,88,79,50],'card')
add('mine','inventory_title',[426,92,69,10],'text',font=7)
for i,x in enumerate([428,454,480]):
    add('mine',f'ore_icon_{i}',[x,105,18,21],'icon')
    add('mine',f'ore_count_{i}',[x,127,18,10],'text',font=8)
add('mine','carry',[421,141,79,35],'card')
add('mine','carry_title',[426,145,30,10],'text',font=7)
add('mine','carry_icon',[427,154,13,17],'icon')
add('mine','carry_bar',[443,154,52,8],'bar')
add('mine','carry_count',[447,165,48,9],'text',font=8)
add('mine','mine_action',[452,287,48,49],'circle','GameSession.TryDig(current plot, available reserve)')
hud('factory',[633,62,58,19],[691,62,57,19],[755,62,19,19])
for i,(x,y,w,h) in enumerate([(519,101,39,15),(574,110,39,15),(628,113,39,15),(690,118,39,15),(744,129,37,15)]):
    add('factory',f'stage_{i}',[x,y,w,h],'card','Select processing stage settings',8)
    add('factory',f'stage_number_{i}',[x+13,y-11,13,13],'circle',font=8)
for i,(x,y) in enumerate([(563,111),(618,116),(677,121),(734,132)]):add('factory',f'flow_arrow_{i}',[x,y,10,7],'text',font=10)
add('factory','inputs',[521,294,101,42],'card')
add('factory','inputs_title',[526,297,40,10],'text',font=7)
for i,x in enumerate([527,560,594]):
    add('factory',f'input_icon_{i}',[x,309,15,19],'icon')
    add('factory',f'input_count_{i}',[x+16,312,15,14],'text',font=7)
add('factory','upgrade',[699,307,77,29],'button','Upgrade selected existing machine',11)
for i,(x,w) in enumerate([(801,67),(870,65),(936,66)]):
    add('craft',f'recipe_{i}',[x,66,w,85],'card','Select existing recipe',7)
    add('craft',f'recipe_name_{i}',[x+5,71,w-10,10],'text',font=7)
    add('craft',f'recipe_icon_{i}',[x+11,84,w-22,39],'icon')
    add('craft',f'recipe_ingredients_{i}',[x+3,127,w-6,19],'card',font=7)
add('craft','next_recipe',[1004,93,13,23],'button','Cycle through the existing recipe catalogue',10)
add('craft','queue',[799,291,212,48],'card')
add('craft','queue_title',[805,294,80,10],'text',font=7)
for i,x in enumerate([806,845,884,922]):add('craft',f'queue_slot_{i}',[x,305,30,23],'card',font=7)
add('craft','queue_progress',[805,330,53,5],'bar')
add('craft','queue_time',[863,329,20,8],'text',font=7)
add('craft','craft_action',[943,301,64,31],'button','GameSession.TryMakeJewel(selected recipe)',11)
hud('shop',[117,383,54,19],[171,383,48,19],[228,383,17,19])
add('shop','request',[159,412,59,54],'card')
add('shop','request_icon',[175,418,27,31],'icon')
add('shop','sale',[19,598,223,51],'card')
add('shop','product',[27,602,45,42],'icon')
add('shop','product_name',[76,607,75,10],'text',font=8)
add('shop','price_icon',[77,623,18,16],'icon')
add('shop','price',[96,623,45,16],'text',font=11)
add('shop','sell_action',[156,606,78,35],'button','GameSession.TrySellGoods(selected product,1)',13)
add('map','region',[270,383,124,24],'card','Previous/next region selection',8)
add('map','region_previous',[275,387,10,14],'button','Previous region',10)
add('map','region_next',[380,387,10,14],'button','Next region',10)
add('map','pin',[395,383,17,24],'icon')
add('map','plot_total',[413,383,84,24],'card',font=8)
add('map','purchase',[417,500,84,128],'card')
add('map','plot_title',[422,505,74,14],'text',font=9)
for i,y in enumerate([519,544,568]):
    add('map',f'reserve_icon_{i}',[425,y,18,20],'icon')
    add('map',f'reserve_value_{i}',[450,y+3,44,15],'text',font=10)
add('map','plot_price',[442,588,48,19],'text',font=11)
add('map','buy_action',[424,602,72,23],'button','GameSession.TryPurchasePlot(selected plot)',12)
add('map','navigation',[301,636,196,25],'card','Page through existing generated plots',8)
add('map','page_previous',[309,640,12,16],'button','Previous plot page',10)
add('map','page_next',[479,640,12,16],'button','Next plot page',10)
for i,(x,y) in enumerate([(298,439),(332,464),(346,416),(288,487),(391,479),(353,503)]):add('map',f'marker_{i}',[x,y,15,17],'button','Select generated plot',10,uncertainty=3)
add('tasks','tabs',[528,385,226,23],'card')
for i,x in enumerate([528,603,678]):add('tasks',f'tab_{i}',[x,385,75,23],'button','Tasks, upgrades or milestones',8)
add('tasks','panel',[522,409,231,253],'card')
for i,y in enumerate([421,454,487]):
    add('tasks',f'task_row_{i}',[533,y,210,29],'card')
    add('tasks',f'task_icon_{i}',[540,y+2,23,25],'icon')
    add('tasks',f'task_title_{i}',[572,y+2,75,10],'text',font=7)
    add('tasks',f'task_progress_{i}',[573,y+16,25,9],'text',font=7)
    add('tasks',f'task_bar_{i}',[599,y+17,42,6],'bar')
    add('tasks',f'task_reward_icon_{i}',[659,y+8,13,13],'icon')
    add('tasks',f'task_reward_{i}',[674,y+9,20,12],'text',font=8)
    add('tasks',f'task_action_{i}',[700,y+4,43,23],'button','Claim real one-time task reward or navigate to its screen',8)
add('tasks','upgrades_title',[535,523,78,14],'text',font=8)
for i,x in enumerate([533,607,680]):
    add('tasks',f'upgrade_card_{i}',[x,540,69,112],'card')
    add('tasks',f'upgrade_name_{i}',[x+5,546,59,12],'text',font=7)
    add('tasks',f'upgrade_icon_{i}',[x+11,560,47,42],'icon')
    add('tasks',f'upgrade_level_{i}',[x+5,606,59,11],'text',font=8)
    add('tasks',f'upgrade_cost_{i}',[x+9,619,51,13],'text',font=9)
    add('tasks',f'upgrade_action_{i}',[x+5,634,59,19],'button','Upgrade an existing processing machine',8)
add('rank','tabs',[787,385,221,23],'card')
add('rank','leaders',[787,385,113,23],'button','Offline record in normal play; labelled demo in review',8)
add('rank','friends',[900,385,108,23],'card','Not an online Friends feature; demo review label only',8)
add('rank','table',[780,407,232,183],'card')
add('rank','column_rank',[785,410,20,13],'text',font=7)
add('rank','column_player',[825,410,90,13],'text',font=7)
add('rank','column_total',[919,410,84,13],'text',font=7)
for i in range(10):
    y=425+i*16
    add('rank',f'row_{i}',[785,y,220,15],'card')
    add('rank',f'row_rank_{i}',[789,y,17,15],'text',font=8)
    add('rank',f'row_avatar_{i}',[816,y,15,15],'icon')
    add('rank',f'row_alias_{i}',[837,y+1,82,13],'text',font=8)
    add('rank',f'row_total_{i}',[948,y+1,49,13],'text',font=8)
add('rank','offline',[781,594,230,65],'card')
add('rank','offline_cart',[786,600,58,54],'icon')
add('rank','offline_title',[850,603,76,13],'text',font=8)
add('rank','offline_amount',[856,621,66,22],'text',font=12)
add('rank','collect_action',[934,606,68,32],'button','Acknowledge already-settled offline report; no duplicate grant',12)
manifest={'source':'Docs/design-boards/gameplay-board.jpg','requested_original_png':'2ac71342-ded9-480a-ba21-684fbb6c8de7.png',
 'original_png_found':False,'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'source_size':list(source.size),
 'coordinate_system':'Top-left origin; source_rect_px is board pixels; normalized_rect is relative to content_crop_px.',
 'review_rule':'Exact content aspect ratio, no 9:16 stretching. Reference render uses 4x native source crop.',
 'measurement_method':'Manual inspection of displayed original plus 3x crops; edge uncertainty 1.5 native pixels unless noted.',
 'acceptance_target':'Per-axis position and size error <= 0.005 of the relevant reference crop dimension. Layout only, not total visual identity.',
 'limitations':['Only preserved JPEG available; original PNG identity unverified.','Removing the numbered ribbon excludes its whole horizontal strip.','Some inferred native edge locations have uncertainty above the initial 0.5% target.','LegacyRuntime typography and existing icon artwork still differ.'],
 'screens':{}}
font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',10)
for name,(outer,crop) in SCREENS.items():
    x,y,w,h=crop
    cut=source.crop((x,y,x+w,y+h));folder=OUT/name;folder.mkdir(parents=True,exist_ok=True);cut.save(folder/'Source.png')
    overlay=cut.resize((w*3,h*3),Image.Resampling.NEAREST);draw=ImageDraw.Draw(overlay)
    for e in all_elements[name]:
        a,b,c,d=e['crop_rect_px'];draw.rectangle((a*3,b*3,(a+c)*3,(b+d)*3),outline='#ff3a87',width=2)
    overlay.save(folder/'Measured-Bounds.png')
    manifest['screens'][name]={'outer_panel_px':outer,'content_crop_px':crop,'crop_size_px':[w,h],
     'presentation_ribbon_excluded_px':[outer[0],outer[1],outer[2],crop[1]-outer[1]],'review_viewport_px':[w*4,h*4],'elements':all_elements[name]}
path=ROOT/'Assets/_Project/Resources/Ui/reference-layout.json';path.write_text(json.dumps(manifest,indent=2))
(OUT/'layout-manifest.json').write_text(json.dumps(manifest,indent=2))
print('Measured',sum(len(e) for e in all_elements.values()),'elements across eight native-aspect reference content crops.')
