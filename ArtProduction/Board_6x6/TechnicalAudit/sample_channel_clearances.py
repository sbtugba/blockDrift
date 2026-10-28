import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=Path(__file__).resolve().parent;r=json.loads((OUT/'audit.json').read_text())
meta=json.loads((OUT.parent/'build_metadata.json').read_text())
s=bpy.data.scenes['AssetPreview'];root=s.objects['Board_6x6_Root']
def tree(o):
    transform=root.matrix_world.inverted()@o.matrix_world
    return BVHTree.FromPolygons([transform@v.co for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons],all_triangles=True)
board={name:tree(s.objects[name]) for name in ('Board_6x6_Base','Board_6x6_Frame','Board_6x6_Cells')}
for fit in meta['fit_objects']:
    w,h=fit['size'];col,row=fit['origin_cell'];points=[]
    for y in range(h):
        for x in range(w-1):points.append((col+x+.5-2.5,row+y-2.5))
    for y in range(h-1):
        for x in range(w):points.append((col+x-2.5,row+y+.5-2.5))
    if w>1 and h>1:points.append((col+(w-1)/2-2.5,row+(h-1)/2-2.5))
    block=tree(s.objects[fit['name']]);samples=[]
    for x,y in points:
        bottom=block.ray_cast(Vector((x,y,-1)),Vector((0,0,1)))[0];assert bottom is not None
        hits=[]
        for name,t in board.items():
            v=t.ray_cast(Vector((x,y,1)),Vector((0,0,-1)))[0]
            if v is not None:hits.append((v.z,name))
        z,name=max(hits)
        samples.append({'xy':[x,y],'block_bottom':bottom.z,'board_surface':name,'surface_z':z,'vertical_gap':bottom.z-z})
    entry=next(e for e in r['fits'] if e['source']==fit['source']);entry['intercell_channel_samples']=samples
(OUT/'audit.json').write_text(json.dumps(r,indent=2),encoding='utf-8')
print(json.dumps([{ 'source':e['source'],'channel_clearances':[c['vertical_gap'] for c in e['intercell_channel_samples']]} for e in r['fits']]))
