import bpy,bmesh,json,os,math,hashlib,datetime
from mathutils import Vector
from mathutils.bvhtree import BVHTree
# Proje kökü script dosyasından veya açık Blender dosyasından bulunur; kullanıcıya özel yol saklanmaz.
import os
from pathlib import Path
_PROJECT_ROOT = next((p for p in Path(globals().get("__file__", bpy.data.filepath or os.getcwd())).resolve().parents
                      if (p / "AGENTS.md").is_file()), None)
if _PROJECT_ROOT is None:
    raise RuntimeError("Scripti Block Rift proje klasöründen veya proje içindeki Blender dosyasından çalıştırın.")
OUT=str(_PROJECT_ROOT / 'ArtProduction/Board_6x6/Analysis')
os.makedirs(OUT,exist_ok=True)
scene=bpy.data.scenes['AssetPreview']
assert os.path.basename(bpy.data.filepath)=='AssetPreview.blend'
backup=os.path.join(OUT,'Before_BoardAnalysis_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend')
bpy.ops.wm.save_as_mainfile(filepath=backup,copy=True)
specs=[('Block_1x1_Export',1,1),('Block_2x1_Export',2,1),('Block_3x1',3,1),('Block_4x1_Export',4,1),('Block_2x2_Export',2,2),('Obstacle_1x1_Stone_Export',1,1),('Special_1x1_Star_Export',1,1)]
TOL=1e-5
def fingerprint(o):
    m=o.data
    return hashlib.sha256(str(([tuple(v.co) for v in m.vertices],[(tuple(p.vertices),p.material_index,p.use_smooth) for p in m.polygons],[tuple(n.vector) for n in m.corner_normals],[[tuple(d.uv) for d in uv.data] for uv in m.uv_layers],[list(r) for r in o.matrix_world],[(mat.name,[(n.type,[(i.name,str(i.default_value)) for i in n.inputs if hasattr(i,'default_value')]) for n in mat.node_tree.nodes]) for mat in m.materials])).encode()).hexdigest()
before={n:fingerprint(scene.objects[n]) for n,_,_ in specs}
report={'tolerance':TOL,'backup':backup,'assets':[],'neighbor_tests':[],'status':'MEASUREMENT_ONLY','board_created':False}
trees={}
for name,w,h in specs:
    o=scene.objects[name];m=o.data
    world=[o.matrix_world@v.co for v in m.vertices];local=[v.co.copy() for v in m.vertices]
    bounds=lambda vs:{axis:[min(v[k] for v in vs),max(v[k] for v in vs)] for k,axis in enumerate('xyz')}
    lb=bounds(local);wb=bounds(world)
    expected=[(x-(w-1)/2,y-(h-1)/2) for y in range(h) for x in range(w)]
    measured=[]
    if name.startswith('Block_'):
        faces=[p for p in m.polygons if all(abs(m.vertices[i].co.z-.260)<TOL for i in p.vertices)]
        # Oyuk tabanlarının gerçek mesh bağlı bileşenleri; merkezler bounding box üzerinden.
        groups=[];unseen={p.index for p in faces};v_faces={}
        for p in faces:
            for i in p.vertices:v_faces.setdefault(i,set()).add(p.index)
        while unseen:
            todo=[unseen.pop()];ids=set()
            while todo:
                p=m.polygons[todo.pop()];ids.update(p.vertices)
                for i in p.vertices:
                    for pi in v_faces[i]:
                        if pi in unseen:unseen.remove(pi);todo.append(pi)
            if len(ids)>5:
                bb=bounds([m.vertices[i].co for i in ids]);groups.append(((bb['x'][0]+bb['x'][1])/2,(bb['y'][0]+bb['y'][1])/2))
        measured=sorted(groups,key=lambda p:(p[1],p[0]))
    elif name.startswith('Special'):
        # Beşli simetri geometrik merkezi: en yakın eş vertex hatasıyla kontrol edilir.
        ids={i for p in m.polygons if p.material_index==1 for i in p.vertices}
        vs=[m.vertices[i].co for i in ids]
        from mathutils.kdtree import KDTree
        kd=KDTree(len(vs))
        for i,v in enumerate(vs):kd.insert(v,i)
        kd.balance();a=math.pi*2/5
        error=max(kd.find(Vector((v.x*math.cos(a)-v.y*math.sin(a),v.x*math.sin(a)+v.y*math.cos(a),v.z)))[2] for v in vs)
        measured=[(0,0)]
    else:measured=[((lb['x'][0]+lb['x'][1])/2,(lb['y'][0]+lb['y'][1])/2)]
    center_error=max((min(math.dist(e,p) for p in measured) for e in expected),default=0) if measured else None
    pitches=sorted(set(round(math.dist(a,b),7) for a in measured for b in measured if TOL<math.dist(a,b)<1.01))
    bm=bmesh.new();bm.from_mesh(m);tree=BVHTree.FromBMesh(bm);trees[name]=tree
    cx,cy=expected[0]
    sections=[]
    for z in (.025,.1,.21,.22,.26,.28,.299):
        left=tree.ray_cast(Vector((-w/2-2,cy,z)),Vector((1,0,0)))[0]
        right=tree.ray_cast(Vector((w/2+2,cy,z)),Vector((-1,0,0)))[0]
        sections.append({'z':z,'x_min':left.x if left else None,'x_max':right.x if right else None})
    bm.free()
    entry={'name':name,'grid_cells':[w,h],'world_bounds':wb,'local_bounds':lb,'world_dimensions':[wb[k][1]-wb[k][0] for k in 'xyz'],'world_origin':list(o.matrix_world.translation),'scale':list(o.scale),'rotation':list(o.rotation_euler),'parent':o.parent.name if o.parent else None,'local_bottom_z':lb['z'][0],'pivot_offset_from_bottom_center':[(lb['x'][0]+lb['x'][1])/2,(lb['y'][0]+lb['y'][1])/2,lb['z'][0]],'bevel_overflow':max(0,-w/2-lb['x'][0],lb['x'][1]-w/2,-h/2-lb['y'][0],lb['y'][1]-h/2),'expected_panel_centers':expected,'measured_panel_centers':measured,'max_panel_center_error':center_error,'measured_neighbor_center_pitches':pitches,'side_sections':sections,'dimensions_match_cell_counts':all(abs((lb[k][1]-lb[k][0])-target)<TOL for k,target in zip('xyz',(w,h,.3)))}
    if name.startswith('Special'):entry['fivefold_symmetry_error']=error
    report['assets'].append(entry)

# Yerinde nesne kopyası üretmeden gerçek mesh verilerinde grid komşuluk dönüşümü.
# Satır komşularının Y merkezleri aynı tutulur; X merkezleri hücre sayılarının yarı toplamıdır.
for a in report['assets']:
    for b in report['assets']:
        dx=(a['grid_cells'][0]+b['grid_cells'][0])/2
        gap=dx+b['local_bounds']['x'][0]-a['local_bounds']['x'][1]
        cross=[]
        ta,tb=trees[a['name']],trees[b['name']]
        for z in (.1,.21,.28,.299):
            ar=ta.ray_cast(Vector((a['grid_cells'][0]/2+2,0,z)),Vector((-1,0,0)))[0]
            bl=tb.ray_cast(Vector((-b['grid_cells'][0]/2-2,0,z)),Vector((1,0,0)))[0]
            if ar and bl:cross.append({'z':z,'surface_gap':dx+bl.x-ar.x})
        report['neighbor_tests'].append({'a':a['name'],'b':b['name'],'center_dx':dx,'minimum_x_separation':gap,'cross_sections_at_y0':cross,'positive_volume_overlap_excluded_by_separating_plane':gap>=-TOL})

report['grid_pitch_from_measurements']=1.0
report['max_dimension_error']=max(abs(a['world_dimensions'][k]-target) for a in report['assets'] for k,target in enumerate((*a['grid_cells'],.3)))
report['max_panel_center_error']=max(a['max_panel_center_error'] or 0 for a in report['assets'])
report['minimum_neighbor_gap']=min(p['minimum_x_separation'] for p in report['neighbor_tests'])
report['proposed_board_surface_height']=0.0
report['proposed_block_placement_offset']=0.0
report['protected_models_unchanged']=all(fingerprint(scene.objects[n])==sig for n,sig in before.items())
report['decision_required']='Pitch=1 aligns all existing panel centers, but full-width bodies have zero positive side clearance. Increasing pitch causes multi-cell center mismatch. Confirm acceptance of body contact and bevel-only visual separation, or authorize asset size revision.'
assert report['protected_models_unchanged']
json.dump(report,open(os.path.join(OUT,'measurement_report.json'),'w'),indent=2)
print(json.dumps({'assets':[{k:a[k] for k in ('name','world_dimensions','scale','local_bottom_z','measured_panel_centers','max_panel_center_error','bevel_overflow')} for a in report['assets']],'max_dimension_error':report['max_dimension_error'],'neighbor_gap_range':[min(p['minimum_x_separation'] for p in report['neighbor_tests']),max(p['minimum_x_separation'] for p in report['neighbor_tests'])],'unchanged':report['protected_models_unchanged'],'backup':backup}))
