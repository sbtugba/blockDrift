import bpy,bmesh,math,os,json,hashlib,shutil
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
OUT=str(_PROJECT_ROOT / 'ArtProduction/Obstacle_1x1_Stone/Final')
FILE=str(_PROJECT_ROOT / 'ArtProduction/AssetPreview/AssetPreview.blend')
os.makedirs(OUT,exist_ok=True)
family=bpy.data.scenes["AssetPreview"];bpy.context.window.scene=family
obj=family.objects["Obstacle_1x1_Stone_Export"]
assert not obj.get("stone_final_refine"),"Final düzenleme zaten uygulanmış"
def signature(o):
    value=[tuple(tuple(r) for r in o.matrix_world),o.data.as_pointer() if o.data else 0,[c.name for c in o.users_collection]]
    if o.type=="MESH":
        d=o.data;value.append(hashlib.sha256(str(([tuple(v.co) for v in d.vertices],[(tuple(p.vertices),p.use_smooth,p.material_index) for p in d.polygons],[tuple(n.vector) for n in d.corner_normals],[[tuple(v.uv) for v in u.data] for u in d.uv_layers],[m.as_pointer() for m in d.materials])).encode()).hexdigest())
    if o.type=="CAMERA":value.append((o.data.type,o.data.lens,o.data.ortho_scale))
    if o.type=="LIGHT":value.append((o.data.type,o.data.energy,tuple(o.data.color)))
    return value
protected=[o for o in family.objects if o!=obj];protected_before={o.name:signature(o) for o in protected}
backup=os.path.join(OUT,"Before_FinalRefine.blend")
assert not os.path.exists(backup),"Yedek üzerine yazılmaz"
bpy.ops.wm.save_as_mainfile(filepath=backup,copy=True)
old=obj.data
oldcoords=[v.co.copy() for v in old.vertices]
old_matrix=obj.matrix_world.copy();old_dimensions=obj.dimensions.copy()
surface=next(n.image for n in old.materials[0].node_tree.nodes if n.type=='TEX_IMAGE' and n.image and n.image.name.startswith("Stone_1x1_Surface"))
W,H=surface.size;pixel=np.array(surface.pixels[:],np.float32).reshape(H,W,4)
locked=set();uv=old.uv_layers.active
# Koyu çatlak piksellerini taşıyan yüzlerin tüm vertexleri kilitlenir.
# Böylece UV'deki çatlak izdüşümü ve mesh çatlakları yer değiştirmez.
for p in old.polygons:
    ids=list(p.loop_indices);coords=np.array([uv.data[i].uv[:] for i in ids],np.float64)*np.array([W,H])
    minx=max(0,int(np.floor(coords[:,0].min())));maxx=min(W-1,int(np.ceil(coords[:,0].max())))
    miny=max(0,int(np.floor(coords[:,1].min())));maxy=min(H-1,int(np.ceil(coords[:,1].max())))
    if minx>maxx or miny>maxy:continue
    x,y=np.meshgrid(np.arange(minx,maxx+1)+.5,np.arange(miny,maxy+1)+.5)
    a,b,c=coords;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
    if abs(den)<1e-10:continue
    wa=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den
    wb=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den
    wc=1-wa-wb
    if np.any((wa>=0)&(wb>=0)&(wc>=0)&(pixel[miny:maxy+1,minx:maxx+1,0]<.4)):
        locked.update(p.vertices)
crack_angles=[math.radians(d) for d in (80,209,320)]+[math.atan2(-.5,-.27),math.atan2(-.16,.5)]
def angle_distance(a,b):return abs((a-b+math.pi)%(2*math.pi)-math.pi)
for v in old.vertices:
    x,y,z=v.co;a=math.atan2(y,x)
    if any(angle_distance(a,ca)<.085 for ca in crack_angles):locked.add(v.index)
obj.data=old.copy();mesh=obj.data;mesh.name="Obstacle_Stone_Final"
changed=[]
for v in mesh.vertices:
    if v.index in locked:continue
    x,y,z=oldcoords[v.index];radius=math.hypot(x-.008,y+.005);a=math.atan2(y+.005,x-.008)
    extent=max(abs(x),abs(y))
    # Yalnız merkezi oyuğun dudak ve iç duvarı; aşınmış dış sınır, taban ve köşeler korunur.
    if .16<extent<.375 and .229<z<.290:
        if any(math.hypot(x-px,y-py)<.055 for px,py in [(-.285,.225),(-.20,.29),(.29,-.22)]):continue
        amplitude=.0031*math.sin(5*a+.37)+.0014*math.sin(11*a-1.2)
        # Az sayıda küçük yontu; rastgele mikro gürültü eklenmez.
        for ca in (.6,2.55,4.8):
            amplitude-=.0025*max(0,1-angle_distance(a,ca)/.14)
        v.co.x+=amplitude*math.cos(a)
        v.co.y+=amplitude*math.sin(a)
        v.co.z+=.0012*math.sin(4*a+.8)*(.5+.5*math.sin(a-.2))
        changed.append(v.index)
mesh.update()
# Küçük üçgenler arasındaki normal kırılmaları temizlenir.
# Büyük yontu düzlemlerinin sınırları ve sert kırıklar ayrı normal gruplarında tutulur.
polygon=[(-.34,-.19),(-.23,-.34),(.08,-.29),(.34,-.08),(.23,.14),(.015,.35),(-.26,.20)]
def cross(a,b):return a[0]*b[1]-a[1]*b[0]
def facet_label(center,normal):
    if not(center.z>.245 and normal.z>.65 and max(abs(center.x),abs(center.y))<.445):return None
    a=math.atan2(center.y+.005,center.x-.008);d=(math.cos(a),math.sin(a));hits=[]
    for idx,(p,q) in enumerate(zip(polygon,polygon[1:]+polygon[:1])):
        edge=(q[0]-p[0],q[1]-p[1]);den=cross(d,edge)
        if abs(den)<1e-9:continue
        r=cross(p,edge)/den;u=cross(p,d)/den
        if r>0 and 0<=u<=1:hits.append((r,idx))
    return min(hits)[1] if hits else None
bm=bmesh.new();bm.from_mesh(mesh);bm.normal_update();bm.faces.ensure_lookup_table();bm.verts.ensure_lookup_table()
labels={f.index:facet_label(f.calc_center_median(),f.normal) for f in bm.faces}
for edge in bm.edges:
    faces=list(edge.link_faces)
    edge.smooth=len(faces)==2 and faces[0].normal.angle(faces[1].normal)<math.radians(12)
    if len(faces)==2:
        la,lb=labels[faces[0].index],labels[faces[1].index]
        if la is not None and lb is not None and la!=lb:edge.smooth=False
loop_normals={}
for vertex in bm.verts:
    for seed in vertex.link_faces:
        allowed=labels[seed.index];group={seed};pending=[seed]
        while pending:
            face=pending.pop()
            for edge in vertex.link_edges:
                if not edge.smooth or face not in edge.link_faces:continue
                for candidate in edge.link_faces:
                    if candidate in group:continue
                    label=labels[candidate.index]
                    if allowed is not None and label is not None and label!=allowed:continue
                    group.add(candidate);pending.append(candidate)
        normal=Vector()
        for face in group:
            lp=next(lp for lp in face.loops if lp.vert==vertex)
            a=(lp.link_loop_prev.vert.co-vertex.co).normalized();b=(lp.link_loop_next.vert.co-vertex.co).normalized()
            normal+=face.normal*max(face.calc_area(),1e-12)*a.angle(b)
        normal.normalize()
        loop_normals[(vertex.index,seed.index)]=tuple(normal)
bm.free()
for p in mesh.polygons:p.use_smooth=True
mesh.normals_split_custom_set([loop_normals[(loop.vertex_index,poly.index)] for poly in mesh.polygons for loop in [mesh.loops[i] for i in poly.loop_indices]])
# Renk, pürüzlülük ve normal gücü URP'ye eşlenebilir ayrı kontroller olarak kalır.
material=old.materials[0].copy();material.name="BR_Stone_Final_URP"
material.diffuse_color=(.085,.085,.085,1)
mesh.materials.clear();mesh.materials.append(material)
texture_hashes={}
for node in material.node_tree.nodes:
    if node.type=='TEX_IMAGE' and node.image:
source=node.image
        pixels=np.array(source.pixels[:],np.float32)
        new=bpy.data.images.new(source.name+"_Final",width=source.size[0],height=source.size[1],alpha=True)
        new.colorspace_settings.name=source.colorspace_settings.name
        new.pixels.foreach_set(pixels);new.update()
        src=bpy.path.abspath(source.filepath_raw)
        dst=os.path.join(OUT,os.path.basename(src))
        new.filepath_raw=dst;new.file_format='PNG';new.save();new.pack();node.image=new
        texture_hashes[os.path.basename(src)]=bool(np.array_equal(pixels,np.array(new.pixels[:],np.float32)))
    if node.type=='MIX_RGB':node.label="URP Base Color Tint"
    if node.type=='BSDF_PRINCIPLED':node.label="URP Roughness = 1 - Smoothness"
    if node.type=='NORMAL_MAP':node.label="URP Normal Scale"
# Karşılaştırma kopyası aynı son geometriyi, aynı birim ölçekte gösterir.
bpy.data.objects["Obstacle_1x1_Stone_Export_StoneComparison"].data=mesh
mesh.calc_tangents(uvmap=mesh.uv_layers.active.name)
bm=bmesh.new();bm.from_mesh(mesh);bm.faces.ensure_lookup_table()
tree=BVHTree.FromBMesh(bm)
pairs=[(a,b) for a,b in tree.overlap(tree) if a<b and not(set(bm.faces[a].verts)&set(bm.faces[b].verts))]
remaining=set(bm.verts);components=0
while remaining:
    components+=1;pending=[remaining.pop()]
    while pending:
        v=pending.pop()
        for e in v.link_edges:
            other=e.other_vert(v)
            if other in remaining:remaining.remove(other);pending.append(other)
report={"revision":"Final","dimensions":list(obj.dimensions),"vertices":len(mesh.vertices),"triangles":len(mesh.polygons),"changed_cavity_vertices":len(changed),"max_vertex_displacement":max((v.co-oldcoords[v.index]).length for v in mesh.vertices),"locked_crack_vertices":len(locked),"max_locked_crack_vertex_displacement":max((mesh.vertices[i].co-oldcoords[i]).length for i in locked),"texture_files_identical":texture_hashes,"uv_identical":all(tuple(d.uv)==tuple(old.uv_layers.active.data[i].uv) for i,d in enumerate(mesh.uv_layers.active.data)),"components":components,"non_manifold_edges":sum(not e.is_manifold for e in bm.edges),"degenerate_faces":sum(f.calc_area()<1e-10 for f in bm.faces),"volume":bm.calc_volume(signed=True),"nonadjacent_triangle_intersections":len(pairs),"finite_unit_normals":all(all(math.isfinite(c) for c in n.vector) and abs(n.vector.length-1)<1e-5 for n in mesh.corner_normals),"finite_tangents":all(all(math.isfinite(c) for c in lp.tangent) for lp in mesh.loops),"transform_unchanged":obj.matrix_world==old_matrix,"other_models_and_preview_studio_unchanged":all(signature(o)==protected_before[o.name] for o in protected),"export_executed":False,"unity_urp_runtime_verified":False,"comparison_scale_1x1":list(bpy.data.objects["Block_1x1_Export_StoneComparison"].scale),"comparison_scale_stone":list(bpy.data.objects["Obstacle_1x1_Stone_Export_StoneComparison"].scale)}
bm.free()
report["source_texture_pixels_identical"]=report.pop("texture_files_identical")
assert report["changed_cavity_vertices"]>0
assert components==1 and report["non_manifold_edges"]==0 and report["degenerate_faces"]==0 and not pairs
assert report["finite_unit_normals"] and report["finite_tangents"] and report["volume"]>0
assert report["max_locked_crack_vertex_displacement"]==0 and all(texture_hashes.values()) and report["uv_identical"]
assert report["transform_unchanged"] and report["other_models_and_preview_studio_unchanged"]
assert all(abs(a-b)<1e-6 for a,b in zip(obj.dimensions,old_dimensions))
obj["stone_final_refine"]=True
with open(os.path.join(OUT,"qa.json"),"w") as f:json.dump(report,f,indent=2)
print(json.dumps(report))
