import bpy,math,json
from pathlib import Path
from mathutils import Vector
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'AGENTS.md').is_file())
OUT=ROOT/'ArtProduction/Board_6x6';family=bpy.data.scenes['AssetPreview']
assert not bpy.data.scenes.get('Review_Board_6x6_Empty'),'Review exists; refuse duplication.'
studio=bpy.data.collections.new('Preview_Studio_Board_6x6')
world=bpy.data.worlds.new('Board_Review_World');world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.7,.7,.7,1);bg.inputs[1].default_value=.35
for name,pos,power,size in [('Key',(-5,-6,10),1200,6),('Fill',(6,-2,7),450,6),('Rim',(1,6,8),1000,5)]:
    data=bpy.data.lights.new('Board_Studio_'+name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    o=bpy.data.objects.new(data.name,data);studio.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
ground=family.objects['ReviewGround.002'].copy();ground.name='Board_Review_Ground';studio.objects.link(ground);ground.location=(0,0,-.181)
cam=bpy.data.objects.new('Board_Review_Camera',bpy.data.cameras.new('Board_Review_Camera'));studio.objects.link(cam);cam.data.type='ORTHO'
board_review=bpy.data.collections.new('Board_6x6_Review_Geometry')
for name in ['Board_6x6_Base','Board_6x6_Frame','Board_6x6_Cells']:
    source=family.objects[name];o=source.copy();o.name=name+'_Review';o.parent=None;o.location=(0,0,0);board_review.objects.link(o)
fit_review=bpy.data.collections.new('Board_6x6_Review_Fits')
for source in bpy.data.collections['Board_6x6_FitTests'].objects:
    o=source.copy();o.name=source.name+'_Review';loc=source.location.copy();o.parent=None;o.location=loc;fit_review.objects.link(o)
scenes=[]
for label in ['Empty','Fits']:
    s=bpy.data.scenes.new('Review_Board_6x6_'+label);s.collection.children.link(studio);s.collection.children.link(board_review)
    if label=='Fits':s.collection.children.link(fit_review)
    s.camera=cam;s.world=world;s.render.engine=family.render.engine
    s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG'
    s.view_settings.view_transform='Standard';s.view_settings.exposure=-.8
    scenes.append((label,s))
for label,s in scenes:
    for view,pos,scale,res in [('Top',(0,0,12),7.35,(1500,1500)),('Perspective40',(4,-8.5,7.90),8.4,(1600,1400)),('Side',(0,-12,.05),7.2,(1600,500))]:
        cam.location=pos;cam.rotation_euler=(Vector((0,0,-.02))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
        s.render.resolution_x=res[0];s.render.resolution_y=res[1]
        s.render.filepath=str(OUT/(label+'_'+view+'.png'));bpy.ops.render.render(write_still=True,scene=s.name)
bpy.context.window.scene=family
for o in bpy.context.selected_objects:o.select_set(False)
for o in bpy.data.collections['Board_6x6'].objects:
    if o.type=='MESH':o.select_set(True)
bpy.context.view_layer.objects.active=family.objects['Board_6x6_Cells']
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        with bpy.context.temp_override(area=area,region=next(r for r in area.regions if r.type=='WINDOW')):bpy.ops.view3d.view_selected(use_all_regions=False)
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('Six board/fit renders complete; original scene active and board framed.')
