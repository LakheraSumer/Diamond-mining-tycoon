"""Render actual authored 3D props as transparent 512px UI assets in Blender 4.5.

No reference pixels or generated-image background. Editable .blend per icon is kept.
Run: blender -b -t 4 --python-exit-code 1 -P tools/art/Render-UiIcons.py
DMT_UI_ICON_IDS optionally selects comma-separated output filenames (without .png).
"""
import bpy
import hashlib
import json
import math
import os
import random
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'ArtSource/UI/Icons'
OUTPUT=ROOT/'Assets/_Project/Resources/Ui'
SOURCE.mkdir(parents=True,exist_ok=True);OUTPUT.mkdir(parents=True,exist_ok=True)
random.seed(581)
audit=[]

def linear(h):
    rgb=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    return tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in rgb)

def material(name,hex,metal=0,rough=.4,noise=0):
    mat=bpy.data.materials.new(name);mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*linear(hex),1)
    bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
    if noise:
        node=mat.node_tree.nodes.new('ShaderNodeTexNoise');node.inputs['Scale'].default_value=80
        bump=mat.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=noise;bump.inputs['Distance'].default_value=.025
        mat.node_tree.links.new(node.outputs['Fac'],bump.inputs['Height']);mat.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
    return mat

def palettes():
    global gold,goldedge,steel,darksteel,wood,leather,teal,lightteal,black,cream,gems,ores
    gold=material('Jewellery gold','ECAF28',.78,.23,.06)
    goldedge=material('Gold highlight','FFD36B',.72,.19)
    steel=material('Bevelled forged steel','9CA7B0',.65,.28,.08)
    darksteel=material('Chassis iron','303B47',.5,.4,.1)
    wood=material('Warm timber','995B2C',0,.65,.24)
    leather=material('Leather sack','8A6747',0,.78,.35)
    teal=material('Crusher teal enamel','286F73',.2,.35,.08)
    lightteal=material('Enamel edge','63A7A3',.2,.28)
    black=material('Rubber','1E2932',0,.82)
    cream=material('Ivory markings','F8E7BC',0,.5)
    gems=[material('Gem facet '+str(i),h,.18,.14) for i,h in enumerate(['42C5F4','9AE6FF','1986C0','DCF9FF','55D8FF'])]
    ores=[material('Rock facet '+str(i),h,.08,.78,.22) for i,h in enumerate(['6B7077','8E9298','AAADB0','4D535D','B4B5AE'])]

def finish(obj,mat,bevel=0,smooth=False):
    if mat:obj.data.materials.append(mat)
    if bevel:
        mod=obj.modifiers.new('Applied bevelled edge','BEVEL');mod.width=bevel;mod.segments=3
        bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=mod.name)
    for poly in obj.data.polygons:poly.use_smooth=smooth
    if not obj.data.uv_layers:
        uv=obj.data.uv_layers.new(name='UVMap')
        for poly in obj.data.polygons:
            normal=poly.normal;axis=max(range(3),key=lambda i:abs(normal[i]));axes=[i for i in range(3) if i!=axis]
            for index in poly.loop_indices:
                co=obj.data.vertices[obj.data.loops[index].vertex_index].co;uv.data[index].uv=(co[axes[0]],co[axes[1]])
    return obj

def box(name,at,size,mat,bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name=name;o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,mat,bevel,True)

def rod(name,a,b,r,mat,vertices=24):
    a,b=Vector(a),Vector(b);d=b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=d.length,location=(a+b)/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    return finish(o,mat,r*.15,True)

def torus(name,at,r,t,mat,rotation=(math.pi/2,0,0),segments=48):
    bpy.ops.mesh.primitive_torus_add(major_segments=segments,minor_segments=12,location=at,rotation=rotation,major_radius=r,minor_radius=t)
    o=bpy.context.object;o.name=name;return finish(o,mat,0,True)

def mesh(name,verts,faces,mat,bevel=0):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    o=bpy.data.objects.new(name,data);bpy.context.scene.collection.objects.link(o);return finish(o,mat,bevel)

def diamond(at=(0,0,0),r=.5,name='Faceted diamond'):
    x,y,z=at;vv=[]
    for h,rad in [(-.08,.96),(0,1),(.48,.55)]:
        for i in range(8):
            a=i*math.tau/8+math.pi/8;vv.append((x+math.cos(a)*rad*r,y+math.sin(a)*rad*r,z+h*r))
    vv.append((x,y,z-1.05*r));ff=[tuple(range(16,24))]
    for ring in range(2):
        for i in range(8):
            ff.append((ring*8+i,ring*8+(i+1)%8,(ring+1)*8+(i+1)%8,(ring+1)*8+i))
    for i in range(8):ff.append((24,i,(i+1)%8))
    o=mesh(name,vv,ff,gems[0])
    for mat in gems[1:]:o.data.materials.append(mat)
    for poly in o.data.polygons:poly.material_index=(poly.index*3+poly.index//8)%len(gems)
    return o

def rock(at,scale):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=at)
    o=bpy.context.object;o.name='Faceted mineral lump';o.scale=scale
    for vertex in o.data.vertices:vertex.co*=random.uniform(.84,1.12)
    finish(o,ores[0])
    for mat in ores[1:]:o.data.materials.append(mat)
    for poly in o.data.polygons:poly.material_index=random.randrange(len(ores))
    return o

def ore_icon():
    rock((-.38,.03,-.07),(.42,.39,.44));rock((.25,0,.07),(.57,.45,.60));rock((.03,-.35,-.3),(.37,.30,.34))

def diamond_icon():diamond((0,0,.15),.75)

def gold_icon():
    vv=[]
    for z,x,y in [(-.23,.85,.40),(.23,.66,.29)]:vv += [(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    mesh('Stamped bullion ingot',vv,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],gold,.065)
    # Raised geometric hallmark; part of the actual bullion geometry.
    hallmark=mesh('Bullion diamond hallmark',[(0,-.12,.245),(.15,0,.245),(0,.12,.245),(-.15,0,.245)],[(0,1,2,3)],goldedge)

def ring_icon():
    torus('Polished gold ring band',(0,0,0),.61,.095,gold)
    diamond((0,0,.76),.29)
    for x,y in [(-.23,-.1),(.23,-.1),(-.18,.15),(.18,.15)]:
        rod('Gem claw setting',(x,y,.49),(x*.92,y,.78),.024,goldedge,12)
    torus('Setting girdle',(0,0,.66),.24,.025,goldedge,(0,0,0))

def necklace_icon():
    for i in range(49):
        t=i/48*math.pi;x=.82*math.cos(t);z=.65-.93*math.sin(t)
        torus('Individual interlocking necklace link',(x,.015*math.sin(i),z),.053,.018,gold,rotation=(math.pi/2,(i%2)*.7,0),segments=20)
    torus('Pendant bail',(0,0,-.36),.09,.025,goldedge)
    diamond((0,0,-.69),.28)
    # Warm metal lower bezel gives the pendant thickness and visible mounting.
    torus('Pendant lower gold setting',(0,.06,-.69),.25,.025,gold,rotation=(math.pi/2,0,0))

def earrings_icon():
    for x in (-.48,.48):
        torus('Earring earwire',(x,0,.56),.16,.025,gold)
        rod('Earring dangling stem',(x,0,.40),(x,0,.06),.024,goldedge)
        torus('Earring small joint',(x,0,.04),.057,.018,gold,segments=20)
        diamond((x,0,-.30),.25)
        torus('Earring bezel',(x,.06,-.30),.23,.025,gold)

def pickaxe_icon():
    rod('Shaped wooden pick handle',(-.32,0,-.95),(.25,0,.61),.072,wood)
    rod('Handle leather butt',(-.32,0,-.95),(-.18,0,-.57),.08,leather)
    # Crescent profile extruded as a real, bevelled forged-steel head.
    profile=[(-.88,.28),(-.68,.55),(-.38,.77),(-.04,.82),(.36,.76),(.79,.54),(.99,.31),(.68,.45),(.30,.53),(-.05,.56),(-.39,.51),(-.67,.38)]
    vv=[(x+.13,y,z-.02) for y in (-.09,.09) for x,z in profile];n=len(profile)
    ff=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]
    ff += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh('Bevelled crescent pick head',vv,ff,steel,.025)
    rod('Pick head ferrule',(.18,0,.46),(.26,0,.70),.12,darksteel)

def crusher_icon():
    box('Cast enamel crusher chassis',(0,0,.02),(1.15,.92,.95),teal,.07)
    box('Crusher dark feed throat',(0,-.49,.18),(.73,.06,.38),darksteel,.03)
    for x in (-.46,.46):box('Crusher structural leg',(x,0,-.63),(.16,.80,.42),darksteel,.025)
    for x in (-.34,0,.34):box('Casting vertical stiffener',(x,-.5,-.02),(.055,.055,.7),lightteal,.012)
    vv=[]
    for z,x,y in [(.45,.36,.31),(1.08,.74,.54),(1.08,.65,.45),(.49,.30,.25)]:vv += [(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    mesh('Thick tapered steel feed hopper',vv,[(r*4+i,r*4+(i+1)%4,(r+1)*4+(i+1)%4,(r+1)*4+i) for r in range(3) for i in range(4)],steel,.025)
    rod('Crusher shaft',(-.72,-.12,0),(.72,-.12,0),.23,darksteel)
    rod('Flywheel',(-.78,-.12,0),(-.65,-.12,0),.32,steel)
    for i in range(7):rock((random.uniform(-.40,.40),random.uniform(-.25,.25),.9),(.14,.13,.13))
    box('Output chute',(0,-.69,-.40),(.65,.45,.1),steel,.025)

def cart_icon():
    path=ROOT/'ArtSource/TimberBunker/TimberBunker.blend'
    with bpy.data.libraries.load(str(path),link=False) as (available,loaded):
        if 'OreCart' not in available.collections:raise RuntimeError('Authored OreCart collection missing.')
        loaded.collections=['OreCart']
    collection=loaded.collections[0];bpy.context.scene.collection.children.link(collection)
    for obj in collection.all_objects:
        if obj.type!='MESH':continue
        for slot in obj.material_slots:
            if slot.material and slot.material.name.split('.')[0] in ('Ore','OreLight'):
                slot.material=gems[0] if slot.material.name.split('.')[0]=='Ore' else gems[1]
    # Source provenance records this actual detailed cart; original .blend is never saved over.

def coin_icon():
    rod('Thick gold coin', (0,-.07,0),(0,.07,0),.72,gold,64)
    torus('Raised coin milled rim',(0,-.085,0),.62,.045,goldedge)
    vv=[(0,-.104,.38),(.28,-.104,0),(0,-.104,-.38),(-.28,-.104,0)]
    mesh('Raised diamond emblem',vv,[(0,3,2,1)],goldedge)
    for i in range(36):
        t=i/36*math.tau;x,z=.704*math.sin(t),.704*math.cos(t)
        rod('Milled coin edge',(x,-.045,z),(x,.045,z),.008,goldedge,8)

def sack_icon():
    rings=[(-.56,.32),(-.48,.51),(-.12,.55),(.24,.34),(.40,.18),(.52,.22)]
    vv=[];n=32
    for z,r in rings:
        for i in range(n):
            t=i/n*math.tau;rr=r*(1+.025*math.cos(i*5+z*3));vv.append((math.cos(t)*rr,math.sin(t)*rr*.75,z))
    ff=[tuple(range(n-1,-1,-1))]
    ff += [(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(rings)-1) for i in range(n)]
    mesh('Gathered canvas carrying sack',vv,ff,leather,.018)
    torus('Sack tied rope',(0,0,.37),.185,.025,wood,rotation=(0,0,0))
    rod('Sack loose rope end',(0,-.16,.38),(.19,-.22,.20),.024,wood)

def clear():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

def frame(camera,objects,offset):
    pts=[obj.matrix_world@Vector(corner) for obj in objects for corner in obj.bound_box]
    lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)])
    centre=(lo+hi)/2;extent=(hi-lo).length
    camera.location=centre+Vector(offset).normalized()*max(5,extent*3)
    camera.rotation_euler=(centre-camera.location).to_track_quat('-Z','Y').to_euler()
    inverse=camera.rotation_euler.to_matrix().transposed();local=[inverse@(p-centre) for p in pts]
    width=max(p.x for p in local)-min(p.x for p in local);height=max(p.y for p in local)-min(p.y for p in local)
    camera.data.type='ORTHO';camera.data.ortho_scale=max(width,height)*1.14
    camera.data.clip_start=.01;camera.data.clip_end=100

def studio():
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU'
    scene.cycles.samples=int(os.environ.get('DMT_UI_ICON_SAMPLES','24'));scene.cycles.use_denoising=True
    scene.cycles.max_bounces=5
    scene.render.resolution_x=scene.render.resolution_y=512;scene.render.resolution_percentage=100
    scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
    scene.view_settings.view_transform='AgX'
    if scene.world is None:scene.world=bpy.data.worlds.new('UI studio environment')
    scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.70,.78,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.6
    for name,at,power,size,colour in [('Warm key',(-3,-4,6),650,5,(1,.89,.72)),('Cool fill',(4,-1,3),420,4,(.73,.87,1)),('Edge light',(0,4,4),850,4,(1,.96,.87))]:
        data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=colour
        obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=at;obj.rotation_euler=(-obj.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('UI orthographic camera');camera=bpy.data.objects.new('UI orthographic camera',data);scene.collection.objects.link(camera);scene.camera=camera
    return camera

BUILDERS={
    'icon_ore':(ore_icon,(3,-5,2.6)),
    'icon_diamond':(diamond_icon,(2,-5,3.8)),
    'icon_gold_bar':(gold_icon,(3,-5,4)),
    'icon_ring':(ring_icon,(1.3,-6,2)),
    'icon_necklace':(necklace_icon,(.5,-6,1.3)),
    'icon_earrings':(earrings_icon,(1,-6,1.5)),
    'icon_pickaxe':(pickaxe_icon,(1,-6,2.1)),
    'icon_crusher':(crusher_icon,(3,-5,3.4)),
    'icon_cart_diamonds':(cart_icon,(3,-5,3)),
    'icon_coin':(coin_icon,(1.3,-6,1.4)),
    'icon_sack':(sack_icon,(2,-5,2.5)),
}
wanted=os.environ.get('DMT_UI_ICON_IDS',','.join(BUILDERS)).split(',')
for identifier in wanted:
    if identifier not in BUILDERS:raise ValueError('Unknown icon '+identifier)
    clear();palettes();build,offset=BUILDERS[identifier];build()
    bpy.context.view_layer.update()
    objects=[obj for obj in bpy.context.scene.objects if obj.type=='MESH']
    camera=studio();frame(camera,objects,offset)
    bpy.ops.file.pack_all()
    blend=SOURCE/(identifier+'.blend');bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    output=OUTPUT/(identifier+'.png');bpy.context.scene.render.filepath=str(output);bpy.ops.render.render(write_still=True)
    record={'id':identifier,'file':output.relative_to(ROOT).as_posix(),'source':blend.relative_to(ROOT).as_posix(),
            'sha256':hashlib.sha256(output.read_bytes()).hexdigest(),'blend_sha256':hashlib.sha256(blend.read_bytes()).hexdigest(),
            'dimensions':[512,512],'transparent':True,'kind':'Actual Blender render of authored 3D props',
            'source_cart':'ArtSource/TimberBunker/TimberBunker.blend#OreCart' if identifier=='icon_cart_diamonds' else 'Original helper geometry in Render-UiIcons.py',
            'mesh_count':len(objects),'uv_mesh_count':sum(bool(obj.data.uv_layers) for obj in objects),
            'camera_offset':list(offset),'samples':bpy.context.scene.cycles.samples}
    audit.append(record);print('UI-ICON-DONE',json.dumps(record),flush=True)
(SOURCE/'render-manifest.json').write_text(json.dumps({'blender':bpy.app.version_string,'script_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    'notes':'Native 512px actual 3D prop renders with transparent background. Reconstructed reference-directed icons, not recovered original source assets.',
    'icons':audit},indent=2),encoding='utf-8')
print('UI icons complete',flush=True)
