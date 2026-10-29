import bpy,numpy as np,json,math
from pathlib import Path
from mathutils import Vector
BASE=Path(__file__).resolve().parent;OUT=BASE/'ReferenceRevision/Reference'
paths=json.loads((OUT/'parameters.json').read_text())['paths']
N=1024;x,y=np.meshgrid(np.linspace(-8,8,N),np.linspace(-8,8,N));height=np.zeros(x.shape);shade=np.zeros(x.shape);rim=np.zeros(x.shape)
for k,path in enumerate(paths):
 points=np.asarray(path['points']);points=np.concatenate([points[::3],points[-1:]],axis=0) if len(points)>8 else points
 distance=np.full(x.shape,99.);along=np.zeros(x.shape);acc=0;lens=np.linalg.norm(np.diff(points,axis=0),axis=1);total=lens.sum()
 for a,b,ln in zip(points,points[1:],lens):
  if ln<1e-8:continue
  d=b-a;t=np.clip(((x-a[0])*d[0]+(y-a[1])*d[1])/ln**2,0,1);dd=np.hypot(x-a[0]-d[0]*t,y-a[1]-d[1]*t);mask=dd<distance;along=np.where(mask,(acc+ln*t)/total,along);distance=np.minimum(distance,dd);acc+=ln
 taper=np.sin(np.pi*along)**.6
 width=path['width']*(1.0+.4*np.sin(along*13+k)+.12*np.sin(x*47+y*31))
 if path['category']=='large':width*=1.25
 groove=np.clip(1-distance/np.maximum(width,.001),0,1)**.55*taper
 ridge=np.clip(1-np.abs(distance-width*1.2)/(width*.45+.001),0,1)*taper
 height-=path['depth']*groove;height+=path['depth']*.12*ridge
 shade=np.maximum(shade,groove*(.24 if path['category']=='large' else .15));rim=np.maximum(rim,ridge)
im=bpy.data.images.load(str(BASE/'Textures/Sand_BaseColor_512.png'),check_existing=False);pixels=np.empty(len(im.pixels),np.float32);im.pixels.foreach_get(pixels);rgb=np.repeat(np.repeat(pixels.reshape(512,512,4)[:,:,:3],2,0),2,1)
rgb=rgb*(1-shade[:,:,None])+.012*rim[:,:,None]
dy,dx=np.gradient(height,16/(N-1),16/(N-1));normal=np.stack((-dx,-dy,np.ones_like(dx)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
m=bpy.data.objects['Sand_Alternative_Reference'].data.materials[0]
for name,array,isnormal in [('Color_BrokenSoil',rgb,False),('Normal_BrokenSoil',normal*.5+.5,True)]:
 im=bpy.data.images.new(name,width=N,height=N,alpha=True)
 if isnormal:im.colorspace_settings.name='Non-Color'
 rgba=np.concatenate([np.clip(array,0,1),np.ones((N,N,1))],-1).astype(np.float32);im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(OUT/(name+'.png'));im.file_format='PNG';im.save();im.pack()
 node=next(n for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and (n.image.colorspace_settings.name=='Non-Color')==isnormal);node.image=im
scene=bpy.data.scenes['Review_Sand_Alternative_Reference'];cam=scene.camera;center=Vector((8,10,0));lp=Vector((*next(p for p in paths if p['category']=='large')['points'][0],-.04))
for name,pos,t,scale in [('LargeCrack',lp+Vector((1,-1,2)),lp,2.4),('Top',Vector((0,0,12)),Vector((0,0,0)),9),('Perspective40',Vector((4,-8.5,7.9)),Vector((0,0,0)),9)]:
 cam.location=center+pos;cam.rotation_euler=(center+t-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale;scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True,scene=scene.name)
bpy.context.window.scene=scene;bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('Angular crack relief textures refined; top, perspective, close-up refreshed.')
