import bpy,math,os,json
from mathutils import Vector
# Proje kökü script dosyasından veya açık Blender dosyasından bulunur; kullanıcıya özel yol saklanmaz.
import os
from pathlib import Path
_PROJECT_ROOT = next((p for p in Path(globals().get("__file__", bpy.data.filepath or os.getcwd())).resolve().parents
                      if (p / "AGENTS.md").is_file()), None)
if _PROJECT_ROOT is None:
    raise RuntimeError("Scripti Block Rift proje klasöründen veya proje içindeki Blender dosyasından çalıştırın.")
OUT=str(_PROJECT_ROOT / 'ArtProduction/Obstacle_1x1_Stone/Final')
obj=bpy.data.objects["Obstacle_1x1_Stone_Export"]
assert obj.get("stone_final_refine")
review=bpy.data.scenes["BR_Stone_1x1_Review"];bpy.context.window.scene=review
camera=review.camera;center=obj.location+Vector((0,0,.15))
def render(name,direction,scale=1.55):
    camera.location=center+Vector(direction)
    camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO';camera.data.ortho_scale=scale
    review.render.filepath=os.path.join(OUT,name+".png");bpy.ops.render.render(write_still=True)
render("Top",(0,0,3))
e=math.radians(40);a=math.radians(22)
render("Perspective",(2.6*math.cos(e)*math.sin(a),-2.6*math.cos(e)*math.cos(a),2.6*math.sin(e)))
compare=bpy.data.scenes["BR_Stone_Block_Comparison"]
compare.render.filepath=os.path.join(OUT,"Compare_Block_1x1.png")
bpy.context.window.scene=compare;bpy.ops.render.render(write_still=True)
bpy.context.window.scene=bpy.data.scenes["AssetPreview"]
for o in bpy.context.scene.objects:o.select_set(False)
obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.wm.save_as_mainfile(filepath=str(_PROJECT_ROOT / 'ArtProduction/AssetPreview/AssetPreview.blend'))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"Obstacle_1x1_Stone_Final.blend"),copy=True)
print("FINAL_STONE_SAVED_NO_EXPORT")
