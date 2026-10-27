import bpy,bmesh,os,json,math,hashlib,re
from mathutils import Vector
from mathutils.bvhtree import BVHTree
family=bpy.data.scenes["AssetPreview"];bpy.context.window.scene=family
obj=family.objects["Block_4x1_Export"];three=family.objects["Block_3x1"]
# Proje kökü script dosyasından veya açık Blender dosyasından bulunur; kullanıcıya özel yol saklanmaz.
import os
from pathlib import Path
_PROJECT_ROOT = next((p for p in Path(globals().get("__file__", bpy.data.filepath or os.getcwd())).resolve().parents
                      if (p / "AGENTS.md").is_file()), None)
if _PROJECT_ROOT is None:
    raise RuntimeError("Scripti Block Rift proje klasöründen veya proje içindeki Blender dosyasından çalıştırın.")
out=str(_PROJECT_ROOT / 'ArtProduction/Block_4x1')
def material_sig(m):
    if not m:return None
    return (m.use_nodes,tuple(m.diffuse_color),[(n.type,[(i.name,tuple(i.default_value) if hasattr(i.default_value,'__len__') and not isinstance(i.default_value,str) else i.default_value) for i in n.inputs if hasattr(i,'default_value') and isinstance(i.default_value,(float,int,str)) or hasattr(i,'default_value') and hasattr(i.default_value,'__len__') and not isinstance(i.default_value,str)]) for n in m.node_tree.nodes] if m.use_nodes else None)
def state(o):
    d={"matrix":[list(r) for r in o.matrix_world],"hide":[o.hide_viewport,o.hide_render],"type":o.type}
    if o.type=="MESH":
        m=o.data
        d["meshhash"]=hashlib.sha256(str(([tuple(v.co) for v in m.vertices],[(tuple(p.vertices),p.material_index,p.use_smooth) for p in m.polygons],[tuple(n.vector) for n in m.corner_normals],[[tuple(v.uv) for v in u.data] for u in m.uv_layers])).encode()).hexdigest()
        d["materials"]=[material_sig(m) for m in m.materials]
    if o.type=="CAMERA":d["settings"]=(o.data.type,o.data.lens,o.data.ortho_scale)
    if o.type=="LIGHT":d["settings"]=(o.data.type,o.data.energy,tuple(o.data.color),o.data.size)
    return d
before_objects=set(bpy.data.objects);before_collections=set(bpy.data.collections)
with bpy.data.libraries.load(str(_PROJECT_ROOT / 'ArtProduction/AssetPreview/AssetPreview_Before4x1.blend'),link=False) as (src,dst):
    dst.scenes=["AssetPreview"]
backup=dst.scenes[0]
bpy.context.window.scene=backup;bpy.context.view_layer.update()
bpy.context.window.scene=family;bpy.context.view_layer.update()
preserved={}
for original in list(family.objects):
    if original==obj:continue
    candidates=[o for o in backup.objects if re.sub(r"\.\d+$","",o.name)==re.sub(r"\.\d+$","",original.name) and o.type==original.type]
    assert len(candidates)==1,(original.name,[o.name for o in candidates])
    preserved[original.name]=state(original)==state(candidates[0])
assert all(preserved.values()),preserved
# Yalnız geçici inceleme için yüklenen yedek objeler temizlenir.
bpy.data.scenes.remove(backup)
for imported in set(bpy.data.objects)-before_objects:bpy.data.objects.remove(imported,do_unlink=True)
for imported in set(bpy.data.collections)-before_collections:bpy.data.collections.remove(imported)
bm=bmesh.new();bm.from_mesh(obj.data);tree=BVHTree.FromBMesh(bm)
def height(x,y):
    hit=tree.ray_cast(Vector((x,y,1)),Vector((0,0,-1)))
    assert hit[0] is not None
    return hit[0].z
centers=(-1.5,-.5,.5,1.5)
floor=[height(x,0) for x in centers]
plateau=[height(x,.25) for x in centers]
seams=[height(x,0) for x in (-1,0,1)]
assert all(abs(h-.260)<1e-6 for h in floor)
assert all(abs(h-.300)<1e-6 for h in plateau)
assert all(abs(h-.220)<1e-6 for h in seams)
errors=[]
for p,offset in enumerate(centers):
    errors.append(max((obj.data.vertices[490+p*2016+k].co-Vector((three.data.vertices[490+k].co.x+1+offset,three.data.vertices[490+k].co.y,three.data.vertices[490+k].co.z))).length for k in range(2016)))
assert max(errors)<2e-7,errors
overlaps=tree.overlap(tree)
bm.faces.ensure_lookup_table()
nonadjacent=[(a,b) for a,b in overlaps if a<b and not (set(bm.faces[a].verts)&set(bm.faces[b].verts))]
assert not nonadjacent,nonadjacent[:10]
remaining=set(bm.verts);components=0
while remaining:
    components+=1;stack=[remaining.pop()]
    while stack:
        v=stack.pop()
        for e in v.link_edges:
            w=e.other_vert(v)
            if w in remaining:remaining.remove(w);stack.append(w)
report=json.load(open(os.path.join(out,"qa.json")))
report.update({"object_name":obj.name,"collection":"Block_4x1","existing_models_and_preview_studio_unchanged":True,"backup_state_comparison":preserved,"panel_geometry_max_error_vs_3x1":errors,"cell_centers_local_x":list(centers),"cell_spacing":1.0,"diamond_floor_heights":floor,"flat_top_heights":plateau,"separator_heights":seams,"nonadjacent_triangle_intersections":len(nonadjacent),"finite_unit_normals":all(all(math.isfinite(c) for c in n.vector) and abs(n.vector.length-1)<1e-5 for n in obj.data.corner_normals),"unit_scale":list(obj.scale)==[1,1,1],"zero_rotation":all(abs(a)<1e-6 for a in obj.rotation_euler),"connected_components":components,"positive_volume":bm.calc_volume(signed=True)>0,"preview_location":list(obj.location)})
bm.free()
assert report["finite_unit_normals"] and report["positive_volume"] and components==1
with open(os.path.join(out,"qa.json"),"w") as f:json.dump(report,f,indent=2)
# Ortak sahnede kamera/stüdyo korunur; yalnız viewport kadrajı düzenlenir.
from mathutils import Quaternion
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_location=Vector((0,2,.15))
        area.spaces.active.region_3d.view_distance=8.5
        area.spaces.active.region_3d.view_rotation=Quaternion((1,0,0),math.radians(35))
for o in family.objects:o.select_set(False)
obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.wm.save_as_mainfile(filepath=str(_PROJECT_ROOT / 'ArtProduction/AssetPreview/AssetPreview.blend'))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,"Block_4x1.blend"),copy=True)
print(json.dumps(report))
