"""Reference-directed, fully clothed 3D crew reconstruction for Diamond Mine Tycoon.

Uses MakeHuman CC0 facial/hand topology; clothing, hair, equipment, rig, textures
and clips are authored here. It is a reconstruction from the supplied image,
not a claim of recovering its original 3D source or unseen views.
Run Blender 4.5 LTS -b -t 4 -P tools/art/Create-Crew.py.
DMT_CHARACTER_IDS=tavi selects a look-development pass; omitted builds all 12.
"""
import bpy, bmesh, math, json, os, gzip, random, hashlib, sys
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(Path(__file__).resolve().parent))
ART=ROOT/'Assets/_Project/Art/Crew'
SRC=ROOT/'ArtSource/Characters'
CACHE=ROOT/'.cache/character-tools/mpfb'
EVIDENCE=ROOT/'release/screenshots/characters'
for path in (ART/'Models',ART/'Textures',SRC,EVIDENCE):path.mkdir(parents=True,exist_ok=True)
DESIGNS=json.loads((ROOT/'tools/art/character-designs.json').read_text())
IDS=os.environ.get('DMT_CHARACTER_IDS',','.join(DESIGNS)).split(',')
all_materials={}; audits={}; current={}; current_id=''; rig=None; objects=[]; bone_defs={}
random.seed(53493)

def color(h):return tuple(int(h[i:i+2],16)/255 for i in (0,2,4))
def lin(c):return tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in c)
def xyz(p):
    s=current.get('height',1)
    return Vector((p[0]*s,p[2]*s,p[1]*s))

# Preserve CC0 base data
source_data=SRC/'third_party/makehuman'
for relative in ['LICENSE.ASSETS.md','LICENSE.md','src/mpfb/data/3dobjs/base.obj',
    'src/mpfb/data/mesh_metadata/basemesh_vertex_groups.json',
    'src/mpfb/data/rigs/standard/weights.mixamo_unity.json']:
    origin=CACHE/relative; target=source_data/relative
    target.parent.mkdir(parents=True,exist_ok=True); target.write_bytes(origin.read_bytes())
for origin in (CACHE/'src/mpfb/data/targets').rglob('*.gz'):
    target=source_data/origin.relative_to(CACHE);target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(origin.read_bytes())
DATA=source_data/'src/mpfb/data'
BASE=[]; BODY_FACES=[]; GROUPS={}; group=''
for line in (DATA/'3dobjs/base.obj').read_text().splitlines():
    if line.startswith('v '):BASE.append(tuple(map(float,line.split()[1:4])))
    elif line.startswith('g '):group=line[2:]
    elif line.startswith('f '):
        face=[int(part.split('/')[0])-1 for part in line.split()[1:]]
        if group=='body':BODY_FACES.append(face)
        GROUPS.setdefault(group,set()).update(face)
BASE=np.array(BASE,dtype=float)
SOURCE_WEIGHTS=json.loads((DATA/'rigs/standard/weights.mixamo_unity.json').read_text())['weights']

def load_target(relative):
    delta=np.zeros_like(BASE)
    for line in gzip.open(DATA/'targets'/relative,'rt'):
        if not line.strip() or line.startswith('#'):continue
        fields=line.split();delta[int(fields[0])]=[float(x) for x in fields[1:4]]
    return delta
TARGETS={}
def target(name):
    if name not in TARGETS:TARGETS[name]=load_target(name)
    return TARGETS[name]

def save_texture(name,values,normal=False):
    h,w=values.shape[:2]
    rgba=np.ones((h,w,4),np.float32);rgba[:,:,:values.shape[2]]=values
    im=bpy.data.images.new(name,width=w,height=h,alpha=True)
    im.colorspace_settings.name='Non-Color';im.pixels.foreach_set(rgba.ravel())
    im.filepath_raw=str(ART/'Textures'/(name+'.png'));im.file_format='PNG';im.save()
    bpy.data.images.remove(im)
    loaded=bpy.data.images.load(str(ART/'Textures'/(name+'.png')),check_existing=True)
    loaded.colorspace_settings.name='Non-Color' if normal else 'sRGB'
    return loaded

def shared_textures():
    n=512;y,x=np.mgrid[0:n,0:n]/n
    weave=np.sin(x*math.tau*128)*np.sin(y*math.tau*128)
    cloth=np.ones((n,n,3),np.float32)*(.94+weave[:,:,None]*.035)
    fabric=save_texture('Canvas_BaseColor',cloth)
    normal=np.stack((weave*.07,np.sin(x*math.tau*128)*.07,np.ones_like(x)),axis=2)
    normal/=np.linalg.norm(normal,axis=2,keepdims=True)
    fabric_n=save_texture('Canvas_Normal',normal*.5+.5,True)
    noise=np.random.default_rng(53493).random((n,n))
    leather=save_texture('Leather_BaseColor',np.ones((n,n,3))*(.89+noise[:,:,None]*.1))
    normal=np.stack(((noise-.5)*.1,(np.roll(noise,1,0)-.5)*.1,np.ones_like(x)),axis=2)
    normal/=np.linalg.norm(normal,axis=2,keepdims=True)
    leather_n=save_texture('Leather_Normal',normal*.5+.5,True)
    # Map and plan textures
    chart=np.ones((n,n,3))*np.array([.84,.73,.51])
    for k in range(9):
        row=(.1+k*.1+.018*np.sin(x*11+k))*n
        line=np.abs(y*n-row)<.7
        chart[line]=np.array([.51,.43,.28])
    for i in range(55,n-55):
        j=int(n*.48+math.sin(i/n*9)*n*.16)
        chart[max(0,j-1):j+2,i]=[.58,.32,.18]
    chart_im=save_texture('SurveyMap_BaseColor',chart)
    blue=np.ones((n,n,3))*np.array([.12,.44,.59])
    for k in range(45,n-40,42):
        blue[k:k+1,35:n-35]=[.68,.85,.88];blue[35:n-35,k:k+1]=[.68,.85,.88]
    blue_im=save_texture('Blueprint_BaseColor',blue)
    return fabric,fabric_n,leather,leather_n,chart_im,blue_im

FABRIC,FABRIC_N,LEATHER,LEATHER_N,MAP_TEX,PLAN_TEX=shared_textures()
M={}
def material(key,h,rough=.66,metal=0,texture=None,normal=None,emission=0,alpha=1):
    name=current_id+'_'+key
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes=True;m.diffuse_color=(*color(h),alpha)
    m.node_tree.nodes.clear()
    bs=m.node_tree.nodes.new('ShaderNodeBsdfPrincipled');output=m.node_tree.nodes.new('ShaderNodeOutputMaterial')
    m.node_tree.links.new(bs.outputs['BSDF'],output.inputs['Surface'])
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*lin(color(h)),1)
    bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
    bs.inputs['Alpha'].default_value=alpha
    if alpha<1:
        m.surface_render_method='DITHERED';bs.inputs['Transmission Weight'].default_value=.65
    if texture:
        tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=texture
        mix=m.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
        mix.inputs[2].default_value=(*lin(color(h)),1)
        m.node_tree.links.new(tex.outputs['Color'],mix.inputs[1]);m.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
    if normal:
        tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=normal
        nm=m.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.35
        m.node_tree.links.new(tex.outputs['Color'],nm.inputs['Color']);m.node_tree.links.new(nm.outputs['Normal'],bs.inputs['Normal'])
    if emission:
        bs.inputs['Emission Color'].default_value=(*lin(color(h)),1);bs.inputs['Emission Strength'].default_value=emission
    all_materials[name]={'color':list(color(h)),'roughness':rough,'metallic':metal,'texture':texture.name if texture else '',
                         'normal':normal.name if normal else '', 'emission':emission,'alpha':alpha}
    M[key]=m;return m

def setup_materials():
    M.clear()
    for key,field in [('Top','top'),('Shirt','shirt'),('Pants','pants'),('Accent','accent')]:
        material(key,current[field],texture=FABRIC,normal=FABRIC_N)
    for key,h in [('Leather','765335'),('Boots',current['boots'])]:material(key,h,texture=LEATHER,normal=LEATHER_N)
    material('Skin',current['skin'],.53);material('Hair',current['hair'],.76)
    hh=color(current['hair']);highlight=''.join(f'{min(255,round(v*255*1.20+5)):02X}' for v in hh)
    material('HairHighlight',highlight,.76)
    material('Metal','75828A',.28,.58);material('Iron','263039',.42,.5)
    material('Brass','BA8B43',.25,.80);material('Ivory','E8DDC4',.69,texture=FABRIC,normal=FABRIC_N)
    material('Black','171E24',.4);material('White','E8E7DE',.22)
    material('Iris','694B2B',.26);material('Pupil','111A1D',.2)
    material('Gem','32B7E8',.14,.24,emission=.25)
    material('Lip','9C5E50',.58)
    material('Wood','5C3A21',.62,.05)
    material('Rubber','222426',.78,.02)
    material('Velvet','1E3A5A',.85,.02)
    material('VioletGlass','7A3FA8',.15,.25,emission=.15)
    if current.get('headband'):material('Headband',current['headband'],.65)
    if current.get('helmet'):material('Helmet',current['helmet'],.34,.12)
    material('Lamp','FFE6A2',.2,0,emission=.6)
    material('Map','FFFFFF',.9,texture=MAP_TEX)
    material('Plan','FFFFFF',.8,texture=PLAN_TEX)

def mesh(name,verts,faces,mat,bone='Head',weights=None,uv=None,bevel=0,subdiv=0,section='Body'):
    data=bpy.data.meshes.new(name);data.from_pydata([xyz(p) for p in verts],[],faces);data.update()
    obj=bpy.data.objects.new(name,data);bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(M[mat] if isinstance(mat,str) else mat)
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    for poly in data.polygons:poly.use_smooth=True
    layer=data.uv_layers.new(name='UVMap')
    for poly in data.polygons:
        for loop in poly.loop_indices:
            vi=data.loops[loop].vertex_index
            layer.data[loop].uv=uv[vi] if uv is not None else ((verts[vi][0]*2)%1,(verts[vi][1]*2+verts[vi][2])%1)
    if weights is None:weights=[{bone:1} for _ in verts]
    groups={}
    for index,entry in enumerate(weights):
        for key,value in entry.items():
            if value<=0:continue
            if key not in groups:groups[key]=obj.vertex_groups.new(name=key)
            groups[key].add([index],value,'REPLACE')
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    if bevel:
        mod=obj.modifiers.new('Tailored edge bevel','BEVEL');mod.width=bevel;mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if subdiv:
        mod=obj.modifiers.new('Smooth facial topology','SUBSURF');mod.levels=subdiv
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if section=='RoleProp' and len(faces)==1:
        mod=obj.modifiers.new('Two-sided cloth or paper thickness','SOLIDIFY');mod.thickness=.002
        bpy.ops.object.modifier_apply(modifier=mod.name)
    colors=obj.data.color_attributes.new(name='ContactAO',type='BYTE_COLOR',domain='CORNER')
    for entry in colors.data:entry.color=(1,1,1,1)
    obj['section']=section
    obj.select_set(False);objects.append(obj);return obj

def tube(name,points,radii,mat,bone='Head',sides=10,section='Body'):
    points=[Vector(p) for p in points];verts=[];uv=[];faces=[]
    if isinstance(radii,(int,float)):radii=[radii]*len(points)
    for i,p in enumerate(points):
        direction=(points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized()
        side=direction.cross(Vector((0,0,1)))
        if side.length<.01:side=direction.cross(Vector((0,1,0)))
        side.normalize();other=direction.cross(side).normalized()
        for j in range(sides):
            a=j/sides*math.tau;verts.append(tuple(p+(side*math.cos(a)+other*math.sin(a))*radii[i]));uv.append((j/sides,i/max(1,len(points)-1)))
    for i in range(len(points)-1):
        for j in range(sides):faces.append((i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j))
    faces.extend([tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+j for j in range(sides))])
    return mesh(name,verts,faces,mat,bone,uv=uv,section=section)

def ellipsoid(name,at,scale,mat,bone='Head',segments=24,rings=14,section='Body'):
    verts=[];uv=[];faces=[]
    for i in range(rings+1):
        lat=math.pi*(i+.001)/(rings+.002)
        for j in range(segments):
            a=j/segments*math.tau
            verts.append((at[0]+math.sin(lat)*math.sin(a)*scale[0],at[1]+math.cos(lat)*scale[1],at[2]+math.sin(lat)*math.cos(a)*scale[2]))
            uv.append((j/segments,i/rings))
    for i in range(rings):
        for j in range(segments):faces.append((i*segments+j,i*segments+(j+1)%segments,(i+1)*segments+(j+1)%segments,(i+1)*segments+j))
    return mesh(name,verts,faces,mat,bone,uv=uv,section=section)

def block(name,at,size,mat,bone='Chest',bevel=.009,section='Body'):
    x,y,z=[v*.5 for v in size]
    verts=[(at[0]+a*x,at[1]+b*y,at[2]+c*z) for c in (-1,1) for b in (-1,1) for a in (-1,1)]
    faces=[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]
    return mesh(name,verts,faces,mat,bone,bevel=bevel,section=section)

def ribbon(name,points,width,mat,bone='Chest',section='Body'):
    verts=[];uv=[]
    for i,p in enumerate(points):
        prev=Vector(points[max(0,i-1)]);nex=Vector(points[min(len(points)-1,i+1)])
        d=nex-prev;side=Vector((d.y,-d.x,0)).normalized()*width*.5
        verts.extend([tuple(Vector(p)-side),tuple(Vector(p)+side)]);uv.extend([(0,i/len(points)),(1,i/len(points))])
    faces=[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(points)-1)]
    obj=mesh(name,verts,faces,mat,bone,uv=uv,section=section)
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    mod=obj.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.002
    bpy.ops.object.modifier_apply(modifier=mod.name);obj.select_set(False)
    return obj

def torso_weight(h):
    if h<1.02:return {'Hips':1}
    if h<1.18:
        t=(h-1.02)/.16;return {'Spine':1-t,'Chest':t}
    return {'Chest':1}

def loft(name,rings,mat,bone='Hips',segments=28,weight_func=None,section='Body'):
    verts=[];uv=[];weights=[];faces=[]
    for i,(xx,h,dd,rx,rz) in enumerate(rings):
        for j in range(segments):
            a=j/segments*math.tau
            wrinkle=(math.sin(a*5+i*1.7)+math.sin(a*3-i*.6))*.0017 if mat in ('Top','Pants','Shirt') else 0
            verts.append((xx+math.sin(a)*(rx+wrinkle),h,dd+math.cos(a)*(rz+wrinkle)))
            uv.append((j/segments,i/max(1,len(rings)-1)))
            weights.append(weight_func(h) if weight_func else {bone:1})
    for i in range(len(rings)-1):
        for j in range(segments):faces.append((i*segments+j,i*segments+(j+1)%segments,(i+1)*segments+(j+1)%segments,(i+1)*segments+j))
    faces.extend([tuple(range(segments-1,-1,-1)),tuple((len(rings)-1)*segments+j for j in range(segments))])
    return mesh(name,verts,faces,mat,bone,weights,uv,section=section)

def face_and_hands():
    sex='female' if current['female'] else 'male'
    asian=.55 if current_id in ('tavi','mira','rumi','nilo') else .15
    african=.65 if current_id in ('oren','pavi','etta','kavi','beni') else 0
    v=BASE.copy()
    for population,weight in [('asian',asian),('african',african),('caucasian',1-asian-african)]:
        v+=target('macrodetails/'+population+'-'+sex+'-young.target.gz')*weight
    for side in ('l','r'):
        v+=target('eyes/'+side+'-eye-scale-incr.target.gz')*.65
        v+=target('eyes/'+side+'-eye-trans-out.target.gz')*.20
    ids=set(i for f in BODY_FACES if all(BASE[j,1]>5.78 for j in f) for i in f)
    bbox=v[list(ids)];low=bbox[:,1].min();high=bbox[:,1].max()
    refined=current.get('revision',0)>=2
    face_refined=refined and current_id in ('tavi','mira')
    if face_refined:
        from CrewFaceRefinements import sculpt_point,attach_eyelids
    sx=(.465 if current_id=='mira' and refined else .45 if refined else .405)/(bbox[:,0].max()-bbox[:,0].min());sz=(.35 if refined else .39)/(bbox[:,2].max()-bbox[:,2].min())
    depth_center=(bbox[:,2].max()+bbox[:,2].min())*.5
    def headpoint(p):return (p[0]*sx,1.37+(p[1]-low)/(high-low)*.525,(p[2]-depth_center)*sz+.015)
    remap={old:new for new,old in enumerate(sorted(ids))}
    verts=[headpoint(v[i]) for i in sorted(ids)]
    lip_target=target('mouth/mouth-trans-up.target.gz')
    lip_ids=np.where(np.abs(lip_target[:,1])>np.max(np.abs(lip_target[:,1]))*.75)[0]
    source_mouth=Vector(headpoint(v[lip_ids].mean(axis=0)))
    
    # Meaningful distinct facial morphology per character
    jaw_round=current.get('jaw_roundness',0.7)
    cheek_full=current.get('cheek_fullness',0.6)
    chin_len=0.025 if current_id=='daro' else (0.012 if current_id in ('oren','kavi') else (-0.015 if current_id in ('mira','pavi','beni') else 0.0))
    nose_broad=0.018 if current_id in ('oren','beni','etta') else (-0.010 if current_id in ('nilo','sola') else 0.0)
    smile_amp=.010 if refined else (0.024 if current_id in ('pavi','sola','tavi') else (0.018 if current_id in ('mira','beni','etta') else 0.010))

    for i,(x,h,d) in enumerate(verts):
        # Cheeks fullness
        if d>.06 and 1.50<h<1.66 and .04<abs(x)<.13:
            cf=(cheek_full-.6)*.030*math.exp(-(((abs(x)-.08)/.035)**2)-(((h-1.58)/.05)**2))
            x+=math.copysign(cf,x);d+=cf*.7
        # Jaw roundness/breadth
        if 1.43<h<1.54 and abs(x)>.04:
            jf=(jaw_round-.7)*.035*math.exp(-(((abs(x)-.085)/.04)**2)-(((h-1.48)/.05)**2))
            x+=math.copysign(jf,x);d+=jf*.5
        # Chin length
        if h<1.49 and abs(x)<.05 and d>.10:
            h-=chin_len*math.exp(-((x/.035)**2)-(((h-1.45)/.04)**2))*.7
        # Nose bridge/breadth
        if 1.55<h<1.66 and abs(x)<.04 and d>.14:
            nf=nose_broad*math.exp(-((x/.03)**2)-(((h-1.60)/.05)**2))*.8
            d+=nf;x*=(1.+nose_broad*7.)
        # Smile shape
        if d>.08:
            smile=smile_amp*min(1,abs(x)/.042)**1.6*math.exp(-(x/.070)**4-((h-source_mouth.y)/.024)**2)
            h+=smile
        if refined and d>.06:
            # Shorter, softly rounded lower face; soften the adult nose bridge.
            if h<1.62:h=1.62+(h-1.62)*.76
            d-=.012*math.exp(-(x/.045)**2-((h-1.62)/.06)**2)
            d+=(.020 if current_id=='mira' else .014)*math.exp(-((abs(x)-.10)/.045)**2-((h-1.59)/.06)**2)
            lip_h=1.62+(source_mouth.y-1.62)*.76
            if not face_refined and abs(h-lip_h)<.032 and abs(x)<.075:
                x*=1+.25*math.exp(-((h-lip_h)/.022)**2-(x/.070)**4)
            if face_refined:x,h,d=sculpt_point(x,h,d,lip_h)
        verts[i]=(x,h,d)

    # Collar closure
    for i,(x,h,d) in enumerate(verts):
        if h<1.455:
            t=max(0,min(1,(h-1.385)/.07))
            angle=math.atan2(x,d)
            verts[i]=(x*t+math.sin(angle)*.066*(1-t),h,d*t+math.cos(angle)*.059*(1-t))
    faces=[tuple(remap[i] for i in f) for f in BODY_FACES if all(i in ids for i in f)]
    if refined:
        # Enlarge the actual facial openings instead of placing white discs over skin.
        centers=[Vector(headpoint(center_point)) for center_point in [v[list(GROUPS['joint-'+s+'-eye'])].mean(axis=0) for s in ('l','r')]]
        filtered=[]
        for face_indices in faces:
            p=sum((Vector(verts[i]) for i in face_indices),Vector())/len(face_indices)
            if not any(p.z>.07 and ((p.x-eye.x)/.051)**2+((p.y-eye.y)/.029)**2<1.12 for eye in centers):filtered.append(face_indices)
        faces=filtered
    uv=[(.5+math.atan2(p[0],p[2]-.015)/math.tau,(p[1]-1.37)/.525) for p in verts]
    n=1024;yy,xx=np.mgrid[0:n,0:n]/n
    face=np.ones((n,n,3))*np.array(color(current['skin']))
    blush=np.zeros_like(xx)
    for cx in (.40,.60):blush+=np.exp(-((xx-cx)/.035)**2-((yy-.45)/.045)**2)
    face=face*(1-blush[:,:,None]*.16)+np.array([.75,.27,.20])*blush[:,:,None]*.16
    if face_refined:
        # Colour follows the sculpted lip surface through the head UVs. It is
        # deliberately soft so the mouth remains anatomical in side views.
        projected_x=np.tan(np.clip((xx-.5)*math.tau,-1.4,1.4))*.15
        projected_h=1.37+yy*.525
        lip_h=1.62+(source_mouth.y-1.62)*.76
        lip_curve=lip_h+.016*np.minimum(1.15,np.abs(projected_x)/.057)**1.65
        lip_mask=np.exp(-((projected_h-lip_curve)/.0075)**4-(projected_x/.060)**8)
        face=face*(1-lip_mask[:,:,None]*.23)+np.array([.66,.25,.19])*lip_mask[:,:,None]*.23
    tex=save_texture(current_id+'_Face_BaseColor',face)
    face_mat=material('Face','FFFFFF',.55,texture=tex)
    head_obj=mesh('Anatomical_face',verts,faces,face_mat,'Head',uv=uv,subdiv=1)
    if face_refined:
        lid_eyes=[Vector(headpoint(v[list(GROUPS['joint-'+s+'-eye'])].mean(axis=0)))+Vector((0,0,.004)) for s in ('l','r')]
        attach_eyelids(head_obj,lid_eyes,.051 if current_id=='tavi' else .049,.026,current.get('height',1))
    bvh=BVHTree.FromPolygons([vert.co for vert in head_obj.data.vertices],[list(p.vertices) for p in head_obj.data.polygons])
    def center(group):return v[list(GROUPS[group])].mean(axis=0)
    def surface(x,h):
        hit=bvh.ray_cast(xyz((x,h,1)),Vector((0,-1,0)))
        if hit[0] is not None:return hit[0].y/current.get('height',1)
        return .16
    eyes=[]
    for side in ('l','r'):
        eye=Vector(headpoint(center('joint-'+side+'-eye')));eye.z+=.004
        eyes.append(eye)
        radius=(.051 if current_id=='tavi' else .049) if refined else .044 if current['female'] else .040
        bulge=.026 if face_refined else .032 if refined else radius
        vv=[];ff=[];segments=40
        for ring in range(8):
            t=ring/7
            for j in range(segments):
                a=j/segments*math.tau;xx=math.sin(a)*radius*t;vertical=math.cos(a)
                if face_refined:vertical=math.copysign(abs(vertical)**1.30,vertical)
                hh=vertical*radius*(.58 if refined else .59)*t
                dd=eye.z+bulge-.009*t*t
                vv.append((eye.x+xx,eye.y+hh,dd))
        for ring in range(7):
            for j in range(segments):ff.append((ring*segments+j,ring*segments+(j+1)%segments,(ring+1)*segments+(j+1)%segments,(ring+1)*segments+j))
        mesh('Almond_eye_white',vv,ff,'White')
        for upper in (True,False):
            pts=[]
            for k in range(17):
                a=(-math.pi/2+k/16*math.pi) if upper else (math.pi/2+k/16*math.pi)
                vertical=math.cos(a)
                if face_refined:vertical=math.copysign(abs(vertical)**1.30,vertical)
                xx=eye.x+math.sin(a)*radius;hh=eye.y+vertical*radius*(.58 if refined else .59)
                pts.append((xx,hh,eye.z+bulge-.008 if refined else max(surface(xx,hh)+.003,eye.z+radius-.011)))
            tube('Upper_eyelid' if upper else 'Lower_eyelid',pts,(.0024 if upper else .0013) if face_refined else .0045 if upper else .003,'Skin',sides=8)
            if upper and current['female']:
                lash_radius=[.0007+.0015*math.sin(i/(len(pts)-1)*math.pi)**.5 for i in range(len(pts))] if face_refined else .0017
                tube('Upper_lash_line',[(x,h,d+(.0015 if face_refined else .006)) for x,h,d in pts],lash_radius,'Hair',sides=6)
        if refined and not face_refined:
            lidverts=[];lidfaces=[]
            for ring in range(4):
                t=ring/3
                for j in range(40):
                    a=j/40*math.tau;xx=eye.x+math.sin(a)*(radius+.018*t);hh=eye.y+math.cos(a)*(radius*.58+.019*t)
                    d=(eye.z+bulge-.007)*(1-t)+(surface(xx,hh)+.001)*t+.002*math.sin(t*math.pi)
                    lidverts.append((xx,hh,d))
            for ring in range(3):
                for j in range(40):lidfaces.append((ring*40+j,ring*40+(j+1)%40,(ring+1)*40+(j+1)%40,(ring+1)*40+j))
            liduv=[(.5+math.atan2(p[0],p[2]-.015)/math.tau,(p[1]-1.37)/.525) for p in lidverts]
            mesh('Sculpted_eye_socket_lids',lidverts,lidfaces,face_mat,uv=liduv)
        vv=[];ff=[];segments=40
        for ring,rr in enumerate((0,.012,.023,.025) if refined else (0,.011,.022,.024)):
            for j in range(segments):
                a=j/segments*math.tau
                vv.append((eye.x+math.sin(a)*rr,eye.y+math.cos(a)*rr,eye.z+bulge-.009*(rr/radius)**2+.0015))
        for ring in range(3):
            for j in range(segments):ff.append((ring*segments+j,ring*segments+(j+1)%segments,(ring+1)*segments+(j+1)%segments,(ring+1)*segments+j))
        iris_obj=mesh('Iris_radial',vv,ff,'Iris')
        ellipsoid('Pupil',(eye.x,eye.y,eye.z+bulge+.001),(.010,.012,.0015),'Pupil',segments=24,rings=8)
        ellipsoid('Eye_catchlight',(eye.x-.007,eye.y+.009,eye.z+bulge+.003),(.0048,.0048,.001),'White',segments=12,rings=6)
        sign=1 if eye.x>0 else -1
        brow=[]
        brow_thickness=0.007 if current_id in ('oren','kavi') else (0.004 if current_id in ('sola','rumi') else 0.005)
        for t in np.linspace(-.047,.043,13):
            xx=eye.x+sign*t;hh=eye.y+.055+.013*math.sin((t+.045)/.09*math.pi)
            brow.append((xx,hh,surface(xx,hh)+.010))
        tube('Sculpted_brow',brow,[.002+brow_thickness*math.sin(i/12*math.pi) for i in range(13)],'Hair',sides=10)
    mouth=source_mouth
    if refined:mouth.y=1.62+(mouth.y-1.62)*.76
    mouth.z=surface(0,mouth.y)+.002
    print('LANDMARK',current_id,'eyes',str(eyes),'mouth',tuple(mouth),flush=True)
    if face_refined:
        smile_points=[]
        for x in np.linspace(-.057,.057,25):
            h=mouth.y+.001+.019*(abs(x)/.057)**1.65
            smile_points.append((x,h,surface(x,h)+.0008))
        tube('Smile_crease',smile_points,[.00035+.0006*math.sin(i/24*math.pi) for i in range(25)],'Lip',sides=6)
    else:
        tube('Smile_crease',[(mouth.x+x,mouth.y+.003+abs(x)*(.12 if refined else .30),surface(x,mouth.y+abs(x)*(.12 if refined else .30))+.0015) for x in (-.035,-.02,0,.02,.035)],
             [.0008,.0014,.0015,.0014,.0008],'Lip',sides=6)
    for sign,side,label in ((1,'l','L'),(-1,'r','R')):
        hand_ids=set()
        for key,pairs in SOURCE_WEIGHTS.items():
            if ('LeftHand' if sign>0 else 'RightHand') in key:
                hand_ids.update(i for i,w in pairs if w>.10 and i<13380)
        faces_src=[f for f in BODY_FACES if all(i in hand_ids for i in f)]
        used=sorted(set(i for f in faces_src for i in f));mapping={i:k for k,i in enumerate(used)}
        wrist=Vector(center('joint-'+side+'-hand'));knuckle=Vector(center('joint-'+side+'-hand-3'))
        rotation=(knuckle-wrist).normalized().rotation_difference(Vector((0,-1,0)))
        wrist_target=Vector((sign*.395,.91,.005))
        handverts=[tuple(wrist_target+rotation@(Vector(v[i])-wrist)*.095) for i in used]
        handmat='Glove' if refined else 'Accent' if current.get('gloves') or current_id in ('mira','pavi','zela','beni','daro') else 'Skin'
        hand_weights=None
        if refined:
            # Retain the source's finger deformation topology and weights.
            finger_names={1:'Thumb',2:'Index',3:'Middle',4:'Ring',5:'Pinky'}
            bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
            bpy.ops.object.mode_set(mode='EDIT')
            for digit,name in finger_names.items():
                points=[tuple(wrist_target+rotation@(Vector(center('joint-'+side+'-finger-'+str(digit)+'-'+str(k)))-wrist)*.095) for k in range(1,5)]
                for k in range(3):
                    key=name+str(k+1)+'_'+label;parent='Hand_'+label if k==0 else name+str(k)+'_'+label
                    bone=rig.data.edit_bones.new(key);bone.head=xyz(points[k]);bone.tail=xyz(points[k+1]);bone.parent=rig.data.edit_bones[parent]
                    bone.align_roll(Vector((0,1,0)));bone_defs[key]=(points[k],points[k+1],parent)
            bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
            hand_weights=[{} for _ in used]
            prefix='mixamorig:'+('LeftHand' if sign>0 else 'RightHand')
            for key,pairs in SOURCE_WEIGHTS.items():
                if not key.startswith(prefix):continue
                bone='Hand_'+label if key==prefix else key[len(prefix):]+'_'+label
                for index,weight in pairs:
                    if index in mapping:hand_weights[mapping[index]][bone]=weight
            for weights in hand_weights:
                total=sum(weights.values())
                if total==0:weights['Hand_'+label]=1
                else:
                    for key in weights:weights[key]/=total
        mesh('Anatomical_hand_'+label,handverts,[tuple(mapping[i] for i in f) for f in faces_src],handmat,'Hand_'+label,weights=hand_weights,subdiv=0 if refined else 1)
    return eyes,mouth

def make_rig():
    global rig,bone_defs
    bone_defs={
      'Hips':((0,.88,0),(0,1.02,0),None),'Spine':((0,1.02,0),(0,1.19,0),'Hips'),
      'Chest':((0,1.19,0),(0,1.34,0),'Spine'),'Neck':((0,1.34,0),(0,1.47,0),'Chest'),
      'Head':((0,1.47,0),(0,1.88,0),'Neck')}
    for sign,label in ((1,'L'),(-1,'R')):
        bone_defs.update({
          'UpperArm_'+label:((sign*.23,1.31,0),(sign*.325,1.105,0),'Chest'),
          'Forearm_'+label:((sign*.325,1.105,0),(sign*.395,.91,.005),'UpperArm_'+label),
          'Hand_'+label:((sign*.395,.91,.005),(sign*.395,.80,.015),'Forearm_'+label),
          'Thigh_'+label:((sign*.105,.91,0),(sign*.11,.55,0),'Hips'),
          'Shin_'+label:((sign*.11,.55,0),(sign*.11,.22,0),'Thigh_'+label),
          'Foot_'+label:((sign*.11,.22,0),(sign*.11,.10,.13),'Shin_'+label)})
    data=bpy.data.armatures.new(current_id+'_Skeleton');rig=bpy.data.objects.new('CrewRig',data);bpy.context.scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    for name,(head,tail,parent) in bone_defs.items():
        bone=data.edit_bones.new(name);bone.head=xyz(head);bone.tail=xyz(tail)
        yy=(bone.tail-bone.head).normalized();zz=Vector((0,1,0));xx=yy.cross(zz).normalized();zz=xx.cross(yy)
        bone.matrix=Matrix(((xx.x,yy.x,zz.x,bone.head.x),(xx.y,yy.y,zz.y,bone.head.y),(xx.z,yy.z,zz.z,bone.head.z),(0,0,0,1)))
        bone.length=(xyz(tail)-xyz(head)).length
        if parent:bone.parent=data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)

def clothes():
    wide=current.get('width',1);female=current['female'];outfit=current['outfit']
    torso=[(0,.91,0,.185*wide,.12),(0,.97,0,.185*wide,.12),(0,1.03,0,.165*wide,.115),
      (0,1.12,0,.184*wide,.13),(0,1.22,0,.215*wide,.135),(0,1.30,0,.23*wide,.12),
      (0,1.34,0,.20*wide,.105),(0,1.365,0,.095,.08)]
    loft('Tailored_under_shirt',torso,'Shirt',weight_func=torso_weight)
    if outfit in ('jacket','coveralls','vest','waistcoat'):
        outer=[(x,h,z,rx+.007,rz+.009) for x,h,z,rx,rz in torso]
        loft('Tailored_outer_garment',outer,'Top',weight_func=torso_weight)
        ribbon('Button_placket',[(0,.97,.137),(0,1.13,.151),(0,1.28,.149)],.028,'Top')
        for y in (1.00,1.08,1.16,1.24):ellipsoid('Shirt_button',(0,y,.155),(.009,.009,.004),'Brass','Chest',12,6)
        for side in (-1,1):
            ribbon('Folded_lapel',[(side*.085,1.36,.084),(side*.14,1.29,.145),(side*.06,1.20,.157)],.045,'Top')
            block('Chest_pocket',(side*.11,1.20,.15),(.105,.092,.013),'Top',bevel=.006)
            block('Pocket_flap',(side*.11,1.249,.160),(.108,.027,.008),'Top',bevel=.003)
            ellipsoid('Pocket_snap',(side*.11,1.244,.168),(.006,.006,.003),'Brass','Chest',12,6)
            tube('Garment_stitch',[(side*.145,1.02,.085),(side*.155,1.13,.105),(side*.175,1.25,.11)],.0008,'Top','Chest',sides=6)
    if outfit in ('overalls','apron'):
        panel=[(-.13,.91,.137),(.13,.91,.137),(.14,1.25,.148),(.10,1.30,.14),(-.10,1.30,.14),(-.14,1.25,.148)]
        mesh('Bib_panel',panel,[(0,1,2,3,4,5)],'Top','Chest',bevel=0)
        for side in (-1,1):
            ribbon('Bib_shoulder_strap',[(side*.11,1.27,.15),(side*.145,1.36,.085),(side*.16,1.34,-.09)],.038,'Leather')
            block('Strap_buckle',(side*.11,1.265,.161),(.038,.038,.009),'Brass',bevel=.003)
        block('Bib_pocket',(0,1.11,.151),(.20,.10,.012),'Top',bevel=.005)
        if outfit=='apron':
            mesh('Leather_apron_skirt',[(-.17,.97,.143),(.17,.97,.143),(.19,.53,.16),(-.19,.53,.16)],[(0,1,2,3)],'Top','Hips')
            for x in (-.11,-.035,.04,.11):block('Apron_tool_pocket',(x,.72,.176),(.057,.13,.018),'Top','Hips',.004)
    loft('Neck',[(0,1.34,0,.074,.064),(0,1.46,0,.068,.059)],'Skin','Neck')
    for sign,label in ((1,'L'),(-1,'R')):
        def legweight(h):
            t=max(0,min(1,(h-.49)/.13));return {'Thigh_'+label:t,'Shin_'+label:1-t}
        loft('Tailored_trouser_'+label,[(sign*.10,.23,0,.075,.073),(sign*.11,.32,0,.082,.079),
            (sign*.11,.47,0,.087,.083),(sign*.11,.56,0,.085,.091),(sign*.105,.68,0,.104,.104),
            (sign*.105,.82,0,.112,.11),(sign*.10,.94,0,.112,.116)],'Pants',weight_func=legweight)
        for hh in (.34,.51,.62):
            tube('Cloth_fold',[(sign*.11-.052,hh,.07),(sign*.11,hh+.008,.085),(sign*.11+.05,hh+.004,.073)],.0022,'Pants','Shin_'+label if hh<.55 else 'Thigh_'+label,8)
        if current.get('knee_pads'):
            block('Knee_pad',(sign*.11,.55,.096),(.135,.16,.033),'Iron','Shin_'+label,.025)
            block('Knee_pad_inset',(sign*.11,.55,.118),(.097,.12,.018),'Accent','Shin_'+label,.018)
        rings=[(sign*.21,1.34,0,.045,.060),(sign*.235,1.305,0,.087,.086),(sign*.275,1.23,0,.083,.081),
          (sign*.31,1.15,0,.073,.072),(sign*.33,1.09,0,.067,.065),(sign*.362,1.00,.004,.060,.058),(sign*.395,.93,.005,.052,.052)]
        def armweight(h):
            t=max(0,min(1,(h-1.07)/.09));return {'UpperArm_'+label:t,'Forearm_'+label:1-t}
        sleeve_mat='Top' if outfit in ('jacket','coveralls') else 'Shirt'
        loft('Shaped_sleeve_'+label,rings,sleeve_mat,weight_func=armweight)
        loft('Turned_cuff_'+label,[(sign*.383,.935,.005,.057,.057),(sign*.389,.973,.005,.060,.060)],
             'Accent' if current_id in ('zela','pavi') else 'Shirt','Forearm_'+label)
        boot_h=.32 if current.get('rubber_boots') else (.11 if current.get('flat_shoes') else .24)
        if current.get('flat_shoes'):
            loft('Visible_ankle_'+label,[(sign*.11,.075,.005,.058,.060),(sign*.11,.27,0,.062,.064)],'Skin','Shin_'+label)
            loft('Flat_shoe_'+label,[(sign*.11,.035,.055,.085,.142),(sign*.11,.065,.055,.085,.14),(sign*.11,.11,.015,.065,.09)],'Boots','Foot_'+label)
        else:
            loft('Laced_boot_'+label,[(sign*.11,.04,.055,.087,.15),(sign*.11,.085,.055,.09,.153),
                (sign*.11,.14,.015,.078,.108),(sign*.11,boot_h,0,.071,.078)],'Boots','Foot_'+label)
            loft('Boot_collar_'+label,[(sign*.11,boot_h-.025,0,.075,.082),(sign*.11,boot_h+.005,0,.078,.084)],'Accent','Foot_'+label)
            if not current.get('rubber_boots'):
                for k in range(4):
                    hh=.12+k*.025;dd=.145-k*.015
                    tube('Crossed_lace',[(sign*.11-.038,hh,dd-.012),(sign*.11,hh+.010,dd+.006),(sign*.11+.038,hh+.019,dd-.015)],.0032,'Ivory','Foot_'+label,6)
                    tube('Crossed_lace',[(sign*.11+.038,hh,dd-.012),(sign*.11,hh+.010,dd+.006),(sign*.11-.038,hh+.019,dd-.015)],.0032,'Ivory','Foot_'+label,6)
                    for xx in (-.043,.043):ellipsoid('Lace_eyelet',(sign*.11+xx,hh,dd-.004),(.006,.006,.003),'Brass','Foot_'+label,10,6)
        loft('Rubber_sole_'+label,[(sign*.11,.02,.055,.091,.154),(sign*.11,.048,.055,.092,.155)],'Iron','Foot_'+label)
    loft('Utility_belt',[(0,.94,0,.197*wide,.137),(0,1.00,0,.194*wide,.135)],'Leather','Hips')
    buckle=[(-.038,.946,.148),(.038,.946,.148),(.038,.993,.148),(-.038,.993,.148),(-.038,.946,.148)]
    tube('Belt_buckle',buckle,.006,'Brass','Hips',8)
    for side in (-1,1):
        block('Leather_hip_pouch',(side*.195,.89,.075),(.115,.15,.075),'Leather','Hips',.015)
        block('Pouch_flap',(side*.195,.95,.119),(.119,.052,.012),'Boots','Hips',.008)
        ellipsoid('Pouch_button',(side*.195,.94,.129),(.008,.008,.003),'Brass','Hips',12,6)

def hair_and_helmet(eyes,mouth):
    style=current['hair_style'];female=current['female']
    ellipsoid('Hair_scalp',(0,1.735,-.032),(.174,.163,.15),'Hair',segments=36,rings=20)
    
    if style in ('waves','redwaves','pony','bob','blonde_waves'):
        length=.29 if style in ('waves','redwaves','blonde_waves') else (.20 if style=='pony' else .16)
        for side in (-1,1):
            for j in range(5):
                pts=[]
                for k in range(18):
                    t=k/17;pts.append((side*(.153+.012*math.sin(t*9+j)+j*.008),1.76-t*length,.035-j*.022+.024*math.sin(t*5+j*.5)))
                tube('Wavy_hair_lock',pts,[.032*(1-k/20) for k in range(18)],'Hair',sides=12)
                tube('Hair_strand_ridge',[(p[0]+side*.006,p[1],p[2]+.027) for p in pts],.0009,'HairHighlight',sides=5)
        for j in range(8):
            xx=-.14+j*.036
            tube('Swept_face_framing_lock',[(xx,1.805,.105),(xx+.015,1.785,.145),(xx+.03,1.746,.151)],
                 [.020,.027,.007],'Hair',sides=12)
        if style=='pony':
            pts=[(0,1.69,-.14),(.05,1.64,-.20),(.08,1.53,-.22),(.04,1.43,-.23),(.07,1.36,-.20)]
            tube('Low_ponytail',pts,[.054,.06,.055,.042,.012],'Hair',sides=14)
            tube('Hair_tie',[(.045+math.sin(a)*.057,1.62+math.cos(a)*.02,-.20) for a in np.linspace(0,math.tau,20)],.007,'Accent')
    elif style=='afro_puff':
        # Etta's textured high puff gathered with headband (NO helmet!)
        for row in range(5):
            phi=.30+row*.55
            for j in range(24):
                a=j/24*math.tau
                center=Vector((.23*math.sin(phi)*math.sin(a),1.78+.22*math.cos(phi),-.08-abs(.20*math.sin(phi)*math.cos(a))))
                pts=[tuple(center+Vector((math.sin(t)*.022,math.cos(t)*.022,math.sin(t*2)*.008))) for t in np.linspace(0,math.tau*1.25,16)]
                tube('Afro_puff_curl',pts,.014,'Hair',sides=8)
        # Headband around forehead
        if current.get('headband'):
            band_pts=[(math.sin(a)*.182,1.765+math.cos(a)*.03,math.cos(a)*.175-.02) for a in np.linspace(0,math.tau,36)]
            tube('Hair_headband',band_pts,.012,'Headband',sides=8)
    else:
        # Men's / short styles
        for j in range(10):
            x=-.145+j*.032
            pts=[(x,1.75,.08),(x-.012,1.84,.06),(x+.035,1.88,-.035),(x+.055,1.81,-.10)]
            tube('Swept_hair_lock',pts,[.017,.025,.025,.009],'Hair',sides=10)
    
    if not current.get('helmet'):
        # Crown hair for characters without helmets (Etta, Kavi, Sola)
        for j in range(12):
            a=-1.38+j/11*2.76
            pts=[]
            for t in np.linspace(0,1,24):
                theta=t*math.pi
                pts.append((.18*math.sin(a)+.014*math.sin(theta*2+a),1.76+.185*math.cos(a)*math.sin(theta),.19*math.cos(a)*math.cos(theta)-.03))
            tube('Parted_crown_lock',pts,[.022+.005*math.sin(k/23*math.pi) for k in range(24)],'Hair',sides=10)
            tube('Crown_strand',[(a,b+.014,c+.006) for a,b,c in pts],.0009,'HairHighlight',sides=5)

    if current.get('beard') or current.get('full_beard') or current.get('goatee'):
        head=bpy.data.objects.get('Anatomical_face')
        factor=current.get('height',1)
        vv=[(v.co.x/factor,v.co.z/factor,v.co.y/factor) for v in head.data.vertices]
        selected=[]
        for poly in head.data.polygons:
            if all(1.435<vv[i][1]<(mouth.y-.012 if abs(vv[i][0])<.075 else mouth.y+.04) and vv[i][2]>.045 for i in poly.vertices):
                selected.append(tuple(poly.vertices))
        used=sorted(set(i for face in selected for i in face));mapping={i:k for k,i in enumerate(used)}
        mesh('Sculpted_full_beard',[(vv[i][0],vv[i][1],vv[i][2]+.008) for i in used],
             [tuple(mapping[i] for i in face) for face in selected],'Hair')
        for side in (-1,1):
            for j in range(7):
                t=j/6;x=side*(.025+t*.090)
                tube('Beard_lock',[(x,1.53+t*.035,.135),(x*.9,1.47+t*.04,.135),(x*.75,1.445+t*.055,.11)],
                    [.019,.022,.007] if current.get('full_beard') else [.009,.011,.004],'Hair',sides=8)
        tube('Moustache',[(-.05,mouth.y+.015,mouth.z+.014),(-.025,mouth.y+.023,mouth.z+.019),(0,mouth.y+.018,mouth.z+.022),(.025,mouth.y+.023,mouth.z+.019),(.05,mouth.y+.015,mouth.z+.014)],
             [.005,.014,.012,.014,.005],'Hair',sides=8)

    # Helmets only for characters that wear them
    if current.get('helmet'):
        helmet_start=len(objects)
        verts=[];uv=[];faces=[];sectors=40;rings=13
        for i in range(rings+1):
            a=i/rings*math.pi*.51
            for j in range(sectors):
                t=j/sectors*math.tau
                verts.append((math.sin(a)*math.sin(t)*.197,1.77+math.cos(a)*.205,math.sin(a)*math.cos(t)*.175-.02));uv.append((j/sectors,i/rings))
        for i in range(rings):
            for j in range(sectors):faces.append((i*sectors+j,i*sectors+(j+1)%sectors,(i+1)*sectors+(j+1)%sectors,(i+1)*sectors+j))
        mesh('Hardhat_shell',verts,faces,'Helmet',uv=uv)
        rim=[(math.sin(a)*.207,1.771,math.cos(a)*.19-.015) for a in np.linspace(0,math.tau,49)]
        tube('Rolled_hardhat_rim',rim,.013,'Helmet',sides=8)
        bill=[(-.18,1.765,.07),(-.15,1.755,.19),(0,1.75,.245),(.15,1.755,.19),(.18,1.765,.07)]
        ribbon('Hardhat_bill',bill,.055,'Helmet','Head')
        if current.get('lamp_on_hat',True):
            tube('Lamp_housing',[(0,1.852,.16),(0,1.852,.204)],[.055,.055],'Iron','Head',24)
            tube('Lamp_metal_ring',[(0,1.852,.202),(0,1.852,.214)],[.049,.049],'Metal','Head',24)
            ellipsoid('Headlamp_glass',(0,1.852,.217),(.040,.040,.008),'Lamp',segments=24,rings=10)
        for side in (-1,1):block('Hardhat_fastener',(side*.169,1.80,.083),(.025,.037,.012),'Brass','Head',.003)
        for obj in objects[helmet_start:]:
            for vert in obj.data.vertices:vert.co.z+=.045*current.get('height',1)

    # Eyewear
    if current.get('glasses')=='violet_square':
        for eye in eyes:
            pts=[(eye.x+math.sin(a)*.048,eye.y+math.cos(a)*.042,eye.z+.046) for a in np.linspace(0,math.tau,28)]
            tube('Violet_spectacle_frame',pts,.006,'Accent',sides=8)
            ellipsoid('Violet_spectacle_lens',(eye.x,eye.y,eye.z+.044),(.045,.038,.004),'VioletGlass',segments=20,rings=8)
        e1,e2=eyes
        tube('Violet_spectacle_bridge',[(e1.x-.040,e1.y+.007,e1.z+.047),(0,e1.y+.014,e1.z+.049),(e2.x+.040,e2.y+.007,e2.z+.047)],.005,'Accent')
    elif current.get('glasses')=='square':
        for eye in eyes:
            pts=[(eye.x+math.sin(a)*.046,eye.y+math.cos(a)*.040,eye.z+.046) for a in np.linspace(0,math.tau,28)]
            tube('Spectacle_frame',pts,.0045,'Iron',sides=8)
        e1,e2=eyes
        tube('Spectacle_bridge',[(e1.x-.040,e1.y+.007,e1.z+.047),(0,e1.y+.014,e1.z+.049),(e2.x+.040,e2.y+.007,e2.z+.047)],.004,'Iron')
    elif current.get('goggles')=='round':
        for eye in eyes:
            pts=[(eye.x+math.sin(a)*.054,eye.y+math.cos(a)*.054,eye.z+.048) for a in np.linspace(0,math.tau,32)]
            tube('Goggle_frame',pts,.007,'Iron',sides=8)
            ellipsoid('Goggle_lens',(eye.x,eye.y,eye.z+.046),(.050,.050,.006),'Metal',segments=24,rings=10)
        e1,e2=eyes
        tube('Goggle_bridge',[(e1.x-.045,e1.y,e1.z+.048),(0,e1.y+.005,e1.z+.050),(e2.x+.045,e2.y,e2.z+.048)],.006,'Iron')

def outfit_details():
    if current.get('scarf'):
        rings=[]
        for h,rx,rz in [(1.35,.115,.120),(1.38,.122,.122),(1.41,.118,.115),(1.44,.105,.102),(1.465,.092,.088)]:rings.append((0,h,.008,rx,rz))
        loft('Wrapped_canvas_scarf',rings,'Ivory','Neck')
        ribbon('Scarf_drape',[(-.08,1.41,.115),(-.02,1.36,.165),(.06,1.30,.170),(.085,1.23,.162)],.085,'Ivory')
        for h in (1.37,1.395,1.425):tube('Scarf_fold',[(-.085,h,.08),(0,h-.008,.125),(.085,h,.08)],.0042,'Ivory','Neck',8)
        ribbon('Satchel_shoulder_strap',[(-.16,1.34,.11),(-.07,1.22,.165),(.06,1.10,.165),(.18,.98,.15)],.035,'Leather')
        block('Explorer_satchel',(.21,.83,-.02),(.17,.21,.10),'Leather','Hips',.02)
    if current_id in ('oren','daro','beni'):
        for side in (-1,1):ribbon('Work_suspender',[(side*.12,.98,.15),(side*.15,1.20,.15),(side*.16,1.34,.07)],.033,'Leather')
    if current_id=='sola':
        ellipsoid('Diamond_brooch',(-.065,1.255,.16),(.018,.022,.009),'Gem','Chest',12,8)
    if current_id in ('oren','etta','daro','kavi'):
        for j in range(4):
            x=-.11+j*.07
            tube('Belt_tool_handle',[(x,.96,.155),(x,.80,.18)],[.012,.009],'Brass','Hips',8)
            block('Tool_belt_holder',(x,.91,.174),(.028,.035,.013),'Leather','Hips',.004)
    if current_id=='zela':
        block('Drill_backpack',(0,1.20,-.18),(.27,.30,.14),'Top','Chest',.035)
        for side in (-1,1):
            tube('Backpack_copper_tank',[(side*.16,1.06,-.21),(side*.16,1.32,-.21)],[.053,.053],'Accent','Chest',20)
        tube('Ribbed_drill_hose',[(.13,1.30,-.25),(.24,1.40,-.16),(.31,1.31,.03),(.34,1.12,.10)],.018,'Iron','Chest',12)

def props():
    section='RoleProp';prop=current['prop'];left=(.395,.83,.065);right=(-.395,.83,.065)
    if prop=='map':
        x,h,d=left
        verts=[(x-.09,h-.06,d+.01),(x+.08,h-.07,d+.02),(x+.10,h+.13,d+.005),(x-.07,h+.15,d-.025)]
        wrist=Vector((x,.91,.005));angle=math.radians(-50);rotation=Matrix.Rotation(angle,3,'X')
        verts=[tuple(wrist+Vector((q.x,q.z,q.y))) for p in verts for q in [rotation@Vector((p[0]-wrist.x,p[2]-wrist.z,p[1]-wrist.y))]]
        mesh('Explorer_folded_map',verts,[(0,1,2,3)],'Map','Hand_L',uv=[(0,0),(1,0),(1,1),(0,1)],section=section)
        tube('Rolled_map_edge',[verts[0],verts[3]],.006,'Ivory','Hand_L',8,section)
    elif prop=='cart_prop':
        # Mira's Ore Cart
        cx,ch,cd=(-.38,.38,.05)
        block('Mira_cart_body',(cx,ch,cd),(.38,.28,.48),'Wood','Hand_R',.012,section)
        block('Mira_cart_rim',(cx,ch+.14,cd),(.40,.04,.50),'Iron','Hand_R',.006,section)
        for sx in (-1,1):
            for sz in (-1,1):
                block('Mira_cart_bracket',(cx+sx*.18,ch,cd+sz*.23),(.04,.26,.04),'Iron','Hand_R',.004,section)
        for sx in (-.17,.17):
            for sz in (-.16,.16):
                tube('Mira_cart_wheel',[(cx+sx-.02,ch-.24,cd+sz),(cx+sx+.02,ch-.24,cd+sz)],[.085,.085],'Iron','Hand_R',16,section)
        for k in range(9):
            ox=cx+(k%3-1)*.08;oz=cd+(k//3-1)*.11
            ellipsoid('Mira_cart_ore',(ox,ch+.14+(k%2)*.04,oz),(.06,.045,.06),'Gem' if k%2==0 else 'Iron','Hand_R',10,6,section)
        tube('Mira_cart_handle',[(cx+.19,ch+.16,cd-.24),(cx+.19,ch+.16,cd+.24)],.014,'Metal','Hand_R',10,section)
    elif prop=='trolley_prop':
        # Beni's Delivery Trolley
        tx,th,td=(.34,.35,.05)
        tube('Trolley_rail_L',[(tx-.10,th-.25,td),(tx-.10,th+.42,td)],.013,'Iron','Hand_L',10,section)
        tube('Trolley_rail_R',[(tx+.10,th-.25,td),(tx+.10,th+.42,td)],.013,'Iron','Hand_L',10,section)
        tube('Trolley_handle_cross',[(tx-.10,th+.42,td),(tx+.10,th+.42,td)],.014,'Rubber','Hand_L',10,section)
        block('Trolley_base',(tx,th-.22,td+.12),(.26,.04,.30),'Iron','Hand_L',.008,section)
        for sx in (-.14,.14):
            tube('Trolley_wheel',[(tx+sx-.02,th-.25,td),(tx+sx+.02,th-.25,td)],[.08,.08],'Rubber','Hand_L',16,section)
            tube('Trolley_wheel_hub',[(tx+sx-.025,th-.25,td),(tx+sx+.025,th-.25,td)],[.035,.035],'Brass','Hand_L',12,section)
        block('Delivery_case_bottom',(tx,th-.08,td+.12),(.24,.22,.26),'Helmet','Hand_L',.012,section)
        block('Delivery_case_top',(tx,th+.14,td+.12),(.22,.18,.24),'Metal','Hand_L',.010,section)
        block('Case_latch',(tx,th-.04,td+.255),(.04,.04,.012),'Brass','Hand_L',.004,section)
        block('Case_diamond_badge',(tx,th-.08,td+.253),(.06,.06,.006),'White','Hand_L',.003,section)
    elif prop=='gem_tweezers':
        # Etta's Precision Tweezers & Diamond
        x,h,d=right
        tube('Tweezer_shaft',[(x,h,d),(x,h+.18,d+.04)],.007,'Metal','Hand_R',8,section)
        for sign in (-1,1):tube('Tweezer_prong',[(x+sign*.004,h+.15,d+.03),(x+sign*.002,h+.23,d+.06)],.0035,'Metal','Hand_R',6,section)
        ellipsoid('Inspected_diamond',(x,h+.235,d+.062),(.022,.024,.022),'Gem','Hand_R',10,8,section)
    elif prop=='ring_loupe':
        # Kavi's Gold Ring & Jewellery Tool
        lx,lh,ld=left
        tube('Jeweller_gold_ring',[(lx+math.sin(a)*.026,lh+.08+math.cos(a)*.026,ld) for a in np.linspace(0,math.tau,24)],.0045,'Brass','Hand_L',8,section)
        ellipsoid('Ring_diamond_mount',(lx,lh+.108,ld),(.012,.014,.012),'Gem','Hand_L',10,8,section)
        rx,rh,rd=right
        tube('Jewellery_burnisher_handle',[(rx,rh-.06,rd),(rx,rh+.10,rd)],.011,'Wood','Hand_R',8,section)
        tube('Jewellery_burnisher_tip',[(rx,rh+.09,rd),(rx,rh+.17,rd)],[.007,.002],'Metal','Hand_R',8,section)
    elif prop=='jewelbox':
        # Sola's Presentation Box
        x,h,d=right
        block('Presentation_box_base',(x,h,d),(.18,.06,.18),'Velvet','Hand_R',.010,section)
        block('Box_satin_cushion',(x,h+.025,d),(.15,.02,.15),'White','Hand_R',.006,section)
        tube('Box_diamond_ring_band',[(x+math.sin(a)*.024,h+.045+math.cos(a)*.024,d) for a in np.linspace(0,math.tau,20)],.004,'Brass','Hand_R',8,section)
        ellipsoid('Box_diamond_gem',(x,h+.07,d),(.014,.016,.014),'Gem','Hand_R',10,8,section)
        block('Presentation_box_lid',(x,h+.11,d-.08),(.18,.16,.025),'Velvet','Hand_R',.010,section)
        block('Lid_gold_trim',(x,h+.11,d-.065),(.15,.13,.006),'Brass','Hand_R',.004,section)
    elif prop=='geology':
        x,h,d=left
        ellipsoid('Hand_sample',(x,h+.05,d),(.055,.047,.05),'Metal','Hand_L',10,7,section)
        rx,rh,rd=right
        tube('Magnifier_handle',[(rx,rh-.04,rd),(rx,rh+.12,rd)],.011,'Wood','Hand_R',8,section)
        tube('Magnifier_rim',[(rx+math.sin(a)*.045,rh+.16+math.cos(a)*.045,rd) for a in np.linspace(0,math.tau,24)],.007,'Brass','Hand_R',8,section)
        ellipsoid('Magnifier_lens',(rx,rh+.16,rd),(.042,.042,.004),'Metal','Hand_R',16,8,section)
        for i in range(3):tube('Sample_tube',[(.04+i*.035,.87,.17),(.04+i*.035,.98,.17)],.013,'Gem' if i%2 else 'Brass','Hips',12)
    elif prop=='drill':
        x,h,d=right
        tube('Drill_motor',[(x,h,d+.02),(x,h,d+.20)],[.065,.055],'Helmet','Hand_R',24,section)
        block('Drill_trigger_grip',(x,h-.06,d+.06),(.045,.12,.04),'Iron','Hand_R',.01,section)
        tube('Drill_chuck',[(x,h,d+.19),(x,h,d+.27)],[.041,.025],'Metal','Hand_R',16,section)
        tube('Drill_bit',[(x,h,d+.26),(x,h,d+.41)],[.019,.004],'Metal','Hand_R',12,section)
        pts=[(x+math.sin(t)*.021,h+math.cos(t)*.021,d+.27+t/(math.tau*4)*.12) for t in np.linspace(0,math.tau*4,64)]
        tube('Spiral_bit_flute',pts,.006,'Metal','Hand_R',6,section)
    elif prop=='pan':
        x,h,d=left
        loft('Washing_pan',[(x,h-.015,d,.115,.11),(x,h+.035,d,.15,.14)],'Iron','Hand_L',section=section)
        for j in range(7):ellipsoid('Pan_ore',(x+random.uniform(-.08,.08),h+.045,d+random.uniform(-.07,.07)),(.025,.018,.023),'Gem' if j%3==0 else 'Metal','Hand_L',10,6,section)
    elif prop=='tray':
        x,h,d=left
        block('Sorting_tray_base',(0,h+.04,.22),(.28,.04,.22),'Iron','Hand_L',.008,section)
        colors=['Gem','White','Iris','Brass','Lip','Helmet']
        for k in range(6):
            a=k/6*math.tau;gx=math.sin(a)*.08;gz=.22+math.cos(a)*.06
            ellipsoid('Sorted_gem_'+str(k),(gx,h+.07,gz),(.018,.020,.018),colors[k],'Hand_L',8,6,section)
        ellipsoid('Sorted_center_diamond',(0,h+.075,.22),(.025,.026,.025),'Gem','Hand_L',10,8,section)
    elif prop=='cloth':
        x,h,d=left
        mesh('Polishing_cloth',[(x-.09,h+.04,d),(x+.08,h+.04,d),(x+.08,h-.12,d+.02),(x-.07,h-.14,d-.025)],[(0,1,2,3)],'Ivory','Hand_L',section=section)
        ellipsoid('Polished_gem',(x+.015,h+.045,d+.01),(.026,.025,.025),'Gem','Hand_L',10,7,section)
    elif prop=='blueprint':
        x,h,d=left
        tube('Rolled_blueprint',[(x-.07,h-.09,d),(x+.07,h+.15,d)],.04,'Plan','Hand_L',24,section)
        x,h,d=right
        block('Planner_clipboard',(x,h+.025,d),(.16,.23,.025),'Wood','Hand_R',.008,section)
        block('Clipboard_clip',(x,h+.13,d+.02),(.065,.025,.015),'Metal','Hand_R',.003,section)

    # Standard mining tool for all
    x,h,d=right
    tube('Pick_handle',[(x,h-.14,d),(x,h+.18,d)],[.013,.011],'Leather','Hand_R',10,'MiningTool')
    tube('Forged_pick_head',[(x-.10,h+.16,d),(x-.04,h+.195,d),(x+.04,h+.195,d),(x+.11,h+.15,d)],
         [.005,.024,.024,.005],'Metal','Hand_R',10,'MiningTool')
    block('Carried_ore_crate',(0,1.10,.32),(.29,.18,.22),'Leather','Chest',.015,'CarryLoad')
    for j in range(7):
        ellipsoid('Carried_ore',((j%3-1)*.07,1.205+(j%2)*.016,.27+(j//3)*.06),(.045,.039,.04),'Metal' if j%3 else 'Gem','Chest',10,7,'CarryLoad')

def join_and_bind():
    bpy.ops.object.select_all(action='DESELECT')
    sections={}
    for o in objects:
        try:
            sec=o.get('section','Body')
            sections.setdefault(sec,[]).append(o)
        except Exception:pass
    for section in ['Body','CarryLoad','MiningTool','RoleProp']:
        selected=sections.get(section,[])
        if not selected:continue
        bpy.ops.object.select_all(action='DESELECT')
        for obj in selected:obj.select_set(True)
        bpy.context.view_layer.objects.active=selected[0]
        if len(selected)>1:bpy.ops.object.join()
        obj=selected[0];obj.name=section
        mod=obj.modifiers.new('Crew deformation rig','ARMATURE');mod.object=rig
        obj.parent=rig;obj.matrix_parent_inverse=rig.matrix_world.inverted()
        obj.select_set(False)
    for name,bone,at in [('ToolSocket','Hand_R',(-.395,.83,.065)),('LookForward','Head',(0,1.65,.50))]:
        obj=bpy.data.objects.new(name,None);bpy.context.scene.collection.objects.link(obj)
        obj.parent=rig;obj.parent_type='BONE';obj.parent_bone=bone
        obj.matrix_world=Matrix.Translation(xyz(at))

CLIPS={'Idle':72,'Walk':48,'Run':32,'Mine':40,'Collect':40,'Carry':48,'Wave':64,'Celebrate':48,'Machine':48,'Craft':48}
def animation():
    rig.animation_data_create()
    for name,length in CLIPS.items():
        action=bpy.data.actions.new(name);rig.animation_data.action=action
        frames=list(range(1,length+1,3))
        if frames[-1]!=length:frames.append(length)
        for frame in frames:
            phase=(frame-1)/(length-1)*math.tau
            for bone in rig.pose.bones:bone.rotation_mode='XYZ';bone.rotation_euler=(0,0,0);bone.location=(0,0,0)
            p=rig.pose.bones
            if name in ('Walk','Run','Carry'):
                amp=42 if name=='Run' else 28
                for sign,side in ((1,'L'),(-1,'R')):
                    value=math.sin(phase)*sign
                    p['Thigh_'+side].rotation_euler.x=math.radians(value*amp)
                    p['Shin_'+side].rotation_euler.x=math.radians(-max(0,-value)*amp*.8)
                    p['Foot_'+side].rotation_euler.x=math.radians(max(0,value)*-10)
                    p['UpperArm_'+side].rotation_euler.x=math.radians(-value*amp*.65)
                    p['Forearm_'+side].rotation_euler.x=math.radians(15+max(0,value)*12)
                p['Hips'].location.y=abs(math.sin(phase))*(.016 if name=='Run' else .009)
                if name=='Carry':
                    for side in ('L','R'):p['Forearm_'+side].rotation_euler.x=math.radians(55);p['UpperArm_'+side].rotation_euler.x=math.radians(20)
            elif name=='Mine':
                swing=.5-.5*math.cos(phase)
                p['UpperArm_R'].rotation_euler.x=math.radians(25+swing*92)
                p['Forearm_R'].rotation_euler.x=math.radians(15+swing*35)
                p['Chest'].rotation_euler.x=math.radians(-swing*6)
                p['UpperArm_L'].rotation_euler.x=math.radians(20)
            elif name=='Collect':
                t=(frame-1)/(length-1);lift=t*t*(3-2*t)
                p['Chest'].rotation_euler.x=math.radians(15*(1-lift))
                p['Hips'].location.y=-.025*(1-lift)
                for side in ('L','R'):
                    p['UpperArm_'+side].rotation_euler.x=math.radians(12+18*lift)
                    p['Forearm_'+side].rotation_euler.x=math.radians(15+42*lift)
            elif name in ('Machine','Craft'):
                for sign,side in ((1,'L'),(-1,'R')):
                    p['UpperArm_'+side].rotation_euler.x=math.radians(30+math.sin(phase)*sign*8)
                    p['Forearm_'+side].rotation_euler.x=math.radians(42+math.cos(phase)*7)
            elif name in ('Wave','Celebrate'):
                p['UpperArm_R'].rotation_euler.x=math.radians(145)
                p['Forearm_R'].rotation_euler.z=math.radians(math.sin(phase)*20)
                if name=='Celebrate':p['UpperArm_L'].rotation_euler.x=math.radians(135);p['Hips'].location.y=max(0,math.sin(phase))*.035
            else:
                # DISTINCT ROLE-SPECIFIC IDLE POSTURE
                breathe=math.sin(phase)*.8
                p['Chest'].rotation_euler.x=math.radians(breathe)
                p['Head'].rotation_euler.z=math.radians(math.sin(phase)*.5)
                
                pose=current.get('pose','explorer')
                if pose=='explorer': # Tavi
                    p['Thigh_R'].rotation_euler.z=math.radians(-3)
                    p['Thigh_L'].rotation_euler.x=math.radians(7)
                    p['Shin_L'].rotation_euler.x=math.radians(-7)
                    p['Hips'].location.x=.012
                    p['Hips'].rotation_euler.z=math.radians(-2)
                    p['Spine'].rotation_euler.z=math.radians(1.5)
                    p['UpperArm_L'].rotation_euler.x=math.radians(38)
                    p['UpperArm_L'].rotation_euler.z=math.radians(16)
                    p['Forearm_L'].rotation_euler.x=math.radians(82)
                    p['UpperArm_R'].rotation_euler.x=math.radians(14)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-12)
                    p['Forearm_R'].rotation_euler.x=math.radians(28)
                    p['Head'].rotation_euler.z=math.radians(-3)
                elif pose=='cart_engineer': # Mira
                    p['Thigh_L'].rotation_euler.z=math.radians(3)
                    p['Thigh_R'].rotation_euler.z=math.radians(-3)
                    p['UpperArm_L'].rotation_euler.x=math.radians(12)
                    p['UpperArm_L'].rotation_euler.z=math.radians(15)
                    p['Forearm_L'].rotation_euler.x=math.radians(32)
                    p['UpperArm_R'].rotation_euler.x=math.radians(24)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-16)
                    p['Forearm_R'].rotation_euler.x=math.radians(38)
                    p['Head'].rotation_euler.z=math.radians(2)
                elif pose=='geologist': # Oren
                    p['UpperArm_L'].rotation_euler.x=math.radians(44)
                    p['UpperArm_L'].rotation_euler.z=math.radians(18)
                    p['Forearm_L'].rotation_euler.x=math.radians(82)
                    p['UpperArm_R'].rotation_euler.x=math.radians(46)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-15)
                    p['Forearm_R'].rotation_euler.x=math.radians(86)
                    p['Head'].rotation_euler.x=math.radians(12)
                    p['Head'].rotation_euler.z=math.radians(4)
                elif pose=='drill_engineer': # Zela
                    p['Thigh_L'].rotation_euler.z=math.radians(4)
                    p['Thigh_R'].rotation_euler.z=math.radians(-4)
                    p['UpperArm_L'].rotation_euler.x=math.radians(10)
                    p['UpperArm_L'].rotation_euler.z=math.radians(22)
                    p['Forearm_L'].rotation_euler.x=math.radians(48)
                    p['UpperArm_R'].rotation_euler.x=math.radians(14)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-12)
                    p['Forearm_R'].rotation_euler.x=math.radians(22)
                    p['Head'].rotation_euler.x=math.radians(-2)
                elif pose=='washer': # Pavi
                    p['UpperArm_L'].rotation_euler.x=math.radians(32)
                    p['UpperArm_L'].rotation_euler.z=math.radians(10)
                    p['Forearm_L'].rotation_euler.x=math.radians(64)
                    p['UpperArm_R'].rotation_euler.x=math.radians(32)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-10)
                    p['Forearm_R'].rotation_euler.x=math.radians(64)
                    p['Head'].rotation_euler.z=math.radians(3)
                elif pose=='sorter': # Nilo
                    p['UpperArm_L'].rotation_euler.x=math.radians(30)
                    p['UpperArm_L'].rotation_euler.z=math.radians(10)
                    p['Forearm_L'].rotation_euler.x=math.radians(58)
                    p['UpperArm_R'].rotation_euler.x=math.radians(30)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-10)
                    p['Forearm_R'].rotation_euler.x=math.radians(58)
                    p['Head'].rotation_euler.x=math.radians(14)
                elif pose=='gem_cutter': # Etta
                    p['UpperArm_R'].rotation_euler.x=math.radians(50)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-18)
                    p['Forearm_R'].rotation_euler.x=math.radians(88)
                    p['UpperArm_L'].rotation_euler.x=math.radians(12)
                    p['UpperArm_L'].rotation_euler.z=math.radians(14)
                    p['Forearm_L'].rotation_euler.x=math.radians(30)
                    p['Head'].rotation_euler.x=math.radians(-3)
                    p['Head'].rotation_euler.z=math.radians(-5)
                elif pose=='polisher': # Rumi
                    p['UpperArm_L'].rotation_euler.x=math.radians(25)
                    p['UpperArm_L'].rotation_euler.z=math.radians(8)
                    p['Forearm_L'].rotation_euler.x=math.radians(72)
                    p['UpperArm_R'].rotation_euler.x=math.radians(25)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-8)
                    p['Forearm_R'].rotation_euler.x=math.radians(72)
                    p['Head'].rotation_euler.z=math.radians(-2)
                elif pose=='jeweller': # Kavi
                    p['UpperArm_L'].rotation_euler.x=math.radians(36)
                    p['UpperArm_L'].rotation_euler.z=math.radians(14)
                    p['Forearm_L'].rotation_euler.x=math.radians(78)
                    p['UpperArm_R'].rotation_euler.x=math.radians(32)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-14)
                    p['Forearm_R'].rotation_euler.x=math.radians(68)
                    p['Head'].rotation_euler.x=math.radians(8)
                elif pose=='shopkeeper': # Sola
                    p['UpperArm_R'].rotation_euler.x=math.radians(32)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-12)
                    p['Forearm_R'].rotation_euler.x=math.radians(62)
                    p['UpperArm_L'].rotation_euler.x=math.radians(18)
                    p['UpperArm_L'].rotation_euler.z=math.radians(24)
                    p['Forearm_L'].rotation_euler.x=math.radians(36)
                    p['Head'].rotation_euler.z=math.radians(3)
                elif pose=='delivery': # Beni
                    p['UpperArm_L'].rotation_euler.x=math.radians(18)
                    p['UpperArm_L'].rotation_euler.z=math.radians(14)
                    p['Forearm_L'].rotation_euler.x=math.radians(32)
                    p['UpperArm_R'].rotation_euler.x=math.radians(10)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-10)
                    p['Forearm_R'].rotation_euler.x=math.radians(20)
                    p['Head'].rotation_euler.z=math.radians(-2)
                elif pose=='planner': # Daro
                    p['UpperArm_L'].rotation_euler.x=math.radians(32)
                    p['UpperArm_L'].rotation_euler.z=math.radians(12)
                    p['Forearm_L'].rotation_euler.x=math.radians(62)
                    p['UpperArm_R'].rotation_euler.x=math.radians(28)
                    p['UpperArm_R'].rotation_euler.z=math.radians(-12)
                    p['Forearm_R'].rotation_euler.x=math.radians(52)
                    p['Head'].rotation_euler.x=math.radians(6)

            if current.get('revision',0)>=2:
                for side in ('L','R'):
                    grip=50 if name in ('Mine','Collect','Carry','Craft','Machine') else (12 if side=='L' and current_id=='tavi' else 18)
                    for digit in ('Thumb','Index','Middle','Ring','Pinky'):
                        for k in range(1,4):
                            bone=p.get(digit+str(k)+'_'+side)
                            if bone:bone.rotation_euler.x=math.radians(grip*(.65 if digit=='Thumb' else 1)*(1 if k<3 else .7))
                if name=='Idle' and current_id=='mira':
                    p['UpperArm_R'].rotation_euler.x=0;p['Forearm_R'].rotation_euler.x=math.radians(5)
            for bone in rig.pose.bones:
                bone.keyframe_insert(data_path='rotation_euler',frame=frame,group=bone.name)
                if bone.name=='Hips':bone.keyframe_insert(data_path='location',frame=frame,group=bone.name)
        action.use_fake_user=True
    rig.animation_data.action=bpy.data.actions.get('Idle')
    bpy.context.scene.frame_set(1)

def render_preview(identifier):
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=int(os.environ.get('DMT_PREVIEW_SAMPLES','32'));scene.cycles.use_denoising=True
    scene.render.resolution_x=800;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
    scene.world.use_nodes=True;bg=scene.world.node_tree.nodes.get('Background');bg.inputs[0].default_value=(.7,.68,.61,1);bg.inputs[1].default_value=.45
    for name,at,power,size,tint in [('Key',(-2,3,3),280,3,(1,.90,.78)),('Fill',(3,2,1),90,3,(.75,.86,1)),('Rim',(1,3,-3),160,2,(1,.88,.72))]:
        data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=tint
        obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=xyz(at)
        obj.rotation_euler=(xyz((0,1.0,0))-obj.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,.002));floor=bpy.context.object
    floor.name='Studio_floor';mat=bpy.data.materials.new('Studio_floor');mat.diffuse_color=(.60,.56,.48,1);floor.data.materials.append(mat)
    data=bpy.data.cameras.new('CharacterCamera');camera=bpy.data.objects.new('CharacterCamera',data);scene.collection.objects.link(camera)
    camera.location=xyz((2.5,1.65,5.8));camera.rotation_euler=(xyz((0,1.0,0))-camera.location).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO';data.ortho_scale=2.28*current.get('height',1);scene.camera=camera
    for obj in rig.children:
        if obj.name in ('MiningTool','CarryLoad'):obj.hide_render=True
    scene.render.filepath=str(EVIDENCE/(identifier+'-blender.png'))
    bpy.ops.render.render(write_still=True)
    camera.location=xyz((.35,1.69,3.0));camera.rotation_euler=(xyz((0,1.665,.015))-camera.location).to_track_quat('-Z','Y').to_euler()
    data.ortho_scale=.69*current.get('height',1)
    scene.render.resolution_x=800;scene.render.resolution_y=800
    scene.render.filepath=str(EVIDENCE/(identifier+'-face-blender.png'))
    bpy.ops.render.render(write_still=True)
    for obj in rig.children:obj.hide_render=False

for identifier in IDS:
    identifier=identifier.strip()
    if identifier not in DESIGNS:raise ValueError(identifier)
    current_id=identifier;current=DESIGNS[identifier];objects=[]
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    setup_materials();make_rig()
    if current.get('revision',0)>=2:
        from CrewRefinements import refine_materials,refined_clothes,refined_hair_helmet,refined_details,refined_props
        refine_materials(globals())
        eyes,mouth=face_and_hands()
        if current_id in ('tavi','mira'):
            refined_clothes(globals());refined_hair_helmet(globals(),eyes,mouth);refined_details(globals());refined_props(globals())
        else:
            from CrewRoleRefinements import create_role_clothes,create_role_hair,create_role_details,create_role_props
            create_role_clothes(globals());create_role_hair(globals(),eyes,mouth);create_role_details(globals());create_role_props(globals())
    else:
        eyes,mouth=face_and_hands();clothes();hair_and_helmet(eyes,mouth);outfit_details();props()
    join_and_bind();animation()
    if current.get('revision',0)>=2 and current_id not in ('tavi','mira'):
        from CrewRoleRefinements import apply_role_hand_pose
        apply_role_hand_pose(globals())
    meshes=[o for o in rig.children if o.type=='MESH']
    for obj in meshes:obj.data.calc_loop_triangles()
    audits[identifier]={'vertices':sum(len(o.data.vertices) for o in meshes),'triangles':sum(len(o.data.loop_triangles) for o in meshes),
        'skinned_meshes':len(meshes),'bones':len(rig.data.bones),'clips':list(CLIPS),'reference_based_reconstruction':True}
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in rig.children:obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(ART/'Models'/(identifier+'.fbx')),use_selection=True,
        object_types={'MESH','ARMATURE','EMPTY'},axis_forward='-Z',axis_up='Y',global_scale=1,
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=False,
        use_mesh_modifiers=True,mesh_smooth_type='OFF',use_tspace=True,use_triangles=True,add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
        bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='RELATIVE',colors_type='LINEAR')
    for im in bpy.data.images:
        if im.source=='FILE' and im.filepath and Path(bpy.path.abspath(im.filepath)).is_file():
            im.pack();im.filepath='//../../Assets/_Project/Art/Crew/Textures/'+Path(im.filepath).name
    bpy.ops.wm.save_as_mainfile(filepath=str(SRC/(identifier+'.blend')))
    if os.environ.get('DMT_CHARACTER_RENDER','0')=='1':render_preview(identifier)
    print('CHARACTER-DONE',identifier,json.dumps(audits[identifier]),flush=True)

mat_path=ART/'material-spec.json'
previous=json.loads(mat_path.read_text()) if mat_path.exists() else {}
previous.update(all_materials);mat_path.write_text(json.dumps(previous,indent=2))
audit_path=SRC/'mesh-audit.json'
previous=json.loads(audit_path.read_text()) if audit_path.exists() else {}
previous.update(audits);audit_path.write_text(json.dumps(previous,indent=2))
(SRC/'third_party/provenance.json').write_text(json.dumps({
    'source':'https://github.com/makehumancommunity/mpfb2','asset_license':'CC0-1.0',
    'scope':'Base anatomical facial/hand topology and morph target data only; no addon code is embedded.',
    'files':{str(p.relative_to(source_data)):hashlib.sha256(p.read_bytes()).hexdigest() for p in source_data.rglob('*') if p.is_file()}},indent=2))
print('Crew export complete',flush=True)
