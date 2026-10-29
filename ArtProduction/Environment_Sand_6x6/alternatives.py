import bpy,bmesh,math,json,hashlib,datetime,numpy as np,random
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
BASE=Path(__file__).resolve().parent;OUT=BASE/'Alternatives';OUT.mkdir(exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/('Before_Alternatives_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend')),copy=True)
code=(BASE.parent/'Board_6x6/generate_board.py').read_text(encoding='utf-8');ns={'hashlib':hashlib};exec(code[code.index('def signature'):code.index('protected=')],ns)
protected={o.name:ns['signature'](o) for o in bpy.data.objects if o.type=='MESH'}
source=bpy.data.objects['Sand_BoardTransition_6x6'];cells=bpy.data.objects['Board_6x6_Frameless_Cells'];center=Vector((8,10,0))
def tree(vs,m):return BVHTree.FromPolygons(vs,[tuple(p.vertices) for p in m.polygons],all_triangles=True)
ctree=tree([v.co for v in cells.data.vertices],cells.data)
clean=bpy.data.images.load(str(BASE/'Textures/Sand_BaseColor_512.png'),check_existing=False);p=np.empty(len(clean.pixels),dtype=np.float32);clean.pixels.foreach_get(p);base=np.repeat(np.repeat(p.reshape(512,512,4)[:,:,:3],2,0),2,1)
x,y=np.meshgrid(np.linspace(-8,8,1024),np.linspace(-8,8,1024));results=[]
for label,seed,density in [('A',1729,.7),('B',8813,1.0),('C',42119,1.4)]:
 rng=random.Random(seed);folder=OUT/label;folder.mkdir(exist_ok=True)
 collection=bpy.data.collections.new('Sand_Alternative_'+label);scene=bpy.data.scenes.new('Review_Sand_Alternative_'+label);scene.collection.children.link(collection)
 scene.collection.children.link(bpy.data.collections['Sand_Board_References_6x6']);scene.collection.children.link(bpy.data.collections['Preview_Studio_Sand_6x6'])
 scene.world=bpy.data.scenes['Review_Sand_6x6_Empty'].world
 mesh=source.data.copy();mesh.name='Sand_'+label+'_Embedded';obj=bpy.data.objects.new('Sand_Alternative_'+label,mesh);obj.matrix_world=source.matrix_world.copy();collection.objects.link(obj)
 contacts=[]
 for v in mesh.vertices:
  j=v.index//144
  if j>3:continue
  # Üç bağımsız birikinti; aralarında alçak ve sakin kenarlar kalır.
  strength=max(math.exp(-((v.co.x+1.5)/.43)**2)*math.exp(-((v.co.y+2.985)/.3)**2),math.exp(-((v.co.x-.5)/.38)**2)*math.exp(-((v.co.y+2.985)/.3)**2),math.exp(-((v.co.y-1.5)/.44)**2)*math.exp(-((v.co.x-2.985)/.3)**2))
  target=-.043+.025*strength
  if j==0:
   v.co.z=target
   if strength>.28:
    n=Vector((math.copysign(1,v.co.x),0,0)) if abs(v.co.x)>abs(v.co.y) else Vector((0,math.copysign(1,v.co.y),0))
    start=Vector((v.co.x,v.co.y,target))+n
    hit=ctree.ray_cast(start,-n)[0]
    if hit is not None:v.co=hit-n*.00035;contacts.append(tuple(v.co))
  elif j<3:v.co.z+=strength*.010*(1-j/3)
 mesh.update()
 height=np.zeros(x.shape);dark=np.zeros(x.shape);edge=np.zeros(x.shape);paths=[]
 specs=[('large',rng.randint(1,2),(.7,1.8),(.012,.028),(.003,.007)),('medium',max(1,int(rng.randint(2,4)*density)),(.25,.8),(.005,.014),(.001,.004)),('micro',max(2,int(rng.randint(3,6)*density)),(.05,.25),(.0025,.005),(.0003,.0012))]
 for category,count,length_range,width_range,depth_range in specs:
  for k in range(count):
   # Konum, açı ve her patikanın rastlantısı bağımsız; şablon döndürülmez.
   for trial in range(100):
    start=np.array([rng.uniform(-4.6,4.6),rng.uniform(-4.6,4.6)])
    if max(abs(start))>3.35:break
   length=rng.uniform(*length_range);width=rng.uniform(*width_range);depth=rng.uniform(*depth_range);angle=rng.uniform(0,math.tau);steps=max(4,int(length/.07));pts=[start]
   for s in range(steps):
    angle+=rng.uniform(-.65,.65);candidate=pts[-1]+np.array([math.cos(angle),math.sin(angle)])*length/steps
    if max(abs(candidate))<3.17:break
    pts.append(candidate)
   if len(pts)<3:continue
   branches=[pts]
   if category=='large' and rng.random()<.65:
    at=rng.randint(1,len(pts)-2);a=rng.uniform(0,math.tau);branch=[pts[at]]
    for s in range(rng.randint(2,5)):a+=rng.uniform(-.7,.7);branch.append(branch[-1]+np.array([math.cos(a),math.sin(a)])*rng.uniform(.035,.11))
    if all(max(abs(p))>3.17 for p in branch):branches.append(branch)
   for bi,points in enumerate(branches):
    dist=np.full(x.shape,100.);u=np.zeros(x.shape);lens=[np.linalg.norm(b-a) for a,b in zip(points,points[1:])];total=sum(lens);acc=0
    for a,b,ln in zip(points,points[1:],lens):
     d=b-a;t=np.clip(((x-a[0])*d[0]+(y-a[1])*d[1])/(ln*ln),0,1);dd=np.hypot(x-a[0]-t*d[0],y-a[1]-t*d[1]);mask=dd<dist;dist=np.minimum(dist,dd);u=np.where(mask,(acc+t*ln)/total,u);acc+=ln
    phase=rng.uniform(0,6);taper=np.sin(np.pi*u)**rng.uniform(.3,1.4);w=width*(.5+.5*taper)*(.8+.3*np.sin(u*rng.uniform(12,30)+phase))
    interruption=1-.65*np.exp(-((u-rng.uniform(.25,.75))/.035)**2) if rng.random()<.4 else 1
    groove=np.exp(-(dist/(w+.0001))**2)*taper*interruption;lip=np.exp(-((dist-w*1.8)/(w*.6+.001))**2)*taper
    height-=depth*groove;height+=depth*.12*lip;dark=np.maximum(dark,groove*rng.uniform(.1,.23));edge=np.maximum(edge,lip)
    paths.append({'category':category,'points':[p.tolist() for p in points],'length':total,'width':width,'depth':depth,'branch':bi})
 rgb=base*(1-dark[:,:,None])+.012*edge[:,:,None];dy,dx=np.gradient(height,16/1023,16/1023);norm=np.stack((-dx,-dy,np.ones_like(dx)),axis=-1);norm/=np.linalg.norm(norm,axis=-1,keepdims=True)
 mat=source.data.materials[0].copy();mat.name='MAT_Sand_Alternative_'+label;mesh.materials[0]=mat
 for name,array,isnormal in [('Color',rgb,False),('Normal',norm*.5+.5,True)]:
  im=bpy.data.images.new('Sand_'+label+'_'+name,width=1024,height=1024,alpha=True)
  if isnormal:im.colorspace_settings.name='Non-Color'
  rgba=np.concatenate([np.clip(array,0,1),np.ones((1024,1024,1))],-1).astype(np.float32);im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(folder/(name+'.png'));im.file_format='PNG';im.save();im.pack()
  node=next(n for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and (n.image.colorspace_settings.name=='Non-Color')==isnormal);node.image=im
 cam=bpy.data.objects.new('Sand_'+label+'_Camera',bpy.data.cameras.new('Sand_'+label+'_Camera'));collection.objects.link(cam);scene.camera=cam;cam.data.type='ORTHO'
 scene.render.engine='BLENDER_EEVEE';scene.view_settings.view_transform='Standard';scene.view_settings.exposure=-.8;scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
 large=next(p for p in paths if p['category']=='large');micro=next(p for p in paths if p['category']=='micro');lp=Vector((*large['points'][len(large['points'])//2],-.08));mp=Vector((*micro['points'][len(micro['points'])//2],-.08))
 views=[('Top',Vector((0,0,12)),Vector((0,0,0)),9),('Perspective40',Vector((4,-8.5,7.9)),Vector((0,0,0)),9),('LargeCrack',lp+Vector((1,-1,2)),lp,1.8),('MicroCrack',mp+Vector((.3,-.3,1)),mp,.55)]
 for idx,t in enumerate([Vector((-1.5,-2.985,-.025)),Vector((.5,-2.985,-.025)),Vector((2.985,1.5,-.025))]):
  delta=Vector((0,-1.5,.28)) if idx<2 else Vector((1.5,0,.28));views.append(('Edge'+str(idx+1),t+delta,t,1.1))
 for name,pos,target,scale in views:
  cam.location=center+pos;cam.rotation_euler=(center+target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale;scene.render.filepath=str(folder/(name+'.png'));bpy.ops.render.render(write_still=True,scene=scene.name)
 sand=tree=None
 contacts_report={'contact_vertices':len(contacts),'max_sand_z':max(v.co.z for v in mesh.vertices),'intentional_lower_side_overlap':.00035,'upper_surface_z':-.001,'block_volume_min_z':0}
 assert contacts and contacts_report['max_sand_z']<-.001
 results.append({'alternative':label,'seed':seed,'paths':paths,'embedding':contacts_report,'vertices':len(mesh.vertices),'triangles':len(mesh.polygons)})
 (folder/'parameters.json').write_text(json.dumps(results[-1],indent=2))
assert all(ns['signature'](bpy.data.objects[n])==sig for n,sig in protected.items())
(OUT/'results.json').write_text(json.dumps(results,indent=2));bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('Three alternatives saved; original mesh assets unchanged. Controlled lower-side overlap only; all sand below top and block plane.')
