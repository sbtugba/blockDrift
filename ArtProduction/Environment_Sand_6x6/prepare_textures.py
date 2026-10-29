"""PNG paletinden dekor içermeyen, URP Lit'e uygun iki küçük kum haritası hazırlar."""
import json,hashlib,math
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'AGENTS.md').is_file())
OUT=Path(__file__).resolve().parent;(OUT/'Textures').mkdir(parents=True,exist_ok=True)
params={'rows':6,'columns':6,'grid_pitch':1.0,'visual_cell_size':.97,'cell_corner_radius':.145,'arc_segments':12,'straight_segments':24,'ring_offsets':[.0015,.016,.05,.12,.24,.40,.65,.95,1.35,1.9,2.8,4.6],'ring_heights':[-.054,-.043,-.047,-.063,-.081,-.096,-.105,-.109,-.106,-.105,-.107,-.110],'texture_half_extent':8.0,'texture_size':512,'seed':1028}
if not (OUT/'parameters.json').exists():(OUT/'parameters.json').write_text(json.dumps(params,indent=2))
P=json.loads((OUT/'parameters.json').read_text())
def filehash(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for chunk in iter(lambda:f.read(1048576),b''):h.update(chunk)
    return h.hexdigest()
manifest={}
for folder in ('Assets','Packages','ProjectSettings'):
    for f in sorted((ROOT/folder).rglob('*')):
        if f.is_file():manifest[f.relative_to(ROOT).as_posix()]=filehash(f)
assert not (OUT/'unity_before.json').exists(),'Keep the original protection baseline.'
(OUT/'unity_before.json').write_text(json.dumps(manifest,indent=2))
png=ROOT/'Assets/_BlockRift/Art/Textures/Backgrounds/blockDriftBackground 1.png'
a=np.array(Image.open(png).convert('RGB'));h,w=a.shape[:2]
sample=a[int(h*.25):int(h*.72),int(w*.27):int(w*.73)]
palette=np.median(sample.reshape(-1,3),axis=0)/255
N=P['texture_size'];extent=P['texture_half_extent']
x,y=np.meshgrid(np.linspace(-extent,extent,N),np.linspace(extent,-extent,N))
hx=(P['columns']-1)/2*P['grid_pitch']+P['visual_cell_size']/2
hy=(P['rows']-1)/2*P['grid_pitch']+P['visual_cell_size']/2
r=P['cell_corner_radius'];qx=np.abs(x)-(hx-r);qy=np.abs(y)-(hy-r)
d=np.sqrt(np.maximum(qx,0)**2+np.maximum(qy,0)**2)+np.minimum(np.maximum(qx,qy),0)-r
rng=np.random.default_rng(P['seed'])
organic=.014*np.sin(.67*x+1.13*y+.5*np.sin(.8*y))+.011*np.sin(1.34*x-.61*y)+.006*np.cos(2.8*y+.7*x)
contact=np.exp(-((d-.008)/.085)**2)
grain=rng.normal(0,.0022,(N,N))
tone=1+organic+grain-.055*contact+.009*np.exp(-((d-.13)/.24)**2)
rgb=palette[None,None,:]*tone[:,:,None]
rgb[:,:,1]-=.004*contact;rgb[:,:,2]-=.009*contact
Image.fromarray(np.uint8(np.clip(rgb,0,1)*255)).save(OUT/'Textures/Sand_BaseColor_512.png')
# Küçük normal haritası yalnızca mikro yüzey içindir; büyük şekiller mesh üzerindedir.
micro=.00025*(np.sin(36*x+8*np.sin(5*y))*.45+np.sin(31*y-6*x)*.3+rng.normal(0,.16,(N,N)))
dy,dx=np.gradient(micro,2*extent/(N-1),2*extent/(N-1))
norm=np.stack((-dx,dy,np.ones_like(dx)),axis=-1);norm/=np.linalg.norm(norm,axis=-1,keepdims=True)
Image.fromarray(np.uint8(np.clip(norm*.5+.5,0,1)*255)).save(OUT/'Textures/Sand_Normal_512.png')
(OUT/'palette.json').write_text(json.dumps({'source_png':png.relative_to(ROOT).as_posix(),'source_size':[w,h],'sample_region_normalized':[.27,.25,.73,.72],'median_srgb':[int(round(v*255)) for v in palette],'median_hex':'#'+''.join(f'{int(round(v*255)):02X}' for v in palette),'unity_files_protected':len(manifest)},indent=2))
print('Palette',palette,'Unity files protected',len(manifest))
