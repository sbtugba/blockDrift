import bpy, math, os, json, bmesh
from mathutils import Vector
# Proje kökü script dosyasından veya açık Blender dosyasından bulunur; kullanıcıya özel yol saklanmaz.
import os
from pathlib import Path
_PROJECT_ROOT = next((p for p in Path(globals().get("__file__", bpy.data.filepath or os.getcwd())).resolve().parents
                      if (p / "AGENTS.md").is_file()), None)
if _PROJECT_ROOT is None:
    raise RuntimeError("Scripti Block Rift proje klasöründen veya proje içindeki Blender dosyasından çalıştırın.")
OUT=str(_PROJECT_ROOT / 'ArtProduction/Block_1x1_Final')
os.makedirs(OUT,exist_ok=True)
scene=bpy.context.scene;obj=scene.objects['Block_1x1'];old=obj.data
assert len(old.vertices)==4800,"Rev03 kaynak modeli gerekli"
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"Backup_Before_Final.blend"),copy=True)
N=192
# İlk dört alt kesit aynen korunur; dik yan gövde 0.220 yüksekliğe uzatılır.
profiles=[(.477,.125,0,0),(.490,.135,.004,0),(.498,.143,.013,0),(.500,.145,.025,0),(.500,.145,.220,0)]
# Çeyrek elips: dik gövdeden düz tepeye monoton dışbükey geçiş.
for k in range(1,13):
    t=math.pi*.5*k/12
    profiles.append((.420+.080*math.cos(t),.105+.040*math.cos(t),.220+.080*math.sin(t),0))
profiles += [(.138,.010,.300,math.pi/4),(.1355,.0095,.299,math.pi/4),(.133,.009,.296,math.pi/4),(.1315,.009,.291,math.pi/4),(.1315,.009,.269,math.pi/4),(.130,.009,.264,math.pi/4),(.127,.010,.260,math.pi/4),(.121,.011,.260,math.pi/4)]
def radial(a,h,r,rot):
    x=abs(math.cos(a-rot));y=abs(math.sin(a-rot));d=h/max(x,y)
    if d*min(x,y)<=h-r:return d
    c=h-r;b=c*(x+y)
    return b+math.sqrt(max(0,b*b-(2*c*c-r*r)))
verts=[];normals=[]
for j,(h,r,z,rot) in enumerate(profiles):
    if j==0:dh,dz=-1,0
    elif j<=4:
        dh=profiles[min(j+1,4)][0]-profiles[j-1][0];dz=profiles[min(j+1,4)][2]-profiles[j-1][2]
        if j==4:dh,dz=0,1
    elif j<=16:
        t=math.pi*.5*(j-4)/12;dh=-math.sin(t);dz=math.cos(t)
    elif j==17:dh,dz=-1,0
    elif j==len(profiles)-1:dh,dz=-1,0
    else:
        dh=profiles[j+1][0]-profiles[j-1][0];dz=profiles[j+1][2]-profiles[j-1][2]
    for i in range(N):
        a=2*math.pi*i/N
        if j<4:
            v=old.vertices[j*N+i].co.copy()
        else:
            d=radial(a,h,r,rot);v=Vector((d*math.cos(a),d*math.sin(a),z))
        verts.append(tuple(v))
        # Kesit ve yuvarlatılmış kareye ait analitik normaller; uzun üçgenler ışığı bükmez.
        x=v.x*math.cos(rot)+v.y*math.sin(rot);y=-v.x*math.sin(rot)+v.y*math.cos(rot)
        c=h-r
        nx=x-max(-c,min(c,x));ny=y-max(-c,min(c,y))
        xy=Vector((nx,ny,0)).normalized()
        n=Vector(((xy.x*math.cos(rot)-xy.y*math.sin(rot))*dz,(xy.x*math.sin(rot)+xy.y*math.cos(rot))*dz,-dh)).normalized()
        normals.append(tuple(n))
faces=[]
for j in range(len(profiles)-1):
    for i in range(N):
        k=(i+1)%N
        faces.append((j*N+i,j*N+k,(j+1)*N+k,(j+1)*N+i))
faces.append(tuple(reversed(range(N))))
faces.append(tuple((len(profiles)-1)*N+i for i in range(N)))
mesh=bpy.data.meshes.new("Block_1x1_Final_ConvexPanel")
mesh.from_pydata(verts,[],faces);mesh.update()
for m in old.materials:mesh.materials.append(m)
obj.data=mesh
for p in mesh.polygons:p.use_smooth=True
for o in scene.objects:o.select_set(False)
obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
tri=obj.modifiers.new("DeterministicTriangles",'TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
# Kapak normalleri ayrı: taban -Z, oyuk tabanı +Z.
ln=[]
for p in mesh.polygons:
    ids=list(p.vertices)
    bottom=all(v<N for v in ids)
    floor=all(v>=(len(profiles)-1)*N for v in ids)
    for idx in p.loop_indices:
        ln.append((0,0,-1) if bottom else (0,0,1) if floor else normals[mesh.loops[idx].vertex_index])
mesh.normals_split_custom_set(ln)
obj["revision"]="Final - refined upright body and diamond";obj["cavity_depth"]=.040
bm=bmesh.new();bm.from_mesh(mesh)
report={"dimensions":list(obj.dimensions),"triangles":len(mesh.polygons),"vertices":len(mesh.vertices),"non_manifold_edges":sum(not e.is_manifold for e in bm.edges),"degenerate_faces":sum(f.calc_area()<1e-10 for f in bm.faces),"lower_body_max_coordinate_delta":max((Vector(verts[j*N+i])-old.vertices[j*N+i].co).length for j in range(4) for i in range(N)),"upper_profile_monotonic":all(profiles[k+1][2]>=profiles[k][2] and profiles[k+1][0]<=profiles[k][0] for k in range(4,16)),"uv_layers":len(mesh.uv_layers)}
bm.free()
report["vertical_side_height_old"]=.180
report["vertical_side_height_final"]=.195
report["upper_shoulder_width_old"]=.095
report["upper_shoulder_width_final"]=.080
report["top_symmetry_max_error"]=max((mesh.vertices[j*N+i].co-Vector((-mesh.vertices[j*N+(i+N//2)%N].co.x,-mesh.vertices[j*N+(i+N//2)%N].co.y,mesh.vertices[j*N+(i+N//2)%N].co.z))).length for j in range(17,25) for i in range(N))
with open(os.path.join(OUT,"qa.json"),"w") as f:json.dump(report,f,indent=2)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,"Block_1x1_Final.fbx"),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
cam=scene.camera
def render(name,loc,orthographic):
    cam.location=loc;cam.rotation_euler=(Vector((0,0,.15))-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO' if orthographic else 'PERSP'
    cam.data.ortho_scale=1.45;cam.data.lens=55
    scene.render.filepath=os.path.join(OUT,name+".png")
    bpy.ops.render.render(write_still=True)
render("Top",(0,0,3),True)
render("Front",(0,-3,.15),True)
e=math.radians(35);d=2.6
render("Perspective35",(0,-d*math.cos(e),.15+d*math.sin(e)),False)
render("Orthographic35",(0,-d*math.cos(e),.15+d*math.sin(e)),True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"Block_1x1_Final.blend"))
print(json.dumps(report))
