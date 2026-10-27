import bpy, bmesh, math, os, json, hashlib, datetime
from mathutils import Vector
from mathutils.bvhtree import BVHTree

# Proje kökü script dosyasından veya açık Blender dosyasından bulunur; kullanıcıya özel yol saklanmaz.
import os
from pathlib import Path
_PROJECT_ROOT = next((p for p in Path(globals().get("__file__", bpy.data.filepath or os.getcwd())).resolve().parents
                      if (p / "AGENTS.md").is_file()), None)
if _PROJECT_ROOT is None:
    raise RuntimeError("Scripti Block Rift proje klasöründen veya proje içindeki Blender dosyasından çalıştırın.")
OUT = str(_PROJECT_ROOT / 'ArtProduction/Special_1x1_Star')
os.makedirs(OUT, exist_ok=True)
scene = bpy.data.scenes['AssetPreview']
bpy.context.window.scene = scene
assert not bpy.data.objects.get('Special_1x1_Star_Export'), 'Star already exists; do not overwrite.'
assert os.path.basename(bpy.data.filepath) == 'AssetPreview.blend'
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'Before_Star_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend'), copy=True)

def signature(o):
    m = o.data
    geometry = ([tuple(v.co) for v in m.vertices], [(tuple(p.vertices),p.material_index,p.use_smooth) for p in m.polygons], [tuple(n.vector) for n in m.corner_normals], [[tuple(d.uv) for d in uv.data] for uv in m.uv_layers]) if o.type=='MESH' else None
    mats = [(m.name, [(n.type, [(i.name,str(i.default_value)) for i in n.inputs if hasattr(i,'default_value')]) for n in m.node_tree.nodes]) for m in o.data.materials if m and m.use_nodes] if o.type=='MESH' else None
    return hashlib.sha256(str((geometry,mats,[list(r) for r in o.matrix_world],o.hide_viewport,o.hide_render)).encode()).hexdigest()
protected = {o.name:signature(o) for o in scene.objects}
json.dump(protected,open(os.path.join(OUT,'protected_before.json'),'w'),indent=2)

def linear(s):
    return s/12.92 if s<=.04045 else ((s+.055)/1.055)**2.4

def material(name,hexcolor,rough,metal):
    assert not bpy.data.materials.get(name), name+' exists'
    mat=bpy.data.materials.new(name);mat.use_nodes=True
    rgba=tuple(linear(int(hexcolor[i:i+2],16)/255) for i in (0,2,4))+(1,)
    bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    bs.inputs['Base Color'].default_value=rgba
    bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
    bs.inputs['Coat Weight'].default_value=.16;bs.inputs['Coat Roughness'].default_value=.22
    mat.diffuse_color=rgba;mat['source_srgb_hex']='#'+hexcolor
    mat['unity_urp_smoothness']=1-rough
    return mat

ivory=material('MAT_Star_Ivory','F0E8DB',.31,0)
gold=material('MAT_Star_Gold','FFC247',.25,.04)
N=80
angles=[math.pi/2+2*math.pi*i/N for i in range(N)]

# Beş uçlu simetrik yıldız: keskin noktalar yerine kısa kuadratik yaylar.
corners=[Vector(((.318 if i%2==0 else .156)*math.cos(math.pi/2+2*math.pi*i/10),(.318 if i%2==0 else .156)*math.sin(math.pi/2+2*math.pi*i/10))) for i in range(10)]
outline=[]
for i,p in enumerate(corners):
    trim=.036 if i%2==0 else .020
    a=p+(corners[(i-1)%10]-p).normalized()*trim
    b=p+(corners[(i+1)%10]-p).normalized()*trim
    for k in range(9):
        t=k/8;outline.append(a*(1-t)**2+p*2*t*(1-t)+b*t*t)

# Yay noktalarını örneklemeye ekle: yıldız uçlarında açısal örnekleme kırığı oluşmaz.
angles=sorted(set(round(a%(2*math.pi),10) for a in angles+[math.atan2(p.y,p.x) for p in outline]))
angles=[a for i,a in enumerate(angles) if i==0 or a-angles[i-1]>1e-6]
if 2*math.pi-angles[-1]+angles[0]<1e-6:angles.pop()
N=len(angles)

def cross(a,b):return a.x*b.y-a.y*b.x
def star_radius(angle):
    d=Vector((math.cos(angle),math.sin(angle)));hits=[]
    for a,b in zip(outline,outline[1:]+outline[:1]):
        e=b-a;den=cross(d,e)
        if abs(den)<1e-12:continue
        t=cross(a,e)/den;u=cross(a,d)/den
        if t>0 and -1e-7<=u<=1+1e-7:hits.append(t)
    assert hits
    return min(hits)
star=[Vector((star_radius(a)*math.cos(a),star_radius(a)*math.sin(a))) for a in angles]

def rounded(a,h,r):
    x,y=abs(math.cos(a)),abs(math.sin(a));d=h/max(x,y)
    if d*min(x,y)<=h-r:return d
    c=h-r;b=c*(x+y)
    return b+math.sqrt(max(0,b*b-(2*c*c-r*r)))

verts=[];faces=[];face_mats=[];vertex_normals=[];flat_faces=set()
def ring(points,z):
    ids=[]
    for p in points:
        ids.append(len(verts));verts.append((p.x,p.y,z));vertex_normals.append(None)
    return ids
def rounded_points(h,r):return [Vector((math.cos(a),math.sin(a)))*rounded(a,h,r) for a in angles]
def star_points(scale):return [p*scale for p in star]
def face(ids,mat,flat=False):
    faces.append(tuple(ids));face_mats.append(mat)
    if flat:flat_faces.add(len(faces)-1)
def bridge(a,b,mat,flat=False):
    for i in range(N):
        k=(i+1)%N
        face((a[i],a[k],b[k]),mat,flat);face((a[i],b[k],b[i]),mat,flat)
def cap(ids,z,mat,up=True):
    center=len(verts);verts.append((0,0,z));vertex_normals.append((0,0,1 if up else -1))
    for i in range(N):
        k=(i+1)%N
        face((center,ids[i],ids[k]) if up else (center,ids[k],ids[i]),mat,True)

profiles=[(.477,.155,0),(.490,.168,.004),(.498,.177,.013),(.5,.18,.025),(.5,.18,.220)]
for k in range(1,10):
    t=math.pi*.5*k/9
    profiles.append((.420+.080*math.cos(t),.130+.050*math.cos(t),.220+.080*math.sin(t)))
body=[ring(rounded_points(h,r),z) for h,r,z in profiles]
for j,(h,r,z) in enumerate(profiles):
    if j==0:dh,dz=-1,0
    elif j==4:dh,dz=0,1
    elif j>4:
        t=math.pi*.5*(j-4)/9;dh,dz=-math.sin(t),math.cos(t)
    else:dh=profiles[j+1][0]-profiles[j-1][0];dz=profiles[j+1][2]-profiles[j-1][2]
    for idx in body[j]:
        x,y,_=verts[idx];c=h-r
        xy=Vector((x-max(-c,min(c,x)),y-max(-c,min(c,y)),0)).normalized()
        vertex_normals[idx]=tuple(Vector((xy.x*dz,xy.y*dz,-dh)).normalized())
for a,b in zip(body,body[1:]):bridge(a,b,0)
cap(body[0],0,0,False)

# Gerçek kör oyuk: çevre dudak, duvar, dip kavisleri ve kapalı taban.
cavity_profiles=[(1,.300),(.991,.2995),(.976,.296),(.962,.289),(.960,.273),(.951,.264),(.931,.259),(.915,.258)]
cavity=[ring(star_points(scale),z) for scale,z in cavity_profiles]
bridge(body[-1],cavity[0],0,True)
for a,b in zip(cavity,cavity[1:]):bridge(a,b,0)
cap(cavity[-1],.258,0,True)

def ring_normals(rings,inward=False):
    for j,ids in enumerate(rings):
        for i,idx in enumerate(ids):
            if j==0 or j==len(rings)-1:
                vertex_normals[idx]=(0,0,1) if inward or j==len(rings)-1 else (0,0,-1)
                continue
            tangent=Vector(verts[ids[(i+1)%N]])-Vector(verts[ids[(i-1)%N]])
            prev=Vector(verts[idx])-Vector(verts[rings[j-1][i]])
            nxt=Vector(verts[rings[j+1][i]])-Vector(verts[idx])
            direction=prev.normalized()+nxt.normalized()
            vertex_normals[idx]=tuple(tangent.cross(direction).normalized())
ring_normals(cavity,True)
body_vertex_count=len(verts)

# Altın inlay bağımsız kapalı mesh adasıdır; oyuk tabanıyla ve duvarıyla temas etmez.
star_profiles=[(.910,.262),(.931,.264),(.938,.269),(.938,.279),(.930,.284),(.913,.288),(.892,.290)]
inlay=[ring(star_points(scale),z) for scale,z in star_profiles]
for a,b in zip(inlay,inlay[1:]):bridge(a,b,1)
cap(inlay[0],.262,1,False);cap(inlay[-1],.290,1,True)
ring_normals(inlay)

mesh=bpy.data.meshes.new('Special_1x1_Star_Mesh');mesh.from_pydata(verts,[],faces);mesh.update()
mesh.materials.append(ivory);mesh.materials.append(gold)
obj=bpy.data.objects.new('Special_1x1_Star_Export',mesh)
coll=bpy.data.collections.new('Special_1x1_Star');scene.collection.children.link(coll);coll.objects.link(obj)
obj.location=(5.2,4,0)
for p,matidx in zip(mesh.polygons,face_mats):p.material_index=matidx;p.use_smooth=True
for o in bpy.context.selected_objects:o.select_set(False)
obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
loopnorm=[]
for p in mesh.polygons:
    flat=p.index in flat_faces
    for li in p.loop_indices:
        loopnorm.append(tuple(p.normal) if flat else vertex_normals[mesh.loops[li].vertex_index])
mesh.normals_split_custom_set(loopnorm)
obj['asset_role']='Unity grid special star block; two material slots'
obj['grid_size']=[1,1];obj['body_height']=.30;obj['inlay_top_z']=.290;obj['cavity_floor_z']=.258
obj['generator']='ArtProduction/Special_1x1_Star/generate_star.py'

bm=bmesh.new();bm.from_mesh(mesh);bm.faces.ensure_lookup_table()
tree=BVHTree.FromBMesh(bm)
overlaps=[(a,b) for a,b in tree.overlap(tree) if a<b and not(set(bm.faces[a].verts)&set(bm.faces[b].verts))]
components=[];unvisited=set(bm.verts)
while unvisited:
    todo=[unvisited.pop()];count=0
    while todo:
        v=todo.pop();count+=1
        for e in v.link_edges:
            other=e.other_vert(v)
            if other in unvisited:unvisited.remove(other);todo.append(other)
    components.append(count)
qa={'dimensions':list(obj.dimensions),'pivot_local':[0,0,0],'preview_location':list(obj.location),'scale':list(obj.scale),'vertices':len(mesh.vertices),'triangles':len(mesh.polygons),'components':components,'non_manifold_edges':sum(not e.is_manifold for e in bm.edges),'degenerate_faces':sum(f.calc_area()<1e-10 for f in bm.faces),'nonadjacent_intersections':len(overlaps),'signed_volume':bm.calc_volume(signed=True),'uv_layers':len(mesh.uv_layers),'finite_unit_normals':all(math.isfinite(n.vector.length) and abs(n.vector.length-1)<1e-4 for n in mesh.corner_normals),'star_top_below_body':.300-.290,'floor_vertical_clearance':.262-.258,'body_vertex_count':body_vertex_count,'protected_objects_unchanged':all(signature(scene.objects[name])==sig for name,sig in protected.items()),'material_hex':['#F0E8DB','#FFC247'],'unity_import_tested':False}
bm.free()
assert qa['non_manifold_edges']==0 and qa['degenerate_faces']==0 and qa['nonadjacent_intersections']==0
assert qa['protected_objects_unchanged'] and qa['finite_unit_normals'] and len(components)==2
json.dump(qa,open(os.path.join(OUT,'qa.json'),'w'),indent=2)
text=bpy.data.texts.new('Special_1x1_Star_generate.py');text.write(open(__file__,encoding='utf-8').read())
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print(json.dumps(qa))
