"""Oluşturulmuş review sahnelerini çoğaltmadan altı görünüşü yeniler."""
import bpy
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent
for label in ('Empty','Fits'):
    s=bpy.data.scenes['Review_Board_6x6_'+label];cam=s.camera
    s.view_settings.view_transform='Standard';s.view_settings.exposure=-.8
    for view,pos,scale,res in [('Top',(0,0,12),7.35,(1500,1500)),('Perspective40',(4,-8.5,7.90),8.4,(1600,1400)),('Side',(0,-12,-.02),7.2,(1600,500))]:
        cam.location=pos;cam.rotation_euler=(Vector((0,0,-.02))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
        s.render.resolution_x=res[0];s.render.resolution_y=res[1]
        s.render.filepath=str(OUT/(label+'_'+view+'.png'));bpy.ops.render.render(write_still=True,scene=s.name)
bpy.context.window.scene=bpy.data.scenes['AssetPreview']
for filename in ('generate_board.py','verify_board.py','render_board.py','render_existing_board.py'):
    name='Board_6x6_'+filename
    t=bpy.data.texts.get(name) or bpy.data.texts.new(name);t.clear();t.write((OUT/filename).read_text(encoding='utf-8'))
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('All six final renders saved and scripts embedded; AssetPreview saved.')
