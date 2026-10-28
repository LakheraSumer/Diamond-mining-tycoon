"""Original Blender-authored timber mine vignette. Run with Blender 4.5 LTS -b -P.

Coordinates in the modelling helpers are Unity-style x / height / depth. FBX is
exported in metres, with applied bevels, UV0, corner normals and vertex AO.
No reference pixels, downloaded models or runtime primitives are used.
"""
import bpy, bmesh, math, random, json, os
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/_Project/Art/TimberBunker"
SOURCE = ROOT / "ArtSource/TimberBunker"
for p in (ART / "Models", ART / "Textures", SOURCE, ROOT / "release/screenshots/timber-bunker"):
    p.mkdir(parents=True, exist_ok=True)
random.seed(41027)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for data in list(bpy.data.materials):
    bpy.data.materials.remove(data)

def xyz(p):
    # FBX + Unity bakeAxisConversion maps Blender Y to Unity Z. Mesh normals
    # are recalculated after this handedness change, before the export.
    return (p[0], p[2], p[1])

def image_file(name, data):
    h, w = data.shape[:2]
    im = bpy.data.images.new(name, width=w, height=h, alpha=True)
    rgba = np.ones((h, w, 4), dtype=np.float32)
    rgba[:, :, :data.shape[2]] = data
    im.pixels.foreach_set(rgba.ravel())
    im.filepath_raw = str(ART / "Textures" / (name + '.png'))
    im.file_format = 'PNG'
    im.save()
    return im

def maps(name, color, height, strength=2):
    base = image_file(name + '_BaseColor', np.clip(color, 0, 1))
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * strength
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * strength
    normal = np.stack((-dx, -dy, np.ones_like(dx)), axis=2)
    normal /= np.linalg.norm(normal, axis=2, keepdims=True)
    norm = image_file(name + '_Normal', normal * .5 + .5)
    norm.colorspace_settings.name = 'Non-Color'
    return base, norm

# Tileable, hand-directed material patterns; seed and source are reproducible.
N = 1024
y, x = np.mgrid[0:N, 0:N] / N
rng = np.random.default_rng(41027)
grain = np.sin((x * 62 + 1.7*np.sin(y*math.tau) + .18*np.sin(y*39)) * math.tau)
fine = np.sin((x*243 + np.sin(y*math.tau)*2) * math.tau)
wood_h = .5 + grain*.08 + fine*.018
wood_c = np.stack((.46+grain*.032, .268+grain*.023, .12+grain*.015), axis=2)
for cx, cy in ((.23,.36),(.77,.81)):
    d = np.sqrt(((x-cx)*2.7)**2 + ((y-cy)*.65)**2)
    rings = np.sin(d*170 + .4*np.sin(y*22)) * np.exp(-d*12)
    wood_c -= rings[:,:,None] * np.array([.075,.05,.03])
wood_maps = maps('AgedOak', wood_c, wood_h, 1.8)
stone_h = .5 + .19*np.sin(x*math.tau*7+np.sin(y*math.tau*3)) + .08*np.sin(y*math.tau*21+x*20)
stone_c = np.stack((.38+stone_h*.08, .365+stone_h*.075, .36+stone_h*.095), axis=2)
stone_maps = maps('StratifiedStone', stone_c, stone_h, 1.2)
soil_h = rng.random((N,N)).astype(np.float32)*.12 + .08*np.sin(x*math.tau*8)*np.sin(y*math.tau*11)
soil_c = np.stack((.51+soil_h*.24, .365+soil_h*.21, .205+soil_h*.18),axis=2)
soil_maps = maps('PackedEarth', soil_c, soil_h, .4)
steel_h = .5 + np.sin(y*math.tau*47)*.025 + rng.random((N,N))*.03
steel_c = np.stack((.36+steel_h*.08,.39+steel_h*.08,.41+steel_h*.08),axis=2)
steel_maps = maps('ForgedSteel', steel_c, steel_h, .6)

materials = {}
def mat(name, color, rough=.65, metal=0, textures=None, emission=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color,1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color,1)
    bs.inputs['Roughness'].default_value = rough
    bs.inputs['Metallic'].default_value = metal
    if textures:
        tex = m.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=textures[0]
        m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
        norm = m.node_tree.nodes.new('ShaderNodeTexImage'); norm.image=textures[1]
        nm = m.node_tree.nodes.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value=.45
        m.node_tree.links.new(norm.outputs['Color'],nm.inputs['Color'])
        m.node_tree.links.new(nm.outputs['Normal'],bs.inputs['Normal'])
    if emission:
        bs.inputs['Emission Color'].default_value=(*color,1)
        bs.inputs['Emission Strength'].default_value=emission
    materials[name] = {'color':color,'roughness':rough,'metallic':metal,'texture':textures[0].name if textures else None,
                       'normal':textures[1].name if textures else None,'emission':emission}
    return m

wood = mat('Oak',(.54,.30,.12),textures=wood_maps)
wood_end = mat('OakEnd',(.64,.39,.18),rough=.77)
darkwood = mat('TunnelOak',(.25,.13,.06),rough=.85)
stone = mat('Rock',(.51,.48,.47),textures=stone_maps)
rock_light = mat('RockLight',(.49,.47,.44),rough=.88)
rock_dark = mat('RockShadow',(.34,.35,.37),rough=.88)
soil = mat('Soil',(.60,.43,.24),textures=soil_maps)
soil_dark = mat('EarthCut',(.32,.235,.15),rough=.96)
moss = mat('Moss',(.28,.36,.10),rough=.95)
steel = mat('Steel',(.30,.34,.36),rough=.46,metal=.65,textures=steel_maps)
steel_edge = mat('WornSteel',(.52,.57,.59),rough=.32,metal=.7)
rust = mat('Oxide',(.29,.18,.095),rough=.8,metal=.25)
iron = mat('BlackIron',(.12,.15,.155),rough=.42,metal=.6)
brass = mat('Brass',(.65,.40,.12),rough=.35,metal=.65)
ivory = mat('PlaqueLetter',(.96,.84,.58),rough=.6)
leaf = mat('Leaf',(.20,.37,.065),rough=.82)
leaf_light = mat('LeafLight',(.42,.54,.105),rough=.87)
leaf_dark = mat('LeafDark',(.115,.25,.06),rough=.86)
grassmat = mat('Grass',(.47,.54,.12),rough=.85)
flower = mat('Flower',(.97,.66,.13),rough=.7)
glass = mat('LanternGlow',(1,.48,.085),rough=.3,emission=2.6)
black = mat('TunnelBlack',(.035,.028,.022),rough=1)
oremat = mat('Ore',(.49,.51,.53),rough=.78)
orelight = mat('OreLight',(.67,.65,.60),rough=.7)

collections={}
def assembly(name):
    col=bpy.data.collections.new(name); bpy.context.scene.collection.children.link(col)
    collections[name]=col
    return col
bunker=assembly('TimberBunker')
terrain=assembly('TerrainPatch')
track=assembly('MineTrack')
cart=assembly('OreCart')

def move_col(obj,col):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    col.objects.link(obj)

def finish(obj,name,col,material,bevel=0,smooth=False):
    obj.name=name; move_col(obj,col)
    obj.data.materials.append(material)
    bpy.context.view_layer.objects.active=obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(obj.data); bm.free()
    if bevel:
        b=obj.modifiers.new('Soft worn edges','BEVEL'); b.width=bevel; b.segments=2; b.affect='EDGES'
        bpy.ops.object.modifier_apply(modifier=b.name)
    if smooth:
        for face in obj.data.polygons: face.use_smooth=True
        mod=obj.modifiers.new('Area weighted corner normals','WEIGHTED_NORMAL'); mod.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    # UV0 exists on every mesh, including the cut surfaces introduced by bevels.
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.025)
    bpy.ops.object.mode_set(mode='OBJECT'); obj.select_set(False)
    return obj

def mesh(name,verts,faces,col,material,bevel=0,smooth=False):
    data=bpy.data.meshes.new(name); data.from_pydata([xyz(p) for p in verts],[],faces); data.update()
    obj=bpy.data.objects.new(name,data); col.objects.link(obj)
    return finish(obj,name,col,material,bevel,smooth)

def beam(name,at,size,col,material=wood,bevel=.025,rotation=0):
    # Slightly splayed cut ends and chamfers give the timber its own silhouette.
    hx,hy,hz=[s*.5 for s in size]
    verts=[(-hx,-hy,-hz),(hx,-hy,-hz),(hx,hy,-hz),(-hx,hy,-hz),
           (-hx,-hy,hz),(hx,-hy,hz),(hx,hy,hz),(-hx,hy,hz)]
    faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    obj=mesh(name,verts,faces,col,material,bevel,True)
    obj.location=xyz(at); obj.rotation_euler[1]=math.radians(rotation)
    return obj

def rod(name,a,b,r,col,material,vertices=12):
    aa,bb=Vector(xyz(a)),Vector(xyz(b)); delta=bb-aa
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=delta.length,location=(aa+bb)*.5)
    obj=bpy.context.object; obj.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
    return finish(obj,name,col,material,.006,True)

def rock(name,at,size,col,material=stone,detail=1):
    # Irregular ring hull, rather than a scaled cube or sphere.
    verts=[]
    for ring,(height,radius) in enumerate(((-.48,.62),(-.22,1),(.23,.87),(.49,.50))):
        for i in range(7):
            ang=(i/7+.035*(ring%2))*math.tau
            rr=radius*random.uniform(.82,1.14)
            verts.append((math.cos(ang)*rr*size[0]*.5, (height+random.uniform(-.08,.08))*size[1], math.sin(ang)*rr*size[2]*.5))
    bm=bmesh.new()
    for v in verts: bm.verts.new(xyz(v))
    bmesh.ops.convex_hull(bm,input=list(bm.verts))
    data=bpy.data.meshes.new(name); bm.to_mesh(data); bm.free()
    obj=bpy.data.objects.new(name,data); col.objects.link(obj); obj.location=xyz(at)
    obj.rotation_euler[2]=random.uniform(-.3,.3)
    return finish(obj,name,col,material,.025 if detail else .008,False)

def leafmesh(name,a,b,width,col,material):
    aa,bb=Vector(a),Vector(b); d=bb-aa
    side=Vector((d.z,0,-d.x)).normalized()*width
    if side.length<.001: side=Vector((width,0,0))
    mid=aa+d*.5+Vector((0,width*.25,0))
    verts=[tuple(aa),tuple(mid+side),tuple(bb),tuple(mid-side),tuple(mid+Vector((0,width*.35,0)))]
    return mesh(name,verts,[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(4,1,0),(4,2,1),(4,3,2),(4,0,3)],col,material)

# Excavated retaining cliff: clear central aperture; staggered layers and ledges.
for side in (-1,1):
    for row in range(4):
        for j in range(2):
            xx=side*(1.08+j*.57+random.uniform(-.1,.1))
            zz=.29+row*.58+random.uniform(-.04,.04)
            rock('Cliff_course_%s_%s_%s'%(side,row,j),(xx,zz,.43+j*.18),(.90,.78,1.32),bunker,
                 random.choice((stone,stone,rock_light,rock_dark)))
    rock('Shoulder_boulder',(side*1.30,2.45,.67),(1.30,.78,1.52),bunker,rock_light)
for j in range(5):
    rock('Crown_stratum_%d'%j,(-1.55+j*.77,2.72,.62),(.99,.69,1.55),bunker,stone if j%2 else rock_light)
for j in range(4):
    rock('Upper_ledge_%d'%j,(-1.35+j*.88,3.04,1.04),(1.2,.43,1.20),bunker,rock_light)

# Actual recessed tunnel shell: 2 m deep with an arched ceiling, no front black cube.
segments=10
verts=[]
for depth in (.04,.64,1.25,2.0):
    for i in range(segments+1):
        a=i/segments*math.pi
        verts.append((math.cos(a)*.75,.90+math.sin(a)*.92,depth))
faces=[]
for ring in range(3):
    for i in range(segments):
        a=ring*(segments+1)+i; faces.append((a,a+1,a+segments+2,a+segments+1))
mesh('Tunnel_vault',verts,faces,bunker,rock_dark)
beam('Tunnel_left_wall',(-.78,.48,1.05),(.22,1.05,2.1),bunker,rock_dark)
beam('Tunnel_right_wall',(.78,.48,1.05),(.22,1.05,2.1),bunker,rock_dark)
beam('Tunnel_end',(0,.85,2.06),(1.6,1.85,.09),bunker,black,.0)
beam('Tunnel_floor',(0,.005,.95),(1.65,.06,2.2),bunker,soil_dark,.0)
for depth in (.64,1.35):
    for side in (-1,1):
        beam('Recessed_support',(side*.63,.87,depth),(.13,1.7,.13),bunker,darkwood,.016)
    beam('Recessed_lintel',(0,1.68,depth),(1.45,.13,.15),bunker,darkwood,.018)

# Hero timber portal, forged brackets, bolt heads, diagonal knee braces.
for side in (-1,1):
    beam('Portal_post_L' if side<0 else 'Portal_post_R',(side*.83,1.03,-.115),(.30,2.06,.34),bunker,wood,.04)
    beam('Post_foot',(side*.83,.12,-.115),(.35,.19,.39),bunker,iron,.015)
    for height in (.38,1.72):
        beam('Post_strap',(side*.83,height,-.305),(.32,.12,.035),bunker,iron,.012)
        for dx in (-.095,.095):
            rod('Hand_forged_bolt',(side*.83+dx,height,-.34),(side*.83+dx,height,-.30),.026,bunker,steel_edge,8)
    beam('Knee_brace',(side*.57,1.70,-.08),(.12,.67,.16),bunker,wood,.025,side*43)
beam('Portal_header',(0,2.06,-.13),(2.24,.32,.40),bunker,wood,.045)
beam('Header_shadow_seam',(0,1.885,-.12),(1.64,.055,.28),bunker,darkwood,.012)
for side in (-1,1):
    beam('Header_end_grain',(side*1.115,2.06,-.13),(.016,.25,.32),bunker,wood_end,.006)
    beam('Header_bracket',(side*.83,2.06,-.345),(.16,.32,.032),bunker,iron,.012)
    for dz in (-.095,.095):
        rod('Header_rivet',(side*.83,2.06+dz,-.38),(side*.83,2.06+dz,-.34),.025,bunker,steel_edge,8)
beam('Number_board',(0,2.42,-.17),(.61,.43,.12),bunker,wood,.035)
beam('Number_inset',(0,2.42,-.241),(.49,.32,.025),bunker,wood_end,.015)
outline=[(-.07,.10),(.01,.17),(.07,.17),(.07,-.12),(.13,-.12),(.13,-.17),(-.10,-.17),(-.10,-.12),(-.01,-.12),(-.01,.075),(-.07,.035)]
numverts=[(xx,2.42+hh,dd) for dd in (-.265,-.283) for xx,hh in outline]
n=len(outline)
numfaces=[tuple(range(n-1,-1,-1)),tuple(n+i for i in range(n))]
numfaces.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
mesh('Raised_mine_number_1',numverts,numfaces,bunker,ivory,.003,True)

# Hanging cage lantern with cap, ring, warm core and four corner mullions.
rod('Lantern_wall_arm',(-1.10,1.88,.0),(-1.10,1.88,-.48),.027,bunker,iron)
rod('Lantern_drop',(-1.10,1.88,-.48),(-1.10,1.69,-.48),.018,bunker,brass)
for height in (1.29,1.69):
    beam('Lantern_frame',(-1.10,height,-.48),(.25,.045,.23),bunker,iron,.015)
beam('Lantern_glass',(-1.10,1.49,-.48),(.16,.34,.15),bunker,glass,.018)
for dx in (-.105,.105):
    for dz in (-.095,.095):
        rod('Lantern_corner',(-1.1+dx,1.31,-.48+dz),(-1.1+dx,1.67,-.48+dz),.012,bunker,brass,8)

# Packed trail and stratified cut edge, broad enough to meet the old yard terrain.
W,D=26,32
v=[]
for iz in range(D+1):
    depth=-3.35+iz/D*5.1
    for ix in range(W+1):
        xx=-2.15+ix/W*4.30
        # Flat wheel/foot path; gentle rise and irregular boundary on the sides.
        edge=max(0,abs(xx)-.7)
        h=.015+edge*.026+math.sin(xx*4+depth*2)*edge*.016
        v.append((xx,h,depth))
f=[]
for iz in range(D):
    for ix in range(W):
        a=iz*(W+1)+ix; f.append((a,a+1,a+W+2,a+W+1))
mesh('Contoured_trail',v,f,terrain,soil,0,True)
beam('Earth_core',(0,-.24,-.8),(4.24,.48,5.0),terrain,soil_dark,.17)
for j in range(8):
    rock('Terrace_edge_%d'%j,(-1.92+j*.55,-.21,-3.25),(.80,.52,.70),terrain,stone)
for side in (-1,1):
    for j in range(6):
        rock('Embedded_edge',(side*2.04,-.13,-2.8+j*.75),(.66,.45,.91),terrain,random.choice((stone,rock_light)))
for j in range(36):
    xx=random.choice((-1,1))*random.uniform(1.0,1.9); dd=random.uniform(-3,1.3)
    if dd>.1 and abs(xx)<1.7: continue
    rock('Trail_talus',(xx,.07,dd),(random.uniform(.1,.30),random.uniform(.10,.23),random.uniform(.10,.30)),terrain,random.choice((stone,rock_light,rock_dark)),0)

# Moss islands, grass blades, fern fronds and leaf-built shrubs.
for i in range(19):
    side=random.choice((-1,1)); xx=side*random.uniform(1.15,2.0); dd=random.uniform(-3.1,1.25)
    rock('Moss_cushion',(xx,.055,dd),(.53,.075,.47),terrain,moss,0)
for i in range(95):
    xx=random.choice((-1,1))*random.uniform(1.1,2.0); dd=random.uniform(-3.05,1.35)
    for b in range(random.randint(3,5)):
        a=random.uniform(0,math.tau); height=random.uniform(.12,.28)
        aa=(xx,.06,dd); bb=(xx+math.cos(a)*height*.45,.06+height,dd+math.sin(a)*height*.45)
        leafmesh('Grass_blade',aa,bb,random.uniform(.025,.045),terrain,random.choice((grassmat,leaf_light,leaf)))
for xx,dd in ((-1.62,-1.3),(1.5,-2.45),(-1.6,.2),(1.48,.6),(-.95,1.4)):
    for j in range(6):
        a=j/6*math.tau; end=Vector((xx+math.cos(a)*.42,.33,dd+math.sin(a)*.42)); start=Vector((xx,.10,dd))
        rod('Fern_stem',tuple(start),tuple(end),.008,terrain,leaf_dark,6)
        for k in range(1,7):
            t=k/7; p=start.lerp(end,t); sidevec=Vector((-math.sin(a),0,math.cos(a)))*(.12*(1-t)+.025)
            for sign in (-1,1): leafmesh('Fern_pinna',tuple(p),tuple(p+sidevec*sign+Vector((math.cos(a)*.06,.015,math.sin(a)*.06))),.034,terrain,leaf)
for xx,hh,dd in ((-1.65,.27,.12),(1.66,.26,.2),(-1.49,2.97,.7),(.94,3.12,1.0)):
    col=terrain if hh<1 else bunker
    for j in range(20):
        a=random.uniform(0,math.tau); r=random.uniform(.06,.34)
        start=(xx+math.cos(a)*r,hh+random.uniform(-.07,.11),dd+math.sin(a)*r)
        end=(start[0]+math.cos(a)*.12,start[1]+random.uniform(.06,.15),start[2]+math.sin(a)*.12)
        leafmesh('Shrub_leaf',start,end,.075,col,random.choice((leaf,leaf_light,leaf_dark)))
for i in range(15):
    xx=random.choice((-1,1))*random.uniform(1.25,1.85); dd=random.uniform(-3.0,-.9)
    hh=random.uniform(.13,.23)
    rod('Wildflower_stem',(xx,.08,dd),(xx,hh,dd),.007,terrain,leaf,6)
    for p in range(5):
        a=p/5*math.tau
        leafmesh('Wildflower_petal',(xx,hh,dd),(xx+math.cos(a)*.065,hh+.008,dd+math.sin(a)*.065),.025,terrain,flower)

# I-profile rails with continuous head/web/foot, sleepers, plates and spikes.
path=[(0,1.95-i*.16) for i in range(20)]
# Curves out toward the yard's east/west main line (local z=-1.3).
for i in range(1,10):
    a=i/9*math.pi/2
    path.append((.85-.85*math.cos(a),-1.09-.85*math.sin(a)))
for i in range(1,8): path.append((.85+i*.18,-1.94))
gauge=.59
profile=[(-.068,0),(.068,0),(.068,.027),(.019,.035),(.019,.089),(.046,.096),(.046,.125),(-.046,.125),(-.046,.096),(-.019,.089),(-.019,.035),(-.068,.027)]
for sign in (-1,1):
    verts=[]
    for k,(xx,dd) in enumerate(path):
        prev=path[max(0,k-1)]; nex=path[min(len(path)-1,k+1)]
        dx,dz=nex[0]-prev[0],nex[1]-prev[1]; length=math.hypot(dx,dz)
        nx,nz=-dz/length,dx/length
        for lateral,height in profile:
            off=sign*gauge*.5+lateral
            verts.append((xx+nx*off,.085+height,dd+nz*off))
    faces=[]; n=len(profile)
    for k in range(len(path)-1):
        for j in range(n): faces.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    faces.extend((tuple(range(n-1,-1,-1)),tuple((len(path)-1)*n+j for j in range(n))))
    mesh('Shaped_rail_L' if sign<0 else 'Shaped_rail_R',verts,faces,track,steel,.006,True)
for k in range(0,len(path),2):
    xx,dd=path[k]; prev=path[max(0,k-1)]; nex=path[min(len(path)-1,k+1)]
    dx,dz=nex[0]-prev[0],nex[1]-prev[1]; yaw=math.atan2(dx,dz)
    sleeper=beam('Oak_sleeper_%02d'%k,(xx,.062,dd),(1.02,.09,.15),track,darkwood,.022)
    sleeper.rotation_euler[2]=yaw
    for sign in (-1,1):
        lateral=sign*gauge*.5; px=xx+math.cos(yaw)*lateral; pd=dd-math.sin(yaw)*lateral
        plate=beam('Rail_chair',(px,.113,pd),(.20,.025,.16),track,rust,.006); plate.rotation_euler[2]=yaw
        for side in (-1,1):
            rod('Track_spike',(px+side*.075,.12,pd),(px+side*.075,.15,pd),.017,track,iron,6)

# Cart: independently modelled tapered, open hopper with thick inset wood panels.
def hopper():
    verts=[]
    # Four rings: outer bottom/top, inner top/bottom, so wall thickness is real.
    for width,depth,h in ((.64,.86,.46),(.88,1.10,.98),(.78,1.0,.98),(.55,.77,.49)):
        verts.extend([(-width/2,h,-depth/2),(width/2,h,-depth/2),(width/2,h,depth/2),(-width/2,h,depth/2)])
    faces=[]
    for ring in range(3):
        for j in range(4): faces.append((ring*4+j,ring*4+(j+1)%4,(ring+1)*4+(j+1)%4,(ring+1)*4+j))
    faces.append((12,13,14,15))
    return mesh('Open_tapered_hopper',verts,faces,cart,steel,.014,True)
hopper()
for h in (.49,.98):
    w=.64+(h-.46)/.52*.24; d=.86+(h-.46)/.52*.24
    for side in (-1,1):
        beam('Hopper_seam',(side*(w*.5+.003),h,0),(.012,.015,d-.04),cart,iron,.003)
        beam('End_panel_seam',(0,h,side*(d*.5+.003)),(w-.04,.015,.012),cart,iron,.003)
for side in (-1,1):
    beam('Rim_side',(side*.44,1.0,0),(.072,.075,1.16),cart,steel_edge,.012)
    beam('Rim_end',(0,1.0,side*.55),(.90,.075,.073),cart,steel_edge,.012)
    beam('Underframe_side',(side*.28,.39,0),(.09,.11,1.14),cart,iron,.014)
    for dd in (-.35,.35):
        strap=beam('Hopper_steel_strap',(side*.397,.745,dd),(.035,.53,.075),cart,steel,.008,side*13)
        for h in (.54,.72,.92):
            # Rivet axes point outward from the side wall.
            xx=side*(.32+(h-.46)/.52*.12+.023)
            rod('Cart_side_rivet',(xx,h,dd),(xx+side*.02,h,dd),.025,cart,steel_edge,8)
for dd in (-.38,.38):
    beam('Cross_member',(0,.39,dd),(.81,.09,.12),cart,iron,.01)
    rod('Axle',( -.47,.335,dd),(.47,.335,dd),.042,cart,steel_edge)
    for side in (-1,1):
        # Wheels have a tread, flange, recessed face, bearing and eight hub bolts.
        cx=side*.342
        wheel_parts=[]
        for radius,width,offset,material in ((.125,.082,0,iron),(.146,.023,-side*.039,steel),(.087,.018,side*.046,steel),(.047,.038,side*.061,brass)):
            wheel_parts.append(rod('Wheel_part',(cx+offset-width*.5,.335,dd),(cx+offset+width*.5,.335,dd),radius,cart,material,24))
        for j in range(6):
            a=j/6*math.tau
            wheel_parts.append(rod('Wheel_hub_bolt',(cx+side*.061,.335+math.cos(a)*.067,dd+math.sin(a)*.067),(cx+side*.073,.335+math.cos(a)*.067,dd+math.sin(a)*.067),.011,cart,steel_edge,6))
        # One mesh per wheel, with its origin exactly on the axle: hub bolts
        # orbit with the wheel and runtime rotation does not allocate geometry.
        bpy.ops.object.select_all(action='DESELECT')
        for obj in wheel_parts: obj.select_set(True)
        bpy.context.view_layer.objects.active=wheel_parts[0]
        bpy.ops.object.join()
        wheel_obj=wheel_parts[0]
        wheel_obj.name='Wheel_'+('Front' if dd<0 else 'Rear')+('L' if side<0 else 'R')
        bpy.context.scene.cursor.location=xyz((cx,.335,dd))
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        wheel_obj.select_set(False)
for dd in (-.68,.68):
    rod('Coupling_shank',(0,.39,dd*.75),(0,.39,dd),.045,cart,iron)
    bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=6,location=xyz((0,.39,dd)),major_radius=.067,minor_radius=.018)
    finish(bpy.context.object,'Coupling_ring',cart,iron,0,True)
for i in range(39):
    xx=random.uniform(-.31,.31); dd=random.uniform(-.4,.4)
    h=.91+max(0,1-(xx/.42)**2-(dd/.57)**2)*.22+random.uniform(-.04,.04)
    size=random.uniform(.11,.22)
    rock('Ore_load_%02d'%i,(xx,h,dd),(size,size*.85,size),cart,random.choice((oremat,oremat,orelight)),0)
for i in range(9):
    rock('Tunnel_ore',(random.uniform(-.4,.4),.14,random.uniform(1.2,1.7)),(.3,.25,.3),bunker,oremat,0)

# Bake inexpensive vertex contact occlusion for the exported meshes. Store the
# independent mesh normal detail too; Unity uses both, plus real-time shadows.
def vertex_ao(col):
    objects=[o for o in col.objects if o.type=='MESH']
    coords=[]; triangles=[]
    for obj in objects:
        obj.data.calc_loop_triangles(); start=len(coords)
        coords.extend([obj.matrix_world@v.co for v in obj.data.vertices])
        triangles.extend([tuple(start+i for i in tri.vertices) for tri in obj.data.loop_triangles])
    tree=BVHTree.FromPolygons(coords,triangles,all_triangles=True)
    for obj in objects:
        colors=obj.data.color_attributes.new(name='ContactAO',type='BYTE_COLOR',domain='CORNER')
        inv=obj.matrix_world.to_3x3().inverted().transposed()
        values={}
        for face in obj.data.polygons:
            n=(inv@face.normal).normalized()
            tangent=n.cross(Vector((0,0,1)))
            if tangent.length<.01: tangent=n.cross(Vector((0,1,0)))
            tangent.normalize(); bitangent=n.cross(tangent)
            for loop_index in face.loop_indices:
                vi=obj.data.loops[loop_index].vertex_index
                key=(vi,face.index)
                p=obj.matrix_world@obj.data.vertices[vi].co+n*.018
                occ=0
                for ray in range(8):
                    angle=ray*2.399963; radius=math.sqrt((ray+.5)/8)*.88
                    d=(tangent*math.cos(angle)*radius+bitangent*math.sin(angle)*radius+n*math.sqrt(1-radius*radius)).normalized()
                    hit=tree.ray_cast(p,d,.72)
                    if hit[0] is not None: occ+=max(.2,1-hit[3]/.72)
                ao=1-occ/8*.80
                colors.data[loop_index].color=(ao,ao,ao,1)
        obj.data.color_attributes.active_color=colors

# Smaller leaf faces already have deliberate self-shadowing; include them in BVH
# but use fewer mesh instances by joining equal materials after AO/UV authoring.
for name,col in collections.items():
    print('AO:',name,len(col.objects),flush=True)
    vertex_ao(col)
    # Leaves / rivets share a draw call per material, while cart wheels remain
    # separate for rotation at runtime (their names and pivots are exported).
    groups={}
    for obj in list(col.objects):
        if obj.type!='MESH': continue
        key=obj.active_material.name
        if name=='OreCart' and obj.name.startswith(('Wheel_','Wheel_hub')): continue
        groups.setdefault(key,[]).append(obj)
    for material,objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects: obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        bpy.ops.object.join(); objects[0].name=name+'_'+material
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)

stats={}
for name,col in collections.items():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in col.objects: obj.select_set(True)
    meshes=[o for o in col.objects if o.type=='MESH']
    for obj in meshes: obj.data.calc_loop_triangles()
    stats[name]={'meshes':len(meshes),'vertices':sum(len(o.data.vertices) for o in meshes),
                 'triangles':sum(len(o.data.loop_triangles) for o in meshes),'uv_meshes':sum(bool(o.data.uv_layers) for o in meshes)}
    bpy.ops.export_scene.fbx(filepath=str(ART/'Models'/(name+'.fbx')),use_selection=True,
        object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z',axis_up='Y',bake_space_transform=True,use_mesh_modifiers=True,
        mesh_smooth_type='OFF',use_tspace=True,use_triangles=True,bake_anim=False,
        path_mode='RELATIVE',embed_textures=False,colors_type='LINEAR')

(ART/'material-spec.json').write_text(json.dumps(materials,indent=2))
(SOURCE/'mesh-audit.json').write_text(json.dumps({'blender':bpy.app.version_string,'seed':41027,'assemblies':stats},indent=2))
# Look-development scene, separate from the runtime prefabs and FBX files.
for obj in cart.objects: obj.location += Vector(xyz((0,0,-.98)))
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.samples=24; scene.cycles.use_denoising=True
scene.render.resolution_x=1200; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.world.color=(.36,.40,.48)
scene.use_nodes=False
scene.view_settings.view_transform='AgX'
def light(name,kind,at,energy,color,size=5):
    data=bpy.data.lights.new(name,kind); data.energy=energy; data.color=color
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=xyz(at)
    if kind=='AREA': data.shape='DISK'; data.size=size; obj.rotation_euler=(Vector(xyz((0,1,0)))-obj.location).to_track_quat('-Z','Y').to_euler()
    return obj
light('Large_warm_key','AREA',(-3,7,-4),1150,(1,.83,.61),5)
light('Cool_sky_fill','AREA',(4,5,1),700,(.66,.79,1),5)
lamp=light('Lantern_light','POINT',(-1.10,1.48,-.60),22,(1,.38,.07)); lamp.data.shadow_soft_size=.25
camera_data=bpy.data.cameras.new('ReferenceCropCamera'); camera=bpy.data.objects.new('ReferenceCropCamera',camera_data); scene.collection.objects.link(camera)
camera.location=xyz((4.9,4.2,-7.8)); target=Vector(xyz((0,1.08,-.2)))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler(); camera_data.type='ORTHO'; camera_data.ortho_scale=5.5; scene.camera=camera
scene.render.image_settings.file_format='PNG'; scene.render.filepath=str(ROOT/'release/screenshots/timber-bunker/blender-lookdev.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'TimberBunker.blend'))
print(json.dumps(stats),flush=True)
if os.environ.get('DIAMOND_BLENDER_RENDER')=='1': bpy.ops.render.render(write_still=True)
