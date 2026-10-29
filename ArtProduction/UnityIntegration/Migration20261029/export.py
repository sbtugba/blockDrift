"""Approved source meshes remain unchanged; FBX copies have local bottom/board-center pivots."""
import bpy,json,hashlib,shutil
from pathlib import Path
from mathutils import Matrix
ROOT=Path(r'C:/Users/sahba/BlockRift');OUT=ROOT/'Assets/_BlockRift/Art/Models/Gameplay'
REPORT=ROOT/'ArtProduction/UnityIntegration/Migration20261029'
def signature(o):
 m=o.data
 return hashlib.sha256(str(([tuple(v.co) for v in m.vertices],[tuple(p.vertices) for p in m.polygons],[tuple(n.vector) for n in m.corner_normals],[[tuple(x.uv) for x in u.data] for u in m.uv_layers],[list(r) for r in o.matrix_world])).encode()).hexdigest()
sources={f'Block_{shape}_Unified':f'Block_{shape}_Unified_Export' for shape in ['2x1','3x1','4x1','2x2']}
for size in [5,6]:
 sources[f'Board_{size}x{size}_Cells']=f'Mobile_Cells_{size}'
 sources[f'Sand_BoardTransition_{size}x{size}']=f'Mobile_Sand_{size}'
before={n:signature(bpy.data.objects[n]) for n in sources.values()}
scene=bpy.context.window.scene;temp=bpy.data.scenes.new('Migration_Transient_Export');report=[]
try:
 bpy.context.window.scene=temp;temp.unit_settings.scale_length=1
 for asset,name in sources.items():
  src=bpy.data.objects[name];cp=src.copy();cp.parent=None;cp.matrix_world=Matrix.Identity(4);temp.collection.objects.link(cp)
  cp.data=src.data.copy()
  fixed=[];repairs=0
  for poly in cp.data.polygons:
   for li in poly.loop_indices:
    normal=cp.data.corner_normals[li].vector.copy()
    if normal.dot(poly.normal)<-1e-4:normal=poly.normal.copy();repairs+=1
    fixed.append(normal)
  cp.data.normals_split_custom_set(fixed)
  cp.hide_viewport=False;cp.hide_render=False;cp.hide_set(False);cp.select_set(True);bpy.context.view_layer.objects.active=cp
  path=OUT/(asset+'.fbx')
  bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_space_transform=True,use_mesh_modifiers=False,mesh_smooth_type='OFF',use_tspace=False,bake_anim=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
  report.append({'asset':asset,'source':name,'triangles':sum(len(p.vertices)-2 for p in src.data.polygons),'opposed_normals':sum(src.data.corner_normals[li].vector.dot(p.normal)<-1e-4 for p in src.data.polygons for li in p.loop_indices),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
  report[-1]['export_copy_normal_repairs']=repairs
  mesh=cp.data;bpy.data.objects.remove(cp,do_unlink=True);bpy.data.meshes.remove(mesh)
finally:
 bpy.context.window.scene=scene;bpy.data.scenes.remove(temp)
for size in [5,6]:
 dest=ROOT/'Assets/_BlockRift/Art/Textures/Sand'/str(size);dest.mkdir(parents=True,exist_ok=True)
 for kind in ['Color','Normal']:shutil.copy2(ROOT/f'ArtProduction/Environment_Sand_6x6/MobilePortrait/{size}/Sand_{kind}.png',dest/f'Sand_{kind}.png')
assert before=={n:signature(bpy.data.objects[n]) for n in sources.values()}
(REPORT/'export.json').write_text(json.dumps({'sources_unchanged':True,'assets':report},indent=2))
print(json.dumps(report))
