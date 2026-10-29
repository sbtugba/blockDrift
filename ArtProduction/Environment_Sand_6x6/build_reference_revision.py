"""Sığ çevresel kum yuvası ve uzayan kırık ağları; kaynak assetleri korur."""
from pathlib import Path
BASE=Path(__file__).resolve().parent
source=(BASE/'alternatives.py').read_text(encoding='utf-8')
source=source.replace("OUT=BASE/'Alternatives'","OUT=BASE/'ReferenceRevision'")
source=source.replace("[('A',1729,.7),('B',8813,1.0),('C',42119,1.4)]","[('Reference',57281,1.2)]")
source=source.replace("(.7,1.8),(.012,.028),(.003,.007)","(1.3,3.0),(.014,.036),(.004,.009)")
source=source.replace("rng.randint(1,2)","rng.randint(3,4)")
source=source.replace("(.25,.8),(.005,.014)","(.3,1.1),(.005,.017)")
source=source.replace("rng.uniform(-4.6,4.6)","rng.uniform(-4.9,4.9)")
source=source.replace("max(abs(start))>3.35","3.3<max(abs(start))<4.65")
source=source.replace("if category=='large' and rng.random()<.65:","if category=='large' and rng.random()<.85:")
source=source.replace("rng.uniform(.035,.11)","rng.uniform(.08,.24)")
start=source.index(' contacts=[]')
end=source.index(' mesh.update()',start)
replacement=''' contacts=[]
 heights=[-.044,-.048,-.045,-.034,-.024,-.018,-.016,-.017,-.018,-.018,-.019,-.020]
 for v in mesh.vertices:
  j=v.index//144;a=math.atan2(v.co.y,v.co.x)
  wave=.0018*math.sin(3*a+.8)+.0011*math.cos(7*a-1)
  v.co.z=heights[j]+wave
  if j==0:
   n=Vector((math.copysign(1,v.co.x),0,0)) if abs(v.co.x)>abs(v.co.y) else Vector((0,math.copysign(1,v.co.y),0))
   hit=ctree.ray_cast(Vector((v.co.x,v.co.y,v.co.z))+n,-n)[0]
   if hit is not None:
    v.co=hit-n*.0002;contacts.append(tuple(v.co))
'''
source=source[:start]+replacement+source[end:]
source=source.replace("'intentional_lower_side_overlap':.00035","'intentional_lower_side_overlap':.0002")
source=source.replace("(BASE/'Textures/Sand_BaseColor_512.png')","(BASE/'Textures/Sand_BaseColor_512.png')")
exec(compile(source,str(BASE/'alternatives.py'),'exec'),{'__file__':str(BASE/'alternatives.py')})
