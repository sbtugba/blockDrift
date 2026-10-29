import bpy,bmesh,math,json,hashlib,datetime
from pathlib import Path
from mathutils import Vector
from mathutils.geometry import delaunay_2d_cdt
OUT=Path('C:\\Users\\sahba\\BlockRift\\ArtProduction\\Block_4x1\\UnifiedRevision')
def sig(o):
 d={'matrix':[list(r) for r in o.matrix_world],'collections':[c.name for c in o.users_collection]}
 if o.type=='MESH':
  m=o.data;d['mesh']=str(([tuple(v.co) for v in m.vertices],[tuple(p.vertices) for p in m.polygons],[tuple(n.vector) for n in m.corner_normals],[[tuple(x.uv) for x in u.data] for u in m.uv_layers],[m.name for m in m.materials]))
 return hashlib.sha256(str(d).encode()).hexdigest()
protected={o.name:sig(o) for o in bpy.data.objects}
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/('Before_Unified_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend')),copy=True)
src=bpy.data.objects['Block_1x1_Export'].data
verts=[];faces=[];norm=[]
N=96
def add(v,n):
 verts.append(tuple(v));norm.append(tuple(n));return len(verts)-1
def bridge(a,b):
 for i in range(len(a)):
  k=(i+1)%len(a);faces.append((a[i],a[k],b[k],b[i]))
# Translate straight portions out by half a cell; retain the master's corner curvature.
outer=[]
for j in range(17):
 ring=[]
 for i in range(N):
  v=src.vertices[j*192+i*2].co.copy()
  prev=src.vertices[j*192+(i*2-2)%192].co
  nxt=src.vertices[j*192+(i*2+2)%192].co
  tangent=nxt-prev; radial=Vector((tangent.y,-tangent.x,0)).normalized()
  if j==0:n=Vector((radial.x*.3,radial.y*.3,-.954)).normalized()
  elif j==16:n=Vector((0,0,1))
  else:
   lo=src.vertices[(j-1)*192+i*2].co;hi=src.vertices[(j+1)*192+i*2].co
   dz=hi.z-lo.z;dr=(Vector((hi.x-lo.x,hi.y-lo.y,0))).dot(radial)
   n=Vector((radial.x*dz,radial.y*dz,-dr)).normalized()
  sx=(1.5,-1.5) if abs(v.x)<1e-7 and i<N//2 else ((-1.5,1.5) if abs(v.x)<1e-7 else (1.5 if v.x>0 else -1.5,))
  sy=(0.,)
  for dx in sx:
   for dy in sy:ring.append(add((v.x+dx,v.y+dy,v.z),n))
 outer.append(ring)
for a,b in zip(outer,outer[1:]):bridge(a,b)
faces.append(tuple(reversed(outer[0])))
holes=[];centers=[(-1.5, 0.0), (-0.5, 0.0), (0.5, 0.0), (1.5, 0.0)]
# Keep the actual master diamond rings, including its rounded lip and .04-deep floor.
for cx,cy in centers:
 rings=[]
 for j in range(17,25):
  ring=[]
  for i in range(N):
   v=src.vertices[j*192+i*2].co
   ring.append(add((v.x+cx,v.y+cy,v.z),(0,0,1)))
  rings.append(ring)
 for a,b in zip(rings,rings[1:]):bridge(a,b)
 faces.append(tuple(rings[-1]));holes.append(rings[0])
loops=[outer[-1]]+holes;ids=[i for r in loops for i in r];xy=[Vector(verts[i][:2]) for i in ids]
edges=[];start=0
for r in loops:
 edges.extend((start+i,start+(i+1)%len(r)) for i in range(len(r)));start+=len(r)
dv,de,df,ov,oe,of=delaunay_2d_cdt(xy,edges,[],0,1e-8)
mapping=[ids[a[0]] if a else add((v.x,v.y,.3),(0,0,1)) for v,a in zip(dv,ov)]
def inside(x,y,r):
 hit=False
 for i in range(len(r)):
  a=verts[r[i]];b=verts[r[(i+1)%len(r)]]
  if (a[1]>y)!=(b[1]>y) and x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0]:hit=not hit
 return hit
for f in df:
 x=sum(dv[i].x for i in f)/len(f);y=sum(dv[i].y for i in f)/len(f)
 if not any(inside(x,y,r) for r in holes):faces.append(tuple(mapping[i] for i in f))
m=bpy.data.meshes.new('Block_4x1_Unified_Mesh');m.from_pydata(verts,[],faces);m.update()
bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free()
s=bpy.data.scenes.new('Review_Block_4x1_Unified');bpy.context.window.scene=s
c=bpy.data.collections.new('Block_4x1_Unified_Revision');s.collection.children.link(c)
o=bpy.data.objects.new('Block_4x1_Unified_Export',m);c.objects.link(o)
m.materials.append(bpy.data.objects['Block_1x1_Export'].data.materials[0])
bpy.context.view_layer.objects.active=o;o.select_set(True)
for p in m.polygons:p.use_smooth=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
# Exterior analytic normals, cavity area-weighted normals, flat deck and floor normals.
acc=[Vector((0,0,0)) for _ in m.vertices]
for p in m.polygons:
 for vi in p.vertices:acc[vi]+=p.normal*p.area
custom=[]
outer_count=sum(map(len,outer))
for p in m.polygons:
 zs=[m.vertices[i].co.z for i in p.vertices];flat=max(zs)-min(zs)<1e-7
 for li in p.loop_indices:
  vi=m.loops[li].vertex_index
  n=Vector((0,0,-1 if max(zs)<1e-6 else 1)) if flat else (Vector(norm[vi]) if vi<outer_count else acc[vi].normalized())
  custom.append(n)
m.normals_split_custom_set(custom)
o['grid_pitch']=1.;o['diamond_centers']=json.dumps(centers);o['cavity_depth']=.04;o['revision']='Unified upper deck; original retained'
studio=bpy.data.collections.new('Studio_Block_4x1_Unified');s.collection.children.link(studio)
world=bpy.data.worlds.new('World_Block_4x1_Unified');world.use_nodes=True
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs['Color'].default_value=(.32,.32,.32,1)
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs['Strength'].default_value=.5;s.world=world
for name,loc,power,size in [('Key',(-3,-4,6),500,4),('Rim',(3,2,4),400,3),('Fill',(2,-3,2),100,3)]:
 d=bpy.data.lights.new('Unified_'+name,'AREA');d.energy=power;d.shape='DISK';d.size=size
 l=bpy.data.objects.new(d.name,d);studio.objects.link(l);l.location=loc;l.rotation_euler=(Vector((0,0,.15))-l.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Unified_Camera');cam=bpy.data.objects.new(d.name,d);studio.objects.link(cam);s.camera=cam
s.render.engine='CYCLES';s.cycles.samples=32;s.cycles.use_denoising=True
s.render.resolution_x=1200;s.render.resolution_y=800;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG'
s.render.film_transparent=False
old=bpy.data.objects['Block_4x1_Export'].copy();old.name='Block_4x1_Original_Review';studio.objects.link(old);old.location=(0,0,0);old.hide_render=True
def render(name,loc,scale=4.7):
 cam.location=loc;cam.rotation_euler=(Vector((0,0,.15))-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=scale
 s.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
bm=bmesh.new();bm.from_mesh(m);remaining=set(bm.verts);components=0
while remaining:
 components+=1;stack=[remaining.pop()]
 while stack:
  v=stack.pop()
  for e in v.link_edges:
   q=e.other_vert(v)
   if q in remaining:remaining.remove(q);stack.append(q)
report={'dimensions':list(o.dimensions),'z_range':[min(v.co.z for v in m.vertices),max(v.co.z for v in m.vertices)],'scale':list(o.scale),'rotation':list(o.rotation_euler),'origin':list(o.location),'diamond_centers':centers,'cavity_depth':.04,'vertices':len(m.vertices),'triangles':sum(len(p.vertices)-2 for p in m.polygons),'components':components,'non_manifold_edges':sum(not e.is_manifold for e in bm.edges),'degenerate_faces':sum(f.calc_area()<1e-12 for f in bm.faces),'signed_volume':bm.calc_volume(signed=True),'uv_layers':len(m.uv_layers)}
bm.free()
assert components==1 and report['non_manifold_edges']==0 and report['degenerate_faces']==0,report
assert all(sig(bpy.data.objects[n])==h for n,h in protected.items()),'Protected object changed'
report['protected_objects_unchanged']=True
(OUT/'qa.json').write_text(json.dumps(report,indent=2))
bpy.data.texts.load(str(OUT/"create.py"))
for label,loc in [('Top',(0,0,5)),('Perspective',(3,-4,4.35)),('Side',(0,-5,.15))]:
 render(label,loc)
 # The protected original remains hidden in this review scene.
old.hide_set(True)
cam.location=(3,-4,4.35);cam.rotation_euler=(Vector((0,0,.15))-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Block_4x1_Unified_Review.blend'),copy=True)
# Keep the original file path and all its objects; persist the new review scene alongside them.
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print(json.dumps(report))
