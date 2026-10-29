"""Dört çevre bölgesinde bağımsız kırık ağları; UI üst/alt boşlukları saklı tutulur."""
from pathlib import Path
import numpy as np,json,math,random
from PIL import Image
BASE=Path(__file__).resolve().parent;OUT=BASE/'MobilePortrait';OUT.mkdir(exist_ok=True)
for n in (6,5):
 rng=random.Random(28411+n);folder=OUT/str(n);folder.mkdir(exist_ok=True)
 N=1024;half=8.0;xx,yy=np.meshgrid(np.linspace(-half,half,N),np.linspace(half,-half,N))
 base=np.asarray(Image.open(BASE/'Textures/Sand_BaseColor_512.png').convert('RGB').resize((N,N)),dtype=float)/255
 height=np.zeros((N,N));dark=np.zeros((N,N));rim=np.zeros((N,N));paths=[]
 edge=n/2+.28;ui_limit=n/2+1.35
 for zone in ('top','right','bottom','left'):
  for category,count,lr,wr,dr in [('long',1,(2.,4.),(.016,.036),(.003,.007)),('medium',rng.randint(1,2),(.5,1.5),(.006,.018),(.001,.004)),('micro',rng.randint(1,3),(.08,.24),(.002,.005),(.0004,.001))]:
   for k in range(count):
    u=rng.uniform(-n*.30,n*.30);v=edge+rng.uniform(.015,.16)
    start=np.array([u,v]) if zone=='top' else np.array([v,u]) if zone=='right' else np.array([u,-v]) if zone=='bottom' else np.array([-v,u])
    angle=(rng.choice([0,math.pi]) if zone in ('top','bottom') else rng.choice([math.pi/2,-math.pi/2]))+rng.uniform(-.20,.20)
    length=rng.uniform(*lr);steps=max(3,int(length/.16));points=[start]
    for j in range(steps):
     angle+=rng.uniform(-.38,.38);q=points[-1]+np.array([math.cos(angle),math.sin(angle)])*length/steps
     axis=1 if zone in ('top','bottom') else 0;sign=1 if zone in ('top','right') else -1
     q[axis]=sign*min(ui_limit-.12,max(edge-.025,abs(q[axis])))
     if abs(q[1])>ui_limit or abs(q[0])>n/2+2:break
     points.append(q)
    lines=[points]
    if category=='long':
     at=rng.randint(1,len(points)-2);a=angle+rng.choice([-1,1])*rng.uniform(.6,1.4);branch=[points[at]]
     for j in range(rng.randint(2,5)):
      a+=rng.uniform(-.4,.4);q=branch[-1]+np.array([math.cos(a),math.sin(a)])*rng.uniform(.08,.20)
      if max(abs(q))<n/2+.13 or abs(q[1])>ui_limit:break
      branch.append(q)
     if len(branch)>1:lines.append(branch)
    width=rng.uniform(*wr);depth=rng.uniform(*dr)
    for bi,line in enumerate(lines):
     dist=np.full(xx.shape,100.);along=np.zeros(xx.shape);lens=np.linalg.norm(np.diff(line,axis=0),axis=1);total=sum(lens);acc=0
     for a,b,ln in zip(line,line[1:],lens):
      if ln<1e-8:continue
      d=b-a;t=np.clip(((xx-a[0])*d[0]+(yy-a[1])*d[1])/ln**2,0,1);dd=np.hypot(xx-a[0]-t*d[0],yy-a[1]-t*d[1]);mask=dd<dist;dist=np.minimum(dist,dd);along=np.where(mask,(acc+ln*t)/total,along);acc+=ln
     taper=np.clip(np.sin(np.pi*np.clip(along,0,1)),0,1)**rng.uniform(.4,1.0);w=width*(.65+.35*taper)*(.9+.2*np.sin(along*19+k));g=np.clip(1-dist/(w+.0001),0,1)**.6*taper
     lip=np.clip(1-abs(dist-w*1.35)/(w*.6+.001),0,1)*taper
     protected=(np.maximum(abs(xx),abs(yy))>n/2+.09)&(abs(yy)<ui_limit)
     g*=protected;lip*=protected;height-=depth*g;height+=depth*.1*lip;dark=np.maximum(dark,g*(.20 if category=='long' else .14));rim=np.maximum(rim,lip)
     paths.append({'zone':zone,'category':category,'branch':bi,'length':total,'width':width,'depth':depth,'points':[q.tolist() for q in line]})
 assert np.isfinite(height).all() and np.isfinite(dark).all() and np.isfinite(rim).all()
 rgb=np.clip(base*(1-dark[:,:,None])+.01*rim[:,:,None],0,1)
 dy,dx=np.gradient(height,16/1023,16/1023);normal=np.stack((-dx,dy,np.ones_like(dx)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
 Image.fromarray(np.uint8(rgb*255)).save(folder/'Sand_Color.png');Image.fromarray(np.uint8((normal*.5+.5)*255)).save(folder/'Sand_Normal.png')
 (folder/'cracks.json').write_text(json.dumps({'rows':n,'columns':n,'seed':28411+n,'grid_pitch':1.0,'ui_clear_world_y':ui_limit,'paths':paths},indent=2))
print('5x5/6x6 mobile crack maps prepared.')
