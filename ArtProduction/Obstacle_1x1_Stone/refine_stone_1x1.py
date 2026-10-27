import bpy,bmesh,math,os,numpy as np
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
assert len(mesh.vertices)>1500 and len(mesh.polygons)>3000,"Beklenen taş geometri verisi gerekli"
# İnce çatlaklar UV dokusuna işlenir; geniş yontuk, kırıklar ve ana üç çatlak zaten mesh'tedir.
S=512
positions=np.zeros((S,S,3),np.float32);valid=np.zeros((S,S),bool)
uv=mesh.uv_layers.active
for poly in mesh.polygons:
    ids=list(poly.loop_indices)
    coords=np.array([uv.data[i].uv[:] for i in ids],np.float64)*S
    pts=np.array([mesh.vertices[mesh.loops[i].vertex_index].co[:] for i in ids],np.float32)
    minx=max(0,int(np.floor(coords[:,0].min())));maxx=min(S-1,int(np.ceil(coords[:,0].max())))
    miny=max(0,int(np.floor(coords[:,1].min())));maxy=min(S-1,int(np.ceil(coords[:,1].max())))
    if minx>maxx or miny>maxy:continue
    x,y=np.meshgrid(np.arange(minx,maxx+1)+.5,np.arange(miny,maxy+1)+.5)
    a,b,c=coords
    den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
    if abs(den)<1e-10:continue
    wa=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den
    wb=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den
    wc=1-wa-wb;mask=(wa>=0)&(wb>=0)&(wc>=0)
    p=wa[:,:,None]*pts[0]+wb[:,:,None]*pts[1]+wc[:,:,None]*pts[2]
    dest=positions[miny:maxy+1,minx:maxx+1];dest[mask]=p[mask]
    valid[miny:maxy+1,minx:maxx+1]|=mask
bm=bmesh.new();bm.from_mesh(mesh);tree=BVHTree.FromBMesh(bm)
def top(xys):
    result=[]
    for x,y in xys:
        hit=tree.ray_cast(Vector((x,y,1)),Vector((0,0,-1)))[0]
        result.append((x,y,hit.z))
    return result
paths=[
top([(-.18,-.28),(-.23,-.34),(-.22,-.40),(-.28,-.46),(-.27,-.492)])+[(-.27,-.499,.22),(-.29,-.499,.17),(-.275,-.499,.11),(-.31,-.499,.065),(-.315,-.493,.018)],
top([(.22,-.08),(.31,-.12),(.37,-.09),(.44,-.14),(.492,-.16)])+[(.499,-.16,.21),(.499,-.18,.15),(.499,-.17,.08)],
top([(-.11,.30),(-.15,.36),(-.14,.42),(-.18,.488)]),
top([(-.23,-.34),(-.30,-.32),(-.33,-.34)])]
bm.free()
distance=np.full((S,S),100,np.float32)
for path in paths:
    for a,b in zip(path,path[1:]):
        a=np.array(a,np.float32);b=np.array(b,np.float32);ab=b-a
        t=np.clip(np.sum((positions-a)*ab,axis=-1)/np.dot(ab,ab),0,1)
        d=np.linalg.norm(positions-(a+t[:,:,None]*ab),axis=-1)
        distance=np.minimum(distance,d)
cracks=np.exp(-(distance/.0035)**2)*valid
rng=np.random.default_rng(673)
grain=rng.random((S,S),dtype=np.float32);speck=rng.random((S,S))
value=.72+.16*(grain-.5)
value=np.where(speck<.020,value*.48,value);value=np.where(speck>.985,np.minimum(value*1.25,.98),value)
value*=1-.80*cracks
# UV adası çevresinde 3 px taşma mipmap dikişlerini azaltır.
micro=.00036*(grain-.5)-.0010*cracks
for _ in range(3):
    missing=~valid
    for axis,shift in [(0,1),(0,-1),(1,1),(1,-1)]:
        donor=np.roll(valid,shift,axis);take=missing&donor
        value[take]=np.roll(value,shift,axis)[take]
        micro[take]=np.roll(micro,shift,axis)[take]
        valid[take]=True;missing[take]=False
gx=(np.roll(micro,-1,1)-np.roll(micro,1,1))*S*.5
gy=(np.roll(micro,-1,0)-np.roll(micro,1,0))*S*.5
normal=np.stack((-gx,-gy,np.ones_like(gx)),axis=-1)
normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
def save_image(name,colors,path):
    image=bpy.data.images.get(name) or bpy.data.images.new(name,width=S,height=S,alpha=False)
    image.colorspace_settings.name="Non-Color"
    pixels=np.ones((S,S,4),np.float32);pixels[:,:,:3]=colors
    image.pixels.foreach_set(pixels.ravel());image.filepath_raw=os.path.join(OUT,path);image.file_format='PNG'
    image.save();image.pack();return image
surface=save_image("Stone_1x1_Surface",np.repeat(value[:,:,None],3,axis=2),"Stone_1x1_Surface.png")
normalimage=save_image("Stone_1x1_Normal",normal*.5+.5,"Stone_1x1_Normal.png")
m=mesh.materials[0]
bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
bs.inputs['Metallic'].default_value=0;bs.inputs['Roughness'].default_value=.85
for n in m.node_tree.nodes:
    if n.type=='TEX_IMAGE':
        if n.image and n.image.name.startswith("Stone_1x1_Surface"):n.image=surface
        if n.image and n.image.name.startswith("Stone_1x1_Normal"):n.image=normalimage
    if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.75
    if n.type=='MIX_RGB':n.inputs[2].default_value=(.30,.30,.30,1)
# Karşılaştırma objesi son mesh'i gösterir; kaynak plastik blok değişmez.
bpy.data.objects["Obstacle_1x1_Stone_Export_StoneComparison"].data=mesh
print("STONE_SURFACE_REFINED",len(mesh.vertices),len(mesh.polygons))
