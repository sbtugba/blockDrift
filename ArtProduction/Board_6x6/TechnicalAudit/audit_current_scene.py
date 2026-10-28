"""Salt okunur tahta/asset denetimi. Bpy verisi yazmaz, sahneyi kaydetmez veya export etmez."""
import bpy,bmesh,json,math,hashlib,itertools
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

OUT=Path(__file__).resolve().parent;OUT.mkdir(parents=True,exist_ok=True)
BOARD=OUT.parent
meta=json.loads((BOARD/'build_metadata.json').read_text())
P=meta['parameters'];TOL=1e-5
scene=bpy.data.scenes['AssetPreview'];root=scene.objects['Board_6x6_Root']
ui_before=(bpy.context.scene.name,bpy.context.view_layer.objects.active.name if bpy.context.view_layer.objects.active else None,sorted(o.name for o in bpy.context.selected_objects))
file_path=Path(bpy.data.filepath)
def filehash(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda:f.read(1048576),b''):h.update(chunk)
    return h.hexdigest()
file_before=filehash(file_path)
code=(BOARD/'generate_board.py').read_text(encoding='utf-8');ns={'hashlib':hashlib}
exec(code[code.index('def signature'):code.index('protected=')],ns)
before={o.name:ns['signature'](o) for o in bpy.data.objects}

def bounds(vs):return [[min(v[k] for v in vs),max(v[k] for v in vs)] for k in range(3)]
def board_vertices(o):
    matrix=root.matrix_world.inverted()@o.matrix_world
    return [matrix@v.co for v in o.data.vertices]
def mesh_tree(vs,m):return BVHTree.FromPolygons(vs,[tuple(p.vertices) for p in m.polygons],all_triangles=True)
def components(mesh,face_filter=None):
    groups=[]
    if face_filter is None:
        adjacency={v.index:set() for v in mesh.vertices}
        for edge in mesh.edges:
            a,b=edge.vertices;adjacency[a].add(b);adjacency[b].add(a)
    else:
        adjacency={}
        for p in mesh.polygons:
            if not face_filter(p):continue
            for a,b in zip(p.vertices,list(p.vertices[1:])+[p.vertices[0]]):
                adjacency.setdefault(a,set()).add(b);adjacency.setdefault(b,set()).add(a)
    unseen=set(adjacency)
    while unseen:
        todo=[unseen.pop()];ids=[]
        while todo:
            a=todo.pop();ids.append(a)
            for b in adjacency[a]:
                if b in unseen:unseen.remove(b);todo.append(b)
        groups.append(ids)
    return groups

board_names=['Board_6x6_Base','Board_6x6_Frame','Board_6x6_Cells']
board_meshes={name:scene.objects[name].data for name in board_names}
board_vs={name:board_vertices(scene.objects[name]) for name in board_names}
board_bounds={name:bounds(vs) for name,vs in board_vs.items()}
board_trees={name:mesh_tree(board_vs[name],m) for name,m in board_meshes.items()}
cell_mesh=board_meshes['Board_6x6_Cells']
cell_components=components(cell_mesh)
cells=[]
for ids in cell_components:
    b=bounds([board_vs['Board_6x6_Cells'][i] for i in ids])
    cells.append({'center':[(b[0][0]+b[0][1])/2,(b[1][0]+b[1][1])/2],'size':[b[k][1]-b[k][0] for k in range(3)],'top_z':b[2][1]})
assert len(cells)==36
measured_pitch={axis:sorted(set(round(c['center'][k],7) for c in cells)) for k,axis in enumerate('xy')}
pitch_errors=[abs((v[i+1]-v[i])-1) for v in measured_pitch.values() for i in range(len(v)-1)]
grid_error=max(min(math.dist(c['center'],p) for c in cells) for p in meta['logical_centers'])
assert max(pitch_errors)<TOL and grid_error<TOL

def panel_centers(o):
    m=o.data
    if o.name.startswith('Block_'):
        groups=components(m,lambda p:all(abs(m.vertices[i].co.z-.260)<TOL for i in p.vertices))
        result=[]
        for ids in groups:
            if len(ids)<6:continue
            b=bounds([m.vertices[i].co for i in ids]);result.append(((b[0][0]+b[0][1])/2,(b[1][0]+b[1][1])/2))
        return result,'actual_diamond_floor_components'
    if o.name.startswith('Special_'):
        from mathutils.kdtree import KDTree
        ids={i for p in m.polygons if p.material_index==1 for i in p.vertices};vs=[m.vertices[i].co for i in ids]
        kd=KDTree(len(vs))
        for i,v in enumerate(vs):kd.insert(v,i)
        kd.balance();a=math.pi*2/5
        error=max(kd.find(Vector((v.x*math.cos(a)-v.y*math.sin(a),v.x*math.sin(a)+v.y*math.cos(a),v.z)))[2] for v in vs)
        assert error<TOL
        return [(0,0)],'verified_fivefold_star_center'
    b=bounds([v.co for v in m.vertices])
    return [((b[0][0]+b[0][1])/2,(b[1][0]+b[1][1])/2)],'actual_stone_footprint_center'

def technical(o):
    m=o.data;bm=bmesh.new();bm.from_mesh(m)
    uv=[]
    for layer in m.uv_layers:
        values=[tuple(d.uv) for d in layer.data];zero=0
        for p in m.polygons:
            if len(p.loop_indices)!=3:continue
            a,b,c=[Vector(layer.data[i].uv) for i in p.loop_indices]
            if abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))<1e-12:zero+=1
        uv.append({'name':layer.name,'range':bounds([Vector((u,v,0)) for u,v in values])[:2],'finite':all(math.isfinite(c) for v in values for c in v),'zero_area_triangles':zero})
    verts=[v.co for v in m.vertices];bb=bounds(verts)
    opposed=0
    for p in m.polygons:
        for li in p.loop_indices:
            if p.normal.dot(m.corner_normals[li].vector)<-1e-4:opposed+=1
    result={'name':o.name,'vertices':len(m.vertices),'triangles':len(m.polygons),'local_bounds':bb,'local_origin':[0,0,0],'world_origin':list(o.matrix_world.translation),'parent':o.parent.name if o.parent else None,'scale':list(o.scale),'rotation':list(o.rotation_euler),'non_manifold_edges':sum(not e.is_manifold for e in bm.edges),'degenerate_faces':sum(f.calc_area()<1e-10 for f in bm.faces),'signed_volume':bm.calc_volume(signed=True),'uv_layers':uv,'active_uv':m.uv_layers.active.name if m.uv_layers.active else None,'custom_normals':m.has_custom_normals,'max_normal_length_error':max(abs(n.vector.length-1) for n in m.corner_normals),'non_finite_normals':sum(not all(math.isfinite(v) for v in n.vector) for n in m.corner_normals),'opposed_corner_normals':opposed,'modifiers':[mod.type for mod in o.modifiers]}
    bm.free();return result

report={'mode':'READ_ONLY','tolerance':TOL,'file':str(file_path),'cells':cells,'measured_center_axes':measured_pitch,'max_grid_center_error':grid_error,'max_pitch_error':max(pitch_errors),'technical':[],'fits':[],'placements_6x6':[],'compatibility_5x5':[],'five_by_five_mesh_created':False}
for name in board_names:report['technical'].append(technical(scene.objects[name]))
for fit in meta['fit_objects']:
    source=scene.objects[fit['source']];o=scene.objects[fit['name']];m=o.data
    tech=technical(source);report['technical'].append(tech)
    centers,method=panel_centers(source);w,h=fit['size'];col,row=fit['origin_cell']
    matrix=root.matrix_world.inverted()@o.matrix_world
    vs=board_vertices(o);bb=bounds(vs);tree=mesh_tree(vs,m)
    expected=[Vector((c-2.5,r-2.5,0)) for r in range(row,row+h) for c in range(col,col+w)]
    actual=[matrix@Vector((x,y,0)) for x,y in centers]
    assert len(actual)==w*h
    alignment=max(min((a-e).length for a in actual) for e in expected)
    intersections={name:len(tree.overlap(t)) for name,t in board_trees.items()}
    bottom_z=bb[2][0]
    bottom_polygons=[p for p in m.polygons if all(abs((matrix@m.vertices[i].co).z-bottom_z)<TOL for i in p.vertices)]
    samples={}
    for p in bottom_polygons:
        pts=[matrix@m.vertices[i].co for i in p.vertices]
        for v in pts+[sum(pts,Vector())/len(pts)]:samples[(round(v.x,7),round(v.y,7))]=v
    support=[]
    for v in samples.values():
        hits=[]
        for name,t in board_trees.items():
            hit=t.ray_cast(Vector((v.x,v.y,1)),Vector((0,0,-1)))[0]
            if hit is not None:hits.append((hit.z,name))
        assert hits
        z,name=max(hits);support.append({'surface':name,'gap':bottom_z-z})
    center_gaps=[]
    for e in expected:
        hit=board_trees['Board_6x6_Cells'].ray_cast(e+Vector((0,0,1)),Vector((0,0,-1)))[0]
        assert hit is not None;center_gaps.append(bottom_z-hit.z)
    e={'source':source.name,'fit_object':o.name,'panel_measurement_method':method,'actual_panel_centers_board_space':[list(v) for v in actual],'grid_center_max_error':alignment,'original_mesh_shared':source.data==o.data,'fit_scale':list(o.scale),'world_bottom_z':min((o.matrix_world@v.co).z for v in m.vertices),'bottom_z_board_space':bottom_z,'bvh_intersections':intersections,'plane_clearance_to_cells':bottom_z-board_bounds['Board_6x6_Cells'][2][1],'plane_clearance_to_frame':bottom_z-board_bounds['Board_6x6_Frame'][2][1],'cell_center_gap_range':[min(center_gaps),max(center_gaps)],'bottom_samples':len(support),'sample_gap_range':[min(s['gap'] for s in support),max(s['gap'] for s in support)],'samples_over_channels_or_frame':sum(s['surface']!='Board_6x6_Cells' for s in support),'physical_bottom_contact':False}
    assert alignment<TOL and all(v==0 for v in intersections.values()) and min(s['gap'] for s in support)>TOL
    assert source.data==o.data and max(abs(s-1) for s in o.scale)<TOL
    report['fits'].append(e)
    for n in (6,5):
        count=0;edgecount=0;min_clear=1e9;max_error=0;frame_bvh=0
        for row in range(n-h+1):
            for col in range(n-w+1):
                offset=Vector((col+(w-1)/2-(n-1)/2,row+(h-1)/2-(n-1)/2,0))
                vertices=[v.co+offset for v in source.data.vertices]
                assert min(v.z for v in vertices)>board_bounds['Board_6x6_Cells'][2][1]
                assert all(abs(v.x)<=n/2+TOL and abs(v.y)<=n/2+TOL for v in vertices)
                # Gerçek frame iç duvarı: sabit merkezli .100 yarıçaplı köşeler.
                hx=n/2+.035;r=.100
                sdf=[]
                for v in vertices:
                    qx=abs(v.x)-(hx-r);qy=abs(v.y)-(hx-r)
                    sdf.append(math.hypot(max(qx,0),max(qy,0))+min(max(qx,qy),0)-r)
                min_clear=min(min_clear,-max(sdf));assert min_clear>0
                for y in range(h):
                    for x in range(w):
                        goal=Vector((col+x-(n-1)/2,row+y-(n-1)/2,0))
                        err=min((Vector((cx,cy,0))+offset-goal).length for cx,cy in centers)
                        max_error=max(max_error,err);assert err<TOL
                if n==6:
                    tr=mesh_tree(vertices,source.data)
                    for name,t in board_trees.items():
                        hits=len(tr.overlap(t));assert hits==0
                        if name=='Board_6x6_Frame':frame_bvh+=hits
                if row==0 or col==0 or row+h==n or col+w==n:edgecount+=1
                count+=1
        result={'source':source.name,'size':n,'placements':count,'edge_placements':edgecount,'min_xy_frame_clearance':min_clear,'max_panel_error':max_error,'bvh_intersections_with_frame':frame_bvh if n==6 else None}
        (report['placements_6x6'] if n==6 else report['compatibility_5x5']).append(result)

report['generator_5x5_notes']={'same_grid_and_assets_compatible':True,'current_scene_names_are_6x6_specific':True,'existing_fit_layout_outside_5x5':[f['source'] for f in meta['fit_objects'] if f['origin_cell'][0]+f['size'][0]>5 or f['origin_cell'][1]+f['size'][1]>5]}
report['height_system']={'logical_surface_z':0,'actual_cell_top_z':board_bounds['Board_6x6_Cells'][2][1],'block_placement_z':0,'frame_top_z':board_bounds['Board_6x6_Frame'][2][1],'base_bounds_z':board_bounds['Board_6x6_Base'][2]}
report['root']={'world_location':list(root.matrix_world.translation),'scale':list(root.scale),'rotation':list(root.rotation_euler),'pivot_policy':'board local XY center at nominal playing plane Z=0; preview root offset must be removed or excluded on future export'}
report['changed_objects']=[name for name,sig in before.items() if ns['signature'](bpy.data.objects[name])!=sig]
report['new_objects']=sorted(set(o.name for o in bpy.data.objects)-set(before))
report['blend_hash_before']=file_before;report['blend_hash_after']=filehash(file_path)
ui_after=(bpy.context.scene.name,bpy.context.view_layer.objects.active.name if bpy.context.view_layer.objects.active else None,sorted(o.name for o in bpy.context.selected_objects))
report['ui_selection_unchanged']=ui_before==ui_after
assert not report['changed_objects'] and not report['new_objects'] and report['blend_hash_before']==report['blend_hash_after'] and report['ui_selection_unchanged']
report['status']='PASS_GEOMETRIC_FIT'
report['unity_import_tested']=False;report['exported']=False
(OUT/'audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({'status':report['status'],'assets':[{'name':e['source'],'error':e['grid_center_max_error'],'gap':e['cell_center_gap_range'],'sample_gap':e['sample_gap_range'],'collisions':e['bvh_intersections']} for e in report['fits']],'checked_6x6':sum(e['placements'] for e in report['placements_6x6']),'edge_6x6':sum(e['edge_placements'] for e in report['placements_6x6']),'numeric_5x5':sum(e['placements'] for e in report['compatibility_5x5']),'technical':[{'name':e['name'],'bad_uv_triangles':sum(u['zero_area_triangles'] for u in e['uv_layers']),'bad_normal_corners':e['opposed_corner_normals'],'nonmanifold':e['non_manifold_edges'],'normal_length_error':e['max_normal_length_error']} for e in report['technical']],'file_unchanged':report['blend_hash_before']==report['blend_hash_after']}))
