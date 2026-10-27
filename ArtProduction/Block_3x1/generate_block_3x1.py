import bpy, bmesh, math, os, json, hashlib
from mathutils import Vector
from mathutils.geometry import delaunay_2d_cdt
# Proje kökü script dosyasından veya açık Blender dosyasından bulunur; kullanıcıya özel yol saklanmaz.
import os
from pathlib import Path
_PROJECT_ROOT = next((p for p in Path(globals().get("__file__", bpy.data.filepath or os.getcwd())).resolve().parents
                      if (p / "AGENTS.md").is_file()), None)
if _PROJECT_ROOT is None:
    raise RuntimeError("Scripti Block Rift proje klasöründen veya proje içindeki Blender dosyasından çalıştırın.")
OUT=str(_PROJECT_ROOT / 'ArtProduction/Block_3x1')
MASTER=str(_PROJECT_ROOT / 'ArtProduction/Block_1x1_Final/Block_1x1_Final.blend')
os.makedirs(OUT,exist_ok=True)
family_scene=bpy.context.scene
master=bpy.data.objects.get("Block_1x1_Export") or bpy.data.objects.get("Block_1x1")
protected=[o for o in family_scene.objects if o.type=="MESH" and o.name.startswith("Block_")]
def signature(o):
    return (o.data.as_pointer(),[list(r) for r in o.matrix_world],hashlib.sha256(str([(tuple(v.co)) for v in o.data.vertices]).encode()).hexdigest(),[m.as_pointer() for m in o.data.materials])
protected_before={o.name:signature(o) for o in protected}
bpy.ops.wm.save_as_mainfile(filepath=str(_PROJECT_ROOT / 'ArtProduction/AssetPreview/AssetPreview_Before3x1.blend'),copy=True)
assert master and len(master.data.vertices)==4800,"Onaylı 1x1 master açık olmalı"
source=master.data
before_hash=hashlib.sha256(open(MASTER,'rb').read()).hexdigest()
scene=bpy.data.scenes.new("BR_Block_3x1_Review");bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1.0
N=96
SOURCE_N=192
SAMPLE_STEP=2
# Master yüzey normalleri üst panel kopyalarında korunur.
source_normals={}
for p in source.polygons:
    for li in p.loop_indices:
        vi=source.loops[li].vertex_index
        if vi not in source_normals:source_normals[vi]=tuple(source.corner_normals[li].vector)
verts=[];faces=[];vn=[]
def add(v,n):
    verts.append(tuple(v));vn.append(tuple(n));return len(verts)-1
def ring_global(j):
    ring=[]
    for i in range(N):
        v=source.vertices[j*SOURCE_N+i*SAMPLE_STEP].co.copy()
        n=source_normals[j*SOURCE_N+i*SAMPLE_STEP]
        if abs(v.x)<1e-7:
            shifts=(1.0,-1.0) if i<N//2 else (-1.0,1.0)
        else:shifts=(1.0 if v.x>0 else -1.0,)
        for shift in shifts:
            ring.append(add((v.x+shift,v.y,v.z),n))
    return ring
def bridge(a,b):
    assert len(a)==len(b)
    for i in range(len(a)):
        k=(i+1)%len(a);faces.append((a[i],a[k],b[k],b[i]))
body=[]
for j in range(5):body.append(ring_global(j))
for j in range(4):bridge(body[j],body[j+1])
faces.append(tuple(reversed(body[0])))
panel_bases=[];panel_exact_error=0
for offset in (-1.0,0.0,1.0):
    rings=[]
    for j in range(4,25):
        ring=[]
        for i in range(N):
            v=source.vertices[j*SOURCE_N+i*SAMPLE_STEP].co.copy()
            # Alt bağlantı çevresindeki 0.006 montaj payı tepeye doğru sıfırlanır.
            if j<=16:
                t=(j-4)*math.pi*.5/12
                h=.420+.080*math.cos(t)
                factor=(h-.006*math.cos(t))/h
                v.x*=factor;v.y*=factor
            if j>=16:
                panel_exact_error=max(panel_exact_error,(v-source.vertices[j*SOURCE_N+i*SAMPLE_STEP].co).length)
            v.x+=offset
            ring.append(add(v,source_normals[j*SOURCE_N+i*SAMPLE_STEP]))
        rings.append(ring)
    for j in range(len(rings)-1):bridge(rings[j],rings[j+1])
    faces.append(tuple(rings[-1]))
    panel_bases.append(rings[0])
# Tek gövdenin üst tablası iki panel deliği ile kısıtlı Delaunay olarak örülür.
loops=[body[-1]]+panel_bases
deck_ids=[i for loop in loops for i in loop]
xy=[Vector(verts[i][:2]) for i in deck_ids]
edges=[];start=0
for loop in loops:
    for i in range(len(loop)):edges.append((start+i,start+(i+1)%len(loop)))
    start+=len(loop)
dv,de,df,origv,orige,origf=delaunay_2d_cdt(xy,edges,[],0,1e-7)
mapping=[]
for k,v in enumerate(dv):
    if origv[k]:mapping.append(deck_ids[origv[k][0]])
    else:mapping.append(add((v.x,v.y,.220),(0,0,1)))
def in_poly(x,y,ids):
    inside=False
    for k in range(len(ids)):
        a=verts[ids[k]];b=verts[ids[(k+1)%len(ids)]]
        if (a[1]>y)!=(b[1]>y):
            if x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0]:inside=not inside
    return inside
deck_faces=0
for f in df:
    cx=sum(dv[i].x for i in f)/len(f);cy=sum(dv[i].y for i in f)/len(f)
    if any(in_poly(cx,cy,p) for p in panel_bases):continue
    ids=tuple(mapping[i] for i in f)
    faces.append(ids);deck_faces+=1
mesh=bpy.data.meshes.new("Block_3x1_IntegratedMesh");mesh.from_pydata(verts,[],faces);mesh.update()
obj=bpy.data.objects.new("Block_3x1",mesh);scene.collection.objects.link(obj)
# Materyal kopyası masterın shaderını değiştirmez.
for m in source.materials:mesh.materials.append(m.copy())
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
# Koordinat tabanlı normal eşlemesi bmesh dönüşümünden bağımsızdır.
lookup={tuple(round(c,7) for c in v):vn[i] for i,v in enumerate(verts)}
for p in mesh.polygons:p.use_smooth=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.018);bpy.ops.object.mode_set(mode='OBJECT')
tri=obj.modifiers.new("ExportTriangles",'TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
from mathutils.kdtree import KDTree
normal_tree=KDTree(len(verts))
for idx,co in enumerate(verts):normal_tree.insert(co,idx)
normal_tree.balance()
custom=[]
for p in mesh.polygons:
    zs=[mesh.vertices[i].co.z for i in p.vertices]
    flat=max(zs)-min(zs)<1e-6
    for li in p.loop_indices:
        v=mesh.vertices[mesh.loops[li].vertex_index].co
        if flat:
            n=(0,0,-1) if max(zs)<1e-6 else (0,0,1)
        else:n=vn[normal_tree.find(v)[1]]
        custom.append(n)
mesh.normals_split_custom_set(custom)
obj["grid_width"]=3;obj["grid_height"]=1;obj["cell_size"]=1.0;obj["cavity_depth"]=.040
obj["master_reference"]=MASTER;obj["panel_base_clearance"]=.006
obj["pivot"]="bottom center"
# Master stüdyonun bağımsız kopyaları; asıl 1x1 objesi bu sahneye bağlanmaz.
oldscene=family_scene
for o in oldscene.objects:
    if o.type in {'LIGHT','CAMERA'} or o.name.startswith("ReviewGround"):
        copy=o.copy();copy.data=o.data.copy();scene.collection.objects.link(copy)
        if o==oldscene.camera:scene.camera=copy
scene.world=oldscene.world.copy()
scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
cam=scene.camera
def render(name,loc,ortho=True,scale=3.65):
    cam.location=loc;cam.rotation_euler=(Vector((0,0,.15))-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO' if ortho else 'PERSP';cam.data.ortho_scale=scale;cam.data.lens=55
    scene.render.filepath=os.path.join(OUT,name+".png");bpy.ops.render.render(write_still=True)
render("Top",(0,0,4))
e=math.radians(35);d=5.8
render("Perspective",(0,-d*math.cos(e),.15+d*math.sin(e)),False)
render("Side",(3,0,.15),True,1.6)
# Bağlı bileşen, kapalı manifold, boyut ve master tutarlılığı testleri.
bm=bmesh.new();bm.from_mesh(mesh)
remaining=set(bm.verts);components=0
while remaining:
    components+=1;stack=[remaining.pop()]
    while stack:
        v=stack.pop()
        for edge in v.link_edges:
            other=edge.other_vert(v)
            if other in remaining:remaining.remove(other);stack.append(other)
report={"export_executed":False,"panel_count":3,"diamond_count":3,"circumference_samples":N,"source_samples":SOURCE_N,"dimensions":list(obj.dimensions),"vertices":len(mesh.vertices),"triangles":len(mesh.polygons),"connected_components":components,"non_manifold_edges":sum(not e.is_manifold for e in bm.edges),"degenerate_faces":sum(f.calc_area()<1e-10 for f in bm.faces),"volume":bm.calc_volume(signed=True),"uv_layers":len(mesh.uv_layers),"panel_top_and_diamond_max_master_error":panel_exact_error,"cavity_depth":.040,"panel_base_inset":.006,"master_file_sha256_unchanged":hashlib.sha256(open(MASTER,'rb').read()).hexdigest()==before_hash}
bm.free()
assert report["connected_components"]==1 and report["non_manifold_edges"]==0 and report["degenerate_faces"]==0,report
assert report["master_file_sha256_unchanged"] and panel_exact_error<1e-6
for o in scene.objects:o.select_set(False)
obj.select_set(True);bpy.context.view_layer.objects.active=obj
with open(os.path.join(OUT,"qa.json"),"w") as f:json.dump(report,f,indent=2)
# Dosya açıldığında genel perspektif görünür.
render("Perspective",(0,-d*math.cos(e),.15+d*math.sin(e)),False)
# Her model kendi koleksiyonunda, stüdyo ayrı tutulur.
model_collection=bpy.data.collections.new("Block_3x1")
studio_collection=bpy.data.collections.new("Preview_Studio_3x1")
scene.collection.children.link(model_collection);scene.collection.children.link(studio_collection)
model_collection.objects.link(obj)
for o in list(scene.collection.objects):
    if o==obj:scene.collection.objects.unlink(o)
    else:studio_collection.objects.link(o);scene.collection.objects.unlink(o)
# Ortak AssetPreview sahnesi: mevcut iki modelin Transform ve verileri korunur.
family_scene.collection.children.link(model_collection)
obj.location=(0,2,0)
# Tek model inceleme kamerası yeni yerleşimi izler.
cam.location.y+=2
bpy.context.window.scene=family_scene
family_camera=family_scene.camera
family_camera.location=(0,-6,5.2)
family_camera.rotation_euler=(Vector((0,1,.15))-family_camera.location).to_track_quat('-Z','Y').to_euler()
family_camera.data.type='ORTHO';family_camera.data.ortho_scale=5.1
family_scene.render.resolution_x=1400;family_scene.render.resolution_y=1100;family_scene.render.resolution_percentage=100
family_scene.render.image_settings.file_format='PNG'
family_scene.render.filepath=os.path.join(OUT,"Family_Comparison.png")
bpy.ops.render.render(write_still=True)
assert all(signature(o)==protected_before[o.name] for o in protected),"Mevcut modeller değişti"
report["existing_1x1_2x1_unchanged"]=True
report["collection"]=model_collection.name
with open(os.path.join(OUT,"qa.json"),"w") as f:json.dump(report,f,indent=2)
for o in family_scene.objects:o.select_set(False)
obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.wm.save_as_mainfile(filepath=str(_PROJECT_ROOT / 'ArtProduction/AssetPreview/AssetPreview.blend'))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"Block_3x1.blend"),copy=True)
print(json.dumps(report))
