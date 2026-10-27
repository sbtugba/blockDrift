import bpy,bmesh,os,json,math
from mathutils import Vector
from mathutils.bvhtree import BVHTree
obj=bpy.data.objects["Block_2x2_Export"];three=bpy.data.objects["Block_3x1"]
# Proje kökü script dosyasından veya açık Blender dosyasından bulunur; kullanıcıya özel yol saklanmaz.
import os
from pathlib import Path
_PROJECT_ROOT = next((p for p in Path(globals().get("__file__", bpy.data.filepath or os.getcwd())).resolve().parents
                      if (p / "AGENTS.md").is_file()), None)
if _PROJECT_ROOT is None:
    raise RuntimeError("Scripti Block Rift proje klasöründen veya proje içindeki Blender dosyasından çalıştırın.")
out=str(_PROJECT_ROOT / 'ArtProduction/Block_2x2')
bm=bmesh.new();bm.from_mesh(obj.data);tree=BVHTree.FromBMesh(bm)
def height(x,y):
    hit=tree.ray_cast(Vector((x,y,1)),Vector((0,0,-1)))
    assert hit[0] is not None,("hole",x,y)
    return hit[0].z
centers=[(-.5,-.5),(.5,-.5),(-.5,.5),(.5,.5)]
floor=[height(x,y) for x,y in centers]
tops=[height(x,y+.25) for x,y in centers]
seams=[height(0,-.5),height(0,.5),height(-.5,0),height(.5,0),height(0,0)]
assert all(abs(z-.26)<1e-6 for z in floor)
assert all(abs(z-.30)<1e-6 for z in tops)
assert all(abs(z-.22)<1e-6 for z in seams)
errors=[]
for p,(dx,dy) in enumerate(centers):
    errors.append(max((obj.data.vertices[500+p*2016+k].co-Vector((three.data.vertices[490+k].co.x+1+dx,three.data.vertices[490+k].co.y+dy,three.data.vertices[490+k].co.z))).length for k in range(2016)))
assert max(errors)<2e-7
center_symmetry=[]
for d in (.025,.075,.125,.20):
    hs=[height(sx*d,sy*d) for sx,sy in [(-1,-1),(1,-1),(-1,1),(1,1)]]
    center_symmetry.append(max(hs)-min(hs))
assert max(center_symmetry)<1e-6
bm.faces.ensure_lookup_table()
intersections=[(a,b) for a,b in tree.overlap(tree) if a<b and not (set(bm.faces[a].verts)&set(bm.faces[b].verts))]
assert not intersections,intersections[:10]
def bounds(o):
    points=[o.matrix_world@Vector(v) for v in o.bound_box]
    return ([min(v[i] for v in points) for i in range(3)],[max(v[i] for v in points) for i in range(3)])
def overlap(a,b):
    return all(a[0][i]<b[1][i]-1e-6 and b[0][i]<a[1][i]-1e-6 for i in range(3))
bpy.context.view_layer.update()
others=[o for o in bpy.data.scenes["AssetPreview"].objects if o.name.startswith("Block_") and o!=obj]
assert not any(overlap(bounds(obj),bounds(o)) for o in others)
normals_ok=all(all(math.isfinite(c) for c in n.vector) and abs(n.vector.length-1)<1e-5 for n in obj.data.corner_normals)
assert normals_ok and bm.calc_volume(signed=True)>0
path=os.path.join(out,"qa.json");report=json.load(open(path))
report.update({"panel_centers_local_xy":centers,"grid_spacing_x":1.0,"grid_spacing_y":1.0,"base_gap_x":.012,"base_gap_y":.012,"flat_top_gap_x":.160,"flat_top_gap_y":.160,"diamond_floor_heights":floor,"panel_flat_heights":tops,"separator_and_center_heights":seams,"center_diagonal_symmetry_max_error":max(center_symmetry),"all_panel_coordinate_errors_vs_approved_3x1":errors,"nonadjacent_triangle_intersections":len(intersections),"finite_unit_normals":normals_ok,"positive_volume":True,"unit_scale":list(obj.scale)==[1,1,1],"zero_rotation":all(abs(v)<1e-6 for v in obj.rotation_euler),"bottom_center_pivot":abs(min(v.co.z for v in obj.data.vertices))<1e-6 and all(abs(min(v.co[i] for v in obj.data.vertices)+max(v.co[i] for v in obj.data.vertices))<1e-6 for i in (0,1)),"preview_location":list(obj.location),"overlaps_existing_models":False})
bm.free()
assert report["unit_scale"] and report["zero_rotation"] and report["bottom_center_pivot"]
with open(path,"w") as f:json.dump(report,f,indent=2)
# İnceleme kadrajında model çevresinde boşluk bırakılır; asıl stüdyoya dokunulmaz.
review=bpy.data.scenes["BR_Block_2x2_Review"];cam=review.camera
cam.data.type='ORTHO';cam.data.ortho_scale=3.25;cam.location=(3.5,2,4)
cam.rotation_euler=(Vector((3.5,2,.15))-cam.location).to_track_quat('-Z','Y').to_euler()
review.render.filepath=os.path.join(out,"Top.png")
bpy.context.window.scene=review;bpy.ops.render.render(write_still=True)
# Kaydedilen inceleme kamerasını 40 derece perspektife geri getir.
e=math.radians(40);d=4.8
cam.data.type='PERSP';cam.data.lens=55
cam.location=(3.5,2-d*math.cos(e),.15+d*math.sin(e))
cam.rotation_euler=(Vector((3.5,2,.15))-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.context.window.scene=bpy.data.scenes["AssetPreview"]
bpy.ops.wm.save_as_mainfile(filepath=str(_PROJECT_ROOT / 'ArtProduction/AssetPreview/AssetPreview.blend'))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,"Block_2x2.blend"),copy=True)
print(json.dumps(report))
