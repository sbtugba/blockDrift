"""Onaylı assetleri değiştirmeden parametrik sunum tahtası ve yerleşim kopyaları üretir."""
import bpy, bmesh, math, json, hashlib, datetime
from pathlib import Path
from dataclasses import dataclass, asdict
from mathutils import Vector

ROOT=next(p for p in Path(__file__).resolve().parents if (p/'AGENTS.md').is_file())
OUT=ROOT/'ArtProduction/Board_6x6';OUT.mkdir(parents=True,exist_ok=True)

@dataclass
class BoardParameters:
    rows:int=6
    columns:int=6
    grid_pitch:float=1.0
    visual_cell_size:float=.97
    cell_gap:float=.03
    cell_corner_radius:float=.145
    cell_height:float=.060
    board_thickness:float=.119
    board_margin:float=.120
    board_surface_height:float=0.0
    block_placement_offset:float=0.0
    visual_surface_offset:float=-.001
    frame_inner_clearance:float=.035
    frame_top_offset:float=-.006

P=BoardParameters()
assert abs(P.grid_pitch-P.visual_cell_size-P.cell_gap)<1e-9
family=bpy.data.scenes['AssetPreview'];bpy.context.window.scene=family
assert Path(bpy.data.filepath).name=='AssetPreview.blend'
assert not bpy.data.collections.get('Board_6x6'),'Board exists: refuse overwrite.'
backup=OUT/('Before_BoardCreation_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(backup),copy=True)

def signature(o):
    mesh=None;mats=None;settings=None
    if o.type=='MESH':
        m=o.data
        mesh=([tuple(v.co) for v in m.vertices],[(tuple(p.vertices),p.material_index,p.use_smooth) for p in m.polygons],[tuple(n.vector) for n in m.corner_normals],[[tuple(d.uv) for d in uv.data] for uv in m.uv_layers])
        mats=[(mat.name,[(n.type,[(i.name,str(i.default_value)) for i in n.inputs if hasattr(i,'default_value')]) for n in mat.node_tree.nodes]) for mat in m.materials if mat and mat.use_nodes]
    elif o.type=='LIGHT':settings=(o.data.type,o.data.energy,tuple(o.data.color),getattr(o.data,'size',None))
    elif o.type=='CAMERA':settings=(o.data.type,o.data.lens,o.data.ortho_scale)
    return hashlib.sha256(str((mesh,mats,settings,[list(r) for r in o.matrix_world],o.hide_viewport,o.hide_render,[c.name for c in o.users_collection])).encode()).hexdigest()

protected={o.name:signature(o) for o in bpy.data.objects}
(OUT/'protected_before.json').write_text(json.dumps(protected,indent=2))
(OUT/'parameters.json').write_text(json.dumps(asdict(P),indent=2))

def linear(c):return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
def mat(name,color,rough):
    assert not bpy.data.materials.get(name)
    m=bpy.data.materials.new(name);m.use_nodes=True
    rgb=tuple(linear(int(color[i:i+2],16)/255) for i in (0,2,4))+(1,)
    n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    n.inputs['Base Color'].default_value=rgb;n.inputs['Metallic'].default_value=0
    n.inputs['Roughness'].default_value=rough;n.inputs['Coat Weight'].default_value=.10
    n.inputs['Coat Roughness'].default_value=.28
    m.diffuse_color=rgb;m['source_srgb_hex']='#'+color;m['URP_Smoothness']=1-rough
    return m

cells_mat=mat('MAT_Board_Cream','F5D9B8',.35)
base_mat=mat('MAT_Board_Beige','D9A77D',.40)
board=bpy.data.collections.new('Board_6x6');family.collection.children.link(board)
tests=bpy.data.collections.new('Board_6x6_FitTests');family.collection.children.link(tests)
root=bpy.data.objects.new('Board_6x6_Root',None);board.objects.link(root);root.location=(0,10,0)
root['purpose']='Preview offset only; export board coordinates relative to this root'
root['grid_pitch']=P.grid_pitch;root['BoardSurfaceHeight']=P.board_surface_height
root['BlockPlacementOffset']=P.block_placement_offset;root['VisualCellSurfaceHeight']=P.visual_surface_offset

class Geometry:
    def __init__(self):self.verts=[];self.faces=[];self.normals=[];self.flat=set()
    def contour(self,hx,hy,r,z,segments):
        # Gerçek köşe yayı örneklemesi: dar köşe bölgelerinde radyal örnekleme kırığı oluşmaz.
        ids=[]
        for sx,sy,start in [(1,1,0),(-1,1,90),(-1,-1,180),(1,-1,270)]:
            for k in range(segments+1):
                a=math.radians(start+90*k/segments)
                x=sx*(hx-r)+r*math.cos(a);y=sy*(hy-r)+r*math.sin(a)
                ids.append(len(self.verts));self.verts.append((x,y,z));self.normals.append(None)
        return ids
    def face(self,ids,flat=False):
        self.faces.append(tuple(ids))
        if flat:self.flat.add(len(self.faces)-1)
    def bridge(self,a,b):
        for i in range(len(a)):
            k=(i+1)%len(a)
            self.face((a[i],a[k],b[k]));self.face((a[i],b[k],b[i]))
    def cap(self,ids,z,up):
        center=len(self.verts);self.verts.append((0,0,z));self.normals.append((0,0,1 if up else -1))
        for i in range(len(ids)):
            k=(i+1)%len(ids)
            self.face((center,ids[i],ids[k]) if up else (center,ids[k],ids[i]),True)
    def solid(self,profiles,segments=6,offset=(0,0),shoulder_start=None):
        begin=len(self.verts)
        rings=[self.contour(hx,hy,r,z,segments) for hx,hy,r,z in profiles]
        for j,(hx,hy,r,z) in enumerate(profiles):
            if j==0:dh,dz=-1,0
            elif j==len(profiles)-1:dh,dz=-1,0
            elif shoulder_start is not None and j>=shoulder_start:
                t=(j-shoulder_start)*math.pi*.5/(len(profiles)-1-shoulder_start)
                dh=-(profiles[shoulder_start][0]-profiles[-1][0])*math.sin(t)
                dz=(profiles[-1][3]-profiles[shoulder_start][3])*math.cos(t)
            else:
                dh=(profiles[j+1][0]-profiles[j-1][0]);dz=profiles[j+1][3]-profiles[j-1][3]
            for idx in rings[j]:
                x,y,_=self.verts[idx];cx=hx-r;cy=hy-r
                xy=Vector((x-max(-cx,min(cx,x)),y-max(-cy,min(cy,y)),0)).normalized()
                self.normals[idx]=tuple(Vector((xy.x*dz,xy.y*dz,-dh)).normalized())
        for a,b in zip(rings,rings[1:]):self.bridge(a,b)
        self.cap(rings[0],profiles[0][3],False);self.cap(rings[-1],profiles[-1][3],True)
        for idx in range(begin,len(self.verts)):
            x,y,z=self.verts[idx];self.verts[idx]=(x+offset[0],y+offset[1],z)
    def ring_solid(self,profiles,segments=12):
        rings=[self.contour(hx,hy,r,z,segments) for hx,hy,r,z in profiles]
        for j,ids in enumerate(rings):
            prev=rings[(j-1)%len(rings)];nxt=rings[(j+1)%len(rings)]
            for i,idx in enumerate(ids):
                tangent=Vector(self.verts[ids[(i+1)%len(ids)]])-Vector(self.verts[ids[(i-1)%len(ids)]])
                v=Vector(self.verts[idx]);direction=(v-Vector(self.verts[prev[i]])).normalized()+(Vector(self.verts[nxt[i]])-v).normalized()
                self.normals[idx]=tuple(tangent.cross(direction).normalized())
        for a,b in zip(rings,rings[1:]+rings[:1]):self.bridge(a,b)
    def object(self,name,material):
        m=bpy.data.meshes.new(name+'_Mesh');m.from_pydata(self.verts,[],self.faces);m.update();m.materials.append(material)
        o=bpy.data.objects.new(name,m);board.objects.link(o);o.parent=root
        for p in m.polygons:p.use_smooth=True
        for ob in bpy.context.selected_objects:ob.select_set(False)
        o.select_set(True);bpy.context.view_layer.objects.active=o
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
        custom=[]
        for p in m.polygons:
            for li in p.loop_indices:custom.append(tuple(p.normal) if p.index in self.flat else self.normals[m.loops[li].vertex_index])
        m.normals_split_custom_set(custom)
        return o

hx=P.columns*P.grid_pitch/2;hy=P.rows*P.grid_pitch/2
top=P.board_surface_height+P.visual_surface_offset
base_top=top-P.cell_height;bottom=base_top-P.board_thickness
g=Geometry();margin=P.board_margin;inner=P.frame_inner_clearance;frame_top=P.board_surface_height+P.frame_top_offset
g.solid([(hx+margin-.035,hy+margin-.035,.215,bottom),(hx+margin-.011,hy+margin-.011,.239,bottom+.005),(hx+margin,hy+margin,.250,bottom+.020),(hx+margin,hy+margin,.250,base_top-.012),(hx+margin-.006,hy+margin-.006,.244,base_top-.003),(hx+margin-.020,hy+margin-.020,.230,base_top)],12)
base=g.object('Board_6x6_Base',base_mat)
g=Geometry()
g.ring_solid([(hx+margin-.020,hy+margin-.020,.230,base_top),(hx+margin-.004,hy+margin-.004,.246,base_top+.008),(hx+margin,hy+margin,.250,frame_top-.034),(hx+margin-.003,hy+margin-.003,.247,frame_top-.014),(hx+margin-.015,hy+margin-.015,.235,frame_top-.002),(hx+inner+.018,hy+inner+.018,.118,frame_top),(hx+inner+.004,hy+inner+.004,.104,frame_top-.006),(hx+inner,hy+inner,.100,frame_top-.018),(hx+inner,hy+inner,.100,base_top)])
frame=g.object('Board_6x6_Frame',base_mat)
g=Geometry();centers=[]
for row in range(P.rows):
    for col in range(P.columns):
        x=(col-(P.columns-1)/2)*P.grid_pitch;y=(row-(P.rows-1)/2)*P.grid_pitch;centers.append((x,y))
        h=P.visual_cell_size/2
        profiles=[(h-.013,h-.013,P.cell_corner_radius-.013,base_top),(h,h,P.cell_corner_radius,base_top+.009),(h,h,P.cell_corner_radius,top-.035)]
        for k in range(1,6):
            t=k*math.pi*.5/5
            profiles.append((h-.050+.050*math.cos(t),h-.050+.050*math.cos(t),P.cell_corner_radius-.050+.050*math.cos(t),top-.035+.035*math.sin(t)))
        g.solid(profiles,6,(x,y),shoulder_start=2)
cells=g.object('Board_6x6_Cells',cells_mat)
cells['logical_surface_z']=0.0;cells['visual_top_z']=top;cells['cell_count']=P.rows*P.columns

# Görsel fit yerleşimi; oynanabilir seviye veya Core çözümü değildir.
placements=[('Block_4x1_Export',4,1,0,0),('Obstacle_1x1_Stone_Export',1,1,4,0),('Special_1x1_Star_Export',1,1,5,0),('Block_3x1',3,1,0,2),('Block_2x1_Export',2,1,3,2),('Block_1x1_Export',1,1,5,2),('Block_2x2_Export',2,2,0,4)]
fit=[]
for name,w,h,col,row in placements:
    source=family.objects[name];o=source.copy();o.name='Fit_6x6_'+name;tests.objects.link(o);o.parent=root
    o.location=((col+(w-1)/2-(P.columns-1)/2)*P.grid_pitch,(row+(h-1)/2-(P.rows-1)/2)*P.grid_pitch,P.board_surface_height+P.block_placement_offset)
    o['source_asset']=name;o['fit_origin_cell']=[col,row];o['occupied_size']=[w,h]
    fit.append({'name':o.name,'source':name,'size':[w,h],'origin_cell':[col,row],'local_location':list(o.location)})

assert all(signature(bpy.data.objects[name])==sig for name,sig in protected.items())
(OUT/'build_metadata.json').write_text(json.dumps({'backup':str(backup),'parameters':asdict(P),'logical_centers':centers,'fit_objects':fit,'board_origin_world':list(root.location),'base_top_z':base_top,'base_bottom_z':bottom,'protected_unchanged':True},indent=2))
text=bpy.data.texts.new('Board_6x6_generate.py');text.write(Path(__file__).read_text(encoding='utf-8'))
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print(json.dumps({'board_objects':[o.name for o in board.objects],'fit_types':len(fit),'cell_visual_top':top,'block_placement':0,'triangles':sum(len(o.data.polygons) for o in (base,frame,cells)),'protected_unchanged':True}))
