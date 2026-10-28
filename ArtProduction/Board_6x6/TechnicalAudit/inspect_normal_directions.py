"""Mevcut meshlerin yalnızca normal yönlerini inceler; düzeltme uygulamaz."""
import bpy,bmesh,json,hashlib
from pathlib import Path
OUT=Path(__file__).resolve().parent
r=json.loads((OUT/'audit.json').read_text())
for e in r['technical']:
    m=bpy.data.objects[e['name']].data;groups={}
    for p in m.polygons:
        for li in p.loop_indices:
            n=m.corner_normals[li].vector;dot=p.normal.dot(n)
            if dot<-.0001:
                z=round(m.vertices[m.loops[li].vertex_index].co.z,6)
                g=groups.setdefault(str(z),{'corner_count':0,'min_dot':1,'max_dot':-1,'face_z_min':1e9,'face_z_max':-1e9})
                g['corner_count']+=1;g['min_dot']=min(g['min_dot'],dot);g['max_dot']=max(g['max_dot'],dot)
                g['face_z_min']=min(g['face_z_min'],min(m.vertices[i].co.z for i in p.vertices))
                g['face_z_max']=max(g['face_z_max'],max(m.vertices[i].co.z for i in p.vertices))
    bm=bmesh.new();bm.from_mesh(m)
    e['winding_inconsistent_edges']=sum(ed.is_manifold and not ed.is_contiguous for ed in bm.edges)
    bm.free();e['opposed_normal_regions']=groups
r['normal_direction_review']='ISSUES_FOUND_NO_CHANGES_APPLIED'
r['unity_normal_readiness']='REVIEW_REQUIRED'
h=hashlib.sha256()
with Path(bpy.data.filepath).open('rb') as f:
    for chunk in iter(lambda:f.read(1048576),b''):h.update(chunk)
r['blend_hash_after_direction_review']=h.hexdigest()
assert r['blend_hash_before']==r['blend_hash_after_direction_review']
(OUT/'audit.json').write_text(json.dumps(r,indent=2),encoding='utf-8')
print('Normal direction review complete; source .blend unchanged; no changes applied.')
