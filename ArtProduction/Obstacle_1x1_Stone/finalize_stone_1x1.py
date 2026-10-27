import bpy,bmesh,math,os,json
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
obj=bpy.data.objects["Obstacle_1x1_Stone_Export"];mesh=obj.data
bpy.data.objects["Obstacle_1x1_Stone_Export_StoneComparison"].data=mesh
bm=bmesh.new();bm.from_mesh(mesh);tree=BVHTree.FromBMesh(bm)
bm.faces.ensure_lookup_table()
intersections=[(a,b) for a,b in tree.overlap(tree) if a<b and not(set(bm.faces[a].verts)&set(bm.faces[b].verts))]
remaining=set(bm.verts);components=0
while remaining:
    components+=1;stack=[remaining.pop()]
    while stack:
        v=stack.pop()
        for e in v.link_edges:
            other=e.other_vert(v)
            if other in remaining:remaining.remove(other);stack.append(other)
mesh.calc_tangents(uvmap=mesh.uv_layers.active.name)
bs=next(n for n in mesh.materials[0].node_tree.nodes if n.type=='BSDF_PRINCIPLED')
def height(x,y):
    hit=tree.ray_cast(Vector((x,y,1)),Vector((0,0,-1)))[0]
    return hit.z if hit else None
report={"object":obj.name,"collection":"Obstacle_1x1_Stone","dimensions":list(obj.dimensions),"vertices":len(mesh.vertices),"triangles":len(mesh.polygons),"components":components,"non_manifold_edges":sum(not e.is_manifold for e in bm.edges),"degenerate_faces":sum(f.calc_area()<1e-10 for f in bm.faces),"volume":bm.calc_volume(signed=True),"nonadjacent_triangle_intersections":len(intersections),"finite_normals":all(all(math.isfinite(c) for c in n.vector) and abs(n.vector.length-1)<1e-5 for n in mesh.corner_normals),"finite_tangents":all(all(math.isfinite(c) for c in loop.tangent) for loop in mesh.loops),"uv_layers":len(mesh.uv_layers),"metallic":bs.inputs["Metallic"].default_value,"roughness":bs.inputs["Roughness"].default_value,"color_is_material_not_vertex_attribute":len(mesh.color_attributes)==0,"cavity_center_z":height(0,0),"rim_z":height(.40,0),"local_min":[min(v.co[i] for v in mesh.vertices) for i in range(3)],"local_max":[max(v.co[i] for v in mesh.vertices) for i in range(3)],"scale":list(obj.scale),"rotation":list(obj.rotation_euler),"preview_location":list(obj.location),"textures":[{"file":"Stone_1x1_Surface.png","size":[512,512],"usage":"linear grayscale base color multiplier"},{"file":"Stone_1x1_Normal.png","size":[512,512],"usage":"tangent +Y normal"}],"unity_export_executed":False,"unity_urp_runtime_verified":False}
assert components==1 and report["non_manifold_edges"]==0 and report["degenerate_faces"]==0 and not intersections
assert report["volume"]>0 and report["finite_normals"] and report["finite_tangents"]
assert all(abs(a-b)<1e-6 for a,b in zip(report["dimensions"],(1,1,.3)))
bm.free()
review=bpy.data.scenes["BR_Stone_1x1_Review"];bpy.context.window.scene=review
cam=review.camera;center=obj.location+Vector((0,0,.15))
def render(name,direction,scale=1.55):
    cam.location=center+Vector(direction)
    cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO';cam.data.ortho_scale=scale
    review.render.filepath=os.path.join(OUT,name+".png");bpy.ops.render.render(write_still=True)
render("Top",(0,0,3))
e=math.radians(40);a=math.radians(22)
render("Perspective",(2.6*math.cos(e)*math.sin(a),-2.6*math.cos(e)*math.cos(a),2.6*math.sin(e)))
render("Side",(0,-3,0))
render("Detail",(.8,-.9,1.2),1.1)
compare=bpy.data.scenes["BR_Stone_Block_Comparison"];bpy.context.window.scene=compare;bpy.ops.render.render(write_still=True)
family=bpy.data.scenes["AssetPreview"];bpy.context.window.scene=family
for o in family.objects:o.select_set(False)
obj.select_set(True);bpy.context.view_layer.objects.active=obj
with open(os.path.join(OUT,"qa.json"),"w") as file:json.dump(report,file,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=str(_PROJECT_ROOT / 'ArtProduction/AssetPreview/AssetPreview.blend'))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"Obstacle_1x1_Stone.blend"),copy=True)
print(json.dumps(report))
