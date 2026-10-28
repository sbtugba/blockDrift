import bpy,bmesh,json,hashlib,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=Path(__file__).resolve().parent
meta=json.loads((OUT/'variant_metadata.json').read_text());s=bpy.data.scenes['AssetPreview'];root=s.objects[meta['root']];TOL=1e-5
def vertices(o):
    matrix=root.matrix_world.inverted()@o.matrix_world
    return [matrix@v.co for v in o.data.vertices]
def bounds(vs):return [[min(v[k] for v in vs),max(v[k] for v in vs)] for k in range(3)]
def tree(vs,m):return BVHTree.FromPolygons(vs,[tuple(p.vertices) for p in m.polygons],all_triangles=True)
board={name:s.objects[name] for name in (meta['support'],meta['cells'])}
bs={name:bounds(vertices(o)) for name,o in board.items()}
trees={name:tree(vertices(o),o.data) for name,o in board.items()}
assert s.objects[meta['cells']].data==s.objects['Board_6x6_Cells'].data
report={'tolerance':TOL,'fits':[],'all_positions':[],'cells_exactly_shared':True,'cell_count':36,'grid_pitch':1.0,'cell_size':.97,'cell_height':.06,'logical_surface':0,'block_placement':0,'actual_cell_top':bs[meta['cells']][2][1],'support_bounds':bs[meta['support']],'frame_in_variant':False}
centers=json.loads((OUT.parent/'TechnicalAudit/audit.json').read_text())
lookup={e['source']:e['actual_panel_centers_board_space'] for e in centers['fits']}
oldmeta=json.loads((OUT.parent/'build_metadata.json').read_text());oldpositions={f['source']:Vector(f['local_location']) for f in oldmeta['fit_objects']}
for fit in meta['fits']:
    o=s.objects[fit['name']];src=s.objects[fit['source']];w,h=fit['size'];col,row=fit['origin_cell']
    local_panels=[Vector(v)-oldpositions[src.name] for v in lookup[src.name]]
    expected=[Vector((col+x-2.5,row+y-2.5,0)) for y in range(h) for x in range(w)]
    actual=[v+o.location for v in local_panels]
    error=max(min((a-e).length for a in actual) for e in expected)
    vs=vertices(o);bb=bounds(vs);tr=tree(vs,o.data)
    overlaps={name:len(tr.overlap(t)) for name,t in trees.items()}
    gaps=[]
    for point in expected:
        hit=trees[meta['cells']].ray_cast(point+Vector((0,0,1)),Vector((0,0,-1)))[0]
        assert hit is not None;gaps.append(bb[2][0]-hit.z)
    entry={'source':src.name,'fit':o.name,'shared_source_mesh':o.data==src.data,'scale':list(o.scale),'world_bottom_z':min((o.matrix_world@v.co).z for v in o.data.vertices),'panel_error':error,'bvh_intersections':overlaps,'cell_center_gap':[min(gaps),max(gaps)],'support_clearance':bb[2][0]-bs[meta['support']][2][1]}
    assert error<TOL and all(v==0 for v in overlaps.values()) and entry['shared_source_mesh'] and abs(entry['world_bottom_z'])<TOL and min(gaps)>0
    report['fits'].append(entry)
    count=0;edge=0
    for row in range(6-h+1):
        for col in range(6-w+1):
            offset=Vector((col+(w-1)/2-2.5,row+(h-1)/2-2.5,0));vs=[v.co+offset for v in src.data.vertices]
            tr=tree(vs,src.data)
            assert all(len(tr.overlap(t))==0 for t in trees.values())
            assert min(v.z for v in vs)>max(b[2][1] for b in bs.values())
            assert all(abs(v.x)<=3+TOL and abs(v.y)<=3+TOL for v in vs)
            if row==0 or col==0 or row+h==6 or col+w==6:edge+=1
            count+=1
    report['all_positions'].append({'source':src.name,'placements':count,'edge_placements':edge,'collisions':0})
code=(OUT.parent/'generate_board.py').read_text(encoding='utf-8');ns={'hashlib':hashlib}
exec(code[code.index('def signature'):code.index('protected=')],ns)
protected=json.loads((OUT/'protected_before.json').read_text())
report['changed_original_objects']=[name for name,sig in protected.items() if ns['signature'](bpy.data.objects[name])!=sig]
assert not report['changed_original_objects']
report['all_placements']=sum(e['placements'] for e in report['all_positions']);report['edge_placements']=sum(e['edge_placements'] for e in report['all_positions'])
report['status']='PASS';report['environment_objects_created']=0;report['unity_exported']=False
report['old_normal_issue_unchanged']='Existing cell/asset lower bevel custom normals were intentionally retained; this revision only replaces the external support.'
(OUT/'fit_report.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'status':report['status'],'actual_fits':len(report['fits']),'all_placements':report['all_placements'],'edge_placements':report['edge_placements'],'cells_identical':True,'cell_top':report['actual_cell_top'],'support_z':report['support_bounds'][2],'original_changes':report['changed_original_objects']}))
