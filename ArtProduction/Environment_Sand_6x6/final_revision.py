import bpy,math,json,hashlib,datetime,numpy as np
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent/'Final';OUT.mkdir(exist_ok=True)
BASE=OUT.parent
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/('Before_Final_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend')),copy=True)
code=(BASE.parent/'Board_6x6/generate_board.py').read_text(encoding='utf-8');ns={'hashlib':hashlib};exec(code[code.index('def signature'):code.index('protected=')],ns)
obj=bpy.data.objects['Sand_BoardTransition_6x6'];protected={o.name:ns['signature'](o) for o in bpy.data.objects if o.data!=obj.data}
old=obj.data;mesh=old.copy();mesh.name='Sand_Final_Embedded_Cracked'
for o in bpy.data.objects:
 if o.type=='MESH' and o.data==old:o.data=mesh
N=144
for v in mesh.vertices:
 j=v.index//N;a=math.atan2(v.co.y,v.co.x)
 if j==0:
  v.co.z+=.014+.004*math.sin(5*a+.7)
 elif j==1:v.co.z+=.010+.004*math.sin(5*a+.7)
 elif j==2:v.co.z+=.005
mesh.update()
# Seyrek, dallanan çatlaklar; tamamı nominal 6x6 alanın dışında.
paths=[[(3.48,-1.9),(3.65,-1.68),(3.61,-1.46),(3.86,-1.23),(4.02,-1.12)],[(3.61,-1.46),(3.40,-1.25),(3.34,-1.04)], [(-3.48,.9),(-3.72,.72),(-3.84,.48),(-4.12,.35)], [(-3.72,.72),(-3.68,1.02),(-3.87,1.21)], [(-1.9,-3.48),(-1.65,-3.61),(-1.46,-3.88),(-1.16,-4.04)], [(-1.46,-3.88),(-1.62,-4.09),(-1.59,-4.24)],[(1.25,3.45),(1.49,3.62),(1.57,3.88),(1.88,4.02)],[(1.49,3.62),(1.76,3.58),(1.95,3.72)]]
size=1024;x,y=np.meshgrid(np.linspace(-8,8,size),np.linspace(-8,8,size));dist=np.full(x.shape,100.)
for path in paths:
 for a,b in zip(path,path[1:]):
  a=np.array(a);b=np.array(b);d=b-a;t=np.clip(((x-a[0])*d[0]+(y-a[1])*d[1])/np.dot(d,d),0,1)
  dist=np.minimum(dist,np.sqrt((x-a[0]-t*d[0])**2+(y-a[1]-t*d[1])**2))
material=obj.data.materials[0].copy();material.name='MAT_Sand_Final_Peach_URP';mesh.materials[0]=material
color=next(n for n in material.node_tree.nodes if n.type=='TEX_IMAGE' and n.image.colorspace_settings.name!='Non-Color')
original=color.image;pixels=np.empty(len(original.pixels),dtype=np.float32);original.pixels.foreach_get(pixels)

rgb=np.repeat(np.repeat(pixels.reshape(original.size[1],original.size[0],4)[:,:,:3],2,axis=0),2,axis=1)
groove=np.exp(-(dist/.016)**2);lip=np.exp(-((dist-.030)/.013)**2)
rgb*=1-.24*groove[:,:,None];rgb+=.025*lip[:,:,None]
height=-.006*np.exp(-(dist/.020)**2);dy,dx=np.gradient(height,16/(size-1),16/(size-1))
normal=np.stack((-dx,-dy,np.ones_like(dx)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
for name,array,noncolor in [('Sand_Final_BaseColor',rgb,False),('Sand_Final_Normal',normal*.5+.5,True)]:
 im=bpy.data.images.new(name,width=size,height=size,alpha=True)
 if noncolor:im.colorspace_settings.name='Non-Color'
 rgba=np.concatenate([array,np.ones((size,size,1))],axis=-1).astype(np.float32);im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(OUT/(name+'.png'));im.file_format='PNG';im.save();im.pack()
 if noncolor:
  node=next(n for n in material.node_tree.nodes if n.type=='TEX_IMAGE' and n.image.colorspace_settings.name=='Non-Color');node.image=im
 else:color.image=im
next(n for n in material.node_tree.nodes if n.type=='NORMAL_MAP').inputs['Strength'].default_value=.55
# Şekilli tahta desteği: maskeyi dış sınır polylines olarak sakla; dikdörtgen varsayımı çatlak verisine gömülmez.
(OUT/'crack_paths.json').write_text(json.dumps({'paths':paths,'coordinate_space':'board-local','future_shaped_board_policy':'replace perimeter contour and clip crack paths outside playable polygon'},indent=2))
root=bpy.data.objects['Board_6x6_Frameless_Root'];center=root.matrix_world.translation
empty=bpy.data.scenes['Review_Sand_6x6_Empty'];filled=bpy.data.scenes['Review_Sand_6x6_Fits'];cam=empty.camera
# Önceki kum aynı ışık ve kamera ile ayrı karşılaştırma sahnesinde korunur.
before=bpy.data.scenes.new('Review_Sand_Previous_FinalComparison');before.world=empty.world;before.camera=cam;before.render.engine=empty.render.engine
before.collection.children.link(bpy.data.collections['Sand_Board_References_6x6']);before.collection.children.link(bpy.data.collections['Preview_Studio_Sand_6x6'])
prev=bpy.data.objects.new('Sand_Previous_Comparison',old);prev.matrix_world=obj.matrix_world.copy();before.collection.objects.link(prev)
for scene in [before,empty,filled]:
 scene.view_settings.view_transform='Standard';scene.view_settings.exposure=-.8;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
views=[(before,'Previous',(4,-8.5,7.9),(0,0,-.02),9), (empty,'Top',(0,0,12),(0,0,-.02),9), (empty,'Perspective40',(4,-8.5,7.9),(0,0,-.02),9), (filled,'WithBlocks',(4,-8.5,7.9),(0,0,-.02),9), (empty,'EdgeClose',(0,-5,.62),(0,-2.96,-.045),1.8),(empty,'CrackClose',(5.1,-3.8,2.8),(3.6,-1.45,-.08),1.6)]
for scene,name,pos,target,scale in views:
 cam.location=center+Vector(pos);cam.rotation_euler=(center+Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
 scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True,scene=scene.name)
changed=[n for n,sig in protected.items() if ns['signature'](bpy.data.objects[n])!=sig and bpy.data.objects[n].type not in ('CAMERA',)]
assert not changed,changed
report={'vertices':len(mesh.vertices),'triangles':len(mesh.polygons),'max_sand_z':max(v.co.z for v in mesh.vertices),'protected_assets_changed':changed,'crack_paths':len(paths),'textures':[1024,1024],'exported':False,'shaped_board_support':'contour-based extension planned; not implemented or validated'}
(OUT/'revision.json').write_text(json.dumps(report,indent=2))
bpy.context.window.scene=filled;bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print(json.dumps(report))

