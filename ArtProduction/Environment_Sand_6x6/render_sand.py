import bpy,json
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent
root=bpy.data.objects['Board_6x6_Frameless_Root'];center=root.matrix_world.translation.copy()
empty=bpy.data.scenes['Review_Sand_6x6_Empty'];assert empty.camera is None,'Preview already configured'
studio=bpy.data.collections.new('Preview_Studio_Sand_6x6')
for src in bpy.data.collections['Preview_Studio_Board_6x6'].objects:
    if src.type!='LIGHT':continue
    o=src.copy();o.data=src.data.copy();o.name='Sand_'+src.name;o.location=src.location+center;studio.objects.link(o)
cam=bpy.data.objects.new('Sand_Review_Camera',bpy.data.cameras.new('Sand_Review_Camera'));studio.objects.link(cam);cam.data.type='ORTHO'
world=bpy.data.scenes['Review_Board_6x6_Empty'].world.copy();world.name='Sand_Review_World'
references=bpy.data.collections['Sand_Board_References_6x6'];terrain=bpy.data.collections['Environment_Sand_6x6']
fits=bpy.data.collections.new('Sand_Fit_References_6x6')
fitmeta=json.loads((OUT.parent/'Board_6x6/Frameless/variant_metadata.json').read_text())
for fit in fitmeta['fits']:
    source=bpy.data.objects[fit['name']];o=source.copy();o.name='SandReview_'+source.name;o.parent=None;o.matrix_world=source.matrix_world.copy();fits.objects.link(o)
filled=bpy.data.scenes.new('Review_Sand_6x6_Fits');filled.collection.children.link(terrain);filled.collection.children.link(references);filled.collection.children.link(fits)
plate=bpy.data.scenes.new('Review_Sand_PNG_Approach');plate.collection.children.link(references)
for s in [empty,filled,plate]:
    s.collection.children.link(studio);s.world=world;s.camera=cam;s.render.engine=bpy.data.scenes['AssetPreview'].render.engine
    s.render.film_transparent=True;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.resolution_percentage=100
    s.view_settings.view_transform='Standard';s.view_settings.exposure=-.8
views=[(empty,'Sand_Top',(0,0,12),9.0,(1500,1500)),(empty,'Sand_Perspective40',(4,-8.5,7.9),9.0,(1600,1400)),(filled,'Sand_WithBlocks',(4,-8.5,7.9),9.0,(1600,1400)),(filled,'Sand_WithBlocks_Top',(0,0,12),9.0,(1500,1500)),(plate,'PNG_Approach_BoardOnly',(4,-8.5,7.9),9.0,(1600,1400))]
for s,name,pos,scale,res in views:
    cam.location=center+Vector(pos);cam.rotation_euler=(center+Vector((0,0,-.02))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
    s.render.resolution_x=res[0];s.render.resolution_y=res[1];s.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True,scene=s.name)
bpy.context.window.scene=filled
for o in bpy.context.selected_objects:o.select_set(False)
cellref=next(o for o in references.objects if 'Cells' in o.name);cellref.select_set(True);bpy.context.view_layer.objects.active=cellref
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        with bpy.context.temp_override(area=area,region=next(r for r in area.regions if r.type=='WINDOW')):bpy.ops.view3d.view_selected(use_all_regions=False)
for filename in ('generate_sand.py','verify_sand.py','render_sand.py'):
    t=bpy.data.texts.new('Environment_Sand_'+filename);t.write((OUT/filename).read_text(encoding='utf-8'))
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('Five renders saved; live scene shows 3D sand and all seven asset references. No extra environment props.')
