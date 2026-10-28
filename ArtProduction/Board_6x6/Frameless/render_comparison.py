import bpy,json
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent;sources=bpy.data.scenes['AssetPreview']
meta=json.loads((OUT/'variant_metadata.json').read_text())
assert not bpy.data.scenes.get('Review_Frameless_Empty'),'Review exists; refuse duplication.'
studio=bpy.data.collections.new('Preview_Studio_Frameless_NoGround')
oldstudio=bpy.data.collections['Preview_Studio_Board_6x6']
for src in oldstudio.objects:
    if src.type!='LIGHT':continue
    o=src.copy();o.data=src.data.copy();o.name='Frameless_'+src.name;studio.objects.link(o)
cam=bpy.data.objects.new('Frameless_Review_Camera',bpy.data.cameras.new('Frameless_Review_Camera'));studio.objects.link(cam);cam.data.type='ORTHO'
world=bpy.data.scenes['Review_Board_6x6_Empty'].world.copy();world.name='Frameless_Review_World'
def geometry(label,names):
    c=bpy.data.collections.new(label)
    for name in names:
        src=sources.objects[name];o=src.copy();o.name='Compare_'+label+'_'+name;loc=src.location.copy();o.parent=None;o.location=loc;c.objects.link(o)
    return c
old=geometry('Framed_Comparison_Meshes',['Board_6x6_Base','Board_6x6_Frame','Board_6x6_Cells'])
new=geometry('Frameless_Comparison_Meshes',[meta['support'],meta['cells']])
fits=geometry('Frameless_Fit_Review',[entry['name'] for entry in meta['fits']])
for label,collection,usefits in [('Framed',old,False),('Frameless',new,False),('FramelessFits',new,True)]:
    s=bpy.data.scenes.new('Review_'+('Frameless_Empty' if label=='Frameless' else 'Frameless_'+label));s.collection.children.link(studio);s.collection.children.link(collection)
    if usefits:s.collection.children.link(fits)
    s.world=world;s.camera=cam;s.render.engine=sources.render.engine
    s.render.film_transparent=True;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.resolution_percentage=100
    s.view_settings.view_transform='Standard';s.view_settings.exposure=-.8
    for view,pos,scale,res in [('Top',(0,0,12),7.35,(1500,1500)),('Perspective40',(4,-8.5,7.9),8.4,(1600,1400)),('Side',(0,-12,-.02),7.2,(1600,500))]:
        cam.location=pos;cam.rotation_euler=(Vector((0,0,-.02))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
        s.render.resolution_x=res[0];s.render.resolution_y=res[1];s.render.filepath=str(OUT/(label+'_'+view+'.png'));bpy.ops.render.render(write_still=True,scene=s.name)
bpy.context.window.scene=sources
for o in bpy.context.selected_objects:o.select_set(False)
for name in (meta['support'],meta['cells']):sources.objects[name].select_set(True)
bpy.context.view_layer.objects.active=sources.objects[meta['cells']]
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        with bpy.context.temp_override(area=area,region=next(r for r in area.regions if r.type=='WINDOW')):bpy.ops.view3d.view_selected(use_all_regions=False)
for name in ('create_frameless.py','verify_frameless.py','render_comparison.py'):
    textname='Board_6x6_Frameless_'+name;t=bpy.data.texts.get(textname) or bpy.data.texts.new(textname);t.clear();t.write((OUT/name).read_text(encoding='utf-8'))
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('Nine transparent-background renders saved. No ground or environment mesh created.')
