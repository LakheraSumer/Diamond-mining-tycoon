"""Render numbered modelling views from the actual saved crew meshes.

Run with Blender --background --python-exit-code 1 -P this file.
DMT_VIEW_IDS=tavi,mira limits the batch. Sources are never saved or modified.
Completed characters with matching source/settings are skipped on a rerun.
DMT_VIEW_FORCE=1 forces fresh renders even when that character is current.
The companion Assemble-CharacterViews.py adds labels and contact-sheet layouts.
"""
import bpy,bmesh,math,json,os,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
from bpy_extras.object_utils import world_to_camera_view

ROOT=Path(__file__).resolve().parents[2]
DESIGNS=json.loads((ROOT/'tools/art/character-designs.json').read_text())
IDS=[s.strip().lower() for s in os.environ.get('DMT_VIEW_IDS',','.join(DESIGNS)).split(',') if s.strip()]
WIDTH=int(os.environ.get('DMT_VIEW_WIDTH','1200'));HEIGHT=int(os.environ.get('DMT_VIEW_HEIGHT','1600'))
SAMPLES=int(os.environ.get('DMT_VIEW_SAMPLES','16'))
ENGINE=os.environ.get('DMT_VIEW_ENGINE','CYCLES')
sha=lambda path:hashlib.sha256(path.read_bytes()).hexdigest()
SCRIPT_HASH=sha(Path(__file__))
RAW_OUTPUTS=['02_Beauty.png','03_Front.png','04_Left.png','05_Right.png',
             '06_Back.png','07_Face.png','_08_Accessories.png','_09_MaterialPreview.png']

def current_manifest(output,source_hash):
    """A finished earlier character is resumable; a partial one is rerendered."""
    path=output/'model-views.json'
    if os.environ.get('DMT_VIEW_FORCE')=='1' or not path.is_file():return None
    try:
        manifest=json.loads(path.read_text(encoding='utf-8'))
        expected={'model_source_sha256':source_hash,'renderer_sha256':SCRIPT_HASH,
                  'engine':ENGINE,'samples':SAMPLES,'full_body_dimensions':[WIDTH,HEIGHT]}
        if any(manifest.get(key)!=value for key,value in expected.items()):return None
        for file in RAW_OUTPUTS:
            if not (output/file).is_file():return None
            if manifest.get('raw_render_outputs',{}).get(file,{}).get('sha256')!=sha(output/file):return None
        return manifest
    except (OSError,ValueError,KeyError):return None

def evaluated_bounds(objects):
    deps=bpy.context.evaluated_depsgraph_get();points=[]
    for obj in objects:
        ev=obj.evaluated_get(deps)
        points.extend(ev.matrix_world@Vector(v) for v in ev.bound_box)
    return min(p.z for p in points),max(p.z for p in points)

def aim(camera,target,position,scale):
    camera.location=Vector(position);camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO';camera.data.ortho_scale=scale

def snapshot(scene,camera,path,width,height):
    scene.camera=camera;scene.render.resolution_x=width;scene.render.resolution_y=height
    scene.render.resolution_percentage=100;scene.render.filepath=str(path)
    bpy.ops.render.render(write_still=True)

def reset_pose(rig):
    rig.animation_data_clear()
    for bone in rig.pose.bones:bone.matrix_basis=Matrix.Identity(4)
    bpy.context.view_layer.update()

def connected_faces(data):
    parent=list(range(len(data.vertices)))
    def find(a):
        while parent[a]!=a:parent[a]=parent[parent[a]];a=parent[a]
        return a
    for edge in data.edges:
        a,b=map(find,edge.vertices)
        if a!=b:parent[b]=a
    groups={}
    for poly in data.polygons:groups.setdefault(find(poly.vertices[0]),[]).append(poly.index)
    return groups.values()

def extracted_accessories(body,role,collection,height):
    """Keep whole loose mesh islands and their original UV/material data."""
    deps=bpy.context.evaluated_depsgraph_get();evaluated=body.evaluated_get(deps)
    data=bpy.data.meshes.new_from_object(evaluated,preserve_all_data_layers=True,depsgraph=deps)
    chosen={key:[] for key in ('Helmet / eyewear','Belt / pouches','Gloves','Boots')}
    for faces in connected_faces(data):
        vertices={v for f in faces for v in data.polygons[f].vertices}
        points=[data.vertices[v].co for v in vertices]
        center=sum(points,Vector())/len(points)
        names={data.materials[data.polygons[f].material_index].name.split('_',1)[-1].split('.')[0] for f in faces}
        key=None
        if center.z>height*.78 and names&{'Helmet','Lamp','Metal','Iron','Brass','Glass'}:key='Helmet / eyewear'
        elif center.z<height*.17 and names&{'Boots','Iron','Brass','Ivory','Rubber','Leather'}:key='Boots'
        elif abs(center.x)>height*.13 and height*.31<center.z<height*.51 and names&{'Accent','Gloves','Glove','Rubber','Leather'}:key='Gloves'
        elif height*.34<center.z<height*.56 and names&{'Leather','Boots','Brass','Iron','Wood'}:key='Belt / pouches'
        if key:chosen[key].extend(faces)
    objects=[];labels=[]
    if role is not None:
        copied=bpy.data.meshes.new_from_object(role.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
        obj=bpy.data.objects.new('Signature role equipment',copied);collection.objects.link(obj);objects.append(obj);labels.append('Signature role equipment')
    for title,faces in chosen.items():
        if not faces:continue
        copied=data.copy();bm=bmesh.new();bm.from_mesh(copied);bm.faces.ensure_lookup_table()
        keep=set(faces);bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.index not in keep],context='FACES')
        bm.to_mesh(copied);bm.free()
        obj=bpy.data.objects.new(title,copied);collection.objects.link(obj);objects.append(obj);labels.append(title)
    bpy.context.view_layer.update()
    placements=[]
    for i,(obj,title) in enumerate(zip(objects,labels)):
        center=sum((Vector(p) for p in obj.bound_box),Vector())/8
        dims=obj.dimensions.copy();factor=.70/max(.02,dims.x,dims.y,dims.z)
        obj.scale=(factor,)*3
        col=i%3;row=i//3
        location=Vector(((col-1)*.95,0,1.45-row*1.0))
        obj.location=location-center*factor
        placements.append({'label':title,'world_center':list(location)})
    return objects,placements

for identifier in IDS:
    design=DESIGNS[identifier];name=design['name'];source=ROOT/'ArtSource/Characters'/(identifier+'.blend')
    output=ROOT/'ArtSource/Characters'/name;output.mkdir(parents=True,exist_ok=True)
    source_hash=sha(source)
    if current_manifest(output,source_hash):
        print('CHARACTER-VIEWS-CURRENT',identifier,source_hash,flush=True)
        continue
    bpy.ops.wm.open_mainfile(filepath=str(source))
    scene=bpy.context.scene;scene.render.engine=ENGINE
    if ENGINE=='CYCLES':scene.cycles.samples=SAMPLES;scene.cycles.use_denoising=True
    scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
    scene.render.film_transparent=False;scene.view_settings.view_transform='AgX'
    scene.world.use_nodes=True
    background=scene.world.node_tree.nodes.get('Background');background.inputs[0].default_value=(.78,.78,.78,1);background.inputs[1].default_value=.45
    rig=next(o for o in scene.objects if o.type=='ARMATURE')
    source_meshes=[o for o in scene.objects if o.type=='MESH' and o.parent==rig]
    body=next(o for o in source_meshes if o.name=='Body')
    role=next((o for o in source_meshes if o.name=='RoleProp'),None)
    # Remove any preview studio from older sources; retain only actual rig meshes.
    for obj in list(scene.objects):
        if obj!=rig and obj not in source_meshes and not obj.parent==rig:bpy.data.objects.remove(obj,do_unlink=True)
    neutral_collection=bpy.data.collections.new('Review studio (not exported)');scene.collection.children.link(neutral_collection)
    for title,at,power,size in [('Key',(-3,4,4),440,4),('Fill',(4,3,2.5),260,4),('Back',(1,-4,4),280,4)]:
        data=bpy.data.lights.new(title,'AREA');data.energy=power;data.shape='DISK';data.size=size
        obj=bpy.data.objects.new(title,data);neutral_collection.objects.link(obj);obj.location=at
        obj.rotation_euler=(Vector((0,0,1))-obj.location).to_track_quat('-Z','Y').to_euler()
    camera=bpy.data.objects.new('Modelling review camera',bpy.data.cameras.new('Modelling review camera'));neutral_collection.objects.link(camera)
    views={};low,high=evaluated_bounds([body]);height=high-low
    for obj in source_meshes:obj.hide_render=obj.name in ('MiningTool','CarryLoad')
    idle=next((a for a in bpy.data.actions if a.name=='Idle' or a.name.endswith('|Idle')),None)
    if idle:
        rig.animation_data_create();rig.animation_data.action=idle;scene.frame_set(1)
    aim(camera,(0,0,1.03),(-3.0,6.5,1.60),2.65)
    snapshot(scene,camera,output/'02_Beauty.png',WIDTH,HEIGHT)
    views['02_Beauty.png']={'camera':list(camera.location),'look_at':[0,0,1.03],'ortho_scale':2.65,'pose':'Actual authored Idle frame 1','props_visible':True,'native_dimensions':[WIDTH,HEIGHT]}
    reset_pose(rig)
    if role:role.hide_render=True
    for file,at,label in [('03_Front.png',(0,7,1.03),'front'),('04_Left.png',(7,0,1.03),'character left'),('05_Right.png',(-7,0,1.03),'character right'),('06_Back.png',(0,-7,1.03),'back')]:
        aim(camera,(0,0,1.03),at,2.65);snapshot(scene,camera,output/file,WIDTH,HEIGHT)
        views[file]={'camera':list(camera.location),'look_at':[0,0,1.03],'ortho_scale':2.65,'pose':'Actual bind/rest A-pose','props_visible':False,'native_dimensions':[WIDTH,HEIGHT],'orientation':label}
    head_group=body.vertex_groups.get('Head');head_points=[]
    if head_group:
        for vert in body.data.vertices:
            if any(g.group==head_group.index and g.weight>.5 for g in vert.groups):head_points.append(body.matrix_world@vert.co)
    head_center=(min(v.z for v in head_points)+max(v.z for v in head_points))/2 if head_points else height*.83
    aim(camera,(0,.02,head_center),(-.22,5,head_center+.03),.85)
    snapshot(scene,camera,output/'07_Face.png',WIDTH,WIDTH)
    views['07_Face.png']={'camera':list(camera.location),'look_at':[0,.02,head_center],'ortho_scale':.85,'pose':'Neutral A-pose head','native_dimensions':[WIDTH,WIDTH]}
    material_objects=[]
    accessory_objects,placements=extracted_accessories(body,role,neutral_collection,height)
    for obj in source_meshes:obj.hide_render=True
    aim(camera,(0,0,.97),(0,7,1.03),2.35)
    snapshot(scene,camera,output/'_08_Accessories.png',1600,1200)
    for item in placements:
        q=world_to_camera_view(scene,camera,Vector(item['world_center']));item['label_anchor_normalized']=[q.x,1-q.y]
    views['08_Accessories.png']={'native_dimensions':[1600,1200],'method':'Actual evaluated role mesh and whole loose Body islands selected by material and anatomical region; UVs and materials preserved. Assemblies are laid out for inspection, not redesigned.','assemblies':placements,'missing_in_current_model':[key for key in ('Helmet / eyewear','Belt / pouches','Gloves','Boots') if key not in [i['label'] for i in placements]]}
    for obj in accessory_objects:obj.hide_render=True
    material_keys=['Face','Hair','Helmet','Top','Shirt','Pants','Accent','Boots','Leather','Metal','Brass','Gem']
    selected=[]
    for key in material_keys:
        mat=next((m for m in bpy.data.materials if m.name==identifier+'_'+key),None)
        if mat:selected.append((key,mat))
    samples=[]
    for i,(key,mat) in enumerate(selected):
        x=(i%3-1)*.62;z=1.65-(i//3)*.52
        bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,radius=.20,location=(x,0,z))
        obj=bpy.context.object;obj.data.materials.append(mat);material_objects.append(obj)
        for poly in obj.data.polygons:poly.use_smooth=True
        samples.append({'key':key,'material':mat.name,'world_center':[x,0,z]})
    aim(camera,(0,0,.91),(0,7,.91),2.35)
    snapshot(scene,camera,output/'_09_MaterialPreview.png',1200,1200)
    for item in samples:
        q=world_to_camera_view(scene,camera,Vector(item['world_center']));item['label_anchor_normalized']=[q.x,1-q.y]
    manifest={'character_id':identifier,'name':name,'model_source':source.relative_to(ROOT).as_posix(),'model_source_sha256':source_hash,
        'status':'Actual-model review of this source revision; not a recovered original model, not a fidelity score.',
        'engine':ENGINE,'samples':SAMPLES,'renderer_sha256':SCRIPT_HASH,'full_body_dimensions':[WIDTH,HEIGHT],
        'colour_management':'AgX','lighting':'Neutral white area key/fill/back and neutral world, shared across characters.',
        'full_body_scale':'All front/left/right/back/beauty views use the same 2.65m orthographic camera scale, without per-character stretching.',
        'inferred_views':['04_Left.png','05_Right.png','06_Back.png'],'views':views,'material_samples':samples,
        'source_preserved':True,'model_saved_by_this_renderer':False,
        'raw_render_outputs':{file:{'sha256':sha(output/file)} for file in RAW_OUTPUTS}}
    (output/'model-views.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print('CHARACTER-VIEWS-DONE',identifier,json.dumps({'source_sha256':source_hash,'views':len(views),'engine':ENGINE,'samples':SAMPLES}),flush=True)
print('All requested actual-model view renders completed.',flush=True)
