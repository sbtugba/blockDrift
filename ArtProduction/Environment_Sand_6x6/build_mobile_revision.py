import bpy,bmesh,math,json,hashlib,datetime
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
BASE=Path(__file__).resolve().parent;OUT=BASE/'MobilePortrait';OUT.mkdir(exist_ok=True)
assert not bpy.data.scenes.get('Review_MobileSand_6'),'Mobile revision exists; do not overwrite.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/('Before_Mobile_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend')),copy=True)
code=(BASE.parent/'Board_6x6/generate_board.py').read_text(encoding='utf-8');ns={'hashlib':hashlib};exec(code[code.index('def signature'):code.index('protected=')],ns)
protected={o.name:ns['signature'](o) for o in bpy.data.objects};(OUT/'protected_before.json').write_text(json.dumps(protected,indent=2))
source=bpy.data.objects['Board_6x6_Frameless_Cells'].data
gen=(BASE/'generate_sand.py').read_text(encoding='utf-8');scope={'math':math,'Vector':Vector};exec(gen[gen.index('def outline'):gen.index('verts,faces,shape=build(P)')],scope)
params=json.loads((BASE/'parameters.json').read_text());report={'variants':[],'unity_changed':False,'exported':False}
center=Vector((8,10,0))
def tree(vs,m):return BVHTree.FromPolygons(vs,[tuple(p.vertices) for p in m.polygons],all_triangles=True)
studio=bpy.data.collections.new('MobileSand_Studio_OutsideCamera')
for src in bpy.data.collections['Preview_Studio_Sand_6x6'].objects:
 if src.type!='LIGHT':continue
 o=src.copy();o.data=src.data.copy();o.location=center+(src.location-center)*8;o.data.energy*=64
 if hasattr(o.data,'size'):o.data.size*=8
 o.name='Mobile_'+src.name;studio.objects.link(o)
world=bpy.data.scenes['Review_Sand_6x6_Empty'].world.copy();world.name='MobileSand_World'
for n in (6,5):
 folder=OUT/str(n);scene=bpy.data.scenes.new('Review_MobileSand_'+str(n));collection=bpy.data.collections.new('MobileSand_'+str(n));scene.collection.children.link(collection);scene.collection.children.link(studio);scene.world=world
 # Hücreler şekil/UV/custom normal olarak kopyalanır; 5×5 merkezleri aynı pitch ile kurulur.
 ids=set()
 for row in range(n):
  for col in range(n):ids.update(range((row*6+col)*226,(row*6+col+1)*226))
 oldids=sorted(ids);mapping={idx:i for i,idx in enumerate(oldids)};shift=(6-n)/2
 verts=[tuple(source.vertices[i].co+Vector((shift,shift,0))) for i in oldids]
 polys=[p for p in source.polygons if all(i in ids for i in p.vertices)]
 cellmesh=bpy.data.meshes.new('Mobile_Cells_'+str(n));cellmesh.from_pydata(verts,[],[tuple(mapping[i] for i in p.vertices) for p in polys]);cellmesh.update()
 for mat in source.materials:cellmesh.materials.append(mat)
 uv=cellmesh.uv_layers.new(name=source.uv_layers.active.name);normals=[]
 for p,old in zip(cellmesh.polygons,polys):
  p.use_smooth=old.use_smooth;p.material_index=old.material_index
  for li,oli in zip(p.loop_indices,old.loop_indices):uv.data[li].uv=source.uv_layers.active.data[oli].uv;normals.append(tuple(source.corner_normals[oli].vector))
 cellmesh.normals_split_custom_set(normals)
 cells=bpy.data.objects.new('Mobile_Cells_'+str(n),cellmesh);collection.objects.link(cells);cells.location=center
 # Dış kum halkası ve aralıklarda görünen taban aynı bağlı mesh içindedir.
 p={**params,'rows':n,'columns':n};sv,sf,shape=scope['build'](p);N=shape['ring_vertices'];ct=tree([v.co for v in cellmesh.vertices],cellmesh)
 levels=[-.044,-.048,-.045,-.034,-.024,-.018,-.016,-.017,-.018,-.018,-.019,-.020]
 vv=[]
 for idx,v in enumerate(sv):
  j=idx//N;v=Vector(v);a=math.atan2(v.y,v.x);v.z=levels[j]+.0015*math.sin(3*a+.8)
  if j==0:
   normal=Vector((math.copysign(1,v.x),0,0)) if abs(v.x)>abs(v.y) else Vector((0,math.copysign(1,v.y),0))
   hit=ct.ray_cast(v+normal,-normal)[0]
   if hit is not None:v=hit-normal*.0002
  if j>=10:v.y*=1+((j-9)/2)*.8
  vv.append(tuple(v))
 floor=[]
 for i in range(N):floor.append(len(vv));v=vv[i];vv.append((v[0],v[1],-.062))
 for i in range(N):
  k=(i+1)%N;sf.extend([(i,floor[k],floor[i]),(i,k,floor[k])])
 middle=len(vv);vv.append((0,0,-.062))
 for i in range(N):sf.append((middle,floor[i],floor[(i+1)%N]))
 mesh=bpy.data.meshes.new('Mobile_Sand_'+str(n));mesh.from_pydata(vv,[],sf);mesh.update()
 uv=mesh.uv_layers.new(name='UV_Sand_World')
 for face in mesh.polygons:
  face.use_smooth=True
  for li in face.loop_indices:
   v=mesh.vertices[mesh.loops[li].vertex_index].co;uv.data[li].uv=((v.x+8)/16,(v.y+8)/16)
 mat=bpy.data.materials.new('MAT_Mobile_Sand_'+str(n));mat.use_nodes=True;bs=next(t for t in mat.node_tree.nodes if t.type=='BSDF_PRINCIPLED');bs.inputs['Roughness'].default_value=.83
 for filename,normalmap in [('Sand_Color.png',False),('Sand_Normal.png',True)]:
  tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(folder/filename),check_existing=False);tex.extension='EXTEND'
  if normalmap:
   tex.image.colorspace_settings.name='Non-Color';nm=mat.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.55;mat.node_tree.links.new(tex.outputs['Color'],nm.inputs['Color']);mat.node_tree.links.new(nm.outputs['Normal'],bs.inputs['Normal'])
  else:mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
  tex.image.pack()
 mesh.materials.append(mat);sand=bpy.data.objects.new('Mobile_Sand_'+str(n),mesh);collection.objects.link(sand);sand.location=center
 st=tree([v.co for v in mesh.vertices],mesh)
 gap_hits=[]
 for row in range(n):
  for col in range(n-1):
   x=col+.5-(n-1)/2;y=row-(n-1)/2;hit=st.ray_cast(Vector((x,y,1)),Vector((0,0,-1)))[0];assert hit is not None;gap_hits.append(hit.z)
 assert all(abs(z+.062)<1e-5 for z in gap_hits)
 maxz=max(v.co.z for v in mesh.vertices);assert maxz<-.001
 cam=bpy.data.objects.new('Mobile_Camera_'+str(n),bpy.data.cameras.new('Mobile_Camera_'+str(n)));collection.objects.link(cam);scene.camera=cam
 scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=1080;scene.render.resolution_y=1920;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.view_settings.view_transform='Standard';scene.view_settings.exposure=-.8
 for view in ('Top','UnityPerspective'):
  if view=='Top':cam.data.type='ORTHO';cam.data.ortho_scale=n/6*12.14;cam.location=center+Vector((0,0,20))
  else:
   cam.data.type='PERSP';cam.data.sensor_fit='VERTICAL';cam.data.sensor_height=32;cam.data.lens=32/(2*math.tan(math.radians(11)));cam.location=center+Vector((0,-6.49963,30.544376))*n/6
  cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();bpy.context.window.scene=scene;bpy.context.view_layer.update()
  dg=bpy.context.evaluated_depsgraph_get();frames=cam.data.view_frame(scene=scene);miss=0
  for ix in range(11):
   for iy in range(19):
    local=frames[2].lerp(frames[1],ix/10).lerp(frames[3].lerp(frames[0],ix/10),iy/18)
    origin=cam.location if view!='Top' else cam.matrix_world@Vector((local.x,local.y,0))
    direction=cam.matrix_world.to_3x3()@(local.normalized() if view!='Top' else Vector((0,0,-1)))
    hit=scene.ray_cast(dg,origin,direction)[0];miss+=not hit
  assert miss==0,(n,view,miss)
  scene.render.filepath=str(folder/(view+'_1080x1920.png'));bpy.ops.render.render(write_still=True,scene=scene.name)
 # Yakın çekimler.
 cam.data.type='ORTHO'
 for view,target,delta,scale in [('GapClose',Vector((0,-(n-1)/2,-.04)),Vector((0,-1,1.3)),1.1),('LowEdge',Vector((-.5,-n/2,-.04)),Vector((0,-2,.30)),1.8)]:
  cam.location=center+target+delta;cam.rotation_euler=(center+target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale;scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.filepath=str(folder/(view+'.png'));bpy.ops.render.render(write_still=True,scene=scene.name)
 report['variants'].append({'size':n,'sand_vertices':len(mesh.vertices),'sand_triangles':len(mesh.polygons),'cell_count':n*n,'gap_rays':len(gap_hits),'gap_surface_z':[-.062,-.062],'max_sand_z':maxz,'ray_coverage_misses':0,'separate_support_panel_present':False,'grid_pitch':1.0,'unity_camera_reference':{'pitch':78,'vertical_fov':22}})
assert all(ns['signature'](bpy.data.objects[name])==sig for name,sig in protected.items())
report['original_objects_changed']=[];(OUT/'qa.json').write_text(json.dumps(report,indent=2));bpy.context.window.scene=bpy.data.scenes['Review_MobileSand_6'];bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath);print(json.dumps(report))
