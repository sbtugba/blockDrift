"""Tahtaya dokunmadan organik, açık arazi halkası üretir; 5×5/6×6 parametreleri ortaktır."""
import bpy,bmesh,math,json,hashlib,datetime
from pathlib import Path
from mathutils import Vector
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'AGENTS.md').is_file())
OUT=Path(__file__).resolve().parent
P=json.loads((OUT/'parameters.json').read_text())
assert not bpy.data.objects.get('Sand_BoardTransition_6x6'),'Sand already exists; no overwrite.'
assert Path(bpy.data.filepath).name=='AssetPreview.blend'
backup=OUT/'Backups'/('Before_Sand_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(backup),copy=True)
code=(ROOT/'ArtProduction/Board_6x6/generate_board.py').read_text(encoding='utf-8');ns={'hashlib':hashlib}
exec(code[code.index('def signature'):code.index('protected=')],ns)
protected={o.name:ns['signature'](o) for o in bpy.data.objects}
(OUT/'protected_before.json').write_text(json.dumps(protected,indent=2))

def outline(hx,hy,r,arcs,straights):
    points=[]
    for sx,sy,start in [(1,1,0),(-1,1,90),(-1,-1,180),(1,-1,270)]:
        cx=sx*(hx-r);cy=sy*(hy-r)
        for k in range(arcs):
            a=math.radians(start+90*k/arcs);n=Vector((math.cos(a),math.sin(a)))
            points.append((Vector((cx,cy))+n*r,n))
        end=math.radians(start+90);a=Vector((cx+r*math.cos(end),cy+r*math.sin(end)))
        if start==0:b=Vector((-hx+r,hy));n=Vector((0,1))
        elif start==90:b=Vector((-hx,-hy+r));n=Vector((-1,0))
        elif start==180:b=Vector((hx-r,-hy));n=Vector((0,-1))
        else:b=Vector((hx,hy-r));n=Vector((1,0))
        for k in range(straights):points.append((a.lerp(b,k/straights),n))
    return points

def build(parameters):
    p=parameters;hx=(p['columns']-1)*p['grid_pitch']/2+p['visual_cell_size']/2
    hy=(p['rows']-1)*p['grid_pitch']/2+p['visual_cell_size']/2;r=p['cell_corner_radius']
    verts=[];rings=[]
    for j,(d,z) in enumerate(zip(p['ring_offsets'],p['ring_heights'])):
        ids=[]
        for point,n in outline(hx+d,hy+d,r+d,p['arc_segments'],p['straight_segments']):
            a=math.atan2(point.y,point.x)
            organic=(.55*math.sin(3*a+.8)+.3*math.sin(7*a-1.2)+.15*math.cos(11*a+.4))
            lateral=(1-math.exp(-d*5))*(.014+.007*d)*organic if j else 0
            xy=point+n*lateral
            edgewave=.0035*math.sin(3*a+.4)+.0018*math.sin(7*a-1)
            undulate=.0045*math.sin(.76*xy.x+1.13*xy.y)+.003*math.sin(1.21*xy.x-.68*xy.y)
            height=z+edgewave*math.exp(-d*2.5)+undulate*(1-math.exp(-d*3))
            ids.append(len(verts));verts.append((xy.x,xy.y,height))
        rings.append(ids)
    faces=[];N=len(rings[0])
    for a,b in zip(rings,rings[1:]):
        for i in range(N):
            k=(i+1)%N;faces.extend([(a[i],b[i],b[k]),(a[i],b[k],a[k])])
    return verts,faces,{'hx':hx,'hy':hy,'corner_radius':r,'ring_vertices':N,'rings':len(rings)}

verts,faces,shape=build(P)
m=bpy.data.meshes.new('Sand_BoardTransition_6x6_Mesh');m.from_pydata(verts,[],faces);m.update()
for p in m.polygons:p.use_smooth=True
uv=m.uv_layers.new(name='UV_Sand_Planar')
extent=P['texture_half_extent']
for p in m.polygons:
    for li in p.loop_indices:
        v=m.vertices[m.loops[li].vertex_index].co;uv.data[li].uv=((v.x+extent)/(2*extent),(v.y+extent)/(2*extent))
mat=bpy.data.materials.new('MAT_Sand_Peach_URP');mat.use_nodes=True
bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
bs.inputs['Roughness'].default_value=.83;bs.inputs['Metallic'].default_value=0
bs.inputs['Coat Weight'].default_value=0
color=mat.node_tree.nodes.new('ShaderNodeTexImage');color.label='URP Base Map — sRGB'
color.image=bpy.data.images.load(str(OUT/'Textures/Sand_BaseColor_512.png'),check_existing=False);color.image.pack()
normaltex=mat.node_tree.nodes.new('ShaderNodeTexImage');normaltex.label='URP Normal Map — tangent +Y'
normaltex.image=bpy.data.images.load(str(OUT/'Textures/Sand_Normal_512.png'),check_existing=False);normaltex.image.colorspace_settings.name='Non-Color';normaltex.image.pack()
normal=mat.node_tree.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.35
mat.node_tree.links.new(color.outputs['Color'],bs.inputs['Base Color']);mat.node_tree.links.new(normaltex.outputs['Color'],normal.inputs['Color']);mat.node_tree.links.new(normal.outputs['Normal'],bs.inputs['Normal'])
palette=json.loads((OUT/'palette.json').read_text());rgb=palette['median_srgb']
def linear(c):
    x=c/255;return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
mat.diffuse_color=tuple(linear(c) for c in rgb)+(1,);mat['URP_Smoothness']=.17;mat['source_palette_hex']=palette['median_hex']
m.materials.append(mat)
collection=bpy.data.collections.new('Environment_Sand_6x6')
s=bpy.data.scenes.new('Review_Sand_6x6_Empty');s.collection.children.link(collection)
o=bpy.data.objects.new('Sand_BoardTransition_6x6',m);collection.objects.link(o)
boardroot=bpy.data.objects['Board_6x6_Frameless_Root'];o.matrix_world=boardroot.matrix_world.copy()
o['board_reference']=boardroot.name;o['grid_pitch']=1.0;o['rows']=P['rows'];o['columns']=P['columns'];o['no_gameplay_collider']=True
refs=bpy.data.collections.new('Sand_Board_References_6x6');s.collection.children.link(refs)
for name in ['Board_6x6_Frameless_Cells','Board_6x6_Frameless_Support']:
    source=bpy.data.objects[name];clone=source.copy();clone.name='SandReview_'+name;clone.parent=None;clone.matrix_world=source.matrix_world.copy();refs.objects.link(clone)
bm=bmesh.new();bm.from_mesh(m)
qa={'vertices':len(m.vertices),'triangles':len(m.polygons),'boundary_edges':sum(e.is_boundary for e in bm.edges),'unexpected_non_manifold_edges':sum(not e.is_manifold and not e.is_boundary for e in bm.edges),'degenerate_faces':sum(f.calc_area()<1e-10 for f in bm.faces),'downward_faces':sum(f.normal.z<=0 for f in bm.faces),'z_range':[min(v.co.z for v in m.vertices),max(v.co.z for v in m.vertices)]}
bm.free();assert qa['unexpected_non_manifold_edges']==qa['degenerate_faces']==qa['downward_faces']==0
assert qa['boundary_edges']==shape['ring_vertices']*2
assert all(ns['signature'](bpy.data.objects[name])==sig for name,sig in protected.items())
(OUT/'generation.json').write_text(json.dumps({'backup':str(backup),'parameters':P,'shape':shape,'qa':qa,'source_board':boardroot.name,'world_pivot':list(o.matrix_world.translation),'board_references':refs.name,'terrain_collection':collection.name,'source_objects_unchanged':True,'environment_not_linked_into_original_assetpreview':True},indent=2))
t=bpy.data.texts.new('Environment_Sand_6x6_generate.py');t.write(Path(__file__).read_text(encoding='utf-8'))
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print(json.dumps({'qa':qa,'pivot':list(o.matrix_world.translation),'palette':palette['median_hex'],'source_objects_unchanged':True}))
