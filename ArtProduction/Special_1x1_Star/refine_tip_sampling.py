import bpy,os,json,bmesh,math
from mathutils.bvhtree import BVHTree
# Proje kökü script dosyasından veya açık Blender dosyasından bulunur; kullanıcıya özel yol saklanmaz.
import os
from pathlib import Path
_PROJECT_ROOT = next((p for p in Path(globals().get("__file__", bpy.data.filepath or os.getcwd())).resolve().parents
                      if (p / "AGENTS.md").is_file()), None)
if _PROJECT_ROOT is None:
    raise RuntimeError("Scripti Block Rift proje klasöründen veya proje içindeki Blender dosyasından çalıştırın.")
OUT=str(_PROJECT_ROOT / 'ArtProduction/Special_1x1_Star')
src=open(os.path.join(OUT,'generate_star.py'),encoding='utf-8').read()
ns={}
exec('import bpy,bmesh,math\nfrom mathutils import Vector\n'+src[src.index('N=80'):src.index("mesh=bpy.data.meshes.new")],ns)
obj=bpy.data.objects['Special_1x1_Star_Export'];old=obj.data
mesh=bpy.data.meshes.new('Special_1x1_Star_RoundedTips')
mesh.from_pydata(ns['verts'],[],ns['faces']);mesh.update()
for mat in old.materials:mesh.materials.append(mat)
for ob in list(bpy.data.objects):
    if ob.type=='MESH' and ob.data==old:ob.data=mesh
for p,mat in zip(mesh.polygons,ns['face_mats']):p.material_index=mat;p.use_smooth=True
bpy.context.window.scene=bpy.data.scenes['AssetPreview']
for ob in bpy.context.selected_objects:ob.select_set(False)
obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
normals=[]
for p in mesh.polygons:
    for li in p.loop_indices:normals.append(tuple(p.normal) if p.index in ns['flat_faces'] else ns['vertex_normals'][mesh.loops[li].vertex_index])
mesh.normals_split_custom_set(normals)
if old.users==0:bpy.data.meshes.remove(old)
bm=bmesh.new();bm.from_mesh(mesh);bm.faces.ensure_lookup_table();tree=BVHTree.FromBMesh(bm)
pairs=[(a,b) for a,b in tree.overlap(tree) if a<b and not(set(bm.faces[a].verts)&set(bm.faces[b].verts))]
qa=json.load(open(os.path.join(OUT,'qa.json')))
qa.update(vertices=len(mesh.vertices),triangles=len(mesh.polygons),non_manifold_edges=sum(not e.is_manifold for e in bm.edges),degenerate_faces=sum(f.calc_area()<1e-10 for f in bm.faces),nonadjacent_intersections=len(pairs),signed_volume=bm.calc_volume(signed=True),finite_unit_normals=all(math.isfinite(n.vector.length) and abs(n.vector.length-1)<1e-4 for n in mesh.corner_normals),body_vertex_count=ns['body_vertex_count'],angular_samples=ns['N'])
qa['components']=[ns['body_vertex_count'],len(mesh.vertices)-ns['body_vertex_count']]
print('QA',qa)
assert qa['non_manifold_edges']==qa['degenerate_faces']==qa['nonadjacent_intersections']==0
assert qa['finite_unit_normals']
bm.free();json.dump(qa,open(os.path.join(OUT,'qa.json'),'w'),indent=2)
text=bpy.data.texts['Special_1x1_Star_generate.py'];text.clear();text.write(src)
review=bpy.data.scenes['Review_Special_1x1_Star'];review.view_settings.view_transform='Standard';review.view_settings.exposure=-.6
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print(json.dumps(qa))
