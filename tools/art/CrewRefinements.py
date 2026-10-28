"""Authored garment, hair, helmet and equipment revisions for Tavi and Mira.

The supplied roster controls identity. These are 3D reconstructions; no likeness
percentage is inferred from mesh complexity or successful export.
"""
import bpy,math,random
from mathutils import Vector,Matrix

def refine_materials(g):
    m=g['material'];c=g['current']
    m('Glove','504335',.79,texture=g['LEATHER'],normal=g['LEATHER_N'])
    m('Stitch','BBA57C',.86)
    m('Rubber','242B2C',.81)
    m('Ore','8C9294',.88)
    if c.get('helmet'):m('Helmet',c['helmet'],.45,.09)
    m('Hair',c['hair'],.77)
    m('Glass','A8CBD1',.12,alpha=.22)
    m('Lamp','F4DE9D',.24,emission=.12)
    m('Skin',c['skin'],.66)
    m('Lip','B87563',.65)
    m('Iris','623B23',.27)

def curve(points,count=30):
    a,b,c,d=[Vector(p) for p in points]
    return [tuple((1-t)**3*a+3*(1-t)**2*t*b+3*(1-t)*t*t*c+t**3*d) for t in [k/(count-1) for k in range(count)]]

def sleeve(g,label,points,radii,mat):
    # Cross-sections are normal to the sleeve's centre line, not horizontal discs.
    verts=[];weights=[];uv=[];faces=[];segments=28
    for i,(p,r) in enumerate(zip(points,radii)):
        p=Vector(p);axis=(Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])).normalized()
        side=axis.cross(Vector((0,0,1))).normalized();front=axis.cross(side).normalized()
        for j in range(segments):
            a=j/segments*math.tau
            fold=(math.sin(a*5+i*1.6)+math.sin(a*3-i*.7))*.0023
            q=p+side*(math.cos(a)*(r+fold))+front*(math.sin(a)*(r*.94+fold))
            verts.append(tuple(q));uv.append((j/segments,i/(len(points)-1)))
            t=max(0,min(1,(q.y-1.06)/.09))
            chest=max(0,min(.85,(q.y-1.24)/.09))
            weights.append({'Chest':chest,'UpperArm_'+label:t*(1-chest),'Forearm_'+label:(1-t)*(1-chest)})
    for i in range(len(points)-1):
        for j in range(segments):faces.append((i*segments+j,i*segments+(j+1)%segments,(i+1)*segments+(j+1)%segments,(i+1)*segments+j))
    return g['mesh']('Sculpted_sleeve_'+label,verts,faces,mat,weights=weights,uv=uv)

def stitch(g,name,points,bone='Chest',mat='Stitch'):
    # Individually spaced thread segments follow garment construction.
    for a,b in zip(points,points[1:]):
        a,b=Vector(a),Vector(b);delta=b-a;steps=max(1,int(delta.length/.012))
        for i in range(steps):
            start=a+delta*((i+.12)/steps);end=a+delta*((i+.62)/steps)
            g['tube'](name,[tuple(start),tuple(end)],.00065,mat,bone,5)

def refined_clothes(g):
    c=g['current'];wide=c['width'];loft=g['loft'];mesh=g['mesh'];tube=g['tube'];block=g['block'];ribbon=g['ribbon'];ell=g['ellipsoid']
    tavi=g['current_id']=='tavi'
    torso=[(0,.91,0,.197*wide,.132),(0,.95,0,.196*wide,.133),(0,1.00,0,.182*wide,.129),(0,1.06,0,.183*wide,.132),
           (0,1.13,0,.205*wide,.145),(0,1.21,0,.227*wide,.149),(0,1.27,0,.240*wide,.139),(0,1.31,0,.223*wide,.124),(0,1.35,0,.113,.085)]
    loft('Woven_work_shirt',torso,'Shirt',segments=40,weight_func=g['torso_weight'])
    loft('Covered_neck',[(0,1.33,0,.075,.066),(0,1.48,0,.069,.062)],'Skin','Neck')
    if tavi:
        # Open jacket with separately constructed front edges and an exposed cream shirt.
        verts=[];uv=[];weights=[];faces=[];segments=40
        for i,(x,h,d,rx,rz) in enumerate(torso):
            gap=math.radians(7 if h<1.08 else 9+(h-1.08)*120)
            for j in range(segments):
                a=gap+(math.tau-gap*2)*j/(segments-1)
                fold=.003*math.sin(a*7+i*1.8)
                verts.append((math.sin(a)*(rx+.008+fold),h,math.cos(a)*(rz+.009+fold)))
                uv.append((j/(segments-1),i/(len(torso)-1)));weights.append(g['torso_weight'](h))
        for i in range(len(torso)-1):
            for j in range(segments-1):faces.append((i*segments+j,i*segments+j+1,(i+1)*segments+j+1,(i+1)*segments+j))
        coat=mesh('Open_teal_field_jacket',verts,faces,'Top',weights=weights,uv=uv)
        bpy.context.view_layer.objects.active=coat;coat.select_set(True)
        thick=coat.modifiers.new('Jacket edge thickness','SOLIDIFY');thick.thickness=.004;bpy.ops.object.modifier_apply(modifier=thick.name);coat.select_set(False)
        for sign in (-1,1):
            ribbon('Jacket_folded_collar',[(sign*.08,1.355,.087),(sign*.13,1.30,.135),(sign*.075,1.235,.159)],.040,'Top')
            block('Jacket_patch_pocket',(sign*.13,1.19,.154),(.105,.098,.013),'Top',bevel=.009)
            block('Pocket_overlap_flap',(sign*.13,1.237,.165),(.109,.032,.012),'Top',bevel=.006)
            ell('Pocket_brass_snap',(sign*.13,1.237,.174),(.006,.006,.003),'Brass','Chest',12,6)
            stitch(g,'Pocket_seam',[(sign*.13-.045,1.225,.17),(sign*.13-.045,1.147,.17),(sign*.13+.045,1.147,.17),(sign*.13+.045,1.225,.17)])
        for h in (1.01,1.09,1.17):ell('Jacket_button',(.028,h,.158),(.005,.005,.0025),'Brass','Chest',12,6)
    else:
        # A shaped orange bib, back bib and crossing straps; shirt stays forest green.
        bib=[(-.14,.94,.144),(.14,.94,.144),(.15,1.225,.155),(.11,1.27,.153),(-.11,1.27,.153),(-.15,1.225,.155)]
        mesh('Engineer_bib',bib,[(0,1,2,3,4,5)],'Top','Chest',subdiv=0)
        block('Bib_double_pocket',(0,1.15,.166),(.205,.103,.018),'Top',bevel=.012)
        tube('Bib_pocket_divider',[(0,1.105,.18),(0,1.192,.18)],.001,'Stitch','Chest',6)
        for sign in (-1,1):
            ribbon('Overall_shoulder_strap',[(sign*.11,1.255,.16),(sign*.16,1.35,.07),(-sign*.13,1.25,-.14),(-sign*.14,1.00,-.14)],.044,'Top')
            block('Overall_fitting',(sign*.11,1.255,.170),(.038,.038,.012),'Metal',bevel=.004)
            tube('Fitting_open_center',[(sign*.11-.011,1.246,.178),(sign*.11+.011,1.246,.178),(sign*.11+.011,1.267,.178),(sign*.11-.011,1.267,.178),(sign*.11-.011,1.246,.178)],.003,'Brass','Chest',6)
            stitch(g,'Bib_outline',[(sign*.13,.98,.16),(sign*.139,1.20,.171),(sign*.10,1.25,.171)])
    for sign,label in ((1,'L'),(-1,'R')):
        def legweight(h):
            t=max(0,min(1,(h-.49)/.13));return {'Thigh_'+label:t,'Shin_'+label:1-t}
        rings=[(sign*.11,.26,0,.081,.086),(sign*.11,.29,0,.095,.094),(sign*.115,.33,.007,.091,.091),(sign*.116,.38,.002,.099,.093),
               (sign*.119,.45,.002,.104,.098),(sign*.12,.52,0,.101,.107),(sign*.118,.57,.004,.096,.105),(sign*.116,.63,.003,.112,.113),
               (sign*.112,.72,0,.118,.122),(sign*.105,.84,0,.123,.126),(sign*.105,.95,0,.12,.126)]
        loft('Cargo_trouser_'+label,rings,'Pants',segments=36,weight_func=legweight)
        for h in (.32,.43,.65):
            tube('Trouser_soft_fold',[(sign*.11-.06,h,.08),(sign*.11-.015,h+.009,.10),(sign*.11+.06,h-.004,.08)],.003,'Pants','Shin_'+label if h<.55 else 'Thigh_'+label,8)
        # Outer thigh cargo pocket and an articulated knee panel.
        block('Cargo_pocket_'+label,(sign*.194,.76,.071),(.085,.17,.074),'Pants','Thigh_'+label,.014)
        block('Cargo_flap_'+label,(sign*.194,.836,.114),(.09,.041,.014),'Pants','Thigh_'+label,.008)
        ell('Cargo_snap',(sign*.194,.832,.126),(.005,.005,.003),'Brass','Thigh_'+label,10,6)
        if not tavi:
            block('Rounded_knee_protector',(sign*.12,.54,.112),(.158,.17,.040),'Metal','Shin_'+label,.028)
            block('Knee_inset',(sign*.12,.54,.139),(.112,.123,.014),'Iron','Shin_'+label,.023)
            for x in (-.055,.055):ell('Knee_rivet',(sign*.12+x,.60,.143),(.006,.006,.003),'Brass','Shin_'+label,10,6)
            loft('Knee_safety_strap',[(sign*.12,.50,0,.112,.11),(sign*.12,.52,0,.113,.112)],'Leather','Shin_'+label)
        else:
            vv=[];ff=[];uv=[]
            for row in range(9):
                v=row/4-1
                for col in range(9):
                    u=col/4-1;x=u*.064*(.95-.09*v*v)
                    vv.append((sign*.12+x,.53+v*.075,.108*math.sqrt(max(.1,1-(x/.11)**2))+.006));uv.append((col/8,row/8))
            for row in range(8):
                for col in range(8):k=row*9+col;ff.append((k,k+1,k+10,k+9))
            mesh('Flush_reinforced_knee',vv,ff,'Pants','Shin_'+label,uv=uv)
        end=1.06 if tavi else 1.12
        points=[(sign*.215,1.31,0),(sign*.235,1.275,0),(sign*.26,1.23,0),(sign*.286,1.17,.001),(sign*.31,end,0)]
        ell('Joined_shoulder_seam',(sign*.205,1.29,0),(.091,.089,.099),'Top' if tavi else 'Shirt','Chest',28,18)
        sleeve(g,label,points,[.078,.096,.091,.086,.08],'Top' if tavi else 'Shirt')
        sleeve(g,label,[(sign*.30,end+.025,0),(sign*.315,end-.005,0)],[.086,.088],'Top' if tavi else 'Shirt')
        # Visible forearm fitted between rolled sleeve and leather glove cuff.
        sleeve(g,label,[(sign*.31,end,0),(sign*.35,1.01,0),(sign*.39,.93,.004)],[.061,.056,.049],'Skin')
        loft('Leather_gauntlet',[(sign*.385,.915,.005,.056,.056),(sign*.383,.962,.005,.061,.059)],'Glove','Forearm_'+label)
        block('Glove_wrist_fastener',(sign*.383,.941,.061),(.061,.028,.012),'Leather','Forearm_'+label,.007)
        ell('Glove_wrist_rivet',(sign*.383,.941,.069),(.005,.005,.003),'Brass','Forearm_'+label,10,6)
        # Substantial lace-up boot, welt, sole, toe cap and separate tread blocks.
        loft('Leather_work_boot',[(sign*.115,.052,.065,.103,.171),(sign*.115,.09,.065,.106,.173),(sign*.115,.14,.039,.094,.139),
             (sign*.115,.18,.011,.081,.105),(sign*.115,.28,-.004,.079,.088),(sign*.115,.30,-.004,.083,.09)],'Boots','Foot_'+label,segments=36)
        loft('Boot_top_padding',[(sign*.115,.276,-.004,.085,.094),(sign*.115,.303,-.004,.087,.096)],'Leather','Foot_'+label)
        loft('Stitched_welt',[(sign*.115,.045,.065,.109,.176),(sign*.115,.064,.065,.111,.178)],'Leather','Foot_'+label)
        loft('Rubber_boot_sole',[(sign*.115,.018,.065,.112,.18),(sign*.115,.046,.065,.113,.181)],'Rubber','Foot_'+label)
        for row in range(4):
            for side in (-1,1):block('Sole_tread',(sign*.115+side*.078,.021,-.055+row*.065),(.06,.024,.043),'Rubber','Foot_'+label,.004)
        for k in range(5):
            h=.132+k*.031;d=.166-k*.018
            for side in (-1,1):ell('Lace_eyelet',(sign*.115+side*.044,h,d-.008),(.0065,.0065,.003),'Brass','Foot_'+label,12,6)
            tube('Woven_boot_lace',[(sign*.115-.044,h,d-.006),(sign*.115,h+.01,d+.008),(sign*.115+.044,h+.024,d-.012)],.003,'Accent','Foot_'+label,6)
            tube('Woven_boot_lace',[(sign*.115+.044,h,d-.006),(sign*.115,h+.01,d+.008),(sign*.115-.044,h+.024,d-.012)],.003,'Accent','Foot_'+label,6)
    loft('Leather_utility_belt',[(0,.94,0,.208*wide,.151),(0,.995,0,.205*wide,.148)],'Leather','Hips',segments=40)
    tube('Open_brass_buckle',[(-.04,.945,.158),(.04,.945,.158),(.04,.993,.158),(-.04,.993,.158),(-.04,.945,.158)],.0055,'Brass','Hips',8)
    for x in (-.15,-.09,.09,.15):block('Belt_loop',(x,.971,.147),(.018,.07,.014),'Pants','Hips',.003)
    for sign in (-1,1):
        if tavi and sign==-1:continue
        block('Belt_tool_pouch',(sign*.213,.875,.076),(.11,.15,.077),'Leather','Hips',.016)
        block('Belt_pouch_flap',(sign*.213,.937,.123),(.115,.055,.017),'Leather','Hips',.010)
        ell('Pouch_clasp',(sign*.213,.925,.136),(.008,.008,.003),'Brass','Hips',12,6)

def hair_lock(g,name,controls,width=.033,squash=.8):
    points=curve(controls,28);verts=[];uv=[];faces=[];segments=10
    for i,p in enumerate(points):
        p=Vector(p);axis=(Vector(points[min(i+1,27)])-Vector(points[max(i-1,0)])).normalized()
        side=axis.cross(Vector((0,0,1)))
        if side.length<.01:side=axis.cross(Vector((0,1,0)))
        side.normalize();other=axis.cross(side).normalized();t=i/27;r=width*(.45+.55*math.sin(math.pi*(t*.85+.10)))*(1-t*.83)
        for j in range(segments):
            a=j/segments*math.tau;v=p+side*math.cos(a)*r+other*math.sin(a)*r*squash
            verts.append(tuple(v));uv.append((j/segments,t))
    for i in range(27):
        for j in range(segments):faces.append((i*segments+j,i*segments+(j+1)%segments,(i+1)*segments+(j+1)%segments,(i+1)*segments+j))
    g['mesh'](name,verts,faces,'Hair','Head',uv=uv)

def refined_hair_helmet(g,eyes,mouth):
    tavi=g['current_id']=='tavi';mesh=g['mesh'];tube=g['tube'];ell=g['ellipsoid'];block=g['block']
    ell('Fitted_hair_mass',(0,1.74,-.025),(.193,.166,.167),'Hair',segments=36,rings=20)
    # Distinct swept forehead, curled side locks and tied-back mass.
    for j in range(5):
        x=-.17+j*.035
        hair_lock(g,'Swept_fringe',[(x,1.83,.12),(x+.05,1.82,.235),(x+.12,1.79,.23),(x+.135,1.745,.155)],.032,.58)
    for sign in (-1,1):
        for j in range(4):
            d=.075-j*.045;x=sign*(.183+j*.008)
            pts=[]
            for k in range(40):
                t=k/39;pts.append((x+sign*.024*math.sin(t*math.tau*1.45+j*.7),1.78-t*(.32 if tavi else .27),d+.024*math.cos(t*math.tau*1.45+j*.7)))
            tube('Sculpted_side_curl',pts,[.026*(1-.82*k/39) for k in range(40)],'Hair','Head',12)
    tie=(.04,1.68,-.176) if not tavi else (.055,1.64,-.18)
    ell('Ponytail_mass',(tie[0]+.015,tie[1]-.085,tie[2]-.06),(.105,.135,.08),'Hair',segments=26,rings=18)
    for j in range(10):
        a=j/10*math.tau;x=tie[0]+math.sin(a)*.045;d=tie[2]+math.cos(a)*.045
        hair_lock(g,'Tied_wavy_lock',[(x,tie[1],d),(x+.09,1.55,d-.12),(x-.05,1.45,d-.12),(x+.035,1.38+(j%3)*.025,d-.035)],.037 if tavi else .043,.83)
    tube('Hair_tie',[(tie[0]+math.sin(a)*.064,tie[1]+math.cos(a)*.025,tie[2]) for a in [k/32*math.tau for k in range(33)]],.007,'Leather','Head',8)
    # Moulded protective shell with thickness, shaped brim and raised ribs.
    verts=[];uv=[];faces=[];sectors=56;rings=18
    for i in range(rings+1):
        phi=i/rings*math.pi/2
        for j in range(sectors):
            a=j/sectors*math.tau
            verts.append((math.sin(phi)*math.sin(a)*.228,1.825+max(0,math.cos(phi))**.73*.215,math.sin(phi)*math.cos(a)*.205-.018));uv.append((j/sectors,i/rings))
    for i in range(rings):
        for j in range(sectors):faces.append((i*sectors+j,i*sectors+(j+1)%sectors,(i+1)*sectors+(j+1)%sectors,(i+1)*sectors+j))
    obj=mesh('Moulded_hardhat_shell',verts,faces,'Helmet','Head',uv=uv)
    bpy.context.view_layer.objects.active=obj;obj.select_set(True);mod=obj.modifiers.new('Shell thickness','SOLIDIFY');mod.thickness=.006;bpy.ops.object.modifier_apply(modifier=mod.name);obj.select_set(False)
    for a in (-1.15,-.48,.48,1.15):
        pts=[]
        for k in range(23):
            phi=.12+k/22*1.38;pts.append((math.sin(phi)*math.sin(a)*.23,1.827+math.cos(phi)**.73*.217,math.sin(phi)*math.cos(a)*.207-.018))
        tube('Moulded_cap_rib',pts,.0038,'Helmet','Head',8)
    vv=[];ff=[];uv=[]
    for ring,(rx,rzfront,rzback,h) in enumerate(((.226,.205,.205,1.828),(.264,.292,.228,1.818),(.265,.292,.229,1.805),(.225,.203,.204,1.815))):
        for j in range(sectors):
            a=j/sectors*math.tau;rz=rzfront if math.cos(a)>0 else rzback
            vv.append((math.sin(a)*rx,h,math.cos(a)*rz-.018));uv.append((j/sectors,ring/3))
    for ring in range(4):
        for j in range(sectors):ff.append((ring*sectors+j,ring*sectors+(j+1)%sectors,((ring+1)%4)*sectors+(j+1)%sectors,((ring+1)%4)*sectors+j))
    mesh('Bevelled_protective_brim',vv,ff,'Helmet','Head',uv=uv)
    strap=[(math.sin(a)*.224,1.865,math.cos(a)*.202-.018) for a in [k/64*math.tau for k in range(65)]]
    tube('Headlamp_leather_band',strap,.016,'Leather','Head',10)
    for side in (-1,1):
        block('Band_buckle',(side*.20,1.865,.102),(.040,.043,.012),'Brass','Head',.004)
        for h in (1.852,1.877):ell('Hat_rivet',(side*.20,h,.111),(.004,.004,.002),'Metal','Head',10,6)
    block('Lamp_mount',(0,1.914,.189),(.114,.115,.035),'Iron','Head',.015)
    tube('Lamp_body',[(0,1.916,.19),(0,1.916,.238)],.052,'Iron','Head',36)
    tube('Lamp_bevel_ring',[(0,1.916,.234),(0,1.916,.248)],[.048,.045],'Metal','Head',36)
    ell('Lamp_warm_lens',(0,1.916,.252),(.038,.038,.008),'Lamp',segments=32,rings=16)
    # The original roster, unlike the auxiliary Tavi sheet, shows goggles on the hat.
    if tavi:
        for x in (-.093,.093):
            tube('Goggles_rubber_frame',[(x+math.sin(a)*.047,1.909+math.cos(a)*.030,.188) for a in [k/40*math.tau for k in range(41)]],.007,'Iron','Head',8)
            ell('Goggles_clear_lens',(x,1.909,.19),(.039,.024,.005),'Glass',segments=28,rings=12)

def refined_details(g):
    tube=g['tube'];loft=g['loft'];ribbon=g['ribbon'];block=g['block'];ell=g['ellipsoid']
    if g['current_id']=='tavi':
        verts=[];uv=[];faces=[];segments=48
        rings=((1.35,.105,.112),(1.375,.144,.158),(1.405,.15,.168),(1.44,.132,.145),(1.465,.105,.114),(1.485,.081,.087))
        for row,(h,rx,rz) in enumerate(rings):
            for j in range(segments):
                a=j/segments*math.tau
                verts.append((math.sin(a)*rx,h+.019*math.sin(a+.35)+.003*math.sin(a*4+row),.008+math.cos(a)*rz))
                uv.append((j/segments,row/5))
        for row in range(5):
            for j in range(segments):faces.append((row*segments+j,row*segments+(j+1)%segments,(row+1)*segments+(j+1)%segments,(row+1)*segments+j))
        g['mesh']('Draped_scarf_wrap',verts,faces,'Ivory','Neck',uv=uv)
        ribbon('Overlapping_diagonal_scarf_fold',[(-.12,1.40,.075),(-.07,1.39,.167),(.045,1.445,.175),(.12,1.46,.085)],.061,'Ivory','Neck')
        ribbon('Scarf_back_tail',[(-.075,1.46,-.09),(-.12,1.37,-.13),(-.10,1.25,-.15)],.075,'Ivory')
        ribbon('Asymmetric_satchel_strap',[(-.16,1.35,.11),(-.09,1.23,.17),(.06,1.09,.166),(.22,.94,.12)],.034,'Leather')
        block('Map_satchel',(-.226,.84,.043),(.17,.22,.105),'Leather','Hips',.023)
        block('Overlapping_satchel_flap',(-.226,.923,.103),(.179,.09,.024),'Leather','Hips',.015)
        for x in (-.282,-.17):block('Satchel_clasp',(x,.894,.12),(.026,.034,.012),'Brass','Hips',.005)
        # Survey plate is fitted to the satchel, not a floating extra accessory.
        g['mesh']('Satchel_survey_chart',[(-.28,.79,.101),(-.17,.79,.101),(-.17,.88,.117),(-.28,.88,.117)],[(0,1,2,3)],'Map','Hips',uv=[(0,0),(1,0),(1,1),(0,1)])
        stitch(g,'Satchel_stitches',[(-.30,.75,.101),(-.30,.94,.101),(-.15,.94,.101),(-.15,.75,.101)],'Hips')
    else:
        for x in (-.17,-.11,.12):
            tube('Belt_tool_handle',[(x,.98,.164),(x,.82,.18)],.011,'Metal','Hips',10)
            for sign in (-1,1):tube('Wrench_open_jaw',[(x,.82,.18),(x+sign*.018,.79,.18),(x+sign*.018,.77,.18)],.007,'Metal','Hips',8)
            block('Leather_tool_loop',(x,.925,.183),(.031,.042,.012),'Leather','Hips',.004)

def refined_props(g):
    # Retain the common mining/carry meshes; replace or refine the role equipment.
    g['props']()
    if g['current_id']=='tavi':
        pivot=g['xyz']((.395,.91,.005));rotation=Matrix.Rotation(math.radians(-28),3,'X')
        for obj in g['objects']:
            if obj.get('section')=='RoleProp':
                for vert in obj.data.vertices:
                    q=vert.co-pivot;q.x*=1.2;q.z*=1.18;vert.co=pivot+rotation@q
    else:
        for obj in list(g['objects']):
            if obj.get('section')=='RoleProp':g['objects'].remove(obj);bpy.data.objects.remove(obj,do_unlink=True)
        cart(g)

def cart(g):
    mesh=g['mesh'];tube=g['tube'];block=g['block'];ell=g['ellipsoid'];section='RoleProp';bone='Hips'
    cx=-.58;cz=.06;bottom=.30;top=.72
    # Thick open tapered hopper: inside/outside walls are actual mesh surfaces.
    verts=[]
    for h,w,d in ((bottom,.20,.25),(top,.285,.34),(top,.263,.317),(bottom+.027,.182,.229)):
        verts.extend([(cx-w,h,cz-d),(cx+w,h,cz-d),(cx+w,h,cz+d),(cx-w,h,cz+d)])
    faces=[]
    for a,b in ((0,4),(4,8),(8,12),(12,0)):
        for k in range(4):faces.append((a+k,a+(k+1)%4,b+(k+1)%4,b+k))
    faces.extend([(12,13,14,15),(3,2,1,0)])
    mesh('Engineer_cart_hopper',verts,faces,'Iron',bone,bevel=.007,section=section)
    for sign in (-1,1):
        block('Cart_side_wood_panel',(cx+sign*.244,.51,cz),(.022,.24,.57),'Leather',bone,.008,section)
        for z in (-.27,.27):
            tube('Hopper_corner_strap',[(cx+sign*.195,bottom,cz+z*.90),(cx+sign*.284,top,cz+z*1.23)],.013,'Metal',bone,8,section)
            for h in (.40,.62):ell('Hopper_rivet',(cx+sign*(.22+(h-.4)*.20),h,cz+z*1.13),(.009,.009,.006),'Brass',bone,12,7,section)
    for z in (-.34,.34):tube('Cart_top_rim',[(cx-.285,top,cz+z),(cx+.285,top,cz+z)],.017,'Metal',bone,10,section)
    for sign in (-1,1):tube('Cart_top_rim',[(cx+sign*.285,top,cz-.34),(cx+sign*.285,top,cz+.34)],.017,'Metal',bone,10,section)
    for x in (-.17,.17):block('Cart_chassis_beam',(cx+x,.26,cz),(.055,.075,.62),'Iron',bone,.006,section)
    for z in (-.23,.23):
        tube('Cart_axle',[(cx-.32,.13,cz+z),(cx+.32,.13,cz+z)],.022,'Metal',bone,12,section)
        for sign in (-1,1):
            x=cx+sign*.28
            tube('Cart_rubber_wheel',[(x-.028,.13,cz+z),(x+.028,.13,cz+z)],.115,'Rubber',bone,32,section)
            tube('Cart_wheel_flange',[(x-sign*.037,.13,cz+z),(x-sign*.022,.13,cz+z)],.125,'Iron',bone,32,section)
            tube('Cart_hub',[(x-.035,.13,cz+z),(x+.035,.13,cz+z)],.045,'Metal',bone,20,section)
    rng=random.Random(921)
    for k in range(18):
        x=cx+rng.uniform(-.20,.20);z=cz+rng.uniform(-.25,.25);h=.72+rng.uniform(-.01,.095)
        ell('Cart_rock_load',(x,h,z),(.052+rng.random()*.02,.05,.055),'Ore',bone,9,6,section)
