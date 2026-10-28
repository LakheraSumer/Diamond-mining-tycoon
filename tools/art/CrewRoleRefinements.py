"""Reference-directed clothing, hair and signature equipment for the other ten crew.

Shared anatomy, rig and low-level mesh helpers remain in Create-Crew.py. This
module makes visible role differences explicit; it does not assign likeness
scores or treat inferred rear construction as verified source geometry.
"""
import bpy,math,random
from mathutils import Vector,Matrix
from CrewRefinements import sleeve,stitch,hair_lock,curve

def material(g,key,value,rough=.7,metal=0,cloth=False,leather=False):
    return g['material'](key,value,rough,metal,texture=g['FABRIC'] if cloth else g['LEATHER'] if leather else None,
                         normal=g['FABRIC_N'] if cloth else g['LEATHER_N'] if leather else None)

def thick(obj,amount=.004):
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    mod=obj.modifiers.new('Actual garment edge thickness','SOLIDIFY');mod.thickness=amount
    bpy.ops.object.modifier_apply(modifier=mod.name);obj.select_set(False)

def open_coat(g,name,rings,mat,gap_bottom=.035,gap_top=.28):
    vv=[];uv=[];weights=[];ff=[];segments=40
    for i,(h,rx,rz) in enumerate(rings):
        t=i/(len(rings)-1);gap=gap_bottom+(gap_top-gap_bottom)*max(0,(t-.45)/.55)
        for j in range(segments):
            a=gap+(math.tau-gap*2)*j/(segments-1);fold=.003*math.sin(a*6+i*1.8)
            vv.append((math.sin(a)*(rx+fold),h,math.cos(a)*(rz+fold)))
            uv.append((j/(segments-1),t));weights.append(g['torso_weight'](h))
    for i in range(len(rings)-1):
        for j in range(segments-1):ff.append((i*segments+j,i*segments+j+1,(i+1)*segments+j+1,(i+1)*segments+j))
    obj=g['mesh'](name,vv,ff,mat,weights=weights,uv=uv);thick(obj);return obj

def create_role_clothes(g):
    c=g['current'];identifier=g['current_id'];wide=c.get('width',1)
    loft,mesh,tube,block,ell,ribbon=[g[n] for n in ('loft','mesh','tube','block','ellipsoid','ribbon')]
    outer={'oren':'74476A','zela':c['top'],'pavi':c['top'],'nilo':c['top'],'etta':c['top'],'rumi':c['top'],
           'kavi':'793D31','sola':c['top'],'beni':c['top'],'daro':c['top']}[identifier]
    inner='DDD0B5' if identifier=='oren' else c['shirt']
    material(g,'RoleOuter',outer,.71,cloth=identifier not in ('pavi','kavi'))
    material(g,'RoleInner',inner,.79,cloth=True)
    material(g,'RoleApron',outer,.52 if identifier=='pavi' else .75,leather=identifier=='kavi')
    material(g,'RoleGlove',c['accent'] if identifier in ('pavi','zela') else ('4A4339' if identifier in ('nilo','etta','rumi') else '65543C'),.43 if identifier in ('pavi','zela') else .8,leather=identifier not in ('pavi','zela'))
    material(g,'RoleRubber','282C2A',.82)
    material(g,'RoleBoot',c['boots'],.42 if identifier=='pavi' else .73,leather=identifier!='pavi')
    material(g,'RoleLace','8754AD' if identifier=='etta' else 'C9A15B',.83)
    # The shared anatomical hands are recoloured by actual role material. Kavi
    # and Sola retain bare hands; no glove silhouette is invented for them.
    for obj in g['objects']:
        if obj.name.startswith('Anatomical_hand'):
            obj.data.materials[0]=g['M']['Skin' if identifier in ('kavi','sola') else 'RoleGlove']
    broad={'oren':1.08,'pavi':1.12,'beni':1.23,'nilo':.94,'sola':.91,'daro':1.02,'kavi':1.05,'etta':.97,'rumi':.96,'zela':1.01}[identifier]
    torso=[(.91,.185*broad,.127),(.97,.185*broad,.129),(1.04,.17*broad,.124),(1.12,.193*broad,.139),
           (1.22,.224*broad,.147),(1.285,.239*broad,.135),(1.32,.216*broad,.115),(1.36,.091,.076)]
    if identifier=='beni':torso=[(h,rx,rz*(1.11 if h<1.23 else 1.04)) for h,rx,rz in torso]
    if identifier=='sola':torso=[(h,rx*(.95 if h<1.15 else 1),rz*.96) for h,rx,rz in torso]
    loft('Role_base_shirt',[(0,h,0,rx,rz) for h,rx,rz in torso],'RoleInner',segments=40,weight_func=g['torso_weight'])
    loft('Closed_collar_neck',[(0,1.32,0,.076,.068),(0,1.48,0,.069,.062)],'Skin','Neck',segments=32)
    if identifier in ('oren','zela','nilo','etta','rumi','sola','beni'):
        coat=[(h,rx+.008,rz+.010) for h,rx,rz in torso]
        gap_top=.40 if identifier in ('nilo','sola') else .24 if identifier in ('oren','etta') else .07
        open_coat(g,'Individual_outer_workwear',coat,'RoleOuter',.018 if identifier in ('beni','zela','rumi') else .07,gap_top)
        for sign in (-1,1):
            ribbon('Tailored_folded_collar',[(sign*.075,1.355,.078),(sign*.13,1.29,.140),(sign*.063,1.225,.163)],.039 if identifier=='sola' else .047,'RoleOuter')
            if identifier!='sola':
                block('Constructed_chest_pocket',(sign*.12,1.20,.161),(.106,.095,.016),'RoleOuter',bevel=.010)
                block('Overlapping_pocket_flap',(sign*.12,1.247,.172),(.11,.030,.012),'RoleOuter',bevel=.006)
                ell('Chest_pocket_snap',(sign*.12,1.244,.181),(.005,.005,.003),'Brass','Chest',12,6)
            else:
                ribbon('Waistcoat_lower_point',[(sign*.015,1.00,.141),(sign*.09,.94,.145),(sign*.16,1.00,.132)],.036,'RoleOuter')
        if identifier in ('beni','zela','rumi'):
            tube('Front_zipper',[(0,.97,.153),(0,1.12,.166),(0,1.30,.145)],.003,'Metal','Chest',8)
            block('Zip_pull',(0,1.27,.159),(.014,.031,.009),'Brass','Chest',.003)
        else:
            for h in (1.01,1.09,1.17,1.25):ell('Garment_button',(.014,h,.163),(.0055,.0055,.003),'Brass','Chest',12,6)
    if identifier in ('pavi','kavi','daro'):
        # A fitted bib and a curved, thick skirt remain legible from the side.
        mat='RoleOuter' if identifier=='daro' else 'RoleApron'
        vv=[(-.145,.96,.148),(.145,.96,.148),(.151,1.23,.159),(.112,1.29,.145),(-.112,1.29,.145),(-.151,1.23,.159)]
        obj=mesh('Role_apron_or_overall_bib',vv,[(0,1,2,3,4,5)],mat,'Chest');thick(obj,.006)
        for sign in (-1,1):
            ribbon('Bib_loadbearing_strap',[(sign*.11,1.27,.165),(sign*.16,1.36,.065),(-sign*.14,1.22,-.151),(-sign*.14,.97,-.14)],.039,mat)
            block('Overall_strap_fitting',(sign*.11,1.271,.177),(.037,.036,.011),'Metal','Chest',.005)
        block('Bib_functional_pocket',(0,1.16,.174),(.20,.10,.018),mat,'Chest',.008)
        if identifier in ('pavi','kavi'):
            vv=[];ff=[];uv=[];weights=[];segments=26
            for i,(h,rx,rz) in enumerate(((1.00,.185,.145),(.90,.207,.153),(.78,.23,.159),(.64,.242,.168),(.53 if identifier=='kavi' else .49,.243,.17))):
                for j in range(segments):
                    a=-1.48+j/(segments-1)*2.96;wr=.004*math.sin(a*6+i)
                    vv.append((math.sin(a)*(rx+wr),h,math.cos(a)*(rz+wr)+.028));uv.append((j/(segments-1),i/4));weights.append(g['torso_weight'](h))
            for i in range(4):
                for j in range(segments-1):ff.append((i*segments+j,i*segments+j+1,(i+1)*segments+j+1,(i+1)*segments+j))
            obj=mesh('Long_volumetric_work_apron',vv,ff,mat,weights=weights,uv=uv);thick(obj,.005)
            if identifier=='kavi':
                for x in (-.13,-.045,.045,.13):block('Apron_tool_compartment',(x,.73,.197),(.065,.15,.018),mat,'Hips',.007)
            else:
                ribbon('Waterproof_waist_tie',[(-.20,.98,.07),(0,.98,-.15),(.20,.98,.07)],.036,mat,'Hips')
    for sign,label in ((1,'L'),(-1,'R')):
        def legweight(h):
            t=max(0,min(1,(h-.49)/.14));return {'Thigh_'+label:t,'Shin_'+label:1-t}
        legwide={'beni':1.16,'pavi':1.06,'oren':1.05,'nilo':.95,'sola':.90}.get(identifier,1)
        bottom=.17 if identifier=='sola' else .27 if identifier=='pavi' else .24
        points=[(sign*.115,bottom,0,.078*legwide,.082),(sign*.115,.31,0,.09*legwide,.092),(sign*.118,.38,.004,.092*legwide,.094),
                (sign*.12,.47,.003,.102*legwide,.101),(sign*.117,.55,0,.098*legwide,.102),(sign*.113,.65,0,.112*legwide,.114),
                (sign*.108,.76,0,.121*legwide,.122),(sign*.105,.87,0,.123*legwide,.127),(sign*.105,.95,0,.12*legwide,.127)]
        if identifier=='sola':points=[(x,h,d,rx*.96,rz*.91) for x,h,d,rx,rz in points]
        loft('Role_tailored_trouser_'+label,points,'Pants',segments=36,weight_func=legweight)
        if identifier!='sola':
            for h in (.32,.44,.64):tube('Soft_trouser_fold',[(sign*.115-.064,h,.081),(sign*.115,h+.010,.105),(sign*.115+.062,h-.004,.079)],.003,'Pants','Shin_'+label if h<.55 else 'Thigh_'+label,8)
        if identifier in ('oren','zela','nilo','etta','rumi','kavi','beni','daro'):
            block('Cargo_side_pocket',(sign*.205,.76,.062),(.074,.16,.078),'Pants','Thigh_'+label,.012)
            block('Cargo_pocket_flap',(sign*.205,.833,.108),(.081,.039,.013),'Pants','Thigh_'+label,.007)
        if identifier in ('zela','etta'):
            block('Rounded_safety_kneepad',(sign*.12,.54,.117),(.145,.17,.038),'Metal','Shin_'+label,.026)
            for x in (-.05,.05):ell('Kneepad_rivet',(sign*.12+x,.598,.141),(.005,.005,.003),'Brass','Shin_'+label,10,6)
            loft('Kneepad_wrap_strap',[(sign*.12,.50,0,.11,.11),(sign*.12,.52,0,.112,.112)],'Leather','Shin_'+label)
        sleeve_material='RoleOuter' if identifier in ('oren','zela','etta','beni') else 'RoleInner'
        rolled=identifier in ('oren','etta','kavi','sola','daro','nilo')
        end=1.075 if rolled else .955
        pts=[(sign*.185,1.29,0),(sign*.218,1.29,0),(sign*.255,1.24,0),(sign*.289,1.17,0),(sign*.315,1.10,0)]
        radii=[.105*broad,.104*broad,.091*broad,.082*broad,.075*broad]
        if not rolled:pts.extend([(sign*.35,1.01,.002),(sign*.388,.955,.005)]);radii.extend([.065,.056])
        sleeve(g,label,pts,radii,sleeve_material)
        cuff_y=1.09 if rolled else .974;cuff_x=sign*(.317 if rolled else .382)
        sleeve(g,label,[(cuff_x,cuff_y+.022,.002),(cuff_x+sign*.009,cuff_y-.021,.003)],[.084 if rolled else .061,.085 if rolled else .061],sleeve_material)
        if rolled:
            sleeve(g,label,[(sign*.317,1.09,.001),(sign*.35,1.01,.002),(sign*.39,.935,.005)],[.060,.056,.049],'RoleGlove' if identifier in ('pavi','zela') else 'Skin')
        if identifier=='rumi':sleeve(g,label,[(sign*.32,1.10,.002),(sign*.35,1.015,.003),(sign*.385,.956,.004)],[.074,.064,.057],'RoleInner')
        if identifier not in ('kavi','sola'):
            top=1.08 if identifier in ('pavi','zela') else .974
            loft('Role_glove_gauntlet',[(sign*.387,.913,.005,.056,.056),(sign*.367 if top>1 else sign*.385,top,.005,.068 if top>1 else .061,.063 if top>1 else .059)],'RoleGlove','Forearm_'+label)
        if identifier=='nilo':block('Navy_elbow_patch',(sign*.326,1.095,-.054),(.105,.13,.026),'RoleOuter','Forearm_'+label,.021)
        if identifier=='sola':
            loft('Visible_ankle',[(sign*.115,.07,.006,.06,.062),(sign*.115,.24,0,.061,.062)],'Skin','Shin_'+label)
            loft('Leather_loafer',[(sign*.115,.035,.06,.088,.146),(sign*.115,.07,.055,.09,.15),(sign*.115,.115,.025,.071,.104)],'RoleBoot','Foot_'+label,segments=36)
            block('Loafer_snaffle',(sign*.115,.102,.13),(.105,.018,.012),'Brass','Foot_'+label,.004)
            loft('Loafer_sole',[(sign*.115,.018,.06,.092,.15),(sign*.115,.04,.06,.092,.15)],'RoleRubber','Foot_'+label)
        elif identifier=='pavi':
            loft('Coral_waterproof_boot',[(sign*.115,.04,.055,.105,.174),(sign*.115,.09,.055,.108,.176),(sign*.115,.15,.015,.09,.12),
                (sign*.115,.27,-.005,.083,.088),(sign*.115,.37,-.005,.087,.091)],'RoleBoot','Foot_'+label,segments=36)
            loft('Rubber_boot_rolled_rim',[(sign*.115,.35,-.005,.091,.095),(sign*.115,.377,-.005,.094,.098)],'RoleBoot','Foot_'+label)
            loft('Boot_outsole',[(sign*.115,.015,.055,.112,.181),(sign*.115,.048,.055,.112,.181)],'RoleRubber','Foot_'+label)
        else:
            loft('Heavy_work_boot',[(sign*.115,.05,.062,.102,.174),(sign*.115,.095,.062,.106,.177),(sign*.115,.15,.029,.090,.129),
                (sign*.115,.22,-.004,.079,.091),(sign*.115,.286,-.004,.082,.093)],'RoleBoot','Foot_'+label,segments=36)
            loft('Boot_padded_collar',[(sign*.115,.263,-.004,.087,.098),(sign*.115,.293,-.004,.089,.100)],'Leather','Foot_'+label)
            loft('Boot_stitched_welt',[(sign*.115,.039,.062,.111,.180),(sign*.115,.060,.062,.112,.182)],'Leather','Foot_'+label)
            loft('Boot_rubber_outsole',[(sign*.115,.015,.062,.114,.184),(sign*.115,.042,.062,.114,.184)],'RoleRubber','Foot_'+label)
            for k in range(5):
                h=.132+k*.031;d=.159-k*.017
                for side in (-1,1):ell('Metal_boot_eyelet',(sign*.115+side*.045,h,d-.007),(.006,.006,.003),'Brass','Foot_'+label,12,6)
                for side in (-1,1):tube('Contrasting_boot_lace',[(sign*.115+side*.045,h,d-.006),(sign*.115,h+.011,d+.008),(sign*.115-side*.045,h+.024,d-.013)],.0032,'RoleLace','Foot_'+label,6)
    loft('Layered_utility_belt',[(0,.94,0,.205*broad,.149),(0,.995,0,.202*broad,.148)],'Leather','Hips',segments=40)
    tube('Rectangular_belt_buckle',[(-.039,.947,.158),(.039,.947,.158),(.039,.992,.158),(-.039,.992,.158),(-.039,.947,.158)],.0055,'Brass','Hips',8)
    for sign in ((-1,) if identifier=='sola' else (-1,1)):
        block('Asymmetrical_hip_pouch',(sign*.22,.86,.050),(.112 if identifier!='sola' else .135,.17,.080),'Leather','Hips',.017)
        block('Hip_pouch_flap',(sign*.22,.933,.098),(.118 if identifier!='sola' else .14,.060,.019),'Leather','Hips',.011)
        ell('Pouch_clasp',(sign*.22,.914,.111),(.007,.008,.003),'Brass','Hips',12,6)

def _helmet(g):
    if not g['current'].get('helmet'):return
    mesh,tube,ell,block=[g[n] for n in ('mesh','tube','ellipsoid','block')]
    sectors=48;rings=15;vv=[];ff=[];uv=[]
    for i in range(rings+1):
        phi=i/rings*math.pi/2
        for j in range(sectors):
            a=j/sectors*math.tau;vv.append((math.sin(phi)*math.sin(a)*.224,1.815+math.cos(phi)**.78*.213,math.sin(phi)*math.cos(a)*.202-.018));uv.append((j/sectors,i/rings))
    for i in range(rings):
        for j in range(sectors):ff.append((i*sectors+j,i*sectors+(j+1)%sectors,(i+1)*sectors+(j+1)%sectors,(i+1)*sectors+j))
    obj=mesh('Moulded_role_helmet',vv,ff,'Helmet','Head',uv=uv);thick(obj,.006)
    for a in (-.85,0,.85):
        tube('Helmet_moulded_rib',[(math.sin(p)*math.sin(a)*.227,1.817+math.cos(p)**.78*.216,math.sin(p)*math.cos(a)*.205-.018) for p in [k/23*1.48 for k in range(24)]],.0034,'Helmet','Head',8)
    for h,rx,rf,rb in ((1.815,.228,.209,.209),(1.799,.26,.286,.226)):
        tube('Protective_helmet_brim',[(math.sin(a)*rx,h,math.cos(a)*(rf if math.cos(a)>0 else rb)-.018) for a in [k/64*math.tau for k in range(65)]],.010,'Helmet','Head',10)
    tube('Headlamp_strap',[(math.sin(a)*.223,1.858,math.cos(a)*.201-.018) for a in [k/64*math.tau for k in range(65)]],.014,'Leather','Head',10)
    for sign in (-1,1):block('Headlamp_strap_fitting',(sign*.196,1.86,.103),(.037,.036,.011),'Brass','Head',.004)
    block('Headlamp_mount',(0,1.907,.18),(.109,.108,.031),'Iron','Head',.014)
    tube('Headlamp_housing',[(0,1.908,.19),(0,1.908,.236)],.052,'Iron','Head',32)
    tube('Headlamp_bezel',[(0,1.908,.232),(0,1.908,.249)],[.049,.044],'Metal','Head',32)
    ell('Headlamp_glass',(0,1.908,.253),(.039,.039,.007),'Lamp','Head',28,12)

def _facial_hair(g,mouth):
    identifier=g['current_id'];head=next((o for o in g['objects'] if o.name.startswith('Anatomical_face')),None)
    if identifier not in ('oren','kavi','beni','daro') or head is None:return
    factor=g['current'].get('height',1);vv=[(v.co.x/factor,v.co.z/factor,v.co.y/factor) for v in head.data.vertices];ff=[]
    wide=identifier in ('oren','kavi')
    for poly in head.data.polygons:
        if all(1.44<vv[i][1]<(mouth.y-.010 if abs(vv[i][0])<.065 else mouth.y+.038) and vv[i][2]>.035 and (wide or abs(vv[i][0])<.067) for i in poly.vertices):ff.append(tuple(poly.vertices))
    used=sorted(set(i for face in ff for i in face));mapping={v:i for i,v in enumerate(used)}
    if used:g['mesh']('Reference_shaped_beard',[(vv[i][0],vv[i][1],vv[i][2]+.007) for i in used],[tuple(mapping[i] for i in face) for face in ff],'Hair','Head')
    if wide:g['ellipsoid']('Full_beard_chin',(0,mouth.y-.068,.12),(.102 if identifier=='oren' else .09,.062,.050),'Hair','Head',28,16)
    elif identifier=='beni':g['ellipsoid']('Small_friendly_goatee',(0,mouth.y-.043,.166),(.046,.028,.014),'Hair','Head',20,12)
    else:g['ellipsoid']('Planner_chin_beard',(0,mouth.y-.057,.137),(.067,.043,.027),'Hair','Head',24,12)
    g['tube']('Shaped_moustache',[(-.058,mouth.y+.016,mouth.z+.008),(-.029,mouth.y+.024,mouth.z+.016),(0,mouth.y+.017,mouth.z+.017),(.029,mouth.y+.024,mouth.z+.016),(.058,mouth.y+.016,mouth.z+.008)], [.003,.011,.008,.011,.003] if wide else [.002,.007,.005,.007,.002],'Hair','Head',10)

def _eyewear(g,eyes):
    identifier=g['current_id']
    if identifier not in ('pavi','etta','daro'):return
    tube,ell=g['tube'],g['ellipsoid'];round_frame=identifier=='pavi';mat='Accent' if identifier=='etta' else 'Metal' if round_frame else 'Iron'
    for eye in eyes:
        z=eye.z+.048;rx=.063 if round_frame else .058;ry=.058 if round_frame else .039
        if round_frame:points=[(eye.x+math.sin(a)*rx,eye.y+math.cos(a)*ry,z) for a in [k/48*math.tau for k in range(49)]]
        else:
            points=[]
            for k in range(65):
                a=k/64*math.tau;s,c=math.sin(a),math.cos(a)
                points.append((eye.x+math.copysign(abs(s)**.48,s)*rx,eye.y+math.copysign(abs(c)**.48,c)*ry,z))
        tube('Distinctive_protective_frame',points,.007 if round_frame else .0055,mat,'Head',10)
        ell('Actual_clear_lens',(eye.x,eye.y,z+.001),(rx-.007,ry-.007,.003),'Glass','Head',32,14)
        sign=1 if eye.x>0 else -1
        tube('Glasses_temple',[(eye.x+sign*rx,eye.y,z),(sign*.185,eye.y+.005,.10),(sign*.192,eye.y,-.025)],.0045,mat,'Head',8)
    a,b=eyes;tube('Eyewear_bridge',[(a.x-.043,a.y+.01,a.z+.049),(0,a.y+.014,a.z+.051),(b.x+.043,b.y+.01,b.z+.049)],.0045,mat,'Head',8)

def create_role_hair(g,eyes,mouth):
    identifier=g['current_id'];ell,tube=g['ellipsoid'],g['tube']
    ell('Fitted_individual_hair_mass',(0,1.74,-.035),(.194,.170,.171),'Hair','Head',36,20)
    if identifier=='etta':
        ell('Gathered_textured_hair_puff',(.025,1.83,-.14),(.238,.245,.175),'Hair','Head',32,20)
        rng=random.Random(186)
        for k in range(120):
            yy=1-2*(k+.5)/120;radius=math.sqrt(max(0,1-yy*yy));a=k*2.399963
            center=Vector((.025+math.cos(a)*radius*.236,1.83+yy*.244,-.14+math.sin(a)*radius*.174))
            points=[tuple(center+Vector((math.sin(t)*.018,math.cos(t)*.018,math.sin(t*2)*.009))) for t in [i/17*math.tau*1.35 for i in range(18)]]
            tube('Individual_afro_curl',points,.0075+rng.random()*.002,'Hair','Head',7)
        # A cloth band and small lamp preserve the roster; there is no hardhat.
        points=[(math.sin(a)*.202,1.76+math.cos(a)*.165,.055) for a in [(-1.45+k/48*2.9) for k in range(49)]]
        tube('Violet_hairband',points,.016,'Accent','Head',10)
        g['block']('Headband_lamp_mount',(.065,1.955,.08),(.07,.065,.025),'Iron','Head',.008)
        tube('Headband_lamp_body',[(.065,1.955,.083),(.065,1.955,.115)],.031,'Metal','Head',24)
        ell('Headband_lamp_lens',(.065,1.955,.119),(.024,.024,.004),'Glass','Head',20,10)
    else:
        length={'zela':.35,'rumi':.20,'sola':.25,'kavi':.19,'oren':.13,'nilo':.13,'pavi':.12,'beni':.10,'daro':.14}[identifier]
        waved=identifier in ('zela','rumi','sola','kavi')
        # Sweep broad, tapered volumes from a side part. Curves are deliberately
        # not a row of equal crown ribs and do not turn Sola's bob into a bun.
        for j in range(6):
            x=-.17+j*.028
            hair_lock(g,'Side_parted_forehead_lock',[(x,1.84,.115),(x+.025,1.875,.205),(x+.12,1.805,.220),(x+.145,1.73,.14)],.035 if waved else .025,.65)
        if not g['current'].get('helmet'):
            for j in range(8):
                offset=j*.022
                hair_lock(g,'Wavy_uncovered_crown',[(-.17+offset,1.80,.09),(-.16+offset,1.995,.10),(.10+offset*.4,1.945,-.045),(.18,1.73,-.10-offset*.35)],.046,.78)
        for sign in (-1,1):
            for j in range(5 if waved else 3):
                d=.055-j*.036;x=sign*(.178+j*.010)
                hair_lock(g,'Shaped_side_hair',[(x,1.79,d),(x+sign*.065,1.70,d+.035),(x-sign*.016,1.53,d-.02),(x+sign*.018,1.79-length,d+.014)],.041 if waved else .023,.82)
        if identifier=='zela':
            for j in range(6):
                x=-.15+j*.06
                hair_lock(g,'Shoulder_red_waves',[(x,1.78,-.12),(x+.08,1.64,-.24),(x-.07,1.50,-.23),(x+.025,1.43,-.13)],.045,.86)
    _facial_hair(g,mouth);_helmet(g);_eyewear(g,eyes)

def create_role_details(g):
    identifier=g['current_id'];block,tube,ribbon,ell=[g[n] for n in ('block','tube','ribbon','ellipsoid')]
    if identifier in ('oren','zela','beni'):
        if identifier=='oren':
            block('Geologist_backpack',(0,1.17,-.185),(.29,.32,.13),'Leather','Chest',.028)
            block('Backpack_flap',(0,1.32,-.205),(.30,.12,.15),'Leather','Chest',.021)
            for x in (-.085,.085):block('Backpack_outer_pocket',(x,1.15,-.26),(.10,.12,.035),'Leather','Chest',.013)
        elif identifier=='zela':
            block('Excavation_backpack',(0,1.20,-.20),(.28,.32,.16),'Iron','Chest',.028)
            block('Painted_backpack_panel',(0,1.22,-.294),(.22,.23,.032),'Helmet','Chest',.017)
            for sign in (-1,1):tube('Compact_drill_air_tank',[(sign*.16,1.07,-.23),(sign*.16,1.34,-.23)],.047,'Brass','Chest',24)
            tube('Flexible_drill_hose',curve([(.13,1.33,-.25),(.34,1.47,-.13),(.41,1.20,.08),(.39,.93,.12)],40),.016,'Iron','Chest',12)
        else:
            # Only the compact shoulder rig visible in the roster is retained.
            block('Delivery_shoulder_rig',(0,1.23,-.16),(.24,.26,.09),'Leather','Chest',.021)
        for sign in (-1,1):
            ribbon('Functional_shoulder_strap',[(sign*.12,.99,.153),(sign*.15,1.22,.167),(sign*.17,1.36,.075),(sign*.15,1.25,-.18)],.036,'Leather')
            block('Shoulder_adjuster',(sign*.15,1.25,.176),(.034,.042,.012),'Brass','Chest',.005)
    if identifier in ('oren','etta','kavi','daro'):
        for i in range(4):
            x=-.12+i*.08;block('Individual_tool_belt_loop',(x,.919,.176),(.029,.048,.014),'Leather','Hips',.004)
            tube('Individual_belt_tool',[(x,.98,.177),(x,.79,.190)],[.010,.007],'Brass' if identifier=='kavi' else 'Metal','Hips',8)
            if identifier=='kavi':tube('Ring_tool_loop',[(x+math.sin(a)*.024,.79+math.cos(a)*.024,.192) for a in [k/24*math.tau for k in range(25)]],.0045,'Brass','Hips',8)
        if identifier=='etta':
            block('Gem_inspection_case',(.24,.86,.08),(.15,.17,.065),'Leather','Hips',.014)
            for i in range(3):
                for j in range(2):g['ellipsoid']('Case_violet_sample',(.20+i*.035,.81+j*.065,.120),(.013,.019,.007),'Gem','Hips',8,6)
    if identifier=='rumi':
        obj=g['mesh']('Belt_polishing_cloth',[(-.23,.94,.11),(-.13,.94,.12),(-.13,.75,.135),(-.21,.73,.129)],[(0,1,2,3)],'Ivory','Hips');thick(obj,.003)
    if identifier=='sola':
        diamond(g,'Diamond_waistcoat_badge',(-.08,1.255,.171),.020,'Chest','Body')

def diamond(g,name,at,radius,bone='Hand_L',section='RoleProp',mat='Gem'):
    x,h,d=at;vv=[];segments=8
    for height,r in ((h+radius*.55,radius*.55),(h,radius),(h-radius*.88,0)):
        for k in range(segments):
            a=k/segments*math.tau;vv.append((x+math.sin(a)*r,height,d+math.cos(a)*r))
    ff=[tuple(range(segments-1,-1,-1))]
    for ring in range(2):
        for k in range(segments):ff.append((ring*segments+k,ring*segments+(k+1)%segments,(ring+1)*segments+(k+1)%segments,(ring+1)*segments+k))
    obj=g['mesh'](name,vv,ff,mat,bone,section=section)
    for poly in obj.data.polygons:poly.use_smooth=False
    return obj

def _rotate(g,objects,pivot,degrees):
    center=g['xyz'](pivot);rotation=Matrix.Rotation(math.radians(degrees),3,'X')
    for obj in objects:
        for vert in obj.data.vertices:vert.co=center+rotation@(vert.co-center)

def create_role_props(g):
    identifier=g['current_id'];mesh,tube,block,ell=[g[n] for n in ('mesh','tube','block','ellipsoid')]
    # Retain the authoritative mining and carry meshes generated by the common
    # helper; replace only the role presentation equipment.
    g['props']()
    for obj in list(g['objects']):
        if obj.get('section')=='RoleProp':g['objects'].remove(obj);bpy.data.objects.remove(obj,do_unlink=True)
    section='RoleProp';left=(.395,.83,.065);right=(-.395,.83,.065)
    if identifier=='oren':
        x,h,d=left;obj=ell('Raised_rock_sample',(x,h,d),(.061,.054,.060),'Ore','Hand_L',10,7,section)
        for poly in obj.data.polygons:poly.use_smooth=False
        x,h,d=right
        tube('Inspection_magnifier_handle',[(x,h-.08,d),(x,h+.08,d)],.011,'Leather','Hand_R',12,section)
        tube('Magnifier_round_frame',[(x+math.sin(a)*.052,h+.10+math.cos(a)*.052,d) for a in [k/36*math.tau for k in range(37)]],.0065,'Metal','Hand_R',10,section)
        ell('Transparent_magnifying_lens',(x,h+.10,d),(.046,.046,.004),'Glass','Hand_R',32,14,section)
    elif identifier=='zela':
        x,h,d=right;d+=.06
        tube('Drill_painted_motor',[(x,h+.07,d),(x,h-.09,d)],[.066,.060],'Helmet','Hand_R',32,section)
        for y in (h+.035,h-.04):tube('Drill_metal_band',[(x,y-.010,d),(x,y+.010,d)],.068,'Metal','Hand_R',32,section)
        block('Drill_hand_grip',(x,h,d-.067),(.045,.115,.052),'Iron','Hand_R',.011,section)
        tube('Drill_tapered_chuck',[(x,h-.085,d),(x,h-.16,d)],[.045,.025],'Metal','Hand_R',24,section)
        tube('Safe_downward_drill_bit',[(x,h-.15,d),(x,h-.34,d)],[.018,.003],'Metal','Hand_R',16,section)
        tube('Actual_spiral_bit_flute',[(x+math.sin(a)*.017,h-.16-a/(math.tau*4)*.16,d+math.cos(a)*.017) for a in [k/64*math.tau*4 for k in range(65)]],.005,'Metal','Hand_R',7,section)
    elif identifier=='pavi':
        x,h,d=0,1.12,.37;segments=48;vv=[];ff=[]
        for height,rx,rz in ((h-.075,.17,.12),(h,.27,.185),(h+.012,.267,.183),(h-.057,.164,.114)):
            for k in range(segments):
                a=k/segments*math.tau;vv.append((x+math.sin(a)*rx,height,d+math.cos(a)*rz))
        for ring in range(4):
            for k in range(segments):ff.append((ring*segments+k,ring*segments+(k+1)%segments,((ring+1)%4)*segments+(k+1)%segments,((ring+1)%4)*segments+k))
        ff.append(tuple(3*segments+k for k in range(segments)));mesh('Deep_washing_pan',vv,ff,'Iron','Chest',section=section)
        for sign in (-1,1):tube('Pan_side_grip',[(sign*.245,h,.36),(sign*.303,h,.36),(sign*.303,h,.42),(sign*.245,h,.42)],.010,'Metal','Chest',10,section)
        rng=random.Random(90)
        for k in range(16):
            at=(rng.uniform(-.19,.19),h-.014+rng.random()*.028,d+rng.uniform(-.11,.11))
            if k%3==0:diamond(g,'Washed_pan_gem',at,.027,'Chest',section)
            else:ell('Washed_pan_ore',at,(.027,.021,.024),'Ore','Chest',9,6,section)
    elif identifier=='nilo':
        x,h,d=0,1.12,.36;corners=[(-.29,-.12),(-.20,-.19),(.20,-.19),(.29,-.12),(.29,.12),(.20,.19),(-.20,.19),(-.29,.12)]
        vv=[(xx,h,zz+d) for xx,zz in corners]+[(xx,h+.055,zz+d) for xx,zz in corners]
        ff=[tuple(range(7,-1,-1))]+[(k,(k+1)%8,(k+1)%8+8,k+8) for k in range(8)]
        obj=mesh('Compartment_sorting_tray',vv,ff,'Iron','Chest',section=section);thick(obj,.006)
        for xx in (-.095,.095):block('Tray_long_divider',(xx,h+.026,d),(.012,.05,.34),'Metal','Chest',.003,section)
        block('Tray_cross_divider',(0,h+.026,d),(.53,.05,.012),'Metal','Chest',.003,section)
        mats=('Ore','Gem','Brass','White','Gem','Ore')
        rng=random.Random(19)
        for col in range(3):
            for row in range(2):
                for k in range(4):diamond(g,'Separate_sorted_resource',(-.19+col*.19+rng.uniform(-.04,.04),h+.035,d-.09+row*.18+rng.uniform(-.033,.033)),.026,'Chest',section,mats[col*2+row])
    elif identifier=='etta':
        x,h,d=right;diamond(g,'Raised_violet_inspection_gem',(x,h+.004,d+.024),.032,'Hand_R',section,'Accent')
        x,h,d=left
        for sign in (-1,1):tube('Precision_tweezer_arm',[(x+sign*.007,h-.06,d),(x-.13+sign*.002,h+.11,d+.018)],.0035,'Metal','Hand_L',8,section)
    elif identifier=='rumi':
        # The cloth is shaped around a gem, rather than a zero-thickness square.
        x,h,d=left;vv=[];ff=[];uv=[];n=12
        for row in range(n):
            for col in range(n):
                u=col/(n-1);v=row/(n-1);vv.append((x+(u-.5)*.20,h+.045-v*.20,d+.025*math.sin(u*math.pi*3)+.018*math.sin(v*math.pi)))
                uv.append((u,v))
        for row in range(n-1):
            for col in range(n-1):
                k=row*n+col;ff.append((k,k+1,k+1+n,k+n))
        obj=mesh('Draped_polishing_cloth',vv,ff,'Ivory','Hand_L',uv=uv,section=section);thick(obj,.002)
        diamond(g,'Partly_polished_diamond',(x+.018,h+.025,d+.044),.031,'Hand_L',section)
    elif identifier=='kavi':
        x,h,d=left
        tube('Inspected_gold_ring',[(x+math.sin(a)*.027,h+math.cos(a)*.027,d+.022) for a in [k/40*math.tau for k in range(41)]],.0045,'Brass','Hand_L',10,section)
        diamond(g,'Jeweller_ring_mount',(x,h+.031,d+.022),.013,'Hand_L',section)
        x,h,d=right;tube('Jewellery_precision_handle',[(x,h-.085,d),(x,h+.035,d)],.009,'Leather','Hand_R',10,section)
        tube('Fine_jewellery_point',[(x,h+.034,d),(x,h+.15,d)],[.005,.0015],'Metal','Hand_R',8,section)
    elif identifier=='sola':
        material(g,'PresentationVelvet','28325C',.94);x,h,d=right;created=[];before=len(g['objects'])
        block('Open_box_base',(x,h,d),(.19,.058,.18),'PresentationVelvet','Hand_R',.013,section)
        block('Box_gold_lip',(x,h+.020,d),(.195,.012,.185),'Brass','Hand_R',.005,section)
        block('Velvet_ring_cushion',(x,h+.028,d),(.161,.020,.151),'PresentationVelvet','Hand_R',.008,section)
        block('Open_box_lid',(x,h+.115,d-.085),(.19,.17,.028),'PresentationVelvet','Hand_R',.012,section)
        block('Lid_gold_inlay',(x,h+.115,d-.067),(.16,.14,.005),'Brass','Hand_R',.005,section)
        block('Inner_velvet_lining',(x,h+.115,d-.063),(.147,.127,.006),'PresentationVelvet','Hand_R',.004,section)
        tube('Actual_box_hinge',[(x-.06,h+.035,d-.087),(x+.06,h+.035,d-.087)],.008,'Brass','Hand_R',12,section)
        tube('Presentation_gold_ring',[(x+math.sin(a)*.026,h+.058+math.cos(a)*.026,d) for a in [k/32*math.tau for k in range(33)]],.004,'Brass','Hand_R',8,section)
        diamond(g,'Presented_box_diamond',(x,h+.090,d),.019,'Hand_R',section)
        _rotate(g,g['objects'][before:],(-.395,.91,.005),-80)
    elif identifier=='beni':
        cx,cz=-.60,.04;bone='Hips'
        for sign in (-1,1):
            tube('Grounded_trolley_upright',[(cx+sign*.13,.13,cz),(cx+sign*.13,.94,cz)],.014,'Iron',bone,12,section)
            tube('Trolley_wheel_axle',[(cx+sign*.14-.026,.115,cz),(cx+sign*.14+.026,.115,cz)],.023,'Metal',bone,12,section)
            tube('Trolley_rolling_wheel',[(cx+sign*.14-.026,.115,cz),(cx+sign*.14+.026,.115,cz)],.110,'RoleRubber',bone,32,section)
            tube('Trolley_metal_hub',[(cx+sign*.14-.03,.115,cz),(cx+sign*.14+.03,.115,cz)],.043,'Metal',bone,20,section)
        tube('Trolley_grip',[(cx-.13,.94,cz),(cx+.13,.94,cz)],.021,'RoleRubber',bone,14,section)
        block('Trolley_ground_platform',(cx,.16,cz+.13),(.35,.045,.36),'Iron',bone,.008,section)
        for height,width,depth in ((.35,.34,.32),(.64,.32,.30)):
            block('Protective_delivery_case',(cx,height,cz+.13),(width,.27,depth),'Metal',bone,.015,section)
            for y in (height-.12,height+.12):block('Amber_case_edge',(cx,y,cz+.13),(width+.015,.035,depth+.015),'Helmet',bone,.006,section)
            for sign in (-1,1):block('Case_corner_guard',(cx+sign*(width/2-.02),height,cz+.13+depth/2+.008),(.037,.25,.018),'Helmet',bone,.005,section)
            for sign in (-1,1):block('Delivery_case_latch',(cx+sign*.065,height+.07,cz+.13+depth/2+.018),(.035,.043,.014),'Brass',bone,.004,section)
            diamond(g,'Delivery_diamond_emblem',(cx,height,cz+.13+depth/2+.024),.027,bone,section,'White')
    elif identifier=='daro':
        x,h,d=left;axis=Vector((.12,.22,0));start=Vector((x-.07,h-.10,d));end=start+axis
        # Open paper roll with visible wall thickness and dark inner bore.
        vec=(end-start).normalized();side=vec.cross(Vector((0,0,1))).normalized();other=vec.cross(side).normalized();vv=[];ff=[];segments=32
        for center,r in ((start,.043),(end,.043),(end,.034),(start,.034)):
            for k in range(segments):
                a=k/segments*math.tau;vv.append(tuple(center+side*math.sin(a)*r+other*math.cos(a)*r))
        for ring in range(4):
            for k in range(segments):ff.append((ring*segments+k,ring*segments+(k+1)%segments,((ring+1)%4)*segments+(k+1)%segments,((ring+1)%4)*segments+k))
        mesh('Hollow_turquoise_blueprint_roll',vv,ff,'Plan','Hand_L',section=section)
        x,h,d=right;before=len(g['objects'])
        block('Dark_planning_board',(x,h+.01,d),(.19,.25,.022),'Iron','Hand_R',.008,section)
        block('Planning_board_clip',(x,h+.12,d+.016),(.07,.022,.012),'Metal','Hand_R',.004,section)
        for i in range(4):tube('Plan_schematic_line',[(x-.07,h-.075+i*.044,d+.013),(x+.060,h-.075+i*.044,d+.013)],.0009,'White','Hand_R',5,section)
        for i in range(3):tube('Plan_schematic_line',[(x-.06+i*.055,h-.08,d+.013),(x-.06+i*.055,h+.09,d+.013)],.0009,'White','Hand_R',5,section)
        _rotate(g,g['objects'][before:],(-.395,.91,.005),-45)

def apply_role_hand_pose(g):
    """Bind the anatomical finger chains to the role grips in the Idle clip."""
    rig=g['rig'];identifier=g['current_id'];action=bpy.data.actions.get('Idle')
    if action is None:return
    rig.animation_data.action=action
    frames=list(range(1,g['CLIPS']['Idle']+1,3))
    if frames[-1]!=g['CLIPS']['Idle']:frames.append(g['CLIPS']['Idle'])
    for frame in frames:
        g['bpy'].context.scene.frame_set(frame)
        for side in ('L','R'):
            grip=.72 if identifier not in ('sola','kavi','etta') else .32
            for digit in ('Index','Middle','Ring','Pinky'):
                for joint in range(1,4):
                    bone=rig.pose.bones.get(digit+str(joint)+'_'+side)
                    if bone:
                        bone.rotation_euler.x=grip*(.7 if joint==1 else 1)
                        bone.keyframe_insert(data_path='rotation_euler',frame=frame,group=bone.name)
    g['bpy'].context.scene.frame_set(1)
