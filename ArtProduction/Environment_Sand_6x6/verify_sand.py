import bpy,bmesh,math,json,hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=Path(__file__).resolve().parent
P=json.loads((OUT/'parameters.json').read_text());meta=json.loads((OUT/'generation.json').read_text())
o=bpy.data.objects['Sand_BoardTransition_6x6'];m=o.data;root=bpy.data.objects[meta['source_board']]
def root_vertices(obj):
    transform=root.matrix_world.inverted()@obj.matrix_world
    return [transform@v.co for v in obj.data.vertices]
def tree(vs,mesh):return BVHTree.FromPolygons(vs,[tuple(p.vertices) for p in mesh.polygons],all_triangles=True)
vs=root_vertices(o);sand=tree(vs,m);zmax=max(v.z for v in vs)
r={'tolerance':1e-5,'vertices':len(m.vertices),'triangles':len(m.polygons),'submeshes':len(m.materials),'scale':list(o.scale),'rotation':list(o.rotation_euler),'pivot_world':list(o.matrix_world.translation),'pivot_matches_board':(o.matrix_world.translation-root.matrix_world.translation).length<1e-8,'z_range':[min(v.z for v in vs),zmax],'board_intersections':{},'asset_tests':[]}
for name in ['Board_6x6_Frameless_Cells','Board_6x6_Frameless_Support']:
    src=bpy.data.objects[name];tr=tree(root_vertices(src),src.data);hits=sand.overlap(tr)
    r['board_intersections'][name]=len(hits);assert len(hits)==0,(name,len(hits))
cells=bpy.data.objects['Board_6x6_Frameless_Cells'];cell_vs=root_vertices(cells)
top_z=max(v.z for v in cell_vs);top_vertices=[v for v in cell_vs if abs(v.z-top_z)<1e-6]
occluded=sum(sand.ray_cast(Vector((v.x,v.y,1)),Vector((0,0,-1)))[0] is not None for v in top_vertices)
assert occluded==0
r['top_vertices_tested']=len(top_vertices);r['top_vertex_occlusions']=occluded;r['minimum_vertical_gap_to_cell_top_plane']=top_z-zmax
celltree=tree(cell_vs,cells.data)
N=meta['shape']['ring_vertices']
r['minimum_inner_edge_to_cell_mesh_distance']=min(celltree.find_nearest(v)[3] for v in vs[:N])
r['inner_edge_height_range']=[min(v.z for v in vs[:N]),max(v.z for v in vs[:N])]
fitmeta=json.loads((OUT.parent/'Board_6x6/Frameless/variant_metadata.json').read_text())
for fit in fitmeta['fits']:
    src=bpy.data.objects[fit['source']];oldfit=bpy.data.objects[fit['name']];tr=tree(root_vertices(oldfit),oldfit.data)
    assert not sand.overlap(tr)
    w,h=fit['size'];count=0;edge=0
    for row in range(7-h):
        for col in range(7-w):
            offset=Vector((col+(w-1)/2-2.5,row+(h-1)/2-2.5,0));points=[v.co+offset for v in src.data.vertices]
            assert min(v.z for v in points)>zmax
            assert not sand.overlap(tree(points,src.data))
            if row==0 or col==0 or row+h==6 or col+w==6:edge+=1
            count+=1
    r['asset_tests'].append({'source':src.name,'placements':count,'edge_placements':edge,'sand_intersections':0})
bm=bmesh.new();bm.from_mesh(m)
r['topology']={'boundary_edges':sum(e.is_boundary for e in bm.edges),'unexpected_non_manifold_edges':sum(not e.is_manifold and not e.is_boundary for e in bm.edges),'loose_vertices':sum(not v.link_edges for v in bm.verts),'degenerate_faces':sum(f.calc_area()<1e-10 for f in bm.faces),'downward_faces':sum(f.normal.z<=0 for f in bm.faces),'inconsistent_winding':sum(e.is_manifold and not e.is_contiguous for e in bm.edges)}
bm.free();assert r['topology']['boundary_edges']==2*N
assert all(v==0 for k,v in r['topology'].items() if k!='boundary_edges')
uvs=[tuple(d.uv) for d in m.uv_layers.active.data]
r['uv']={'name':m.uv_layers.active.name,'finite':all(math.isfinite(v) for uv in uvs for v in uv),'min':[min(uv[k] for uv in uvs) for k in range(2)],'max':[max(uv[k] for uv in uvs) for k in range(2)]}
assert r['uv']['finite'] and all(0<=v<=1 for uv in uvs for v in uv)
r['normal']={'unit_max_error':max(abs(n.vector.length-1) for n in m.corner_normals),'opposed_corners':sum(p.normal.dot(m.corner_normals[li].vector)<-.0001 for p in m.polygons for li in p.loop_indices)}
assert r['normal']['unit_max_error']<1e-5 and r['normal']['opposed_corners']==0
m.calc_tangents();r['finite_tangents']=all(all(math.isfinite(c) for c in l.tangent) for l in m.loops);m.free_tangents();assert r['finite_tangents']
src=(OUT/'generate_sand.py').read_text(encoding='utf-8');scope={'Vector':Vector,'math':math}
exec(src[src.index('def outline'):src.index('verts,faces,shape=build(P)')],scope)
p5={**P,'rows':5,'columns':5};v5,f5,shape5=scope['build'](p5)
r['five_by_five_parameters']={'tested_numeric_only':True,'vertices':len(v5),'triangles':len(f5),'hx':shape5['hx'],'hy':shape5['hy'],'grid_pitch':p5['grid_pitch'],'inner_clearance':p5['ring_offsets'][0],'scene_asset_created':False}
assert abs(shape5['hx']-2.485)<1e-6 and abs(shape5['hy']-2.485)<1e-6
basecode=(OUT.parent/'Board_6x6/generate_board.py').read_text(encoding='utf-8');ns={'hashlib':hashlib}
exec(basecode[basecode.index('def signature'):basecode.index('protected=')],ns)
protected=json.loads((OUT/'protected_before.json').read_text());r['changed_original_objects']=[n for n,sig in protected.items() if ns['signature'](bpy.data.objects[n])!=sig];assert not r['changed_original_objects']
r['status']='PASS';r['colliders_created']=0;r['unity_exported']=False
r['texture_resolution']=[512,512];r['texture_count']=2;r['rgba8_mipmap_memory_mib']=2*512*512*4*4/3/1048576
r['mesh_bytes_estimate_32_byte_vertex_16_bit_index']=len(m.vertices)*32+len(m.polygons)*3*2
(OUT/'qa.json').write_text(json.dumps(r,indent=2))
print(json.dumps({'status':r['status'],'board_intersections':r['board_intersections'],'top_occlusion':occluded,'inner_edge_cell_clearance':r['minimum_inner_edge_to_cell_mesh_distance'],'placements':sum(t['placements'] for t in r['asset_tests']),'normal':r['normal'],'topology':r['topology'],'source_changes':r['changed_original_objects']}))
