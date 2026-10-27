import bpy,bmesh,math,os,json,hashlib,random
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
# Proje kökü script dosyasından veya açık Blender dosyasından bulunur; kullanıcıya özel yol saklanmaz.
import os
from pathlib import Path
_PROJECT_ROOT = next((p for p in Path(globals().get("__file__", bpy.data.filepath or os.getcwd())).resolve().parents
                      if (p / "AGENTS.md").is_file()), None)
if _PROJECT_ROOT is None:
    raise RuntimeError("Scripti Block Rift proje klasöründen veya proje içindeki Blender dosyasından çalıştırın.")
OUT=str(_PROJECT_ROOT / 'ArtProduction/Obstacle_1x1_Stone')
FILE=str(_PROJECT_ROOT / 'ArtProduction/AssetPreview/AssetPreview.blend')
os.makedirs(OUT,exist_ok=True)
family=bpy.context.scene
assert family.name=="AssetPreview"
assert not bpy.data.objects.get("Obstacle_1x1_Stone_Export"),"Taş zaten mevcut; üzerine yazılmadı"
def snapshot(o):
    d=[o.type,[list(r) for r in o.matrix_world],o.hide_viewport,o.hide_render,[c.name for c in o.users_collection],o.data.as_pointer() if o.data else None]
    if o.type=="MESH":
        m=o.data
        d.append(hashlib.sha256(str(([tuple(v.co) for v in m.vertices],[(tuple(p.vertices),p.use_smooth,p.material_index) for p in m.polygons],[[tuple(v.uv) for v in u.data] for u in m.uv_layers],[tuple(n.vector) for n in m.corner_normals],[m.as_pointer() for m in m.materials])).encode()).hexdigest())
    if o.type=="CAMERA":d.append((o.data.type,o.data.lens,o.data.ortho_scale))
    if o.type=="LIGHT":d.append((o.data.type,o.data.energy,tuple(o.data.color),o.data.size))
    return d
protected=list(family.objects);before={o.name:snapshot(o) for o in protected}
bpy.ops.wm.save_as_mainfile(filepath=FILE.replace(".blend","_BeforeStone.blend"),copy=True)
scene=bpy.data.scenes.new("BR_Stone_1x1_Review");bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
collection=bpy.data.collections.new("Obstacle_1x1_Stone");scene.collection.children.link(collection)
# Kontrollü düşük poligonlu kesitler: tok yan gövde, kırık omuz, düzensiz geniş yontuk.
N=128
profiles=[(.472,.105,0),(.491,.121,.010),(.500,.134,.031),(.500,.134,.215),(.494,.130,.250),(.478,.121,.278),(.449,.111,.296),(.407,.095,.297),(.350,.075,.291),(.297,.060,.258),(.244,.050,.247),(.135,.045,.247)]
def radial(a,h,r):
    x=abs(math.cos(a));y=abs(math.sin(a));d=h/max(x,y)
    if d*min(x,y)<=h-r:return d
    c=h-r;b=c*(x+y)
    return b+math.sqrt(max(0,b*b-(2*c*c-r*r)))
def circ_delta(a,b):
    return (a-b+math.pi)%(2*math.pi)-math.pi
verts=[];faces=[]
crack_angles=[math.radians(209),math.radians(320),math.radians(80)]
for j,(h,r,z) in enumerate(profiles):
    for i in range(N):
        a=2*math.pi*i/N
        d=radial(a,h,r)
        # Aşınma aynı açıda üst/alt kesitler boyunca devam eder; silüet hücreyi aşmaz.
        wear=(.5+.5*math.sin(7*a+.6))*(.5+.5*math.sin(11*a+1.2))
        inset=.003*wear if j<4 else .009*wear
        if j>=7:inset+=.013*math.sin(3*a+.2)+.008*math.sin(7*a-1.4)
        if i%(N//4)==0 and j in (2,3):inset=0
        zz=z
        if 4<=j<=9:zz+=.003*math.sin(5*a+.3)+.002*math.sin(9*a)
        # Kırık kenar yüzleri: birkaç sektörde farklı kesitler, genel bevel değil.
        for ca in (math.radians(42),math.radians(137),math.radians(266)):
            weight=max(0,1-abs(circ_delta(a,ca))/.12)
            if 4<=j<=7:
                inset+=.013*weight*(1 if j in (5,6) else .3)
                zz-=.008*weight
        # Üç sığ V çatlağı omuzdan yan yüze yürür; kalın gövde bölünmez.
        for k,ca in enumerate(crack_angles):
            drift=.023*math.sin(j*1.8+k)
            width=.032 if j<=8 else .026
            w=max(0,1-abs(circ_delta(a,ca+drift))/width)
            if j in range(1,10):
                if j<=4:inset+=.010*w
                else:zz-=.010*w
        d-=inset
        off=max(0,(j-6)/5)
        x=d*math.cos(a)+.012*off;y=d*math.sin(a)-.008*off
        verts.append((x,y,min(.300,max(0,zz))))
for j in range(len(profiles)-1):
    for i in range(N):
        k=(i+1)%N;faces.append((j*N+i,j*N+k,(j+1)*N+k,(j+1)*N+i))
faces.append(tuple(reversed(range(N))))
faces.append(tuple((len(profiles)-1)*N+i for i in range(N)))
mesh=bpy.data.meshes.new("Obstacle_Stone_ChiseledMesh")
mesh.from_pydata(verts,[],faces);mesh.update()
obj=bpy.data.objects.new("Obstacle_1x1_Stone_Export",mesh);collection.objects.link(obj)
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
# Referanstaki küçük kırık parçalar, ana gövdeye gerçek boolean union ile kaynaştırılır.
bm=bmesh.new();bm.from_mesh(mesh);tree=BVHTree.FromBMesh(bm)
chips=[(-.278,.221,.027,.032,.015),(-.213,.290,.017,.022,.011),(.287,-.222,.026,.024,.014),(-.242,-.261,.012,.014,.009)]
for index,(x,y,sx,sy,sz) in enumerate(chips):
    hit=tree.ray_cast(Vector((x,y,1)),Vector((0,0,-1)))[0]
    assert hit is not None
    center_z=min(hit.z+.002,.300-sz)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(x,y,center_z))
    chip=bpy.context.object;chip.name="Stone_Chip_Tool_"+str(index)
    chip.scale=(sx,sy,sz);chip.rotation_euler=(.13*index,.17,.47*index)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bpy.context.view_layer.objects.active=obj
    mod=obj.modifiers.new("Fused stone fragment",'BOOLEAN');mod.operation='UNION'
    choices=[i.identifier for i in mod.bl_rna.properties['solver'].enum_items]
    mod.solver=next(v for v in choices if v=='EXACT')
    mod.object=chip
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(chip,do_unlink=True)
bm.free()
# Gerçek geometri aşırı sınırları aşmaz; yerel taban merkezi korunur.
mesh=obj.data
for v in mesh.vertices:
    v.co.x=max(-.5,min(.5,v.co.x));v.co.y=max(-.5,min(.5,v.co.y));v.co.z=max(0,min(.3,v.co.z))
bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
for o in scene.objects:o.select_set(False)
obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(55),island_margin=.02)
bpy.ops.object.mode_set(mode='OBJECT')
tri=obj.modifiers.new("Export topology",'TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
mesh=obj.data
# Yan yüzeyler yumuşak, yontulmuş üst yüzler kontrollü düz yüzeylerdir.
for p in mesh.polygons:p.use_smooth=p.center.z<.235
# UV tabanlı, nötr, renkten bağımsız taş normal haritası.
SIZE=512
rng=np.random.default_rng(36472)
def noise(size):
    grid=rng.random((size,size)).astype(np.float32)
    axis=np.arange(SIZE)*size/SIZE
    base=axis.astype(np.int32);f=axis-base;f=f*f*(3-2*f)
    ix=base[None,:];iy=base[:,None];fx=f[None,:];fy=f[:,None]
    return ((1-fy)*((1-fx)*grid[iy%size,ix%size]+fx*grid[iy%size,(ix+1)%size])+fy*((1-fx)*grid[(iy+1)%size,ix%size]+fx*grid[(iy+1)%size,(ix+1)%size]))
height=.00045*noise(32)+.00020*noise(80)+.00008*noise(160)
gx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*SIZE*.5
gy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*SIZE*.5
normal=np.stack((-gx,-gy,np.ones_like(gx)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
rgba=np.ones((SIZE,SIZE,4),np.float32);rgba[:,:,:3]=normal*.5+.5
normal_image=bpy.data.images.new("Stone_1x1_Normal",width=SIZE,height=SIZE,alpha=False)
normal_image.colorspace_settings.name="Non-Color"
normal_image.pixels.foreach_set(rgba.ravel())
normal_image.filepath_raw=os.path.join(OUT,"Stone_1x1_Normal.png");normal_image.file_format='PNG';normal_image.save()
normal_image.pack()
material=bpy.data.materials.new("BR_Stone_Neutral_URP");material.use_nodes=True
bs=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
bs.inputs["Base Color"].default_value=(.32,.32,.32,1)
bs.inputs["Metallic"].default_value=0;bs.inputs["Roughness"].default_value=.82
tex=material.node_tree.nodes.new("ShaderNodeTexImage");tex.image=normal_image;tex.location=(-600,-100)
normal_node=material.node_tree.nodes.new("ShaderNodeNormalMap");normal_node.inputs["Strength"].default_value=.8;normal_node.location=(-300,-100)
material.node_tree.links.new(tex.outputs["Color"],normal_node.inputs["Color"])
material.node_tree.links.new(normal_node.outputs["Normal"],bs.inputs["Normal"])
mesh.materials.clear();mesh.materials.append(material)
obj["grid_width"]=1;obj["grid_height"]=1;obj["cell_size"]=1.0
obj["role"]="fixed stone obstacle";obj["pivot"]="bottom center";obj["texture_normal_space"]="tangent +Y"
studio=bpy.data.collections.new("Review_Studio_Stone");scene.collection.children.link(studio)
for original in family.objects:
    if original.type in {'LIGHT','CAMERA'} or original.name.startswith("ReviewGround"):
        cp=original.copy();cp.data=original.data.copy();studio.objects.link(cp)
        if original==family.camera:scene.camera=cp
scene.world=family.world.copy()
scene.render.resolution_x=1100;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
cam=scene.camera
def render(name,location,ortho=True,scale=1.55,target=(0,0,.15)):
    cam.location=location;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO' if ortho else 'PERSP';cam.data.ortho_scale=scale;cam.data.lens=55
    scene.render.filepath=os.path.join(OUT,name+".png");bpy.ops.render.render(write_still=True)
render("Top",(0,0,3))
e=math.radians(40);d=2.6
render("Perspective",(1.0,-d*math.cos(e),.15+d*math.sin(e)),True)
render("Side",(0,-3,.15),True,1.55)
render("Detail",(1,-1,1.5),True,1.10)
# Başka objeleri değiştirmeden taş ve onaylı 1x1'in birlikte incelemesi.
compare=bpy.data.scenes.new("BR_Stone_Block_Comparison")
cs=bpy.data.collections.new("Stone_Compare_Studio");compare.collection.children.link(cs)
master=family.objects['Block_1x1_Export']
for src,pos in [(master,(-.75,0,0)),(obj,(.75,0,0))]:
    cp=src.copy();cp.name=src.name+"_StoneComparison";cp.location=pos;compare.collection.objects.link(cp)
for src in studio.objects:
    cp=src.copy();cp.data=src.data.copy();cs.objects.link(cp)
    if src==cam:compare.camera=cp
compare.world=scene.world.copy()
compare.render.resolution_x=1400;compare.render.resolution_y=900;compare.render.resolution_percentage=100
compare.render.image_settings.file_format='PNG';compare.render.filepath=os.path.join(OUT,"Compare_Block_1x1.png")
cc=compare.camera;cc.location=(0,-4,3.4);cc.rotation_euler=(Vector((0,0,.15))-cc.location).to_track_quat('-Z','Y').to_euler()
cc.data.type='ORTHO';cc.data.ortho_scale=3.3
bpy.context.window.scene=compare;bpy.ops.render.render(write_still=True)
# Asıl sahnede ayrı konum; mevcut Preview_Studio ve bloklar aynen korunur.
bpy.context.window.scene=family
family.collection.children.link(collection)
obj.location=(3.5,4,0)
for cp in studio.objects:
    if cp.type in {'CAMERA','LIGHT'}:cp.location+=Vector((3.5,4,0))
assert all(snapshot(o)==before[o.name] for o in protected),"Korunan sahne değişti"
for o in family.objects:o.select_set(False)
obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.wm.save_as_mainfile(filepath=FILE)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"Obstacle_1x1_Stone.blend"),copy=True)
print(json.dumps({"dimensions":list(obj.dimensions),"vertices":len(mesh.vertices),"triangles":len(mesh.polygons),"normal_texture":normal_image.filepath_raw,"existing_scene_unchanged":True,"export_executed":False}))
