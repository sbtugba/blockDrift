import bpy, math, os
from mathutils import Vector
# Proje kökü script dosyasından veya açık Blender dosyasından bulunur; kullanıcıya özel yol saklanmaz.
import os
from pathlib import Path
_PROJECT_ROOT = next((p for p in Path(globals().get("__file__", bpy.data.filepath or os.getcwd())).resolve().parents
                      if (p / "AGENTS.md").is_file()), None)
if _PROJECT_ROOT is None:
    raise RuntimeError("Scripti Block Rift proje klasöründen veya proje içindeki Blender dosyasından çalıştırın.")
OUT=str(_PROJECT_ROOT / 'ArtProduction/Special_1x1_Star')
family=bpy.data.scenes['AssetPreview'];source=family.objects['Special_1x1_Star_Export']
review=bpy.data.scenes.new('Review_Special_1x1_Star')
review.render.engine=family.render.engine
review.render.resolution_x=1100;review.render.resolution_y=1100;review.render.resolution_percentage=100
review.render.image_settings.file_format='PNG'
review.view_settings.view_transform=family.view_settings.view_transform
review.view_settings.look=family.view_settings.look
review.view_settings.view_transform='Standard'
review.view_settings.exposure=-.6
studio=bpy.data.collections.new('Preview_Studio_Star');review.collection.children.link(studio)
world=bpy.data.worlds.new('Star_Review_World');world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs[0].default_value=(.65,.65,.65,1);bg.inputs[1].default_value=.35;review.world=world
for name,pos,power,size in [('Key',(-3,-4,5),350,4),('Fill',(4,-1,3),100,4),('Rim',(1,3,4),200,3)]:
    data=bpy.data.lights.new('Star_Studio_'+name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    ob=bpy.data.objects.new(data.name,data);studio.objects.link(ob);ob.location=pos
    ob.rotation_euler=(Vector((0,0,.1))-ob.location).to_track_quat('-Z','Y').to_euler()
ground=family.objects['ReviewGround.002'].copy();ground.name='Star_Studio_Ground';studio.objects.link(ground)
ground.location=(0,0,-.003)
cam=bpy.data.objects.new('Star_Review_Camera',bpy.data.cameras.new('Star_Review_Camera'))
studio.objects.link(cam);review.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=1.5
model=source.copy();model.name='Special_1x1_Star_Review';review.collection.objects.link(model);model.location=(0,0,0)
target=Vector((0,0,.14))
for name,pos in [('Top',(0,0,4)),('Perspective40',(1.45,-2.5,2.565)),('Side',(0,-4,.15))]:
    cam.location=pos;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    review.render.filepath=os.path.join(OUT,name+'.png')
    bpy.ops.render.render(write_still=True,scene=review.name)
bpy.context.window.scene=family
for o in bpy.context.selected_objects:o.select_set(False)
source.select_set(True);bpy.context.view_layer.objects.active=source
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        with bpy.context.temp_override(area=area,region=next(r for r in area.regions if r.type=='WINDOW')):
            bpy.ops.view3d.view_selected(use_all_regions=False)
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('Top, Perspective40 and Side rendered. Main model selected. Review setup is separate from original studio.')
