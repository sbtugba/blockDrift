"""Mevcut hücre meshini aynen koruyarak ince destekli alternatif üretir."""
import bpy,bmesh,math,json,hashlib,datetime
from pathlib import Path
from mathutils import Vector
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'AGENTS.md').is_file())
OUT=ROOT/'ArtProduction/Board_6x6/Frameless';OUT.mkdir(parents=True,exist_ok=True)
family=bpy.data.scenes['AssetPreview'];bpy.context.window.scene=family
assert Path(bpy.data.filepath).name=='AssetPreview.blend'
assert not bpy.data.collections.get('Board_6x6_Frameless'),'Alternative exists; refuse overwrite.'
backup=OUT/('Before_Frameless_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(backup),copy=True)
basecode=(OUT.parent/'generate_board.py').read_text(encoding='utf-8');ns={'hashlib':hashlib}
exec(basecode[basecode.index('def signature'):basecode.index('protected=')],ns)
protected={o.name:ns['signature'](o) for o in bpy.data.objects}
(OUT/'protected_before.json').write_text(json.dumps(protected,indent=2))

coll=bpy.data.collections.new('Board_6x6_Frameless');family.collection.children.link(coll)
fitcoll=bpy.data.collections.new('Board_6x6_Frameless_FitTests');family.collection.children.link(fitcoll)
root=bpy.data.objects.new('Board_6x6_Frameless_Root',None);coll.objects.link(root);root.location=(8,10,0)
for key,val in {'grid_pitch':1.0,'BoardSurfaceHeight':0.0,'BlockPlacementOffset':0.0,'VisualCellSurfaceHeight':-.001,'variant':'Frameless; no environment geometry'}.items():root[key]=val
source_cells=family.objects['Board_6x6_Cells']
cells=source_cells.copy();cells.name='Board_6x6_Frameless_Cells';cells.parent=root;cells.location=(0,0,0);coll.objects.link(cells)
assert cells.data==source_cells.data

# Hücre dış sınırından sadece .005 taşan destek; kalınlık .006, yükseltilmiş kenar yok.
profiles=[(2.988,.148,-.067),(2.989,.149,-.066),(2.990,.150,-.063),(2.989,.149,-.061)]
verts=[];faces=[];vnorm=[];flat=set();rings=[];SEG=16
for h,r,z in profiles:
    ids=[]
    for sx,sy,start in [(1,1,0),(-1,1,90),(-1,-1,180),(1,-1,270)]:
        for k in range(SEG+1):
            a=math.radians(start+90*k/SEG)
            ids.append(len(verts));verts.append((sx*(h-r)+r*math.cos(a),sy*(h-r)+r*math.sin(a),z));vnorm.append(None)
    rings.append(ids)
N=len(rings[0])
for j,ids in enumerate(rings):
    for i,idx in enumerate(ids):
        tangent=Vector(verts[ids[(i+1)%N]])-Vector(verts[ids[(i-1)%N]])
        v=Vector(verts[idx])
        if j==0:direction=Vector(verts[rings[1][i]])-v
        elif j==len(rings)-1:direction=v-Vector(verts[rings[j-1][i]])
        else:direction=(v-Vector(verts[rings[j-1][i]])).normalized()+(Vector(verts[rings[j+1][i]])-v).normalized()
        vnorm[idx]=tuple(tangent.cross(direction).normalized())
for a,b in zip(rings,rings[1:]):
    for i in range(N):
        k=(i+1)%N;faces.extend([(a[i],a[k],b[k]),(a[i],b[k],b[i])])
for ids,z,up in [(rings[0],profiles[0][2],False),(rings[-1],profiles[-1][2],True)]:
    center=len(verts);verts.append((0,0,z));vnorm.append((0,0,1 if up else -1))
    for i in range(N):
        k=(i+1)%N;flat.add(len(faces));faces.append((center,ids[i],ids[k]) if up else (center,ids[k],ids[i]))
m=bpy.data.meshes.new('Board_6x6_Frameless_Support_Mesh');m.from_pydata(verts,[],faces);m.update()
m.materials.append(bpy.data.materials['MAT_Board_Beige'])
support=bpy.data.objects.new('Board_6x6_Frameless_Support',m);coll.objects.link(support);support.parent=root
for p in m.polygons:p.use_smooth=True
for o in bpy.context.selected_objects:o.select_set(False)
support.select_set(True);bpy.context.view_layer.objects.active=support
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
m.normals_split_custom_set([tuple(p.normal) if p.index in flat else vnorm[m.loops[li].vertex_index] for p in m.polygons for li in p.loop_indices])
support['support_thickness']=.006;support['outer_extension_from_cell_side']=.005;support['no_raised_rim']=True

meta=json.loads((OUT.parent/'build_metadata.json').read_text());fits=[]
for entry in meta['fit_objects']:
    source=family.objects[entry['source']];o=source.copy();o.name='Fit_Frameless_'+entry['source'];fitcoll.objects.link(o);o.parent=root;o.location=entry['local_location']
    o['source_asset']=entry['source'];o['fit_origin_cell']=entry['origin_cell'];o['occupied_size']=entry['size']
    fits.append({**entry,'name':o.name})
bm=bmesh.new();bm.from_mesh(m)
qa={'support_vertices':len(m.vertices),'support_triangles':len(m.polygons),'non_manifold_edges':sum(not e.is_manifold for e in bm.edges),'degenerate_faces':sum(f.calc_area()<1e-10 for f in bm.faces),'signed_volume':bm.calc_volume(signed=True),'opposed_corner_normals':sum(p.normal.dot(m.corner_normals[li].vector)<-.0001 for p in m.polygons for li in p.loop_indices)}
bm.free();assert qa['non_manifold_edges']==qa['degenerate_faces']==qa['opposed_corner_normals']==0 and qa['signed_volume']>0,qa
assert all(ns['signature'](bpy.data.objects[name])==sig for name,sig in protected.items())
(OUT/'variant_metadata.json').write_text(json.dumps({'backup':str(backup),'root':root.name,'root_preview_offset':list(root.location),'support':support.name,'cells':cells.name,'fits':fits,'grid_pitch':1.0,'cell_size':.97,'cell_height':.06,'logical_surface_z':0,'visual_cell_top_z':-.001,'block_placement_z':0,'support_bounds_z':[-.067,-.061],'support_width':5.98,'max_outer_reveal':.005,'protected_unchanged':True,'qa':qa},indent=2))
t=bpy.data.texts.new('Board_6x6_Frameless_create.py');t.write(Path(__file__).read_text(encoding='utf-8'))
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print(json.dumps({'collection':coll.name,'cell_mesh_shared_unchanged':cells.data==source_cells.data,'support_qa':qa,'old_models_unchanged':True,'backup':str(backup)}))
