import bpy,bmesh,math,json,hashlib,datetime,numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
BASE=Path(__file__).resolve().parent;OUT=BASE/'Touchup';OUT.mkdir(exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/('Before_Touchup_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend')),copy=True)
code=(BASE.parent/'Board_6x6/generate_board.py').read_text(encoding='utf-8');ns={'hashlib':hashlib};exec(code[code.index('def signature'):code.index('protected=')],ns)
obj=bpy.data.objects['Sand_BoardTransition_6x6'];old=obj.data
protected={o.name:ns['signature'](o) for o in bpy.data.objects if o.type=='MESH' and o.data!=old}
mesh=old.copy();mesh.name='Sand_OrganicEdge_TaperedCracks'
for o in bpy.data.objects:
 if o.type=='MESH' and o.data==old:o.data=mesh
for v in mesh.vertices:
 j=v.index//144;a=math.atan2(v.co.y,v.co.x)
 if j<3:v.co.z+=([1,.6,.15][j])*(.001+.004*math.sin(3*a-1.2)+.002*math.cos(9*a+.2))
mesh.update()
material=old.materials[0].copy();material.name='MAT_Sand_OrganicCracks';mesh.materials[0]=material
# Başlangıçtaki temiz kum dokusu kullanılır; önceki düz çizgiler yeni dokunun altında kalmaz.
baseimage=bpy.data.images.load(str(BASE/'Textures/Sand_BaseColor_512.png'),check_existing=False)
pixels=np.empty(len(baseimage.pixels),dtype=np.float32);baseimage.pixels.foreach_get(pixels)
rgb=np.repeat(np.repeat(pixels.reshape(512,512,4)[:,:,:3],2,axis=0),2,axis=1)
N=1024;x,y=np.meshgrid(np.linspace(-8,8,N),np.linspace(-8,8,N));height=np.zeros((N,N));dark=np.zeros((N,N));lip=np.zeros((N,N))
paths=json.loads((BASE/'Final/crack_paths.json').read_text())['paths'];rng=np.random.default_rng(1029);newpaths=[]
for pi,path in enumerate(paths):
 points=[]
 for si,(a,b) in enumerate(zip(path,path[1:])):
  a=np.array(a);b=np.array(b);normal=np.array([-(b-a)[1],(b-a)[0]]);normal/=np.linalg.norm(normal)
  for k in range(5):
   t=k/5;point=a*(1-t)+b*t+normal*(math.sin(t*math.pi)*rng.uniform(-.027,.027));points.append(point)
 points.append(np.array(path[-1]));newpaths.append([p.tolist() for p in points])
 distance=np.full((N,N),100.);along=np.zeros((N,N));lengths=np.array([np.linalg.norm(b-a) for a,b in zip(points,points[1:])]);total=lengths.sum();acc=0
 for a,b,length in zip(points,points[1:],lengths):
  d=b-a;t=np.clip(((x-a[0])*d[0]+(y-a[1])*d[1])/(length*length),0,1);dd=np.hypot(x-a[0]-t*d[0],y-a[1]-t*d[1]);mask=dd<distance
  distance=np.minimum(distance,dd);along=np.where(mask,(acc+t*length)/total,along);acc+=length
 taper=np.clip(np.sin(np.pi*along),0,1)**.45
 width=(.009+.004*(pi%3))*(.8+.22*np.sin(along*19+pi))*(.12+.88*taper)
 groove=np.exp(-(distance/np.maximum(width,.001))**2)*taper
 edge=np.exp(-((distance-width*1.8)/(width*.65+.001))**2)*taper
 depth=.003+.0006*(pi%4)
 height-=depth*groove;height+=.00065*edge;dark=np.maximum(dark,groove*(.14+.025*(pi%3)));lip=np.maximum(lip,edge)
rgb*=1-dark[:,:,None];rgb+=.012*lip[:,:,None]
dy,dx=np.gradient(height,16/(N-1),16/(N-1));normal=np.stack((-dx,-dy,np.ones_like(dx)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
for name,array,isnormal in [('Sand_Touchup_BaseColor',rgb,False),('Sand_Touchup_Normal',normal*.5+.5,True)]:
 image=bpy.data.images.new(name,width=N,height=N,alpha=True)
 if isnormal:image.colorspace_settings.name='Non-Color'
 rgba=np.concatenate([np.clip(array,0,1),np.ones((N,N,1))],axis=-1).astype(np.float32);image.pixels.foreach_set(rgba.ravel());image.filepath_raw=str(OUT/(name+'.png'));image.file_format='PNG';image.save();image.pack()
 node=next(n for n in material.node_tree.nodes if n.type=='TEX_IMAGE' and (n.image.colorspace_settings.name=='Non-Color')==isnormal);node.image=image
next(n for n in material.node_tree.nodes if n.type=='NORMAL_MAP').inputs['Strength'].default_value=.65
(OUT/'crack_paths.json').write_text(json.dumps(newpaths,indent=2))
# Yeni geometriye karşı gerçek blok yerleşimleri ve hücre örtülmesi.
root=bpy.data.objects['Board_6x6_Frameless_Root'];center=Vector((8,10,0))
def tree(vs,m):return BVHTree.FromPolygons(vs,[tuple(p.vertices) for p in m.polygons],all_triangles=True)
sand=tree([v.co for v in mesh.vertices],mesh);zmax=max(v.co.z for v in mesh.vertices)
meta=json.loads((BASE.parent/'Board_6x6/Frameless/variant_metadata.json').read_text());count=0
for f in meta['fits']:
 src=bpy.data.objects[f['source']];w,h=f['size']
 for row in range(7-h):
  for col in range(7-w):
   offset=Vector((col+(w-1)/2-2.5,row+(h-1)/2-2.5,0));vs=[v.co+offset for v in src.data.vertices];assert min(v.z for v in vs)>zmax;assert not sand.overlap(tree(vs,src.data));count+=1
cells=bpy.data.objects['Board_6x6_Frameless_Cells'];assert not sand.overlap(tree([v.co for v in cells.data.vertices],cells.data))
top=max(v.co.z for v in cells.data.vertices);occluded=sum(sand.ray_cast(Vector((v.co.x,v.co.y,1)),Vector((0,0,-1)))[0] is not None for v in cells.data.vertices if abs(v.co.z-top)<1e-6);assert occluded==0
bm=bmesh.new();bm.from_mesh(mesh);bad=sum(f.calc_area()<1e-10 for f in bm.faces);bm.free();assert bad==0
assert all(ns['signature'](bpy.data.objects[n])==sig for n,sig in protected.items())
empty=bpy.data.scenes['Review_Sand_6x6_Empty'];filled=bpy.data.scenes['Review_Sand_6x6_Fits'];cam=empty.camera
before=bpy.data.scenes.new('Review_Sand_BeforeTouchup');before.world=empty.world;before.camera=cam;before.render.engine=empty.render.engine
before.collection.children.link(bpy.data.collections['Sand_Board_References_6x6']);before.collection.children.link(bpy.data.collections['Preview_Studio_Sand_6x6'])
prev=bpy.data.objects.new('Sand_BeforeTouchup',old);prev.matrix_world=obj.matrix_world.copy();before.collection.objects.link(prev)
for s in (empty,filled,before):s.view_settings.view_transform='Standard';s.view_settings.exposure=-.8;s.render.image_settings.file_format='PNG';s.render.resolution_x=1200;s.render.resolution_y=1000;s.render.resolution_percentage=100
for s,name,pos,target,scale in [(before,'Previous',(4,-8.5,7.9),(0,0,-.02),9),(empty,'Top',(0,0,12),(0,0,-.02),9),(empty,'Perspective40',(4,-8.5,7.9),(0,0,-.02),9),(filled,'WithBlocks',(4,-8.5,7.9),(0,0,-.02),9),(empty,'EdgeClose',(0,-5,.62),(0,-2.96,-.04),1.8),(empty,'CrackClose',(5.1,-3.8,2.8),(3.6,-1.45,-.08),1.6)]:
 cam.location=center+Vector(pos);cam.rotation_euler=(center+Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale;s.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True,scene=s.name)
report={'vertices':len(mesh.vertices),'triangles':len(mesh.polygons),'tested_placements':count,'block_intersections':0,'cell_intersections':0,'occluded_top_vertices':occluded,'max_sand_z':zmax,'degenerate_faces':bad,'original_mesh_assets_changed':[],'crack_count':len(paths),'exported':False}
(OUT/'qa.json').write_text(json.dumps(report,indent=2));bpy.context.window.scene=filled;bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath);print(json.dumps(report))
