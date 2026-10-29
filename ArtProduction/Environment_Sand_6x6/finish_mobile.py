import bpy,bmesh,math,json,hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from bpy_extras.object_utils import world_to_camera_view
BASE=Path(__file__).resolve().parent;OUT=BASE/'MobilePortrait';r=json.loads((OUT/'qa.json').read_text());center=Vector((8,10,0))
for n in (6,5):
 scene=bpy.data.scenes['Review_MobileSand_'+str(n)];cam=scene.camera;cells=bpy.data.objects['Mobile_Cells_'+str(n)];sand=bpy.data.objects['Mobile_Sand_'+str(n)];m=sand.data
 for tex in m.materials[0].node_tree.nodes:
  if tex.type=='TEX_IMAGE':
   data=Path(bpy.path.abspath(tex.image.filepath)).read_bytes();assert hashlib.sha256(tex.image.packed_file.data).digest()==hashlib.sha256(data).digest(),'Packed map stale'
 cam.data.type='PERSP';cam.data.sensor_fit='VERTICAL';cam.data.sensor_height=32;cam.data.lens=32/(2*math.tan(math.radians(11)));cam.location=center+Vector((0,-6.49963,30.544376))*n/6;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
 scene.render.resolution_x=1080;scene.render.resolution_y=1920;bpy.context.window.scene=scene;bpy.context.view_layer.update()
 projected=[world_to_camera_view(scene,cam,cells.matrix_world@v.co) for v in cells.data.vertices];bounds=[[min(v[k] for v in projected),max(v[k] for v in projected)] for k in range(2)]
 tr=BVHTree.FromPolygons([v.co for v in m.vertices],[tuple(p.vertices) for p in m.polygons],all_triangles=True)
 points=[]
 for row in range(n):
  for col in range(n-1):points.append((col+.5-(n-1)/2,row-(n-1)/2))
 for row in range(n-1):
  for col in range(n):points.append((col-(n-1)/2,row+.5-(n-1)/2))
 for row in range(n-1):
  for col in range(n-1):points.append((col+.5-(n-1)/2,row+.5-(n-1)/2))
 hits=[tr.ray_cast(Vector((x,y,1)),Vector((0,0,-1)))[0] for x,y in points];assert all(v and abs(v.z+.062)<1e-5 for v in hits)
 bm=bmesh.new();bm.from_mesh(m);quality={'unexpected_nonmanifold':sum(not e.is_manifold and not e.is_boundary for e in bm.edges),'boundary_edges':sum(e.is_boundary for e in bm.edges),'degenerate_faces':sum(f.calc_area()<1e-10 for f in bm.faces)};bm.free();assert quality['unexpected_nonmanifold']==quality['degenerate_faces']==0
 e=next(e for e in r['variants'] if e['size']==n);e['all_gap_rays']=len(points);e['projected_board_bounds']=bounds;e['top_free_pixels']=(1-bounds[1][1])*1920;e['bottom_free_pixels']=bounds[1][0]*1920;e['mesh_quality']=quality
 e['uv_finite']=all(math.isfinite(v) for d in m.uv_layers.active.data for v in d.uv);e['normal_unit_max_error']=max(abs(v.vector.length-1) for v in m.corner_normals)
 assert 0<bounds[0][0]<bounds[0][1]<1 and 0<bounds[1][0]<bounds[1][1]<1
code=(BASE.parent/'Board_6x6/generate_board.py').read_text(encoding='utf-8');ns={'hashlib':hashlib};exec(code[code.index('def signature'):code.index('protected=')],ns)
protected=json.loads((OUT/'protected_before.json').read_text());changed=[n for n,sig in protected.items() if ns['signature'](bpy.data.objects[n])!=sig];assert not changed
r['post_render_original_changes']=changed;(OUT/'qa.json').write_text(json.dumps(r,indent=2))
bpy.context.window.scene=bpy.data.scenes['Review_MobileSand_6'];bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print(json.dumps(r))
