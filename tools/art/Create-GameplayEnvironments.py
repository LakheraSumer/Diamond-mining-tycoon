"""Authored, editable context scenes for the existing built-in-renderer game.

Blender 4.5 LTS -b -t 4 --python-exit-code 1 -P this_file.py
DMT_ENV_IDS optionally selects comma-separated Explore,Mine,Factory,Workshop,
Boutique,RegionMap. No simulation data or saved player state is changed here.
"""
import bpy,bmesh,math,random,json,os
from pathlib import Path
from mathutils import Vector
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/_Project/Art/GameplayEnvironments'
SOURCE=ROOT/'ArtSource/GameplayEnvironments'
for p in (ART/'Models',ART/'Textures',SOURCE):p.mkdir(parents=True,exist_ok=True)
random.seed(195241)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
IDS=os.environ.get('DMT_ENV_IDS','Explore,Mine,Factory,Workshop,Boutique,RegionMap').split(',')
materials={};M={};col=None

def xyz(p):return Vector((p[0],p[2],p[1]))
def linear(rgb):return tuple(v/12.92 if v<.04045 else ((v+.055)/1.055)**2.4 for v in rgb)
def save_image(name,values,normal=False):
    h,w=values.shape[:2];im=bpy.data.images.new(name,width=w,height=h,alpha=True)
    rgba=np.ones((h,w,4),np.float32);rgba[:,:,:3]=values
    im.colorspace_settings.name='Non-Color';im.pixels.foreach_set(rgba.ravel())
    im.filepath_raw=str(ART/'Textures'/(name+'.png'));im.file_format='PNG';im.save()
    im.colorspace_settings.name='Non-Color' if normal else 'sRGB'
    return im
def texture_pair(name,value,height,strength):
    c=np.repeat(value[:,:,None],3,axis=2)
    dx=(np.roll(height,-1,1)-np.roll(height,1,1))*strength
    dy=(np.roll(height,-1,0)-np.roll(height,1,0))*strength
    n=np.stack((-dx,-dy,np.ones_like(dx)),axis=2);n/=np.linalg.norm(n,axis=2,keepdims=True)
    return save_image(name+'_BaseColor',c),save_image(name+'_Normal',n*.5+.5,True)
N=512;y,x=np.mgrid[0:N,0:N]/N;rng=np.random.default_rng(195241)
grain=np.sin((x*48+np.sin(y*math.tau)*1.3)*math.tau)*.09+np.sin(x*math.tau*173)*.02
wood_maps=texture_pair('TimberGrain',np.clip(.83+grain,0,1),.5+grain,1.5)
chips=(rng.random((N,N))>.993).astype(float)
paint_maps=texture_pair('PaintWear',.92-chips*.24+np.sin(x*math.tau*61)*.008,.5+chips*.1,.5)
strata=np.sin(y*math.tau*19+np.sin(x*math.tau*4))*.06+np.sin(x*math.tau*6)*.04
stone_maps=texture_pair('RockStrata',.81+strata,.5+strata,1.2)
soilnoise=rng.random((N,N))*.12
soil_maps=texture_pair('SoilGrain',.82+soilnoise,.5+soilnoise,.3)

def material(key,rgb,rough=.65,metal=0,maps=None,emission=0,alpha=1):
    name='Env_'+key;m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*rgb,alpha)
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*linear(rgb),alpha)
    bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
    if maps:
        tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=maps[0]
        mix=m.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(*linear(rgb),1)
        m.node_tree.links.new(tex.outputs['Color'],mix.inputs[1]);m.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
        nmtex=m.node_tree.nodes.new('ShaderNodeTexImage');nmtex.image=maps[1]
        nm=m.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.4
        m.node_tree.links.new(nmtex.outputs['Color'],nm.inputs['Color']);m.node_tree.links.new(nm.outputs['Normal'],bs.inputs['Normal'])
    if emission:bs.inputs['Emission Color'].default_value=(*linear(rgb),1);bs.inputs['Emission Strength'].default_value=emission
    if alpha<1:bs.inputs['Alpha'].default_value=alpha
    M[key]=m;materials[name]={'color':list(rgb),'roughness':rough,'metallic':metal,'emission':emission,'alpha':alpha,
       'texture':maps[0].name if maps else '', 'normal':maps[1].name if maps else ''}
material('Oak',(.55,.32,.145),maps=wood_maps);material('DarkOak',(.26,.145,.067),maps=wood_maps)
material('LightOak',(.72,.48,.24),maps=wood_maps);material('Floor',(.60,.36,.16),maps=wood_maps)
material('Stone',(.52,.50,.46),.9,maps=stone_maps);material('StoneLight',(.67,.63,.55),.9,maps=stone_maps)
material('StoneDark',(.30,.32,.34),.92,maps=stone_maps);material('Soil',(.65,.49,.29),.95,maps=soil_maps)
material('Grass',(.33,.45,.16),.97);material('Leaf',(.19,.36,.075),.9);material('LeafLight',(.43,.57,.16),.92)
material('Teal',(.19,.48,.44),.49,.1,paint_maps);material('TealLight',(.35,.66,.59),.52,.1,paint_maps)
material('TealDark',(.10,.29,.28),.55,.15,paint_maps);material('Orange',(.88,.48,.105),.53,.25,paint_maps)
material('Steel',(.46,.53,.55),.34,.72);material('Iron',(.13,.17,.18),.56,.55)
material('Brass',(.73,.49,.18),.30,.7);material('Gold',(.98,.67,.13),.25,.7)
material('GemBlue',(.10,.68,.94),.22,.12,emission=.16);material('GemPale',(.68,.89,.95),.28,.1)
material('Glass',(.69,.88,.88),.15,.05,alpha=.20);material('Water',(.17,.67,.80),.18,.12,alpha=.7)
material('Lamp', (1,.65,.19),.3,emission=1.7);material('Paper',(.92,.85,.66),.86)
material('Velvet',(.10,.17,.25),.91);material('Copper',(.68,.32,.17),.43,.55)

def finish(obj,name,key,bevel=0,smooth=False):
    obj.name=name
    if not obj.data.materials:obj.data.materials.append(M[key])
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    if bevel:
        mod=obj.modifiers.new('Applied worn bevel','BEVEL');mod.width=bevel;mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
    if smooth:
        for face in obj.data.polygons:face.use_smooth=True
        mod=obj.modifiers.new('Weighted face normals','WEIGHTED_NORMAL');mod.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    uv=obj.data.uv_layers.new(name='UVMap') if not obj.data.uv_layers else obj.data.uv_layers.active
    for face in obj.data.polygons:
        axis=max(range(3),key=lambda i:abs(face.normal[i]))
        axes=(1,2) if axis==0 else ((0,2) if axis==1 else (0,1))
        for li in face.loop_indices:
            co=obj.data.vertices[obj.data.loops[li].vertex_index].co;uv.data[li].uv=(co[axes[0]]*1.6,co[axes[1]]*1.6)
    colors=obj.data.color_attributes.new(name='ContactAO',type='BYTE_COLOR',domain='CORNER')
    low=min(v.co.z for v in obj.data.vertices);high=max(v.co.z for v in obj.data.vertices)
    for face in obj.data.polygons:
        for li in face.loop_indices:
            co=obj.data.vertices[obj.data.loops[li].vertex_index].co;t=(co.z-low)/max(.001,high-low)
            value=.74+.26*min(1,t*2.5);colors.data[li].color=(value,value,value,1)
    obj.select_set(False);return obj
def mesh(name,verts,faces,key,bevel=0,smooth=False):
    data=bpy.data.meshes.new(name);data.from_pydata([xyz(p) for p in verts],[],faces);data.update()
    obj=bpy.data.objects.new(name,data);col.objects.link(obj);return finish(obj,name,key,bevel,smooth)
def box(name,at,size,key,bevel=.025):
    hx,hy,hz=[v*.5 for v in size]
    vv=[(at[0]+a*hx,at[1]+b*hy,at[2]+c*hz) for c in (-1,1) for b in (-1,1) for a in (-1,1)]
    return mesh(name,vv,[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)],key,bevel,True)
def tube(name,points,radius,key,sides=10):
    points=[Vector(p) for p in points];vv=[];ff=[]
    if isinstance(radius,(int,float)):radius=[radius]*len(points)
    for i,p in enumerate(points):
        d=(points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized();a=d.cross(Vector((0,0,1)))
        if a.length<.01:a=d.cross(Vector((0,1,0)))
        a.normalize();b=d.cross(a).normalized()
        for j in range(sides):vv.append(tuple(p+(a*math.cos(j/sides*math.tau)+b*math.sin(j/sides*math.tau))*radius[i]))
    for i in range(len(points)-1):
        for j in range(sides):ff.append((i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j))
    ff.extend([tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+j for j in range(sides))])
    return mesh(name,vv,ff,key,0,True)
def rod(name,a,b,r,key,sides=12):return tube(name,[a,b],r,key,sides)
def beam(name,a,b,width,depth,key):
    a,b=Vector(a),Vector(b);d=(b-a).normalized();x=d.cross(Vector((0,0,1)))
    if x.length<.01:x=d.cross(Vector((0,1,0)))
    x.normalize();z=d.cross(x).normalized()
    vv=[tuple(p+x*sx*width*.5+z*sz*depth*.5) for p in (a,b) for sz in (-1,1) for sx in (-1,1)]
    return mesh(name,vv,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],key,min(width,depth)*.1,True)
def rock(name,at,size,key='Stone',detail=1):
    vv=[]
    for row,(height,radius) in enumerate(((-.48,.63),(-.20,1),(.24,.88),(.49,.52))):
        for j in range(7):
            a=(j/7+row%2*.035)*math.tau;rr=radius*random.uniform(.84,1.16)
            vv.append((at[0]+math.cos(a)*rr*size[0]*.5,at[1]+(height+random.uniform(-.07,.07))*size[1],at[2]+math.sin(a)*rr*size[2]*.5))
    bm=bmesh.new()
    for p in vv:bm.verts.new(xyz(p))
    bmesh.ops.convex_hull(bm,input=list(bm.verts));data=bpy.data.meshes.new(name);bm.to_mesh(data);bm.free()
    obj=bpy.data.objects.new(name,data);col.objects.link(obj);return finish(obj,name,key,.012 if detail else .0)
def ring(name,at,r,key,thick=.03,axis='z'):
    x,h,d=at
    pts=[(x+math.sin(a)*r,h+math.cos(a)*r,d) if axis=='z' else (x+math.sin(a)*r,h,d+math.cos(a)*r) for a in np.linspace(0,math.tau,33)]
    return tube(name,pts,thick,key,8)
def leaf(name,a,b,width,key='Leaf'):
    a,b=Vector(a),Vector(b);d=b-a;s=Vector((d.z,0,-d.x)).normalized()*width
    mid=a+d*.5+Vector((0,width*.23,0));vv=[tuple(a),tuple(mid+s),tuple(b),tuple(mid-s),tuple(mid+Vector((0,width*.35,0)))]
    return mesh(name,vv,[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(4,1,0),(4,2,1),(4,3,2),(4,0,3)],key)
def bush(at,scale=1):
    x,h,d=at
    for j in range(8):
        a=j/8*math.tau;end=(x+math.sin(a)*.35*scale,h+random.uniform(.25,.5)*scale,d+math.cos(a)*.35*scale)
        rod('Shrub_stem',(x,h,d),end,.012*scale,'DarkOak',6)
        leaf('Broad_leaf',(x,h,d),end,.09*scale,'LeafLight' if j%3==0 else 'Leaf')
        for t in (.45,.7):
            p=Vector((x,h,d)).lerp(Vector(end),t)
            leaf('Fern_leaflet',tuple(p),tuple(p+Vector((math.sin(a+1)*.15,.10,math.cos(a+1)*.15))*scale),.045*scale)
def pine(at,scale=1):
    x,h,d=at;rod('Pine_trunk',(x,h,d),(x,h+1.5*scale,d),.09*scale,'DarkOak',9)
    for level in range(4):
        y=h+(.55+level*.34)*scale;r=(.68-level*.115)*scale
        for j in range(9):
            a=j/9*math.tau+level*.21
            leaf('Pine_bough',(x,h+1.7*scale,d),(x+math.sin(a)*r,y,d+math.cos(a)*r),.26*scale,'Leaf' if j%2 else 'LeafLight')
def lantern(at):
    x,h,d=at
    for dh in (-.18,.18):box('Lantern_cap',(x,h+dh,d),(.25,.055,.23),'Iron',.012)
    box('Lantern_warm_glass',at,(.15,.31,.14),'Lamp',.014)
    for dx in (-.105,.105):
        for dz in (-.095,.095):rod('Lantern_corner',(x+dx,h-.16,d+dz),(x+dx,h+.16,d+dz),.011,'Brass',7)
    ring('Lantern_ring',(x,h+.24,d),.047,'Iron',.01)
def ore_load(at,count=20,r=.12,gem=False):
    x,h,d=at
    for j in range(count):
        a=random.random()*math.tau;rr=random.random()*.42
        rock('Recovered_crystal' if gem else 'Ore_chunk',(x+math.sin(a)*rr,h+random.uniform(0,.10),d+math.cos(a)*rr),(r*random.uniform(.8,1.3),r*(1.6 if gem else 1),r),'GemBlue' if gem else random.choice(('Stone','StoneLight','StoneDark')),0)
def rail_line(points):
    profile=[(-.058,0),(.058,0),(.058,.027),(.022,.037),(.022,.105),(.047,.11),(.047,.151),(-.047,.151),(-.047,.11),(-.022,.105),(-.022,.037),(-.058,.027)]
    for sign in (-1,1):
        vv=[];ff=[]
        for i,(x,d) in enumerate(points):
            prev=Vector(points[max(0,i-1)]);nex=Vector(points[min(len(points)-1,i+1)]);delta=(nex-prev).normalized();side=Vector((delta.y,-delta.x))
            for px,hh in profile:vv.append((x+side.x*(sign*.32+px),.08+hh,d+side.y*(sign*.32+px)))
        n=len(profile)
        for i in range(len(points)-1):
            for j in range(n):ff.append((i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j))
        mesh('Continuous_I_profile_rail',vv,ff,'Steel',.004,True)
    for i in range(0,len(points),3):
        x,d=points[i];prev=Vector(points[max(0,i-1)]);nex=Vector(points[min(len(points)-1,i+1)]);side=Vector(((nex-prev).y,-(nex-prev).x)).normalized()
        beam('Rail_sleeper',(x-side.x*.55,.045,d-side.y*.55),(x+side.x*.55,.045,d+side.y*.55),.14,.10,'DarkOak')
        for sign in (-1,1):box('Rail_chair',(x+side.x*sign*.32,.11,d+side.y*sign*.32),(.18,.025,.18),'Iron',.005)
def fence(a,b,height=.85):
    a,b=Vector(a),Vector(b);length=(b-a).length;n=max(1,math.ceil(length/.9))
    for i in range(n+1):
        p=a.lerp(b,i/n);rod('Safety_fence_post',tuple(p),tuple(p+Vector((0,height,0))),.035,'Orange',9)
    for h in (.32,height):beam('Safety_fence_rail',tuple(a+Vector((0,h,0))),tuple(b+Vector((0,h,0))),.045,.045,'Orange')
def plank_floor(xrange,zrange,h=.0):
    x0,x1=xrange;z0,z1=zrange
    count=math.ceil((z1-z0)/.23)
    for j in range(count):box('Floorboard',((x0+x1)/2,h,z0+(j+.5)/count*(z1-z0)),(x1-x0,.09,(z1-z0)/count-.012),'Floor',.012)
def timber_wall(xrange,z,height=3.5):
    x0,x1=xrange
    for j in range(math.ceil((x1-x0)/.32)):
        x=x0+(j+.5)/math.ceil((x1-x0)/.32)*(x1-x0);box('Wall_timber_board',(x,height/2,z),(.305,height,.12),'Oak',.01)
    for x in (x0,x1,(x0+x1)/2):box('Wall_bearing_post',(x,height/2,z-.08),(.20,height+.14,.22),'DarkOak',.03)
    for h in (.18,2.95):box('Wall_horizontal_rail',((x0+x1)/2,h,z-.12),(x1-x0,.18,.15),'DarkOak',.02)
def worktable(at,width=2.2,depth=.9):
    x,h,d=at;box('Bench_top',(x,h+.90,d),(width,.16,depth),'LightOak',.045)
    for sx in (-1,1):
        for sz in (-1,1):box('Bench_leg',(x+sx*(width/2-.15),h+.43,d+sz*(depth/2-.12)),(.17,.85,.17),'DarkOak',.025)
    box('Bench_crossbar',(x,h+.26,d),(width-.15,.13,.12),'DarkOak',.015)
def cabinet(at,width=1.8,height=1.4):
    x,h,d=at;box('Cabinet_case',(x,h+height/2,d),(width,height,.70),'Oak',.045)
    for row in range(3):
        box('Cabinet_drawer',(x,h+(row+.5)/3*height,d-.37),(width-.12,height/3-.055,.08),'LightOak',.02)
        rod('Drawer_handle',(x-.16,h+(row+.5)/3*height,d-.43),(x+.16,h+(row+.5)/3*height,d-.43),.023,'Brass',10)
def gem(at,r=.12,key='GemBlue'):
    x,h,d=at;vv=[]
    for hh,rr in ((-.07,.15),(0,.22),(.14,.13)):
        for j in range(8):a=j/8*math.tau;vv.append((x+math.sin(a)*rr*r/.15,h+hh*r/.15,d+math.cos(a)*rr*r/.15))
    vv.append((x,h-.25*r/.15,d));ff=[tuple(range(16,24))]
    for row in range(2):
        for j in range(8):ff.append((row*8+j,row*8+(j+1)%8,(row+1)*8+(j+1)%8,(row+1)*8+j))
    for j in range(8):ff.append((24,j,(j+1)%8))
    mesh('Faceted_diamond',vv,ff,key,.001)

with bpy.data.libraries.load(str(ROOT/'ArtSource/TimberBunker/TimberBunker.blend'),link=False) as (available,loaded):
    loaded.collections=['TimberBunker']
template_bunker=loaded.collections[0]
def imported_bunker(plot):
    portal_x=8.2 if plot==5 else -8.2+plot*2.7
    shift=xyz((portal_x,0,6.8))
    for source in template_bunker.objects:
        if source.type!='MESH' or source.name.endswith('_PlaqueLetter'):continue
        obj=source.copy();obj.data=source.data.copy();obj.location+=shift;obj.name='Starter'+str(plot+1)+'_'+source.name;col.objects.link(obj)
    # Numerals are geometry, not painted reference pixels.
    patterns={'2':'abged','3':'abgcd','4':'fgbc','5':'afgcd','6':'afgecd'}
    paths={'a':((-.1,.13),(.1,.13)),'b':((.1,.13),(.1,0)),'c':((.1,0),(.1,-.13)),'d':((-.1,-.13),(.1,-.13)),
           'e':((-.1,-.13),(-.1,0)),'f':((-.1,0),(-.1,.13)),'g':((-.1,0),(.1,0))}
    for segment in patterns[str(plot+1)]:
        a,b=paths[segment];beam('Raised_number_'+str(plot+1),(portal_x+a[0],2.42+a[1],6.51),(portal_x+b[0],2.42+b[1],6.51),.032,.022,'Paper')
def explore():
    # Organic terrain, paved work paths, layered cliff and modelled vegetation.
    nx,nz=44,42;vv=[];ff=[]
    for iz in range(nz+1):
        z=-4.5+iz/nz*21
        for ix in range(nx+1):
            x=-11+ix/nx*22;h=-.09+.035*math.sin(x*.6)*math.cos(z*.7)
            vv.append((x,h,z))
    for iz in range(nz):
        for ix in range(nx):a=iz*(nx+1)+ix;ff.append((a,a+1,a+nx+2,a+nx+1))
    mesh('Contoured_grass_terrain',vv,ff,'Grass',0,True)
    box('Layered_earth_base',(0,-.48,6),(22,.7,21),'Soil',.28)
    for j in range(25):
        z=-3.8+j*.43;cx=-.7+math.sin(z*.5)*.7
        box('Packed_soil_path',(cx,-.035,z),(3.1,.07,.48),'Soil',.08)
    for plot in range(1,6):
        imported_bunker(plot)
        x=8.2 if plot==5 else -8.2+plot*2.7
        for j in range(5):box('Bunker_approach',(x,-.02,5.0+j*.35),(1.55,.08,.38),'Soil',.04)
    # Leave a cutout under the original reviewed terrain patch around plot 0.
    for row in range(3):
        for j in range(19):
            x=-10.4+j*1.10;z=9.0+row*.77
            rock('Layered_back_cliff',(x,1.8+row*.78,z),(1.6,2.7,1.5),random.choice(('Stone','Stone','StoneLight','StoneDark')))
    for j in range(33):
        x=random.uniform(-10.5,10.5);z=random.uniform(-4.0,14.8)
        if abs(x)<2.1 or (z>4.0 and z<8.5):continue
        rock('Terrain_embedded_stone',(x,.06,z),(.45,.35,.52),random.choice(('Stone','StoneLight')))
        bush((x+.25,0,z+.15),random.uniform(.7,1.3))
    for j in range(15):pine((-10.1+j*1.42,3.1+random.uniform(0,.4),11.1+random.uniform(-.3,.3)),random.uniform(.9,1.5))
    for x,z in [(-9,-3.3),(-7,-3.5),(8.6,-3.4),(10,0),(9.8,10),(-10.4,1.5)]:pine((x,0,z),random.uniform(.8,1.25))
    # Curving haul line with continuous profiled steel and actual sleepers.
    rail_line([(-7.5+i/34*14,.0+4.86) for i in range(35)])
    for x in (-8.0,-6.5,-5.0,-3.5,-2.0):
        box('Ore_bin_floor',(x,.10,-2.6),(1.25,.15,.9),'DarkOak',.03)
        for side in (-1,1):box('Ore_bin_side',(x+side*.61,.44,-2.6),(.09,.70,.95),'Oak',.02)
        box('Ore_bin_back',(x,.44,-2.16),(1.25,.70,.09),'Oak',.025)
        box('Ore_bin_label',(x,.42,-3.07),(.8,.28,.06),'Paper',.015)
        ore_load((x,.35,-2.6),17,.18)
    # A shallow stream with a small timber footbridge at the west foreground.
    for j in range(12):
        z=-4+j*.6;x=-10.1+.45*math.sin(z*.8)
        box('Stream_surface',(x,-.09,z),(1.12,.035,.72),'Water',.05)
        for side in (-1,1):rock('Stream_bank',(x+side*.67,-.04,z),(.55,.45,.65),'StoneLight')
    for j in range(7):box('Footbridge_plank',(-10.1+(j-3)*.23,.13,-.5),(.22,.10,1.3),'LightOak',.018)
    for z in (-1.13,.13):
        for x in (-10.85,-9.35):box('Footbridge_post',(x,.48,z),(.10,.85,.10),'DarkOak',.02)
        beam('Footbridge_handrail',(-10.85,.85,z),(-9.35,.85,z),.07,.08,'Oak')
    for x in (-3.2,3.8):
        rod('Path_lamp_post',(x,0,-1.7),(x,1.65,-1.7),.065,'DarkOak')
        lantern((x,1.75,-1.7))

def mine():
    # Open front, excavated rock chamber, warm supports, multiple crystal veins.
    box('Cave_soil_floor',(0,-.12,.2),(7.6,.3,8.2),'Soil',.22)
    for row in range(4):
        for j in range(8):rock('Cave_back_course',(-3.6+j*1.03,.4+row*.9,3.15), (1.4,1.28,1.25),random.choice(('Stone','StoneDark')))
    for side in (-1,1):
        for z in (-2.8,-1.2,.5,2.1):
            for h in (.4,1.5,2.6):rock('Cave_sidewall',(side*3.5,h,z),(1.3,1.5,2.1),'StoneDark' if h>2 else 'Stone')
        for depth in (-1.7,.6,2.6):
            box('Tunnel_support',(side*2.85,1.65,depth),(.27,3.3,.31),'Oak',.045)
            box('Foot_iron_bracket',(side*2.85,.18,depth),(.33,.28,.38),'Iron',.02)
    for depth in (-1.7,.6,2.6):
        box('Tunnel_header',(0,3.25,depth),(6.2,.30,.34),'Oak',.045)
        for side in (-1,1):beam('Tunnel_knee_brace',(side*2.8,2.65,depth),(side*2.2,3.2,depth),.18,.2,'DarkOak')
    for side in (-1,1):
        for z in (-1.4,1.3):lantern((side*2.62,2.25,z))
    rail_line([(1.25-.18*math.sin(i/30*math.pi),-3.7+i/30*6.0) for i in range(31)])
    rock('Mineable_ore_mass',(-.75,1.00,.15),(1.8,2.05,1.3),'StoneDark')
    for at,size in [((-.95,1.95,.2),.22),((-.45,1.55,-.34),.17),((-1.25,.85,-.30),.19),((-.65,.65,-.48),.12),((-2.8,2.4,1.4),.22),((1.8,2.7,2.7),.18)]:
        gem(at,size);gem((at[0]+.13,at[1]-.2,at[2]+.08),size*.6,'GemPale')
    for j in range(28):rock('Mine_floor_talus',(random.uniform(-2.9,2.9),.08,random.uniform(-3.1,2.5)),(.12,.17,.15),'StoneLight',0)
    box('Mine_supply_crate',(-2.3,.34,1.5),(.8,.7,.7),'Oak',.03)
    for sign in (-1,1):box('Crate_iron_band',(-2.3+sign*.25,.35,1.5),(.07,.73,.73),'Iron',.01)

def conveyor(a,b,width=.62,h=.72,ore=True):
    a,b=Vector(a),Vector(b);d=(b-a).normalized();side=Vector((d.z,0,-d.x));length=(b-a).length
    for sign in (-1,1):beam('Conveyor_side_frame',tuple(a+side*sign*(width/2+.07)+Vector((0,h,0))),tuple(b+side*sign*(width/2+.07)+Vector((0,h,0))),.12,.13,'Teal')
    for i in range(max(3,math.ceil(length/.18))):
        p=a.lerp(b,i/max(2,math.ceil(length/.18)-1));pa=p+side*width/2+Vector((0,h,0));pb=p-side*width/2+Vector((0,h,0))
        rod('Conveyor_roller',tuple(pa),tuple(pb),.055,'Iron',10)
        if ore and i%3==0:rock('Conveyor_ore',tuple(p+Vector((0,h+.11,0))),(.13,.14,.15),'StoneLight',0)
    for i in range(max(2,math.ceil(length/1.3))):
        p=a.lerp(b,i/max(1,math.ceil(length/1.3)-1))
        for sign in (-1,1):box('Conveyor_support_leg',tuple(p+side*sign*width*.42+Vector((0,h*.46,0))),(.095,h*.95,.1),'Steel',.012)
def panel_face(at,width=.75,height=.65):
    x,h,d=at;box('Machine_control_panel',at,(width,height,.12),'TealDark',.025)
    for i in range(3):
        rod('Control_button',(x-.23+i*.23,h-.13,d-.07),(x-.23+i*.23,h-.13,d-.10),.045,'Lamp' if i==1 else 'Orange',12)
    box('Control_meter',(x,h+.12,d-.08),(width*.72,.16,.035),'Paper',.012)
def hopper(at):
    x,h,d=at;vv=[]
    for yy,xx,zz in ((h,.36,.32),(h+.65,.65,.58),(h+.65,.56,.49),(h+.08,.29,.25)):
        vv.extend([(x-xx,yy,d-zz),(x+xx,yy,d-zz),(x+xx,yy,d+zz),(x-xx,yy,d+zz)])
    ff=[]
    for ring in range(3):
        for j in range(4):ff.append((ring*4+j,ring*4+(j+1)%4,(ring+1)*4+(j+1)%4,(ring+1)*4+j))
    mesh('Tapered_thick_hopper',vv,ff,'Steel',.025,True);ore_load((x,h+.48,d),12,.18)
def factory():
    box('Factory_concrete_plinth',(0,-.17,.6),(11,.34,7.5),'StoneLight',.16)
    for x in np.linspace(-5,5,12):box('Factory_floor_joint',(x,.008,.6),(.025,.008,7.0),'StoneDark',.001)
    for x in (-5.1,0,5.1):
        box('Factory_structural_column',(x,2,3.9),(.25,4,.28),'Teal',.025)
        box('Column_foot',(x,.12,3.9),(.45,.22,.48),'Steel',.025)
    box('Factory_overhead_beam',(0,3.95,3.9),(10.8,.29,.35),'Orange',.035)
    box('Factory_back_knee_wall',(0,.65,4.2),(10.8,1.3,.22),'Stone',.04)
    conveyor((-4.8,0,-1.3),(4.8,0,-1.3),.68,.74)
    for x in (-3.8,-1.9,0,1.9,3.8):conveyor((x,0,-1.2),(x,0,1.1),.65,.78)
    # Crusher: feed hopper, grizzly throat, motor, belt guard, collection chute.
    x=-3.8
    box('Crusher_chassis',(x,.50,1.0),(1.35,1.0,1.4),'Teal',.10);hopper((x,1.25,1.0))
    for h in (.65,.95):box('Crusher_casting_rib',(x,h,.25),(1.45,.07,.10),'TealLight',.015)
    for side in (-1,1):rod('Crusher_shaft',(x+side*.75,.95,.7),(x+side*.75,.95,1.28),.25,'Steel',18)
    panel_face((x,1.12,.19))
    # Washer: clear-water drum with open bracing, pipework, bath and paddles.
    x=-1.9
    box('Washer_water_bath',(x,.40,1.1),(1.6,.68,1.7),'Teal',.09)
    box('Washer_surface',(x,.77,1.1),(1.38,.04,1.45),'Water',.035)
    rod('Wash_drum',(x-.72,1.15,1.1),(x+.72,1.15,1.1),.52,'Glass',32)
    for t in (-.70,-.36,0,.36,.70):
        pts=[(x+t,1.15+math.sin(a)*.535,1.1+math.cos(a)*.535) for a in np.linspace(0,math.tau,33)]
        tube('Drum_retaining_ring',pts,.025,'Steel',8)
    for t in np.linspace(0,math.tau,8):rod('Drum_internal_paddle',(x-.6,1.15+math.sin(t)*.43,1.1+math.cos(t)*.43),(x+.6,1.15+math.sin(t)*.43,1.1+math.cos(t)*.43),.035,'TealLight')
    tube('Wash_inlet_pipe',[(x+.8,.2,2),(x+.8,1.8,2),(x+.2,1.8,2),(x+.2,1.6,1.25)],.08,'Steel',12)
    # Sorting station: divided ore lanes, separation screens and resource bins.
    x=0;box('Sort_machine_base',(x,.55,1.05),(1.5,1.1,1.65),'Teal',.07)
    for side in (-.45,0,.45):
        conveyor((x+side,0,.55),(x+side,0,1.85),.32,1.18)
        box('Sort_output_bin',(x+side,.25,-.28),(.39,.5,.55),'Orange',.025)
    box('Sorting_inspection_hood',(x,1.95,1.8),(1.6,.12,.4),'TealDark',.03)
    # Cutter: four-sided metal/glass safety booth, table, diamond wheel.
    x=1.9;box('Cutter_base',(x,.40,1.0),(1.55,.8,1.7),'Teal',.075)
    for sx in (-.72,.72):
        for z in (.2,1.8):box('Cut_booth_corner',(x+sx,1.55,z),(.075,1.55,.075),'Steel',.012)
    box('Cut_booth_roof',(x,2.35,1),(1.65,.12,1.8),'TealLight',.03)
    box('Cut_booth_glass',(x,1.55,.2),(1.38,1.42,.025),'Glass',.005)
    rod('Diamond_cutting_wheel',(x,1.05,.75),(x,1.05,1.2),.32,'Steel',24)
    gem((x-.4,1.0,.7),.10)
    panel_face((x,1.12,.05),.60,.5)
    # Polisher: twin buffing wheels, spindle, blue-gem tray and lit inspection hood.
    x=3.8;box('Polisher_bench',(x,.50,1.0),(1.5,1,1.55),'Teal',.08)
    for sx in (-.42,.42):rod('Buffing_wheel',(x+sx,1.18,.65),(x+sx,1.18,1.03),.25,'Paper',24)
    box('Inspection_tray',(x,1.08,.30),(1.12,.09,.43),'Steel',.015)
    for j in range(5):gem((x-.4+j*.2,1.17,.30),.07)
    for x in (-4.6,-.8,2.9):
        tube('Factory_overhead_pipe',[(x,.25,3.8),(x,3.4,3.8),(x+1.1,3.4,3.8),(x+1.1,.85,2.9)],.085,'Steel',12)
        ring('Pipe_valve',(x,1.7,3.68),.16,'Orange',.025)
    fence((-5.2,0,-2.7),(5.2,0,-2.7));fence((-5.2,0,-2.7),(-5.2,0,3.6));fence((5.2,0,-2.7),(5.2,0,3.6))
    for at in [(-4.8,2.7,3.6),(.0,2.7,3.6),(4.8,2.7,3.6)]:lantern(at)

def workshop():
    plank_floor((-3.5,3.5),(-3.0,3.8));timber_wall((-3.5,3.5),3.8)
    box('Workshop_sidewall',(-3.5,1.55,.8),(.18,3.1,6),'Oak',.03)
    worktable((0,0,.7),3.7,1.6)
    for sx in (-1,1):cabinet((sx*2.6,0,2.9),1.5,1.3)
    for h in (1.8,2.6):
        box('Workshop_wall_shelf',(0,h,3.42),(5.8,.15,.65),'DarkOak',.025)
        for j in range(12):
            x=-2.5+j*.46
            if j%3:box('Bullion_bar',(x,h+.15,3.4),(.32,.18,.19),'Gold' if j%2 else 'Steel',.025)
            else:gem((x,h+.18,3.4),.1)
    # Anvil, vice, pin cushion and shaded work light.
    box('Jeweller_anvil_base',(-.8,1.12,.6),(.4,.3,.36),'Steel',.035)
    beam('Anvil_horn',(-1.05,1.27,.6),(-1.45,1.25,.6),.17,.19,'Steel')
    box('Bench_vice',(.72,1.14,.11),(.45,.35,.40),'TealDark',.035)
    rod('Vice_screw',(.5,1.08,-.12),(.95,1.08,-.12),.025,'Steel')
    ring('Ring_in_vice',(.72,1.42,.11),.1,'Gold',.018)
    gem((.72,1.53,.11),.06)
    tube('Adjustable_bench_lamp',[(1.4,1.02,.9),(1.4,1.55,.9),(.9,1.9,.85),(.3,1.75,.75)],.035,'Iron')
    tube('Lamp_shade',[(.3,1.82,.75),(.3,1.70,.75)],[.20,.28],'TealDark',24)
    rod('Bench_lamp_core',(.3,1.70,.75),(.3,1.69,.75),.21,'Lamp',20)
    box('Gem_drawer_tray',(-.2,1.02,-.45),(1.6,.12,.65),'Oak',.025)
    for j in range(6):gem((-.8+j*.25,1.15,-.45),.085)
    for x in (-2.3,2.3):lantern((x,2.45,3.45))
    for x in (-2.7,2.7):bush((x,1.35,2.6),.8)

def jewellery_stand(at):
    x,h,d=at;box('Display_pedestal',(x,h+.07,d),(.50,.14,.42),'Velvet',.035)
    ring('Display_ring',(x,h+.26,d),.11,'Gold',.025);gem((x,h+.4,d),.07)
def boutique():
    plank_floor((-3.7,3.7),(-3.9,3.7));timber_wall((-3.7,3.7),3.7)
    box('Boutique_cream_wall',(-3.7,1.55,.0),(.18,3.1,7.4),'Paper',.03)
    # Jewellery cabinet with mullions, shelves, padded stands and glass front.
    box('Boutique_counter_base',(0,.48,.3),(5.0,.96,1.05),'Oak',.07)
    box('Boutique_counter_top',(0,1.40,.3),(5.2,.12,1.17),'LightOak',.04)
    box('Counter_velvet_bed',(0,1.0,.3),(4.75,.035,.89),'Velvet',.015)
    for x in (-2.48,-1.24,0,1.24,2.48):box('Counter_vertical_mullion',(x,1.20,-.25),(.055,.42,.06),'Brass',.008)
    box('Counter_front_glass',(0,1.20,-.25),(4.9,.4,.022),'Glass',.004)
    for x in (-1.8,-.6,.6,1.8):jewellery_stand((x,1.03,.3))
    for x in (-2.8,2.8):
        cabinet((x,0,2.8),1.2,1.1)
        for h in (1.4,2.2):
            box('Boutique_display_shelf',(x,h,3.05),(1.32,.12,.65),'DarkOak',.022)
            jewellery_stand((x,h+.07,3.05))
    # Back feature display and dimensional diamond emblem.
    box('Diamond_emblem_backing',(0,2.13,3.51),(1.9,1.75,.10),'TealDark',.065)
    gem((0,2.13,3.35),.38,'GemPale')
    for x in (-2.4,2.4):lantern((x,2.8,3.38))
    for x in (-3.2,3.2):
        tube('Boutique_planter',[(x,0,-1.9),(x,.45,-1.9)],[.30,.38],'Teal',20)
        for j in range(3):bush((x,.4,-1.9),1.0+j*.2)
    # Small presentation box on the counter. The lid has a real hinge axis.
    box('Presentation_box_base',(.9,1.50,.15),(.52,.17,.42),'Velvet',.045)
    lid=box('Presentation_box_open_lid',(.9,1.88,.35),(.52,.58,.09),'Velvet',.035)
    for side in (-1,1):rod('Jewellery_box_hinge',(.9+side*.19,1.57,.36),(.9+side*.12,1.57,.36),.035,'Brass')
    jewellery_stand((.9,1.57,.12))

def island(at,size):
    x,h,d=at;sx,sy,sz=size
    rock('Island_stone_mass',(x,h-sy*.45,d),(sx,sy,sz),'StoneLight')
    # Irregular broad top creates a grass silhouette rather than a flat box.
    vv=[(x,h+.02,d)]
    for j in range(24):
        a=j/24*math.tau;r=.5*(1+.08*math.sin(j*2.3));vv.append((x+math.sin(a)*sx*r,h+.04*math.sin(a*3),d+math.cos(a)*sz*r))
    mesh('Island_grass_crown',vv,[(0,j+1,(j+1)%24+1) for j in range(24)],'Grass')
def regional_map():
    box('Map_water_plane',(0,-.8,1.0),(20,.12,23),'Water',.2)
    islands=[(-3.6,.1,1.2,6.4,2.3,8.2),(3.9,.05,3.1,5.4,2.1,6.5),(-.3,.15,-5.4,7.1,2.2,4.8),(1.2,.2,8.9,6.7,3.1,5.4),(-6.4,.0,8.4,3.1,1.5,3.4)]
    for x,h,d,sx,sy,sz in islands:
        island((x,h,d),(sx,sy,sz))
        for j in range(7):
            a=j/7*math.tau;pine((x+math.sin(a)*sx*.30,h+.04,d+math.cos(a)*sz*.27),random.uniform(.45,.8))
        for j in range(4):rock('Map_mountain',(x+random.uniform(-sx*.2,sx*.2),h+.6,d+sz*.25),(1.4,2.1,1.3),'StoneLight')
    # Six dimensional mine markers are tied to this view by the UI manifest.
    for i,(x,d) in enumerate([(-4.7,.4),(-2.2,2.4),(-4.1,3.8),(3.2,2.4),(.5,-5.6),(1.3,8.5)]):
        box('Map_portal_header',(x,.67,d),(1.04,.16,.26),'Oak',.035)
        for side in (-1,1):box('Map_portal_post',(x+side*.40,.34,d),(.17,.67,.21),'Oak',.025)
        box('Map_dark_tunnel',(x,.33,d+.17),(.69,.62,.18),'Iron',.025)
        for side in (-1,1):rock('Map_portal_rock',(x+side*.58,.5,d+.12),(.52,1.0,.60),'Stone')
        gem((x+.57,.22,d-.34),.12)
    # Tiny bridges and stepping routes between islands.
    for a,b in [((-1.1,.15,1.0),(1.4,.15,2.2)),((-.5,.15,-3.8),(-1.6,.15,-1.3)),((2.6,.15,5.5),(2.0,.15,7.0))]:
        a,b=Vector(a),Vector(b);count=math.ceil((b-a).length/.22)
        for j in range(count+1):
            p=a.lerp(b,j/count);box('Map_bridge_plank',tuple(p),(.65,.08,.20),'LightOak',.015)

BUILDERS={'Explore':explore,'Mine':mine,'Factory':factory,'Workshop':workshop,'Boutique':boutique,'RegionMap':regional_map}
stats={}
for identifier in IDS:
    identifier=identifier.strip();bpy.ops.object.select_all(action='DESELECT')
    col=bpy.data.collections.new(identifier);bpy.context.scene.collection.children.link(col)
    BUILDERS[identifier]()
    # Static batches retain UVs and contact colours; the game never recreates them.
    groups={}
    for obj in list(col.objects):
        if obj.type=='MESH':groups.setdefault(obj.active_material.name,[]).append(obj)
    for mat,objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        if len(objects)>1:bpy.ops.object.join()
        objects[0].name=identifier+'_'+mat;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in col.objects:obj.select_set(True);obj.data.calc_loop_triangles()
    stats[identifier]={'meshes':len(col.objects),'vertices':sum(len(o.data.vertices) for o in col.objects),
       'triangles':sum(len(o.data.loop_triangles) for o in col.objects),'uv_meshes':sum(bool(o.data.uv_layers) for o in col.objects),
       'source':'authored meshes; original timber portal reused for the starter row','inferred_composition':True}
    bpy.ops.export_scene.fbx(filepath=str(ART/'Models'/(identifier+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',
       apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,use_mesh_modifiers=True,
       mesh_smooth_type='OFF',use_tspace=True,use_triangles=True,bake_anim=False,path_mode='RELATIVE',colors_type='LINEAR')
    print('ENVIRONMENT-DONE',identifier,json.dumps(stats[identifier]),flush=True)
    # Save only this context as an editable source, preserving packed texture maps.
    for im in bpy.data.images:
        if im.source=='FILE' and Path(bpy.path.abspath(im.filepath)).is_file():im.pack()
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(identifier+'.blend')))
    for obj in list(col.objects):bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.collections.remove(col)
(ART/'material-spec.json').write_text(json.dumps(materials,indent=2))
previous=json.loads((SOURCE/'mesh-audit.json').read_text()) if (SOURCE/'mesh-audit.json').exists() else {}
previous.update(stats);(SOURCE/'mesh-audit.json').write_text(json.dumps(previous,indent=2))
print('Gameplay environment export complete.',flush=True)
