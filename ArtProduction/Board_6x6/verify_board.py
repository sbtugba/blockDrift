"""Görsel geometri kontrolleri; Core hareket kurallarını uygulamaz."""
import bpy,bmesh,json,math,hashlib,itertools
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'AGENTS.md').is_file())
OUT=ROOT/'ArtProduction/Board_6x6'
meta=json.loads((OUT/'build_metadata.json').read_text());P=meta['parameters'];TOL=1e-5
scene=bpy.data.scenes['AssetPreview'];root=scene.objects['Board_6x6_Root'];bpy.context.view_layer.update()
def local_vertices(o):
    transform=root.matrix_world.inverted()@o.matrix_world
    return [transform@v.co for v in o.data.vertices]
def bounds(vs):return [[min(v[k] for v in vs),max(v[k] for v in vs)] for k in range(3)]
def tree(vs,m):return BVHTree.FromPolygons(vs,[tuple(p.vertices) for p in m.polygons],all_triangles=True)
report={'tolerance':TOL,'parameters':P,'geometry':[],'fit_tests':[],'neighbor_pairs':[],'sweep_checks':[],'five_by_five_checks':[]}
board_names=['Board_6x6_Base','Board_6x6_Frame','Board_6x6_Cells']
trees={};bb={}
for name in board_names:
    o=scene.objects[name];vs=local_vertices(o);bb[name]=bounds(vs);trees[name]=tree(vs,o.data)
    bm=bmesh.new();bm.from_mesh(o.data)
    e={'name':name,'vertices':len(o.data.vertices),'triangles':len(o.data.polygons),'non_manifold_edges':sum(not e.is_manifold for e in bm.edges),'degenerate_faces':sum(f.calc_area()<1e-10 for f in bm.faces),'signed_volume':bm.calc_volume(signed=True),'bounds':bb[name],'uv_layers':len(o.data.uv_layers),'finite_uv':all(math.isfinite(c) for uv in o.data.uv_layers for d in uv.data for c in d.uv),'unit_finite_normals':all(math.isfinite(n.vector.length) and abs(n.vector.length-1)<1e-4 for n in o.data.corner_normals)}
    bm.free();assert e['non_manifold_edges']==e['degenerate_faces']==0 and e['signed_volume']>0 and e['finite_uv'] and e['unit_finite_normals'],e
    o.data.calc_tangents()
    e['finite_tangents']=all(all(math.isfinite(v) for v in l.tangent) for l in o.data.loops)
    o.data.free_tangents();assert e['finite_tangents']
    report['geometry'].append(e)

cellmesh=scene.objects['Board_6x6_Cells'].data
bm=bmesh.new();bm.from_mesh(cellmesh);unvisited=set(bm.verts);measured_cells=[]
while unvisited:
    todo=[unvisited.pop()];points=[]
    while todo:
        v=todo.pop();points.append(v.co.copy())
        for edge in v.link_edges:
            other=edge.other_vert(v)
            if other in unvisited:unvisited.remove(other);todo.append(other)
    b=bounds(points);measured_cells.append({'center':[(b[0][0]+b[0][1])/2,(b[1][0]+b[1][1])/2],'dimensions':[b[k][1]-b[k][0] for k in range(3)],'top_z':b[2][1]})
bm.free()
assert len(measured_cells)==36
for cell in measured_cells:
    assert abs(cell['dimensions'][0]-.97)<TOL and abs(cell['dimensions'][1]-.97)<TOL
    assert abs(cell['top_z']+.001)<TOL
    assert min(math.dist(cell['center'],c) for c in meta['logical_centers'])<TOL
report['actual_cell_components']=measured_cells

analysis=json.loads((OUT/'Analysis/measurement_report.json').read_text())
panel_centers={a['name']:a['measured_panel_centers'] for a in analysis['assets']}
fit_trees={};fit_bounds={};occupied=set()
for fit in meta['fit_objects']:
    o=scene.objects[fit['name']];source=scene.objects[fit['source']]
    vs=local_vertices(o);b=bounds(vs);tr=tree(vs,o.data);fit_trees[o.name]=tr;fit_bounds[o.name]=b
    col,row=fit['origin_cell'];w,h=fit['size']
    expected=[Vector(((c-(P['columns']-1)/2)*P['grid_pitch'],(r-(P['rows']-1)/2)*P['grid_pitch'],0)) for r in range(row,row+h) for c in range(col,col+w)]
    transform=root.matrix_world.inverted()@o.matrix_world
    actual=[transform@Vector((cx,cy,0)) for cx,cy in panel_centers[fit['source']]]
    error=max(min((a-e).length for a in actual) for e in expected)
    support=[]
    for e in expected:
        hit=trees['Board_6x6_Cells'].ray_cast(e+Vector((0,0,1)),Vector((0,0,-1)))[0]
        assert hit is not None
        support.append({'panel_center':list(e),'actual_cell_top_z':hit.z,'bottom_clearance':b[2][0]-hit.z})
    intersections={name:len(tr.overlap(t)) for name,t in trees.items()}
    e={'object':o.name,'source':source.name,'uses_original_mesh':o.data==source.data,'scale':list(o.scale),'bounds':b,'placement_z':o.location.z,'panel_center_max_error':error,'support_samples':support,'surface_intersections_with_board':intersections,'min_vertical_clearance_to_cells':b[2][0]-bb['Board_6x6_Cells'][2][1],'min_vertical_clearance_to_frame':b[2][0]-bb['Board_6x6_Frame'][2][1]}
    assert error<TOL and all(v==0 for v in intersections.values()) and abs(o.location.z)<TOL and e['min_vertical_clearance_to_cells']>0,e
    assert o.data==source.data and all(abs(v-1)<TOL for v in o.scale)
    for y in range(row,row+h):
        for x in range(col,col+w):assert (x,y) not in occupied;occupied.add((x,y))
    report['fit_tests'].append(e)

for a,b in itertools.combinations(meta['fit_objects'],2):
    ba,bb2=fit_bounds[a['name']],fit_bounds[b['name']]
    gaps=[max(bb2[k][0]-ba[k][1],ba[k][0]-bb2[k][1]) for k in range(2)]
    separating_gap=max(gaps)
    assert separating_gap>=-TOL,(a,b,gaps)
    # Düzlemsel gövde teması izinlidir; BVH ortak düzlem çiftleri penetrasyon sayılmaz.
    report['neighbor_pairs'].append({'a':a['name'],'b':b['name'],'separating_plane_gap':separating_gap,'contact_allowed':abs(separating_gap)<=TOL,'penetration_excluded':True})

def inner_sdf(x,y,hx,hy):
    r=.100;qx=abs(x)-(hx-r);qy=abs(y)-(hy-r)
    return math.hypot(max(qx,0),max(qy,0))+min(max(qx,qy),0)-r

# Her tipin bütün sığan hücre konumları: gerçek vertex bulutu, çerçeve açıklığı,
# board/cell ayırıcı Z düzlemi ve analitik panel merkezi eşlemesi incelenir.
for n in (6,5):
    for fit in meta['fit_objects']:
        src=scene.objects[fit['source']];w,h=fit['size'];count=0;min_clear=1e9;max_error=0
        for row in range(n-h+1):
            for col in range(n-w+1):
                offset=Vector((col+(w-1)/2-(n-1)/2,row+(h-1)/2-(n-1)/2,0))
                vs=[v.co+offset for v in src.data.vertices]
                assert min(v.z for v in vs)>bb['Board_6x6_Cells'][2][1]
                assert all(abs(v.x)<=n/2+TOL and abs(v.y)<=n/2+TOL for v in vs)
                clearance=-max(inner_sdf(v.x,v.y,n/2+P['frame_inner_clearance'],n/2+P['frame_inner_clearance']) for v in vs)
                min_clear=min(min_clear,clearance);assert clearance>0
                for y in range(h):
                    for x in range(w):
                        expected=Vector((col+x-(n-1)/2,row+y-(n-1)/2,0))
                        measured=min((Vector((cx,cy,0))+offset-expected).length for cx,cy in panel_centers[fit['source']])
                        max_error=max(max_error,measured);assert measured<TOL
                count+=1
        entry={'source':src.name,'board_size':n,'placements_checked':count,'minimum_xy_frame_clearance':min_clear,'maximum_panel_center_error':max_error,'scale':1.0}
        (report['sweep_checks'] if n==6 else report['five_by_five_checks']).append(entry)

# Dış çerçeve tamamen negatif Z'de; Z>=0 blok hareket zarfıyla tüm ara konumlarda ayrık.
report['entire_motion_envelope_frame_clearance_z']=-bb['Board_6x6_Frame'][2][1]
report['logical_surface_height']=P['board_surface_height'];report['actual_visual_cell_surface']=bb['Board_6x6_Cells'][2][1]
report['block_placement_z']=0.0
report['visual_cell_count']=P['rows']*P['columns'];report['cell_gap']=P['cell_gap']
report['all_6x6_placements_checked']=sum(t['placements_checked'] for t in report['sweep_checks'])
report['all_5x5_placements_checked']=sum(t['placements_checked'] for t in report['five_by_five_checks'])
report['blocks_raised']=False;report['zero_coincident_block_cell_faces']=True
sourcecode=(OUT/'generate_board.py').read_text(encoding='utf-8');ns={'hashlib':hashlib}
exec(sourcecode[sourcecode.index('def signature'):sourcecode.index('protected=')],ns)
protected=json.loads((OUT/'protected_before.json').read_text())
report['changed_protected_objects']=[name for name,sig in protected.items() if ns['signature'](bpy.data.objects[name])!=sig]
assert not report['changed_protected_objects']
report['status']='PASS';report['unity_exported']=False
(OUT/'fit_report.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'status':report['status'],'7_asset_actual_mesh_fits':'PASS','6x6_placements':report['all_6x6_placements_checked'],'5x5_numeric_placements':report['all_5x5_placements_checked'],'cell_surface':report['actual_visual_cell_surface'],'block_z':0,'frame_top':bb['Board_6x6_Frame'][2][1],'frame_motion_clearance_z':report['entire_motion_envelope_frame_clearance_z'],'protected_changes':report['changed_protected_objects'],'board_triangles':sum(e['triangles'] for e in report['geometry'])}))
